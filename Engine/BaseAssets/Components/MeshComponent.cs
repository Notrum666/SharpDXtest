using System;
using System.Collections.Generic;

namespace Engine.BaseAssets.Components
{
    public class MeshComponent : Component
    {
        public Mesh mesh;
        public List<Material> materials = new List<Material>();

        public void Render()
        {
            if (mesh.Primitives.Count != materials.Count)
                throw new Exception("Primitives exceed materials count");
            for (int i = 0; i < mesh.Primitives.Count; ++i)
            {
                materials[i].Use();
                mesh.Primitives[i].Render();
            }
        }
    }
}
