using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

using Engine;
using Engine.BaseAssets.Components;

using LinearAlgebra;

using SharpDX.DXGI;

using SharpDXtest.Assets.Components;

using Point = System.Drawing.Point;

namespace Editor
{
    public enum CursorMode
    {
        Normal,
        Hidden,
        HiddenAndLocked
    }
    public struct AspectRatio
    {
        public double width;
        public double height;
        public double Ratio => width / height;
        public string displayText;

        public AspectRatio() :
            this(double.NaN, double.NaN, "") { }

        public AspectRatio(double width, double height) :
            this(width, height, "") { }

        public AspectRatio(double width, double height, string displayText)
        {
            this.width = width;
            this.height = height;
            this.displayText = displayText;
        }

        public override string ToString()
        {
            if (displayText != "")
                return displayText;
            if (double.IsNaN(width) || double.IsNaN(height))
                return "Free aspect";
            return width.ToString() + ":" + height.ToString();
        }
    }
    public partial class CameraRenderControl : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private GameObject controlledGameObject;
        private EditorCameraController controller;
        private Camera camera;

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

        private FrameBuffer copyFramebuffer;

        private int framesCount = 0;
        private double timeCounter = 0.0;
        private bool keyboardFocused = false;

        public CameraRenderControl()
        {
            InitializeComponent();

            CursorMode = CursorMode.Normal;

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

                controlledGameObject = new GameObject();
                controlledGameObject.Transform.Position = new Vector3(0, -10, 5);
                controller = controlledGameObject.AddComponent<EditorCameraController>();
                camera = controlledGameObject.AddComponent<Camera>();
                camera.Near = 0.001;
                camera.Far = 500;
                camera.OnResized += c => Logger.Log(LogType.Info, string.Format("Editor camera was resized, new size: ({0}, {1}).", c.Width, c.Height));

                ResizeCameraAndFramebuffer((int)ActualWidth, (int)ActualHeight);
            }

            EngineCore.OnFrameEnded += GameCore_OnFrameEnded;

            CompositionTarget.Rendering += OnRender;

            loaded = true;
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            EngineCore.OnFrameEnded -= GameCore_OnFrameEnded;

            CompositionTarget.Rendering -= OnRender;
        }

        private void OnRender(object sender, EventArgs e)
        {
            if (!IsVisible)
                return;

            if (cursorMode == CursorMode.HiddenAndLocked && IsKeyboardFocused)
                System.Windows.Forms.Cursor.Position = cursorLockPoint;

            d3dimage.Lock();

            d3dimage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, copyFramebuffer.D9SurfaceNativePointer);
            d3dimage.AddDirtyRect(new Int32Rect(0, 0, copyFramebuffer.Width, copyFramebuffer.Height));

            d3dimage.Unlock();
        }

        private void GameCore_OnFrameEnded()
        {
            if (!EngineCore.IsAlive || !IsVisible)
                return;

            controlledGameObject.Update();
            if (keyboardFocused)
                controller.UpdateInput();

            GraphicsCore.RenderScene(camera);

            FrameBuffer buffer = camera.GetNextFrontBuffer();

            GraphicsCore.CurrentDevice.ImmediateContext.ResolveSubresource(buffer.RenderTargetTexture.texture, 0, copyFramebuffer.RenderTargetTexture.texture, 0, Format.B8G8R8A8_UNorm);

            timeCounter += Time.DeltaTime;
            framesCount++;
            if (timeCounter >= 1.0)
            {
                FPSTextBlock.Dispatcher.Invoke(() => { FPSTextBlock.Text = framesCount.ToString(); });

                timeCounter -= 1.0;
                framesCount = 0;
            }
        }

        private void UserControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            Focus();

            if (e.RightButton == MouseButtonState.Pressed)
                CursorMode = CursorMode.HiddenAndLocked;

            if(e.LeftButton == MouseButtonState.Pressed)
                HandlePicking((int)e.GetPosition(RenderControl).X, (int)e.GetPosition(RenderControl).Y);
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
            keyboardFocused = true;
        }

        private void UserControl_LostKeyboardFocus(object sender, RoutedEventArgs e)
        {
            Cursor = Cursors.Arrow;
            keyboardFocused = false;
        }

        private void ResizeCameraAndFramebuffer(int width, int height)
        {
            camera.Aspect = width / (double)height;

            camera.Resize(width, height);

            copyFramebuffer = new FrameBuffer(width, height);
        }

        private void RenderControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!loaded)
                return;

            ResizeCameraAndFramebuffer((int)RenderControl.ActualWidth, (int)RenderControl.ActualHeight);
        }

        private void UpdateRenderControlSize()
        {
            if (double.IsNaN(SelectedAspectRatio.width) || double.IsNaN(SelectedAspectRatio.height))
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

        private void HandlePicking(int mouseX, int mouseY)
        {
            HitResult hitResult;
            Vector3 screenToWorldDir = camera.ScreenToWorldRay(mouseX, mouseY);
            Vector3 nearPlanePos = screenToWorldDir * camera.Near + camera.GameObject.Transform.Position;

            //Logger.Log(LogType.Info, $"Mouse screen pos {mouseX}, {mouseY} \nMouse world dir {screenToWorldDir}\nMouse dir {screenToWorldDir - camera.GameObject.Transform.Position}");

            bool hasHit = Raycast.HitMesh(
                new Ray
                {
                    Origin = nearPlanePos,
                    Direction = screenToWorldDir
                },
                out hitResult
            );

            GameObject cursor = Scene.CurrentScene.GameObjects.First(obj => obj.Name == "Cursor");

            cursor.Transform.Position = hitResult.Point;

            InspectorControl.GameObjectViewModel.Target = hasHit ? hitResult.Target : null;
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateRenderControlSize();
        }

        private void AspectRatioComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateRenderControlSize();
        }
    }
}