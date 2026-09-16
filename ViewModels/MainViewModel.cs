using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YoutubeShortsEditorMobile.Models;
using YoutubeShortsEditorMobile.Repositories;
using YoutubeShortsEditorMobile.Services;
using YoutubeShortsEditorMobile.Views;

namespace YoutubeShortsEditorMobile.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly IProjectRepository _repository;
    private readonly IYouTubeDownloadService _downloadService;
    private readonly CompositeDisposable _disposables = new();

    [ObservableProperty]
    private ObservableCollection<ProjectCardViewModel> _projects = new();

    [ObservableProperty]
    private string _youtubeUrl = string.Empty;

    [ObservableProperty]
    private string? _urlValidationError;

    public MainViewModel(IProjectRepository repository, IYouTubeDownloadService downloadService, System.Reactive.Concurrency.IScheduler scheduler)
    {
        _repository = repository;
        _downloadService = downloadService;

        var sub = _downloadService.DownloadEvents
            .ObserveOn(scheduler)
            .Subscribe(async evt =>
            {
                var vm = Projects.FirstOrDefault(p => p.Id == evt.ProjectId);
                if (vm != null)
                {
                    vm.DownloadProgress = evt.Progress;
                    vm.DownloadState = evt.State;
                    vm.ErrorMessage = evt.ErrorMessage;

                    if (evt.State == DownloadState.Completed)
                    {
                        var project = await _repository.GetProjectByIdAsync(evt.ProjectId);
                        if (project != null)
                        {
                            project.DownloadState = evt.State;
                            project.DownloadProgress = evt.Progress;
                            project.LocalVideoPath = vm.ProjectEntity.LocalVideoPath;
                            project.ThumbnailPath = vm.ProjectEntity.ThumbnailPath;
                            project.Title = vm.ProjectEntity.Title;
                            project.Duration = vm.ProjectEntity.Duration;
                            
                            await _repository.UpdateProjectAsync(project);
                            
                            // Re-sync VM properties with updated entity
                            vm.ThumbnailPath = project.ThumbnailPath;
                            vm.Title = project.Title;
                        }
                    }
                    else if (evt.State == DownloadState.Failed)
                    {
                        var project = await _repository.GetProjectByIdAsync(evt.ProjectId);
                        if (project != null)
                        {
                            project.DownloadState = evt.State;
                            project.ErrorMessage = evt.ErrorMessage;
                            await _repository.UpdateProjectAsync(project);
                        }
                    }
                }
            });

        _disposables.Add(sub);
    }

    [RelayCommand]
    private async Task InitializeAsync(CancellationToken cancellationToken)
    {
        Projects.Clear();
        await foreach (var project in _repository.GetAllProjectsAsync(cancellationToken))
        {
            Projects.Add(new ProjectCardViewModel(project, _downloadService));
        }
    }

    [RelayCommand]
    private async Task SubmitUrlAsync(CancellationToken cancellationToken)
    {
        UrlValidationError = null;
        if (string.IsNullOrWhiteSpace(YoutubeUrl))
        {
            UrlValidationError = "URL cannot be empty.";
            return;
        }

        var isValid = await _downloadService.ValidateUrlAsync(YoutubeUrl, cancellationToken);
        if (!isValid)
        {
            UrlValidationError = "Invalid YouTube URL.";
            return;
        }

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Title = "New Download",
            SourceUrl = YoutubeUrl,
            DownloadState = DownloadState.Pending,
            CreatedOn = DateTime.UtcNow,
            LastEditedOn = DateTime.UtcNow
        };

        await _repository.CreateProjectAsync(project, cancellationToken);

        var vm = new ProjectCardViewModel(project, _downloadService);
        Projects.Insert(0, vm);

        YoutubeUrl = string.Empty;

        _ = Task.Run(() => _downloadService.StartDownloadAsync(project, CancellationToken.None));
    }

    [RelayCommand]
    private async Task NavigateToEditorAsync(ProjectCardViewModel projectVm)
    {
        if (projectVm?.IsReady == true)
        {
            await Shell.Current.GoToAsync($"{nameof(EditorPage)}?projectId={projectVm.Id}");
        }
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}
