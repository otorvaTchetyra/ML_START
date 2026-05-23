using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace Client.Converters;

public class StringEqualColorConverter : IValueConverter
{
    public static readonly StringEqualColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var active = value as string;
        var expected = parameter as string;
        return string.Equals(active, expected, StringComparison.OrdinalIgnoreCase)
            ? new SolidColorBrush(Color.Parse("#2E7D32"))
            : new SolidColorBrush(Color.Parse("#3A3A3A"));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
