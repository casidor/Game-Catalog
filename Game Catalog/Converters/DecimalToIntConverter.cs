using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace Game_Catalog.Converters
{
    /// <summary>
    /// Converts between decimal? (NumericUpDown) and int (ViewModel).
    /// Returns 0 when the field is cleared to prevent null binding exceptions.
    /// </summary>
    public class DecimalToIntConverter : IValueConverter
    {
        /// <summary>Shared singleton instance of the converter.</summary>
        public static readonly DecimalToIntConverter Instance = new();

        /// <summary>Converts int from ViewModel to decimal? for NumericUpDown display.</summary>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int i) return (decimal?)i;
            return null;
        }

        /// <summary>Converts decimal? from NumericUpDown to int for ViewModel. Returns 0 for null.</summary>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is decimal d) return (int)d;
            return 0;
        }
    }
}