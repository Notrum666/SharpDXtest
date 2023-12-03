using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

using Editor.Tools.SceneOverviewControl;

using Engine;

namespace Editor
{
    /// <summary>
    /// Interaction logic for OutputControl.xaml
    /// </summary>
    public partial class SceneOverviewControl : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public SceneViewModel SceneViewModel { get; }
        private bool loaded = false;
        public SceneOverviewControl()
        {
            InitializeComponent();

            SceneViewModel = new SceneViewModel();

            DataContext = this;
        }

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // to prevent errors during xaml designer loading in visual studio
            if (!EngineCore.IsAlive)
                return;

            if (!loaded)
            {
                Width = double.NaN;
                Height = double.NaN;
            }

            SceneViewModel.Scene = EngineCore.CurrentScene;

            loaded = true;
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            // to prevent errors during xaml designer loading in visual studio
            if (!EngineCore.IsAlive)
                return;

            SceneViewModel.Scene = null;
        }
    }
}