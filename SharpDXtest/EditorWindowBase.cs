using System.Windows;

namespace Editor
{
    public class EditorWindowBase : Window
    {
        public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(nameof(Header), typeof(object), typeof(EditorWindowBase));
        public object Header
        {
            get => GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        public static readonly DependencyProperty FooterProperty = DependencyProperty.Register(nameof(Footer), typeof(object), typeof(EditorWindowBase));
        public object Footer
        {
            get => GetValue(FooterProperty);
            set => SetValue(FooterProperty, value);
        }

        static EditorWindowBase()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(EditorWindowBase), new FrameworkPropertyMetadata(typeof(EditorWindowBase)));
        }
    }
}