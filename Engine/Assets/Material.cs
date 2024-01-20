using System;

using LinearAlgebra;

using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace Engine
{
    public class Material : BaseAsset
    {
        internal Vector4f? BaseColor { get; set; }

        private Texture albedo;
        public Texture Albedo
        {
            get => albedo;
            set
            {
                if (value == null)
                {
                    Logger.Log(LogType.Error, "Material's texture 'Albedo' can't be null");
                    return;
                }
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
                {
                    Logger.Log(LogType.Error, "Material's texture 'Normal' can't be null");
                    return;
                }
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
                {
                    Logger.Log(LogType.Error, "Material's texture 'Metallic' can't be null");
                    return;
                }
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
                {
                    Logger.Log(LogType.Error, "Material's texture 'Roughness' can't be null");
                    return;
                }
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
                {
                    Logger.Log(LogType.Error, "Material's texture 'Emissive' can't be null");
                    return;
                }
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
                {
                    Logger.Log(LogType.Error, "Material's texture 'Emissive' can't be null");
                    return;
                }
                emissive = value;
            }
        }

        private bool disposed = false;

        public static readonly Material Default = CreateDefault();

        public static Material CreateDefault()
        {
            return new Material
            {
                BaseColor = null,
                albedo = new Texture(64, 64, new Vector4f(1.0f, 1.0f, 1.0f, 1.0f).GetBytes(), Format.R32G32B32A32_Float, BindFlags.ShaderResource),
                normal = new Texture(64, 64, new Vector4f(0.5f, 0.5f, 1.0f, 0.0f).GetBytes(), Format.R32G32B32A32_Float, BindFlags.ShaderResource),
                metallic = new Texture(64, 64, 0.1f.GetBytes(), Format.R32_Typeless, BindFlags.ShaderResource),
                roughness = new Texture(64, 64, 0.5f.GetBytes(), Format.R32_Typeless, BindFlags.ShaderResource),
                ambientOcclusion = new Texture(64, 64, 0.0f.GetBytes(), Format.R32_Typeless, BindFlags.ShaderResource),
                emissive = new Texture(64, 64, 0.0f.GetBytes(), Format.R32_Typeless, BindFlags.ShaderResource),
            };
        }

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