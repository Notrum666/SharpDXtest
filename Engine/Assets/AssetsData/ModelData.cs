using System;
using System.Collections.Generic;
using System.IO;
using LinearAlgebra;

using YamlDotNet.Serialization;

namespace Engine.AssetsData
{
    [AssetData<Model>]
    public class ModelData : AssetData
    {
        public Guid SkeletonGuid = Guid.Empty;

        public Dictionary<string, Guid> EmbeddedTexturesGuids = new Dictionary<string, Guid>();

        public List<Guid> MaterialsGuids = new List<Guid>();

        public List<MeshData> Meshes = new List<MeshData>();

        public void AddEmbeddedTexture(string fileName, Guid guid)
        {
            EmbeddedTexturesGuids[fileName] = guid;
        }

        public Guid GetEmbeddedTexture(string fileName)
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

        public override Model ToRealAsset()
        {
            Model model = new Model();

            foreach (MeshData meshData in Meshes)
            {
                Mesh mesh = new Mesh();
                mesh.DefaultMaterial = AssetsManager.LoadAssetByGuid<Material>(meshData.Material);
                mesh.Skeleton = AssetsManager.LoadAssetByGuid<Skeleton>(meshData.Skeleton);

                foreach (VertexData vertexData in meshData.Vertices)
                {
                    Mesh.PrimitiveVertex vertex = new Mesh.PrimitiveVertex
                    {
                        v = vertexData.Position,
                        n = vertexData.Normal,
                        t = vertexData.Texture
                    };
                    if (vertexData.BoneWeights.Count > 0)
                    {
                        vertex.bones.x = vertexData.BoneIndices[0];
                        vertex.weights.x = vertexData.BoneWeights[0];
                    }
                    if (vertexData.BoneWeights.Count > 1)
                    {
                        vertex.bones.y = vertexData.BoneIndices[1];
                        vertex.weights.y = vertexData.BoneWeights[1];
                    }
                    if (vertexData.BoneWeights.Count > 2)
                    {
                        vertex.bones.z = vertexData.BoneIndices[2];
                        vertex.weights.z = vertexData.BoneWeights[2];
                    }
                    if (vertexData.BoneWeights.Count > 3)
                    {
                        vertex.bones.w = vertexData.BoneIndices[3];
                        vertex.weights.w = vertexData.BoneWeights[3];
                    }
                    mesh.Vertices.Add(vertex);
                }
                mesh.Indices.AddRange(meshData.Indices);

                mesh.GenerateGPUData();
                model.Meshes.Add(mesh);
            }

            return model;
        }
    }

    public class MeshData
    {
        public string Name;
        public Guid Material;
        public Guid Skeleton;

        public List<VertexData> Vertices = new List<VertexData>();
        public List<int> Indices = new List<int>();
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