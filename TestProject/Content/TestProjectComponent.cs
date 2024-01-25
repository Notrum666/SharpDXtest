using Engine;
using Engine.BaseAssets.Components;

using LinearAlgebra;

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
    public class TestProjectComponent : BehaviourComponent
    {
        [SerializedField]
        private GameObject obj = null;
        public override void Start()
        {

        }
        public override void Update()
        {

        }
    }
}