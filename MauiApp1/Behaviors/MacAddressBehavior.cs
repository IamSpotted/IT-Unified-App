using Microsoft.Maui.Controls;
using MauiApp1.Converters;

namespace MauiApp1.Behaviors
{
    public class MacAddressBehavior : Behavior<Entry>
    {
        private Entry? _entry;

        protected override void OnAttachedTo(Entry entry)
        {
            _entry = entry;
            entry.TextChanged += OnTextChanged;
            base.OnAttachedTo(entry);
        }

        protected override void OnDetachingFrom(Entry entry)
        {
            entry.TextChanged -= OnTextChanged;
            _entry = null;
            base.OnDetachingFrom(entry);
        }

        private void OnTextChanged(object? sender, TextChangedEventArgs e)
        {
            if (_entry == null || string.IsNullOrEmpty(e.NewTextValue))
                return;

            var cursorPosition = _entry.CursorPosition;
            var oldLength = e.OldTextValue?.Length ?? 0;
            var newLength = e.NewTextValue.Length;

            // Format the MAC address
            var formattedText = MacAddressConverter.FormatMacAddress(e.NewTextValue);

            // Only update if the formatted text is different
            if (formattedText != e.NewTextValue)
            {
                _entry.Text = formattedText;

                // Adjust cursor position based on formatting changes
                var lengthDifference = formattedText.Length - newLength;
                var newCursorPosition = cursorPosition + lengthDifference;

                // Ensure cursor position is within bounds
                newCursorPosition = Math.Max(0, Math.Min(newCursorPosition, formattedText.Length));
                
                _entry.CursorPosition = newCursorPosition;
            }
        }
    }
}
