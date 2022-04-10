﻿using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Linq;

using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.D3DCompiler;
using SharpDX.XAudio2;
using SharpDX.Multimedia;
using SharpDX.Mathematics.Interop;

using Buffer = SharpDX.Direct3D11.Buffer;

using Engine.BaseAssets.Components;
using LinearAlgebra;
using Vector3 = LinearAlgebra.Vector3;
using Vector2 = LinearAlgebra.Vector2;
using Matrix3x3 = LinearAlgebra.Matrix3x3;
using Quaternion = LinearAlgebra.Quaternion;

namespace Engine
{
    public class Model
    {
        public List<Vector3> v = null;
        public List<Vector2> t = null;
        public List<Vector3> n = null;
        public List<int[]> v_i = null;
        public List<int[]> t_i = null;
        public List<int[]> n_i = null;

        private Buffer vertexBuffer;
        private VertexBufferBinding vertexBufferBinding;
        private Buffer indexBuffer;

        private struct ModelVertex
        {
            public Vector3f v;
            public Vector2f t;
            public Vector3f n;
            public Vector3f tx;
        }

        public void updateModel()
        {
            if (v == null || v_i == null)
                throw new Exception("Model can't be empty.");

            List<ModelVertex> vertexes = new List<ModelVertex>();
            ModelVertex[] curPolygon = new ModelVertex[3];
            int polygonsCount = v_i.Count;
            uint[] indexes = new uint[3 * polygonsCount];
            for (int i = 0; i < polygonsCount; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    ModelVertex curVertex = new ModelVertex();
                    curVertex.v.x = (float)v[v_i[i][j]].x;
                    curVertex.v.y = (float)v[v_i[i][j]].y;
                    curVertex.v.z = (float)v[v_i[i][j]].z;
                    if (t != null)
                    {
                        curVertex.t.x = (float)t[t_i[i][j]].x;
                        curVertex.t.y = (float)t[t_i[i][j]].y;
                    }
                    else
                    {
                        curVertex.t.x = 0;
                        curVertex.t.y = 0;
                    }
                    if (n != null)
                    {
                        curVertex.n.x = (float)n[n_i[i][j]].x;
                        curVertex.n.y = (float)n[n_i[i][j]].y;
                        curVertex.n.z = (float)n[n_i[i][j]].z;
                    }
                    else
                    {
                        Vector3 p1 = v[v_i[i][0]];
                        Vector3 p2 = v[v_i[i][1]];
                        Vector3 p3 = v[v_i[i][2]];
                        Vector3 normal = (p2 - p1).vecMul(p3 - p1).normalized();
                        curVertex.n.x = (float)normal.x;
                        curVertex.n.y = (float)normal.y;
                        curVertex.n.z = (float)normal.z;
                    }

                    //curPolygon.Add(curVertex);
                    curPolygon[j] = curVertex;
                }

                Vector3 edge1 = curPolygon[1].v - curPolygon[0].v;
                Vector3 edge2 = curPolygon[2].v - curPolygon[0].v;
                Vector2 UVedge1 = curPolygon[1].t - curPolygon[0].t;
                Vector2 UVedge2 = curPolygon[2].t - curPolygon[0].t;
                Vector3 norm = edge1.cross(edge2).normalized();

                Matrix3x3 invB = new Matrix3x3(new Vector3(UVedge1), new Vector3(UVedge2), Vector3.UnitZ, false).inverse();
                Matrix3x3 A = new Matrix3x3(edge1, edge2, norm, false);
                Matrix3x3 invP = A * invB;
                Vector3f tangent = new Vector3f((float)invP.v00, (float)invP.v10, (float)invP.v20);//(Vector3f)(invP * Vector3.UnitX);
                curPolygon[0].tx = tangent;
                curPolygon[1].tx = tangent;
                curPolygon[2].tx = tangent;

                for (int j = 0; j < 3; j++)
                {
                    ModelVertex curVertex = curPolygon[j];
                    uint k;
                    for (k = 0; k < vertexes.Count; k++)
                    {
                        if (vertexes[(int)k].Equals(curVertex))
                        {
                            indexes[i * 3 + j] = k;
                            break;
                        }
                    }
                    if (k == vertexes.Count)
                    {
                        indexes[i * 3 + j] = k;
                        vertexes.Add(curVertex);
                    }
                }
            }

            vertexBuffer = Buffer.Create(GraphicsCore.CurrentDevice, BindFlags.VertexBuffer, vertexes.ToArray());
            vertexBufferBinding = new VertexBufferBinding(vertexBuffer, Utilities.SizeOf<ModelVertex>(), 0);
            indexBuffer = Buffer.Create(GraphicsCore.CurrentDevice, BindFlags.IndexBuffer, indexes);
        }

        public void Render()
        {
            GraphicsCore.CurrentDevice.ImmediateContext.InputAssembler.SetVertexBuffers(0, vertexBufferBinding);
            GraphicsCore.CurrentDevice.ImmediateContext.InputAssembler.SetIndexBuffer(indexBuffer, Format.R32_UInt, 0);
            GraphicsCore.CurrentDevice.ImmediateContext.DrawIndexed(v_i.Count * 3, 0, 0);
        }
    }
    public class Sampler
    {
        private SamplerState sampler;
        public Sampler(TextureAddressMode addressU, TextureAddressMode addressV, Filter filter = Filter.Anisotropic, int maximumAnisotropy = 8, RawColor4 borderColor = new RawColor4(), TextureAddressMode addressW = TextureAddressMode.Clamp)
        {
            sampler = new SamplerState(GraphicsCore.CurrentDevice, new SamplerStateDescription()
            {
                AddressU = addressU,
                AddressV = addressV,
                AddressW = addressW,
                Filter = filter,
                MaximumAnisotropy = maximumAnisotropy,
                MipLodBias = 0,
                MinimumLod = float.MinValue,
                MaximumLod = float.MaxValue,
                BorderColor = borderColor
            });
        }
        public void use(string variable)
        {
            foreach (Shader shader in ShaderPipeline.Current.Shaders)
                if (shader.Locations.ContainsKey(variable))
                    GraphicsCore.CurrentDevice.ImmediateContext.PixelShader.SetSampler(shader.Locations[variable], sampler);
        }
    }
    public class Texture
    {
        public Texture2D texture { get; private set; }
        public BindFlags Usage
        {
            get => texture.Description.BindFlags;
        }
        public ShaderResourceView ShaderResource { get; private set; }
        public DepthStencilView DepthStencil { get; private set; }
        public RenderTargetView RenderTarget { get; private set; }
        public Texture(Bitmap image, bool applyGammaCorrection = true, BindFlags usage = BindFlags.ShaderResource)
        {
            BindFlags unsupportedUsage = usage & ~(BindFlags.RenderTarget | BindFlags.ShaderResource);
            if (unsupportedUsage != BindFlags.None)
                throw new NotImplementedException("Texture with image base does not support \"" + unsupportedUsage.ToString() + "\" usage.");

            if (image.PixelFormat != PixelFormat.Format32bppArgb)
                image = image.Clone(new System.Drawing.Rectangle(0, 0, image.Width, image.Height), PixelFormat.Format32bppArgb);
            BitmapData data = image.LockBits(new System.Drawing.Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            texture = new Texture2D(GraphicsCore.CurrentDevice, new Texture2DDescription()
            {
                Width = image.Width,
                Height = image.Height,
                ArraySize = 1,
                BindFlags = usage,
                Usage = ((usage & ~BindFlags.ShaderResource) == BindFlags.None) ? ResourceUsage.Immutable : ResourceUsage.Default,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = applyGammaCorrection ? Format.B8G8R8A8_UNorm_SRgb : Format.B8G8R8A8_UNorm,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.Shared,
                SampleDescription = new SampleDescription(1, 0)
            }, new DataRectangle(data.Scan0, data.Stride));

            image.UnlockBits(data);

            generateViews();
        }
        public Texture(Texture2D texture)
        {
            this.texture = texture;

            generateViews();
        }
        public Texture(int width, int height, Vector4f color, bool applyGammaCorrection = true, BindFlags usage = BindFlags.ShaderResource)
        {
            if (width <= 0)
                throw new ArgumentOutOfRangeException("width", "Texture width must be positive.");
            if (height <= 0)
                throw new ArgumentOutOfRangeException("height", "Texture height must be positive.");

            BindFlags unsupportedUsage = usage & ~(BindFlags.RenderTarget | BindFlags.ShaderResource);
            if (unsupportedUsage != BindFlags.None)
                throw new NotImplementedException("Texture with color base does not support \"" + unsupportedUsage.ToString() + "\" usage.");

            byte[] data = new byte[width * height * 4];
            for (int i = 0; i < height; i++)
                for (int j = 0; j < width; j++)
                {
                    int pos = (i * width  + j) * 4;
                    data[pos] = (byte)(color.x * 255);
                    data[pos + 1] = (byte)(color.y * 255);
                    data[pos + 2] = (byte)(color.z * 255);
                    data[pos + 3] = (byte)(color.w * 255);
                }

            IntPtr dataPtr = Marshal.AllocHGlobal(width * height * 4);
            Marshal.Copy(data, 0, dataPtr, width * height * 4);

            texture = new Texture2D(GraphicsCore.CurrentDevice, new Texture2DDescription()
            {
                Width = width,
                Height = height,
                ArraySize = 1,
                BindFlags = usage,
                Usage = ((usage & ~BindFlags.ShaderResource) == BindFlags.None) ? ResourceUsage.Immutable : ResourceUsage.Default,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = applyGammaCorrection ? Format.B8G8R8A8_UNorm_SRgb : Format.B8G8R8A8_UNorm,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.Shared,
                SampleDescription = new SampleDescription(1, 0)
            }, new DataRectangle(dataPtr, width * 4));

            Marshal.FreeHGlobal(dataPtr);

            generateViews();
        }
        public Texture(int width, int height, float value, BindFlags usage = BindFlags.DepthStencil)
        {
            if (width <= 0)
                throw new ArgumentOutOfRangeException("width", "Texture width must be positive.");
            if (height <= 0)
                throw new ArgumentOutOfRangeException("height", "Texture height must be positive.");

            BindFlags unsupportedUsage = usage & ~(BindFlags.DepthStencil | BindFlags.ShaderResource);
            if (unsupportedUsage != BindFlags.None)
                throw new NotImplementedException("Texture with scalar base does not support \"" + unsupportedUsage.ToString() + "\" usage.");

            float[] data = new float[width * height];
            for (int i = 0; i < height; i++)
                for (int j = 0; j < width; j++)
                    data[i * width + j] = value;

            IntPtr dataPtr = Marshal.AllocHGlobal(width * height * 4);
            Marshal.Copy(data, 0, dataPtr, width * height);

            texture = new Texture2D(GraphicsCore.CurrentDevice, new Texture2DDescription()
            {
                Width = width,
                Height = height,
                ArraySize = 1,
                BindFlags = usage,
                Usage = ((usage & ~BindFlags.ShaderResource) == BindFlags.None) ? ResourceUsage.Immutable : ResourceUsage.Default,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = Format.R32_Typeless,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.Shared,
                SampleDescription = new SampleDescription(1, 0)
            }, new DataRectangle(dataPtr, width * 4));

            Marshal.FreeHGlobal(dataPtr);

            generateViews();
        }
        private void generateViews()
        {
            Format format = texture.Description.Format;
            if (format == Format.B8G8R8A8_UNorm || format == Format.B8G8R8A8_UNorm_SRgb || 
                format == Format.R8G8B8A8_UNorm || format == Format.R8G8B8A8_UNorm_SRgb)
            {
                if (Usage.HasFlag(BindFlags.RenderTarget))
                    RenderTarget = new RenderTargetView(GraphicsCore.CurrentDevice, texture,
                    new RenderTargetViewDescription()
                    {
                        Format = format,
                        Dimension = RenderTargetViewDimension.Texture2D,
                        Texture2D = new RenderTargetViewDescription.Texture2DResource()
                        {
                            MipSlice = 0
                        }
                    });
                if (Usage.HasFlag(BindFlags.ShaderResource))
                    ShaderResource = new ShaderResourceView(GraphicsCore.CurrentDevice, texture,
                        new ShaderResourceViewDescription()
                        {
                            Format = format,
                            Dimension = ShaderResourceViewDimension.Texture2D,
                            Texture2D = new ShaderResourceViewDescription.Texture2DResource()
                            {
                                MipLevels = texture.Description.MipLevels,
                                MostDetailedMip = texture.Description.MipLevels - 1
                            }
                        });
            }
            else if (texture.Description.Format.HasFlag(Format.R32_Typeless))
            {
                DepthStencil = new DepthStencilView(GraphicsCore.CurrentDevice, texture,
                    new DepthStencilViewDescription()
                    {
                        Format = Format.D32_Float,
                        Dimension = DepthStencilViewDimension.Texture2D,
                        Flags = DepthStencilViewFlags.None,
                        Texture2D = new DepthStencilViewDescription.Texture2DResource()
                        {
                            MipSlice = 0
                        }
                    });
                if (Usage.HasFlag(BindFlags.ShaderResource))
                    ShaderResource = new ShaderResourceView(GraphicsCore.CurrentDevice, texture,
                        new ShaderResourceViewDescription()
                        {
                            Format = Format.R32_Float,
                            Dimension = ShaderResourceViewDimension.Texture2D,
                            Texture2D = new ShaderResourceViewDescription.Texture2DResource()
                            {
                                MipLevels = texture.Description.MipLevels,
                                MostDetailedMip = texture.Description.MipLevels - 1
                            }
                        });
            }
            else
                throw new NotImplementedException("Texture format \"" + texture.Description.Format.ToString() + "\" is not supported.");
        }
        public void use(string variable)
        {
            if ((texture.Description.BindFlags & BindFlags.ShaderResource) == BindFlags.None)
                throw new Exception("This texture is not a shader resource");
            foreach (Shader shader in ShaderPipeline.Current.Shaders)
                if (shader.Locations.ContainsKey(variable))
                    GraphicsCore.CurrentDevice.ImmediateContext.PixelShader.SetShaderResource(shader.Locations[variable], ShaderResource);
        }
    }
    public class Sound : IDisposable
    {
        public AudioBuffer Buffer { get; private set; }
        public WaveFormat Format { get; private set; }
        public uint[] DecodedPacketsInfo { get; private set; }
        
        private bool disposed = false;

        public Sound(AudioBuffer buffer, WaveFormat format, uint[] decodedPacketsInfo)
        {
            Buffer = buffer;
            Format = format;
            DecodedPacketsInfo = decodedPacketsInfo;
        }
        ~Sound()
        {
            Dispose(disposing: false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                Buffer.Stream.Dispose();
                disposed = true;
            }
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
        FragmentShader
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
                if (buffer.variables.ContainsKey(name))
                    return true;
            return false;
        }
        public void updateUniform(string name, object value)
        {
            ShaderVariable variable;
            foreach (ShaderBuffer buffer in buffers)
                if (buffer.variables.TryGetValue(name, out variable))
                {
                    if (variable.size < Marshal.SizeOf(value))
                        throw new ArgumentException("Value size can't be bigger than " + variable.size.ToString() + " bytes for \"" + name + "\".");
                    variable.value = value;
                    buffer.invalidated = true;
                    return;
                }
            throw new ArgumentException("Variable does not exists.");
        }
        public bool tryUpdateUniform(string name, object value)
        {
            ShaderVariable variable;
            foreach (ShaderBuffer buffer in buffers)
                if (buffer.variables.TryGetValue(name, out variable))
                {
                    if (variable.size < Marshal.SizeOf(value))
                        return false;
                    variable.value = value;
                    buffer.invalidated = true;
                    return true;
                }
            return false;
        }
        protected void generateBuffersAndLocations(ShaderReflection reflection)
        {
            for (int i = 0; i < reflection.Description.ConstantBuffers; i++)
            {
                ConstantBuffer buffer = reflection.GetConstantBuffer(i);
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
            return variables;
        }
        public void uploadUpdatedUniforms()
        {
            foreach (ShaderBuffer buf in buffers)
                if (buf.invalidated)
                {
                    DataStream stream;
                    GraphicsCore.CurrentDevice.ImmediateContext.MapSubresource(buf.buffer, 0, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out stream);
                    foreach (ShaderVariable variable in buf.variables.Values)
                    {
                        if (variable.value == null)
                            continue;

                        stream.Position = variable.offset;

                        Marshal.StructureToPtr(variable.value, stream.PositionPointer, true);
                    }
                    GraphicsCore.CurrentDevice.ImmediateContext.UnmapSubresource(buf.buffer, 0);
                    buf.invalidated = false;
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
            public override ShaderType Type { get => ShaderType.VertexShader; }
            private InputLayout layout;
            private VertexShader shader;

            public Shader_Vertex(string path)
            {
                ShaderBytecode bytecode = ShaderBytecode.CompileFromFile(path, "main", "vs_5_0");
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
                                    inputElementDescription.Format = Format.R32_Float;
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
            public override ShaderType Type { get => ShaderType.HullShader; }
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
            public override ShaderType Type { get => ShaderType.DomainShader; }
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
            public override ShaderType Type { get => ShaderType.GeometryShader; }
            private GeometryShader shader;
            public Shader_Geometry(string path)
            {
                ShaderBytecode bytecode = ShaderBytecode.CompileFromFile(path, "main", "gs_5_0");
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
            public override ShaderType Type { get => ShaderType.FragmentShader; }
            private PixelShader shader;
            public Shader_Fragment(string path)
            {
                ShaderBytecode bytecode = ShaderBytecode.CompileFromFile(path, "main", "ps_5_0");
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
    }
    public class ShaderPipeline
    {
        private List<Shader> shaders = new List<Shader>();
        public ReadOnlyCollection<Shader> Shaders { get => shaders.AsReadOnly(); }
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
                if (shader.hasVariable(name))
                {
                    shader.updateUniform(name, value);
                    exists = true;
                }
            if (!exists)
                throw new ArgumentException("Variable \n" + name + "\n does not exists in this shader pipeline.");
        }
        public bool TryUpdateUniform(string name, object value)
        {
            bool exists = false;
            foreach (Shader shader in shaders)
                if (shader.hasVariable(name))
                {
                    shader.tryUpdateUniform(name, value);
                    exists = true;
                }
            if (!exists)
                return false;
            return true;
        }
        public void Use()
        {
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
    public static class AssetsManager
    {
        public static Dictionary<string, Model> Models { get; } = new Dictionary<string, Model>();
        public static Dictionary<string, ShaderPipeline> ShaderPipelines { get; } = new Dictionary<string, ShaderPipeline>();
        public static Dictionary<string, Texture> Textures { get; } = new Dictionary<string, Texture>();
        public static Dictionary<string, Sampler> Samplers { get; } = new Dictionary<string, Sampler>();
        public static Dictionary<string, Scene> Scenes { get; } = new Dictionary<string, Scene>();
        public static Dictionary<string, Sound> Sounds { get; } = new Dictionary<string, Sound>();

        public static Dictionary<string, Model> LoadModelsFile(string path, float scaleFactor = 1.0f, bool reverse = false)
        {
            StreamReader reader = new StreamReader(File.OpenRead(path));

            Dictionary<string, Model> models = new Dictionary<string, Model>();

            string modelName = Path.GetFileNameWithoutExtension(path);
            Model model = new Model();
            string line;
            int offset_v = 0;
            int offset_t = 0;
            int offset_n = 0;
            char[] separator = new char[] { ' ' };
            while ((line = reader.ReadLine()) != null)
            {
                if (NumberFormatInfo.CurrentInfo.NumberDecimalSeparator == ",")
                    line = line.Replace('.', ',');
                else
                    line = line.Replace(',', '.');
                string[] words = line.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length == 0)
                    continue;
                switch (words[0])
                {
                    case "o":
                        if (model.v != null)
                        {
                            if (models.ContainsKey(modelName))
                                throw new ArgumentException("File can't have more than one model with the same name.");
                            if (Models.ContainsKey(modelName))
                                throw new ArgumentException("Model with name \"" + modelName + "\" already loaded.");
                            models[modelName] = model;
                            Models[modelName] = model;
                            offset_v += model.v.Count;
                            if (model.t != null)
                                offset_t += model.t.Count;
                            if (model.n != null)
                                offset_n += model.n.Count;
                            model = new Model();
                        }
                        modelName = line.Substring(2);
                        break;
                    case "v":
                        if (model.v == null)
                        {
                            model.v = new List<Vector3>();
                            model.v_i = new List<int[]>();
                        }
                        model.v.Add(new Vector3(double.Parse(words[1]) * scaleFactor, double.Parse(words[2]) * scaleFactor, double.Parse(words[3]) * scaleFactor));
                        break;
                    case "vt":
                        if (model.t == null)
                        {
                            model.t = new List<Vector2>();
                            model.t_i = new List<int[]>();
                        }
                        model.t.Add(new Vector2(double.Parse(words[1]), double.Parse(words[2])));
                        break;
                    case "vn":
                        if (model.n == null)
                        {
                            model.n = new List<Vector3>();
                            model.n_i = new List<int[]>();
                        }
                        model.n.Add(new Vector3(double.Parse(words[1]), double.Parse(words[2]), double.Parse(words[3])));
                        break;
                    case "f":
                        int vertexesCount = words.Length - 1;
                        if (vertexesCount > 3)
                            throw new NotSupportedException("DirectX does not support non-triangulated models.");
                        int[] v_i = new int[vertexesCount];
                        int[] t_i = null;
                        int[] n_i = null;
                        if (model.t_i != null)
                            t_i = new int[vertexesCount];
                        if (model.n_i != null)
                            n_i = new int[vertexesCount];
                        if (reverse)
                            for (int i = 0; i < vertexesCount; i++)
                            {
                                string[] values = words[1 + i].Split('/');
                                v_i[vertexesCount - i - 1] = int.Parse(values[0]) - offset_v - 1;
                                if (t_i != null)
                                    t_i[vertexesCount - i - 1] = int.Parse(values[1]) - offset_t - 1;
                                if (n_i != null)
                                    n_i[vertexesCount - i - 1] = int.Parse(values[2]) - offset_n - 1;
                            }
                        else
                            for (int i = 0; i < vertexesCount; i++)
                            {
                                string[] values = words[1 + i].Split('/');
                                v_i[i] = int.Parse(values[0]) - offset_v - 1;
                                if (t_i != null)
                                    t_i[i] = int.Parse(values[1]) - offset_t - 1;
                                if (n_i != null)
                                    n_i[i] = int.Parse(values[2]) - offset_n - 1;
                            }
                        for (int i = 1; i < vertexesCount - 1; i++)
                        {
                            model.v_i.Add(new int[3] { v_i[0], v_i[i], v_i[i + 1] });
                            if (t_i != null)
                                model.t_i.Add(new int[3] { t_i[0], t_i[i], t_i[i + 1] });
                            if (n_i != null)
                                model.n_i.Add(new int[3] { n_i[0], n_i[i], n_i[i + 1] });
                        }
                        break;
                }
            }
            if (model.v != null)
            {
                if (models.ContainsKey(modelName))
                    throw new ArgumentException("File can't have more than one model with the same name.");
                if (Models.ContainsKey(modelName))
                    throw new ArgumentException("Model with name \"" + modelName + "\" already loaded.");
                models[modelName] = model;
                Models[modelName] = model;
            }
            foreach (Model mdl in models.Values)
                mdl.updateModel();
            return models;
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
                throw new ArgumentException("Texture with name \"" + textureName + "\" is already loaded.");
        
            Texture texture = new Texture(new Bitmap(path), applyGammaCorrection);
        
            Textures[textureName] = texture;
        
            return texture;
        }
        public static Sound LoadSound(string path, string soundName = "")
        {
            if (soundName == "")
                soundName = Path.GetFileNameWithoutExtension(path);
            if (Sounds.ContainsKey(soundName))
                throw new ArgumentException("Sound with name \"" + soundName + "\" is already loaded.");

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
                    case "Model":
                        {
                            if (!Models.ContainsKey(words[1]))
                                throw new Exception("Model " + words[1] + " not loaded.");
                            PropertyInfo property = objType.GetProperty(name);
                            if (property != null)
                                property.SetValue(obj, Models[words[1]]);
                            else
                            {
                                FieldInfo field = objType.GetField(name);
                                if (field != null)
                                    field.SetValue(obj, Models[words[1]]);
                                else
                                    throw new Exception(objType.Name + " don't have " + name + ".");
                            }
                            break;
                        }
                    case "Texture":
                        {
                            if (!Textures.ContainsKey(words[1]))
                                throw new Exception("Texture " + words[1] + " not loaded.");
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
                            case "Models":
                                {
                                    foreach (XElement model in assetsSet.Elements())
                                    {
                                        MethodInfo method = typeof(AssetsManager).GetMethod("LoadModelsFile");
                                        ParameterInfo[] parameters = method.GetParameters();
                                        Dictionary<string, object> parameterValues = new Dictionary<string, object>();
                                        foreach (XAttribute attrib in model.Attributes())
                                        {
                                            bool found = false;
                                            foreach (ParameterInfo param in parameters)
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
                                            if (!found)
                                                throw new Exception("Attribute \"" + attrib.Name.LocalName + "\" not found.");
                                        }
                                        method.Invoke(null, parameters.Select(p => parameterValues.ContainsKey(p.Name) ? parameterValues[p.Name] :
                                                        (p.IsOptional ? p.DefaultValue : throw new Exception("Missing required attribute: \"" + p.Name + "\""))).ToArray());
                                    }
                                    continue;
                                }
                            case "Textures":
                                {
                                    foreach (XElement texture in assetsSet.Elements())
                                    {
                                        MethodInfo method = typeof(AssetsManager).GetMethod("LoadTexture");
                                        ParameterInfo[] parameters = method.GetParameters();
                                        Dictionary<string, object> parameterValues = new Dictionary<string, object>();
                                        foreach (XAttribute attrib in texture.Attributes())
                                        {
                                            bool found = false;
                                            foreach (ParameterInfo param in parameters)
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
                                            if (!found)
                                                throw new Exception("Attribute \"" + attrib.Name.LocalName + "\" not found.");
                                        }
                                        method.Invoke(null, parameters.Select(p => parameterValues.ContainsKey(p.Name) ? parameterValues[p.Name] :
                                                        (p.IsOptional ? p.DefaultValue : throw new Exception("Missing required attribute: \"" + p.Name + "\""))).ToArray());
                                    }
                                    continue;
                                }
                            case "Sounds":
                                {
                                    foreach (XElement sound in assetsSet.Elements())
                                    {
                                        MethodInfo method = typeof(AssetsManager).GetMethod("LoadSound");
                                        ParameterInfo[] parameters = method.GetParameters();
                                        Dictionary<string, object> parameterValues = new Dictionary<string, object>();
                                        foreach (XAttribute attrib in sound.Attributes())
                                        {
                                            bool found = false;
                                            foreach (ParameterInfo param in parameters)
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
                                            if (!found)
                                                throw new Exception("Attribute \"" + attrib.Name.LocalName + "\" not found.");
                                        }
                                        method.Invoke(null, parameters.Select(p => parameterValues.ContainsKey(p.Name) ? parameterValues[p.Name] :
                                                        (p.IsOptional ? p.DefaultValue : throw new Exception("Missing required attribute: \"" + p.Name + "\""))).ToArray());
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
                            (curObj as GameObject).transform.setParent((parent as GameObject).transform);
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
                            curObj = (parent as GameObject).transform;
                        else
                            curObj = (parent as GameObject).addComponent(curType);
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
                                    property.SetValue(parent, genericListType.GetMethod("ToArray").Invoke(curObj, null));
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
}