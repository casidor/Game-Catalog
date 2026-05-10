using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using System;
using System.Globalization;
using System.IO;

namespace Game_Catalog.Converters
{
    /// <summary> Converts a file path string to an Avalonia Bitmap for image binding. </summary>
    public class PathToBitmapConverter : IValueConverter
    {
        public static readonly PathToBitmapConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string path && File.Exists(path))
                return new Bitmap(path);
            return null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}