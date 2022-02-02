using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Windows.Forms;

using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using Device = SharpDX.Direct3D11.Device;

using SharpDXtest.Assets.Components;
using SharpDXtest.BaseAssets.Components;

namespace SharpDXtest
{
    public class GraphicsCore
    {
        private static Device device;
        private static SwapChain swapchain;
        private static Texture renderTexture;
        private static Texture depthTexture;
        private static int width, height;
        private static bool disposed = false;

        public static Device CurrentDevice { get => device; }

        private static Sampler sampler;
        private static Sampler shadowsSampler;

        public static Camera CurrentCamera { get; set; }

        public static void Init(Control control)
        {
            width = control.ClientSize.Width;
            height = control.ClientSize.Height;

            InitDirectX(control.Handle, width, height);

            AssetsManager.Textures["default_albedo"] = new Texture(64, 64, new Vector4f(1.0f, 1.0f, 1.0f, 1.0f), true);
            AssetsManager.Textures["default_normal"] = new Texture(64, 64, new Vector4f(0.5f, 0.5f, 1.0f, 0.0f), false);
            AssetsManager.Textures["default_metallic"] = new Texture(64, 64, new Vector4f(0.1f, 0.0f, 0.0f, 0.0f), false);
            AssetsManager.Textures["default_roughness"] = new Texture(64, 64, new Vector4f(0.5f, 0.0f, 0.0f, 0.0f), false);
            AssetsManager.Textures["default_ambientOcclusion"] = new Texture(64, 64, new Vector4f(0.0f, 0.0f, 0.0f, 0.0f), false);

            AssetsManager.LoadShaderPipeline("default", Shader.Create("BaseAssets\\Shaders\\pbr_lighting.vsh"), 
                                                        Shader.Create("BaseAssets\\Shaders\\pbr_lighting.fsh"));
            sampler = AssetsManager.Samplers["default"] = new Sampler(TextureAddressMode.Clamp, TextureAddressMode.Clamp);

            AssetsManager.LoadShaderPipeline("depth_only", Shader.Create("BaseAssets\\Shaders\\depth_only.vsh"),
                                                                  Shader.Create("BaseAssets\\Shaders\\depth_only.fsh"));
            shadowsSampler = AssetsManager.Samplers["default_shadows"] = new Sampler(TextureAddressMode.Border, TextureAddressMode.Border, Filter.MinMagMipPoint, borderColor: new RawColor4(0.0f, 0.0f, 0.0f, 0.0f));

            AssetsManager.LoadShaderPipeline("tex_to_screen", Shader.Create("BaseAssets\\Shaders\\tex_to_screen.vsh"),
                                                              Shader.Create("BaseAssets\\Shaders\\tex_to_screen.fsh"));
        }
        private static void InitDirectX(IntPtr HWND, int width, int height)
        {
            SwapChainDescription sc_desc = new SwapChainDescription()
            {
                BufferCount = 1,
                Flags = SwapChainFlags.None,
                IsWindowed = true,
                OutputHandle = HWND,
                SwapEffect = SwapEffect.Discard,
                Usage = Usage.RenderTargetOutput,
                ModeDescription = new ModeDescription(width, height, new Rational(60, 1), Format.R8G8B8A8_UNorm),
                SampleDescription = new SampleDescription(1, 0)
            };

#if DEBUG
            Device.CreateWithSwapChain(DriverType.Reference,
                                       DeviceCreationFlags.Debug,
                                       new FeatureLevel[] { FeatureLevel.Level_11_0 },
                                       sc_desc,
                                       out device,
                                       out swapchain);
#else
            Device.CreateWithSwapChain(DriverType.Hardware,
                                       DeviceCreationFlags.None,
                                       new FeatureLevel[] { FeatureLevel.Level_11_0 },
                                       sc_desc,
                                       out device,
                                       out swapchain);
#endif

            RasterizerState rastState = new RasterizerState(device, new RasterizerStateDescription()
            {
                FillMode = FillMode.Solid,
                CullMode = CullMode.Back,
                IsFrontCounterClockwise = true,
                IsScissorEnabled = false,
                IsAntialiasedLineEnabled = true,
                IsDepthClipEnabled = true,
                IsMultisampleEnabled = true
            });
            device.ImmediateContext.Rasterizer.State = rastState;

            swapchain.ResizeBuffers(sc_desc.BufferCount, width, height, Format.Unknown, SwapChainFlags.None);

            renderTexture = new Texture(Texture2D.FromSwapChain<Texture2D>(swapchain, 0));
            depthTexture = new Texture(width, height);

            device.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
        }
        public static void Update()
        {
            RenderShadows();
            RenderScene();
        }

        private static void RenderShadows()
        {
            if (GameCore.CurrentScene == null)
                return;

            List<GameObject> objects = GameCore.CurrentScene.objects;

            ShaderPipeline pipeline = null;
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
                        device.ImmediateContext.Rasterizer.SetViewport(new Viewport(0, 0, curLight.ShadowSize, curLight.ShadowSize, 0.0f, 1.0f));
                        device.ImmediateContext.OutputMerger.SetTargets(curLight.ShadowTexture.DepthStencil, renderTargetView: curLight.ShadowTexture.RenderTarget);
                        device.ImmediateContext.ClearDepthStencilView(curLight.ShadowTexture.DepthStencil, DepthStencilClearFlags.Depth, 1.0f, 0);

                        pipeline.UpdateUniform("view", curLight.lightSpace);
                    }
                    else if (light is DirectionalLight)
                    {
                        DirectionalLight curLight = light as DirectionalLight;

                        pipeline = AssetsManager.ShaderPipelines["depth_only"];
                        pipeline.Use();
                        device.ImmediateContext.Rasterizer.SetViewport(new Viewport(0, 0, curLight.ShadowSize, curLight.ShadowSize, 0.0f, 1.0f));
                        device.ImmediateContext.OutputMerger.SetTargets(curLight.ShadowTexture.DepthStencil, renderTargetView: curLight.ShadowTexture.RenderTarget);
                        device.ImmediateContext.ClearDepthStencilView(curLight.ShadowTexture.DepthStencil, DepthStencilClearFlags.Depth, 1.0f, 0);

                        pipeline.UpdateUniform("view", curLight.lightSpace);
                    }
                    else if (light is PointLight)
                    {
                        PointLight curLight = light as PointLight;


                    }
                    else
                        continue;

                    foreach (GameObject obj in objects)
                    {
                        if (!obj.Enabled)
                            continue;
                        foreach (Mesh mesh in obj.getComponents<Mesh>())
                        {
                            if (!mesh.Enabled)
                                continue;
                            pipeline.UpdateUniform("model", (Matrix4x4f)obj.transform.Model);

                            pipeline.UploadUpdatedUniforms();

                            mesh.model.Render();
                        }
                    }
                }
            }
        }

        private static void RenderScene()
        {
            device.ImmediateContext.Rasterizer.SetViewport(new Viewport(0, 0, width, height, 0.0f, 1.0f));
            device.ImmediateContext.OutputMerger.SetTargets(depthTexture.DepthStencil, renderTexture.RenderTarget);

            device.ImmediateContext.ClearRenderTargetView(renderTexture.RenderTarget, Color.FromRgba(0xFF323232));
            device.ImmediateContext.ClearDepthStencilView(depthTexture.DepthStencil, DepthStencilClearFlags.Depth, 1.0f, 0);

            if (GameCore.CurrentScene == null || CurrentCamera == null || !CurrentCamera.Enabled)
            {
                device.ImmediateContext.Flush();
                swapchain.Present(0, PresentFlags.None);
                return;
            }

            List<GameObject> objects = GameCore.CurrentScene.objects;

            ShaderPipeline pipeline = AssetsManager.ShaderPipelines["default"];
            pipeline.Use();

            int spotLights = 0;
            int directionalLights = 0;
            int pointLights = 0;
            int ambientLights = 0;
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
                        string baseLocation = "spotLights[" + spotLights.ToString() + "].";

                        pipeline.UpdateUniform(baseLocation + "position", (Vector3f)obj.transform.Position);
                        pipeline.UpdateUniform(baseLocation + "direction", (Vector3f)obj.transform.Forward);
                        pipeline.UpdateUniform(baseLocation + "radius", curLight.Radius);
                        pipeline.UpdateUniform(baseLocation + "brightness", curLight.Brightness);
                        pipeline.UpdateUniform(baseLocation + "intensity", curLight.Intensity);
                        pipeline.UpdateUniform(baseLocation + "angularIntensity", curLight.AngularIntensity);
                        pipeline.UpdateUniform(baseLocation + "angle", curLight.Angle / 2.0f);
                        pipeline.UpdateUniform(baseLocation + "color", curLight.color);

                        pipeline.UpdateUniform(baseLocation + "lightSpace", curLight.lightSpace);

                        curLight.ShadowTexture.use(baseLocation + "shadowMap");

                        spotLights++;
                    }
                    else if (light is DirectionalLight)
                    {
                        DirectionalLight curLight = light as DirectionalLight;
                        string baseLocation = "directionalLights[" + directionalLights.ToString() + "].";

                        pipeline.UpdateUniform(baseLocation + "direction", (Vector3f)curLight.gameObject.transform.Forward);
                        pipeline.UpdateUniform(baseLocation + "brightness", curLight.Brightness);
                        pipeline.UpdateUniform(baseLocation + "color", curLight.color);

                        pipeline.UpdateUniform(baseLocation + "lightSpace", curLight.lightSpace);

                        curLight.ShadowTexture.use(baseLocation + "shadowMap");

                        directionalLights++;
                    }
                    else if (light is PointLight)
                    {
                        PointLight curLight = light as PointLight;
                        string baseLocation = "pointLights[" + pointLights.ToString() + "].";

                        pipeline.UpdateUniform(baseLocation + "position", (Vector3f)curLight.gameObject.transform.Position);
                        pipeline.UpdateUniform(baseLocation + "radius", curLight.Radius);
                        pipeline.UpdateUniform(baseLocation + "brightness", curLight.Brightness);
                        pipeline.UpdateUniform(baseLocation + "intensity", curLight.Intensity);
                        pipeline.UpdateUniform(baseLocation + "color", curLight.color);

                        pointLights++;
                    }
                    else if (light is AmbientLight)
                    {
                        AmbientLight curLight = light as AmbientLight;
                        string baseLocation = "ambientLights[" + ambientLights.ToString() + "].";

                        pipeline.UpdateUniform(baseLocation + "brightness", curLight.Brightness);
                        pipeline.UpdateUniform(baseLocation + "color", curLight.color);

                        ambientLights++;
                    }
                    else
                        throw new NotImplementedException("Light type " + light.GetType().Name + " is not supported.");
                }
            }

            pipeline.UpdateUniform("spotLightsCount", spotLights);
            pipeline.UpdateUniform("directionalLightsCount", directionalLights);
            pipeline.UpdateUniform("pointLightsCount", pointLights);
            pipeline.UpdateUniform("ambientLightsCount", ambientLights);

            pipeline.UpdateUniform("spotLight_NEAR", SpotLight.NEAR);

            sampler.use("texSampler");
            shadowsSampler.use("shadowSampler");

            pipeline.UpdateUniform("camPos", (Vector3f)CurrentCamera.gameObject.transform.Position);

            pipeline.UpdateUniform("view", (Matrix4x4f)CurrentCamera.gameObject.transform.View);
            pipeline.UpdateUniform("proj", (Matrix4x4f)CurrentCamera.proj);

            foreach (GameObject obj in objects)
            {
                if (!obj.Enabled)
                    continue;
                foreach (Mesh mesh in obj.getComponents<Mesh>())
                {
                    if (!mesh.Enabled)
                        continue;
                    pipeline.UpdateUniform("model", (Matrix4x4f)obj.transform.Model);

                    pipeline.UploadUpdatedUniforms();
                    mesh.Material.Albedo.use("albedoMap");
                    mesh.Material.Normal.use("normalMap");
                    mesh.Material.Metallic.use("metallicMap");
                    mesh.Material.Roughness.use("roughnessMap");
                    mesh.Material.AmbientOcclusion.use("ambientOcclusionMap");
                    mesh.model.Render();
                }
            }

            device.ImmediateContext.Flush();
            swapchain.Present(0, PresentFlags.None);
        }

        private static void RenderTexture(Texture tex)
        {
            device.ImmediateContext.Rasterizer.SetViewport(new Viewport(0, 0, width, height, 0.0f, 1.0f));
            device.ImmediateContext.OutputMerger.SetTargets(null, renderTargetView: renderTexture.RenderTarget);

            AssetsManager.ShaderPipelines["tex_to_screen"].Use();

            tex.use("tex");
            sampler.use("texSampler");
            CurrentDevice.ImmediateContext.Draw(6, 0);

            device.ImmediateContext.Flush();
            swapchain.Present(0, PresentFlags.None);
        }

        public static void Dispose()
        {
            if (!disposed)
            {
                device.Dispose();
                swapchain.Dispose();
                //renderTarget.Dispose();

                disposed = true;
            }
        }
    }
}
