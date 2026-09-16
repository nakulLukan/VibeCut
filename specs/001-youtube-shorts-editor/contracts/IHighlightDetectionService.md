# Contract: IHighlightDetectionService

**Layer**: Service
**File**: `Services/IHighlightDetectionService.cs`
**Lifetime**: Singleton

---

## Responsibility

Runs FFmpeg `volumedetect` audio filter against a local video file, parses the output log to identify time windows of high audio activity, and returns an ordered list of suggested clip segments ranked by activity score.

---

## Interface

```csharp
/// <summary>
/// Analyses a local video file and returns suggested high-engagement clip segments
/// based on audio volume activity heuristics.
/// </summary>
public interface IHighlightDetectionService
{
    /// <summary>
    /// Analyses the video at <paramref name="videoPath"/> and returns suggested highlight segments.
    /// Emits suggestions progressively via the returned observable; completes when analysis finishes.
    /// If no distinct segments are found, the sequence completes with zero items.
    /// </summary>
    IObservable<HighlightSuggestion> DetectHighlightsAsync(string videoPath, CancellationToken ct = default);
}
```

---

## Algorithm (implementation hint for tasks phase)

1. Run: `ffmpeg -i {videoPath} -af "silencedetect=noise=-30dB:d=0.5" -f null /dev/null`
2. Parse stderr for `silence_start` / `silence_end` timestamps.
3. Invert silence windows to obtain audio-active windows.
4. Score each active window by duration and peak volume.
5. Filter windows shorter than 5 seconds or longer than 60 seconds.
6. Return sorted by `ActivityScore` descending.

---

## Error Scenarios

| Scenario | Behaviour |
|---|---|
| Video file not found | `FileNotFoundException` |
| Video has no audio track | Returns empty observable (completes immediately) |
| FFmpeg analysis fails | Observable terminates with `FFmpegException` |
| Cancellation requested | Observable terminates cleanly |
