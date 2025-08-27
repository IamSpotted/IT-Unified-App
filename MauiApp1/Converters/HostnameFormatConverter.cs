using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace MauiApp1.Converters;

public class HostnameFormatConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var hostname = value?.ToString() ?? "";
        return hostname.Replace("-", " ").Replace("_", " ");
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}