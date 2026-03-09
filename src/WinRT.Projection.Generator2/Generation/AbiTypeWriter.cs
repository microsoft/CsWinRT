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
/// Writes the ABI (Application Binary Interface) types that handle marshaling between managed
/// and unmanaged code. This is a port of the ABI code generation logic from the C++ cswinrt
/// <c>code_writers.h</c>, producing marshaller classes, vtable structures, and
/// <c>[UnmanagedCallersOnly]</c> callbacks for each WinRT type category.
/// </summary>
internal static class AbiTypeWriter
{
    /// <summary>
    /// Main entry point that dispatches to specific writers based on <see cref="TypeCategory"/>.
    /// Called during Phase 3 of code generation to emit ABI types in the <c>ABI.{Namespace}</c> namespace.
    /// </summary>
    /// <param name="writer">The writer to output to.</param>
    /// <param name="type">The type definition to process.</param>
    /// <param name="category">The category of the type.</param>
    /// <param name="args">The projection generator arguments.</param>
    public static void WriteAbiType(CodeWriter writer, TypeDefinition type, TypeCategory category, ProjectionGeneratorArgs args)
    {
        switch (category)
        {
            case TypeCategory.Enum:
                WriteAbiEnum(writer, type, args);
                break;
            case TypeCategory.Struct:
                WriteAbiStruct(writer, type, args);
                break;
            case TypeCategory.Delegate:
                WriteAbiDelegate(writer, type, args);
                break;
            case TypeCategory.Interface:
                WriteAbiInterface(writer, type, args);
                break;
            case TypeCategory.Class:
                WriteAbiClass(writer, type, args);
                break;
            case TypeCategory.Attribute:
            case TypeCategory.ApiContract:
            default:
                break;
        }
    }

    /// <summary>
    /// Writes the ABI marshaller for an enum type, providing conversions between
    /// the projected enum and its underlying integer ABI representation.
    /// </summary>
    private static void WriteAbiEnum(CodeWriter writer, TypeDefinition type, ProjectionGeneratorArgs args)
    {
        _ = args;

        string name = TypeNameHelpers.GetSimpleName(type);
        string fullProjectedName = GetFullProjectedTypeName(type);
        string underlyingType = GetEnumUnderlyingType(type);

        writer.WriteLine();
        writer.WriteLine($"/// <summary>Marshaller for <see cref=\"{fullProjectedName}\"/>.</summary>");
        writer.WriteLine($"internal static class {name}Marshaller");

        using (writer.WriteBlock())
        {
            // ABI struct that holds the raw integer value
            writer.WriteLine("internal struct AbiType");

            using (writer.WriteBlock())
            {
                writer.WriteLine($"public {underlyingType} Value;");
            }

            writer.WriteLine();

            // ConvertToUnmanaged: projected enum → ABI integer
            writer.WriteLine($"public static AbiType ConvertToUnmanaged({fullProjectedName} value)");

            using (writer.WriteBlock())
            {
                writer.WriteLine($"return new AbiType {{ Value = ({underlyingType})value }};");
            }

            writer.WriteLine();

            // ConvertToManaged: ABI integer → projected enum
            writer.WriteLine($"public static {fullProjectedName} ConvertToManaged(AbiType value)");

            using (writer.WriteBlock())
            {
                writer.WriteLine($"return ({fullProjectedName})value.Value;");
            }
        }
    }

    /// <summary>
    /// Writes the ABI marshaller for a struct type, converting between
    /// projected struct fields and their ABI representations (e.g., strings → <c>IntPtr</c>).
    /// </summary>
    private static void WriteAbiStruct(CodeWriter writer, TypeDefinition type, ProjectionGeneratorArgs args)
    {
        _ = args;

        string name = TypeNameHelpers.GetSimpleName(type);
        string fullProjectedName = GetFullProjectedTypeName(type);

        // Collect fields and their ABI types
        List<(string Name, string ProjectedType, string AbiType, bool RequiresMarshaling)> fields = [];

        foreach (FieldDefinition field in type.Fields)
        {
            if (field.IsStatic || field.IsSpecialName)
            {
                continue;
            }

            string fieldName = CSharpKeywords.EscapeIdentifier(field.Name?.Value ?? "");
            string projectedType = GetProjectedTypeName(field.Signature?.FieldType);
            string abiType = GetAbiTypeName(field.Signature?.FieldType);
            bool requiresMarshaling = projectedType != abiType;

            fields.Add((fieldName, projectedType, abiType, requiresMarshaling));
        }

        bool hasNonBlittableFields = false;

        foreach ((_, _, _, bool req) in fields)
        {
            if (req)
            {
                hasNonBlittableFields = true;
                break;
            }
        }

        writer.WriteLine();
        writer.WriteLine($"/// <summary>Marshaller for <see cref=\"{fullProjectedName}\"/>.</summary>");
        writer.WriteLine($"internal static class {name}Marshaller");

        using (writer.WriteBlock())
        {
            // ABI struct with marshaled field types
            writer.WriteLine("internal struct AbiType");

            using (writer.WriteBlock())
            {
                foreach ((string fieldName, _, string abiType, _) in fields)
                {
                    writer.WriteLine($"public {abiType} {fieldName};");
                }
            }

            writer.WriteLine();

            // ConvertToUnmanaged
            writer.WriteLine($"public static AbiType ConvertToUnmanaged({fullProjectedName} value)");

            using (writer.WriteBlock())
            {
                writer.WriteLine("return new AbiType");

                using (writer.WriteBlock())
                {
                    foreach ((string fieldName, _, string abiType, bool requiresMarshaling) in fields)
                    {
                        if (requiresMarshaling && abiType == "global::System.IntPtr")
                        {
                            // String fields: marshal via WindowsRuntimeStringMarshaller
                            writer.WriteLine($"{fieldName} = global::System.Runtime.InteropServices.Marshalling.WindowsRuntimeStringMarshaller.ConvertToUnmanaged(value.{fieldName}),");
                        }
                        else
                        {
                            writer.WriteLine($"{fieldName} = value.{fieldName},");
                        }
                    }
                }

                writer.Write(";");
                writer.WriteLine();
            }

            writer.WriteLine();

            // ConvertToManaged
            writer.WriteLine($"public static {fullProjectedName} ConvertToManaged(AbiType value)");

            using (writer.WriteBlock())
            {
                writer.WriteLine($"return new {fullProjectedName}");

                using (writer.WriteBlock())
                {
                    foreach ((string fieldName, _, string abiType, bool requiresMarshaling) in fields)
                    {
                        if (requiresMarshaling && abiType == "global::System.IntPtr")
                        {
                            writer.WriteLine($"{fieldName} = global::System.Runtime.InteropServices.Marshalling.WindowsRuntimeStringMarshaller.ConvertToManaged(value.{fieldName}),");
                        }
                        else
                        {
                            writer.WriteLine($"{fieldName} = value.{fieldName},");
                        }
                    }
                }

                writer.Write(";");
                writer.WriteLine();
            }

            // Free method: only needed if any field requires marshaling
            if (hasNonBlittableFields)
            {
                writer.WriteLine();
                writer.WriteLine("public static void Free(AbiType value)");

                using (writer.WriteBlock())
                {
                    foreach ((string fieldName, _, string abiType, bool requiresMarshaling) in fields)
                    {
                        if (requiresMarshaling && abiType == "global::System.IntPtr")
                        {
                            writer.WriteLine($"global::System.Runtime.InteropServices.Marshalling.WindowsRuntimeStringMarshaller.Free(value.{fieldName});");
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Writes the ABI helper for a delegate type, including a vtable structure
    /// and an <c>[UnmanagedCallersOnly]</c> invoke callback for CCW support.
    /// </summary>
    private static void WriteAbiDelegate(CodeWriter writer, TypeDefinition type, ProjectionGeneratorArgs args)
    {
        _ = args;

        string name = TypeNameHelpers.GetSimpleName(type);
        string fullProjectedName = GetFullProjectedTypeName(type);

        MethodDefinition? invokeMethod = TypeHelpers.GetDelegateInvoke(type);

        if (invokeMethod?.Signature is not { } methodSig)
        {
            return;
        }

        string returnType = GetProjectedTypeName(methodSig.ReturnType);
        bool hasReturnValue = returnType != "void";

        writer.WriteLine();
        writer.WriteLine($"/// <summary>ABI helper for the <see cref=\"{fullProjectedName}\"/> delegate.</summary>");
        writer.WriteLine($"internal static unsafe class {name}Abi");

        using (writer.WriteBlock())
        {
            // Vtable field
            writer.WriteLine("/// <summary>The static vtable pointer for this delegate's CCW.</summary>");
            writer.WriteLine("private static readonly void* Vtable;");
            writer.WriteLine();

            // Static constructor to initialize the vtable
            writer.WriteLine($"static {name}Abi()");

            using (writer.WriteBlock())
            {
                writer.WriteLine("// Vtable initialization is deferred to runtime type registration.");
            }

            writer.WriteLine();

            // UnmanagedCallersOnly invoke stub
            writer.WriteLine("/// <summary>ABI invoke callback for native-to-managed delegate calls.</summary>");
            writer.WriteLine("[global::System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = [typeof(global::System.Runtime.CompilerServices.CallConvMemberFunction)])]");

            // Build ABI parameter list
            string abiParams = BuildAbiParameterList(invokeMethod, includeThisPtr: true, includeReturnParam: hasReturnValue);
            writer.WriteLine($"private static int Do_Abi_Invoke({abiParams})");

            using (writer.WriteBlock())
            {
                writer.WriteLine("try");

                using (writer.WriteBlock())
                {
                    // Marshal parameters and invoke
                    WriteDelegateInvokeBody(writer, invokeMethod, methodSig, fullProjectedName);
                    writer.WriteLine("return 0; // S_OK");
                }

                writer.WriteLine("catch (global::System.Exception __e)");

                using (writer.WriteBlock())
                {
                    writer.WriteLine("return global::System.Runtime.InteropServices.Marshalling.RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(__e);");
                }
            }
        }
    }

    /// <summary>
    /// Writes the ABI static methods class and vtable for an interface type.
    /// The methods class provides managed wrappers that call through the vtable,
    /// and the vtable class provides <c>[UnmanagedCallersOnly]</c> callbacks for CCW support.
    /// </summary>
    private static void WriteAbiInterface(CodeWriter writer, TypeDefinition type, ProjectionGeneratorArgs args)
    {
        _ = args;

        string name = TypeNameHelpers.GetSimpleName(type);
        string fullProjectedName = GetFullProjectedTypeName(type);

        // Collect property and event accessor names to avoid duplicating them as regular methods
        HashSet<string> specialMethodNames = CollectSpecialMethodNames(type);

        writer.WriteLine();
        writer.WriteLine($"/// <summary>ABI methods for <see cref=\"{fullProjectedName}\"/>.</summary>");
        writer.WriteLine($"internal static unsafe class {name}Methods");

        using (writer.WriteBlock())
        {
            // Vtable slot index counter: IUnknown has 3 methods (QueryInterface, AddRef, Release),
            // and IInspectable adds 3 more (GetIids, GetRuntimeClassName, GetTrustLevel).
            int vtableSlotIndex = 6;

            // Write ABI methods for interface members
            foreach (MethodDefinition method in type.Methods)
            {
                if (method.IsSpecialName || method.IsRuntimeSpecialName)
                {
                    vtableSlotIndex++;
                    continue;
                }

                string? methodName = method.Name?.Value;

                if (methodName is null || specialMethodNames.Contains(methodName))
                {
                    vtableSlotIndex++;
                    continue;
                }

                WriteAbiMethodWrapper(writer, method, vtableSlotIndex);
                vtableSlotIndex++;
            }

            // Write ABI methods for properties
            foreach (PropertyDefinition property in type.Properties)
            {
                (MethodDefinition? getter, MethodDefinition? setter) = TypeHelpers.GetPropertyMethods(property);

                if (getter is not null)
                {
                    WriteAbiMethodWrapper(writer, getter, vtableSlotIndex);
                    vtableSlotIndex++;
                }

                if (setter is not null)
                {
                    WriteAbiMethodWrapper(writer, setter, vtableSlotIndex);
                    vtableSlotIndex++;
                }
            }

            // Write ABI methods for events
            foreach (EventDefinition evt in type.Events)
            {
                (MethodDefinition? add, MethodDefinition? remove) = TypeHelpers.GetEventMethods(evt);

                if (add is not null)
                {
                    WriteAbiMethodWrapper(writer, add, vtableSlotIndex);
                    vtableSlotIndex++;
                }

                if (remove is not null)
                {
                    WriteAbiMethodWrapper(writer, remove, vtableSlotIndex);
                    vtableSlotIndex++;
                }
            }
        }

        // Write the vtable class (file-scoped, not accessible outside this compilation unit)
        writer.WriteLine();
        writer.WriteLine($"/// <summary>Vtable for <see cref=\"{fullProjectedName}\"/> CCW support.</summary>");
        writer.WriteLine($"file static unsafe class {name}Vtable");

        using (writer.WriteBlock())
        {
            writer.WriteLine("[global::System.Runtime.CompilerServices.FixedAddressValueType]");
            writer.WriteLine("private static readonly global::System.IntPtr Vftbl;");
            writer.WriteLine();

            writer.WriteLine($"static {name}Vtable()");

            using (writer.WriteBlock())
            {
                writer.WriteLine("// Vtable function pointer initialization is deferred to runtime type registration.");
            }
        }
    }

    /// <summary>
    /// Writes the ABI helper for a runtime class type, including factory method wrappers
    /// and static interface method forwarders.
    /// </summary>
    private static void WriteAbiClass(CodeWriter writer, TypeDefinition type, ProjectionGeneratorArgs args)
    {
        _ = args;

        string name = TypeNameHelpers.GetSimpleName(type);
        string fullProjectedName = GetFullProjectedTypeName(type);
        bool isStatic = TypeHelpers.IsStaticClass(type);

        writer.WriteLine();
        writer.WriteLine($"/// <summary>ABI helper for <see cref=\"{fullProjectedName}\"/>.</summary>");
        writer.WriteLine($"internal static class {name}Abi");

        using (writer.WriteBlock())
        {
            if (!isStatic && TypeHelpers.HasDefaultConstructor(type))
            {
                // Activation factory method for default constructor
                writer.WriteLine("/// <summary>Activates a new instance via the activation factory.</summary>");
                writer.WriteLine($"public static unsafe void* ActivateInstance()");

                using (writer.WriteBlock())
                {
                    writer.WriteLine("throw new global::System.NotImplementedException();");
                }
            }

            // Static interface forwarders
            foreach (InterfaceImplementation interfaceImpl in type.Interfaces)
            {
                if (interfaceImpl.Interface is not { } interfaceRef)
                {
                    continue;
                }

                // Only process static interfaces (marked with StaticAttribute on the class)
                if (!TypeHelpers.IsDefaultInterface(interfaceImpl) && !TypeHelpers.IsOverridable(interfaceImpl))
                {
                    TypeDefinition? interfaceDef = interfaceRef.Resolve();

                    if (interfaceDef is null)
                    {
                        continue;
                    }

                    // Write a comment indicating the static interface source
                    string interfaceName = TypeNameHelpers.GetSimpleName(interfaceDef);
                    writer.WriteLine();
                    writer.WriteLine($"// Static members from {interfaceName}");
                }
            }
        }
    }

    /// <summary>
    /// Writes a managed wrapper method that calls into native code through a vtable slot.
    /// </summary>
    private static void WriteAbiMethodWrapper(CodeWriter writer, MethodDefinition method, int vtableSlotIndex)
    {
        if (method.Signature is not { } methodSig)
        {
            return;
        }

        string returnType = GetProjectedTypeName(methodSig.ReturnType);
        bool hasReturnValue = returnType != "void";
        string methodName = CSharpKeywords.EscapeIdentifier(method.Name?.Value ?? "");

        // Build projected parameter list (for the public-facing signature)
        string projectedParams = BuildProjectedParameterList(method);

        writer.WriteLine();
        writer.WriteLine($"/// <summary>Calls the ABI method at vtable slot {vtableSlotIndex}.</summary>");

        string fullParams = string.IsNullOrEmpty(projectedParams)
            ? "global::WindowsRuntime.WindowsRuntimeObjectReference thisReference"
            : $"global::WindowsRuntime.WindowsRuntimeObjectReference thisReference, {projectedParams}";

        writer.WriteLine($"public static unsafe {returnType} {methodName}({fullParams})");

        using (writer.WriteBlock())
        {
            writer.WriteLine("using global::WindowsRuntime.WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();");
            writer.WriteLine("void* thisPtr = thisValue.GetThisPtrUnsafe();");

            if (hasReturnValue)
            {
                string abiReturnType = GetAbiTypeName(methodSig.ReturnType);
                writer.WriteLine($"{abiReturnType} __retval = default;");
            }

            writer.WriteLine();

            // Call the native method through the vtable
            writer.WriteLine($"// Native call via vtable slot {vtableSlotIndex}");
            writer.WriteLine("global::System.Runtime.InteropServices.Marshal.ThrowExceptionForHR(");
            writer.IncreaseIndent();

            // Build the native call arguments
            string nativeCallArgs = BuildNativeCallArguments(method, hasReturnValue);
            writer.WriteLine($"((delegate* unmanaged[MemberFunction]<void*, {GetNativeSignature(method, hasReturnValue)}>)");
            writer.IncreaseIndent();
            writer.WriteLine($"(*(void***)thisPtr)[{vtableSlotIndex}])(thisPtr{nativeCallArgs}));");
            writer.DecreaseIndent();
            writer.DecreaseIndent();

            if (hasReturnValue)
            {
                writer.WriteLine();

                string abiReturnType = GetAbiTypeName(methodSig.ReturnType);

                if (abiReturnType != returnType)
                {
                    // Return value needs marshaling
                    writer.WriteLine($"return {GetMarshalToManagedExpression(methodSig.ReturnType, "__retval")};");
                }
                else
                {
                    writer.WriteLine("return __retval;");
                }
            }
        }
    }

    /// <summary>
    /// Writes the body of a delegate's <c>Do_Abi_Invoke</c> method that marshals
    /// parameters and invokes the managed delegate.
    /// </summary>
    private static void WriteDelegateInvokeBody(
        CodeWriter writer,
        MethodDefinition invokeMethod,
        MethodSignature methodSig,
        string fullProjectedName)
    {
        string returnType = GetProjectedTypeName(methodSig.ReturnType);
        bool hasReturnValue = returnType != "void";

        // Build the argument list for the managed delegate call
        List<string> managedArgs = [];

        for (int i = 0; i < methodSig.ParameterTypes.Count; i++)
        {
            TypeSignature paramType = methodSig.ParameterTypes[i];
            string paramName = GetParameterName(invokeMethod, i);
            string abiType = GetAbiTypeName(paramType);
            string projectedType = GetProjectedTypeName(paramType);

            if (abiType != projectedType)
            {
                managedArgs.Add(GetMarshalToManagedExpression(paramType, paramName));
            }
            else
            {
                managedArgs.Add(paramName);
            }
        }

        string argsJoined = string.Join(", ", managedArgs);

        if (hasReturnValue)
        {
            writer.WriteLine($"var __managed = global::System.Runtime.InteropServices.ComWrappers.ComInterfaceDispatch.GetInstance<{fullProjectedName}>((global::System.Runtime.InteropServices.ComWrappers.ComInterfaceDispatch*)thisPtr);");
            writer.WriteLine($"var __result = __managed({argsJoined});");

            string abiReturnType = GetAbiTypeName(methodSig.ReturnType);

            if (abiReturnType != GetProjectedTypeName(methodSig.ReturnType))
            {
                writer.WriteLine($"*retval = {GetMarshalToUnmanagedExpression(methodSig.ReturnType, "__result")};");
            }
            else
            {
                writer.WriteLine("*retval = __result;");
            }
        }
        else
        {
            writer.WriteLine($"var __managed = global::System.Runtime.InteropServices.ComWrappers.ComInterfaceDispatch.GetInstance<{fullProjectedName}>((global::System.Runtime.InteropServices.ComWrappers.ComInterfaceDispatch*)thisPtr);");
            writer.WriteLine($"__managed({argsJoined});");
        }
    }

    #region Type name helpers

    /// <summary>
    /// Gets the fully qualified projected type name suitable for use in generated ABI code
    /// (references the type from the projected namespace, not the ABI namespace).
    /// </summary>
    private static string GetFullProjectedTypeName(TypeDefinition type)
    {
        string? ns = type.Namespace?.Value;
        string name = TypeNameHelpers.GetSimpleName(type);

        return string.IsNullOrEmpty(ns)
            ? $"global::{name}"
            : $"global::{ns}.{name}";
    }

    /// <summary>
    /// Gets the projected C# type name for a type signature (mirrors <c>ProjectedTypeWriter.GetProjectedTypeName</c>).
    /// </summary>
    private static string GetProjectedTypeName(TypeSignature? typeSig)
    {
        if (typeSig is null)
        {
            return "void";
        }

        if (typeSig is CorLibTypeSignature corLibType)
        {
            string fullName = $"{corLibType.Namespace}.{corLibType.Name}";
            string? csharpName = TypeNameHelpers.GetCSharpTypeName(fullName);

            return csharpName ?? $"global::{fullName}";
        }

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
                string fullName = $"{ns}.{typeName}";
                string? csharpName = TypeNameHelpers.GetCSharpTypeName(fullName);

                if (csharpName is not null)
                {
                    return csharpName;
                }

                TypeMappings.MappedType? mapping = TypeMappings.GetMappedType(ns, typeName);

                if (mapping is { MappedNamespace: not null, MappedName: not null })
                {
                    string mappedName = RemoveGenericArity(mapping.Value.MappedName);

                    return $"global::{mapping.Value.MappedNamespace}.{mappedName}";
                }
            }

            return TypeNameHelpers.GetProjectedTypeName(referencedType);
        }

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

            List<string> typeArgs = [];

            foreach (TypeSignature typeArg in genericInstance.TypeArguments)
            {
                typeArgs.Add(GetProjectedTypeName(typeArg));
            }

            return $"{baseName}<{string.Join(", ", typeArgs)}>";
        }

        if (typeSig is SzArrayTypeSignature szArray)
        {
            string elementType = GetProjectedTypeName(szArray.BaseType);

            return $"{elementType}[]";
        }

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

        string? fallbackFullName = typeSig.FullName;

        if (fallbackFullName is not null)
        {
            string? csharpFallbackName = TypeNameHelpers.GetCSharpTypeName(fallbackFullName);

            return csharpFallbackName ?? $"global::{fallbackFullName}";
        }

        return "object";
    }

    /// <summary>
    /// Gets the ABI type name for a type signature. This maps projected types
    /// to their unmanaged representations (e.g., <c>string</c> → <c>IntPtr</c>,
    /// interface/class → <c>void*</c>, enum → underlying integer).
    /// </summary>
    private static string GetAbiTypeName(TypeSignature? typeSig)
    {
        if (typeSig is null)
        {
            return "void";
        }

        // Fundamental (blittable) types stay the same in ABI
        if (typeSig is CorLibTypeSignature corLibType)
        {
            string fullName = $"{corLibType.Namespace}.{corLibType.Name}";

            // String is not blittable - it marshals to IntPtr (HSTRING)
            if (fullName == "System.String")
            {
                return "global::System.IntPtr";
            }

            // System.Object marshals to void* (IInspectable*)
            if (fullName == "System.Object")
            {
                return "void*";
            }

            // All other fundamental types (int, float, bool, etc.) are blittable
            string? csharpName = TypeNameHelpers.GetCSharpTypeName(fullName);

            return csharpName ?? $"global::{fullName}";
        }

        if (typeSig is TypeDefOrRefSignature typeDefOrRef)
        {
            ITypeDefOrRef? referencedType = typeDefOrRef.Type;

            if (referencedType is null)
            {
                return "void*";
            }

            TypeDefinition? resolved = referencedType.Resolve();

            if (resolved is not null)
            {
                string? baseFullName = resolved.BaseType?.FullName;

                // Enums marshal to their underlying integer type
                if (baseFullName == "System.Enum")
                {
                    return GetEnumUnderlyingType(resolved);
                }

                // Structs: check if any field requires marshaling
                if (baseFullName == "System.ValueType")
                {
                    return GetAbiStructTypeName(resolved);
                }

                // Delegates and interfaces marshal to void*
                if (baseFullName == "System.MulticastDelegate" || resolved.IsInterface)
                {
                    return "void*";
                }
            }

            // Classes marshal to void*
            return "void*";
        }

        // Generic instances marshal to void* (they are always reference types in WinRT)
        if (typeSig is GenericInstanceTypeSignature)
        {
            return "void*";
        }

        // Arrays marshal to IntPtr
        if (typeSig is SzArrayTypeSignature)
        {
            return "global::System.IntPtr";
        }

        // By-reference: get the ABI type of the underlying type
        return typeSig is ByReferenceTypeSignature byRef
            ? GetAbiTypeName(byRef.BaseType)
            : "void*";
    }

    /// <summary>
    /// Gets the ABI struct type name. If the struct has only blittable fields,
    /// uses the projected name directly; otherwise, references the marshaller's <c>AbiType</c>.
    /// </summary>
    private static string GetAbiStructTypeName(TypeDefinition structType)
    {
        foreach (FieldDefinition field in structType.Fields)
        {
            if (field.IsStatic || field.IsSpecialName)
            {
                continue;
            }

            string projectedType = GetProjectedTypeName(field.Signature?.FieldType);
            string abiType = GetAbiTypeName(field.Signature?.FieldType);

            if (projectedType != abiType)
            {
                // Non-blittable struct: reference the marshaller's AbiType
                string? ns = structType.Namespace?.Value;
                string simpleName = TypeNameHelpers.GetSimpleName(structType);

                return string.IsNullOrEmpty(ns)
                    ? $"global::ABI.{simpleName}Marshaller.AbiType"
                    : $"global::ABI.{ns}.{simpleName}Marshaller.AbiType";
            }
        }

        // All fields are blittable: use the projected type directly
        return GetFullProjectedTypeName(structType);
    }

    /// <summary>
    /// Gets the underlying C# type for a WinRT enum (<c>int</c> or <c>uint</c>).
    /// </summary>
    private static string GetEnumUnderlyingType(TypeDefinition enumType)
    {
        foreach (FieldDefinition field in enumType.Fields)
        {
            if (field.IsRuntimeSpecialName && field.Name == "value__")
            {
                string? fullTypeName = field.Signature?.FieldType?.FullName;

                return fullTypeName == "System.UInt32" ? "uint" : "int";
            }
        }

        return "int";
    }

    #endregion

    #region Parameter and signature helpers

    /// <summary>
    /// Builds the ABI parameter list for an <c>[UnmanagedCallersOnly]</c> method.
    /// Includes <c>void* thisPtr</c> and optionally a return-value out parameter.
    /// </summary>
    private static string BuildAbiParameterList(MethodDefinition method, bool includeThisPtr, bool includeReturnParam)
    {
        List<string> parts = [];

        if (includeThisPtr)
        {
            parts.Add("void* thisPtr");
        }

        if (method.Signature is { } methodSig)
        {
            for (int i = 0; i < methodSig.ParameterTypes.Count; i++)
            {
                TypeSignature paramType = methodSig.ParameterTypes[i];
                string paramName = GetParameterName(method, i);
                string abiType = GetAbiTypeName(paramType);

                if (paramType is ByReferenceTypeSignature)
                {
                    parts.Add($"{abiType}* {paramName}");
                }
                else
                {
                    parts.Add($"{abiType} {paramName}");
                }
            }

            if (includeReturnParam && methodSig.ReturnType is not null)
            {
                string returnAbiType = GetAbiTypeName(methodSig.ReturnType);

                if (returnAbiType != "void")
                {
                    parts.Add($"{returnAbiType}* retval");
                }
            }
        }

        return string.Join(", ", parts);
    }

    /// <summary>
    /// Builds the projected (managed) parameter list for an ABI method wrapper.
    /// </summary>
    private static string BuildProjectedParameterList(MethodDefinition method)
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

            if (paramType is ByReferenceTypeSignature byRef)
            {
                typeName = GetProjectedTypeName(byRef.BaseType);
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
    /// Builds the native function pointer signature string (the type parameters for
    /// <c>delegate* unmanaged[MemberFunction]</c>).
    /// </summary>
    private static string GetNativeSignature(MethodDefinition method, bool hasReturnValue)
    {
        List<string> types = [];

        if (method.Signature is { } methodSig)
        {
            for (int i = 0; i < methodSig.ParameterTypes.Count; i++)
            {
                TypeSignature paramType = methodSig.ParameterTypes[i];
                string abiType = GetAbiTypeName(paramType);

                if (paramType is ByReferenceTypeSignature)
                {
                    types.Add($"{abiType}*");
                }
                else
                {
                    types.Add(abiType);
                }
            }

            if (hasReturnValue && methodSig.ReturnType is not null)
            {
                string returnAbiType = GetAbiTypeName(methodSig.ReturnType);

                if (returnAbiType != "void")
                {
                    types.Add($"{returnAbiType}*");
                }
            }
        }

        // The last type is always the HRESULT return
        types.Add("int");

        return string.Join(", ", types);
    }

    /// <summary>
    /// Builds the native call argument list (excludes the <c>thisPtr</c> which is written separately).
    /// </summary>
    private static string BuildNativeCallArguments(MethodDefinition method, bool hasReturnValue)
    {
        List<string> args = [];

        if (method.Signature is { } methodSig)
        {
            for (int i = 0; i < methodSig.ParameterTypes.Count; i++)
            {
                TypeSignature paramType = methodSig.ParameterTypes[i];
                string paramName = GetParameterName(method, i);
                string projectedType = GetProjectedTypeName(paramType);
                string abiType = GetAbiTypeName(paramType);

                if (paramType is ByReferenceTypeSignature)
                {
                    // Out/ref parameters are passed as pointers
                    string managedParamName = CSharpKeywords.EscapeIdentifier(paramName);

                    args.Add($"&{managedParamName}");
                }
                else if (abiType != projectedType)
                {
                    // Needs marshaling
                    string managedParamName = CSharpKeywords.EscapeIdentifier(paramName);
                    args.Add(GetMarshalToUnmanagedExpression(paramType, managedParamName));
                }
                else
                {
                    args.Add(CSharpKeywords.EscapeIdentifier(paramName));
                }
            }

            if (hasReturnValue)
            {
                args.Add("&__retval");
            }
        }

        return args.Count > 0
            ? $", {string.Join(", ", args)}"
            : "";
    }

    #endregion

    #region Marshaling expression helpers

    /// <summary>
    /// Gets a C# expression to marshal a value from unmanaged (ABI) to managed (projected) representation.
    /// </summary>
    private static string GetMarshalToManagedExpression(TypeSignature? typeSig, string variableName)
    {
        if (typeSig is null)
        {
            return variableName;
        }

        if (typeSig is CorLibTypeSignature corLibType)
        {
            string fullName = $"{corLibType.Namespace}.{corLibType.Name}";

            if (fullName == "System.String")
            {
                return $"global::System.Runtime.InteropServices.Marshalling.WindowsRuntimeStringMarshaller.ConvertToManaged({variableName})";
            }
        }

        if (typeSig is TypeDefOrRefSignature typeDefOrRef)
        {
            TypeDefinition? resolved = typeDefOrRef.Type?.Resolve();

            if (resolved is not null && resolved.BaseType?.FullName == "System.Enum")
            {
                string projectedType = GetProjectedTypeName(typeSig);

                return $"({projectedType}){variableName}";
            }
        }

        return variableName;
    }

    /// <summary>
    /// Gets a C# expression to marshal a value from managed (projected) to unmanaged (ABI) representation.
    /// </summary>
    private static string GetMarshalToUnmanagedExpression(TypeSignature? typeSig, string variableName)
    {
        if (typeSig is null)
        {
            return variableName;
        }

        if (typeSig is CorLibTypeSignature corLibType)
        {
            string fullName = $"{corLibType.Namespace}.{corLibType.Name}";

            if (fullName == "System.String")
            {
                return $"global::System.Runtime.InteropServices.Marshalling.WindowsRuntimeStringMarshaller.ConvertToUnmanaged({variableName})";
            }
        }

        if (typeSig is TypeDefOrRefSignature typeDefOrRef)
        {
            TypeDefinition? resolved = typeDefOrRef.Type?.Resolve();

            if (resolved is not null && resolved.BaseType?.FullName == "System.Enum")
            {
                string abiType = GetEnumUnderlyingType(resolved);

                return $"({abiType}){variableName}";
            }
        }

        return variableName;
    }

    #endregion

    #region Shared utility helpers

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
    /// Removes the generic arity suffix from a type name (e.g., <c>IList`1</c> → <c>IList</c>).
    /// </summary>
    private static string RemoveGenericArity(string typeName)
    {
        int backtickIndex = typeName.IndexOf('`');

        return backtickIndex >= 0 ? typeName[..backtickIndex] : typeName;
    }

    /// <summary>
    /// Collects the names of all property and event accessor methods so they
    /// can be skipped when iterating regular methods.
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

    #endregion
}
