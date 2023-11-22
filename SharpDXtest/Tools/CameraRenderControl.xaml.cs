using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Interop;

using Engine;
using LinearAlgebra;
using SharpDXtest.Assets.Components;

using SharpDX.Direct3D11;
using SharpDX;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SharpDX.Direct3D9;

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

        private CursorMode cursorMode;
        public CursorMode CursorMode
        {
            get => cursorMode;
            set
            {
                cursorMode = value;
                if (IsKeyboardFocused)
                    Cursor = value == CursorMode.Normal ? Cursors.Arrow : Cursors.None;
                OnPropertyChanged();
            }
        }
        
        private bool loaded = false;
        
        private FrameBuffer copyFramebuffer;
        
        private int framesCount = 0;
        private double timeCounter = 0.0;
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
            if (!EngineCore.IsAlive)
                return;

            if (copyFramebuffer == null)
                copyFramebuffer = new FrameBuffer((int)ActualWidth, (int)ActualHeight);

            EngineCore.OnFrameEnded += GameCore_OnFrameEnded;

            CompositionTarget.Rendering += OnRender;

            loaded = true;
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (!EngineCore.IsAlive)
                return;

            EngineCore.OnFrameEnded -= GameCore_OnFrameEnded;

            CompositionTarget.Rendering -= OnRender;
        }
        private void OnRender(object sender, EventArgs e)
        {
            if (EngineCore.IsPaused || !IsVisible)
                return;

            if (cursorMode == CursorMode.HiddenAndLocked && IsKeyboardFocused)
            {
                System.Windows.Point point = PointToScreen(new System.Windows.Point(ActualWidth / 2, ActualHeight / 2));
                System.Windows.Forms.Cursor.Position = new System.Drawing.Point((int)point.X, (int)point.Y);
            }

            d3dimage.Lock();
        
            d3dimage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, copyFramebuffer.D9SurfaceNativePointer);
            d3dimage.AddDirtyRect(new Int32Rect(0, 0, copyFramebuffer.Width, copyFramebuffer.Height));
        
            d3dimage.Unlock();
        }
        private void GameCore_OnFrameEnded()
        {
            if (!EngineCore.IsAlive || !IsVisible) 
                return;
        
            FrameBuffer buffer = GraphicsCore.GetNextFrontBuffer();
        
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
        private void RenderControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!loaded)
                return;
        
            if (GraphicsCore.CurrentCamera != null)
                GraphicsCore.CurrentCamera.Aspect = RenderControl.ActualWidth / RenderControl.ActualHeight;
        
            GraphicsCore.Resize((int)RenderControl.ActualWidth, (int)RenderControl.ActualHeight);
        
            copyFramebuffer = new FrameBuffer((int)RenderControl.ActualWidth, (int)RenderControl.ActualHeight);
        }

        private void UserControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            Keyboard.Focus(this);
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
    }
}
