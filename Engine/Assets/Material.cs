using System;
using LinearAlgebra;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace Engine
{
    public class Material : BaseAsset
    {
        private Texture albedo;
        public Texture Albedo
        {
            get => albedo;
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
            get => normal;
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
            get => metallic;
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
            get => roughness;
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
            get => ambientOcclusion;
            set
            {
                if (value == null)
                    throw new ArgumentNullException("AmbientOcclusion", "Texture can't be null.");
                ambientOcclusion = value;
            }
        }
        private Texture emissive;
        public Texture Emissive
        {
            get => emissive;
            set
            {
                if (value == null)
                    throw new ArgumentNullException("Emissive", "Texture can't be null.");
                emissive = value;
            }
        }

        private bool disposed = false;

        #region Legacy

        public static readonly Material Default = new Material()
        {
            albedo = new Texture(64, 64, new Vector4f(1.0f, 1.0f, 1.0f, 1.0f).GetBytes(), Format.R32G32B32A32_Float, BindFlags.ShaderResource),
            normal = new Texture(64, 64, new Vector4f(0.5f, 0.5f, 1.0f, 0.0f).GetBytes(), Format.R32G32B32A32_Float, BindFlags.ShaderResource),
            metallic = new Texture(64, 64, 0.1f.GetBytes(), Format.R32_Typeless, BindFlags.ShaderResource),
            roughness = new Texture(64, 64, 0.5f.GetBytes(), Format.R32_Typeless, BindFlags.ShaderResource),
            ambientOcclusion = new Texture(64, 64, 0.0f.GetBytes(), Format.R32_Typeless, BindFlags.ShaderResource),
            emissive = new Texture(64, 64, 0.0f.GetBytes(), Format.R32_Typeless, BindFlags.ShaderResource)
        };

        #endregion

        public void Use()
        {
            Albedo.Use("albedoMap");
            Normal.Use("normalMap");
            Metallic.Use("metallicMap");
            Roughness.Use("roughnessMap");
            AmbientOcclusion.Use("ambientOcclusionMap");
            Emissive.Use("emissiveMap");
            ShaderPipeline.Current.UploadUpdatedUniforms();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing) { }
            disposed = true;

            base.Dispose(disposing);
        }
    }
}