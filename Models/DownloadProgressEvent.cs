namespace YoutubeShortsEditorMobile.Models;

/// <summary>
/// Event record representing the download progress of a project.
/// </summary>
public record DownloadProgressEvent(
    Guid ProjectId,
    double Progress,
    DownloadState State,
    string? ErrorMessage
);
