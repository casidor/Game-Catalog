using Avalonia.Data.Converters;
using Game_Catalog.Models;
using System;
using System.Globalization;

namespace Game_Catalog.Converters
{
    /// <summary>
    /// Converts a <see cref="GameStatus"/> value to a localized Ukrainian display string.
    /// Returns an empty string for null values, allowing nullable status filters to display a placeholder.
    /// </summary>
    public class GameStatusConverter : IValueConverter
    {
        /// <summary>Shared singleton instance of the converter.</summary>
        public static readonly GameStatusConverter Instance = new();

        /// <summary>Converts a GameStatus or nullable GameStatus to a Ukrainian string.</summary>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value switch
            {
                GameStatus.Planned => "Планую",
                GameStatus.Playing => "Граю",
                GameStatus.Completed => "Пройдено",
                GameStatus.Abandoned => "Закинуто",
                _ => null
            };
        }

        /// <summary>Not supported. Always throws <see cref="NotSupportedException"/>.</summary>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}