namespace YoutubeShortsEditorMobile.Models;

/// <summary>
/// Event record representing the progress or completion of a media processing operation (e.g. FFmpeg).
/// </summary>
public record ProcessingEvent(
    Guid OperationId,
    bool IsComplete,
    bool IsSuccess,
    long ProgressMs,
    string? ErrorDetail
);
