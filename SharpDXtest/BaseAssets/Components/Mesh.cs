using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpDXtest.BaseAssets.Components
{
    public class Mesh : Component
    {
        public Model model;
        public Texture texture;
        public Material material = Material.Default;
    }
    public class Material
    {
        public static Material Default { get { return new Material(new Vector3(1.0, 1.0, 1.0), new Vector3(1.0, 1.0, 1.0), new Vector3(1.0, 1.0, 1.0), 1.0f); } }
        public Vector3 ambient;
        public Vector3 diffuse;
        public Vector3 specular;
        public float metallic;
        public Material(Vector3 ambient, Vector3 diffuse, Vector3 specular, float metallic)
        {
            this.ambient = ambient;
            this.diffuse = diffuse;
            this.specular = specular;
            this.metallic = metallic;
        }
    }
}
