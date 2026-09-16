using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YoutubeShortsEditorMobile.Models;
using YoutubeShortsEditorMobile.Repositories;
using YoutubeShortsEditorMobile.Services;

namespace YoutubeShortsEditorMobile.ViewModels;

[QueryProperty(nameof(ProjectIdString), "projectId")]
public partial class EditorViewModel : ObservableObject, IDisposable
{
    private readonly IProjectRepository _repository;
    private readonly IMediaProcessingService _processingService;
    private readonly CompositeDisposable _disposables = new();

    // Receives the Shell query parameter as a string
    public string? ProjectIdString
    {
        get => CurrentProject?.Id.ToString();
        set
        {
            if (Guid.TryParse(value, out var id))
            {
                LoadProjectCommand.Execute(id);
            }
        }
    }

    [ObservableProperty]
    private Project? _currentProject;

    [ObservableProperty]
    private ObservableCollection<ClipSegmentViewModel> _segments = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotProcessing))]
    private bool _isProcessing;

    [ObservableProperty]
    private double _renderProgress;

    [ObservableProperty]
    private bool _isExportReady;

    [ObservableProperty]
    private TimeSpan _playheadPosition;

    [ObservableProperty]
    private string? _previewOutputPath;

    // Highlights state
    [ObservableProperty]
    private ObservableCollection<HighlightSuggestion> _highlightSuggestions = new();

    [ObservableProperty]
    private bool _isDetectingHighlights;

    [ObservableProperty]
    private bool _hasNoHighlights;

    // Export State
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotProcessing))]
    private bool _isExporting;

    [ObservableProperty]
    private double _exportProgress;

    [ObservableProperty]
    private bool _showExportResult;

    [ObservableProperty]
    private string? _exportedFilePath;

    [ObservableProperty]
    private TimeSpan _exportDuration;

    [ObservableProperty]
    private double _exportFileSizeMb;

    public bool IsNotProcessing => !IsProcessing && !IsExporting;

    private readonly IHighlightDetectionService _highlightService;

    public EditorViewModel(IProjectRepository repository, IMediaProcessingService processingService, IHighlightDetectionService highlightService)
    {
        _repository = repository;
        _processingService = processingService;
        _highlightService = highlightService;

        // T073 & T085: Subscribe to ProcessingEvents and drive UI progress
        var processingSubscription = _processingService.ProcessingEvents
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(evt =>
            {
                if (!evt.IsComplete)
                {
                    double progress = 0;
                    if (CurrentProject?.Duration is TimeSpan dur && dur.TotalMilliseconds > 0)
                        progress = Math.Min(1.0, evt.ProgressMs / dur.TotalMilliseconds);

                    if (IsExporting)
                    {
                        ExportProgress = progress;
#if ANDROID
                        UpdateAndroidNotification((int)(progress * 100), false, false);
#endif
                    }
                    else
                    {
                        IsProcessing = true;
                        RenderProgress = progress;
                    }
                }
                else
                {
                    if (IsExporting)
                    {
                        // Export handles completion in ExportAsync after Concatenate finishes
                    }
                    else
                    {
                        IsProcessing = false;
                        RenderProgress = evt.IsSuccess ? 1.0 : 0.0;
                    }
                }
            });
        _disposables.Add(processingSubscription);
    }

    [RelayCommand]
    private async Task LoadProjectAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await _repository.GetProjectByIdAsync(projectId, cancellationToken);
        if (project == null) return;

        CurrentProject = project;

        // Validate video file on disk
        if (string.IsNullOrEmpty(project.LocalVideoPath) || !File.Exists(project.LocalVideoPath))
        {
            IsExportReady = false;
        }
        else
        {
            IsExportReady = true;
        }

        project.LastEditedOn = DateTime.UtcNow;
        await _repository.UpdateProjectAsync(project, cancellationToken);

        Segments.Clear();
        var ordered = project.Segments.OrderBy(s => s.SequenceOrder);
        foreach (var seg in ordered)
        {
            Segments.Add(new ClipSegmentViewModel(seg));
        }
    }

    [RelayCommand(CanExecute = nameof(IsNotProcessing))]
    private async Task SplitSegmentAsync(ClipSegmentViewModel segment, CancellationToken cancellationToken)
    {
        if (segment == null) return;
        if (PlayheadPosition <= segment.StartTime || PlayheadPosition >= segment.EndTime) return;

        // Create two new segments split at the playhead
        var left = new ClipSegment
        {
            Id = Guid.NewGuid(),
            ProjectId = segment.ProjectId,
            StartTime = segment.StartTime,
            EndTime = PlayheadPosition,
            SequenceOrder = segment.SequenceOrder,
            IsAudioEnabled = segment.IsAudioEnabled,
            IsCropApplied = segment.IsCropApplied
        };
        var right = new ClipSegment
        {
            Id = Guid.NewGuid(),
            ProjectId = segment.ProjectId,
            StartTime = PlayheadPosition,
            EndTime = segment.EndTime,
            SequenceOrder = segment.SequenceOrder + 1,
            IsAudioEnabled = segment.IsAudioEnabled,
            IsCropApplied = segment.IsCropApplied
        };

        await _repository.DeleteSegmentAsync(segment.Id, cancellationToken);
        await _repository.AddSegmentAsync(left, cancellationToken);
        await _repository.AddSegmentAsync(right, cancellationToken);

        if (CurrentProject != null)
        {
            await _repository.NormalizeSequenceOrderAsync(CurrentProject.Id, cancellationToken);
            await RefreshSegmentsAsync(cancellationToken);
        }
    }

    [RelayCommand(CanExecute = nameof(IsNotProcessing))]
    private async Task UpdateSegmentTrimAsync(ClipSegmentViewModel segment, CancellationToken cancellationToken)
    {
        if (segment == null || segment.EndTime <= segment.StartTime) return;
        segment.SyncToEntity();
        await _repository.UpdateSegmentAsync(segment.Entity, cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(IsNotProcessing))]
    private async Task ToggleMuteAsync(ClipSegmentViewModel segment, CancellationToken cancellationToken)
    {
        if (segment == null) return;
        segment.IsAudioEnabled = !segment.IsAudioEnabled;
        segment.SyncToEntity();
        await _repository.UpdateSegmentAsync(segment.Entity, cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(IsNotProcessing))]
    private async Task ToggleCropAsync(ClipSegmentViewModel segment, CancellationToken cancellationToken)
    {
        if (segment == null) return;
        segment.IsCropApplied = !segment.IsCropApplied;
        segment.SyncToEntity();
        await _repository.UpdateSegmentAsync(segment.Entity, cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(IsNotProcessing))]
    private async Task DeleteSegmentAsync(ClipSegmentViewModel segment, CancellationToken cancellationToken)
    {
        if (segment == null) return;
        await _repository.DeleteSegmentAsync(segment.Id, cancellationToken);
        Segments.Remove(segment);

        if (CurrentProject != null)
        {
            await _repository.NormalizeSequenceOrderAsync(CurrentProject.Id, cancellationToken);
        }

        IsExportReady = Segments.Count > 0;
    }

    [RelayCommand(CanExecute = nameof(IsNotProcessing))]
    private async Task ReorderSegmentAsync((int oldIndex, int newIndex) args, CancellationToken cancellationToken)
    {
        var (oldIndex, newIndex) = args;
        if (oldIndex < 0 || newIndex < 0 || oldIndex >= Segments.Count || newIndex >= Segments.Count) return;

        var item = Segments[oldIndex];
        Segments.RemoveAt(oldIndex);
        Segments.Insert(newIndex, item);

        if (CurrentProject != null)
        {
            await _repository.NormalizeSequenceOrderAsync(CurrentProject.Id, cancellationToken);
        }
    }

    private async Task RefreshSegmentsAsync(CancellationToken cancellationToken)
    {
        if (CurrentProject == null) return;
        var segs = await _repository.GetSegmentsAsync(CurrentProject.Id, cancellationToken);
        Segments.Clear();
        foreach (var seg in segs.OrderBy(s => s.SequenceOrder))
        {
            Segments.Add(new ClipSegmentViewModel(seg));
        }
    }

    // T072: RenderPreviewCommand — chains Trim → Crop (if needed) → Concatenate for all segments
    [RelayCommand(CanExecute = nameof(IsNotProcessing))]
    private async Task RenderPreviewAsync(CancellationToken cancellationToken)
    {
        if (CurrentProject == null || Segments.Count == 0) return;

        IsProcessing = true;
        RenderProgress = 0;

        try
        {
            var tempFiles = new List<string>();
            var cacheDir = FileSystem.CacheDirectory;

            foreach (var seg in Segments.OrderBy(s => s.SequenceOrder))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var trimOutput = Path.Combine(cacheDir, $"trim_{seg.Id}.mp4");
                _processingService.TrimAsync(
                    CurrentProject.LocalVideoPath!,
                    seg.StartTime,
                    seg.EndTime,
                    trimOutput,
                    muteAudio: !seg.IsAudioEnabled);

                // Wait for trim to complete (ProcessingEvents subscription handles progress)
                await Task.Delay(100, cancellationToken); // yield to allow queue processing

                if (seg.IsCropApplied)
                {
                    var cropOutput = Path.Combine(cacheDir, $"crop_{seg.Id}.mp4");
                    _processingService.CropToVerticalAsync(trimOutput, cropOutput);
                    tempFiles.Add(cropOutput);
                }
                else
                {
                    tempFiles.Add(trimOutput);
                }
            }

            var previewPath = Path.Combine(cacheDir, $"preview_{CurrentProject.Id}.mp4");
            await _processingService.ConcatenateAsync(tempFiles, previewPath);

            PreviewOutputPath = previewPath;
        }
        catch (OperationCanceledException)
        {
            _processingService.CancelAll();
            IsProcessing = false;
        }
        catch (Exception)
        {
            IsProcessing = false;
            RenderProgress = 0;
        }
    }

    public void Dispose()
    {
        _processingService.CancelAll();
        _disposables.Dispose();
    }

    [RelayCommand]
    private void DetectHighlights()
    {
        if (CurrentProject?.LocalVideoPath == null || !File.Exists(CurrentProject.LocalVideoPath)) return;

        IsDetectingHighlights = true;
        HasNoHighlights = false;
        HighlightSuggestions.Clear();

        var sub = _highlightService.DetectHighlightsStreamAsync(CurrentProject.LocalVideoPath)
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(
                suggestion => HighlightSuggestions.Add(suggestion),
                ex => 
                {
                    IsDetectingHighlights = false;
                    // In a real app, we'd show an error snackbar here
                },
                () => 
                {
                    IsDetectingHighlights = false;
                    HasNoHighlights = HighlightSuggestions.Count == 0;
                }
            );

        _disposables.Add(sub);
    }

    [RelayCommand]
    private void PreviewHighlight(HighlightSuggestion suggestion)
    {
        if (suggestion == null) return;
        PlayheadPosition = suggestion.StartTime;
        // The actual play logic and stopping at EndTime will be handled in code-behind or a dedicated behavior, 
        // as we only control PlayheadPosition from the VM. We can set a property if needed, but for MVP
        // setting PlayheadPosition to StartTime is a good start.
    }

    [RelayCommand]
    private async Task AcceptHighlightAsync(HighlightSuggestion suggestion, CancellationToken cancellationToken)
    {
        if (suggestion == null || CurrentProject == null) return;

        var segment = new ClipSegment
        {
            Id = Guid.NewGuid(),
            ProjectId = CurrentProject.Id,
            StartTime = suggestion.StartTime,
            EndTime = suggestion.EndTime,
            SequenceOrder = Segments.Count,
            IsAudioEnabled = true,
            IsCropApplied = false
        };

        await _repository.AddSegmentAsync(segment, cancellationToken);
        Segments.Add(new ClipSegmentViewModel(segment));
        HighlightSuggestions.Remove(suggestion);

        IsExportReady = true;
    }

    [RelayCommand]
    private void DismissHighlight(HighlightSuggestion suggestion)
    {
        if (suggestion != null)
        {
            HighlightSuggestions.Remove(suggestion);
            if (HighlightSuggestions.Count == 0)
            {
                HasNoHighlights = true; // Optional: show empty state if user dismissed all
            }
        }
    }

    [RelayCommand(CanExecute = nameof(IsNotProcessing))]
    private async Task ExportAsync(CancellationToken cancellationToken)
    {
        if (CurrentProject == null || Segments.Count == 0) return;

        IsExporting = true;
        ExportProgress = 0;
        ShowExportResult = false;

        try
        {
            var tempFiles = new List<string>();
            var cacheDir = FileSystem.CacheDirectory;

            foreach (var seg in Segments.OrderBy(s => s.SequenceOrder))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var trimOutput = Path.Combine(cacheDir, $"trim_{seg.Id}.mp4");
                _processingService.TrimAsync(
                    CurrentProject.LocalVideoPath!,
                    seg.StartTime,
                    seg.EndTime,
                    trimOutput,
                    muteAudio: !seg.IsAudioEnabled);

                await Task.Delay(100, cancellationToken);

                if (seg.IsCropApplied)
                {
                    var cropOutput = Path.Combine(cacheDir, $"crop_{seg.Id}.mp4");
                    _processingService.CropToVerticalAsync(trimOutput, cropOutput);
                    tempFiles.Add(cropOutput);
                }
                else
                {
                    tempFiles.Add(trimOutput);
                }
            }

            var exportPath = Path.Combine(cacheDir, $"{CurrentProject.Id}_export.mp4");
            await _processingService.ConcatenateAsync(tempFiles, exportPath);

            ExportedFilePath = exportPath;
            
            if (File.Exists(exportPath))
            {
                var fileInfo = new FileInfo(exportPath);
                ExportFileSizeMb = fileInfo.Length / (1024.0 * 1024.0);
                ExportDuration = TimeSpan.FromSeconds(Segments.Sum(s => s.Duration.TotalSeconds));
                ShowExportResult = true;
            }
        }
        catch (IOException ex) when (ex.Message.Contains("No space left"))
        {
            // FR-027: Inform user about storage space
#if ANDROID
            UpdateAndroidNotification(0, false, true);
#endif
        }
        catch (OperationCanceledException)
        {
            _processingService.CancelAll();
#if ANDROID
            DismissAndroidNotification();
#endif
        }
        finally
        {
            IsExporting = false;
#if ANDROID
            if (ShowExportResult) 
                UpdateAndroidNotification(100, true, false);
#endif
        }
    }

#if ANDROID
    private int _notificationId = 1001;
    private Android.App.NotificationManager? _notificationManager;
    private AndroidX.Core.App.NotificationCompat.Builder? _notificationBuilder;

    private void UpdateAndroidNotification(int progressPercent, bool isComplete, bool isError)
    {
        var context = Android.App.Application.Context;
        _notificationManager ??= context.GetSystemService(Android.Content.Context.NotificationService) as Android.App.NotificationManager;
        if (_notificationManager == null) return;

        var channelId = "export_channel";
        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
        {
            var channel = new Android.App.NotificationChannel(channelId, "Exports", Android.App.NotificationImportance.Low);
            _notificationManager.CreateNotificationChannel(channel);
        }

#pragma warning disable CS8602
        if (_notificationBuilder == null)
        {
            _notificationBuilder = new AndroidX.Core.App.NotificationCompat.Builder(context, channelId)
                .SetContentTitle("Exporting Video")
                .SetSmallIcon(Android.Resource.Drawable.IcPopupSync) // Fallback icon
                .SetOngoing(true)
                .SetOnlyAlertOnce(true);
        }
        if (isError)
        {
            _notificationBuilder!.SetContentTitle("Export Failed")
                .SetContentText("Insufficient storage space.")
                .SetProgress(0, 0, false)
                .SetOngoing(false);
        }
        else if (isComplete)
        {
            _notificationBuilder!.SetContentTitle("Export Complete")
                .SetContentText("Your video is ready to share.")
                .SetProgress(0, 0, false)
                .SetOngoing(false);
        }
        else
        {
            _notificationBuilder!.SetContentText($"{progressPercent}%")
                .SetProgress(100, progressPercent, false);
        }

        _notificationManager.Notify(_notificationId, _notificationBuilder!.Build());
#pragma warning restore CS8602
    }

    private void DismissAndroidNotification()
    {
        _notificationManager?.Cancel(_notificationId);
    }
#endif

    [RelayCommand]
    private async Task SaveToGalleryAsync()
    {
        if (string.IsNullOrEmpty(ExportedFilePath) || !File.Exists(ExportedFilePath)) return;

#if ANDROID
        try
        {
            var context = Android.App.Application.Context;
            var resolver = context.ContentResolver;
            if (resolver == null) return;

            var contentValues = new Android.Content.ContentValues();
            contentValues.Put(Android.Provider.MediaStore.IMediaColumns.DisplayName, $"{CurrentProject?.Title}_{DateTime.Now.Ticks}.mp4");
            contentValues.Put(Android.Provider.MediaStore.IMediaColumns.MimeType, "video/mp4");
            contentValues.Put(Android.Provider.MediaStore.IMediaColumns.RelativePath, "Movies/YTShortsEditor");

            var uri = resolver.Insert(Android.Provider.MediaStore.Video.Media.ExternalContentUri!, contentValues);
            if (uri != null)
            {
                using var stream = resolver.OpenOutputStream(uri);
                if (stream != null)
                {
                    using var fileStream = File.OpenRead(ExportedFilePath);
                    await fileStream.CopyToAsync(stream);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save to gallery: {ex.Message}");
        }
#else
        await Task.CompletedTask;
#endif
    }

    [RelayCommand]
    private async Task ShareAsync()
    {
        if (string.IsNullOrEmpty(ExportedFilePath) || !File.Exists(ExportedFilePath)) return;

        await Microsoft.Maui.ApplicationModel.DataTransfer.Share.RequestAsync(new ShareFileRequest
        {
            Title = CurrentProject?.Title ?? "YouTube Shorts Editor Video",
            File = new ShareFile(ExportedFilePath, "video/mp4")
        });
    }

    [RelayCommand]
    private void DismissExportResult()
    {
        ShowExportResult = false;
    }
}
