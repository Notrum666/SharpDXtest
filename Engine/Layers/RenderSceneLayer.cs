using Engine.BaseAssets.Components;
using LinearAlgebra;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.Mathematics.Interop;
using SharpDX;
using System.Collections.Generic;
using System;
using Engine.BaseAssets.Components.Postprocessing;

namespace Engine.Layers
{
    internal class RenderSceneLayer : Layer
    {
        public override float UpdateOrder => 3;

        public override float InitOrder => 3;

        private static Device CurrentDevice => GraphicsCore.CurrentDevice;

        private static Sampler sampler;
        private static Sampler shadowsSampler;

        private static RasterizerState backCullingRasterizer;
        private static RasterizerState frontCullingRasterizer;

        public static BlendState additiveBlendState;
        private static BlendState blendingBlendState;

        private static PostProcessEffect_Bloom bloomEffect;

        public override void Init()
        {
            sampler = AssetsManager_Old.Samplers["default"] = new Sampler(TextureAddressMode.Wrap, TextureAddressMode.Wrap);
            shadowsSampler = AssetsManager_Old.Samplers["default_shadows"] = new Sampler(TextureAddressMode.Border, TextureAddressMode.Border, Filter.ComparisonMinMagMipLinear, 0, new RawColor4(0.0f, 0.0f, 0.0f, 0.0f), Comparison.LessEqual);
            
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

            bloomEffect = new PostProcessEffect_Bloom();
        }

        public override void Update()
        {  
            RenderScene(GraphicsCore.CurrentCamera);
        }

        public static void RenderScene(Camera camera)
        {
            if (camera == null)
                return;

            camera.PreRenderUpdate();

            CurrentDevice.ImmediateContext.ClearRenderTargetView(camera.Backbuffer.RenderTargetTexture.GetView<RenderTargetView>(), camera.BackgroundColor);
            if (EngineCore.CurrentScene == null || !camera.Enabled || camera.GameObject == null)
            {
                GraphicsCore.FlushAndSwapFrameBuffers(camera);
                return;
            }

            GeometryPass(camera);
            LightingPass(camera);
            VolumetricPass(camera);
            PrePostProcessingPass(camera);
            GammaCorrectionPass(camera);

            GraphicsCore.FlushAndSwapFrameBuffers(camera);
#if GraphicsDebugging
            swapChain.Present(1, 0);
#endif
        }

        private static void RenderShadows(Camera camera)
        {
            if (EngineCore.CurrentScene == null)
                return;

            CurrentDevice.ImmediateContext.Rasterizer.State = frontCullingRasterizer;
            CurrentDevice.ImmediateContext.OutputMerger.BlendState = null;

            IReadOnlyList<GameObject> objects = EngineCore.CurrentScene.Objects;

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

                        pipeline = AssetsManager_Old.ShaderPipelines["depth_only"];
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

                        pipeline = AssetsManager_Old.ShaderPipelines["depth_only"];
                        pipeline.Use();

                        CurrentDevice.ImmediateContext.Rasterizer.SetViewport(new Viewport(0, 0, curLight.ShadowSize, curLight.ShadowSize, 0.0f, 1.0f));

                        Matrix4x4f[] lightSpaces = curLight.GetLightSpaces(camera);
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

            IReadOnlyList<GameObject> objects = EngineCore.CurrentScene.Objects;

            ShaderPipeline pipeline = AssetsManager_Old.ShaderPipelines["deferred_geometry"];
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

            pipeline = AssetsManager_Old.ShaderPipelines["deferred_geometry_particles"];
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

            IReadOnlyList<GameObject> objects = EngineCore.CurrentScene.Objects;

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
                        ShaderPipeline pipeline = AssetsManager_Old.ShaderPipelines["deferred_light_directional"];
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
                        ShaderPipeline pipeline = AssetsManager_Old.ShaderPipelines["deferred_light_point"];
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

            AssetsManager_Old.ShaderPipelines["deferred_addLight"].Use();

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

            IReadOnlyList<GameObject> objects = EngineCore.CurrentScene.Objects;

            ShaderPipeline pipeline = AssetsManager_Old.ShaderPipelines["volume"];
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

            AssetsManager_Old.ShaderPipelines["deferred_gamma_correction"].Use();

            camera.ColorBuffer.Use("colorTex");
            sampler.use("texSampler");
            CurrentDevice.ImmediateContext.Draw(6, 0);
        }
    }
}
