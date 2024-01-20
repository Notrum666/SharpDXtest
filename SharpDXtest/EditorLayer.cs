using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

using Editor.AssetsImport;

using Engine;
using Engine.Layers;

using SharpDXtest;

namespace Editor
{
    internal class EditorLayer : Layer, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

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

        private DispatcherTimer WpfTimer;

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

            EngineCore.Run();

            SceneManager.ReloadScene();

            Current.WpfTimer = new DispatcherTimer();
            Current.WpfTimer.Interval = TimeSpan.FromSeconds(0.1);
            Current.WpfTimer.Tick += Current.WpfTick;
            current.WpfTimer.Start();
        }

        public override void Init()
        {
            AssetsRegistry.InitializeInFolder(ProjectViewModel.Current.FolderPath);

            ScriptManager.Recompile();
            AssetsRegistry.Refresh();

            ProjectViewModel.Current.ApplyProjectSettings();
            ProjectViewModel.Current.MonitorGameScenes();

            ScriptManager.OnCodeRecompiled += SceneManager.ReloadScene;

            Application.Current.Activated += Application_Activated;
        }

        private void Application_Activated(object sender, EventArgs e)
        {
            if (ProjectViewModel.Current != null && RecompileOnFocus && !ScriptManager.IsCompilationRelevant && !isPlaying)
                Task.Run(ScriptManager.Recompile);
        }

        #region EditorState

        public GameObject SelectedGameObject
        {
            get => InspectedObject as GameObject;
            set => InspectedObject = value;
        }
        private object inspectedObject;
        public object InspectedObject
        {
            get => inspectedObject;
            set
            {
                inspectedObject = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedGameObject));
            }
        }

        #endregion

        #region Playmode

        public event Action OnPlaymodeEntered;
        public event Action OnPlaymodeExited;

        private bool isPlaying = false;
        /// <summary> True if in Play mode, false if in Edit mode </summary>
        public bool IsPlaying
        {
            get => isPlaying;
            private set
            {
                if (isPlaying == value)
                    return;

                desiredIsPlaying = value;
            }
        }
        private bool desiredIsPlaying = false;
        private bool stepInProcess = false;

        public bool IsEnginePaused
        {
            get => EngineCore.IsPaused;
            set => EngineCore.IsPaused = value;
        }

        // can't make it internal cause then wpf binding stops working :c
        // TODO: move to converter
        public bool PauseButtonVisible => !IsPlaying || !IsEnginePaused;

        /// <summary>
        /// Starts EngineCore playing
        /// </summary>
        public void EnterPlaymode()
        {
            if (ScriptManager.IsRecompiling)
                return;
            IsPlaying = true;
        }

        /// <summary>
        /// Stops EngineCore playing and resets the scene
        /// </summary>
        public void ExitPlaymode()
        {
            if (ScriptManager.IsRecompiling)
                return;
            IsPlaying = false;
        }

        public void ProcessStep()
        {
            EngineCore.IsPaused = true;
            if (IsPlaying)
                stepInProcess = true;
        }

        public override void OnFrameEnded()
        {
            if (desiredIsPlaying != isPlaying)
            {
                isPlaying = desiredIsPlaying;
                stepInProcess = false;
                InspectedObject = null;

                if (isPlaying)
                {
                    ProjectViewModel.Current.SaveCurrentScene();
                    OnPlaymodeEntered?.Invoke();
                    EngineCore.IsPaused = false;
                }
                else
                {
                    SceneManager.ReloadScene();
                    OnPlaymodeExited?.Invoke();
                    EngineCore.IsPaused = true;
                }

                return;
            }
            
            if (isPlaying && stepInProcess)
            {
                stepInProcess = false;
                EngineCore.IsPaused = true;
            }
        }

        private void WpfTick(object sender, EventArgs e)
        {
            OnPropertyChanged(nameof(IsPlaying));
            OnPropertyChanged(nameof(IsEnginePaused));
            OnPropertyChanged(nameof(PauseButtonVisible));
        }

        #endregion Playmode

    }
}