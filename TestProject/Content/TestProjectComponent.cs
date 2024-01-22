using Engine;
using Engine.BaseAssets.Components;
using System;
using System.Collections;
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

            Coroutine.Start(SomeMethod);
        }
        private IEnumerator SomeMethod()
        {
            for (int i = 0; i < 10; i++)
            {
                Logger.Log(LogType.Info, $"Log {i}");
                yield return new WaitForSeconds(1.0);
            }
        }
    }
}