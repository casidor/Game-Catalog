using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Styling;
using Game_Catalog.Models;
using System;
using System.Globalization;

namespace Game_Catalog.Converters
{
    /// <summary>
    /// Brushes of a tag badge: converter parameter "Background" gives a translucent fill,
    /// anything else gives the text color. A tag without color gets neutral brushes.
    /// </summary>
    public class TagBadgeBrushConverter : IValueConverter
    {
        /// <summary>Shared singleton instance of the converter.</summary>
        public static readonly TagBadgeBrushConverter Instance = new();

        private const double BackgroundOpacity = 0.18;

        private static readonly IBrush NeutralBackground =
            new SolidColorBrush(Avalonia.Media.Color.Parse("#30808080"));

        /// <summary>Converts a palette key to the badge fill or text brush for the current theme.</summary>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var isBackground = parameter as string == "Background";

            if (value is not string key || key.Length == 0)
                return isBackground ? NeutralBackground : NeutralText();

            var color = TagPalette.Find(key) ?? TagPalette.Fallback;
            var isLight = Application.Current?.ActualThemeVariant == ThemeVariant.Light;
            var parsed = Avalonia.Media.Color.Parse(isLight ? color.LightHex : color.DarkHex);

            return isBackground
                ? new SolidColorBrush(parsed, BackgroundOpacity)
                : new SolidColorBrush(parsed);
        }

        /// <summary>Not supported. Always throws <see cref="NotSupportedException"/>.</summary>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();

        /// <summary>Text brush of the current theme, gray if the resource is missing.</summary>
        private static IBrush NeutralText()
        {
            var app = Application.Current;
            if (app != null &&
                app.TryGetResource("NavForeground", app.ActualThemeVariant, out var resource) &&
                resource is IBrush brush)
                return brush;

            return Brushes.Gray;
        }
    }
}