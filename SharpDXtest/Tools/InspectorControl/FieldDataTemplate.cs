using System;
using System.Windows;

namespace Editor
{
    public class FieldDataTemplate : DataTemplate
    {
        public Predicate<Type> Predicate { get; set; } = (t) => true;
        public Type TargetType { get; set; } = typeof(object);
    }
}