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
        private Scene aiCurrentScene; // external Assimp data -> ai* prefix
        private ModelData currentModelData;
        private AssetImportContext currentImportContext;
        private ModelImportSettings currentImportSettings;

        private SkeletonData currentSkeletonData = null;

        public class ModelImportSettings : BaseImportSettings
        {
            public Guid? SkeletonOverride = null;
            public Dictionary<string, Guid?> MeshMaterialsOverride = new Dictionary<string, Guid?>();
        }

        protected override void OnImportAsset(AssetImportContext importContext)
        {
            AssimpContext aiAssimpContext = new AssimpContext();
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

            aiCurrentScene = aiAssimpContext.ImportFileFromStream(importContext.DataStream, postProcessSteps);

            currentModelData = new ModelData();
            currentImportContext = importContext;
            currentImportSettings = importContext.GetImportSettings<ModelImportSettings>();

            ProcessScene();

            if (currentSkeletonData is not null) {
                Guid subGuid = currentImportContext.AddSubAsset("skeleton", currentSkeletonData);
                currentModelData.SkeletonGuid = subGuid;

                currentImportSettings.SkeletonOverride ??= subGuid;
                currentModelData.SkeletonGuid = currentImportSettings.SkeletonOverride ?? Guid.Empty;
            }
            importContext.AddMainAsset(currentModelData);
        }

        private void ProcessScene()
        {
            ProcessEmbeddedTextures();
            ProcessMaterials();

            BuildSkeleton();
            ProcessNodeAsMeshHolder(aiCurrentScene.RootNode);
        }

        #region Appearance

        private void ProcessEmbeddedTextures()
        {
            if (aiCurrentScene.Textures == null)
                return;

            foreach (EmbeddedTexture embeddedTexture in aiCurrentScene.Textures)
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
                    throw new NotImplementedException();

                Guid subGuid = currentImportContext.AddSubAsset(textureName, textureData);
                currentModelData.AddEmbeddedTexture(textureName, subGuid);
            }
        }

        private void ProcessMaterials()
        {
            if (aiCurrentScene.Materials == null)
                return;

            string sourceAssetPath = currentImportContext.AssetSourcePath;
            string sourceAssetFolder = Path.GetDirectoryName(sourceAssetPath);

            foreach (Material material in aiCurrentScene.Materials)
                currentModelData.MaterialsGuids.Add(ProcessMaterial(material, sourceAssetFolder));
        }

        private Guid ProcessMaterial(Material material, string sourceAssetFolder)
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

                if (aiCurrentScene.GetEmbeddedTexture(relativeFilePath) != null)
                    materialData.AddTexture(materialTextureType, currentModelData.GetEmbeddedTexture(relativeFilePath));
                else
                {
                    while (relativeFilePath.StartsWith(@".\") || relativeFilePath.StartsWith(@"./"))
                        relativeFilePath = relativeFilePath.Substring(2);
                    string filePath = Path.Combine(sourceAssetFolder, relativeFilePath);
                    Guid? guid = AssetsRegistry.ImportAsset(filePath);
                    if (guid.HasValue)
                        materialData.AddTexture(materialTextureType, guid.Value);
                }
            }

            if (materialData.IsDefault())
                return Guid.Empty;

            string materialId = material.Name ?? $"mat_{aiCurrentScene.Materials.IndexOf(material)}";
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
            if (aiCurrentScene.HasMeshes && aiCurrentScene.Meshes[0].HasBones) {
                currentSkeletonData = new SkeletonData();
                currentSkeletonData.InverseRootTransform = ConvertMatrix(aiCurrentScene.RootNode.Transform);
                currentSkeletonData.InverseRootTransform.invert();
                ProcessNodeAsBone(aiCurrentScene.RootNode, -1);
                ProcessAnimations();
            }
        }

        private int ProcessNodeAsBone(Node currentNode, int parentBoneIndex)
        {
            int currentBoneIndex = currentSkeletonData.Bones.Count;

            Engine.Bone currentBone = new Engine.Bone
            {
                Name = currentNode.Name,
                // Transform = ConvertMatrix(currentNode.Transform),

                Index = currentBoneIndex,
                ParentIndex = parentBoneIndex
            };

            currentSkeletonData.Bones.Add(currentBone);

            foreach (Node childNode in currentNode.Children)
            {
                int childBoneIndex = ProcessNodeAsBone(childNode, currentBoneIndex);
                currentBone.ChildIndices.Add(childBoneIndex);
            }

            return currentBoneIndex;
        }

        private void ProcessAnimations()
        {
            foreach (Animation aiAnimation in aiCurrentScene.Animations)
            {
                AnimationData animationData = new AnimationData();
                animationData.Name = aiAnimation.Name;
                animationData.DurationInTicks = (float)aiAnimation.DurationInTicks;
                animationData.TickPerSecond = (float)aiAnimation.TicksPerSecond;
                foreach (NodeAnimationChannel aiAnimChannel in aiAnimation.NodeAnimationChannels)
                {
                    Engine.AnimationChannel animationChannel = new Engine.AnimationChannel();
                    foreach (VectorKey aiScalingKey in aiAnimChannel.ScalingKeys)
                    {
                        Engine.AnimationChannel.ScalingKey scalingKey = new Engine.AnimationChannel.ScalingKey();
                        scalingKey.Time = (float)aiScalingKey.Time;
                        scalingKey.Scaling = new Vector3f(aiScalingKey.Value.X, aiScalingKey.Value.Y, aiScalingKey.Value.Z);
                        animationChannel.ScalingKeys.Add(scalingKey);
                    }
                    foreach (VectorKey aiPositionKey in aiAnimChannel.PositionKeys)
                    {
                        Engine.AnimationChannel.PositionKey positionKey = new Engine.AnimationChannel.PositionKey();
                        positionKey.Time = (float)aiPositionKey.Time;
                        positionKey.Position = new Vector3f(aiPositionKey.Value.X, aiPositionKey.Value.Y, aiPositionKey.Value.Z);
                        animationChannel.PositionKeys.Add(positionKey);
                    }
                    foreach (QuaternionKey aiRotationKey in aiAnimChannel.RotationKeys)
                    {
                        Engine.AnimationChannel.RotationKey rotationKey = new Engine.AnimationChannel.RotationKey();
                        rotationKey.Time = (float)aiRotationKey.Time;
                        rotationKey.Rotation = new LinearAlgebra.Quaternion(aiRotationKey.Value.W, aiRotationKey.Value.X, aiRotationKey.Value.Y, aiRotationKey.Value.Z);
                        animationChannel.RotationKeys.Add(rotationKey);
                    }
                    animationChannel.BoneName = aiAnimChannel.NodeName;
                    animationData.Channels.Add(animationChannel);
                }

                Guid subGuid = currentImportContext.AddSubAsset(animationData.Name, animationData);
                currentSkeletonData.AnimationData.Add(subGuid);
            }
        }

        private void ProcessNodeAsMeshHolder(Node currentNode)
        {
            foreach (int meshIndex in currentNode.MeshIndices)
                ProcessMesh(aiCurrentScene.Meshes[meshIndex], currentNode.Name, meshIndex);

            foreach (Node childNode in currentNode.Children)
                ProcessNodeAsMeshHolder(childNode);
        }

        private void ProcessMesh(Mesh mesh, string nodeName, int meshIndex)
        {
            MeshData meshData = null;
            string meshName = $"{nodeName}_{mesh.Name}_{meshIndex}";
            foreach (MeshData loadedMeshData in currentModelData.Meshes)
                if (loadedMeshData.Name == meshName) {
                    meshData = loadedMeshData;
                    break;
                }
            if (meshData is null)
                meshData = new MeshData
                {
                    Name = meshName
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
                for (int i = 0; i < vertexCount; i++)
                {
                    Vector3D aiNorm = mesh.Normals[i];
                    vertices[i].Normal = new Vector3f(aiNorm.X, aiNorm.Y, aiNorm.Z);
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
                if (face.IndexCount == 3)
                    meshData.Indices.AddRange(face.Indices);
                else
                    throw new Exception("mesh face was not triangulated");

            if (currentSkeletonData is not null)
                foreach (Assimp.Bone bone in mesh.Bones)
                    for (int boneIndex = 0; boneIndex < currentSkeletonData.Bones.Count; ++boneIndex)
                        if (bone.Name == currentSkeletonData.Bones[boneIndex].Name)
                        {
                            currentSkeletonData.Bones[boneIndex].Offset = ConvertMatrix(bone.OffsetMatrix);

                            foreach (VertexWeight vertexWeight in bone.VertexWeights)
                            {
                                VertexData vertex = meshData.Vertices[vertexWeight.VertexID];

                                if (vertexWeight.Weight != 0)
                                {
                                    vertex.BoneIndices.Add(boneIndex);
                                    vertex.BoneWeights.Add(vertexWeight.Weight);
                                }

                                if (vertex.BoneIndices.Count > 4) //TODO: Extract to settings?
                                    throw new Exception("vertex bone indices count is greater than 4");
                            }
                            break;
                        }

            currentModelData.Meshes.Add(meshData);
        }

        #endregion Geometry

        private static Matrix4x4f ConvertMatrix(Assimp.Matrix4x4 sourceMatrix)
        {
            nint ptr = Marshal.AllocHGlobal(Marshal.SizeOf(sourceMatrix));
            Marshal.StructureToPtr(sourceMatrix, ptr, false);
            Matrix4x4f matrix = Marshal.PtrToStructure<Matrix4x4f>(ptr);
            Marshal.FreeHGlobal(ptr);

            return matrix;
        }
    }
}