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

        private static Transform transform;
        private static Model obj;
        private static Texture tex;
        private static Sampler sampler;

        private static Controller cameraController;

        private static ShaderPipeline pipeline;

        public static void Init(Control control)
        {
            InitDirectX(control);

            GameObject cameraObject = new GameObject();
            cameraController = (Controller) cameraObject.addComponent<Controller>();
            Camera camera = (Camera)cameraObject.addComponent<Camera>();
            camera.resolution = control.ClientSize.Width / control.ClientSize.Height;
            camera.FOV = 95.0 / 180.0 * Math.PI;
            camera.near = 0.01;
            camera.far = 100;
            camera.MakeCurrent();

            obj = AssetsManager.LoadModelsFile("Assets\\Models\\cube.obj")["cube"];
            transform = new Transform();
            transform.position = new Vector3(0.0, 10, 0.0);
            transform.rotation = Quaternion.FromEuler(new Vector3(0.0, 0.0, Math.PI / 4));

            pipeline = AssetsManager.LoadShaderPipeline("default", Shader.Create("BaseAssets\\Shaders\\default.vsh"), 
                                                                   Shader.Create("BaseAssets\\Shaders\\default.fsh"));
            tex = AssetsManager.LoadTexture("BaseAssets\\Textures\\template.png", "default", false);
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

            Device.CreateWithSwapChain(DriverType.Reference,
                                       DeviceCreationFlags.Debug,
                                       new FeatureLevel[] { FeatureLevel.Level_11_0 },
                                       sc_desc,
                                       out device,
                                       out swapchain);

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
            cameraController.update();

            device.ImmediateContext.ClearRenderTargetView(renderTarget, Color.FromRgba(0xFF323232));
            device.ImmediateContext.ClearDepthStencilView(depthView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);
            pipeline.Use();

            pipeline.UpdateUniform("model", (Matrix4x4f)transform.model);
            pipeline.UpdateUniform("view", (Matrix4x4f)Camera.Current.view);
            pipeline.UpdateUniform("proj", (Matrix4x4f)Camera.Current.proj);

            pipeline.UploadUpdatedUniforms();

            tex.use("tex");
            sampler.use("texSampler");

            obj.Render();

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
