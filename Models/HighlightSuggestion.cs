namespace YoutubeShortsEditorMobile.Models;

/// <summary>
/// Record representing a suggested highlight segment.
/// </summary>
public record HighlightSuggestion(
    TimeSpan StartTime,
    TimeSpan EndTime,
    double ActivityScore
);
