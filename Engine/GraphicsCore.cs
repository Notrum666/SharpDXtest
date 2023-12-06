using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Engine.BaseAssets.Components;
using Engine.BaseAssets.Components.Postprocessing;

using LinearAlgebra;

using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.Direct3D9;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;

using BlendOperation = SharpDX.Direct3D11.BlendOperation;
using Device = SharpDX.Direct3D11.Device;
using FillMode = SharpDX.Direct3D11.FillMode;
using Filter = SharpDX.Direct3D11.Filter;
using Format = SharpDX.DXGI.Format;
using Light = Engine.BaseAssets.Components.Light;
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

        private static RasterizerState backCullingRasterizer;
        private static RasterizerState frontCullingRasterizer;

        private static Sampler sampler;
        private static Sampler shadowsSampler;
        public static Camera CurrentCamera { get; set; }

        private static BlendState additiveBlendState;
        private static BlendState blendingBlendState;

        private static Query synchQuery;

        private static PostProcessEffect_Bloom bloomEffect;

#if GraphicsDebugging
        private static SharpDX.DXGI.SwapChain swapChain;
#endif

        public static void Init(nint HWND, int width, int height)
        {
            InitDirectX(HWND, width, height);

            if (AssetsManager.Textures.Count > 0)
                throw new Exception("AssetsManager.Textures must be empty on GraphicsCore init stage");
            AssetsManager.Textures.Add("default_albedo", new Texture(64, 64, new Vector4f(1.0f, 1.0f, 1.0f, 1.0f).GetBytes(), Format.R32G32B32A32_Float, BindFlags.ShaderResource));
            AssetsManager.Textures.Add("default_normal", new Texture(64, 64, new Vector4f(0.5f, 0.5f, 1.0f, 0.0f).GetBytes(), Format.R32G32B32A32_Float, BindFlags.ShaderResource));
            AssetsManager.Textures.Add("default_metallic", new Texture(64, 64, 0.1f.GetBytes(), Format.R32_Typeless, BindFlags.ShaderResource));
            AssetsManager.Textures.Add("default_roughness", new Texture(64, 64, 0.5f.GetBytes(), Format.R32_Typeless, BindFlags.ShaderResource));
            AssetsManager.Textures.Add("default_ambientOcclusion", new Texture(64, 64, 0.0f.GetBytes(), Format.R32_Typeless, BindFlags.ShaderResource));
            AssetsManager.Textures.Add("default_emissive", new Texture(64, 64, 0.0f.GetBytes(), Format.R32_Typeless, BindFlags.ShaderResource));
            AssetsManager.Materials.Add("default", new Material());

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
            AssetsManager.LoadShader("particles_emit_point", "BaseAssets\\Shaders\\Particles\\particles_emit_point.csh");
            AssetsManager.LoadShader("particles_emit_sphere", "BaseAssets\\Shaders\\Particles\\particles_emit_sphere.csh");
            AssetsManager.LoadShader("particles_force_constant", "BaseAssets\\Shaders\\Particles\\particles_force_constant.csh");
            AssetsManager.LoadShader("particles_force_point", "BaseAssets\\Shaders\\Particles\\particles_force_point.csh");
            AssetsManager.LoadShader("particles_init", "BaseAssets\\Shaders\\Particles\\particles_init.csh");
            AssetsManager.LoadShader("particles_update_energy", "BaseAssets\\Shaders\\Particles\\particles_update_energy.csh");
            AssetsManager.LoadShader("particles_update_physics", "BaseAssets\\Shaders\\Particles\\particles_update_physics.csh");
            AssetsManager.LoadShader("screen_quad", "BaseAssets\\Shaders\\screen_quad.vsh");

            AssetsManager.LoadShaderPipeline("volume", Shader.Create("BaseAssets\\Shaders\\VolumetricRender\\volume.vsh"),
                                             Shader.Create("BaseAssets\\Shaders\\VolumetricRender\\volume.fsh"));

            Shader screenQuadShader = AssetsManager.Shaders["screen_quad"];
            AssetsManager.LoadShaderPipeline("deferred_light_point", screenQuadShader, Shader.Create("BaseAssets\\Shaders\\DeferredRender\\deferred_light_point.fsh"));
            AssetsManager.LoadShaderPipeline("deferred_light_directional", screenQuadShader, Shader.Create("BaseAssets\\Shaders\\DeferredRender\\deferred_light_directional.fsh"));
            AssetsManager.LoadShaderPipeline("deferred_addLight", screenQuadShader, Shader.Create("BaseAssets\\Shaders\\DeferredRender\\deffered_addLight.fsh"));
            AssetsManager.LoadShaderPipeline("deferred_gamma_correction", screenQuadShader, Shader.Create("BaseAssets\\Shaders\\DeferredRender\\deferred_gamma_correction.fsh"));

            bloomEffect = new PostProcessEffect_Bloom();
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

            backCullingRasterizer = new RasterizerState(CurrentDevice, new RasterizerStateDescription()
            {
                FillMode = FillMode.Solid,
                CullMode = CullMode.Back,
                IsFrontCounterClockwise = true,
                IsScissorEnabled = false,
                IsAntialiasedLineEnabled = true,
                IsDepthClipEnabled = true,
                IsMultisampleEnabled = true
            });
            frontCullingRasterizer = new RasterizerState(CurrentDevice, new RasterizerStateDescription()
            {
                FillMode = FillMode.Solid,
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
            blendStateDesc.RenderTarget[0] = new RenderTargetBlendDescription(true, BlendOption.One, BlendOption.One, BlendOperation.Add,
                                                                              BlendOption.Zero, BlendOption.One, BlendOperation.Add, ColorWriteMaskFlags.All);
            additiveBlendState = new BlendState(CurrentDevice, blendStateDesc);

            blendStateDesc = new BlendStateDescription()
            {
                AlphaToCoverageEnable = false,
                IndependentBlendEnable = false
            };
            blendStateDesc.RenderTarget[0] = new RenderTargetBlendDescription(true, BlendOption.SourceAlpha, BlendOption.InverseSourceAlpha, BlendOperation.Add,
                                                                              BlendOption.SourceAlpha, BlendOption.InverseSourceAlpha, BlendOperation.Add, ColorWriteMaskFlags.All);
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

        public static void Update()
        {
            if (CurrentCamera != null)
                //RenderShadows();
                RenderScene(CurrentCamera);
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
                    foreach (MeshComponent meshComponent in obj.GetComponents<MeshComponent>())
                    {
                        if (!meshComponent.Enabled)
                            continue;
                        pipeline.UpdateUniform("model", (Matrix4x4f)obj.Transform.Model);

                        pipeline.UploadUpdatedUniforms();

                        meshComponent.Render();
                    }
                }
            }

            foreach (GameObject lightObj in objects)
            {
                if (!lightObj.Enabled)
                    continue;
                foreach (Light light in lightObj.GetComponents<Light>())
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
                            DepthStencilView curDSV = curLight.ShadowTexture.GetView<DepthStencilView>(i);
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

        public static void RenderScene(Camera camera)
        {
            if (camera == null)
                throw new ArgumentNullException(nameof(camera));

            camera.PreRenderUpdate();

            CurrentDevice.ImmediateContext.ClearRenderTargetView(camera.Backbuffer.RenderTargetTexture.GetView<RenderTargetView>(), camera.BackgroundColor);
            if (EngineCore.CurrentScene == null || !camera.Enabled || camera.GameObject == null)
            {
                FlushAndSwapFrameBuffers(camera);
                return;
            }

            GeometryPass(camera);
            LightingPass(camera);
            VolumetricPass(camera);
            PrePostProcessingPass(camera);
            GammaCorrectionPass(camera);

            FlushAndSwapFrameBuffers(camera);
#if GraphicsDebugging
            swapChain.Present(1, 0);
#endif
        }

        private static void GeometryPass(Camera camera)
        {
            CurrentDevice.ImmediateContext.Rasterizer.State = backCullingRasterizer;
            CurrentDevice.ImmediateContext.OutputMerger.BlendState = null;
            //CurrentDevice.ImmediateContext.OutputMerger.DepthStencilState = depthState_checkDepth;

            CurrentDevice.ImmediateContext.Rasterizer.SetViewport(new Viewport(0, 0, camera.Width, camera.Height, 0.0f, 1.0f));
            CurrentDevice.ImmediateContext.OutputMerger.SetTargets(camera.DepthBuffer.GetView<DepthStencilView>(),
                                                                   camera.GBuffer.worldPos.GetView<RenderTargetView>(),
                                                                   camera.GBuffer.albedo.GetView<RenderTargetView>(),
                                                                   camera.GBuffer.normal.GetView<RenderTargetView>(),
                                                                   camera.GBuffer.metallic.GetView<RenderTargetView>(),
                                                                   camera.GBuffer.roughness.GetView<RenderTargetView>(),
                                                                   camera.GBuffer.ambientOcclusion.GetView<RenderTargetView>(),
                                                                   camera.GBuffer.emission.GetView<RenderTargetView>());
            
            CurrentDevice.ImmediateContext.ClearRenderTargetView(camera.GBuffer.worldPos.GetView<RenderTargetView>(), new RawColor4(0.0f, 0.0f, 0.0f, 0.0f));
#if GraphicsDebugging
            CurrentDevice.ImmediateContext.ClearRenderTargetView(camera.GBuffer.albedo.GetView<RenderTargetView>(), new RawColor4(0.0f, 0.0f, 0.0f, 0.0f));
            CurrentDevice.ImmediateContext.ClearRenderTargetView(camera.GBuffer.normal.GetView<RenderTargetView>(), new RawColor4(0.0f, 0.0f, 0.0f, 0.0f));
            CurrentDevice.ImmediateContext.ClearRenderTargetView(camera.GBuffer.metallic.GetView<RenderTargetView>(), new RawColor4(0.0f, 0.0f, 0.0f, 0.0f));
            CurrentDevice.ImmediateContext.ClearRenderTargetView(camera.GBuffer.roughness.GetView<RenderTargetView>(), new RawColor4(0.0f, 0.0f, 0.0f, 0.0f));
            CurrentDevice.ImmediateContext.ClearRenderTargetView(camera.GBuffer.ambientOcclusion.GetView<RenderTargetView>(), new RawColor4(0.0f, 0.0f, 0.0f, 0.0f));
            CurrentDevice.ImmediateContext.ClearRenderTargetView(camera.GBuffer.emission.GetView<RenderTargetView>(), new RawColor4(0.0f, 0.0f, 0.0f, 0.0f));
#endif
            CurrentDevice.ImmediateContext.ClearDepthStencilView(camera.DepthBuffer.GetView<DepthStencilView>(), DepthStencilClearFlags.Depth, 1.0f, 0);

            List<GameObject> objects = EngineCore.CurrentScene.objects;

            ShaderPipeline pipeline = AssetsManager.ShaderPipelines["deferred_geometry"];
            pipeline.Use();

            sampler.use("texSampler");

            pipeline.UpdateUniform("view", (Matrix4x4f)camera.GameObject.Transform.View);
            pipeline.UpdateUniform("proj", (Matrix4x4f)camera.Proj);

            foreach (GameObject obj in objects)
            {
                if (!obj.Enabled)
                    continue;
                foreach (MeshComponent meshComponent in obj.GetComponents<MeshComponent>())
                {
                    if (!meshComponent.Enabled)
                        continue;
                    pipeline.UpdateUniform("model", (Matrix4x4f)obj.Transform.Model);
                    pipeline.UpdateUniform("modelNorm", (Matrix4x4f)obj.Transform.Model.inverse().transposed());

                    pipeline.UploadUpdatedUniforms();
                    meshComponent.Render();
                }
            }

            CurrentDevice.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.PointList;

            pipeline = AssetsManager.ShaderPipelines["deferred_geometry_particles"];
            pipeline.Use();

            sampler.use("texSampler");

            pipeline.UpdateUniform("view", (Matrix4x4f)camera.GameObject.Transform.View);
            pipeline.UpdateUniform("proj", (Matrix4x4f)camera.Proj);

            pipeline.UpdateUniform("camDir", (Vector3f)camera.GameObject.Transform.Forward);
            pipeline.UpdateUniform("camUp", (Vector3f)camera.GameObject.Transform.Up);

            pipeline.UpdateUniform("size", new Vector2f(0.1f, 0.1f));

            foreach (GameObject obj in objects)
            {
                if (!obj.Enabled)
                    continue;
                foreach (ParticleSystem particleSystem in obj.GetComponents<ParticleSystem>())
                {
                    if (!particleSystem.Enabled)
                        continue;
                    pipeline.UpdateUniform("model", particleSystem.WorldSpaceParticles ? Matrix4x4f.Identity : (Matrix4x4f)obj.Transform.Model);

                    pipeline.UploadUpdatedUniforms();
                    particleSystem.Material.Use();
                    particleSystem.Render();
                }
            }

            CurrentDevice.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
        }

        private static void LightingPass(Camera camera)
        {
            CurrentDevice.ImmediateContext.Rasterizer.State = backCullingRasterizer;
            CurrentDevice.ImmediateContext.OutputMerger.BlendState = additiveBlendState;

            CurrentDevice.ImmediateContext.ClearRenderTargetView(camera.RadianceBuffer.GetView<RenderTargetView>(), new RawColor4(0.0f, 0.0f, 0.0f, 1.0f));

            CurrentDevice.ImmediateContext.Rasterizer.SetViewport(new Viewport(0, 0, camera.Backbuffer.Width, camera.Backbuffer.Height, 0.0f, 1.0f));
            CurrentDevice.ImmediateContext.OutputMerger.SetTargets(null, renderTargetView: camera.RadianceBuffer.GetView<RenderTargetView>());

            List<GameObject> objects = EngineCore.CurrentScene.objects;

            foreach (GameObject obj in objects)
            {
                if (!obj.Enabled)
                    continue;
                foreach (Light light in obj.GetComponents<Light>())
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

                        pipeline.UpdateUniform("camPos", (Vector3f)camera.GameObject.Transform.Position);

                        pipeline.UpdateUniform("cam_NEAR", (float)camera.Near);
                        pipeline.UpdateUniform("cam_FAR", (float)camera.Far);

                        pipeline.UpdateUniform("directionalLight.direction", (Vector3f)curLight.GameObject.Transform.Forward);
                        pipeline.UpdateUniform("directionalLight.brightness", curLight.Brightness);
                        pipeline.UpdateUniform("directionalLight.color", curLight.color);

                        Matrix4x4f[] lightSpaces = curLight.GetLightSpaces(camera);
                        for (int i = 0; i < lightSpaces.Length; i++)
                            pipeline.UpdateUniform("directionalLight.lightSpaces[" + i.ToString() + "]", lightSpaces[i]);
                        float[] cascadeDepths = DirectionalLight.CascadeFrustumDistances;
                        for (int i = 0; i < cascadeDepths.Length; i++)
                            pipeline.UpdateUniform("directionalLight.cascadesDepths[" + i.ToString() + "]", cascadeDepths[i]);
                        pipeline.UpdateUniform("directionalLight.cascadesCount", lightSpaces.Length);

                        pipeline.UpdateUniform("directionalLight.shadowMapSize", new Vector2f(curLight.ShadowSize, curLight.ShadowSize));

                        pipeline.UploadUpdatedUniforms();

                        curLight.ShadowTexture.Use("directionalLight.shadowMaps");
                        shadowsSampler.use("shadowSampler");
                        camera.DepthBuffer.Use("depthTex");
                    }
                    else if (light is PointLight)
                    {
                        PointLight curLight = light as PointLight;
                        ShaderPipeline pipeline = AssetsManager.ShaderPipelines["deferred_light_point"];
                        pipeline.Use();

                        pipeline.UpdateUniform("camPos", (Vector3f)camera.GameObject.Transform.Position);

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

                    camera.GBuffer.worldPos.Use("worldPosTex");
                    camera.GBuffer.albedo.Use("albedoTex");
                    camera.GBuffer.normal.Use("normalTex");
                    camera.GBuffer.metallic.Use("metallicTex");
                    camera.GBuffer.roughness.Use("roughnessTex");
                    sampler.use("texSampler");
                    CurrentDevice.ImmediateContext.Draw(6, 0);
                }
            }

            CurrentDevice.ImmediateContext.OutputMerger.BlendState = null;

            CurrentDevice.ImmediateContext.OutputMerger.SetTargets(null, renderTargetView: camera.ColorBuffer.GetView<RenderTargetView>());
            CurrentDevice.ImmediateContext.ClearRenderTargetView(camera.ColorBuffer.GetView<RenderTargetView>(), camera.BackgroundColor);

            AssetsManager.ShaderPipelines["deferred_addLight"].Use();

            camera.GBuffer.worldPos.Use("worldPosTex");
            camera.GBuffer.albedo.Use("albedoTex");
            camera.GBuffer.ambientOcclusion.Use("ambientOcclusionTex");
            camera.RadianceBuffer.Use("radianceTex");
            sampler.use("texSampler");
            CurrentDevice.ImmediateContext.Draw(6, 0);
        }

        private static void VolumetricPass(Camera camera)
        {
            CurrentDevice.ImmediateContext.Rasterizer.State = frontCullingRasterizer;
            CurrentDevice.ImmediateContext.OutputMerger.BlendState = blendingBlendState;

            Viewport viewport = new Viewport(0, 0, camera.Backbuffer.Width, camera.Backbuffer.Height, 0.0f, 1.0f);
            CurrentDevice.ImmediateContext.Rasterizer.SetViewport(viewport);
            CurrentDevice.ImmediateContext.OutputMerger.SetTargets(null, renderTargetView: camera.ColorBuffer.GetView<RenderTargetView>());

            List<GameObject> objects = EngineCore.CurrentScene.objects;

            ShaderPipeline pipeline = AssetsManager.ShaderPipelines["volume"];
            pipeline.Use();

            camera.DepthBuffer.Use("depthTex");
            sampler.use("texSampler");

            pipeline.UpdateUniform("cam_near", (float)camera.Near);
            pipeline.UpdateUniform("cam_far", (float)camera.Far);
            pipeline.UpdateUniform("cam_farDivFarMinusNear", (float)(camera.Far / (camera.Far - camera.Near)));
            pipeline.UpdateUniform("invScreenSize", new Vector2f(1.0f / viewport.Width, 1.0f / viewport.Height));

            foreach (GameObject obj in objects)
            {
                if (!obj.Enabled)
                    continue;
                foreach (GasVolume volume in obj.GetComponents<GasVolume>())
                {
                    if (!volume.Enabled)
                        continue;

                    pipeline.UpdateUniform("modelViewProj", (Matrix4x4f)(camera.Proj * camera.GameObject.Transform.View * obj.Transform.Model));
                    pipeline.UpdateUniform("invModelViewProj", (Matrix4x4f)(obj.Transform.View * camera.GameObject.Transform.Model * camera.InvProj));

                    Vector3f halfSize = volume.Size * 0.5f;
                    Vector3f relativeCamPos = (Vector3f)obj.Transform.View.TransformPoint(camera.GameObject.Transform.Position);
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

        private static void PrePostProcessingPass(Camera camera)
        {
            bloomEffect.Process(camera.ColorBuffer);
        }

        private static void GammaCorrectionPass(Camera camera)
        {
            CurrentDevice.ImmediateContext.Rasterizer.State = backCullingRasterizer;
            CurrentDevice.ImmediateContext.OutputMerger.BlendState = null;

            CurrentDevice.ImmediateContext.Rasterizer.SetViewport(new Viewport(0, 0, camera.Backbuffer.Width, camera.Backbuffer.Height, 0.0f, 1.0f));
            CurrentDevice.ImmediateContext.OutputMerger.SetTargets(null, renderTargetView: camera.Backbuffer.RenderTargetTexture.GetView<RenderTargetView>());

            AssetsManager.ShaderPipelines["deferred_gamma_correction"].Use();

            camera.ColorBuffer.Use("colorTex");
            sampler.use("texSampler");
            CurrentDevice.ImmediateContext.Draw(6, 0);
        }

        private static void RenderTexture(Camera camera, Texture tex)
        {
            CurrentDevice.ImmediateContext.Rasterizer.SetViewport(new Viewport(0, 0, camera.Backbuffer.Width, camera.Backbuffer.Height, 0.0f, 1.0f));
            CurrentDevice.ImmediateContext.OutputMerger.SetTargets(null, renderTargetView: camera.Backbuffer.RenderTargetTexture.GetView<RenderTargetView>());

            AssetsManager.ShaderPipelines["tex_to_screen"].Use();

            tex.Use("tex");
            sampler.use("texSampler");
            CurrentDevice.ImmediateContext.Draw(6, 0);

            FlushAndSwapFrameBuffers(camera);
        }

        private static void Flush()
        {
            CurrentDevice.ImmediateContext.Flush();
            CurrentDevice.ImmediateContext.End(synchQuery);

            int result;
            while (!(CurrentDevice.ImmediateContext.GetData(synchQuery, out result) && result != 0))
                Thread.Yield();
        }

        private static void FlushAndSwapFrameBuffers(Camera camera)
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