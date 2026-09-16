using System.Globalization;

namespace YoutubeShortsEditorMobile.Converters;

public class BoolToMuteIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isAudioEnabled)
        {
            return isAudioEnabled ? "volume_on.png" : "volume_off.png";
        }
        return "volume_on.png";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
