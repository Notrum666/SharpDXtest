using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Threading;

using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.Direct3D9;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using Device = SharpDX.Direct3D11.Device;
using Filter = SharpDX.Direct3D11.Filter;
using Query = SharpDX.Direct3D11.Query;
using Light = Engine.BaseAssets.Components.Light;
using Mesh = Engine.BaseAssets.Components.Mesh;
using Format = SharpDX.DXGI.Format;

using Engine.BaseAssets.Components;
using System.Windows.Interop;
using LinearAlgebra;
using System.Diagnostics;
using static System.Net.Mime.MediaTypeNames;
using Engine.BaseAssets.Components.Postprocessing;

namespace Engine
{
    public static class GraphicsCore
    {
        private struct GBuffer
        {
            public Texture worldPos;
            public Texture albedo;
            public Texture normal;
            public Texture metallic;
            public Texture roughness;
            public Texture ambientOcclusion;

            public GBuffer(int width, int height)
            {
                worldPos = new Texture(width, height, Vector4f.Zero.GetBytes(), Format.R32G32B32A32_Float, BindFlags.RenderTarget | BindFlags.ShaderResource);
                albedo = new Texture(width, height, Vector4f.Zero.GetBytes(), Format.R32G32B32A32_Float, BindFlags.RenderTarget | BindFlags.ShaderResource);
                normal = new Texture(width, height, Vector4f.Zero.GetBytes(), Format.R32G32B32A32_Float, BindFlags.RenderTarget | BindFlags.ShaderResource);
                metallic = new Texture(width, height, 0.0f.GetBytes(), Format.R32_Typeless, BindFlags.RenderTarget | BindFlags.ShaderResource);
                roughness = new Texture(width, height, 0.0f.GetBytes(), Format.R32_Typeless, BindFlags.RenderTarget | BindFlags.ShaderResource);
                ambientOcclusion = new Texture(width, height, 0.0f.GetBytes(), Format.R32_Typeless, BindFlags.RenderTarget | BindFlags.ShaderResource);
            }
        }

        private static bool disposed = false;

        public static event Action<int, int> OnResized;

        public static Device CurrentDevice { get; private set; }
        public static SharpDX.Direct3D9.Device D9Device { get; private set; }

        private static RasterizerState backCullingRasterizer;
        private static RasterizerState frontCullingRasterizer;

        private static Sampler sampler;
        private static Sampler shadowsSampler;

        private static Color backgroundColor;
        public static Camera CurrentCamera
        {
            get
            {
                return Camera.Current;
            }
            set
            {
                Camera.Current = value;
            }
        }

        private static GBuffer gbuffer;
        private static Texture depthBuffer;
        private static Texture radianceBuffer;
        private static Texture colorBuffer;
        private static BlendState additiveBlendState;
        private static BlendState blendingBlendState;

        private static FrameBuffer frontbuffer;
        private static FrameBuffer middlebuffer;
        private static FrameBuffer backbuffer;

        private static Query synchQuery;

        private static bool needsToBeResized;
        private static int targetWidth;
        private static int targetHeight;
        private static object resizeLockObject = new object();

        private static PostProcessEffect_Bloom bloomEffect;

#if GraphicsDebugging
        private static SharpDX.DXGI.SwapChain swapChain;
#endif

        public static void Init(IntPtr HWND, int width, int height)
        {
            InitDirectX(HWND, width, height);

            AssetsManager.Textures["default_albedo"] = new Texture(64, 64, new Vector4f(1.0f, 1.0f, 1.0f, 1.0f).GetBytes(), Format.R32G32B32A32_Float, BindFlags.ShaderResource);
            AssetsManager.Textures["default_normal"] = new Texture(64, 64, new Vector4f(0.5f, 0.5f, 1.0f, 0.0f).GetBytes(), Format.R32G32B32A32_Float, BindFlags.ShaderResource);
            AssetsManager.Textures["default_metallic"] = new Texture(64, 64, 0.1f.GetBytes(), Format.R32_Typeless, BindFlags.ShaderResource);
            AssetsManager.Textures["default_roughness"] = new Texture(64, 64, 0.5f.GetBytes(), Format.R32_Typeless, BindFlags.ShaderResource);
            AssetsManager.Textures["default_ambientOcclusion"] = new Texture(64, 64, 0.0f.GetBytes(), Format.R32_Typeless, BindFlags.ShaderResource);

            //AssetsManager.LoadShaderPipeline("default", Shader.Create("BaseAssets\\Shaders\\pbr_lighting.vsh"), 
            //                                            Shader.Create("BaseAssets\\Shaders\\pbr_lighting.fsh"));
            sampler = AssetsManager.Samplers["default"] = new Sampler(TextureAddressMode.Wrap, TextureAddressMode.Wrap);

            AssetsManager.LoadShaderPipeline("depth_only", Shader.Create("BaseAssets\\Shaders\\depth_only.vsh"),
                                                           Shader.Create("BaseAssets\\Shaders\\depth_only.fsh"));
            shadowsSampler = AssetsManager.Samplers["default_shadows"] = new Sampler(TextureAddressMode.Border, TextureAddressMode.Border, Filter.ComparisonMinMagMipLinear, 0, new RawColor4(0.0f, 0.0f, 0.0f, 0.0f), Comparison.LessEqual);

            AssetsManager.LoadShaderPipeline("deferred_geometry", Shader.Create("BaseAssets\\Shaders\\DeferredRender\\deferred_geometry.vsh"), 
                                                                  Shader.Create("BaseAssets\\Shaders\\DeferredRender\\deferred_geometry.fsh"));

            AssetsManager.LoadShaderPipeline("deferred_geometry_particles", Shader.Create("BaseAssets\\Shaders\\DeferredRender\\deferred_geometry_particles.vsh"),
                                                                            Shader.Create("BaseAssets\\Shaders\\DeferredRender\\deferred_geometry_particles.gsh"),
                                                                            Shader.Create("BaseAssets\\Shaders\\DeferredRender\\deferred_geometry_particles.fsh"));
            AssetsManager.LoadShader("particles_bitonic_sort_step", "BaseAssets\\Shaders\\Particles\\particles_bitonic_sort_step.csh");
            AssetsManager.LoadShader("particles_emit_point",        "BaseAssets\\Shaders\\Particles\\particles_emit_point.csh");
            AssetsManager.LoadShader("particles_emit_sphere",       "BaseAssets\\Shaders\\Particles\\particles_emit_sphere.csh");
            AssetsManager.LoadShader("particles_force_constant",    "BaseAssets\\Shaders\\Particles\\particles_force_constant.csh");
            AssetsManager.LoadShader("particles_force_point",       "BaseAssets\\Shaders\\Particles\\particles_force_point.csh");
            AssetsManager.LoadShader("particles_init",              "BaseAssets\\Shaders\\Particles\\particles_init.csh");
            AssetsManager.LoadShader("particles_update_energy",     "BaseAssets\\Shaders\\Particles\\particles_update_energy.csh");
            AssetsManager.LoadShader("particles_update_physics",    "BaseAssets\\Shaders\\Particles\\particles_update_physics.csh");
            AssetsManager.LoadShader("screen_quad",                 "BaseAssets\\Shaders\\screen_quad.vsh");

            AssetsManager.LoadShaderPipeline("volume", Shader.Create("BaseAssets\\Shaders\\VolumetricRender\\volume.vsh"),
                                                       Shader.Create("BaseAssets\\Shaders\\VolumetricRender\\volume.fsh"));

            Shader screenQuadShader = AssetsManager.Shaders["screen_quad"];
            AssetsManager.LoadShaderPipeline("deferred_light_point", screenQuadShader, Shader.Create("BaseAssets\\Shaders\\DeferredRender\\deferred_light_point.fsh"));
            AssetsManager.LoadShaderPipeline("deferred_light_directional", screenQuadShader, Shader.Create("BaseAssets\\Shaders\\DeferredRender\\deferred_light_directional.fsh"));
            AssetsManager.LoadShaderPipeline("deferred_addLight", screenQuadShader, Shader.Create("BaseAssets\\Shaders\\DeferredRender\\deffered_addLight.fsh"));
            AssetsManager.LoadShaderPipeline("deferred_gamma_correction", screenQuadShader, Shader.Create("BaseAssets\\Shaders\\DeferredRender\\deferred_gamma_correction.fsh"));

            bloomEffect = new PostProcessEffect_Bloom();

            backgroundColor = Color.FromRgba(0xFF010101);
            //backgroundColor = Color.FromRgba(0xFFFFFFFF);
        }

        private static void InitDirectX(IntPtr HWND, int width, int height)
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
                        Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
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

            backCullingRasterizer = new RasterizerState(CurrentDevice, new RasterizerStateDescription()
            {
                FillMode = SharpDX.Direct3D11.FillMode.Solid,
                CullMode = CullMode.Back,
                IsFrontCounterClockwise = true,
                IsScissorEnabled = false,
                IsAntialiasedLineEnabled = true,
                IsDepthClipEnabled = true,
                IsMultisampleEnabled = true
            });
            frontCullingRasterizer = new RasterizerState(CurrentDevice, new RasterizerStateDescription()
            {
                FillMode = SharpDX.Direct3D11.FillMode.Solid,
                CullMode = CullMode.Front,
                IsFrontCounterClockwise = true,
                IsScissorEnabled = false,
                IsAntialiasedLineEnabled = true,
                IsDepthClipEnabled = true,
                IsMultisampleEnabled = true
            });
            CurrentDevice.ImmediateContext.Rasterizer.State = backCullingRasterizer;

            BlendStateDescription blendStateDesc = new BlendStateDescription()
            {
                AlphaToCoverageEnable = false,
                IndependentBlendEnable = false
            };
            blendStateDesc.RenderTarget[0] = new RenderTargetBlendDescription(true, BlendOption.One, BlendOption.One, SharpDX.Direct3D11.BlendOperation.Add,
                                                                                    BlendOption.Zero, BlendOption.One, SharpDX.Direct3D11.BlendOperation.Add, ColorWriteMaskFlags.All);
            additiveBlendState = new BlendState(CurrentDevice, blendStateDesc);

            blendStateDesc = new BlendStateDescription()
            {
                AlphaToCoverageEnable = false,
                IndependentBlendEnable = false
            };
            blendStateDesc.RenderTarget[0] = new RenderTargetBlendDescription(true, BlendOption.SourceAlpha, BlendOption.InverseSourceAlpha, SharpDX.Direct3D11.BlendOperation.Add,
                                                                                    BlendOption.SourceAlpha, BlendOption.InverseSourceAlpha, SharpDX.Direct3D11.BlendOperation.Add, ColorWriteMaskFlags.All);
            blendingBlendState = new BlendState(CurrentDevice, blendStateDesc);

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
                                         IntPtr.Zero,
                                         CreateFlags.HardwareVertexProcessing | CreateFlags.Multithreaded | CreateFlags.FpuPreserve,
                                         new SharpDX.Direct3D9.PresentParameters()
                                         {
                                             Windowed = true,
                                             SwapEffect = SharpDX.Direct3D9.SwapEffect.Discard,
                                             DeviceWindowHandle = HWND,
                                             PresentationInterval = PresentInterval.Default,
                                         });

            GenerateBuffers(width, height);

            synchQuery = new Query(CurrentDevice, new QueryDescription() { Type = SharpDX.Direct3D11.QueryType.Event, Flags = QueryFlags.None });
        }

        private static void GenerateBuffers(int width, int height)
        {
            frontbuffer = new FrameBuffer(width, height);
            backbuffer = new FrameBuffer(width, height);
            middlebuffer = new FrameBuffer(width, height);
            gbuffer = new GBuffer(width, height);
            depthBuffer = new Texture(width, height, 0.0f.GetBytes(), Format.R32_Typeless, BindFlags.DepthStencil | BindFlags.ShaderResource);
            radianceBuffer = new Texture(width, height, Vector4f.Zero.GetBytes(), Format.R32G32B32A32_Float, BindFlags.ShaderResource | BindFlags.RenderTarget);
            colorBuffer = new Texture(width, height, Vector4f.Zero.GetBytes(), Format.R32G32B32A32_Float, BindFlags.ShaderResource | BindFlags.RenderTarget);
        }

        public static void Resize(int width, int height)
        {
            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height));

            lock (resizeLockObject)
            {
                targetHeight = height;
                targetWidth = width;
                needsToBeResized = true;
            }
        }

        public static void Update()
        {
            if (needsToBeResized)
            {
                lock (resizeLockObject)
                {
                    GenerateBuffers(targetWidth, targetHeight);
            
                    needsToBeResized = false;

                    OnResized?.Invoke(targetWidth, targetHeight);
                }
            }

            RenderShadows();
            RenderScene();
        }

        private static void RenderShadows()
        {
            if (EngineCore.CurrentScene == null)
                return;

            CurrentDevice.ImmediateContext.Rasterizer.State = frontCullingRasterizer;
            CurrentDevice.ImmediateContext.OutputMerger.BlendState = null;

            List<GameObject> objects = EngineCore.CurrentScene.objects;

            ShaderPipeline pipeline = null;

            void renderObjects()
            {
                foreach (GameObject obj in objects)
                {
                    if (!obj.Enabled)
                        continue;
                    foreach (Mesh mesh in obj.getComponents<Mesh>())
                    {
                        if (!mesh.Enabled)
                            continue;
                        pipeline.UpdateUniform("model", (Matrix4x4f)obj.Transform.Model);

                        pipeline.UploadUpdatedUniforms();

                        mesh.model.Render();
                    }
                }
            }

            foreach (GameObject lightObj in objects)
            {
                if (!lightObj.Enabled)
                    continue;
                foreach (Light light in lightObj.getComponents<Light>())
                {
                    if (!light.Enabled)
                        continue;

                    if (light is SpotLight)
                    {
                        SpotLight curLight = light as SpotLight;

                        pipeline = AssetsManager.ShaderPipelines["depth_only"];
                        pipeline.Use();
                        CurrentDevice.ImmediateContext.Rasterizer.SetViewport(new Viewport(0, 0, curLight.ShadowSize, curLight.ShadowSize, 0.0f, 1.0f));
                        CurrentDevice.ImmediateContext.OutputMerger.SetTargets(curLight.ShadowTexture.GetView<DepthStencilView>(), renderTargetView: null);
                        CurrentDevice.ImmediateContext.ClearDepthStencilView(curLight.ShadowTexture.GetView<DepthStencilView>(), DepthStencilClearFlags.Depth, 1.0f, 0);

                        pipeline.UpdateUniform("view", curLight.lightSpace);

                        renderObjects();
                    }
                    else if (light is DirectionalLight)
                    {
                        DirectionalLight curLight = light as DirectionalLight;

                        pipeline = AssetsManager.ShaderPipelines["depth_only"];
                        pipeline.Use();

                        CurrentDevice.ImmediateContext.Rasterizer.SetViewport(new Viewport(0, 0, curLight.ShadowSize, curLight.ShadowSize, 0.0f, 1.0f));

                        Matrix4x4f[] lightSpaces = curLight.GetLightSpaces(CurrentCamera);
                        for (int i = 0; i < lightSpaces.Length; i++)
                        {
                            DepthStencilView curDSV = curLight.ShadowTexture.GetViews<DepthStencilView>().First(view => view.Description.Texture2DArray.FirstArraySlice == i);
                            CurrentDevice.ImmediateContext.OutputMerger.SetTargets(curDSV, renderTargetView: null);
                            CurrentDevice.ImmediateContext.ClearDepthStencilView(curDSV, DepthStencilClearFlags.Depth, 1.0f, 0);

                            pipeline.UpdateUniform("view", lightSpaces[i]);

                            renderObjects();
                        }
                    }
                    else if (light is PointLight)
                    {
                        PointLight curLight = light as PointLight;

                        // TODO: IMPLEMENT POINT LIGHT SHADOWS

                        continue;
                    }
                    else
                        continue;
                }
            }
        }

        private static void RenderScene()
        {
            CurrentDevice.ImmediateContext.ClearRenderTargetView(backbuffer.RenderTargetTexture.GetView<RenderTargetView>(), backgroundColor);
            if (EngineCore.CurrentScene == null || CurrentCamera == null || !CurrentCamera.Enabled)
            {
                FlushAndSwapFrameBuffers();
                return;
            }

            GeometryPass();
            LightingPass();
            VolumetricPass();
            PrePostProcessingPass();
            GammaCorrectionPass();

            FlushAndSwapFrameBuffers();
#if GraphicsDebugging
            swapChain.Present(1, 0);
#endif

            //CurrentDevice.ImmediateContext.Rasterizer.State = defaultRasterizer;
            //
            //CurrentDevice.ImmediateContext.Rasterizer.SetViewport(new Viewport(0, 0, backbuffer.Width, backbuffer.Height, 0.0f, 1.0f));
            //CurrentDevice.ImmediateContext.OutputMerger.SetTargets(backbuffer.DepthTexture.GetView<DepthStencilView>(), backbuffer.RenderTargetTexture.GetView<RenderTargetView>());
            //
            //CurrentDevice.ImmediateContext.ClearRenderTargetView(backbuffer.RenderTargetTexture.GetView<RenderTargetView>(), backgroundColor);
            //CurrentDevice.ImmediateContext.ClearDepthStencilView(backbuffer.DepthTexture.GetView<DepthStencilView>(), DepthStencilClearFlags.Depth, 1.0f, 0);
            //
            //if (GameCore.CurrentScene == null || CurrentCamera == null || !CurrentCamera.Enabled)
            //{
            //    FlushAndSwapFrameBuffers();
            //    return;
            //}
            //
            //List<GameObject> objects = GameCore.CurrentScene.objects;
            //
            //ShaderPipeline pipeline = AssetsManager.ShaderPipelines["default"];
            //pipeline.Use();
            //
            //int spotLights = 0;
            //int directionalLights = 0;
            //int pointLights = 0;
            //int ambientLights = 0;
            //foreach (GameObject obj in objects)
            //{
            //    if (!obj.Enabled)
            //        continue;
            //    foreach (Light light in obj.getComponents<Light>())
            //    {
            //        if (!light.Enabled)
            //            continue;
            //
            //        if (light is SpotLight)
            //        {
            //            SpotLight curLight = light as SpotLight;
            //            string baseLocation = "spotLights[" + spotLights.ToString() + "].";
            //
            //            pipeline.UpdateUniform(baseLocation + "position", (Vector3f)obj.transform.Position);
            //            pipeline.UpdateUniform(baseLocation + "direction", (Vector3f)obj.transform.Forward);
            //            pipeline.UpdateUniform(baseLocation + "radius", curLight.Radius);
            //            pipeline.UpdateUniform(baseLocation + "brightness", curLight.Brightness);
            //            pipeline.UpdateUniform(baseLocation + "intensity", curLight.Intensity);
            //            pipeline.UpdateUniform(baseLocation + "angularIntensity", curLight.AngularIntensity);
            //            pipeline.UpdateUniform(baseLocation + "angle", curLight.Angle / 2.0f);
            //            pipeline.UpdateUniform(baseLocation + "color", curLight.color);
            //
            //            pipeline.UpdateUniform(baseLocation + "lightSpace", curLight.lightSpace);
            //
            //            pipeline.UpdateUniform(baseLocation + "shadowMapSize", new Vector2f(curLight.ShadowSize, curLight.ShadowSize));
            //
            //            curLight.ShadowTexture.use(baseLocation + "shadowMap");
            //
            //            spotLights++;
            //        }
            //        else if (light is DirectionalLight)
            //        {
            //            DirectionalLight curLight = light as DirectionalLight;
            //            string baseLocation = "directionalLights[" + directionalLights.ToString() + "].";
            //
            //            pipeline.UpdateUniform(baseLocation + "direction", (Vector3f)curLight.gameObject.transform.Forward);
            //            pipeline.UpdateUniform(baseLocation + "brightness", curLight.Brightness);
            //            pipeline.UpdateUniform(baseLocation + "color", curLight.color);
            //
            //            Matrix4x4f[] lightSpaces = curLight.GetLightSpaces(CurrentCamera);
            //            for (int i = 0; i < lightSpaces.Length; i++)
            //                pipeline.UpdateUniform(baseLocation + "lightSpaces[" + i.ToString() + "]", lightSpaces[i]);
            //            float[] cascadeDepths = DirectionalLight.CascadeFrustumDistances;
            //            for (int i = 0; i < cascadeDepths.Length; i++)
            //                pipeline.UpdateUniform(baseLocation + "cascadesDepths[" + i.ToString() + "]", cascadeDepths[i]);
            //            pipeline.UpdateUniform(baseLocation + "cascadesCount", lightSpaces.Length);
            //
            //            pipeline.UpdateUniform(baseLocation + "shadowMapSize", new Vector2f(curLight.ShadowSize, curLight.ShadowSize));
            //
            //            curLight.ShadowTexture.use(baseLocation + "shadowMaps", true);
            //
            //            directionalLights++;
            //        }
            //        else if (light is PointLight)
            //        {
            //            PointLight curLight = light as PointLight;
            //            string baseLocation = "pointLights[" + pointLights.ToString() + "].";
            //
            //            pipeline.UpdateUniform(baseLocation + "position", (Vector3f)curLight.gameObject.transform.Position);
            //            pipeline.UpdateUniform(baseLocation + "radius", curLight.Radius);
            //            pipeline.UpdateUniform(baseLocation + "brightness", curLight.Brightness);
            //            pipeline.UpdateUniform(baseLocation + "intensity", curLight.Intensity);
            //            pipeline.UpdateUniform(baseLocation + "color", curLight.color);
            //
            //            pointLights++;
            //        }
            //        else if (light is AmbientLight)
            //        {
            //            AmbientLight curLight = light as AmbientLight;
            //            string baseLocation = "ambientLights[" + ambientLights.ToString() + "].";
            //
            //            pipeline.UpdateUniform(baseLocation + "brightness", curLight.Brightness);
            //            pipeline.UpdateUniform(baseLocation + "color", curLight.color);
            //
            //            ambientLights++;
            //        }
            //        else
            //            throw new NotImplementedException("Light type " + light.GetType().Name + " is not supported.");
            //    }
            //}
            //
            //pipeline.UpdateUniform("spotLightsCount", spotLights);
            //pipeline.UpdateUniform("directionalLightsCount", directionalLights);
            //pipeline.UpdateUniform("pointLightsCount", pointLights);
            //pipeline.UpdateUniform("ambientLightsCount", ambientLights);
            //
            //pipeline.UpdateUniform("spotLight_NEAR", SpotLight.NEAR);
            //
            //sampler.use("texSampler");
            //shadowsSampler.use("shadowSampler");
            //
            //pipeline.UpdateUniform("camPos", (Vector3f)CurrentCamera.gameObject.transform.Position);
            //pipeline.UpdateUniform("cam_NEAR", (float)CurrentCamera.Near);
            //pipeline.UpdateUniform("cam_FAR", (float)CurrentCamera.Far);
            //
            //pipeline.UpdateUniform("view", (Matrix4x4f)CurrentCamera.gameObject.transform.View);
            //pipeline.UpdateUniform("proj", (Matrix4x4f)CurrentCamera.Proj);
            //
            //foreach (GameObject obj in objects)
            //{
            //    if (!obj.Enabled)
            //        continue;
            //    foreach (Mesh mesh in obj.getComponents<Mesh>())
            //    {
            //        if (!mesh.Enabled)
            //            continue;
            //        pipeline.UpdateUniform("model", (Matrix4x4f)obj.transform.Model);
            //        pipeline.UpdateUniform("modelNorm", (Matrix4x4f)obj.transform.Model.inverse().transposed());
            //
            //        pipeline.UploadUpdatedUniforms();
            //        mesh.Material.Albedo.use("albedoMap");
            //        mesh.Material.Normal.use("normalMap");
            //        mesh.Material.Metallic.use("metallicMap");
            //        mesh.Material.Roughness.use("roughnessMap");
            //        mesh.Material.AmbientOcclusion.use("ambientOcclusionMap");
            //        mesh.model.Render();
            //    }
            //}
            //
            //FlushAndSwapFrameBuffers();
            //#if GraphicsDebugging
            //            swapChain.Present(1, 0);
            //#endif
        }

        private static void GeometryPass()
        {
            CurrentDevice.ImmediateContext.Rasterizer.State = backCullingRasterizer;
            CurrentDevice.ImmediateContext.OutputMerger.BlendState = null;
            //CurrentDevice.ImmediateContext.OutputMerger.DepthStencilState = depthState_checkDepth;

            CurrentDevice.ImmediateContext.Rasterizer.SetViewport(new Viewport(0, 0, backbuffer.Width, backbuffer.Height, 0.0f, 1.0f));
            CurrentDevice.ImmediateContext.OutputMerger.SetTargets(depthBuffer.GetView<DepthStencilView>(), gbuffer.worldPos.GetView<RenderTargetView>(),
                                                                                                            gbuffer.albedo.GetView<RenderTargetView>(),
                                                                                                            gbuffer.normal.GetView<RenderTargetView>(),
                                                                                                            gbuffer.metallic.GetView<RenderTargetView>(),
                                                                                                            gbuffer.roughness.GetView<RenderTargetView>(),
                                                                                                            gbuffer.ambientOcclusion.GetView<RenderTargetView>());
            
            CurrentDevice.ImmediateContext.ClearRenderTargetView(gbuffer.worldPos.GetView<RenderTargetView>(), new RawColor4(0.0f, 0.0f, 0.0f, 0.0f));
            CurrentDevice.ImmediateContext.ClearDepthStencilView(depthBuffer.GetView<DepthStencilView>(), DepthStencilClearFlags.Depth, 1.0f, 0);
            
            List<GameObject> objects = EngineCore.CurrentScene.objects;
            
            ShaderPipeline pipeline = AssetsManager.ShaderPipelines["deferred_geometry"];
            pipeline.Use();

            sampler.use("texSampler");
            
            pipeline.UpdateUniform("view", (Matrix4x4f)CurrentCamera.GameObject.Transform.View);
            pipeline.UpdateUniform("proj", (Matrix4x4f)CurrentCamera.Proj);
            
            foreach (GameObject obj in objects)
            {
                if (!obj.Enabled)
                    continue;
                foreach (Mesh mesh in obj.getComponents<Mesh>())
                {
                    if (!mesh.Enabled)
                        continue;
                    pipeline.UpdateUniform("model", (Matrix4x4f)obj.Transform.Model);
                    pipeline.UpdateUniform("modelNorm", (Matrix4x4f)obj.Transform.Model.inverse().transposed());
            
                    pipeline.UploadUpdatedUniforms();
                    mesh.Material.Albedo.use("albedoMap");
                    mesh.Material.Normal.use("normalMap");
                    mesh.Material.Metallic.use("metallicMap");
                    mesh.Material.Roughness.use("roughnessMap");
                    mesh.Material.AmbientOcclusion.use("ambientOcclusionMap");
                    mesh.model.Render();
                }
            }

            CurrentDevice.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.PointList;

            pipeline = AssetsManager.ShaderPipelines["deferred_geometry_particles"];
            pipeline.Use();
            
            sampler.use("texSampler");
            
            pipeline.UpdateUniform("view", (Matrix4x4f)CurrentCamera.GameObject.Transform.View);
            pipeline.UpdateUniform("proj", (Matrix4x4f)CurrentCamera.Proj);
            
            pipeline.UpdateUniform("camDir", (Vector3f)CurrentCamera.GameObject.Transform.Forward);
            pipeline.UpdateUniform("camUp", (Vector3f)CurrentCamera.GameObject.Transform.Up);
            
            pipeline.UpdateUniform("size", new Vector2f(0.1f, 0.1f));

            foreach (GameObject obj in objects)
            {
                if (!obj.Enabled)
                    continue;
                foreach (ParticleSystem particleSystem in obj.getComponents<ParticleSystem>())
                {
                    if (!particleSystem.Enabled)
                        continue;
                    pipeline.UpdateUniform("model", particleSystem.WorldSpaceParticles ? Matrix4x4f.Identity : (Matrix4x4f)obj.Transform.Model);
            
                    pipeline.UploadUpdatedUniforms();
                    particleSystem.Material.Albedo.use("albedoMap");
                    particleSystem.Material.Normal.use("normalMap");
                    particleSystem.Material.Metallic.use("metallicMap");
                    particleSystem.Material.Roughness.use("roughnessMap");
                    particleSystem.Material.AmbientOcclusion.use("ambientOcclusionMap");
            
                    particleSystem.Render();
                }
            }

            CurrentDevice.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
        }

        private static void LightingPass()
        {
            CurrentDevice.ImmediateContext.Rasterizer.State = backCullingRasterizer;
            CurrentDevice.ImmediateContext.OutputMerger.BlendState = additiveBlendState;

            CurrentDevice.ImmediateContext.ClearRenderTargetView(radianceBuffer.GetView<RenderTargetView>(), new RawColor4(0.0f, 0.0f, 0.0f, 1.0f));

            CurrentDevice.ImmediateContext.Rasterizer.SetViewport(new Viewport(0, 0, backbuffer.Width, backbuffer.Height, 0.0f, 1.0f));
            CurrentDevice.ImmediateContext.OutputMerger.SetTargets(null, renderTargetView: radianceBuffer.GetView<RenderTargetView>());

            List<GameObject> objects = EngineCore.CurrentScene.objects;

            foreach (GameObject obj in objects)
            {
                if (!obj.Enabled)
                    continue;
                foreach (Light light in obj.getComponents<Light>())
                {
                    if (!light.Enabled)
                        continue;
            
                    if (light is SpotLight)
                    {
                        SpotLight curLight = light as SpotLight;

                        continue;
                    }
                    else if (light is DirectionalLight)
                    {
                        DirectionalLight curLight = light as DirectionalLight;
                        ShaderPipeline pipeline = AssetsManager.ShaderPipelines["deferred_light_directional"];
                        pipeline.Use();

                        pipeline.UpdateUniform("camPos", (Vector3f)CurrentCamera.GameObject.Transform.Position);

                        pipeline.UpdateUniform("cam_NEAR", (float)CurrentCamera.Near);
                        pipeline.UpdateUniform("cam_FAR", (float)CurrentCamera.Far);

                        pipeline.UpdateUniform("directionalLight.direction", (Vector3f)curLight.GameObject.Transform.Forward);
                        pipeline.UpdateUniform("directionalLight.brightness", curLight.Brightness);
                        pipeline.UpdateUniform("directionalLight.color", curLight.color);
                        
                        Matrix4x4f[] lightSpaces = curLight.GetLightSpaces(CurrentCamera);
                        for (int i = 0; i < lightSpaces.Length; i++)
                            pipeline.UpdateUniform("directionalLight.lightSpaces[" + i.ToString() + "]", lightSpaces[i]);
                        float[] cascadeDepths = DirectionalLight.CascadeFrustumDistances;
                        for (int i = 0; i < cascadeDepths.Length; i++)
                            pipeline.UpdateUniform("directionalLight.cascadesDepths[" + i.ToString() + "]", cascadeDepths[i]);
                        pipeline.UpdateUniform("directionalLight.cascadesCount", lightSpaces.Length);
                        
                        pipeline.UpdateUniform("directionalLight.shadowMapSize", new Vector2f(curLight.ShadowSize, curLight.ShadowSize));

                        pipeline.UploadUpdatedUniforms();

                        curLight.ShadowTexture.use("directionalLight.shadowMaps", true);
                        shadowsSampler.use("shadowSampler");
                        depthBuffer.use("depthTex");
                    }
                    else if (light is PointLight)
                    {
                        PointLight curLight = light as PointLight;
                        ShaderPipeline pipeline = AssetsManager.ShaderPipelines["deferred_light_point"];
                        pipeline.Use();

                        pipeline.UpdateUniform("camPos", (Vector3f)CurrentCamera.GameObject.Transform.Position);

                        pipeline.UpdateUniform("pointLight.position", (Vector3f)curLight.GameObject.Transform.Position);
                        pipeline.UpdateUniform("pointLight.radius", curLight.Radius);
                        pipeline.UpdateUniform("pointLight.brightness", curLight.Brightness);
                        pipeline.UpdateUniform("pointLight.intensity", curLight.Intensity);
                        pipeline.UpdateUniform("pointLight.color", curLight.color);

                        pipeline.UploadUpdatedUniforms();
                    }
                    else if (light is AmbientLight)
                    {
                        AmbientLight curLight = light as AmbientLight;

                        continue;
                    }
                    else
                        throw new NotImplementedException("Light type " + light.GetType().Name + " is not supported.");

                    gbuffer.worldPos.use("worldPosTex");
                    gbuffer.albedo.use("albedoTex");
                    gbuffer.normal.use("normalTex");
                    gbuffer.metallic.use("metallicTex");
                    gbuffer.roughness.use("roughnessTex");
                    sampler.use("texSampler");
                    CurrentDevice.ImmediateContext.Draw(6, 0);
                }
            }

            CurrentDevice.ImmediateContext.OutputMerger.BlendState = null;

            CurrentDevice.ImmediateContext.OutputMerger.SetTargets(null, renderTargetView: colorBuffer.GetView<RenderTargetView>());
            CurrentDevice.ImmediateContext.ClearRenderTargetView(colorBuffer.GetView<RenderTargetView>(), backgroundColor);

            AssetsManager.ShaderPipelines["deferred_addLight"].Use();

            gbuffer.worldPos.use("worldPosTex");
            gbuffer.albedo.use("albedoTex");
            gbuffer.ambientOcclusion.use("ambientOcclusionTex");
            radianceBuffer.use("radianceTex");
            sampler.use("texSampler");
            CurrentDevice.ImmediateContext.Draw(6, 0);
        }

        private static void VolumetricPass()
        {
            CurrentDevice.ImmediateContext.Rasterizer.State = frontCullingRasterizer;
            CurrentDevice.ImmediateContext.OutputMerger.BlendState = blendingBlendState;

            Viewport viewport = new Viewport(0, 0, backbuffer.Width, backbuffer.Height, 0.0f, 1.0f);
            CurrentDevice.ImmediateContext.Rasterizer.SetViewport(viewport);
            CurrentDevice.ImmediateContext.OutputMerger.SetTargets(null, renderTargetView: colorBuffer.GetView<RenderTargetView>());

            List<GameObject> objects = EngineCore.CurrentScene.objects;

            ShaderPipeline pipeline = AssetsManager.ShaderPipelines["volume"];
            pipeline.Use();

            depthBuffer.use("depthTex");
            sampler.use("texSampler");

            pipeline.UpdateUniform("cam_near", (float)CurrentCamera.Near);
            pipeline.UpdateUniform("cam_far", (float)CurrentCamera.Far);
            pipeline.UpdateUniform("cam_farDivFarMinusNear", (float)(CurrentCamera.Far / (CurrentCamera.Far - CurrentCamera.Near)));
            pipeline.UpdateUniform("invScreenSize", new Vector2f(1.0f / viewport.Width, 1.0f / viewport.Height));

            foreach (GameObject obj in objects)
            {
                if (!obj.Enabled)
                    continue;
                foreach (GasVolume volume in obj.getComponents<GasVolume>())
                {
                    if (!volume.Enabled)
                        continue;

                    pipeline.UpdateUniform("modelViewProj", (Matrix4x4f)(CurrentCamera.Proj * CurrentCamera.GameObject.Transform.View * obj.Transform.Model));
                    pipeline.UpdateUniform("invModelViewProj", (Matrix4x4f)(obj.Transform.View * CurrentCamera.GameObject.Transform.Model * CurrentCamera.InvProj));

                    Vector3f halfSize = volume.Size * 0.5f;
                    Vector3f relativeCamPos = (Vector3f)obj.Transform.View.TransformPoint(CurrentCamera.GameObject.Transform.Position);
                    pipeline.UpdateUniform("relCamPos", relativeCamPos);
                    pipeline.UpdateUniform("camToHalfSize", halfSize - relativeCamPos);
                    pipeline.UpdateUniform("camToMinusHalfSize", -halfSize - relativeCamPos);

                    pipeline.UpdateUniform("absorptionCoef", (float)volume.AbsorptionCoef);
                    pipeline.UpdateUniform("scatteringCoef", (float)volume.ScatteringCoef);
                    pipeline.UpdateUniform("halfSize", (Vector3f)(volume.Size * 0.5f));

                    //pipeline.UpdateUniform("ambientLight", new Vector3f(0.001f, 0.001f, 0.001f));
                    pipeline.UpdateUniform("ambientLight", new Vector3f(0.5f, 0.5f, 0.5f));

                    Vector3f lightDir = new Vector3f(0.0f, 0.0f, -1.0f);
                    pipeline.UpdateUniform("negLightDir", -lightDir);
                    pipeline.UpdateUniform("invNegLightDir", -1.0f / lightDir);
                    pipeline.UpdateUniform("lightIntensity", new Vector3f(5.0f, 5.0f, 5.0f));

                    pipeline.UploadUpdatedUniforms();
                    volume.Render();
                }
            }

            CurrentDevice.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
        }

        private static void PrePostProcessingPass()
        {
            bloomEffect.Process(colorBuffer);
        }

        private static void GammaCorrectionPass()
        {
            CurrentDevice.ImmediateContext.Rasterizer.State = backCullingRasterizer;
            CurrentDevice.ImmediateContext.OutputMerger.BlendState = null;

            CurrentDevice.ImmediateContext.Rasterizer.SetViewport(new Viewport(0, 0, backbuffer.Width, backbuffer.Height, 0.0f, 1.0f));
            CurrentDevice.ImmediateContext.OutputMerger.SetTargets(null, renderTargetView: backbuffer.RenderTargetTexture.GetView<RenderTargetView>());

            AssetsManager.ShaderPipelines["deferred_gamma_correction"].Use();

            colorBuffer.use("colorTex");
            sampler.use("texSampler");
            CurrentDevice.ImmediateContext.Draw(6, 0);
        }

        private static void RenderTexture(Texture tex)
        {
            CurrentDevice.ImmediateContext.Rasterizer.SetViewport(new Viewport(0, 0, backbuffer.Width, backbuffer.Height, 0.0f, 1.0f));
            CurrentDevice.ImmediateContext.OutputMerger.SetTargets(null, renderTargetView: backbuffer.RenderTargetTexture.GetView<RenderTargetView>());

            AssetsManager.ShaderPipelines["tex_to_screen"].Use();

            tex.use("tex");
            sampler.use("texSampler");
            CurrentDevice.ImmediateContext.Draw(6, 0);

            FlushAndSwapFrameBuffers();
        }

        private static void FlushAndSwapFrameBuffers()
        {
            CurrentDevice.ImmediateContext.Flush();

            CurrentDevice.ImmediateContext.End(synchQuery);
            int result = 0;
            while (!(CurrentDevice.ImmediateContext.GetData(synchQuery, out result) && result != 0))
                Thread.Yield();

            lock (middlebuffer)
            {
                FrameBuffer tmp = backbuffer;
                backbuffer = middlebuffer;
                middlebuffer = tmp;
            }
        }

        public static FrameBuffer GetNextFrontBuffer()
        {
            lock (middlebuffer)
            {
                FrameBuffer tmp = middlebuffer;
                middlebuffer = frontbuffer;
                frontbuffer = tmp;
            }
            return frontbuffer;
        }

        public static void Dispose()
        {
            if (!disposed)
            {
                CurrentDevice.Dispose();
                frontbuffer.Dispose();
                middlebuffer.Dispose();
                backbuffer.Dispose();

                disposed = true;
            }
        }
    }
}
