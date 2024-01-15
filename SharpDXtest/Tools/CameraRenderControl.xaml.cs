using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using Engine;
using Engine.BaseAssets.Components;

using LinearAlgebra;

using SharpDXtest.Assets.Components;

using Point = System.Drawing.Point;

namespace Editor
{
    public partial class CameraRenderControl : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private Point cursorLockPoint;
        private CursorMode cursorMode;

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
        public CursorMode CursorMode
        {
            get => cursorMode;
            set
            {
                cursorMode = value;
                if (IsKeyboardFocused)
                    Cursor = value == CursorMode.Normal ? Cursors.Arrow : Cursors.None;
                if (value == CursorMode.HiddenAndLocked)
                    cursorLockPoint = System.Windows.Forms.Cursor.Position;

                OnPropertyChanged();
            }
        }

        private bool loaded = false;
        private ViewportType ViewportType { get; }
        public CameraViewModel CameraViewModel { get; }

        private Camera editorCamera;

        // private int framesCount = 0;
        // private double timeCounter = 0.0;
        // private bool keyboardFocused = false;

        public CameraRenderControl() : this(ViewportType.EditorView) { }

        public CameraRenderControl(ViewportType type)
        {
            InitializeComponent();

            CursorMode = CursorMode.Normal;

            ViewportType = type;
            CameraViewModel = new CameraViewModel();

            DataContext = this;
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

            if (!loaded)
            {
                Width = double.NaN;
                Height = double.NaN;

                EditorLayer.OnPlaymodeEntered += OnPlaymodeEntered;
                EditorLayer.OnPlaymodeExited += OnPlaymodeExited;

                if (ViewportType.HasFlag(ViewportType.EditorView))
                {
                    editorCamera = CreateEditorCamera();
                    CameraViewModel.ResizeCamera(editorCamera, (int)ActualWidth, (int)ActualHeight);
                    CameraViewModel.SetCamera(editorCamera);
                }

                loaded = true;
            }

            CompositionTarget.Rendering += OnRender;
        }

        private void OnPlaymodeEntered()
        {
            if (ViewportType.HasFlag(ViewportType.GameView))
            {
                // TODO: find non editor camera
            }
        }

        private void OnPlaymodeExited()
        {
            if (ViewportType.HasFlag(ViewportType.EditorView))
            {
                CameraViewModel.SetCamera(editorCamera);
            }
        }

        private static Camera CreateEditorCamera()
        {
            GameObject editorCamera = EditorScene.Instantiate("Editor camera");
            editorCamera.Transform.Position = new Vector3(0, -10, 5);
            editorCamera.AddComponent<EditorCameraController>();

            Camera camera = editorCamera.AddComponent<Camera>();
            camera.Near = 0.001;
            camera.Far = 500;
            camera.OnResized += () => Logger.Log(LogType.Info, $"Editor camera was resized, new size: ({camera.Width}, {camera.Height}).");

            return camera;
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            CompositionTarget.Rendering -= OnRender;
        }

        private void OnRender(object sender, EventArgs e)
        {
            if (!IsVisible)
                return;

            if (cursorMode == CursorMode.HiddenAndLocked && IsKeyboardFocused)
                System.Windows.Forms.Cursor.Position = cursorLockPoint;

            CameraViewModel?.Render(D3DImage);
        }

        private void UserControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            Focus();

            if (e.RightButton == MouseButtonState.Pressed)
                CursorMode = CursorMode.HiddenAndLocked;
        }

        private void UserControl_MouseUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;

            if (e.RightButton == MouseButtonState.Released)
                CursorMode = CursorMode.Normal;
        }

        private void UserControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Keyboard.ClearFocus();
        }

        private void UserControl_GotKeyboardFocus(object sender, RoutedEventArgs e)
        {
            Cursor = cursorMode == CursorMode.Normal ? Cursors.Arrow : Cursors.None;
        }

        private void UserControl_LostKeyboardFocus(object sender, RoutedEventArgs e)
        {
            Cursor = Cursors.Arrow;
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
            CameraViewModel.ResizeCamera((int)RenderControl.ActualWidth, (int)RenderControl.ActualHeight);
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
        Both = GameView & EditorView
    }
}