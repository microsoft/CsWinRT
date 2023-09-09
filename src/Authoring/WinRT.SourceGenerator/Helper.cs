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
        private static bool IsFundamentalType(ISymbol type)
        {
            if (type is INamedTypeSymbol namedTypeSymbol)
            {
                switch (namedTypeSymbol.SpecialType)
                {
                    case SpecialType.System_Boolean:
                    case SpecialType.System_String:
                    case SpecialType.System_Single:
                    case SpecialType.System_Double:
                    case SpecialType.System_UInt16:
                    case SpecialType.System_UInt32:
                    case SpecialType.System_UInt64:
                    case SpecialType.System_Int16:
                    case SpecialType.System_Int32:
                    case SpecialType.System_Int64:
                    case SpecialType.System_Char:
                    case SpecialType.System_Byte:
                    case SpecialType.System_Object:
                        return true; 
                }
            }

            return type.ToDisplayString() == "System.Guid";
        }

        public static bool IsWinRTType(ISymbol type)
        {
            bool isProjectedType = type.GetAttributes().
                Any(attribute => string.CompareOrdinal(attribute.AttributeClass.Name, "WindowsRuntimeTypeAttribute") == 0) ||
                IsFundamentalType(type);

            if (!isProjectedType & type.ContainingNamespace != null)
            {
                isProjectedType = MappedCSharpTypes.ContainsKey(string.Join(".", type.ContainingNamespace.ToDisplayString(), type.MetadataName));
            }

            // Ensure all generic parameters are WinRT types.
            if (isProjectedType && type is INamedTypeSymbol namedType && namedType.IsGenericType && !namedType.IsDefinition)
            {
                isProjectedType = namedType.TypeArguments.All(IsWinRTType);
            }
            return isProjectedType;
        }

        public static bool IsWinRTType(MemberDeclarationSyntax node)
        {
            bool isProjectedType = node.AttributeLists.SelectMany(list => list.Attributes).
                Any(attribute => string.CompareOrdinal(attribute.Name.NormalizeWhitespace().ToFullString(), "WindowsRuntimeTypeAttribute") == 0);
            return isProjectedType;
        }

        public readonly struct MappedType
        {
            private readonly string @namespace;
            private readonly string name;
            private readonly string assembly;
            private readonly bool isSystemType;
            private readonly bool isValueType;
            private readonly Func<ISymbol, (string, string, string, bool, bool)> multipleMappingFunc;

            public MappedType(string @namespace, string name, string assembly, bool isValueType = false)
            {
                this.@namespace = @namespace;
                this.name = name;
                this.assembly = assembly;
                isSystemType = string.CompareOrdinal(this.assembly, "mscorlib") == 0;
                this.isValueType = isValueType;
                multipleMappingFunc = null;
            }

            public MappedType(Func<ISymbol, (string, string, string, bool, bool)> multipleMappingFunc)
            {
                @namespace = null;
                name = null;
                assembly = null;
                isSystemType = false;
                isValueType = false;
                this.multipleMappingFunc = multipleMappingFunc;
            }

            public (string, string, string, bool, bool) GetMapping(ISymbol containingType = null)
            {
                return multipleMappingFunc != null ?
                    multipleMappingFunc(containingType) : (@namespace, name, assembly, isSystemType, isValueType);
            }
        }

        // Based on whether System.Type is used in an attribute declaration or elsewhere, we need to choose the correct custom mapping
        // as attributes don't use the TypeName mapping.
        internal static (string, string, string, bool, bool) GetSystemTypeCustomMapping(ISymbol containingSymbol)
        {
            bool isDefinedInAttribute =
                containingSymbol != null &&
                    string.CompareOrdinal((containingSymbol as INamedTypeSymbol).BaseType?.ToString(), "System.Attribute") == 0;
            return isDefinedInAttribute ?
                ("System", "Type", "mscorlib", true, false) :
                ("Windows.UI.Xaml.Interop", "TypeName", "Windows.Foundation.UniversalApiContract", false, true);
        }

        // This should be in sync with the reverse mapping from WinRT.Runtime/Projections.cs and cswinrt/helpers.h.
        public static readonly Dictionary<string, MappedType> MappedCSharpTypes = new(StringComparer.Ordinal)
        {
            { "System.DateTimeOffset", new MappedType("Windows.Foundation", "DateTime", "Windows.Foundation.FoundationContract", true) },
            { "System.Exception", new MappedType("Windows.Foundation", "HResult", "Windows.Foundation.FoundationContract", true) },
            { "System.EventHandler`1", new MappedType("Windows.Foundation", "EventHandler`1", "Windows.Foundation.FoundationContract", true) },
            { "System.FlagsAttribute", new MappedType("System", "FlagsAttribute", "mscorlib" ) },
            { "System.IDisposable", new MappedType("Windows.Foundation", "IClosable", "Windows.Foundation.FoundationContract") },
            { "System.IServiceProvider", new MappedType("Microsoft.UI.Xaml", "IXamlServiceProvider", "Microsoft.UI") },
            { "System.Nullable`1", new MappedType("Windows.Foundation", "IReference`1", "Windows.Foundation.FoundationContract" ) },
            { "System.Object", new MappedType("System", "Object", "mscorlib" ) },
            { "System.TimeSpan", new MappedType("Windows.Foundation", "TimeSpan", "Windows.Foundation.FoundationContract", true) },
            { "System.Uri", new MappedType("Windows.Foundation", "Uri", "Windows.Foundation.FoundationContract") },
            { "System.ComponentModel.DataErrorsChangedEventArgs", new MappedType("Microsoft.UI.Xaml.Data", "DataErrorsChangedEventArgs", "Microsoft.UI") },
            { "System.ComponentModel.INotifyDataErrorInfo", new MappedType("Microsoft.UI.Xaml.Data", "INotifyDataErrorInfo", "Microsoft.UI") },
            { "System.ComponentModel.INotifyPropertyChanged", new MappedType("Microsoft.UI.Xaml.Data", "INotifyPropertyChanged", "Microsoft.UI") },
            { "System.ComponentModel.PropertyChangedEventArgs", new MappedType("Microsoft.UI.Xaml.Data", "PropertyChangedEventArgs", "Microsoft.UI") },
            { "System.ComponentModel.PropertyChangedEventHandler", new MappedType("Microsoft.UI.Xaml.Data", "PropertyChangedEventHandler", "Microsoft.UI") },
            { "System.Windows.Input.ICommand", new MappedType("Microsoft.UI.Xaml.Input", "ICommand", "Microsoft.UI") },
            { "System.Collections.IEnumerable", new MappedType("Microsoft.UI.Xaml.Interop", "IBindableIterable", "Microsoft.UI") },
            { "System.Collections.IList", new MappedType("Microsoft.UI.Xaml.Interop", "IBindableVector", "Microsoft.UI") },
            { "System.Collections.Specialized.INotifyCollectionChanged", new MappedType("Microsoft.UI.Xaml.Interop", "INotifyCollectionChanged", "Microsoft.UI") },
            { "System.Collections.Specialized.NotifyCollectionChangedAction", new MappedType("Microsoft.UI.Xaml.Interop", "NotifyCollectionChangedAction", "Microsoft.UI") },
            { "System.Collections.Specialized.NotifyCollectionChangedEventArgs", new MappedType("Microsoft.UI.Xaml.Interop", "NotifyCollectionChangedEventArgs", "Microsoft.UI") },
            { "System.Collections.Specialized.NotifyCollectionChangedEventHandler", new MappedType("Microsoft.UI.Xaml.Interop", "NotifyCollectionChangedEventHandler", "Microsoft.UI") },
            { "WinRT.EventRegistrationToken", new MappedType("Windows.Foundation", "EventRegistrationToken", "Windows.Foundation.FoundationContract", true) },
            { "System.AttributeTargets", new MappedType("Windows.Foundation.Metadata", "AttributeTargets", "Windows.Foundation.FoundationContract", true) },
            { "System.AttributeUsageAttribute", new MappedType("Windows.Foundation.Metadata", "AttributeUsageAttribute", "Windows.Foundation.FoundationContract") },
            { "System.Numerics.Matrix3x2", new MappedType("Windows.Foundation.Numerics", "Matrix3x2", "Windows.Foundation.FoundationContract", true) },
            { "System.Numerics.Matrix4x4", new MappedType("Windows.Foundation.Numerics", "Matrix4x4", "Windows.Foundation.FoundationContract", true) },
            { "System.Numerics.Plane", new MappedType("Windows.Foundation.Numerics", "Plane", "Windows.Foundation.FoundationContract", true) },
            { "System.Numerics.Quaternion", new MappedType("Windows.Foundation.Numerics", "Quaternion", "Windows.Foundation.FoundationContract", true) },
            { "System.Numerics.Vector2", new MappedType("Windows.Foundation.Numerics", "Vector2", "Windows.Foundation.FoundationContract", true) },
            { "System.Numerics.Vector3", new MappedType("Windows.Foundation.Numerics", "Vector3", "Windows.Foundation.FoundationContract", true) },
            { "System.Numerics.Vector4", new MappedType("Windows.Foundation.Numerics", "Vector4", "Windows.Foundation.FoundationContract", true) },
            { "System.Type", new MappedType(GetSystemTypeCustomMapping) },
            { "System.Collections.Generic.IEnumerable`1", new MappedType("Windows.Foundation.Collections", "IIterable`1", "Windows.Foundation.FoundationContract") },
            { "System.Collections.Generic.IEnumerator`1", new MappedType("Windows.Foundation.Collections", "IIterator`1", "Windows.Foundation.FoundationContract") },
            { "System.Collections.Generic.KeyValuePair`2", new MappedType("Windows.Foundation.Collections", "IKeyValuePair`2", "Windows.Foundation.FoundationContract") },
            { "System.Collections.Generic.IReadOnlyDictionary`2", new MappedType("Windows.Foundation.Collections", "IMapView`2", "Windows.Foundation.FoundationContract") },
            { "System.Collections.Generic.IDictionary`2", new MappedType("Windows.Foundation.Collections", "IMap`2", "Windows.Foundation.FoundationContract") },
            { "System.Collections.Generic.IReadOnlyList`1", new MappedType("Windows.Foundation.Collections", "IVectorView`1", "Windows.Foundation.FoundationContract") },
            { "System.Collections.Generic.IList`1", new MappedType("Windows.Foundation.Collections", "IVector`1", "Windows.Foundation.FoundationContract") },
            { "Windows.UI.Color", new MappedType("Windows.UI", "Color", "Windows.Foundation.UniversalApiContract", true) },
        };
    }
}
