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
        public override void Start()
        {
            try
            {
                GraphicsCore.GameInterface.Dispatcher.Invoke(() =>
                {
                    var control = new UserControlTest();
                    Grid grid = (Grid)control.Content;
                    TextBlock textBlock = (TextBlock)grid.Children[0];
                    textBlock.Text = "SOSI_PISOS";
                    GraphicsCore.GameInterface.Children.Add( control );
                    Debug.WriteLine($"control = {control}");
                });
                
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                throw;
            }
        }
    }
}