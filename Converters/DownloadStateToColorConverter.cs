using System.Globalization;
using YoutubeShortsEditorMobile.Models;

namespace YoutubeShortsEditorMobile.Converters;

public class DownloadStateToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DownloadState state)
        {
            if (Application.Current?.Resources.TryGetValue("MD3Primary", out var primary) == true &&
                Application.Current?.Resources.TryGetValue("MD3Error", out var error) == true)
            {
                return state switch
                {
                    DownloadState.Pending => Colors.Gray,
                    DownloadState.Downloading => primary,
                    DownloadState.Completed => Colors.Green,
                    DownloadState.Failed => error,
                    _ => Colors.Transparent
                };
            }
        }
        return Colors.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
