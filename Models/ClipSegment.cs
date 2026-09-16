using System.ComponentModel.DataAnnotations;

namespace YoutubeShortsEditorMobile.Models;

/// <summary>
/// Represents one contiguous slice of the source video placed on the timeline.
/// </summary>
public class ClipSegment
{
    public Guid Id { get; set; }
    
    [Required]
    public Guid ProjectId { get; set; }
    
    [Required]
    public TimeSpan StartTime { get; set; }
    
    [Required]
    public TimeSpan EndTime { get; set; }
    
    [Required]
    public int SequenceOrder { get; set; }
    
    public bool IsAudioEnabled { get; set; } = true;
    
    public bool IsCropApplied { get; set; } = false;
    
    public Project? Project { get; set; }
}
