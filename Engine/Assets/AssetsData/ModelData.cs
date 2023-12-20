using System;
using System.Collections.Generic;
using System.IO;
using LinearAlgebra;
using YamlDotNet.Serialization;

namespace Engine.AssetsData
{
    public class ModelData : AssetData
    {
        public string Skeleton = null;

        public Dictionary<string, string> EmbeddedTextures = new Dictionary<string, string>();

        public List<string> Materials = new List<string>();

        public List<MeshData> Meshes = new List<MeshData>();

        public void AddEmbeddedTexture(string fileName, string guid)
        {
            EmbeddedTextures[fileName] = guid;
        }

        public string GetEmbeddedTexture(string fileName)
        {
            return EmbeddedTextures[fileName];
        }

        public override void Serialize(BinaryWriter writer)
        {
            YamlManager.SaveToStream(writer.BaseStream, this);
        }

        public override void Deserialize(BinaryReader reader)
        {
            YamlManager.LoadFromStream(reader.BaseStream, this);
        }

        public override Mesh ToRealAsset(Type assetType)
        {
            if (assetType != typeof(Mesh))
                return null;

            Mesh mesh = new Mesh();

            foreach (MeshData meshData in Meshes)
            {
                Primitive primitive = new Primitive();
                primitive.DefaultMaterial = AssetsManager.LoadAssetByGuid<Material>(meshData.Material, typeof(MaterialData));

                primitive.vertices = new List<Primitive.PrimitiveVertex>();
                foreach (VertexData vertexData in meshData.Vertices)
                {
                    Primitive.PrimitiveVertex vertex = new Primitive.PrimitiveVertex
                    {
                        v = vertexData.Position,
                        n = vertexData.Normal,
                        t = vertexData.Texture
                    };
                    primitive.vertices.Add(vertex);
                }
                primitive.indices.AddRange(meshData.Indices);

                mesh.Primitives.Add(primitive);
            }

            return mesh;
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