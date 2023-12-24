using System;

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

        public static Material Default => new Material()
        {
            albedo = AssetsManager_Old.Textures["default_albedo"],
            normal = AssetsManager_Old.Textures["default_normal"],
            metallic = AssetsManager_Old.Textures["default_metallic"],
            roughness = AssetsManager_Old.Textures["default_roughness"],
            ambientOcclusion = AssetsManager_Old.Textures["default_ambientOcclusion"],
            emissive = AssetsManager_Old.Textures["default_emissive"]
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