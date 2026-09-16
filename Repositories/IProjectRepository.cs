using YoutubeShortsEditorMobile.Models;

namespace YoutubeShortsEditorMobile.Repositories;

/// <summary>
/// Repository for managing Project and ClipSegment entities in the local SQLite database.
/// </summary>
public interface IProjectRepository
{
    IAsyncEnumerable<Project> GetAllProjectsAsync(CancellationToken cancellationToken = default);
    Task<Project?> GetProjectByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Project> CreateProjectAsync(Project project, CancellationToken cancellationToken = default);
    Task<Project> UpdateProjectAsync(Project project, CancellationToken cancellationToken = default);
    Task DeleteProjectAsync(Guid id, CancellationToken cancellationToken = default);

    Task<List<ClipSegment>> GetSegmentsAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<ClipSegment> AddSegmentAsync(ClipSegment segment, CancellationToken cancellationToken = default);
    Task<ClipSegment> UpdateSegmentAsync(ClipSegment segment, CancellationToken cancellationToken = default);
    Task DeleteSegmentAsync(Guid segmentId, CancellationToken cancellationToken = default);
    Task NormalizeSequenceOrderAsync(Guid projectId, CancellationToken cancellationToken = default);
}
