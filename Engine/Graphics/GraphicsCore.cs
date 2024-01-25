using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

using Engine.BaseAssets.Components;
using Engine.BaseAssets.Components.Postprocessing;

using LinearAlgebra;

using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.Direct3D9;
using SharpDX.Mathematics.Interop;

using BlendOperation = SharpDX.Direct3D11.BlendOperation;
using Device = SharpDX.Direct3D11.Device;
using FillMode = SharpDX.Direct3D11.FillMode;
using Light = Engine.BaseAssets.Components.Light;
using Query = SharpDX.Direct3D11.Query;
using QueryType = SharpDX.Direct3D11.QueryType;
using SwapEffect = SharpDX.Direct3D9.SwapEffect;

namespace Engine
{
    public static class GraphicsCore
    {
        private static bool disposed = false;
        public static Device CurrentDevice { get; private set; }
        public static SharpDX.Direct3D9.Device D9Device { get; private set; }

        public static ViewportControl ViewportPanel { get; private set; }

        public static Sampler ShadowsSampler => shadowsSampler;

        private static RasterizerState backCullingRasterizer;
        private static RasterizerState frontCullingRasterizer;

        private static Sampler sampler;
        private static Sampler shadowsSampler;

        private static BlendState additiveBlendState;
        private static BlendState blendingBlendState;

        private static Query synchQuery;

#if GraphicsDebugging
        private static SharpDX.DXGI.SwapChain swapChain;
#endif

        public static void Init()
        {
            InitDirectX();

            sampler = Sampler.Default;
            shadowsSampler = Sampler.DefaultShadows;

            ViewportPanel = new ViewportControl();
            ViewportPanel.Focusable = true;
            ViewportPanel.Background = System.Windows.Media.Brushes.Transparent;

            SceneManager.OnSceneUnloading += ClearViewportPanel;
        }

        private static void InitDirectX()
        {
            nint HWND = new WindowInteropHelper(Application.Current.MainWindow!).Handle;

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

        //TODO: Separate into light classes
        public static void RenderShadows(Camera camera)
        {
            if (Scene.CurrentScene == null)
                return;

            CurrentDevice.ImmediateContext.Rasterizer.State = frontCullingRasterizer;
            CurrentDevice.ImmediateContext.OutputMerger.BlendState = null;

            foreach (Light light in Scene.FindComponentsOfType<Light>())
            {
                if (!light.LocalEnabled)
                    continue;

                light.RenderShadows(camera);
            }
        }

        internal static void RenderScene(Camera camera)
        {
            if (camera == null)
            {
                Logger.Log(LogType.Warning, "Tried to render scene with null given as Camera");
                return;
            }

            camera.PreRenderUpdate();
            CurrentDevice.ImmediateContext.ClearRenderTargetView(camera.BackBuffer.RenderTargetTexture.GetView<RenderTargetView>(), camera.BackgroundColor);

            if (Scene.CurrentScene == null || !camera.ShouldRender)
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
                                                                   camera.GBuffer.WorldPos.GetView<RenderTargetView>(),
                                                                   camera.GBuffer.Albedo.GetView<RenderTargetView>(),
                                                                   camera.GBuffer.Normal.GetView<RenderTargetView>(),
                                                                   camera.GBuffer.Metallic.GetView<RenderTargetView>(),
                                                                   camera.GBuffer.Roughness.GetView<RenderTargetView>(),
                                                                   camera.GBuffer.AmbientOcclusion.GetView<RenderTargetView>(),
                                                                   camera.GBuffer.Emission.GetView<RenderTargetView>());

            CurrentDevice.ImmediateContext.ClearRenderTargetView(camera.GBuffer.WorldPos.GetView<RenderTargetView>(), new RawColor4(0.0f, 0.0f, 0.0f, 0.0f));
#if GraphicsDebugging
            CurrentDevice.ImmediateContext.ClearRenderTargetView(camera.GBuffer.albedo.GetView<RenderTargetView>(), new RawColor4(0.0f, 0.0f, 0.0f, 0.0f));
            CurrentDevice.ImmediateContext.ClearRenderTargetView(camera.GBuffer.normal.GetView<RenderTargetView>(), new RawColor4(0.0f, 0.0f, 0.0f, 0.0f));
            CurrentDevice.ImmediateContext.ClearRenderTargetView(camera.GBuffer.metallic.GetView<RenderTargetView>(), new RawColor4(0.0f, 0.0f, 0.0f, 0.0f));
            CurrentDevice.ImmediateContext.ClearRenderTargetView(camera.GBuffer.roughness.GetView<RenderTargetView>(), new RawColor4(0.0f, 0.0f, 0.0f, 0.0f));
            CurrentDevice.ImmediateContext.ClearRenderTargetView(camera.GBuffer.ambientOcclusion.GetView<RenderTargetView>(), new RawColor4(0.0f, 0.0f, 0.0f, 0.0f));
            CurrentDevice.ImmediateContext.ClearRenderTargetView(camera.GBuffer.emission.GetView<RenderTargetView>(), new RawColor4(0.0f, 0.0f, 0.0f, 0.0f));
#endif
            CurrentDevice.ImmediateContext.ClearDepthStencilView(camera.DepthBuffer.GetView<DepthStencilView>(), DepthStencilClearFlags.Depth, 1.0f, 0);

            IEnumerable<MeshComponent> meshes = Scene.FindComponentsOfType<MeshComponent>().Where(m => m.LocalEnabled);
            if (ShaderPipeline.TryGetPipeline("deferred_geometry", out ShaderPipeline pipeline))
            {
                pipeline.Use();

                sampler.use("texSampler");

                pipeline.UpdateUniform("view", (Matrix4x4f)camera.GameObject.Transform.View);
                pipeline.UpdateUniform("proj", (Matrix4x4f)camera.Proj);

                foreach (MeshComponent meshComponent in meshes.Where(m => m is not SkeletalMeshComponent))
                {
                    if (!meshComponent.LocalEnabled)
                        continue;

                    Transform transform = meshComponent.GameObject.Transform;
                    pipeline.UpdateUniform("model", (Matrix4x4f)transform.Model);
                    pipeline.UpdateUniform("modelNorm", (Matrix4x4f)transform.Model.inverse().transposed());

                    pipeline.UploadUpdatedUniforms();
                    meshComponent.Render();
                }
            }

            if (ShaderPipeline.TryGetPipeline("deferred_geometry_skinned", out pipeline))
            {
                pipeline.Use();

                sampler.use("texSampler");

                pipeline.UpdateUniform("view", (Matrix4x4f)camera.GameObject.Transform.View);
                pipeline.UpdateUniform("proj", (Matrix4x4f)camera.Proj);

                foreach (MeshComponent meshComponent in meshes.Where(m => m is SkeletalMeshComponent))
                {
                    if (!meshComponent.LocalEnabled)
                        continue;

                    Transform transform = meshComponent.GameObject.Transform;
                    pipeline.UpdateUniform("model", (Matrix4x4f)transform.Model);
                    pipeline.UpdateUniform("modelNorm", (Matrix4x4f)transform.Model.inverse().transposed());

                    pipeline.UploadUpdatedUniforms();
                    meshComponent.Render();
                }
            }

            CurrentDevice.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.PointList;

            if (ShaderPipeline.TryGetPipeline("deferred_geometry_particles", out pipeline))
            {
                pipeline.Use();

                sampler.use("texSampler");

                pipeline.UpdateUniform("view", (Matrix4x4f)camera.GameObject.Transform.View);
                pipeline.UpdateUniform("proj", (Matrix4x4f)camera.Proj);

                pipeline.UpdateUniform("camDir", (Vector3f)camera.GameObject.Transform.Forward);
                pipeline.UpdateUniform("camUp", (Vector3f)camera.GameObject.Transform.Up);

                pipeline.UpdateUniform("size", new Vector2f(0.1f, 0.1f));

                foreach (ParticleSystem particleSystem in Scene.FindComponentsOfType<ParticleSystem>())
                {
                    if (!particleSystem.LocalEnabled)
                        continue;
                    pipeline.UpdateUniform("model", particleSystem.WorldSpaceParticles ? Matrix4x4f.Identity : (Matrix4x4f)particleSystem.GameObject.Transform.Model);

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

            CurrentDevice.ImmediateContext.Rasterizer.SetViewport(new Viewport(0, 0, camera.BackBuffer.Width, camera.BackBuffer.Height, 0.0f, 1.0f));
            CurrentDevice.ImmediateContext.OutputMerger.SetTargets(null, renderTargetView: camera.RadianceBuffer.GetView<RenderTargetView>());

            foreach (Light light in Scene.FindComponentsOfType<Light>())
            {
                if (!light.LocalEnabled)
                    continue;

                if (light.PrepareLightPass(camera))
                {
                    camera.GBuffer.WorldPos.Use("worldPosTex");
                    camera.GBuffer.Albedo.Use("albedoTex");
                    camera.GBuffer.Normal.Use("normalTex");
                    camera.GBuffer.Metallic.Use("metallicTex");
                    camera.GBuffer.Roughness.Use("roughnessTex");
                    sampler.use("texSampler");
                    CurrentDevice.ImmediateContext.Draw(6, 0);
                }
            }

            CurrentDevice.ImmediateContext.OutputMerger.BlendState = null;

            CurrentDevice.ImmediateContext.OutputMerger.SetTargets(null, renderTargetView: camera.ColorBuffer.GetView<RenderTargetView>());
            CurrentDevice.ImmediateContext.ClearRenderTargetView(camera.ColorBuffer.GetView<RenderTargetView>(), camera.BackgroundColor);

            if (!ShaderPipeline.TryGetPipeline("deferred_addLight", out ShaderPipeline pipeline))
                return;

            pipeline.Use();

            camera.GBuffer.WorldPos.Use("worldPosTex");
            camera.GBuffer.Albedo.Use("albedoTex");
            camera.GBuffer.AmbientOcclusion.Use("ambientOcclusionTex");
            camera.RadianceBuffer.Use("radianceTex");
            sampler.use("texSampler");
            CurrentDevice.ImmediateContext.Draw(6, 0);
        }

        private static void VolumetricPass(Camera camera)
        {
            CurrentDevice.ImmediateContext.Rasterizer.State = frontCullingRasterizer;
            CurrentDevice.ImmediateContext.OutputMerger.BlendState = blendingBlendState;

            Viewport viewport = new Viewport(0, 0, camera.BackBuffer.Width, camera.BackBuffer.Height, 0.0f, 1.0f);
            CurrentDevice.ImmediateContext.Rasterizer.SetViewport(viewport);
            CurrentDevice.ImmediateContext.OutputMerger.SetTargets(null, renderTargetView: camera.ColorBuffer.GetView<RenderTargetView>());

            if (!ShaderPipeline.TryGetPipeline("volume", out ShaderPipeline pipeline))
                return;

            pipeline.Use();

            camera.DepthBuffer.Use("depthTex");
            sampler.use("texSampler");

            pipeline.UpdateUniform("cam_near", (float)camera.Near);
            pipeline.UpdateUniform("cam_far", (float)camera.Far);
            pipeline.UpdateUniform("cam_farDivFarMinusNear", (float)(camera.Far / (camera.Far - camera.Near)));
            pipeline.UpdateUniform("invScreenSize", new Vector2f(1.0f / viewport.Width, 1.0f / viewport.Height));

            foreach (GasVolume volume in Scene.FindComponentsOfType<GasVolume>())
            {
                if (!volume.LocalEnabled)
                    continue;

                Transform transform = volume.GameObject.Transform;
                pipeline.UpdateUniform("modelViewProj", (Matrix4x4f)(camera.Proj * camera.GameObject.Transform.View * transform.Model));
                pipeline.UpdateUniform("invModelViewProj", (Matrix4x4f)(transform.View * camera.GameObject.Transform.Model * camera.InvProj));

                Vector3f halfSize = volume.Size * 0.5f;
                Vector3f relativeCamPos = (Vector3f)transform.View.TransformPoint(camera.GameObject.Transform.Position);
                pipeline.UpdateUniform("relCamPos", relativeCamPos);
                pipeline.UpdateUniform("camToHalfSize", halfSize - relativeCamPos);
                pipeline.UpdateUniform("camToMinusHalfSize", -halfSize - relativeCamPos);

                pipeline.UpdateUniform("absorptionCoef", (float)volume.AbsorptionCoef);
                pipeline.UpdateUniform("scatteringCoef", (float)volume.ScatteringCoef);
                pipeline.UpdateUniform("halfSize", (Vector3f)(volume.Size * 0.5f));

                //pipeline.UpdateUniform("ambientLight", new Vector3f(0.001f, 0.001f, 0.001f));
                pipeline.UpdateUniform("ambientLight", new Vector3f(0.5f, 0.5f, 0.5f));

                Vector3f lightDir = volume.LightDirection;
                if (lightDir.isZero())
                    lightDir = -Vector3f.Up;
                lightDir.normalize();
                pipeline.UpdateUniform("negLightDir", -lightDir);
                pipeline.UpdateUniform("invNegLightDir", -1.0f / lightDir);
                pipeline.UpdateUniform("lightIntensity", new Vector3f(5.0f, 5.0f, 5.0f));

                pipeline.UploadUpdatedUniforms();
                volume.Render();
            }

            CurrentDevice.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
        }

        private static void PrePostProcessingPass(Camera camera)
        {
            foreach (PostProcessEffect effect in camera.PostProcessEffects)
                effect.Process(camera.ColorBuffer);
        }

        private static void GammaCorrectionPass(Camera camera)
        {
            CurrentDevice.ImmediateContext.Rasterizer.State = backCullingRasterizer;
            CurrentDevice.ImmediateContext.OutputMerger.BlendState = null;

            CurrentDevice.ImmediateContext.Rasterizer.SetViewport(new Viewport(0, 0, camera.BackBuffer.Width, camera.BackBuffer.Height, 0.0f, 1.0f));
            CurrentDevice.ImmediateContext.OutputMerger.SetTargets(null, renderTargetView: camera.BackBuffer.RenderTargetTexture.GetView<RenderTargetView>());

            if (!ShaderPipeline.TryGetPipeline("deferred_gamma_correction", out ShaderPipeline pipeline))
                return;

            pipeline.Use();

            camera.ColorBuffer.Use("colorTex");
            sampler.use("texSampler");
            CurrentDevice.ImmediateContext.Draw(6, 0);
        }

        private static void RenderTexture(Camera camera, Texture tex)
        {
            CurrentDevice.ImmediateContext.Rasterizer.SetViewport(new Viewport(0, 0, camera.BackBuffer.Width, camera.BackBuffer.Height, 0.0f, 1.0f));
            CurrentDevice.ImmediateContext.OutputMerger.SetTargets(null, renderTargetView: camera.BackBuffer.RenderTargetTexture.GetView<RenderTargetView>());

            if (!ShaderPipeline.TryGetPipeline("tex_to_screen", out ShaderPipeline pipeline))
                return;

            pipeline.Use();

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

        private static void ClearViewportPanel(string _)
        {
            ViewportPanel.Dispatcher.Invoke(() => { ViewportPanel.Children.Clear(); });
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