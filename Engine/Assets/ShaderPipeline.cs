using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using SharpDX.Direct3D11;

namespace Engine
{
    public class ShaderPipeline
    {
        private readonly List<Shader> shaders = new List<Shader>();
        public ReadOnlyCollection<Shader> Shaders => shaders.AsReadOnly();
        public static ShaderPipeline Current { get; private set; }

        private static readonly Dictionary<string, ShaderPipeline> registeredPipelines = new Dictionary<string, ShaderPipeline>();

        public static bool TryGetPipeline(string pipelineName, out ShaderPipeline pipeline)
        {
            if (registeredPipelines.TryGetValue(pipelineName, out pipeline))
                return true;

            Logger.Log(LogType.Error, $"Tried to get pipeline \"{pipelineName}\", but it is not registered");
            return false;
        }

        public static void LoadPipeline(string pipelineName, IEnumerable<string> shadersNames)
        {
            if (registeredPipelines.ContainsKey(pipelineName))
                throw new ArgumentException($"Shader pipeline with name {pipelineName} is already loaded.");

            IEnumerable<Shader> shaders = shadersNames.Select(AssetsManager.LoadAssetAtPath<Shader>);
            registeredPipelines[pipelineName] = new ShaderPipeline(shaders.ToArray());
        }

        public static void UnloadPipeline(string pipelineName)
        {
            registeredPipelines.Remove(pipelineName);
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