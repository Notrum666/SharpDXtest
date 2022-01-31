using System;
using System.IO;
using System.Collections.Generic;
using System.Windows.Forms;

using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;

using SharpDXtest.Assets.Components;
using SharpDXtest.BaseAssets.Components;

namespace SharpDXtest
{
    public class GraphicsCore
    {
        private static Device device;
        private static SwapChain swapchain;
        private static RenderTargetView renderTarget;
        private static DepthStencilView depthView;
        private static bool disposed = false;

        public static Device CurrentDevice { get => device; }

        private static Sampler sampler;
        private static ShaderPipeline pipeline;

        public static Camera CurrentCamera { get; set; }

        public static void Init(Control control)
        {
            InitDirectX(control);

            AssetsManager.Textures["default_albedo"] = Texture.SolidColor(64, 64, new Vector3f(1.0f, 1.0f, 1.0f), 255, true);
            AssetsManager.Textures["default_normal"] = Texture.SolidColor(64, 64, new Vector3f(0.5f, 0.5f, 1.0f), 255, false);
            AssetsManager.Textures["default_metallic"] = Texture.SolidColor(64, 64, new Vector3f(0.1f, 0.0f, 0.0f), 0, false);
            AssetsManager.Textures["default_roughness"] = Texture.SolidColor(64, 64, new Vector3f(0.5f, 0.0f, 0.0f), 0, false);
            AssetsManager.Textures["default_ambientOcclusion"] = Texture.SolidColor(64, 64, new Vector3f(0.0f, 0.0f, 0.0f), 0, false);

            pipeline = AssetsManager.LoadShaderPipeline("default", Shader.Create("BaseAssets\\Shaders\\default.vsh"), 
                                                                   Shader.Create("BaseAssets\\Shaders\\default.fsh"));
            sampler = AssetsManager.Samplers["default"] = new Sampler(TextureAddressMode.Clamp, TextureAddressMode.Clamp);

            device.ImmediateContext.Rasterizer.SetViewport(new Viewport(0, 0, control.ClientSize.Width, control.ClientSize.Height, 0.0f, 1.0f));
            device.ImmediateContext.OutputMerger.SetTargets(depthView, renderTarget);
        }
        private static void InitDirectX(Control control)
        {
            SwapChainDescription sc_desc = new SwapChainDescription()
            {
                BufferCount = 1,
                Flags = SwapChainFlags.None,
                IsWindowed = true,
                OutputHandle = control.Handle,
                SwapEffect = SwapEffect.Discard,
                Usage = Usage.RenderTargetOutput,
                ModeDescription = new ModeDescription(control.ClientSize.Width, control.ClientSize.Height, new Rational(60, 1), Format.R8G8B8A8_UNorm),
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

            swapchain.ResizeBuffers(sc_desc.BufferCount, control.ClientSize.Width, control.ClientSize.Height, Format.Unknown, SwapChainFlags.None);

            Texture2D backBuffer = Texture2D.FromSwapChain<Texture2D>(swapchain, 0);
            renderTarget = new RenderTargetView(device, backBuffer);
            Texture2D depthBuffer = new Texture2D(device, new Texture2DDescription()
            {
                Format = Format.D32_Float_S8X24_UInt,
                ArraySize = 1,
                MipLevels = 1,
                Width = control.ClientSize.Width,
                Height = control.ClientSize.Height,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.DepthStencil,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            });
            depthView = new DepthStencilView(device, depthBuffer);

            device.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
        }
        public static void Update()
        {
            device.ImmediateContext.ClearRenderTargetView(renderTarget, Color.FromRgba(0xFF323232));
            device.ImmediateContext.ClearDepthStencilView(depthView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);

            if (GameCore.CurrentScene == null || CurrentCamera == null || !CurrentCamera.Enabled)
            {
                device.ImmediateContext.Flush();
                swapchain.Present(0, PresentFlags.None);
                return;
            }

            List<GameObject> objects = GameCore.CurrentScene.objects;

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

                        pipeline.UpdateUniform(baseLocation + "lightSpace", (Matrix4x4f)obj.transform.View);

                        spotLights++;
                    }
                    else if (light is DirectionalLight)
                    {
                        DirectionalLight curLight = light as DirectionalLight;
                        string baseLocation = "directionalLights[" + directionalLights.ToString() + "].";

                        pipeline.UpdateUniform(baseLocation + "direction", (Vector3f)curLight.gameObject.transform.Forward);
                        pipeline.UpdateUniform(baseLocation + "brightness", curLight.Brightness);
                        pipeline.UpdateUniform(baseLocation + "color", curLight.color);

                        pipeline.UpdateUniform(baseLocation + "lightSpace", (Matrix4x4f)obj.transform.View);

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

            pipeline.UpdateUniform("camPos", (Vector3f)CurrentCamera.gameObject.transform.Position);

            pipeline.UpdateUniform("exposure", 1.0f);

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
                    sampler.use("texSampler");
                    mesh.model.Render();
                }
            }

            device.ImmediateContext.Flush();
            swapchain.Present(0, PresentFlags.None);
        }

        public static void Dispose()
        {
            if (!disposed)
            {
                device.Dispose();
                swapchain.Dispose();
                renderTarget.Dispose();

                disposed = true;
            }
        }
    }
}
