using System;
using System.Collections.Generic;
using System.IO;

using LinearAlgebra;

using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace Engine.AssetsData
{
    [AssetData<Material>]
    public class MaterialData : AssetData
    {
        public Vector4f? BaseColor = null;
        public Dictionary<MaterialTextureType, Guid> TexturesGuids = new Dictionary<MaterialTextureType, Guid>();

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
            return TexturesGuids.Count == 0 && BaseColor == null;
        }

        public override void Serialize(BinaryWriter writer)
        {
            YamlManager.SaveToStream(writer.BaseStream, this);
        }

        public override void Deserialize(BinaryReader reader)
        {
            YamlManager.LoadFromStream(reader.BaseStream, this);
        }

        public override Material ToRealAsset()
        {
            Material material = new Material();

            if (TexturesGuids.Count != 0)
            {
                foreach (KeyValuePair<MaterialTextureType, Guid> texturePair in TexturesGuids)
                {
                    LoadTextureToMaterial(material, texturePair.Key, texturePair.Value);
                }
            }
            else if (BaseColor != null)
            {
                material.Albedo = new Texture(64, 64, BaseColor?.GetBytes(), Format.R32G32B32A32_Float, BindFlags.ShaderResource);
                material.Normal = Material.Default.Normal;
                material.Metallic = Material.Default.Metallic;
                material.Roughness = Material.Default.Roughness;
                material.AmbientOcclusion = Material.Default.AmbientOcclusion;
                material.Emissive = Material.Default.Emissive;
            }

            return material;
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