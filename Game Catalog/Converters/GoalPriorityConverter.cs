using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace Game_Catalog.Converters
{
    /// <summary>
    /// Converts a goal priority (1 is the highest, 5 the lowest) to its Ukrainian name.
    /// The number stays in the database and orders the goals; the app shows only the name.
    /// </summary>
    public class GoalPriorityConverter : IValueConverter
    {
        /// <summary>Shared singleton instance of the converter.</summary>
        public static readonly GoalPriorityConverter Instance = new();

        /// <summary>Converts the priority number to its name.</summary>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value switch
            {
                1 => "Найвищий",
                2 => "Високий",
                3 => "Середній",
                4 => "Низький",
                5 => "Найнижчий",
                _ => null
            };
        }

        /// <summary>Not supported. Always throws <see cref="NotSupportedException"/>.</summary>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}