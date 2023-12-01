using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Interop;

namespace Editor
{
    partial class WindowStyle
    {
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            ((Window)sender).StateChanged += WindowStateChanged;
            WindowStateChanged((Window)sender, e);
        }

        private void WindowStateChanged(object sender, EventArgs e)
        {
            Window window = (Window)sender;
            nint handle = new WindowInteropHelper(window).Handle;
            Border containerBorder = (Border)window.Template.FindName("WindowContainer", window);

            if (window.WindowState == WindowState.Maximized)
            {
                // Make sure window doesn't overlap with the taskbar.
                Screen screen = Screen.FromHandle(handle);
                if (screen.Primary)
                {
                    containerBorder.Padding = new Thickness(
                        SystemParameters.WorkArea.Left + 7,
                        SystemParameters.WorkArea.Top + 7,
                        SystemParameters.PrimaryScreenWidth - SystemParameters.WorkArea.Right + 7,
                        SystemParameters.PrimaryScreenHeight - SystemParameters.WorkArea.Bottom + 7);
                }
            }
            else
                containerBorder.Padding = new Thickness(7);
        }

        private Window GetWindowFromTemplate(object templateFrameworkElement)
        {
            return ((FrameworkElement)templateFrameworkElement).TemplatedParent as Window;
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            GetWindowFromTemplate(sender).WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            SwitchState(GetWindowFromTemplate(sender));
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            GetWindowFromTemplate(sender).Close();
        }

        private void SwitchState(Window window)
        {
            window.WindowState = window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }
    }
}