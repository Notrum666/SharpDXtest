using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Controls;

using Engine.Assets;

using LinearAlgebra;

using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace Engine.AssetsData
{
    [AssetData<Material>]
    public class MaterialData : NativeAssetData
    {
        public sealed override string FileExtension => ".mat";

        //BaseColor = 1,
        //Normals = 2,
        //Emissive = 3,
        //Metallic = 4,
        //Roughness = 5,
        //AmbientOcclusion = 6
        public Vector4f? BaseColor = null;
        //public readonly Dictionary<MaterialTextureType, Guid> TexturesGuids = new Dictionary<MaterialTextureType, Guid>();
        [GuidExpectedType(typeof(Texture))]
        public Guid Albedo = Guid.Empty;
        [GuidExpectedType(typeof(Texture))]
        public Guid Normal = Guid.Empty;
        [GuidExpectedType(typeof(Texture))]
        public Guid Metallic = Guid.Empty;
        [GuidExpectedType(typeof(Texture))]
        public Guid Roughness = Guid.Empty;
        [GuidExpectedType(typeof(Texture))]
        public Guid AmbientOcclusion = Guid.Empty;
        [GuidExpectedType(typeof(Texture))]
        public Guid Emissive = Guid.Empty;

        protected sealed override void SetDefaultValues()
        {
            BaseColor = null;
            //foreach (MaterialTextureType textureType in Enum.GetValues<MaterialTextureType>())
            //{
            //    TexturesGuids[textureType] = Guid.Empty;
            //}
        }

        public sealed override void Serialize(BinaryWriter writer)
        {
            YamlManager.SaveToStream(writer.BaseStream, this);
        }

        public sealed override void Deserialize(BinaryReader reader)
        {
            YamlManager.LoadFromStream(reader.BaseStream, this);
        }

        public override Material ToRealAsset(BaseAsset targetAsset = null)
        {
            Material material = targetAsset as Material ?? new Material();

            if (Albedo.NotEmpty())
                material.Albedo = AssetsManager.LoadAssetByGuid<Texture>(Albedo);
            else
            {
                if (BaseColor == null)
                    material.Albedo = Material.Default.Albedo;
                else if (BaseColor != material.BaseColor)
                    material.Albedo = new Texture(64, 64, BaseColor?.GetBytes(), Format.R32G32B32A32_Float, BindFlags.ShaderResource);
            }

            material.Normal = Normal.NotEmpty() ? AssetsManager.LoadAssetByGuid<Texture>(Normal) : Material.Default.Normal;
            material.Metallic = Metallic.NotEmpty() ? AssetsManager.LoadAssetByGuid<Texture>(Metallic) : Material.Default.Metallic;
            material.Roughness = Roughness.NotEmpty() ? AssetsManager.LoadAssetByGuid<Texture>(Roughness) : Material.Default.Roughness;
            material.AmbientOcclusion = AmbientOcclusion.NotEmpty() ? AssetsManager.LoadAssetByGuid<Texture>(AmbientOcclusion) : Material.Default.AmbientOcclusion;
            material.Emissive = Emissive.NotEmpty() ? AssetsManager.LoadAssetByGuid<Texture>(Emissive) : Material.Default.Emissive;

            return material;
        }

        public void AddTexture(MaterialTextureType textureType, Guid guid)
        {
            if (guid != Guid.Empty)
            {
                switch (textureType)
                {
                    case MaterialTextureType.BaseColor:
                        Albedo = guid;
                        break;
                    case MaterialTextureType.Normals:
                        Normal = guid;
                        break;
                    case MaterialTextureType.Emissive:
                        Emissive = guid;
                        break;
                    case MaterialTextureType.Metallic:
                        Metallic = guid;
                        break;
                    case MaterialTextureType.Roughness:
                        Roughness = guid;
                        break;
                    case MaterialTextureType.AmbientOcclusion:
                        AmbientOcclusion = guid;
                        break;
                }
            }
        }

        public bool HasTexture(MaterialTextureType textureType)
        {
            return textureType switch
            {
                MaterialTextureType.BaseColor => Albedo != Guid.Empty,
                MaterialTextureType.Normals => Normal != Guid.Empty,
                MaterialTextureType.Emissive => Emissive != Guid.Empty,
                MaterialTextureType.Metallic => Metallic != Guid.Empty,
                MaterialTextureType.Roughness => Roughness != Guid.Empty,
                MaterialTextureType.AmbientOcclusion => AmbientOcclusion != Guid.Empty
            };
        }

        public bool IsDefault()
        {
            return BaseColor == null &&
                   Albedo == Guid.Empty &&
                   Normal == Guid.Empty &&
                   Emissive == Guid.Empty &&
                   Metallic == Guid.Empty &&
                   Roughness == Guid.Empty &&
                   AmbientOcclusion == Guid.Empty;
        }

        private void LoadTextureToMaterial(Material material, MaterialTextureType textureType, Guid guid)
        {
            Texture texture = AssetsManager.LoadAssetByGuid<Texture>(guid);
            switch (textureType)
            {
                case MaterialTextureType.BaseColor:
                    material.Albedo = texture;
                    break;
                case MaterialTextureType.Normals:
                    material.Normal = texture;
                    break;
                case MaterialTextureType.Emissive:
                    material.Emissive = texture;
                    break;
                case MaterialTextureType.Metallic:
                    material.Metallic = texture;
                    break;
                case MaterialTextureType.Roughness:
                    material.Roughness = texture;
                    break;
                case MaterialTextureType.AmbientOcclusion:
                    material.AmbientOcclusion = texture;
                    break;
            }
        }
    }

    public enum MaterialTextureType
    {
        Unknown = 0,
        BaseColor = 1,
        Normals = 2,
        Emissive = 3,
        Metallic = 4,
        Roughness = 5,
        AmbientOcclusion = 6
    }
}