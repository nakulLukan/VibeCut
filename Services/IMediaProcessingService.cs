using YoutubeShortsEditorMobile.Models;

namespace YoutubeShortsEditorMobile.Services;

/// <summary>
/// Service for FFmpeg-powered media operations: trim, crop, concatenate.
/// All operations are reactive (Rx.NET) and queued sequentially.
/// </summary>
public interface IMediaProcessingService
{
    /// <summary>Hot observable stream of all in-flight processing events.</summary>
    IObservable<ProcessingEvent> ProcessingEvents { get; }

    /// <summary>Enqueues a trim operation. Returns the operationId to filter events.</summary>
    Guid TrimAsync(string inputPath, TimeSpan startTime, TimeSpan endTime, string outputPath, bool muteAudio);

    /// <summary>Enqueues a 9:16 crop+scale. Returns the operationId to filter events.</summary>
    Guid CropToVerticalAsync(string inputPath, string outputPath);

    /// <summary>Enqueues a concat demuxer operation. Returns the operationId to filter events.</summary>
    Task<Guid> ConcatenateAsync(IReadOnlyList<string> inputPaths, string outputPath);

    /// <summary>Cancels all in-flight and queued operations.</summary>
    void CancelAll();

    /// <summary>Legacy: Generate a single thumbnail frame.</summary>
    Task<string> GenerateThumbnailAsync(string videoPath, TimeSpan time, CancellationToken cancellationToken = default);

    /// <summary>Legacy: Full project export (wraps the pipeline above).</summary>
    Task<string> ExportProjectAsync(Project project, string outputPath, CancellationToken cancellationToken = default);
}
