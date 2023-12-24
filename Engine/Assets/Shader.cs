using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Buffer = SharpDX.Direct3D11.Buffer;
using MapFlags = SharpDX.Direct3D11.MapFlags;

namespace Engine
{
    public abstract class Shader : BaseAsset
    {
        public abstract ShaderType Type { get; }

        public Dictionary<string, int> Locations { get; } = new Dictionary<string, int>();
        protected List<ShaderBuffer> buffers = new List<ShaderBuffer>();

        private static readonly Dictionary<string, Shader> staticShaders = new Dictionary<string, Shader>();

        public static Shader LoadStaticShader(string name)
        {
            return staticShaders[name];
        }

        public static Shader CreateStaticShader(string shaderName, string path)
        {
            if (staticShaders.ContainsKey(shaderName))
                throw new ArgumentException($"Shader with name {shaderName} is already loaded.");
            
            staticShaders[shaderName] = Create(path);
            return LoadStaticShader(shaderName);
        }

        public static Shader Create(string path)
        {
            return AssetsManager.LoadAssetAtPath<Shader>(path);
        }

        public static Shader Create(string path, ShaderType type) //TODO: Remove?
        {
            Shader shader = AssetsManager.LoadAssetAtPath<Shader>(path);
            return shader.Type == type ? shader : null;
        }

        public static Shader Create(ShaderType type, byte[] bytecode)
        {
            switch (type)
            {
                case ShaderType.VertexShader:
                    return new Shader_Vertex(bytecode);
                case ShaderType.HullShader:
                    return new Shader_Hull(bytecode);
                case ShaderType.DomainShader:
                    return new Shader_Domain(bytecode);
                case ShaderType.GeometryShader:
                    return new Shader_Geometry(bytecode);
                case ShaderType.FragmentShader:
                    return new Shader_Fragment(bytecode);
                case ShaderType.ComputeShader:
                    return new Shader_Compute(bytecode);
                default:
                    throw new NotImplementedException();
            }
        }

        public abstract void use();

        public bool hasVariable(string name)
        {
            foreach (ShaderBuffer buffer in buffers)
            {
                if (buffer.variables.ContainsKey(name))
                    return true;
            }
            return false;
        }

        public void updateUniform(string name, object value)
        {
            ShaderVariable variable;
            foreach (ShaderBuffer buffer in buffers)
            {
                if (buffer.variables.TryGetValue(name, out variable))
                {
                    if (variable.size < Marshal.SizeOf(value))
                        throw new ArgumentException("Value size can't be bigger than " + variable.size.ToString() + " bytes for \"" + name + "\".");
                    variable.value = value;
                    buffer.invalidated = true;
                    return;
                }
            }
            throw new ArgumentException("Variable does not exists.");
        }

        public bool tryUpdateUniform(string name, object value)
        {
            ShaderVariable variable;
            foreach (ShaderBuffer buffer in buffers)
            {
                if (buffer.variables.TryGetValue(name, out variable))
                {
                    if (variable.size < Marshal.SizeOf(value))
                        return false;
                    variable.value = value;
                    buffer.invalidated = true;
                    return true;
                }
            }
            return false;
        }

        public void uploadUpdatedUniforms()
        {
            foreach (ShaderBuffer buf in buffers)
            {
                if (buf.invalidated)
                {
                    DataStream stream;
                    GraphicsCore.CurrentDevice.ImmediateContext.MapSubresource(buf.buffer, 0, MapMode.WriteDiscard, MapFlags.None, out stream);
                    foreach (ShaderVariable variable in buf.variables.Values)
                    {
                        if (variable.value == null)
                            continue;

                        Marshal.StructureToPtr(variable.value, stream.PositionPointer + variable.offset, true);
                    }
                    GraphicsCore.CurrentDevice.ImmediateContext.UnmapSubresource(buf.buffer, 0);
                    buf.invalidated = false;
                }
            }
        }

        internal void AddBuffer(ShaderBuffer shaderBuffer)
        {
            buffers.Add(shaderBuffer);
        }

        public class ShaderBuffer
        {
            public Dictionary<string, ShaderVariable> variables = new Dictionary<string, ShaderVariable>();
            public Buffer buffer;
            public bool invalidated = true;
        }
        public class ShaderVariable
        {
            public int size;
            public int offset;
            public object value;
        }

        private class Shader_Vertex : Shader
        {
            public override ShaderType Type => ShaderType.VertexShader;
            private InputLayout layout;
            private VertexShader shader;

            public Shader_Vertex(byte[] bytecode)
            {
                ShaderReflection reflection = new ShaderReflection(bytecode);
                List<InputElement> inputDescription = new List<InputElement>();
                for (int i = 0; i < reflection.Description.InputParameters; i++)
                {
                    ShaderParameterDescription shaderParameterDescription = reflection.GetInputParameterDescription(i);

                    InputElement inputElementDescription = new InputElement();
                    inputElementDescription.SemanticName = shaderParameterDescription.SemanticName;
                    inputElementDescription.SemanticIndex = shaderParameterDescription.SemanticIndex;
                    inputElementDescription.Slot = 0;
                    inputElementDescription.AlignedByteOffset = InputElement.AppendAligned;
                    inputElementDescription.Classification = InputClassification.PerVertexData;
                    inputElementDescription.InstanceDataStepRate = 0;
                    switch ((int)shaderParameterDescription.UsageMask)
                    {
                        case 1:
                            switch (shaderParameterDescription.ComponentType)
                            {
                                case RegisterComponentType.Float32:
                                    inputElementDescription.Format = Format.R32_Typeless;
                                    break;
                                case RegisterComponentType.UInt32:
                                    inputElementDescription.Format = Format.R32_UInt;
                                    break;
                                case RegisterComponentType.SInt32:
                                    inputElementDescription.Format = Format.R32_SInt;
                                    break;
                            }
                            break;
                        case 3:
                            switch (shaderParameterDescription.ComponentType)
                            {
                                case RegisterComponentType.Float32:
                                    inputElementDescription.Format = Format.R32G32_Float;
                                    break;
                                case RegisterComponentType.UInt32:
                                    inputElementDescription.Format = Format.R32G32_UInt;
                                    break;
                                case RegisterComponentType.SInt32:
                                    inputElementDescription.Format = Format.R32G32_SInt;
                                    break;
                            }
                            break;
                        case 7:
                            switch (shaderParameterDescription.ComponentType)
                            {
                                case RegisterComponentType.Float32:
                                    inputElementDescription.Format = Format.R32G32B32_Float;
                                    break;
                                case RegisterComponentType.UInt32:
                                    inputElementDescription.Format = Format.R32G32B32_UInt;
                                    break;
                                case RegisterComponentType.SInt32:
                                    inputElementDescription.Format = Format.R32G32B32_SInt;
                                    break;
                            }
                            break;
                        case 15:
                            switch (shaderParameterDescription.ComponentType)
                            {
                                case RegisterComponentType.Float32:
                                    inputElementDescription.Format = Format.R32G32B32A32_Float;
                                    break;
                                case RegisterComponentType.UInt32:
                                    inputElementDescription.Format = Format.R32G32B32A32_UInt;
                                    break;
                                case RegisterComponentType.SInt32:
                                    inputElementDescription.Format = Format.R32G32B32A32_SInt;
                                    break;
                            }
                            break;
                    }

                    inputDescription.Add(inputElementDescription);
                }
                layout = new InputLayout(GraphicsCore.CurrentDevice, bytecode, inputDescription.ToArray());
                shader = new VertexShader(GraphicsCore.CurrentDevice, bytecode);
            }

            public override void use()
            {
                GraphicsCore.CurrentDevice.ImmediateContext.InputAssembler.InputLayout = layout;
                GraphicsCore.CurrentDevice.ImmediateContext.VertexShader.Set(shader);
                GraphicsCore.CurrentDevice.ImmediateContext.VertexShader.SetConstantBuffers(0, buffers.Select(buf => buf.buffer).ToArray());
            }
        }
        private class Shader_Hull : Shader
        {
            public override ShaderType Type => ShaderType.HullShader;
            private HullShader shader;

            public Shader_Hull(byte[] bytecode)
            {
                shader = new HullShader(GraphicsCore.CurrentDevice, bytecode);
            }

            public override void use()
            {
                GraphicsCore.CurrentDevice.ImmediateContext.HullShader.Set(shader);
                GraphicsCore.CurrentDevice.ImmediateContext.HullShader.SetConstantBuffers(0, buffers.Select(buf => buf.buffer).ToArray());
            }
        }
        private class Shader_Domain : Shader
        {
            public override ShaderType Type => ShaderType.DomainShader;
            private DomainShader shader;

            public Shader_Domain(byte[] bytecode)
            {
                shader = new DomainShader(GraphicsCore.CurrentDevice, bytecode);
            }

            public override void use()
            {
                GraphicsCore.CurrentDevice.ImmediateContext.DomainShader.Set(shader);
                GraphicsCore.CurrentDevice.ImmediateContext.DomainShader.SetConstantBuffers(0, buffers.Select(buf => buf.buffer).ToArray());
            }
        }
        private class Shader_Geometry : Shader
        {
            public override ShaderType Type => ShaderType.GeometryShader;
            private GeometryShader shader;

            public Shader_Geometry(byte[] bytecode)
            {
                shader = new GeometryShader(GraphicsCore.CurrentDevice, bytecode);
            }

            public override void use()
            {
                GraphicsCore.CurrentDevice.ImmediateContext.GeometryShader.Set(shader);
                GraphicsCore.CurrentDevice.ImmediateContext.GeometryShader.SetConstantBuffers(0, buffers.Select(buf => buf.buffer).ToArray());
            }
        }
        private class Shader_Fragment : Shader
        {
            public override ShaderType Type => ShaderType.FragmentShader;
            private PixelShader shader;

            public Shader_Fragment(byte[] bytecode)
            {
                shader = new PixelShader(GraphicsCore.CurrentDevice, bytecode);
            }

            public override void use()
            {
                GraphicsCore.CurrentDevice.ImmediateContext.PixelShader.Set(shader);
                GraphicsCore.CurrentDevice.ImmediateContext.PixelShader.SetConstantBuffers(0, buffers.Select(buf => buf.buffer).ToArray());
            }
        }
        private class Shader_Compute : Shader
        {
            public override ShaderType Type => ShaderType.ComputeShader;
            private ComputeShader shader;

            public Shader_Compute(byte[] bytecode)
            {
                shader = new ComputeShader(GraphicsCore.CurrentDevice, bytecode);
            }

            public override void use()
            {
                GraphicsCore.CurrentDevice.ImmediateContext.ComputeShader.Set(shader);
                GraphicsCore.CurrentDevice.ImmediateContext.ComputeShader.SetConstantBuffers(0, buffers.Select(buf => buf.buffer).ToArray());
            }
        }
    }

    public enum ShaderType
    {
        VertexShader,
        HullShader,
        DomainShader,
        GeometryShader,
        FragmentShader,
        ComputeShader
    }
}