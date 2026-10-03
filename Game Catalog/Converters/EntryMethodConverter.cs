using Avalonia.Data.Converters;
using Game_Catalog.Models;
using System;
using System.Globalization;

namespace Game_Catalog.Converters
{
    /// <summary> Converts an <see cref="EntryMethod"/> to a localized Ukrainian string. </summary>
    public class EntryMethodConverter : IValueConverter
    {
        /// <summary>Shared singleton instance of the converter.</summary>
        public static readonly EntryMethodConverter Instance = new();

        /// <summary>Converts the enum value to a display string.</summary>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value switch
            {
                EntryMethod.Manual => "Вручну",
                EntryMethod.Auto => "Авто",
                _ => null
            };
        }

        /// <summary>Not supported. Always throws <see cref="NotSupportedException"/>.</summary>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}