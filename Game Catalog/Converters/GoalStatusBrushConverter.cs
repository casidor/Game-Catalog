using Avalonia.Data.Converters;
using Avalonia.Media;
using Game_Catalog.Models;
using System;
using System.Globalization;

namespace Game_Catalog.Converters
{
    /// <summary>
    /// Converts a <see cref="GoalStatus"/> to the brush of its badge:
    /// indigo for active, red for overdue, green for completed, gray for cancelled.
    /// </summary>
    public class GoalStatusBrushConverter : IValueConverter
    {
        /// <summary>Translucent fill of the badge.</summary>
        public static readonly GoalStatusBrushConverter Background = new(0x38);

        /// <summary>Solid color of the badge text.</summary>
        public static readonly GoalStatusBrushConverter Foreground = new(0xFF);

        private readonly IBrush _active, _overdue, _completed, _cancelled;

        private GoalStatusBrushConverter(byte alpha)
        {
            _active = new SolidColorBrush(Color.FromArgb(alpha, 0x81, 0x8C, 0xF8));
            _overdue = new SolidColorBrush(Color.FromArgb(alpha, 0xEF, 0x53, 0x50));
            _completed = new SolidColorBrush(Color.FromArgb(alpha, 0x3F, 0xB9, 0x6B));
            _cancelled = new SolidColorBrush(Color.FromArgb(alpha, 0x9A, 0x9A, 0xA8));
        }

        /// <summary>Returns the brush of the given status.</summary>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value switch
            {
                GoalStatus.Active => _active,
                GoalStatus.Overdue => _overdue,
                GoalStatus.Completed => _completed,
                GoalStatus.Cancelled => _cancelled,
                _ => Brushes.Transparent
            };
        }

        /// <summary>Not supported. Always throws <see cref="NotSupportedException"/>.</summary>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}