using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace Game_Catalog.Converters
{
    /// <summary> Converts a nullable TimeSpan to a short Ukrainian string ("1 г 30 хв"). </summary>
    public class DurationConverter : IValueConverter
    {
        /// <summary>Shared singleton instance of the converter.</summary>
        public static readonly DurationConverter Instance = new();

        /// <summary> Converts a TimeSpan or a number of hours to a short Ukrainian string ("1 г 30 хв"). </summary>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            TimeSpan? ts = value switch
            {
                TimeSpan t => t,
                double hours => TimeSpan.FromHours(hours),
                _ => null
            };
            if (ts is null) return "—";

            var totalMinutes = (int)Math.Round(ts.Value.TotalMinutes);
            return $"{totalMinutes / 60} г {totalMinutes % 60} хв";
        }

        /// <summary>Not supported. Always throws <see cref="NotSupportedException"/>.</summary>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}