namespace MauiApp1.Controls;

public partial class SevenSegmentDigit : ContentView
{
    // Define which segments are lit for each digit
    private static readonly Dictionary<char, bool[]> DigitSegments = new()
    {
        ['0'] = new[] { true, true, true, true, true, true, false },   // A,B,C,D,E,F,G
        ['1'] = new[] { false, true, true, false, false, false, false }, // B,C
        ['2'] = new[] { true, true, false, true, true, false, true },    // A,B,D,E,G
        ['3'] = new[] { true, true, true, true, false, false, true },    // A,B,C,D,G
        ['4'] = new[] { false, true, true, false, false, true, true },   // B,C,F,G
        ['5'] = new[] { true, false, true, true, false, true, true },    // A,C,D,F,G
        ['6'] = new[] { true, false, true, true, true, true, true },     // A,C,D,E,F,G
        ['7'] = new[] { true, true, true, false, false, false, false },  // A,B,C
        ['8'] = new[] { true, true, true, true, true, true, true },      // All segments
        ['9'] = new[] { true, true, true, true, false, true, true },     // A,B,C,D,F,G
        [' '] = new[] { false, false, false, false, false, false, false } // No segments
    };

    public static readonly BindableProperty DigitProperty =
        BindableProperty.Create(nameof(Digit), typeof(char), typeof(SevenSegmentDigit), ' ', propertyChanged: OnDigitChanged);

    public char Digit
    {
        get => (char)GetValue(DigitProperty);
        set => SetValue(DigitProperty, value);
    }

    public SevenSegmentDigit()
    {
        InitializeComponent();
        UpdateDisplay();
    }

    private static void OnDigitChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is SevenSegmentDigit control)
        {
            control.UpdateDisplay();
        }
    }

    private void UpdateDisplay()
    {
        // Get all the individual grid squares for each segment
        var segmentA = new[] { A1, A2, A3, A4, A5 };
        var segmentB = new[] { B1, B2, B3, B4 };
        var segmentC = new[] { C1, C2, C3, C4 };
        var segmentD = new[] { D1, D2, D3, D4, D5 };
        var segmentE = new[] { E1, E2, E3, E4 };
        var segmentF = new[] { F1, F2, F3, F4 };
        var segmentG = new[] { G1, G2, G3, G4, G5 };
        
        var allSegments = new[] { segmentA, segmentB, segmentC, segmentD, segmentE, segmentF, segmentG };
        
        if (DigitSegments.TryGetValue(Digit, out var pattern))
        {
            for (int i = 0; i < allSegments.Length; i++)
            {
                var color = pattern[i] ? Color.FromArgb("#FF2020") : Color.FromArgb("#110000");
                foreach (var square in allSegments[i])
                {
                    square.BackgroundColor = color;
                }
            }
        }
        else
        {
            // Unknown character - turn all segments off
            foreach (var segmentGroup in allSegments)
            {
                foreach (var square in segmentGroup)
                {
                    square.BackgroundColor = Color.FromArgb("#110000");
                }
            }
        }
    }
}
