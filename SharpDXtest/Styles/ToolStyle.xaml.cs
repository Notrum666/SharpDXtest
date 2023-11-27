using System.Windows;
using System.Windows.Input;

namespace Editor
{
    partial class ToolStyle
    {
        private void ToolControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            ((FrameworkElement)sender).Focus();
        }
    }
}