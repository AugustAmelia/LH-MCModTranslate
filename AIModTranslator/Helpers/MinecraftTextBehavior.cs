using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace AIModTranslator.Helpers;

public class MinecraftTextBehavior : AvaloniaObject
{
    public static readonly AttachedProperty<string> FormattedTextProperty =
        AvaloniaProperty.RegisterAttached<MinecraftTextBehavior, TextBlock, string>(
            "FormattedText", string.Empty);

    static MinecraftTextBehavior()
    {
        FormattedTextProperty.Changed.AddClassHandler<TextBlock>(OnFormattedTextChanged);
    }

    public static string GetFormattedText(AvaloniaObject obj)
    {
        return obj.GetValue(FormattedTextProperty);
    }

    public static void SetFormattedText(AvaloniaObject obj, string value)
    {
        obj.SetValue(FormattedTextProperty, value);
    }

    private static void OnFormattedTextChanged(TextBlock textBlock, AvaloniaPropertyChangedEventArgs e)
    {
        if (textBlock.Inlines == null)
            textBlock.Inlines = new InlineCollection();
        
        textBlock.Inlines.Clear();
        string text = (e.NewValue as string) ?? string.Empty;

        if (string.IsNullOrEmpty(text)) return;

        var parts = text.Split('§');
        textBlock.Inlines.Add(new Run { Text = parts[0] }); // Initial text without formatting

        IBrush currentColor = Brushes.White; // Default color for preview

        for (int i = 1; i < parts.Length; i++)
        {
            string part = parts[i];
            if (part.Length == 0) continue;

            char formatCode = part[0];
            string content = part.Substring(1);

            // Apply color based on code
            currentColor = formatCode switch
            {
                '0' => Brushes.Black,
                '1' => new SolidColorBrush(Color.FromRgb(0, 0, 170)),
                '2' => new SolidColorBrush(Color.FromRgb(0, 170, 0)),
                '3' => new SolidColorBrush(Color.FromRgb(0, 170, 170)),
                '4' => new SolidColorBrush(Color.FromRgb(170, 0, 0)),
                '5' => new SolidColorBrush(Color.FromRgb(170, 0, 170)),
                '6' => new SolidColorBrush(Color.FromRgb(255, 170, 0)),
                '7' => new SolidColorBrush(Color.FromRgb(170, 170, 170)),
                '8' => new SolidColorBrush(Color.FromRgb(85, 85, 85)),
                '9' => new SolidColorBrush(Color.FromRgb(85, 85, 255)),
                'a' or 'A' => new SolidColorBrush(Color.FromRgb(85, 255, 85)),
                'b' or 'B' => new SolidColorBrush(Color.FromRgb(85, 255, 255)),
                'c' or 'C' => new SolidColorBrush(Color.FromRgb(255, 85, 85)),
                'd' or 'D' => new SolidColorBrush(Color.FromRgb(255, 85, 255)),
                'e' or 'E' => new SolidColorBrush(Color.FromRgb(255, 255, 85)),
                'f' or 'F' => Brushes.White,
                'r' or 'R' => Brushes.White, // Reset
                _ => currentColor // Keep current if style (l, m, n, o)
            };

            textBlock.Inlines.Add(new Run { Text = content, Foreground = currentColor });
        }
    }
}
