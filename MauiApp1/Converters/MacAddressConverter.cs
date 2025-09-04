using Microsoft.Maui.Controls;
using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace MauiApp1.Converters
{
    public class MacAddressConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string macAddress && !string.IsNullOrEmpty(macAddress))
            {
                return FormatMacAddress(macAddress);
            }
            return value;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string macAddress && !string.IsNullOrEmpty(macAddress))
            {
                return FormatMacAddress(macAddress);
            }
            return value;
        }

        public static string FormatMacAddress(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            // Remove all non-hexadecimal characters
            string cleanInput = Regex.Replace(input.ToUpper(), @"[^0-9A-F]", "");

            // Limit to 12 characters (6 octets * 2 hex chars each)
            if (cleanInput.Length > 12)
                cleanInput = cleanInput.Substring(0, 12);

            // Add colons every 2 characters
            string formatted = "";
            for (int i = 0; i < cleanInput.Length; i += 2)
            {
                if (i > 0)
                    formatted += ":";
                
                if (i + 1 < cleanInput.Length)
                    formatted += cleanInput.Substring(i, 2);
                else
                    formatted += cleanInput.Substring(i, 1);
            }

            return formatted;
        }

        public static bool IsValidHexChar(char c)
        {
            return (c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f');
        }
    }
}
