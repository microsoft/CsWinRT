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
    // ──────────────────────────────────────────────────────────────────────
    //  Assembly-level attributes
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Writes assembly-level attributes for type map groups.
    /// Maps to <c>write_winrt_comwrappers_typemapgroup_assembly_attribute</c>,
    /// <c>write_winrt_windowsmetadata_typemapgroup_assembly_attribute</c>, and
    /// <c>write_winrt_idic_typemapgroup_assembly_attribute</c> in the C++ cswinrt.
    /// </summary>
    public static void WriteAssemblyAttributes(CodeWriter writer, TypeDefinition type, TypeCategory category, ProjectionGeneratorArgs _)
    {
        string? ns = type.Namespace?.Value;
        string? typeName = type.Name?.Value;

        if (ns is null || typeName is null)
        {
            return;
        }

        string winrtName = $"{ns}.{typeName}";
        string simpleName = TypeNameHelpers.GetSimpleName(type);
        string fullProjectedName = $"global::{ns}.{simpleName}";

        switch (category)
        {
            case TypeCategory.Class:
                writer.WriteLine("#pragma warning disable IL2026");
                writer.WriteLine($"[assembly: TypeMap<WindowsRuntimeComWrappersTypeMapGroup>(");
                writer.IncreaseIndent();
                writer.WriteLine($"value: \"{winrtName}\",");
                writer.WriteLine($"target: typeof({fullProjectedName}),");
                writer.WriteLine($"trimTarget: typeof({fullProjectedName}))]");
                writer.DecreaseIndent();
                writer.WriteLine("#pragma warning restore IL2026");
                break;
            case TypeCategory.Interface:
                WriteInterfaceAssemblyAttributes(writer, ns, simpleName, winrtName, fullProjectedName);
                break;
            case TypeCategory.Delegate:
                writer.WriteLine("#pragma warning disable IL2026");
                writer.WriteLine($"[assembly: TypeMap<WindowsRuntimeComWrappersTypeMapGroup>(");
                writer.IncreaseIndent();
                writer.WriteLine($"value: \"{winrtName}\",");
                writer.WriteLine($"target: typeof({fullProjectedName}),");
                writer.WriteLine($"trimTarget: typeof({fullProjectedName}))]");
                writer.DecreaseIndent();
                writer.WriteLine("#pragma warning restore IL2026");
                break;
            case TypeCategory.Enum:
            case TypeCategory.Struct:
            case TypeCategory.Attribute:
            case TypeCategory.ApiContract:
            default:
                break;
        }
    }

    /// <summary>
    /// Writes assembly-level attributes for interface types (IDIC + metadata type map).
    /// </summary>
    private static void WriteInterfaceAssemblyAttributes(
        CodeWriter writer,
        string ns,
        string simpleName,
        string winrtName,
        string fullProjectedName)
    {
        string abiTypeName = $"global::ABI.{ns}.{simpleName}";

        writer.WriteLine("#pragma warning disable IL2026");
        writer.WriteLine($"[assembly: TypeMapAssociation<DynamicInterfaceCastableImplementationTypeMapGroup>(");
        writer.IncreaseIndent();
        writer.WriteLine($"source: typeof({fullProjectedName}),");
        writer.WriteLine($"proxy: typeof({abiTypeName}))]");
        writer.DecreaseIndent();
        writer.WriteLine($"[assembly: TypeMap<WindowsRuntimeMetadataTypeMapGroup>(");
        writer.IncreaseIndent();
        writer.WriteLine($"value: \"{winrtName}\",");
        writer.WriteLine($"target: typeof({fullProjectedName}),");
        writer.WriteLine($"trimTarget: typeof({fullProjectedName}))]");
        writer.DecreaseIndent();
        writer.WriteLine("#pragma warning restore IL2026");
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Enum
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Generates a C# enum from a WinRT enum type definition.
    /// Port of <c>write_enum</c> from the C++ cswinrt <c>code_writers.h</c>.
    /// </summary>
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

    // ──────────────────────────────────────────────────────────────────────
    //  Struct
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Generates a C# struct from a WinRT struct type definition.
    /// Port of <c>write_struct</c> from the C++ cswinrt <c>code_writers.h</c>.
    /// </summary>
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

    // ──────────────────────────────────────────────────────────────────────
    //  Delegate
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Generates a C# delegate from a WinRT delegate type definition.
    /// Port of <c>write_delegate</c> from the C++ cswinrt <c>code_writers.h</c>.
    /// </summary>
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

        // Write metadata attribute
        WriteWindowsRuntimeMetadataAttribute(writer, type);

        // Write contract version attribute
        WriteContractVersionAttribute(writer, type);

        // Write ComWrappers marshaller attribute
        string? ns = type.Namespace?.Value;
        if (ns is not null)
        {
            writer.WriteLine($"[ABI.{ns}.{name}ComWrappersMarshaller]");
        }

        // Write the GUID attribute
        string? guid = FormatGuid(type);

        if (guid is not null)
        {
            writer.WriteLine($"[Guid(\"{guid}\")]");
        }

        // Build the parameter list
        string returnType = GetProjectedTypeName(methodSig.ReturnType);
        string parameters = BuildParameterList(invokeMethod);

        writer.WriteLine($"{access} delegate {returnType} {name}({parameters});");
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Interface
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Generates a C# interface from a WinRT interface type definition.
    /// Port of <c>write_interface</c> from the C++ cswinrt <c>code_writers.h</c>.
    /// </summary>
    public static void WriteInterface(CodeWriter writer, TypeDefinition type, ProjectionGeneratorArgs args)
    {
        string name = TypeNameHelpers.GetSimpleName(type);
        bool isExclusiveTo = TypeHelpers.IsExclusiveTo(type);
        string access = GetInterfaceAccessModifier(type, args, isExclusiveTo);

        writer.WriteLine();

        // Write metadata attribute
        WriteWindowsRuntimeMetadataAttribute(writer, type);

        // Write the GUID attribute
        string? guid = FormatGuid(type);

        if (guid is not null)
        {
            writer.WriteLine($"[Guid(\"{guid}\")]");
        }

        // Write contract version attribute
        WriteContractVersionAttribute(writer, type);

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

                WriteInterfaceMethodSignature(writer, method);
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

    // ──────────────────────────────────────────────────────────────────────
    //  Class
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Generates a C# class from a WinRT runtime class type definition.
    /// Port of <c>write_class</c> from the C++ cswinrt <c>code_writers.h</c>.
    /// </summary>
    public static void WriteClass(CodeWriter writer, TypeDefinition type, ProjectionGeneratorArgs args)
    {
        string access = GetAccessModifier(type, args);
        string name = TypeNameHelpers.GetSimpleName(type);
        bool isStatic = TypeHelpers.IsStaticClass(type);
        string? ns = type.Namespace?.Value;
        string winrtClassName = ns is not null ? $"{ns}.{name}" : name;

        writer.WriteLine();

        if (isStatic)
        {
            WriteStaticClass(writer, type, args, access, name, winrtClassName);
        }
        else
        {
            WriteSealedClass(writer, type, args, access, name, winrtClassName);
        }
    }

    /// <summary>
    /// Writes a static WinRT class (abstract + sealed), which gets its members from statics interfaces.
    /// </summary>
    private static void WriteStaticClass(
        CodeWriter writer,
        TypeDefinition type,
        ProjectionGeneratorArgs _,
        string access,
        string name,
        string winrtClassName)
    {
        // Write class-level attributes
        WriteWindowsRuntimeMetadataAttribute(writer, type);
        WriteContractVersionAttribute(writer, type);

        writer.WriteLine($"{access} static class {name}");

        using (writer.WriteBlock())
        {
            // For each statics interface, generate an activation factory property and delegate members
            foreach (InterfaceImplementation ifaceImpl in type.Interfaces)
            {
                if (ifaceImpl.Interface is not { } ifaceRef)
                {
                    continue;
                }

                TypeDefinition? ifaceType = ifaceRef.Resolve();

                if (ifaceType is null)
                {
                    continue;
                }

                // Skip mapped interfaces that should be suppressed
                string? ifaceNs = ifaceRef.Namespace;
                string? ifaceTypeName = ifaceRef.Name;

                if (ifaceNs is not null && ifaceTypeName is not null)
                {
                    TypeMappings.MappedType? mapping = TypeMappings.GetMappedType(ifaceNs, ifaceTypeName);

                    if (mapping is { MappedNamespace: null })
                    {
                        continue;
                    }
                }

                string ifaceSimpleName = TypeNameHelpers.GetSimpleName(ifaceType);
                string objRefFieldName = GetObjRefFieldName(ifaceRef);
                string iidFieldRef = GetIIDFieldReferenceForTypeRef(ifaceRef);

                // Write static activation factory property
                WriteStaticActivationFactoryProperty(writer, objRefFieldName, winrtClassName, iidFieldRef);

                // Write delegating static members from this interface
                string? resolvedIfaceNs = ifaceType.Namespace?.Value;
                string methodsClassName = resolvedIfaceNs is not null
                    ? $"global::ABI.{resolvedIfaceNs}.{ifaceSimpleName}Methods"
                    : $"global::ABI.{ifaceSimpleName}Methods";

                WriteStaticDelegatingMembers(writer, ifaceType, objRefFieldName, methodsClassName);
            }
        }
    }

    /// <summary>
    /// Writes a sealed (non-static) WinRT class that extends WindowsRuntimeObject.
    /// </summary>
    private static void WriteSealedClass(
        CodeWriter writer,
        TypeDefinition type,
        ProjectionGeneratorArgs _,
        string access,
        string name,
        string winrtClassName)
    {
        // Classify all interfaces on the class
        ITypeDefOrRef? defaultInterfaceRef = null;
        List<InterfaceImplementation> instanceInterfaces = [];
        List<InterfaceImplementation> factoryInterfaces = [];
        List<InterfaceImplementation> staticsInterfaces = [];
        List<InterfaceImplementation> overridableInterfaces = [];

        foreach (InterfaceImplementation ifaceImpl in type.Interfaces)
        {
            if (ifaceImpl.Interface is not { } ifaceRef)
            {
                continue;
            }

            // Skip suppressed mapped types
            string? ifaceNs = ifaceRef.Namespace;
            string? ifaceTypeName = ifaceRef.Name;

            if (ifaceNs is not null && ifaceTypeName is not null)
            {
                TypeMappings.MappedType? mapping = TypeMappings.GetMappedType(ifaceNs, ifaceTypeName);

                if (mapping is { MappedNamespace: null })
                {
                    continue;
                }
            }

            if (TypeHelpers.IsDefaultInterface(ifaceImpl))
            {
                defaultInterfaceRef = ifaceRef;
                instanceInterfaces.Add(ifaceImpl);
            }
            else if (IsFactoryInterface(ifaceRef))
            {
                factoryInterfaces.Add(ifaceImpl);
            }
            else if (IsStaticsInterface(ifaceRef))
            {
                staticsInterfaces.Add(ifaceImpl);
            }
            else if (TypeHelpers.IsOverridable(ifaceImpl))
            {
                overridableInterfaces.Add(ifaceImpl);
                instanceInterfaces.Add(ifaceImpl);
            }
            else
            {
                instanceInterfaces.Add(ifaceImpl);
            }
        }

        // Write class-level attributes
        WriteWindowsRuntimeMetadataAttribute(writer, type);
        writer.WriteLine($"[WindowsRuntimeClassName(\"{winrtClassName}\")]");
        WriteContractVersionAttribute(writer, type);

        // Write ComWrappers marshaller attribute
        string? ns = type.Namespace?.Value;
        if (ns is not null)
        {
            writer.WriteLine($"[ABI.{ns}.{name}ComWrappersMarshaller]");
        }

        // Write default interface attribute
        if (defaultInterfaceRef is not null)
        {
            string defaultIfaceName = GetProjectedTypeNameFromTypeRef(defaultInterfaceRef);
            writer.WriteLine($"[WindowsRuntimeDefaultInterfaceAttribute(typeof({defaultIfaceName}))]");
        }

        // Build base list: WindowsRuntimeObject + mapped interfaces + IWindowsRuntimeInterface<> markers
        string baseList = BuildSealedClassBaseList(instanceInterfaces);
        string declaration = string.IsNullOrEmpty(baseList)
            ? $"{access} sealed class {name}"
            : $"{access} sealed class {name} : {baseList}";

        writer.WriteLine(declaration);

        using (writer.WriteBlock())
        {
            // 1. _objRef_* fields for each instance interface
            WriteObjRefFields(writer, instanceInterfaces, defaultInterfaceRef);

            // 2. Internal constructor taking WindowsRuntimeObjectReference
            writer.WriteLine();
            writer.WriteLine($"internal {name}(WindowsRuntimeObjectReference nativeObjectReference)");
            writer.WriteLine(": base(nativeObjectReference)");
            using (writer.WriteBlock())
            {
            }

            // 3. Static activation factory properties for factory interfaces
            foreach (InterfaceImplementation factoryImpl in factoryInterfaces)
            {
                if (factoryImpl.Interface is not { } factoryRef)
                {
                    continue;
                }

                string objRefFieldName = GetObjRefFieldName(factoryRef);
                string iidFieldRef = GetIIDFieldReferenceForTypeRef(factoryRef);

                writer.WriteLine();
                WriteStaticActivationFactoryProperty(writer, objRefFieldName, winrtClassName, iidFieldRef);
            }

            // 4. Default activatable constructor (if the class has a default constructor)
            if (TypeHelpers.HasDefaultConstructor(type) && defaultInterfaceRef is not null)
            {
                string defaultIidFieldRef = GetIIDFieldReferenceForTypeRef(defaultInterfaceRef);

                writer.WriteLine();
                writer.WriteLine($"public {name}()");
                writer.WriteLine($"  :base(WindowsRuntimeActivationFactory.ActivateInstance<{name}>(\"{winrtClassName}\", {defaultIidFieldRef}))");
                using (writer.WriteBlock())
                {
                }
            }

            // 5. Factory constructors (from factory interfaces)
            int factoryCtorIndex = 1;

            foreach (InterfaceImplementation factoryImpl in factoryInterfaces)
            {
                if (factoryImpl.Interface is not { } factoryRef)
                {
                    continue;
                }

                TypeDefinition? factoryType = factoryRef.Resolve();

                if (factoryType is null)
                {
                    continue;
                }

                string factoryObjRefFieldName = GetObjRefFieldName(factoryRef);

                if (defaultInterfaceRef is null)
                {
                    continue;
                }

                string defaultIidFieldRef = GetIIDFieldReferenceForTypeRef(defaultInterfaceRef);

                foreach (MethodDefinition method in factoryType.Methods)
                {
                    if (method.Name?.Value is null or ".ctor" or ".cctor")
                    {
                        continue;
                    }

                    if (method.Signature is not { } methodSig || methodSig.ReturnType is null)
                    {
                        continue;
                    }

                    // Build the public constructor parameter list
                    string ctorParams = BuildParameterList(method);

                    if (string.IsNullOrEmpty(ctorParams))
                    {
                        continue;
                    }

                    // Build the array of parameters for the callback
                    List<string> paramNames = [];

                    for (int i = 0; i < methodSig.ParameterTypes.Count; i++)
                    {
                        paramNames.Add(GetParameterName(method, i));
                    }

                    string callbackClassName = $"Create_{factoryCtorIndex}";
                    string paramsArray = string.Join(", ", paramNames);

                    writer.WriteLine();
                    writer.WriteLine($"public unsafe {name}({ctorParams})");
                    writer.WriteLine($"  :base({callbackClassName}.Instance, {defaultIidFieldRef}, [{paramsArray}])");
                    using (writer.WriteBlock())
                    {
                    }

                    // Write the nested callback class
                    WriteFactoryCallbackClass(writer, callbackClassName, factoryObjRefFieldName, method, factoryType);

                    factoryCtorIndex++;
                }
            }

            // 6. HasUnwrappableNativeObjectReference and IsOverridableInterface overrides
            writer.WriteLine();
            writer.WriteLine("protected override bool HasUnwrappableNativeObjectReference => true;");

            if (overridableInterfaces.Count > 0)
            {
                writer.Write("protected override bool IsOverridableInterface(in Guid iid) => ");

                List<string> iidChecks = [];

                foreach (InterfaceImplementation overridableImpl in overridableInterfaces)
                {
                    if (overridableImpl.Interface is { } overridableRef)
                    {
                        string iidRef = GetIIDFieldReferenceForTypeRef(overridableRef);
                        iidChecks.Add($"iid == {iidRef}");
                    }
                }

                writer.WriteLine($"{string.Join(" || ", iidChecks)};");
            }
            else
            {
                writer.WriteLine("protected override bool IsOverridableInterface(in Guid iid) => false;");
            }

            // 7. Delegating instance members from each interface
            foreach (InterfaceImplementation ifaceImpl in instanceInterfaces)
            {
                if (ifaceImpl.Interface is not { } ifaceRef)
                {
                    continue;
                }

                TypeDefinition? ifaceType = ifaceRef.Resolve();

                if (ifaceType is null)
                {
                    continue;
                }

                string objRefFieldName = GetObjRefFieldName(ifaceRef);
                string ifaceSimpleName = TypeNameHelpers.GetSimpleName(ifaceType);
                string? ifaceNs = ifaceType.Namespace?.Value;
                string methodsClassName = ifaceNs is not null
                    ? $"global::ABI.{ifaceNs}.{ifaceSimpleName}Methods"
                    : $"global::ABI.{ifaceSimpleName}Methods";

                // Check if this interface is a mapped .NET type (e.g., IClosable -> IDisposable)
                string? ifaceRefNs = ifaceRef.Namespace;
                string? ifaceRefName = ifaceRef.Name;
                TypeMappings.MappedType? ifaceMapping = null;

                if (ifaceRefNs is not null && ifaceRefName is not null)
                {
                    ifaceMapping = TypeMappings.GetMappedType(ifaceRefNs, ifaceRefName);
                }

                bool isMappedInterface = ifaceMapping is { MappedNamespace: not null, MappedName: not null };

                if (isMappedInterface)
                {
                    string mappedFullName = $"global::{ifaceMapping!.Value.MappedNamespace}.{RemoveGenericArity(ifaceMapping.Value.MappedName!)}";

                    // Write IWindowsRuntimeInterface<>.GetInterface()
                    writer.WriteLine();
                    writer.WriteLine($"WindowsRuntimeObjectReferenceValue IWindowsRuntimeInterface<{mappedFullName}>.GetInterface()");
                    using (writer.WriteBlock())
                    {
                        writer.WriteLine($"return {objRefFieldName}.AsValue();");
                    }

                    // Write delegating members using the ABI methods class for the WinRT interface
                    WriteInstanceDelegatingMembers(writer, ifaceType, objRefFieldName, methodsClassName);
                }
                else
                {
                    WriteInstanceDelegatingMembers(writer, ifaceType, objRefFieldName, methodsClassName);
                }
            }

            // 8. Static members from statics interfaces
            foreach (InterfaceImplementation staticsImpl in staticsInterfaces)
            {
                if (staticsImpl.Interface is not { } staticsRef)
                {
                    continue;
                }

                TypeDefinition? staticsType = staticsRef.Resolve();

                if (staticsType is null)
                {
                    continue;
                }

                string staticsObjRefFieldName = GetObjRefFieldName(staticsRef);
                string staticsIidFieldRef = GetIIDFieldReferenceForTypeRef(staticsRef);

                // Write static activation factory property
                writer.WriteLine();
                WriteStaticActivationFactoryProperty(writer, staticsObjRefFieldName, winrtClassName, staticsIidFieldRef);

                string staticsSimpleName = TypeNameHelpers.GetSimpleName(staticsType);
                string? staticsNs = staticsType.Namespace?.Value;
                string methodsClassName = staticsNs is not null
                    ? $"global::ABI.{staticsNs}.{staticsSimpleName}Methods"
                    : $"global::ABI.{staticsSimpleName}Methods";

                WriteStaticDelegatingMembers(writer, staticsType, staticsObjRefFieldName, methodsClassName);
            }
        }
    }

    /// <summary>
    /// Writes _objRef_* properties for each instance interface on a sealed class.
    /// The default interface uses <c>=> NativeObjectReference;</c>.
    /// Non-default interfaces use lazy initialization with <c>Interlocked.CompareExchange</c>.
    /// </summary>
    private static void WriteObjRefFields(
        CodeWriter writer,
        List<InterfaceImplementation> instanceInterfaces,
        ITypeDefOrRef? defaultInterfaceRef)
    {
        foreach (InterfaceImplementation ifaceImpl in instanceInterfaces)
        {
            if (ifaceImpl.Interface is not { } ifaceRef)
            {
                continue;
            }

            string objRefFieldName = GetObjRefFieldName(ifaceRef);

            bool isDefault = defaultInterfaceRef is not null &&
                             ifaceRef.FullName == defaultInterfaceRef.FullName;

            if (isDefault)
            {
                writer.WriteLine($"private WindowsRuntimeObjectReference {objRefFieldName} => NativeObjectReference;");
            }
            else
            {
                string iidFieldRef = GetIIDFieldReferenceForTypeRef(ifaceRef);

                writer.WriteLine($"private WindowsRuntimeObjectReference {objRefFieldName}");
                using (writer.WriteBlock())
                {
                    writer.WriteLine("get");
                    using (writer.WriteBlock())
                    {
                        writer.WriteLine("[MethodImpl(MethodImplOptions.NoInlining)]");
                        writer.WriteLine("WindowsRuntimeObjectReference MakeObjectReference()");
                        using (writer.WriteBlock())
                        {
                            writer.WriteLine("_ = global::System.Threading.Interlocked.CompareExchange(");
                            writer.IncreaseIndent();
                            writer.WriteLine("location1: ref field,");
                            writer.WriteLine($"value: NativeObjectReference.As({iidFieldRef}),");
                            writer.WriteLine("comparand: null);");
                            writer.DecreaseIndent();
                            writer.WriteLine("return field;");
                        }

                        writer.WriteLine("return field ?? MakeObjectReference();");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Writes a static activation factory property with context checking.
    /// </summary>
    private static void WriteStaticActivationFactoryProperty(
        CodeWriter writer,
        string fieldName,
        string winrtClassName,
        string iidFieldRef)
    {
        writer.WriteLine($"private static WindowsRuntimeObjectReference {fieldName}");
        using (writer.WriteBlock())
        {
            writer.WriteLine("get");
            using (writer.WriteBlock())
            {
                writer.WriteLine($"var __objRef = field;");
                writer.WriteLine("if (__objRef != null && __objRef.IsInCurrentContext)");
                using (writer.WriteBlock())
                {
                    writer.WriteLine("return __objRef;");
                }

                writer.WriteLine($"return field = WindowsRuntimeActivationFactory.GetActivationFactory(\"{winrtClassName}\", {iidFieldRef});");
            }
        }
    }

    /// <summary>
    /// Writes a nested factory callback class used for constructors that take parameters.
    /// </summary>
    private static void WriteFactoryCallbackClass(
        CodeWriter writer,
        string callbackClassName,
        string factoryObjRefFieldName,
        MethodDefinition factoryMethod,
        TypeDefinition factoryType)
    {
        if (factoryMethod.Signature is not { } methodSig)
        {
            return;
        }

        string? factoryNs = factoryType.Namespace?.Value;
        string factorySimpleName = TypeNameHelpers.GetSimpleName(factoryType);
        string methodsClassName = factoryNs is not null
            ? $"global::ABI.{factoryNs}.{factorySimpleName}Methods"
            : $"global::ABI.{factorySimpleName}Methods";

        string methodName = CSharpKeywords.EscapeIdentifier(factoryMethod.Name?.Value ?? "");

        writer.WriteLine();
        writer.WriteLine($"private sealed class {callbackClassName} : WindowsRuntimeActivationFactoryCallback.DerivedSealed");
        using (writer.WriteBlock())
        {
            writer.WriteLine($"public static readonly {callbackClassName} Instance = new();");
            writer.WriteLine("[MethodImpl(MethodImplOptions.NoInlining)]");
            writer.WriteLine("public override unsafe void Invoke(");
            writer.IncreaseIndent();
            writer.WriteLine("ReadOnlySpan<object> additionalParameters,");
            writer.WriteLine("out void* retval)");
            writer.DecreaseIndent();
            using (writer.WriteBlock())
            {
                writer.WriteLine($"using WindowsRuntimeObjectReferenceValue activationFactoryValue = {factoryObjRefFieldName}.AsValue();");
                writer.WriteLine("void* ThisPtr = activationFactoryValue.GetThisPtrUnsafe();");

                // Extract parameters from additionalParameters span
                for (int i = 0; i < methodSig.ParameterTypes.Count; i++)
                {
                    string paramName = GetParameterName(factoryMethod, i);
                    string paramType = GetProjectedTypeName(methodSig.ParameterTypes[i]);
                    writer.WriteLine($"{paramType} {paramName} = ({paramType})additionalParameters[{i}];");
                }

                // Delegate to the ABI methods class
                // Build arguments for the factory method call
                List<string> callArgs = ["ThisPtr"];

                for (int i = 0; i < methodSig.ParameterTypes.Count; i++)
                {
                    callArgs.Add(GetParameterName(factoryMethod, i));
                }

                writer.WriteLine("void* __retval = default;");
                writer.WriteLine($"__retval = {methodsClassName}.{methodName}({string.Join(", ", callArgs)});");
                writer.WriteLine("retval = __retval;");
            }
        }
    }

    /// <summary>
    /// Writes delegating instance members from a resolved interface type.
    /// Each method/property/event delegates to the corresponding ABI methods class.
    /// </summary>
    private static void WriteInstanceDelegatingMembers(
        CodeWriter writer,
        TypeDefinition ifaceType,
        string objRefFieldName,
        string methodsClassName)
    {
        HashSet<string> specialMethodNames = CollectSpecialMethodNames(ifaceType);

        // Methods
        foreach (MethodDefinition method in ifaceType.Methods)
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

            if (method.Signature is not { } methodSig)
            {
                continue;
            }

            string returnType = GetProjectedTypeName(methodSig.ReturnType);
            string escapedName = CSharpKeywords.EscapeIdentifier(methodName);
            string parameters = BuildParameterList(method);
            string callArgs = BuildCallArguments(method, objRefFieldName);

            writer.WriteLine();

            if (returnType == "void")
            {
                writer.WriteLine($"public void {escapedName}({parameters}) => {methodsClassName}.{escapedName}({callArgs});");
            }
            else
            {
                writer.WriteLine($"public {returnType} {escapedName}({parameters}) => {methodsClassName}.{escapedName}({callArgs});");
            }
        }

        // Properties
        foreach (PropertyDefinition property in ifaceType.Properties)
        {
            string propertyName = CSharpKeywords.EscapeIdentifier(property.Name?.Value ?? "");
            string propertyType = GetProjectedTypeName(property.Signature?.ReturnType);

            (MethodDefinition? getter, MethodDefinition? setter) = TypeHelpers.GetPropertyMethods(property);

            writer.WriteLine();

            if (getter is not null && setter is not null)
            {
                // Read-write property
                writer.WriteLine($"public {propertyType} {propertyName}");
                using (writer.WriteBlock())
                {
                    writer.WriteLine($"get => {methodsClassName}.get_{property.Name?.Value ?? ""}({objRefFieldName});");
                    writer.WriteLine($"set => {methodsClassName}.put_{property.Name?.Value ?? ""}({objRefFieldName}, value);");
                }
            }
            else if (getter is not null)
            {
                // Read-only property - use expression body
                writer.WriteLine($"public {propertyType} {propertyName} => {methodsClassName}.get_{property.Name?.Value ?? ""}({objRefFieldName});");
            }
            else if (setter is not null)
            {
                // Write-only property
                writer.WriteLine($"public {propertyType} {propertyName}");
                using (writer.WriteBlock())
                {
                    writer.WriteLine($"set => {methodsClassName}.put_{property.Name?.Value ?? ""}({objRefFieldName}, value);");
                }
            }
        }

        // Events
        foreach (EventDefinition evt in ifaceType.Events)
        {
            string eventName = CSharpKeywords.EscapeIdentifier(evt.Name?.Value ?? "");
            string eventType = evt.EventType is { } eventTypeRef
                ? GetProjectedTypeNameFromTypeRef(eventTypeRef)
                : "global::System.EventHandler";

            (MethodDefinition? add, MethodDefinition? remove) = TypeHelpers.GetEventMethods(evt);

            writer.WriteLine();
            writer.WriteLine($"public event {eventType} {eventName}");
            using (writer.WriteBlock())
            {
                if (add is not null)
                {
                    writer.WriteLine($"add => {methodsClassName}.add_{evt.Name?.Value ?? ""}({objRefFieldName}, value);");
                }

                if (remove is not null)
                {
                    writer.WriteLine($"remove => {methodsClassName}.remove_{evt.Name?.Value ?? ""}({objRefFieldName}, value);");
                }
            }
        }
    }

    /// <summary>
    /// Writes delegating static members from a resolved interface type.
    /// </summary>
    private static void WriteStaticDelegatingMembers(
        CodeWriter writer,
        TypeDefinition ifaceType,
        string objRefFieldName,
        string methodsClassName)
    {
        HashSet<string> specialMethodNames = CollectSpecialMethodNames(ifaceType);

        // Methods
        foreach (MethodDefinition method in ifaceType.Methods)
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

            if (method.Signature is not { } methodSig)
            {
                continue;
            }

            string returnType = GetProjectedTypeName(methodSig.ReturnType);
            string escapedName = CSharpKeywords.EscapeIdentifier(methodName);
            string parameters = BuildParameterList(method);
            string callArgs = BuildCallArguments(method, objRefFieldName);

            writer.WriteLine();

            if (returnType == "void")
            {
                writer.WriteLine($"public static void {escapedName}({parameters}) => {methodsClassName}.{escapedName}({callArgs});");
            }
            else
            {
                writer.WriteLine($"public static {returnType} {escapedName}({parameters}) => {methodsClassName}.{escapedName}({callArgs});");
            }
        }

        // Properties
        foreach (PropertyDefinition property in ifaceType.Properties)
        {
            string propertyName = CSharpKeywords.EscapeIdentifier(property.Name?.Value ?? "");
            string propertyType = GetProjectedTypeName(property.Signature?.ReturnType);

            (MethodDefinition? getter, MethodDefinition? setter) = TypeHelpers.GetPropertyMethods(property);

            writer.WriteLine();

            if (getter is not null && setter is not null)
            {
                writer.WriteLine($"public static {propertyType} {propertyName}");
                using (writer.WriteBlock())
                {
                    writer.WriteLine($"get => {methodsClassName}.get_{property.Name?.Value ?? ""}({objRefFieldName});");
                    writer.WriteLine($"set => {methodsClassName}.put_{property.Name?.Value ?? ""}({objRefFieldName}, value);");
                }
            }
            else if (getter is not null)
            {
                writer.WriteLine($"public static {propertyType} {propertyName} => {methodsClassName}.get_{property.Name?.Value ?? ""}({objRefFieldName});");
            }
            else if (setter is not null)
            {
                writer.WriteLine($"public static {propertyType} {propertyName}");
                using (writer.WriteBlock())
                {
                    writer.WriteLine($"set => {methodsClassName}.put_{property.Name?.Value ?? ""}({objRefFieldName}, value);");
                }
            }
        }

        // Events
        foreach (EventDefinition evt in ifaceType.Events)
        {
            string eventName = CSharpKeywords.EscapeIdentifier(evt.Name?.Value ?? "");
            string eventType = evt.EventType is { } eventTypeRef
                ? GetProjectedTypeNameFromTypeRef(eventTypeRef)
                : "global::System.EventHandler";

            (MethodDefinition? add, MethodDefinition? remove) = TypeHelpers.GetEventMethods(evt);

            writer.WriteLine();
            writer.WriteLine($"public static event {eventType} {eventName}");
            using (writer.WriteBlock())
            {
                if (add is not null)
                {
                    writer.WriteLine($"add => {methodsClassName}.add_{evt.Name?.Value ?? ""}({objRefFieldName}, value);");
                }

                if (remove is not null)
                {
                    writer.WriteLine($"remove => {methodsClassName}.remove_{evt.Name?.Value ?? ""}({objRefFieldName}, value);");
                }
            }
        }
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Attribute
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Generates a C# attribute class from a WinRT attribute type definition.
    /// Port of <c>write_attribute</c> from the C++ cswinrt <c>code_writers.h</c>.
    /// </summary>
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

    // ──────────────────────────────────────────────────────────────────────
    //  API Contract
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Generates a C# struct for a WinRT API contract type definition.
    /// Port of <c>write_contract</c> from the C++ cswinrt <c>code_writers.h</c>.
    /// </summary>
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

    // ──────────────────────────────────────────────────────────────────────
    //  Shared helpers: access modifiers
    // ──────────────────────────────────────────────────────────────────────

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

    // ──────────────────────────────────────────────────────────────────────
    //  Shared helpers: type name projection
    // ──────────────────────────────────────────────────────────────────────

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
    /// Gets the projected type name from an <see cref="ITypeDefOrRef"/> (used for event types and base lists).
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

    // ──────────────────────────────────────────────────────────────────────
    //  Shared helpers: method signatures and parameters
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Writes a method signature for interface members (declaration only, no body).
    /// </summary>
    private static void WriteInterfaceMethodSignature(CodeWriter writer, MethodDefinition method)
    {
        if (method.Signature is not { } methodSig)
        {
            return;
        }

        string returnType = GetProjectedTypeName(methodSig.ReturnType);
        string methodName = CSharpKeywords.EscapeIdentifier(method.Name?.Value ?? "");
        string parameters = BuildParameterList(method);

        writer.WriteLine($"{returnType} {methodName}({parameters});");
    }

    /// <summary>
    /// Writes a property declaration for interfaces.
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
    /// Writes an event declaration for interfaces.
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
    /// Builds the call arguments for a method delegation, prepending the object reference field name.
    /// </summary>
    private static string BuildCallArguments(MethodDefinition method, string objRefFieldName)
    {
        if (method.Signature is not { } methodSig || methodSig.ParameterTypes.Count == 0)
        {
            return objRefFieldName;
        }

        List<string> args = [objRefFieldName];

        for (int i = 0; i < methodSig.ParameterTypes.Count; i++)
        {
            TypeSignature paramType = methodSig.ParameterTypes[i];
            string paramName = GetParameterName(method, i);
            string escapedName = CSharpKeywords.EscapeIdentifier(paramName);

            if (paramType is ByReferenceTypeSignature)
            {
                ParameterDefinition? paramDef = GetParameterDefinition(method, i);
                string modifier = paramDef?.IsOut == true ? "out " : "ref ";
                args.Add($"{modifier}{escapedName}");
            }
            else
            {
                args.Add(escapedName);
            }
        }

        return string.Join(", ", args);
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

    // ──────────────────────────────────────────────────────────────────────
    //  Shared helpers: GUID formatting
    // ──────────────────────────────────────────────────────────────────────

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

    // ──────────────────────────────────────────────────────────────────────
    //  Shared helpers: interface classification
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Determines whether an interface reference is a factory interface by naming convention.
    /// Factory interfaces end with "Factory" or contain "Factory" followed by a digit.
    /// </summary>
    private static bool IsFactoryInterface(ITypeDefOrRef ifaceRef)
    {
        string? name = ifaceRef.Name;

        if (name is null)
        {
            return false;
        }

        // Match patterns like IXxxFactory, IXxxFactory2, etc.
        int factoryIndex = name.IndexOf("Factory", StringComparison.Ordinal);

        if (factoryIndex < 0)
        {
            return false;
        }

        // Everything after "Factory" should be empty or digits
        string suffix = name[(factoryIndex + "Factory".Length)..];

        return suffix.Length == 0 || int.TryParse(suffix, out _);
    }

    /// <summary>
    /// Determines whether an interface reference is a statics interface by naming convention.
    /// Statics interfaces end with "Statics" or contain "Statics" followed by a digit.
    /// </summary>
    private static bool IsStaticsInterface(ITypeDefOrRef ifaceRef)
    {
        string? name = ifaceRef.Name;

        if (name is null)
        {
            return false;
        }

        // Match patterns like IXxxStatics, IXxxStatics2, etc.
        int staticsIndex = name.IndexOf("Statics", StringComparison.Ordinal);

        if (staticsIndex < 0)
        {
            return false;
        }

        // Everything after "Statics" should be empty or digits
        string suffix = name[(staticsIndex + "Statics".Length)..];

        return suffix.Length == 0 || int.TryParse(suffix, out _);
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Shared helpers: base lists
    // ──────────────────────────────────────────────────────────────────────

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
    /// Builds the base list for a sealed WinRT class.
    /// Includes WindowsRuntimeObject, mapped .NET interfaces, and IWindowsRuntimeInterface markers.
    /// </summary>
    private static string BuildSealedClassBaseList(List<InterfaceImplementation> instanceInterfaces)
    {
        List<string> bases = ["WindowsRuntimeObject"];

        foreach (InterfaceImplementation ifaceImpl in instanceInterfaces)
        {
            if (ifaceImpl.Interface is not { } ifaceRef)
            {
                continue;
            }

            string? ifaceNs = ifaceRef.Namespace;
            string? ifaceTypeName = ifaceRef.Name;

            if (ifaceNs is null || ifaceTypeName is null)
            {
                continue;
            }

            // Check if this is a mapped type (e.g., IClosable -> IDisposable)
            TypeMappings.MappedType? mapping = TypeMappings.GetMappedType(ifaceNs, ifaceTypeName);

            if (mapping is { MappedNamespace: not null, MappedName: not null })
            {
                string mappedFullName = $"global::{mapping.Value.MappedNamespace}.{RemoveGenericArity(mapping.Value.MappedName)}";
                bases.Add(mappedFullName);
                bases.Add($"IWindowsRuntimeInterface<{mappedFullName}>");
            }
        }

        return string.Join(", ", bases);
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Shared helpers: field names and IID references
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets the _objRef_ field name for an interface, using the escaped full type name.
    /// E.g., <c>_objRef_Windows_Foundation_IDeferral</c>.
    /// </summary>
    private static string GetObjRefFieldName(ITypeDefOrRef ifaceRef)
    {
        string? ns = ifaceRef.Namespace;
        string? typeName = ifaceRef.Name;

        if (ns is not null && typeName is not null)
        {
            // Check for mapped types
            TypeMappings.MappedType? mapping = TypeMappings.GetMappedType(ns, typeName);

            if (mapping is { MappedNamespace: not null, MappedName: not null })
            {
                string mappedEscaped = TypeNameHelpers.EscapeTypeNameForIdentifier(
                    $"{mapping.Value.MappedNamespace}.{RemoveGenericArity(mapping.Value.MappedName)}");
                return $"_objRef_{mappedEscaped}";
            }
        }

        string escaped = TypeNameHelpers.EscapeTypeNameForIdentifier($"{ns}.{RemoveGenericArity(typeName ?? "")}");
        return $"_objRef_{escaped}";
    }

    /// <summary>
    /// Gets the IID field reference for a type reference.
    /// E.g., <c>global::ABI.InterfaceIIDs.IID_Windows_Foundation_IDeferral</c>.
    /// </summary>
    private static string GetIIDFieldReferenceForTypeRef(ITypeDefOrRef typeRef)
    {
        string escapedName = TypeNameHelpers.EscapeTypeNameForIdentifier($"{typeRef.Namespace}.{typeRef.Name}");

        return $"global::ABI.InterfaceIIDs.IID_{escapedName}";
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Shared helpers: attributes
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Writes the [WindowsRuntimeMetadata] attribute for a type,
    /// using the first component of the namespace (e.g., "Windows", "Microsoft").
    /// </summary>
    private static void WriteWindowsRuntimeMetadataAttribute(CodeWriter writer, TypeDefinition type)
    {
        string? ns = type.Namespace?.Value;

        if (ns is null)
        {
            return;
        }

        // Get the root namespace component (e.g., "Windows" from "Windows.Foundation")
        int dotIndex = ns.IndexOf('.');
        string rootNs = dotIndex >= 0 ? ns[..dotIndex] : ns;

        writer.WriteLine($"[WindowsRuntimeMetadata(\"{rootNs}\")]");
    }

    /// <summary>
    /// Writes the [ContractVersion] attribute for a type, if it has a ContractVersionAttribute.
    /// Produces: <c>[global::Windows.Foundation.Metadata.ContractVersion(typeof(ContractType), version)]</c>.
    /// </summary>
    private static void WriteContractVersionAttribute(CodeWriter writer, TypeDefinition type)
    {
        CustomAttribute? attr = TypeHelpers.GetAttribute(type, "Windows.Foundation.Metadata", "ContractVersionAttribute");

        if (attr?.Signature?.FixedArguments is not { Count: > 0 } args)
        {
            return;
        }

        // Extract the contract type (first argument) and version (last uint argument)
        TypeSignature? contractTypeSig = null;
        uint? version = null;

        foreach (CustomAttributeArgument arg in args)
        {
            if (arg.Element is TypeSignature typeSig)
            {
                contractTypeSig = typeSig;
            }
            else if (arg.Element is uint uintVal)
            {
                version = uintVal;
            }
        }

        if (version is null)
        {
            return;
        }

        if (contractTypeSig is TypeDefOrRefSignature typeDefOrRef && typeDefOrRef.Type is { } contractTypeRef)
        {
            string contractTypeName = TypeNameHelpers.GetProjectedTypeName(contractTypeRef);
            writer.WriteLine($"[global::Windows.Foundation.Metadata.ContractVersion(typeof({contractTypeName}), {version.Value}u)]");
        }
        else
        {
            // Fallback: emit with just the version
            writer.WriteLine($"[global::Windows.Foundation.Metadata.ContractVersion({version.Value}u)]");
        }
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Shared helpers: attribute properties
    // ──────────────────────────────────────────────────────────────────────

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

    // ──────────────────────────────────────────────────────────────────────
    //  Shared helpers: special method collection
    // ──────────────────────────────────────────────────────────────────────

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

    // ──────────────────────────────────────────────────────────────────────
    //  Shared helpers: formatting utilities
    // ──────────────────────────────────────────────────────────────────────

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
