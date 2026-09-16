# Quickstart Validation Guide: YouTube Shorts Editor

**Phase**: 1 — Design & Contracts
**Date**: 2026-09-15
**Feature**: [spec.md](spec.md) | **Data Model**: [data-model.md](data-model.md) | **Contracts**: [contracts/](contracts/)

---

## Prerequisites

| Requirement | Details |
|---|---|
| Development machine | Windows 11 with Visual Studio 2022 (17.10+) or Rider 2024+ |
| .NET SDK | .NET 9 SDK with MAUI workload (`dotnet workload install maui-android`) |
| Android tooling | Android SDK 35, Build Tools 35, Platform Tools |
| Test device / emulator | Android 10+ (API 29+); emulator with x86_64 image recommended for speed |
| Internet connectivity | Required for YouTube download scenarios |
| YouTube test URL | A public, non-age-restricted video ≤ 10 minutes (keep for reproducibility) |

---

## Setup Steps

```bash
# 1. Clone / open the repository
cd YoutubeShortsEditorMobile

# 2. Restore NuGet packages
dotnet restore

# 3. Apply EF Core migrations to the local dev SQLite database
#    (Only needed if running via dotnet CLI; Visual Studio applies on first run)
dotnet ef database update --project YoutubeShortsEditorMobile

# 4. Build for Android (debug)
dotnet build -f net9.0-android -c Debug

# 5. Deploy to connected device or emulator
dotnet run -f net9.0-android
```

---

## Validation Scenarios

### VS-001: App Launches Without Crash

**Steps**:
1. Deploy the debug build to an Android 10+ emulator.
2. Observe the splash screen and MainPage (Dashboard).

**Expected outcome**: Dashboard loads with an empty project list, a URL input field, and a submit button. No crash. SQLite database file created at `AppDataDirectory/yte_editor.db`.

---

### VS-002: URL Submission Creates a Project Card (FR-001, FR-002, FR-003)

**Steps**:
1. Tap the URL entry field on the Dashboard.
2. Enter a valid public YouTube URL (e.g., `https://www.youtube.com/watch?v=dQw4w9WgXcQ`).
3. Tap the submit / download button.

**Expected outcome**:
- A new project card appears immediately in the list with `DownloadState = Downloading` and a progress indicator.
- The card title matches the YouTube video title fetched from metadata.
- A row is present in the `Projects` SQLite table with `DownloadState = 1` (Downloading).

---

### VS-003: Invalid URL Shows Inline Error (FR-002)

**Steps**:
1. Enter `not-a-url` in the URL field.
2. Tap submit.

**Expected outcome**: An inline error message appears below the field. No project card is created. No database row is inserted.

---

### VS-004: Download Completes and Card Becomes Interactive (FR-004)

**Steps**:
1. Submit a valid short YouTube URL (< 2 min recommended for speed).
2. Wait for the download progress bar to reach 100%.

**Expected outcome**:
- Progress indicator disappears.
- Card state changes to `Completed` (visual indicator).
- Tapping the card navigates to `EditorPage` with the project ID in the route.
- `Project.LocalVideoPath` in SQLite is set to the downloaded file path.
- The file exists on disk at that path.

---

### VS-005: Editor Loads and Video Plays (FR-007)

**Steps**:
1. Complete VS-004.
2. Tap the project card.

**Expected outcome**:
- `EditorPage` loads with a `MediaElement` showing the first frame of the downloaded video.
- Tapping the play button starts playback.
- Timeline shows one full-length segment spanning the video duration.

---

### VS-006: Trim a Segment (FR-009)

**Steps**:
1. In the Editor with a loaded video, drag the left trim handle of the segment rightward by ~20% of the timeline width.
2. Tap Play.

**Expected outcome**:
- Playback starts from the new in-point (approximately 20% into the video), not from 00:00.
- The `ClipSegment.StartTime` in SQLite is updated to match.

---

### VS-007: Split a Segment (FR-010)

**Steps**:
1. Play the video and pause at ~30% through.
2. Tap the "Split" toolbar button.

**Expected outcome**:
- The timeline now shows two segments.
- `ClipSegments` table has two rows for this project with non-overlapping `StartTime`/`EndTime`.
- `SequenceOrder` is `0` and `1` respectively.

---

### VS-008: Mute a Segment (FR-012)

**Steps**:
1. With two segments on the timeline, tap the mute icon on the second segment.

**Expected outcome**:
- Mute icon becomes active (visual toggle).
- `ClipSegment.IsAudioEnabled = false` in SQLite for that segment.
- Playback of that segment is silent (validate on device with audio output).

---

### VS-009: Export Produces Playable MP4 (FR-022, FR-024)

**Steps**:
1. With at least one segment on the timeline, tap "Export".
2. Wait for export progress to complete.
3. Tap "Save to Gallery".

**Expected outcome**:
- A progress bar is shown during export.
- On completion, a success notification appears.
- Opening the device Gallery app shows the exported MP4.
- The MP4 is playable and reflects the trimmed/edited timeline.

---

### VS-010: Smart Highlights Returns Suggestions (FR-016, FR-018)

**Steps**:
1. Open a project with a downloaded video that has varied audio (music, speech).
2. Tap "Detect Highlights".

**Expected outcome**:
- A non-blocking progress indicator appears.
- After analysis (≤ 30 s for 10-min video), a list of suggested clips appears with start time, end time, and an activity score.
- Tapping "Preview" on a suggestion plays that time range in the MediaElement.
- Tapping "Accept" appends the segment to the timeline.

---

## Known Limitations (v1)

- Age-restricted or private YouTube videos will return an error (by design — see Assumptions).
- Audio-only or live-stream URLs are not supported and will fail with an informative error.
- Very long videos (> 60 minutes) may produce slow highlight analysis; this is out-of-scope for v1 optimization.

---

## References

- Data entities and field rules: [data-model.md](data-model.md)
- Service contracts: [contracts/IYouTubeDownloadService.md](contracts/IYouTubeDownloadService.md), [contracts/IMediaProcessingService.md](contracts/IMediaProcessingService.md), [contracts/IHighlightDetectionService.md](contracts/IHighlightDetectionService.md), [contracts/IProjectRepository.md](contracts/IProjectRepository.md)
- Architecture & DI: [plan.md](plan.md)
- Technical research decisions: [research.md](research.md)
