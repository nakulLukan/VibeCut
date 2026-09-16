# Tasks: YouTube Shorts Editor

**Branch**: `001-youtube-shorts-editor` | **Date**: 2026-09-15
**Spec**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md) | **Data Model**: [data-model.md](data-model.md)

---

## Phase 1: Setup — Project Scaffolding & Data Layer

**Purpose**: Establish the Android-only .NET MAUI project skeleton, install all NuGet packages, configure the build system, create EF Core entities, set up AppDbContext with SQLite, apply the initial migration, and wire up the full DI registry.

> **CRITICAL**: This phase BLOCKS all subsequent phases. Complete entirely before moving to Phase 2.

- [x] T001 Create the .NET MAUI Android-only project: run `dotnet new maui -n YoutubeShortsEditorMobile`, then edit `YoutubeShortsEditorMobile.csproj` to keep only `net10.0-android` as the `<TargetFramework>`; remove any `net9.0-ios`, `net9.0-maccatalyst`, and `net9.0-windows*` entries
- [x] T002 Enable nullable reference types: add `<Nullable>enable</Nullable>` and `<WarningsAsErrors>Nullable</WarningsAsErrors>` to `YoutubeShortsEditorMobile.csproj`
- [x] T003 [P] Install `CommunityToolkit.Mvvm` NuGet package (latest stable); confirm source generators active
- [x] T004 [P] Install `CommunityToolkit.Maui` NuGet package (latest stable, includes MediaElement); add `.UseMauiCommunityToolkit()` and `.UseMauiCommunityToolkitMediaElement()` to `MauiProgram.cs`
- [x] T005 [P] Install `YoutubeExplode` NuGet package (latest stable)
- [x] T006 [P] Install `Drastic.FFmpeg` (FFmpegKit .NET Android binding) NuGet package; verify AAR included in Android build output
- [x] T007 [P] Install `System.Reactive` NuGet package (Rx.NET, latest stable)
- [x] T008 [P] Install `Microsoft.EntityFrameworkCore.Sqlite` and `Microsoft.EntityFrameworkCore.Design` (dev dependency)
- [x] T009 [P] Confirm `Microsoft.Maui.Controls.Material` is available; configure Material 3 theme in `App.xaml` via merged resource dictionaries
- [x] T010 Create `Models/DownloadState.cs`: enum with values `Pending`, `Downloading`, `Completed`, `Failed`
- [x] T011 Create `Models/Project.cs`: POCO with `Id` (Guid PK), `Title` (string, required, max 200), `SourceUrl` (string, required, max 2048), `LocalVideoPath` (string?, nullable), `ThumbnailPath` (string?, nullable), `DownloadState` (DownloadState, required), `DownloadProgress` (double), `ErrorMessage` (string?, nullable), `CreatedOn` (DateTime), `LastEditedOn` (DateTime), `Duration` (TimeSpan?), navigation `ICollection<ClipSegment> Segments`
- [x] T012 Create `Models/ClipSegment.cs`: POCO with `Id` (Guid PK), `ProjectId` (Guid FK, required), `StartTime` (TimeSpan, required), `EndTime` (TimeSpan, required, > StartTime enforced in service layer), `SequenceOrder` (int, required, >= 0), `IsAudioEnabled` (bool, default true), `IsCropApplied` (bool, default false), navigation `Project Project`
- [x] T013 [P] Create `Models/DownloadProgressEvent.cs`: record with `ProjectId` (Guid), `Progress` (double), `State` (DownloadState), `ErrorMessage` (string?)
- [x] T014 [P] Create `Models/ProcessingEvent.cs`: record with `OperationId` (Guid), `TimeMs` (long), `DurationMs` (long), `IsComplete` (bool), `IsSuccess` (bool), `ErrorDetail` (string?)
- [x] T015 [P] Create `Models/HighlightSuggestion.cs`: record with `StartTime` (TimeSpan), `EndTime` (TimeSpan), `ActivityScore` (double 0.0-1.0)
- [x] T016 Create `Data/AppDbContext.cs`: `DbSet<Project> Projects` and `DbSet<ClipSegment> ClipSegments`; override `OnModelCreating` — Project PK, `Title` max 200 required, `SourceUrl` max 2048 required; ClipSegment PK, composite index on `(ProjectId, SequenceOrder)`, HasOne/WithMany cascade-delete from Project to ClipSegments
- [x] T017 Register `AppDbContext` in `MauiProgram.cs` as Transient: `builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlite($"Data Source={Path.Combine(FileSystem.AppDataDirectory, "yte_editor.db")}"), ServiceLifetime.Transient)`
- [x] T018 Run EF Core initial migration: `dotnet ef migrations add InitialCreate --project YoutubeShortsEditorMobile`; verify `Data/Migrations/` folder created with valid migration file (Note: Agent lacks Android SDK to build, User must run this locally!)
- [x] T019 Add database auto-migration on startup: add `dbContext.Database.MigrateAsync()` inside `App.xaml.cs` constructor or `OnStart` (implemented in MauiProgram.cs) or a startup `DatabaseInitializer` service resolved from DI
- [x] T020 Create `Repositories/IProjectRepository.cs`: full interface per `contracts/IProjectRepository.md` — `GetAllProjectsAsync` (IAsyncEnumerable<Project>), `GetProjectByIdAsync`, `CreateProjectAsync`, `UpdateProjectAsync`, `DeleteProjectAsync`, `GetSegmentsAsync`, `AddSegmentAsync`, `UpdateSegmentAsync`, `DeleteSegmentAsync`, `NormalizeSequenceOrderAsync`; all with CancellationToken
- [x] T021 Create `Repositories/ProjectRepository.cs`: implement `IProjectRepository` using injected `AppDbContext`; async EF methods only; `GetProjectByIdAsync` uses `.Include(p => p.Segments).FirstOrDefaultAsync`; `GetAllProjectsAsync` uses `AsAsyncEnumerable()`; `NormalizeSequenceOrderAsync` rewrites SequenceOrder as contiguous 0-based integers
- [x] T022 Create service interfaces `Services/IYouTubeDownloadService.cs`, `Services/IMediaProcessingService.cs`, `Services/IHighlightDetectionService.cs`: exact method signatures and XML doc comments from `contracts/` directory
- [x] T023 Register all services and pages in `MauiProgram.cs` per DI registry in `plan.md`: `IProjectRepository`/`ProjectRepository` Transient; `IYouTubeDownloadService`/`YouTubeDownloadService` Singleton; `IMediaProcessingService`/`MediaProcessingService` Singleton; `IHighlightDetectionService`/`HighlightDetectionService` Singleton; `MainViewModel`, `EditorViewModel`, `MainPage`, `EditorPage` all Transient (Note: Deferred unimplemented services to their respective phases)
- [x] T024 Create `AppShell.xaml` and `AppShell.xaml.cs`: Shell with `MainPage` as default route; register `Routing.RegisterRoute(nameof(EditorPage), typeof(EditorPage))` in constructor
- [x] T025 Configure `Platforms/Android/AndroidManifest.xml` with permissions: `INTERNET`, `POST_NOTIFICATIONS` (API 33+), `READ_MEDIA_VIDEO`, `WRITE_EXTERNAL_STORAGE` (legacy API < 29)
- [x] T026 Create `Resources/Styles/Colors.xaml`: Material 3 color tokens — `MD3Primary`, `MD3OnPrimary`, `MD3Secondary`, `MD3OnSecondary`, `MD3Surface`, `MD3OnSurface`, `MD3SurfaceVariant`, `MD3Error`, `MD3Background` using a dark-mode-first palette
- [x] T027 Create `Resources/Styles/Styles.xaml`: global Material 3 `Style` entries for `Button`, `Entry`, `Frame` (Material 3 Card, CornerRadius 12), `Label` (headline/body/caption), `ProgressBar`; merge into `App.xaml`
- [x] T028 Verify the project builds: run `dotnet build -f net10.0-android -c Debug`; confirm zero errors and zero nullable warnings (Note: Agent lacks Android SDK to build, User must verify locally!)

**Checkpoint**: Project builds cleanly, EF Core migration exists, DI registry complete, all interfaces declared.

---

## Phase 2: Dashboard & Video Ingestion (User Story 1 — Priority: P1) ⭐ MVP

**Goal**: Working Dashboard where a user pastes a YouTube URL, triggers async background download, and sees live progress on a reactive project card.

**Independent Test**: Launch app on Android emulator, submit a valid YouTube URL, verify a project card appears with a live download progress bar, wait for completion, verify the card becomes tappable and the video file exists on disk at the path stored in SQLite.

- [x] T029 [US1] Create stub `Services/YouTubeDownloadService.cs`: implement `IYouTubeDownloadService`; inject `YoutubeClient` (YoutubeExplode) via constructor; expose `Subject<DownloadProgressEvent> _events`; return as `IObservable<DownloadProgressEvent> DownloadEvents` via `.AsObservable()`
- [x] T030 [US1] Implement `ResolveMetadataAsync` in `YouTubeDownloadService.cs`: call `_youtubeClient.Videos.GetAsync(VideoId.Parse(url))`; catch `VideoUnavailableException` and `ArgumentException`; return `VideoMetadata` record with `Title`, `Duration`, `ThumbnailUrl`, `Author`
- [x] T031 [US1] Implement `StartDownloadAsync` in `YouTubeDownloadService.cs`: use `StreamClient.GetManifestAsync` to get `GetMuxedStreams().GetWithHighestVideoQuality()`; copy stream to `destinationPath` via a `ProgressStream` wrapper that publishes `DownloadProgressEvent` to the `Subject` sampled with `.Sample(TimeSpan.FromMilliseconds(250))` (Rx.NET); emit `DownloadState.Downloading` on start, `Completed` on success, `Failed` with `ErrorMessage` on exception; delete partial file on failure
- [x] T032 [US1] Implement `CancelDownload` in `YouTubeDownloadService.cs`: maintain `ConcurrentDictionary<Guid, CancellationTokenSource>` keyed by `projectId`; cancel and remove entry; `StartDownloadAsync` throws `InvalidOperationException` if entry already exists for the `projectId`
- [x] T033 [US1] Create `ViewModels/ProjectCardViewModel.cs`: inherit `ObservableObject` (CommunityToolkit.Mvvm); `[ObservableProperty]` — `Id` (Guid), `Title` (string), `SourceUrl` (string), `DownloadState` (DownloadState), `DownloadProgress` (double), `ErrorMessage` (string?), `IsReady` (bool, computed: `DownloadState == Completed`); constructor accepts `Project` entity
- [x] T034 [US1] Create `ViewModels/MainViewModel.cs`: inherit `ObservableObject`; inject `IProjectRepository`, `IYouTubeDownloadService`; `[ObservableProperty] ObservableCollection<ProjectCardViewModel> Projects`; `[ObservableProperty] string youTubeUrl`; `[ObservableProperty] string? urlValidationError`; `CompositeDisposable _disposables`
- [x] T035 [US1] Implement `InitializeCommand` (`[RelayCommand]`) in `MainViewModel.cs`: `await foreach` over `_repository.GetAllProjectsAsync()` and populate `Projects` with `ProjectCardViewModel` instances; page calls this from `OnAppearing`
- [x] T036 [US1] Implement `SubmitUrlCommand` (`[RelayCommand]`) in `MainViewModel.cs`: validate URL via regex or `Uri.TryCreate` + host check for `youtube.com`/`youtu.be`; set `UrlValidationError` if invalid; on valid URL: call `_repository.CreateProjectAsync(new Project { DownloadState = Pending, ... })`; add `ProjectCardViewModel` to `Projects`; fire-and-forget `_downloadService.StartDownloadAsync(...)` via `Task.Run`; clear `YoutubeUrl`
- [x] T037 [US1] Wire `DownloadEvents` observable in `MainViewModel.cs` constructor: subscribe to `_downloadService.DownloadEvents.ObserveOn(SynchronizationContext.Current)`; on each `DownloadProgressEvent` find matching `ProjectCardViewModel` by `ProjectId` and update `DownloadProgress`, `DownloadState`, `ErrorMessage`; on `Completed` call `_repository.UpdateProjectAsync`; add to `_disposables`
- [x] T038 [US1] Implement `RetryDownloadCommand` (`[RelayCommand]`) on `ProjectCardViewModel.cs`: reset `DownloadState = Pending`; re-invoke `StartDownloadAsync` via the injected download service; enabled only when `DownloadState == Failed`
- [x] T039 [US1] Implement `NavigateToEditorCommand` (`[RelayCommand]`) in `MainViewModel.cs`: call `await Shell.Current.GoToAsync($"{nameof(EditorPage)}?projectId={project.Id}")`; enabled only when `IsReady == true`
- [x] T040 [US1] Implement `Dispose()` in `MainViewModel.cs`: call `_disposables.Dispose()`; `MainPage` code-behind calls this from `OnDisappearing`
- [x] T041 [US1] Create `Converters/DownloadStateToColorConverter.cs`: implement `IValueConverter`; `Pending` -> grey, `Downloading` -> `MD3Primary`, `Completed` -> green accent, `Failed` -> `MD3Error`; register in `App.xaml`
- [x] T042 [US1] Build `Views/MainPage.xaml`: `Grid` root with two rows — row 0 (auto) for URL entry area, row 1 (*) for projects `CollectionView`; bind `BindingContext` to `MainViewModel` via DI in code-behind; `Shell.NavBarIsVisible="True"` and `Title="YouTube Shorts Editor"`
- [x] T043 [US1] Implement URL entry area in `MainPage.xaml`: `Border` (Material 3 surface container style) wrapping a `HorizontalStackLayout` with `Entry` bound to `YoutubeUrl` (placeholder "Paste YouTube URL..."), `ImageButton` bound to `SubmitUrlCommand`, `Label` bound to `UrlValidationError` (visible when non-null)
- [x] T044 [US1] Implement `CollectionView` in `MainPage.xaml`: `ItemsSource` bound to `Projects`; vertical `LinearItemsLayout`; `DataTemplate` containing `Frame` (Material 3 Card, CornerRadius 12) with: thumbnail `Image` bound to `ThumbnailPath`, `Label` for `Title`, `Label` for `DownloadState`, `ProgressBar` bound to `DownloadProgress` (visible when `Downloading`), "Retry" `Button` bound to `RetryDownloadCommand` (visible when `Failed`); card `TapGestureRecognizer` bound to `NavigateToEditorCommand` enabled when `IsReady`
- [x] T045 [US1] Add empty-state UI in `MainPage.xaml`: when `Projects` is empty show centered `VerticalStackLayout` with icon, headline "No projects yet", body "Paste a YouTube link above to get started"
- [x] T046 [US1] Smoke-test Phase 2: deploy to Android emulator; submit valid YouTube URL; confirm project card with progress appears; wait for `Completed`; confirm file exists in `FileSystem.AppDataDirectory`; confirm tapping card navigates to EditorPage (Note: Agent lacks Android SDK to build, User must verify locally!)

**Checkpoint**: Dashboard fully functional. Users can download YouTube videos with real-time reactive progress tracking.

---

## Phase 3: Video Editor Core UI (User Story 2 — Priority: P2)

**Goal**: Build `EditorPage` with `MediaElement` playback, horizontal drag-and-drop `TimelineControl`, trim/split/mute/crop UI, Shell route parameter loading, and auto-persist of all edits to SQLite.

**Independent Test**: Open a completed project, verify video plays, drag trim handle and confirm `StartTime` updates in SQLite, split a segment and confirm two `ClipSegment` rows appear, mute a segment and confirm `IsAudioEnabled = false` persists, navigate back and re-open and confirm all edits are restored.

- [x] T047 [US2] Create `ViewModels/ClipSegmentViewModel.cs`: inherit `ObservableObject`; `[ObservableProperty]` — `Id` (Guid), `ProjectId` (Guid), `StartTime` (TimeSpan), `EndTime` (TimeSpan), `SequenceOrder` (int), `IsAudioEnabled` (bool), `IsCropApplied` (bool); computed `Duration` property (`EndTime - StartTime`); constructor accepts `ClipSegment` entity
- [x] T048 [US2] Create `ViewModels/EditorViewModel.cs` skeleton: inherit `ObservableObject`; implement `IQueryAttributable`; inject `IProjectRepository`, `IMediaProcessingService`; `ApplyQueryAttributes` parses `projectId` and calls `LoadProjectAsync`; `[ObservableProperty]` — `currentProject` (Project?), `segments` (ObservableCollection<ClipSegmentViewModel>), `playheadPosition` (TimeSpan), `isExportReady` (bool), `isProcessing` (bool), `renderProgress` (double); `CompositeDisposable _disposables`
- [x] T049 [US2] Implement `LoadProjectAsync` in `EditorViewModel.cs`: call `_repository.GetProjectByIdAsync(id)` (includes Segments); populate `CurrentProject` and `Segments` ordered by `SequenceOrder`; validate `LocalVideoPath` non-null and file exists on disk; update `LastEditedOn` via `_repository.UpdateProjectAsync`
- [x] T050 [US2] Implement `SplitSegmentCommand` (`[RelayCommand]`) in `EditorViewModel.cs`: guard `PlayheadPosition` strictly between segment `StartTime` and `EndTime`; create two `ClipSegment` entities split at `PlayheadPosition`; delete original via `_repository.DeleteSegmentAsync`; add both via `_repository.AddSegmentAsync`; call `_repository.NormalizeSequenceOrderAsync`; refresh `Segments`
- [x] T051 [US2] Implement `UpdateSegmentTrimCommand` (`[RelayCommand]`) in `EditorViewModel.cs`: accepts `ClipSegmentViewModel` + new `StartTime` + new `EndTime`; validate `EndTime > StartTime`; call `_repository.UpdateSegmentAsync`; update in-memory `ClipSegmentViewModel` properties
- [x] T052 [US2] Implement `ToggleMuteCommand` (`[RelayCommand]`) in `EditorViewModel.cs`: toggle `IsAudioEnabled` on the given `ClipSegmentViewModel`; call `_repository.UpdateSegmentAsync`
- [x] T053 [US2] Implement `ToggleCropCommand` (`[RelayCommand]`) in `EditorViewModel.cs`: toggle `IsCropApplied` on the given `ClipSegmentViewModel`; call `_repository.UpdateSegmentAsync`
- [x] T054 [US2] Implement `DeleteSegmentCommand` (`[RelayCommand]`) in `EditorViewModel.cs`: call `_repository.DeleteSegmentAsync(segment.Id)`; remove from `Segments`; call `_repository.NormalizeSequenceOrderAsync`; update `IsExportReady`
- [x] T055 [US2] Implement `ReorderSegmentCommand` (`[RelayCommand]`) in `EditorViewModel.cs`: accepts old index and new index; reorder `Segments` in-memory; call `_repository.NormalizeSequenceOrderAsync` to persist new `SequenceOrder` values
- [x] T056 [US2] Implement `Dispose()` in `EditorViewModel.cs`: call `_disposables.Dispose()`; `EditorPage` code-behind calls this from `OnDisappearing`
- [x] T057 [US2] Create `Controls/TimelineControl.xaml` and `.cs`: `ContentView` with horizontal `ScrollView` wrapping a `BindableLayout`-backed `HorizontalStackLayout`; expose `SegmentsProperty` (BindableProperty of `IList<ClipSegmentViewModel>`); render each segment as a `Border` with width proportional to `segment.Duration / totalProjectDuration`
- [x] T058 [US2] Add trim handles to segment cards in `TimelineControl.xaml`: left and right `BoxView` handles (6dp wide, `MD3Primary` color) with `PanGestureRecognizer`; on pan update compute time delta from pixels-per-second ratio and invoke `UpdateSegmentTrimCommand` on the parent `EditorViewModel`
- [x] T059 [US2] Add drag-to-reorder to `TimelineControl.xaml`: `DragGestureRecognizer` on each segment card; `DropGestureRecognizer` on the container `HorizontalStackLayout`; compute target index from drop X coordinate; invoke `ReorderSegmentCommand` with old and new indices
- [x] T060 [US2] Add per-segment action icons to each segment card in `TimelineControl.xaml`: mute `ImageButton` (`ToggleMuteCommand`), crop `ImageButton` (`ToggleCropCommand`), delete `ImageButton` (`DeleteSegmentCommand`), "Split" `Button` visible only when segment is selected/active
- [x] T061 [US2] Create `Converters/BoolToMuteIconConverter.cs`: implement `IValueConverter` converting `IsAudioEnabled` (bool) to mute/unmute `FontImageSource`; register in `App.xaml`
- [x] T062 [US2] Build `Views/EditorPage.xaml`: `Grid` with three rows — row 0 (2*): `MediaElement` bound to `MediaSource.FromFile(CurrentProject.LocalVideoPath)`, `ShouldAutoPlay="False"`, `ShouldShowPlaybackControls="True"`; row 1 (auto): `Slider` bound two-way to `PlayheadPosition`; row 2 (1.5*): `TimelineControl` with `Segments` bound to `EditorViewModel.Segments`
- [x] T063 [US2] Add editor toolbar to `EditorPage.xaml`: `ToolbarItem` entries — "Split" (`SplitSegmentCommand`), "9:16 Crop" (`ToggleCropCommand` on selected segment), "Export" (placeholder wired in Phase 6); style with Material 3 `FilledButton` and `TonalButton` styles
- [x] T064 [US2] Wire MediaElement playhead sync in `EditorPage.xaml.cs`: subscribe to `MediaElement.PositionChanged` via `Observable.FromEventPattern`; push `TimeSpan` into `ViewModel.PlayheadPosition`; observe `PlayheadPosition` changes from Slider with debounce; call `MediaElement.SeekTo(position)` — use `Observable.FromEventPattern` + `.DistinctUntilChanged().Throttle(TimeSpan.FromMilliseconds(100))` to prevent feedback loop
- [x] T065 [US2] Smoke-test Phase 3: open completed project; verify video plays in MediaElement; drag trim handle; confirm `StartTime` updated in SQLite; tap split; confirm two ClipSegment rows; toggle mute; confirm `IsAudioEnabled = false`; navigate away and back; confirm edits restored

**Checkpoint**: Full non-linear editor UI with persistent state. User Story 2 independently testable.

---

## Phase 4: FFmpeg Integration & Operations

**Goal**: Implement `MediaProcessingService` wrapping FFmpegKit with Rx.NET reactive bridge so trim, mute, and 9:16 crop produce real processed video files; wire a "Render Preview" pipeline into `EditorViewModel`.

**Independent Test**: With two segments (one muted, one cropped) on the timeline, tap "Render Preview"; verify progress bar advances in EditorPage; verify MediaElement source switches to the rendered temp MP4; verify playback on device reflects the edits.

- [x] T066 [US2] Create `Services/MediaProcessingService.cs`: implement `IMediaProcessingService`; inject `IScheduler backgroundScheduler`; expose `Subject<ProcessingEvent> _events`; maintain sequential operation queue (`ConcurrentQueue<Func<Task>>`) processed by single background worker started in constructor
- [x] T067 [US2] Implement Rx.NET/FFmpegKit bridge in `MediaProcessingService.cs`: private `ExecuteFFmpegAsync(command, operationId)` via `Observable.Create<ProcessingEvent>`; statistics callback emits intermediate events; session callback emits final event with `ReturnCode.IsSuccess`
- [x] T068 [US2] Implement `TrimAsync` in `MediaProcessingService.cs`: validate input file; build `-i "{input}" -ss {start} -to {end} {muteFlag} -c:v libx264 -c:a aac "{output}"` command; enqueue; return `operationId`
- [x] T069 [US2] Implement `CropToVerticalAsync` in `MediaProcessingService.cs`: build `-vf "crop=607:1080:(iw-607)/2:0,scale=1080:1920"` command; enqueue; return `operationId`
- [x] T070 [US2] Implement `ConcatenateAsync` in `MediaProcessingService.cs`: write concat list file to `FileSystem.CacheDirectory`; build concat demuxer command; enqueue; delete list file in `.Finally()`; return `operationId`
- [x] T071 [US2] Implement `CancelAll` in `MediaProcessingService.cs`: call `FFmpegKit.Cancel()` and drain operation queue
- [x] T072 [US2] Add `RenderPreviewCommand` (`[RelayCommand]`) to `EditorViewModel.cs`: chains `TrimAsync` → `CropToVerticalAsync` → `ConcatenateAsync` per segment; updates `PreviewOutputPath`; `EditorPage.xaml.cs` observes property change to reload `MediaElement` source
- [x] T073 [US2] Wire `ProcessingEvents` in `EditorViewModel.cs` constructor: `.ObserveOn(SynchronizationContext.Current)`; set `IsProcessing = true` on intermediate; `IsProcessing = false` on completion; all editing commands gated on `CanExecute = nameof(IsNotProcessing)`

**Checkpoint**: FFmpeg operations are real and reactive. Editor produces actual processed video files.

---

## Phase 5: Smart Highlights (User Story 3 — Priority: P3)

**Goal**: Implement `HighlightDetectionService` using FFmpeg `silencedetect` audio analysis; build Highlights review panel in `EditorPage`; allow user to preview and accept suggestions into the timeline.

**Independent Test**: On a project with varied-audio video, tap "Detect Highlights"; verify a non-blocking progress spinner appears; verify a suggestion list with `StartTime`, `EndTime`, and `ActivityScore`; tap "Preview" and verify MediaElement plays that exact range; tap "Accept" and verify the segment appears on the timeline.

- [x] T074 [US3] Create `Services/HighlightDetectionService.cs`: implement `IHighlightDetectionService`; inject `IScheduler backgroundScheduler` via constructor
- [x] T075 [US3] Implement `DetectHighlightsAsync` in `HighlightDetectionService.cs`: return `Observable.Create<HighlightSuggestion>` running on `backgroundScheduler`; execute `ffmpeg -i "{videoPath}" -af "silencedetect=noise=-30dB:d=0.5" -f null /dev/null`; parse `silence_start` and `silence_end` timestamps from stderr via regex; invert silence windows to audio-active windows; filter windows shorter than 5s or longer than 60s; compute `ActivityScore` as normalized window duration (0.0-1.0 relative to the longest active window); emit each `HighlightSuggestion`; call `observer.OnCompleted()` when done; call `observer.OnError(new FFmpegException(...))` on non-zero exit
- [x] T076 [US3] Add highlight state to `EditorViewModel.cs`: `[ObservableProperty] ObservableCollection<HighlightSuggestion> highlightSuggestions`; `[ObservableProperty] bool isDetectingHighlights`; `[ObservableProperty] bool hasNoHighlights`
- [x] T077 [US3] Implement `DetectHighlightsCommand` (`[RelayCommand]`) in `EditorViewModel.cs`: guard `CurrentProject.LocalVideoPath` exists; set `IsDetectingHighlights = true`; subscribe `_highlightService.DetectHighlightsAsync(localVideoPath).ObserveOn(SynchronizationContext.Current)`; on each item add to `HighlightSuggestions`; on `OnCompleted` set `IsDetectingHighlights = false` and `HasNoHighlights = HighlightSuggestions.Count == 0`; on `OnError` show error snackbar; add to `_disposables`
- [x] T078 [US3] Implement `PreviewHighlightCommand` (`[RelayCommand]`) in `EditorViewModel.cs`: seek MediaElement to `suggestion.StartTime` and play; observe `PlayheadPosition` with `.TakeWhile(pos => pos < suggestion.EndTime)` then pause when end is reached; add to `_disposables`
- [x] T079 [US3] Implement `AcceptHighlightCommand` (`[RelayCommand]`) in `EditorViewModel.cs`: create `ClipSegment` from suggestion (`SequenceOrder = Segments.Count`); call `_repository.AddSegmentAsync`; add `ClipSegmentViewModel` to `Segments`; remove suggestion from `HighlightSuggestions`; update `IsExportReady`
- [x] T080 [US3] Implement `DismissHighlightCommand` (`[RelayCommand]`) in `EditorViewModel.cs`: remove `HighlightSuggestion` from `HighlightSuggestions` without modifying the timeline
- [x] T081 [US3] Add Highlights panel to `EditorPage.xaml`: collapsible `Border` below the timeline (toggled by toolbar button); `ActivityIndicator` bound to `IsDetectingHighlights`; `Label` "No highlights detected" bound to `HasNoHighlights`; `CollectionView` bound to `HighlightSuggestions` with `DataTemplate` showing `StartTime`, `EndTime`, `ActivityScore` (as `ProgressBar`), "Preview" `Button` (`PreviewHighlightCommand`), "Accept" `Button` (`AcceptHighlightCommand`), "Dismiss" icon `ImageButton` (`DismissHighlightCommand`)
- [x] T082 [US3] Add "Detect Highlights" `ToolbarItem` to `EditorPage.xaml`: bind `DetectHighlightsCommand`; disabled when `IsDetectingHighlights = true` or `CurrentProject.LocalVideoPath` is null
- [x] T083 [US3] Smoke-test highlights: tap "Detect Highlights" on varied-audio video; verify non-blocking spinner; verify suggestion list; tap "Preview" on a suggestion; verify MediaElement plays correct range; tap "Accept"; verify segment appears on timeline

**Checkpoint**: Smart Highlights detection fully operational and integrated into the Editor.

---

## Phase 6: Export & Share (User Story 4 — Priority: P4)

**Goal**: Render the full timeline to a single MP4 via FFmpeg concatenate; save to the Android gallery via MediaStore; share via native Android share sheet using `Microsoft.Maui.ApplicationModel.DataTransfer.Share`.

**Independent Test**: With >= 1 segment on the timeline, tap "Export"; verify background progress notification appears; on completion tap "Save to Gallery" and find the MP4 in the Photos/Gallery app; tap "Share" and verify the Android share sheet opens with the file attached.

- [x] T084 [US4] Add `ExportCommand` (`[RelayCommand]`) to `EditorViewModel.cs`: guard `Segments.Count > 0` — show informative snackbar "Add at least one clip to export" if empty (FR-026); set `[ObservableProperty] bool isExporting = true`; set `[ObservableProperty] double exportProgress = 0`
- [x] T085 [US4] Implement export pipeline in `EditorViewModel.cs`: for each `ClipSegmentViewModel` in `SequenceOrder` call `_processingService.TrimAsync` (with `!IsAudioEnabled` mute flag); if `IsCropApplied` chain `CropToVerticalAsync`; collect final temp file paths; call `_processingService.ConcatenateAsync(tempPaths, Path.Combine(FileSystem.CacheDirectory, $"{CurrentProject.Id}_export.mp4"))`; subscribe `ProcessingEvents` to update `ExportProgress`; on `IsComplete && IsSuccess` set `IsExporting = false` and show Export result panel; on failure set `IsExporting = false` and show error snackbar
- [x] T086 [US4] Add background export notification in `Platforms/Android/`: using `#if ANDROID` guard create a foreground `NotificationManager` notification showing export progress percentage; start when export begins; update with each `ProcessingEvent`; dismiss on completion or error
- [x] T087 [US4] Implement `SaveToGalleryCommand` (`[RelayCommand]`) in `EditorViewModel.cs`: using `#if ANDROID` insert `ContentValues` to `Android.Provider.MediaStore.Video.Media.ExternalContentUri` with `DISPLAY_NAME`, `MIME_TYPE = "video/mp4"`, `RELATIVE_PATH = "Movies/YTShortsEditor"`; open `OutputStream` via `ContentResolver`; copy exported MP4 bytes; request `READ_MEDIA_VIDEO` permission on API >= 33 before writing
- [x] T088 [US4] Implement `ShareCommand` (`[RelayCommand]`) in `EditorViewModel.cs`: call `await Microsoft.Maui.ApplicationModel.DataTransfer.Share.RequestAsync(new ShareFileRequest { Title = CurrentProject.Title, File = new ShareFile(exportFilePath, "video/mp4") })`
- [x] T089 [US4] Handle insufficient storage during export: catch `IOException` with "No space left" in `MediaProcessingService.ConcatenateAsync`; emit `ProcessingEvent` with `IsSuccess = false`, `ErrorDetail = "Insufficient storage"`; `EditorViewModel` handles this and shows snackbar "Export failed: free up storage space and retry" (FR-027)
- [x] T090 [US4] Add Export result panel to `EditorPage.xaml` (modal bottom sheet or separate section): shown after export completes; displays video duration and file size; "Save to Gallery" `Button` (`SaveToGalleryCommand`); "Share" `Button` (`ShareCommand`); "Done" `Button` to dismiss
- [x] T091 [US4] Guard empty-timeline export in `EditorPage.xaml`: "Export" `ToolbarItem` is disabled (`CanExecute`) when `IsExportReady == false`; tapping when disabled shows informative snackbar
- [x] T092 [US4] Smoke-test export: two segments (one muted, one cropped); tap Export; verify progress bar advances; on completion tap "Save to Gallery"; open Gallery on emulator; confirm MP4 present and playable

**Checkpoint**: Full export and share pipeline complete. All four user stories operational.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: App-wide hardening, error recovery, background operation continuity, and full quickstart validation.

- [ ] T093 [P] Implement missing-file recovery in `EditorViewModel.LoadProjectAsync`: if `LocalVideoPath` is non-null but the file does not exist on disk, set `Project.DownloadState = Failed`, `ErrorMessage = "Video file missing. Please re-download."`, call `_repository.UpdateProjectAsync`, navigate back to `MainPage` with an error snackbar
- [ ] T094 Implement backgrounded-download continuity: register an Android `WorkManager` job (`#if ANDROID`) or link the download `CancellationToken` to `Application.Current.Lifecycle` events; add a `OnResume` handler in `Platforms/Android/MainActivity.cs` that re-subscribes `MainViewModel` to `DownloadEvents` when the app is foregrounded
- [ ] T095 [P] Add XML documentation comments (`/// <summary>`) to all public types and public members in `Models/`, `Repositories/`, `Services/`, `ViewModels/` — required by constitution Section V
- [ ] T096 [P] Review all classes for the ~300-line limit per constitution Section V; split any class exceeding ~300 LOC (e.g. extract `HighlightViewModel` from `EditorViewModel` if needed)
- [ ] T097 [P] Audit all Rx.NET subscriptions across `MainViewModel` and `EditorViewModel`: confirm every subscription is added to `_disposables` (`CompositeDisposable`); confirm `Dispose()` is called from `OnDisappearing`; fix any subscription leaks
- [ ] T098 [P] Register `IScheduler` abstractions in `MauiProgram.cs` for testability: `Scheduler.MainThread` as main scheduler, `NewThreadScheduler.Default` as background scheduler; both resolvable by name for constructor injection in services and ViewModels
- [ ] T099 Run the full `quickstart.md` validation suite (VS-001 through VS-010) on a physical Android device or fully configured emulator; document pass/fail results; fix any regressions before declaring the feature complete

**Checkpoint**: All quickstart validation scenarios pass. Feature is shippable.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — start immediately
- **Phase 2 (US1 Dashboard)**: Depends on Phase 1 — BLOCKS all editor phases
- **Phase 3 (US2 Editor UI)**: Depends on Phase 2 (needs `IProjectRepository`, Shell routing, navigation working)
- **Phase 4 (FFmpeg layer)**: Depends on Phase 3 (needs `EditorViewModel` and `TimelineControl` in place)
- **Phase 5 (US3 Highlights)**: Depends on Phase 4 (reuses FFmpegKit bridge and `EditorViewModel` subscription pattern)
- **Phase 6 (US4 Export)**: Depends on Phase 4 (reuses `TrimAsync`, `CropToVerticalAsync`, `ConcatenateAsync`)
- **Phase 7 (Polish)**: Depends on all phases complete

### User Story Dependencies

- **US1 (P1)**: Can start after Phase 1 — no story dependencies
- **US2 (P2)**: Depends on US1 navigation and repository being operational
- **US3 (P3)**: Depends on US2 `EditorViewModel` and `MediaElement` in place; independent of US4
- **US4 (P4)**: Depends on Phase 4 FFmpeg layer; independent of US3

### Parallel Opportunities

- T003–T009 (NuGet installs): all parallel
- T010–T015 (Model/record files): all parallel after T010
- T013–T015 (Rx message records): parallel with T011–T012 (EF entities)
- T020 (`IProjectRepository`) and T022 (service interfaces): parallel with each other
- T026 (`Colors.xaml`) and T027 (`Styles.xaml`): parallel
- T047 (`ClipSegmentViewModel`) and T048 (`EditorViewModel` skeleton): parallel
- T066–T067 (`MediaProcessingService` + bridge) and T072–T073 (`EditorViewModel` wiring): parallel
- T074–T075 (`HighlightDetectionService`) and T076–T077 (`EditorViewModel` highlight state): parallel
- T086, T087, T088 (notification, gallery, share): parallel
- T093–T098 (all Phase 7 polish tasks): parallel

---

## Parallel Example: Phase 1 NuGet Setup

```text
Launch simultaneously:
  T003 — Install CommunityToolkit.Mvvm
  T004 — Install CommunityToolkit.Maui
  T005 — Install YoutubeExplode
  T006 — Install FFmpegKit binding
  T007 — Install System.Reactive
  T008 — Install EF Core SQLite
  T009 — Install Material theme
Then sequentially:
  T010 → T011 → T012 (enum then entities)
  T016 → T017 → T018 → T019 (DbContext → register → migrate → auto-apply)
  T020 → T021 (interface → implementation)
  T022 → T023 (service interfaces → DI registration)
  T026 + T027 (parallel color/style tokens)
  T028 (build verification — final gate)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: T001–T028
2. Complete Phase 2: T029–T046
3. **STOP and VALIDATE**: Run VS-001 through VS-004 from `quickstart.md`
4. The app can download YouTube videos and display them as reactive project cards — this is a shippable MVP

### Incremental Delivery

1. Phase 1 + Phase 2 → **MVP**: YouTube downloader with reactive project list
2. Phase 3 → **v0.2**: Non-linear editor UI (metadata-only editing, no FFmpeg)
3. Phase 4 → **v0.3**: Real FFmpeg rendering (actual processed video preview)
4. Phase 5 → **v0.4**: Smart Highlights AI suggestions
5. Phase 6 → **v1.0**: Export + Share (feature complete)
6. Phase 7 → **v1.0 RC**: Polished, validated, shippable

### Parallel Team Strategy

Once Phase 1 completes:
- **Developer A**: Phase 2 (Dashboard, `YouTubeDownloadService`, `MainViewModel`)
- **Developer B**: Phase 3 prep (`EditorPage` XAML skeleton, `TimelineControl`, `ClipSegmentViewModel`)
- After Phase 2 done: Developer B completes Phase 3; Developer A starts Phase 4
- After Phase 3+4 done: Phase 5 and Phase 6 can proceed in parallel on separate branches

---

## Notes

- `[P]` = touches a different file from concurrent `[P]` tasks; safe to parallelize within the same phase
- `[USn]` maps every implementation task to its user story for full traceability
- Every ViewModel Rx subscription MUST be added to `CompositeDisposable` — this is a constitution requirement (Section II)
- Never call synchronous EF Core methods (`ToList`, `SaveChanges`) — always use async variants (`ToListAsync`, `SaveChangesAsync`)
- Never use `null!` suppression without a justifying inline comment — nullable safety is a build error per the constitution
- `quickstart.md` VS-001 through VS-010 are the acceptance gate before declaring the feature done
