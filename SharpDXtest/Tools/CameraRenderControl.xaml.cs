using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Interop;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using Engine;
using Engine.BaseAssets.Components;
using SharpDXtest.Assets.Components;

namespace Editor
{
    public enum CursorMode
    {
        Normal,
        Hidden,
        HiddenAndLocked
    }
    public partial class CameraRenderControl : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private GameObject controlledGameObject;
        private Camera camera;

        private System.Drawing.Point cursorLockPoint;
        private CursorMode cursorMode;
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
                controlledGameObject.Transform.Position = new LinearAlgebra.Vector3(0, -10, 5);
                EditorCameraController controller = controlledGameObject.AddComponent<EditorCameraController>();
                controller.speed = 5;
                camera = controlledGameObject.AddComponent<Camera>();
                camera.Near = 0.001;
                camera.Far = 500;
                camera.OnResized += c => Logger.Log(LogType.Info, string.Format("Editor camera was resized, new size: ({0}, {1}).", c.Width, c.Height));

                Resize((int)ActualWidth, (int)ActualHeight);
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

            if (keyboardFocused)
                controlledGameObject.Update();

            GraphicsCore.RenderScene(camera);

            FrameBuffer buffer = camera.GetNextFrontBuffer();
        
            GraphicsCore.CurrentDevice.ImmediateContext.ResolveSubresource(buffer.RenderTargetTexture.texture, 0, copyFramebuffer.RenderTargetTexture.texture, 0, SharpDX.DXGI.Format.B8G8R8A8_UNorm);
        
            timeCounter += Time.DeltaTime;
            framesCount++;
            if (timeCounter >= 1.0)
            {
                FPSTextBlock.Dispatcher.Invoke(() => { FPSTextBlock.Text = framesCount.ToString(); });
        
                timeCounter -= 1.0;
                framesCount = 0;
            }
        }
        private void Resize(int width, int height)
        {
            camera.Aspect = width / (double)height;

            camera.Resize(width, height);

            copyFramebuffer = new FrameBuffer(width, height);
        }

        private void UserControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;

            if (e.RightButton == MouseButtonState.Pressed)
                CursorMode = CursorMode.HiddenAndLocked;

            Keyboard.Focus(this);
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

        private void RenderControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!loaded)
                return;

            Resize((int)ActualWidth, (int)ActualHeight);
        }
    }
}
