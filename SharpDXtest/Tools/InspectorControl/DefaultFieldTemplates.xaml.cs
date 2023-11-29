using System;

namespace Editor
{
    public partial class DefaultFieldTemplates
    {
        public bool ReferenceFieldPredicate(Type type)
        {
            return type.IsClass;
        }

        public bool StructFieldPredicate(Type type)
        {
            return !type.IsEnum && !type.IsPrimitive;
        }
    }
}