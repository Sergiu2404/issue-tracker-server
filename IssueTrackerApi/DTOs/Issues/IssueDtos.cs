namespace IssueTrackerApi.DTOs.Issues;

public class IssueResponseDto
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string? OriginalImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateIssueDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public IFormFile? Image { get; set; }
}

public class UpdateStatusDto
{
    public string Status { get; set; } = string.Empty;
}
