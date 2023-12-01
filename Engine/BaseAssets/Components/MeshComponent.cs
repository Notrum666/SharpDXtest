using System;
using System.Collections.Generic;

namespace Engine.BaseAssets.Components
{
    public class MeshComponent : Component
    {
        public Mesh mesh;
        public Material[] Materials
        {
            get
            {
                return materials.ToArray();
            }
          private set
            {
                if (materials != null)
                {
                    throw new InvalidOperationException("material slots are already allocated");
                }
                materials = new List<Material>();
                materials.AddRange(value);
            }
        }
        private List<Material> materials = null;

        public void Render()
        {
            if (mesh.Primitives.Count != materials.Count)
                throw new Exception("Primitives exceed materials count");
            for (int i = 0; i < mesh.Primitives.Count; ++i)
            {
                if (materials[i] == null)
                {
                    (new Material()).Use();
                }
                else
                {
                    materials[i].Use();
                }
                mesh.Primitives[i].Render();
            }
        }
    }
}
