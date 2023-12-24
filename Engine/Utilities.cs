using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.IO.Enumeration;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Engine.BaseAssets.Components;
using LinearAlgebra;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using SharpDX.Multimedia;
using SharpDX.XAudio2;
using Assimp;
using Quaternion = LinearAlgebra.Quaternion;
using Vector3 = LinearAlgebra.Vector3;

namespace Engine
{
    // model contains a number of meshes
    // materials from model should be loaded as separate assets too
    // materials contain a number of textures
    // each of them: meshes, materials, textures should be loaded as separate assets

    [Obsolete]
    public static class AssetsManager_Old
    {
        public static Dictionary<string, Model> Models { get; } = new Dictionary<string, Model>();
        public static Dictionary<string, ShaderPipeline> ShaderPipelines { get; } = new Dictionary<string, ShaderPipeline>();
        public static Dictionary<string, Shader> Shaders { get; } = new Dictionary<string, Shader>();
        public static Dictionary<string, Material> Materials { get; } = new Dictionary<string, Material>();
        public static Dictionary<string, Texture> Textures { get; } = new Dictionary<string, Texture>();
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

            Dictionary<string, Model> models = new Dictionary<string, Model>();
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
                Material material = Material.Default;
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

                Model model;

                // in order to store different primitives into one mesh trying to find it by name
                if (!models.TryGetValue(aiMesh.Name, out model))
                {
                    model = new Model();
                    models.Add(aiMesh.Name, model);
                }

                Mesh mesh = new Mesh();
                mesh.DefaultMaterial = materials[materialPrefix + aiMesh.MaterialIndex.ToString()];
                mesh.vertices = new List<Mesh.PrimitiveVertex>();
                mesh.indices = new List<int>();
                List<Vector3D> verts = aiMesh.Vertices;
                List<Vector3D> norms = (aiMesh.HasNormals) ? aiMesh.Normals : null;
                List<Vector3D> uvs = aiMesh.HasTextureCoords(0) ? aiMesh.TextureCoordinateChannels[0] : null;
                for (int i = 0; i < verts.Count; i++)
                {
                    Vector3D pos = verts[i];
                    Vector3D norm = (norms != null) ? norms[i] : new Vector3D(0, 1, 0); // Y-up by default
                    Vector3D uv = (uvs != null) ? uvs[i] : new Vector3D(0, 0, 0);
                    Mesh.PrimitiveVertex vertex = new Mesh.PrimitiveVertex();
                    vertex.v = new Vector3f(pos.X * scaleFactor, pos.Y * scaleFactor, pos.Z * scaleFactor);
                    vertex.n = new Vector3f(norm.X, norm.Y, norm.Z);
                    vertex.t = new Vector2f(uv.X, 1 - uv.Y);
                    mesh.vertices.Add(vertex);
                }

                foreach (Face face in aiMesh.Faces)
                    mesh.indices.AddRange(face.Indices);

                mesh.GenerateGPUData();

                model.Meshes.Add(mesh);
            }

            Models.AddRange(models);
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

        #region Legacy_LoadScene

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
                            if (!Models.ContainsKey(words[1]))
                                throw new Exception("Mesh " + words[1] + " is not loaded.");
                            PropertyInfo property = objType.GetProperty(name);
                            if (property != null)
                                property.SetValue(obj, Models[words[1]]);
                            else
                            {
                                FieldInfo field = objType.GetField(name);
                                if (field != null)
                                    field.SetValue(obj, Models[words[1]]);
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

        #endregion Legacy_LoadScene

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