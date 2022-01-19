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
using System.Threading;
using System.Globalization;

using Device = SharpDX.Direct3D11.Device;
using Rectangle = System.Drawing.Rectangle;

namespace SharpDXtest
{
    public partial class MainWindow : Window
    {
        private bool isAlive;
        private Task renderLoopTask;

        public MainWindow()
        {
            InitializeComponent();

            System.Windows.Forms.Cursor.Hide();

            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-us");
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Order of initialization is important, same number means no difference
            GraphicsCore.Init(WinFormsControl); // 1
            SoundCore.Init(); // 1
            Time.Init(); // 1
            InputManager.Init(); // 2
            GameCore.Init(); // 2

            isAlive = true;

            renderLoopTask = Task.Run(() =>
            {
                while (isAlive)
                {
                    InputManager.Update();
                    Time.Update();
                    GameCore.Update();
                    SoundCore.Update();
                    GraphicsCore.Update();
                }
            });
        }

        private void MainWindowInst_Closed(object sender, EventArgs e)
        {
            isAlive = false;

            Task.WaitAll(renderLoopTask);
            GraphicsCore.Dispose();
        }
    }
}
