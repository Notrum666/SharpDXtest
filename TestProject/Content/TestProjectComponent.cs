using Engine;
using Engine.BaseAssets.Components;
using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace TestProject
{
    public class TestProjectComponent : BehaviourComponent, INotifyPropertyChanged
    {
        private string text = "";
        public string Text
        {
            get => text;
            set
            {
                text = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName]string property = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
        public override void Start()
        {
            GraphicsCore.ViewportPanel.Dispatcher.Invoke(() =>
            {
                UserControlTest userControlTest = new UserControlTest();
                GraphicsCore.ViewportPanel.Children.Add(userControlTest);
                userControlTest.DataContext = this;
            });
        }
        public override void Update()
        {
            Text = Time.DeltaTime.ToString();
        }
    }
}