using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Threading;
using System.Globalization;
using System.ComponentModel;
using System.Windows.Interop;

using Engine;
using LinearAlgebra;
using SharpDXtest.Assets.Components;

using SharpDX.Direct3D11;
using SharpDX;

namespace SharpDXtest
{
    public partial class EditorWindow : Window, INotifyPropertyChanged
    {
        public bool IsPaused { get => EngineCore.IsPaused; }
        public event PropertyChangedEventHandler PropertyChanged;

        private bool cursorLocking = true;
        private bool isCursorShown = true;

        private bool windowLoaded = false;

        private FrameBuffer copyFramebuffer;

        private int framesCount = 0;
        private double timeCounter = 0.0;

        public EditorWindow()
        {
            InitializeComponent();

            if (cursorLocking)
                HideCursor();

            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-us");

            DataContext = this;

            EngineCore.OnPaused += GameCore_OnPaused;
            EngineCore.OnResumed += GameCore_OnResumed;
            EngineCore.OnFrameEnded += GameCore_OnFrameEnded;

            CompositionTarget.Rendering += OnRender;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
            throw new InvalidOperationException("exception");
        }
        private void HideCursor()
        {
            if (!isCursorShown)
                return;

            isCursorShown = false;
            System.Windows.Forms.Cursor.Hide();
        }
        private void ShowCursor()
        {
            if (isCursorShown)
                return;

            isCursorShown = true;
            System.Windows.Forms.Cursor.Position = new System.Drawing.Point((int)ActualWidth / 2, (int)ActualHeight / 2);
            System.Windows.Forms.Cursor.Show();
        }
        private void OnRender(object sender, EventArgs e)
        {
            if (EngineCore.IsPaused)
                return;

            d3dimage.Lock();

            d3dimage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, copyFramebuffer.D9SurfaceNativePointer);
            d3dimage.AddDirtyRect(new System.Windows.Int32Rect(0, 0, copyFramebuffer.Width, copyFramebuffer.Height));

            d3dimage.Unlock();
        }
        private void GameCore_OnFrameEnded()
        {
            if (!isCursorShown && cursorLocking)
                System.Windows.Forms.Cursor.Position = new System.Drawing.Point((int)ActualWidth / 2, (int)ActualHeight / 2);

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
        private void GameCore_OnPaused()
        {
            Dispatcher.Invoke(() =>
            {
                if (cursorLocking)
                    ShowCursor();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsPaused"));
            });
        }
        private void GameCore_OnResumed()
        {
            Dispatcher.Invoke(() =>
            {
                if (cursorLocking)
                    HideCursor();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsPaused"));
            });
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            EngineCore.Init(new WindowInteropHelper(this).Handle, (int)ActualWidth, (int)ActualHeight);

            copyFramebuffer = new FrameBuffer((int)ActualWidth, (int)ActualHeight);

            EngineCore.CurrentScene = AssetsManager.LoadScene("Assets\\Scenes\\Scene5.xml");

            EngineCore.Run();

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsPaused"));

            windowLoaded = true;
        }

        private void MainWindowInst_Closed(object sender, EventArgs e)
        {
            if (EngineCore.IsAlive)
                EngineCore.Stop();
        }

        private void MainWindowInst_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                EngineCore.IsPaused = !EngineCore.IsPaused;
        }

        private void PauseMenuButton_Exit_Click(object sender, RoutedEventArgs e)
        {
            EngineCore.Stop();
            Close();
        }

        private void PauseMenuButton_Resume_Click(object sender, RoutedEventArgs e)
        {
            EngineCore.IsPaused = false;
        }

        private void ChangeCursorLocking(object sender, RoutedEventArgs e)
        {
            cursorLocking = !cursorLocking;

            CursorLockingButton.Content = cursorLocking ? "Cursor locking (On)" : "Cursor locking (Off)";
        }

        private void PauseMenuButton_Scene1_Click(object sender, RoutedEventArgs e)
        {
            EngineCore.CurrentScene = AssetsManager.LoadScene("Assets\\Scenes\\Scene5.xml");
            EngineCore.IsPaused = false;
        }

        private void PauseMenuButton_Scene2_Click(object sender, RoutedEventArgs e)
        {
            EngineCore.CurrentScene = AssetsManager.LoadScene("Assets\\Scenes\\Bloom.xml");
            EngineCore.IsPaused = false;
        }

        private void PauseMenuButton_Scene3_Click(object sender, RoutedEventArgs e)
        {
            EngineCore.CurrentScene = AssetsManager.LoadScene("Assets\\Scenes\\Scene3.xml");
            EngineCore.IsPaused = false;
        }

        private void RenderControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!windowLoaded)
                return;

            if (GraphicsCore.CurrentCamera != null)
                GraphicsCore.CurrentCamera.Aspect = RenderControl.ActualWidth / RenderControl.ActualHeight;

            GraphicsCore.Resize((int)RenderControl.ActualWidth, (int)RenderControl.ActualHeight);

            copyFramebuffer = new FrameBuffer((int)RenderControl.ActualWidth, (int)RenderControl.ActualHeight);
        }
        private void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Logger.CurrentDomainOnUnhandledException(sender, e);
        }
    }
}
