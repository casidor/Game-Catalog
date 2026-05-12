using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using System;
using System.Globalization;
using System.IO;

namespace Game_Catalog.Converters
{
    /// <summary>
    /// Converts a file path string to an Avalonia Bitmap for image binding.
    /// Returns null if the file does not exist, is corrupted, or cannot be read,
    /// allowing the UI to fall back to a placeholder.
    /// </summary>
    public class PathToBitmapConverter : IValueConverter
    {
        /// <summary>Shared singleton instance of the converter.</summary>
        public static readonly PathToBitmapConverter Instance = new();

        /// <summary>
        /// Converts a file path to a Bitmap. Returns null on any failure.
        /// </summary>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not string path || !File.Exists(path))
                return null;

            try
            {
                return new Bitmap(path);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Not supported. Always throws <see cref="NotSupportedException"/>.</summary>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}