using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Device = SharpDX.Direct3D11.Device;

namespace SharpDXtest
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            GraphicsCore.Init(WinFormsControl);
        }

        private void WinFormsControl_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            GraphicsCore.Update();
        }

        private void MainWindowInst_Closed(object sender, EventArgs e)
        {
            GraphicsCore.Dispose();
        }
    }
}
