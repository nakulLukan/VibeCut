# Research: YouTube Shorts Editor

**Phase**: 0 — Resolve Technical Unknowns
**Date**: 2026-09-15
**Feature**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md)

---

## RES-001: YoutubeExplode Integration Pattern

**Decision**: Use `YoutubeExplode` (NuGet: `YoutubeExplode`) to parse YouTube URLs and select the best muxed or separate video+audio stream, then download via `HttpClient` progress-reporting stream copy.

**Rationale**: YoutubeExplode is the de-facto .NET library for YouTube stream extraction. It exposes `VideoClient.GetAsync(videoId)` for metadata and `StreamClient.GetManifestAsync(videoId)` for stream manifests. It handles age-gating gracefully (throws `VideoUnavailableException`).

**Download Progress Pattern**:
```csharp
// In YouTubeDownloadService — uses IObservable<double> for progress
var manifest = await _youtubeClient.Videos.Streams.GetManifestAsync(videoId);
var streamInfo = manifest.GetMuxedStreams().GetWithHighestVideoQuality();
var stream = await _youtubeClient.Videos.Streams.GetAsync(streamInfo);
// Wrap stream in a ProgressStream that publishes to Subject<double>
```

**Rx.NET integration**: `YouTubeDownloadService` exposes `IObservable<DownloadProgressEvent>` per project ID. `MainViewModel` subscribes and throttles updates with `.Sample(TimeSpan.FromMilliseconds(250))` to avoid flooding the UI.

**Alternatives considered**: `yt-dlp` via process invocation — rejected (requires bundling a Python binary on Android, complex deployment). `VideoLibrary` — rejected (unmaintained, breaks frequently with YouTube API changes).

---

## RES-002: FFmpegKit Android Binding for .NET MAUI

**Decision**: Use `FFmpegKit.Net` / `Drastic.FFmpeg` NuGet package, which wraps the `ffmpeg-kit-android-full` AAR. Call `FFmpegKit.ExecuteAsync(command, callback)` from a background thread, bridging the result back via Rx.NET `Subject<T>`.

**Key FFmpeg Command Patterns**:

| Operation | FFmpeg Command Skeleton |
|---|---|
| Trim | `ffmpeg -ss {start} -to {end} -i {input} -c copy {output}` |
| Trim + re-encode | `ffmpeg -i {input} -ss {start} -to {end} -c:v libx264 -c:a aac {output}` |
| Mute segment | `ffmpeg -i {input} -an {output}` |
| 9:16 crop (1080p) | `ffmpeg -i {input} -vf "crop=607:1080:(iw-607)/2:0,scale=1080:1920" {output}` |
| Join (concat demuxer) | `ffmpeg -f concat -safe 0 -i {listFile} -c copy {output}` |
| Audio volume detection | `ffmpeg -i {input} -af "volumedetect" -f null /dev/null` |

**Rx.NET bridge**:
```csharp
public IObservable<FFmpegResult> RunCommandAsync(string command) =>
    Observable.Create<FFmpegResult>(observer =>
    {
        FFmpegKit.ExecuteAsync(command,
            session => {
                if (ReturnCode.isSuccess(session.ReturnCode))
                    observer.OnNext(new FFmpegResult(true, session.Output));
                else
                    observer.OnError(new FFmpegException(session.FailStackTrace));
                observer.OnCompleted();
            },
            log => { /* forward log */ },
            stats => observer.OnNext(new FFmpegResult(false, stats.Time)));
        return Disposable.Empty;
    });
```

**Alternatives considered**: `Xabe.FFmpeg` — rejected (invokes the `ffmpeg` binary as a process; no suitable static binary for Android ARM easily bundled). Native `MediaCodec` — rejected (no trim/join/filter support without extensive custom code).

---

## RES-003: CommunityToolkit.Maui.MediaElement on Android

**Decision**: Use `MediaElement` from `CommunityToolkit.Maui` (v9+). Call `MediaElement.SeekTo(TimeSpan)` to jump the playhead programmatically. Bind `Source` to a `MediaSource.FromFile(localPath)` constructed in the ViewModel. Subscribe to `MediaElement.PositionChanged` event via a Behavior or code-behind relay to drive timeline scrubber position.

**Playhead sync pattern**:
- `EditorViewModel` exposes `IObservable<TimeSpan> PlayheadPosition` (a `BehaviorSubject<TimeSpan>`).
- The View subscribes in code-behind and updates `MediaElement.Position` or calls `SeekTo`.
- Conversely, `PositionChanged` events from `MediaElement` push into the ViewModel observable.

**Limitation**: `MediaElement` does not support segment-gated playback natively. Preview of a single suggested highlight or trimmed segment is simulated by: `SeekTo(segment.StartTime)` → play → stop when `Position >= segment.EndTime` (polled via the `PositionChanged` observable with `.TakeWhile()`).

**Alternatives considered**: `ExoPlayer` via Android platform binding — provides richer gapless segment playback. Considered as a Phase 4/5 enhancement if `MediaElement` proves insufficient; initially out of scope to limit complexity.

---

## RES-004: EF Core SQLite on Android — DbContext Lifetime & Path

**Decision**: Register `AppDbContext` as **Transient**. Resolve the SQLite database path at runtime using `FileSystem.AppDataDirectory` (MAUI abstraction for Android internal storage, no special permissions required).

**DbContext configuration**:
```csharp
// In MauiProgram.cs
var dbPath = Path.Combine(FileSystem.AppDataDirectory, "yte_editor.db");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"), ServiceLifetime.Transient);
```

**Migration strategy**: EF Core design-time migrations generated via `dotnet ef migrations add`. The `AppDbContext.Database.MigrateAsync()` call is made during app startup (in `App.xaml.cs` or a dedicated `DatabaseInitializer` service) to auto-apply pending migrations on the device.

**Alternatives considered**: Singleton `DbContext` — rejected (EF Core `DbContext` is not thread-safe; concurrent ViewModel access causes exceptions). Scoped lifetime — not directly supported in MAUI without a custom scope factory; Transient is the idiomatic MAUI pattern.

---

## RES-005: Rx.NET State Management Between Services and ViewModels

**Decision**: Use the following reactive pipeline topology:

```
IYouTubeDownloadService
  └─ Subject<DownloadEvent>  →  IObservable<DownloadEvent>  (per project ID)
        ↓ (observed by MainViewModel)
        .ObserveOn(RxApp.MainThreadScheduler)  ← or MainThread.BeginInvokeOnMainThread
        .Subscribe(UpdateProjectCard)

IMediaProcessingService
  └─ Subject<ProcessingEvent>  →  IObservable<ProcessingEvent>
        ↓ (observed by EditorViewModel)
        .ObserveOn(MainScheduler)
        .Subscribe(UpdateProgressBar + EnableExportButton)
```

**Subscription lifecycle**: Each ViewModel holds a `CompositeDisposable _disposables`. All subscriptions are added via `.AddTo(_disposables)`. The ViewModel exposes a `Dispose()` method (called from Page's `OnDisappearing`) that calls `_disposables.Dispose()` to prevent leaks.

**`IScheduler` injection**: Services and ViewModels accept `IScheduler mainScheduler` and `IScheduler backgroundScheduler` via constructor injection (registered as `Scheduler.MainThread` and `NewThreadScheduler.Default`). This makes schedulers replaceable in unit tests with `TestScheduler`.

**Alternatives considered**: `IAsyncEnumerable<T>` — suitable for sequential streams but lacks `CombineLatest`, `Throttle`, `Sample` operators needed here. Used for simple DB streaming (project list) but not for cross-component state.

---

## RES-006: Android Shell Routing & Navigation

**Decision**: Use MAUI Shell with query-parameter routing. Register `EditorPage` with a route parameter:

```csharp
// AppShell.xaml.cs
Routing.RegisterRoute(nameof(EditorPage), typeof(EditorPage));
```

Navigation from `MainViewModel`:
```csharp
await Shell.Current.GoToAsync($"{nameof(EditorPage)}?projectId={project.Id}");
```

`EditorViewModel` implements `IQueryAttributable`:
```csharp
public void ApplyQueryAttributes(IDictionary<string, object> query)
{
    if (query.TryGetValue("projectId", out var id))
        _ = LoadProjectAsync(Guid.Parse(id.ToString()!));
}
```

**Alternatives considered**: `NavigationPage` stack — rejected (Shell is the constitution-aligned pattern for .NET MAUI and supports deep-link URI routing).

---

## RES-007: Material 3 Theming in .NET MAUI

**Decision**: Use the `MaterialDesignTheme` resource dictionary from `CommunityToolkit.Maui.Core` / `Material` package. Define all color tokens in `Resources/Styles/Colors.xaml` using Material 3 naming (`MD3Primary`, `MD3Secondary`, `MD3Surface`, `MD3Error`, etc.). Reference tokens via `DynamicResource` in control styles.

**Card pattern for project list**:
```xml
<Frame Style="{StaticResource Material3Card}"
       CornerRadius="12" HasShadow="True">
    <!-- Project card content -->
</Frame>
```

**Alternatives considered**: Custom CSS-like styling — rejected in favour of Material 3 tokens to maintain constitutional compliance. Third-party Material library (`Maui.Material3` community package) — acceptable if the MAUI built-in theme is insufficient; evaluate during Phase 2.

---

## RES-008: Android Permissions & Share Intent

**Decision**:
- `INTERNET` — required for YoutubeExplode HTTP stream download.
- `POST_NOTIFICATIONS` — required on Android 13+ (API 33+) for background download completion notifications.
- `READ_MEDIA_VIDEO` / `WRITE_EXTERNAL_STORAGE` — required only for saving to the public gallery (MediaStore API). Internal app storage (`FileSystem.AppDataDirectory`) requires no permissions.

**Gallery save**: Use `Android.Provider.MediaStore.Video.Media.ExternalContentUri` with `ContentValues` via a platform-specific Android service (`#if ANDROID`).

**Share**: Use `Microsoft.Maui.ApplicationModel.DataTransfer.Share.RequestAsync(new ShareFileRequest { File = new ShareFile(exportPath) })` — MAUI cross-platform abstraction that triggers the Android share sheet with the MP4 file attached.

**Alternatives considered**: `FileProvider` + `ACTION_SEND` intent directly — more control but more boilerplate. The MAUI `Share` abstraction is sufficient and constitution-compliant.

---

## RES-009: Timeline Custom Control Strategy

**Decision**: Implement `TimelineControl` as a custom `ContentView` with a `ScrollView` wrapping a horizontal `StackLayout` of `Frame`-based segment cards. Each segment card shows a thumbnail (extracted via FFmpeg at design time), trim handles (custom `PanGestureRecognizer`), and overlay icons (mute, crop, delete).

**Drag-to-reorder**: Use `CollectionView` with `ItemsLayout = LinearItemsLayout(ItemsLayoutOrientation.Horizontal)` and handle reorder via long-press gesture + `CollectionView.CanMixGroups` / custom drag logic. Alternatively, use a `DragGestureRecognizer` on each segment card and compute drop index from touch position.

**ViewModel binding**: `EditorViewModel` exposes `ObservableCollection<ClipSegmentViewModel>` which directly backs the timeline CollectionView. Reorder triggers an `[RelayCommand]` that updates `SequenceOrder` on each `ClipSegmentViewModel` and calls `_repository.SaveChangesAsync()`.

**Alternatives considered**: Third-party timeline controls — no mature .NET MAUI timeline control exists. Building from scratch with `CollectionView` + gestures is the only viable path.

---
