using System.Collections.Generic;

namespace Editor.GameProject
{
    public class ProjectScenesSettings
    {
        public readonly Dictionary<string, string> Scenes = new Dictionary<string, string>();
        public string StartingSceneName = null;
    }
}