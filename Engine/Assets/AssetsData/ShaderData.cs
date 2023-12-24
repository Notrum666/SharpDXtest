using System;
using System.Collections.Generic;
using System.IO;
using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace Engine.AssetsData
{
    public class ShaderData : AssetData
    {
        public ShaderType ShaderType;

        public Dictionary<string, int> Locations = new Dictionary<string, int>();
        public List<ShaderBufferData> Buffers = new List<ShaderBufferData>();

        public byte[] Bytecode;

        public override void Serialize(BinaryWriter writer)
        {
            YamlManager.SaveToStream(writer.BaseStream, this);
        }

        public override void Deserialize(BinaryReader reader)
        {
            YamlManager.LoadFromStream(reader.BaseStream, this);
        }

        public override Shader ToRealAsset(Type assetType)
        {
            if (assetType != typeof(Shader))
                return null;

            Shader shader = Shader.Create(ShaderType, Bytecode);
            
            foreach (KeyValuePair<string, int> location in Locations)
            {
                shader.Locations[location.Key] = location.Value;
            }

            foreach (ShaderBufferData bufferData in Buffers)
            {
                Shader.ShaderBuffer shaderBuffer = new Shader.ShaderBuffer();
                shaderBuffer.Buffer = new Buffer(GraphicsCore.CurrentDevice, bufferData.BufferSize, ResourceUsage.Dynamic, BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None, 0);

                foreach (KeyValuePair<string, ShaderBufferData.ShaderVariableData> variable in bufferData.Variables)
                {
                    ShaderBufferData.ShaderVariableData variableData = variable.Value;
                    Shader.ShaderVariable shaderVariable = new Shader.ShaderVariable
                    {
                        Offset = variableData.Offset,
                        Size = variableData.Size,
                        Value = null
                    };
                    shaderBuffer.Variables[variable.Key] = shaderVariable;
                }

                shader.AddBuffer(shaderBuffer);
            }

            return shader;
        }

        public class ShaderBufferData
        {
            public Dictionary<string, ShaderVariableData> Variables = new Dictionary<string, ShaderVariableData>();
            public int BufferSize;

            public class ShaderVariableData
            {
                public int Size;
                public int Offset;
            }
        }
    }
}