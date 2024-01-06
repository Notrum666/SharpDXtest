using System.Threading;
using Engine.BaseAssets.Components;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.Direct3D9;
using BlendOperation = SharpDX.Direct3D11.BlendOperation;
using Device = SharpDX.Direct3D11.Device;
using FillMode = SharpDX.Direct3D11.FillMode;
using Format = SharpDX.DXGI.Format;
using Query = SharpDX.Direct3D11.Query;
using QueryType = SharpDX.Direct3D11.QueryType;
using SwapEffect = SharpDX.Direct3D9.SwapEffect;

namespace Engine
{
    struct GBuffer
    {
        public Texture worldPos;
        public Texture albedo;
        public Texture normal;
        public Texture metallic;
        public Texture roughness;
        public Texture ambientOcclusion;
        public Texture emission;

        public GBuffer(int width, int height)
        {
            worldPos = new Texture(width, height, null, Format.R32G32B32A32_Float, BindFlags.RenderTarget | BindFlags.ShaderResource);
            albedo = new Texture(width, height, null, Format.R32G32B32A32_Float, BindFlags.RenderTarget | BindFlags.ShaderResource);
            normal = new Texture(width, height, null, Format.R32G32B32A32_Float, BindFlags.RenderTarget | BindFlags.ShaderResource);
            metallic = new Texture(width, height, null, Format.R32_Typeless, BindFlags.RenderTarget | BindFlags.ShaderResource);
            roughness = new Texture(width, height, null, Format.R32_Typeless, BindFlags.RenderTarget | BindFlags.ShaderResource);
            ambientOcclusion = new Texture(width, height, null, Format.R32_Typeless, BindFlags.RenderTarget | BindFlags.ShaderResource);
            emission = new Texture(width, height, null, Format.R32_Typeless, BindFlags.RenderTarget | BindFlags.ShaderResource);
        }

        internal void Dispose()
        {
            worldPos?.Dispose();
            albedo?.Dispose();
            normal?.Dispose();
            metallic?.Dispose();
            roughness?.Dispose();
            ambientOcclusion?.Dispose();
            emission?.Dispose();
        }
    }

    public static class GraphicsCore
    {
        private static bool disposed = false;
        public static Device CurrentDevice { get; private set; }
        public static SharpDX.Direct3D9.Device D9Device { get; private set; }

        public static RasterizerState BackCullingRasterizer;
        public static RasterizerState FrontCullingRasterizer;

        public static Camera CurrentCamera { get; set; }

        public static BlendState AdditiveBlendState;
        public static BlendState BlendingBlendState;

        private static Query synchQuery;

#if GraphicsDebugging
        private static SharpDX.DXGI.SwapChain swapChain;
#endif

        public static void Init(nint HWND, int width, int height)
        {
            InitDirectX(HWND, width, height);

            Shader.CreateStaticShader("particles_bitonic_sort_step", "BaseAssets\\Shaders\\Particles\\particles_bitonic_sort_step.csh");
            Shader.CreateStaticShader("particles_emit_point", "BaseAssets\\Shaders\\Particles\\particles_emit_point.csh");
            Shader.CreateStaticShader("particles_emit_sphere", "BaseAssets\\Shaders\\Particles\\particles_emit_sphere.csh");
            Shader.CreateStaticShader("particles_force_constant", "BaseAssets\\Shaders\\Particles\\particles_force_constant.csh");
            Shader.CreateStaticShader("particles_force_point", "BaseAssets\\Shaders\\Particles\\particles_force_point.csh");
            Shader.CreateStaticShader("particles_init", "BaseAssets\\Shaders\\Particles\\particles_init.csh");
            Shader.CreateStaticShader("particles_update_energy", "BaseAssets\\Shaders\\Particles\\particles_update_energy.csh");
            Shader.CreateStaticShader("particles_update_physics", "BaseAssets\\Shaders\\Particles\\particles_update_physics.csh");
            Shader.CreateStaticShader("screen_quad", "BaseAssets\\Shaders\\screen_quad.vsh");

            ShaderPipeline.InitializeStaticPipelines();
        }

        private static void InitDirectX(nint HWND, int width, int height)
        {
#if !GraphicsDebugging
            CurrentDevice = new Device(DriverType.Hardware, DeviceCreationFlags.Debug | DeviceCreationFlags.BgraSupport, FeatureLevel.Level_11_0);
#else
            Device device;
            Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.Debug | DeviceCreationFlags.BgraSupport,
                new SharpDX.DXGI.SwapChainDescription()
                {
                    ModeDescription =
                    {
                        Width = 1,
                        Height = 1,
                        Format = Format.B8G8R8A8_UNorm,
                        RefreshRate = new SharpDX.DXGI.Rational(60, 1),
                        Scaling = SharpDX.DXGI.DisplayModeScaling.Unspecified,
                        ScanlineOrdering = SharpDX.DXGI.DisplayModeScanlineOrder.Unspecified
                    },
                    SampleDescription =
                    {
                        Count = 1,
                        Quality = 0
                    },
                    BufferCount = 1,
                    Usage = SharpDX.DXGI.Usage.RenderTargetOutput,
                    IsWindowed = true,
                    OutputHandle = HWND,
                    Flags = 0,
                    SwapEffect = SharpDX.DXGI.SwapEffect.Discard
                }, out device, out swapChain);
            CurrentDevice = device;
#endif

            BackCullingRasterizer = new RasterizerState(CurrentDevice, new RasterizerStateDescription()
            {
                FillMode = FillMode.Solid,
                CullMode = CullMode.Back,
                IsFrontCounterClockwise = true,
                IsScissorEnabled = false,
                IsAntialiasedLineEnabled = true,
                IsDepthClipEnabled = true,
                IsMultisampleEnabled = true
            });
            FrontCullingRasterizer = new RasterizerState(CurrentDevice, new RasterizerStateDescription()
            {
                FillMode = FillMode.Solid,
                CullMode = CullMode.Front,
                IsFrontCounterClockwise = true,
                IsScissorEnabled = false,
                IsAntialiasedLineEnabled = true,
                IsDepthClipEnabled = true,
                IsMultisampleEnabled = true
            });
            CurrentDevice.ImmediateContext.Rasterizer.State = BackCullingRasterizer;

            BlendStateDescription blendStateDesc = new BlendStateDescription()
            {
                AlphaToCoverageEnable = false,
                IndependentBlendEnable = false
            };
            blendStateDesc.RenderTarget[0] = new RenderTargetBlendDescription(true, BlendOption.One, BlendOption.One, BlendOperation.Add,
                                                                              BlendOption.Zero, BlendOption.One, BlendOperation.Add, ColorWriteMaskFlags.All);
            AdditiveBlendState = new BlendState(CurrentDevice, blendStateDesc);

            blendStateDesc = new BlendStateDescription()
            {
                AlphaToCoverageEnable = false,
                IndependentBlendEnable = false
            };
            blendStateDesc.RenderTarget[0] = new RenderTargetBlendDescription(true, BlendOption.SourceAlpha, BlendOption.InverseSourceAlpha, BlendOperation.Add,
                                                                              BlendOption.SourceAlpha, BlendOption.InverseSourceAlpha, BlendOperation.Add, ColorWriteMaskFlags.All);
            BlendingBlendState = new BlendState(CurrentDevice, blendStateDesc);

            //depthState_checkDepth = new DepthStencilState(CurrentDevice, new DepthStencilStateDescription()
            //{
            //    DepthComparison =  Comparison.Less,
            //    IsDepthEnabled = true,
            //    IsStencilEnabled = false
            //});
            //depthState_skipDepth = new DepthStencilState(CurrentDevice, new DepthStencilStateDescription()
            //{
            //    DepthComparison = Comparison.Less,
            //    IsDepthEnabled = false,
            //    IsStencilEnabled = false
            //});
            //CurrentDevice.ImmediateContext.OutputMerger.DepthStencilState = depthState_checkDepth;

            CurrentDevice.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            Direct3DEx context = new Direct3DEx();

            D9Device = new SharpDX.Direct3D9.Device(context,
                                                    0,
                                                    DeviceType.Hardware,
                                                    0,
                                                    CreateFlags.HardwareVertexProcessing | CreateFlags.Multithreaded | CreateFlags.FpuPreserve,
                                                    new SharpDX.Direct3D9.PresentParameters()
                                                    {
                                                        Windowed = true,
                                                        SwapEffect = SwapEffect.Discard,
                                                        DeviceWindowHandle = HWND,
                                                        PresentationInterval = PresentInterval.Default
                                                    });

            synchQuery = new Query(CurrentDevice, new QueryDescription() { Type = QueryType.Event, Flags = QueryFlags.None });
        }

        private static void Flush()
        {
            CurrentDevice.ImmediateContext.Flush();
            CurrentDevice.ImmediateContext.End(synchQuery);

            int result;
            while (!(CurrentDevice.ImmediateContext.GetData(synchQuery, out result) && result != 0))
                Thread.Yield();
        }

        public static void FlushAndSwapFrameBuffers(Camera camera)
        {
            Flush();

            camera.SwapFrameBuffers();
        }

        public static void Dispose()
        {
            if (!disposed)
            {
                CurrentDevice.Dispose();

                disposed = true;
            }
        }
    }
}