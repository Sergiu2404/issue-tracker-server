namespace IssueTrackerApi.Services.Interfaces;

public interface IS3Service
{
    Task<string> UploadOriginalAsync(IFormFile file, Guid issueId);
    string GetThumbnailUrl(Guid issueId);
    Task DeleteImagesAsync(Guid issueId);
}
