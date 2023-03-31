using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.BaseAssets.Components
{
    public class Mesh : Component
    {
        public Model model;
        private Material material = new Material();
        public Material Material
        {
            get
            {
                return material;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("Material", "Material can't be null.");
                material = value;
            }
        }
    }
}
