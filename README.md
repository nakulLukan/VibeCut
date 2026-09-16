# VibeCut

A .NET MAUI Android application designed to streamline the creation of YouTube Shorts. This app allows you to download YouTube videos, perform non-linear editing (trim, split, crop, mute), automatically identify high-engagement segments using audio analysis, and export or share the final Shorts directly from your mobile device.

## Features

- **Download YouTube Videos**: Paste a YouTube URL to download the video directly to your local storage for editing.
- **Non-Linear Timeline Editing**: 
  - **Trim & Split**: Easily drag handles to trim clips or split segments at the playhead.
  - **Reorder**: Drag-and-drop segments to reorder them on the timeline.
  - **Mute**: Toggle audio on or off per individual segment.
  - **Vertical Crop**: Apply a 9:16 aspect ratio crop to optimize for Shorts.
- **Smart Highlights Detection**: Automatically analyze the video's audio to suggest high-activity, high-engagement segments.
- **Export & Share**: Render your final timeline into a single MP4 file and instantly save it to your device's gallery or share it via the Android share sheet.
- **Offline Editing**: Once downloaded, all editing operations are performed locally on-device.

## Requirements

- Android device running Android 10 (API 29) or later.
- Active internet connection to download source YouTube videos.

## Development Setup

This project is built using [.NET MAUI](https://learn.microsoft.com/en-us/dotnet/maui/).

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download) (or later)
- Visual Studio 2022 (with .NET Multi-platform App UI development workload) or Visual Studio Code with the .NET MAUI extension.
- Android SDK (API 29+)

### Running the App
1. Clone the repository.
2. Open the solution `YoutubeShortsEditorMobile.sln` or the project folder in your IDE.
3. Select an Android emulator or a physical device as the target.
4. Build and deploy the app.

## Project Architecture

- `Models`: Data structures for Projects and Clip Segments.
- `ViewModels`: Business logic and state management for views.
- `Views`: XAML UI pages (Dashboard, Editor, etc.).
- `Services`: Background services for downloading, video processing, and audio analysis.

## License

This project is currently under development.
