using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace Game_Catalog.Converters
{
    /// <summary> Returns true when the bound value equals the converter parameter. Used for radio menu items. </summary>
    public class EqualsParameterConverter : IValueConverter
    {
        /// <summary>Shared singleton instance of the converter.</summary>
        public static readonly EqualsParameterConverter Instance = new();

        /// <summary>Compares the value with the parameter.</summary>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => Equals(value, parameter);

        /// <summary>Not supported. Always throws <see cref="NotSupportedException"/>.</summary>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}