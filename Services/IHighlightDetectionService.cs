using YoutubeShortsEditorMobile.Models;

namespace YoutubeShortsEditorMobile.Services;

/// <summary>
/// Service responsible for identifying high-engagement or active segments in a video.
/// </summary>
public interface IHighlightDetectionService
{
    /// <summary>
    /// Analyzes the video and returns a list of suggested highlight segments.
    /// </summary>
    Task<List<HighlightSuggestion>> DetectHighlightsAsync(string videoPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reactive stream of highlight suggestions as they are discovered.
    /// </summary>
    IObservable<HighlightSuggestion> DetectHighlightsStreamAsync(string videoPath, CancellationToken cancellationToken = default);
}
