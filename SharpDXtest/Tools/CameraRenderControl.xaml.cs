using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

using Engine;
using Engine.BaseAssets.Components;

using LinearAlgebra;

using SharpDXtest.Assets.Components;

namespace Editor
{
    public partial class CameraRenderControl : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<AspectRatio> AspectRatios { get; set; } = new ObservableCollection<AspectRatio>
        {
            new AspectRatio(),
            new AspectRatio(1, 1),
            new AspectRatio(4, 3),
            new AspectRatio(5, 4),
            new AspectRatio(3, 2),
            new AspectRatio(16, 10),
            new AspectRatio(16, 9),
            new AspectRatio(17, 9),
            new AspectRatio(21, 9),
            new AspectRatio(32, 9)
        };

        public AspectRatio SelectedAspectRatio { get; set; } = new AspectRatio();

        private bool loaded = false;
        private ViewportType ViewportType { get; }
        public CameraViewModel CameraViewModel { get; }

        private Camera editorCamera;
        private EditorCameraController EditorCameraController => editorCamera?.GameObject?.GetComponent<EditorCameraController>();
        private static int editorCamerasCount = 0;

        private DispatcherTimer updateTimer;

        public CameraRenderControl() : this(ViewportType.Both) { }

        public CameraRenderControl(ViewportType type)
        {
            InitializeComponent();

            ViewportType = type;
            CameraViewModel = new CameraViewModel();

            DataContext = this;
            
            updateTimer = new DispatcherTimer();
            updateTimer.Interval = TimeSpan.FromSeconds(0.1);
            updateTimer.Tick += UpdateTick;
        }

        ~CameraRenderControl()
        {
            editorCamera?.GameObject?.DestroyImmediate();
        }

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // to prevent errors during xaml designer loading in visual studio
            if (!EngineCore.IsAlive)
                return;

            EditorLayer.Current.OnPlaymodeEntered += OnPlaymodeEntered;
            EditorLayer.Current.OnPlaymodeExited += OnPlaymodeExited;

            if (!loaded)
            {
                Width = double.NaN;
                Height = double.NaN;

                if (ViewportType.HasFlag(ViewportType.EditorView))
                {
                    editorCamera = CreateEditorCamera();
                    CameraViewModel.ResizeCamera(editorCamera, (int)ActualWidth, (int)ActualHeight);
                    CameraViewModel.SetCamera(editorCamera);
                }

                loaded = true;
            }

            if (GraphicsCore.ViewportPanel.Parent is null)
                GameInterfaceHost.Children.Add(GraphicsCore.ViewportPanel);

            if (editorCamera != null)
                editorCamera.LocalEnabled = true;

            CompositionTarget.Rendering += OnRender;
        }

        private void OnPlaymodeEntered()
        {
            if (ViewportType.HasFlag(ViewportType.EditorView))
                EditorCameraController.LocalEnabled = false;

            if (ViewportType.HasFlag(ViewportType.GameView))
            {
                CameraViewModel.SetCamera(Camera.Current);
                ResizeCameraView();
                updateTimer.Start();
            }
        }

        private void OnPlaymodeExited()
        {
            if (ViewportType.HasFlag(ViewportType.EditorView))
            {
                EditorCameraController.LocalEnabled = true;
                updateTimer.Stop();
                CameraViewModel.SetCamera(editorCamera);
                ResizeCameraView();
            }
        }
        
        private void UpdateTick(object sender, EventArgs e)
        {
            if (ViewportType.HasFlag(ViewportType.GameView))
            {
                CameraViewModel.SetCamera(Camera.Current);
                ResizeCameraView();
            }
        }

        private static Camera CreateEditorCamera()
        {
            editorCamerasCount++;

            GameObject editorCamera = EditorScene.Instantiate($"Editor_camera_{editorCamerasCount}");
            editorCamera.Transform.Position = new Vector3(0, -10, 5);
            editorCamera.AddComponent<EditorCameraController>();

            Camera camera = editorCamera.AddComponent<Camera>();
            camera.Near = 0.001;
            camera.Far = 500;

            return camera;
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            CompositionTarget.Rendering -= OnRender;

            if (editorCamera != null)
                editorCamera.LocalEnabled = false;

            EditorLayer.Current.OnPlaymodeEntered -= OnPlaymodeEntered;
            EditorLayer.Current.OnPlaymodeExited -= OnPlaymodeExited;
            
            GameInterfaceHost.Children.Clear();
        }

        private void OnRender(object sender, EventArgs e)
        {
            if (!IsVisible)
                return;

            CameraViewModel?.Render(D3DImage);
        }

        private void UserControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            Focus();

            if (editorCamera != null && !EditorLayer.Current.IsPlaying)
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                    HandlePicking((int)e.GetPosition(RenderControl).X, (int)e.GetPosition(RenderControl).Y);
            }
        }

        private void UserControl_MouseUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void UserControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Keyboard.ClearFocus();

                e.Handled = true;
            }
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateRenderControlSize();
        }

        private void AspectRatioComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateRenderControlSize();
        }

        private void UpdateRenderControlSize()
        {
            if (double.IsNaN(SelectedAspectRatio.Width) || double.IsNaN(SelectedAspectRatio.Height))
            {
                RenderControl.Width = double.NaN;
                RenderControl.Height = double.NaN;
                return;
            }

            if (RenderControlHost.ActualWidth > RenderControlHost.ActualHeight * SelectedAspectRatio.Ratio)
            {
                RenderControl.Height = RenderControlHost.ActualHeight;
                RenderControl.Width = RenderControl.Height * SelectedAspectRatio.Ratio;
            }
            else
            {
                RenderControl.Width = RenderControlHost.ActualWidth;
                RenderControl.Height = RenderControl.Width / SelectedAspectRatio.Ratio;
            }
        }

        private void RenderControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ResizeCameraView();
        }

        private void ResizeCameraView()
        {
            CameraViewModel.ResizeCamera((int)RenderControl.ActualWidth, (int)RenderControl.ActualHeight);
        }

        private void HandlePicking(int mouseX, int mouseY)
        {
            HitResult hitResult;
            Vector3 screenToWorldDir = editorCamera.ScreenToWorld(new Vector2(mouseX, mouseY));

            bool hasHit = Raycast.HitMesh(
                new Ray
                {
                    Origin = editorCamera.GameObject.Transform.Position,
                    Direction = screenToWorldDir
                },
                out hitResult
            );

            EditorLayer.Current.SelectedGameObject = hasHit ? hitResult.HitObject : null;
        }
    }

    public readonly struct AspectRatio
    {
        public readonly double Width;
        public readonly double Height;
        public double Ratio => Width / Height;
        public readonly string DisplayText;

        public AspectRatio() :
            this(double.NaN, double.NaN) { }

        // public AspectRatio(double width, double height) :
        //     this(width, height, "") { }

        public AspectRatio(double width, double height, string displayText = "")
        {
            Width = width;
            Height = height;
            DisplayText = displayText;
        }

        public override string ToString()
        {
            if (DisplayText != "")
                return DisplayText;
            if (double.IsNaN(Width) || double.IsNaN(Height))
                return "Free aspect";
            return $"{Width} : {Height}";
        }
    }

    public enum CursorMode
    {
        Normal,
        Hidden,
        HiddenAndLocked
    }

    [Flags]
    public enum ViewportType
    {
        GameView = 1 << 0,
        EditorView = 1 << 1,
        Both = GameView | EditorView
    }
}