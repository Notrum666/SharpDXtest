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
        private static bool disposed = false;

        public static Device CurrentDevice { get; private set; }
        public static SharpDX.Direct3D9.Device D9Device { get; private set; }

        private static Sampler sampler;
        private static Sampler shadowsSampler;

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

        public static FrameBuffer Frontbuffer { get; private set; }
        private static FrameBuffer middlebuffer;
        private static FrameBuffer backbuffer;

        private static bool needsToBeResized;
        private static int targetWidth;
        private static int targetHeight;
        private static object resizeLockObject = new object();

        public static void Init(IntPtr HWND, int width, int height)
        {
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
            CurrentDevice = new Device(DriverType.Hardware, DeviceCreationFlags.Debug | DeviceCreationFlags.BgraSupport, FeatureLevel.Level_11_0);

            RasterizerState rastState = new RasterizerState(CurrentDevice, new RasterizerStateDescription()
            {
                FillMode = SharpDX.Direct3D11.FillMode.Solid,
                CullMode = CullMode.Back,
                IsFrontCounterClockwise = true,
                IsScissorEnabled = false,
                IsAntialiasedLineEnabled = true,
                IsDepthClipEnabled = true,
                IsMultisampleEnabled = true
            });
            CurrentDevice.ImmediateContext.Rasterizer.State = rastState;

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

            Frontbuffer = new FrameBuffer(new Texture(width, height, Vector4f.Zero, false, BindFlags.RenderTarget | BindFlags.ShaderResource),
                                          new Texture(width, height, 0.0f));
            backbuffer = new FrameBuffer(new Texture(width, height, Vector4f.Zero, false, BindFlags.RenderTarget | BindFlags.ShaderResource),
                                         new Texture(width, height, 0.0f));
            middlebuffer = new FrameBuffer(new Texture(width, height, Vector4f.Zero, false, BindFlags.RenderTarget | BindFlags.ShaderResource),
                                           new Texture(width, height, 0.0f));
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
                    //Frontbuffer.Dispose();
                    //middlebuffer.Dispose();
                    //backbuffer.Dispose();
                    Frontbuffer = new FrameBuffer(targetWidth, targetHeight);
                    middlebuffer = new FrameBuffer(targetWidth, targetHeight);
                    backbuffer = new FrameBuffer(targetWidth, targetHeight);

                    needsToBeResized = false;
                }
            }

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
                        CurrentDevice.ImmediateContext.Rasterizer.SetViewport(new Viewport(0, 0, curLight.ShadowSize, curLight.ShadowSize, 0.0f, 1.0f));
                        CurrentDevice.ImmediateContext.OutputMerger.SetTargets(curLight.ShadowTexture.DepthStencil, renderTargetView: curLight.ShadowTexture.RenderTarget);
                        CurrentDevice.ImmediateContext.ClearDepthStencilView(curLight.ShadowTexture.DepthStencil, DepthStencilClearFlags.Depth, 1.0f, 0);

                        pipeline.UpdateUniform("view", curLight.lightSpace);
                    }
                    else if (light is DirectionalLight)
                    {
                        DirectionalLight curLight = light as DirectionalLight;

                        pipeline = AssetsManager.ShaderPipelines["depth_only"];
                        pipeline.Use();
                        CurrentDevice.ImmediateContext.Rasterizer.SetViewport(new Viewport(0, 0, curLight.ShadowSize, curLight.ShadowSize, 0.0f, 1.0f));
                        CurrentDevice.ImmediateContext.OutputMerger.SetTargets(curLight.ShadowTexture.DepthStencil, renderTargetView: curLight.ShadowTexture.RenderTarget);
                        CurrentDevice.ImmediateContext.ClearDepthStencilView(curLight.ShadowTexture.DepthStencil, DepthStencilClearFlags.Depth, 1.0f, 0);

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
            CurrentDevice.ImmediateContext.Rasterizer.SetViewport(new Viewport(0, 0, backbuffer.Width, backbuffer.Height, 0.0f, 1.0f));
            CurrentDevice.ImmediateContext.OutputMerger.SetTargets(backbuffer.DepthTexture.DepthStencil, backbuffer.ColorTexture.RenderTarget);

            CurrentDevice.ImmediateContext.ClearRenderTargetView(backbuffer.ColorTexture.RenderTarget, Color.FromRgba(0xFF202020));
            CurrentDevice.ImmediateContext.ClearDepthStencilView(backbuffer.DepthTexture.DepthStencil, DepthStencilClearFlags.Depth, 1.0f, 0);

            if (GameCore.CurrentScene == null || CurrentCamera == null || !CurrentCamera.Enabled)
            {
                CurrentDevice.ImmediateContext.Flush();
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
            pipeline.UpdateUniform("proj", (Matrix4x4f)CurrentCamera.Proj);

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

            CurrentDevice.ImmediateContext.Flush();
            SwapFramebuffers();
        }

        private static void RenderTexture(Texture tex)
        {
            CurrentDevice.ImmediateContext.Rasterizer.SetViewport(new Viewport(0, 0, backbuffer.Width, backbuffer.Height, 0.0f, 1.0f));
            CurrentDevice.ImmediateContext.OutputMerger.SetTargets(null, renderTargetView: backbuffer.ColorTexture.RenderTarget);

            AssetsManager.ShaderPipelines["tex_to_screen"].Use();

            tex.use("tex");
            sampler.use("texSampler");
            CurrentDevice.ImmediateContext.Draw(6, 0);

            CurrentDevice.ImmediateContext.Flush();
            SwapFramebuffers();
        }

        private static void SwapFramebuffers()
        {
            CurrentDevice.ImmediateContext.ResolveSubresource(backbuffer.ColorTexture.texture, 0, middlebuffer.ColorTexture.texture, 0, SharpDX.DXGI.Format.B8G8R8A8_UNorm);
            lock (Frontbuffer)
            {
                FrameBuffer tmp = Frontbuffer;
                Frontbuffer = middlebuffer;
                middlebuffer = tmp;
            }
        }

        public static void Dispose()
        {
            if (!disposed)
            {
                CurrentDevice.Dispose();
                Frontbuffer.Dispose();
                middlebuffer.Dispose();
                backbuffer.Dispose();

                disposed = true;
            }
        }
    }
}
