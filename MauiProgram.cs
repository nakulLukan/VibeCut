using CommunityToolkit.Maui;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YoutubeShortsEditorMobile.Data;
using YoutubeShortsEditorMobile.Repositories;
using YoutubeShortsEditorMobile.Services;

namespace YoutubeShortsEditorMobile;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.UseMauiCommunityToolkitMediaElement(false)
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		builder.Services.AddDbContext<AppDbContext>(o => 
			o.UseSqlite($"Data Source={Path.Combine(FileSystem.AppDataDirectory, "yte_editor.db")}"), 
			ServiceLifetime.Transient);
			
		builder.Services.AddTransient<IProjectRepository, ProjectRepository>();
		// NewThreadScheduler: each FFmpeg operation runs on a fresh thread, preventing UI blocking
		builder.Services.AddSingleton<System.Reactive.Concurrency.IScheduler>(System.Reactive.Concurrency.NewThreadScheduler.Default);
		
		builder.Services.AddSingleton<YoutubeExplode.YoutubeClient>();
		builder.Services.AddSingleton<YoutubeShortsEditorMobile.Services.IYouTubeDownloadService, YoutubeShortsEditorMobile.Services.YouTubeDownloadService>();
		
		builder.Services.AddTransient<YoutubeShortsEditorMobile.ViewModels.MainViewModel>();
		builder.Services.AddTransient<YoutubeShortsEditorMobile.Views.MainPage>();
		
		builder.Services.AddSingleton<IMediaProcessingService, MediaProcessingService>();
		builder.Services.AddSingleton<IHighlightDetectionService, HighlightDetectionService>();
		
		builder.Services.AddTransient<YoutubeShortsEditorMobile.ViewModels.EditorViewModel>();
		builder.Services.AddTransient<YoutubeShortsEditorMobile.Views.EditorPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		var app = builder.Build();
		return app;
	}
}
