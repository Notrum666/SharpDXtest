using System;

namespace Engine
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class SerializeFieldAttribute : Attribute
    {
        public string? DisplayName { get; private set; }

        public SerializeFieldAttribute()
            : this(null)
        {
        }

        public SerializeFieldAttribute(string? displayName)
        {
            DisplayName = displayName;
        }
    }
}