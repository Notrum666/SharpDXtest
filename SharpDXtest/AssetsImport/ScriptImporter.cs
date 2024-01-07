using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using Engine;
using Engine.AssetsData;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Editor.AssetsImport
{
    [AssetImporter("cs")]
    public class ScriptImporter : AssetImporter
    {
        public override int LatestVersion => 1;

        protected override void OnImportAsset(AssetImportContext importContext)
        {
            importContext.DataStream.Position = 0;
            using StreamReader reader = new StreamReader(importContext.DataStream, Encoding.UTF8);
            string scriptCode = reader.ReadToEnd();

            ParseScriptCode(scriptCode);

            throw new NotImplementedException();
            
            ScriptData scriptData = new ScriptData();

            importContext.AddMainAsset(scriptData);
        }

        private void ParseScriptCode(string code)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
            SyntaxNode root = tree.GetRoot();
            IEnumerable<ClassDeclarationSyntax> classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
            Debug.WriteLine($"Found {classDeclarations.Count()} classes in file");
            foreach (ClassDeclarationSyntax classDeclaration in classDeclarations)
            {
                var baseTypes = classDeclaration.BaseList?.Types;
                if (!baseTypes.HasValue || baseTypes.Value.Count == 0)
                    continue;
                
                foreach (BaseTypeSyntax baseType in baseTypes)
                {
                    string baseName = baseType.Type.ToString();
                    Debug.WriteLine($"baseName = {baseName}");
                }
            }
        }
    }
}