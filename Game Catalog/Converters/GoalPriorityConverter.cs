using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace Game_Catalog.Converters
{
    /// <summary> Converts a goal priority (1 is the highest, 5 the lowest) to a number with a Ukrainian caption. </summary>
    public class GoalPriorityConverter : IValueConverter
    {
        /// <summary>Shared singleton instance of the converter.</summary>
        public static readonly GoalPriorityConverter Instance = new();

        /// <summary>Converts the priority number to a display string.</summary>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value switch
            {
                1 => "1 — найвищий",
                2 => "2 — високий",
                3 => "3 — середній",
                4 => "4 — низький",
                5 => "5 — найнижчий",
                _ => null
            };
        }

        /// <summary>Not supported. Always throws <see cref="NotSupportedException"/>.</summary>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}