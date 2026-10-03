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

        /// <summary>Returns a dash for null (ongoing session).</summary>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not TimeSpan ts) return "—";
            return $"{(int)ts.TotalHours} г {ts.Minutes} хв";
        }

        /// <summary>Not supported. Always throws <see cref="NotSupportedException"/>.</summary>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}