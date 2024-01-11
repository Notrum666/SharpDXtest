using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Engine
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class DisplayOverridesAttribute : Attribute
    {
        public Type ConverterType { get; }
        public bool PropagateStructUpdate { get; }
        public DisplayOverridesAttribute(Type converterType = null, bool propagateStructUpdate = false)
        {
            if (converterType is not null && !converterType.IsSubclassOf(typeof(FieldConverter)))
                throw new ArgumentException("Subclass of FieldConverter type was expected", nameof(converterType));
            ConverterType = converterType;
            PropagateStructUpdate = propagateStructUpdate;
        }
    }
}
