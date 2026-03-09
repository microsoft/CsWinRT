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

    #region Enum

    /// <summary>
    /// Writes a simple enum marshaller that casts between the projected enum and its underlying integer type.
    /// </summary>
    private static void WriteAbiEnum(CodeWriter writer, TypeDefinition type, ProjectionGeneratorArgs args)
    {
        _ = args;

        string name = TypeNameHelpers.GetSimpleName(type);
        string fullProjectedName = GetFullProjectedTypeName(type);
        string underlyingType = GetEnumUnderlyingType(type);

        writer.WriteLine();
        writer.WriteLine($"public static class {name}Marshaller");

        using (writer.WriteBlock())
        {
            writer.WriteLine($"public static {underlyingType} ConvertToUnmanaged({fullProjectedName} value)");

            using (writer.WriteBlock())
            {
                writer.WriteLine($"return ({underlyingType})value;");
            }

            writer.WriteLine();

            writer.WriteLine($"public static {fullProjectedName} ConvertToManaged({underlyingType} value)");

            using (writer.WriteBlock())
            {
                writer.WriteLine($"return ({fullProjectedName})value;");
            }
        }
    }

    #endregion

    #region Struct

    /// <summary>
    /// Writes the ABI marshaller for a struct type, converting between
    /// projected struct fields and their ABI representations.
    /// </summary>
    private static void WriteAbiStruct(CodeWriter writer, TypeDefinition type, ProjectionGeneratorArgs args)
    {
        _ = args;

        string name = TypeNameHelpers.GetSimpleName(type);
        string fullProjectedName = GetFullProjectedTypeName(type);

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

        if (!hasNonBlittableFields)
        {
            // Blittable struct: no marshaller needed
            return;
        }

        writer.WriteLine();
        writer.WriteLine($"public static class {name}Marshaller");

        using (writer.WriteBlock())
        {
            writer.WriteLine("[global::System.Runtime.InteropServices.StructLayout(global::System.Runtime.InteropServices.LayoutKind.Sequential)]");
            writer.WriteLine("public struct AbiType");

            using (writer.WriteBlock())
            {
                foreach ((string fieldName, _, string abiType, _) in fields)
                {
                    writer.WriteLine($"public {abiType} {fieldName};");
                }
            }

            writer.WriteLine();

            writer.WriteLine($"public static AbiType ConvertToUnmanaged({fullProjectedName} value)");

            using (writer.WriteBlock())
            {
                writer.WriteLine("return new AbiType");

                using (writer.WriteBlock())
                {
                    foreach ((string fieldName, _, string abiType, bool requiresMarshaling) in fields)
                    {
                        if (requiresMarshaling)
                        {
                            writer.WriteLine($"{fieldName} = {GetFieldMarshalToUnmanagedExpression(abiType, $"value.{fieldName}")},");
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

            writer.WriteLine($"public static {fullProjectedName} ConvertToManaged(AbiType value)");

            using (writer.WriteBlock())
            {
                writer.WriteLine($"return new {fullProjectedName}");

                using (writer.WriteBlock())
                {
                    foreach ((string fieldName, _, string abiType, bool requiresMarshaling) in fields)
                    {
                        if (requiresMarshaling)
                        {
                            writer.WriteLine($"{fieldName} = {GetFieldMarshalToManagedExpression(abiType, $"value.{fieldName}")},");
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

            writer.WriteLine("public static void Free(AbiType value)");

            using (writer.WriteBlock())
            {
                foreach ((string fieldName, _, string abiType, bool requiresMarshaling) in fields)
                {
                    if (requiresMarshaling)
                    {
                        WriteFieldFree(writer, abiType, $"value.{fieldName}");
                    }
                }
            }
        }
    }

    #endregion

    #region Delegate

    /// <summary>
    /// Writes the ABI marshaller, ComWrappers callback, vtable, and impl for a delegate type.
    /// </summary>
    private static void WriteAbiDelegate(CodeWriter writer, TypeDefinition type, ProjectionGeneratorArgs args)
    {
        _ = args;

        string name = TypeNameHelpers.GetSimpleName(type);
        string fullProjectedName = GetFullProjectedTypeName(type);
        string iidFieldName = GetIIDFieldReference(type);

        MethodDefinition? invokeMethod = TypeHelpers.GetDelegateInvoke(type);

        if (invokeMethod?.Signature is not { } methodSig)
        {
            return;
        }

        // --- Marshaller ---
        writer.WriteLine();
        writer.WriteLine($"public static unsafe class {name}Marshaller");

        using (writer.WriteBlock())
        {
            writer.WriteLine($"public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged({fullProjectedName} value)");

            using (writer.WriteBlock())
            {
                writer.WriteLine($"return WindowsRuntimeDelegateMarshaller.ConvertToUnmanaged(value, in {iidFieldName});");
            }

            writer.WriteLine();

            writer.WriteLine($"public static {fullProjectedName}? ConvertToManaged(void* value)");

            using (writer.WriteBlock())
            {
                writer.WriteLine($"return ({fullProjectedName}?)WindowsRuntimeDelegateMarshaller.ConvertToManaged<{name}ComWrappersCallback>(value);");
            }
        }

        // --- ComWrappersMarshallerAttribute ---
        writer.WriteLine();
        writer.WriteLine($"file sealed unsafe class {name}ComWrappersMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute");

        using (writer.WriteBlock())
        {
            writer.WriteLine("public override object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)");

            using (writer.WriteBlock())
            {
                writer.WriteLine("WindowsRuntimeObjectReference valueReference = WindowsRuntimeComWrappersMarshal.CreateObjectReference(");
                writer.IncreaseIndent();
                writer.WriteLine("externalComObject: value,");
                writer.WriteLine($"iid: {iidFieldName},");
                writer.WriteLine("wrapperFlags: out wrapperFlags);");
                writer.DecreaseIndent();
                writer.WriteLine($"return WindowsRuntimeDelegateMarshal.CreateDelegate<{fullProjectedName}>(valueReference);");
            }
        }

        // --- ComWrappersCallback ---
        writer.WriteLine();
        writer.WriteLine($"file sealed unsafe class {name}ComWrappersCallback : IWindowsRuntimeObjectComWrappersCallback");

        using (writer.WriteBlock())
        {
            writer.WriteLine("public static object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)");

            using (writer.WriteBlock())
            {
                writer.WriteLine("WindowsRuntimeObjectReference valueReference = WindowsRuntimeComWrappersMarshal.CreateObjectReferenceUnsafe(");
                writer.IncreaseIndent();
                writer.WriteLine("externalComObject: value,");
                writer.WriteLine($"iid: {iidFieldName},");
                writer.WriteLine("wrapperFlags: out wrapperFlags);");
                writer.DecreaseIndent();
                writer.WriteLine($"return WindowsRuntimeDelegateMarshal.CreateDelegate<{fullProjectedName}>(valueReference);");
            }
        }

        // --- Vftbl ---
        WriteDelegateVftbl(writer, name, invokeMethod, methodSig);

        // --- Impl ---
        WriteDelegateImpl(writer, name, fullProjectedName, type, invokeMethod, methodSig);
    }

    /// <summary>
    /// Writes the vtable struct for a delegate type. Delegates have IUnknown (3 slots) + Invoke (1 slot).
    /// </summary>
    private static void WriteDelegateVftbl(CodeWriter writer, string name, MethodDefinition invokeMethod, MethodSignature methodSig)
    {
        writer.WriteLine();
        writer.WriteLine("[global::System.Runtime.InteropServices.StructLayout(global::System.Runtime.InteropServices.LayoutKind.Sequential)]");
        writer.WriteLine($"internal unsafe struct {name}Vftbl");

        using (writer.WriteBlock())
        {
            writer.WriteLine("public delegate* unmanaged[MemberFunction]<void*, global::System.Guid*, void**, int> QueryInterface;");
            writer.WriteLine("public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;");
            writer.WriteLine("public delegate* unmanaged[MemberFunction]<void*, uint> Release;");

            string invokeSignature = BuildVftblFunctionPointerSignature(invokeMethod, methodSig);
            writer.WriteLine($"public {invokeSignature} Invoke_0;");
        }
    }
    /// <summary>
    /// Writes the Impl class for a delegate type with UnmanagedCallersOnly invoke callback.
    /// </summary>
    private static void WriteDelegateImpl(
        CodeWriter writer,
        string name,
        string fullProjectedName,
        TypeDefinition type,
        MethodDefinition invokeMethod,
        MethodSignature methodSig)
    {
        string iidFieldName = GetIIDFieldReference(type);

        writer.WriteLine();
        writer.WriteLine($"public static unsafe class {name}Impl");

        using (writer.WriteBlock())
        {
            writer.WriteLine("[global::System.Runtime.CompilerServices.FixedAddressValueType]");
            writer.WriteLine($"private static readonly {name}Vftbl Vftbl;");
            writer.WriteLine();

            writer.WriteLine($"static {name}Impl()");

            using (writer.WriteBlock())
            {
                writer.WriteLine($"*(IUnknownVftbl*)global::System.Runtime.CompilerServices.Unsafe.AsPointer(ref Vftbl) = *(IUnknownVftbl*)IUnknownImpl.Vtable;");
                writer.WriteLine("Vftbl.Invoke_0 = &Do_Abi_Invoke_0;");
            }

            writer.WriteLine();

            writer.WriteLine("public static ref readonly global::System.Guid IID");

            using (writer.WriteBlock())
            {
                writer.WriteLine("[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
                writer.WriteLine($"get => ref {iidFieldName};");
            }

            writer.WriteLine();

            writer.WriteLine("public static nint Vtable");

            using (writer.WriteBlock())
            {
                writer.WriteLine("[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
                writer.WriteLine("get => (nint)global::System.Runtime.CompilerServices.Unsafe.AsPointer(in Vftbl);");
            }

            writer.WriteLine();

            string abiParams = BuildAbiParameterList(invokeMethod, includeThisPtr: true, includeReturnParam: true);
            writer.WriteLine("[global::System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(global::System.Runtime.CompilerServices.CallConvMemberFunction) })]");
            writer.WriteLine($"private static unsafe int Do_Abi_Invoke_0({abiParams})");

            using (writer.WriteBlock())
            {
                writer.WriteLine("try");

                using (writer.WriteBlock())
                {
                    WriteDelegateInvokeBody(writer, invokeMethod, methodSig, fullProjectedName);
                    writer.WriteLine("return 0;");
                }

                writer.WriteLine("catch (global::System.Exception __exception__)");

                using (writer.WriteBlock())
                {
                    writer.WriteLine("return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(__exception__);");
                }
            }
        }
    }

    #endregion

    #region Interface

    /// <summary>
    /// Writes the ABI Methods class, Vftbl struct, and Impl class for an interface type.
    /// </summary>
    private static void WriteAbiInterface(CodeWriter writer, TypeDefinition type, ProjectionGeneratorArgs args)
    {
        _ = args;

        string name = TypeNameHelpers.GetSimpleName(type);
        string fullProjectedName = GetFullProjectedTypeName(type);

        List<(MethodDefinition Method, int SlotIndex)> vtableMethods = CollectVtableMethods(type);

        // --- Methods class ---
        writer.WriteLine();
        writer.WriteLine($"internal static class {name}Methods");

        using (writer.WriteBlock())
        {
            foreach ((MethodDefinition method, int slotIndex) in vtableMethods)
            {
                WriteAbiMethodWrapper(writer, method, slotIndex);
            }
        }

        // --- Vftbl struct ---
        WriteInterfaceVftbl(writer, name, vtableMethods);

        // --- Impl class ---
        WriteInterfaceImpl(writer, name, fullProjectedName, type, vtableMethods);
    }

    /// <summary>
    /// Collects all methods on an interface in their vtable order, assigning slot indices starting at 6.
    /// Methods are enumerated in metadata order: all MethodDef entries sequentially.
    /// </summary>
    private static List<(MethodDefinition Method, int SlotIndex)> CollectVtableMethods(TypeDefinition type)
    {
        List<(MethodDefinition, int)> result = [];
        int slotIndex = 6;

        foreach (MethodDefinition method in type.Methods)
        {
            if (method.Name?.Value is ".ctor" or ".cctor")
            {
                continue;
            }

            result.Add((method, slotIndex));
            slotIndex++;
        }

        return result;
    }

    /// <summary>
    /// Writes the Vftbl struct for an interface.
    /// </summary>
    private static void WriteInterfaceVftbl(
        CodeWriter writer,
        string name,
        List<(MethodDefinition Method, int SlotIndex)> vtableMethods)
    {
        writer.WriteLine();
        writer.WriteLine("[global::System.Runtime.InteropServices.StructLayout(global::System.Runtime.InteropServices.LayoutKind.Sequential)]");
        writer.WriteLine($"internal unsafe struct {name}Vftbl");

        using (writer.WriteBlock())
        {
            // IUnknown (slots 0-2)
            writer.WriteLine("public delegate* unmanaged[MemberFunction]<void*, global::System.Guid*, void**, int> QueryInterface;");
            writer.WriteLine("public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;");
            writer.WriteLine("public delegate* unmanaged[MemberFunction]<void*, uint> Release;");

            // IInspectable (slots 3-5)
            writer.WriteLine("public delegate* unmanaged[MemberFunction]<void*, uint*, global::System.Guid**, int> GetIids;");
            writer.WriteLine("public delegate* unmanaged[MemberFunction]<void*, void**, int> GetRuntimeClassName;");
            writer.WriteLine("public delegate* unmanaged[MemberFunction]<void*, int*, int> GetTrustLevel;");

            // Interface methods (slots 6+)
            foreach ((MethodDefinition method, int slotIndex) in vtableMethods)
            {
                if (method.Signature is not { } methodSig)
                {
                    continue;
                }

                string methodName = method.Name?.Value ?? $"Method{slotIndex}";
                string escapedName = SanitizeVftblMethodName(methodName);
                int methodIndex = slotIndex - 6;
                string signature = BuildVftblFunctionPointerSignature(method, methodSig);
                writer.WriteLine($"public {signature} {escapedName}_{methodIndex};");
            }
        }
    }

    /// <summary>
    /// Writes the Impl class for an interface with static Vftbl, IID, Vtable, and UnmanagedCallersOnly methods.
    /// </summary>
    private static void WriteInterfaceImpl(
        CodeWriter writer,
        string name,
        string fullProjectedName,
        TypeDefinition type,
        List<(MethodDefinition Method, int SlotIndex)> vtableMethods)
    {
        string iidFieldName = GetIIDFieldReference(type);

        writer.WriteLine();
        writer.WriteLine($"public static unsafe class {name}Impl");

        using (writer.WriteBlock())
        {
            writer.WriteLine("[global::System.Runtime.CompilerServices.FixedAddressValueType]");
            writer.WriteLine($"private static readonly {name}Vftbl Vftbl;");
            writer.WriteLine();

            writer.WriteLine($"static {name}Impl()");

            using (writer.WriteBlock())
            {
                writer.WriteLine($"*(IInspectableVftbl*)global::System.Runtime.CompilerServices.Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;");

                foreach ((MethodDefinition method, int slotIndex) in vtableMethods)
                {
                    string methodName = method.Name?.Value ?? $"Method{slotIndex}";
                    string escapedName = SanitizeVftblMethodName(methodName);
                    int methodIndex = slotIndex - 6;
                    writer.WriteLine($"Vftbl.{escapedName}_{methodIndex} = &Do_Abi_{escapedName}_{methodIndex};");
                }
            }

            writer.WriteLine();

            writer.WriteLine("public static ref readonly global::System.Guid IID");

            using (writer.WriteBlock())
            {
                writer.WriteLine("[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
                writer.WriteLine($"get => ref {iidFieldName};");
            }

            writer.WriteLine();

            writer.WriteLine("public static nint Vtable");

            using (writer.WriteBlock())
            {
                writer.WriteLine("[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
                writer.WriteLine("get => (nint)global::System.Runtime.CompilerServices.Unsafe.AsPointer(in Vftbl);");
            }

            // UnmanagedCallersOnly methods
            foreach ((MethodDefinition method, int slotIndex) in vtableMethods)
            {
                if (method.Signature is not { } methodSig)
                {
                    continue;
                }

                WriteImplCallbackMethod(writer, method, methodSig, slotIndex, fullProjectedName);
            }
        }
    }
    /// <summary>
    /// Writes a single [UnmanagedCallersOnly] callback method for the Impl class.
    /// </summary>
    private static void WriteImplCallbackMethod(
        CodeWriter writer,
        MethodDefinition method,
        MethodSignature methodSig,
        int slotIndex,
        string fullProjectedName)
    {
        string methodName = method.Name?.Value ?? $"Method{slotIndex}";
        string escapedName = SanitizeVftblMethodName(methodName);
        int methodIndex = slotIndex - 6;

        string returnType = GetProjectedTypeName(methodSig.ReturnType);
        bool hasReturnValue = returnType != "void";

        string abiParams = BuildAbiParameterList(method, includeThisPtr: true, includeReturnParam: hasReturnValue);

        writer.WriteLine();
        writer.WriteLine("[global::System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(global::System.Runtime.CompilerServices.CallConvMemberFunction) })]");
        writer.WriteLine($"private static unsafe int Do_Abi_{escapedName}_{methodIndex}({abiParams})");

        using (writer.WriteBlock())
        {
            writer.WriteLine("try");

            using (writer.WriteBlock())
            {
                WriteImplMethodBody(writer, method, methodSig, fullProjectedName, hasReturnValue);
                writer.WriteLine("return 0;");
            }

            writer.WriteLine("catch (global::System.Exception __exception__)");

            using (writer.WriteBlock())
            {
                writer.WriteLine("return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(__exception__);");
            }
        }
    }

    /// <summary>
    /// Writes the body of an Impl callback that gets the managed instance and calls it.
    /// </summary>
    private static void WriteImplMethodBody(
        CodeWriter writer,
        MethodDefinition method,
        MethodSignature methodSig,
        string fullProjectedName,
        bool hasReturnValue)
    {
        List<string> managedArgs = [];

        for (int i = 0; i < methodSig.ParameterTypes.Count; i++)
        {
            TypeSignature paramType = methodSig.ParameterTypes[i];
            string paramName = GetParameterName(method, i);

            if (paramType is ByReferenceTypeSignature)
            {
                ParameterDefinition? paramDef = GetParameterDefinition(method, i);
                string prefix = paramDef?.IsOut == true ? "out " : "ref ";
                managedArgs.Add($"{prefix}*{paramName}");
            }
            else
            {
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
        }

        string argsJoined = string.Join(", ", managedArgs);

        string callMethodName = CSharpKeywords.EscapeIdentifier(method.Name?.Value ?? "");
        bool isPropertyGetter = method.IsSpecialName && (method.Name?.Value?.StartsWith("get_", StringComparison.Ordinal) ?? false);
        bool isPropertySetter = method.IsSpecialName && (method.Name?.Value?.StartsWith("put_", StringComparison.Ordinal) ?? false);
        bool isEventAdd = method.IsSpecialName && (method.Name?.Value?.StartsWith("add_", StringComparison.Ordinal) ?? false);
        bool isEventRemove = method.IsSpecialName && (method.Name?.Value?.StartsWith("remove_", StringComparison.Ordinal) ?? false);

        if (isPropertyGetter)
        {
            string propName = method.Name!.Value![4..];

            if (hasReturnValue)
            {
                string abiReturnType = GetAbiTypeName(methodSig.ReturnType);
                string projectedReturnType = GetProjectedTypeName(methodSig.ReturnType);

                if (abiReturnType != projectedReturnType)
                {
                    writer.WriteLine($"*retval = {GetMarshalToUnmanagedExpression(methodSig.ReturnType, $"ComInterfaceDispatch.GetInstance<{fullProjectedName}>((ComInterfaceDispatch*)thisPtr).{propName}")};");
                }
                else
                {
                    writer.WriteLine($"*retval = ComInterfaceDispatch.GetInstance<{fullProjectedName}>((ComInterfaceDispatch*)thisPtr).{propName};");
                }
            }
        }
        else if (isPropertySetter)
        {
            string propName = method.Name!.Value![4..];
            writer.WriteLine($"ComInterfaceDispatch.GetInstance<{fullProjectedName}>((ComInterfaceDispatch*)thisPtr).{propName} = {argsJoined};");
        }
        else if (isEventAdd)
        {
            string eventName = method.Name!.Value![4..];

            if (hasReturnValue)
            {
                writer.WriteLine($"*retval = ComInterfaceDispatch.GetInstance<{fullProjectedName}>((ComInterfaceDispatch*)thisPtr).{eventName} += {argsJoined};");
            }
            else
            {
                writer.WriteLine($"ComInterfaceDispatch.GetInstance<{fullProjectedName}>((ComInterfaceDispatch*)thisPtr).{eventName} += {argsJoined};");
            }
        }
        else if (isEventRemove)
        {
            string eventName = method.Name!.Value![7..];
            writer.WriteLine($"ComInterfaceDispatch.GetInstance<{fullProjectedName}>((ComInterfaceDispatch*)thisPtr).{eventName} -= {argsJoined};");
        }
        else
        {
            if (hasReturnValue)
            {
                string abiReturnType = GetAbiTypeName(methodSig.ReturnType);
                string projectedReturnType = GetProjectedTypeName(methodSig.ReturnType);

                if (abiReturnType != projectedReturnType)
                {
                    writer.WriteLine($"*retval = {GetMarshalToUnmanagedExpression(methodSig.ReturnType, $"ComInterfaceDispatch.GetInstance<{fullProjectedName}>((ComInterfaceDispatch*)thisPtr).{callMethodName}({argsJoined})")};");
                }
                else
                {
                    writer.WriteLine($"*retval = ComInterfaceDispatch.GetInstance<{fullProjectedName}>((ComInterfaceDispatch*)thisPtr).{callMethodName}({argsJoined});");
                }
            }
            else
            {
                writer.WriteLine($"ComInterfaceDispatch.GetInstance<{fullProjectedName}>((ComInterfaceDispatch*)thisPtr).{callMethodName}({argsJoined});");
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

        string projectedParams = BuildProjectedParameterList(method);

        writer.WriteLine();
        writer.WriteLine("[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]");

        string fullParams = string.IsNullOrEmpty(projectedParams)
            ? "WindowsRuntimeObjectReference thisReference"
            : $"WindowsRuntimeObjectReference thisReference, {projectedParams}";

        writer.WriteLine($"public static unsafe {returnType} {methodName}({fullParams})");

        using (writer.WriteBlock())
        {
            writer.WriteLine("using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();");
            writer.WriteLine("void* ThisPtr = thisValue.GetThisPtrUnsafe();");

            // Marshal input parameters
            List<string> marshaledParamDeclarations = [];

            if (method.Signature is { } sig)
            {
                for (int i = 0; i < sig.ParameterTypes.Count; i++)
                {
                    TypeSignature paramType = sig.ParameterTypes[i];
                    string paramName = GetParameterName(method, i);
                    string escapedParamName = CSharpKeywords.EscapeIdentifier(paramName);
                    string abiType = GetAbiTypeName(paramType);
                    string projType = GetProjectedTypeName(paramType);

                    if (paramType is ByReferenceTypeSignature)
                    {
                        continue;
                    }

                    if (abiType != projType)
                    {
                        bool isRefType = IsReferenceAbiType(paramType);

                        if (isRefType)
                        {
                            string marshalExpr = GetMarshalParamToUnmanagedExpression(paramType, escapedParamName);
                            marshaledParamDeclarations.Add($"using WindowsRuntimeObjectReferenceValue __{paramName} = {marshalExpr};");
                        }
                        else
                        {
                            string marshalExpr = GetMarshalToUnmanagedExpression(paramType, escapedParamName);
                            marshaledParamDeclarations.Add($"var __{paramName} = {marshalExpr};");
                        }
                    }
                }
            }

            foreach (string decl in marshaledParamDeclarations)
            {
                writer.WriteLine(decl);
            }

            if (hasReturnValue)
            {
                string abiReturnType = GetAbiTypeName(methodSig.ReturnType);
                writer.WriteLine($"{abiReturnType} __retval = default;");
            }

            bool needsTryFinally = hasReturnValue && NeedsReturnValueCleanup(methodSig.ReturnType);

            if (needsTryFinally)
            {
                writer.WriteLine("try");

                using (writer.WriteBlock())
                {
                    WriteVtableCall(writer, method, methodSig, vtableSlotIndex, hasReturnValue);
                    WriteReturnMarshal(writer, methodSig, hasReturnValue);
                }

                writer.WriteLine("finally");

                using (writer.WriteBlock())
                {
                    WriteReturnValueCleanup(writer, methodSig);
                }
            }
            else
            {
                WriteVtableCall(writer, method, methodSig, vtableSlotIndex, hasReturnValue);
                WriteReturnMarshal(writer, methodSig, hasReturnValue);
            }
        }
    }
    /// <summary>
    /// Writes the vtable function pointer call.
    /// </summary>
    private static void WriteVtableCall(
        CodeWriter writer,
        MethodDefinition method,
        MethodSignature methodSig,
        int vtableSlotIndex,
        bool hasReturnValue)
    {
        string fPtrSig = BuildFunctionPointerSignature(method, hasReturnValue);

        List<string> callArgs = [];

        for (int i = 0; i < methodSig.ParameterTypes.Count; i++)
        {
            TypeSignature paramType = methodSig.ParameterTypes[i];
            string paramName = GetParameterName(method, i);
            string escapedParamName = CSharpKeywords.EscapeIdentifier(paramName);
            string abiType = GetAbiTypeName(paramType);
            string projType = GetProjectedTypeName(paramType);

            if (paramType is ByReferenceTypeSignature)
            {
                callArgs.Add($"&{escapedParamName}");
            }
            else if (abiType != projType)
            {
                if (IsReferenceAbiType(paramType))
                {
                    callArgs.Add($"__{paramName}.GetThisPtrUnsafe()");
                }
                else
                {
                    callArgs.Add($"__{paramName}");
                }
            }
            else
            {
                callArgs.Add(escapedParamName);
            }
        }

        if (hasReturnValue)
        {
            callArgs.Add("&__retval");
        }

        writer.Write($"RestrictedErrorInfo.ThrowExceptionForHR((*(delegate* unmanaged[MemberFunction]<{fPtrSig}>**)ThisPtr)[{vtableSlotIndex}](ThisPtr");

        if (callArgs.Count > 0)
        {
            writer.Write(",");
            writer.WriteLine();
            writer.IncreaseIndent();
            writer.IncreaseIndent();

            for (int i = 0; i < callArgs.Count; i++)
            {
                if (i < callArgs.Count - 1)
                {
                    writer.WriteLine($"{callArgs[i]},");
                }
                else
                {
                    writer.Write($"{callArgs[i]}");
                }
            }

            writer.Write("));");
            writer.WriteLine();
            writer.DecreaseIndent();
            writer.DecreaseIndent();
        }
        else
        {
            writer.Write("));");
            writer.WriteLine();
        }
    }

    /// <summary>
    /// Writes the return value marshaling if needed.
    /// </summary>
    private static void WriteReturnMarshal(CodeWriter writer, MethodSignature methodSig, bool hasReturnValue)
    {
        if (!hasReturnValue)
        {
            return;
        }

        string abiReturnType = GetAbiTypeName(methodSig.ReturnType);
        string projectedReturnType = GetProjectedTypeName(methodSig.ReturnType);

        if (abiReturnType != projectedReturnType)
        {
            writer.WriteLine($"return {GetMarshalToManagedExpression(methodSig.ReturnType, "__retval")};");
        }
        else
        {
            writer.WriteLine("return __retval;");
        }
    }

    #endregion

    #region Class

    /// <summary>
    /// Writes the ABI marshaller and ComWrappers callback for a runtime class type.
    /// </summary>
    private static void WriteAbiClass(CodeWriter writer, TypeDefinition type, ProjectionGeneratorArgs args)
    {
        _ = args;

        string name = TypeNameHelpers.GetSimpleName(type);
        string fullProjectedName = GetFullProjectedTypeName(type);
        bool isStatic = TypeHelpers.IsStaticClass(type);

        if (isStatic)
        {
            return;
        }

        ITypeDefOrRef? defaultInterface = TypeHelpers.GetDefaultInterface(type);

        if (defaultInterface is null)
        {
            return;
        }

        string iidFieldName = GetIIDFieldReferenceForTypeRef(defaultInterface);

        // --- Marshaller ---
        writer.WriteLine();
        writer.WriteLine($"public static unsafe class {name}Marshaller");

        using (writer.WriteBlock())
        {
            writer.WriteLine($"public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged({fullProjectedName} value)");

            using (writer.WriteBlock())
            {
                writer.WriteLine("if (WindowsRuntimeComWrappersMarshal.TryUnwrapObjectReference(value, out WindowsRuntimeObjectReference? objectReference))");

                using (writer.WriteBlock())
                {
                    writer.WriteLine("return objectReference.AsValue();");
                }

                writer.WriteLine("return default;");
            }

            writer.WriteLine();

            writer.WriteLine($"public static {fullProjectedName}? ConvertToManaged(void* value)");

            using (writer.WriteBlock())
            {
                writer.WriteLine($"return ({fullProjectedName}?)WindowsRuntimeObjectMarshaller.ConvertToManaged<{name}ComWrappersCallback>(value);");
            }
        }

        // --- ComWrappersMarshallerAttribute ---
        writer.WriteLine();
        writer.WriteLine($"file sealed unsafe class {name}ComWrappersMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute");

        using (writer.WriteBlock())
        {
            writer.WriteLine("public override object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)");

            using (writer.WriteBlock())
            {
                writer.WriteLine("WindowsRuntimeObjectReference valueReference = WindowsRuntimeComWrappersMarshal.CreateObjectReference(");
                writer.IncreaseIndent();
                writer.WriteLine("externalComObject: value,");
                writer.WriteLine($"iid: {iidFieldName},");
                writer.WriteLine("wrapperFlags: out wrapperFlags);");
                writer.DecreaseIndent();
                writer.WriteLine($"return new {fullProjectedName}(valueReference);");
            }
        }

        // --- ComWrappersCallback ---
        writer.WriteLine();
        writer.WriteLine($"file sealed unsafe class {name}ComWrappersCallback : IWindowsRuntimeObjectComWrappersCallback");

        using (writer.WriteBlock())
        {
            writer.WriteLine("public static object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)");

            using (writer.WriteBlock())
            {
                writer.WriteLine("WindowsRuntimeObjectReference valueReference = WindowsRuntimeComWrappersMarshal.CreateObjectReferenceUnsafe(");
                writer.IncreaseIndent();
                writer.WriteLine("externalComObject: value,");
                writer.WriteLine($"iid: {iidFieldName},");
                writer.WriteLine("wrapperFlags: out wrapperFlags);");
                writer.DecreaseIndent();
                writer.WriteLine($"return new {fullProjectedName}(valueReference);");
            }
        }
    }

    #endregion
    #region Type name helpers

    /// <summary>
    /// Gets the fully qualified projected type name.
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
    /// Gets the projected C# type name for a type signature.
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

        if (typeSig is CustomModifierTypeSignature customMod)
        {
            return GetProjectedTypeName(customMod.BaseType);
        }

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
    /// Gets the ABI type name for a type signature. Maps projected types to their
    /// unmanaged representations.
    /// </summary>
    private static string GetAbiTypeName(TypeSignature? typeSig)
    {
        if (typeSig is null)
        {
            return "void";
        }

        if (typeSig is CorLibTypeSignature corLibType)
        {
            string fullName = $"{corLibType.Namespace}.{corLibType.Name}";

            return fullName switch
            {
                "System.String" => "void*",
                "System.Object" => "void*",
                "System.Boolean" => "byte",
                "System.Char" => "ushort",
                _ => TypeNameHelpers.GetCSharpTypeName(fullName) ?? $"global::{fullName}"
            };
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

                if (baseFullName == "System.Enum")
                {
                    return GetEnumUnderlyingType(resolved);
                }

                if (baseFullName == "System.ValueType")
                {
                    return GetAbiStructTypeName(resolved);
                }

                if (baseFullName == "System.MulticastDelegate" || resolved.IsInterface)
                {
                    return "void*";
                }
            }

            return "void*";
        }

        return typeSig switch
        {
            GenericInstanceTypeSignature => "void*",
            SzArrayTypeSignature => "void*",
            ByReferenceTypeSignature byRef => GetAbiTypeName(byRef.BaseType),
            _ => "void*"
        };
    }

    /// <summary>
    /// Gets the ABI struct type name. Blittable structs use their projected name directly.
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
                string? ns = structType.Namespace?.Value;
                string simpleName = TypeNameHelpers.GetSimpleName(structType);

                return string.IsNullOrEmpty(ns)
                    ? $"global::ABI.{simpleName}Marshaller.AbiType"
                    : $"global::ABI.{ns}.{simpleName}Marshaller.AbiType";
            }
        }

        return GetFullProjectedTypeName(structType);
    }

    /// <summary>
    /// Gets the underlying C# type for a WinRT enum.
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

    /// <summary>
    /// Gets the IID field reference for a type definition.
    /// </summary>
    private static string GetIIDFieldReference(TypeDefinition type)
    {
        string escapedName = TypeNameHelpers.EscapeTypeNameForIdentifier($"{type.Namespace}.{type.Name}");

        return $"global::ABI.InterfaceIIDs.IID_{escapedName}";
    }

    /// <summary>
    /// Gets the IID field reference for a type reference.
    /// </summary>
    private static string GetIIDFieldReferenceForTypeRef(ITypeDefOrRef typeRef)
    {
        string escapedName = TypeNameHelpers.EscapeTypeNameForIdentifier($"{typeRef.Namespace}.{typeRef.Name}");

        return $"global::ABI.InterfaceIIDs.IID_{escapedName}";
    }

    #endregion
    #region Parameter and signature helpers

    /// <summary>
    /// Builds the ABI parameter list for an [UnmanagedCallersOnly] method.
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
    /// Builds the function pointer signature string including thisPtr and HRESULT return.
    /// </summary>
    private static string BuildFunctionPointerSignature(MethodDefinition method, bool hasReturnValue)
    {
        List<string> types = ["void*"];

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

        types.Add("int");

        return string.Join(", ", types);
    }

    /// <summary>
    /// Builds a vftbl function pointer type string.
    /// </summary>
    private static string BuildVftblFunctionPointerSignature(MethodDefinition _, MethodSignature methodSig)
    {
        List<string> types = ["void*"];

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

        string returnType = GetProjectedTypeName(methodSig.ReturnType);
        bool hasReturnValue = returnType != "void";

        if (hasReturnValue && methodSig.ReturnType is not null)
        {
            string returnAbiType = GetAbiTypeName(methodSig.ReturnType);

            if (returnAbiType != "void")
            {
                types.Add($"{returnAbiType}*");
            }
        }

        types.Add("int");

        return $"delegate* unmanaged[MemberFunction]<{string.Join(", ", types)}>";
    }

    #endregion
    #region Marshaling expression helpers

    /// <summary>
    /// Gets an expression to marshal from unmanaged (ABI) to managed (projected).
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

            return fullName switch
            {
                "System.String" => $"HStringMarshaller.ConvertToManaged({variableName})",
                "System.Boolean" => $"{variableName} != 0",
                "System.Char" => $"(char){variableName}",
                _ => variableName
            };
        }

        if (typeSig is TypeDefOrRefSignature typeDefOrRef)
        {
            TypeDefinition? resolved = typeDefOrRef.Type?.Resolve();

            if (resolved is not null)
            {
                string? baseFullName = resolved.BaseType?.FullName;

                if (baseFullName == "System.Enum")
                {
                    string projectedType = GetProjectedTypeName(typeSig);

                    return $"({projectedType}){variableName}";
                }

                if (baseFullName == "System.MulticastDelegate")
                {
                    string? ns = resolved.Namespace?.Value;
                    string simpleName = TypeNameHelpers.GetSimpleName(resolved);
                    string marshallerName = string.IsNullOrEmpty(ns)
                        ? $"global::ABI.{simpleName}Marshaller"
                        : $"global::ABI.{ns}.{simpleName}Marshaller";

                    return $"{marshallerName}.ConvertToManaged({variableName})";
                }

                if (resolved.IsInterface)
                {
                    string projectedType = GetProjectedTypeName(typeSig);

                    return $"({projectedType})WindowsRuntimeObjectMarshaller.ConvertToManaged({variableName})";
                }

                if (baseFullName is not "System.Enum" and not "System.ValueType" and not "System.MulticastDelegate")
                {
                    string? ns = resolved.Namespace?.Value;
                    string simpleName = TypeNameHelpers.GetSimpleName(resolved);
                    string marshallerName = string.IsNullOrEmpty(ns)
                        ? $"global::ABI.{simpleName}Marshaller"
                        : $"global::ABI.{ns}.{simpleName}Marshaller";

                    return $"{marshallerName}.ConvertToManaged({variableName})";
                }
            }
        }

        return typeSig is GenericInstanceTypeSignature
            ? $"WindowsRuntimeObjectMarshaller.ConvertToManaged({variableName})"
            : variableName;
    }

    /// <summary>
    /// Gets an expression to marshal from managed (projected) to unmanaged (ABI).
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

            return fullName switch
            {
                "System.String" => $"HStringMarshaller.ConvertToUnmanaged({variableName})",
                "System.Boolean" => $"(byte)({variableName} ? 1 : 0)",
                "System.Char" => $"(ushort){variableName}",
                _ => variableName
            };
        }

        if (typeSig is TypeDefOrRefSignature typeDefOrRef)
        {
            TypeDefinition? resolved = typeDefOrRef.Type?.Resolve();

            if (resolved is not null)
            {
                string? baseFullName = resolved.BaseType?.FullName;

                if (baseFullName == "System.Enum")
                {
                    string abiType = GetEnumUnderlyingType(resolved);

                    return $"({abiType}){variableName}";
                }
            }
        }

        return variableName;
    }

    /// <summary>
    /// Gets an expression to marshal a parameter to unmanaged, returning a WindowsRuntimeObjectReferenceValue.
    /// </summary>
    private static string GetMarshalParamToUnmanagedExpression(TypeSignature paramType, string variableName)
    {
        if (paramType is TypeDefOrRefSignature typeDefOrRef)
        {
            TypeDefinition? resolved = typeDefOrRef.Type?.Resolve();

            if (resolved is not null)
            {
                string? baseFullName = resolved.BaseType?.FullName;

                if (baseFullName == "System.MulticastDelegate")
                {
                    string? ns = resolved.Namespace?.Value;
                    string simpleName = TypeNameHelpers.GetSimpleName(resolved);
                    string marshallerName = string.IsNullOrEmpty(ns)
                        ? $"global::ABI.{simpleName}Marshaller"
                        : $"global::ABI.{ns}.{simpleName}Marshaller";

                    return $"{marshallerName}.ConvertToUnmanaged({variableName})";
                }

                if (resolved.IsInterface || (baseFullName is not "System.Enum" and not "System.ValueType" and not "System.MulticastDelegate"))
                {
                    string? ns = resolved.Namespace?.Value;
                    string simpleName = TypeNameHelpers.GetSimpleName(resolved);
                    string marshallerName = string.IsNullOrEmpty(ns)
                        ? $"global::ABI.{simpleName}Marshaller"
                        : $"global::ABI.{ns}.{simpleName}Marshaller";

                    return $"{marshallerName}.ConvertToUnmanaged({variableName})";
                }
            }
        }

        return $"WindowsRuntimeObjectMarshaller.ConvertToUnmanaged({variableName})";
    }

    /// <summary>
    /// Checks whether a type's ABI representation is void*.
    /// </summary>
    private static bool IsReferenceAbiType(TypeSignature typeSig)
    {
        string abiType = GetAbiTypeName(typeSig);

        return abiType == "void*";
    }

    /// <summary>
    /// Checks whether a return value needs cleanup in a finally block.
    /// </summary>
    private static bool NeedsReturnValueCleanup(TypeSignature? returnType)
    {
        if (returnType is null)
        {
            return false;
        }

        string abiType = GetAbiTypeName(returnType);
        string projectedType = GetProjectedTypeName(returnType);

        return abiType == "void*" && projectedType != "void";
    }

    /// <summary>
    /// Writes cleanup code for a return value in a finally block.
    /// </summary>
    private static void WriteReturnValueCleanup(CodeWriter writer, MethodSignature methodSig)
    {
        if (methodSig.ReturnType is null)
        {
            return;
        }

        if (methodSig.ReturnType is CorLibTypeSignature corLib && $"{corLib.Namespace}.{corLib.Name}" == "System.String")
        {
            writer.WriteLine("HStringMarshaller.Free(__retval);");
        }
        else
        {
            writer.WriteLine("WindowsRuntimeUnknownMarshaller.Free(__retval);");
        }
    }

    /// <summary>
    /// Gets the expression to marshal a struct field to unmanaged.
    /// </summary>
    private static string GetFieldMarshalToUnmanagedExpression(string abiType, string expression)
    {
        return abiType == "void*"
            ? $"HStringMarshaller.ConvertToUnmanaged({expression})"
            : expression;
    }

    /// <summary>
    /// Gets the expression to marshal a struct field to managed.
    /// </summary>
    private static string GetFieldMarshalToManagedExpression(string abiType, string expression)
    {
        return abiType == "void*"
            ? $"HStringMarshaller.ConvertToManaged({expression})"
            : expression;
    }

    /// <summary>
    /// Writes the Free call for a struct field.
    /// </summary>
    private static void WriteFieldFree(CodeWriter writer, string abiType, string expression)
    {
        if (abiType == "void*")
        {
            writer.WriteLine($"HStringMarshaller.Free({expression});");
        }
    }

    /// <summary>
    /// Writes the body of a delegate's Do_Abi_Invoke callback.
    /// </summary>
    private static void WriteDelegateInvokeBody(
        CodeWriter writer,
        MethodDefinition invokeMethod,
        MethodSignature methodSig,
        string fullProjectedName)
    {
        string returnType = GetProjectedTypeName(methodSig.ReturnType);
        bool hasReturnValue = returnType != "void";

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
            writer.WriteLine($"var __result = ComInterfaceDispatch.GetInstance<{fullProjectedName}>((ComInterfaceDispatch*)thisPtr).Invoke({argsJoined});");

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
            writer.WriteLine($"ComInterfaceDispatch.GetInstance<{fullProjectedName}>((ComInterfaceDispatch*)thisPtr).Invoke({argsJoined});");
        }
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
    /// Gets the ParameterDefinition for a parameter at the given index.
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
    /// Removes the generic arity suffix from a type name.
    /// </summary>
    private static string RemoveGenericArity(string typeName)
    {
        int backtickIndex = typeName.IndexOf('`');

        return backtickIndex >= 0 ? typeName[..backtickIndex] : typeName;
    }

    /// <summary>
    /// Sanitizes a method name for use in the Vftbl struct field names.
    /// </summary>
    private static string SanitizeVftblMethodName(string methodName)
    {
        return methodName
            .Replace('.', '_')
            .Replace('<', '_')
            .Replace('>', '_');
    }

    #endregion
}