using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace Game_Catalog.Converters
{
    /// <summary> Converts the search-match flag to a row highlight brush. </summary>
    public class SearchMatchBrushConverter : IValueConverter
    {
        /// <summary>Shared singleton instance of the converter.</summary>
        public static readonly SearchMatchBrushConverter Instance = new();

        private static readonly IBrush Highlight = new SolidColorBrush(Color.Parse("#40E0B040"));

        /// <summary>Returns the highlight brush for matching rows and transparent otherwise.</summary>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is true ? Highlight : Brushes.Transparent;

        /// <summary>Not supported. Always throws <see cref="NotSupportedException"/>.</summary>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}