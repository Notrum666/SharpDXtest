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
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Globalization;
using System.ComponentModel;
using System.Windows.Interop;

using Engine;
using LinearAlgebra;

namespace SharpDXtest
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public bool IsPaused { get => GameCore.IsPaused; }
        public string CollisionExitSAT { get; private set; }
        public string CollisionNormalSAT { get; private set; }
        public string CollisionExitGJK { get; private set; }
        public string CollisionNormalGJK { get; private set; }
        public event PropertyChangedEventHandler PropertyChanged;

        private bool isCursorShown = true;

        private bool windowLoaded = false;
        public MainWindow()
        {
            InitializeComponent();

            HideCursor();

            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-us");

            DataContext = this;

            GameCore.OnPaused += GameCore_OnPaused;
            GameCore.OnResumed += GameCore_OnResumed;
            GameCore.OnFrameEnded += GameCore_OnFrameEnded;

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
            lock (GraphicsCore.Frontbuffer)
            {
                d3dimage.Lock();

                d3dimage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, GraphicsCore.Frontbuffer.D9SurfaceNativePointer);
                d3dimage.AddDirtyRect(new System.Windows.Int32Rect(0, 0, GraphicsCore.Frontbuffer.Width, GraphicsCore.Frontbuffer.Height));

                d3dimage.Unlock();
            }
        }
        private void GameCore_OnFrameEnded()
        {
            List<Engine.BaseAssets.Components.Collider> colliders =
                GameCore.CurrentScene.objects.Where(obj => obj.Components.Any(comp => comp is Engine.BaseAssets.Components.Collider))
                .Select(obj => obj.getComponent<Engine.BaseAssets.Components.Collider>()).ToList();
            colliders[0].fixedUpdate();
            colliders[1].fixedUpdate();
            Vector3? collisionExitSAT, collisionNormalSAT, collisionExitGJK, collisionNormalGJK;
            colliders[0].getCollisionExitVector_SAT(colliders[1], out collisionExitSAT, out collisionNormalSAT, out _);
            colliders[0].getCollisionExitVector_GJK_EPA(colliders[1], out collisionExitGJK, out collisionNormalGJK, out _);
            if (collisionExitSAT != null && collisionExitGJK != null && collisionNormalSAT != null && collisionNormalGJK != null)
            {
                CollisionExitSAT = collisionExitSAT.Value.ToString("f2");
                CollisionNormalSAT = collisionNormalSAT.Value.normalized().ToString("f2");
                CollisionExitGJK = collisionExitGJK.Value.ToString("f2");
                CollisionNormalGJK = collisionNormalGJK.Value.normalized().ToString("f2");
            }
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CollisionExitSAT"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CollisionNormalSAT"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CollisionExitGJK"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CollisionNormalGJK"));
        }
        private void GameCore_OnPaused()
        {
            Dispatcher.Invoke(() =>
            {
                ShowCursor();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsPaused"));
            });
        }
        private void GameCore_OnResumed()
        {
            Dispatcher.Invoke(() =>
            {
                HideCursor();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsPaused"));
            });
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            GameCore.Init(new WindowInteropHelper(this).Handle, (int)ActualWidth, (int)ActualHeight);

            GameCore.Run();

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsPaused"));

            windowLoaded = true;
        }

        private void MainWindowInst_Closed(object sender, EventArgs e)
        {
            if (GameCore.IsAlive)
                GameCore.Stop();
        }

        private void MainWindowInst_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                GameCore.IsPaused = !GameCore.IsPaused;
        }

        private void PauseMenuButton_Exit_Click(object sender, RoutedEventArgs e)
        {
            GameCore.Stop();
            Close();
        }

        private void PauseMenuButton_Resume_Click(object sender, RoutedEventArgs e)
        {
            GameCore.IsPaused = false;
        }

        private void MainWindowInst_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!windowLoaded)
                return;

            if (GraphicsCore.CurrentCamera != null)
                GraphicsCore.CurrentCamera.Aspect = RenderControl.ActualWidth / RenderControl.ActualHeight;

            GraphicsCore.Resize((int)RenderControl.ActualWidth, (int)RenderControl.ActualHeight);
        }
    }
}
