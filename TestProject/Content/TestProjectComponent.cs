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
            Logger.Log(LogType.Warning, "Test warning");
            throw new Exception("Test exception");
        }
    }
}