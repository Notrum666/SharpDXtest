using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

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
            if (!ScriptManager.IsCompilationRelevant)
                ScriptManager.Recompile();

            // ParseScriptCode(importContext.DataStream);

            ScriptData scriptData = new ScriptData();

            if (ScriptManager.FilesToTypesMap.TryGetValue(importContext.AssetSourcePath, out List<Type> fileTypes))
                scriptData.ClassTypes.AddRange(fileTypes);

            importContext.AddMainAsset(scriptData);
        }

        private void ParseScriptCode(Stream codeStream)
        {
            codeStream.Position = 0;
            using StreamReader reader = new StreamReader(codeStream, Encoding.UTF8);
            string code = reader.ReadToEnd();

            SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
            SyntaxNode root = tree.GetRoot();

            IEnumerable<ClassDeclarationSyntax> classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
            Debug.WriteLine($"Found {classDeclarations.Count()} classes in file");
            foreach (ClassDeclarationSyntax classDeclaration in classDeclarations)
            {
                var baseTypes = classDeclaration.BaseList?.Types;
                if (!baseTypes.HasValue || baseTypes.Value.Count == 0)
                    continue;

                var baseType = baseTypes.Value[0];
            }
        }
    }
}