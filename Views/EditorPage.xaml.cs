using System.Reactive.Disposables;
using System.Reactive.Linq;
using CommunityToolkit.Maui.Views;
using YoutubeShortsEditorMobile.ViewModels;

namespace YoutubeShortsEditorMobile.Views;

public partial class EditorPage : ContentPage
{
    private readonly EditorViewModel _viewModel;
    private readonly CompositeDisposable _pageDisposables = new();
    private bool _isSeeking;

    public EditorPage(EditorViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        WireMediaElementSync();
    }

    private void WireMediaElementSync()
    {
        // Sync MediaElement position -> ViewModel PlayheadPosition (throttled to avoid feedback loop)
        var positionSub = Observable
            .FromEventPattern<EventHandler<CommunityToolkit.Maui.Core.MediaPositionChangedEventArgs>, CommunityToolkit.Maui.Core.MediaPositionChangedEventArgs>(
                h => MediaPlayerElement.PositionChanged += h,
                h => MediaPlayerElement.PositionChanged -= h)
            .Select(e => e.EventArgs.Position)
            .DistinctUntilChanged()
            .Throttle(TimeSpan.FromMilliseconds(100))
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(pos =>
            {
                if (!_isSeeking)
                    _viewModel.PlayheadPosition = pos;
            });
        _pageDisposables.Add(positionSub);

        // T072: When preview render completes, reload MediaElement with the new file
        var previewSub = Observable
            .FromEventPattern<System.ComponentModel.PropertyChangedEventHandler, System.ComponentModel.PropertyChangedEventArgs>(
                h => _viewModel.PropertyChanged += h,
                h => _viewModel.PropertyChanged -= h)
            .Where(e => e.EventArgs.PropertyName == nameof(EditorViewModel.PreviewOutputPath))
            .Select(_ => _viewModel.PreviewOutputPath)
            .Where(path => !string.IsNullOrEmpty(path) && File.Exists(path))
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(path =>
            {
                MediaPlayerElement.Stop();
                MediaPlayerElement.Source = MediaSource.FromFile(path!);
                MediaPlayerElement.Play();
            });
        _pageDisposables.Add(previewSub);

        // Load video source once CurrentProject is available
        if (_viewModel.CurrentProject?.LocalVideoPath != null &&
            File.Exists(_viewModel.CurrentProject.LocalVideoPath))
        {
            MediaPlayerElement.Source = MediaSource.FromFile(_viewModel.CurrentProject.LocalVideoPath);
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _pageDisposables.Dispose();
        _viewModel.Dispose();
    }
}
