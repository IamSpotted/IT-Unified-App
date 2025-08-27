using System.Globalization;

namespace MauiApp1.Converters;

public class SevenSegmentConverter : IValueConverter
{
    // 7-segment display character mapping using fullwidth characters for better spacing
    private static readonly Dictionary<char, string> SegmentMap = new()
    {
        ['0'] = "０",
        ['1'] = "１", 
        ['2'] = "２",
        ['3'] = "３",
        ['4'] = "４",
        ['5'] = "５",
        ['6'] = "６",
        ['7'] = "７",
        ['8'] = "８",
        ['9'] = "９",
        [':'] = "：",
        ['/'] = "／",
        [' '] = "　"
    };

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string text)
            return string.Empty;

        var result = string.Empty;
        foreach (char c in text)
        {
            if (SegmentMap.TryGetValue(c, out var segment))
            {
                result += segment;
            }
            else
            {
                result += c; // Keep original character if not mapped
            }
        }

        return result;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
