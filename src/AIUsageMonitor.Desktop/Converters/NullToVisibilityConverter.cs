using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AIUsageMonitor.Desktop.Converters;

public sealed class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is null || string.IsNullOrWhiteSpace(value.ToString())
            ? Visibility.Collapsed
            : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
