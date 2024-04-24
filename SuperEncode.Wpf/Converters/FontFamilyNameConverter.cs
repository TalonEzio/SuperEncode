using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SuperEncode.Wpf.Converters
{
    public class FontFamilyNameConverter: IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not FontFamily input) return "No Name Font";

            var fontName = input.Source.Split("#")[^1];
            return fontName;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
