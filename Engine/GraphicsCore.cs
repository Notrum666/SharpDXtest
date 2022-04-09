using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Windows.Forms;

using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.Direct3D9;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using Device = SharpDX.Direct3D11.Device;
using Filter = SharpDX.Direct3D11.Filter;
using Light = Engine.BaseAssets.Components.Light;
using Mesh = Engine.BaseAssets.Components.Mesh;

using Engine.BaseAssets.Components;
using System.Windows.Interop;
using LinearAlgebra;

namespace Engine
{
    public static class GraphicsCore
    {
        private static Device device;
        //private static SwapChain swapchain;
        private static Texture renderTexture;
        private static Texture depthTexture;
        private static int width, height;
        private static bool disposed = false;

        public static Device CurrentDevice { get => device; }

        private static D3DImage d3dimage;
        private static SharpDX.Direct3D9.Texture d9texture;

        private static Sampler sampler;
        private static Sampler shadowsSampler;

        public static Camera CurrentCamera { get; set; }

        public static event Action<Texture2D> OnRedrawn;

        public static void Init(D3DImage d3dimage, IntPtr HWND, int width, int height)
        {
            GraphicsCore.width = width;
            GraphicsCore.height = height;
            GraphicsCore.d3dimage = d3dimage;

            InitDirectX(HWND, width, height);

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
            device = new Device(DriverType.Hardware, DeviceCreationFlags.Debug | DeviceCreationFlags.BgraSupport, FeatureLevel.Level_11_0);

            RasterizerState rastState = new RasterizerState(device, new RasterizerStateDescription()
            {
                FillMode = SharpDX.Direct3D11.FillMode.Solid,
                CullMode = CullMode.Back,
                IsFrontCounterClockwise = true,
                IsScissorEnabled = false,
                IsAntialiasedLineEnabled = true,
                IsDepthClipEnabled = true,
                IsMultisampleEnabled = true
            });
            device.ImmediateContext.Rasterizer.State = rastState;

            renderTexture = new Texture(width, height, Vector4f.Zero, false, BindFlags.RenderTarget | BindFlags.ShaderResource);
            depthTexture = new Texture(width, height);

            device.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            Direct3DEx context = new Direct3DEx();

            SharpDX.Direct3D9.Device d9device = new SharpDX.Direct3D9.Device(context,
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
            IntPtr renderTextureHandle = renderTexture.texture.QueryInterface<SharpDX.DXGI.Resource>().SharedHandle;
            d9texture = new SharpDX.Direct3D9.Texture(d9device,
                                                      width,
                                                      height,
                                                      1,
                                                      SharpDX.Direct3D9.Usage.RenderTarget,
                                                      SharpDX.Direct3D9.Format.A8R8G8B8,
                                                      Pool.Default,
                                                      ref renderTextureHandle);
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
                PresentTexture(d9texture);
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
            PresentTexture(d9texture);

            OnRedrawn?.Invoke(renderTexture.texture);
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
            PresentTexture(d9texture);
        }

        private static void PresentTexture(SharpDX.Direct3D9.Texture tex)
        {
            SharpDX.Direct3D9.Surface surface = tex.GetSurfaceLevel(0);

            d3dimage.Dispatcher.Invoke(() =>
            {
                d3dimage.Lock();

                d3dimage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, surface.NativePointer);
                d3dimage.AddDirtyRect(new System.Windows.Int32Rect(0, 0, renderTexture.texture.Description.Width, renderTexture.texture.Description.Height));

                d3dimage.Unlock();
            });
        }

        public static void Dispose()
        {
            if (!disposed)
            {
                device.Dispose();
                //swapchain.Dispose();
                //renderTarget.Dispose();

                disposed = true;
            }
        }
    }
}
