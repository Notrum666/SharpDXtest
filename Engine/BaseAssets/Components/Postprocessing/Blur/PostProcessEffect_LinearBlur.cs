using SharpDX.Direct3D11;
using SharpDX;
using SharpDX.DXGI;
using LinearAlgebra;

namespace Engine.BaseAssets.Components.Postprocessing
{
    public class PostProcessEffect_LinearBlur : PostProcessEffect
    {
        private static SharpDX.Direct3D11.Device device => GraphicsCore.CurrentDevice;
        private static Sampler sampler;
        private static RasterizerState backCullingRasterizerState;

        private static ShaderPipeline pipeline;

        private Texture texture;
        private int inputTextureWidth;
        private int inputTextureHeight;

        static PostProcessEffect_LinearBlur()
        {
            sampler = new Sampler(TextureAddressMode.Clamp, TextureAddressMode.Clamp, Filter.MinMagMipLinear);

            backCullingRasterizerState = new RasterizerState(device, new RasterizerStateDescription()
            {
                FillMode = FillMode.Solid,
                CullMode = CullMode.Back,
                IsFrontCounterClockwise = true,
                IsScissorEnabled = false,
                IsAntialiasedLineEnabled = true,
                IsDepthClipEnabled = true,
                IsMultisampleEnabled = true
            });

            Shader screenQuadShader = AssetsManager.Shaders["screen_quad"];
            pipeline = new ShaderPipeline(new Shader[] { screenQuadShader, Shader.Create("BaseAssets\\Shaders\\Blur\\linear_blur.fsh") });
        }

        private void PrepareRenderTarget(Texture texture)
        {
            device.ImmediateContext.Rasterizer.State = backCullingRasterizerState;
            device.ImmediateContext.Rasterizer.SetViewport(
                new Viewport(
                    0,
                    0,
                    texture.texture.Description.Width,
                    texture.texture.Description.Height,
                    0.0f,
                    1.0f
                )
            );

            device.ImmediateContext.OutputMerger.SetTargets(
                null,
                renderTargetView: texture.GetView<RenderTargetView>()
            );
        }

        public override void Process(Texture inputTexture)
        {
            if (inputTextureWidth != inputTexture.texture.Description.Width
                || inputTextureHeight != inputTexture.texture.Description.Height)
            {
                inputTextureWidth = inputTexture.texture.Description.Width;
                inputTextureHeight = inputTexture.texture.Description.Height;

                texture?.Dispose();
                texture = new Texture(inputTextureWidth, inputTextureHeight, null, Format.R32G32B32A32_Float, BindFlags.ShaderResource | BindFlags.RenderTarget);
            }

            ProcessBlur(inputTexture, texture);
            ProcessBlur(texture, inputTexture);
        }

        private void ProcessBlur(Texture input, Texture output)
        {
            PrepareRenderTarget(output);

            pipeline.Use();

            pipeline.UpdateUniform(
            "texelSize",
            new Vector2f(
                    1.0f / input.texture.Description.Width,
                    1.0f / input.texture.Description.Height
                )
            );

            pipeline.UploadUpdatedUniforms();

            input.use("inputTex");
            sampler.use("texSampler");

            device.ImmediateContext.Draw(6, 0);
        }
    }
}
