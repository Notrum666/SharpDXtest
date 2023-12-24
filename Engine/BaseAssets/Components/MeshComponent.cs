using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine.BaseAssets.Components
{
    public class MeshComponent : Component
    {
        public Model Mesh { get => Model; set => Model = value; }
        private Model model = null;
        public Model Model
        {
            get => model;
            set
            {
                model = value;
                if (model is null)
                    Materials = new Material[0];
                else
                    Materials = value.Meshes.Select(p => p.DefaultMaterial).ToArray();
            }
        }
        public Material[] Materials { get; private set; } = new Material[0];

        public void Render()
        {
            if (model is null)
            {
                Logger.Log(LogType.Warning, GameObject.ToString() + ": trying to render MeshComponent with no mesh set");
                return;
            }

            for (int i = 0; i < model.Meshes.Count; ++i)
            {
                Material curMaterial = Materials[i];
                if (curMaterial is null)
                    curMaterial = model.Meshes[i].DefaultMaterial;
                if (curMaterial is null)
                    curMaterial = Material.Default;
                curMaterial.Use();
                model.Meshes[i].Render();
            }
        }
    }
}
