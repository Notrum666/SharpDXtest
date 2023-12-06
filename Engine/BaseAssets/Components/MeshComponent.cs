using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine.BaseAssets.Components
{
    public class MeshComponent : Component
    {
        private Mesh mesh = null;
        public Mesh Mesh
        {
            get => mesh;
            set
            {
                mesh = value;
                if (mesh is null)
                    Materials = new Material[0];
                else
                    Materials = value.Primitives.Select(p => p.DefaultMaterial).ToArray();
            }
        }
        public Material[] Materials { get; private set; } = new Material[0];

        public void Render()
        {
            if (mesh is null)
            {
                Logger.Log(LogType.Warning, GameObject.ToString() + ": trying to render MeshComponent with no mesh set");
                return;
            }

            for (int i = 0; i < mesh.Primitives.Count; ++i)
            {
                Material curMaterial = Materials[i];
                if (curMaterial is null)
                    curMaterial = mesh.Primitives[i].DefaultMaterial;
                if (curMaterial is null)
                    curMaterial = AssetsManager.Materials["default"];
                curMaterial.Use();
                mesh.Primitives[i].Render();
            }
        }
    }
}
