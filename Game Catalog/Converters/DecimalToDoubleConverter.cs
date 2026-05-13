using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace Game_Catalog.Converters
{
    /// <summary>
    /// Converts between decimal? (NumericUpDown) and double (ViewModel).
    /// Returns 0.0 when the field is cleared to prevent null binding exceptions.
    /// </summary>
    public class DecimalToDoubleConverter : IValueConverter
    {
        /// <summary>Shared singleton instance of the converter.</summary>
        public static readonly DecimalToDoubleConverter Instance = new();

        /// <summary>Converts double from ViewModel to decimal? for NumericUpDown display.</summary>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double d) return (decimal?)d;
            return null;
        }

        /// <summary>Converts decimal? from NumericUpDown to double for ViewModel. Returns 0.0 for null.</summary>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is decimal d) return (double)d;
            return 0.0;
        }
    }
}