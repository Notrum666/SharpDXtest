using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Assets
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class GuidExpectedTypeAttribute : Attribute
    {
        public readonly Type ExpectedType;
        public GuidExpectedTypeAttribute(Type expectedType)
        {
            ExpectedType = expectedType;
        }
    }
}
