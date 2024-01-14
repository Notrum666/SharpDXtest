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

        internal static void LaunchEngine()
        {
            EngineCore.Init(Current, new EditorRuntimeLayer());

            EngineCore.IsPaused = true;
            isPlaying = false;

            EngineCore.Run();
        }

        public override void Init()
        {
            AssetsRegistry.InitializeInFolder(ProjectViewModel.Current.FolderPath);

            ProjectViewModel.Current.ApplyProjectSettings();
            ProjectViewModel.Current.UpdateGameScenes();
            SceneManager.ReloadScene();

            ScriptManager.OnCodeRecompiled += SceneManager.ReloadScene;
            ScriptManager.Recompile();

            EngineCore.OnPaused += OnEnginePaused;
        }

        public override void Update()
        {
            if (ProjectViewModel.Current != null && RecompileOnFocus) //TODO: Use event subscription instead of Update
            {
                if (App.IsActive && !ScriptManager.IsCompilationRelevant)
                    ScriptManager.Recompile();
            }
        }

        #region Playmode

        public static event Action OnPlaymodeEntered;
        public static event Action OnPlaymodeExited;

        /// <summary> True if in Play mode, false if in Edit mode </summary>
        public static bool IsPlaying
        {
            get => isPlaying;
            set
            {
                if (isPlaying == value)
                    return;

                desiredIsPlaying = value;
            }
        }

        private static bool isPlaying = false;
        private static bool desiredIsPlaying = false;
        private static bool stepInProcess = false;

        /// <summary>
        /// Starts EngineCore playing
        /// </summary>
        public static void EnterPlaymode()
        {
            IsPlaying = true;
        }

        /// <summary>
        /// Stops EngineCore playing and resets the scene
        /// </summary>
        public static void ExitPlaymode()
        {
            IsPlaying = false;
        }

        public static void ProcessStep()
        {
            if (IsPlaying && EngineCore.IsPaused)
                stepInProcess = true;
        }

        public override void OnFrameEnded()
        {
            if (desiredIsPlaying != IsPlaying)
            {
                stepInProcess = false;
                EngineCore.IsPaused = !desiredIsPlaying;

                if (desiredIsPlaying)
                {
                    isPlaying = true;
                    OnPlaymodeEntered?.Invoke();
                }
            }
            else if (IsPlaying && stepInProcess)
            {
                stepInProcess = EngineCore.IsPaused;
                EngineCore.IsPaused = !EngineCore.IsPaused;
            }
        }

        private static void OnEnginePaused()
        {
            if (desiredIsPlaying)
                return;

            isPlaying = false;
            SceneManager.ReloadScene();
            OnPlaymodeExited?.Invoke();
        }

        #endregion Playmode

    }
}