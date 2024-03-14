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
        private Buffer octreePool;
        private UnorderedAccessView octreePoolUAV;
        private ShaderResourceView octreePoolSRV;
        //private Buffer octreeCounterRetrieveBuffer;

        private Buffer meshVerticesPool;
        private UnorderedAccessView meshVerticesPoolUAV;
        private ShaderResourceView meshVerticesPoolSRV;

        private Buffer tetrahedronsPool;
        private UnorderedAccessView tetrahedronsPoolUAV;
        private ShaderResourceView tetrahedronsPoolSRV;

        private Buffer tetrahedronsCounter;
        private UnorderedAccessView tetrahedronsCounterUAV;

        private bool needsToBeResized = true;

        private Shader octreeInitShader;
        private Shader octreeSubdivisionShader;
        private Shader verticesGenerationShader;
        private Shader verticesFetchingShader;
        private Shader tetrahedralizationShader;

        protected override void OnInitialized()
        {
            base.OnInitialized();

            // compute shaders
            octreeInitShader = AssetsManager.LoadAssetAtPath<Shader>(@"BaseAssets\Shaders\Volumetric\FEM_gas_octree_initialization.csh");
            octreeSubdivisionShader = AssetsManager.LoadAssetAtPath<Shader>(@"BaseAssets\Shaders\Volumetric\FEM_gas_octree_subdivision.csh");
            verticesGenerationShader = AssetsManager.LoadAssetAtPath<Shader>(@"BaseAssets\Shaders\Volumetric\FEM_gas_vertices_generation.csh");
            verticesFetchingShader = AssetsManager.LoadAssetAtPath<Shader>(@"BaseAssets\Shaders\Volumetric\FEM_gas_vertices_fetching.csh");
            tetrahedralizationShader = AssetsManager.LoadAssetAtPath<Shader>(@"BaseAssets\Shaders\Volumetric\FEM_gas_tetrahedralization.csh");

            // octree

            int octreePoolSize = 1 << 10;

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

            int meshVerticesPoolSize = 1 << 14;

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

            int tetrahedronsPoolSize = 1 << 14;

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
                CpuAccessFlags.Write, ResourceOptionFlags.BufferStructured, sizeof(int));

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

        private void InitOctree()
        {
            octreeInitShader.Use();
            GraphicsCore.CurrentDevice.ImmediateContext.ComputeShader.SetUnorderedAccessView(0, octreePoolUAV);
            GraphicsCore.CurrentDevice.ImmediateContext.Dispatch(1, 1, 1);

            octreeSubdivisionShader.Use();
            for (int i = 0; i < 3; i++)
                GraphicsCore.CurrentDevice.ImmediateContext.Dispatch(1, 1, 1);

            GraphicsCore.CurrentDevice.ImmediateContext.ComputeShader.SetUnorderedAccessView(0, null);
        }

        private void GenerateVertices()
        {
            verticesGenerationShader.Use();

            verticesGenerationShader.UpdateUniform("volumeHalfSize", size * 0.5f);
            verticesGenerationShader.UploadUpdatedUniforms();

            GraphicsCore.CurrentDevice.ImmediateContext.ComputeShader.SetUnorderedAccessViews(0, octreePoolUAV, meshVerticesPoolUAV);
            GraphicsCore.CurrentDevice.ImmediateContext.Dispatch(1, 1, 1);

            verticesFetchingShader.Use();

            for (int i = 0; i < 3; i++)
                GraphicsCore.CurrentDevice.ImmediateContext.Dispatch(1, 1, 1);

            GraphicsCore.CurrentDevice.ImmediateContext.ComputeShader.SetUnorderedAccessViews(0, null, (UnorderedAccessView)null);
        }

        private void Tetrahedralize()
        {
            tetrahedralizationShader.Use();
            GraphicsCore.CurrentDevice.ImmediateContext.ComputeShader.SetUnorderedAccessViews(0, 
                octreePoolUAV, meshVerticesPoolUAV, tetrahedronsPoolUAV, tetrahedronsCounterUAV);
            GraphicsCore.CurrentDevice.ImmediateContext.Dispatch(1, 1, 1);
            GraphicsCore.CurrentDevice.ImmediateContext.ComputeShader.SetUnorderedAccessViews(0, null, null, null, null);
        }
        
        public override void OnFieldChanged(FieldInfo fieldInfo)
        {
            base.OnFieldChanged(fieldInfo);

            if (fieldInfo.Name == nameof(size))
                needsToBeResized = true;
        }

        public override void Render()
        {
            if (needsToBeResized)
            {
                GenerateVertices();
                Tetrahedralize();
                needsToBeResized = false;
            }
            GraphicsCore.CurrentDevice.ImmediateContext.PixelShader.SetShaderResource(0, octreePoolSRV);
            GraphicsCore.CurrentDevice.ImmediateContext.PixelShader.SetShaderResource(1, meshVerticesPoolSRV);
            GraphicsCore.CurrentDevice.ImmediateContext.PixelShader.SetShaderResource(2, tetrahedronsPoolSRV);
            base.Render();
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
        }

        private struct Tetrahedron
        {
            public Vector4i indices;
            public Matrix4x4f alphaMatrix;
        }
    }
}