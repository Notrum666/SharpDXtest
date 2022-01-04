using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpDXtest.Components
{
    public class Mesh : Component
    {
        public Model model;
        public Texture texture;
        public Material material = Material.Default;
    }
    public class Material
    {
        public static Material Default { get { return new Material(new Vector4(1.0, 1.0, 1.0, 1.0), new Vector4(1.0, 1.0, 1.0, 1.0), new Vector4(1.0, 1.0, 1.0, 1.0), 1.0f); } }
        public Vector4 ambient;
        public Vector4 diffuse;
        public Vector4 specular;
        public float metallic;
        public Material(Vector4 ambient, Vector4 diffuse, Vector4 specular, float metallic)
        {
            this.ambient = ambient;
            this.diffuse = diffuse;
            this.specular = specular;
            this.metallic = metallic;
        }
    }
}
