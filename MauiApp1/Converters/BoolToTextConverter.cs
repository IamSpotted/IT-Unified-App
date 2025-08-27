using System.Globalization;

namespace MauiApp1.Converters;

public class BoolToTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue && parameter is string parameterText)
        {
            // Support parameter format: "TrueText|FalseText"
            var parts = parameterText.Split('|');
            if (parts.Length == 2)
            {
                return boolValue ? parts[0] : parts[1];
            }
        }
        
        // Fallback to default behavior
        if (value is bool isActive)
        {
            return isActive ? "🟢" : "🔴";
        }
        
        return "❓";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
