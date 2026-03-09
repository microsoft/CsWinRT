// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.ProjectionGenerator.Helpers;
using WindowsRuntime.ProjectionGenerator.Models;

namespace WindowsRuntime.ProjectionGenerator.Generation;

/// <summary>
/// Writes the projected (public-facing) C# types that correspond to WinRT types.
/// This is a port of the <c>write_enum</c>, <c>write_struct</c>, <c>write_delegate</c>,
/// <c>write_interface</c>, <c>write_class</c>, <c>write_attribute</c>, and <c>write_contract</c>
/// functions from the C++ cswinrt <c>code_writers.h</c>.
/// </summary>
internal static class ProjectedTypeWriter
{
    /// <summary>
    /// Writes assembly-level attributes for type map groups.
    /// Maps to <c>write_winrt_comwrappers_typemapgroup_assembly_attribute</c>,
    /// <c>write_winrt_windowsmetadata_typemapgroup_assembly_attribute</c>, and
    /// <c>write_winrt_idic_typemapgroup_assembly_attribute</c> in the C++ cswinrt.
    /// </summary>
    /// <param name="writer">The writer to output to.</param>
    /// <param name="type">The type definition to process.</param>
    /// <param name="category">The category of the type.</param>
    /// <param name="args">The projection generator arguments.</param>
    public static void WriteAssemblyAttributes(CodeWriter writer, TypeDefinition type, TypeCategory category, ProjectionGeneratorArgs args)
    {
        _ = writer;
        _ = type;
        _ = category;
        _ = args;

        // Assembly attributes are emitted based on type category.
        // These map to WinRTComWrappersTypeMapGroup, WinRTWindowsMetadataTypeMapGroup, and WinRTIDICTypeMapGroup.
        // The actual attribute emission requires runtime types that will be implemented in a subsequent pass.
    }

    /// <summary>
    /// Generates a C# enum from a WinRT enum type definition.
    /// Port of <c>write_enum</c> from the C++ cswinrt <c>code_writers.h</c>.
    /// </summary>
    /// <param name="writer">The writer to output to.</param>
    /// <param name="type">The enum type definition.</param>
    /// <param name="args">The projection generator arguments.</param>
    public static void WriteEnum(CodeWriter writer, TypeDefinition type, ProjectionGeneratorArgs args)
    {
        string access = GetAccessModifier(type, args);
        string name = TypeNameHelpers.GetSimpleName(type);
        bool isFlags = TypeHelpers.IsFlagsEnum(type);

        // Determine the underlying type from the special value__ field
        string underlyingType = "int";

        foreach (FieldDefinition field in type.Fields)
        {
            if (field.IsRuntimeSpecialName && field.Name == "value__")
            {
                string? fullTypeName = field.Signature?.FieldType?.FullName;
                underlyingType = fullTypeName == "System.UInt32" ? "uint" : "int";

                break;
            }
        }

        writer.WriteLine();

        if (isFlags)
        {
            writer.WriteLine("[global::System.FlagsAttribute]");
        }

        writer.WriteLine($"{access} enum {name} : {underlyingType}");

        using (writer.WriteBlock())
        {
            bool isFirst = true;

            foreach (FieldDefinition field in type.Fields)
            {
                // Skip the special value__ field
                if (field.IsRuntimeSpecialName || field.IsSpecialName)
                {
                    continue;
                }

                // Only emit fields with constant values (enum members)
                if (field.Constant is not { } constant)
                {
                    continue;
                }

                if (!isFirst)
                {
                    // Separate enum members for readability (no extra blank line needed for enums)
                }

                isFirst = false;

                string fieldName = CSharpKeywords.EscapeIdentifier(field.Name?.Value ?? "");
                object? value = constant.InterpretData();

                writer.WriteLine($"{fieldName} = {FormatConstantValue(value, underlyingType)},");
            }
        }
    }

    /// <summary>
    /// Generates a C# struct from a WinRT struct type definition.
    /// Port of <c>write_struct</c> from the C++ cswinrt <c>code_writers.h</c>.
    /// </summary>
    /// <param name="writer">The writer to output to.</param>
    /// <param name="type">The struct type definition.</param>
    /// <param name="args">The projection generator arguments.</param>
    public static void WriteStruct(CodeWriter writer, TypeDefinition type, ProjectionGeneratorArgs args)
    {
        string access = GetAccessModifier(type, args);
        string name = TypeNameHelpers.GetSimpleName(type);

        writer.WriteLine();
        writer.WriteLine($"{access} struct {name}");

        using (writer.WriteBlock())
        {
            foreach (FieldDefinition field in type.Fields)
            {
                // Skip static and special fields
                if (field.IsStatic || field.IsSpecialName)
                {
                    continue;
                }

                string fieldName = CSharpKeywords.EscapeIdentifier(field.Name?.Value ?? "");
                string fieldTypeName = GetProjectedTypeName(field.Signature?.FieldType);

                writer.WriteLine($"public {fieldTypeName} {fieldName};");
            }
        }
    }

    /// <summary>
    /// Generates a C# delegate from a WinRT delegate type definition.
    /// Port of <c>write_delegate</c> from the C++ cswinrt <c>code_writers.h</c>.
    /// </summary>
    /// <param name="writer">The writer to output to.</param>
    /// <param name="type">The delegate type definition.</param>
    /// <param name="args">The projection generator arguments.</param>
    public static void WriteDelegate(CodeWriter writer, TypeDefinition type, ProjectionGeneratorArgs args)
    {
        string access = GetAccessModifier(type, args);
        string name = TypeNameHelpers.GetSimpleName(type);

        // Get the Invoke method to determine the delegate signature
        MethodDefinition? invokeMethod = TypeHelpers.GetDelegateInvoke(type);

        if (invokeMethod?.Signature is not { } methodSig)
        {
            return;
        }

        writer.WriteLine();

        // Write the GUID attribute
        string? guid = FormatGuid(type);

        if (guid is not null)
        {
            writer.WriteLine($"[global::System.Runtime.InteropServices.Guid(\"{guid}\")]");
        }

        // Build the parameter list
        string returnType = GetProjectedTypeName(methodSig.ReturnType);
        string parameters = BuildParameterList(invokeMethod);

        writer.WriteLine($"{access} delegate {returnType} {name}({parameters});");
    }

    /// <summary>
    /// Generates a C# interface from a WinRT interface type definition.
    /// Port of <c>write_interface</c> from the C++ cswinrt <c>code_writers.h</c>.
    /// </summary>
    /// <param name="writer">The writer to output to.</param>
    /// <param name="type">The interface type definition.</param>
    /// <param name="args">The projection generator arguments.</param>
    public static void WriteInterface(CodeWriter writer, TypeDefinition type, ProjectionGeneratorArgs args)
    {
        string name = TypeNameHelpers.GetSimpleName(type);
        bool isExclusiveTo = TypeHelpers.IsExclusiveTo(type);
        string access = GetInterfaceAccessModifier(type, args, isExclusiveTo);

        writer.WriteLine();

        // Write the GUID attribute
        string? guid = FormatGuid(type);

        if (guid is not null)
        {
            writer.WriteLine($"[global::System.Runtime.InteropServices.Guid(\"{guid}\")]");
        }

        // Build the interface declaration with base interfaces
        string baseInterfaces = BuildBaseInterfaceList(type);
        string declaration = string.IsNullOrEmpty(baseInterfaces)
            ? $"{access} interface {name}"
            : $"{access} interface {name} : {baseInterfaces}";

        writer.WriteLine(declaration);

        using (writer.WriteBlock())
        {
            // Collect property and event accessor names to skip them in method output
            HashSet<string> specialMethodNames = CollectSpecialMethodNames(type);

            // Write methods (skip property/event accessors)
            foreach (MethodDefinition method in type.Methods)
            {
                if (method.IsSpecialName || method.IsRuntimeSpecialName)
                {
                    continue;
                }

                string? methodName = method.Name?.Value;

                if (methodName is null || specialMethodNames.Contains(methodName))
                {
                    continue;
                }

                WriteMethodSignature(writer, method, isInterfaceMethod: true);
            }

            // Write properties
            foreach (PropertyDefinition property in type.Properties)
            {
                WritePropertyDeclaration(writer, property, isInterfaceProperty: true);
            }

            // Write events
            foreach (EventDefinition evt in type.Events)
            {
                WriteEventDeclaration(writer, evt, isInterfaceEvent: true);
            }
        }
    }

    /// <summary>
    /// Generates a C# class from a WinRT runtime class type definition.
    /// Port of <c>write_class</c> from the C++ cswinrt <c>code_writers.h</c>.
    /// </summary>
    /// <param name="writer">The writer to output to.</param>
    /// <param name="type">The class type definition.</param>
    /// <param name="args">The projection generator arguments.</param>
    public static void WriteClass(CodeWriter writer, TypeDefinition type, ProjectionGeneratorArgs args)
    {
        string access = GetAccessModifier(type, args);
        string name = TypeNameHelpers.GetSimpleName(type);
        bool isStatic = TypeHelpers.IsStaticClass(type);

        writer.WriteLine();

        // Build the type declaration
        string baseList = BuildClassBaseList(type);

        if (isStatic)
        {
            string declaration = $"{access} static class {name}";
            writer.WriteLine(declaration);

            using (writer.WriteBlock())
            {
                WriteStaticClassMembers(writer, type, args);
            }
        }
        else
        {
            string classModifiers = $"{access} sealed class";
            string declaration = string.IsNullOrEmpty(baseList)
                ? $"{classModifiers} {name}"
                : $"{classModifiers} {name} : {baseList}";

            writer.WriteLine(declaration);

            using (writer.WriteBlock())
            {
                WriteClassMembers(writer, type, args);
            }
        }
    }

    /// <summary>
    /// Generates a C# attribute class from a WinRT attribute type definition.
    /// Port of <c>write_attribute</c> from the C++ cswinrt <c>code_writers.h</c>.
    /// </summary>
    /// <param name="writer">The writer to output to.</param>
    /// <param name="type">The attribute type definition.</param>
    /// <param name="args">The projection generator arguments.</param>
    public static void WriteAttribute(CodeWriter writer, TypeDefinition type, ProjectionGeneratorArgs args)
    {
        string access = GetAccessModifier(type, args);
        string name = TypeNameHelpers.GetSimpleName(type);

        writer.WriteLine();

        // Write AttributeUsage - extract from the type's own AttributeUsageAttribute if present
        string targets = GetAttributeTargets(type);
        writer.WriteLine($"[global::System.AttributeUsage({targets}, AllowMultiple = true)]");

        writer.WriteLine($"{access} sealed class {name} : global::System.Attribute");

        using (writer.WriteBlock())
        {
            // Find constructors and emit them with their parameters
            foreach (MethodDefinition method in type.Methods)
            {
                if (method.Name?.Value != ".ctor" || method.IsStatic)
                {
                    continue;
                }

                if (method.Signature is not { } methodSig || methodSig.ParameterTypes.Count == 0)
                {
                    continue;
                }

                string parameters = BuildParameterList(method);
                writer.WriteLine($"public {name}({parameters})");

                using (writer.WriteBlock())
                {
                    // Assign parameters to auto-properties
                    for (int i = 0; i < methodSig.ParameterTypes.Count; i++)
                    {
                        string paramName = GetParameterName(method, i);
                        string propName = ToPascalCase(paramName);
                        writer.WriteLine($"this.{propName} = {CSharpKeywords.EscapeIdentifier(paramName)};");
                    }
                }
            }

            // Write auto-properties for each constructor parameter
            WriteAttributeProperties(writer, type);
        }
    }

    /// <summary>
    /// Generates a C# struct for a WinRT API contract type definition.
    /// Port of <c>write_contract</c> from the C++ cswinrt <c>code_writers.h</c>.
    /// </summary>
    /// <param name="writer">The writer to output to.</param>
    /// <param name="type">The API contract type definition.</param>
    /// <param name="args">The projection generator arguments.</param>
    public static void WriteContract(CodeWriter writer, TypeDefinition type, ProjectionGeneratorArgs args)
    {
        string access = GetAccessModifier(type, args);
        string name = TypeNameHelpers.GetSimpleName(type);

        writer.WriteLine();
        writer.WriteLine($"{access} struct {name}");

        using (writer.WriteBlock())
        {
        }
    }

    /// <summary>
    /// Gets the appropriate access modifier based on the type and generator arguments.
    /// </summary>
    private static string GetAccessModifier(TypeDefinition type, ProjectionGeneratorArgs args)
    {
        return args.IsInternal || TypeHelpers.IsProjectionInternal(type) ? "internal" : "public";
    }

    /// <summary>
    /// Gets the access modifier for an interface, considering exclusive-to attributes.
    /// </summary>
    private static string GetInterfaceAccessModifier(TypeDefinition type, ProjectionGeneratorArgs args, bool isExclusiveTo)
    {
        if (args.IsInternal || TypeHelpers.IsProjectionInternal(type))
        {
            return "internal";
        }

        // Exclusive-to interfaces are internal unless PublicExclusiveTo is set
        return isExclusiveTo && !args.PublicExclusiveTo ? "internal" : "public";
    }

    /// <summary>
    /// Gets the projected C# type name for a type signature, converting WinRT types to their .NET equivalents.
    /// </summary>
    private static string GetProjectedTypeName(TypeSignature? typeSig)
    {
        if (typeSig is null)
        {
            return "void";
        }

        // Handle CoreLib types (fundamental types like int, string, etc.)
        if (typeSig is CorLibTypeSignature corLibType)
        {
            string fullName = $"{corLibType.Namespace}.{corLibType.Name}";
            string? csharpName = TypeNameHelpers.GetCSharpTypeName(fullName);

            return csharpName ?? $"global::{fullName}";
        }

        // Handle type references/definitions
        if (typeSig is TypeDefOrRefSignature typeDefOrRef)
        {
            ITypeDefOrRef? referencedType = typeDefOrRef.Type;

            if (referencedType is null)
            {
                return typeSig.FullName ?? "object";
            }

            string? ns = referencedType.Namespace;
            string? typeName = referencedType.Name;

            if (ns is not null && typeName is not null)
            {
                // Check for C# keyword type name (e.g., System.String → string)
                string fullName = $"{ns}.{typeName}";
                string? csharpName = TypeNameHelpers.GetCSharpTypeName(fullName);

                if (csharpName is not null)
                {
                    return csharpName;
                }

                // Check for WinRT to .NET type mapping
                TypeMappings.MappedType? mapping = TypeMappings.GetMappedType(ns, typeName);

                if (mapping is { MappedNamespace: not null, MappedName: not null })
                {
                    string mappedName = RemoveGenericArity(mapping.Value.MappedName);

                    return $"global::{mapping.Value.MappedNamespace}.{mappedName}";
                }
            }

            return TypeNameHelpers.GetProjectedTypeName(referencedType);
        }

        // Handle generic instantiations (e.g., IVector<string> to IList<string>)
        if (typeSig is GenericInstanceTypeSignature genericInstance)
        {
            ITypeDefOrRef? genericType = genericInstance.GenericType;

            if (genericType is null)
            {
                return typeSig.FullName ?? "object";
            }

            string? ns = genericType.Namespace;
            string? typeName = genericType.Name;
            string baseName;

            if (ns is not null && typeName is not null)
            {
                TypeMappings.MappedType? mapping = TypeMappings.GetMappedType(ns, typeName);

                if (mapping is { MappedNamespace: not null, MappedName: not null })
                {
                    string mappedName = RemoveGenericArity(mapping.Value.MappedName);
                    baseName = $"global::{mapping.Value.MappedNamespace}.{mappedName}";
                }
                else
                {
                    string simpleName = RemoveGenericArity(typeName);
                    baseName = $"global::{ns}.{simpleName}";
                }
            }
            else
            {
                baseName = RemoveGenericArity(genericType.Name ?? "Unknown");
            }

            // Build generic arguments list
            List<string> typeArgs = [];

            foreach (TypeSignature typeArg in genericInstance.TypeArguments)
            {
                typeArgs.Add(GetProjectedTypeName(typeArg));
            }

            return $"{baseName}<{string.Join(", ", typeArgs)}>";
        }

        // Handle arrays
        if (typeSig is SzArrayTypeSignature szArray)
        {
            string elementType = GetProjectedTypeName(szArray.BaseType);

            return $"{elementType}[]";
        }

        // Handle by-reference types (ref/out parameters)
        if (typeSig is ByReferenceTypeSignature byRef)
        {
            return GetProjectedTypeName(byRef.BaseType);
        }

        // Handle custom modifier types (modopt/modreq) - just unwrap them
        if (typeSig is CustomModifierTypeSignature customMod)
        {
            return GetProjectedTypeName(customMod.BaseType);
        }

        // Handle generic parameter references (T, T0, T1, etc.)
        if (typeSig is GenericParameterSignature genericParam)
        {
            return genericParam.ParameterType == GenericParameterType.Type
                ? $"T{(genericParam.Index == 0 ? "" : genericParam.Index.ToString())}"
                : $"TMethod{genericParam.Index}";
        }

        // Fallback: try using FullName
        string? fallbackFullName = typeSig.FullName;

        if (fallbackFullName is not null)
        {
            string? csharpFallbackName = TypeNameHelpers.GetCSharpTypeName(fallbackFullName);

            return csharpFallbackName ?? $"global::{fallbackFullName}";
        }

        return "object";
    }

    /// <summary>
    /// Writes a method signature for interface or class members.
    /// </summary>
    private static void WriteMethodSignature(CodeWriter writer, MethodDefinition method, bool isInterfaceMethod = false)
    {
        if (method.Signature is not { } methodSig)
        {
            return;
        }

        string returnType = GetProjectedTypeName(methodSig.ReturnType);
        string methodName = CSharpKeywords.EscapeIdentifier(method.Name?.Value ?? "");
        string parameters = BuildParameterList(method);

        if (isInterfaceMethod)
        {
            writer.WriteLine($"{returnType} {methodName}({parameters});");
        }
        else
        {
            writer.WriteLine($"public {returnType} {methodName}({parameters})");

            using (writer.WriteBlock())
            {
                writer.WriteLine("throw new global::System.NotImplementedException();");
            }
        }
    }

    /// <summary>
    /// Writes a property declaration.
    /// </summary>
    private static void WritePropertyDeclaration(CodeWriter writer, PropertyDefinition property, bool isInterfaceProperty = false)
    {
        string propertyName = CSharpKeywords.EscapeIdentifier(property.Name?.Value ?? "");
        string propertyType = GetProjectedTypeName(property.Signature?.ReturnType);

        bool hasGetter = property.GetMethod is not null;
        bool hasSetter = property.SetMethod is not null;

        string accessors = hasGetter && hasSetter
            ? "{ get; set; }"
            : hasGetter ? "{ get; }" : "{ set; }";

        if (isInterfaceProperty)
        {
            writer.WriteLine($"{propertyType} {propertyName} {accessors}");
        }
        else
        {
            writer.WriteLine($"public {propertyType} {propertyName} {accessors}");
        }
    }

    /// <summary>
    /// Writes an event declaration.
    /// </summary>
    private static void WriteEventDeclaration(CodeWriter writer, EventDefinition evt, bool isInterfaceEvent = false)
    {
        string eventName = CSharpKeywords.EscapeIdentifier(evt.Name?.Value ?? "");
        string eventType = evt.EventType is { } eventTypeRef
            ? GetProjectedTypeNameFromTypeRef(eventTypeRef)
            : "global::System.EventHandler";

        if (isInterfaceEvent)
        {
            writer.WriteLine($"event {eventType} {eventName};");
        }
        else
        {
            writer.WriteLine($"public event {eventType} {eventName};");
        }
    }

    /// <summary>
    /// Gets the projected type name from an <see cref="ITypeDefOrRef"/> (used for event types).
    /// </summary>
    private static string GetProjectedTypeNameFromTypeRef(ITypeDefOrRef typeRef)
    {
        // Handle TypeSpecification (generic instances like IVector`1<string>)
        if (typeRef is TypeSpecification typeSpec && typeSpec.Signature is GenericInstanceTypeSignature genericInst)
        {
            return GetProjectedTypeName(genericInst);
        }

        string? ns = typeRef.Namespace;
        string? typeName = typeRef.Name;

        if (ns is not null && typeName is not null)
        {
            // Check for WinRT → .NET type mapping
            TypeMappings.MappedType? mapping = TypeMappings.GetMappedType(ns, typeName);

            if (mapping is { MappedNamespace: not null, MappedName: not null })
            {
                string mappedName = RemoveGenericArity(mapping.Value.MappedName);

                return $"global::{mapping.Value.MappedNamespace}.{mappedName}";
            }

            // Remove backtick suffix for non-mapped generic types
            string simpleName = RemoveGenericArity(typeName);

            return $"global::{ns}.{simpleName}";
        }

        return TypeNameHelpers.GetProjectedTypeName(typeRef);
    }

    /// <summary>
    /// Builds the parameter list string for a method.
    /// </summary>
    private static string BuildParameterList(MethodDefinition method)
    {
        if (method.Signature is not { } methodSig || methodSig.ParameterTypes.Count == 0)
        {
            return "";
        }

        List<string> parts = [];

        for (int i = 0; i < methodSig.ParameterTypes.Count; i++)
        {
            TypeSignature paramType = methodSig.ParameterTypes[i];
            string paramName = GetParameterName(method, i);
            string typeName;
            string prefix = "";

            // Handle by-reference parameters (out/ref)
            if (paramType is ByReferenceTypeSignature byRef)
            {
                typeName = GetProjectedTypeName(byRef.BaseType);

                // Determine if it's an out parameter
                ParameterDefinition? paramDef = GetParameterDefinition(method, i);

                prefix = paramDef?.IsOut == true ? "out " : "ref ";
            }
            else
            {
                typeName = GetProjectedTypeName(paramType);
            }

            parts.Add($"{prefix}{typeName} {CSharpKeywords.EscapeIdentifier(paramName)}");
        }

        return string.Join(", ", parts);
    }

    /// <summary>
    /// Gets the parameter name for a method parameter at the given index.
    /// </summary>
    private static string GetParameterName(MethodDefinition method, int index)
    {
        ParameterDefinition? paramDef = GetParameterDefinition(method, index);

        return paramDef?.Name?.Value ?? $"param{index}";
    }

    /// <summary>
    /// Gets the <see cref="ParameterDefinition"/> for a parameter at the given index.
    /// </summary>
    private static ParameterDefinition? GetParameterDefinition(MethodDefinition method, int index)
    {
        // ParameterDefinitions use 1-based Sequence numbers (0 is for return parameter)
        int targetSequence = index + 1;

        foreach (ParameterDefinition paramDef in method.ParameterDefinitions)
        {
            if (paramDef.Sequence == targetSequence)
            {
                return paramDef;
            }
        }

        return null;
    }

    /// <summary>
    /// Formats a GUID from the type's GuidAttribute.
    /// </summary>
    private static string? FormatGuid(TypeDefinition type)
    {
        CustomAttribute? guidAttr = TypeHelpers.GetAttribute(type, "Windows.Foundation.Metadata", "GuidAttribute");

        if (guidAttr?.Signature?.FixedArguments is not { Count: 11 } args)
        {
            return null;
        }

        // GuidAttribute(uint a, ushort b, ushort c, byte d, byte e, byte f, byte g, byte h, byte i, byte j, byte k)
        uint a = Convert.ToUInt32(args[0].Element);
        ushort b = Convert.ToUInt16(args[1].Element);
        ushort c = Convert.ToUInt16(args[2].Element);
        byte d = Convert.ToByte(args[3].Element);
        byte e = Convert.ToByte(args[4].Element);
        byte f = Convert.ToByte(args[5].Element);
        byte g = Convert.ToByte(args[6].Element);
        byte h = Convert.ToByte(args[7].Element);
        byte i = Convert.ToByte(args[8].Element);
        byte j = Convert.ToByte(args[9].Element);
        byte k = Convert.ToByte(args[10].Element);

        Guid guid = new(a, b, c, d, e, f, g, h, i, j, k);

        return guid.ToString("D");
    }

    /// <summary>
    /// Builds the list of base interfaces for an interface type.
    /// </summary>
    private static string BuildBaseInterfaceList(TypeDefinition type)
    {
        List<string> bases = [];

        foreach (InterfaceImplementation interfaceImpl in type.Interfaces)
        {
            if (interfaceImpl.Interface is not { } interfaceRef)
            {
                continue;
            }

            string? ns = interfaceRef.Namespace;
            string? typeName = interfaceRef.Name;

            // Skip mapped interfaces that shouldn't appear in the base list
            if (ns is not null && typeName is not null)
            {
                TypeMappings.MappedType? mapping = TypeMappings.GetMappedType(ns, typeName);

                if (mapping is { MappedNamespace: null })
                {
                    continue;
                }
            }

            bases.Add(GetProjectedTypeNameFromTypeRef(interfaceRef));
        }

        return string.Join(", ", bases);
    }

    /// <summary>
    /// Builds the base type and interface list for a class.
    /// </summary>
    private static string BuildClassBaseList(TypeDefinition type)
    {
        List<string> bases = [];

        // Add base class if it's not System.Object
        if (type.BaseType is { } baseType &&
            baseType.FullName is not "System.Object" and not null)
        {
            bases.Add(TypeNameHelpers.GetProjectedTypeName(baseType));
        }

        // Add implemented interfaces (non-exclusive, non-suppressed)
        foreach (InterfaceImplementation interfaceImpl in type.Interfaces)
        {
            if (interfaceImpl.Interface is not { } interfaceRef)
            {
                continue;
            }

            string? ns = interfaceRef.Namespace;
            string? typeName = interfaceRef.Name;

            if (ns is not null && typeName is not null)
            {
                TypeMappings.MappedType? mapping = TypeMappings.GetMappedType(ns, typeName);

                if (mapping is { MappedNamespace: null })
                {
                    continue;
                }
            }

            bases.Add(GetProjectedTypeNameFromTypeRef(interfaceRef));
        }

        return string.Join(", ", bases);
    }

    /// <summary>
    /// Writes instance members for a runtime class (constructors, methods, properties, events).
    /// </summary>
    private static void WriteClassMembers(CodeWriter writer, TypeDefinition type, ProjectionGeneratorArgs _)
    {
        // Collect property and event accessor names to skip in method output
        HashSet<string> specialMethodNames = CollectSpecialMethodNames(type);

        // Write constructors
        string name = TypeNameHelpers.GetSimpleName(type);

        foreach (MethodDefinition method in type.Methods)
        {
            if (method.Name?.Value != ".ctor" || method.IsStatic)
            {
                continue;
            }

            string parameters = BuildParameterList(method);
            writer.WriteLine($"public {name}({parameters})");

            using (writer.WriteBlock())
            {
                writer.WriteLine("throw new global::System.NotImplementedException();");
            }
        }

        // Write instance methods (skip property/event accessors and constructors)
        foreach (MethodDefinition method in type.Methods)
        {
            string? methodName = method.Name?.Value;

            if (methodName is null or ".ctor" or ".cctor")
            {
                continue;
            }

            if (method.IsSpecialName || method.IsRuntimeSpecialName)
            {
                continue;
            }

            if (specialMethodNames.Contains(methodName))
            {
                continue;
            }

            // Skip static methods for instance class body (they come from static interfaces)
            if (method.IsStatic)
            {
                continue;
            }

            WriteMethodSignature(writer, method, isInterfaceMethod: false);
        }

        // Write properties
        foreach (PropertyDefinition property in type.Properties)
        {
            WritePropertyDeclaration(writer, property, isInterfaceProperty: false);
        }

        // Write events
        foreach (EventDefinition evt in type.Events)
        {
            WriteEventDeclaration(writer, evt, isInterfaceEvent: false);
        }
    }

    /// <summary>
    /// Writes static members for a static runtime class.
    /// </summary>
    private static void WriteStaticClassMembers(CodeWriter writer, TypeDefinition type, ProjectionGeneratorArgs _)
    {
        // Static classes get their members from static interfaces
        // For now, iterate the type's own methods, properties, and events
        HashSet<string> specialMethodNames = CollectSpecialMethodNames(type);

        foreach (MethodDefinition method in type.Methods)
        {
            string? methodName = method.Name?.Value;

            if (methodName is null or ".ctor" or ".cctor")
            {
                continue;
            }

            if (method.IsSpecialName || method.IsRuntimeSpecialName)
            {
                continue;
            }

            if (specialMethodNames.Contains(methodName))
            {
                continue;
            }

            if (method.Signature is not { } methodSig)
            {
                continue;
            }

            string returnType = GetProjectedTypeName(methodSig.ReturnType);
            string escapedName = CSharpKeywords.EscapeIdentifier(methodName);
            string parameters = BuildParameterList(method);

            writer.WriteLine($"public static {returnType} {escapedName}({parameters})");

            using (writer.WriteBlock())
            {
                writer.WriteLine("throw new global::System.NotImplementedException();");
            }
        }

        foreach (PropertyDefinition property in type.Properties)
        {
            string propertyName = CSharpKeywords.EscapeIdentifier(property.Name?.Value ?? "");
            string propertyType = GetProjectedTypeName(property.Signature?.ReturnType);

            bool hasGetter = property.GetMethod is not null;
            bool hasSetter = property.SetMethod is not null;

            string accessors = hasGetter && hasSetter
                ? "{ get; set; }"
                : hasGetter ? "{ get; }" : "{ set; }";

            writer.WriteLine($"public static {propertyType} {propertyName} {accessors}");
        }

        foreach (EventDefinition evt in type.Events)
        {
            string eventName = CSharpKeywords.EscapeIdentifier(evt.Name?.Value ?? "");
            string eventType = evt.EventType is { } eventTypeRef
                ? GetProjectedTypeNameFromTypeRef(eventTypeRef)
                : "global::System.EventHandler";

            writer.WriteLine($"public static event {eventType} {eventName};");
        }
    }

    /// <summary>
    /// Writes auto-properties for a WinRT attribute based on its constructor parameters.
    /// </summary>
    private static void WriteAttributeProperties(CodeWriter writer, TypeDefinition type)
    {
        // Collect all unique parameter types/names across constructors to generate properties
        HashSet<string> emittedProperties = new(StringComparer.Ordinal);

        foreach (MethodDefinition method in type.Methods)
        {
            if (method.Name?.Value != ".ctor" || method.IsStatic)
            {
                continue;
            }

            if (method.Signature is not { } methodSig)
            {
                continue;
            }

            for (int i = 0; i < methodSig.ParameterTypes.Count; i++)
            {
                string paramName = GetParameterName(method, i);
                string propName = ToPascalCase(paramName);

                if (!emittedProperties.Add(propName))
                {
                    continue;
                }

                string propType = GetProjectedTypeName(methodSig.ParameterTypes[i]);
                writer.WriteLine($"public {propType} {propName} {{ get; set; }}");
            }
        }
    }

    /// <summary>
    /// Gets the AttributeTargets for a WinRT attribute type.
    /// </summary>
    private static string GetAttributeTargets(TypeDefinition type)
    {
        CustomAttribute? usageAttr = TypeHelpers.GetAttribute(type, "Windows.Foundation.Metadata", "AttributeUsageAttribute");

        if (usageAttr?.Signature?.FixedArguments is { Count: > 0 } args && args[0].Element is int targets)
        {
            // WinRT AttributeTargets maps to System.AttributeTargets
            // 0xFFFFFFFF = All
            return targets == -1
                ? "global::System.AttributeTargets.All"
                : $"(global::System.AttributeTargets){targets}";
        }

        return "global::System.AttributeTargets.All";
    }

    /// <summary>
    /// Collects the names of all property and event accessor methods.
    /// </summary>
    private static HashSet<string> CollectSpecialMethodNames(TypeDefinition type)
    {
        HashSet<string> names = new(StringComparer.Ordinal);

        foreach (PropertyDefinition property in type.Properties)
        {
            if (property.GetMethod?.Name?.Value is { } getterName)
            {
                _ = names.Add(getterName);
            }

            if (property.SetMethod?.Name?.Value is { } setterName)
            {
                _ = names.Add(setterName);
            }
        }

        foreach (EventDefinition evt in type.Events)
        {
            if (evt.AddMethod?.Name?.Value is { } addName)
            {
                _ = names.Add(addName);
            }

            if (evt.RemoveMethod?.Name?.Value is { } removeName)
            {
                _ = names.Add(removeName);
            }
        }

        return names;
    }

    /// <summary>
    /// Formats a constant value for enum member output.
    /// </summary>
    private static string FormatConstantValue(object? value, string underlyingType)
    {
        return value is null
            ? "0"
            : underlyingType == "uint" && value is uint uintValue
                ? uintValue.ToString()
                : value.ToString() ?? "0";
    }

    /// <summary>
    /// Removes the generic arity suffix from a type name (e.g., "IList`1" → "IList").
    /// </summary>
    private static string RemoveGenericArity(string typeName)
    {
        int backtickIndex = typeName.IndexOf('`');

        return backtickIndex >= 0 ? typeName[..backtickIndex] : typeName;
    }

    /// <summary>
    /// Converts a parameter name to PascalCase for use as a property name.
    /// </summary>
    private static string ToPascalCase(string name)
    {
        return string.IsNullOrEmpty(name) || char.IsUpper(name[0])
            ? name
            : $"{char.ToUpperInvariant(name[0])}{name[1..]}";
    }
}
