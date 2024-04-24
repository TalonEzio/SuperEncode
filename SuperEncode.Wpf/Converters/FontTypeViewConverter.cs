using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SuperEncode.Wpf.Converters
{
    public class FontTypeViewConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Typeface typeface)
            {
                return $"{typeface.Style} {typeface.Weight}";
            }
            return "Error binding";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
