using System.Globalization;
using System.Windows.Data;

namespace SuperEncode.Wpf.Converters;

internal class SubtitleInFileConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool state) return !state;
        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var boolValue = (bool)(value ?? true);
        return !boolValue;
    }
}