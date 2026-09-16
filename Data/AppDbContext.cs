using Microsoft.EntityFrameworkCore;
using YoutubeShortsEditorMobile.Models;

namespace YoutubeShortsEditorMobile.Data;

/// <summary>
/// Database context for the application's local SQLite database.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Project> Projects { get; set; } = null!;
    public DbSet<ClipSegment> ClipSegments { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.SourceUrl).IsRequired().HasMaxLength(2048);
        });

        modelBuilder.Entity<ClipSegment>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // Composite index on ProjectId and SequenceOrder
            entity.HasIndex(e => new { e.ProjectId, e.SequenceOrder });

            // Cascade delete relationship
            entity.HasOne(e => e.Project)
                  .WithMany(p => p.Segments)
                  .HasForeignKey(e => e.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
