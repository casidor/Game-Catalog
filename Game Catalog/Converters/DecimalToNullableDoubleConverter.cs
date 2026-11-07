using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace Game_Catalog.Converters
{
    /// <summary> Converts between decimal? (NumericUpDown) and double? (ViewModel), preserving null. </summary>
    public class DecimalToNullableDoubleConverter : IValueConverter
    {
        /// <summary>Shared singleton instance of the converter.</summary>
        public static readonly DecimalToNullableDoubleConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is double d ? (decimal?)d : null;

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is decimal d ? (double?)d : null;
    }
}