using System;
using System.Windows;

namespace SharpDXtest
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static bool IsActive;

        void App_Activated(object sender, EventArgs e)
        {
            // Application activated
            IsActive = true;
        }

        void App_Deactivated(object sender, EventArgs e)
        {
            // Application deactivated
            IsActive = false;
        }
    }
}