using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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