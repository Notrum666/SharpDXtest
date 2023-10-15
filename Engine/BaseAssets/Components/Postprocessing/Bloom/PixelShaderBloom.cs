using LinearAlgebra;
using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct3D11;
using System;
using System.Windows.Documents;
using System.Collections.Generic;

namespace Engine.BaseAssets.Components.Postprocessing.Bloom
{
    public class PixelShaderBloom : PostProcessEffect
    {
        private const int MaxIterationsCount = 16;
        private SharpDX.Direct3D11.Device device => GraphicsCore.CurrentDevice;

        private Texture[] samplingsTextures;
        private Sampler sampler;
        private RasterizerState backCullingRasterizerState;
        private BlendState additiveBlendState;

        private ShaderPipeline downsampleBoxPipeline;
        private ShaderPipeline upsampleBoxPipeline;
        private ShaderPipeline prefilterPipeline;

        private int samplingsCount;

        public PixelShaderBloom(int screenWidth, int screenHeight, int iterationsCount = 5)
        {
            if(iterationsCount <= 0 && iterationsCount > MaxIterationsCount)
                throw new ArgumentOutOfRangeException(nameof(iterationsCount));

            samplingsCount = iterationsCount;

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

            Shader screenQuadShader = AssetsManager.Shaders["screen_quad"];
            downsampleBoxPipeline = new ShaderPipeline(new Shader[] { screenQuadShader, Shader.Create("BaseAssets\\Shaders\\Bloom\\bloom_downsample_box.fsh") });
            upsampleBoxPipeline = new ShaderPipeline(new Shader[] { screenQuadShader, Shader.Create("BaseAssets\\Shaders\\Bloom\\bloom_upsample_box.fsh") });
            prefilterPipeline = new ShaderPipeline(new Shader[] { screenQuadShader, Shader.Create("BaseAssets\\Shaders\\Bloom\\bloom_prefilter.fsh") });

            Resize(screenWidth, screenHeight);
        }

        public override void Resize(int width, int height)
        {
            samplingsTextures = CreateSamplingTextures(width, height);
        }

        public override void Process(Texture targetTexture)
        {
            device.ImmediateContext.Rasterizer.State = backCullingRasterizerState;
            device.ImmediateContext.OutputMerger.BlendState = null;

            Prefilter(targetTexture, samplingsTextures[0], 1.0f);

            for (int i = 0; i < samplingsTextures.Length - 1; i++)
                DownsampleBox(samplingsTextures[i], samplingsTextures[i + 1]);

            device.ImmediateContext.OutputMerger.BlendState = additiveBlendState;

            for (int i = samplingsTextures.Length - 1; i > 0; i--)
                UpsampleBox(samplingsTextures[i], samplingsTextures[i - 1]);

            UpsampleBox(samplingsTextures[0], targetTexture);
        }

        private Texture[] CreateSamplingTextures(int width, int height)
        {
            List<Texture> textures = new List<Texture>(samplingsCount);

            for (int i = 0; i < samplingsCount; i++)
            {
                width /= 2;
                height /= 2;

                if (width == 0 || height == 0)
                    break;

                textures.Add(
                    new Texture(
                        width,
                        height,
                        Vector4f.Zero.GetBytes(),
                        Format.R32G32B32A32_Float,
                        BindFlags.ShaderResource | BindFlags.RenderTarget
                    )
                );
            }

            if (textures.Count == 0)
                throw new Exception("Failed to create sample textures: target texture size is very small");

            return textures.ToArray();
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
                "texSize",
                new Vector2f(
                    texture.texture.Description.Width,
                    texture.texture.Description.Height
                )
            );

            texture.use("tex");
            sampler.use("texSampler");
        }
    }
}
