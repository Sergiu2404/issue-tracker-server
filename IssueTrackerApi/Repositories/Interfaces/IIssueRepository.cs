using IssueTrackerApi.Models;

namespace IssueTrackerApi.Repositories.Interfaces;

public interface IIssueRepository
{
    Task<IEnumerable<IssueReport>> GetAllAsync();
    Task<IssueReport?> GetByIdAsync(Guid id);
    Task<IssueReport> CreateAsync(IssueReport issue);
    Task<IssueReport> UpdateAsync(IssueReport issue);
    Task DeleteAsync(IssueReport issue);
}
