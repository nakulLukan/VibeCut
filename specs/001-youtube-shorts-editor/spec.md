# Feature Specification: YouTube Shorts Editor

**Feature Branch**: `001-youtube-shorts-editor`

**Created**: 2026-09-15

**Status**: Draft

**Input**: User description: "YouTube Shorts Editor — a .NET MAUI Android app to download YouTube videos, perform non-linear editing (trim, split, crop, mute), identify high-engagement segments, and export or share the final Shorts."

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Download a YouTube Video as a New Project (Priority: P1)

A user arrives at the Dashboard, pastes a YouTube URL into the input field, and submits it. The app validates the link, creates a new Project entry, and downloads the video to local storage in the background. While the download is in progress the project card displays a live progress indicator. When complete, the card becomes tappable and the user can open the editor.

**Why this priority**: This is the entry point of the entire workflow. Nothing downstream (editing, highlights, export) is possible until a video is available locally. It unblocks every other story and constitutes a minimal, demonstrable product slice.

**Independent Test**: Submitting a valid YouTube URL results in a new project card appearing and eventually becoming fully interactive after the download completes, with no other features required.

**Acceptance Scenarios**:

1. **Given** the Dashboard is open and the URL field is empty, **When** the user submits a valid YouTube URL, **Then** a new project card appears immediately with a progress indicator and download status text.
2. **Given** a download is in progress, **When** the download completes successfully, **Then** the progress indicator disappears, the card is tappable, and the locally stored video file exists on the device.
3. **Given** the user submits a URL with no network connectivity, **When** the download is attempted, **Then** the project card shows a clear error state with a "Retry" option and no incomplete file is left on disk.
4. **Given** the user submits an invalid or private YouTube URL, **When** the app attempts to parse it, **Then** an inline validation message is shown immediately and no project is created.
5. **Given** multiple projects have been previously downloaded, **When** the Dashboard is opened, **Then** all past projects are listed in reverse-chronological order by creation date.

---

### User Story 2 — Edit Video on the Timeline (Priority: P2)

A user opens a downloaded project and is taken to the Editor screen. They see a video playback area and a horizontal timeline showing the full clip as a single segment. The user can drag handles to trim the start and end, tap to split the clip at the current playback position, reorder segments by dragging, toggle audio on or off per segment, and apply a 9:16 vertical crop to any segment. Playback reflects the edited timeline in real-time preview.

**Why this priority**: Editing is the core value proposition. Once a video is downloaded (P1), this story delivers the primary differentiation of the app.

**Independent Test**: With a single downloaded video, a user can produce a correctly trimmed, cropped, and muted preview sequence by manipulating the timeline controls, without requiring the export or highlights features.

**Acceptance Scenarios**:

1. **Given** the Editor is open with a downloaded video, **When** the user drags the left trim handle inward, **Then** playback starts from the new in-point and the timeline reflects the trimmed segment visually.
2. **Given** the user positions the playhead on the timeline, **When** they tap "Split", **Then** the single segment is divided into two independently controllable segments at that position.
3. **Given** a segment exists, **When** the user taps the mute toggle for that segment, **Then** playback of that segment is silent and the mute state is visually indicated on the timeline.
4. **Given** multiple segments exist, **When** the user drags a segment to a new position in the timeline, **Then** the sequence order updates and the preview reflects the new order.
5. **Given** a segment is selected, **When** the user applies the 9:16 crop, **Then** the preview renders the segment with vertical aspect ratio cropping applied.
6. **Given** the user makes edits and navigates away, **When** they return to the project, **Then** all edits are fully restored.

---

### User Story 3 — Smart Highlights Detection (Priority: P3)

A user who does not know which part of their video will perform best taps "Detect Highlights" in the Editor. The app analyzes the audio of the downloaded video and surfaces a list of suggested clip segments ordered by estimated engagement (based on audio activity). The user can preview each suggestion in the playback area, accept it to add it to the timeline, or dismiss it.

**Why this priority**: This is an enhancement that adds intelligence to the workflow. It is independently useful alongside a manually edited timeline and does not block any other feature.

**Independent Test**: With a downloaded video, tapping "Detect Highlights" produces a reviewable list of suggested time-ranges; accepting one appends it as a segment in the timeline.

**Acceptance Scenarios**:

1. **Given** the Editor is open, **When** the user initiates highlight detection, **Then** a progress indicator is shown and the user can still interact with the rest of the Editor (non-blocking).
2. **Given** analysis completes successfully, **When** the results are presented, **Then** at least one suggested clip is shown with its start time, end time, and a confidence or activity score.
3. **Given** a suggested clip is shown, **When** the user taps "Preview", **Then** the MediaElement plays exactly that suggested segment.
4. **Given** a suggested clip is shown, **When** the user taps "Accept", **Then** the segment is appended to the current timeline and appears in the segment list.
5. **Given** the video has uniformly silent audio throughout, **When** highlight detection completes, **Then** the user is informed that no distinct high-activity segments were detected.

---

### User Story 4 — Export & Share Finished Short (Priority: P4)

After finalizing the timeline, the user taps "Export". The app renders all timeline segments — with their trim points, sequence order, audio states, and crop filters — into a single MP4 file. Once rendering is complete, the user is presented with options: save the video to the device gallery, or share it directly to another app (e.g., the YouTube app).

**Why this priority**: Export closes the loop and delivers the end output. It depends on the editing workflow (P2) being complete, but is independently testable once a timeline with at least one segment exists.

**Independent Test**: With a timeline containing at least one trimmed segment, tapping Export produces a playable MP4 file on the device; the user can then tap "Save to Gallery" and find the file in the Photos/Gallery app.

**Acceptance Scenarios**:

1. **Given** the timeline has at least one segment, **When** the user initiates export, **Then** a progress indicator is shown with estimated completion; the UI remains responsive.
2. **Given** export completes successfully, **When** the user taps "Save to Gallery", **Then** the rendered MP4 appears in the device media gallery and is playable.
3. **Given** export completes successfully, **When** the user taps "Share", **Then** the Android share sheet appears with the rendered MP4 pre-attached.
4. **Given** an export is in progress, **When** the user navigates away, **Then** the export continues in the background and the user is notified upon completion.
5. **Given** available storage is insufficient during export, **When** the render process detects the shortage, **Then** the export is aborted cleanly and the user is informed with actionable guidance.

---

### Edge Cases

- What happens when the device runs out of storage mid-download?
- How does the system handle a YouTube video that is age-restricted, private, or region-locked?
- What happens if the user deletes the downloaded video file externally while a project still references it?
- What if the user applies a 9:16 crop to a video already in portrait orientation?
- What if the timeline is empty (all segments deleted) when the user taps Export?
- What happens when a split operation is attempted at the very start or very end of a segment?
- How does the app behave when backgrounded during a long download or export?

---

## Requirements *(mandatory)*

### Functional Requirements

**Dashboard & Project Management**

- **FR-001**: The system MUST display a scrollable list of all previously created projects on the Dashboard, ordered by last-edited date descending.
- **FR-002**: The system MUST provide a text input field on the Dashboard that accepts a YouTube URL and validates it before submission.
- **FR-003**: The system MUST create a new project record upon valid URL submission and immediately reflect it in the project list.
- **FR-004**: The system MUST download the video to local device storage in the background, reporting progress to the project card in real time.
- **FR-005**: The system MUST surface a clear error state on the project card if a download fails, with the ability for the user to retry.
- **FR-006**: The system MUST prevent duplicate project creation if the same URL is submitted while an active download for that URL is already in progress.

**Editor & Timeline**

- **FR-007**: The Editor MUST allow the user to preview the video using a media player embedded in the screen.
- **FR-008**: The system MUST represent the current timeline as an ordered list of clip segments displayed horizontally.
- **FR-009**: The system MUST allow trimming each segment's start and end points via draggable handles.
- **FR-010**: The system MUST allow splitting a segment into two at the current playhead position.
- **FR-011**: The system MUST allow the user to reorder segments by drag-and-drop.
- **FR-012**: The system MUST allow toggling audio on or off per individual segment.
- **FR-013**: The system MUST allow applying a 9:16 vertical crop filter to the rendered output.
- **FR-014**: The system MUST automatically persist all timeline edits without requiring an explicit "Save" action.
- **FR-015**: The system MUST update the playback preview to reflect the current state of the timeline when the user plays back.

**Smart Highlights**

- **FR-016**: The system MUST offer an on-demand action to analyze the downloaded video audio and identify high-activity segments.
- **FR-017**: The analysis MUST run asynchronously without blocking the Editor UI.
- **FR-018**: The system MUST present detected highlight segments as a reviewable list with start time, end time, and an activity/confidence indicator.
- **FR-019**: The user MUST be able to preview any suggested highlight segment in the media player before accepting or dismissing it.
- **FR-020**: The user MUST be able to accept a suggested segment to append it to the timeline or dismiss it without affecting the timeline.
- **FR-021**: The system MUST display an informative empty-state when no distinct high-activity segments are detected.

**Export & Share**

- **FR-022**: The system MUST render the complete timeline (all segments in sequence order, with trim points, audio states, and crop filters applied) into a single MP4 file.
- **FR-023**: The export process MUST run in the background; the user MUST receive a completion notification when it finishes.
- **FR-024**: The system MUST allow the user to save the exported MP4 to the device media gallery.
- **FR-025**: The system MUST allow the user to share the exported MP4 via the Android native share sheet.
- **FR-026**: The system MUST prevent export when the timeline contains no segments and inform the user.
- **FR-027**: The system MUST handle insufficient storage gracefully during export, aborting cleanly and notifying the user.

### Key Entities *(include if feature involves data)*

- **Project**: Represents a single editing session tied to one source YouTube video. Tracks the original URL, the path to the locally downloaded file, creation and last-edited timestamps, and a human-readable title derived from video metadata.
- **Clip Segment**: Represents one contiguous slice of the source video placed on the timeline. Belongs to a Project. Tracks its start and end times within the source video, its position in the sequence, whether audio is enabled, and whether the 9:16 crop is applied.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can submit a YouTube URL and reach a playable video in the Editor within 5 minutes for videos up to 10 minutes long on a standard Wi-Fi connection.
- **SC-002**: Timeline edits (trim, split, reorder, mute) are reflected in the preview within 300 ms of the user's action.
- **SC-003**: Export of a 60-second final cut assembled from multiple segments completes within 3 minutes on a mid-range Android device.
- **SC-004**: 90% of first-time users can successfully download a video, make at least one edit, and export the result without external guidance.
- **SC-005**: Smart Highlights analysis completes within 30 seconds for a 10-minute source video.
- **SC-006**: Exported MP4 files are immediately playable in the device gallery and shareable without additional format conversion.
- **SC-007**: The app does not crash or produce data loss if interrupted (backgrounded or killed by the OS) during a download or export; operations resume or restart cleanly upon re-launch.

---

## Assumptions

- Users have a valid internet connection capable of downloading YouTube video content at the time of initiating a download; offline-only usage is out of scope.
- The target device runs Android 10 (API 29) or later; older Android versions are out of scope.
- YouTube videos that are age-restricted, private, or DRM-protected are expected to fail gracefully with an informative error; no bypass is attempted.
- The source video remains on-device for the duration of an editing session; external deletion of the file results in a recoverable error state, not silent data corruption.
- Each video has one audio track; multi-track audio selection is out of scope for v1.
- The 9:16 crop targets a fixed output resolution (e.g., 1080×1920 or 720×1280); per-segment adaptive resolution is out of scope for v1.
- The Smart Highlights feature uses audio-activity heuristics only; computer-vision or ML-based detection is out of scope for v1.
- Export produces a single merged MP4; chapter markers, subtitles, and thumbnail generation are out of scope for v1.
- The app is a single-user, local-first tool; cloud sync, multi-device support, and user accounts are out of scope.
