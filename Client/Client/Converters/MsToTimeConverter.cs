using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace Client.Converters;

public class MsToTimeConverter : IValueConverter
{
    public static readonly MsToTimeConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        long ms = value switch
        {
            long l => l,
            int i  => i,
            _      => 0
        };
        var totalSec = ms / 1000;
        return $"{totalSec / 60}:{totalSec % 60:D2}";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
