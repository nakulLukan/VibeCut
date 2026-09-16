# Contract: IYouTubeDownloadService

**Layer**: Service
**File**: `Services/IYouTubeDownloadService.cs`
**Lifetime**: Singleton

---

## Responsibility

Encapsulates all interaction with the YoutubeExplode library. Resolves YouTube video metadata, selects the best available stream, and downloads the video file to Android local storage while publishing real-time progress events via a reactive stream.

---

## Interface

```csharp
/// <summary>
/// Provides YouTube video metadata resolution and file download capabilities.
/// </summary>
public interface IYouTubeDownloadService
{
    /// <summary>
    /// Validates that the given URL points to a downloadable, public YouTube video.
    /// Returns video metadata on success, or throws <see cref="VideoUnavailableException"/>.
    /// </summary>
    Task<VideoMetadata> ResolveMetadataAsync(string url, CancellationToken ct = default);

    /// <summary>
    /// Begins downloading the video identified by <paramref name="projectId"/> to local storage.
    /// Progress and state changes are emitted on <see cref="DownloadEvents"/>.
    /// Throws <see cref="InvalidOperationException"/> if a download for this project is already active.
    /// </summary>
    Task StartDownloadAsync(Guid projectId, string url, string destinationPath, CancellationToken ct = default);

    /// <summary>
    /// Hot observable of download progress/state events for ALL projects.
    /// Subscribe and filter by <see cref="DownloadProgressEvent.ProjectId"/>.
    /// </summary>
    IObservable<DownloadProgressEvent> DownloadEvents { get; }

    /// <summary>
    /// Cancels an in-progress download for the given project.
    /// No-op if no download is active.
    /// </summary>
    void CancelDownload(Guid projectId);
}
```

---

## VideoMetadata (return type)

| Field | Type | Description |
|-------|------|-------------|
| `Title` | `string` | YouTube video title |
| `Duration` | `TimeSpan` | Video duration |
| `ThumbnailUrl` | `string` | Best-available thumbnail URL |
| `Author` | `string` | Channel name |

---

## Error Scenarios

| Scenario | Exception / Behaviour |
|---|---|
| Invalid / non-YouTube URL | `ArgumentException` from `ResolveMetadataAsync` |
| Private / deleted video | `VideoUnavailableException` (YoutubeExplode) |
| No network connectivity | `HttpRequestException`; `DownloadEvents` emits `DownloadState.Failed` |
| Duplicate download attempt | `InvalidOperationException` from `StartDownloadAsync` |
| Disk full mid-download | `IOException`; partial file deleted; `DownloadState.Failed` emitted |
