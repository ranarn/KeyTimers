using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace KeyTimers.Views;

/// <summary>
/// Converts a hex colour string (e.g. "#FF4444") to a <see cref="SolidColorBrush"/>.
/// Returns <see cref="Brushes.White"/> on any parse failure.
/// </summary>
[ValueConversion(typeof(string), typeof(SolidColorBrush))]
public sealed class HexColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string hex)
        {
            try
            {
                var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex);
                return new SolidColorBrush(color);
            }
            catch { /* fall through */ }
        }
        return System.Windows.Media.Brushes.White;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
