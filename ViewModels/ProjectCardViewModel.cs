using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YoutubeShortsEditorMobile.Models;
using YoutubeShortsEditorMobile.Services;

namespace YoutubeShortsEditorMobile.ViewModels;

public partial class ProjectCardViewModel : ObservableObject
{
    private readonly IYouTubeDownloadService _downloadService;
    private readonly Project _project;

    public Guid Id { get; }

    [ObservableProperty]
    private string _title;

    [ObservableProperty]
    private string _sourceUrl;

    [ObservableProperty]
    private string? _thumbnailPath;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsReady))]
    [NotifyCanExecuteChangedFor(nameof(RetryDownloadCommand))]
    private DownloadState _downloadState;

    [ObservableProperty]
    private double _downloadProgress;

    [ObservableProperty]
    private string? _errorMessage;

    public bool IsReady => DownloadState == DownloadState.Completed;

    public Project ProjectEntity => _project;

    public ProjectCardViewModel(Project project, IYouTubeDownloadService downloadService)
    {
        _project = project;
        _downloadService = downloadService;
        
        Id = project.Id;
        _title = project.Title;
        _sourceUrl = project.SourceUrl;
        _thumbnailPath = project.ThumbnailPath;
        _downloadState = project.DownloadState;
        _downloadProgress = project.DownloadProgress;
        _errorMessage = project.ErrorMessage;
    }

    [RelayCommand(CanExecute = nameof(CanRetryDownload))]
    private async Task RetryDownloadAsync(CancellationToken cancellationToken)
    {
        DownloadState = DownloadState.Pending;
        ErrorMessage = null;
        DownloadProgress = 0;
        
        try
        {
            await Task.Run(() => _downloadService.StartDownloadAsync(_project, cancellationToken));
        }
        catch (Exception ex)
        {
            DownloadState = DownloadState.Failed;
            ErrorMessage = ex.Message;
        }
    }

    private bool CanRetryDownload() => DownloadState == DownloadState.Failed;
}
