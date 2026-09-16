namespace YoutubeShortsEditorMobile.Models;

/// <summary>
/// Represents the lifecycle state of a YouTube video download.
/// </summary>
public enum DownloadState
{
    Pending,
    Downloading,
    Completed,
    Failed
}
