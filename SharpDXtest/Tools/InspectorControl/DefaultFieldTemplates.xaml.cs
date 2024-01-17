using System;
using System.Windows;

using Engine;

namespace Editor
{
    public partial class DefaultFieldTemplates
    {
        public bool ReferenceFieldPredicate(Type type)
        {
            return type.IsClass;
        }

        private bool isStruct(Type type)
        {
            return !type.IsEnum && !type.IsPrimitive;
        }

        public bool NonNullableStructFieldPredicate(Type type)
        {
            return isStruct(type) && Nullable.GetUnderlyingType(type) is null;
        }

        public bool NullableStructFieldPredicate(Type type)
        {
            return isStruct(type) && Nullable.GetUnderlyingType(type) is not null;
        }
    }
}