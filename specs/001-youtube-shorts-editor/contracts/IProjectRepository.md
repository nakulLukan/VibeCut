# Contract: IProjectRepository

**Layer**: Repository
**File**: `Repositories/IProjectRepository.cs`
**Lifetime**: Transient

---

## Responsibility

Provides all database access operations for the `Project` and `ClipSegment` entities via EF Core. All methods are async. Encapsulates LINQ queries so ViewModels never touch `AppDbContext` directly.

---

## Interface

```csharp
/// <summary>
/// Data access contract for Project and ClipSegment entities.
/// All operations are asynchronous.
/// </summary>
public interface IProjectRepository
{
    // ── Project Operations ──────────────────────────────────────────────────

    /// <summary>Returns all projects ordered by LastEditedOn descending.</summary>
    IAsyncEnumerable<Project> GetAllProjectsAsync(CancellationToken ct = default);

    /// <summary>Returns a single project by ID including its segments, or null.</summary>
    Task<Project?> GetProjectByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Inserts a new project record and returns it with a generated ID.</summary>
    Task<Project> CreateProjectAsync(Project project, CancellationToken ct = default);

    /// <summary>Persists all changes to an existing project (title, state, paths, etc.).</summary>
    Task UpdateProjectAsync(Project project, CancellationToken ct = default);

    /// <summary>Deletes a project and cascades to its segments.</summary>
    Task DeleteProjectAsync(Guid id, CancellationToken ct = default);

    // ── ClipSegment Operations ──────────────────────────────────────────────

    /// <summary>Returns all segments for a project ordered by SequenceOrder ascending.</summary>
    Task<List<ClipSegment>> GetSegmentsAsync(Guid projectId, CancellationToken ct = default);

    /// <summary>Inserts a new segment.</summary>
    Task<ClipSegment> AddSegmentAsync(ClipSegment segment, CancellationToken ct = default);

    /// <summary>Updates an existing segment (trim points, audio, crop, order).</summary>
    Task UpdateSegmentAsync(ClipSegment segment, CancellationToken ct = default);

    /// <summary>Deletes a segment by ID.</summary>
    Task DeleteSegmentAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Re-writes SequenceOrder for all segments of a project to ensure contiguous 0-based ordering.
    /// Called after reorder or delete operations.
    /// </summary>
    Task NormalizeSequenceOrderAsync(Guid projectId, CancellationToken ct = default);
}
```

---

## Query Notes

- `GetAllProjectsAsync` uses `IAsyncEnumerable<Project>` (via `AsAsyncEnumerable()`) so `MainViewModel` can bind reactively with `await foreach`.
- `GetProjectByIdAsync` uses `.Include(p => p.Segments).FirstOrDefaultAsync(...)`.
- All methods call `SaveChangesAsync()` internally after write operations.
