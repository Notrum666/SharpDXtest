using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using SharpDX.Direct3D11;

namespace Engine
{
    public class ShaderPipeline
    {
        private readonly List<Shader> shaders = new List<Shader>();
        public ReadOnlyCollection<Shader> Shaders => shaders.AsReadOnly();
        public static ShaderPipeline Current { get; private set; }

        private static readonly Dictionary<string, ShaderPipeline> staticPipelines = new Dictionary<string, ShaderPipeline>();

        public static ShaderPipeline GetStaticPipeline(string name)
        {
            return staticPipelines[name];
        }

        public static ShaderPipeline CreateStaticPipeline(string pipelineName, params Shader[] shaders)
        {
            if (staticPipelines.ContainsKey(pipelineName))
                throw new ArgumentException($"Shader pipeline with name {pipelineName} is already loaded.");

            staticPipelines[pipelineName] = new ShaderPipeline(shaders);
            return GetStaticPipeline(pipelineName);
        }

        public static void InitializeStaticPipelines()
        {
            // CreateStaticPipeline("default", Shader.Create("BaseAssets\\Shaders\\pbr_lighting.vsh"),
            //                      Shader.Create("BaseAssets\\Shaders\\pbr_lighting.fsh"));
            
            CreateStaticPipeline("depth_only", Shader.Create("BaseAssets\\Shaders\\depth_only.vsh"),
                                 Shader.Create("BaseAssets\\Shaders\\depth_only.fsh"));

            CreateStaticPipeline("deferred_geometry", Shader.Create("BaseAssets\\Shaders\\DeferredRender\\deferred_geometry.vsh"),
                                 Shader.Create("BaseAssets\\Shaders\\DeferredRender\\deferred_geometry.fsh"));

            CreateStaticPipeline("deferred_geometry_particles", Shader.Create("BaseAssets\\Shaders\\DeferredRender\\deferred_geometry_particles.vsh"),
                                 Shader.Create("BaseAssets\\Shaders\\DeferredRender\\deferred_geometry_particles.gsh"),
                                 Shader.Create("BaseAssets\\Shaders\\DeferredRender\\deferred_geometry_particles.fsh"));
            CreateStaticPipeline("volume", Shader.Create("BaseAssets\\Shaders\\VolumetricRender\\volume.vsh"),
                                 Shader.Create("BaseAssets\\Shaders\\VolumetricRender\\volume.fsh"));

            Shader screenQuadShader = Shader.GetStaticShader("screen_quad");
            CreateStaticPipeline("deferred_light_point", screenQuadShader, Shader.Create("BaseAssets\\Shaders\\DeferredRender\\deferred_light_point.fsh"));
            CreateStaticPipeline("deferred_light_directional", screenQuadShader, Shader.Create("BaseAssets\\Shaders\\DeferredRender\\deferred_light_directional.fsh"));
            CreateStaticPipeline("deferred_addLight", screenQuadShader, Shader.Create("BaseAssets\\Shaders\\DeferredRender\\deffered_addLight.fsh"));
            CreateStaticPipeline("deferred_gamma_correction", screenQuadShader, Shader.Create("BaseAssets\\Shaders\\DeferredRender\\deferred_gamma_correction.fsh"));
        }

        public ShaderPipeline(params Shader[] shaders)
        {
            List<ShaderType> shaderTypes = new List<ShaderType>();
            foreach (Shader shader in shaders)
            {
                if (shaderTypes.Contains(shader.Type))
                    throw new ArgumentException("Shader pipeline can't have more than one shader of the same type.");
                shaderTypes.Add(shader.Type);
                this.shaders.Add(shader);
            }
            if (!shaderTypes.Contains(ShaderType.VertexShader))
                throw new ArgumentException("Vertex shader is required for shader pipeline.");
            if (!shaderTypes.Contains(ShaderType.FragmentShader))
                throw new ArgumentException("Fragment shader is required for shader pipeline.");
        }

        public void UpdateUniform(string name, object value)
        {
            bool exists = false;
            foreach (Shader shader in shaders)
            {
                if (shader.TryUpdateUniform(name, value))
                    exists = true;
            }

            if (!exists)
                throw new ArgumentException("Variable \n" + name + "\n does not exists in this shader pipeline.");
        }

        public void UploadTexture(string variable, ShaderResourceView view)
        {
            bool correctLocation = false;
            int location;

            foreach (Shader shader in shaders)
            {
                if (shader.Locations.TryGetValue(variable, out location))
                {
                    correctLocation = true;
                    GraphicsCore.CurrentDevice.ImmediateContext.PixelShader.SetShaderResource(location, view);
                }
            }

            if (!correctLocation)
                throw new ArgumentException("Variable " + variable + " not found in current pipeline.");
        }

        public bool TryUpdateUniform(string name, object value)
        {
            bool exists = false;
            foreach (Shader shader in shaders)
            {
                if (shader.TryUpdateUniform(name, value))
                    exists = true;
            }

            return exists;
        }

        public void Use()
        {
            GraphicsCore.CurrentDevice.ImmediateContext.PixelShader.Set(null);
            GraphicsCore.CurrentDevice.ImmediateContext.VertexShader.Set(null);
            GraphicsCore.CurrentDevice.ImmediateContext.GeometryShader.Set(null);
            foreach (Shader shader in shaders)
                shader.Use();
            Current = this;
        }

        public void UploadUpdatedUniforms()
        {
            foreach (Shader shader in shaders)
                shader.UploadUpdatedUniforms();
        }
    }
}