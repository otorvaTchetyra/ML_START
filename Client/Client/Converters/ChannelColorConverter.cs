using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace Client.Converters;

public class ChannelColorConverter : IValueConverter
{
    public static readonly ChannelColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int active && parameter is string param && int.TryParse(param, out int channel))
            return active == channel
                ? new SolidColorBrush(Color.Parse("#2E7D32"))
                : new SolidColorBrush(Color.Parse("#3A3A3A"));
        return new SolidColorBrush(Color.Parse("#3A3A3A"));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
