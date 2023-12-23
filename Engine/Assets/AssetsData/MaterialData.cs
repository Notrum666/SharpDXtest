using System;
using System.Collections.Generic;
using System.IO;

namespace Engine.AssetsData
{
    public class MaterialData : AssetData
    {
        public Dictionary<MaterialTextureType, string> TexturesGuids = new Dictionary<MaterialTextureType, string>();

        public void AddTexture(MaterialTextureType textureType, string guid)
        {
            if (!string.IsNullOrEmpty(guid))
                TexturesGuids[textureType] = guid;
        }

        public bool HasTexture(MaterialTextureType textureType)
        {
            return TexturesGuids.ContainsKey(textureType);
        }

        public bool IsDefault()
        {
            return TexturesGuids.Count == 0;
        }

        public override void Serialize(BinaryWriter writer)
        {
            YamlManager.SaveToStream(writer.BaseStream, this);
        }

        public override void Deserialize(BinaryReader reader)
        {
            YamlManager.LoadFromStream(reader.BaseStream, this);
        }

        public override Material ToRealAsset(Type assetType)
        {
            if (assetType != typeof(Material))
                return null;

            Material material = new Material();

            foreach (KeyValuePair<MaterialTextureType, string> texturePair in TexturesGuids)
            {
                LoadTextureToMaterial(material, texturePair.Key, texturePair.Value);
            }

            return material;
        }

        private void LoadTextureToMaterial(Material material, MaterialTextureType textureType, string guid)
        {
            switch (textureType)
            {
                case MaterialTextureType.BaseColor:
                    material.Albedo = AssetsManager.LoadAssetByGuid<Texture>(guid, typeof(TextureData));
                    break;
                case MaterialTextureType.Normals:
                    material.Normal = AssetsManager.LoadAssetByGuid<Texture>(guid, typeof(TextureData));
                    break;
                case MaterialTextureType.Emissive:
                    material.Emissive = AssetsManager.LoadAssetByGuid<Texture>(guid, typeof(TextureData));
                    break;
                case MaterialTextureType.Metallic:
                    material.Metallic = AssetsManager.LoadAssetByGuid<Texture>(guid, typeof(TextureData));
                    break;
                case MaterialTextureType.Roughness:
                    material.Roughness = AssetsManager.LoadAssetByGuid<Texture>(guid, typeof(TextureData));
                    break;
                case MaterialTextureType.AmbientOcclusion:
                    material.AmbientOcclusion = AssetsManager.LoadAssetByGuid<Texture>(guid, typeof(TextureData));
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