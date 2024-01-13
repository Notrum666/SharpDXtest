using System.Collections.Generic;

namespace Engine
{
    public static class Game
    {
        private static bool initialized = false;

        public static string Name { get; set; }
        public static string FolderPath { get; private set; }

        //TODO: Add null check
        public static GameGraphicsSettings GraphicsSettings { get; set; }

        public static string StartingSceneName { get; set; }
        public static Dictionary<string, string> Scenes = new Dictionary<string, string>(); //path to name map

        public static void Initialize(string name, string folderPath)
        {
            if (initialized)
                return;
            initialized = true;

            Name = name;
            FolderPath = folderPath;

            GraphicsSettings = GameGraphicsSettings.Default();

            AssetsManager.InitializeInFolder(FolderPath);
        }

        public static void Load()
        {
            GraphicsSettings.Load();
        }

        public static void Unload()
        {
            GraphicsSettings.Unload();
        }

        public static void Destroy()
        {
            initialized = false;
        }
    }
}