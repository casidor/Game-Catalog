using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace Game_Catalog.Converters
{
    /// <summary> Converts between decimal? (NumericUpDown) and int? (ViewModel), preserving null. </summary>
    public class DecimalToNullableIntConverter : IValueConverter
    {
        /// <summary>Shared singleton instance of the converter.</summary>
        public static readonly DecimalToNullableIntConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is int i ? (decimal?)i : null;

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is decimal d ? (int?)(int)d : null;
    }
}