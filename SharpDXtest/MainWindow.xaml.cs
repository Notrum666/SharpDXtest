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
using System.ComponentModel;

namespace SharpDXtest
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public bool IsPaused { get => GameCore.IsPaused; }
        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindow()
        {
            InitializeComponent();

            System.Windows.Forms.Cursor.Hide();

            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-us");

            DataContext = this;

            GameCore.OnPaused += () => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsPaused"));
            GameCore.OnResumed += () => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsPaused"));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            GameCore.Init(WinFormsControl);

            GameCore.Run();

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsPaused"));
        }

        private void MainWindowInst_Closed(object sender, EventArgs e)
        {
            if (GameCore.IsAlive)
                GameCore.Stop();
            
            GraphicsCore.Dispose();
        }

        private void MainWindowInst_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                GameCore.IsPaused = !GameCore.IsPaused;
                if (GameCore.IsPaused)
                {
                    System.Windows.Forms.Cursor.Position = new System.Drawing.Point(WinFormsControl.ClientSize.Width / 2, WinFormsControl.ClientSize.Height / 2);
                    System.Windows.Forms.Cursor.Show();
                }
                else
                    System.Windows.Forms.Cursor.Hide();
            }
        }

        private void PauseMenuButton_Exit_Click(object sender, RoutedEventArgs e)
        {
            GameCore.Stop();
            Close();
        }

        private void PauseMenuButton_Resume_Click(object sender, RoutedEventArgs e)
        {
            GameCore.IsPaused = false;
            System.Windows.Forms.Cursor.Hide();
        }
    }
}
