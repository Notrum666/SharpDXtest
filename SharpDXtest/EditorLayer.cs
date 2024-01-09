using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Editor.AssetsImport;
using Editor.GameProject;

using Engine.Layers;
using SharpDXtest;

namespace Editor
{
    internal class EditorLayer : Layer
    {
        private const bool RecompileOnFocus = true; // TODO: move to settings?

        private static EditorLayer current = null;
        public static EditorLayer Current => current ??= new EditorLayer();

        public override float InitOrder => 1;
        public override float UpdateOrder => 1;

        public override void Init()
        {
        }

        public override void Update()
        {
            if (ProjectViewModel.Current != null && RecompileOnFocus)
            {
                if (App.IsActive && !ScriptManager.IsCompilationRelevant)
                    ScriptManager.Recompile();
            }
        }
    }
}