using System;
using System.Collections.Generic;
using System.IO;
using LinearAlgebra;
using YamlDotNet.Serialization;

namespace Engine.AssetsData
{
    public class ModelData : AssetData
    {
        public string SkeletonGuid = null;

        public Dictionary<string, string> EmbeddedTexturesGuids = new Dictionary<string, string>();

        public List<string> MaterialsGuids = new List<string>();

        public List<MeshData> Meshes = new List<MeshData>();

        public void AddEmbeddedTexture(string fileName, string guid)
        {
            EmbeddedTexturesGuids[fileName] = guid;
        }

        public string GetEmbeddedTexture(string fileName)
        {
            return EmbeddedTexturesGuids[fileName];
        }

        public override void Serialize(BinaryWriter writer)
        {
            YamlManager.SaveToStream(writer.BaseStream, this);
        }

        public override void Deserialize(BinaryReader reader)
        {
            YamlManager.LoadFromStream(reader.BaseStream, this);
        }

        public override Model ToRealAsset(Type assetType)
        {
            if (assetType != typeof(Model))
                return null;

            Model model = new Model();

            foreach (MeshData meshData in Meshes)
            {
                Mesh mesh = new Mesh();
                mesh.DefaultMaterial = AssetsManager.LoadAssetByGuid<Material>(meshData.Material, typeof(MaterialData));

                mesh.vertices = new List<Mesh.PrimitiveVertex>();
                foreach (VertexData vertexData in meshData.Vertices)
                {
                    Mesh.PrimitiveVertex vertex = new Mesh.PrimitiveVertex
                    {
                        v = vertexData.Position,
                        n = vertexData.Normal,
                        t = vertexData.Texture
                    };
                    mesh.vertices.Add(vertex);
                }
                mesh.indices.AddRange(meshData.Indices);

                model.Meshes.Add(mesh);
            }

            return model;
        }
    }

    public class MeshData
    {
        public string Name;
        public string Material;

        public List<VertexData> Vertices = new List<VertexData>();
        public List<int> Indices = new List<int>();

        [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
        public List<SkinnedBoneData> SkinnedBones = new List<SkinnedBoneData>();
    }

    public class SkinnedBoneData
    {
        public string Name;
        public Matrix4x4f Offset;
    }

    public class VertexData
    {
        public Vector3f Position;
        public Vector3f Normal;
        public Vector2f Texture;

        [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
        public List<int> BoneIndices = new List<int>();

        [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
        public List<float> BoneWeights = new List<float>();
    }
}