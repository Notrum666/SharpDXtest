using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace Editor
{
    partial class DefaultInspectorStyles
    {
        private void InspectorTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Escape)
                ((DependencyObject)sender).FindParent<InspectorControl>().Focus();
        }

        private void InspectorTextBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;

            string text = textBox.Text;

            Binding binding = textBox.GetBindingExpression(TextBox.TextProperty).ParentBinding;
            Binding newBinding = new Binding();
            newBinding.Path = binding.Path;
            newBinding.Converter = binding.Converter;
            newBinding.Mode = BindingMode.OneWayToSource;
            textBox.SetBinding(TextBox.TextProperty, newBinding);

            textBox.Text = text;

            textBox.SelectAll();
        }

        private void InspectorTextBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            BindingExpression expr = textBox.GetBindingExpression(TextBox.TextProperty);
            expr.UpdateSource();
            Binding binding = expr.ParentBinding;
            Binding newBinding = new Binding();
            newBinding.Path = binding.Path;
            newBinding.Converter = binding.Converter;
            newBinding.Mode = BindingMode.OneWay;
            textBox.SetBinding(TextBox.TextProperty, newBinding);
        }

        private void InspectorTextBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            if (!textBox.IsKeyboardFocusWithin)
            {
                textBox.Focus();
                e.Handled = true;
            }
        }
    }
}