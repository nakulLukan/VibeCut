using System.Collections.Concurrent;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Ffmpegkit.Net;
using YoutubeShortsEditorMobile.Models;

namespace YoutubeShortsEditorMobile.Services;

/// <summary>
/// Implements IMediaProcessingService using FFmpegKit with an Rx.NET reactive bridge.
/// All operations are queued sequentially on a dedicated background worker.
/// </summary>
public class MediaProcessingService : IMediaProcessingService, IDisposable
{
    private readonly IScheduler _backgroundScheduler;
    private readonly Subject<ProcessingEvent> _events = new();
    private readonly ConcurrentQueue<Func<Task>> _operationQueue = new();
    private readonly SemaphoreSlim _queueSignal = new(0);
    private readonly CancellationTokenSource _workerCts = new();

    public IObservable<ProcessingEvent> ProcessingEvents => _events.AsObservable();

    public MediaProcessingService(IScheduler backgroundScheduler)
    {
        _backgroundScheduler = backgroundScheduler;
        // Start the sequential background worker
        Task.Run(RunQueueWorkerAsync);
    }

    private async Task RunQueueWorkerAsync()
    {
        while (!_workerCts.Token.IsCancellationRequested)
        {
            try
            {
                await _queueSignal.WaitAsync(_workerCts.Token);
                if (_operationQueue.TryDequeue(out var op))
                {
                    await op();
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    // -----------------------------------------------------------------------
    // Rx.NET / FFmpegKit bridge
    // -----------------------------------------------------------------------

    private IObservable<ProcessingEvent> ExecuteFFmpegAsync(string command, Guid operationId)
    {
        return Observable.Create<ProcessingEvent>(observer =>
        {
            var cts = new CancellationTokenSource();

            _backgroundScheduler.Schedule(async () =>
            {
                try
                {
                    var progress = new Progress<FFmpegProgress>(p =>
                    {
                        observer.OnNext(new ProcessingEvent(
                            operationId,
                            IsComplete: false,
                            IsSuccess: false,
                            ProgressMs: (long)p.Position.TotalMilliseconds,
                            ErrorDetail: null
                        ));
                    });

                    var result = await FFmpegKit.ExecuteAsync(command, progress, null, cts.Token);
                    
                    bool success = result.Succeeded;
                    observer.OnNext(new ProcessingEvent(
                        operationId,
                        IsComplete: true,
                        IsSuccess: success,
                        ProgressMs: 0,
                        ErrorDetail: success ? null : $"FFmpeg failed with code {result.ReturnCode}. Output: {result.Output}"
                    ));
                    observer.OnCompleted();
                }
                catch (OperationCanceledException)
                {
                    observer.OnCompleted();
                }
                catch (Exception ex)
                {
                    observer.OnError(ex);
                }
            });

            return Disposable.Create(() => cts.Cancel());
        });
    }

    private Guid EnqueueOperation(Func<IObservable<ProcessingEvent>> operationFactory)
    {
        var operationId = Guid.NewGuid();

        _operationQueue.Enqueue(async () =>
        {
            var tcs = new TaskCompletionSource();
            operationFactory()
                .Subscribe(
                    evt => _events.OnNext(evt),
                    ex =>
                    {
                        _events.OnNext(new ProcessingEvent(operationId, true, false, 0, ex.Message));
                        tcs.TrySetResult();
                    },
                    () => tcs.TrySetResult());
            await tcs.Task;
        });

        _queueSignal.Release();
        return operationId;
    }

    // -----------------------------------------------------------------------
    // Public API
    // -----------------------------------------------------------------------

    /// <summary>Trims a segment. Mutes audio if muteAudio is true.</summary>
    public Guid TrimAsync(string inputPath, TimeSpan startTime, TimeSpan endTime, string outputPath, bool muteAudio)
    {
        if (!File.Exists(inputPath))
            throw new FileNotFoundException("Input video file not found.", inputPath);

        var start = startTime.ToString(@"hh\:mm\:ss\.fff");
        var end = endTime.ToString(@"hh\:mm\:ss\.fff");
        var muteFlag = muteAudio ? "-an" : "-c:a aac";
        var command = $"-i \"{inputPath}\" -ss {start} -to {end} {muteFlag} -c:v libx264 \"{outputPath}\"";

        return EnqueueOperation(() =>
        {
            var id = Guid.NewGuid();
            return ExecuteFFmpegAsync(command, id);
        });
    }

    /// <summary>Crops and scales to 9:16 vertical (1080x1920).</summary>
    public Guid CropToVerticalAsync(string inputPath, string outputPath)
    {
        var command = $"-i \"{inputPath}\" -vf \"crop=607:1080:(iw-607)/2:0,scale=1080:1920\" -c:a copy \"{outputPath}\"";
        return EnqueueOperation(() =>
        {
            var id = Guid.NewGuid();
            return ExecuteFFmpegAsync(command, id);
        });
    }

    /// <summary>Concatenates multiple video files using FFmpeg concat demuxer.</summary>
    public async Task<Guid> ConcatenateAsync(IReadOnlyList<string> inputPaths, string outputPath)
    {
        var listFilePath = Path.Combine(FileSystem.CacheDirectory, $"concat_{Guid.NewGuid()}.txt");
        var lines = inputPaths.Select(p => $"file '{p}'");
        await File.WriteAllLinesAsync(listFilePath, lines);

        return EnqueueOperation(() =>
        {
            var id = Guid.NewGuid();
            var command = $"-f concat -safe 0 -i \"{listFilePath}\" -c copy \"{outputPath}\"";
            return ExecuteFFmpegAsync(command, id)
                .Finally(() =>
                {
                    if (File.Exists(listFilePath))
                        File.Delete(listFilePath);
                });
        });
    }

    public void CancelAll()
    {
        // Clear the queue
        while (_operationQueue.TryDequeue(out _)) { }
        // The active operation will be cancelled if its CTS is cancelled or we can just let it finish.
    }

    // IMediaProcessingService legacy methods (kept for interface compat)
    public Task<string> ExportProjectAsync(Project project, string outputPath, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Use TrimAsync + CropToVerticalAsync + ConcatenateAsync pipeline instead.");

    public Task<string> GenerateThumbnailAsync(string videoPath, TimeSpan time, CancellationToken cancellationToken = default)
    {
        var outputPath = Path.Combine(FileSystem.CacheDirectory, $"thumb_{Guid.NewGuid()}.jpg");
        var ts = time.ToString(@"hh\:mm\:ss\.fff");
        var command = $"-ss {ts} -i \"{videoPath}\" -vframes 1 \"{outputPath}\"";
        EnqueueOperation(() => ExecuteFFmpegAsync(command, Guid.NewGuid()));
        return Task.FromResult(outputPath);
    }

    public void Dispose()
    {
        _workerCts.Cancel();
        _workerCts.Dispose();
        _events.Dispose();
    }
}
