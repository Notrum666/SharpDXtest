using System;
using System.ComponentModel;
using System.Globalization;

namespace Engine.Serialization
{
    public class SerializedObjectPromiseConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(SerializedObjectPromise);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            return value is SerializedObjectPromise
                ? value
                : base.ConvertFrom(context, culture, value);
        }
    }
}