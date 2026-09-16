# Data Model: YouTube Shorts Editor

**Phase**: 1 — Design & Contracts
**Date**: 2026-09-15
**Feature**: [spec.md](spec.md) | **Research**: [research.md](research.md)

---

## Entities

### Project

Represents a single editing session linked to one YouTube source video.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| `Id` | `Guid` | PK, non-null | Auto-generated primary key |
| `Title` | `string` | non-null, max 200 | Derived from YouTube video metadata title |
| `SourceUrl` | `string` | non-null, max 2048 | Original YouTube URL submitted by user |
| `LocalVideoPath` | `string?` | nullable | Absolute path to downloaded video file; null while download is pending or failed |
| `ThumbnailPath` | `string?` | nullable | Path to extracted thumbnail image; null until generated |
| `DownloadState` | `DownloadState` (enum) | non-null | `Pending`, `Downloading`, `Completed`, `Failed` |
| `DownloadProgress` | `double` | 0.0–1.0 | Last recorded download progress fraction |
| `ErrorMessage` | `string?` | nullable | Human-readable error detail if `DownloadState == Failed` |
| `CreatedOn` | `DateTime` | non-null | UTC timestamp of project creation |
| `LastEditedOn` | `DateTime` | non-null | UTC timestamp of most recent edit |
| `Duration` | `TimeSpan?` | nullable | Total duration of the source video; populated after download |

**Navigation**:
- `ICollection<ClipSegment> Segments` — one-to-many relationship to ClipSegment

**EF Core Configuration** (via Fluent API in `AppDbContext.OnModelCreating`):
```csharp
entity.HasKey(p => p.Id);
entity.Property(p => p.Title).IsRequired().HasMaxLength(200);
entity.Property(p => p.SourceUrl).IsRequired().HasMaxLength(2048);
entity.HasMany(p => p.Segments)
      .WithOne(s => s.Project)
      .HasForeignKey(s => s.ProjectId)
      .OnDelete(DeleteBehavior.Cascade);
```

---

### ClipSegment

Represents one ordered slice of the source video on the editing timeline.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| `Id` | `Guid` | PK, non-null | Auto-generated primary key |
| `ProjectId` | `Guid` | FK → Project.Id, non-null | Owning project |
| `StartTime` | `TimeSpan` | non-null, ≥ 00:00:00 | In-point within source video |
| `EndTime` | `TimeSpan` | non-null, > StartTime | Out-point within source video |
| `SequenceOrder` | `int` | non-null, ≥ 0 | Zero-based position in the timeline |
| `IsAudioEnabled` | `bool` | non-null, default `true` | Whether audio is included in this segment |
| `IsCropApplied` | `bool` | non-null, default `false` | Whether 9:16 vertical crop is applied to this segment |

**Navigation**:
- `Project Project` — back-reference to owning Project

**EF Core Configuration**:
```csharp
entity.HasKey(s => s.Id);
entity.Property(s => s.SequenceOrder).IsRequired();
entity.HasIndex(s => new { s.ProjectId, s.SequenceOrder });  // fast ordering queries
entity.HasOne(s => s.Project)
      .WithMany(p => p.Segments)
      .HasForeignKey(s => s.ProjectId);
```

---

## Enumerations

### DownloadState

```csharp
public enum DownloadState
{
    Pending,      // Project created, download not yet started
    Downloading,  // Active download in progress
    Completed,    // Video file present on device
    Failed        // Download failed; ErrorMessage populated
}
```

---

## Domain Events / Reactive Messages

These are **not** persisted entities — they are in-memory Rx.NET message types used for cross-layer communication.

### DownloadProgressEvent

| Field | Type | Description |
|-------|------|-------------|
| `ProjectId` | `Guid` | Identifies which project this event belongs to |
| `Progress` | `double` | 0.0–1.0 fraction of bytes downloaded |
| `State` | `DownloadState` | Current download state |
| `ErrorMessage` | `string?` | Populated only when `State == Failed` |

### ProcessingEvent

| Field | Type | Description |
|-------|------|-------------|
| `OperationId` | `Guid` | Unique ID for this FFmpeg operation |
| `TimeMs` | `long` | Current encode position in milliseconds |
| `DurationMs` | `long` | Total expected duration in milliseconds |
| `IsComplete` | `bool` | True when FFmpeg session finished |
| `IsSuccess` | `bool` | True if return code = 0 |
| `ErrorDetail` | `string?` | FFmpeg fail stack trace if `IsSuccess == false` |

### HighlightSuggestion

| Field | Type | Description |
|-------|------|-------------|
| `StartTime` | `TimeSpan` | Detected high-activity segment start |
| `EndTime` | `TimeSpan` | Detected high-activity segment end |
| `ActivityScore` | `double` | Normalized 0.0–1.0 score (higher = more audio activity) |

---

## State Transitions

### Project.DownloadState

```
[Pending] ──(download started)──▶ [Downloading]
[Downloading] ──(success)──▶ [Completed]
[Downloading] ──(error / cancel)──▶ [Failed]
[Failed] ──(user retries)──▶ [Pending]
```

---

## Validation Rules

| Rule | Enforced At |
|------|-------------|
| `EndTime > StartTime` for every `ClipSegment` | Service layer before DB write |
| `SourceUrl` must match a YouTube URL pattern | `MainViewModel` before project creation |
| `LocalVideoPath` must exist on disk before export begins | `EditorViewModel.ExportCommand` guard |
| Timeline must have ≥ 1 segment before export | `EditorViewModel.ExportCommand` guard (FR-026) |
| `SequenceOrder` values must be contiguous (0, 1, 2, …) after reorder/delete | `ProjectRepository.NormalizeSequenceOrderAsync()` called after any reorder |

---
