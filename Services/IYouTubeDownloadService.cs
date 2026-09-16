using System.Reactive.Subjects;
using YoutubeShortsEditorMobile.Models;

namespace YoutubeShortsEditorMobile.Services;

public interface IYouTubeDownloadService
{
    IObservable<DownloadProgressEvent> DownloadEvents { get; }
    Task<bool> ValidateUrlAsync(string url, CancellationToken cancellationToken = default);
    Task StartDownloadAsync(Project project, CancellationToken cancellationToken = default);
    void CancelDownload(Guid projectId);
}
