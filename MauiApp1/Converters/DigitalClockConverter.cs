namespace MauiApp1.Converters;

/// <summary>
/// Converts DateTime to 7-segment digital clock display style
/// </summary>
public class DigitalClockConverter : IValueConverter
{
    // 7-segment digit patterns using special characters that look like digital display
    private static readonly Dictionary<char, string> DigitPatterns = new()
    {
        { '0', "𝟎" }, { '1', "𝟏" }, { '2', "𝟐" }, { '3', "𝟑" }, { '4', "𝟒" },
        { '5', "𝟓" }, { '6', "𝟔" }, { '7', "𝟕" }, { '8', "𝟖" }, { '9', "𝟗" },
        { ':', ":" }, { ' ', " " }, { '/', "/" }, { '-', "-" }
    };

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not DateTime dateTime)
            return value?.ToString() ?? string.Empty;

        var format = parameter?.ToString() ?? "datetime";
        
        string text = format.ToLower() switch
        {
            "time" => dateTime.ToString("HH:mm:ss"),
            "date" => dateTime.ToString("MM/dd/yyyy"),
            "datetime" => dateTime.ToString("MM/dd/yyyy HH:mm:ss"),
            _ => dateTime.ToString(format)
        };

        // Convert each character to its digital display equivalent
        var digitalText = new StringBuilder();
        foreach (char c in text)
        {
            if (DigitPatterns.TryGetValue(c, out var pattern))
            {
                digitalText.Append(pattern);
            }
            else
            {
                digitalText.Append(c);
            }
        }

        return digitalText.ToString();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
