using System;
using System.Collections.Generic;
using System.Linq;

using Engine.BaseAssets.Components;

using LinearAlgebra;

namespace Engine
{
    public static class SceneManager
    {
        private const string DefaultSceneName = "|defaultScene";

        public static Action<string> OnSceneUnloading;
        public static Action<string> OnSceneLoaded;

        private static Dictionary<string, string> scenes = new Dictionary<string, string>();

        private static string nextSceneName;

        public static void UpdateScenesList(Dictionary<string, string> newScenes)
        {
            scenes = newScenes;
        }

        public static void LoadSceneByName(string sceneName)
        {
            if (sceneName == null || !scenes.Any())
            {
                nextSceneName = DefaultSceneName;
                return;
            }

            if (!scenes.ContainsKey(sceneName))
            {
                Logger.Log(LogType.Error, $"Scene \"{sceneName}\" is not registered!");
                return;
            }

            nextSceneName = sceneName;
        }

        internal static void TryLoadNextScene()
        {
            if (nextSceneName == null || Scene.CurrentScene != null)
                return;

            if (nextSceneName == DefaultSceneName)
            {
                LoadDefaultScene();
                OnSceneLoaded?.Invoke(nextSceneName);
            }
            else if (scenes.TryGetValue(nextSceneName, out string scenePath))
            {
                Scene.CurrentScene = AssetsManager.LoadAssetAtPath<Scene>(scenePath);
                Scene.CurrentScene.Name = nextSceneName;
                OnSceneLoaded?.Invoke(nextSceneName);
            }
            else
                Logger.Log(LogType.Error, $"Tried to load not registered scene \"{nextSceneName}\"!");

            nextSceneName = null;
        }

        internal static void TryUnloadCurrentScene()
        {
            // Scene currentScene = Scene.CurrentScene;
            if (nextSceneName == null || Scene.CurrentScene is not { } currentScene)
                return;

            Scene.CurrentScene = null;
            OnSceneUnloading?.Invoke(currentScene.Name);
            currentScene.Dispose();
        }

        private static void LoadDefaultScene()
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

            GameObject gasVolumeObj = GameObject.Instantiate("GasVolume");
            Transform gasVolumeTransform = gasVolumeObj.GetComponent<Transform>();
            gasVolumeTransform.Position = new Vector3(0, 0, 60);
            gasVolumeObj.AddComponent<GasVolume>().Size = new Vector3f(200, 200, 50);

            GameObject cubeObj1 = GameObject.Instantiate("Plane");
            Transform cubeObj1Transform = cubeObj1.GetComponent<Transform>();
            cubeObj1Transform.LocalScale = new Vector3(50, 50, 0.5);
            MeshComponent cubeObj1Mesh = cubeObj1.AddComponent<MeshComponent>();
            cubeObj1Mesh.Model = AssetsManager.LoadAssetAtPath<Model>("Models\\cube.obj");

            GameObject cubeObj2 = GameObject.Instantiate("Cube");
            Transform cubeObj2Transform = cubeObj2.GetComponent<Transform>();
            cubeObj2Transform.Position = new Vector3(0, 0, 2);
            cubeObj2Transform.LocalRotation = Quaternion.FromEuler(new Vector3(45 * (3.14 / 180), 45 * (3.14 / 180), 0));
            cubeObj2Transform.LocalScale = new Vector3(1, 1, 2);
            MeshComponent cubeObj2Mesh = cubeObj2.AddComponent<MeshComponent>();
            cubeObj2Mesh.Model = AssetsManager.LoadAssetAtPath<Model>("Models\\cube_materials.fbx");

            GameObject cesiumMan = GameObject.Instantiate("CesiumMan");
            Transform cesiumManTransform = cesiumMan.GetComponent<Transform>();
            cesiumManTransform.Position = new Vector3(0, 0, 5);
            cesiumManTransform.Rotation = Quaternion.FromEuler(new Vector3(-90 * (3.14 / 180), 0, 0));
            SkeletalMeshComponent cesiumManMesh = cesiumMan.AddComponent<SkeletalMeshComponent>();
            cesiumManMesh.Model = AssetsManager.LoadAssetAtPath<Model>("Models\\cesium_man.fbx");
            cesiumManMesh.Animation = AssetsManager.LoadAssetByGuid<SkeletalAnimation>(new Guid("32c68bd7597e4c1f9c1037607098c766"));

            GameObject cesiumMan2 = GameObject.Instantiate("CesiumMan2");
            Transform cesiumManTransform2 = cesiumMan2.GetComponent<Transform>();
            cesiumManTransform2.Position = new Vector3(0, 2, 5);
            cesiumManTransform2.Rotation = Quaternion.FromEuler(new Vector3(-90 * (3.14 / 180), 0, 0));
            SkeletalMeshComponent cesiumManMesh2 = cesiumMan2.AddComponent<SkeletalMeshComponent>();
            cesiumManMesh2.Model = AssetsManager.LoadAssetAtPath<Model>("Models\\cesium_man.fbx");
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

            ScriptAsset scriptAsset = AssetsManager.LoadAssetAtPath<ScriptAsset>("TestProjectComponent.cs");
            cubeObj1.AddComponent(scriptAsset.ComponentType);
        }
    }
}