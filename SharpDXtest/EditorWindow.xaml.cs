using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

using Engine;
using Engine.BaseAssets.Components;
using LinearAlgebra;
using SharpDXtest.Assets.Components;

namespace Editor
{
    public partial class EditorWindow : EditorWindowBase
    {
        private RelayCommand spawnFlyingControl;

        public RelayCommand SpawnFlyingControl => spawnFlyingControl ?? (spawnFlyingControl = new RelayCommand(
            obj =>
            {
                FlyingControl flyingControl = new FlyingControl();
                FrameworkElement item = (FrameworkElement)Activator.CreateInstance((Type)obj);
                flyingControl.Items.Add(item);
                EditorDockingManager.AddFlyingControl(flyingControl);
            },
            obj => obj is Type && (obj as Type).IsSubclassOf(typeof(FrameworkElement))
        ));

        public EditorWindow()
        {
            InitializeComponent();

            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-us");

            DataContext = this;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var mainPath = Directory.GetCurrentDirectory();
            var solutionPath = Directory.GetParent(mainPath)?.Parent?.Parent?.Parent?.FullName;
            AssetsManager.InitializeInFolder(solutionPath);
            AssetsRegistry.InitializeInFolder(solutionPath);
            
            EngineCore.Init(new WindowInteropHelper(this).Handle, (int)ActualWidth, (int)ActualHeight);

            CreateBaseScene();

            EngineCore.IsPaused = true;
            EngineCore.Run();
        }

        private void EditorWindowInst_Closing(object sender, CancelEventArgs e)
        {
            if (EngineCore.IsAlive)
                EngineCore.Stop();
        }

        private void EditorWindowInst_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Keyboard.ClearFocus();
        }

        private void CreateBaseScene()
        {
            Scene.CurrentScene = new Scene();
            GameObject cameraObj = GameObject.Instantiate("Camera");
            Transform cameraTransform = cameraObj.GetComponent<Transform>();
            cameraTransform.Position = new Vector3(0, -40, 90);
            cameraTransform.Rotation = Quaternion.FromEuler(new Vector3(-45));
            Camera camera = cameraObj.AddComponent<Camera>();
            camera.FOV = 16f / 9f;
            camera.Aspect = 16f / 9f;
            camera.Near = 0.001;
            camera.Far = 500;
            cameraObj.AddComponent<SoundListener>();
            cameraObj.AddComponent<CameraController>().Speed = 5;
            cameraObj.AddComponent<InspectorTestComponent>();

            GameObject gasVolumeObj = GameObject.Instantiate("GasVolume");
            Transform gasVolumeTransform = gasVolumeObj.GetComponent<Transform>();
            gasVolumeTransform.Position = new Vector3(0, 0, 60);
            gasVolumeObj.AddComponent<GasVolume>().Size = new Vector3f(200, 200, 50);

            GameObject cubeObj1 = GameObject.Instantiate("Plane");
            Transform cubeObj1Transform = cubeObj1.GetComponent<Transform>();
            cubeObj1Transform.LocalScale = new Vector3(50, 50, 0.5);
            MeshComponent cubeObj1Mesh = cubeObj1.AddComponent<MeshComponent>();
            cubeObj1Mesh.Model = AssetsManager.LoadAssetAtPath<Model>("BaseAssets\\Models\\cube.obj");

            GameObject cubeObj2 = GameObject.Instantiate("Cube");
            Transform cubeObj2Transform = cubeObj2.GetComponent<Transform>();
            cubeObj2Transform.Position = new Vector3(0, 0, 2);
            cubeObj2Transform.LocalRotation = Quaternion.FromEuler(new Vector3(45 * (3.14 / 180), 45 * (3.14 / 180), 0));
            cubeObj2Transform.LocalScale = new Vector3(1, 1, 2);
            MeshComponent cubeObj2Mesh = cubeObj2.AddComponent<MeshComponent>();
            cubeObj2Mesh.Model = AssetsManager.LoadAssetAtPath<Model>("BaseAssets\\Models\\cube_materials.fbx");

            GameObject cesiumMan = GameObject.Instantiate("CesiumMan");
            Transform cesiumManTransform = cesiumMan.GetComponent<Transform>();
            cesiumManTransform.Position = new Vector3(0, 0, 5);
            cesiumManTransform.Rotation = Quaternion.FromEuler(new Vector3(-90 * (3.14 / 180), 0, 0));
            SkeletalMeshComponent cesiumManMesh = cesiumMan.AddComponent<SkeletalMeshComponent>();
            cesiumManMesh.Model = AssetsManager.LoadAssetAtPath<Model>("BaseAssets\\Models\\cesium_man.fbx");
            cesiumManMesh.Animation = AssetsManager.LoadAssetByGuid<SkeletalAnimation>(new Guid("32c68bd7597e4c1f9c1037607098c766"));

            GameObject cesiumMan2 = GameObject.Instantiate("CesiumMan2");
            Transform cesiumManTransform2 = cesiumMan2.GetComponent<Transform>();
            cesiumManTransform2.Position = new Vector3(0, 2, 5);
            cesiumManTransform2.Rotation = Quaternion.FromEuler(new Vector3(-90 * (3.14 / 180), 0, 0));
            SkeletalMeshComponent cesiumManMesh2 = cesiumMan2.AddComponent<SkeletalMeshComponent>();
            cesiumManMesh2.Model = AssetsManager.LoadAssetAtPath<Model>("BaseAssets\\Models\\cesium_man.fbx");
            //cesiumManMesh2.AnimationIndex = 14;

            GameObject light1 = GameObject.Instantiate("Light1");
            light1.GetComponent<Transform>().LocalPosition = new Vector3(5, 0, 3);
            light1.AddComponent<PointLight>().Radius.Set(10);

            GameObject light2 = GameObject.Instantiate("Light2");
            light2.GetComponent<Transform>().LocalPosition = new Vector3(-5, 0, 3);
            light2.AddComponent<PointLight>().Radius.Set(10);

            GameObject light3 = GameObject.Instantiate("Light3");
            light3.GetComponent<Transform>().LocalPosition = new Vector3(-5, 0, 3);
            light3.AddComponent<PointLight>().Radius.Set(10);

            GameObject light4 = GameObject.Instantiate("Light4");
            light4.GetComponent<Transform>().LocalPosition = new Vector3(5, 0, 3);
            light4.AddComponent<PointLight>().Radius.Set(10);
        }
    }
}