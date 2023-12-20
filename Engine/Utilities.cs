using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Enumeration;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Linq;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Text;
using System.Text.RegularExpressions;

using Engine.BaseAssets.Components;

using LinearAlgebra;

using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using SharpDX.Multimedia;
using SharpDX.XAudio2;

using Assimp;

using Buffer = SharpDX.Direct3D11.Buffer;
using MapFlags = SharpDX.Direct3D11.MapFlags;
using Quaternion = LinearAlgebra.Quaternion;
using Rectangle = System.Drawing.Rectangle;
using Vector2 = LinearAlgebra.Vector2;
using Vector3 = LinearAlgebra.Vector3;

namespace Engine
{
    // model contains a number of meshes
    // materials from model should be loaded as separate assets too
    // materials contain a number of textures
    // each of them: meshes, materials, textures should be loaded as separate assets

    public class Primitive : IDisposable
    {
        private bool disposed;

        public struct PrimitiveVertex
        {
            public Vector3f v;
            public Vector2f t;
            public Vector3f n;
            public Vector3f tx;
        };
        public List<PrimitiveVertex> vertices = null;
        public List<int> indices = null;

        private Buffer vertexBuffer;
        private VertexBufferBinding vertexBufferBinding;
        private Buffer indexBuffer;

        // material assigned on mesh load
        public Material DefaultMaterial { get; set; } = null;

        ~Primitive()
        {
            Dispose(disposing: false);
        }

        public void GenerateGPUData()
        {
            if (vertices == null || indices == null)
                throw new Exception("Geometry data can't be empty.");

            for (int i = 0; i < indices.Count / 3; i++)
            {
                Vector3f edge1 = vertices[i * 3 + 1].v - vertices[i * 3 + 0].v;
                Vector3f edge2 = vertices[i * 3 + 2].v - vertices[i * 3 + 0].v;
                Vector2f UVedge1 = vertices[i * 3 + 1].t - vertices[i * 3 + 0].t;
                Vector2f UVedge2 = vertices[i * 3 + 2].t - vertices[i * 3 + 0].t;
                Vector3f tx = ((edge1 * UVedge2.y - edge2 * UVedge1.y) / (UVedge1.x * UVedge2.y - UVedge1.y * UVedge2.x)).normalized();
                PrimitiveVertex vertex0 = vertices[i * 3 + 0];
                PrimitiveVertex vertex1 = vertices[i * 3 + 1];
                PrimitiveVertex vertex2 = vertices[i * 3 + 2];

                vertex0.tx = new Vector3f(tx.x, tx.y, tx.z);
                vertex1.tx = new Vector3f(tx.x, tx.y, tx.z);
                vertex2.tx = new Vector3f(tx.x, tx.y, tx.z);

                vertices[i * 3 + 0] = vertex0;
                vertices[i * 3 + 1] = vertex1;
                vertices[i * 3 + 2] = vertex2;
            }

            vertexBuffer = Buffer.Create(GraphicsCore.CurrentDevice, BindFlags.VertexBuffer, vertices.ToArray());
            vertexBufferBinding = new VertexBufferBinding(vertexBuffer, Utilities.SizeOf<PrimitiveVertex>(), 0);
            indexBuffer = Buffer.Create(GraphicsCore.CurrentDevice, BindFlags.IndexBuffer, indices.ToArray());
        }

        public void Render()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(Primitive));
            GraphicsCore.CurrentDevice.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            GraphicsCore.CurrentDevice.ImmediateContext.InputAssembler.SetVertexBuffers(0, vertexBufferBinding);
            GraphicsCore.CurrentDevice.ImmediateContext.InputAssembler.SetIndexBuffer(indexBuffer, Format.R32_UInt, 0);
            GraphicsCore.CurrentDevice.ImmediateContext.DrawIndexed(indices.Count, 0, 0);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            vertices = null;
            indices = null;

            if (vertexBuffer != null)
                vertexBuffer.Dispose();
            if (indexBuffer != null)
                indexBuffer.Dispose();

            disposed = true;
        }
    }
    public class Mesh : IDisposable
    {
        private bool disposed;
        public List<Primitive> Primitives { get; } = new List<Primitive>();

        ~Mesh()
        {
            Dispose(disposing: false);
        }

        public void Render()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(Mesh));
            foreach (Primitive primitive in Primitives)
                primitive.Render();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                foreach(Primitive primitive in Primitives)
                    primitive.Dispose(disposing);
                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    };
    public class Material
    {
        private Texture albedo;
        public Texture Albedo
        {
            get => albedo;
            set
            {
                if (value == null)
                    throw new ArgumentNullException("Albedo", "Texture can't be null.");
                albedo = value;
            }
        }
        private Texture normal;
        public Texture Normal
        {
            get => normal;
            set
            {
                if (value == null)
                    throw new ArgumentNullException("Normal", "Texture can't be null.");
                normal = value;
            }
        }
        private Texture metallic;
        public Texture Metallic
        {
            get => metallic;
            set
            {
                if (value == null)
                    throw new ArgumentNullException("Metallic", "Texture can't be null.");
                metallic = value;
            }
        }
        private Texture roughness;
        public Texture Roughness
        {
            get => roughness;
            set
            {
                if (value == null)
                    throw new ArgumentNullException("Roughness", "Texture can't be null.");
                roughness = value;
            }
        }
        private Texture ambientOcclusion;
        public Texture AmbientOcclusion
        {
            get => ambientOcclusion;
            set
            {
                if (value == null)
                    throw new ArgumentNullException("AmbientOcclusion", "Texture can't be null.");
                ambientOcclusion = value;
            }
        }
        private Texture emissive;
        public Texture Emissive
        {
            get => emissive;
            set
            {
                if (value == null)
                    throw new ArgumentNullException("Emissive", "Texture can't be null.");
                emissive = value;
            }
        }
        public Material()
        {
            albedo = AssetsManager_Old.Textures["default_albedo"];
            normal = AssetsManager_Old.Textures["default_normal"];
            metallic = AssetsManager_Old.Textures["default_metallic"];
            roughness = AssetsManager_Old.Textures["default_roughness"];
            ambientOcclusion = AssetsManager_Old.Textures["default_ambientOcclusion"];
            emissive = AssetsManager_Old.Textures["default_emissive"];
        }
        public Material(Texture albedo, Texture normal, Texture metallic, Texture roughness, Texture ambientOcclusion, Texture emissive)
        {
            Albedo = albedo;
            Normal = normal;
            Metallic = metallic;
            Roughness = roughness;
            AmbientOcclusion = ambientOcclusion;
            Emissive = emissive;
        }

        public void Use()
        {
            Albedo.Use("albedoMap");
            Normal.Use("normalMap");
            Metallic.Use("metallicMap");
            Roughness.Use("roughnessMap");
            AmbientOcclusion.Use("ambientOcclusionMap");
            Emissive.Use("emissiveMap");
            ShaderPipeline.Current.UploadUpdatedUniforms();
        }
    }
    public class Sampler : IDisposable
    {
        private SamplerState sampler;
        private bool disposed;

        public Sampler(TextureAddressMode addressU, TextureAddressMode addressV, Filter filter = Filter.Anisotropic, int maximumAnisotropy = 8, RawColor4 borderColor = new RawColor4(), Comparison comparisonFunction = Comparison.Always, TextureAddressMode addressW = TextureAddressMode.Clamp)
        {
            sampler = new SamplerState(GraphicsCore.CurrentDevice, new SamplerStateDescription()
            {
                AddressU = addressU,
                AddressV = addressV,
                AddressW = addressW,
                Filter = filter,
                MaximumAnisotropy = maximumAnisotropy,
                MipLodBias = 0,
                MinimumLod = 0,
                MaximumLod = float.MaxValue,
                BorderColor = borderColor,
                ComparisonFunction = comparisonFunction
            });
        }

        public void use(string variable)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(Sampler));
            bool correctLocation = false;
            int location;
            foreach (Shader shader in ShaderPipeline.Current.Shaders)
            {
                if (shader.Locations.TryGetValue(variable, out location))
                {
                    correctLocation = true;
                    GraphicsCore.CurrentDevice.ImmediateContext.PixelShader.SetSampler(location, sampler);
                }
            }
            if (!correctLocation)
                throw new ArgumentException("Variable " + variable + " not found in current pipeline.");
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                sampler.Dispose();

                disposed = true;
            }
        }

        ~Sampler()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
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
    public class ShaderPipeline
    {
        private List<Shader> shaders = new List<Shader>();
        public ReadOnlyCollection<Shader> Shaders => shaders.AsReadOnly();
        public static ShaderPipeline Current { get; private set; }

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
                if (shader.tryUpdateUniform(name, value))
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
                if (shader.tryUpdateUniform(name, value))
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
                shader.use();
            Current = this;
        }

        public void UploadUpdatedUniforms()
        {
            foreach (Shader shader in shaders)
                shader.uploadUpdatedUniforms();
        }
    }
    public class Scene
    {
        public List<GameObject> objects { get; } = new List<GameObject>();
    }
    
    [Obsolete]
    public static class AssetsManager_Old
    {
        public static Dictionary<string, Mesh> Meshes { get; } = new Dictionary<string, Mesh>();
        public static Dictionary<string, ShaderPipeline> ShaderPipelines { get; } = new Dictionary<string, ShaderPipeline>();
        public static Dictionary<string, Shader> Shaders { get; } = new Dictionary<string, Shader>();
        public static Dictionary<string, Material> Materials { get; } = new Dictionary<string, Material>();
        public static Dictionary<string, Texture> Textures { get; } = new Dictionary<string, Texture> ();
        public static Dictionary<string, Sampler> Samplers { get; } = new Dictionary<string, Sampler>();
        public static Dictionary<string, Scene> Scenes { get; } = new Dictionary<string, Scene>();
        public static Dictionary<string, Sound> Sounds { get; } = new Dictionary<string, Sound>();

        // loads meshes, materials and textures stored in one model file
        public static void LoadModel(string path, float scaleFactor = 1.0f)
        {
            string modelName = Path.GetFileNameWithoutExtension(path);
            string texturePrefix = modelName + "_texture";
            string materialPrefix = modelName + "_materials";

            AssimpContext aiImporter = new AssimpContext();

            Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
            Dictionary<string, Material> materials = new Dictionary<string, Material>();

            // any type model import
            Assimp.Scene aiScene = aiImporter.ImportFile(path);

            Dictionary<string, Texture> textures = new Dictionary<string, Texture>();
            for (int i = 0; i < aiScene.Textures.Count; ++i)
            {
                EmbeddedTexture aiTexture = aiScene.Textures[i];
                if (aiTexture.HasCompressedData)
                    textures.Add(texturePrefix + i.ToString(), new Texture(Texture.DecodeTexture(aiTexture.CompressedData)));
                else if (aiTexture.Filename.Length > 0)
                    textures.Add(texturePrefix + i.ToString(), new Texture(new Bitmap(aiTexture.Filename)));
            }

            for (int i = 0; i < aiScene.Materials.Count; ++i)
            {
                Assimp.Material aiMaterial = aiScene.Materials[i];
                Material material = new Material();
                Texture albedo = null;
                Texture normal = null;
                if (aiMaterial.GetMaterialTextureCount(TextureType.BaseColor) > 0)
                {
                    TextureSlot textureSlot;
                    aiMaterial.GetMaterialTexture(TextureType.BaseColor, 0, out textureSlot);
                    albedo = textures[texturePrefix + textureSlot.TextureIndex.ToString()];
                }
                else if (aiMaterial.HasColorDiffuse)
                {
                    Color4D color = aiMaterial.ColorDiffuse;
                    albedo = new Texture(64, 64, new Vector4f(color.B, color.G, color.R, color.A).GetBytes(), Format.R32G32B32A32_Float, BindFlags.ShaderResource);
                }

                if (aiMaterial.GetMaterialTextureCount(TextureType.Normals) > 0)
                {
                    TextureSlot textureSlot;
                    aiMaterial.GetMaterialTexture(TextureType.Normals, 0, out textureSlot);
                    normal = textures[texturePrefix + textureSlot.TextureIndex.ToString()];
                }

                if (albedo != null)
                    material.Albedo = albedo;
                if (normal != null)
                    material.Normal = normal;

                materials.Add(materialPrefix + i.ToString(), material);
            }

            Materials.AddRange(materials);

            foreach (Assimp.Mesh aiMesh in aiScene.Meshes)
            {
                // handle only triangles
                if (aiMesh.PrimitiveType != PrimitiveType.Triangle)
                {
                    Logger.Log(LogType.Warning, path + ": Non-triangulated primitives are not supported");
                    continue;
                }

                Mesh mesh;

                // in order to store different primitives into one mesh trying to find it by name
                if (!meshes.TryGetValue(aiMesh.Name, out mesh))
                {
                    mesh = new Mesh();
                    meshes.Add(aiMesh.Name, mesh);
                }

                Primitive primitive = new Primitive();
                primitive.DefaultMaterial = materials[materialPrefix + aiMesh.MaterialIndex.ToString()];
                primitive.vertices = new List<Primitive.PrimitiveVertex>();
                primitive.indices = new List<int>();
                List<Vector3D> verts = aiMesh.Vertices;
                List<Vector3D> norms = (aiMesh.HasNormals) ? aiMesh.Normals : null;
                List<Vector3D> uvs = aiMesh.HasTextureCoords(0) ? aiMesh.TextureCoordinateChannels[0] : null;
                for (int i = 0; i < verts.Count; i++)
                {
                    Vector3D pos = verts[i];
                    Vector3D norm = (norms != null) ? norms[i] : new Vector3D(0, 1, 0); // Y-up by default
                    Vector3D uv = (uvs != null) ? uvs[i] : new Vector3D(0, 0, 0);
                    Primitive.PrimitiveVertex vertex = new Primitive.PrimitiveVertex();
                    vertex.v = new Vector3f(pos.X * scaleFactor, pos.Y * scaleFactor, pos.Z * scaleFactor);
                    vertex.n = new Vector3f(norm.X, norm.Y, norm.Z);
                    vertex.t = new Vector2f(uv.X, 1 - uv.Y);
                    primitive.vertices.Add(vertex);
                }

                foreach (Face face in aiMesh.Faces)
                    primitive.indices.AddRange(face.Indices);

                primitive.GenerateGPUData();

                mesh.Primitives.Add(primitive);
            }

            Meshes.AddRange(meshes);
        }

        public static Shader LoadShader(string shaderName, string shaderPath)
        {
            if (Shaders.ContainsKey(shaderName))
                throw new ArgumentException("Shader with name \"" + shaderName + "\" is already loaded.");
            Shader shader = Shader.Create(shaderPath);
            Shaders[shaderName] = shader;
            return shader;
        }

        public static Shader LoadShader(string shaderName, string shaderPath, ShaderType shaderType)
        {
            if (Shaders.ContainsKey(shaderName))
                throw new ArgumentException("Shader with name \"" + shaderName + "\" is already loaded.");
            Shader shader = Shader.Create(shaderPath, shaderType);
            Shaders[shaderName] = shader;
            return shader;
        }

        public static ShaderPipeline LoadShaderPipeline(string shaderPipelineName, params Shader[] shaders)
        {
            if (ShaderPipelines.ContainsKey(shaderPipelineName))
                throw new ArgumentException("Shader pipeline with name \"" + shaderPipelineName + "\" is already loaded.");
            ShaderPipeline shaderPipeline = new ShaderPipeline(shaders);
            ShaderPipelines[shaderPipelineName] = shaderPipeline;
            return shaderPipeline;
        }

        public static Texture LoadTexture(string path, string textureName = "", bool applyGammaCorrection = false)
        {
            if (textureName == "")
                textureName = Path.GetFileNameWithoutExtension(path);
            if (Textures.ContainsKey(textureName))
                return Textures[textureName];

            Texture texture = new Texture(new Bitmap(path), applyGammaCorrection);

            Textures[textureName] = texture;

            return texture;
        }

        public static Sound LoadSound(string path, string soundName = "")
        {
            if (soundName == "")
                soundName = Path.GetFileNameWithoutExtension(path);
            if (Sounds.ContainsKey(soundName))
                return Sounds[soundName];
            //throw new ArgumentException("Sound with name \"" + soundName + "\" is already loaded.");

            SoundStream stream = new SoundStream(File.OpenRead(path));
            AudioBuffer buffer = new AudioBuffer
            {
                Stream = stream.ToDataStream(),
                AudioBytes = (int)stream.Length,
                Flags = BufferFlags.EndOfStream
            };
            stream.Close();

            Sound sound = new Sound(buffer, stream.Format, stream.DecodedPacketsInfo);
            Sounds[soundName] = sound;
            return sound;
        }

        private struct Reference
        {
            public object obj;
            public string fieldName;
            public string referenceObjName;

            public Reference(object obj, string fieldName, string referenceObjName)
            {
                this.obj = obj;
                this.fieldName = fieldName;
                this.referenceObjName = referenceObjName;
            }
        }

        public static Scene LoadScene(string path)
        {
            XDocument document = XDocument.Parse(File.ReadAllText(path));

            Dictionary<string, object> namedObjects = new Dictionary<string, object>();
            List<Reference> references = new List<Reference>();
            List<Type> types = new List<Type>();

            IEnumerable<string> blacklistedAssemblies = new List<string>() { "PresentationCore" };
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies().Where(assembly => !blacklistedAssemblies.Contains(assembly.GetName().Name)))
                types.AddRange(assembly.GetTypes());

            void parseSpecialAttribute(object obj, string name, string value)
            {
                string[] words = value.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length < 2)
                    throw new Exception("Wrong attribute format.");
                Type objType = obj.GetType();
                switch (words[0])
                {
                    case "Reference":
                        references.Add(new Reference(obj, name, words[1]));
                        break;
                    case "Mesh":
                        {
                            if (!Meshes.ContainsKey(words[1]))
                                throw new Exception("Mesh " + words[1] + " is not loaded.");
                            PropertyInfo property = objType.GetProperty(name);
                            if (property != null)
                                property.SetValue(obj, Meshes[words[1]]);
                            else
                            {
                                FieldInfo field = objType.GetField(name);
                                if (field != null)
                                    field.SetValue(obj, Meshes[words[1]]);
                                else
                                    throw new Exception(objType.Name + " doesn't have " + name + ".");
                            }
                            break;
                        }
                    case "Texture":
                        {
                            //if (!Textures.ContainsKey(words[1]))
                            //    throw new Exception("Texture " + words[1] + " not loaded.");
                            PropertyInfo property = objType.GetProperty(name);
                            if (property != null)
                                property.SetValue(obj, Textures[words[1]]);
                            else
                            {
                                FieldInfo field = objType.GetField(name);
                                if (field != null)
                                    field.SetValue(obj, Textures[words[1]]);
                                else
                                    throw new Exception(objType.Name + " don't have " + name + ".");
                            }
                            break;
                        }
                    case "Sound":
                        {
                            if (!Sounds.ContainsKey(words[1]))
                                throw new Exception("Sound " + words[1] + " not loaded.");
                            PropertyInfo property = objType.GetProperty(name);
                            if (property != null)
                                property.SetValue(obj, Sounds[words[1]]);
                            else
                            {
                                FieldInfo field = objType.GetField(name);
                                if (field != null)
                                    field.SetValue(obj, Sounds[words[1]]);
                                else
                                    throw new Exception(objType.Name + " don't have " + name + ".");
                            }
                            break;
                        }
                }
            }
            void parseAttributes(ref object obj, IEnumerable<XAttribute> attributes)
            {
                Type objType = obj.GetType();
                foreach (XAttribute attrib in attributes)
                {
                    if (attrib.Name.LocalName == "x.Name")
                    {
                        if (namedObjects.ContainsKey(attrib.Value))
                            throw new Exception("Scene can't have two or more objects with same name.");
                        namedObjects[attrib.Value] = obj;
                        continue;
                    }
                    if (attrib.Value.StartsWith("{") && attrib.Value.EndsWith("}"))
                        parseSpecialAttribute(obj, attrib.Name.LocalName, attrib.Value.Substring(1, attrib.Value.Length - 2));
                    else
                    {
                        if (obj is Quaternion)
                        {
                            switch (attrib.Name.LocalName.ToLower())
                            {
                                case "x":
                                    obj = Quaternion.FromAxisAngle(Vector3.UnitX, double.Parse(attrib.Value) / 180.0 * Math.PI) * (Quaternion)obj;
                                    continue;
                                case "y":
                                    obj = Quaternion.FromAxisAngle(Vector3.UnitY, double.Parse(attrib.Value) / 180.0 * Math.PI) * (Quaternion)obj;
                                    continue;
                                case "z":
                                    obj = Quaternion.FromAxisAngle(Vector3.UnitZ, double.Parse(attrib.Value) / 180.0 * Math.PI) * (Quaternion)obj;
                                    continue;
                                default:
                                    throw new Exception("Quaternion does not have \"" + attrib.Name.LocalName + "\"");
                            }
                        }
                        PropertyInfo property = objType.GetProperty(attrib.Name.LocalName);
                        if (property != null)
                        {
                            if (property.PropertyType.IsSubclassOf(typeof(Enum)))
                            {
                                int value = 0;
                                foreach (string subValue in attrib.Value.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries))
                                    value |= (int)Enum.Parse(property.PropertyType, subValue);
                                property.SetValue(obj, value);
                            }
                            else
                                property.SetValue(obj, Convert.ChangeType(attrib.Value, property.PropertyType));
                        }
                        else
                        {
                            FieldInfo field = objType.GetField(attrib.Name.LocalName);
                            if (field != null)
                            {
                                if (field.FieldType.IsSubclassOf(typeof(Enum)))
                                {
                                    int value = 0;
                                    foreach (string subValue in attrib.Value.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries))
                                        value |= (int)Enum.Parse(field.FieldType, subValue);
                                    field.SetValue(obj, value);
                                }
                                else
                                    field.SetValue(obj, Convert.ChangeType(attrib.Value, field.FieldType));
                            }
                            else
                                throw new Exception(objType.Name + " don't have " + attrib.Name.LocalName + ".");
                        }
                    }
                }
            }

            List<GameObject> gameObjects = new List<GameObject>();
            object parseElement(object parent, XElement parentElement, XElement element)
            {
                if (element.NodeType == XmlNodeType.Text)
                    return Convert.ChangeType(element.Value.Trim(' ', '\n'), parent.GetType());
                if (element.Name.LocalName == "Assets")
                {
                    foreach (XElement assetsSet in element.Elements())
                    {
                        switch (assetsSet.Name.LocalName)
                        {
                            case "Meshes":
                                {
                                    foreach (XElement mesh in assetsSet.Elements())
                                    {
                                        MethodInfo method = typeof(AssetsManager_Old).GetMethod("LoadModel");
                                        ParameterInfo[] parameters = method.GetParameters();
                                        Dictionary<string, object> parameterValues = new Dictionary<string, object>();
                                        foreach (XAttribute attrib in mesh.Attributes())
                                        {
                                            bool found = false;
                                            foreach (ParameterInfo param in parameters)
                                            {
                                                if (param.Name == attrib.Name.LocalName)
                                                {
                                                    if (parameterValues.ContainsKey(param.Name))
                                                        throw new Exception("Attribute \"" + param.Name + "\" is set multiple times.");
                                                    parameterValues[param.Name] = Convert.ChangeType(attrib.Value, param.ParameterType);
                                                    if (param.Name == "path")
                                                        parameterValues[param.Name] = "Assets\\Models\\" + parameterValues[param.Name];
                                                    found = true;
                                                    break;
                                                }
                                            }
                                            if (!found)
                                                throw new Exception("Attribute \"" + attrib.Name.LocalName + "\" not found.");
                                        }
                                        method.Invoke(null, parameters.Select(p => parameterValues.ContainsKey(p.Name) ? parameterValues[p.Name] :
                                                                                  p.IsOptional ? p.DefaultValue : throw new Exception("Missing required attribute: \"" + p.Name + "\"")).ToArray());
                                    }
                                    continue;
                                }
                            case "Textures":
                                {
                                    foreach (XElement texture in assetsSet.Elements())
                                    {
                                        MethodInfo method = typeof(AssetsManager_Old).GetMethod("LoadTexture");
                                        ParameterInfo[] parameters = method.GetParameters();
                                        Dictionary<string, object> parameterValues = new Dictionary<string, object>();
                                        foreach (XAttribute attrib in texture.Attributes())
                                        {
                                            bool found = false;
                                            foreach (ParameterInfo param in parameters)
                                            {
                                                if (param.Name == attrib.Name.LocalName)
                                                {
                                                    if (parameterValues.ContainsKey(param.Name))
                                                        throw new Exception("Attribute \"" + param.Name + "\" is set multiple times.");
                                                    parameterValues[param.Name] = Convert.ChangeType(attrib.Value, param.ParameterType);
                                                    if (param.Name == "path")
                                                        parameterValues[param.Name] = "Assets\\Textures\\" + parameterValues[param.Name];
                                                    found = true;
                                                    break;
                                                }
                                            }
                                            if (!found)
                                                throw new Exception("Attribute \"" + attrib.Name.LocalName + "\" not found.");
                                        }
                                        method.Invoke(null, parameters.Select(p => parameterValues.ContainsKey(p.Name) ? parameterValues[p.Name] :
                                                                                  p.IsOptional ? p.DefaultValue : throw new Exception("Missing required attribute: \"" + p.Name + "\"")).ToArray());
                                    }
                                    continue;
                                }
                            case "Sounds":
                                {
                                    foreach (XElement sound in assetsSet.Elements())
                                    {
                                        MethodInfo method = typeof(AssetsManager_Old).GetMethod("LoadSound");
                                        ParameterInfo[] parameters = method.GetParameters();
                                        Dictionary<string, object> parameterValues = new Dictionary<string, object>();
                                        foreach (XAttribute attrib in sound.Attributes())
                                        {
                                            bool found = false;
                                            foreach (ParameterInfo param in parameters)
                                            {
                                                if (param.Name == attrib.Name.LocalName)
                                                {
                                                    if (parameterValues.ContainsKey(param.Name))
                                                        throw new Exception("Attribute \"" + param.Name + "\" is set multiple times.");
                                                    parameterValues[param.Name] = Convert.ChangeType(attrib.Value, param.ParameterType);
                                                    if (param.Name == "path")
                                                        parameterValues[param.Name] = "Assets\\Sounds\\" + parameterValues[param.Name];
                                                    found = true;
                                                    break;
                                                }
                                            }
                                            if (!found)
                                                throw new Exception("Attribute \"" + attrib.Name.LocalName + "\" not found.");
                                        }
                                        method.Invoke(null, parameters.Select(p => parameterValues.ContainsKey(p.Name) ? parameterValues[p.Name] :
                                                                                  p.IsOptional ? p.DefaultValue : throw new Exception("Missing required attribute: \"" + p.Name + "\"")).ToArray());
                                    }
                                    continue;
                                }
                            default:
                                throw new Exception("Unknown assets type: " + assetsSet.Name.LocalName);
                        }
                    }
                    return null;
                }
                object curObj = null;
                Type curType = types.Find(t => t.Name == element.Name.LocalName);
                if (curType != null)
                {
                    if (curType == typeof(GameObject))
                    {
                        curObj = Activator.CreateInstance(typeof(GameObject));
                        if (parent != null && parent.GetType() == typeof(GameObject))
                            (curObj as GameObject).Transform.SetParent((parent as GameObject).Transform, false);
                        parseAttributes(ref curObj, element.Attributes());
                        gameObjects.Add(curObj as GameObject);
                        foreach (XElement elem in element.Elements())
                            parseElement(curObj, element, elem);
                    }
                    else if (curType.IsSubclassOf(typeof(Component)))
                    {
                        if (!(parent is GameObject))
                            throw new Exception("Components can only be inside of GameObject.");
                        if (curType == typeof(Transform))
                            curObj = (parent as GameObject).Transform;
                        else
                            curObj = (parent as GameObject).AddComponent(curType);
                        parseAttributes(ref curObj, element.Attributes());
                        foreach (XElement elem in element.Elements())
                            parseElement(curObj, element, elem);
                    }
                    else if (curType == typeof(Scene))
                    {
                        if (parent != null)
                            throw new Exception("Scene must be the root.");
                        curObj = Activator.CreateInstance(typeof(Scene));
                        parseAttributes(ref curObj, element.Attributes());
                        foreach (XElement elem in element.Elements())
                        {
                            object sceneObject = parseElement(curObj, element, elem);
                            if (sceneObject != null && !(sceneObject is GameObject))
                                throw new Exception("Scene can contain only GameObjects.");
                        }
                        (curObj as Scene).objects.AddRange(gameObjects);
                    }
                    else
                    {
                        if (curType == typeof(Quaternion))
                            curObj = Quaternion.Identity;
                        else
                        {
                            if (curType.GetConstructor(Type.EmptyTypes) != null)
                                curObj = Activator.CreateInstance(curType);
                            else
                                curObj = FormatterServices.GetUninitializedObject(curType);
                        }
                        parseAttributes(ref curObj, element.Attributes());
                        foreach (XElement elem in element.Elements())
                            parseElement(curObj, element, elem);
                    }
                }
                else
                {
                    string[] nameParts = element.Name.LocalName.Split('.');
                    if (nameParts.Length != 2 || nameParts[0] != parentElement.Name.LocalName)
                        throw new Exception(element.Name.LocalName + " not found.");

                    IEnumerable<XAttribute> attributes = element.Attributes();
                    IEnumerable<XElement> elements = element.Elements();
                    FieldInfo field = parent.GetType().GetField(nameParts[1]);
                    if (field == null)
                    {
                        PropertyInfo property = parent.GetType().GetProperty(nameParts[1]);
                        if (property == null)
                            throw new Exception(parent.GetType().Name + " don't have " + nameParts[1] + ".");

                        if (attributes.Count() != 0)
                        {
                            if (elements.Count() != 0)
                                throw new Exception("Setter can't have values in both places");
                            if (property.PropertyType == typeof(Quaternion))
                                curObj = Quaternion.Identity;
                            else
                            {
                                if (property.PropertyType.GetConstructor(Type.EmptyTypes) != null)
                                    curObj = Activator.CreateInstance(property.PropertyType);
                                else
                                    curObj = FormatterServices.GetUninitializedObject(property.PropertyType);
                            }
                            parseAttributes(ref curObj, element.Attributes());
                            property.SetValue(parent, curObj);
                        }
                        else
                        {
                            curType = property.PropertyType;
                            if (curType.IsArray || curType.IsGenericType && curType.GetGenericTypeDefinition() == typeof(List<>))
                            {
                                Type elementType;
                                if (curType.IsArray)
                                    elementType = curType.GetElementType();
                                else
                                    elementType = curType.GetGenericArguments()[0];
                                Type genericListType = typeof(List<>).MakeGenericType(elementType);
                                curObj = Activator.CreateInstance(genericListType);
                                MethodInfo addMethod = genericListType.GetMethod("Add");
                                foreach (XElement elem in elements)
                                {
                                    object listElement = parseElement(curObj, element, elem);
                                    if (listElement.GetType() != elementType && !listElement.GetType().IsSubclassOf(elementType))
                                        throw new Exception(listElement.GetType().Name + " does not match for " + elementType.Name + ".");
                                    addMethod.Invoke(curObj, new object[] { listElement });
                                }
                                if (curType.IsArray)
                                {
                                    if (property.SetMethod.IsPrivate)
                                    {
                                        int originalLength = ((Array)property.GetValue(parent)).Length;
                                        int curLength = Math.Min(((System.Collections.IList)curObj).Count, originalLength);
                                        object boxedArray = property.GetValue(parent);
                                        for (int i = 0; i < originalLength; i++)
                                            ((Array)boxedArray).SetValue(i < curLength ? ((System.Collections.IList)curObj)[i] : null, i);
                                    }
                                    else
                                        property.SetValue(parent, genericListType.GetMethod("ToArray").Invoke(curObj, null));
                                }
                                else
                                    property.SetValue(parent, curObj);
                            }
                            else
                            {
                                if (elements.Count() > 1)
                                    throw new Exception("Only array and list types can contain more than one element.");
                                if (elements.Count() == 1)
                                {
                                    object nestedObject = parseElement(parent, parentElement, elements.First());
                                    if (nestedObject.GetType() != curType && !nestedObject.GetType().IsSubclassOf(curType))
                                        throw new Exception(nestedObject.GetType().Name + " does not match for " + curType.Name + ".");
                                    property.SetValue(parent, nestedObject);
                                }
                                else
                                {
                                    if (property.PropertyType == typeof(Quaternion))
                                        curObj = Quaternion.Identity;
                                    else
                                        curObj = Activator.CreateInstance(property.PropertyType);
                                    property.SetValue(parent, curObj);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (attributes.Count() != 0)
                        {
                            if (elements.Count() != 0)
                                throw new Exception("Setter can't have values in both places");
                            if (field.FieldType == typeof(Quaternion))
                                curObj = Quaternion.Identity;
                            else
                                curObj = Activator.CreateInstance(field.FieldType);
                            parseAttributes(ref curObj, element.Attributes());
                            field.SetValue(parent, curObj);
                        }
                        else
                        {
                            curType = field.FieldType;
                            if (curType.IsArray || curType.IsGenericType && curType.GetGenericTypeDefinition() == typeof(List<>))
                            {
                                Type elementType;
                                if (curType.IsArray)
                                    elementType = curType.GetElementType();
                                else
                                    elementType = curType.GetGenericArguments()[0];
                                Type genericListType = typeof(List<>).MakeGenericType(elementType);
                                curObj = Activator.CreateInstance(genericListType);
                                MethodInfo addMethod = genericListType.GetMethod("Add");
                                foreach (XElement elem in elements)
                                {
                                    object listElement = parseElement(curObj, element, elem);
                                    if (listElement.GetType() != elementType && !listElement.GetType().IsSubclassOf(elementType))
                                        throw new Exception(listElement.GetType().Name + " does not match for " + elementType.Name + ".");
                                    addMethod.Invoke(curObj, new object[] { listElement });
                                }
                                if (curType.IsArray)
                                    field.SetValue(parent, genericListType.GetMethod("ToArray").Invoke(curObj, null));
                                else
                                    field.SetValue(parent, curObj);
                            }
                            else
                            {
                                if (elements.Count() > 1)
                                    throw new Exception("Only array and list types can contain more than one element.");
                                if (elements.Count() == 1)
                                {
                                    object nestedObject = parseElement(parent, parentElement, elements.First());
                                    if (nestedObject.GetType() != curType && !nestedObject.GetType().IsSubclassOf(curType))
                                        throw new Exception(nestedObject.GetType().Name + " does not match for " + curType.Name + ".");
                                    field.SetValue(parent, nestedObject);
                                }
                                else
                                {
                                    if (field.FieldType == typeof(Quaternion))
                                        curObj = Quaternion.Identity;
                                    else
                                        curObj = Activator.CreateInstance(field.FieldType);
                                    field.SetValue(parent, curObj);
                                }
                            }
                        }
                    }
                }
                return curObj;
            }

            object scene = parseElement(null, null, document.Root);
            if (!(scene is Scene))
                throw new Exception("Scene must be as root.");

            foreach (Reference reference in references)
            {
                if (!namedObjects.ContainsKey(reference.referenceObjName))
                    throw new Exception(reference.referenceObjName + " not found.");
                FieldInfo field = reference.obj.GetType().GetField(reference.fieldName);
                if (field != null)
                    field.SetValue(reference.obj, namedObjects[reference.referenceObjName]);
                else
                {
                    PropertyInfo property = reference.obj.GetType().GetProperty(reference.fieldName);
                    if (property != null)
                        property.SetValue(reference.obj, namedObjects[reference.referenceObjName]);
                    else
                        throw new Exception(reference.obj.GetType().Name + " don't have " + reference.fieldName + ".");
                }
            }

            Scenes[Path.GetFileNameWithoutExtension(path)] = scene as Scene;
            return scene as Scene;
        }
    }
    
    public static partial class FileSystemHelper
    {
        private static readonly EnumerationOptions defaultEnumerator = new EnumerationOptions() { IgnoreInaccessible = true };
        private static readonly EnumerationOptions recursiveEnumerator = new EnumerationOptions() { IgnoreInaccessible = true, RecurseSubdirectories = true };

        public static string SanitizeFileName(string name)
        {
            int fileNameIndex = name.LastIndexOf(Path.DirectorySeparatorChar) + 1;
            StringBuilder path = new StringBuilder(name[..fileNameIndex]);
            StringBuilder fileName = new StringBuilder(name[fileNameIndex..]);

            foreach (char c in Path.GetInvalidPathChars())
            {
                path.Replace(c, '_');
            }
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                fileName.Replace(c, '_');
            }

            return path.Append(fileName).ToString();
        }

        public static string GenerateUniquePath(string path)
        {
            if (!Path.Exists(path))
                return path;

            path = Path.TrimEndingDirectorySeparator(path);

            FileInfo fileInfo = new FileInfo(path);
            string parentFolderName = fileInfo.DirectoryName ?? string.Empty;
            string fileName = fileInfo.Name;
            string extension = fileInfo.Extension;

            Match regexMatch = FileNameIndexRegex().Match(fileName);
            int fileNameIndex = regexMatch.Success ? int.Parse(regexMatch.Value) : 0;
            fileName = fileName[..^regexMatch.Length];

            do
            {
                fileNameIndex++;
                path = Path.Combine(parentFolderName, $"{fileName} {fileNameIndex}{extension}");
            } while (Path.Exists(path));

            return path;
        }

        public static IEnumerable<PathInfo> EnumeratePathInfoEntries(string path, string expression, bool recursive)
        {
            return new FileSystemEnumerable<PathInfo>(path, PathInfo.FromSystemEntry, recursive ? recursiveEnumerator : defaultEnumerator)
            {
                ShouldIncludePredicate = (ref FileSystemEntry entry) => FileSystemName.MatchesSimpleExpression(expression.AsSpan(), entry.FileName)
            };
        }

        public struct PathInfo
        {
            public bool IsDirectory;
            public string FullPath;

            public static PathInfo FromSystemEntry(ref FileSystemEntry entry)
            {
                return new PathInfo
                {
                    IsDirectory = entry.IsDirectory,
                    FullPath = entry.ToFullPath()
                };
            }
        }

        [GeneratedRegex("\\s\\d+$", RegexOptions.RightToLeft)]
        private static partial Regex FileNameIndexRegex();
    }
}