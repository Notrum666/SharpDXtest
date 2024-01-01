using System;
using System.Collections.Generic;

using LinearAlgebra;

using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

using Device = SharpDX.Direct3D11.Device;

namespace Engine.BaseAssets.Components.Postprocessing
{
    public class PostProcessEffect_Bloom : PostProcessEffect
    {
        public const int MaxIterationsCount = 16;
        
        private int iterations = 5;
        public Ranged<int> Iterations => new Ranged<int>(ref iterations, 1, MaxIterationsCount);

        private float treshold = 1.0f;
        public Ranged<float> Treshold => new Ranged<float>(ref treshold, 0.0f);

        private static Device device => GraphicsCore.CurrentDevice;

        private static Sampler sampler;
        private static RasterizerState backCullingRasterizerState;
        private static BlendState additiveBlendState;

        private static ShaderPipeline downsampleBoxPipeline;
        private static ShaderPipeline upsampleBoxPipeline;
        private static ShaderPipeline prefilterPipeline;


        private Texture[] samplingsTextures;
        private int inputTextureWidth;
        private int inputTextureHeight;

        static PostProcessEffect_Bloom()
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

            BlendStateDescription blendStateDesc = new BlendStateDescription()
            {
                AlphaToCoverageEnable = false,
                IndependentBlendEnable = false
            };

            blendStateDesc.RenderTarget[0] = new RenderTargetBlendDescription(
                true,
                BlendOption.One,
                BlendOption.One,
                BlendOperation.Add,
                BlendOption.Zero,
                BlendOption.One,
                BlendOperation.Add,
                ColorWriteMaskFlags.All
            );

            additiveBlendState = new BlendState(device, blendStateDesc);

            Shader screenQuadShader = Shader.GetStaticShader("screen_quad");
            downsampleBoxPipeline = new ShaderPipeline(new Shader[] { screenQuadShader, Shader.Create("BaseAssets\\Shaders\\Bloom\\bloom_downsample_box.fsh") });
            upsampleBoxPipeline = new ShaderPipeline(new Shader[] { screenQuadShader, Shader.Create("BaseAssets\\Shaders\\Bloom\\bloom_upsample_box.fsh") });
            prefilterPipeline = new ShaderPipeline(new Shader[] { screenQuadShader, Shader.Create("BaseAssets\\Shaders\\Bloom\\bloom_prefilter.fsh") });
        }

        public override void Process(Texture inputTexture)
        {
            if (inputTextureWidth != inputTexture.texture.Description.Width
                || inputTextureHeight != inputTexture.texture.Description.Height
                || samplingsTextures == null)
            {
                inputTextureWidth = inputTexture.texture.Description.Width;
                inputTextureHeight = inputTexture.texture.Description.Height;

                CreateSamplingTextures(inputTextureWidth, inputTextureHeight);
            }

            device.ImmediateContext.Rasterizer.State = backCullingRasterizerState;
            device.ImmediateContext.OutputMerger.BlendState = null;

            Prefilter(inputTexture, samplingsTextures[0], treshold);

            for (int i = 0; i < samplingsTextures.Length - 1; i++)
                DownsampleBox(samplingsTextures[i], samplingsTextures[i + 1]);

            device.ImmediateContext.OutputMerger.BlendState = additiveBlendState;

            for (int i = samplingsTextures.Length - 1; i > 0; i--)
                UpsampleBox(samplingsTextures[i], samplingsTextures[i - 1]);

            UpsampleBox(samplingsTextures[0], inputTexture);
        }

        private void CreateSamplingTextures(int width, int height)
        {
            List<Texture> textures = new List<Texture>(iterations);

            for (int i = 0; i < iterations; i++)
            {
                width /= 2;
                height /= 2;

                if (width == 0 || height == 0)
                    break;

                textures.Add(new Texture(width, height, null, Format.R32G32B32A32_Float, BindFlags.ShaderResource | BindFlags.RenderTarget));
            }

            if (textures.Count == 0)
                throw new Exception("Failed to create sample textures: target texture size is very small");

            samplingsTextures = textures.ToArray();
        }

        private void SetRenderTargetTexture(Texture texture)
        {
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

        private void DownsampleBox(Texture source, Texture dest)
        {
            SetRenderTargetTexture(dest);

            ShaderPipeline pipeline = downsampleBoxPipeline;

            pipeline.Use();

            BindSourceTexture(source, pipeline);

            pipeline.UploadUpdatedUniforms();

            device.ImmediateContext.Draw(6, 0);
        }

        private void UpsampleBox(Texture source, Texture dest)
        {
            SetRenderTargetTexture(dest);

            ShaderPipeline pipeline = upsampleBoxPipeline;

            pipeline.Use();

            BindSourceTexture(source, pipeline);

            pipeline.UploadUpdatedUniforms();

            device.ImmediateContext.Draw(6, 0);
        }

        private void Prefilter(Texture source, Texture dest, float treshold)
        {
            SetRenderTargetTexture(dest);

            ShaderPipeline pipeline = prefilterPipeline;

            pipeline.Use();

            BindSourceTexture(source, pipeline);
            pipeline.UpdateUniform("treshold", treshold);

            pipeline.UploadUpdatedUniforms();

            device.ImmediateContext.Draw(6, 0);
        }

        private void BindSourceTexture(Texture texture, ShaderPipeline pipeline)
        {
            pipeline.UpdateUniform(
                "texelSize",
                new Vector2f(
                    1.0f / texture.texture.Description.Width,
                    1.0f / texture.texture.Description.Height
                )
            );

            texture.Use("tex");
            sampler.use("texSampler");
        }
    }
}