using System;
using System.Collections.Generic;
using System.IO;
using Engine;
using Engine.AssetsData;
using SharpDX.D3DCompiler;

namespace Editor.AssetsImport
{
    [AssetImporter("vsh", "hsh", "dsh", "gsh", "fsh", "csh")]
    public class ShaderImporter : AssetImporter
    {
        public override int LatestVersion => 1;

        private ShaderData currentShaderData;

        protected override void OnImportAsset(AssetImportContext importContext)
        {
            currentShaderData = new ShaderData();

            ProcessShaderType(importContext.AssetContentPath);
            CompileBytecode(importContext.DataStream);

            ShaderReflection reflection = new ShaderReflection(currentShaderData.Bytecode);
            ProcessLocations(reflection);
            ProcessBuffers(reflection);

            importContext.AddMainAsset(currentShaderData);
        }

        private void ProcessShaderType(string path)
        {
            string extension = Path.GetExtension(path);
            currentShaderData.ShaderType = extension switch
            {
                ".vsh" => ShaderType.VertexShader,
                ".hsh" => ShaderType.HullShader,
                ".dsh" => ShaderType.DomainShader,
                ".gsh" => ShaderType.GeometryShader,
                ".fsh" => ShaderType.FragmentShader,
                ".csh" => ShaderType.ComputeShader,
                _ => throw new ArgumentException("Can't get shader type from extension, consider using other Shader.Create() overload.")
            };
        }

        private void CompileBytecode(Stream dataStream)
        {
            string data = null;
            using (StreamReader reader = new StreamReader(dataStream))
                data = reader.ReadToEnd();

            ShaderFlags shaderFlags = ShaderFlags.None;
#if GraphicsDebugging
            shaderFlags |= ShaderFlags.Debug | ShaderFlags.SkipOptimization | ShaderFlags.PreferFlowControl;
#endif

            currentShaderData.Bytecode = currentShaderData.ShaderType switch
            {
                ShaderType.VertexShader => ShaderBytecode.Compile(data, "main", "vs_5_0", shaderFlags),
                ShaderType.HullShader => ShaderBytecode.Compile(data, "main", "hs_5_0", shaderFlags),
                ShaderType.DomainShader => ShaderBytecode.Compile(data, "main", "ds_5_0", shaderFlags),
                ShaderType.GeometryShader => ShaderBytecode.Compile(data, "main", "gs_5_0", shaderFlags),
                ShaderType.FragmentShader => ShaderBytecode.Compile(data, "main", "ps_5_0", shaderFlags | ShaderFlags.SkipOptimization),
                ShaderType.ComputeShader => ShaderBytecode.Compile(data, "main", "cs_5_0", shaderFlags),
                _ => throw new ArgumentException($"Unsupported ShaderType = {currentShaderData.ShaderType}")
            };
        }

        private void ProcessLocations(ShaderReflection reflection)
        {
            for (int i = 0; i < reflection.Description.BoundResources; i++)
            {
                InputBindingDescription desc = reflection.GetResourceBindingDescription(i);
                switch (desc.Type)
                {
                    case ShaderInputType.Texture:
                    case ShaderInputType.Sampler:
                        currentShaderData.Locations[desc.Name] = desc.BindPoint;
                        break;
                }
            }
        }

        private void ProcessBuffers(ShaderReflection reflection)
        {
            for (int i = 0; i < reflection.Description.ConstantBuffers; i++)
            {
                ConstantBuffer buffer = reflection.GetConstantBuffer(i);
                if (buffer.Description.Type != ConstantBufferType.ConstantBuffer) //TODO: Do we need this? Always FALSE
                    continue;

                ShaderData.ShaderBufferData shaderBuffer = new ShaderData.ShaderBufferData();
                shaderBuffer.BufferSize = buffer.Description.Size;

                for (int j = 0; j < buffer.Description.VariableCount; j++)
                    shaderBuffer.Variables.AddRange(ParseShaderVariable(buffer.GetVariable(j)));

                currentShaderData.Buffers.Add(shaderBuffer);
            }
        }

        private Dictionary<string, ShaderData.ShaderBufferData.ShaderVariableData> ParseShaderVariable(ShaderReflectionVariable variable)
        {
            return ParseShaderVariable(variable.GetVariableType(), variable.Description.Name, variable.Description.StartOffset, variable.Description.Size);
        }

        private Dictionary<string, ShaderData.ShaderBufferData.ShaderVariableData> ParseShaderVariable(ShaderReflectionType type, string varName, int parentOffset, int varSize)
        {
            int elementCount = type.Description.ElementCount;
            if (elementCount == 0)
                return ParseNonArrayShaderVariable(type, varName, parentOffset, varSize);

            int elementOffset = (int)Math.Ceiling(varSize / (double)elementCount / 16.0) * 16;
            int elementSize = varSize - elementOffset * (elementCount - 1);
            Dictionary<string, ShaderData.ShaderBufferData.ShaderVariableData> variables = new Dictionary<string, ShaderData.ShaderBufferData.ShaderVariableData>();

            for (int i = 0; i < elementCount; i++)
                variables.AddRange(ParseNonArrayShaderVariable(type, $"{varName}[{i}]", parentOffset + i * elementOffset, elementSize));

            return variables;
        }

        private Dictionary<string, ShaderData.ShaderBufferData.ShaderVariableData> ParseNonArrayShaderVariable(ShaderReflectionType type, string varName, int parentOffset, int varSize)
        {
            Dictionary<string, ShaderData.ShaderBufferData.ShaderVariableData> variables = new Dictionary<string, ShaderData.ShaderBufferData.ShaderVariableData>();
            if (type.Description.MemberCount == 0)
                variables.Add(varName, new ShaderData.ShaderBufferData.ShaderVariableData() { Offset = parentOffset + type.Description.Offset, Size = varSize });
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

                    variables.AddRange(ParseShaderVariable(subtype, $"{varName}.{type.GetMemberTypeName(i)}", parentOffset + type.Description.Offset, memberSize));
                }
            }
            return variables;
        }
    }
}