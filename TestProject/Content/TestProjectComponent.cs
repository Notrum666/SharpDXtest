using Engine;
using Engine.BaseAssets.Components;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace TestProject
{
    public class TestProjectComponent : BehaviourComponent
    {
        [SerializedField]
        private Sound SoundToPlay = null;
        public override void Start()
        {
            SoundCore.Play(SoundToPlay);

            GraphicsCore.ViewportPanel.Dispatcher.Invoke(() =>
            {
                Logger.Log(LogType.Info, "Show userControl");
                GraphicsCore.ViewportPanel.Children.Add(new UserControlTest());
            });
        }
    }
}