using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;
using System.ComponentModel.DataAnnotations;

namespace Editor
{
    [ValueConversion(typeof(int), typeof(bool))]
    public sealed class IntToBoolConverter : IValueConverter
    {
        public enum ComparisonMode
        {
            Equal,
            NotEqual,
            GreaterThan,
            GreaterThanOrEqual,
            LessThan,
            LessThanOrEqual
        }
        private static readonly Dictionary<ComparisonMode, Func<int, int, bool>> Comparators = new Dictionary<ComparisonMode, Func<int, int, bool>>()
        {
            [ComparisonMode.Equal] = (int a, int b) => a == b,
            [ComparisonMode.NotEqual] = (int a, int b) => a != b,
            [ComparisonMode.GreaterThan] = (int a, int b) => a > b,
            [ComparisonMode.GreaterThanOrEqual] = (int a, int b) => a >= b,
            [ComparisonMode.LessThan] = (int a, int b) => a < b,
            [ComparisonMode.LessThanOrEqual] = (int a, int b) => a <= b,
        };
        public int TargetValue { get; set; }
        public ComparisonMode Mode { get; set; }

        public IntToBoolConverter()
        {
            TargetValue = 0;
            Mode = ComparisonMode.GreaterThan;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not int)
                return DependencyProperty.UnsetValue;

            return Comparators[Mode]((int)value, TargetValue);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}
