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
        public Material material = new Material();
    }
    public class Material
    {
        private Texture albedo;
        public Texture Albedo
        {
            get
            {
                return albedo;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("Albedo", "Texture can't be null.");
                albedo = value;
            }
        }
        private Texture normal;
        public Texture Normal
        {
            get
            {
                return normal;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("Normal", "Texture can't be null.");
                normal = value;
            }
        }
        private Texture metallic;
        public Texture Metallic
        {
            get
            {
                return metallic;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("Metallic", "Texture can't be null.");
                metallic = value;
            }
        }
        private Texture roughness;
        public Texture Roughness
        {
            get
            {
                return roughness;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("Roughness", "Texture can't be null.");
                roughness = value;
            }
        }
        private Texture ambientOcclusion;
        public Texture AmbientOcclusion
        {
            get
            {
                return ambientOcclusion;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("AmbientOcclusion", "Texture can't be null.");
                ambientOcclusion = value;
            }
        }
        public Material()
        {
            albedo = AssetsManager.Textures["default_albedo"];
            normal = AssetsManager.Textures["default_normal"];
            metallic = AssetsManager.Textures["default_metallic"];
            roughness = AssetsManager.Textures["default_roughness"];
            ambientOcclusion = AssetsManager.Textures["default_ambientOcclusion"];
        }
        public Material(Texture albedo, Texture normal, Texture metallic, Texture roughness, Texture ambientOcclusion)
        {
            Albedo = albedo;
            Metallic = metallic;
            Roughness = roughness;
            AmbientOcclusion = ambientOcclusion;
            Normal = normal;
        }
    }
}
