using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Editor
{
    public class FieldDataTemplate : DataTemplate
    {
        public Predicate<Type> Predicate { get; set; } = (t) => true;
        public Type TargetType { get; set; } = typeof(object);
    }
}
