using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
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

            ProjectViewModel.Current.ApplyProjectSettings();
            ProjectViewModel.Current.UpdateGameScenes();

            ScriptManager.Recompile();
            ScriptManager.OnCodeRecompiled += SceneManager.ReloadScene;

        }

        public override void Update()
        {
            if (ProjectViewModel.Current != null && RecompileOnFocus) //TODO: Use event subscription instead of Update
            {
                if (App.IsActive && !ScriptManager.IsCompilationRelevant)
                    ScriptManager.Recompile();
            }
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
            set
            {
                if (isPlaying == value)
                    return;

                desiredIsPlaying = value;
            }
        }
        private bool desiredIsPlaying = false;
        private bool stepInProcess = false;
        private bool notifyPlaymodeEntered = false;

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
            IsPlaying = true;
        }

        /// <summary>
        /// Stops EngineCore playing and resets the scene
        /// </summary>
        public void ExitPlaymode()
        {
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
            // to delay event firing until next frame because CameraRenderControl can get null from Camera.Current
            // TODO: rework CameraRenderControl camera selection to go off of actually Camera.Current being changed
            if (notifyPlaymodeEntered)
            {
                OnPlaymodeEntered?.Invoke();
                notifyPlaymodeEntered = false;
            }

            if (desiredIsPlaying != isPlaying)
            {
                isPlaying = desiredIsPlaying;
                stepInProcess = false;

                if (isPlaying)
                {
                    EngineCore.IsPaused = false;
                    notifyPlaymodeEntered = true;
                }
                else
                {
                    EngineCore.IsPaused = true;
                    OnPlaymodeExited?.Invoke();
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