namespace IssueTrackerApi.Services.Interfaces;

public interface IS3Service
{
    string GetPresignedUrl(string key, int expiryMinutes = 60);
    Task<string> UploadOriginalAsync(IFormFile file, Guid issueId);
    string GetThumbnailUrl(Guid issueId);
    Task DeleteImagesAsync(Guid issueId);
}
