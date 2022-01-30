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
                    throw new ArgumentNullException("Texture can't be null.");
                albedo = value;
            }
        }
        private double metallic;
        public double Metallic
        {
            get
            {
                return metallic;
            }
            set
            {
                if (value > 1.0 || value < 0.0)
                    throw new ArgumentOutOfRangeException("Metallic can't be lower than 0 or bigger than 1.");
                metallic = value;
            }
        }
        private double roughness;
        public double Roughness
        {
            get
            {
                return roughness;
            }
            set
            {
                if (value > 1.0 || value < 0.0)
                    throw new ArgumentOutOfRangeException("Roughness can't be lower than 0 or bigger than 1.");
                roughness = value;
            }
        }
        private double ambientOcclusion;
        public double AmbientOcclusion
        {
            get
            {
                return ambientOcclusion;
            }
            set
            {
                if (value > 1.0 || value < 0.0)
                    throw new ArgumentOutOfRangeException("Ambient occlusion can't be lower than 0 or bigger than 1.");
                ambientOcclusion = value;
            }
        }
        public Material()
        {
            albedo = AssetsManager.Textures["default"];
            metallic = 0.1;
            roughness = 0.5;
            ambientOcclusion = 0.0;
        }
        public Material(Texture albedo, double metallic = 0.1, double roughness = 0.5, double ambientOcclusion = 0.0)
        {
            Albedo = albedo;
            Metallic = metallic;
            Roughness = roughness;
            AmbientOcclusion = ambientOcclusion;
        }
    }
}
