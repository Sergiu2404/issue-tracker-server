using IssueTrackerApi.DTOs.Issues;

namespace IssueTrackerApi.Services.Interfaces;

public interface IIssueService
{
    Task<IEnumerable<IssueResponseDto>> GetAllAsync();
    Task<IssueResponseDto?> GetByIdAsync(Guid id);
    Task<IssueResponseDto> CreateAsync(CreateIssueDto dto, string userId, string userName);
    Task<IssueResponseDto?> UpdateStatusAsync(Guid id, string status);
    Task<bool> DeleteAsync(Guid id, string userId);
}
