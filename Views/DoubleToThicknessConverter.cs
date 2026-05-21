using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace KeyTimers.Views;

/// <summary>Converts a <see cref="double"/> to a uniform <see cref="Thickness"/>.</summary>
public sealed class DoubleToThicknessConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => new Thickness(value is double d ? d : 0);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
