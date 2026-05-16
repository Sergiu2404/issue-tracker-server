using IssueTrackerApi.Models;
using Microsoft.EntityFrameworkCore;

namespace IssueTrackerApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<IssueReport> IssueReports => Set<IssueReport>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IssueReport>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.Location).HasMaxLength(300).IsRequired();
            entity.Property(e => e.UserId).HasMaxLength(200).IsRequired();
            entity.Property(e => e.UserName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.ThumbnailUrl).HasMaxLength(1000);
            entity.Property(e => e.OriginalImageUrl).HasMaxLength(1000);
            entity.HasIndex(e => e.CreatedAt);
        });
    }
}
