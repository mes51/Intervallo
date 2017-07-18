using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Intervallo.Converter
{
    public class PenConverter : TypeConverter
    {
        static readonly BrushConverter BrushConverter = new BrushConverter();

        static bool Registered { get; set; }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string)
            {
                var brush = BrushConverter.ConvertFromInvariantString((string)value) as Brush;
                if (brush != null)
                {
                    var pen = new Pen(brush, 1);
                    pen.Freeze();
                    return pen;
                }
            }
            return base.ConvertFrom(context, culture, value);
        }

        public static void Register()
        {
            if (!Registered)
            {
                TypeDescriptor.AddAttributes(typeof(Pen), new Attribute[] { new TypeConverterAttribute(typeof(PenConverter)) });
                Registered = true;
            }
        }
    }
}
