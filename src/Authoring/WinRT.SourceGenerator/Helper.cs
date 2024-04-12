// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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

        public static bool GetCsWinRTRcwFactoryFallbackGeneratorForceOptIn(this AnalyzerConfigOptionsProvider provider)
        {
            if (provider.GlobalOptions.TryGetValue("build_property.CsWinRTRcwFactoryFallbackGeneratorForceOptIn", out var csWinRTRcwFactoryFallbackGeneratorForceOptIn))
            {
                return bool.TryParse(csWinRTRcwFactoryFallbackGeneratorForceOptIn, out var isCsWinRTRcwFactoryFallbackGeneratorForceOptIn) && isCsWinRTRcwFactoryFallbackGeneratorForceOptIn;
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

        public static bool IsWinRTType(ISymbol type, TypeMapper mapper)
        {
            return IsWinRTType(type, mapper, null);
        }

        public static bool IsWinRTType(ISymbol type, Func<ISymbol, TypeMapper, bool> isAuthoringWinRTType, TypeMapper mapper)
        {
            bool isProjectedType = type.GetAttributes().
                Any(attribute => string.CompareOrdinal(attribute.AttributeClass.Name, "WindowsRuntimeTypeAttribute") == 0) ||
                IsFundamentalType(type);

            if (!isProjectedType & type.ContainingNamespace != null)
            {
                isProjectedType = mapper.HasMappingForType(string.Join(".", type.ContainingNamespace.ToDisplayString(), type.MetadataName));
            }

            // Ensure all generic parameters are WinRT types.
            if (isProjectedType && type is INamedTypeSymbol namedType && namedType.IsGenericType && !namedType.IsDefinition)
            {
                isProjectedType = namedType.TypeArguments.All(t => 
                    IsWinRTType(t, mapper, isAuthoringWinRTType) || 
                    (isAuthoringWinRTType != null && isAuthoringWinRTType(t)));
            }

            return isProjectedType;
        }

        public static bool IsWinRTType(ISymbol type, ITypeSymbol winrtRuntimeTypeAttribute, bool isComponentProject, IAssemblySymbol currentAssembly)
        {
            if (IsFundamentalType(type))
            {
                return true;
            }

            if (isComponentProject &&
                // Make sure type is in component project.
                SymbolEqualityComparer.Default.Equals(type.ContainingAssembly, currentAssembly) &&
                type.DeclaredAccessibility == Accessibility.Public)
            {
                // Authoring diagnostics will make sure all public types are valid WinRT types.
                return true;
            }

            bool isProjectedType = HasAttributeWithType(type, winrtRuntimeTypeAttribute);
            if (!isProjectedType & type.ContainingNamespace != null)
            {
                isProjectedType = MappedCSharpTypes.ContainsKey(string.Join(".", type.ContainingNamespace.ToDisplayString(), type.MetadataName));
            }

            // Ensure all generic parameters are WinRT types.
            if (isProjectedType && 
                type is INamedTypeSymbol namedType && 
                namedType.IsGenericType && 
                !namedType.IsDefinition)
            {
                isProjectedType = namedType.TypeArguments.All(t => IsWinRTType(t, winrtRuntimeTypeAttribute, isComponentProject, currentAssembly));
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

        public static bool IsPartial(INamedTypeSymbol symbol)
        {
            return symbol.DeclaringSyntaxReferences.Any(syntax => syntax.GetSyntax() is BaseTypeDeclarationSyntax declaration && declaration.Modifiers.Any(SyntaxKind.PartialKeyword));
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
                Any(attribute => string.CompareOrdinal(attribute.Name.NormalizeWhitespace().ToFullString(), "global::WinRT.WindowsRuntimeType") == 0);
            return isProjectedType;
        }

        /// <summary>
        /// Checks whether or not a given symbol has an attribute with the specified type.
        /// </summary>
        /// <param name="symbol">The input <see cref="ISymbol"/> instance to check.</param>
        /// <param name="attributeTypeSymbol">The <see cref="ITypeSymbol"/> instance for the attribute type to look for.</param>
        /// <returns>Whether or not <paramref name="symbol"/> has an attribute with the specified type.</returns>
        public static bool HasAttributeWithType(ISymbol symbol, ITypeSymbol attributeTypeSymbol)
        {
            foreach (AttributeData attribute in symbol.GetAttributes())
            {
                if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeTypeSymbol))
                {
                    return true;
                }
            }

            return false;
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

        public static bool IsBlittableValueType(ITypeSymbol type, TypeMapper typeMapper)
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
            if (typeMapper.HasMappingForType(customTypeMapKey))
            {
                return typeMapper.GetMappedType(customTypeMapKey).IsBlittable();
            }

            if (type.TypeKind == TypeKind.Enum)
            {
                return true;
            }
            
            if (type.TypeKind == TypeKind.Struct)
            {
                foreach (var typeMember in type.GetMembers())
                {
                    if (typeMember is IFieldSymbol field && !IsBlittableValueType(field.Type, typeMapper))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public static string GetAbiType(ITypeSymbol type, TypeMapper mapper)
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
                if (mapper.HasMappingForType(customTypeMapKey))
                {
                    string prefix = mapper.GetMappedType(customTypeMapKey).IsBlittable() ? "" : "ABI.";
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
                else if (!IsBlittableValueType(type, mapper))
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
    }
}
