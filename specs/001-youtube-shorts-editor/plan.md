# Implementation Plan: YouTube Shorts Editor

**Branch**: `001-youtube-shorts-editor` | **Date**: 2026-09-15 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/001-youtube-shorts-editor/spec.md`

---

## Summary

Build a single-user, local-first Android mobile application that allows a user to paste a YouTube URL, download the source video to the device, perform non-linear timeline editing (trim, split, reorder, mute, 9:16 crop), automatically detect high-engagement audio segments, and export/share the finished Short as an MP4.

The application is implemented as an Android-only .NET MAUI project. All persistence uses Entity Framework Core with a local SQLite database. The MVVM pattern is enforced via CommunityToolkit.Mvvm with source generators. Reactive UI state and background task orchestration are handled through Rx.NET observables. Video downloading is handled by YoutubeExplode; media processing (trim, split, join, crop, mute, audio analysis) by FFmpegKit for Android; in-app playback by CommunityToolkit.Maui.MediaElement.

---

## Technical Context

**Language/Version**: C# 12 / .NET 9 — `net9.0-android` target framework only

**Primary Dependencies**:
- `CommunityToolkit.Mvvm` (MVVM + source generators)
- `CommunityToolkit.Maui` (MediaElement, core helpers)
- `YoutubeExplode` (YouTube URL parsing + stream downloading)
- `FFmpegKit` Android binding / `Drastic.FFmpeg` NuGet wrapper (media processing)
- `System.Reactive` / Rx.NET (reactive pipelines)
- `Microsoft.EntityFrameworkCore.Sqlite` (local persistence)
- `Microsoft.Maui.Controls.Material` (Material 3 theme)

**Storage**: SQLite via EF Core on the Android internal storage path (`FileSystem.AppDataDirectory`)

**Testing**: xUnit + Moq for unit tests on ViewModels and services (pure logic); manual device/emulator testing for UI and FFmpeg integration

**Target Platform**: Android 10+ (API 29+), single APK, `net9.0-android`

**Project Type**: Mobile app (Android-only .NET MAUI)

**Performance Goals**:
- UI actions (trim handle drag, split tap) respond within 300 ms
- Export of a 60-second timeline completes within 3 minutes on mid-range hardware
- Smart Highlights analysis completes within 30 seconds for a 10-minute source video

**Constraints**:
- Android-only; no iOS, macOS, or Windows targets in `.csproj`
- No raw SQL strings unless justified with an inline comment
- All EF Core queries must be async
- Nullable reference types enabled; nullable warnings treated as errors
- No static service-locator patterns; constructor injection only

**Scale/Scope**: Single-user, local-first; ~5 screens; up to ~50 projects in SQLite; video files up to ~500 MB on device storage

---

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design.*

| Principle | Gate | Status |
|-----------|------|--------|
| Android-only target framework (`net9.0-android`) | Only one `<TargetFramework>` in `.csproj` | ✅ PASS — design specifies single Android TFM |
| MVVM via CommunityToolkit.Mvvm with source generators | All VMs inherit `ObservableObject`; `[ObservableProperty]` / `[RelayCommand]` used | ✅ PASS — planned throughout |
| No manual `INotifyPropertyChanged` | Enforced by source generator usage | ✅ PASS |
| Constructor injection only; no service-locator | All services registered in `MauiProgram.cs`; resolved via DI | ✅ PASS |
| Rx.NET / `IAsyncEnumerable<T>` for reactive/background work | Download progress, FFmpeg task state, and highlight detection all modelled as observables | ✅ PASS |
| EF Core + SQLite; async queries only | `AppDbContext` with `ToListAsync`, `SaveChangesAsync` etc. | ✅ PASS |
| Material 3 design tokens | XAML uses Material 3 styles/themes throughout | ✅ PASS |
| Nullable reference types enabled; warnings as errors | Planned in `.csproj` | ✅ PASS |
| SOLID principles; classes ≤ ~300 LOC | Enforced by interface segregation in service layer | ✅ PASS |

No violations. Complexity Tracking section not required.

---

## Project Structure

### Documentation (this feature)

```text
specs/001-youtube-shorts-editor/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   ├── IYouTubeDownloadService.md
│   ├── IMediaProcessingService.md
│   ├── IHighlightDetectionService.md
│   └── IProjectRepository.md
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
YoutubeShortsEditorMobile/          # .NET MAUI Android project root
├── YoutubeShortsEditorMobile.csproj
├── MauiProgram.cs                  # DI registration + MAUI app builder
├── AppShell.xaml / .cs             # Shell + route registration
│
├── Models/                         # EF Core entity classes (plain C# POCOs)
│   ├── Project.cs
│   └── ClipSegment.cs
│
├── Data/                           # EF Core DbContext + migrations
│   ├── AppDbContext.cs
│   └── Migrations/
│
├── Repositories/                   # Data-access abstraction layer
│   ├── IProjectRepository.cs
│   └── ProjectRepository.cs
│
├── Services/                       # Business/integration services
│   ├── IYouTubeDownloadService.cs
│   ├── YouTubeDownloadService.cs
│   ├── IMediaProcessingService.cs
│   ├── MediaProcessingService.cs
│   ├── IHighlightDetectionService.cs
│   └── HighlightDetectionService.cs
│
├── ViewModels/                     # CommunityToolkit.Mvvm ViewModels
│   ├── MainViewModel.cs
│   └── EditorViewModel.cs
│
├── Views/                          # XAML Pages
│   ├── MainPage.xaml / .cs
│   └── EditorPage.xaml / .cs
│
├── Controls/                       # Custom reusable XAML controls
│   └── TimelineControl.xaml / .cs  # Horizontal drag-and-drop timeline
│
├── Converters/                     # XAML value converters
│   ├── BoolToMuteIconConverter.cs
│   └── DownloadStateToColorConverter.cs
│
├── Resources/
│   ├── Styles/
│   │   ├── Colors.xaml             # Material 3 color tokens
│   │   └── Styles.xaml             # Global Material 3 control styles
│   └── Fonts/
│
└── Platforms/
    └── Android/
        ├── AndroidManifest.xml     # Permissions: INTERNET, WRITE_EXTERNAL_STORAGE, POST_NOTIFICATIONS
        ├── MainActivity.cs
        └── MainApplication.cs
```

**Structure Decision**: Single .NET MAUI Android project. No separate API or backend. All code lives under the project root, organized by layer (Models → Data → Repositories → Services → ViewModels → Views).

---

## Dependency Injection Registry

Registered in `MauiProgram.cs`:

| Registration | Lifetime | Reason |
|---|---|---|
| `AppDbContext` | Transient | Avoids cross-thread DbContext sharing; each VM gets its own instance |
| `IProjectRepository` → `ProjectRepository` | Transient | Depends on `AppDbContext`; scoped per operation |
| `IYouTubeDownloadService` → `YouTubeDownloadService` | Singleton | Holds a single `HttpClient` / `YoutubeClient`; manages concurrent download queue |
| `IMediaProcessingService` → `MediaProcessingService` | Singleton | FFmpegKit session management; one active session at a time |
| `IHighlightDetectionService` → `HighlightDetectionService` | Singleton | Stateless analysis; Singleton avoids repeated initialisation |
| `MainViewModel` | Transient | One per page navigation |
| `EditorViewModel` | Transient | One per editor session |
| `MainPage` | Transient | MAUI page wired to ViewModel |
| `EditorPage` | Transient | MAUI page wired to ViewModel |

---
