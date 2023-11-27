using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Editor
{
    partial class DefaultInspectorStyles
    {
        private void InspectorTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Escape)
            {
                FrameworkElement root = (FrameworkElement)FocusManager.GetFocusScope((DependencyObject)sender);
                root.Focus();
                Keyboard.Focus(root);
            }
        }

        private void InspectorTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            ((TextBox)sender).GetBindingExpression(TextBox.TextProperty).UpdateTarget();
        }
    }
}