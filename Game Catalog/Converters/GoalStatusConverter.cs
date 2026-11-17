using Avalonia.Data.Converters;
using Game_Catalog.Models;
using System;
using System.Globalization;

namespace Game_Catalog.Converters
{
    /// <summary> Converts a <see cref="GoalStatus"/> to a localized Ukrainian string. </summary>
    public class GoalStatusConverter : IValueConverter
    {
        /// <summary>Shared singleton instance of the converter.</summary>
        public static readonly GoalStatusConverter Instance = new();

        /// <summary>Converts the enum value to a display string.</summary>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value switch
            {
                GoalStatus.Active => "Активна",
                GoalStatus.Completed => "Виконана",
                GoalStatus.Cancelled => "Скасована",
                GoalStatus.Overdue => "Невиконана",
                _ => null
            };
        }

        /// <summary>Not supported. Always throws <see cref="NotSupportedException"/>.</summary>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}