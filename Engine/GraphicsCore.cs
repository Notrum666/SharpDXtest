using System;
using System.Threading;

using Engine.BaseAssets.Components;

using LinearAlgebra;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.Direct3D9;
using Device = SharpDX.Direct3D11.Device;
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
            worldPos =          new Texture(width, height, null, Format.R32G32B32A32_Float, BindFlags.RenderTarget | BindFlags.ShaderResource);
            albedo =            new Texture(width, height, null, Format.R32G32B32A32_Float, BindFlags.RenderTarget | BindFlags.ShaderResource);
            normal =            new Texture(width, height, null, Format.R32G32B32A32_Float, BindFlags.RenderTarget | BindFlags.ShaderResource);
            metallic =          new Texture(width, height, null, Format.R32_Typeless, BindFlags.RenderTarget | BindFlags.ShaderResource);
            roughness =         new Texture(width, height, null, Format.R32_Typeless, BindFlags.RenderTarget | BindFlags.ShaderResource);
            ambientOcclusion =  new Texture(width, height, null, Format.R32_Typeless, BindFlags.RenderTarget | BindFlags.ShaderResource);
            emission =          new Texture(width, height, null, Format.R32_Typeless, BindFlags.RenderTarget | BindFlags.ShaderResource);
        }
    }

    public static class GraphicsCore
    {

        private static bool disposed = false;
        public static Device CurrentDevice { get; private set; }
        public static SharpDX.Direct3D9.Device D9Device { get; private set; }
        public static Camera CurrentCamera { get; set; }

        private static Query synchQuery;

#if GraphicsDebugging
        private static SharpDX.DXGI.SwapChain swapChain;
#endif

        public static void Init(nint HWND, int width, int height)
        {
            InitDirectX(HWND, width, height);

            if (AssetsManager_Old.Textures.Count > 0)
                throw new Exception("AssetsManager.Textures must be empty on GraphicsCore init stage");
            AssetsManager_Old.Textures.Add("default_albedo", new Texture(64, 64, new Vector4f(1.0f, 1.0f, 1.0f, 1.0f).GetBytes(), Format.R32G32B32A32_Float, BindFlags.ShaderResource));
            AssetsManager_Old.Textures.Add("default_normal", new Texture(64, 64, new Vector4f(0.5f, 0.5f, 1.0f, 0.0f).GetBytes(), Format.R32G32B32A32_Float, BindFlags.ShaderResource));
            AssetsManager_Old.Textures.Add("default_metallic", new Texture(64, 64, 0.1f.GetBytes(), Format.R32_Typeless, BindFlags.ShaderResource));
            AssetsManager_Old.Textures.Add("default_roughness", new Texture(64, 64, 0.5f.GetBytes(), Format.R32_Typeless, BindFlags.ShaderResource));
            AssetsManager_Old.Textures.Add("default_ambientOcclusion", new Texture(64, 64, 0.0f.GetBytes(), Format.R32_Typeless, BindFlags.ShaderResource));
            AssetsManager_Old.Textures.Add("default_emissive", new Texture(64, 64, 0.0f.GetBytes(), Format.R32_Typeless, BindFlags.ShaderResource));
            AssetsManager_Old.Materials.Add("default", new Material());

            //AssetsManager.LoadShaderPipeline("default", Shader.Create("BaseAssets\\Shaders\\pbr_lighting.vsh"),
            //                                            Shader.Create("BaseAssets\\Shaders\\pbr_lighting.fsh"));
       

            AssetsManager_Old.LoadShaderPipeline("depth_only", Shader.Create("BaseAssets\\Shaders\\depth_only.vsh"),
                                             Shader.Create("BaseAssets\\Shaders\\depth_only.fsh"));
            

            AssetsManager_Old.LoadShaderPipeline("deferred_geometry", Shader.Create("BaseAssets\\Shaders\\DeferredRender\\deferred_geometry.vsh"),
                                             Shader.Create("BaseAssets\\Shaders\\DeferredRender\\deferred_geometry.fsh"));

            AssetsManager_Old.LoadShaderPipeline("deferred_geometry_particles", Shader.Create("BaseAssets\\Shaders\\DeferredRender\\deferred_geometry_particles.vsh"),
                                             Shader.Create("BaseAssets\\Shaders\\DeferredRender\\deferred_geometry_particles.gsh"),
                                             Shader.Create("BaseAssets\\Shaders\\DeferredRender\\deferred_geometry_particles.fsh"));
            AssetsManager_Old.LoadShader("particles_bitonic_sort_step", "BaseAssets\\Shaders\\Particles\\particles_bitonic_sort_step.csh");
            AssetsManager_Old.LoadShader("particles_emit_point", "BaseAssets\\Shaders\\Particles\\particles_emit_point.csh");
            AssetsManager_Old.LoadShader("particles_emit_sphere", "BaseAssets\\Shaders\\Particles\\particles_emit_sphere.csh");
            AssetsManager_Old.LoadShader("particles_force_constant", "BaseAssets\\Shaders\\Particles\\particles_force_constant.csh");
            AssetsManager_Old.LoadShader("particles_force_point", "BaseAssets\\Shaders\\Particles\\particles_force_point.csh");
            AssetsManager_Old.LoadShader("particles_init", "BaseAssets\\Shaders\\Particles\\particles_init.csh");
            AssetsManager_Old.LoadShader("particles_update_energy", "BaseAssets\\Shaders\\Particles\\particles_update_energy.csh");
            AssetsManager_Old.LoadShader("particles_update_physics", "BaseAssets\\Shaders\\Particles\\particles_update_physics.csh");
            AssetsManager_Old.LoadShader("screen_quad", "BaseAssets\\Shaders\\screen_quad.vsh");

            AssetsManager_Old.LoadShaderPipeline("volume", Shader.Create("BaseAssets\\Shaders\\VolumetricRender\\volume.vsh"),
                                             Shader.Create("BaseAssets\\Shaders\\VolumetricRender\\volume.fsh"));

            Shader screenQuadShader = AssetsManager_Old.Shaders["screen_quad"];
            AssetsManager_Old.LoadShaderPipeline("deferred_light_point", screenQuadShader, Shader.Create("BaseAssets\\Shaders\\DeferredRender\\deferred_light_point.fsh"));
            AssetsManager_Old.LoadShaderPipeline("deferred_light_directional", screenQuadShader, Shader.Create("BaseAssets\\Shaders\\DeferredRender\\deferred_light_directional.fsh"));
            AssetsManager_Old.LoadShaderPipeline("deferred_addLight", screenQuadShader, Shader.Create("BaseAssets\\Shaders\\DeferredRender\\deffered_addLight.fsh"));
            AssetsManager_Old.LoadShaderPipeline("deferred_gamma_correction", screenQuadShader, Shader.Create("BaseAssets\\Shaders\\DeferredRender\\deferred_gamma_correction.fsh"));
        }

        private static void InitDirectX(nint HWND, int width, int height)
        {
#if !GraphicsDebugging
            CurrentDevice = new Device(DriverType.Hardware, DeviceCreationFlags.Debug | DeviceCreationFlags.BgraSupport, FeatureLevel.Level_11_0);
#else
            Device device;
            Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.Debug | DeviceCreationFlags.BgraSupport,
                new SwapChainDescription()
                {
                    ModeDescription =
                    {
                        Width = 1,
                        Height = 1,
                        Format = Format.B8G8R8A8_UNorm,
                        RefreshRate = new Rational(60, 1),
                        Scaling = DisplayModeScaling.Unspecified,
                        ScanlineOrdering = DisplayModeScanlineOrder.Unspecified
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