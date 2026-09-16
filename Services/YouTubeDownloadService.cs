using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using YoutubeExplode;
using YoutubeExplode.Exceptions;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;
using YoutubeShortsEditorMobile.Models;

namespace YoutubeShortsEditorMobile.Services;

public class YouTubeDownloadService : IYouTubeDownloadService, IDisposable
{
    private readonly YoutubeClient _youtubeClient;
    private readonly Subject<DownloadProgressEvent> _events = new();
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _activeDownloads = new();

    public IObservable<DownloadProgressEvent> DownloadEvents => _events.AsObservable();

    public YouTubeDownloadService(YoutubeClient youtubeClient)
    {
        _youtubeClient = youtubeClient;
    }

    public Task<bool> ValidateUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            var videoId = VideoId.Parse(url);
            return Task.FromResult(true);
        }
        catch (ArgumentException)
        {
            return Task.FromResult(false);
        }
    }

    private async Task<(string Title, TimeSpan? Duration, string ThumbnailUrl, string Author)> ResolveMetadataAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            var videoId = VideoId.Parse(url);
            var video = await _youtubeClient.Videos.GetAsync(videoId, cancellationToken);

            var thumbnailUrl = video.Thumbnails.OrderByDescending(t => t.Resolution.Area).FirstOrDefault()?.Url ?? "";

            return (video.Title, video.Duration, thumbnailUrl, video.Author.ChannelTitle);
        }
        catch (VideoUnavailableException ex)
        {
            throw new Exception("Video is unavailable.", ex);
        }
        catch (ArgumentException ex)
        {
            throw new Exception("Invalid YouTube URL.", ex);
        }
    }

    public async Task StartDownloadAsync(Project project, CancellationToken cancellationToken = default)
    {
        if (_activeDownloads.ContainsKey(project.Id))
        {
            throw new InvalidOperationException("A download is already in progress for this project.");
        }

        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _activeDownloads[project.Id] = cts;

        _events.OnNext(new DownloadProgressEvent(project.Id, 0, DownloadState.Downloading, null));

        try
        {
            var metadata = await ResolveMetadataAsync(project.SourceUrl, cts.Token);

            // Assuming local storage for the file
            var directory = FileSystem.AppDataDirectory;
            var fileName = $"{project.Id}.mp4";
            var destinationPath = Path.Combine(directory, fileName);

            var manifest = await _youtubeClient.Videos.Streams.GetManifestAsync(project.SourceUrl, cts.Token);
            var streamInfo = manifest.GetMuxedStreams().GetWithHighestVideoQuality();

            if (streamInfo == null)
            {
                throw new Exception("No suitable video stream found.");
            }

            var progressSubject = new Subject<double>();
            using var subscription = progressSubject
                .Sample(TimeSpan.FromMilliseconds(250))
                .Subscribe(p => _events.OnNext(new DownloadProgressEvent(project.Id, p, DownloadState.Downloading, null)));

            var progressReporter = new Progress<double>(p => progressSubject.OnNext(p));

            await _youtubeClient.Videos.Streams.DownloadAsync(streamInfo, destinationPath, progressReporter, cts.Token);

            project.LocalVideoPath = destinationPath;
            project.ThumbnailPath = metadata.ThumbnailUrl;
            project.Duration = metadata.Duration;
            project.Title = metadata.Title;

            _events.OnNext(new DownloadProgressEvent(project.Id, 1.0, DownloadState.Completed, null));
        }
        catch (OperationCanceledException)
        {
            _events.OnNext(new DownloadProgressEvent(project.Id, 0, DownloadState.Failed, "Download was canceled."));
        }
        catch (Exception ex)
        {
            // Cleanup partial file if needed
            var partialFile = Path.Combine(FileSystem.AppDataDirectory, $"{project.Id}.mp4");
            if (File.Exists(partialFile))
            {
                File.Delete(partialFile);
            }
            _events.OnNext(new DownloadProgressEvent(project.Id, 0, DownloadState.Failed, ex.Message));
        }
        finally
        {
            _activeDownloads.TryRemove(project.Id, out _);
            cts.Dispose();
        }
    }

    public void CancelDownload(Guid projectId)
    {
        if (_activeDownloads.TryGetValue(projectId, out var cts))
        {
            cts.Cancel();
        }
    }

    public void Dispose()
    {
        foreach (var cts in _activeDownloads.Values)
        {
            cts.Cancel();
            cts.Dispose();
        }
        _events.Dispose();
    }
}
