using Microsoft.EntityFrameworkCore;
using YoutubeShortsEditorMobile.Data;
using YoutubeShortsEditorMobile.Models;

namespace YoutubeShortsEditorMobile.Repositories;

public class ProjectRepository : IProjectRepository
{
    private readonly AppDbContext _context;

    public ProjectRepository(AppDbContext context)
    {
        _context = context;
    }

    public IAsyncEnumerable<Project> GetAllProjectsAsync(CancellationToken cancellationToken = default)
    {
        return _context.Projects
            .OrderByDescending(p => p.LastEditedOn)
            .AsAsyncEnumerable();
    }

    public async Task<Project?> GetProjectByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Projects
            .Include(p => p.Segments)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Project> CreateProjectAsync(Project project, CancellationToken cancellationToken = default)
    {
        project.CreatedOn = DateTime.UtcNow;
        project.LastEditedOn = DateTime.UtcNow;
        _context.Projects.Add(project);
        await _context.SaveChangesAsync(cancellationToken);
        return project;
    }

    public async Task<Project> UpdateProjectAsync(Project project, CancellationToken cancellationToken = default)
    {
        project.LastEditedOn = DateTime.UtcNow;
        _context.Projects.Update(project);
        await _context.SaveChangesAsync(cancellationToken);
        return project;
    }

    public async Task DeleteProjectAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var project = await _context.Projects.FindAsync(new object[] { id }, cancellationToken);
        if (project != null)
        {
            _context.Projects.Remove(project);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<List<ClipSegment>> GetSegmentsAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await _context.ClipSegments
            .Where(s => s.ProjectId == projectId)
            .OrderBy(s => s.SequenceOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<ClipSegment> AddSegmentAsync(ClipSegment segment, CancellationToken cancellationToken = default)
    {
        _context.ClipSegments.Add(segment);
        await _context.SaveChangesAsync(cancellationToken);
        return segment;
    }

    public async Task<ClipSegment> UpdateSegmentAsync(ClipSegment segment, CancellationToken cancellationToken = default)
    {
        _context.ClipSegments.Update(segment);
        await _context.SaveChangesAsync(cancellationToken);
        return segment;
    }

    public async Task DeleteSegmentAsync(Guid segmentId, CancellationToken cancellationToken = default)
    {
        var segment = await _context.ClipSegments.FindAsync(new object[] { segmentId }, cancellationToken);
        if (segment != null)
        {
            _context.ClipSegments.Remove(segment);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task NormalizeSequenceOrderAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var segments = await _context.ClipSegments
            .Where(s => s.ProjectId == projectId)
            .OrderBy(s => s.SequenceOrder)
            .ToListAsync(cancellationToken);

        for (int i = 0; i < segments.Count; i++)
        {
            segments[i].SequenceOrder = i;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
