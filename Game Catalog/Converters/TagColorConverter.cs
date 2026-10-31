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
    /// Converts a tag color key to a brush matching the current theme.
    /// An empty key gives a transparent brush, an unknown key gives gray.
    /// </summary>
    public class TagColorConverter : IValueConverter
    {
        /// <summary>Shared singleton instance of the converter.</summary>
        public static readonly TagColorConverter Instance = new();

        /// <summary>Converts a palette key to a brush for the current theme.</summary>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not string key || key.Length == 0)
                return Brushes.Transparent;

            var color = TagPalette.Find(key) ?? TagPalette.Fallback;
            var isLight = Application.Current?.ActualThemeVariant == ThemeVariant.Light;
            return new SolidColorBrush(Avalonia.Media.Color.Parse(isLight ? color.LightHex : color.DarkHex));
        }

        /// <summary>Not supported. Always throws <see cref="NotSupportedException"/>.</summary>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}