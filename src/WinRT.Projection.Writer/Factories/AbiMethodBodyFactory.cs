// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.ProjectionWriter.Extensions;
using WindowsRuntime.ProjectionWriter.Models;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter;

/// <summary>
/// Emits the ABI method body shapes for runtime interface vtable invocations: simple/forwarding bodies, parameter conversion glue, and per-method UnsafeAccessor accessors for generic vtables.
/// </summary>
internal static class AbiMethodBodyFactory
{
    /// <summary>
    /// Emits a real Do_Abi (CCW) body for the cases we can handle. Mirrors C++
    /// <c>write_abi_method_call_marshalers</c> (<c>code_writers.h:6682</c>) which
    /// unconditionally emits a real body via the <c>abi_marshaler</c> abstraction
    /// for every WinRT-valid signature.
    /// </summary>
    internal static void EmitDoAbiBodyIfSimple(IndentedTextWriter writer, ProjectionEmitContext context, MethodSig sig, string ifaceFullName, string methodName)
    {
        AsmResolver.DotNet.Signatures.TypeSignature? rt = sig.ReturnType;

        // String params drive whether we need HString header allocation in the body.
        bool hasStringParams = false;
        foreach (ParamInfo p in sig.Params)
        {
            if (p.Type.IsString()) { hasStringParams = true; break; }
        }
        bool returnIsReceiveArrayDoAbi = rt is AsmResolver.DotNet.Signatures.SzArrayTypeSignature retSzAbi
            && (CodeWriters.IsBlittablePrimitive(context.Cache, retSzAbi.BaseType) || CodeWriters.IsAnyStruct(context.Cache, retSzAbi.BaseType)
                || retSzAbi.BaseType.IsString() || CodeWriters.IsRuntimeClassOrInterface(context.Cache, retSzAbi.BaseType) || retSzAbi.BaseType.IsObject()
                || CodeWriters.IsComplexStruct(context.Cache, retSzAbi.BaseType));
        bool returnIsHResultExceptionDoAbi = rt is not null && rt.IsHResultException();
        bool returnIsString = rt is not null && rt.IsString();
        bool returnIsRefType = rt is not null && (CodeWriters.IsRuntimeClassOrInterface(context.Cache, rt) || rt.IsObject() || rt.IsGenericInstance());
        bool returnIsGenericInstance = rt is not null && rt.IsGenericInstance();
        bool returnIsBlittableStruct = rt is not null && CodeWriters.IsAnyStruct(context.Cache, rt);

        bool isGetter = methodName.StartsWith("get_", System.StringComparison.Ordinal);
        bool isSetter = methodName.StartsWith("put_", System.StringComparison.Ordinal);
        bool isAddEvent = methodName.StartsWith("add_", System.StringComparison.Ordinal);
        bool isRemoveEvent = methodName.StartsWith("remove_", System.StringComparison.Ordinal);

        if (isAddEvent || isRemoveEvent)
        {
            // Events go through dedicated EmitDoAbiAddEvent / EmitDoAbiRemoveEvent paths
            // upstream (see lines 1153-1159). If we reach here for an event accessor it's a
            // generator bug. Defensive guard against future regressions.
            throw new System.InvalidOperationException(
                $"EmitDoAbiBodyIfSimple: unexpectedly called for event accessor '{methodName}' " +
                $"on '{ifaceFullName}'. Events should dispatch through EmitDoAbiAddEvent / EmitDoAbiRemoveEvent.");
        }

        writer.Write("\n{\n");
        string retParamName = CodeWriters.GetReturnParamName(sig);
        string retSizeParamName = CodeWriters.GetReturnSizeParamName(sig);
        // The local name for the unmarshalled return value mirrors C++
        // 'abi_marshaler::get_marshaler_local()' which prefixes '__' to the param name.
        // For the default '__return_value__' param this becomes '____return_value__'.
        string retLocalName = "__" + retParamName;
        // at the TOP of the method body (before local declarations and the try block). The
        // actual call sites later in the body just reference the already-declared accessor.
        // For a generic-instance return type, the accessor is named ConvertToUnmanaged_<retParamName>.
        // Skip Nullable<T> returns: those use <T>Marshaller.BoxToUnmanaged at the call site
        // instead of the generic-instance UnsafeAccessor (V3-M7).
        if (returnIsGenericInstance && !(rt is not null && rt.IsNullableT()))
        {
            string interopTypeName = InteropTypeNameWriter.EncodeInteropTypeName(rt!, TypedefNameType.ABI) + ", WinRT.Interop";
            IndentedTextWriter __scratchProjectedTypeName = new();
            MethodFactory.WriteProjectedSignature(__scratchProjectedTypeName, context, rt!, false);
            string projectedTypeName = __scratchProjectedTypeName.ToString();
            writer.Write("    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"ConvertToUnmanaged\")]\n");
            writer.Write("    static extern WindowsRuntimeObjectReferenceValue ConvertToUnmanaged_");
            writer.Write(retParamName);
            writer.Write("([UnsafeAccessorType(\"");
            writer.Write(interopTypeName);
            writer.Write("\")] object _, ");
            writer.Write(projectedTypeName);
            writer.Write(" value);\n\n");
        }

        // Hoist [UnsafeAccessor] declarations for Out generic-instance params:
        // ConvertToUnmanaged_<name> wraps the projected value into a WindowsRuntimeObjectReferenceValue.
        // The body's writeback later references these already-declared accessors.
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.Out) { continue; }
            AsmResolver.DotNet.Signatures.TypeSignature uOut = CodeWriters.StripByRefAndCustomModifiers(p.Type);
            if (!uOut.IsGenericInstance()) { continue; }
            string raw = p.Parameter.Name ?? "param";
            string interopTypeName = InteropTypeNameWriter.EncodeInteropTypeName(uOut, TypedefNameType.ABI) + ", WinRT.Interop";
            IndentedTextWriter __scratchProjectedTypeName = new();
            MethodFactory.WriteProjectedSignature(__scratchProjectedTypeName, context, uOut, false);
            string projectedTypeName = __scratchProjectedTypeName.ToString();
            writer.Write("    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"ConvertToUnmanaged\")]\n");
            writer.Write("    static extern WindowsRuntimeObjectReferenceValue ConvertToUnmanaged_");
            writer.Write(raw);
            writer.Write("([UnsafeAccessorType(\"");
            writer.Write(interopTypeName);
            writer.Write("\")] object _, ");
            writer.Write(projectedTypeName);
            writer.Write(" value);\n\n");
        }
        // ConvertToUnmanaged_<param> and the return-array ConvertToUnmanaged_<retParam> to the
        // top of the method body, before locals and the try block. The actual call sites later
        // in the body reference these already-declared accessors.
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.ReceiveArray) { continue; }
            string raw = p.Parameter.Name ?? "param";
            AsmResolver.DotNet.Signatures.SzArrayTypeSignature sza = (AsmResolver.DotNet.Signatures.SzArrayTypeSignature)CodeWriters.StripByRefAndCustomModifiers(p.Type);
            IndentedTextWriter __scratchElementProjected = new();
            TypedefNameWriter.WriteProjectionType(__scratchElementProjected, context, TypeSemanticsFactory.Get(sza.BaseType));
            string elementProjected = __scratchElementProjected.ToString();
            string elementInteropArg = InteropTypeNameWriter.EncodeInteropTypeName(sza.BaseType, TypedefNameType.Projected);

            _ = elementInteropArg;
            string marshallerPath = ArrayElementEncoder.GetArrayMarshallerInteropPath(sza.BaseType);
            string elementAbi = sza.BaseType.IsString() || CodeWriters.IsRuntimeClassOrInterface(context.Cache, sza.BaseType) || sza.BaseType.IsObject()
                ? "void*"
                : CodeWriters.IsComplexStruct(context.Cache, sza.BaseType)
                    ? CodeWriters.GetAbiStructTypeName(writer, context, sza.BaseType)
                    : CodeWriters.IsAnyStruct(context.Cache, sza.BaseType)
                        ? CodeWriters.GetBlittableStructAbiType(writer, context, sza.BaseType)
                        : CodeWriters.GetAbiPrimitiveType(context.Cache, sza.BaseType);
            writer.Write("    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"ConvertToUnmanaged\")]\n");
            writer.Write("    static extern void ConvertToUnmanaged_");
            writer.Write(raw);
            writer.Write("([UnsafeAccessorType(\"");
            writer.Write(marshallerPath);
            writer.Write("\")] object _, ReadOnlySpan<");
            writer.Write(elementProjected);
            writer.Write("> span, out uint length, out ");
            writer.Write(elementAbi);
            writer.Write("* data);\n\n");
        }
        if (returnIsReceiveArrayDoAbi && rt is AsmResolver.DotNet.Signatures.SzArrayTypeSignature retSzHoist)
        {
            IndentedTextWriter __scratchElementProjected = new();
            TypedefNameWriter.WriteProjectionType(__scratchElementProjected, context, TypeSemanticsFactory.Get(retSzHoist.BaseType));
            string elementProjected = __scratchElementProjected.ToString();
            string elementAbi = retSzHoist.BaseType.IsString() || CodeWriters.IsRuntimeClassOrInterface(context.Cache, retSzHoist.BaseType) || retSzHoist.BaseType.IsObject()
                ? "void*"
                : CodeWriters.IsComplexStruct(context.Cache, retSzHoist.BaseType)
                    ? CodeWriters.GetAbiStructTypeName(writer, context, retSzHoist.BaseType)
                    : CodeWriters.IsAnyStruct(context.Cache, retSzHoist.BaseType)
                        ? CodeWriters.GetBlittableStructAbiType(writer, context, retSzHoist.BaseType)
                        : CodeWriters.GetAbiPrimitiveType(context.Cache, retSzHoist.BaseType);
            string elementInteropArg = InteropTypeNameWriter.EncodeInteropTypeName(retSzHoist.BaseType, TypedefNameType.Projected);

            _ = elementInteropArg;
            string marshallerPath = ArrayElementEncoder.GetArrayMarshallerInteropPath(retSzHoist.BaseType);
            writer.Write("    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"ConvertToUnmanaged\")]\n");
            writer.Write("    static extern void ConvertToUnmanaged_");
            writer.Write(retParamName);
            writer.Write("([UnsafeAccessorType(\"");
            writer.Write(marshallerPath);
            writer.Write("\")] object _, ReadOnlySpan<");
            writer.Write(elementProjected);
            writer.Write("> span, out uint length, out ");
            writer.Write(elementAbi);
            writer.Write("* data);\n\n");
        }
        // the OUT pointer(s). The actual assignment happens inside the try block.
        if (rt is not null)
        {
            if (returnIsString)
            {
                writer.Write("    string ");
                writer.Write(retLocalName);
                writer.Write(" = default;\n");
            }
            else if (returnIsRefType)
            {
                IndentedTextWriter __scratchProjected = new();
                MethodFactory.WriteProjectedSignature(__scratchProjected, context, rt, false);
                string projected = __scratchProjected.ToString();
                writer.Write("    ");
                writer.Write(projected);
                writer.Write(" ");
                writer.Write(retLocalName);
                writer.Write(" = default;\n");
            }
            else if (returnIsReceiveArrayDoAbi)
            {
                IndentedTextWriter __scratchProjected = new();
                MethodFactory.WriteProjectedSignature(__scratchProjected, context, rt, false);
                string projected = __scratchProjected.ToString();
                writer.Write("    ");
                writer.Write(projected);
                writer.Write(" ");
                writer.Write(retLocalName);
                writer.Write(" = default;\n");
            }
            else
            {
                IndentedTextWriter __scratchProjected = new();
                MethodFactory.WriteProjectedSignature(__scratchProjected, context, rt, false);
                string projected = __scratchProjected.ToString();
                writer.Write("    ");
                writer.Write(projected);
                writer.Write(" ");
                writer.Write(retLocalName);
                writer.Write(" = default;\n");
            }
        }

        if (rt is not null)
        {
            if (returnIsReceiveArrayDoAbi)
            {
                writer.Write("    *");
                writer.Write(retParamName);
                writer.Write(" = default;\n");
                writer.Write("    *");
                writer.Write(retSizeParamName);
                writer.Write(" = default;\n");
            }
            else
            {
                writer.Write("    *");
                writer.Write(retParamName);
                writer.Write(" = default;\n");
            }
        }
        // For each out parameter, clear the destination and declare a local.
        // NOTE: Ref params (WinRT 'in T' / 'ref const T') are READ-ONLY inputs from the caller's
        // perspective. Do NOT zero *<name> (it's the input value) and do NOT declare a local
        // (we read directly via *<name>).
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.Out) { continue; }
            string raw = p.Parameter.Name ?? "param";
            string ptr = CSharpKeywords.IsKeyword(raw) ? "@" + raw : raw;
            writer.Write("    *");
            writer.Write(ptr);
            writer.Write(" = default;\n");
        }
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.Out) { continue; }
            string raw = p.Parameter.Name ?? "param";
            // Use the projected (non-ABI) type for the local variable.
            // Strip ByRef and CustomModifier wrappers to get the underlying base type.
            AsmResolver.DotNet.Signatures.TypeSignature underlying = CodeWriters.StripByRefAndCustomModifiers(p.Type);
            IndentedTextWriter __scratchProjected = new();
            MethodFactory.WriteProjectedSignature(__scratchProjected, context, underlying, false);
            string projected = __scratchProjected.ToString();
            writer.Write("    ");
            writer.Write(projected);
            writer.Write(" __");
            writer.Write(raw);
            writer.Write(" = default;\n");
        }
        // For each ReceiveArray parameter (out T[]), zero the destination + size out pointers
        // and declare a managed array local. The managed call passes 'out __<name>' and after
        // the call we copy to the ABI buffer via UnsafeAccessor.
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.ReceiveArray) { continue; }
            string raw = p.Parameter.Name ?? "param";
            string ptr = CSharpKeywords.IsKeyword(raw) ? "@" + raw : raw;
            AsmResolver.DotNet.Signatures.SzArrayTypeSignature sza = (AsmResolver.DotNet.Signatures.SzArrayTypeSignature)CodeWriters.StripByRefAndCustomModifiers(p.Type);
            IndentedTextWriter __scratchElementProjected = new();
            TypedefNameWriter.WriteProjectionType(__scratchElementProjected, context, TypeSemanticsFactory.Get(sza.BaseType));
            string elementProjected = __scratchElementProjected.ToString();
            writer.Write("    *");
            writer.Write(ptr);
            writer.Write(" = default;\n");
            writer.Write("    *__");
            writer.Write(raw);
            writer.Write("Size = default;\n");
            writer.Write("    ");
            writer.Write(elementProjected);
            writer.Write("[] __");
            writer.Write(raw);
            writer.Write(" = default;\n");
        }
        // For each blittable array (PassArray / FillArray) parameter, declare a Span<T> local that
        // wraps the (length, pointer) pair from the ABI signature.
        // For non-blittable element types (string/runtime class/object), declare InlineArray16<T> +
        // ArrayPool fallback then CopyToManaged via UnsafeAccessor.
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.PassArray && cat != ParamCategory.FillArray) { continue; }
            if (p.Type is not AsmResolver.DotNet.Signatures.SzArrayTypeSignature sz) { continue; }
            string raw = p.Parameter.Name ?? "param";
            string ptr = CSharpKeywords.IsKeyword(raw) ? "@" + raw : raw;
            IndentedTextWriter __scratchElementProjected = new();
            TypedefNameWriter.WriteProjectionType(__scratchElementProjected, context, TypeSemanticsFactory.Get(sz.BaseType));
            string elementProjected = __scratchElementProjected.ToString();
            bool isBlittableElem = CodeWriters.IsBlittablePrimitive(context.Cache, sz.BaseType) || CodeWriters.IsAnyStruct(context.Cache, sz.BaseType);
            if (isBlittableElem)
            {
                writer.Write("    ");
                writer.Write(cat == ParamCategory.PassArray ? "ReadOnlySpan<" : "Span<");
                writer.Write(elementProjected);
                writer.Write("> __");
                writer.Write(raw);
                writer.Write(" = new(");
                writer.Write(ptr);
                writer.Write(", (int)__");
                writer.Write(raw);
                writer.Write("Size);\n");
            }
            else
            {
                // Non-blittable element: InlineArray16<T> + ArrayPool<T> with size from ABI.
                writer.Write("\n    Unsafe.SkipInit(out InlineArray16<");
                writer.Write(elementProjected);
                writer.Write("> __");
                writer.Write(raw);
                writer.Write("_inlineArray);\n");
                writer.Write("    ");
                writer.Write(elementProjected);
                writer.Write("[] __");
                writer.Write(raw);
                writer.Write("_arrayFromPool = null;\n");
                writer.Write("    Span<");
                writer.Write(elementProjected);
                writer.Write("> __");
                writer.Write(raw);
                writer.Write(" = __");
                writer.Write(raw);
                writer.Write("Size <= 16\n        ? __");
                writer.Write(raw);
                writer.Write("_inlineArray[..(int)__");
                writer.Write(raw);
                writer.Write("Size]\n        : (__");
                writer.Write(raw);
                writer.Write("_arrayFromPool = global::System.Buffers.ArrayPool<");
                writer.Write(elementProjected);
                writer.Write(">.Shared.Rent((int)__");
                writer.Write(raw);
                writer.Write("Size));\n");
            }
        }
        writer.Write("    try\n    {\n");

        // For non-blittable PassArray params (read-only input arrays), emit CopyToManaged_<name>
        // via UnsafeAccessor to convert the native ABI buffer into the managed Span<T> the
        // delegate sees. For FillArray params, the buffer is fresh storage the user delegate
        // fills — the post-call writeback loop handles that. (Mirrors C++ which only emits the
        // pre-call CopyToManaged for PassArray, see write_copy_to_managed.)
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.PassArray) { continue; }
            if (p.Type is not AsmResolver.DotNet.Signatures.SzArrayTypeSignature szArr) { continue; }
            if (CodeWriters.IsBlittablePrimitive(context.Cache, szArr.BaseType) || CodeWriters.IsAnyStruct(context.Cache, szArr.BaseType)) { continue; }
            string raw = p.Parameter.Name ?? "param";
            string ptr = CSharpKeywords.IsKeyword(raw) ? "@" + raw : raw;
            IndentedTextWriter __scratchElementProjected = new();
            TypedefNameWriter.WriteProjectionType(__scratchElementProjected, context, TypeSemanticsFactory.Get(szArr.BaseType));
            string elementProjected = __scratchElementProjected.ToString();
            string elementInteropArg = InteropTypeNameWriter.EncodeInteropTypeName(szArr.BaseType, TypedefNameType.Projected);

            _ = elementInteropArg;
            // For complex structs, the data param is the ABI struct pointer (e.g. BasicStruct*).
            // The Do_Abi parameter we receive is void* (per V3R3-M8), so the call-site needs an
            // explicit (T*) cast to bridge the type. For ref-types (string/runtime-class/object),
            // the data param is void** and the cast is (void**).
            string dataParamType;
            string dataCastExpr;
            if (CodeWriters.IsComplexStruct(context.Cache, szArr.BaseType))
            {
                string abiStructName = CodeWriters.GetAbiStructTypeName(writer, context, szArr.BaseType);
                dataParamType = abiStructName + "* data";
                dataCastExpr = "(" + abiStructName + "*)" + ptr;
            }
            else
            {
                dataParamType = "void** data";
                dataCastExpr = "(void**)" + ptr;
            }
            writer.Write("        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"CopyToManaged\")]\n");
            writer.Write("        static extern void CopyToManaged_");
            writer.Write(raw);
            writer.Write("([UnsafeAccessorType(\"");
            writer.Write(ArrayElementEncoder.GetArrayMarshallerInteropPath(szArr.BaseType));
            writer.Write("\")] object _, uint length, ");
            writer.Write(dataParamType);
            writer.Write(", Span<");
            writer.Write(elementProjected);
            writer.Write("> span);\n");
            writer.Write("        CopyToManaged_");
            writer.Write(raw);
            writer.Write("(null, __");
            writer.Write(raw);
            writer.Write("Size, ");
            writer.Write(dataCastExpr);
            writer.Write(", __");
            writer.Write(raw);
            writer.Write(");\n");
        }

        // For generic instance ABI input parameters, emit local UnsafeAccessor delegates and locals
        // first so the call site can reference them.
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            if (p.Type.IsNullableT())
            {
                // Nullable<T> param (server-side): use <T>Marshaller.UnboxToManaged. Mirrors truth pattern.
                string rawName = p.Parameter.Name ?? "param";
                string callName = CSharpKeywords.IsKeyword(rawName) ? "@" + rawName : rawName;
                AsmResolver.DotNet.Signatures.TypeSignature inner = p.Type.GetNullableInnerType()!;
                string innerMarshaller = CodeWriters.GetNullableInnerMarshallerName(writer, context, inner);
                writer.Write("        var __arg_");
                writer.Write(rawName);
                writer.Write(" = ");
                writer.Write(innerMarshaller);
                writer.Write(".UnboxToManaged(");
                writer.Write(callName);
                writer.Write(");\n");
            }
            else if (p.Type.IsGenericInstance())
            {
                string rawName = p.Parameter.Name ?? "param";
                string callName = CSharpKeywords.IsKeyword(rawName) ? "@" + rawName : rawName;
                string interopTypeName = InteropTypeNameWriter.EncodeInteropTypeName(p.Type, TypedefNameType.ABI) + ", WinRT.Interop";
                IndentedTextWriter __scratchProjectedTypeName = new();
                MethodFactory.WriteProjectedSignature(__scratchProjectedTypeName, context, p.Type, false);
                string projectedTypeName = __scratchProjectedTypeName.ToString();
                writer.Write("        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"ConvertToManaged\")]\n");
                writer.Write("        static extern ");
                writer.Write(projectedTypeName);
                writer.Write(" ConvertToManaged_arg_");
                writer.Write(rawName);
                writer.Write("([UnsafeAccessorType(\"");
                writer.Write(interopTypeName);
                writer.Write("\")] object _, void* value);\n");
                writer.Write("        var __arg_");
                writer.Write(rawName);
                writer.Write(" = ConvertToManaged_arg_");
                writer.Write(rawName);
                writer.Write("(null, ");
                writer.Write(callName);
                writer.Write(");\n");
            }
        }

        if (returnIsString)
        {
            writer.Write("        ");
            writer.Write(retLocalName);
            writer.Write(" = ");
        }
        else if (returnIsRefType)
        {
            writer.Write("        ");
            writer.Write(retLocalName);
            writer.Write(" = ");
        }
        else if (returnIsReceiveArrayDoAbi)
        {
            // For T[] return: assign to existing local.
            writer.Write("        ");
            writer.Write(retLocalName);
            writer.Write(" = ");
        }
        else if (rt is not null)
        {
            writer.Write("        ");
            writer.Write(retLocalName);
            writer.Write(" = ");
        }
        else
        {
            writer.Write("        ");
        }

        if (isGetter)
        {
            string propName = methodName[4..];
            writer.Write("ComInterfaceDispatch.GetInstance<");
            writer.Write(ifaceFullName);
            writer.Write(">((ComInterfaceDispatch*)thisPtr).");
            writer.Write(propName);
            writer.Write(";\n");
        }
        else if (isSetter)
        {
            string propName = methodName[4..];
            writer.Write("ComInterfaceDispatch.GetInstance<");
            writer.Write(ifaceFullName);
            writer.Write(">((ComInterfaceDispatch*)thisPtr).");
            writer.Write(propName);
            writer.Write(" = ");
            EmitDoAbiParamArgConversion(writer, context, sig.Params[0]);
            writer.Write(";\n");
        }
        else
        {
            writer.Write("ComInterfaceDispatch.GetInstance<");
            writer.Write(ifaceFullName);
            writer.Write(">((ComInterfaceDispatch*)thisPtr).");
            writer.Write(methodName);
            writer.Write("(");
            for (int i = 0; i < sig.Params.Count; i++)
            {
                if (i > 0) { writer.Write(",\n  "); }
                ParamInfo p = sig.Params[i];
                ParamCategory cat = ParamHelpers.GetParamCategory(p);
                if (cat == ParamCategory.Out)
                {
                    string raw = p.Parameter.Name ?? "param";
                    writer.Write("out __");
                    writer.Write(raw);
                }
                else if (cat == ParamCategory.Ref)
                {
                    // WinRT 'in T' / 'ref const T' is a read-only by-ref input on the ABI side
                    // (pointer to a value the native caller owns). On the C# delegate / interface
                    // side it's projected as 'in T'. Read directly from *<name> via the appropriate
                    // marshaller — DO NOT zero or write back.
                    string raw = p.Parameter.Name ?? "param";
                    string ptr = CSharpKeywords.IsKeyword(raw) ? "@" + raw : raw;
                    AsmResolver.DotNet.Signatures.TypeSignature uRef = CodeWriters.StripByRefAndCustomModifiers(p.Type);
                    if (uRef.IsString())
                    {
                        writer.Write("HStringMarshaller.ConvertToManaged(*");
                        writer.Write(ptr);
                        writer.Write(")");
                    }
                    else if (uRef.IsObject())
                    {
                        writer.Write("WindowsRuntimeObjectMarshaller.ConvertToManaged(*");
                        writer.Write(ptr);
                        writer.Write(")");
                    }
                    else if (CodeWriters.IsRuntimeClassOrInterface(context.Cache, uRef))
                    {
                        writer.Write(CodeWriters.GetMarshallerFullName(writer, context, uRef));
                        writer.Write(".ConvertToManaged(*");
                        writer.Write(ptr);
                        writer.Write(")");
                    }
                    else if (CodeWriters.IsMappedAbiValueType(uRef))
                    {
                        writer.Write(CodeWriters.GetMappedMarshallerName(uRef));
                        writer.Write(".ConvertToManaged(*");
                        writer.Write(ptr);
                        writer.Write(")");
                    }
                    else if (uRef.IsHResultException())
                    {
                        writer.Write("global::ABI.System.ExceptionMarshaller.ConvertToManaged(*");
                        writer.Write(ptr);
                        writer.Write(")");
                    }
                    else if (CodeWriters.IsComplexStruct(context.Cache, uRef))
                    {
                        writer.Write(CodeWriters.GetMarshallerFullName(writer, context, uRef));
                        writer.Write(".ConvertToManaged(*");
                        writer.Write(ptr);
                        writer.Write(")");
                    }
                    else if (CodeWriters.IsAnyStruct(context.Cache, uRef) || CodeWriters.IsBlittablePrimitive(context.Cache, uRef) || CodeWriters.IsEnumType(context.Cache, uRef))
                    {
                        // Blittable/almost-blittable: ABI layout matches projected layout.
                        writer.Write("*");
                        writer.Write(ptr);
                    }
                    else
                    {
                        writer.Write("*");
                        writer.Write(ptr);
                    }
                }
                else if (cat == ParamCategory.PassArray || cat == ParamCategory.FillArray)
                {
                    string raw = p.Parameter.Name ?? "param";
                    writer.Write("__");
                    writer.Write(raw);
                }
                else if (cat == ParamCategory.ReceiveArray)
                {
                    string raw = p.Parameter.Name ?? "param";
                    writer.Write("out __");
                    writer.Write(raw);
                }
                else
                {
                    EmitDoAbiParamArgConversion(writer, context, p);
                }
            }
            writer.Write(");\n");
        }
        // After call: write back out params to caller's pointer.
        // NOTE: Ref params (WinRT 'in T') are read-only inputs — never written back.
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.Out) { continue; }
            string raw = p.Parameter.Name ?? "param";
            string ptr = CSharpKeywords.IsKeyword(raw) ? "@" + raw : raw;
            AsmResolver.DotNet.Signatures.TypeSignature underlying = CodeWriters.StripByRefAndCustomModifiers(p.Type);
            writer.Write("        *");
            writer.Write(ptr);
            writer.Write(" = ");
            // String: HStringMarshaller.ConvertToUnmanaged
            if (underlying.IsString())
            {
                writer.Write("HStringMarshaller.ConvertToUnmanaged(__");
                writer.Write(raw);
                writer.Write(")");
            }
            // Object/runtime class: <Marshaller>.ConvertToUnmanaged(...).DetachThisPtrUnsafe()
            else if (underlying.IsObject())
            {
                writer.Write("WindowsRuntimeObjectMarshaller.ConvertToUnmanaged(__");
                writer.Write(raw);
                writer.Write(").DetachThisPtrUnsafe()");
            }
            else if (CodeWriters.IsRuntimeClassOrInterface(context.Cache, underlying))
            {
                writer.Write(CodeWriters.GetMarshallerFullName(writer, context, underlying));
                writer.Write(".ConvertToUnmanaged(__");
                writer.Write(raw);
                writer.Write(").DetachThisPtrUnsafe()");
            }
            // Generic instance (e.g. IEnumerable<string>): use the hoisted UnsafeAccessor
            // 'ConvertToUnmanaged_<name>' declared at the top of the method body.
            else if (underlying.IsGenericInstance())
            {
                writer.Write("ConvertToUnmanaged_");
                writer.Write(raw);
                writer.Write("(null, __");
                writer.Write(raw);
                writer.Write(").DetachThisPtrUnsafe()");
            }
            // For enums, function pointer signature uses the projected enum type, no cast needed.
            // For bool, cast to byte. For char, cast to ushort.
            else if (CodeWriters.IsEnumType(context.Cache, underlying))
            {
                writer.Write("__");
                writer.Write(raw);
            }
            else if (underlying is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlibBool &&
                     corlibBool.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Boolean)
            {
                writer.Write("__");
                writer.Write(raw);
            }
            else if (underlying is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlibChar &&
                     corlibChar.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Char)
            {
                writer.Write("__");
                writer.Write(raw);
            }
            // Non-blittable struct (e.g. authored BasicStruct with string fields): marshal
            // the local managed value through <Type>Marshaller.ConvertToUnmanaged before
            // writing it into the *out ABI struct slot. Mirrors C++ marshaler.write_marshal_from_managed
            //: "Marshaller.ConvertToUnmanaged(local)".
            else if (CodeWriters.IsComplexStruct(context.Cache, underlying))
            {
                writer.Write(CodeWriters.GetMarshallerFullName(writer, context, underlying));
                writer.Write(".ConvertToUnmanaged(__");
                writer.Write(raw);
                writer.Write(")");
            }
            else
            {
                writer.Write("__");
                writer.Write(raw);
            }
            writer.Write(";\n");
        }
        // After call: for ReceiveArray params, emit ConvertToUnmanaged_<name> call (the
        // [UnsafeAccessor] declaration was hoisted to the top of the method body).
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.ReceiveArray) { continue; }
            string raw = p.Parameter.Name ?? "param";
            string ptr = CSharpKeywords.IsKeyword(raw) ? "@" + raw : raw;
            writer.Write("        ConvertToUnmanaged_");
            writer.Write(raw);
            writer.Write("(null, __");
            writer.Write(raw);
            writer.Write(", out *__");
            writer.Write(raw);
            writer.Write("Size, out *");
            writer.Write(ptr);
            writer.Write(");\n");
        }
        // After call: for non-blittable FillArray params (Span<T> where T is string/runtime
        // class/object/non-blittable struct), copy the managed delegate's writes back into the
        // native ABI buffer. Mirrors C++ write_marshal_from_managed
        // which emits 'CopyToUnmanaged_<name>(null, __<name>, __<name>Size, (T*)<name>)'.
        // Blittable element types don't need this — the Span wraps the native buffer directly.
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.FillArray) { continue; }
            if (p.Type is not AsmResolver.DotNet.Signatures.SzArrayTypeSignature szFA) { continue; }
            // Blittable element types: Span wraps the native buffer; no copy-back needed.
            if (CodeWriters.IsBlittablePrimitive(context.Cache, szFA.BaseType) || CodeWriters.IsAnyStruct(context.Cache, szFA.BaseType)) { continue; }
            string raw = p.Parameter.Name ?? "param";
            string ptr = CSharpKeywords.IsKeyword(raw) ? "@" + raw : raw;
            IndentedTextWriter __scratchElementProjected = new();
            TypedefNameWriter.WriteProjectionType(__scratchElementProjected, context, TypeSemanticsFactory.Get(szFA.BaseType));
            string elementProjected = __scratchElementProjected.ToString();
            string elementInteropArg = InteropTypeNameWriter.EncodeInteropTypeName(szFA.BaseType, TypedefNameType.Projected);

            _ = elementInteropArg;
            // Determine the ABI element type for the data pointer cast.
            // - Strings / runtime classes / objects: void**
            // - HResult exception: global::ABI.System.Exception*
            // - Mapped value types (DateTime/TimeSpan): global::ABI.System.{DateTimeOffset/TimeSpan}*
            // - Complex structs: <ABI struct>*
            string dataParamType;
            string dataCastType;
            if (szFA.BaseType.IsString() || CodeWriters.IsRuntimeClassOrInterface(context.Cache, szFA.BaseType) || szFA.BaseType.IsObject())
            {
                dataParamType = "void** data";
                dataCastType = "(void**)";
            }
            else if (szFA.BaseType.IsHResultException())
            {
                dataParamType = "global::ABI.System.Exception* data";
                dataCastType = "(global::ABI.System.Exception*)";
            }
            else if (CodeWriters.IsMappedAbiValueType(szFA.BaseType))
            {
                string abiName = CodeWriters.GetMappedAbiTypeName(szFA.BaseType);
                dataParamType = abiName + "* data";
                dataCastType = "(" + abiName + "*)";
            }
            else
            {
                string abiStructName = CodeWriters.GetAbiStructTypeName(writer, context, szFA.BaseType);
                dataParamType = abiStructName + "* data";
                dataCastType = "(" + abiStructName + "*)";
            }
            writer.Write("        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"CopyToUnmanaged\")]\n");
            writer.Write("        static extern void CopyToUnmanaged_");
            writer.Write(raw);
            writer.Write("([UnsafeAccessorType(\"");
            writer.Write(ArrayElementEncoder.GetArrayMarshallerInteropPath(szFA.BaseType));
            writer.Write("\")] object _, ReadOnlySpan<");
            writer.Write(elementProjected);
            writer.Write("> span, uint length, ");
            writer.Write(dataParamType);
            writer.Write(");\n");
            writer.Write("        CopyToUnmanaged_");
            writer.Write(raw);
            writer.Write("(null, __");
            writer.Write(raw);
            writer.Write(", __");
            writer.Write(raw);
            writer.Write("Size, ");
            writer.Write(dataCastType);
            writer.Write(ptr);
            writer.Write(");\n");
        }
        if (rt is not null)
        {
            if (returnIsHResultExceptionDoAbi)
            {
                writer.Write("        *");
                writer.Write(retParamName);
                writer.Write(" = global::ABI.System.ExceptionMarshaller.ConvertToUnmanaged(");
                writer.Write(retLocalName);
                writer.Write(");\n");
            }
            else if (returnIsString)
            {
                writer.Write("        *");
                writer.Write(retParamName);
                writer.Write(" = HStringMarshaller.ConvertToUnmanaged(");
                writer.Write(retLocalName);
                writer.Write(");\n");
            }
            else if (returnIsRefType)
            {
                if (rt is not null && rt.IsNullableT())
                {
                    // Nullable<T> return (server-side): use <T>Marshaller.BoxToUnmanaged.
                    AsmResolver.DotNet.Signatures.TypeSignature inner = rt.GetNullableInnerType()!;
                    string innerMarshaller = CodeWriters.GetNullableInnerMarshallerName(writer, context, inner);
                    writer.Write("        *");
                    writer.Write(retParamName);
                    writer.Write(" = ");
                    writer.Write(innerMarshaller);
                    writer.Write(".BoxToUnmanaged(");
                    writer.Write(retLocalName);
                    writer.Write(").DetachThisPtrUnsafe();\n");
                }
                else if (returnIsGenericInstance)
                {
                    // Generic instance return: use the UnsafeAccessor static local function declared at
                    // the top of the method body via the M12 hoisting pass; just emit the call here.
                    writer.Write("        *");
                    writer.Write(retParamName);
                    writer.Write(" = ConvertToUnmanaged_");
                    writer.Write(retParamName);
                    writer.Write("(null, ");
                    writer.Write(retLocalName);
                    writer.Write(").DetachThisPtrUnsafe();\n");
                }
                else
                {
                    writer.Write("        *");
                    writer.Write(retParamName);
                    writer.Write(" = ");
                    EmitMarshallerConvertToUnmanaged(writer, context, rt!, retLocalName);
                    writer.Write(".DetachThisPtrUnsafe();\n");
                }
            }
            else if (returnIsReceiveArrayDoAbi)
            {
                // Return-receive-array: emit ConvertToUnmanaged_<retParam> call (declaration
                // was hoisted to the top of the method body).
                writer.Write("        ConvertToUnmanaged_");
                writer.Write(retParamName);
                writer.Write("(null, ");
                writer.Write(retLocalName);
                writer.Write(", out *");
                writer.Write(retSizeParamName);
                writer.Write(", out *");
                writer.Write(retParamName);
                writer.Write(");\n");
            }
            else if (CodeWriters.IsMappedAbiValueType(rt))
            {
                // Mapped value type return (DateTime/TimeSpan): convert via marshaller.
                writer.Write("        *");
                writer.Write(retParamName);
                writer.Write(" = ");
                writer.Write(CodeWriters.GetMappedMarshallerName(rt));
                writer.Write(".ConvertToUnmanaged(");
                writer.Write(retLocalName);
                writer.Write(");\n");
            }
            else if (rt.IsSystemType())
            {
                // System.Type return (server-side): convert managed System.Type to ABI Type struct.
                writer.Write("        *");
                writer.Write(retParamName);
                writer.Write(" = global::ABI.System.TypeMarshaller.ConvertToUnmanaged(");
                writer.Write(retLocalName);
                writer.Write(");\n");
            }
            else if (CodeWriters.IsComplexStruct(context.Cache, rt))
            {
                // Complex struct return (server-side): convert managed struct to ABI struct via marshaller.
                writer.Write("        *");
                writer.Write(retParamName);
                writer.Write(" = ");
                writer.Write(CodeWriters.GetMarshallerFullName(writer, context, rt));
                writer.Write(".ConvertToUnmanaged(");
                writer.Write(retLocalName);
                writer.Write(");\n");
            }
            else if (returnIsBlittableStruct)
            {
                writer.Write("        *");
                writer.Write(retParamName);
                writer.Write(" = ");
                writer.Write(retLocalName);
                writer.Write(";\n");
            }
            else
            {
                string abiType = CodeWriters.GetAbiPrimitiveType(context.Cache, rt);
                writer.Write("        *");
                writer.Write(retParamName);
                writer.Write(" = ");
                if (rt is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlib &&
                    corlib.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Boolean)
                {
                    writer.Write(retLocalName);
                    writer.Write(";\n");
                }
                else if (rt is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlib2 &&
                         corlib2.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Char)
                {
                    writer.Write(retLocalName);
                    writer.Write(";\n");
                }
                else if (CodeWriters.IsEnumType(context.Cache, rt))
                {
                    // Enum: function pointer signature uses the projected enum type, no cast needed.
                    writer.Write(retLocalName);
                    writer.Write(";\n");
                }
                else
                {
                    writer.Write(retLocalName);
                    writer.Write(";\n");
                }
            }
        }
        writer.Write("        return 0;\n    }\n");
        writer.Write("    catch (Exception __exception__)\n    {\n");
        writer.Write("        return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(__exception__);\n    }\n");

        // For non-blittable PassArray params, emit finally block with ArrayPool<T>.Shared.Return.
        bool hasNonBlittableArrayDoAbi = false;
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.PassArray && cat != ParamCategory.FillArray) { continue; }
            if (p.Type is not AsmResolver.DotNet.Signatures.SzArrayTypeSignature szArr) { continue; }
            if (CodeWriters.IsBlittablePrimitive(context.Cache, szArr.BaseType) || CodeWriters.IsAnyStruct(context.Cache, szArr.BaseType)) { continue; }
            hasNonBlittableArrayDoAbi = true;
            break;
        }
        if (hasNonBlittableArrayDoAbi)
        {
            writer.Write("    finally\n    {\n");
            for (int i = 0; i < sig.Params.Count; i++)
            {
                ParamInfo p = sig.Params[i];
                ParamCategory cat = ParamHelpers.GetParamCategory(p);
                if (cat != ParamCategory.PassArray && cat != ParamCategory.FillArray) { continue; }
                if (p.Type is not AsmResolver.DotNet.Signatures.SzArrayTypeSignature szArr) { continue; }
                if (CodeWriters.IsBlittablePrimitive(context.Cache, szArr.BaseType) || CodeWriters.IsAnyStruct(context.Cache, szArr.BaseType)) { continue; }
                string raw = p.Parameter.Name ?? "param";
                IndentedTextWriter __scratchElementProjected = new();
                TypedefNameWriter.WriteProjectionType(__scratchElementProjected, context, TypeSemanticsFactory.Get(szArr.BaseType));
                string elementProjected = __scratchElementProjected.ToString();
                writer.Write("\n        if (__");
                writer.Write(raw);
                writer.Write("_arrayFromPool is not null)\n        {\n");
                writer.Write("            global::System.Buffers.ArrayPool<");
                writer.Write(elementProjected);
                writer.Write(">.Shared.Return(__");
                writer.Write(raw);
                writer.Write("_arrayFromPool);\n        }\n");
            }
            writer.Write("    }\n");
        }

        writer.Write("}\n\n");
        _ = hasStringParams;
    }

    /// <summary>Converts an ABI parameter to its projected (managed) form for the Do_Abi call.</summary>
    internal static void EmitDoAbiParamArgConversion(IndentedTextWriter writer, ProjectionEmitContext context, ParamInfo p)
    {
        string rawName = p.Parameter.Name ?? "param";
        string pname = CSharpKeywords.IsKeyword(rawName) ? "@" + rawName : rawName;
        if (p.Type is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlib &&
            corlib.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Boolean)
        {
            writer.Write(pname);
        }
        else if (p.Type is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlib2 &&
                 corlib2.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Char)
        {
            writer.Write(pname);
        }
        else if (p.Type is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlibStr &&
                 corlibStr.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.String)
        {
            writer.Write("HStringMarshaller.ConvertToManaged(");
            writer.Write(pname);
            writer.Write(")");
        }
        else if (p.Type.IsGenericInstance())
        {
            // Generic instance ABI parameter: caller already declared a local UnsafeAccessor +
            // local var __arg_<name> that holds the converted value.
            writer.Write("__arg_");
            writer.Write(rawName);
        }
        else if (CodeWriters.IsRuntimeClassOrInterface(context.Cache, p.Type) || p.Type.IsObject())
        {
            EmitMarshallerConvertToManaged(writer, context, p.Type, pname);
        }
        else if (CodeWriters.IsMappedAbiValueType(p.Type))
        {
            // Mapped value type input (DateTime/TimeSpan): the parameter is the ABI type;
            // convert to the projected managed type via the marshaller.
            writer.Write(CodeWriters.GetMappedMarshallerName(p.Type));
            writer.Write(".ConvertToManaged(");
            writer.Write(pname);
            writer.Write(")");
        }
        else if (p.Type.IsSystemType())
        {
            // System.Type input (server-side): convert ABI Type struct to System.Type.
            writer.Write("global::ABI.System.TypeMarshaller.ConvertToManaged(");
            writer.Write(pname);
            writer.Write(")");
        }
        else if (CodeWriters.IsComplexStruct(context.Cache, p.Type))
        {
            // Complex struct input (server-side): convert ABI struct to managed via marshaller.
            writer.Write(CodeWriters.GetMarshallerFullName(writer, context, p.Type));
            writer.Write(".ConvertToManaged(");
            writer.Write(pname);
            writer.Write(")");
        }
        else if (CodeWriters.IsAnyStruct(context.Cache, p.Type))
        {
            // Blittable / almost-blittable struct: pass directly (projected type == ABI type).
            writer.Write(pname);
        }
        else if (CodeWriters.IsEnumType(context.Cache, p.Type))
        {
            // Enum: param signature is already the projected enum type, no cast needed.
            writer.Write(pname);
        }
        else
        {
            writer.Write(pname);
        }
    }

    /// <summary>
    /// Emits the [UnsafeAccessor] declaration for the default interface IID inside a file-scoped
    /// ComWrappers class. Only emits if the default interface is a generic instantiation.
    /// behavior of inserting <c>write_unsafe_accessor_for_iid</c> at the top of the class body.
    /// </summary>
    internal static void EmitUnsafeAccessorForDefaultIfaceIfGeneric(IndentedTextWriter writer, ProjectionEmitContext context, ITypeDefOrRef? defaultIface)
    {
        if (defaultIface is TypeSpecification ts && ts.Signature is GenericInstanceTypeSignature gi)
        {
            ObjRefNameGenerator.EmitUnsafeAccessorForIid(writer, context, gi);
        }
    }

    /// <summary>
    /// Emits the per-interface members (methods, properties, events) into an already-open Methods
    /// static class. Used both for the standalone case and for the fast-abi merged emission.
    /// </summary>
    internal static void EmitMethodsClassMembersFor(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type, int startSlot, bool skipExclusiveEvents)
    {
        // Build a map from each MethodDefinition to its WinMD vtable slot.
        // In AsmResolver, type.Methods is iterated in MethodDef row order, so the position of each
        // method in type.Methods (relative to the first method of the type) gives us the same value.
        Dictionary<MethodDefinition, int> methodSlot = new();
        {
            int idx = 0;
            foreach (MethodDefinition m in type.Methods)
            {
                methodSlot[m] = idx + startSlot;
                idx++;
            }
        }

        // Emit non-special methods first (output order is unchanged from before; only the slot lookup changes).
        foreach (MethodDefinition method in type.Methods)
        {
            if (method.IsSpecial()) { continue; }
            string mname = method.Name?.Value ?? string.Empty;
            MethodSig sig = new(method);

            writer.Write("    [MethodImpl(MethodImplOptions.NoInlining)]\n");
            writer.Write("    public static unsafe ");
            MethodFactory.WriteProjectionReturnType(writer, context, sig);
            writer.Write(" ");
            writer.Write(mname);
            writer.Write("(WindowsRuntimeObjectReference thisReference");
            if (sig.Params.Count > 0) { writer.Write(", "); }
            MethodFactory.WriteParameterList(writer, context, sig);
            writer.Write(")");

            // Emit the body if we can handle this case. Slot comes from the method's WinMD index.
            EmitAbiMethodBodyIfSimple(writer, context, sig, methodSlot[method], isNoExcept: method.IsNoExcept());
        }

        // Emit property accessors. Each getter / setter consumes one vtable slot — looked up from the underlying method.
        foreach (PropertyDefinition prop in type.Properties)
        {
            string pname = prop.Name?.Value ?? string.Empty;
            (MethodDefinition? getter, MethodDefinition? setter) = prop.GetPropertyMethods();
            string propType = CodeWriters.WritePropType(context, prop);
            (MethodDefinition? gMethod, MethodDefinition? sMethod) = (getter, setter);
            // accessors of the property (the attribute is on the property itself, not on the
            // individual accessors).
            bool propIsNoExcept = prop.IsNoExcept();
            if (gMethod is not null)
            {
                MethodSig getSig = new(gMethod);
                writer.Write("    [MethodImpl(MethodImplOptions.NoInlining)]\n");
                writer.Write("    public static unsafe ");
                writer.Write(propType);
                writer.Write(" ");
                writer.Write(pname);
                writer.Write("(WindowsRuntimeObjectReference thisReference)");
                EmitAbiMethodBodyIfSimple(writer, context, getSig, methodSlot[gMethod], isNoExcept: propIsNoExcept);
            }
            if (sMethod is not null)
            {
                MethodSig setSig = new(sMethod);
                writer.Write("    [MethodImpl(MethodImplOptions.NoInlining)]\n");
                writer.Write("    public static unsafe void ");
                writer.Write(pname);
                writer.Write("(WindowsRuntimeObjectReference thisReference, ");
                // form of write_prop_type, which for SZ array types emits ReadOnlySpan<T> instead
                // of T[] (the getter's return-type form).
                writer.Write(CodeWriters.WritePropType(context, prop, isSetProperty: true));
                writer.Write(" value)");
                EmitAbiMethodBodyIfSimple(writer, context, setSig, methodSlot[sMethod], paramNameOverride: "value", isNoExcept: propIsNoExcept);
            }
        }

        // Emit event member methods (returns an event source, takes thisObject + thisReference).
        // Skip events on exclusive interfaces used by their class — they're inlined directly in
        // the RCW class. (Mirrors C++ skip_exclusive_events.)
        foreach (EventDefinition evt in type.Events)
        {
            if (skipExclusiveEvents) { continue; }
            string evtName = evt.Name?.Value ?? string.Empty;
            AsmResolver.DotNet.Signatures.TypeSignature evtSig = evt.EventType!.ToTypeSignature(false);
            bool isGenericEvent = evtSig is AsmResolver.DotNet.Signatures.GenericInstanceTypeSignature;

            // Use the add method's WinMD slot. Mirrors C++: events use the add_X method's vmethod_index.
            (MethodDefinition? addMethod, MethodDefinition? _) = evt.GetEventMethods();
            int eventSlot = addMethod is not null && methodSlot.TryGetValue(addMethod, out int es) ? es : 0;

            // Build the projected event source type name. For non-generic delegate handlers, the
            // EventSource subclass lives in the ABI namespace alongside this Methods class, so
            // we need to use the ABI-qualified name. For generic handlers (Windows.Foundation.*EventHandler),
            // it's mapped to global::WindowsRuntime.InteropServices.EventHandlerEventSource<...>.
            string eventSourceProjectedFull;
            if (isGenericEvent)
            {
                IndentedTextWriter __scratchEvSrcGeneric = new();
                TypedefNameWriter.WriteTypeName(__scratchEvSrcGeneric, context, TypeSemanticsFactory.Get(evtSig), TypedefNameType.EventSource, true);
                eventSourceProjectedFull = __scratchEvSrcGeneric.ToString();
                if (!eventSourceProjectedFull.StartsWith("global::", System.StringComparison.Ordinal))
                {
                    eventSourceProjectedFull = "global::" + eventSourceProjectedFull;
                }
            }
            else
            {
                // Non-generic delegate handler: the EventSource lives in the same ABI namespace
                // as this Methods class, so we use just the short name (matches truth output).
                string delegateName = string.Empty;
                if (evtSig is AsmResolver.DotNet.Signatures.TypeDefOrRefSignature td)
                {
                    delegateName = td.Type?.Name?.Value ?? string.Empty;
                    delegateName = IdentifierEscaping.StripBackticks(delegateName);
                }
                eventSourceProjectedFull = delegateName + "EventSource";
            }
            string eventSourceInteropType = isGenericEvent
                ? InteropTypeNameWriter.EncodeInteropTypeName(evtSig, TypedefNameType.EventSource) + ", WinRT.Interop"
                : string.Empty;

            // Emit the per-event ConditionalWeakTable static field.
            writer.Write("\n    private static ConditionalWeakTable<object, ");
            writer.Write(eventSourceProjectedFull);
            writer.Write("> _");
            writer.Write(evtName);
            writer.Write("\n    {\n");
            writer.Write("        [MethodImpl(MethodImplOptions.AggressiveInlining)]\n");
            writer.Write("        get\n        {\n");
            writer.Write("            [MethodImpl(MethodImplOptions.NoInlining)]\n");
            writer.Write("            static ConditionalWeakTable<object, ");
            writer.Write(eventSourceProjectedFull);
            writer.Write("> MakeTable()\n            {\n");
            writer.Write("                _ = global::System.Threading.Interlocked.CompareExchange(ref field, [], null);\n\n");
            writer.Write("                return global::System.Threading.Volatile.Read(in field);\n");
            writer.Write("            }\n\n");
            writer.Write("            return global::System.Threading.Volatile.Read(in field) ?? MakeTable();\n        }\n    }\n");

            // Emit the static method that returns the per-instance event source.
            writer.Write("\n    public static ");
            writer.Write(eventSourceProjectedFull);
            writer.Write(" ");
            writer.Write(evtName);
            writer.Write("(object thisObject, WindowsRuntimeObjectReference thisReference)\n    {\n");
            if (isGenericEvent && !string.IsNullOrEmpty(eventSourceInteropType))
            {
                writer.Write("        [UnsafeAccessor(UnsafeAccessorKind.Constructor)]\n");
                writer.Write("        [return: UnsafeAccessorType(\"");
                writer.Write(eventSourceInteropType);
                writer.Write("\")]\n");
                writer.Write("        static extern object ctor(WindowsRuntimeObjectReference nativeObjectReference, int index);\n\n");
                writer.Write("        return _");
                writer.Write(evtName);
                writer.Write(".GetOrAdd(\n");
                writer.Write("            key: thisObject,\n");
                writer.Write("            valueFactory: static (_, thisReference) => Unsafe.As<");
                writer.Write(eventSourceProjectedFull);
                writer.Write(">(ctor(thisReference, ");
                writer.Write(eventSlot.ToString(System.Globalization.CultureInfo.InvariantCulture));
                writer.Write(")),\n");
                writer.Write("            factoryArgument: thisReference);\n");
            }
            else
            {
                // Non-generic delegate: directly construct.
                writer.Write("        return _");
                writer.Write(evtName);
                writer.Write(".GetOrAdd(\n");
                writer.Write("            key: thisObject,\n");
                writer.Write("            valueFactory: static (_, thisReference) => new ");
                writer.Write(eventSourceProjectedFull);
                writer.Write("(thisReference, ");
                writer.Write(eventSlot.ToString(System.Globalization.CultureInfo.InvariantCulture));
                writer.Write("),\n");
                writer.Write("            factoryArgument: thisReference);\n");
            }
            writer.Write("    }\n");
        }
    }

    /// <summary>
    /// Emits a real method body for the cases we can fully marshal, otherwise emits
    /// the 'throw null!' stub. Trailing newline is included.
    /// </summary>
    /// <param name="isNoExcept">When true, the vtable call is emitted WITHOUT the
    /// <c>RestrictedErrorInfo.ThrowExceptionForHR(...)</c> wrap. Mirrors C++
    /// <c>code_writers.h:6725</c> which checks <c>has_noexcept_attr</c>
    /// (<c>is_noexcept(MethodDef)</c> / <c>is_noexcept(Property)</c> in <c>helpers.h:41-49</c>):
    /// methods/properties annotated with <c>[Windows.Foundation.Metadata.NoExceptionAttribute]</c>
    /// (or remove-overload methods) contractually return <c>S_OK</c>, so the wrap is omitted.</param>
    internal static void EmitAbiMethodBodyIfSimple(IndentedTextWriter writer, ProjectionEmitContext context, MethodSig sig, int slot, string? paramNameOverride = null, bool isNoExcept = false)
    {
        AsmResolver.DotNet.Signatures.TypeSignature? rt = sig.ReturnType;

        bool returnIsString = rt is not null && rt.IsString();
        bool returnIsRefType = rt is not null && (CodeWriters.IsRuntimeClassOrInterface(context.Cache, rt) || rt.IsObject() || rt.IsGenericInstance());
        bool returnIsAnyStruct = rt is not null && CodeWriters.IsAnyStruct(context.Cache, rt);
        bool returnIsComplexStruct = rt is not null && CodeWriters.IsComplexStruct(context.Cache, rt);
        bool returnIsReceiveArray = rt is AsmResolver.DotNet.Signatures.SzArrayTypeSignature retSzCheck
            && (CodeWriters.IsBlittablePrimitive(context.Cache, retSzCheck.BaseType) || CodeWriters.IsAnyStruct(context.Cache, retSzCheck.BaseType)
                || retSzCheck.BaseType.IsString() || CodeWriters.IsRuntimeClassOrInterface(context.Cache, retSzCheck.BaseType) || retSzCheck.BaseType.IsObject()
                || CodeWriters.IsComplexStruct(context.Cache, retSzCheck.BaseType)
                || retSzCheck.BaseType.IsHResultException()
                || CodeWriters.IsMappedAbiValueType(retSzCheck.BaseType));
        bool returnIsHResultException = rt is not null && rt.IsHResultException();

        // Build the function pointer signature: void*, [paramAbiType...,] [retAbiType*,] int
        System.Text.StringBuilder fp = new();
        fp.Append("void*");
        foreach (ParamInfo p in sig.Params)
        {
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat == ParamCategory.PassArray || cat == ParamCategory.FillArray)
            {
                fp.Append(", uint, void*");
                continue;
            }
            if (cat == ParamCategory.Out)
            {
                AsmResolver.DotNet.Signatures.TypeSignature uOut = CodeWriters.StripByRefAndCustomModifiers(p.Type);
                fp.Append(", ");
                if (uOut.IsString() || CodeWriters.IsRuntimeClassOrInterface(context.Cache, uOut) || uOut.IsObject() || uOut.IsGenericInstance()) { fp.Append("void**"); }
                else if (uOut.IsSystemType()) { fp.Append("global::ABI.System.Type*"); }
                else if (CodeWriters.IsComplexStruct(context.Cache, uOut)) { fp.Append(CodeWriters.GetAbiStructTypeName(writer, context, uOut)); fp.Append('*'); }
                else if (CodeWriters.IsAnyStruct(context.Cache, uOut)) { fp.Append(CodeWriters.GetBlittableStructAbiType(writer, context, uOut)); fp.Append('*'); }
                else { fp.Append(CodeWriters.GetAbiPrimitiveType(context.Cache, uOut)); fp.Append('*'); }
                continue;
            }
            if (cat == ParamCategory.Ref)
            {
                AsmResolver.DotNet.Signatures.TypeSignature uRef = CodeWriters.StripByRefAndCustomModifiers(p.Type);
                fp.Append(", ");
                if (CodeWriters.IsComplexStruct(context.Cache, uRef)) { fp.Append(CodeWriters.GetAbiStructTypeName(writer, context, uRef)); fp.Append('*'); }
                else if (CodeWriters.IsAnyStruct(context.Cache, uRef)) { fp.Append(CodeWriters.GetBlittableStructAbiType(writer, context, uRef)); fp.Append('*'); }
                else { fp.Append(CodeWriters.GetAbiPrimitiveType(context.Cache, uRef)); fp.Append('*'); }
                continue;
            }
            if (cat == ParamCategory.ReceiveArray)
            {
                AsmResolver.DotNet.Signatures.SzArrayTypeSignature sza = (AsmResolver.DotNet.Signatures.SzArrayTypeSignature)CodeWriters.StripByRefAndCustomModifiers(p.Type);
                fp.Append(", uint*, ");
                if (sza.BaseType.IsString() || CodeWriters.IsRuntimeClassOrInterface(context.Cache, sza.BaseType) || sza.BaseType.IsObject())
                {
                    fp.Append("void*");
                }
                else if (sza.BaseType.IsHResultException())
                {
                    fp.Append("global::ABI.System.Exception");
                }
                else if (CodeWriters.IsMappedAbiValueType(sza.BaseType))
                {
                    fp.Append(CodeWriters.GetMappedAbiTypeName(sza.BaseType));
                }
                else if (CodeWriters.IsComplexStruct(context.Cache, sza.BaseType)) { fp.Append(CodeWriters.GetAbiStructTypeName(writer, context, sza.BaseType)); }
                else if (CodeWriters.IsAnyStruct(context.Cache, sza.BaseType)) { fp.Append(CodeWriters.GetBlittableStructAbiType(writer, context, sza.BaseType)); }
                else { fp.Append(CodeWriters.GetAbiPrimitiveType(context.Cache, sza.BaseType)); }
                fp.Append("**");
                continue;
            }
            fp.Append(", ");
            if (p.Type.IsHResultException()) { fp.Append("global::ABI.System.Exception"); }
            else if (p.Type.IsString() || CodeWriters.IsRuntimeClassOrInterface(context.Cache, p.Type) || p.Type.IsObject() || p.Type.IsGenericInstance()) { fp.Append("void*"); }
            else if (p.Type.IsSystemType()) { fp.Append("global::ABI.System.Type"); }
            else if (CodeWriters.IsAnyStruct(context.Cache, p.Type)) { fp.Append(CodeWriters.GetBlittableStructAbiType(writer, context, p.Type)); }
            else if (CodeWriters.IsMappedAbiValueType(p.Type)) { fp.Append(CodeWriters.GetMappedAbiTypeName(p.Type)); }
            else if (CodeWriters.IsComplexStruct(context.Cache, p.Type)) { fp.Append(CodeWriters.GetAbiStructTypeName(writer, context, p.Type)); }
            else { fp.Append(CodeWriters.GetAbiPrimitiveType(context.Cache, p.Type)); }
        }
        if (rt is not null)
        {
            if (returnIsReceiveArray)
            {
                AsmResolver.DotNet.Signatures.SzArrayTypeSignature retSz = (AsmResolver.DotNet.Signatures.SzArrayTypeSignature)rt;
                fp.Append(", uint*, ");
                if (retSz.BaseType.IsString() || CodeWriters.IsRuntimeClassOrInterface(context.Cache, retSz.BaseType) || retSz.BaseType.IsObject())
                {
                    fp.Append("void*");
                }
                else if (CodeWriters.IsComplexStruct(context.Cache, retSz.BaseType))
                {
                    fp.Append(CodeWriters.GetAbiStructTypeName(writer, context, retSz.BaseType));
                }
                else if (retSz.BaseType.IsHResultException())
                {
                    fp.Append("global::ABI.System.Exception");
                }
                else if (CodeWriters.IsMappedAbiValueType(retSz.BaseType))
                {
                    fp.Append(CodeWriters.GetMappedAbiTypeName(retSz.BaseType));
                }
                else if (CodeWriters.IsAnyStruct(context.Cache, retSz.BaseType))
                {
                    fp.Append(CodeWriters.GetBlittableStructAbiType(writer, context, retSz.BaseType));
                }
                else
                {
                    fp.Append(CodeWriters.GetAbiPrimitiveType(context.Cache, retSz.BaseType));
                }
                fp.Append("**");
            }
            else if (returnIsHResultException)
            {
                fp.Append(", global::ABI.System.Exception*");
            }
            else
            {
                fp.Append(", ");
                if (returnIsString || returnIsRefType) { fp.Append("void**"); }
                else if (rt is not null && rt.IsSystemType()) { fp.Append("global::ABI.System.Type*"); }
                else if (returnIsAnyStruct) { fp.Append(CodeWriters.GetBlittableStructAbiType(writer, context, rt!)); fp.Append('*'); }
                else if (returnIsComplexStruct) { fp.Append(CodeWriters.GetAbiStructTypeName(writer, context, rt!)); fp.Append('*'); }
                else if (rt is not null && CodeWriters.IsMappedAbiValueType(rt)) { fp.Append(CodeWriters.GetMappedAbiTypeName(rt)); fp.Append('*'); }
                else { fp.Append(CodeWriters.GetAbiPrimitiveType(context.Cache, rt!)); fp.Append('*'); }
            }
        }
        fp.Append(", int");

        writer.Write("\n    {\n");
        writer.Write("        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();\n");
        writer.Write("        void* ThisPtr = thisValue.GetThisPtrUnsafe();\n");

        // Declare 'using' marshaller values for ref-type parameters (these need disposing).
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            if (CodeWriters.IsRuntimeClassOrInterface(context.Cache, p.Type) || p.Type.IsObject())
            {
                string localName = CodeWriters.GetParamLocalName(p, paramNameOverride);
                string callName = CodeWriters.GetParamName(p, paramNameOverride);
                writer.Write("        using WindowsRuntimeObjectReferenceValue __");
                writer.Write(localName);
                writer.Write(" = ");
                EmitMarshallerConvertToUnmanaged(writer, context, p.Type, callName);
                writer.Write(";\n");
            }
            else if (p.Type.IsNullableT())
            {
                // Nullable<T> param: use <T>Marshaller.BoxToUnmanaged. Mirrors truth pattern.
                string localName = CodeWriters.GetParamLocalName(p, paramNameOverride);
                string callName = CodeWriters.GetParamName(p, paramNameOverride);
                AsmResolver.DotNet.Signatures.TypeSignature inner = p.Type.GetNullableInnerType()!;
                string innerMarshaller = CodeWriters.GetNullableInnerMarshallerName(writer, context, inner);
                writer.Write("        using WindowsRuntimeObjectReferenceValue __");
                writer.Write(localName);
                writer.Write(" = ");
                writer.Write(innerMarshaller);
                writer.Write(".BoxToUnmanaged(");
                writer.Write(callName);
                writer.Write(");\n");
            }
            else if (p.Type.IsGenericInstance())
            {
                // Generic instance param: emit a local UnsafeAccessor delegate to get the marshaller method.
                string localName = CodeWriters.GetParamLocalName(p, paramNameOverride);
                string callName = CodeWriters.GetParamName(p, paramNameOverride);
                string interopTypeName = InteropTypeNameWriter.EncodeInteropTypeName(p.Type, TypedefNameType.ABI) + ", WinRT.Interop";
                IndentedTextWriter __scratchProjectedTypeName = new();
                MethodFactory.WriteProjectedSignature(__scratchProjectedTypeName, context, p.Type, false);
                string projectedTypeName = __scratchProjectedTypeName.ToString();
                writer.Write("        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"ConvertToUnmanaged\")]\n");
                writer.Write("        static extern WindowsRuntimeObjectReferenceValue ConvertToUnmanaged_");
                writer.Write(localName);
                writer.Write("([UnsafeAccessorType(\"");
                writer.Write(interopTypeName);
                writer.Write("\")] object _, ");
                writer.Write(projectedTypeName);
                writer.Write(" value);\n");
                writer.Write("        using WindowsRuntimeObjectReferenceValue __");
                writer.Write(localName);
                writer.Write(" = ConvertToUnmanaged_");
                writer.Write(localName);
                writer.Write("(null, ");
                writer.Write(callName);
                writer.Write(");\n");
            }
        }
        // (String input params are now stack-allocated via the fast-path pinning pattern below;
        //  no separate void* local declaration or up-front allocation is needed.)
        // Declare locals for HResult/Exception input parameters (converted up-front).
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            if (ParamHelpers.GetParamCategory(p) != ParamCategory.In) { continue; }
            if (!p.Type.IsHResultException()) { continue; }
            string localName = CodeWriters.GetParamLocalName(p, paramNameOverride);
            string callName = CodeWriters.GetParamName(p, paramNameOverride);
            writer.Write("        global::ABI.System.Exception __");
            writer.Write(localName);
            writer.Write(" = global::ABI.System.ExceptionMarshaller.ConvertToUnmanaged(");
            writer.Write(callName);
            writer.Write(");\n");
        }
        // Declare locals for mapped value-type input parameters (DateTime/TimeSpan): convert via marshaller up-front.
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            if (ParamHelpers.GetParamCategory(p) != ParamCategory.In) { continue; }
            if (!CodeWriters.IsMappedAbiValueType(p.Type)) { continue; }
            string localName = CodeWriters.GetParamLocalName(p, paramNameOverride);
            string callName = CodeWriters.GetParamName(p, paramNameOverride);
            writer.Write("        ");
            writer.Write(CodeWriters.GetMappedAbiTypeName(p.Type));
            writer.Write(" __");
            writer.Write(localName);
            writer.Write(" = ");
            writer.Write(CodeWriters.GetMappedMarshallerName(p.Type));
            writer.Write(".ConvertToUnmanaged(");
            writer.Write(callName);
            writer.Write(");\n");
        }
        // Declare locals for complex-struct input parameters (e.g. ProfileUsage with nested
        // string/Nullable fields): default-initialize OUTSIDE try, assign inside try via marshaller,
        // dispose in finally. Mirrors C++ behavior for non-blittable struct input params.
        // Includes both 'in' (ParamCategory.In) and 'in T' (ParamCategory.Ref) forms.
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.In && cat != ParamCategory.Ref) { continue; }
            AsmResolver.DotNet.Signatures.TypeSignature pType = CodeWriters.StripByRefAndCustomModifiers(p.Type);
            if (!CodeWriters.IsComplexStruct(context.Cache, pType)) { continue; }
            string localName = CodeWriters.GetParamLocalName(p, paramNameOverride);
            writer.Write("        ");
            writer.Write(CodeWriters.GetAbiStructTypeName(writer, context, pType));
            writer.Write(" __");
            writer.Write(localName);
            writer.Write(" = default;\n");
        }
        // Declare locals for Out parameters (need to be passed as &__<name> to the call).
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.Out) { continue; }
            string localName = CodeWriters.GetParamLocalName(p, paramNameOverride);
            AsmResolver.DotNet.Signatures.TypeSignature uOut = CodeWriters.StripByRefAndCustomModifiers(p.Type);
            writer.Write("        ");
            if (uOut.IsString() || CodeWriters.IsRuntimeClassOrInterface(context.Cache, uOut) || uOut.IsObject() || uOut.IsGenericInstance()) { writer.Write("void*"); }
            else if (uOut.IsSystemType()) { writer.Write("global::ABI.System.Type"); }
            else if (CodeWriters.IsComplexStruct(context.Cache, uOut)) { writer.Write(CodeWriters.GetAbiStructTypeName(writer, context, uOut)); }
            else if (CodeWriters.IsAnyStruct(context.Cache, uOut)) { writer.Write(CodeWriters.GetBlittableStructAbiType(writer, context, uOut)); }
            else { writer.Write(CodeWriters.GetAbiPrimitiveType(context.Cache, uOut)); }
            writer.Write(" __");
            writer.Write(localName);
            writer.Write(" = default;\n");
        }
        // Declare locals for ReceiveArray params (uint length + element pointer).
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.ReceiveArray) { continue; }
            string localName = CodeWriters.GetParamLocalName(p, paramNameOverride);
            AsmResolver.DotNet.Signatures.SzArrayTypeSignature sza = (AsmResolver.DotNet.Signatures.SzArrayTypeSignature)CodeWriters.StripByRefAndCustomModifiers(p.Type);
            writer.Write("        uint __");
            writer.Write(localName);
            writer.Write("_length = default;\n");
            writer.Write("        ");
            // Element ABI type: void* for ref types; ABI struct for complex/blittable structs;
            // primitive ABI otherwise.
            if (sza.BaseType.IsString() || CodeWriters.IsRuntimeClassOrInterface(context.Cache, sza.BaseType) || sza.BaseType.IsObject())
            {
                writer.Write("void*");
            }
            else if (CodeWriters.IsComplexStruct(context.Cache, sza.BaseType))
            {
                writer.Write(CodeWriters.GetAbiStructTypeName(writer, context, sza.BaseType));
            }
            else if (CodeWriters.IsAnyStruct(context.Cache, sza.BaseType))
            {
                writer.Write(CodeWriters.GetBlittableStructAbiType(writer, context, sza.BaseType));
            }
            else
            {
                writer.Write(CodeWriters.GetAbiPrimitiveType(context.Cache, sza.BaseType));
            }
            writer.Write("* __");
            writer.Write(localName);
            writer.Write("_data = default;\n");
        }
        // Declare InlineArray16 + ArrayPool fallback for non-blittable PassArray params
        // (runtime classes, objects, strings). Runtime class/object: just one InlineArray16<nint>.
        // String: also needs InlineArray16<HStringHeader> + InlineArray16<nint> for pinned handles.
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.PassArray && cat != ParamCategory.FillArray) { continue; }
            if (p.Type is not AsmResolver.DotNet.Signatures.SzArrayTypeSignature szArr) { continue; }
            if (CodeWriters.IsBlittablePrimitive(context.Cache, szArr.BaseType) || CodeWriters.IsAnyStruct(context.Cache, szArr.BaseType)) { continue; }
            // Non-blittable element type: emit InlineArray16<storageT> + ArrayPool<storageT>.
            // For mapped value types (DateTime/TimeSpan), use the ABI struct type.
            // For complex structs (e.g. authored BasicStruct with reference fields), use the ABI
            // struct type. For everything else (runtime classes, objects, strings), use nint.
            string localName = CodeWriters.GetParamLocalName(p, paramNameOverride);
            string callName = CodeWriters.GetParamName(p, paramNameOverride);
            string storageT = CodeWriters.IsMappedAbiValueType(szArr.BaseType)
                ? CodeWriters.GetMappedAbiTypeName(szArr.BaseType)
                : CodeWriters.IsComplexStruct(context.Cache, szArr.BaseType)
                    ? CodeWriters.GetAbiStructTypeName(writer, context, szArr.BaseType)
                    : szArr.BaseType.IsHResultException()
                        ? "global::ABI.System.Exception"
                        : "nint";
            writer.Write("\n        Unsafe.SkipInit(out InlineArray16<");
            writer.Write(storageT);
            writer.Write("> __");
            writer.Write(localName);
            writer.Write("_inlineArray);\n");
            writer.Write("        ");
            writer.Write(storageT);
            writer.Write("[] __");
            writer.Write(localName);
            writer.Write("_arrayFromPool = null;\n");
            writer.Write("        Span<");
            writer.Write(storageT);
            writer.Write("> __");
            writer.Write(localName);
            writer.Write("_span = ");
            writer.Write(callName);
            writer.Write(".Length <= 16\n            ? __");
            writer.Write(localName);
            writer.Write("_inlineArray[..");
            writer.Write(callName);
            writer.Write(".Length]\n            : (__");
            writer.Write(localName);
            writer.Write("_arrayFromPool = global::System.Buffers.ArrayPool<");
            writer.Write(storageT);
            writer.Write(">.Shared.Rent(");
            writer.Write(callName);
            writer.Write(".Length));\n");

            if (szArr.BaseType.IsString() && cat == ParamCategory.PassArray)
            {
                // Strings need an additional InlineArray16<HStringHeader> + InlineArray16<nint> (pinned handles).
                // Only required for PassArray (managed -> HSTRING conversion); FillArray's native side
                // fills HSTRING handles directly into the nint storage.
                writer.Write("\n        Unsafe.SkipInit(out InlineArray16<HStringHeader> __");
                writer.Write(localName);
                writer.Write("_inlineHeaderArray);\n");
                writer.Write("        HStringHeader[] __");
                writer.Write(localName);
                writer.Write("_headerArrayFromPool = null;\n");
                writer.Write("        Span<HStringHeader> __");
                writer.Write(localName);
                writer.Write("_headerSpan = ");
                writer.Write(callName);
                writer.Write(".Length <= 16\n            ? __");
                writer.Write(localName);
                writer.Write("_inlineHeaderArray[..");
                writer.Write(callName);
                writer.Write(".Length]\n            : (__");
                writer.Write(localName);
                writer.Write("_headerArrayFromPool = global::System.Buffers.ArrayPool<HStringHeader>.Shared.Rent(");
                writer.Write(callName);
                writer.Write(".Length));\n");

                writer.Write("\n        Unsafe.SkipInit(out InlineArray16<nint> __");
                writer.Write(localName);
                writer.Write("_inlinePinnedHandleArray);\n");
                writer.Write("        nint[] __");
                writer.Write(localName);
                writer.Write("_pinnedHandleArrayFromPool = null;\n");
                writer.Write("        Span<nint> __");
                writer.Write(localName);
                writer.Write("_pinnedHandleSpan = ");
                writer.Write(callName);
                writer.Write(".Length <= 16\n            ? __");
                writer.Write(localName);
                writer.Write("_inlinePinnedHandleArray[..");
                writer.Write(callName);
                writer.Write(".Length]\n            : (__");
                writer.Write(localName);
                writer.Write("_pinnedHandleArrayFromPool = global::System.Buffers.ArrayPool<nint>.Shared.Rent(");
                writer.Write(callName);
                writer.Write(".Length));\n");
            }
        }
        if (returnIsReceiveArray)
        {
            AsmResolver.DotNet.Signatures.SzArrayTypeSignature retSz = (AsmResolver.DotNet.Signatures.SzArrayTypeSignature)rt!;
            writer.Write("        uint __retval_length = default;\n");
            writer.Write("        ");
            if (retSz.BaseType.IsString() || CodeWriters.IsRuntimeClassOrInterface(context.Cache, retSz.BaseType) || retSz.BaseType.IsObject())
            {
                writer.Write("void*");
            }
            else if (CodeWriters.IsComplexStruct(context.Cache, retSz.BaseType))
            {
                writer.Write(CodeWriters.GetAbiStructTypeName(writer, context, retSz.BaseType));
            }
            else if (retSz.BaseType.IsHResultException())
            {
                writer.Write("global::ABI.System.Exception");
            }
            else if (CodeWriters.IsMappedAbiValueType(retSz.BaseType))
            {
                writer.Write(CodeWriters.GetMappedAbiTypeName(retSz.BaseType));
            }
            else if (CodeWriters.IsAnyStruct(context.Cache, retSz.BaseType))
            {
                writer.Write(CodeWriters.GetBlittableStructAbiType(writer, context, retSz.BaseType));
            }
            else
            {
                writer.Write(CodeWriters.GetAbiPrimitiveType(context.Cache, retSz.BaseType));
            }
            writer.Write("* __retval_data = default;\n");
        }
        else if (returnIsHResultException)
        {
            writer.Write("        global::ABI.System.Exception __retval = default;\n");
        }
        else if (returnIsString || returnIsRefType)
        {
            writer.Write("        void* __retval = default;\n");
        }
        else if (returnIsAnyStruct)
        {
            writer.Write("        ");
            writer.Write(CodeWriters.GetBlittableStructAbiType(writer, context, rt!));
            writer.Write(" __retval = default;\n");
        }
        else if (returnIsComplexStruct)
        {
            writer.Write("        ");
            writer.Write(CodeWriters.GetAbiStructTypeName(writer, context, rt!));
            writer.Write(" __retval = default;\n");
        }
        else if (rt is not null && CodeWriters.IsMappedAbiValueType(rt))
        {
            // Mapped value type return (e.g. DateTime/TimeSpan): use the ABI struct as __retval.
            writer.Write("        ");
            writer.Write(CodeWriters.GetMappedAbiTypeName(rt));
            writer.Write(" __retval = default;\n");
        }
        else if (rt is not null && rt.IsSystemType())
        {
            // System.Type return: use ABI Type struct as __retval.
            writer.Write("        global::ABI.System.Type __retval = default;\n");
        }
        else if (rt is not null)
        {
            writer.Write("        ");
            writer.Write(CodeWriters.GetAbiPrimitiveType(context.Cache, rt));
            writer.Write(" __retval = default;\n");
        }

        // Determine if we need a try/finally (for cleanup of string/refType return or receive array
        // return or Out runtime class params). Input string params no longer need try/finally —
        // they use the HString fast-path (stack-allocated HStringReference, no free needed).
        bool hasOutNeedsCleanup = false;
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.Out) { continue; }
            AsmResolver.DotNet.Signatures.TypeSignature uOut = CodeWriters.StripByRefAndCustomModifiers(p.Type);
            if (uOut.IsString() || CodeWriters.IsRuntimeClassOrInterface(context.Cache, uOut) || uOut.IsObject() || uOut.IsSystemType() || CodeWriters.IsComplexStruct(context.Cache, uOut) || uOut.IsGenericInstance()) { hasOutNeedsCleanup = true; break; }
        }
        bool hasReceiveArray = false;
        for (int i = 0; i < sig.Params.Count; i++)
        {
            if (ParamHelpers.GetParamCategory(sig.Params[i]) == ParamCategory.ReceiveArray) { hasReceiveArray = true; break; }
        }
        bool hasNonBlittablePassArray = false;
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if ((cat == ParamCategory.PassArray || cat == ParamCategory.FillArray)
                && p.Type is AsmResolver.DotNet.Signatures.SzArrayTypeSignature szArrCheck
                && !CodeWriters.IsBlittablePrimitive(context.Cache, szArrCheck.BaseType) && !CodeWriters.IsAnyStruct(context.Cache, szArrCheck.BaseType)
                && !CodeWriters.IsMappedAbiValueType(szArrCheck.BaseType))
            {
                hasNonBlittablePassArray = true; break;
            }
        }
        bool hasComplexStructInput = false;
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if ((cat == ParamCategory.In || cat == ParamCategory.Ref) && CodeWriters.IsComplexStruct(context.Cache, CodeWriters.StripByRefAndCustomModifiers(p.Type))) { hasComplexStructInput = true; break; }
        }
        // System.Type return: ABI.System.Type contains an HSTRING that must be disposed
        // after marshalling to managed System.Type, otherwise the HSTRING leaks. Mirrors
        // C++ abi_marshaler::write_dispose path for is_out + non-empty marshaler_type.
        bool returnIsSystemTypeForCleanup = rt is not null && rt.IsSystemType();
        bool needsTryFinally = returnIsString || returnIsRefType || returnIsReceiveArray || hasOutNeedsCleanup || hasReceiveArray || returnIsComplexStruct || hasNonBlittablePassArray || hasComplexStructInput || returnIsSystemTypeForCleanup;
        if (needsTryFinally) { writer.Write("        try\n        {\n"); }

        string indent = needsTryFinally ? "            " : "        ";

        // Inside try (if applicable): assign complex-struct input locals via marshaller.
        // Mirrors truth pattern: '__value = ProfileUsageMarshaller.ConvertToUnmanaged(value);'
        // Includes both 'in' (ParamCategory.In) and 'in T' (ParamCategory.Ref) forms.
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.In && cat != ParamCategory.Ref) { continue; }
            AsmResolver.DotNet.Signatures.TypeSignature pType = CodeWriters.StripByRefAndCustomModifiers(p.Type);
            if (!CodeWriters.IsComplexStruct(context.Cache, pType)) { continue; }
            string localName = CodeWriters.GetParamLocalName(p, paramNameOverride);
            string callName = CodeWriters.GetParamName(p, paramNameOverride);
            writer.Write(indent);
            writer.Write("__");
            writer.Write(localName);
            writer.Write(" = ");
            writer.Write(CodeWriters.GetMarshallerFullName(writer, context, pType));
            writer.Write(".ConvertToUnmanaged(");
            writer.Write(callName);
            writer.Write(");\n");
        }
        // Type input params: set up TypeReference locals before the fixed block. Mirrors truth:
        //   global::ABI.System.TypeMarshaller.ConvertToUnmanagedUnsafe(forType, out TypeReference __forType);
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            if (ParamHelpers.GetParamCategory(p) != ParamCategory.In) { continue; }
            if (!p.Type.IsSystemType()) { continue; }
            string localName = CodeWriters.GetParamLocalName(p, paramNameOverride);
            string callName = CodeWriters.GetParamName(p, paramNameOverride);
            writer.Write(indent);
            writer.Write("global::ABI.System.TypeMarshaller.ConvertToUnmanagedUnsafe(");
            writer.Write(callName);
            writer.Write(", out TypeReference __");
            writer.Write(localName);
            writer.Write(");\n");
        }
        // Open a SINGLE fixed-block for ALL pinnable inputs (mirrors C++ write_abi_invoke):
        //   1. Ref params (typed ptr, separate "fixed(T* _x = &x)\n" lines, no braces)
        //   2. Complex-struct PassArrays (typed ptr, separate fixed line)
        //   3. All other "void*"-style pinnables (strings, Type[], blittable PassArrays,
        //      reference-type PassArrays via inline-pool span) merged into ONE
        //      "fixed(void* _a = ..., _b = ..., ...) {\n" block.
        //
        // C# allows multiple chained "fixed(...)" without braces to share the next braced
        // body, which is what the C++ tool emits. This avoids the deep nesting mine had
        // when emitting a separate fixed block per PassArray.
        int fixedNesting = 0;

        // Step 1: Emit typed-pointer fixed lines for Ref params and complex-struct PassArrays
        // (no braces - they share the body of the upcoming combined fixed-void* block, OR
        // each other if no void* block is needed).
        bool hasAnyVoidStarPinnable = false;
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (p.Type.IsString() || p.Type.IsSystemType()) { hasAnyVoidStarPinnable = true; continue; }
            if (cat == ParamCategory.PassArray || cat == ParamCategory.FillArray)
            {
                // All PassArrays (including complex structs) go in the void* combined block,
                // matching truth's pattern. Complex structs use a (T*) cast at the call site.
                hasAnyVoidStarPinnable = true;
            }
        }
        // Emit typed fixed lines for Ref params.
        // Skip Ref+ComplexStruct: those are marshalled via __local (no fixed needed) and
        // passed as &__local at the call site, mirroring C++ tool's is_value_type_in path.
        int typedFixedCount = 0;
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat == ParamCategory.Ref)
            {
                AsmResolver.DotNet.Signatures.TypeSignature uRefSkip = CodeWriters.StripByRefAndCustomModifiers(p.Type);
                if (CodeWriters.IsComplexStruct(context.Cache, uRefSkip)) { continue; }
                string callName = CodeWriters.GetParamName(p, paramNameOverride);
                string localName = CodeWriters.GetParamLocalName(p, paramNameOverride);
                AsmResolver.DotNet.Signatures.TypeSignature uRef = uRefSkip;
                string abiType = CodeWriters.IsAnyStruct(context.Cache, uRef) ? CodeWriters.GetBlittableStructAbiType(writer, context, uRef) : CodeWriters.GetAbiPrimitiveType(context.Cache, uRef);
                writer.Write(indent);
                writer.Write(new string(' ', fixedNesting * 4));
                writer.Write("fixed(");
                writer.Write(abiType);
                writer.Write("* _");
                writer.Write(localName);
                writer.Write(" = &");
                writer.Write(callName);
                writer.Write(")\n");
                typedFixedCount++;
            }
        }

        // Step 2: Emit ONE combined fixed-void* block for all pinnables that share the
        // same scope. Each variable is "_localName = rhsExpr". Strings get an extra
        // "_localName_inlineHeaderArray = __localName_headerSpan" entry.
        bool stringPinnablesEmitted = false;
        if (hasAnyVoidStarPinnable)
        {
            writer.Write(indent);
            writer.Write(new string(' ', fixedNesting * 4));
            writer.Write("fixed(void* ");
            bool first = true;
            for (int i = 0; i < sig.Params.Count; i++)
            {
                ParamInfo p = sig.Params[i];
                ParamCategory cat = ParamHelpers.GetParamCategory(p);
                bool isString = p.Type.IsString();
                bool isType = p.Type.IsSystemType();
                bool isPassArray = cat == ParamCategory.PassArray || cat == ParamCategory.FillArray;
                if (!isString && !isType && !isPassArray) { continue; }
                string callName = CodeWriters.GetParamName(p, paramNameOverride);
                string localName = CodeWriters.GetParamLocalName(p, paramNameOverride);
                if (!first) { writer.Write(", "); }
                first = false;
                writer.Write("_");
                writer.Write(localName);
                writer.Write(" = ");
                if (isType)
                {
                    writer.Write("__");
                    writer.Write(localName);
                }
                else if (isPassArray)
                {
                    AsmResolver.DotNet.Signatures.TypeSignature elemT = ((AsmResolver.DotNet.Signatures.SzArrayTypeSignature)p.Type).BaseType;
                    bool isBlittableElem = CodeWriters.IsBlittablePrimitive(context.Cache, elemT) || CodeWriters.IsAnyStruct(context.Cache, elemT);
                    bool isStringElem = elemT.IsString();
                    if (isBlittableElem)
                    {
                        writer.Write(callName);
                    }
                    else
                    {
                        writer.Write("__");
                        writer.Write(localName);
                        writer.Write("_span");
                    }
                    // For string elements: only PassArray needs the additional inlineHeaderArray
                    // pinned alongside the data span. FillArray fills HSTRINGs into the nint
                    // storage directly (no header conversion needed).
                    if (isStringElem && cat == ParamCategory.PassArray)
                    {
                        writer.Write(", _");
                        writer.Write(localName);
                        writer.Write("_inlineHeaderArray = __");
                        writer.Write(localName);
                        writer.Write("_headerSpan");
                    }
                }
                else
                {
                    // string param
                    writer.Write(callName);
                }
            }
            writer.Write(")\n");
            writer.Write(indent);
            writer.Write(new string(' ', fixedNesting * 4));
            writer.Write("{\n");
            fixedNesting++;
            // Inside the body: emit HStringMarshaller calls for input string params.
            for (int i = 0; i < sig.Params.Count; i++)
            {
                if (!sig.Params[i].Type.IsString()) { continue; }
                string callName = CodeWriters.GetParamName(sig.Params[i], paramNameOverride);
                string localName = CodeWriters.GetParamLocalName(sig.Params[i], paramNameOverride);
                writer.Write(indent);
                writer.Write(new string(' ', fixedNesting * 4));
                writer.Write("HStringMarshaller.ConvertToUnmanagedUnsafe((char*)_");
                writer.Write(localName);
                writer.Write(", ");
                writer.Write(callName);
                writer.Write("?.Length, out HStringReference __");
                writer.Write(localName);
                writer.Write(");\n");
            }
            stringPinnablesEmitted = true;
        }
        else if (typedFixedCount > 0)
        {
            // Typed fixed lines exist but no void* combined block - we need a body block
            // to host them. Open a brace block after the last typed fixed line.
            writer.Write(indent);
            writer.Write(new string(' ', fixedNesting * 4));
            writer.Write("{\n");
            fixedNesting++;
        }
        // Suppress unused variable warning when block above doesn't fire.
        _ = stringPinnablesEmitted;

        string callIndent = indent + new string(' ', fixedNesting * 4);

        // For non-blittable PassArray params, emit CopyToUnmanaged_<name> (UnsafeAccessor) and call
        // it to populate the inline/pooled storage from the user-supplied span. For string arrays,
        // use HStringArrayMarshaller.ConvertToUnmanagedUnsafe instead.
        // FillArray of strings is the exception: the native side fills the HSTRING handles, so
        // there's nothing to convert pre-call (the post-call CopyToManaged_<name> handles writeback).
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.PassArray && cat != ParamCategory.FillArray) { continue; }
            if (p.Type is not AsmResolver.DotNet.Signatures.SzArrayTypeSignature szArr) { continue; }
            if (CodeWriters.IsBlittablePrimitive(context.Cache, szArr.BaseType) || CodeWriters.IsAnyStruct(context.Cache, szArr.BaseType)) { continue; }
            string callName = CodeWriters.GetParamName(p, paramNameOverride);
            string localName = CodeWriters.GetParamLocalName(p, paramNameOverride);
            if (szArr.BaseType.IsString())
            {
                // Skip pre-call ConvertToUnmanagedUnsafe for FillArray of strings — there's
                // nothing to convert (native fills the handles). Mirrors C++ truth pattern.
                if (cat == ParamCategory.FillArray) { continue; }
                writer.Write(callIndent);
                writer.Write("HStringArrayMarshaller.ConvertToUnmanagedUnsafe(\n");
                writer.Write(callIndent);
                writer.Write("    source: ");
                writer.Write(callName);
                writer.Write(",\n");
                writer.Write(callIndent);
                writer.Write("    hstringHeaders: (HStringHeader*) _");
                writer.Write(localName);
                writer.Write("_inlineHeaderArray,\n");
                writer.Write(callIndent);
                writer.Write("    hstrings: __");
                writer.Write(localName);
                writer.Write("_span,\n");
                writer.Write(callIndent);
                writer.Write("    pinnedGCHandles: __");
                writer.Write(localName);
                writer.Write("_pinnedHandleSpan);\n");
            }
            else
            {
                // FillArray (Span<T>) of non-blittable element types: skip pre-call
                // CopyToUnmanaged. The buffer the native side gets (_<name>) is uninitialized
                // ABI-format storage; the native callee fills it. The post-call writeback loop
                // emits CopyToManaged_<name> to propagate the native fills into the user's
                // managed Span<T>. (Mirrors C++ marshaler.write_marshal_to_abi which only emits
                // CopyToUnmanaged for PassArray, not FillArray.)
                if (cat == ParamCategory.FillArray) { continue; }
                IndentedTextWriter __scratchElementProjected = new();
                TypedefNameWriter.WriteProjectionType(__scratchElementProjected, context, TypeSemanticsFactory.Get(szArr.BaseType));
                string elementProjected = __scratchElementProjected.ToString();
                string elementInteropArg = InteropTypeNameWriter.EncodeInteropTypeName(szArr.BaseType, TypedefNameType.Projected);

                _ = elementInteropArg;
                // For mapped value types (DateTime/TimeSpan) and complex structs, the storage
                // element is the ABI struct type; the data pointer parameter type uses that
                // ABI struct. The fixed() opens with void* (per truth's pattern), so a cast
                // is required at the call site. For runtime classes/objects, use void**.
                string dataParamType;
                string dataCastType;
                if (CodeWriters.IsMappedAbiValueType(szArr.BaseType))
                {
                    dataParamType = CodeWriters.GetMappedAbiTypeName(szArr.BaseType) + "*";
                    dataCastType = "(" + CodeWriters.GetMappedAbiTypeName(szArr.BaseType) + "*)";
                }
                else if (szArr.BaseType.IsHResultException())
                {
                    dataParamType = "global::ABI.System.Exception*";
                    dataCastType = "(global::ABI.System.Exception*)";
                }
                else if (CodeWriters.IsComplexStruct(context.Cache, szArr.BaseType))
                {
                    string abiStructName = CodeWriters.GetAbiStructTypeName(writer, context, szArr.BaseType);
                    dataParamType = abiStructName + "*";
                    dataCastType = "(" + abiStructName + "*)";
                }
                else
                {
                    dataParamType = "void**";
                    dataCastType = "(void**)";
                }
                writer.Write(callIndent);
                writer.Write("[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"CopyToUnmanaged\")]\n");
                writer.Write(callIndent);
                writer.Write("static extern void CopyToUnmanaged_");
                writer.Write(localName);
                writer.Write("([UnsafeAccessorType(\"");
                writer.Write(ArrayElementEncoder.GetArrayMarshallerInteropPath(szArr.BaseType));
                writer.Write("\")] object _, ReadOnlySpan<");
                writer.Write(elementProjected);
                writer.Write("> span, uint length, ");
                writer.Write(dataParamType);
                writer.Write(" data);\n");
                writer.Write(callIndent);
                writer.Write("CopyToUnmanaged_");
                writer.Write(localName);
                writer.Write("(null, ");
                writer.Write(callName);
                writer.Write(", (uint)");
                writer.Write(callName);
                writer.Write(".Length, ");
                writer.Write(dataCastType);
                writer.Write("_");
                writer.Write(localName);
                writer.Write(");\n");
            }
        }

        writer.Write(callIndent);
        // method/property is [NoException] (its HRESULT is contractually S_OK).
        if (!isNoExcept)
        {
            writer.Write("RestrictedErrorInfo.ThrowExceptionForHR((*(delegate* unmanaged[MemberFunction]<");
        }
        else
        {
            writer.Write("(*(delegate* unmanaged[MemberFunction]<");
        }
        writer.Write(fp.ToString());
        writer.Write(">**)ThisPtr)[");
        writer.Write(slot.ToString(System.Globalization.CultureInfo.InvariantCulture));
        writer.Write("](ThisPtr");
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat == ParamCategory.PassArray || cat == ParamCategory.FillArray)
            {
                string callName = CodeWriters.GetParamName(p, paramNameOverride);
                string localName = CodeWriters.GetParamLocalName(p, paramNameOverride);
                writer.Write(",\n  (uint)");
                writer.Write(callName);
                writer.Write(".Length, _");
                writer.Write(localName);
                continue;
            }
            if (cat == ParamCategory.Out)
            {
                string localName = CodeWriters.GetParamLocalName(p, paramNameOverride);
                writer.Write(",\n  &__");
                writer.Write(localName);
                continue;
            }
            if (cat == ParamCategory.ReceiveArray)
            {
                string localName = CodeWriters.GetParamLocalName(p, paramNameOverride);
                writer.Write(",\n  &__");
                writer.Write(localName);
                writer.Write("_length, &__");
                writer.Write(localName);
                writer.Write("_data");
                continue;
            }
            if (cat == ParamCategory.Ref)
            {
                string localName = CodeWriters.GetParamLocalName(p, paramNameOverride);
                AsmResolver.DotNet.Signatures.TypeSignature uRefArg = CodeWriters.StripByRefAndCustomModifiers(p.Type);
                if (CodeWriters.IsComplexStruct(context.Cache, uRefArg))
                {
                    // Complex struct 'in' (Ref) param: pass &__local (the marshaled ABI struct).
                    writer.Write(",\n  &__");
                    writer.Write(localName);
                }
                else
                {
                    // 'in T' projected param: pass the pinned pointer.
                    writer.Write(",\n  _");
                    writer.Write(localName);
                }
                continue;
            }
            writer.Write(",\n  ");
            if (p.Type.IsHResultException())
            {
                writer.Write("__");
                writer.Write(CodeWriters.GetParamLocalName(p, paramNameOverride));
            }
            else if (p.Type.IsString())
            {
                writer.Write("__");
                writer.Write(CodeWriters.GetParamLocalName(p, paramNameOverride));
                writer.Write(".HString");
            }
            else if (CodeWriters.IsRuntimeClassOrInterface(context.Cache, p.Type) || p.Type.IsObject() || p.Type.IsGenericInstance())
            {
                writer.Write("__");
                writer.Write(CodeWriters.GetParamLocalName(p, paramNameOverride));
                writer.Write(".GetThisPtrUnsafe()");
            }
            else if (p.Type.IsSystemType())
            {
                // System.Type input: pass the pre-converted ABI Type struct (via the local set up before the call).
                writer.Write("__");
                writer.Write(CodeWriters.GetParamLocalName(p, paramNameOverride));
                writer.Write(".ConvertToUnmanagedUnsafe()");
            }
            else if (CodeWriters.IsMappedAbiValueType(p.Type))
            {
                // Mapped value-type input: pass the pre-converted ABI local.
                writer.Write("__");
                writer.Write(CodeWriters.GetParamLocalName(p, paramNameOverride));
            }
            else if (CodeWriters.IsComplexStruct(context.Cache, p.Type))
            {
                // Complex struct input: pass the pre-converted ABI struct local.
                writer.Write("__");
                writer.Write(CodeWriters.GetParamLocalName(p, paramNameOverride));
            }
            else if (CodeWriters.IsAnyStruct(context.Cache, p.Type))
            {
                writer.Write(CodeWriters.GetParamName(p, paramNameOverride));
            }
            else
            {
                EmitParamArgConversion(writer, context, p, paramNameOverride);
            }
        }
        if (returnIsReceiveArray)
        {
            writer.Write(",\n  &__retval_length, &__retval_data");
        }
        else if (rt is not null)
        {
            writer.Write(",\n  &__retval");
        }
        // Close the vtable call. One less ')' when noexcept (no ThrowExceptionForHR wrap).
        writer.Write(isNoExcept ? ");\n" : "));\n");

        // After call: copy native-filled values back into the user's managed Span<T> for
        // FillArray of non-blittable element types. The native callee wrote into our
        // ABI-format buffer (_<name>) which is separate from the user's Span<T>; we need to
        // CopyToManaged_<name> to convert each ABI element back to the projected form and
        // store it in the user's Span. Mirrors C++ marshaler.write_marshal_from_abi
        //.
        // Blittable element types (primitives and almost-blittable structs) don't need this
        // because the user's Span wraps the same memory the native side wrote to.
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.FillArray) { continue; }
            if (p.Type is not AsmResolver.DotNet.Signatures.SzArrayTypeSignature szFA) { continue; }
            if (CodeWriters.IsBlittablePrimitive(context.Cache, szFA.BaseType) || CodeWriters.IsAnyStruct(context.Cache, szFA.BaseType)) { continue; }
            string callName = CodeWriters.GetParamName(p, paramNameOverride);
            string localName = CodeWriters.GetParamLocalName(p, paramNameOverride);
            IndentedTextWriter __scratchElementProjected = new();
            TypedefNameWriter.WriteProjectionType(__scratchElementProjected, context, TypeSemanticsFactory.Get(szFA.BaseType));
            string elementProjected = __scratchElementProjected.ToString();
            string elementInteropArg = InteropTypeNameWriter.EncodeInteropTypeName(szFA.BaseType, TypedefNameType.Projected);

            _ = elementInteropArg;
            // Determine the ABI element type for the data pointer parameter.
            // - Strings / runtime classes / objects: void**
            // - HResult exception: global::ABI.System.Exception*
            // - Mapped value types: global::ABI.System.{DateTimeOffset|TimeSpan}*
            // - Complex structs: <ABI struct>*
            string dataParamType;
            string dataCastType;
            if (szFA.BaseType.IsString() || CodeWriters.IsRuntimeClassOrInterface(context.Cache, szFA.BaseType) || szFA.BaseType.IsObject())
            {
                dataParamType = "void** data";
                dataCastType = "(void**)";
            }
            else if (szFA.BaseType.IsHResultException())
            {
                dataParamType = "global::ABI.System.Exception* data";
                dataCastType = "(global::ABI.System.Exception*)";
            }
            else if (CodeWriters.IsMappedAbiValueType(szFA.BaseType))
            {
                string abiName = CodeWriters.GetMappedAbiTypeName(szFA.BaseType);
                dataParamType = abiName + "* data";
                dataCastType = "(" + abiName + "*)";
            }
            else
            {
                string abiStructName = CodeWriters.GetAbiStructTypeName(writer, context, szFA.BaseType);
                dataParamType = abiStructName + "* data";
                dataCastType = "(" + abiStructName + "*)";
            }
            writer.Write(callIndent);
            writer.Write("[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"CopyToManaged\")]\n");
            writer.Write(callIndent);
            writer.Write("static extern void CopyToManaged_");
            writer.Write(localName);
            writer.Write("([UnsafeAccessorType(\"");
            writer.Write(ArrayElementEncoder.GetArrayMarshallerInteropPath(szFA.BaseType));
            writer.Write("\")] object _, uint length, ");
            writer.Write(dataParamType);
            writer.Write(", Span<");
            writer.Write(elementProjected);
            writer.Write("> span);\n");
            writer.Write(callIndent);
            writer.Write("CopyToManaged_");
            writer.Write(localName);
            writer.Write("(null, (uint)__");
            writer.Write(localName);
            writer.Write("_span.Length, ");
            writer.Write(dataCastType);
            writer.Write("_");
            writer.Write(localName);
            writer.Write(", ");
            writer.Write(callName);
            writer.Write(");\n");
        }

        // After call: write back Out params to caller's 'out' var.
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.Out) { continue; }
            string callName = CodeWriters.GetParamName(p, paramNameOverride);
            string localName = CodeWriters.GetParamLocalName(p, paramNameOverride);
            AsmResolver.DotNet.Signatures.TypeSignature uOut = CodeWriters.StripByRefAndCustomModifiers(p.Type);

            // For Out generic instance: emit inline UnsafeAccessor to ConvertToManaged_<name>
            // before the writeback. Mirrors the truth pattern (e.g. Collection1HandlerInvoke
            // emits the accessor inside try, right before the assignment).
            if (uOut.IsGenericInstance())
            {
                string interopTypeName = InteropTypeNameWriter.EncodeInteropTypeName(uOut, TypedefNameType.ABI) + ", WinRT.Interop";
                IndentedTextWriter __scratchProjectedTypeName = new();
                MethodFactory.WriteProjectedSignature(__scratchProjectedTypeName, context, uOut, false);
                string projectedTypeName = __scratchProjectedTypeName.ToString();
                writer.Write(callIndent);
                writer.Write("[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"ConvertToManaged\")]\n");
                writer.Write(callIndent);
                writer.Write("static extern ");
                writer.Write(projectedTypeName);
                writer.Write(" ConvertToManaged_");
                writer.Write(localName);
                writer.Write("([UnsafeAccessorType(\"");
                writer.Write(interopTypeName);
                writer.Write("\")] object _, void* value);\n");
                writer.Write(callIndent);
                writer.Write(callName);
                writer.Write(" = ConvertToManaged_");
                writer.Write(localName);
                writer.Write("(null, __");
                writer.Write(localName);
                writer.Write(");\n");
                continue;
            }

            writer.Write(callIndent);
            writer.Write(callName);
            writer.Write(" = ");
            if (uOut.IsString())
            {
                writer.Write("HStringMarshaller.ConvertToManaged(__");
                writer.Write(localName);
                writer.Write(")");
            }
            else if (uOut.IsObject())
            {
                writer.Write("WindowsRuntimeObjectMarshaller.ConvertToManaged(__");
                writer.Write(localName);
                writer.Write(")");
            }
            else if (CodeWriters.IsRuntimeClassOrInterface(context.Cache, uOut))
            {
                writer.Write(CodeWriters.GetMarshallerFullName(writer, context, uOut));
                writer.Write(".ConvertToManaged(__");
                writer.Write(localName);
                writer.Write(")");
            }
            else if (uOut.IsSystemType())
            {
                writer.Write("global::ABI.System.TypeMarshaller.ConvertToManaged(__");
                writer.Write(localName);
                writer.Write(")");
            }
            else if (CodeWriters.IsComplexStruct(context.Cache, uOut))
            {
                writer.Write(CodeWriters.GetMarshallerFullName(writer, context, uOut));
                writer.Write(".ConvertToManaged(__");
                writer.Write(localName);
                writer.Write(")");
            }
            else if (CodeWriters.IsAnyStruct(context.Cache, uOut))
            {
                writer.Write("__");
                writer.Write(localName);
            }
            else if (uOut is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlibBool && corlibBool.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Boolean)
            {
                writer.Write("__");
                writer.Write(localName);
            }
            else if (uOut is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlibChar && corlibChar.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Char)
            {
                writer.Write("__");
                writer.Write(localName);
            }
            else if (CodeWriters.IsEnumType(context.Cache, uOut))
            {
                // Enum out param: __<name> local is already the projected enum type (since the
                // function pointer signature uses the projected type). No cast needed.
                writer.Write("__");
                writer.Write(localName);
            }
            else
            {
                writer.Write("__");
                writer.Write(localName);
            }
            writer.Write(";\n");
        }

        // Writeback for ReceiveArray params: emit a UnsafeAccessor + assign to the out param.
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.ReceiveArray) { continue; }
            string callName = CodeWriters.GetParamName(p, paramNameOverride);
            string localName = CodeWriters.GetParamLocalName(p, paramNameOverride);
            AsmResolver.DotNet.Signatures.SzArrayTypeSignature sza = (AsmResolver.DotNet.Signatures.SzArrayTypeSignature)CodeWriters.StripByRefAndCustomModifiers(p.Type);
            IndentedTextWriter __scratchElementProjected = new();
            TypedefNameWriter.WriteProjectionType(__scratchElementProjected, context, TypeSemanticsFactory.Get(sza.BaseType));
            string elementProjected = __scratchElementProjected.ToString();
            // Element ABI type: void* for ref types (string/runtime class/object); ABI struct
            // type for complex structs (e.g. authored BasicStruct); blittable struct ABI for
            // blittable structs; primitive ABI otherwise.
            string elementAbi = sza.BaseType.IsString() || CodeWriters.IsRuntimeClassOrInterface(context.Cache, sza.BaseType) || sza.BaseType.IsObject()
                ? "void*"
                : CodeWriters.IsComplexStruct(context.Cache, sza.BaseType)
                    ? CodeWriters.GetAbiStructTypeName(writer, context, sza.BaseType)
                    : CodeWriters.IsAnyStruct(context.Cache, sza.BaseType)
                        ? CodeWriters.GetBlittableStructAbiType(writer, context, sza.BaseType)
                        : CodeWriters.GetAbiPrimitiveType(context.Cache, sza.BaseType);
            string elementInteropArg = InteropTypeNameWriter.EncodeInteropTypeName(sza.BaseType, TypedefNameType.Projected);

            _ = elementInteropArg;
            string marshallerPath = ArrayElementEncoder.GetArrayMarshallerInteropPath(sza.BaseType);
            writer.Write(callIndent);
            writer.Write("[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"ConvertToManaged\")]\n");
            writer.Write(callIndent);
            writer.Write("static extern ");
            writer.Write(elementProjected);
            writer.Write("[] ConvertToManaged_");
            writer.Write(localName);
            writer.Write("([UnsafeAccessorType(\"");
            writer.Write(marshallerPath);
            writer.Write("\")] object _, uint length, ");
            writer.Write(elementAbi);
            writer.Write("* data);\n");
            writer.Write(callIndent);
            writer.Write(callName);
            writer.Write(" = ConvertToManaged_");
            writer.Write(localName);
            writer.Write("(null, __");
            writer.Write(localName);
            writer.Write("_length, __");
            writer.Write(localName);
            writer.Write("_data);\n");
        }
        if (rt is not null)
        {
            if (returnIsReceiveArray)
            {
                AsmResolver.DotNet.Signatures.SzArrayTypeSignature retSz = (AsmResolver.DotNet.Signatures.SzArrayTypeSignature)rt;
                IndentedTextWriter __scratchElementProjected = new();
                TypedefNameWriter.WriteProjectionType(__scratchElementProjected, context, TypeSemanticsFactory.Get(retSz.BaseType));
                string elementProjected = __scratchElementProjected.ToString();
                string elementAbi = retSz.BaseType.IsString() || CodeWriters.IsRuntimeClassOrInterface(context.Cache, retSz.BaseType) || retSz.BaseType.IsObject()
                    ? "void*"
                    : CodeWriters.IsComplexStruct(context.Cache, retSz.BaseType)
                        ? CodeWriters.GetAbiStructTypeName(writer, context, retSz.BaseType)
                        : retSz.BaseType.IsHResultException()
                            ? "global::ABI.System.Exception"
                            : CodeWriters.IsMappedAbiValueType(retSz.BaseType)
                                ? CodeWriters.GetMappedAbiTypeName(retSz.BaseType)
                                : CodeWriters.IsAnyStruct(context.Cache, retSz.BaseType)
                                    ? CodeWriters.GetBlittableStructAbiType(writer, context, retSz.BaseType)
                                    : CodeWriters.GetAbiPrimitiveType(context.Cache, retSz.BaseType);
                string elementInteropArg = InteropTypeNameWriter.EncodeInteropTypeName(retSz.BaseType, TypedefNameType.Projected);

                _ = elementInteropArg;
                writer.Write(callIndent);
                writer.Write("[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"ConvertToManaged\")]\n");
                writer.Write(callIndent);
                writer.Write("static extern ");
                writer.Write(elementProjected);
                writer.Write("[] ConvertToManaged_retval([UnsafeAccessorType(\"");
                writer.Write(ArrayElementEncoder.GetArrayMarshallerInteropPath(retSz.BaseType));
                writer.Write("\")] object _, uint length, ");
                writer.Write(elementAbi);
                writer.Write("* data);\n");
                writer.Write(callIndent);
                writer.Write("return ConvertToManaged_retval(null, __retval_length, __retval_data);\n");
            }
            else if (returnIsHResultException)
            {
                writer.Write(callIndent);
                writer.Write("return global::ABI.System.ExceptionMarshaller.ConvertToManaged(__retval);\n");
            }
            else if (returnIsString)
            {
                writer.Write(callIndent);
                writer.Write("return HStringMarshaller.ConvertToManaged(__retval);\n");
            }
            else if (returnIsRefType)
            {
                if (rt.IsNullableT())
                {
                    // Nullable<T> return: use <T>Marshaller.UnboxToManaged. Mirrors truth pattern;
                    // there is no Nullable<T>Marshaller, the inner-T marshaller has UnboxToManaged.
                    AsmResolver.DotNet.Signatures.TypeSignature inner = rt.GetNullableInnerType()!;
                    string innerMarshaller = CodeWriters.GetNullableInnerMarshallerName(writer, context, inner);
                    writer.Write(callIndent);
                    writer.Write("return ");
                    writer.Write(innerMarshaller);
                    writer.Write(".UnboxToManaged(__retval);\n");
                }
                else if (rt.IsGenericInstance())
                {
                    string interopTypeName = InteropTypeNameWriter.EncodeInteropTypeName(rt, TypedefNameType.ABI) + ", WinRT.Interop";
                    IndentedTextWriter __scratchProjectedTypeName = new();
                    MethodFactory.WriteProjectedSignature(__scratchProjectedTypeName, context, rt, false);
                    string projectedTypeName = __scratchProjectedTypeName.ToString();
                    writer.Write(callIndent);
                    writer.Write("[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"ConvertToManaged\")]\n");
                    writer.Write(callIndent);
                    writer.Write("static extern ");
                    writer.Write(projectedTypeName);
                    writer.Write(" ConvertToManaged_retval([UnsafeAccessorType(\"");
                    writer.Write(interopTypeName);
                    writer.Write("\")] object _, void* value);\n");
                    writer.Write(callIndent);
                    writer.Write("return ConvertToManaged_retval(null, __retval);\n");
                }
                else
                {
                    writer.Write(callIndent);
                    writer.Write("return ");
                    EmitMarshallerConvertToManaged(writer, context, rt, "__retval");
                    writer.Write(";\n");
                }
            }
            else if (rt is not null && CodeWriters.IsMappedAbiValueType(rt))
            {
                // Mapped value type return (e.g. DateTime/TimeSpan): convert ABI struct back via marshaller.
                writer.Write(callIndent);
                writer.Write("return ");
                writer.Write(CodeWriters.GetMappedMarshallerName(rt));
                writer.Write(".ConvertToManaged(__retval);\n");
            }
            else if (rt is not null && rt.IsSystemType())
            {
                // System.Type return: convert ABI Type struct back to System.Type via TypeMarshaller.
                writer.Write(callIndent);
                writer.Write("return global::ABI.System.TypeMarshaller.ConvertToManaged(__retval);\n");
            }
            else if (returnIsAnyStruct)
            {
                writer.Write(callIndent);
                if (rt is not null && CodeWriters.IsMappedAbiValueType(rt))
                {
                    // Mapped value type return: convert ABI struct back to projected via marshaller.
                    writer.Write("return ");
                    writer.Write(CodeWriters.GetMappedMarshallerName(rt));
                    writer.Write(".ConvertToManaged(__retval);\n");
                }
                else
                {
                    writer.Write("return __retval;\n");
                }
            }
            else if (returnIsComplexStruct)
            {
                writer.Write(callIndent);
                writer.Write("return ");
                writer.Write(CodeWriters.GetMarshallerFullName(writer, context, rt!));
                writer.Write(".ConvertToManaged(__retval);\n");
            }
            else
            {
                writer.Write(callIndent);
                writer.Write("return ");
                IndentedTextWriter __scratchProjected = new();
                MethodFactory.WriteProjectedSignature(__scratchProjected, context, rt!, false);
                string projected = __scratchProjected.ToString();
                string abiType = CodeWriters.GetAbiPrimitiveType(context.Cache, rt!);
                if (projected == abiType) { writer.Write("__retval;\n"); }
                else
                {
                    writer.Write("(");
                    writer.Write(projected);
                    writer.Write(")__retval;\n");
                }
            }
        }

        // Close fixed blocks (innermost first).
        for (int i = fixedNesting - 1; i >= 0; i--)
        {
            writer.Write(indent);
            writer.Write(new string(' ', i * 4));
            writer.Write("}\n");
        }

        if (needsTryFinally)
        {
            writer.Write("        }\n        finally\n        {\n");

            // Order matches truth (mirrors C++ disposer iteration order):
            // 0. Complex-struct input param Dispose (e.g. ProfileUsageMarshaller.Dispose(__value))
            // 1. Non-blittable PassArray/FillArray cleanup (Dispose + ArrayPools)
            // 2. Out param frees (HString / object / runtime class)
            // 3. ReceiveArray param frees (Free_<name> via UnsafeAccessor)
            // 4. Return free (__retval) — last

            // 0. Dispose complex-struct input params via marshaller (both 'in' and 'in T' forms).
            for (int i = 0; i < sig.Params.Count; i++)
            {
                ParamInfo p = sig.Params[i];
                ParamCategory cat = ParamHelpers.GetParamCategory(p);
                if (cat != ParamCategory.In && cat != ParamCategory.Ref) { continue; }
                AsmResolver.DotNet.Signatures.TypeSignature pType = CodeWriters.StripByRefAndCustomModifiers(p.Type);
                if (!CodeWriters.IsComplexStruct(context.Cache, pType)) { continue; }
                string localName = CodeWriters.GetParamLocalName(p, paramNameOverride);
                writer.Write("            ");
                writer.Write(CodeWriters.GetMarshallerFullName(writer, context, pType));
                writer.Write(".Dispose(__");
                writer.Write(localName);
                writer.Write(");\n");
            }
            // 1. Cleanup non-blittable PassArray/FillArray params:
            // For strings: HStringArrayMarshaller.Dispose + return ArrayPools (3 of them).
            // For runtime classes/objects: Dispose_<name> (UnsafeAccessor) + return ArrayPool.
            // For mapped value types (DateTime/TimeSpan): no per-element disposal needed and truth
            // doesn't return the ArrayPool either, so skip entirely.
            for (int i = 0; i < sig.Params.Count; i++)
            {
                ParamInfo p = sig.Params[i];
                ParamCategory cat = ParamHelpers.GetParamCategory(p);
                if (cat != ParamCategory.PassArray && cat != ParamCategory.FillArray) { continue; }
                if (p.Type is not AsmResolver.DotNet.Signatures.SzArrayTypeSignature szArr) { continue; }
                if (CodeWriters.IsBlittablePrimitive(context.Cache, szArr.BaseType) || CodeWriters.IsAnyStruct(context.Cache, szArr.BaseType)) { continue; }
                if (CodeWriters.IsMappedAbiValueType(szArr.BaseType)) { continue; }
                if (szArr.BaseType.IsHResultException())
                {
                    // HResultException ABI is just an int; per-element Dispose is a no-op (mirror
                    // the truth: no Dispose_<name> emitted). Just return the inline-array's pool
                    // using the correct element type (ABI.System.Exception, not nint).
                    string localNameH = CodeWriters.GetParamLocalName(p, paramNameOverride);
                    writer.Write("\n            if (__");
                    writer.Write(localNameH);
                    writer.Write("_arrayFromPool is not null)\n            {\n");
                    writer.Write("                global::System.Buffers.ArrayPool<global::ABI.System.Exception>.Shared.Return(__");
                    writer.Write(localNameH);
                    writer.Write("_arrayFromPool);\n            }\n");
                    continue;
                }
                string localName = CodeWriters.GetParamLocalName(p, paramNameOverride);
                if (szArr.BaseType.IsString())
                {
                    // The HStringArrayMarshaller.Dispose + ArrayPool returns for strings only
                    // apply to PassArray (where we set up the pinned handles + headers in the
                    // first place). FillArray writes back HSTRING handles into the nint storage
                    // array directly, with no per-element pinned handle / header to release.
                    if (cat == ParamCategory.PassArray)
                    {
                        writer.Write("            HStringArrayMarshaller.Dispose(__");
                        writer.Write(localName);
                        writer.Write("_pinnedHandleSpan);\n\n");
                        writer.Write("            if (__");
                        writer.Write(localName);
                        writer.Write("_pinnedHandleArrayFromPool is not null)\n            {\n");
                        writer.Write("                global::System.Buffers.ArrayPool<nint>.Shared.Return(__");
                        writer.Write(localName);
                        writer.Write("_pinnedHandleArrayFromPool);\n            }\n\n");
                        writer.Write("            if (__");
                        writer.Write(localName);
                        writer.Write("_headerArrayFromPool is not null)\n            {\n");
                        writer.Write("                global::System.Buffers.ArrayPool<HStringHeader>.Shared.Return(__");
                        writer.Write(localName);
                        writer.Write("_headerArrayFromPool);\n            }\n");
                    }
                    // Both PassArray and FillArray need the inline-array's nint pool returned.
                    writer.Write("\n            if (__");
                    writer.Write(localName);
                    writer.Write("_arrayFromPool is not null)\n            {\n");
                    writer.Write("                global::System.Buffers.ArrayPool<nint>.Shared.Return(__");
                    writer.Write(localName);
                    writer.Write("_arrayFromPool);\n            }\n");
                }
                else
                {
                    // For complex structs, both the Dispose_<name> data param and the fixed()
                    // pointer must be typed as <ABI struct>*; the cast can be omitted. For
                    // runtime classes / objects / strings the data is void** and the fixed()
                    // remains void* with a (void**) cast.
                    string disposeDataParamType;
                    string fixedPtrType;
                    string disposeCastType;
                    if (CodeWriters.IsComplexStruct(context.Cache, szArr.BaseType))
                    {
                        string abiStructName = CodeWriters.GetAbiStructTypeName(writer, context, szArr.BaseType);
                        disposeDataParamType = abiStructName + "*";
                        fixedPtrType = abiStructName + "*";
                        disposeCastType = string.Empty;
                    }
                    else
                    {
                        disposeDataParamType = "void** data";
                        fixedPtrType = "void*";
                        disposeCastType = "(void**)";
                    }
                    string elementInteropArg = InteropTypeNameWriter.EncodeInteropTypeName(szArr.BaseType, TypedefNameType.Projected);

                    _ = elementInteropArg;
                    writer.Write("            [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"Dispose\")]\n");
                    writer.Write("            static extern void Dispose_");
                    writer.Write(localName);
                    writer.Write("([UnsafeAccessorType(\"");
                    writer.Write(ArrayElementEncoder.GetArrayMarshallerInteropPath(szArr.BaseType));
                    writer.Write("\")] object _, uint length, ");
                    writer.Write(disposeDataParamType);
                    if (!disposeDataParamType.EndsWith("data", System.StringComparison.Ordinal)) { writer.Write(" data"); }
                    writer.Write(");\n\n");
                    writer.Write("            fixed(");
                    writer.Write(fixedPtrType);
                    writer.Write(" _");
                    writer.Write(localName);
                    writer.Write(" = __");
                    writer.Write(localName);
                    writer.Write("_span)\n            {\n");
                    writer.Write("                Dispose_");
                    writer.Write(localName);
                    writer.Write("(null, (uint) __");
                    writer.Write(localName);
                    writer.Write("_span.Length, ");
                    writer.Write(disposeCastType);
                    writer.Write("_");
                    writer.Write(localName);
                    writer.Write(");\n            }\n");
                }
                // ArrayPool storage type matches the InlineArray storage (mapped ABI value type
                // for DateTime/TimeSpan; ABI struct for complex structs; nint otherwise).
                string poolStorageT = CodeWriters.IsMappedAbiValueType(szArr.BaseType)
                    ? CodeWriters.GetMappedAbiTypeName(szArr.BaseType)
                    : CodeWriters.IsComplexStruct(context.Cache, szArr.BaseType)
                        ? CodeWriters.GetAbiStructTypeName(writer, context, szArr.BaseType)
                        : "nint";
                writer.Write("\n            if (__");
                writer.Write(localName);
                writer.Write("_arrayFromPool is not null)\n            {\n");
                writer.Write("                global::System.Buffers.ArrayPool<");
                writer.Write(poolStorageT);
                writer.Write(">.Shared.Return(__");
                writer.Write(localName);
                writer.Write("_arrayFromPool);\n            }\n");
            }

            // 2. Free Out string/object/runtime-class params.
            for (int i = 0; i < sig.Params.Count; i++)
            {
                ParamInfo p = sig.Params[i];
                ParamCategory cat = ParamHelpers.GetParamCategory(p);
                if (cat != ParamCategory.Out) { continue; }
                AsmResolver.DotNet.Signatures.TypeSignature uOut = CodeWriters.StripByRefAndCustomModifiers(p.Type);
                string localName = CodeWriters.GetParamLocalName(p, paramNameOverride);
                if (uOut.IsString())
                {
                    writer.Write("            HStringMarshaller.Free(__");
                    writer.Write(localName);
                    writer.Write(");\n");
                }
                else if (uOut.IsObject() || CodeWriters.IsRuntimeClassOrInterface(context.Cache, uOut) || uOut.IsGenericInstance())
                {
                    writer.Write("            WindowsRuntimeUnknownMarshaller.Free(__");
                    writer.Write(localName);
                    writer.Write(");\n");
                }
                else if (uOut.IsSystemType())
                {
                    writer.Write("            global::ABI.System.TypeMarshaller.Dispose(__");
                    writer.Write(localName);
                    writer.Write(");\n");
                }
                else if (CodeWriters.IsComplexStruct(context.Cache, uOut))
                {
                    writer.Write("            ");
                    writer.Write(CodeWriters.GetMarshallerFullName(writer, context, uOut));
                    writer.Write(".Dispose(__");
                    writer.Write(localName);
                    writer.Write(");\n");
                }
            }

            // 3. Free ReceiveArray params via UnsafeAccessor.
            for (int i = 0; i < sig.Params.Count; i++)
            {
                ParamInfo p = sig.Params[i];
                ParamCategory cat = ParamHelpers.GetParamCategory(p);
                if (cat != ParamCategory.ReceiveArray) { continue; }
                string localName = CodeWriters.GetParamLocalName(p, paramNameOverride);
                AsmResolver.DotNet.Signatures.SzArrayTypeSignature sza = (AsmResolver.DotNet.Signatures.SzArrayTypeSignature)CodeWriters.StripByRefAndCustomModifiers(p.Type);
                // Element ABI type: void* for ref types; ABI struct for complex/blittable structs;
                // primitive ABI otherwise. (Same categorization as the ConvertToManaged_<name> path.)
                string elementAbi = sza.BaseType.IsString() || CodeWriters.IsRuntimeClassOrInterface(context.Cache, sza.BaseType) || sza.BaseType.IsObject()
                    ? "void*"
                    : CodeWriters.IsComplexStruct(context.Cache, sza.BaseType)
                        ? CodeWriters.GetAbiStructTypeName(writer, context, sza.BaseType)
                        : CodeWriters.IsAnyStruct(context.Cache, sza.BaseType)
                            ? CodeWriters.GetBlittableStructAbiType(writer, context, sza.BaseType)
                            : CodeWriters.GetAbiPrimitiveType(context.Cache, sza.BaseType);
                string elementInteropArg = InteropTypeNameWriter.EncodeInteropTypeName(sza.BaseType, TypedefNameType.Projected);

                _ = elementInteropArg;
                string marshallerPath = ArrayElementEncoder.GetArrayMarshallerInteropPath(sza.BaseType);
                writer.Write("            [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"Free\")]\n");
                writer.Write("            static extern void Free_");
                writer.Write(localName);
                writer.Write("([UnsafeAccessorType(\"");
                writer.Write(marshallerPath);
                writer.Write("\")] object _, uint length, ");
                writer.Write(elementAbi);
                writer.Write("* data);\n\n");
                writer.Write("            Free_");
                writer.Write(localName);
                writer.Write("(null, __");
                writer.Write(localName);
                writer.Write("_length, __");
                writer.Write(localName);
                writer.Write("_data);\n");
            }

            // 4. Free return value (__retval) — emitted last to match truth ordering.
            if (returnIsString)
            {
                writer.Write("            HStringMarshaller.Free(__retval);\n");
            }
            else if (returnIsRefType)
            {
                writer.Write("            WindowsRuntimeUnknownMarshaller.Free(__retval);\n");
            }
            else if (returnIsComplexStruct)
            {
                writer.Write("            ");
                writer.Write(CodeWriters.GetMarshallerFullName(writer, context, rt!));
                writer.Write(".Dispose(__retval);\n");
            }
            else if (returnIsSystemTypeForCleanup)
            {
                // System.Type return: dispose the ABI.System.Type's HSTRING fields.
                writer.Write("            global::ABI.System.TypeMarshaller.Dispose(__retval);\n");
            }
            else if (returnIsReceiveArray)
            {
                AsmResolver.DotNet.Signatures.SzArrayTypeSignature retSz = (AsmResolver.DotNet.Signatures.SzArrayTypeSignature)rt!;
                string elementAbi = retSz.BaseType.IsString() || CodeWriters.IsRuntimeClassOrInterface(context.Cache, retSz.BaseType) || retSz.BaseType.IsObject()
                    ? "void*"
                    : CodeWriters.IsComplexStruct(context.Cache, retSz.BaseType)
                        ? CodeWriters.GetAbiStructTypeName(writer, context, retSz.BaseType)
                        : retSz.BaseType.IsHResultException()
                            ? "global::ABI.System.Exception"
                            : CodeWriters.IsMappedAbiValueType(retSz.BaseType)
                                ? CodeWriters.GetMappedAbiTypeName(retSz.BaseType)
                                : CodeWriters.IsAnyStruct(context.Cache, retSz.BaseType)
                                    ? CodeWriters.GetBlittableStructAbiType(writer, context, retSz.BaseType)
                                    : CodeWriters.GetAbiPrimitiveType(context.Cache, retSz.BaseType);
                string elementInteropArg = InteropTypeNameWriter.EncodeInteropTypeName(retSz.BaseType, TypedefNameType.Projected);

                _ = elementInteropArg;
                writer.Write("            [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"Free\")]\n");
                writer.Write("            static extern void Free_retval([UnsafeAccessorType(\"");
                writer.Write(ArrayElementEncoder.GetArrayMarshallerInteropPath(retSz.BaseType));
                writer.Write("\")] object _, uint length, ");
                writer.Write(elementAbi);
                writer.Write("* data);\n");
                writer.Write("            Free_retval(null, __retval_length, __retval_data);\n");
            }

            writer.Write("        }\n");
        }

        writer.Write("    }\n");
    }

    /// <summary>Emits the call to the appropriate marshaller's ConvertToUnmanaged for a runtime class / object input parameter.</summary>
    internal static void EmitMarshallerConvertToUnmanaged(IndentedTextWriter writer, ProjectionEmitContext context, AsmResolver.DotNet.Signatures.TypeSignature sig, string argName)
    {
        if (sig.IsObject())
        {
            writer.Write("WindowsRuntimeObjectMarshaller.ConvertToUnmanaged(");
            writer.Write(argName);
            writer.Write(")");
            return;
        }
        // Runtime class / interface: use ABI.<NS>.<Name>Marshaller
        writer.Write(CodeWriters.GetMarshallerFullName(writer, context, sig));
        writer.Write(".ConvertToUnmanaged(");
        writer.Write(argName);
        writer.Write(")");
    }

    /// <summary>Emits the call to the appropriate marshaller's ConvertToManaged for a runtime class / object return value.</summary>
    internal static void EmitMarshallerConvertToManaged(IndentedTextWriter writer, ProjectionEmitContext context, AsmResolver.DotNet.Signatures.TypeSignature sig, string argName)
    {
        if (sig.IsObject())
        {
            writer.Write("WindowsRuntimeObjectMarshaller.ConvertToManaged(");
            writer.Write(argName);
            writer.Write(")");
            return;
        }
        writer.Write(CodeWriters.GetMarshallerFullName(writer, context, sig));
        writer.Write(".ConvertToManaged(");
        writer.Write(argName);
        writer.Write(")");
    }

    /// <summary>Emits the conversion of a parameter from its projected (managed) form to the ABI argument form.</summary>
    internal static void EmitParamArgConversion(IndentedTextWriter writer, ProjectionEmitContext context, ParamInfo p, string? paramNameOverride = null)
    {
        string pname = paramNameOverride ?? p.Parameter.Name ?? "param";
        // bool: ABI is 'bool' directly; pass as-is.
        if (p.Type is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlib &&
            corlib.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Boolean)
        {
            writer.Write(pname);
        }
        // char: ABI is 'char' directly; pass as-is.
        else if (p.Type is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlib2 &&
                 corlib2.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Char)
        {
            writer.Write(pname);
        }
        // Enums: function pointer signature uses the projected enum type, so pass directly.
        else if (CodeWriters.IsEnumType(context.Cache, p.Type))
        {
            writer.Write(pname);
        }
        else
        {
            writer.Write(pname);
        }
    }
}
