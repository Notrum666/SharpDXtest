using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Assimp;
using Engine.AssetsData;
using LinearAlgebra;

namespace Editor.AssetsImport
{
    [AssetImporter("fbx", "dae", "obj")]
    public class ModelImporter : AssetImporter
    {
        private Scene currentScene;
        private ModelData currentModelData;
        private AssetImportContext currentImportContext;
        private ModelImportSettings currentImportSettings;

        public class ModelImportSettings : BaseImportSettings
        {
            public Guid? SkeletonOverride = null;
            public Dictionary<string, Guid?> MeshMaterialsOverride = new Dictionary<string, Guid?>();
        }

        protected override void OnImportAsset(AssetImportContext importContext)
        {
            AssimpContext assimpContext = new AssimpContext();
            // assimpContext.SetConfig(new Assimp.Configs.MaxBoneCountConfig(72));
            // assimpContext.SetConfig(new Assimp.Configs.VertexBoneWeightLimitConfig(4));

            PostProcessSteps postProcessSteps = PostProcessSteps.FlipUVs
                                                | PostProcessSteps.FlipWindingOrder
                                                | PostProcessSteps.MakeLeftHanded
                                                | PostProcessSteps.Triangulate
                                                // | PostProcessSteps.PreTransformVertices
                                                | PostProcessSteps.GenerateNormals
                                                // | PostProcessSteps.FixInFacingNormals
                                                // | PostProcessSteps.SplitByBoneCount
                                                | PostProcessSteps.LimitBoneWeights;

            currentScene = assimpContext.ImportFileFromStream(importContext.DataStream, postProcessSteps);

            currentModelData = new ModelData();
            currentImportContext = importContext;
            currentImportSettings = importContext.GetImportSettings<ModelImportSettings>();

            ProcessScene();

            importContext.AddMainAsset(currentModelData);
        }

        private void ProcessScene()
        {
            ProcessEmbeddedTextures();
            ProcessMaterials();

            BuildSkeleton();
            ProcessNodeAsMeshHolder(currentScene.RootNode);
        }

        #region Appearance

        private void ProcessEmbeddedTextures()
        {
            if (currentScene.Textures == null)
                return;

            foreach (EmbeddedTexture embeddedTexture in currentScene.Textures)
            {
                string textureName = embeddedTexture.Filename;
                TextureData textureData;

                if (embeddedTexture.IsCompressed)
                {
                    string extension = embeddedTexture.CompressedFormatHint; //TODO?
                    using MemoryStream memoryStream = new MemoryStream(embeddedTexture.CompressedData);
                    textureData = TextureImporter.DecodeData(memoryStream, new TextureImporter.TextureImportSettings());
                }
                else
                {
                    throw new NotImplementedException();
                }

                Guid subGuid = currentImportContext.AddSubAsset(textureName, textureData);
                currentModelData.AddEmbeddedTexture(textureName, subGuid);
            }
        }

        private void ProcessMaterials()
        {
            if (currentScene.Materials == null)
                return;

            string sourceAssetPath = currentImportContext.AssetSourcePath;
            string sourceAssetFolder = Path.GetDirectoryName(sourceAssetPath);

            foreach (Assimp.Material material in currentScene.Materials)
            {
                currentModelData.MaterialsGuids.Add(ProcessMaterial(material, sourceAssetFolder));
            }
        }

        private Guid ProcessMaterial(Assimp.Material material, string sourceAssetFolder)
        {
            MaterialData materialData = new MaterialData();

            if (material.HasColorDiffuse)
            {
                Color4D color = material.ColorDiffuse;
                materialData.BaseColor = new Vector4f(color.R, color.G, color.B, color.A);
            }
            
            foreach (TextureType textureType in Enum.GetValues<TextureType>())
            {
                MaterialTextureType materialTextureType = ConvertTextureType(textureType);
                if (materialTextureType == MaterialTextureType.Unknown)
                    continue;

                if (materialData.HasTexture(materialTextureType) || !material.GetMaterialTexture(textureType, 0, out TextureSlot textureSlot))
                    continue;

                string relativeFilePath = textureSlot.FilePath;

                if (currentScene.GetEmbeddedTexture(relativeFilePath) != null)
                    materialData.AddTexture(materialTextureType, currentModelData.GetEmbeddedTexture(relativeFilePath));
                else
                {
                    while (relativeFilePath.StartsWith(@".\") || relativeFilePath.StartsWith(@"./"))
                    {
                        relativeFilePath = relativeFilePath.Substring(2);
                    }
                    string filePath = Path.Combine(sourceAssetFolder, relativeFilePath);
                    Guid? guid = AssetsRegistry.ImportAsset(filePath);
                    if (guid.HasValue)
                        materialData.AddTexture(materialTextureType, guid.Value);
                }
            }

            if (materialData.IsDefault())
                return Guid.Empty;

            string materialId = material.Name ?? $"mat_{currentScene.Materials.IndexOf(material)}";
            Guid subGuid = currentImportContext.AddSubAsset(materialId, materialData);

            return subGuid;
        }

        private static MaterialTextureType ConvertTextureType(TextureType assimpTextureType)
        {
            switch (assimpTextureType)
            {
                case TextureType.Diffuse or TextureType.BaseColor:
                    return MaterialTextureType.BaseColor;
                case TextureType.Normals or TextureType.NormalCamera:
                    return MaterialTextureType.Normals;
                case TextureType.Emissive or TextureType.EmissionColor:
                    return MaterialTextureType.Emissive;
                case TextureType.Lightmap or TextureType.AmbientOcclusion:
                    return MaterialTextureType.AmbientOcclusion;
                case TextureType.Metalness:
                    return MaterialTextureType.Metallic;
                case TextureType.Roughness:
                    return MaterialTextureType.Roughness;
                default:
                    return MaterialTextureType.Unknown;
            }
        }

        #endregion Appearance

        #region Geometry

        private void BuildSkeleton()
        {
            SkeletonData skeleton = new SkeletonData();
            ProcessNodeAsBone(currentScene.RootNode, -1, skeleton);
            Guid subGuid = currentImportContext.AddSubAsset("Skeleton", skeleton);

            currentImportSettings.SkeletonOverride ??= subGuid;
            currentModelData.SkeletonGuid = currentImportSettings.SkeletonOverride ?? Guid.Empty;
        }

        private int ProcessNodeAsBone(Node currentNode, int parentBoneIndex, SkeletonData skeleton)
        {
            int currentBoneIndex = skeleton.Bones.Count;

            BoneData currentBone = new BoneData
            {
                Name = currentNode.Name,
                Transform = ConvertMatrix(currentNode.Transform),

                Index = currentBoneIndex,
                ParentIndex = parentBoneIndex
            };

            skeleton.Bones.Add(currentBone);

            foreach (Node childNode in currentNode.Children)
            {
                int childBoneIndex = ProcessNodeAsBone(childNode, currentBoneIndex, skeleton);
                currentBone.ChildIndices.Add(childBoneIndex);
            }

            return currentBoneIndex;
        }

        private void ProcessNodeAsMeshHolder(Node currentNode)
        {
            foreach (int meshIndex in currentNode.MeshIndices)
            {
                ProcessMesh(currentScene.Meshes[meshIndex], currentNode.Name, meshIndex);
            }

            foreach (Node childNode in currentNode.Children)
            {
                ProcessNodeAsMeshHolder(childNode);
            }
        }

        private void ProcessMesh(Mesh mesh, string nodeName, int meshIndex)
        {
            MeshData meshData = new MeshData
            {
                Name = $"{nodeName}_{mesh.Name}_{meshIndex}"
            };

            if (!currentImportSettings.MeshMaterialsOverride.ContainsKey(meshData.Name))
                currentImportSettings.MeshMaterialsOverride[meshData.Name] = null;
            currentImportSettings.MeshMaterialsOverride[meshData.Name] ??= currentModelData.MaterialsGuids.ElementAtOrDefault(mesh.MaterialIndex);
            meshData.Material = currentImportSettings.MeshMaterialsOverride[meshData.Name] ?? Guid.Empty;

            int vertexCount = mesh.VertexCount;
            List<VertexData> vertices = meshData.Vertices;
            for (int i = 0; i < vertexCount; i++)
            {
                vertices.Add(new VertexData());
                Vector3D aiPos = mesh.Vertices[i];
                vertices[i].Position = new Vector3f(aiPos.X, aiPos.Y, aiPos.Z);
            }

            if (mesh.HasNormals)
            {
                for (int i = 0; i < vertexCount; i++)
                {
                    Vector3D aiNorm = mesh.Normals[i];
                    vertices[i].Normal = new Vector3f(aiNorm.X, aiNorm.Y, aiNorm.Z);
                }
            }

            if (mesh.HasTextureCoords(0))
            {
                List<Vector3D> textureCoords = mesh.TextureCoordinateChannels[0];
                for (int i = 0; i < vertexCount; i++)
                {
                    Vector3D uv = textureCoords[i];
                    vertices[i].Texture = new Vector2f(uv.X, uv.Y);
                }
            }

            foreach (Face face in mesh.Faces)
            {
                if (face.IndexCount == 3)
                    meshData.Indices.AddRange(face.Indices);
            }

            foreach (Bone bone in mesh.Bones)
            {
                ProcessBone(meshData, bone);
            }

            currentModelData.Meshes.Add(meshData);
        }

        private void ProcessBone(MeshData meshData, Bone bone)
        {
            SkinnedBoneData skinnedBone = new SkinnedBoneData
            {
                Name = bone.Name,
                Offset = ConvertMatrix(bone.OffsetMatrix)
            };
            meshData.SkinnedBones.Add(skinnedBone);

            int boneIndex = meshData.SkinnedBones.Count - 1;

            foreach (VertexWeight vertexWeight in bone.VertexWeights)
            {
                int vertexIndex = vertexWeight.VertexID;
                VertexData vertex = meshData.Vertices[vertexIndex];

                if (vertex.BoneIndices.Count >= 4) //TODO: Extract to settings?
                    continue;

                vertex.BoneIndices.Add(boneIndex);
                vertex.BoneWeights.Add(vertexWeight.Weight);
            }
        }

        #endregion Geometry

        private static Matrix4x4f ConvertMatrix(Assimp.Matrix4x4 sourceMatrix)
        {
            nint ptr = Marshal.AllocHGlobal(Marshal.SizeOf(sourceMatrix));
            Marshal.StructureToPtr(sourceMatrix, ptr, false);
            Matrix4x4f matrix = Marshal.PtrToStructure<Matrix4x4f>(ptr);
            Marshal.FreeHGlobal(ptr);

            matrix.transpose();
            return matrix;
        }
    }
}