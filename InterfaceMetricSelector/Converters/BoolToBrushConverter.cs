using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace InterfaceMetricSelector.Converters
{
    public sealed class BoolToBrushConverter : IValueConverter
    {
        public Brush? TrueBrush { get; set; }
        public Brush? FalseBrush { get; set; }

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is true) return TrueBrush ?? Brushes.White;
            return FalseBrush ?? Brushes.Gray;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
