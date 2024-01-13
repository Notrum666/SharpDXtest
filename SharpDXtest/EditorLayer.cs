using System;
using System.IO;

using Editor.AssetsImport;

using Engine;
using Engine.Layers;

using SharpDXtest;

namespace Editor
{
    internal class EditorLayer : Layer
    {
        private const bool RecompileOnFocus = true; // TODO: move to settings?

        private static EditorLayer current = null;
        public static EditorLayer Current => current ??= new EditorLayer();

        public override float InitOrder => 1000;
        public override float UpdateOrder => 1000;

        public const string EditorPathVarName = "EnvVar_SharpDxEditor";
        private const string DataFolderName = "SharpDxEditor";
        private const string ResourcesFolderName = "Resources";

        public string DataFolderPath { get; private set; }
        public string EditorFolderPath { get; private set; }
        public string ResourcesFolderPath { get; private set; }

        private EditorLayer()
        {
            DataFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), DataFolderName);
            EditorFolderPath = AppDomain.CurrentDomain.BaseDirectory;
            // setting variable is surprisingly slow (up to a few seconds), but reading it is fast
            string? curValue = Environment.GetEnvironmentVariable(EditorPathVarName);
            if (curValue is null || curValue != EditorFolderPath)
                Environment.SetEnvironmentVariable(EditorPathVarName, EditorFolderPath, EnvironmentVariableTarget.User);

            ResourcesFolderPath = Path.Combine(EditorFolderPath, ResourcesFolderName);

            ProjectsManager.InitializeInFolder(DataFolderPath);
        }

        public override void Init()
        {
            ProjectViewModel.Current.ApplyProjectSettings();

            AssetsRegistry.InitializeInFolder(ProjectViewModel.Current.FolderPath);

            ProjectViewModel.Current.UpdateGameScenes();

            ScriptManager.OnCodeRecompiled += () => SceneManager.LoadSceneByName(Scene.CurrentScene?.Name ?? Game.StartingSceneName);
        }

        public override void Update()
        {
            if (ProjectViewModel.Current != null && RecompileOnFocus) //TODO: Use event subscription instead of Update
            {
                if (App.IsActive && !ScriptManager.IsCompilationRelevant)
                    ScriptManager.Recompile();
            }
        }
    }
}