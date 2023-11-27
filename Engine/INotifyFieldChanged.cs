using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Engine
{
    public interface INotifyFieldChanged
    {
        public abstract void OnFieldChanged(FieldInfo fieldInfo);
    }
}