﻿using System;
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
using System.Diagnostics;
using System.Runtime.InteropServices;
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
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public bool IsPaused { get => EngineCore.IsPaused; }
        public event PropertyChangedEventHandler PropertyChanged;

        private bool cursorLocking = true;
        private bool isCursorShown = true;

        private bool windowLoaded = false;

        private FrameBuffer copyFramebuffer;

        struct tmp
        {
            public Vector4 a;
            public Vector4 b;
        }
        public MainWindow()
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
            //if (d3dimage.TryLock(new Duration(TimeSpan.Zero)))
            //{
            d3dimage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, copyFramebuffer.D9SurfaceNativePointer);
            d3dimage.AddDirtyRect(new System.Windows.Int32Rect(0, 0, copyFramebuffer.Width, copyFramebuffer.Height));
            //}
            //else
            //{
            //
            //}
            d3dimage.Unlock();
        }
        private void GameCore_OnFrameEnded()
        {
            if (!isCursorShown && cursorLocking)
                System.Windows.Forms.Cursor.Position = new System.Drawing.Point((int)ActualWidth / 2, (int)ActualHeight / 2);

            FrameBuffer buffer = GraphicsCore.GetNextFrontBuffer();

            GraphicsCore.CurrentDevice.ImmediateContext.ResolveSubresource(buffer.RenderTargetTexture.texture, 0, copyFramebuffer.RenderTargetTexture.texture, 0, SharpDX.DXGI.Format.B8G8R8A8_UNorm);
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

            EngineCore.CurrentScene = AssetsManager.LoadScene("Assets\\Scenes\\Scene4.xml");
            foreach (GameObject obj in EngineCore.CurrentScene.objects)
            {
                BallRestarter comp = obj.getComponent<BallRestarter>();
                if (comp == null)
                    continue;
                comp.OnScoreChanged += OnScoreChanged;
                break;
            }
            OnScoreChanged(0, 0);
            LeftScore.Visibility = Visibility.Hidden;
            RightScore.Visibility = Visibility.Hidden;

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

        private void MainWindowInst_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!windowLoaded)
                return;

            if (GraphicsCore.CurrentCamera != null)
                GraphicsCore.CurrentCamera.Aspect = RenderControl.ActualWidth / RenderControl.ActualHeight;

            GraphicsCore.Resize((int)RenderControl.ActualWidth, (int)RenderControl.ActualHeight);

            copyFramebuffer = new FrameBuffer((int)RenderControl.ActualWidth, (int)RenderControl.ActualHeight);
        }

        private void PauseMenuButton_LoadScene1_Click(object sender, RoutedEventArgs e)
        {
            LeftScore.Visibility = Visibility.Visible;
            RightScore.Visibility = Visibility.Visible;
            EngineCore.CurrentScene = AssetsManager.LoadScene("Assets\\Scenes\\Scene1.xml");
            foreach (GameObject obj in EngineCore.CurrentScene.objects)
            {
                BallRestarter comp = obj.getComponent<BallRestarter>();
                if (comp == null)
                    continue;
                comp.OnScoreChanged += OnScoreChanged;
                break;
            }
            OnScoreChanged(0, 0);

            EngineCore.IsPaused = false;
        }

        private void OnScoreChanged(int arg1, int arg2)
        {
            LeftScore.Dispatcher.Invoke(new Action(() =>
            {
                LeftScore.Text = arg1.ToString();
                RightScore.Text = arg2.ToString();
            }));
        }

        private void PauseMenuButton_LoadScene2_Click(object sender, RoutedEventArgs e)
        {
            LeftScore.Visibility = Visibility.Hidden;
            RightScore.Visibility = Visibility.Hidden;
            EngineCore.CurrentScene = AssetsManager.LoadScene("Assets\\Scenes\\Scene2.xml");
            EngineCore.IsPaused = false;
        }

        private void PauseMenuButton_LoadScene3_Click(object sender, RoutedEventArgs e)
        {
            LeftScore.Visibility = Visibility.Hidden;
            RightScore.Visibility = Visibility.Hidden;
            EngineCore.CurrentScene = AssetsManager.LoadScene("Assets\\Scenes\\Scene3.xml");
            EngineCore.IsPaused = false;
        }

        private void PauseMenuButton_LoadScene4_Click(object sender, RoutedEventArgs e)
        {
            LeftScore.Visibility = Visibility.Hidden;
            RightScore.Visibility = Visibility.Hidden;
            EngineCore.CurrentScene = AssetsManager.LoadScene("Assets\\Scenes\\Scene4.xml");
            EngineCore.IsPaused = false;
        }

        private void ChangeCursorLocking(object sender, RoutedEventArgs e)
        {
            cursorLocking = !cursorLocking;

            CursorLockingButton.Content = cursorLocking ? "Cursor locking (On)" : "Cursor locking (Off)";
        }
    }
}
