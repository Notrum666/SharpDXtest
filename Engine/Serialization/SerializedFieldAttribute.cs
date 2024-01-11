using System;

namespace Engine
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class SerializedFieldAttribute : Attribute
    {
        public string NameOverride { get; private set; }

        public SerializedFieldAttribute()
            : this(null) { }

        public SerializedFieldAttribute(string nameOverride = null)
        {
            NameOverride = string.IsNullOrEmpty(nameOverride) ? null : nameOverride;
        }
    }
}