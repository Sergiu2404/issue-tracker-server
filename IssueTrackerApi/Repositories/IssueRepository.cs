using IssueTrackerApi.Data;
using IssueTrackerApi.Models;
using IssueTrackerApi.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace IssueTrackerApi.Repositories;

public class IssueRepository : IIssueRepository
{
    private readonly AppDbContext _db;

    public IssueRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<IssueReport>> GetAllAsync()
    {
        return await _db.IssueReports
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();
    }

    public async Task<IssueReport?> GetByIdAsync(Guid id)
    {
        return await _db.IssueReports.FindAsync(id);
    }

    public async Task<IssueReport> CreateAsync(IssueReport issue)
    {
        _db.IssueReports.Add(issue);
        await _db.SaveChangesAsync();
        return issue;
    }

    public async Task<IssueReport> UpdateAsync(IssueReport issue)
    {
        _db.IssueReports.Update(issue);
        await _db.SaveChangesAsync();
        return issue;
    }

    public async Task DeleteAsync(IssueReport issue)
    {
        _db.IssueReports.Remove(issue);
        await _db.SaveChangesAsync();
    }
}
