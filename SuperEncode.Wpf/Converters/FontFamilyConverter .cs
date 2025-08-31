using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SuperEncode.Wpf.Converters;

public class FontFamilyConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var fonts = Fonts.GetFontFamilies(@"C:\Windows\Fonts");

        return value as FontFamily ?? fonts.First();
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}