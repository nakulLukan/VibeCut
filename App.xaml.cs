using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace YoutubeShortsEditorMobile;

public partial class App : Application
{
    private readonly IServiceProvider _services;

	public App(IServiceProvider services)
	{
		InitializeComponent();
        _services = services;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}

    protected override void OnStart()
    {
        base.OnStart();
        
        Task.Run(async () => 
        {
            try
            {
                using var scope = _services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<YoutubeShortsEditorMobile.Data.AppDbContext>();
                await db.Database.MigrateAsync();
            }
            catch (Exception ex)
            {
                // In a production app, log this exception
                System.Diagnostics.Debug.WriteLine($"Migration failed: {ex.Message}");
            }
        });
    }
}