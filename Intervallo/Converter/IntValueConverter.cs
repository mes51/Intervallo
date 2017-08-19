using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Intervallo.Converter
{
    public class IntValueConverter : IValueConverter
    {
        static readonly Regex IntRegex = new Regex("^[0-9]+", RegexOptions.Compiled);

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType == typeof(int))
            {
                return value;
            }
            else if (targetType == typeof(double))
            {
                return (double)(int)value;
            }
            else if (targetType == typeof(string))
            {
                return value.ToString();
            }
            else
            {
                throw new InvalidCastException();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int)
            {
                return value;
            }
            else if (value is double)
            {
                return (int)(double)value;
            }
            else if (value is string)
            {
                var numberText = IntRegex.Match(value as string ?? "");
                if (numberText.Success)
                {
                    return int.Parse(numberText.Value);
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                throw new InvalidCastException();
            }
        }
    }
}
