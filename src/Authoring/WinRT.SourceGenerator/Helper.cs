﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

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

        public static string GetCsWinRTExeTFM(this GeneratorExecutionContext context)
        {
            context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.CsWinRTExeTFM", out var csWinRTExeTFM);
            return csWinRTExeTFM;
        }

        public static bool IsCsWinRTComponent(this GeneratorExecutionContext context)
        {
            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.CsWinRTComponent", out var isCsWinRTComponentStr))
            {
                return bool.TryParse(isCsWinRTComponentStr, out var isCsWinRTComponent) && isCsWinRTComponent;
            }

            return false;
        }

        public static bool IsCsWinRTComponent(this AnalyzerConfigOptionsProvider provider)
        {
            if (provider.GlobalOptions.TryGetValue("build_property.CsWinRTComponent", out var isCsWinRTComponentStr))
            {
                return bool.TryParse(isCsWinRTComponentStr, out var isCsWinRTComponent) && isCsWinRTComponent;
            }
            
            return false;
        }

        public static bool IsCsWinRTAotOptimizerEnabled(this AnalyzerConfigOptionsProvider provider)
        {
            if (provider.GlobalOptions.TryGetValue("build_property.CsWinRTAotOptimizerEnabled", out var isCsWinRTAotOptimizerEnabledStr))
            {
                return bool.TryParse(isCsWinRTAotOptimizerEnabledStr, out var isCsWinRTAotOptimizerEnabled) && isCsWinRTAotOptimizerEnabled;
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

        /// <summary>
        /// Gets whether the <c>"CsWinRTAotExportsEnabled"</c> MSBuild property is defined.
        /// </summary>
        /// <param name="context">The input <see cref="GeneratorExecutionContext"/> value to use.</param>
        /// <returns>Whether the <c>"CsWinRTAotExportsEnabled"</c> MSBuild property is defined.</returns>
        public static bool ShouldGenerateWinRTNativeExports(this GeneratorExecutionContext context)
        {
            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.CsWinRTAotExportsEnabled", out var isCsWinRTAotExportsEnabledStr))
            {
                return bool.TryParse(isCsWinRTAotExportsEnabledStr, out var isCsWinRTAotExportsEnabled) && isCsWinRTAotExportsEnabled;
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
            return IsWinRTType(type, null);
        }

        public static bool IsWinRTType(ISymbol type, Func<ISymbol, bool> isAuthoringWinRTType)
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
                isProjectedType = namedType.TypeArguments.All(t => 
                    IsWinRTType(t, isAuthoringWinRTType) || 
                    (isAuthoringWinRTType != null && isAuthoringWinRTType(t)));
            }

            return isProjectedType;
        }

        // Checks if the interface references any internal types (either the interface itself or within its generic types).
        public static bool IsInternalInterfaceFromReferences(INamedTypeSymbol iface, IAssemblySymbol currentAssembly)
        {
            return (iface.DeclaredAccessibility == Accessibility.Internal && !SymbolEqualityComparer.Default.Equals(iface.ContainingAssembly, currentAssembly)) ||
                (iface.IsGenericType && iface.TypeArguments.Any(typeArgument => IsInternalInterfaceFromReferences(typeArgument as INamedTypeSymbol, currentAssembly)));
        }

        // Checks whether the symbol references any generic that hasn't been instantiated
        // and is used by a WinRT interface. For instance, List<T> where T is a generic.
        // If the generic isn't used by any WinRT interface, this returns false as for
        // instance, we can still generate the vtable attribute for it.
        public static bool HasNonInstantiatedWinRTGeneric(ITypeSymbol symbol)
        {
            return symbol is INamedTypeSymbol namedType && 
                (IsArgumentTypeParameter(namedType) || 
                 (namedType.TypeArguments.Any(IsArgumentTypeParameter) && 
                  namedType.AllInterfaces.Any(iface => iface.TypeArguments.Any(IsArgumentTypeParameter) && 
                  // Checks if without the non-instantiated generic, whether it would be a WinRT type.
                  IsWinRTType(iface.OriginalDefinition, null))));

            static bool IsArgumentTypeParameter(ITypeSymbol argument)
            {
                return argument.TypeKind == TypeKind.TypeParameter;
            }
        }

        public static bool HasPrivateclass(ITypeSymbol symbol)
        {
            return symbol is INamedTypeSymbol namedType &&
                (namedType.DeclaredAccessibility == Accessibility.Private ||
                 namedType.TypeArguments.Any(argument => argument.DeclaredAccessibility == Accessibility.Private));
        }

        public static bool HasWinRTExposedTypeAttribute(ISymbol type)
        {
            return type.GetAttributes().
                Any(attribute => string.CompareOrdinal(attribute.AttributeClass.Name, "WinRTExposedTypeAttribute") == 0);
        }

        public static bool IsWinRTType(MemberDeclarationSyntax node)
        {
            bool isProjectedType = node.AttributeLists.SelectMany(list => list.Attributes).
                Any(attribute => string.CompareOrdinal(attribute.Name.NormalizeWhitespace().ToFullString(), "WindowsRuntimeTypeAttribute") == 0);
            return isProjectedType;
        }

        private static string GetAbiTypeForFundamentalType(ISymbol type)
        {
            if (type is INamedTypeSymbol namedTypeSymbol)
            {
                switch (namedTypeSymbol.SpecialType)
                {
                    case SpecialType.System_Boolean:
                        return "byte";
                    case SpecialType.System_String:
                        return "IntPtr";
                    case SpecialType.System_Char:
                        return "ushort";
                    case SpecialType.System_Object:
                        return "IntPtr";
                    case SpecialType.System_Single:
                    case SpecialType.System_Double:
                    case SpecialType.System_UInt16:
                    case SpecialType.System_UInt32:
                    case SpecialType.System_UInt64:
                    case SpecialType.System_Int16:
                    case SpecialType.System_Int32:
                    case SpecialType.System_Int64:
                    case SpecialType.System_Byte:
                        return type.ToDisplayString();
                }
            }

            return type.ToDisplayString();
        }

        public static bool IsBlittableValueType(ITypeSymbol type)
        {
            if (!type.IsValueType)
            {
                return false;
            }

            if (type.SpecialType != SpecialType.None)
            {
                switch (type.SpecialType)
                {
                    case SpecialType.System_Single:
                    case SpecialType.System_Double:
                    case SpecialType.System_UInt16:
                    case SpecialType.System_UInt32:
                    case SpecialType.System_UInt64:
                    case SpecialType.System_Int16:
                    case SpecialType.System_Int32:
                    case SpecialType.System_Int64:
                    case SpecialType.System_Byte:
                    case SpecialType.System_SByte:
                    case SpecialType.System_IntPtr:
                    case SpecialType.System_UIntPtr:
                        return true;
                    default:
                        return false;
                }
            }

            string customTypeMapKey = string.Join(".", type.ContainingNamespace.ToDisplayString(), type.MetadataName);
            if (MappedCSharpTypes.ContainsKey(customTypeMapKey))
            {
                return MappedCSharpTypes[customTypeMapKey].IsBlittable();
            }

            if (type.TypeKind == TypeKind.Enum)
            {
                return true;
            }
            
            if (type.TypeKind == TypeKind.Struct)
            {
                foreach (var typeMember in type.GetMembers())
                {
                    if (typeMember is IFieldSymbol field && !IsBlittableValueType(field.Type))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public static string GetAbiType(ITypeSymbol type)
        {
            if (IsFundamentalType(type))
            {
                return GetAbiTypeForFundamentalType(type);
            }

            var typeStr = type.ToDisplayString();
            if (typeStr == "System.Type")
            {
                return "ABI.System.Type";
            }
            else if (typeStr.StartsWith("System.Collections.Generic.KeyValuePair<"))
            {
                return "IntPtr";
            }

            if (type.IsValueType)
            {
                string customTypeMapKey = string.Join(".", type.ContainingNamespace.ToDisplayString(), type.MetadataName);
                if (MappedCSharpTypes.ContainsKey(customTypeMapKey))
                {
                    string prefix = MappedCSharpTypes[customTypeMapKey].IsBlittable() ? "" : "ABI.";
                    return prefix + typeStr;
                }

                var winrtHelperAttribute = type.GetAttributes().
                    Where(attribute => string.CompareOrdinal(attribute.AttributeClass.Name, "WindowsRuntimeHelperTypeAttribute") == 0).
                    FirstOrDefault();
                if (winrtHelperAttribute != null && 
                    winrtHelperAttribute.ConstructorArguments.Any())
                {
                    return winrtHelperAttribute.ConstructorArguments[0].Value.ToString();
                }
                // Handling authoring scenario where Impl type has the attributes and
                // if the current component is the one being authored, it may not be
                // generated yet to check given it is the same compilation.
                else if (!IsBlittableValueType(type))
                {
                    return "ABI." + typeStr;
                }
                else
                {
                    return typeStr;
                }
            }

            return "IntPtr";
        }

        public static string GetMarshalerClass(string type, string abiType, TypeKind kind, bool isArray, bool useGenericMarshaler = false)
        {
            if (type == "System.String" || type == "string")
            {
                return "MarshalString";
            }
            else if (type == "System.Type" || type == "Type")
            {
                if (isArray)
                {
                    return "MarshalNonBlittable<global::System.Type>";
                }
                else
                {
                    return "global::ABI.System.Type";
                }
            }
            else if (type == "System.Object" || type == "object")
            {
                return "MarshalInspectable<object>";
            }
            else if (type.StartsWith("System.Collections.Generic.KeyValuePair<"))
            {
                return $$"""MarshalInterface<{{type}}>""";
            }
            else if (kind == TypeKind.Enum)
            {
                if (isArray)
                {
                    return $$"""MarshalBlittable<{{type}}>""";
                }
                else
                {
                    return "";
                }
            }
            else if (kind == TypeKind.Struct)
            {
                if (type == abiType)
                {
                    if (isArray)
                    {
                        return $$"""MarshalBlittable<{{type}}>""";
                    }
                    else
                    {
                        return "";
                    }
                }
                else
                {
                    return "global::ABI." + type;
                }
            }
            else if (kind == TypeKind.Interface)
            {
                return $$"""MarshalInterface<{{type}}>""";
            }
            else if (kind == TypeKind.Class || kind == TypeKind.Delegate)
            {
                return useGenericMarshaler ? "MarshalInspectable<object>" : "global::ABI." + type;
            }

            throw new ArgumentException();
        }

        public static string GetFromAbiMarshaler(GenericParameter genericParameter)
        {
            return GetFromAbiMarshaler(
                genericParameter.ProjectedType, 
                genericParameter.AbiType, 
                genericParameter.TypeKind);
        }

        public static string GetFromAbiMarshaler(string type, string abiType, TypeKind kind)
        {
            string marshalerType = GetMarshalerClass(type, abiType, kind, false);
            if (kind == TypeKind.Enum || (kind == TypeKind.Struct && type == abiType))
            {
                return "";
            }
            else if (type == "bool")
            {
                return "(bool)";
            }
            else if (type == "char")
            {
                return "(char)";
            }
            else
            {
                return marshalerType + ".FromAbi";
            }
        }

        public static string GetFromManagedMarshaler(GenericParameter genericParameter)
        {
            return GetFromManagedMarshaler(
                genericParameter.ProjectedType,
                genericParameter.AbiType,
                genericParameter.TypeKind);
        }

        public static string GetFromManagedMarshaler(string type, string abiType, TypeKind kind)
        {
            string marshalerType = GetMarshalerClass(type, abiType, kind, false);
            if (kind == TypeKind.Enum || (kind == TypeKind.Struct && type == abiType))
            {
                return "";
            }
            else if (type == "bool")
            {
                return "(byte)";
            }
            else if (type == "char")
            {
                return "(ushort)";
            }
            else
            {
                return marshalerType + ".FromManaged";
            }
        }

        public static string GetCopyManagedArrayMarshaler(string type, string abiType, TypeKind kind)
        {
            if (kind == TypeKind.Class || kind == TypeKind.Delegate)
            {
                // TODO: Classes and delegates are missing CopyManagedArray.
                return $$"""Marshaler<{{type}}>""";
            }
            else
            {
                return GetMarshalerClass(type, abiType, kind, true);
            }
        }

        public static string GetCreateMarshaler(GenericParameter genericParameter, string arg)
        {
            return GetCreateMarshaler(
                genericParameter.ProjectedType,
                genericParameter.AbiType,
                genericParameter.TypeKind,
                arg);
        }

        public static string GetCreateMarshaler(string type, string abiType, TypeKind kind, string arg)
        {
            if (kind == TypeKind.Enum || (kind == TypeKind.Struct && type == abiType) || 
                type == "bool" || 
                type == "char")
            {
                return "";
            }
            else if (type == "System.String" || type == "string")
            {
                // TODO: Consider switching to pinning
                return $$"""__{{arg}} = MarshalString.CreateMarshaler({{arg}});""";
            }
            else
            {
                string marshalerClass = GetMarshalerClass(type, abiType, kind, false);
                return $$"""__{{arg}} = {{marshalerClass}}.CreateMarshaler2({{arg}});""";
            }
        }

        public static string GetDisposeMarshaler(GenericParameter genericParameter, string arg)
        {
            return GetDisposeMarshaler(
                genericParameter.ProjectedType,
                genericParameter.AbiType,
                genericParameter.TypeKind,
                arg);
        }

        public static string GetDisposeMarshaler(string type, string abiType, TypeKind kind, string arg)
        {
            if (kind == TypeKind.Enum || (kind == TypeKind.Struct && type == abiType) ||
                type == "bool" ||
                type == "char")
            {
                return "";
            }
            else
            {
                string marshalerClass = GetMarshalerClass(type, abiType, kind, false, true);
                return $$"""{{marshalerClass}}.DisposeMarshaler(__{{arg}});""";
            }
        }

        public static string GetAbiFromMarshaler(GenericParameter genericParameter, string arg)
        {
            return GetAbiFromMarshaler(
                genericParameter.ProjectedType,
                genericParameter.AbiType,
                genericParameter.TypeKind,
                arg);
        }

        public static string GetAbiFromMarshaler(string type, string abiType, TypeKind kind, string arg)
        {
            if (kind == TypeKind.Enum || (kind == TypeKind.Struct && type == abiType))
            {
                return arg;
            }
            else if (type == "bool")
            {
                return $"(byte){arg}";
            }
            else if (type == "char")
            {
                return $"(ushort){arg}";
            }
            else
            {
                string marshalerClass = GetMarshalerClass(type, abiType, kind, false, true);
                return $"{marshalerClass}.GetAbi(__{arg})";
            }
        }

        public static string GetMarshalerDeclaration(GenericParameter genericParameter, string arg)
        {
            return GetMarshalerDeclaration(
                genericParameter.ProjectedType,
                genericParameter.AbiType,
                genericParameter.TypeKind,
                arg);
        }

        public static string GetMarshalerDeclaration(string type, string abiType, TypeKind kind, string arg)
        {
            if (kind == TypeKind.Enum || (kind == TypeKind.Struct && type == abiType) ||
                type == "bool" ||
                type == "char")
            {
                return "";
            }
            else
            {
                return $"{GetAbiMarshalerType(type, abiType, kind, false)} __{arg} = default;";
            }
        }

        public static string GetAbiMarshalerType(string type, string abiType, TypeKind kind, bool isArray)
        {
            if (type == "System.String" || type == "string")
            {
                return isArray ? "MarshalString.MarshalerArray" : "MarshalString";
            }
            else if (type == "System.Type" || type == "Type")
            {
                if (isArray)
                {
                    return "MarshalNonBlittable<global::System.Type>.MarshalerArray";
                }
                else
                {
                    return "global::ABI.System.Type.Marshaler";
                }
            }
            else if (type.StartsWith("System.Collections.Generic.KeyValuePair<"))
            {
                return isArray ? $$"""MarshalInterfaceHelper<{{type}}>.MarshalerArray""" : "ObjectReferenceValue";
            }
            else if (kind == TypeKind.Enum)
            {
                return isArray ? $$"""MarshalBlittable<{{type}}>.MarshalerArray""" : type;
            }
            else if (kind == TypeKind.Struct)
            {
                if (type == abiType)
                {
                    return isArray ? $$"""MarshalBlittable<{{type}}>.MarshalerArray""" : type;
                }
                else
                {
                    return isArray ? $$"""MarshalNonBlittable<{{type}}>.MarshalerArray""" : "ABI." + type;
                }
            }
            else if (type == "System.Object" || type == "object" || kind == TypeKind.Class || kind == TypeKind.Interface || kind == TypeKind.Delegate)
            {
                return isArray ? $$"""MarshalInterfaceHelper<{{type}}>.MarshalerArray""" : "ObjectReferenceValue";
            }

            throw new ArgumentException();
        }
        public static string EscapeTypeNameForIdentifier(string typeName)
        {
            return Regex.Replace(typeName, """[(\ |:<>,\.)]""", "_");
        }

        public readonly struct MappedType
        {
            private readonly string @namespace;
            private readonly string name;
            private readonly string assembly;
            private readonly bool isSystemType;
            private readonly bool isValueType;
            private readonly bool isBlittable;
            private readonly Func<ISymbol, (string, string, string, bool, bool)> multipleMappingFunc;

            public MappedType(string @namespace, string name, string assembly, bool isValueType = false, bool isBlittable = false)
            {
                this.@namespace = @namespace;
                this.name = name;
                this.assembly = assembly;
                isSystemType = string.CompareOrdinal(this.assembly, "mscorlib") == 0;
                this.isValueType = isValueType;
                this.isBlittable = isBlittable;
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

            public bool IsBlittable()
            {
                return isValueType && isBlittable;
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
            { "System.DateTimeOffset", new MappedType("Windows.Foundation", "DateTime", "Windows.Foundation.FoundationContract", true, false) },
            { "System.Exception", new MappedType("Windows.Foundation", "HResult", "Windows.Foundation.FoundationContract", true, false) },
            { "System.EventHandler`1", new MappedType("Windows.Foundation", "EventHandler`1", "Windows.Foundation.FoundationContract") },
            { "System.FlagsAttribute", new MappedType("System", "FlagsAttribute", "mscorlib" ) },
            { "System.IDisposable", new MappedType("Windows.Foundation", "IClosable", "Windows.Foundation.FoundationContract") },
            { "System.IServiceProvider", new MappedType("Microsoft.UI.Xaml", "IXamlServiceProvider", "Microsoft.UI") },
            { "System.Nullable`1", new MappedType("Windows.Foundation", "IReference`1", "Windows.Foundation.FoundationContract" ) },
            { "System.Object", new MappedType("System", "Object", "mscorlib" ) },
            { "System.TimeSpan", new MappedType("Windows.Foundation", "TimeSpan", "Windows.Foundation.FoundationContract", true, false) },
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
            { "WinRT.EventRegistrationToken", new MappedType("Windows.Foundation", "EventRegistrationToken", "Windows.Foundation.FoundationContract", true, true) },
            { "System.AttributeTargets", new MappedType("Windows.Foundation.Metadata", "AttributeTargets", "Windows.Foundation.FoundationContract", true, true) },
            { "System.AttributeUsageAttribute", new MappedType("Windows.Foundation.Metadata", "AttributeUsageAttribute", "Windows.Foundation.FoundationContract") },
            { "System.Numerics.Matrix3x2", new MappedType("Windows.Foundation.Numerics", "Matrix3x2", "Windows.Foundation.FoundationContract", true, true) },
            { "System.Numerics.Matrix4x4", new MappedType("Windows.Foundation.Numerics", "Matrix4x4", "Windows.Foundation.FoundationContract", true, true) },
            { "System.Numerics.Plane", new MappedType("Windows.Foundation.Numerics", "Plane", "Windows.Foundation.FoundationContract", true, true) },
            { "System.Numerics.Quaternion", new MappedType("Windows.Foundation.Numerics", "Quaternion", "Windows.Foundation.FoundationContract", true, true) },
            { "System.Numerics.Vector2", new MappedType("Windows.Foundation.Numerics", "Vector2", "Windows.Foundation.FoundationContract", true, true) },
            { "System.Numerics.Vector3", new MappedType("Windows.Foundation.Numerics", "Vector3", "Windows.Foundation.FoundationContract", true, true) },
            { "System.Numerics.Vector4", new MappedType("Windows.Foundation.Numerics", "Vector4", "Windows.Foundation.FoundationContract", true, true) },
            { "System.Type", new MappedType(GetSystemTypeCustomMapping) },
            { "System.Collections.Generic.IEnumerable`1", new MappedType("Windows.Foundation.Collections", "IIterable`1", "Windows.Foundation.FoundationContract") },
            { "System.Collections.Generic.IEnumerator`1", new MappedType("Windows.Foundation.Collections", "IIterator`1", "Windows.Foundation.FoundationContract") },
            { "System.Collections.Generic.KeyValuePair`2", new MappedType("Windows.Foundation.Collections", "IKeyValuePair`2", "Windows.Foundation.FoundationContract") },
            { "System.Collections.Generic.IReadOnlyDictionary`2", new MappedType("Windows.Foundation.Collections", "IMapView`2", "Windows.Foundation.FoundationContract") },
            { "System.Collections.Generic.IDictionary`2", new MappedType("Windows.Foundation.Collections", "IMap`2", "Windows.Foundation.FoundationContract") },
            { "System.Collections.Generic.IReadOnlyList`1", new MappedType("Windows.Foundation.Collections", "IVectorView`1", "Windows.Foundation.FoundationContract") },
            { "System.Collections.Generic.IList`1", new MappedType("Windows.Foundation.Collections", "IVector`1", "Windows.Foundation.FoundationContract") },
            { "Windows.UI.Color", new MappedType("Windows.UI", "Color", "Windows.Foundation.UniversalApiContract", true, true) },
        };
    }
}
