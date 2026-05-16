namespace IssueTrackerApi.Models;

public class IssueReport
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public IssueStatus Status { get; set; } = IssueStatus.Pending;
    public string? ThumbnailUrl { get; set; }
    public string? OriginalImageUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum IssueStatus
{
    Pending,
    Investigating,
    Fixed
}
