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
    public class StringValueConverter : IValueConverter
    {
        static readonly Regex DoubleRegex = new Regex("^[0-9]*(\\.[0-9]+)?", RegexOptions.Compiled);

        static readonly Regex IntRegex = new Regex("^[0-9]+", RegexOptions.Compiled);

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType == typeof(int))
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
            else if (targetType == typeof(double))
            {
                var numberText = DoubleRegex.Match(value as string ?? "");
                if (numberText.Success)
                {
                    return double.Parse(numberText.Value);
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                return null;
            }
        }
    }
}
