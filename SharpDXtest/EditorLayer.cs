using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using Editor.AssetsImport;
using Editor.GameProject;

using Engine.Layers;

namespace Editor
{
    internal class EditorLayer : Layer
    {
        private const bool RecompileOnFocus = false; // TODO: move to settings?

        private static EditorLayer current = null;
        public static EditorLayer Current => current ??= new EditorLayer();

        public override float InitOrder => 1;
        public override float UpdateOrder => 1;

        private Window mainWindow;

        public override void Init()
        {
            mainWindow = Application.Current.MainWindow;
        }

        public override void Update()
        {
            if (ProjectViewModel.Current != null && RecompileOnFocus)
            {
                if (mainWindow.IsFocused && !ScriptManager.IsCompilationRelevant)
                    ScriptManager.Recompile();
            }
        }
    }
}