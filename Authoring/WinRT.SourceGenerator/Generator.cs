﻿using Microsoft.CodeAnalysis;
using System;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace Generator
{
    [Generator]
    public class SourceGenerator : ISourceGenerator
    {
        private string GetAssemblyName(SourceGeneratorContext context)
        {
            context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.AssemblyName", out var assemblyName);
            return assemblyName;
        }

        private string GetAssemblyVersion(SourceGeneratorContext context)
        {
            context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.AssemblyVersion", out var assemblyVersion);
            return assemblyVersion;
        }

        private string GetWinmdOutputFile(SourceGeneratorContext context)
        {
            string winmdDir = Path.Combine(Directory.GetCurrentDirectory(), "Generated Files");
            Directory.CreateDirectory(winmdDir);
            return Path.Combine(winmdDir, GetAssemblyName(context) + ".winmd");
        }

        private void GenerateWinMD(MetadataBuilder metadataBuilder, string outputFile)
        {
            Logger.Log("Writing " + outputFile);
            var managedPeBuilder = new ManagedPEBuilder(
                new PEHeaderBuilder(imageCharacteristics: Characteristics.ExecutableImage),
                new MetadataRootBuilder(metadataBuilder, "WindowsRuntime 1.4"),
                new BlobBuilder(),
                flags: CorFlags.ILOnly);

            var peBlob = new BlobBuilder();
            managedPeBuilder.Serialize(peBlob);

            using var fs = new FileStream(outputFile, FileMode.Create, FileAccess.Write);
            peBlob.WriteContentTo(fs);
        }

        public void Execute(SourceGeneratorContext context)
        {
            Logger.Initialize(context);

            try
            {
                string assembly = GetAssemblyName(context);
                string version = GetAssemblyVersion(context);
                MetadataBuilder metadataBuilder = new MetadataBuilder();

                var writer = new WinRTTypeWriter(
                    assembly,
                    version,
                    metadataBuilder);
                foreach (SyntaxTree tree in context.Compilation.SyntaxTrees)
                {
                    writer.Model = context.Compilation.GetSemanticModel(tree);
                    writer.Visit(tree.GetRoot());
                }
                writer.FinalizeGeneration();

                GenerateWinMD(metadataBuilder, GetWinmdOutputFile(context));
            }
            catch(Exception e)
            {
                Logger.Log(e.ToString());
                if(e.InnerException != null)
                {
                    Logger.Log(e.InnerException.ToString());
                }
                Logger.Close();
                throw;
            }

            Logger.Log("Done");
            Logger.Close();
        }

        public void Initialize(InitializationContext context)
        {
        }
    }
}
