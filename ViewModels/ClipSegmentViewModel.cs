using CommunityToolkit.Mvvm.ComponentModel;
using YoutubeShortsEditorMobile.Models;

namespace YoutubeShortsEditorMobile.ViewModels;

public partial class ClipSegmentViewModel : ObservableObject
{
    private readonly ClipSegment _entity;

    public Guid Id { get; }
    public Guid ProjectId { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Duration))]
    private TimeSpan _startTime;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Duration))]
    private TimeSpan _endTime;

    [ObservableProperty]
    private int _sequenceOrder;

    [ObservableProperty]
    private bool _isAudioEnabled;

    [ObservableProperty]
    private bool _isCropApplied;

    public TimeSpan Duration => EndTime - StartTime;

    public ClipSegment Entity => _entity;

    public ClipSegmentViewModel(ClipSegment entity)
    {
        _entity = entity;
        Id = entity.Id;
        ProjectId = entity.ProjectId;
        _startTime = entity.StartTime;
        _endTime = entity.EndTime;
        _sequenceOrder = entity.SequenceOrder;
        _isAudioEnabled = entity.IsAudioEnabled;
        _isCropApplied = entity.IsCropApplied;
    }

    /// <summary>
    /// Syncs the VM state back into the entity for repository persistence.
    /// </summary>
    public void SyncToEntity()
    {
        _entity.StartTime = StartTime;
        _entity.EndTime = EndTime;
        _entity.SequenceOrder = SequenceOrder;
        _entity.IsAudioEnabled = IsAudioEnabled;
        _entity.IsCropApplied = IsCropApplied;
    }
}
