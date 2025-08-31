using System.Globalization;
using Notification.Wpf.Converters;

namespace SuperEncode.Wpf.Converters
{
    internal class TimeSpanTextConverter : ValueConverter
    {
        public override object Convert(object? v, Type t, object? p, CultureInfo c)
        {
            if (v is TimeSpan timeSpan)
                return $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
            return "00:00:00";
        }
    }
}
