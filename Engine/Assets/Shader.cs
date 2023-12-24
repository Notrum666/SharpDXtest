using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
    public abstract class Shader
    {
        public abstract ShaderType Type { get; }
        public Dictionary<string, int> Locations { get; } = new Dictionary<string, int>();
        protected List<ShaderBuffer> buffers = new List<ShaderBuffer>();

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

        protected void generateBuffersAndLocations(ShaderReflection reflection)
        {
            for (int i = 0; i < reflection.Description.ConstantBuffers; i++)
            {
                ConstantBuffer buffer = reflection.GetConstantBuffer(i);
                if (buffer.Description.Type != ConstantBufferType.ConstantBuffer)
                    continue;
                ShaderBuffer shaderBuffer = new ShaderBuffer();
                shaderBuffer.buffer = new Buffer(GraphicsCore.CurrentDevice, buffer.Description.Size, ResourceUsage.Dynamic, BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None, 0);
                for (int j = 0; j < buffer.Description.VariableCount; j++)
                    shaderBuffer.variables.AddRange(parseShaderVariable(buffer.GetVariable(j)));

                buffers.Add(shaderBuffer);
            }
            for (int i = 0; i < reflection.Description.BoundResources; i++)
            {
                InputBindingDescription desc = reflection.GetResourceBindingDescription(i);
                switch (desc.Type)
                {
                    case ShaderInputType.Texture:
                    case ShaderInputType.Sampler:
                        Locations[desc.Name] = desc.BindPoint;
                        break;
                }
            }
        }

        private Dictionary<string, ShaderVariable> parseShaderVariable(ShaderReflectionVariable variable)
        {
            return parseShaderVariableType(variable.GetVariableType(), variable.Description.Name, variable.Description.StartOffset, variable.Description.Size);
        }

        private Dictionary<string, ShaderVariable> parseShaderVariableType(ShaderReflectionType type, string varName, int parentOffset, int varSize)
        {
            int elementCount = type.Description.ElementCount;
            if (elementCount == 0)
                return parseNonArrayShaderVariableType(type, varName, parentOffset, varSize);

            int elementOffset = (int)Math.Ceiling(varSize / (double)elementCount / 16.0) * 16;
            int elementSize = varSize - elementOffset * (elementCount - 1);
            Dictionary<string, ShaderVariable> variables = new Dictionary<string, ShaderVariable>();

            for (int i = 0; i < elementCount; i++)
                variables.AddRange(parseNonArrayShaderVariableType(type, varName + "[" + i.ToString() + "]", parentOffset + i * elementOffset, elementSize));

            return variables;
        }

        private Dictionary<string, ShaderVariable> parseNonArrayShaderVariableType(ShaderReflectionType type, string varName, int parentOffset, int varSize)
        {
            Dictionary<string, ShaderVariable> variables = new Dictionary<string, ShaderVariable>();
            if (type.Description.MemberCount == 0)
                variables.Add(varName, new ShaderVariable() { offset = parentOffset + type.Description.Offset, size = varSize, value = null });
            else
            {
                for (int i = 0; i < type.Description.MemberCount; i++)
                {
                    ShaderReflectionType subtype = type.GetMemberType(i);
                    int memberSize;
                    if (i == type.Description.MemberCount - 1)
                        memberSize = varSize - subtype.Description.Offset;
                    else
                        memberSize = type.GetMemberType(i + 1).Description.Offset - subtype.Description.Offset;
                    variables.AddRange(parseShaderVariableType(subtype, varName + "." + type.GetMemberTypeName(i), parentOffset + type.Description.Offset, memberSize));
                }
            }
            return variables;
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

        public static Shader Create(string path)
        {
            string extension = Path.GetExtension(path);
            switch (extension)
            {
                case ".vsh":
                    return Create(path, ShaderType.VertexShader);
                case ".hsh":
                    return Create(path, ShaderType.HullShader);
                case ".dsh":
                    return Create(path, ShaderType.DomainShader);
                case ".gsh":
                    return Create(path, ShaderType.GeometryShader);
                case ".fsh":
                    return Create(path, ShaderType.FragmentShader);
                case ".csh":
                    return Create(path, ShaderType.ComputeShader);
                default:
                    throw new ArgumentException("Can't get shader type from extension, consider using other Shader.Create() overload.");
            }
        }

        public static Shader Create(string path, ShaderType type)
        {
            switch (type)
            {
                case ShaderType.VertexShader:
                    return new Shader_Vertex(path);
                case ShaderType.HullShader:
                    return new Shader_Hull(path);
                case ShaderType.DomainShader:
                    return new Shader_Domain(path);
                case ShaderType.GeometryShader:
                    return new Shader_Geometry(path);
                case ShaderType.FragmentShader:
                    return new Shader_Fragment(path);
                case ShaderType.ComputeShader:
                    return new Shader_Compute(path);
                default:
                    throw new NotImplementedException();
            }
        }

        protected class ShaderBuffer
        {
            public Dictionary<string, ShaderVariable> variables = new Dictionary<string, ShaderVariable>();
            public Buffer buffer;
            public bool invalidated = true;
        }
        protected class ShaderVariable
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

            public Shader_Vertex(string path)
            {
#if !GraphicsDebugging
                ShaderBytecode bytecode = ShaderBytecode.CompileFromFile(path, "main", "vs_5_0");
#else
                ShaderBytecode bytecode = ShaderBytecode.CompileFromFile(path, "main", "vs_5_0", ShaderFlags.Debug | ShaderFlags.SkipOptimization);
#endif
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
                generateBuffersAndLocations(reflection);
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

            public Shader_Hull(string path)
            {
                ShaderBytecode bytecode = ShaderBytecode.CompileFromFile(path, "main", "hs_5_0");
                ShaderReflection reflection = new ShaderReflection(bytecode);
                shader = new HullShader(GraphicsCore.CurrentDevice, bytecode);
                generateBuffersAndLocations(reflection);
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

            public Shader_Domain(string path)
            {
                ShaderBytecode bytecode = ShaderBytecode.CompileFromFile(path, "main", "ds_5_0");
                ShaderReflection reflection = new ShaderReflection(bytecode);
                shader = new DomainShader(GraphicsCore.CurrentDevice, bytecode);
                generateBuffersAndLocations(reflection);
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

            public Shader_Geometry(string path)
            {
#if !GraphicsDebugging
                ShaderBytecode bytecode = ShaderBytecode.CompileFromFile(path, "main", "gs_5_0");
#else
                ShaderBytecode bytecode = ShaderBytecode.CompileFromFile(path, "main", "gs_5_0", ShaderFlags.Debug | ShaderFlags.SkipOptimization);
#endif
                ShaderReflection reflection = new ShaderReflection(bytecode);
                shader = new GeometryShader(GraphicsCore.CurrentDevice, bytecode);
                generateBuffersAndLocations(reflection);
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

            public Shader_Fragment(string path)
            {
#if !GraphicsDebugging
                ShaderBytecode bytecode = ShaderBytecode.CompileFromFile(path, "main", "ps_5_0", ShaderFlags.SkipOptimization);
#else
                ShaderBytecode bytecode = ShaderBytecode.CompileFromFile(path, "main", "ps_5_0", ShaderFlags.Debug | ShaderFlags.SkipOptimization);
#endif
                ShaderReflection reflection = new ShaderReflection(bytecode);
                shader = new PixelShader(GraphicsCore.CurrentDevice, bytecode);
                generateBuffersAndLocations(reflection);
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

            public Shader_Compute(string path)
            {
                ShaderBytecode bytecode = ShaderBytecode.CompileFromFile(path, "main", "cs_5_0");
                ShaderReflection reflection = new ShaderReflection(bytecode);
                shader = new ComputeShader(GraphicsCore.CurrentDevice, bytecode);
                generateBuffersAndLocations(reflection);
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