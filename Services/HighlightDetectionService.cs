using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using Ffmpegkit.Net;
using YoutubeShortsEditorMobile.Models;

namespace YoutubeShortsEditorMobile.Services;

public class HighlightDetectionService : IHighlightDetectionService
{
    private readonly IScheduler _backgroundScheduler;

    public HighlightDetectionService(IScheduler backgroundScheduler)
    {
        _backgroundScheduler = backgroundScheduler;
    }

    public IObservable<HighlightSuggestion> DetectHighlightsStreamAsync(string videoPath, CancellationToken cancellationToken = default)
    {
        return Observable.Create<HighlightSuggestion>(observer =>
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            _backgroundScheduler.Schedule(async () =>
            {
                try
                {
                    // Run silencedetect
                    var command = $"-i \"{videoPath}\" -af \"silencedetect=noise=-30dB:d=0.5\" -f null /dev/null";
                    var session = await FFmpegKit.ExecuteAsync(command, cancellationToken: cts.Token);

                    if (cts.IsCancellationRequested) return;

                    var success = session.Succeeded;
                    if (!success)
                    {
                        observer.OnError(new Exception($"FFmpeg silencedetect failed with code {session.ReturnCode}"));
                        return;
                    }

                    // Parse stderr for silence windows
                    var output = session.Output ?? string.Empty;

                    // Regex to find silence_start and silence_end
                    var startRegex = new Regex(@"silence_start:\s+([\d\.]+)");
                    var endRegex = new Regex(@"silence_end:\s+([\d\.]+)");

                    var starts = startRegex.Matches(output).Select(m => double.Parse(m.Groups[1].Value)).ToList();
                    var ends = endRegex.Matches(output).Select(m => double.Parse(m.Groups[1].Value)).ToList();

                    // Get total duration from FFProbe to calculate the last active window
                    var mediaInfo = await FFprobeKit.GetMediaInformationAsync(videoPath);
                    double totalDuration = mediaInfo?.Duration?.TotalSeconds ?? 0.0;

                    // Invert silence windows to find active (highlight) windows
                    var activeWindows = new List<(double Start, double End)>();
                    double currentStart = 0;

                    for (int i = 0; i < Math.Min(starts.Count, ends.Count); i++)
                    {
                        if (starts[i] > currentStart)
                        {
                            activeWindows.Add((currentStart, starts[i]));
                        }
                        currentStart = ends[i];
                    }

                    if (totalDuration > 0 && currentStart < totalDuration)
                    {
                        activeWindows.Add((currentStart, totalDuration));
                    }

                    // Filter and score
                    var validWindows = activeWindows
                        .Where(w => (w.End - w.Start) >= 5.0 && (w.End - w.Start) <= 60.0)
                        .ToList();

                    if (validWindows.Any())
                    {
                        var maxDuration = validWindows.Max(w => w.End - w.Start);

                        foreach (var window in validWindows)
                        {
                            if (cts.IsCancellationRequested) break;

                            var duration = window.End - window.Start;
                            var score = duration / maxDuration;

                            var suggestion = new HighlightSuggestion(
                                TimeSpan.FromSeconds(window.Start),
                                TimeSpan.FromSeconds(window.End),
                                score
                            );
                            observer.OnNext(suggestion);
                        }
                    }

                    observer.OnCompleted();
                }
                catch (Exception ex)
                {
                    observer.OnError(ex);
                }
            });

            return Disposable.Create(() =>
            {
                cts.Cancel();
            });
        });
    }

    // Keep the task-based signature for backward compat, wrapper over the observable stream
    public async Task<List<HighlightSuggestion>> DetectHighlightsAsync(string videoPath, CancellationToken cancellationToken = default)
    {
        var list = await DetectHighlightsStreamAsync(videoPath, cancellationToken).ToList();
        return list.ToList();
    }
}
