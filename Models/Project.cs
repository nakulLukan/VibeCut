using System.ComponentModel.DataAnnotations;

namespace YoutubeShortsEditorMobile.Models;

/// <summary>
/// Represents a single editing session tied to one source YouTube video.
/// </summary>
public class Project
{
    public Guid Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(2048)]
    public string SourceUrl { get; set; } = string.Empty;
    
    public string? LocalVideoPath { get; set; }
    
    public string? ThumbnailPath { get; set; }
    
    [Required]
    public DownloadState DownloadState { get; set; }
    
    public double DownloadProgress { get; set; }
    
    public string? ErrorMessage { get; set; }
    
    public DateTime CreatedOn { get; set; }
    
    public DateTime LastEditedOn { get; set; }
    
    public TimeSpan? Duration { get; set; }
    
    public ICollection<ClipSegment> Segments { get; set; } = new List<ClipSegment>();
}
