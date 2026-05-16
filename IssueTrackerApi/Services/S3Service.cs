using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using IssueTrackerApi.Services.Interfaces;

namespace IssueTrackerApi.Services;

public class S3Service : IS3Service
{
    private readonly string _bucketName;
    private readonly IAmazonS3 _s3;
    private readonly ILogger<S3Service> _logger;

    public S3Service(IConfiguration config, ILogger<S3Service> logger)
    {
        _bucketName = config["Aws:S3BucketName"]!;
        var region = RegionEndpoint.GetBySystemName(config["Aws:Region"]!);
        _s3 = new AmazonS3Client(region);
        _logger = logger;
    }

    public string GetPresignedUrl(string key, int expiryMinutes = 60)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = key,
            Expires = DateTime.UtcNow.AddMinutes(expiryMinutes),
            Protocol = Protocol.HTTPS,
            Verb = HttpVerb.GET,
        };
        return _s3.GetPreSignedURL(request);
    }

    public async Task<string> UploadOriginalAsync(IFormFile file, Guid issueId)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension is not (".jpg" or ".jpeg" or ".png"))
            throw new ArgumentException("Only JPG and PNG images are accepted.");

        var key = $"original/{issueId}.jpg";

        using var stream = file.OpenReadStream();
        var uploadRequest = new TransferUtilityUploadRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = stream,
            ContentType = "image/jpeg",
            //CannedACL = S3CannedACL.PublicRead,
        };

        var utility = new TransferUtility(_s3);
        await utility.UploadAsync(uploadRequest);

        return $"https://{_bucketName}.s3.amazonaws.com/{key}";
    }

    public string GetThumbnailUrl(Guid issueId)
    {
        return $"https://{_bucketName}.s3.amazonaws.com/thumbnails/{issueId}.jpg";
    }

    public async Task DeleteImagesAsync(Guid issueId)
    {
        try
        {
            await _s3.DeleteObjectAsync(_bucketName, $"original/{issueId}.jpg");
            await _s3.DeleteObjectAsync(_bucketName, $"thumbnails/{issueId}.jpg");
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to delete S3 images for {IssueId}: {Message}", issueId, ex.Message);
        }
    }
}
