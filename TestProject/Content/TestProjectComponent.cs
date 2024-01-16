using Engine;
using Engine.BaseAssets.Components;
using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Controls;

namespace TestProject
{
    public class TestProjectComponent : BehaviourComponent
    {
        public override void Start()
        {
            Uri uri = new Uri("/TestProject;component/content/usercontrol1.xaml", System.UriKind.Relative);
            try
            {
                Debug.WriteLine($"resourceAssembly = {System.Windows.Application.ResourceAssembly}");

                Thread thread = new Thread(() =>
                {
                    var control = new UserControl1();
                    Debug.WriteLine($"control = {control}");
                });
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join();

                var resource = System.Windows.Application.LoadComponent(uri);
                Debug.WriteLine($"resource = {resource}");
            } 
            catch (Exception ex) 
            {
                Debug.WriteLine(ex.Message);
            }
            //GraphicsCore.GameInterface.Dispatcher.Invoke(() => GraphicsCore.GameInterface.Children.Add(resource));
        }
    }
}