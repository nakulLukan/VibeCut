# Contract: IMediaProcessingService

**Layer**: Service
**File**: `Services/IMediaProcessingService.cs`
**Lifetime**: Singleton

---

## Responsibility

Wraps all FFmpegKit operations for media manipulation: trimming segments, muting audio, applying 9:16 crop, concatenating segments into a single output file, and running audio volume analysis. Exposes results and progress via Rx.NET observables.

---

## Interface

```csharp
/// <summary>
/// Provides FFmpeg-based video processing operations.
/// All operations are non-blocking and communicate progress via <see cref="ProcessingEvents"/>.
/// </summary>
public interface IMediaProcessingService
{
    /// <summary>
    /// Hot observable of processing progress/completion events.
    /// Each event carries an <see cref="ProcessingEvent.OperationId"/> to correlate with the originating call.
    /// </summary>
    IObservable<ProcessingEvent> ProcessingEvents { get; }

    /// <summary>
    /// Trims a video file between <paramref name="start"/> and <paramref name="end"/>,
    /// optionally muting audio, writing to <paramref name="outputPath"/>.
    /// Returns the operation ID for correlating <see cref="ProcessingEvents"/>.
    /// </summary>
    Task<Guid> TrimAsync(string inputPath, TimeSpan start, TimeSpan end,
                         bool muteAudio, string outputPath, CancellationToken ct = default);

    /// <summary>
    /// Applies a 9:16 vertical crop to the input video, writing to <paramref name="outputPath"/>.
    /// Returns the operation ID.
    /// </summary>
    Task<Guid> CropToVerticalAsync(string inputPath, string outputPath, CancellationToken ct = default);

    /// <summary>
    /// Concatenates an ordered list of video files into a single output MP4.
    /// Returns the operation ID.
    /// </summary>
    Task<Guid> ConcatenateAsync(IReadOnlyList<string> inputPaths, string outputPath, CancellationToken ct = default);

    /// <summary>
    /// Cancels all active or queued FFmpeg sessions.
    /// </summary>
    void CancelAll();
}
```

---

## Error Scenarios

| Scenario | Behaviour |
|---|---|
| FFmpeg returns non-zero exit code | `ProcessingEvent.IsSuccess = false`; `ErrorDetail` populated with fail stack |
| Input file not found | `FileNotFoundException` thrown synchronously before FFmpeg invocation |
| Concurrent operations | Queued; processed sequentially (one active FFmpegKit session at a time) |
| Cancellation requested | Active FFmpegKit session cancelled; `ProcessingEvent` with `IsComplete=true, IsSuccess=false` emitted |
