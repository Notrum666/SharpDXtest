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
        public override void Start()
        {
            GameObject.GetComponent<Rigidbody>().Material = new PhysicalMaterial(0.0, 1.0, CombineMode.Maximum, CombineMode.Maximum);
        }
    }
}