using System.Windows;

namespace Editor
{
    public class CustomWindowBase : Window
    {
        public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(nameof(Header), typeof(object), typeof(CustomWindowBase));
        public object Header
        {
            get => GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        static CustomWindowBase()
        {
            //DefaultStyleKeyProperty.OverrideMetadata(typeof(CustomWindowBase), new FrameworkPropertyMetadata(typeof(CustomWindowBase)));
        }
    }
}