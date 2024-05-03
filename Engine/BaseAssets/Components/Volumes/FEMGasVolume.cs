using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

using Assimp;

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

        private int meshVerticesPoolSize;
        private Buffer meshVerticesPool;
        private UnorderedAccessView meshVerticesPoolUAV;
        private ShaderResourceView meshVerticesPoolSRV;
        private Buffer freeMeshVerticesList;
        private UnorderedAccessView freeMeshVerticesListUAV;

        private int tetrahedronsPoolSize;
        private Buffer tetrahedronsPool;
        private UnorderedAccessView tetrahedronsPoolUAV;
        private ShaderResourceView tetrahedronsPoolSRV;
        private Buffer freeOctantsList;
        private UnorderedAccessView freeOctantsListUAV;

        private int operationListsSize;
        private Buffer octreeSubdivisionList;
        private UnorderedAccessView octreeSubdivisionListUAV;
        private Buffer octreeUnsubdivisionList;
        private UnorderedAccessView octreeUnsubdivisionListUAV;

        private Buffer tetrahedronsCounter;
        private UnorderedAccessView tetrahedronsCounterUAV;
        private Buffer toRetetrahedralizeList;
        private UnorderedAccessView toRetetrahedralizeListUAV;
        private Buffer tetrahedronsShiftLocation;
        private UnorderedAccessView tetrahedronsShiftLocationUAV;

        private Buffer counterRetrieveBuffer;

        private Vector3f curSize;

        private Shader allocationArrayInitializationShader;
        private Shader verticesInitShader;
        private Shader tetrahedronsInitShader;
        private Shader octreeInitShader;
        //private Shader octreeSubdivisionShader;
        //private Shader verticesGenerationShader;
        //private Shader verticesInheritanceShader;
        //private Shader verticesFetchingShader;
        private Shader tetrahedralizationShader;
        private Shader simulationStepShader;
        private Shader flipVerticesDataShader;

        private Shader operationListInitializationShader;
        private Shader adaptationCollectionShader;
        private Shader subdivisionOctantsShader;
        private Shader subdivisionVerticesShader;
        private Shader subdivisionRetetrahedralizationCollectionShader;
        private Shader unsubdivisionOctantsShader;
        private Shader unsubdivisionRetetrahedralizationCollectionShader;
        private Shader listedRetetrahedralizationShader;
        private Shader findTetrahedronsShiftLocationShader;
        private Shader shiftTetrahedronsShader;
        private Shader shiftOctantsTetrahedronsShader;

        private Shader scaleVerticesShader;

        protected override void OnInitialized()
        {
            base.OnInitialized();

            #region shaders
            allocationArrayInitializationShader = AssetsManager.LoadAssetAtPath<Shader>(@"BaseAssets\Shaders\Volumetric\FEM_gas_allocation_array_initialization.csh");
            verticesInitShader = AssetsManager.LoadAssetAtPath<Shader>(@"BaseAssets\Shaders\Volumetric\FEM_gas_vertices_initialization.csh");
            tetrahedronsInitShader = AssetsManager.LoadAssetAtPath<Shader>(@"BaseAssets\Shaders\Volumetric\FEM_gas_tetrahedrons_initialization.csh");
            octreeInitShader = AssetsManager.LoadAssetAtPath<Shader>(@"BaseAssets\Shaders\Volumetric\FEM_gas_octree_initialization.csh");
            //octreeSubdivisionShader = AssetsManager.LoadAssetAtPath<Shader>(@"BaseAssets\Shaders\Volumetric\FEM_gas_octree_subdivision.csh");
            //verticesGenerationShader = AssetsManager.LoadAssetAtPath<Shader>(@"BaseAssets\Shaders\Volumetric\FEM_gas_vertices_generation.csh");
            //verticesInheritanceShader = AssetsManager.LoadAssetAtPath<Shader>(@"BaseAssets\Shaders\Volumetric\FEM_gas_vertices_inheritance.csh");
            //verticesFetchingShader = AssetsManager.LoadAssetAtPath<Shader>(@"BaseAssets\Shaders\Volumetric\FEM_gas_vertices_fetching.csh");
            tetrahedralizationShader = AssetsManager.LoadAssetAtPath<Shader>(@"BaseAssets\Shaders\Volumetric\FEM_gas_tetrahedralization.csh");
            simulationStepShader = AssetsManager.LoadAssetAtPath<Shader>(@"BaseAssets\Shaders\Volumetric\FEM_gas_simulation_step.csh");
            flipVerticesDataShader = AssetsManager.LoadAssetAtPath<Shader>(@"BaseAssets\Shaders\Volumetric\FEM_gas_flip_vertices_data.csh");

            operationListInitializationShader = AssetsManager.LoadAssetAtPath<Shader>(@"BaseAssets\Shaders\Volumetric\FEM_gas_operation_list_initialization.csh");
            adaptationCollectionShader = AssetsManager.LoadAssetAtPath<Shader>(@"BaseAssets\Shaders\Volumetric\FEM_gas_adaptation_collection.csh");

            subdivisionOctantsShader = AssetsManager.LoadAssetAtPath<Shader>(@"BaseAssets\Shaders\Volumetric\FEM_gas_subdivision_octants.csh");
            subdivisionVerticesShader = AssetsManager.LoadAssetAtPath<Shader>(@"BaseAssets\Shaders\Volumetric\FEM_gas_subdivision_vertices.csh");
            subdivisionRetetrahedralizationCollectionShader = AssetsManager.LoadAssetAtPath<Shader>(@"BaseAssets\Shaders\Volumetric\FEM_gas_subdivision_retetrahedralization_collection.csh");

            unsubdivisionOctantsShader = AssetsManager.LoadAssetAtPath<Shader>(@"BaseAssets\Shaders\Volumetric\FEM_gas_unsubdivision_octants.csh");
            unsubdivisionRetetrahedralizationCollectionShader = AssetsManager.LoadAssetAtPath<Shader>(@"BaseAssets\Shaders\Volumetric\FEM_gas_unsubdivision_retetrahedralization_collection.csh");

            listedRetetrahedralizationShader = AssetsManager.LoadAssetAtPath<Shader>(@"BaseAssets\Shaders\Volumetric\FEM_gas_listed_retetrahedralization.csh");

            findTetrahedronsShiftLocationShader = AssetsManager.LoadAssetAtPath<Shader>(@"BaseAssets\Shaders\Volumetric\FEM_gas_find_tetrahedrons_shift_location.csh");
            shiftTetrahedronsShader = AssetsManager.LoadAssetAtPath<Shader>(@"BaseAssets\Shaders\Volumetric\FEM_gas_shift_tetrahedrons.csh");
            shiftOctantsTetrahedronsShader = AssetsManager.LoadAssetAtPath<Shader>(@"BaseAssets\Shaders\Volumetric\FEM_gas_shift_octants_tetrahedrons.csh");

            scaleVerticesShader = AssetsManager.LoadAssetAtPath<Shader>(@"BaseAssets\Shaders\Volumetric\FEM_gas_scale_vertices.csh");

            #endregion

            #region octree

            octreePoolSize = 1 << 19; // 19

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
                    Flags = UnorderedAccessViewBufferFlags.None
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

            freeOctantsList = new Buffer(GraphicsCore.CurrentDevice, sizeof(int) * octreePoolSize,
                ResourceUsage.Default, BindFlags.UnorderedAccess,
                CpuAccessFlags.None, ResourceOptionFlags.BufferStructured, sizeof(int));

            freeOctantsListUAV = new UnorderedAccessView(GraphicsCore.CurrentDevice, freeOctantsList, new UnorderedAccessViewDescription()
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

            #endregion
            #region mesh vertices

            meshVerticesPoolSize = 1 << 20; // 20

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
                    Flags = UnorderedAccessViewBufferFlags.None
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

            freeMeshVerticesList = new Buffer(GraphicsCore.CurrentDevice, sizeof(int) * meshVerticesPoolSize,
                ResourceUsage.Default, BindFlags.UnorderedAccess,
                CpuAccessFlags.None, ResourceOptionFlags.BufferStructured, sizeof(int));

            freeMeshVerticesListUAV = new UnorderedAccessView(GraphicsCore.CurrentDevice, freeMeshVerticesList, new UnorderedAccessViewDescription()
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

            #endregion
            #region tetrahedrons

            tetrahedronsPoolSize = 1 << 22; // 22

            int tetrahedronSize = Marshal.SizeOf(typeof(Tetrahedron));
            tetrahedronsPool = new Buffer(GraphicsCore.CurrentDevice, tetrahedronSize * tetrahedronsPoolSize,
                ResourceUsage.Default, BindFlags.ShaderResource | BindFlags.UnorderedAccess,
                CpuAccessFlags.None, ResourceOptionFlags.BufferStructured, tetrahedronSize);

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

            #endregion

            #region operations

            counterRetrieveBuffer = new Buffer(GraphicsCore.CurrentDevice, sizeof(uint), ResourceUsage.Staging, BindFlags.None, CpuAccessFlags.Read, ResourceOptionFlags.None, 0);

            operationListsSize = octreePoolSize >> 2;

            octreeSubdivisionList = new Buffer(GraphicsCore.CurrentDevice, operationListsSize * sizeof(int),
                ResourceUsage.Default, BindFlags.UnorderedAccess,
                CpuAccessFlags.None, ResourceOptionFlags.BufferStructured, sizeof(int));

            octreeSubdivisionListUAV = new UnorderedAccessView(GraphicsCore.CurrentDevice, octreeSubdivisionList, new UnorderedAccessViewDescription()
            {
                Dimension = UnorderedAccessViewDimension.Buffer,
                Format = Format.Unknown,
                Buffer = new UnorderedAccessViewDescription.BufferResource()
                {
                    FirstElement = 0,
                    ElementCount = operationListsSize,
                    Flags = UnorderedAccessViewBufferFlags.Counter
                }
            });

            octreeUnsubdivisionList = new Buffer(GraphicsCore.CurrentDevice, operationListsSize * sizeof(int),
                ResourceUsage.Default, BindFlags.UnorderedAccess,
                CpuAccessFlags.None, ResourceOptionFlags.BufferStructured, sizeof(int));

            octreeUnsubdivisionListUAV = new UnorderedAccessView(GraphicsCore.CurrentDevice, octreeUnsubdivisionList, new UnorderedAccessViewDescription()
            {
                Dimension = UnorderedAccessViewDimension.Buffer,
                Format = Format.Unknown,
                Buffer = new UnorderedAccessViewDescription.BufferResource()
                {
                    FirstElement = 0,
                    ElementCount = operationListsSize,
                    Flags = UnorderedAccessViewBufferFlags.Counter
                }
            });

            toRetetrahedralizeList = new Buffer(GraphicsCore.CurrentDevice, octreePoolSize * sizeof(int),
                ResourceUsage.Default, BindFlags.UnorderedAccess,
                CpuAccessFlags.None, ResourceOptionFlags.BufferStructured, sizeof(int));

            toRetetrahedralizeListUAV = new UnorderedAccessView(GraphicsCore.CurrentDevice, toRetetrahedralizeList, new UnorderedAccessViewDescription()
            {
                Dimension = UnorderedAccessViewDimension.Buffer,
                Format = Format.Unknown,
                Buffer = new UnorderedAccessViewDescription.BufferResource()
                {
                    FirstElement = 0,
                    ElementCount = octreePoolSize,
                    Flags = UnorderedAccessViewBufferFlags.None
                }
            });

            tetrahedronsShiftLocation = new Buffer(GraphicsCore.CurrentDevice, sizeof(int) * 2,
                ResourceUsage.Default, BindFlags.UnorderedAccess,
                CpuAccessFlags.Write | CpuAccessFlags.Read, ResourceOptionFlags.BufferStructured, sizeof(int));

            tetrahedronsShiftLocationUAV = new UnorderedAccessView(GraphicsCore.CurrentDevice, tetrahedronsShiftLocation, new UnorderedAccessViewDescription()
            {
                Dimension = UnorderedAccessViewDimension.Buffer,
                Format = Format.Unknown,
                Buffer = new UnorderedAccessViewDescription.BufferResource()
                {
                    FirstElement = 0,
                    ElementCount = 2,
                    Flags = UnorderedAccessViewBufferFlags.None
                }
            });

            #endregion

            // initialization

            InitVolume();

            GraphicsCore.CurrentDevice.ImmediateContext.ComputeShader.SetUnorderedAccessViews(0, null, null, null, null, null);
        }

        private void InitVertices()
        {
            verticesInitShader.Use();
            GraphicsCore.CurrentDevice.ImmediateContext.ComputeShader.SetUnorderedAccessView(0, meshVerticesPoolUAV);
            GraphicsCore.CurrentDevice.ImmediateContext.Dispatch(meshVerticesPoolSize / 1024, 1, 1);
        }

        private void InitTetrahedrons()
        {
            tetrahedronsInitShader.Use();
            GraphicsCore.CurrentDevice.ImmediateContext.ComputeShader.SetUnorderedAccessView(0, tetrahedronsPoolUAV);
            GraphicsCore.CurrentDevice.ImmediateContext.Dispatch(tetrahedronsPoolSize / 1024, 1, 1);
        }

        private void InitOctree()
        {
            octreeInitShader.Use();
            GraphicsCore.CurrentDevice.ImmediateContext.ComputeShader.SetUnorderedAccessViews(0, 
                octreePoolUAV, freeOctantsListUAV, meshVerticesPoolUAV, freeMeshVerticesListUAV);
            octreeInitShader.UpdateUniform("volumeHalfSize", size * 0.5f);
            octreeInitShader.UploadUpdatedUniforms();
            GraphicsCore.CurrentDevice.ImmediateContext.Dispatch(octreePoolSize / 1024, 1, 1);
            curSize = size;
        }

        private void InitAllocationArray(UnorderedAccessView array, int size)
        {
            allocationArrayInitializationShader.Use();
            GraphicsCore.CurrentDevice.ImmediateContext.ComputeShader.SetUnorderedAccessView(0, array, size);
            allocationArrayInitializationShader.UpdateUniform("arraySize", size);
            allocationArrayInitializationShader.UploadUpdatedUniforms();
            GraphicsCore.CurrentDevice.ImmediateContext.Dispatch(size / 1024, 1, 1);
        }

        private void InitVolume()
        {
            ResetOperationLists();
            InitAllocationArray(freeOctantsListUAV, octreePoolSize);
            InitAllocationArray(freeMeshVerticesListUAV, meshVerticesPoolSize);
            InitVertices();
            InitTetrahedrons();
            InitOctree();
            Tetrahedralize();
        }

        private void RescaleVertices()
        {
            scaleVerticesShader.Use();
            GraphicsCore.CurrentDevice.ImmediateContext.ComputeShader.SetUnorderedAccessView(0, meshVerticesPoolUAV);
            scaleVerticesShader.UpdateUniform("scaleFactor", size.compDiv(curSize));
            scaleVerticesShader.UploadUpdatedUniforms();
            GraphicsCore.CurrentDevice.ImmediateContext.Dispatch(meshVerticesPoolSize / 1024, 1, 1);
            curSize = size;
        }

        private void RescaleVolume()
        {
            if (size.equals(curSize))
                return;
            RescaleVertices();
            Retetrahedralize();
            GraphicsCore.CurrentDevice.ImmediateContext.ComputeShader.SetUnorderedAccessViews(0, null, null, null, null, null);
        }

        private void ResetTetrahedronsCounter()
        {
            DataStream stream;
            GraphicsCore.CurrentDevice.ImmediateContext.MapSubresource(tetrahedronsCounter, MapMode.Write, SharpDX.Direct3D11.MapFlags.None, out stream);
            stream.Write<int>(0);
            GraphicsCore.CurrentDevice.ImmediateContext.UnmapSubresource(tetrahedronsCounter, 0);
        }

        private void Tetrahedralize()
        {
            tetrahedralizationShader.Use();
            GraphicsCore.CurrentDevice.ImmediateContext.ComputeShader.SetUnorderedAccessViews(0,
                octreePoolUAV, meshVerticesPoolUAV, tetrahedronsPoolUAV, tetrahedronsCounterUAV);
            GraphicsCore.CurrentDevice.ImmediateContext.Dispatch(octreePoolSize / 1024, 1, 1);
        }

        private void Retetrahedralize()
        {
            ResetTetrahedronsCounter();
            Tetrahedralize();
        }

        private void ResetOperationLists()
        {
            operationListInitializationShader.Use();
            GraphicsCore.CurrentDevice.ImmediateContext.ComputeShader.SetUnorderedAccessView(0, octreeSubdivisionListUAV, 0);
            GraphicsCore.CurrentDevice.ImmediateContext.Dispatch(operationListsSize / 1024, 1, 1);
            GraphicsCore.CurrentDevice.ImmediateContext.ComputeShader.SetUnorderedAccessView(0, octreeUnsubdivisionListUAV, 0);
            GraphicsCore.CurrentDevice.ImmediateContext.Dispatch(operationListsSize / 1024, 1, 1);
            GraphicsCore.CurrentDevice.ImmediateContext.ComputeShader.SetUnorderedAccessView(0, toRetetrahedralizeListUAV, 0);
            GraphicsCore.CurrentDevice.ImmediateContext.Dispatch(octreePoolSize / 1024, 1, 1);
        }

        private void ResetTetrahedronsShiftLocation()
        {
            DataStream stream;
            GraphicsCore.CurrentDevice.ImmediateContext.MapSubresource(tetrahedronsShiftLocation, MapMode.Write, SharpDX.Direct3D11.MapFlags.None, out stream);
            stream.Write<int>(0);
            stream.Write<int>(0);
            GraphicsCore.CurrentDevice.ImmediateContext.UnmapSubresource(tetrahedronsShiftLocation, 0);
        }

        private void CollectAdaptationLists()
        {
            adaptationCollectionShader.Use();
            GraphicsCore.CurrentDevice.ImmediateContext.ComputeShader.SetUnorderedAccessViews(0,
                octreePoolUAV, tetrahedronsPoolUAV, meshVerticesPoolUAV, octreeSubdivisionListUAV, octreeUnsubdivisionListUAV);
            GraphicsCore.CurrentDevice.ImmediateContext.Dispatch(octreePoolSize / 1024, 1, 1);
        }

        private void SubdivideCollectedOctants()
        {
            subdivisionOctantsShader.Use();
            GraphicsCore.CurrentDevice.ImmediateContext.ComputeShader.SetUnorderedAccessViews(0,
                octreePoolUAV, freeOctantsListUAV, octreeSubdivisionListUAV);
            GraphicsCore.CurrentDevice.ImmediateContext.Dispatch(operationListsSize / 1024, 1, 1);
        }

        private void GenerateVerticesForSubdividedOctants()
        {
            subdivisionVerticesShader.Use();
            subdivisionVerticesShader.UpdateUniform("volumeHalfSize", size * 0.5f);
            subdivisionVerticesShader.UploadUpdatedUniforms();
            GraphicsCore.CurrentDevice.ImmediateContext.ComputeShader.SetUnorderedAccessViews(0,
                octreePoolUAV, tetrahedronsPoolUAV, meshVerticesPoolUAV, freeMeshVerticesListUAV, octreeSubdivisionListUAV);
            GraphicsCore.CurrentDevice.ImmediateContext.Dispatch(operationListsSize / 1024, 1, 1);
            GraphicsCore.CurrentDevice.ImmediateContext.Dispatch(operationListsSize / 1024, 1, 1);
        }

        private void CollectSubdividedOctantsForRetetrahedralization()
        {
            subdivisionRetetrahedralizationCollectionShader.Use();
            GraphicsCore.CurrentDevice.ImmediateContext.ComputeShader.SetUnorderedAccessViews(0,
                octreePoolUAV, tetrahedronsPoolUAV, octreeSubdivisionListUAV, toRetetrahedralizeListUAV);
            GraphicsCore.CurrentDevice.ImmediateContext.Dispatch(operationListsSize / 1024, 1, 1);
        }

        private void CollectUnsubdividedOctantsForRetetrahedralization()
        {
            unsubdivisionRetetrahedralizationCollectionShader.Use();
            GraphicsCore.CurrentDevice.ImmediateContext.ComputeShader.SetUnorderedAccessViews(0,
                octreePoolUAV, octreeUnsubdivisionListUAV, toRetetrahedralizeListUAV);
            GraphicsCore.CurrentDevice.ImmediateContext.Dispatch(operationListsSize / 1024, 1, 1);
        }

        private void RetetrahedralizeListedOctants()
        {
            listedRetetrahedralizationShader.Use();
            GraphicsCore.CurrentDevice.ImmediateContext.ComputeShader.SetUnorderedAccessViews(0,
                octreePoolUAV, tetrahedronsPoolUAV, tetrahedronsCounterUAV, meshVerticesPoolUAV, toRetetrahedralizeListUAV);
            GraphicsCore.CurrentDevice.ImmediateContext.Dispatch(octreePoolSize / 1024, 1, 1);
        }

        private void UnsubdivideCollectedOctants()
        {
            unsubdivisionOctantsShader.Use();
            GraphicsCore.CurrentDevice.ImmediateContext.ComputeShader.SetUnorderedAccessViews(0,
                octreePoolUAV, freeOctantsListUAV, tetrahedronsPoolUAV, meshVerticesPoolUAV, freeMeshVerticesListUAV, octreeUnsubdivisionListUAV);
            GraphicsCore.CurrentDevice.ImmediateContext.Dispatch(operationListsSize / 1024, 1, 1);
        }

        private void FindTetrahedronsShiftLocation()
        {
            findTetrahedronsShiftLocationShader.Use();
            GraphicsCore.CurrentDevice.ImmediateContext.ComputeShader.SetUnorderedAccessViews(0,
                tetrahedronsPoolUAV, tetrahedronsCounterUAV, tetrahedronsShiftLocationUAV);
            GraphicsCore.CurrentDevice.ImmediateContext.Dispatch(tetrahedronsPoolSize / 1024, 1, 1);
            GraphicsCore.CurrentDevice.ImmediateContext.Dispatch(tetrahedronsPoolSize / 1024, 1, 1);
        }

        private void ShiftTetrahedrons()
        {
            int tetrahedronsCount = RetrieveCounterBufferValue(tetrahedronsCounter);

            DataStream stream;
            GraphicsCore.CurrentDevice.ImmediateContext.MapSubresource(tetrahedronsShiftLocation, MapMode.Read, SharpDX.Direct3D11.MapFlags.None, out stream);
            int shiftOrigin = stream.Read<int>();
            int shiftCount = stream.Read<int>();
            GraphicsCore.CurrentDevice.ImmediateContext.UnmapSubresource(tetrahedronsShiftLocation, 0);

            if (shiftCount == 0)
                return;

            shiftTetrahedronsShader.Use();
            GraphicsCore.CurrentDevice.ImmediateContext.ComputeShader.SetUnorderedAccessViews(0,
                tetrahedronsPoolUAV, tetrahedronsCounterUAV, tetrahedronsShiftLocationUAV);
            for (int i = shiftOrigin; i < tetrahedronsCount; i += 1024)
            {
                shiftTetrahedronsShader.UpdateUniform("shiftStart", i);
                shiftTetrahedronsShader.UploadUpdatedUniforms();
                GraphicsCore.CurrentDevice.ImmediateContext.Dispatch(1, 1, 1);
            }
        }

        private void ShiftOctantsTetrahedrons()
        {
            shiftOctantsTetrahedronsShader.Use();
            GraphicsCore.CurrentDevice.ImmediateContext.ComputeShader.SetUnorderedAccessViews(0,
                octreePoolUAV, tetrahedronsCounterUAV, tetrahedronsShiftLocationUAV);
            GraphicsCore.CurrentDevice.ImmediateContext.Dispatch(octreePoolSize / 1024, 1, 1);
        }

        private void Adapt(bool subdivision)
        {
            ResetOperationLists();
            CollectAdaptationLists();

            if (subdivision)
            {
                SubdivideCollectedOctants();
                GenerateVerticesForSubdividedOctants();
                CollectSubdividedOctantsForRetetrahedralization();
            }
            else
            {
                UnsubdivideCollectedOctants();
                CollectUnsubdividedOctantsForRetetrahedralization();
            }
            RetetrahedralizeListedOctants();

            ResetTetrahedronsShiftLocation();
            FindTetrahedronsShiftLocation();
            ShiftTetrahedrons();
            ShiftOctantsTetrahedrons();

            int freeOctantsLeft = RetrieveCounter(freeOctantsListUAV);
            if (freeOctantsLeft < octreePoolSize * 0.2)
                Logger.Log(LogType.Info, "Number of octants is dangerously high!");
            int freeVerticesLeft = RetrieveCounter(freeMeshVerticesListUAV);
            if (freeVerticesLeft < meshVerticesPoolSize * 0.2)
                Logger.Log(LogType.Info, "Number of vertices is dangerously high!");
        }

        private void CalculateNextDensities()
        {
            simulationStepShader.Use();
            GraphicsCore.CurrentDevice.ImmediateContext.ComputeShader.SetUnorderedAccessViews(0,
                octreePoolUAV, meshVerticesPoolUAV, tetrahedronsPoolUAV);

            Vector3f halfSize = Size * 0.5f;
            simulationStepShader.UpdateUniform("invHalfSize", (Vector3f)(1.0f / halfSize));
            simulationStepShader.UpdateUniform("deltaTime", (float)Time.FixedDeltaTime);
            simulationStepShader.UpdateUniform("sourceEnabled", Input.IsKeyDown(System.Windows.Input.Key.F));
            simulationStepShader.UploadUpdatedUniforms();

            GraphicsCore.CurrentDevice.ImmediateContext.Dispatch(meshVerticesPoolSize / 1024, 1, 1);
        }

        private void SetNextDensities()
        {
            flipVerticesDataShader.Use();
            GraphicsCore.CurrentDevice.ImmediateContext.ComputeShader.SetUnorderedAccessViews(0, meshVerticesPoolUAV);
            GraphicsCore.CurrentDevice.ImmediateContext.Dispatch(meshVerticesPoolSize / 1024, 1, 1);
        }

        private void SimulateStep()
        {
            CalculateNextDensities();
            SetNextDensities();
        }

        private bool subdivide = true;
        public override void FixedUpdate()
        {
            try
            {
                SimulateStep();
                Adapt(subdivide);
                subdivide = !subdivide;
                GraphicsCore.CurrentDevice.ImmediateContext.ComputeShader.SetUnorderedAccessViews(0, null, null, null, null, null);
            }
            catch (System.Exception e)
            {
                Result res = GraphicsCore.CurrentDevice.DeviceRemovedReason;
            }
        }

        public override void Render()
        {
            RescaleVolume();

            GraphicsCore.CurrentDevice.ImmediateContext.PixelShader.SetShaderResource(0, octreePoolSRV);
            GraphicsCore.CurrentDevice.ImmediateContext.PixelShader.SetShaderResource(1, meshVerticesPoolSRV);
            GraphicsCore.CurrentDevice.ImmediateContext.PixelShader.SetShaderResource(2, tetrahedronsPoolSRV);
            base.Render();
        }

        internal void RenderOctree()
        {
            RescaleVolume();

            GraphicsCore.CurrentDevice.ImmediateContext.VertexShader.SetShaderResource(0, octreePoolSRV);
            GraphicsCore.CurrentDevice.ImmediateContext.VertexShader.SetShaderResource(1, meshVerticesPoolSRV);

            GraphicsCore.CurrentDevice.ImmediateContext.Draw((octreePoolSize >> 5) * 24, 0);
        }

        internal void RenderTetrahedrons()
        {
            RescaleVolume();

            GraphicsCore.CurrentDevice.ImmediateContext.VertexShader.SetShaderResource(0, tetrahedronsPoolSRV);
            GraphicsCore.CurrentDevice.ImmediateContext.VertexShader.SetShaderResource(1, meshVerticesPoolSRV);

            GraphicsCore.CurrentDevice.ImmediateContext.Draw((tetrahedronsPoolSize >> 3) * 12, 0);
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