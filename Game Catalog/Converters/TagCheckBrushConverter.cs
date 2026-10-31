using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Styling;
using System;
using System.Globalization;

namespace Game_Catalog.Converters
{
    /// <summary>
    /// Picks a readable check mark brush based on the current theme
    /// </summary>
    public class TagCheckBrushConverter : IValueConverter
    {
        /// <summary>Shared singleton instance of the converter.</summary>
        public static readonly TagCheckBrushConverter Instance = new();

        private static readonly IBrush DarkCheck = new SolidColorBrush(Avalonia.Media.Color.Parse("#1f2937"));
        private static readonly IBrush LightCheck = Brushes.White;

        /// <summary>Converts a palette key to a check mark brush.</summary>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var isDarkTheme = Application.Current?.ActualThemeVariant == ThemeVariant.Dark;

            if (value is not string key || key.Length == 0)
            {
                return isDarkTheme ? LightCheck : DarkCheck;
            }

            return isDarkTheme ? DarkCheck : LightCheck;
        }

        /// <summary>Not supported. Always throws <see cref="NotSupportedException"/>.</summary>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
