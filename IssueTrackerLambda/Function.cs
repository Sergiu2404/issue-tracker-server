using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.S3;
using Amazon.S3.Model;
using Npgsql;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

// Register the Lambda serializer
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace IssueTrackerLambda;

public class Function
{
    //private readonly IAmazonS3 _s3 = new AmazonS3Client();
    private readonly IAmazonS3 _s3 = new AmazonS3Client(Amazon.RegionEndpoint.EUNorth1);
    private readonly string _bucketName = Environment.GetEnvironmentVariable("S3_BUCKET_NAME")!;
    private readonly string _dbConnectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")!;

    /// <summary>
    /// Triggered by S3 PUT on original/{issueId}.jpg
    /// 1. Downloads the original image
    /// 2. Resizes to 128×128 px (cover crop)
    /// 3. Uploads thumbnail to thumbnails/{issueId}.jpg
    /// 4. Updates the thumbnailUrl column in PostgreSQL
    /// </summary>
    public async Task FunctionHandler(S3Event s3Event, ILambdaContext context)
    {
        context.Logger.LogInformation($"Received event with {s3Event?.Records?.Count ?? 0} records");
        if (s3Event?.Records == null || s3Event.Records.Count == 0)
        {
            context.Logger.LogWarning("No records in event. Raw event may be malformed or trigger misconfigured.");
            return;
        }

        foreach (var record in s3Event.Records)
        {
            var key = record.S3.Object.Key;
            context.Logger.LogInformation($"Processing S3 object: {key}");

            if (!key.StartsWith("original/")) continue;

            var fileName = Path.GetFileName(key);
            var issueIdStr = Path.GetFileNameWithoutExtension(fileName);

            if (!Guid.TryParse(issueIdStr, out var issueId))
            {
                context.Logger.LogWarning($"Skipping — key doesn't contain a valid GUID: {key}");
                continue;
            }

            try
            {
                // get original from s3
                var getRequest = new GetObjectRequest
                {
                    BucketName = _bucketName,
                    Key = key,
                };

                using var response = await _s3.GetObjectAsync(getRequest);
                using var originalStream = new MemoryStream();
                await response.ResponseStream.CopyToAsync(originalStream);
                originalStream.Position = 0;

                // resize with ImageSharp
                using var image = await Image.LoadAsync(originalStream);
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(128, 128),
                    Mode = ResizeMode.Crop,
                }));

                using var thumbnailStream = new MemoryStream();
                await image.SaveAsJpegAsync(thumbnailStream);
                thumbnailStream.Position = 0;

                // upload thumbnail to S3
                var thumbnailKey = $"thumbnails/{issueId}.jpg";
                var putRequest = new PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = thumbnailKey,
                    InputStream = thumbnailStream,
                    ContentType = "image/jpeg",
                    //CannedACL = S3CannedACL.PublicRead,
                };

                await _s3.PutObjectAsync(putRequest);
                context.Logger.LogInformation($"Thumbnail uploaded: {thumbnailKey}");

                // update thumbnailUrl in postgres
                var thumbnailUrl = $"https://{_bucketName}.s3.amazonaws.com/{thumbnailKey}";

                await using var conn = new NpgsqlConnection(_dbConnectionString);
                await conn.OpenAsync();

                await using var cmd = new NpgsqlCommand(
                    @"UPDATE ""IssueReports"" SET ""ThumbnailUrl"" = @url WHERE ""Id"" = @id",
                    conn);
                cmd.Parameters.AddWithValue("url", thumbnailUrl);
                cmd.Parameters.AddWithValue("id", issueId);

                var rows = await cmd.ExecuteNonQueryAsync();
                context.Logger.LogInformation($"Updated {rows} row(s) in DB for issue {issueId}");
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"Error processing {key}: {ex.Message}");
                throw; // Let Lambda retry
            }
        }
    }
}
