using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

using LinearAlgebra;

using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace Engine.BaseAssets.Components
{
    public class FEMGasVolume : GasVolume
    {
        [SerializedField]
        private bool showOctree = false;
        [SerializedField]
        private bool showTetrahedrons = false;

        public bool ShowOctree
        {
            get => showOctree;
            set => showOctree = value;
        }
        public bool ShowTetrahedrons
        {
            get => showTetrahedrons;
            set => showTetrahedrons = value;
        }

        private int octreePoolSize;
        private Buffer octreePool;
        private UnorderedAccessView octreePoolUAV;
        private ShaderResourceView octreePoolSRV;
        //private Buffer octreeCounterRetrieveBuffer;

        private int meshVerticesPoolSize;
        private Buffer meshVerticesPool;
        private UnorderedAccessView meshVerticesPoolUAV;
        private ShaderResourceView meshVerticesPoolSRV;

        private int tetrahedronsPoolSize;
        private Buffer tetrahedronsPool;
        private UnorderedAccessView tetrahedronsPoolUAV;
        private ShaderResourceView tetrahedronsPoolSRV;

        private Buffer tetrahedronsCounter;
        private UnorderedAccessView tetrahedronsCounterUAV;

        private Buffer counterRetrieveBuffer;

        private bool needsToBeResized = true;

        private Shader verticesInitShader;
        private Shader octreeInitShader;
        private Shader octreeSubdivisionShader;
        private Shader verticesGenerationShader;
        private Shader verticesInheritanceShader;
        private Shader verticesFetchingShader;
        private Shader tetrahedralizationShader;
        private Shader simulationStepShader;
        private Shader flipVerticesDataShader;

        [SerializedField]
        private int Subdivisions = 1;

        protected override void OnInitialized()
        {
            base.OnInitialized();

            // compute shaders
            verticesInitShader = AssetsManager.LoadAssetAtPath<Shader>(@"BaseAssets\Shaders\Volumetric\FEM_gas_vertices_initialization.csh");
            octreeInitShader = AssetsManager.LoadAssetAtPath<Shader>(@"BaseAssets\Shaders\Volumetric\FEM_gas_octree_initialization.csh");
            octreeSubdivisionShader = AssetsManager.LoadAssetAtPath<Shader>(@"BaseAssets\Shaders\Volumetric\FEM_gas_octree_subdivision.csh");
            verticesGenerationShader = AssetsManager.LoadAssetAtPath<Shader>(@"BaseAssets\Shaders\Volumetric\FEM_gas_vertices_generation.csh");
            verticesInheritanceShader = AssetsManager.LoadAssetAtPath<Shader>(@"BaseAssets\Shaders\Volumetric\FEM_gas_vertices_inheritance.csh");
            verticesFetchingShader = AssetsManager.LoadAssetAtPath<Shader>(@"BaseAssets\Shaders\Volumetric\FEM_gas_vertices_fetching.csh");
            tetrahedralizationShader = AssetsManager.LoadAssetAtPath<Shader>(@"BaseAssets\Shaders\Volumetric\FEM_gas_tetrahedralization.csh");
            simulationStepShader = AssetsManager.LoadAssetAtPath<Shader>(@"BaseAssets\Shaders\Volumetric\FEM_gas_simulation_step.csh");
            flipVerticesDataShader = AssetsManager.LoadAssetAtPath<Shader>(@"BaseAssets\Shaders\Volumetric\FEM_gas_flip_vertices_data.csh");

            // counter retrieve buffer

            counterRetrieveBuffer = new Buffer(GraphicsCore.CurrentDevice, sizeof(uint), ResourceUsage.Staging, BindFlags.None, CpuAccessFlags.Read, ResourceOptionFlags.None, 0); // In case of error - move back to constructor

            // octree

            octreePoolSize = 1 << 19;

            int octreeNodeStructureSize = Marshal.SizeOf(typeof(OctreeNode));
            octreePool = new Buffer(GraphicsCore.CurrentDevice, octreeNodeStructureSize * octreePoolSize, 
                ResourceUsage.Default, BindFlags.ShaderResource | BindFlags.UnorderedAccess, 
                CpuAccessFlags.None, ResourceOptionFlags.BufferStructured, octreeNodeStructureSize);

            octreePoolUAV = new UnorderedAccessView(GraphicsCore.CurrentDevice, octreePool, new UnorderedAccessViewDescription()
            {
                Dimension = UnorderedAccessViewDimension.Buffer,
                Format = Format.Unknown,
                Buffer = new UnorderedAccessViewDescription.BufferResource()
                {
                    FirstElement = 0,
                    ElementCount = octreePoolSize,
                    Flags = UnorderedAccessViewBufferFlags.Counter
                }
            });

            octreePoolSRV = new ShaderResourceView(GraphicsCore.CurrentDevice, octreePool, new ShaderResourceViewDescription()
            {
                Dimension = ShaderResourceViewDimension.Buffer,
                Format = Format.Unknown,
                Buffer = new ShaderResourceViewDescription.BufferResource()
                {
                    FirstElement = 0,
                    ElementCount = octreePoolSize
                }
            });

            // mesh vertices

            meshVerticesPoolSize = 1 << 20;

            int meshVertexSize = Marshal.SizeOf(typeof(MeshVertex));
            meshVerticesPool = new Buffer(GraphicsCore.CurrentDevice, meshVertexSize * meshVerticesPoolSize,
                ResourceUsage.Default, BindFlags.ShaderResource | BindFlags.UnorderedAccess,
                CpuAccessFlags.Write, ResourceOptionFlags.BufferStructured, meshVertexSize);

            meshVerticesPoolUAV = new UnorderedAccessView(GraphicsCore.CurrentDevice, meshVerticesPool, new UnorderedAccessViewDescription()
            {
                Dimension = UnorderedAccessViewDimension.Buffer,
                Format = Format.Unknown,
                Buffer = new UnorderedAccessViewDescription.BufferResource()
                {
                    FirstElement = 0,
                    ElementCount = meshVerticesPoolSize,
                    Flags = UnorderedAccessViewBufferFlags.Counter
                }
            });

            meshVerticesPoolSRV = new ShaderResourceView(GraphicsCore.CurrentDevice, meshVerticesPool, new ShaderResourceViewDescription()
            {
                Dimension = ShaderResourceViewDimension.Buffer,
                Format = Format.Unknown,
                Buffer = new ShaderResourceViewDescription.BufferResource()
                {
                    FirstElement = 0,
                    ElementCount = meshVerticesPoolSize
                }
            });

            // tetrahedrons

            tetrahedronsPoolSize = 1 << 22;

            int tetrahedronSize = Marshal.SizeOf(typeof(Tetrahedron));
            tetrahedronsPool = new Buffer(GraphicsCore.CurrentDevice, tetrahedronSize * tetrahedronsPoolSize,
                ResourceUsage.Default, BindFlags.ShaderResource | BindFlags.UnorderedAccess,
                CpuAccessFlags.Write, ResourceOptionFlags.BufferStructured, tetrahedronSize);

            tetrahedronsPoolUAV = new UnorderedAccessView(GraphicsCore.CurrentDevice, tetrahedronsPool, new UnorderedAccessViewDescription()
            {
                Dimension = UnorderedAccessViewDimension.Buffer,
                Format = Format.Unknown,
                Buffer = new UnorderedAccessViewDescription.BufferResource()
                {
                    FirstElement = 0,
                    ElementCount = tetrahedronsPoolSize,
                    Flags = UnorderedAccessViewBufferFlags.None
                }
            });

            tetrahedronsPoolSRV = new ShaderResourceView(GraphicsCore.CurrentDevice, tetrahedronsPool, new ShaderResourceViewDescription()
            {
                Dimension = ShaderResourceViewDimension.Buffer,
                Format = Format.Unknown,
                Buffer = new ShaderResourceViewDescription.BufferResource()
                {
                    FirstElement = 0,
                    ElementCount = tetrahedronsPoolSize
                }
            });

            // tetrahedrons counter

            tetrahedronsCounter = new Buffer(GraphicsCore.CurrentDevice, sizeof(int),
                ResourceUsage.Default, BindFlags.UnorderedAccess,
                CpuAccessFlags.Read | CpuAccessFlags.Write, ResourceOptionFlags.BufferStructured, sizeof(int));

            tetrahedronsCounterUAV = new UnorderedAccessView(GraphicsCore.CurrentDevice, tetrahedronsCounter, new UnorderedAccessViewDescription()
            {
                Dimension = UnorderedAccessViewDimension.Buffer,
                Format = Format.Unknown,
                Buffer = new UnorderedAccessViewDescription.BufferResource()
                {
                    FirstElement = 0,
                    ElementCount = 1,
                    Flags = UnorderedAccessViewBufferFlags.None
                }
            });

            // initialization

            InitOctree();
            GenerateVertices();
            Tetrahedralize();
        }

        private void ResetTetrahedronsCounter()
        {
            DataStream stream;
            GraphicsCore.CurrentDevice.ImmediateContext.MapSubresource(tetrahedronsCounter, MapMode.Write, SharpDX.Direct3D11.MapFlags.None, out stream);
            stream.Write<int>(0);
            GraphicsCore.CurrentDevice.ImmediateContext.UnmapSubresource(tetrahedronsCounter, 0);
        }

        private void InitOctree()
        {
            ResetTetrahedronsCounter();

            verticesInitShader.Use();
            GraphicsCore.CurrentDevice.ImmediateContext.ComputeShader.SetUnorderedAccessView(0, meshVerticesPoolUAV);
            GraphicsCore.CurrentDevice.ImmediateContext.Dispatch(meshVerticesPoolSize / 1024, 1, 1);

            octreeInitShader.Use();
            GraphicsCore.CurrentDevice.ImmediateContext.ComputeShader.SetUnorderedAccessView(0, octreePoolUAV);
            GraphicsCore.CurrentDevice.ImmediateContext.Dispatch(octreePoolSize / 1024, 1, 1);

            octreeSubdivisionShader.Use();
            for (int i = 0; i < Subdivisions; i++)
                GraphicsCore.CurrentDevice.ImmediateContext.Dispatch(octreePoolSize / 1024, 1, 1);

            GraphicsCore.CurrentDevice.ImmediateContext.ComputeShader.SetUnorderedAccessView(0, null);
        }

        private void GenerateVertices()
        {
            verticesGenerationShader.Use();

            verticesGenerationShader.UpdateUniform("volumeHalfSize", size * 0.5f);
            verticesGenerationShader.UploadUpdatedUniforms();

            GraphicsCore.CurrentDevice.ImmediateContext.ComputeShader.SetUnorderedAccessViews(0, octreePoolUAV, meshVerticesPoolUAV);
            GraphicsCore.CurrentDevice.ImmediateContext.Dispatch(octreePoolSize / 1024, 1, 1);

            verticesFetchingShader.Use();
            for (int i = 0; i < 3; i++)
                GraphicsCore.CurrentDevice.ImmediateContext.Dispatch(octreePoolSize / 1024, 1, 1);

            verticesInheritanceShader.Use();
            for (int i = 0; i < 16; i++)
                GraphicsCore.CurrentDevice.ImmediateContext.Dispatch(octreePoolSize / 1024, 1, 1);

            //verticesFetchingShader.Use();
            //for (int i = 0; i < 3; i++) // fetch vertices from smaller octants that have been fetched by their parents
            //    GraphicsCore.CurrentDevice.ImmediateContext.Dispatch(octreePoolSize / 1024, 1, 1);

            GraphicsCore.CurrentDevice.ImmediateContext.ComputeShader.SetUnorderedAccessViews(0, null, (UnorderedAccessView)null);
        }

        private void Tetrahedralize()
        {
            tetrahedralizationShader.Use();
            GraphicsCore.CurrentDevice.ImmediateContext.ComputeShader.SetUnorderedAccessViews(0, 
                octreePoolUAV, meshVerticesPoolUAV, tetrahedronsPoolUAV, tetrahedronsCounterUAV);
            GraphicsCore.CurrentDevice.ImmediateContext.Dispatch(octreePoolSize / 1024, 1, 1);
            GraphicsCore.CurrentDevice.ImmediateContext.ComputeShader.SetUnorderedAccessViews(0, null, null, null, null);
        }
        
        public override void OnFieldChanged(FieldInfo fieldInfo)
        {
            base.OnFieldChanged(fieldInfo);

            if (fieldInfo.Name == nameof(size))
                needsToBeResized = true;

            if (fieldInfo.Name == nameof(Subdivisions))
                needsToBeResized = true;
        }

        public override void FixedUpdate()
        {
            simulationStepShader.Use();
            GraphicsCore.CurrentDevice.ImmediateContext.ComputeShader.SetUnorderedAccessViews(0,
                octreePoolUAV, meshVerticesPoolUAV, tetrahedronsPoolUAV);

            Vector3f halfSize = Size * 0.5f;
            simulationStepShader.UpdateUniform("invHalfSize", (Vector3f)(1.0f / halfSize));
            simulationStepShader.UpdateUniform("deltaTime", (float)Time.FixedDeltaTime);
            simulationStepShader.UploadUpdatedUniforms();

            GraphicsCore.CurrentDevice.ImmediateContext.Dispatch(meshVerticesPoolSize / 1024, 1, 1);

            flipVerticesDataShader.Use();
            GraphicsCore.CurrentDevice.ImmediateContext.ComputeShader.SetUnorderedAccessViews(0, meshVerticesPoolUAV);

            GraphicsCore.CurrentDevice.ImmediateContext.Dispatch(meshVerticesPoolSize / 1024, 1, 1);

            GraphicsCore.CurrentDevice.ImmediateContext.ComputeShader.SetUnorderedAccessViews(0, null, null, null);
        }

        public override void Render()
        {
            if (needsToBeResized)
            {
                OnInitialized();
                needsToBeResized = false;
            }
            GraphicsCore.CurrentDevice.ImmediateContext.PixelShader.SetShaderResource(0, octreePoolSRV);
            GraphicsCore.CurrentDevice.ImmediateContext.PixelShader.SetShaderResource(1, meshVerticesPoolSRV);
            GraphicsCore.CurrentDevice.ImmediateContext.PixelShader.SetShaderResource(2, tetrahedronsPoolSRV);
            base.Render();
        }

        internal void RenderOctree()
        {
            if (needsToBeResized)
            {
                OnInitialized();
                needsToBeResized = false;
            }
            GraphicsCore.CurrentDevice.ImmediateContext.VertexShader.SetShaderResource(0, octreePoolSRV);
            GraphicsCore.CurrentDevice.ImmediateContext.VertexShader.SetShaderResource(1, meshVerticesPoolSRV);

            int octantsCount = RetrieveCounter(octreePoolUAV);
            GraphicsCore.CurrentDevice.ImmediateContext.Draw(octantsCount * 24, 0);
        }

        internal void RenderTetrahedrons()
        {
            if (needsToBeResized)
            {
                OnInitialized();
                needsToBeResized = false;
            }
            GraphicsCore.CurrentDevice.ImmediateContext.VertexShader.SetShaderResource(0, tetrahedronsPoolSRV);
            GraphicsCore.CurrentDevice.ImmediateContext.VertexShader.SetShaderResource(1, meshVerticesPoolSRV);

            int tetrahedronsCount = RetrieveCounterBufferValue(tetrahedronsCounter);
            GraphicsCore.CurrentDevice.ImmediateContext.Draw(tetrahedronsCount * 12, 0);
        }

        private int RetrieveCounter(UnorderedAccessView bufferUAV)
        {
            GraphicsCore.CurrentDevice.ImmediateContext.CopyStructureCount(counterRetrieveBuffer, 0, bufferUAV);
            return RetrieveCounterBufferValue(counterRetrieveBuffer);
        }

        private int RetrieveCounterBufferValue(Buffer buffer)
        {
            DataStream stream;
            GraphicsCore.CurrentDevice.ImmediateContext.MapSubresource(buffer, MapMode.Read, SharpDX.Direct3D11.MapFlags.None, out stream);
            uint amount = stream.Read<uint>();
            GraphicsCore.CurrentDevice.ImmediateContext.UnmapSubresource(buffer, 0);
            return (int)amount;
        }

        private struct OctreeNode
        {
            public Vector4i subLeavesBottom;
            public Vector4i subLeavesTop;
            public int parent;
            public int tetrahedronsStart, tetrahedronsEnd; // starting and ending indicies of tetrahedrons
            public int middleVertex;
            public Vector4i verticesBottom;
            public Vector4i verticesTop;
        }

        private struct MeshVertex
        {
            public Vector3f position;
            public float density;
            public Vector3f velocity;
            public float nextDensity;
        }

        private struct Tetrahedron
        {
            public Vector4i indices;
            public Matrix4x4f alphaMatrix;
        }
    }
}