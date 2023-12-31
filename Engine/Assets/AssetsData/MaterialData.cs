using System;
using System.Collections.Generic;
using System.IO;

namespace Engine.AssetsData
{
    public class MaterialData : AssetData
    {
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

            foreach (KeyValuePair<MaterialTextureType, Guid> texturePair in TexturesGuids)
            {
                LoadTextureToMaterial(material, texturePair.Key, texturePair.Value);
            }

            return material;
        }

        private void LoadTextureToMaterial(Material material, MaterialTextureType textureType, Guid guid)
        {
            Texture texture = AssetsManager.LoadAssetByGuid<Texture>(guid, typeof(TextureData));
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