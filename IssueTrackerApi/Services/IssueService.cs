using IssueTrackerApi.DTOs.Issues;
using IssueTrackerApi.Models;
using IssueTrackerApi.Repositories.Interfaces;
using IssueTrackerApi.Services.Interfaces;

namespace IssueTrackerApi.Services;

public class IssueService : IIssueService
{
    private readonly IIssueRepository _repo;
    private readonly IS3Service _s3;
    private readonly ILogger<IssueService> _logger;

    public IssueService(IIssueRepository repo, IS3Service s3, ILogger<IssueService> logger)
    {
        _repo = repo;
        _s3 = s3;
        _logger = logger;
    }

    public async Task<IEnumerable<IssueResponseDto>> GetAllAsync()
    {
        var issues = await _repo.GetAllAsync();
        return issues.Select(ToDto);
    }

    public async Task<IssueResponseDto?> GetByIdAsync(Guid id)
    {
        var issue = await _repo.GetByIdAsync(id);
        return issue is null ? null : ToDto(issue);
    }

    public async Task<IssueResponseDto> CreateAsync(CreateIssueDto dto, string userId, string userName)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            throw new ArgumentException("Title is required.");
        if (string.IsNullOrWhiteSpace(dto.Description))
            throw new ArgumentException("Description is required.");
        if (string.IsNullOrWhiteSpace(dto.Location))
            throw new ArgumentException("Location is required.");
        if (dto.Image is null || dto.Image.Length == 0)
            throw new ArgumentException("Image is required.");

        var issue = new IssueReport
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            UserName = userName,
            Title = dto.Title.Trim(),
            Description = dto.Description.Trim(),
            Location = dto.Location.Trim(),
            Status = IssueStatus.Pending,
            CreatedAt = DateTime.UtcNow,
        };

        issue.OriginalImageUrl = await _s3.UploadOriginalAsync(dto.Image, issue.Id);
        issue.ThumbnailUrl = _s3.GetThumbnailUrl(issue.Id);

        var created = await _repo.CreateAsync(issue);
        _logger.LogInformation("Issue {IssueId} created by {UserId}", created.Id, userId);
        return ToDto(created);
    }

    public async Task<IssueResponseDto?> UpdateStatusAsync(Guid id, string status)
    {
        if (!Enum.TryParse<IssueStatus>(status, ignoreCase: true, out var newStatus))
            throw new ArgumentException($"Invalid status '{status}'. Valid: Pending, Investigating, Fixed.");

        var issue = await _repo.GetByIdAsync(id);
        if (issue is null) return null;

        issue.Status = newStatus;
        var updated = await _repo.UpdateAsync(issue);
        return ToDto(updated);
    }

    public async Task<bool> DeleteAsync(Guid id, string userId)
    {
        var issue = await _repo.GetByIdAsync(id);
        if (issue is null) return false;

        if (issue.UserId != userId)
            throw new UnauthorizedAccessException("You can only delete your own issues.");

        await _s3.DeleteImagesAsync(issue.Id);
        await _repo.DeleteAsync(issue);
        return true;
    }

    private IssueResponseDto ToDto(IssueReport i) => new()
    {
        Id = i.Id,
        UserId = i.UserId,
        UserName = i.UserName,
        Title = i.Title,
        Description = i.Description,
        Location = i.Location,
        Status = i.Status.ToString(),
        //ThumbnailUrl = i.ThumbnailUrl,
        //OriginalImageUrl = i.OriginalImageUrl,
        ThumbnailUrl = _s3.GetPresignedUrl($"thumbnails/{i.Id}.jpg"),
        OriginalImageUrl = _s3.GetPresignedUrl($"original/{i.Id}.jpg"),
        CreatedAt = i.CreatedAt,
    };
}
