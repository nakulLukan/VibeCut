using System.Collections;
using YoutubeShortsEditorMobile.ViewModels;

namespace YoutubeShortsEditorMobile.Controls;

public partial class TimelineControl : ContentView
{
    public static readonly BindableProperty SegmentsProperty =
        BindableProperty.Create(
            nameof(Segments),
            typeof(IList<ClipSegmentViewModel>),
            typeof(TimelineControl),
            null,
            propertyChanged: OnSegmentsChanged);

    public static readonly BindableProperty TotalDurationProperty =
        BindableProperty.Create(
            nameof(TotalDuration),
            typeof(TimeSpan),
            typeof(TimelineControl),
            TimeSpan.FromSeconds(60),
            propertyChanged: OnSegmentsChanged);

    public IList<ClipSegmentViewModel>? Segments
    {
        get => (IList<ClipSegmentViewModel>?)GetValue(SegmentsProperty);
        set => SetValue(SegmentsProperty, value);
    }

    public TimeSpan TotalDuration
    {
        get => (TimeSpan)GetValue(TotalDurationProperty);
        set => SetValue(TotalDurationProperty, value);
    }

    // Commands passed in from parent
    public static readonly BindableProperty ToggleMuteCommandProperty =
        BindableProperty.Create(nameof(ToggleMuteCommand), typeof(System.Windows.Input.ICommand), typeof(TimelineControl));
    public static readonly BindableProperty ToggleCropCommandProperty =
        BindableProperty.Create(nameof(ToggleCropCommand), typeof(System.Windows.Input.ICommand), typeof(TimelineControl));
    public static readonly BindableProperty DeleteSegmentCommandProperty =
        BindableProperty.Create(nameof(DeleteSegmentCommand), typeof(System.Windows.Input.ICommand), typeof(TimelineControl));
    public static readonly BindableProperty SplitSegmentCommandProperty =
        BindableProperty.Create(nameof(SplitSegmentCommand), typeof(System.Windows.Input.ICommand), typeof(TimelineControl));

    public System.Windows.Input.ICommand? ToggleMuteCommand
    {
        get => (System.Windows.Input.ICommand?)GetValue(ToggleMuteCommandProperty);
        set => SetValue(ToggleMuteCommandProperty, value);
    }
    public System.Windows.Input.ICommand? ToggleCropCommand
    {
        get => (System.Windows.Input.ICommand?)GetValue(ToggleCropCommandProperty);
        set => SetValue(ToggleCropCommandProperty, value);
    }
    public System.Windows.Input.ICommand? DeleteSegmentCommand
    {
        get => (System.Windows.Input.ICommand?)GetValue(DeleteSegmentCommandProperty);
        set => SetValue(DeleteSegmentCommandProperty, value);
    }
    public System.Windows.Input.ICommand? SplitSegmentCommand
    {
        get => (System.Windows.Input.ICommand?)GetValue(SplitSegmentCommandProperty);
        set => SetValue(SplitSegmentCommandProperty, value);
    }

    private readonly ScrollView _scrollView;
    private readonly HorizontalStackLayout _container;

    public TimelineControl()
    {
        _container = new HorizontalStackLayout { Spacing = 4 };
        _scrollView = new ScrollView
        {
            Orientation = ScrollOrientation.Horizontal,
            Content = _container
        };

        Content = _scrollView;
    }

    private static void OnSegmentsChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is TimelineControl ctrl)
        {
            ctrl.RebuildTimeline();
        }
    }

    private void RebuildTimeline()
    {
        _container.Children.Clear();

        if (Segments == null || TotalDuration == TimeSpan.Zero) return;

        const double TimelineWidth = 900.0;

        foreach (var segment in Segments)
        {
            var segFraction = TotalDuration.TotalSeconds > 0
                ? segment.Duration.TotalSeconds / TotalDuration.TotalSeconds
                : 0;
            var segWidth = Math.Max(80, segFraction * TimelineWidth);

            var card = BuildSegmentCard(segment, segWidth);
            _container.Children.Add(card);
        }
    }

    private View BuildSegmentCard(ClipSegmentViewModel segment, double width)
    {
        var label = new Label
        {
            Text = $"{segment.StartTime:mm\\:ss} → {segment.EndTime:mm\\:ss}",
            FontSize = 10,
            TextColor = Colors.White,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };

        var muteBtn = new ImageButton
        {
            Source = segment.IsAudioEnabled ? "volume_on.png" : "volume_off.png",
            WidthRequest = 24,
            HeightRequest = 24,
            Command = ToggleMuteCommand,
            CommandParameter = segment
        };
        var cropBtn = new ImageButton
        {
            Source = "crop.png",
            WidthRequest = 24,
            HeightRequest = 24,
            Command = ToggleCropCommand,
            CommandParameter = segment
        };
        var deleteBtn = new ImageButton
        {
            Source = "delete.png",
            WidthRequest = 24,
            HeightRequest = 24,
            Command = DeleteSegmentCommand,
            CommandParameter = segment
        };
        var splitBtn = new Button
        {
            Text = "Cut",
            FontSize = 9,
            HeightRequest = 24,
            Padding = new Thickness(6, 0),
            Command = SplitSegmentCommand,
            CommandParameter = segment
        };

        var iconRow = new HorizontalStackLayout
        {
            Spacing = 4,
            Children = { muteBtn, cropBtn, deleteBtn, splitBtn }
        };

        var cardContent = new VerticalStackLayout
        {
            Spacing = 4,
            Children = { label, iconRow }
        };

        var border = new Border
        {
            BackgroundColor = segment.IsCropApplied ? Color.FromArgb("#663d9eff") : Color.FromArgb("#AA49454F"),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(8) },
            WidthRequest = width,
            HeightRequest = 100,
            Padding = new Thickness(8),
            Content = cardContent
        };

        // Left trim handle
        var leftHandle = new BoxView { Color = Color.FromArgb("#D0BCFF"), WidthRequest = 6, HeightRequest = 100 };
        var leftPan = new PanGestureRecognizer();
        leftPan.PanUpdated += (s, e) => OnTrimHandlePan(segment, e, isLeft: true);
        leftHandle.GestureRecognizers.Add(leftPan);

        // Right trim handle
        var rightHandle = new BoxView { Color = Color.FromArgb("#D0BCFF"), WidthRequest = 6, HeightRequest = 100 };
        var rightPan = new PanGestureRecognizer();
        rightPan.PanUpdated += (s, e) => OnTrimHandlePan(segment, e, isLeft: false);
        rightHandle.GestureRecognizers.Add(rightPan);

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = new GridLength(6) },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = new GridLength(6) }
            }
        };
        grid.Add(leftHandle, 0, 0);
        grid.Add(border, 1, 0);
        grid.Add(rightHandle, 2, 0);

        return grid;
    }

    private double _pixelsPerSecond = 10.0;

    private void OnTrimHandlePan(ClipSegmentViewModel segment, PanUpdatedEventArgs e, bool isLeft)
    {
        if (e.StatusType != GestureStatus.Running) return;

        var delta = TimeSpan.FromSeconds(e.TotalX / _pixelsPerSecond);

        if (isLeft)
        {
            var newStart = segment.StartTime + delta;
            if (newStart < segment.EndTime - TimeSpan.FromSeconds(0.5))
            {
                segment.StartTime = newStart;
            }
        }
        else
        {
            var newEnd = segment.EndTime + delta;
            if (newEnd > segment.StartTime + TimeSpan.FromSeconds(0.5))
            {
                segment.EndTime = newEnd;
            }
        }
    }
}
