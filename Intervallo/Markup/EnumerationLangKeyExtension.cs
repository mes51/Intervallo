using Intervallo.InternalUtil;
using Intervallo.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace Intervallo.Markup
{
    public class EnumerationLangKeyExtension : MarkupExtension
    {
        public static Tuple<Enum, string> CreateTuple(object enumValue)
        {
            var type = enumValue.GetType();
            if (!type.IsEnum)
            {
                throw new ArgumentException("value must be enum");
            }
            var a = Optional<LangKeyAttribute>.FromNull(type.GetField(enumValue.ToString()).GetCustomAttributes(typeof(LangKeyAttribute), false).Cast<LangKeyAttribute>().FirstOrDefault());
            return Tuple.Create(enumValue as Enum, a.Fold(() => enumValue.ToString(), at => at.GetResourceValue()));
        }

        private Type enumType;

        public EnumerationLangKeyExtension(Type type)
        {
            if (type == null)
            {
                throw new ArgumentException(nameof(type));
            }

            EnumType = type;
        }

        public Type EnumType
        {
            get { return enumType; }
            set
            {
                if (value != null)
                {
                    var type = Nullable.GetUnderlyingType(value) ?? value;
                    if (!type.IsEnum)
                    {
                        throw new ArgumentException("type must be enum");
                    }
                    else
                    {
                        enumType = type;
                    }
                }
            }
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return Enum.GetValues(EnumType).Cast<Enum>().Select(CreateTuple);
        }

        [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
        public class LangKeyAttribute : Attribute
        {
            public LangKeyAttribute(string key)
            {
                Key = key;
            }

            public string Key { get; }

            public string GetResourceValue()
            {
                return typeof(LangResources).GetProperty(Key).GetValue(null) as string;
            }
        }
    }
}
