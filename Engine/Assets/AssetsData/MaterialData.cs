using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using LinearAlgebra;

using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace Engine.AssetsData
{
    [AssetData<Material>]
    public class MaterialData : NativeAssetData
    {
        public sealed override string FileExtension => ".mat";

        public Vector4f? BaseColor = null;
        public readonly Dictionary<MaterialTextureType, Guid> TexturesGuids = new Dictionary<MaterialTextureType, Guid>();

        protected sealed override void SetDefaultValues()
        {
            BaseColor = null;
            foreach (MaterialTextureType textureType in Enum.GetValues<MaterialTextureType>())
            {
                TexturesGuids[textureType] = Guid.Empty;
            }
        }
        
        public sealed override void Serialize(BinaryWriter writer)
        {
            YamlManager.SaveToStream(writer.BaseStream, this);
        }

        public sealed override void Deserialize(BinaryReader reader)
        {
            YamlManager.LoadFromStream(reader.BaseStream, this);
        }

        public override Material ToRealAsset()
        {
            Material material = Material.CreateDefault();

            foreach (KeyValuePair<MaterialTextureType, Guid> texturePair in TexturesGuids)
            {
                if (texturePair.Value != Guid.Empty)
                    LoadTextureToMaterial(material, texturePair.Key, texturePair.Value);
            }

            Guid albedoGuid = TexturesGuids.GetValueOrDefault(MaterialTextureType.BaseColor, Guid.Empty);
            if (albedoGuid == Guid.Empty && BaseColor != null)
            {
                material.Albedo = new Texture(64, 64, BaseColor?.GetBytes(), Format.R32G32B32A32_Float, BindFlags.ShaderResource);
            }

            return material;
        }

        public void AddTexture(MaterialTextureType textureType, Guid guid)
        {
            if (guid != Guid.Empty)
                TexturesGuids[textureType] = guid;
        }

        public bool HasTexture(MaterialTextureType textureType)
        {
            return TexturesGuids.ContainsKey(textureType);
        }

        public bool IsDefault()
        {
            return BaseColor == null && TexturesGuids.Values.All(x => x == Guid.Empty);
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