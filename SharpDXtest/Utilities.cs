using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.D3DCompiler;

using Buffer = SharpDX.Direct3D11.Buffer;

namespace SharpDXtest
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
        private Buffer indexBuffer;

        private struct ModelVertex
        {
            public Vector3f v;
            public Vector2f t;
            public Vector3f n;
        }

        public void updateModel()
        {
            if (v == null || v_i == null)
                throw new Exception("Model can't be empty.");

            List<ModelVertex> vertexes = new List<ModelVertex>();
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
                        Vector3 normal = (p3 - p1).vecMul(p2 - p1).normalized();
                        curVertex.n.x = (float)normal.x;
                        curVertex.n.y = (float)normal.y;
                        curVertex.n.z = (float)normal.z;
                    }
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
            indexBuffer = Buffer.Create(GraphicsCore.CurrentDevice, BindFlags.IndexBuffer, indexes);
        }

        public void Render()
        {
            GraphicsCore.CurrentDevice.ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertexBuffer, Utilities.SizeOf<ModelVertex>(), 0));
            GraphicsCore.CurrentDevice.ImmediateContext.InputAssembler.SetIndexBuffer(indexBuffer, Format.R32_UInt, 0);
            GraphicsCore.CurrentDevice.ImmediateContext.DrawIndexed(v_i.Count * 3, 0, 0);
        }
    }
    //public class Texture
    //{
    //    public Bitmap image;
    //    //public int id;
    //    public Texture(Bitmap image, bool applyGammaCorrection = false)
    //    {
    //        this.image = image;
    //
    //        //id = GL.GenTexture();
    //        //GL.BindTexture(TextureTarget.Texture2D, id);
    //        //
    //        //GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, new int[] { (int)TextureMinFilter.Nearest });
    //        //GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, new int[] { (int)TextureMagFilter.Nearest });
    //        //GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, new int[] { (int)TextureWrapMode.ClampToBorder });
    //        //GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, new int[] { (int)TextureWrapMode.ClampToBorder });
    //
    //        int height = image.Height;
    //        int width = image.Width;
    //        byte[,] data = new byte[height, width * 4];
    //        Color color;
    //        for (int i = 0; i < height; i++)
    //            for (int j = 0; j < width; j++)
    //            {
    //                color = image.GetPixel(j, i);
    //                data[i, j * 4] = color.R;
    //                data[i, j * 4 + 1] = color.G;
    //                data[i, j * 4 + 2] = color.B;
    //                data[i, j * 4 + 3] = color.A;
    //            }
    //
    //        //GL.TexImage2D(TextureTarget.Texture2D, 0, applyGammaCorrection ? PixelInternalFormat.SrgbAlpha : PixelInternalFormat.Rgba, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data);
    //
    //        //GL.BindTexture(TextureTarget.Texture2D, 0);
    //    }
    //}
    //public class Shader
    //{
    //    public int id;
    //    public Dictionary<string, int> locations;
    //    public Shader(int id)
    //    {
    //        this.id = id;
    //        locations = new Dictionary<string, int>();
    //        int uniformsCount;
    //        GL.GetProgram(id, GetProgramParameterName.ActiveUniforms, out uniformsCount);
    //        for (int i = 0; i < uniformsCount; i++)
    //        {
    //            string uniformName = GL.GetActiveUniform(id, i, out _, out _);
    //            if (uniformName.EndsWith("[0]"))
    //                uniformName = uniformName.Substring(0, uniformName.Length - 3);
    //            locations[uniformName] = GL.GetUniformLocation(id, uniformName);
    //        }
    //    }
    //}
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
        public abstract void Use();
        public static Shader Create(string path)
        {
            string extension = Path.GetExtension(path);
            switch (extension)
            {
                case ".vsh":
                    return new Shader_Vertex(path);
                case ".hsh":
                    return new Shader_Hull(path);
                case ".dsh":
                    return new Shader_Domain(path);
                case ".gsh":
                    return new Shader_Geometry(path);
                case ".fsh":
                    return new Shader_Fragment(path);
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

        private Buffer buf;
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

                buf = null;
                for (int i = 0; i < reflection.Description.ConstantBuffers; i++)
                {
                    ConstantBuffer buffer = reflection.GetConstantBuffer(i);
                    buf = new Buffer(GraphicsCore.CurrentDevice, buffer.Description.Size, ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
                    for (int j = 0; j < buffer.Description.VariableCount; j++)
                    {
                        ShaderReflectionVariable variable = buffer.GetVariable(j);
                    }
                }
            }
            public override void Use()
            {
                GraphicsCore.CurrentDevice.ImmediateContext.InputAssembler.InputLayout = layout;
                GraphicsCore.CurrentDevice.ImmediateContext.VertexShader.Set(shader);
                if (buf != null)
                {
                    Matrix4f mat = Matrix4f.Identity;
                    mat.v00 = 0.5f;
                    mat.v11 = 0.5f;
                    mat.v30 = 0.5f;
                    GraphicsCore.CurrentDevice.ImmediateContext.UpdateSubresource(new[] { mat }, buf, 0);
                    //GraphicsCore.CurrentDevice.ImmediateContext.UpdateSubresource(new[] { Matrix4f.Identity }, buf, 1);
                    //GraphicsCore.CurrentDevice.ImmediateContext.UpdateSubresource(new[] { Matrix4f.Identity }, buf, 2);
                    GraphicsCore.CurrentDevice.ImmediateContext.VertexShader.SetConstantBuffer(0, buf);
                }
            }
        }
        private class Shader_Hull : Shader
        {
            public override ShaderType Type { get => ShaderType.HullShader; }
            private HullShader shader;
            public Shader_Hull(string path)
            {
                shader = new HullShader(GraphicsCore.CurrentDevice, ShaderBytecode.CompileFromFile(path, "main", "hs_5_0"));
            }
            public override void Use()
            {
                GraphicsCore.CurrentDevice.ImmediateContext.HullShader.Set(shader);
            }
        }
        private class Shader_Domain : Shader
        {
            public override ShaderType Type { get => ShaderType.DomainShader; }
            private DomainShader shader;
            public Shader_Domain(string path)
            {
                shader = new DomainShader(GraphicsCore.CurrentDevice, ShaderBytecode.CompileFromFile(path, "main", "ds_5_0"));
            }
            public override void Use()
            {
                GraphicsCore.CurrentDevice.ImmediateContext.DomainShader.Set(shader);
            }
        }
        private class Shader_Geometry : Shader
        {
            public override ShaderType Type { get => ShaderType.GeometryShader; }
            private GeometryShader shader;
            public Shader_Geometry(string path)
            {
                shader = new GeometryShader(GraphicsCore.CurrentDevice, ShaderBytecode.CompileFromFile(path, "main", "gs_5_0"));
            }
            public override void Use()
            {
                GraphicsCore.CurrentDevice.ImmediateContext.GeometryShader.Set(shader);
            }
        }
        private class Shader_Fragment : Shader
        {
            public override ShaderType Type { get => ShaderType.FragmentShader; }
            private PixelShader shader;
            public Shader_Fragment(string path)
            {
                shader = new PixelShader(GraphicsCore.CurrentDevice, ShaderBytecode.CompileFromFile(path, "main", "ps_5_0"));
            }
            public override void Use()
            {
                GraphicsCore.CurrentDevice.ImmediateContext.PixelShader.Set(shader);
            }
        }
    }
    public class ShaderPipeline
    {
        private List<Shader> shaders = new List<Shader>();
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
        public void Use()
        {
            foreach (Shader shader in shaders)
                shader.Use();
        }
    }
    //public class Scene
    //{
    //    public List<GameObject> objects = new List<GameObject>();
    //    public Camera MainCamera;
    //}
    public static class AssetsManager
    {
        public static Dictionary<string, Model> Models { get; } = new Dictionary<string, Model>();
        public static Dictionary<string, ShaderPipeline> ShaderPipelines { get; } = new Dictionary<string, ShaderPipeline>();
        //public static Dictionary<string, Texture> Textures = new Dictionary<string, Texture>();
        //public static Dictionary<string, Scene> Scenes = new Dictionary<string, Scene>();
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
                throw new ArgumentException("Shader pipeline with this name already loaded.");
            ShaderPipeline shaderPipeline = new ShaderPipeline(shaders);
            ShaderPipelines[shaderPipelineName] = shaderPipeline;
            return shaderPipeline;
        }
        //public static Texture LoadTexture(string path, string textureName = "", bool applyGammaCorrection = false)
        //{
        //    if (textureName == "")
        //        textureName = Path.GetFileNameWithoutExtension(path);
        //
        //    Texture texture = new Texture(new Bitmap(path), applyGammaCorrection);
        //
        //    Textures[textureName] = texture;
        //
        //    return texture;
        //}
        //private struct Reference
        //{
        //    public object obj;
        //    public string fieldName;
        //    public string referenceObjName;
        //    public Reference(object obj, string fieldName, string referenceObjName)
        //    {
        //        this.obj = obj;
        //        this.fieldName = fieldName;
        //        this.referenceObjName = referenceObjName;
        //    }
        //}
        //public static Scene LoadScene(string path)
        //{
        //    XDocument document = XDocument.Parse(File.ReadAllText(path));
        //
        //    Dictionary<string, object> namedObjects = new Dictionary<string, object>();
        //    List<Reference> references = new List<Reference>();
        //    List<Type> types = new List<Type>();
        //    foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        //        types.AddRange(assembly.GetTypes());
        //
        //    void parseSpecialAttribute(object obj, string name, string value)
        //    {
        //        string[] words = value.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        //        if (words.Length < 2)
        //            throw new Exception("Wrong attribute format.");
        //        Type objType = obj.GetType();
        //        switch (words[0])
        //        {
        //            case "Reference":
        //                references.Add(new Reference(obj, name, words[1]));
        //                break;
        //            case "Model":
        //                {
        //                    if (!Models.ContainsKey(words[1]))
        //                        throw new Exception("Model " + words[1] + " not loaded.");
        //                    PropertyInfo property = objType.GetProperty(name);
        //                    if (property != null)
        //                        property.SetValue(obj, Models[words[1]]);
        //                    else
        //                    {
        //                        FieldInfo field = objType.GetField(name);
        //                        if (field != null)
        //                            field.SetValue(obj, Models[words[1]]);
        //                        else
        //                            throw new Exception(objType.Name + " don't have " + name + ".");
        //                    }
        //                    break;
        //                }
        //            case "Texture":
        //                {
        //                    if (!Textures.ContainsKey(words[1]))
        //                        throw new Exception("Texture " + words[1] + " not loaded.");
        //                    PropertyInfo property = objType.GetProperty(name);
        //                    if (property != null)
        //                        property.SetValue(obj, Textures[words[1]]);
        //                    else
        //                    {
        //                        FieldInfo field = objType.GetField(name);
        //                        if (field != null)
        //                            field.SetValue(obj, Textures[words[1]]);
        //                        else
        //                            throw new Exception(objType.Name + " don't have " + name + ".");
        //                    }
        //                    break;
        //                }
        //        }
        //    }
        //    void parseAttributes(ref object obj, IEnumerable<XAttribute> attributes)
        //    {
        //        Type objType = obj.GetType();
        //        foreach (XAttribute attrib in attributes)
        //        {
        //            if (attrib.Name.LocalName == "x.Name")
        //            {
        //                if (namedObjects.ContainsKey(attrib.Value))
        //                    throw new Exception("Scene can't have two or more objects with same name.");
        //                namedObjects[attrib.Value] = obj;
        //                continue;
        //            }
        //            if (attrib.Value.StartsWith("{") && attrib.Value.EndsWith("}"))
        //                parseSpecialAttribute(obj, attrib.Name.LocalName, attrib.Value.Substring(1, attrib.Value.Length - 2));
        //            else
        //            {
        //                if (obj is Quaternion)
        //                {
        //                    switch (attrib.Name.LocalName)
        //                    {
        //                        case "X":
        //                            obj = Quaternion.FromAxisAngle(Vector3.UnitX, float.Parse(attrib.Value)) * (Quaternion)obj;
        //                            continue;
        //                        case "Y":
        //                            obj = Quaternion.FromAxisAngle(Vector3.UnitY, float.Parse(attrib.Value)) * (Quaternion)obj;
        //                            continue;
        //                        case "Z":
        //                            obj = Quaternion.FromAxisAngle(Vector3.UnitZ, float.Parse(attrib.Value)) * (Quaternion)obj;
        //                            continue;
        //                    }
        //                }
        //                PropertyInfo property = objType.GetProperty(attrib.Name.LocalName);
        //                if (property != null)
        //                    property.SetValue(obj, Convert.ChangeType(attrib.Value, property.PropertyType));
        //                else
        //                {
        //                    FieldInfo field = objType.GetField(attrib.Name.LocalName);
        //                    if (field != null)
        //                        field.SetValue(obj, Convert.ChangeType(attrib.Value, field.FieldType));
        //                    else
        //                        throw new Exception(objType.Name + " don't have " + attrib.Name.LocalName + ".");
        //                }
        //            }
        //        }
        //    }
        //    object parseElement(object parent, XElement parentElement, XElement element)
        //    {
        //        if (element.NodeType == XmlNodeType.Text)
        //            return Convert.ChangeType(element.Value.Trim(' ', '\n'), parent.GetType());
        //        object curObj = null;
        //        Type curType = types.Find(t => t.Name == element.Name.LocalName);
        //        if (curType != null)
        //        {
        //            if (curType == typeof(GameObject))
        //            {
        //                curObj = Activator.CreateInstance(typeof(GameObject));
        //                parseAttributes(ref curObj, element.Attributes());
        //                object nestedObject;
        //                foreach (XElement elem in element.Elements())
        //                    if ((nestedObject = parseElement(curObj, element, elem)).GetType() == typeof(GameObject))
        //                        (nestedObject as GameObject).transform.Parent = (curObj as GameObject).transform;
        //            }
        //            else if (curType.IsSubclassOf(typeof(Component)))
        //            {
        //                if (!(parent is GameObject))
        //                    throw new Exception("Components can only be inside of GameObject.");
        //                if (curType == typeof(Transform))
        //                    curObj = (parent as GameObject).transform;
        //                else
        //                    curObj = (parent as GameObject).addComponent(curType);
        //                parseAttributes(ref curObj, element.Attributes());
        //                foreach (XElement elem in element.Elements())
        //                    parseElement(curObj, element, elem);
        //            }
        //            else if (curType == typeof(Scene))
        //            {
        //                if (parent != null)
        //                    throw new Exception("Scene must be the root.");
        //                curObj = Activator.CreateInstance(typeof(Scene));
        //                parseAttributes(ref curObj, element.Attributes());
        //                foreach (XElement elem in element.Elements())
        //                {
        //                    object sceneObject = parseElement(curObj, element, elem);
        //                    if (!(sceneObject is GameObject))
        //                        throw new Exception("Scene can contain only GameObjects.");
        //                    (curObj as Scene).objects.Add(sceneObject as GameObject);
        //                }
        //            }
        //            else
        //            {
        //                if (curType == typeof(Quaternion))
        //                    curObj = Quaternion.Identity;
        //                else
        //                {
        //                    if (curType.GetConstructor(Type.EmptyTypes) != null)
        //                        curObj = Activator.CreateInstance(curType);
        //                    else
        //                        curObj = FormatterServices.GetUninitializedObject(curType);
        //                }
        //                parseAttributes(ref curObj, element.Attributes());
        //                foreach (XElement elem in element.Elements())
        //                    parseElement(curObj, element, elem);
        //            }
        //        }
        //        else
        //        {
        //            string[] nameParts = element.Name.LocalName.Split('.');
        //            if (nameParts.Length != 2 || nameParts[0] != parentElement.Name.LocalName)
        //                throw new Exception(element.Name.LocalName + " not found.");
        //
        //            IEnumerable<XAttribute> attributes = element.Attributes();
        //            IEnumerable<XElement> elements = element.Elements();
        //            FieldInfo field = parent.GetType().GetField(nameParts[1]);
        //            if (field == null)
        //                throw new Exception(parent.GetType().Name + " don't have " + nameParts[1] + ".");
        //            if (attributes.Count() != 0)
        //            {
        //                if (elements.Count() != 0)
        //                    throw new Exception("Setter can't have values in both places");
        //                if (field.FieldType == typeof(Quaternion))
        //                    curObj = Quaternion.Identity;
        //                else
        //                    curObj = Activator.CreateInstance(field.FieldType);
        //                parseAttributes(ref curObj, element.Attributes());
        //                field.SetValue(parent, curObj);
        //            }
        //            else
        //            {
        //                curType = field.FieldType;
        //                if (curType.IsArray || curType.IsGenericType && curType.GetGenericTypeDefinition() == typeof(List<>))
        //                {
        //                    Type listBaseType = field.FieldType.GetGenericArguments()[0];
        //                    Type listType = typeof(List<>).MakeGenericType(listBaseType);
        //                    curObj = Activator.CreateInstance(listType);
        //                    MethodInfo addMethod = listType.GetMethod("Add");
        //                    foreach (XElement elem in elements)
        //                    {
        //                        object listElement = parseElement(curObj, element, elem);
        //                        if (listElement.GetType() != listBaseType && !listElement.GetType().IsSubclassOf(listBaseType))
        //                            throw new Exception(listElement.GetType().Name + " does not match for " + listBaseType.Name + ".");
        //                        addMethod.Invoke(curObj, new object[] { listElement });
        //                    }
        //                    field.SetValue(parent, curObj);
        //                }
        //                else
        //                {
        //                    if (elements.Count() != 1)
        //                        throw new Exception("Only array and list types can contain more than one element.");
        //                    object nestedObject = parseElement(parent, parentElement, elements.First());
        //                    if (nestedObject.GetType() != curType && nestedObject.GetType().IsSubclassOf(curType))
        //                        throw new Exception(nestedObject.GetType().Name + " does not match for " + curType.Name + ".");
        //                    field.SetValue(parent, nestedObject);
        //                }
        //            }
        //        }
        //        return curObj;
        //    }
        //
        //    object scene = parseElement(null, null, document.Root);
        //    if (!(scene is Scene))
        //        throw new Exception("Scene must be as root.");
        //
        //    foreach (Reference reference in references)
        //    {
        //        if (!namedObjects.ContainsKey(reference.referenceObjName))
        //            throw new Exception(reference.referenceObjName + " not found.");
        //        FieldInfo field = reference.obj.GetType().GetField(reference.fieldName);
        //        if (field != null)
        //            field.SetValue(reference.obj, namedObjects[reference.referenceObjName]);
        //        else
        //        {
        //            PropertyInfo property = reference.obj.GetType().GetProperty(reference.fieldName);
        //            if (property != null)
        //                property.SetValue(reference.obj, namedObjects[reference.referenceObjName]);
        //            else
        //                throw new Exception(reference.obj.GetType().Name + " don't have " + reference.fieldName + ".");
        //        }
        //    }
        //
        //    Scenes[Path.GetFileNameWithoutExtension(path)] = scene as Scene;
        //    return scene as Scene;
        //}
    }
}