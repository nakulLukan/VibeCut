using YoutubeShortsEditorMobile.Views;

namespace YoutubeShortsEditorMobile;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		Routing.RegisterRoute(nameof(EditorPage), typeof(EditorPage));
	}
}
