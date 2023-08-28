using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Generator
{
    public static class Helper
    {
        public static Guid EncodeGuid(byte[] data)
        {
            if (BitConverter.IsLittleEndian)
            {
                // swap bytes of int a
                byte t = data[0];
                data[0] = data[3];
                data[3] = t;
                t = data[1];
                data[1] = data[2];
                data[2] = t;
                // swap bytes of short b
                t = data[4];
                data[4] = data[5];
                data[5] = t;
                // swap bytes of short c and encode rfc time/version field
                t = data[6];
                data[6] = data[7];
                data[7] = (byte)((t & 0x0f) | (5 << 4));
                // encode rfc clock/reserved field
                data[8] = (byte)((data[8] & 0x3f) | 0x80);
            }
            return new Guid(data.Take(16).ToArray());
        }
    }

    class AttributeDataComparer : IEqualityComparer<AttributeData>
    {
        public bool Equals(AttributeData x, AttributeData y)
        {
            return string.CompareOrdinal(x.ToString(), y.ToString()) == 0;
        }

        public int GetHashCode(AttributeData obj)
        {
            return obj.ToString().GetHashCode();
        }
    }

    static class GeneratorExecutionContextHelper
    {
        public static string GetAssemblyName(this GeneratorExecutionContext context)
        {
            context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.AssemblyName", out var assemblyName);
            return assemblyName;
        }

        public static string GetAssemblyVersion(this GeneratorExecutionContext context)
        {
            context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.AssemblyVersion", out var assemblyVersion);
            return assemblyVersion;
        }

        public static string GetGeneratedFilesDir(this GeneratorExecutionContext context)
        {
            context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.CsWinRTGeneratedFilesDir", out var generatedFilesDir);
            Directory.CreateDirectory(generatedFilesDir);
            return generatedFilesDir;
        }

        public static bool IsCsWinRTComponent(this GeneratorExecutionContext context)
        {
            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.CsWinRTComponent", out var isCsWinRTComponentStr))
            {
                return bool.TryParse(isCsWinRTComponentStr, out var isCsWinRTComponent) && isCsWinRTComponent;
            }

            return false;
        }

        public static bool ShouldGenerateWinMDOnly(this GeneratorExecutionContext context)
        {
            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.CsWinRTGenerateWinMDOnly", out var CsWinRTGenerateWinMDOnlyStr))
            {
                return bool.TryParse(CsWinRTGenerateWinMDOnlyStr, out var CsWinRTGenerateWinMDOnly) && CsWinRTGenerateWinMDOnly;
            }

            return false;
        }

        public static string GetCsWinRTExe(this GeneratorExecutionContext context)
        {
            context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.CsWinRTExe", out var cswinrtExe);
            return cswinrtExe;
        }

        public static bool GetKeepGeneratedSources(this GeneratorExecutionContext context)
        {
            context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.CsWinRTKeepGeneratedSources", out var keepGeneratedSourcesStr);
            return keepGeneratedSourcesStr != null && bool.TryParse(keepGeneratedSourcesStr, out var keepGeneratedSources) && keepGeneratedSources;
        }

        public static string GetCsWinRTWindowsMetadata(this GeneratorExecutionContext context)
        {
            context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.CsWinRTWindowsMetadata", out var cswinrtWindowsMetadata);
            return cswinrtWindowsMetadata;
        }

        public static string GetCsWinRTDependentMetadata(this GeneratorExecutionContext context)
        {
            context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.CsWinRTAuthoringInputs", out var winmds);
            return winmds;
        }

        public static string GetWinmdOutputFile(this GeneratorExecutionContext context)
        {
            var fileName = context.GetAssemblyName();
            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.CsWinRTWinMDOutputFile", out var ret))
            {
                fileName = ret!;
            }
            return Path.Combine(context.GetGeneratedFilesDir(), fileName + ".winmd");
        }
    }

    static class GeneratorHelper
    {
        public static bool IsWinRTType(ISymbol type)
        {
            bool isProjectedType = type.GetAttributes().
                Any(attribute => string.CompareOrdinal(attribute.AttributeClass.Name, "WindowsRuntimeTypeAttribute") == 0);
            return isProjectedType;
        }

        public static bool IsWinRTType(MemberDeclarationSyntax node)
        {
            bool isProjectedType = node.AttributeLists.SelectMany(list => list.Attributes).
                Any(attribute => string.CompareOrdinal(attribute.Name.NormalizeWhitespace().ToFullString(), "WindowsRuntimeTypeAttribute") == 0);
            return isProjectedType;
        }
    }
}
