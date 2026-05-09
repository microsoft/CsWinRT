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
            && (AbiTypeHelpers.IsBlittablePrimitive(context.Cache, retSzAbi.BaseType) || AbiTypeHelpers.IsAnyStruct(context.Cache, retSzAbi.BaseType)
                || retSzAbi.BaseType.IsString() || AbiTypeHelpers.IsRuntimeClassOrInterface(context.Cache, retSzAbi.BaseType) || retSzAbi.BaseType.IsObject()
                || AbiTypeHelpers.IsComplexStruct(context.Cache, retSzAbi.BaseType));
        bool returnIsHResultExceptionDoAbi = rt is not null && rt.IsHResultException();
        bool returnIsString = rt is not null && rt.IsString();
        bool returnIsRefType = rt is not null && (AbiTypeHelpers.IsRuntimeClassOrInterface(context.Cache, rt) || rt.IsObject() || rt.IsGenericInstance());
        bool returnIsGenericInstance = rt is not null && rt.IsGenericInstance();
        bool returnIsBlittableStruct = rt is not null && AbiTypeHelpers.IsAnyStruct(context.Cache, rt);

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
        string retParamName = AbiTypeHelpers.GetReturnParamName(sig);
        string retSizeParamName = AbiTypeHelpers.GetReturnSizeParamName(sig);
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
            writer.WriteLine("    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"ConvertToUnmanaged\")]");
            writer.Write($"    static extern WindowsRuntimeObjectReferenceValue ConvertToUnmanaged_{retParamName}([UnsafeAccessorType(\"{interopTypeName}\")] object _, {projectedTypeName} value);\n\n");
        }

        // Hoist [UnsafeAccessor] declarations for Out generic-instance params:
        // ConvertToUnmanaged_<name> wraps the projected value into a WindowsRuntimeObjectReferenceValue.
        // The body's writeback later references these already-declared accessors.
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.Out) { continue; }
            AsmResolver.DotNet.Signatures.TypeSignature uOut = AbiTypeHelpers.StripByRefAndCustomModifiers(p.Type);
            if (!uOut.IsGenericInstance()) { continue; }
            string raw = p.Parameter.Name ?? "param";
            string interopTypeName = InteropTypeNameWriter.EncodeInteropTypeName(uOut, TypedefNameType.ABI) + ", WinRT.Interop";
            IndentedTextWriter __scratchProjectedTypeName = new();
            MethodFactory.WriteProjectedSignature(__scratchProjectedTypeName, context, uOut, false);
            string projectedTypeName = __scratchProjectedTypeName.ToString();
            writer.WriteLine("    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"ConvertToUnmanaged\")]");
            writer.Write($"    static extern WindowsRuntimeObjectReferenceValue ConvertToUnmanaged_{raw}([UnsafeAccessorType(\"{interopTypeName}\")] object _, {projectedTypeName} value);\n\n");
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
            AsmResolver.DotNet.Signatures.SzArrayTypeSignature sza = (AsmResolver.DotNet.Signatures.SzArrayTypeSignature)AbiTypeHelpers.StripByRefAndCustomModifiers(p.Type);
            IndentedTextWriter __scratchElementProjected = new();
            TypedefNameWriter.WriteProjectionType(__scratchElementProjected, context, TypeSemanticsFactory.Get(sza.BaseType));
            string elementProjected = __scratchElementProjected.ToString();
            string elementInteropArg = InteropTypeNameWriter.EncodeInteropTypeName(sza.BaseType, TypedefNameType.Projected);

            _ = elementInteropArg;
            string marshallerPath = ArrayElementEncoder.GetArrayMarshallerInteropPath(sza.BaseType);
            string elementAbi = sza.BaseType.IsString() || AbiTypeHelpers.IsRuntimeClassOrInterface(context.Cache, sza.BaseType) || sza.BaseType.IsObject()
                ? "void*"
                : AbiTypeHelpers.IsComplexStruct(context.Cache, sza.BaseType)
                    ? AbiTypeHelpers.GetAbiStructTypeName(writer, context, sza.BaseType)
                    : AbiTypeHelpers.IsAnyStruct(context.Cache, sza.BaseType)
                        ? AbiTypeHelpers.GetBlittableStructAbiType(writer, context, sza.BaseType)
                        : AbiTypeHelpers.GetAbiPrimitiveType(context.Cache, sza.BaseType);
            writer.WriteLine("    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"ConvertToUnmanaged\")]");
            writer.Write($"    static extern void ConvertToUnmanaged_{raw}([UnsafeAccessorType(\"{marshallerPath}\")] object _, ReadOnlySpan<{elementProjected}> span, out uint length, out {elementAbi}* data);\n\n");
        }
        if (returnIsReceiveArrayDoAbi && rt is AsmResolver.DotNet.Signatures.SzArrayTypeSignature retSzHoist)
        {
            IndentedTextWriter __scratchElementProjected = new();
            TypedefNameWriter.WriteProjectionType(__scratchElementProjected, context, TypeSemanticsFactory.Get(retSzHoist.BaseType));
            string elementProjected = __scratchElementProjected.ToString();
            string elementAbi = retSzHoist.BaseType.IsString() || AbiTypeHelpers.IsRuntimeClassOrInterface(context.Cache, retSzHoist.BaseType) || retSzHoist.BaseType.IsObject()
                ? "void*"
                : AbiTypeHelpers.IsComplexStruct(context.Cache, retSzHoist.BaseType)
                    ? AbiTypeHelpers.GetAbiStructTypeName(writer, context, retSzHoist.BaseType)
                    : AbiTypeHelpers.IsAnyStruct(context.Cache, retSzHoist.BaseType)
                        ? AbiTypeHelpers.GetBlittableStructAbiType(writer, context, retSzHoist.BaseType)
                        : AbiTypeHelpers.GetAbiPrimitiveType(context.Cache, retSzHoist.BaseType);
            string elementInteropArg = InteropTypeNameWriter.EncodeInteropTypeName(retSzHoist.BaseType, TypedefNameType.Projected);

            _ = elementInteropArg;
            string marshallerPath = ArrayElementEncoder.GetArrayMarshallerInteropPath(retSzHoist.BaseType);
            writer.WriteLine("    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"ConvertToUnmanaged\")]");
            writer.Write($"    static extern void ConvertToUnmanaged_{retParamName}([UnsafeAccessorType(\"{marshallerPath}\")] object _, ReadOnlySpan<{elementProjected}> span, out uint length, out {elementAbi}* data);\n\n");
        }
        // the OUT pointer(s). The actual assignment happens inside the try block.
        if (rt is not null)
        {
            if (returnIsString)
            {
                writer.WriteLine($"    string {retLocalName} = default;");
            }
            else if (returnIsRefType)
            {
                IndentedTextWriter __scratchProjected = new();
                MethodFactory.WriteProjectedSignature(__scratchProjected, context, rt, false);
                string projected = __scratchProjected.ToString();
                writer.WriteLine($"    {projected} {retLocalName} = default;");
            }
            else if (returnIsReceiveArrayDoAbi)
            {
                IndentedTextWriter __scratchProjected = new();
                MethodFactory.WriteProjectedSignature(__scratchProjected, context, rt, false);
                string projected = __scratchProjected.ToString();
                writer.WriteLine($"    {projected} {retLocalName} = default;");
            }
            else
            {
                IndentedTextWriter __scratchProjected = new();
                MethodFactory.WriteProjectedSignature(__scratchProjected, context, rt, false);
                string projected = __scratchProjected.ToString();
                writer.WriteLine($"    {projected} {retLocalName} = default;");
            }
        }

        if (rt is not null)
        {
            if (returnIsReceiveArrayDoAbi)
            {
                writer.Write("    *");
                writer.Write(retParamName);
                writer.WriteLine(" = default;");
                writer.WriteLine($"    *{retSizeParamName} = default;");
            }
            else
            {
                writer.WriteLine($"    *{retParamName} = default;");
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
            writer.WriteLine($"    *{ptr} = default;");
        }
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.Out) { continue; }
            string raw = p.Parameter.Name ?? "param";
            // Use the projected (non-ABI) type for the local variable.
            // Strip ByRef and CustomModifier wrappers to get the underlying base type.
            AsmResolver.DotNet.Signatures.TypeSignature underlying = AbiTypeHelpers.StripByRefAndCustomModifiers(p.Type);
            IndentedTextWriter __scratchProjected = new();
            MethodFactory.WriteProjectedSignature(__scratchProjected, context, underlying, false);
            string projected = __scratchProjected.ToString();
            writer.WriteLine($"    {projected} __{raw} = default;");
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
            AsmResolver.DotNet.Signatures.SzArrayTypeSignature sza = (AsmResolver.DotNet.Signatures.SzArrayTypeSignature)AbiTypeHelpers.StripByRefAndCustomModifiers(p.Type);
            IndentedTextWriter __scratchElementProjected = new();
            TypedefNameWriter.WriteProjectionType(__scratchElementProjected, context, TypeSemanticsFactory.Get(sza.BaseType));
            string elementProjected = __scratchElementProjected.ToString();
            writer.Write("    *");
            writer.Write(ptr);
            writer.WriteLine(" = default;");
            writer.Write("    *__");
            writer.Write(raw);
            writer.WriteLine("Size = default;");
            writer.WriteLine($"    {elementProjected}[] __{raw} = default;");
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
            bool isBlittableElem = AbiTypeHelpers.IsBlittablePrimitive(context.Cache, sz.BaseType) || AbiTypeHelpers.IsAnyStruct(context.Cache, sz.BaseType);
            if (isBlittableElem)
            {
                writer.WriteLine($"    {(cat == ParamCategory.PassArray ? "ReadOnlySpan<" : "Span<")}{elementProjected}> __{raw} = new({ptr}, (int)__{raw}Size);");
            }
            else
            {
                // Non-blittable element: InlineArray16<T> + ArrayPool<T> with size from ABI.
                writer.Write("\n    Unsafe.SkipInit(out InlineArray16<");
                writer.Write(elementProjected);
                writer.Write("> __");
                writer.Write(raw);
                writer.WriteLine("_inlineArray);");
                writer.Write("    ");
                writer.Write(elementProjected);
                writer.Write("[] __");
                writer.Write(raw);
                writer.WriteLine("_arrayFromPool = null;");
                writer.WriteLine($"    Span<{elementProjected}> __{raw} = __{raw}Size <= 16\n        ? __{raw}_inlineArray[..(int)__{raw}Size]\n        : (__{raw}_arrayFromPool = global::System.Buffers.ArrayPool<{elementProjected}>.Shared.Rent((int)__{raw}Size));");
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
            if (AbiTypeHelpers.IsBlittablePrimitive(context.Cache, szArr.BaseType) || AbiTypeHelpers.IsAnyStruct(context.Cache, szArr.BaseType)) { continue; }
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
            if (AbiTypeHelpers.IsComplexStruct(context.Cache, szArr.BaseType))
            {
                string abiStructName = AbiTypeHelpers.GetAbiStructTypeName(writer, context, szArr.BaseType);
                dataParamType = abiStructName + "* data";
                dataCastExpr = "(" + abiStructName + "*)" + ptr;
            }
            else
            {
                dataParamType = "void** data";
                dataCastExpr = "(void**)" + ptr;
            }
            writer.WriteLine("        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"CopyToManaged\")]");
            writer.Write("        static extern void CopyToManaged_");
            writer.Write(raw);
            writer.Write("([UnsafeAccessorType(\"");
            writer.Write(ArrayElementEncoder.GetArrayMarshallerInteropPath(szArr.BaseType));
            writer.Write("\")] object _, uint length, ");
            writer.Write(dataParamType);
            writer.Write(", Span<");
            writer.Write(elementProjected);
            writer.WriteLine("> span);");
            writer.WriteLine($"        CopyToManaged_{raw}(null, __{raw}Size, {dataCastExpr}, __{raw});");
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
                string innerMarshaller = AbiTypeHelpers.GetNullableInnerMarshallerName(writer, context, inner);
                writer.WriteLine($"        var __arg_{rawName} = {innerMarshaller}.UnboxToManaged({callName});");
            }
            else if (p.Type.IsGenericInstance())
            {
                string rawName = p.Parameter.Name ?? "param";
                string callName = CSharpKeywords.IsKeyword(rawName) ? "@" + rawName : rawName;
                string interopTypeName = InteropTypeNameWriter.EncodeInteropTypeName(p.Type, TypedefNameType.ABI) + ", WinRT.Interop";
                IndentedTextWriter __scratchProjectedTypeName = new();
                MethodFactory.WriteProjectedSignature(__scratchProjectedTypeName, context, p.Type, false);
                string projectedTypeName = __scratchProjectedTypeName.ToString();
                writer.WriteLine("        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"ConvertToManaged\")]");
                writer.Write("        static extern ");
                writer.Write(projectedTypeName);
                writer.Write(" ConvertToManaged_arg_");
                writer.Write(rawName);
                writer.Write("([UnsafeAccessorType(\"");
                writer.Write(interopTypeName);
                writer.WriteLine("\")] object _, void* value);");
                writer.WriteLine($"        var __arg_{rawName} = ConvertToManaged_arg_{rawName}(null, {callName});");
            }
        }

        if (returnIsString)
        {
            writer.Write($"        {retLocalName} = ");
        }
        else if (returnIsRefType)
        {
            writer.Write($"        {retLocalName} = ");
        }
        else if (returnIsReceiveArrayDoAbi)
        {
            // For T[] return: assign to existing local.
            writer.Write($"        {retLocalName} = ");
        }
        else if (rt is not null)
        {
            writer.Write($"        {retLocalName} = ");
        }
        else
        {
            writer.Write("        ");
        }

        if (isGetter)
        {
            string propName = methodName[4..];
            writer.WriteLine($"ComInterfaceDispatch.GetInstance<{ifaceFullName}>((ComInterfaceDispatch*)thisPtr).{propName};");
        }
        else if (isSetter)
        {
            string propName = methodName[4..];
            writer.Write($"ComInterfaceDispatch.GetInstance<{ifaceFullName}>((ComInterfaceDispatch*)thisPtr).{propName} = ");
            EmitDoAbiParamArgConversion(writer, context, sig.Params[0]);
            writer.WriteLine(";");
        }
        else
        {
            writer.Write($"ComInterfaceDispatch.GetInstance<{ifaceFullName}>((ComInterfaceDispatch*)thisPtr).{methodName}(");
            for (int i = 0; i < sig.Params.Count; i++)
            {
                if (i > 0) { writer.Write(",\n  "); }
                ParamInfo p = sig.Params[i];
                ParamCategory cat = ParamHelpers.GetParamCategory(p);
                if (cat == ParamCategory.Out)
                {
                    string raw = p.Parameter.Name ?? "param";
                    writer.Write($"out __{raw}");
                }
                else if (cat == ParamCategory.Ref)
                {
                    // WinRT 'in T' / 'ref const T' is a read-only by-ref input on the ABI side
                    // (pointer to a value the native caller owns). On the C# delegate / interface
                    // side it's projected as 'in T'. Read directly from *<name> via the appropriate
                    // marshaller — DO NOT zero or write back.
                    string raw = p.Parameter.Name ?? "param";
                    string ptr = CSharpKeywords.IsKeyword(raw) ? "@" + raw : raw;
                    AsmResolver.DotNet.Signatures.TypeSignature uRef = AbiTypeHelpers.StripByRefAndCustomModifiers(p.Type);
                    if (uRef.IsString())
                    {
                        writer.Write($"HStringMarshaller.ConvertToManaged(*{ptr})");
                    }
                    else if (uRef.IsObject())
                    {
                        writer.Write($"WindowsRuntimeObjectMarshaller.ConvertToManaged(*{ptr})");
                    }
                    else if (AbiTypeHelpers.IsRuntimeClassOrInterface(context.Cache, uRef))
                    {
                        writer.Write($"{AbiTypeHelpers.GetMarshallerFullName(writer, context, uRef)}.ConvertToManaged(*{ptr})");
                    }
                    else if (AbiTypeHelpers.IsMappedAbiValueType(uRef))
                    {
                        writer.Write($"{AbiTypeHelpers.GetMappedMarshallerName(uRef)}.ConvertToManaged(*{ptr})");
                    }
                    else if (uRef.IsHResultException())
                    {
                        writer.Write($"global::ABI.System.ExceptionMarshaller.ConvertToManaged(*{ptr})");
                    }
                    else if (AbiTypeHelpers.IsComplexStruct(context.Cache, uRef))
                    {
                        writer.Write($"{AbiTypeHelpers.GetMarshallerFullName(writer, context, uRef)}.ConvertToManaged(*{ptr})");
                    }
                    else if (AbiTypeHelpers.IsAnyStruct(context.Cache, uRef) || AbiTypeHelpers.IsBlittablePrimitive(context.Cache, uRef) || AbiTypeHelpers.IsEnumType(context.Cache, uRef))
                    {
                        // Blittable/almost-blittable: ABI layout matches projected layout.
                        writer.Write($"*{ptr}");
                    }
                    else
                    {
                        writer.Write($"*{ptr}");
                    }
                }
                else if (cat == ParamCategory.PassArray || cat == ParamCategory.FillArray)
                {
                    string raw = p.Parameter.Name ?? "param";
                    writer.Write($"__{raw}");
                }
                else if (cat == ParamCategory.ReceiveArray)
                {
                    string raw = p.Parameter.Name ?? "param";
                    writer.Write($"out __{raw}");
                }
                else
                {
                    EmitDoAbiParamArgConversion(writer, context, p);
                }
            }
            writer.WriteLine(");");
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
            AsmResolver.DotNet.Signatures.TypeSignature underlying = AbiTypeHelpers.StripByRefAndCustomModifiers(p.Type);
            writer.Write($"        *{ptr} = ");
            // String: HStringMarshaller.ConvertToUnmanaged
            if (underlying.IsString())
            {
                writer.Write($"HStringMarshaller.ConvertToUnmanaged(__{raw})");
            }
            // Object/runtime class: <Marshaller>.ConvertToUnmanaged(...).DetachThisPtrUnsafe()
            else if (underlying.IsObject())
            {
                writer.Write($"WindowsRuntimeObjectMarshaller.ConvertToUnmanaged(__{raw}).DetachThisPtrUnsafe()");
            }
            else if (AbiTypeHelpers.IsRuntimeClassOrInterface(context.Cache, underlying))
            {
                writer.Write($"{AbiTypeHelpers.GetMarshallerFullName(writer, context, underlying)}.ConvertToUnmanaged(__{raw}).DetachThisPtrUnsafe()");
            }
            // Generic instance (e.g. IEnumerable<string>): use the hoisted UnsafeAccessor
            // 'ConvertToUnmanaged_<name>' declared at the top of the method body.
            else if (underlying.IsGenericInstance())
            {
                writer.Write($"ConvertToUnmanaged_{raw}(null, __{raw}).DetachThisPtrUnsafe()");
            }
            // For enums, function pointer signature uses the projected enum type, no cast needed.
            // For bool, cast to byte. For char, cast to ushort.
            else if (AbiTypeHelpers.IsEnumType(context.Cache, underlying))
            {
                writer.Write($"__{raw}");
            }
            else if (underlying is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlibBool &&
                     corlibBool.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Boolean)
            {
                writer.Write($"__{raw}");
            }
            else if (underlying is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlibChar &&
                     corlibChar.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Char)
            {
                writer.Write($"__{raw}");
            }
            // Non-blittable struct (e.g. authored BasicStruct with string fields): marshal
            // the local managed value through <Type>Marshaller.ConvertToUnmanaged before
            // writing it into the *out ABI struct slot. Mirrors C++ marshaler.write_marshal_from_managed
            //: "Marshaller.ConvertToUnmanaged(local)".
            else if (AbiTypeHelpers.IsComplexStruct(context.Cache, underlying))
            {
                writer.Write($"{AbiTypeHelpers.GetMarshallerFullName(writer, context, underlying)}.ConvertToUnmanaged(__{raw})");
            }
            else
            {
                writer.Write($"__{raw}");
            }
            writer.WriteLine(";");
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
            writer.WriteLine($"        ConvertToUnmanaged_{raw}(null, __{raw}, out *__{raw}Size, out *{ptr});");
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
            if (AbiTypeHelpers.IsBlittablePrimitive(context.Cache, szFA.BaseType) || AbiTypeHelpers.IsAnyStruct(context.Cache, szFA.BaseType)) { continue; }
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
            if (szFA.BaseType.IsString() || AbiTypeHelpers.IsRuntimeClassOrInterface(context.Cache, szFA.BaseType) || szFA.BaseType.IsObject())
            {
                dataParamType = "void** data";
                dataCastType = "(void**)";
            }
            else if (szFA.BaseType.IsHResultException())
            {
                dataParamType = "global::ABI.System.Exception* data";
                dataCastType = "(global::ABI.System.Exception*)";
            }
            else if (AbiTypeHelpers.IsMappedAbiValueType(szFA.BaseType))
            {
                string abiName = AbiTypeHelpers.GetMappedAbiTypeName(szFA.BaseType);
                dataParamType = abiName + "* data";
                dataCastType = "(" + abiName + "*)";
            }
            else
            {
                string abiStructName = AbiTypeHelpers.GetAbiStructTypeName(writer, context, szFA.BaseType);
                dataParamType = abiStructName + "* data";
                dataCastType = "(" + abiStructName + "*)";
            }
            writer.WriteLine("        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"CopyToUnmanaged\")]");
            writer.Write("        static extern void CopyToUnmanaged_");
            writer.Write(raw);
            writer.Write("([UnsafeAccessorType(\"");
            writer.Write(ArrayElementEncoder.GetArrayMarshallerInteropPath(szFA.BaseType));
            writer.Write("\")] object _, ReadOnlySpan<");
            writer.Write(elementProjected);
            writer.Write("> span, uint length, ");
            writer.Write(dataParamType);
            writer.WriteLine(");");
            writer.WriteLine($"        CopyToUnmanaged_{raw}(null, __{raw}, __{raw}Size, {dataCastType}{ptr});");
        }
        if (rt is not null)
        {
            if (returnIsHResultExceptionDoAbi)
            {
                writer.WriteLine($"        *{retParamName} = global::ABI.System.ExceptionMarshaller.ConvertToUnmanaged({retLocalName});");
            }
            else if (returnIsString)
            {
                writer.WriteLine($"        *{retParamName} = HStringMarshaller.ConvertToUnmanaged({retLocalName});");
            }
            else if (returnIsRefType)
            {
                if (rt is not null && rt.IsNullableT())
                {
                    // Nullable<T> return (server-side): use <T>Marshaller.BoxToUnmanaged.
                    AsmResolver.DotNet.Signatures.TypeSignature inner = rt.GetNullableInnerType()!;
                    string innerMarshaller = AbiTypeHelpers.GetNullableInnerMarshallerName(writer, context, inner);
                    writer.WriteLine($"        *{retParamName} = {innerMarshaller}.BoxToUnmanaged({retLocalName}).DetachThisPtrUnsafe();");
                }
                else if (returnIsGenericInstance)
                {
                    // Generic instance return: use the UnsafeAccessor static local function declared at
                    // the top of the method body via the M12 hoisting pass; just emit the call here.
                    writer.WriteLine($"        *{retParamName} = ConvertToUnmanaged_{retParamName}(null, {retLocalName}).DetachThisPtrUnsafe();");
                }
                else
                {
                    writer.Write($"        *{retParamName} = ");
                    EmitMarshallerConvertToUnmanaged(writer, context, rt!, retLocalName);
                    writer.WriteLine(".DetachThisPtrUnsafe();");
                }
            }
            else if (returnIsReceiveArrayDoAbi)
            {
                // Return-receive-array: emit ConvertToUnmanaged_<retParam> call (declaration
                // was hoisted to the top of the method body).
                writer.WriteLine($"        ConvertToUnmanaged_{retParamName}(null, {retLocalName}, out *{retSizeParamName}, out *{retParamName});");
            }
            else if (AbiTypeHelpers.IsMappedAbiValueType(rt))
            {
                // Mapped value type return (DateTime/TimeSpan): convert via marshaller.
                writer.WriteLine($"        *{retParamName} = {AbiTypeHelpers.GetMappedMarshallerName(rt)}.ConvertToUnmanaged({retLocalName});");
            }
            else if (rt.IsSystemType())
            {
                // System.Type return (server-side): convert managed System.Type to ABI Type struct.
                writer.WriteLine($"        *{retParamName} = global::ABI.System.TypeMarshaller.ConvertToUnmanaged({retLocalName});");
            }
            else if (AbiTypeHelpers.IsComplexStruct(context.Cache, rt))
            {
                // Complex struct return (server-side): convert managed struct to ABI struct via marshaller.
                writer.WriteLine($"        *{retParamName} = {AbiTypeHelpers.GetMarshallerFullName(writer, context, rt)}.ConvertToUnmanaged({retLocalName});");
            }
            else if (returnIsBlittableStruct)
            {
                writer.WriteLine($"        *{retParamName} = {retLocalName};");
            }
            else
            {
                string abiType = AbiTypeHelpers.GetAbiPrimitiveType(context.Cache, rt);
                writer.Write($"        *{retParamName} = ");
                if (rt is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlib &&
                    corlib.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Boolean)
                {
                    writer.WriteLine($"{retLocalName};");
                }
                else if (rt is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlib2 &&
                         corlib2.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Char)
                {
                    writer.WriteLine($"{retLocalName};");
                }
                else if (AbiTypeHelpers.IsEnumType(context.Cache, rt))
                {
                    // Enum: function pointer signature uses the projected enum type, no cast needed.
                    writer.WriteLine($"{retLocalName};");
                }
                else
                {
                    writer.WriteLine($"{retLocalName};");
                }
            }
        }
        writer.Write("        return 0;\n    }\n    catch (Exception __exception__)\n    {\n        return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(__exception__);\n    }\n");

        // For non-blittable PassArray params, emit finally block with ArrayPool<T>.Shared.Return.
        bool hasNonBlittableArrayDoAbi = false;
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.PassArray && cat != ParamCategory.FillArray) { continue; }
            if (p.Type is not AsmResolver.DotNet.Signatures.SzArrayTypeSignature szArr) { continue; }
            if (AbiTypeHelpers.IsBlittablePrimitive(context.Cache, szArr.BaseType) || AbiTypeHelpers.IsAnyStruct(context.Cache, szArr.BaseType)) { continue; }
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
                if (AbiTypeHelpers.IsBlittablePrimitive(context.Cache, szArr.BaseType) || AbiTypeHelpers.IsAnyStruct(context.Cache, szArr.BaseType)) { continue; }
                string raw = p.Parameter.Name ?? "param";
                IndentedTextWriter __scratchElementProjected = new();
                TypedefNameWriter.WriteProjectionType(__scratchElementProjected, context, TypeSemanticsFactory.Get(szArr.BaseType));
                string elementProjected = __scratchElementProjected.ToString();
                writer.Write($"\n        if (__{raw}_arrayFromPool is not null)\n        {{\n            global::System.Buffers.ArrayPool<{elementProjected}>.Shared.Return(__{raw}_arrayFromPool);\n        }}\n");
            }
            writer.WriteLine("    }");
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
            writer.Write($"HStringMarshaller.ConvertToManaged({pname})");
        }
        else if (p.Type.IsGenericInstance())
        {
            // Generic instance ABI parameter: caller already declared a local UnsafeAccessor +
            // local var __arg_<name> that holds the converted value.
            writer.Write($"__arg_{rawName}");
        }
        else if (AbiTypeHelpers.IsRuntimeClassOrInterface(context.Cache, p.Type) || p.Type.IsObject())
        {
            EmitMarshallerConvertToManaged(writer, context, p.Type, pname);
        }
        else if (AbiTypeHelpers.IsMappedAbiValueType(p.Type))
        {
            // Mapped value type input (DateTime/TimeSpan): the parameter is the ABI type;
            // convert to the projected managed type via the marshaller.
            writer.Write($"{AbiTypeHelpers.GetMappedMarshallerName(p.Type)}.ConvertToManaged({pname})");
        }
        else if (p.Type.IsSystemType())
        {
            // System.Type input (server-side): convert ABI Type struct to System.Type.
            writer.Write($"global::ABI.System.TypeMarshaller.ConvertToManaged({pname})");
        }
        else if (AbiTypeHelpers.IsComplexStruct(context.Cache, p.Type))
        {
            // Complex struct input (server-side): convert ABI struct to managed via marshaller.
            writer.Write($"{AbiTypeHelpers.GetMarshallerFullName(writer, context, p.Type)}.ConvertToManaged({pname})");
        }
        else if (AbiTypeHelpers.IsAnyStruct(context.Cache, p.Type))
        {
            // Blittable / almost-blittable struct: pass directly (projected type == ABI type).
            writer.Write(pname);
        }
        else if (AbiTypeHelpers.IsEnumType(context.Cache, p.Type))
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

            writer.WriteLine("    [MethodImpl(MethodImplOptions.NoInlining)]");
            writer.Write("    public static unsafe ");
            MethodFactory.WriteProjectionReturnType(writer, context, sig);
            writer.Write($" {mname}(WindowsRuntimeObjectReference thisReference");
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
            string propType = InterfaceFactory.WritePropType(context, prop);
            (MethodDefinition? gMethod, MethodDefinition? sMethod) = (getter, setter);
            // accessors of the property (the attribute is on the property itself, not on the
            // individual accessors).
            bool propIsNoExcept = prop.IsNoExcept();
            if (gMethod is not null)
            {
                MethodSig getSig = new(gMethod);
                writer.WriteLine("    [MethodImpl(MethodImplOptions.NoInlining)]");
                writer.Write($"    public static unsafe {propType} {pname}(WindowsRuntimeObjectReference thisReference)");
                EmitAbiMethodBodyIfSimple(writer, context, getSig, methodSlot[gMethod], isNoExcept: propIsNoExcept);
            }
            if (sMethod is not null)
            {
                MethodSig setSig = new(sMethod);
                writer.WriteLine("    [MethodImpl(MethodImplOptions.NoInlining)]");
                writer.Write($"    public static unsafe void {pname}(WindowsRuntimeObjectReference thisReference, {InterfaceFactory.WritePropType(context, prop, isSetProperty: true)} value)");
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
            writer.WriteLine("        [MethodImpl(MethodImplOptions.AggressiveInlining)]");
            writer.Write("        get\n        {\n");
            writer.WriteLine("            [MethodImpl(MethodImplOptions.NoInlining)]");
            writer.Write("            static ConditionalWeakTable<object, ");
            writer.Write(eventSourceProjectedFull);
            writer.Write("> MakeTable()\n            {\n");
            writer.Write("                _ = global::System.Threading.Interlocked.CompareExchange(ref field, [], null);\n\n");
            writer.WriteLine("                return global::System.Threading.Volatile.Read(in field);");
            writer.Write($"            }}\n\n            return global::System.Threading.Volatile.Read(in field) ?? MakeTable();\n        }}\n    }}\n\n    public static {eventSourceProjectedFull} {evtName}(object thisObject, WindowsRuntimeObjectReference thisReference)\n    {{\n");
            if (isGenericEvent && !string.IsNullOrEmpty(eventSourceInteropType))
            {
                writer.WriteLine("        [UnsafeAccessor(UnsafeAccessorKind.Constructor)]");
                writer.Write("        [return: UnsafeAccessorType(\"");
                writer.Write(eventSourceInteropType);
                writer.WriteLine("\")]");
                writer.Write("        static extern object ctor(WindowsRuntimeObjectReference nativeObjectReference, int index);\n\n");
                writer.Write("        return _");
                writer.Write(evtName);
                writer.WriteLine(".GetOrAdd(");
                writer.WriteLine("            key: thisObject,");
                writer.Write("            valueFactory: static (_, thisReference) => Unsafe.As<");
                writer.Write(eventSourceProjectedFull);
                writer.Write(">(ctor(thisReference, ");
                writer.Write(eventSlot.ToString(System.Globalization.CultureInfo.InvariantCulture));
                writer.WriteLine(")),");
                writer.WriteLine("            factoryArgument: thisReference);");
            }
            else
            {
                // Non-generic delegate: directly construct.
                writer.Write("        return _");
                writer.Write(evtName);
                writer.WriteLine(".GetOrAdd(");
                writer.WriteLine("            key: thisObject,");
                writer.Write("            valueFactory: static (_, thisReference) => new ");
                writer.Write(eventSourceProjectedFull);
                writer.Write("(thisReference, ");
                writer.Write(eventSlot.ToString(System.Globalization.CultureInfo.InvariantCulture));
                writer.WriteLine("),");
                writer.WriteLine("            factoryArgument: thisReference);");
            }
            writer.WriteLine("    }");
        }
    }

    /// <summary>
    /// Emits a real method body for the cases we can fully marshal, otherwise emits
    /// the 'throw null!' stub. Trailing newline is included.
    /// </summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="context">The active emit context.</param>
    /// <param name="sig">The interface method signature being emitted.</param>
    /// <param name="slot">The vtable slot of the method on the runtime interface.</param>
    /// <param name="paramNameOverride">When provided, overrides the default 'thisReference' parameter name (used by FastAbi-merged Methods classes).</param>
    /// <param name="isNoExcept">When true, the vtable call is emitted WITHOUT the
    /// <c>RestrictedErrorInfo.ThrowExceptionForHR(...)</c> wrap (methods/properties annotated with
    /// <c>[Windows.Foundation.Metadata.NoExceptionAttribute]</c>, or remove-overload methods,
    /// contractually return <c>S_OK</c>).</param>
    internal static void EmitAbiMethodBodyIfSimple(IndentedTextWriter writer, ProjectionEmitContext context, MethodSig sig, int slot, string? paramNameOverride = null, bool isNoExcept = false)
    {
        AsmResolver.DotNet.Signatures.TypeSignature? rt = sig.ReturnType;

        bool returnIsString = rt is not null && rt.IsString();
        bool returnIsRefType = rt is not null && (AbiTypeHelpers.IsRuntimeClassOrInterface(context.Cache, rt) || rt.IsObject() || rt.IsGenericInstance());
        bool returnIsAnyStruct = rt is not null && AbiTypeHelpers.IsAnyStruct(context.Cache, rt);
        bool returnIsComplexStruct = rt is not null && AbiTypeHelpers.IsComplexStruct(context.Cache, rt);
        bool returnIsReceiveArray = rt is AsmResolver.DotNet.Signatures.SzArrayTypeSignature retSzCheck
            && (AbiTypeHelpers.IsBlittablePrimitive(context.Cache, retSzCheck.BaseType) || AbiTypeHelpers.IsAnyStruct(context.Cache, retSzCheck.BaseType)
                || retSzCheck.BaseType.IsString() || AbiTypeHelpers.IsRuntimeClassOrInterface(context.Cache, retSzCheck.BaseType) || retSzCheck.BaseType.IsObject()
                || AbiTypeHelpers.IsComplexStruct(context.Cache, retSzCheck.BaseType)
                || retSzCheck.BaseType.IsHResultException()
                || AbiTypeHelpers.IsMappedAbiValueType(retSzCheck.BaseType));
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
                AsmResolver.DotNet.Signatures.TypeSignature uOut = AbiTypeHelpers.StripByRefAndCustomModifiers(p.Type);
                fp.Append(", ");
                if (uOut.IsString() || AbiTypeHelpers.IsRuntimeClassOrInterface(context.Cache, uOut) || uOut.IsObject() || uOut.IsGenericInstance()) { fp.Append("void**"); }
                else if (uOut.IsSystemType()) { fp.Append("global::ABI.System.Type*"); }
                else if (AbiTypeHelpers.IsComplexStruct(context.Cache, uOut)) { fp.Append(AbiTypeHelpers.GetAbiStructTypeName(writer, context, uOut)); fp.Append('*'); }
                else if (AbiTypeHelpers.IsAnyStruct(context.Cache, uOut)) { fp.Append(AbiTypeHelpers.GetBlittableStructAbiType(writer, context, uOut)); fp.Append('*'); }
                else { fp.Append(AbiTypeHelpers.GetAbiPrimitiveType(context.Cache, uOut)); fp.Append('*'); }
                continue;
            }
            if (cat == ParamCategory.Ref)
            {
                AsmResolver.DotNet.Signatures.TypeSignature uRef = AbiTypeHelpers.StripByRefAndCustomModifiers(p.Type);
                fp.Append(", ");
                if (AbiTypeHelpers.IsComplexStruct(context.Cache, uRef)) { fp.Append(AbiTypeHelpers.GetAbiStructTypeName(writer, context, uRef)); fp.Append('*'); }
                else if (AbiTypeHelpers.IsAnyStruct(context.Cache, uRef)) { fp.Append(AbiTypeHelpers.GetBlittableStructAbiType(writer, context, uRef)); fp.Append('*'); }
                else { fp.Append(AbiTypeHelpers.GetAbiPrimitiveType(context.Cache, uRef)); fp.Append('*'); }
                continue;
            }
            if (cat == ParamCategory.ReceiveArray)
            {
                AsmResolver.DotNet.Signatures.SzArrayTypeSignature sza = (AsmResolver.DotNet.Signatures.SzArrayTypeSignature)AbiTypeHelpers.StripByRefAndCustomModifiers(p.Type);
                fp.Append(", uint*, ");
                if (sza.BaseType.IsString() || AbiTypeHelpers.IsRuntimeClassOrInterface(context.Cache, sza.BaseType) || sza.BaseType.IsObject())
                {
                    fp.Append("void*");
                }
                else if (sza.BaseType.IsHResultException())
                {
                    fp.Append("global::ABI.System.Exception");
                }
                else if (AbiTypeHelpers.IsMappedAbiValueType(sza.BaseType))
                {
                    fp.Append(AbiTypeHelpers.GetMappedAbiTypeName(sza.BaseType));
                }
                else if (AbiTypeHelpers.IsComplexStruct(context.Cache, sza.BaseType)) { fp.Append(AbiTypeHelpers.GetAbiStructTypeName(writer, context, sza.BaseType)); }
                else if (AbiTypeHelpers.IsAnyStruct(context.Cache, sza.BaseType)) { fp.Append(AbiTypeHelpers.GetBlittableStructAbiType(writer, context, sza.BaseType)); }
                else { fp.Append(AbiTypeHelpers.GetAbiPrimitiveType(context.Cache, sza.BaseType)); }
                fp.Append("**");
                continue;
            }
            fp.Append(", ");
            if (p.Type.IsHResultException()) { fp.Append("global::ABI.System.Exception"); }
            else if (p.Type.IsString() || AbiTypeHelpers.IsRuntimeClassOrInterface(context.Cache, p.Type) || p.Type.IsObject() || p.Type.IsGenericInstance()) { fp.Append("void*"); }
            else if (p.Type.IsSystemType()) { fp.Append("global::ABI.System.Type"); }
            else if (AbiTypeHelpers.IsAnyStruct(context.Cache, p.Type)) { fp.Append(AbiTypeHelpers.GetBlittableStructAbiType(writer, context, p.Type)); }
            else if (AbiTypeHelpers.IsMappedAbiValueType(p.Type)) { fp.Append(AbiTypeHelpers.GetMappedAbiTypeName(p.Type)); }
            else if (AbiTypeHelpers.IsComplexStruct(context.Cache, p.Type)) { fp.Append(AbiTypeHelpers.GetAbiStructTypeName(writer, context, p.Type)); }
            else { fp.Append(AbiTypeHelpers.GetAbiPrimitiveType(context.Cache, p.Type)); }
        }
        if (rt is not null)
        {
            if (returnIsReceiveArray)
            {
                AsmResolver.DotNet.Signatures.SzArrayTypeSignature retSz = (AsmResolver.DotNet.Signatures.SzArrayTypeSignature)rt;
                fp.Append(", uint*, ");
                if (retSz.BaseType.IsString() || AbiTypeHelpers.IsRuntimeClassOrInterface(context.Cache, retSz.BaseType) || retSz.BaseType.IsObject())
                {
                    fp.Append("void*");
                }
                else if (AbiTypeHelpers.IsComplexStruct(context.Cache, retSz.BaseType))
                {
                    fp.Append(AbiTypeHelpers.GetAbiStructTypeName(writer, context, retSz.BaseType));
                }
                else if (retSz.BaseType.IsHResultException())
                {
                    fp.Append("global::ABI.System.Exception");
                }
                else if (AbiTypeHelpers.IsMappedAbiValueType(retSz.BaseType))
                {
                    fp.Append(AbiTypeHelpers.GetMappedAbiTypeName(retSz.BaseType));
                }
                else if (AbiTypeHelpers.IsAnyStruct(context.Cache, retSz.BaseType))
                {
                    fp.Append(AbiTypeHelpers.GetBlittableStructAbiType(writer, context, retSz.BaseType));
                }
                else
                {
                    fp.Append(AbiTypeHelpers.GetAbiPrimitiveType(context.Cache, retSz.BaseType));
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
                else if (returnIsAnyStruct) { fp.Append(AbiTypeHelpers.GetBlittableStructAbiType(writer, context, rt!)); fp.Append('*'); }
                else if (returnIsComplexStruct) { fp.Append(AbiTypeHelpers.GetAbiStructTypeName(writer, context, rt!)); fp.Append('*'); }
                else if (rt is not null && AbiTypeHelpers.IsMappedAbiValueType(rt)) { fp.Append(AbiTypeHelpers.GetMappedAbiTypeName(rt)); fp.Append('*'); }
                else { fp.Append(AbiTypeHelpers.GetAbiPrimitiveType(context.Cache, rt!)); fp.Append('*'); }
            }
        }
        fp.Append(", int");

        writer.Write("\n    {\n");
        writer.WriteLine("        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();");
        writer.WriteLine("        void* ThisPtr = thisValue.GetThisPtrUnsafe();");

        // Declare 'using' marshaller values for ref-type parameters (these need disposing).
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            if (AbiTypeHelpers.IsRuntimeClassOrInterface(context.Cache, p.Type) || p.Type.IsObject())
            {
                string localName = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);
                string callName = AbiTypeHelpers.GetParamName(p, paramNameOverride);
                writer.Write($"        using WindowsRuntimeObjectReferenceValue __{localName} = ");
                EmitMarshallerConvertToUnmanaged(writer, context, p.Type, callName);
                writer.WriteLine(";");
            }
            else if (p.Type.IsNullableT())
            {
                // Nullable<T> param: use <T>Marshaller.BoxToUnmanaged. Mirrors truth pattern.
                string localName = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);
                string callName = AbiTypeHelpers.GetParamName(p, paramNameOverride);
                AsmResolver.DotNet.Signatures.TypeSignature inner = p.Type.GetNullableInnerType()!;
                string innerMarshaller = AbiTypeHelpers.GetNullableInnerMarshallerName(writer, context, inner);
                writer.WriteLine($"        using WindowsRuntimeObjectReferenceValue __{localName} = {innerMarshaller}.BoxToUnmanaged({callName});");
            }
            else if (p.Type.IsGenericInstance())
            {
                // Generic instance param: emit a local UnsafeAccessor delegate to get the marshaller method.
                string localName = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);
                string callName = AbiTypeHelpers.GetParamName(p, paramNameOverride);
                string interopTypeName = InteropTypeNameWriter.EncodeInteropTypeName(p.Type, TypedefNameType.ABI) + ", WinRT.Interop";
                IndentedTextWriter __scratchProjectedTypeName = new();
                MethodFactory.WriteProjectedSignature(__scratchProjectedTypeName, context, p.Type, false);
                string projectedTypeName = __scratchProjectedTypeName.ToString();
                writer.WriteLine("        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"ConvertToUnmanaged\")]");
                writer.Write("        static extern WindowsRuntimeObjectReferenceValue ConvertToUnmanaged_");
                writer.Write(localName);
                writer.Write("([UnsafeAccessorType(\"");
                writer.Write(interopTypeName);
                writer.Write("\")] object _, ");
                writer.Write(projectedTypeName);
                writer.WriteLine(" value);");
                writer.WriteLine($"        using WindowsRuntimeObjectReferenceValue __{localName} = ConvertToUnmanaged_{localName}(null, {callName});");
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
            string localName = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);
            string callName = AbiTypeHelpers.GetParamName(p, paramNameOverride);
            writer.WriteLine($"        global::ABI.System.Exception __{localName} = global::ABI.System.ExceptionMarshaller.ConvertToUnmanaged({callName});");
        }
        // Declare locals for mapped value-type input parameters (DateTime/TimeSpan): convert via marshaller up-front.
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            if (ParamHelpers.GetParamCategory(p) != ParamCategory.In) { continue; }
            if (!AbiTypeHelpers.IsMappedAbiValueType(p.Type)) { continue; }
            string localName = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);
            string callName = AbiTypeHelpers.GetParamName(p, paramNameOverride);
            writer.WriteLine($"        {AbiTypeHelpers.GetMappedAbiTypeName(p.Type)} __{localName} = {AbiTypeHelpers.GetMappedMarshallerName(p.Type)}.ConvertToUnmanaged({callName});");
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
            AsmResolver.DotNet.Signatures.TypeSignature pType = AbiTypeHelpers.StripByRefAndCustomModifiers(p.Type);
            if (!AbiTypeHelpers.IsComplexStruct(context.Cache, pType)) { continue; }
            string localName = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);
            writer.WriteLine($"        {AbiTypeHelpers.GetAbiStructTypeName(writer, context, pType)} __{localName} = default;");
        }
        // Declare locals for Out parameters (need to be passed as &__<name> to the call).
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.Out) { continue; }
            string localName = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);
            AsmResolver.DotNet.Signatures.TypeSignature uOut = AbiTypeHelpers.StripByRefAndCustomModifiers(p.Type);
            writer.Write("        ");
            if (uOut.IsString() || AbiTypeHelpers.IsRuntimeClassOrInterface(context.Cache, uOut) || uOut.IsObject() || uOut.IsGenericInstance()) { writer.Write("void*"); }
            else if (uOut.IsSystemType()) { writer.Write("global::ABI.System.Type"); }
            else if (AbiTypeHelpers.IsComplexStruct(context.Cache, uOut)) { writer.Write(AbiTypeHelpers.GetAbiStructTypeName(writer, context, uOut)); }
            else if (AbiTypeHelpers.IsAnyStruct(context.Cache, uOut)) { writer.Write(AbiTypeHelpers.GetBlittableStructAbiType(writer, context, uOut)); }
            else { writer.Write(AbiTypeHelpers.GetAbiPrimitiveType(context.Cache, uOut)); }
            writer.WriteLine($" __{localName} = default;");
        }
        // Declare locals for ReceiveArray params (uint length + element pointer).
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.ReceiveArray) { continue; }
            string localName = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);
            AsmResolver.DotNet.Signatures.SzArrayTypeSignature sza = (AsmResolver.DotNet.Signatures.SzArrayTypeSignature)AbiTypeHelpers.StripByRefAndCustomModifiers(p.Type);
            writer.Write("        uint __");
            writer.Write(localName);
            writer.WriteLine("_length = default;");
            writer.Write("        ");
            // Element ABI type: void* for ref types; ABI struct for complex/blittable structs;
            // primitive ABI otherwise.
            if (sza.BaseType.IsString() || AbiTypeHelpers.IsRuntimeClassOrInterface(context.Cache, sza.BaseType) || sza.BaseType.IsObject())
            {
                writer.Write("void*");
            }
            else if (AbiTypeHelpers.IsComplexStruct(context.Cache, sza.BaseType))
            {
                writer.Write(AbiTypeHelpers.GetAbiStructTypeName(writer, context, sza.BaseType));
            }
            else if (AbiTypeHelpers.IsAnyStruct(context.Cache, sza.BaseType))
            {
                writer.Write(AbiTypeHelpers.GetBlittableStructAbiType(writer, context, sza.BaseType));
            }
            else
            {
                writer.Write(AbiTypeHelpers.GetAbiPrimitiveType(context.Cache, sza.BaseType));
            }
            writer.WriteLine($"* __{localName}_data = default;");
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
            if (AbiTypeHelpers.IsBlittablePrimitive(context.Cache, szArr.BaseType) || AbiTypeHelpers.IsAnyStruct(context.Cache, szArr.BaseType)) { continue; }
            // Non-blittable element type: emit InlineArray16<storageT> + ArrayPool<storageT>.
            // For mapped value types (DateTime/TimeSpan), use the ABI struct type.
            // For complex structs (e.g. authored BasicStruct with reference fields), use the ABI
            // struct type. For everything else (runtime classes, objects, strings), use nint.
            string localName = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);
            string callName = AbiTypeHelpers.GetParamName(p, paramNameOverride);
            string storageT = AbiTypeHelpers.IsMappedAbiValueType(szArr.BaseType)
                ? AbiTypeHelpers.GetMappedAbiTypeName(szArr.BaseType)
                : AbiTypeHelpers.IsComplexStruct(context.Cache, szArr.BaseType)
                    ? AbiTypeHelpers.GetAbiStructTypeName(writer, context, szArr.BaseType)
                    : szArr.BaseType.IsHResultException()
                        ? "global::ABI.System.Exception"
                        : "nint";
            writer.Write("\n        Unsafe.SkipInit(out InlineArray16<");
            writer.Write(storageT);
            writer.Write("> __");
            writer.Write(localName);
            writer.WriteLine("_inlineArray);");
            writer.Write("        ");
            writer.Write(storageT);
            writer.Write("[] __");
            writer.Write(localName);
            writer.WriteLine("_arrayFromPool = null;");
            writer.WriteLine($"        Span<{storageT}> __{localName}_span = {callName}.Length <= 16\n            ? __{localName}_inlineArray[..{callName}.Length]\n            : (__{localName}_arrayFromPool = global::System.Buffers.ArrayPool<{storageT}>.Shared.Rent({callName}.Length));");

            if (szArr.BaseType.IsString() && cat == ParamCategory.PassArray)
            {
                // Strings need an additional InlineArray16<HStringHeader> + InlineArray16<nint> (pinned handles).
                // Only required for PassArray (managed -> HSTRING conversion); FillArray's native side
                // fills HSTRING handles directly into the nint storage.
                writer.Write("\n        Unsafe.SkipInit(out InlineArray16<HStringHeader> __");
                writer.Write(localName);
                writer.WriteLine("_inlineHeaderArray);");
                writer.Write("        HStringHeader[] __");
                writer.Write(localName);
                writer.WriteLine("_headerArrayFromPool = null;");
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
                writer.WriteLine(".Length));");

                writer.Write("\n        Unsafe.SkipInit(out InlineArray16<nint> __");
                writer.Write(localName);
                writer.WriteLine("_inlinePinnedHandleArray);");
                writer.Write("        nint[] __");
                writer.Write(localName);
                writer.WriteLine("_pinnedHandleArrayFromPool = null;");
                writer.WriteLine($"        Span<nint> __{localName}_pinnedHandleSpan = {callName}.Length <= 16\n            ? __{localName}_inlinePinnedHandleArray[..{callName}.Length]\n            : (__{localName}_pinnedHandleArrayFromPool = global::System.Buffers.ArrayPool<nint>.Shared.Rent({callName}.Length));");
            }
        }
        if (returnIsReceiveArray)
        {
            AsmResolver.DotNet.Signatures.SzArrayTypeSignature retSz = (AsmResolver.DotNet.Signatures.SzArrayTypeSignature)rt!;
            writer.WriteLine("        uint __retval_length = default;");
            writer.Write("        ");
            if (retSz.BaseType.IsString() || AbiTypeHelpers.IsRuntimeClassOrInterface(context.Cache, retSz.BaseType) || retSz.BaseType.IsObject())
            {
                writer.Write("void*");
            }
            else if (AbiTypeHelpers.IsComplexStruct(context.Cache, retSz.BaseType))
            {
                writer.Write(AbiTypeHelpers.GetAbiStructTypeName(writer, context, retSz.BaseType));
            }
            else if (retSz.BaseType.IsHResultException())
            {
                writer.Write("global::ABI.System.Exception");
            }
            else if (AbiTypeHelpers.IsMappedAbiValueType(retSz.BaseType))
            {
                writer.Write(AbiTypeHelpers.GetMappedAbiTypeName(retSz.BaseType));
            }
            else if (AbiTypeHelpers.IsAnyStruct(context.Cache, retSz.BaseType))
            {
                writer.Write(AbiTypeHelpers.GetBlittableStructAbiType(writer, context, retSz.BaseType));
            }
            else
            {
                writer.Write(AbiTypeHelpers.GetAbiPrimitiveType(context.Cache, retSz.BaseType));
            }
            writer.WriteLine("* __retval_data = default;");
        }
        else if (returnIsHResultException)
        {
            writer.WriteLine("        global::ABI.System.Exception __retval = default;");
        }
        else if (returnIsString || returnIsRefType)
        {
            writer.WriteLine("        void* __retval = default;");
        }
        else if (returnIsAnyStruct)
        {
            writer.WriteLine($"        {AbiTypeHelpers.GetBlittableStructAbiType(writer, context, rt!)} __retval = default;");
        }
        else if (returnIsComplexStruct)
        {
            writer.WriteLine($"        {AbiTypeHelpers.GetAbiStructTypeName(writer, context, rt!)} __retval = default;");
        }
        else if (rt is not null && AbiTypeHelpers.IsMappedAbiValueType(rt))
        {
            // Mapped value type return (e.g. DateTime/TimeSpan): use the ABI struct as __retval.
            writer.WriteLine($"        {AbiTypeHelpers.GetMappedAbiTypeName(rt)} __retval = default;");
        }
        else if (rt is not null && rt.IsSystemType())
        {
            // System.Type return: use ABI Type struct as __retval.
            writer.WriteLine("        global::ABI.System.Type __retval = default;");
        }
        else if (rt is not null)
        {
            writer.WriteLine($"        {AbiTypeHelpers.GetAbiPrimitiveType(context.Cache, rt)} __retval = default;");
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
            AsmResolver.DotNet.Signatures.TypeSignature uOut = AbiTypeHelpers.StripByRefAndCustomModifiers(p.Type);
            if (uOut.IsString() || AbiTypeHelpers.IsRuntimeClassOrInterface(context.Cache, uOut) || uOut.IsObject() || uOut.IsSystemType() || AbiTypeHelpers.IsComplexStruct(context.Cache, uOut) || uOut.IsGenericInstance()) { hasOutNeedsCleanup = true; break; }
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
                && !AbiTypeHelpers.IsBlittablePrimitive(context.Cache, szArrCheck.BaseType) && !AbiTypeHelpers.IsAnyStruct(context.Cache, szArrCheck.BaseType)
                && !AbiTypeHelpers.IsMappedAbiValueType(szArrCheck.BaseType))
            {
                hasNonBlittablePassArray = true; break;
            }
        }
        bool hasComplexStructInput = false;
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if ((cat == ParamCategory.In || cat == ParamCategory.Ref) && AbiTypeHelpers.IsComplexStruct(context.Cache, AbiTypeHelpers.StripByRefAndCustomModifiers(p.Type))) { hasComplexStructInput = true; break; }
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
            AsmResolver.DotNet.Signatures.TypeSignature pType = AbiTypeHelpers.StripByRefAndCustomModifiers(p.Type);
            if (!AbiTypeHelpers.IsComplexStruct(context.Cache, pType)) { continue; }
            string localName = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);
            string callName = AbiTypeHelpers.GetParamName(p, paramNameOverride);
            writer.WriteLine($"{indent}__{localName} = {AbiTypeHelpers.GetMarshallerFullName(writer, context, pType)}.ConvertToUnmanaged({callName});");
        }
        // Type input params: set up TypeReference locals before the fixed block. Mirrors truth:
        //   global::ABI.System.TypeMarshaller.ConvertToUnmanagedUnsafe(forType, out TypeReference __forType);
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            if (ParamHelpers.GetParamCategory(p) != ParamCategory.In) { continue; }
            if (!p.Type.IsSystemType()) { continue; }
            string localName = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);
            string callName = AbiTypeHelpers.GetParamName(p, paramNameOverride);
            writer.WriteLine($"{indent}global::ABI.System.TypeMarshaller.ConvertToUnmanagedUnsafe({callName}, out TypeReference __{localName});");
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
                AsmResolver.DotNet.Signatures.TypeSignature uRefSkip = AbiTypeHelpers.StripByRefAndCustomModifiers(p.Type);
                if (AbiTypeHelpers.IsComplexStruct(context.Cache, uRefSkip)) { continue; }
                string callName = AbiTypeHelpers.GetParamName(p, paramNameOverride);
                string localName = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);
                AsmResolver.DotNet.Signatures.TypeSignature uRef = uRefSkip;
                string abiType = AbiTypeHelpers.IsAnyStruct(context.Cache, uRef) ? AbiTypeHelpers.GetBlittableStructAbiType(writer, context, uRef) : AbiTypeHelpers.GetAbiPrimitiveType(context.Cache, uRef);
                writer.WriteLine($"{indent}{new string(' ', fixedNesting * 4)}fixed({abiType}* _{localName} = &{callName})");
                typedFixedCount++;
            }
        }

        // Step 2: Emit ONE combined fixed-void* block for all pinnables that share the
        // same scope. Each variable is "_localName = rhsExpr". Strings get an extra
        // "_localName_inlineHeaderArray = __localName_headerSpan" entry.
        bool stringPinnablesEmitted = false;
        if (hasAnyVoidStarPinnable)
        {
            writer.Write($"{indent}{new string(' ', fixedNesting * 4)}fixed(void* ");
            bool first = true;
            for (int i = 0; i < sig.Params.Count; i++)
            {
                ParamInfo p = sig.Params[i];
                ParamCategory cat = ParamHelpers.GetParamCategory(p);
                bool isString = p.Type.IsString();
                bool isType = p.Type.IsSystemType();
                bool isPassArray = cat == ParamCategory.PassArray || cat == ParamCategory.FillArray;
                if (!isString && !isType && !isPassArray) { continue; }
                string callName = AbiTypeHelpers.GetParamName(p, paramNameOverride);
                string localName = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);
                if (!first) { writer.Write(", "); }
                first = false;
                writer.Write($"_{localName} = ");
                if (isType)
                {
                    writer.Write($"__{localName}");
                }
                else if (isPassArray)
                {
                    AsmResolver.DotNet.Signatures.TypeSignature elemT = ((AsmResolver.DotNet.Signatures.SzArrayTypeSignature)p.Type).BaseType;
                    bool isBlittableElem = AbiTypeHelpers.IsBlittablePrimitive(context.Cache, elemT) || AbiTypeHelpers.IsAnyStruct(context.Cache, elemT);
                    bool isStringElem = elemT.IsString();
                    if (isBlittableElem)
                    {
                        writer.Write(callName);
                    }
                    else
                    {
                        writer.Write($"__{localName}_span");
                    }
                    // For string elements: only PassArray needs the additional inlineHeaderArray
                    // pinned alongside the data span. FillArray fills HSTRINGs into the nint
                    // storage directly (no header conversion needed).
                    if (isStringElem && cat == ParamCategory.PassArray)
                    {
                        writer.Write($", _{localName}_inlineHeaderArray = __{localName}_headerSpan");
                    }
                }
                else
                {
                    // string param
                    writer.Write(callName);
                }
            }
            writer.WriteLine(")");
            writer.WriteLine($"{indent}{new string(' ', fixedNesting * 4)}{{");
            fixedNesting++;
            // Inside the body: emit HStringMarshaller calls for input string params.
            for (int i = 0; i < sig.Params.Count; i++)
            {
                if (!sig.Params[i].Type.IsString()) { continue; }
                string callName = AbiTypeHelpers.GetParamName(sig.Params[i], paramNameOverride);
                string localName = AbiTypeHelpers.GetParamLocalName(sig.Params[i], paramNameOverride);
                writer.WriteLine($"{indent}{new string(' ', fixedNesting * 4)}HStringMarshaller.ConvertToUnmanagedUnsafe((char*)_{localName}, {callName}?.Length, out HStringReference __{localName});");
            }
            stringPinnablesEmitted = true;
        }
        else if (typedFixedCount > 0)
        {
            // Typed fixed lines exist but no void* combined block - we need a body block
            // to host them. Open a brace block after the last typed fixed line.
            writer.WriteLine($"{indent}{new string(' ', fixedNesting * 4)}{{");
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
            if (AbiTypeHelpers.IsBlittablePrimitive(context.Cache, szArr.BaseType) || AbiTypeHelpers.IsAnyStruct(context.Cache, szArr.BaseType)) { continue; }
            string callName = AbiTypeHelpers.GetParamName(p, paramNameOverride);
            string localName = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);
            if (szArr.BaseType.IsString())
            {
                // Skip pre-call ConvertToUnmanagedUnsafe for FillArray of strings — there's
                // nothing to convert (native fills the handles). Mirrors C++ truth pattern.
                if (cat == ParamCategory.FillArray) { continue; }
                writer.Write(callIndent);
                writer.WriteLine("HStringArrayMarshaller.ConvertToUnmanagedUnsafe(");
                writer.Write(callIndent);
                writer.Write("    source: ");
                writer.Write(callName);
                writer.WriteLine(",");
                writer.Write(callIndent);
                writer.Write("    hstringHeaders: (HStringHeader*) _");
                writer.Write(localName);
                writer.WriteLine("_inlineHeaderArray,");
                writer.Write(callIndent);
                writer.Write("    hstrings: __");
                writer.Write(localName);
                writer.WriteLine("_span,");
                writer.WriteLine($"{callIndent}    pinnedGCHandles: __{localName}_pinnedHandleSpan);");
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
                if (AbiTypeHelpers.IsMappedAbiValueType(szArr.BaseType))
                {
                    dataParamType = AbiTypeHelpers.GetMappedAbiTypeName(szArr.BaseType) + "*";
                    dataCastType = "(" + AbiTypeHelpers.GetMappedAbiTypeName(szArr.BaseType) + "*)";
                }
                else if (szArr.BaseType.IsHResultException())
                {
                    dataParamType = "global::ABI.System.Exception*";
                    dataCastType = "(global::ABI.System.Exception*)";
                }
                else if (AbiTypeHelpers.IsComplexStruct(context.Cache, szArr.BaseType))
                {
                    string abiStructName = AbiTypeHelpers.GetAbiStructTypeName(writer, context, szArr.BaseType);
                    dataParamType = abiStructName + "*";
                    dataCastType = "(" + abiStructName + "*)";
                }
                else
                {
                    dataParamType = "void**";
                    dataCastType = "(void**)";
                }
                writer.Write(callIndent);
                writer.WriteLine("[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"CopyToUnmanaged\")]");
                writer.Write(callIndent);
                writer.Write("static extern void CopyToUnmanaged_");
                writer.Write(localName);
                writer.Write("([UnsafeAccessorType(\"");
                writer.Write(ArrayElementEncoder.GetArrayMarshallerInteropPath(szArr.BaseType));
                writer.Write("\")] object _, ReadOnlySpan<");
                writer.Write(elementProjected);
                writer.Write("> span, uint length, ");
                writer.Write(dataParamType);
                writer.WriteLine(" data);");
                writer.WriteLine($"{callIndent}CopyToUnmanaged_{localName}(null, {callName}, (uint){callName}.Length, {dataCastType}_{localName});");
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
        writer.Write($"{fp}>**)ThisPtr)[{slot.ToString(System.Globalization.CultureInfo.InvariantCulture)}](ThisPtr");
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat == ParamCategory.PassArray || cat == ParamCategory.FillArray)
            {
                string callName = AbiTypeHelpers.GetParamName(p, paramNameOverride);
                string localName = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);
                writer.Write($",\n  (uint){callName}.Length, _{localName}");
                continue;
            }
            if (cat == ParamCategory.Out)
            {
                string localName = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);
                writer.Write($",\n  &__{localName}");
                continue;
            }
            if (cat == ParamCategory.ReceiveArray)
            {
                string localName = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);
                writer.Write($",\n  &__{localName}_length, &__{localName}_data");
                continue;
            }
            if (cat == ParamCategory.Ref)
            {
                string localName = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);
                AsmResolver.DotNet.Signatures.TypeSignature uRefArg = AbiTypeHelpers.StripByRefAndCustomModifiers(p.Type);
                if (AbiTypeHelpers.IsComplexStruct(context.Cache, uRefArg))
                {
                    // Complex struct 'in' (Ref) param: pass &__local (the marshaled ABI struct).
                    writer.Write($",\n  &__{localName}");
                }
                else
                {
                    // 'in T' projected param: pass the pinned pointer.
                    writer.Write($",\n  _{localName}");
                }
                continue;
            }
            writer.Write(",\n  ");
            if (p.Type.IsHResultException())
            {
                writer.Write($"__{AbiTypeHelpers.GetParamLocalName(p, paramNameOverride)}");
            }
            else if (p.Type.IsString())
            {
                writer.Write($"__{AbiTypeHelpers.GetParamLocalName(p, paramNameOverride)}.HString");
            }
            else if (AbiTypeHelpers.IsRuntimeClassOrInterface(context.Cache, p.Type) || p.Type.IsObject() || p.Type.IsGenericInstance())
            {
                writer.Write($"__{AbiTypeHelpers.GetParamLocalName(p, paramNameOverride)}.GetThisPtrUnsafe()");
            }
            else if (p.Type.IsSystemType())
            {
                // System.Type input: pass the pre-converted ABI Type struct (via the local set up before the call).
                writer.Write($"__{AbiTypeHelpers.GetParamLocalName(p, paramNameOverride)}.ConvertToUnmanagedUnsafe()");
            }
            else if (AbiTypeHelpers.IsMappedAbiValueType(p.Type))
            {
                // Mapped value-type input: pass the pre-converted ABI local.
                writer.Write($"__{AbiTypeHelpers.GetParamLocalName(p, paramNameOverride)}");
            }
            else if (AbiTypeHelpers.IsComplexStruct(context.Cache, p.Type))
            {
                // Complex struct input: pass the pre-converted ABI struct local.
                writer.Write($"__{AbiTypeHelpers.GetParamLocalName(p, paramNameOverride)}");
            }
            else if (AbiTypeHelpers.IsAnyStruct(context.Cache, p.Type))
            {
                writer.Write(AbiTypeHelpers.GetParamName(p, paramNameOverride));
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
            if (AbiTypeHelpers.IsBlittablePrimitive(context.Cache, szFA.BaseType) || AbiTypeHelpers.IsAnyStruct(context.Cache, szFA.BaseType)) { continue; }
            string callName = AbiTypeHelpers.GetParamName(p, paramNameOverride);
            string localName = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);
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
            if (szFA.BaseType.IsString() || AbiTypeHelpers.IsRuntimeClassOrInterface(context.Cache, szFA.BaseType) || szFA.BaseType.IsObject())
            {
                dataParamType = "void** data";
                dataCastType = "(void**)";
            }
            else if (szFA.BaseType.IsHResultException())
            {
                dataParamType = "global::ABI.System.Exception* data";
                dataCastType = "(global::ABI.System.Exception*)";
            }
            else if (AbiTypeHelpers.IsMappedAbiValueType(szFA.BaseType))
            {
                string abiName = AbiTypeHelpers.GetMappedAbiTypeName(szFA.BaseType);
                dataParamType = abiName + "* data";
                dataCastType = "(" + abiName + "*)";
            }
            else
            {
                string abiStructName = AbiTypeHelpers.GetAbiStructTypeName(writer, context, szFA.BaseType);
                dataParamType = abiStructName + "* data";
                dataCastType = "(" + abiStructName + "*)";
            }
            writer.Write(callIndent);
            writer.WriteLine("[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"CopyToManaged\")]");
            writer.Write(callIndent);
            writer.Write("static extern void CopyToManaged_");
            writer.Write(localName);
            writer.Write("([UnsafeAccessorType(\"");
            writer.Write(ArrayElementEncoder.GetArrayMarshallerInteropPath(szFA.BaseType));
            writer.Write("\")] object _, uint length, ");
            writer.Write(dataParamType);
            writer.Write(", Span<");
            writer.Write(elementProjected);
            writer.WriteLine("> span);");
            writer.WriteLine($"{callIndent}CopyToManaged_{localName}(null, (uint)__{localName}_span.Length, {dataCastType}_{localName}, {callName});");
        }

        // After call: write back Out params to caller's 'out' var.
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.Out) { continue; }
            string callName = AbiTypeHelpers.GetParamName(p, paramNameOverride);
            string localName = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);
            AsmResolver.DotNet.Signatures.TypeSignature uOut = AbiTypeHelpers.StripByRefAndCustomModifiers(p.Type);

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
                writer.WriteLine("[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"ConvertToManaged\")]");
                writer.Write(callIndent);
                writer.Write("static extern ");
                writer.Write(projectedTypeName);
                writer.Write(" ConvertToManaged_");
                writer.Write(localName);
                writer.Write("([UnsafeAccessorType(\"");
                writer.Write(interopTypeName);
                writer.WriteLine("\")] object _, void* value);");
                writer.WriteLine($"{callIndent}{callName} = ConvertToManaged_{localName}(null, __{localName});");
                continue;
            }

            writer.Write($"{callIndent}{callName} = ");
            if (uOut.IsString())
            {
                writer.Write($"HStringMarshaller.ConvertToManaged(__{localName})");
            }
            else if (uOut.IsObject())
            {
                writer.Write($"WindowsRuntimeObjectMarshaller.ConvertToManaged(__{localName})");
            }
            else if (AbiTypeHelpers.IsRuntimeClassOrInterface(context.Cache, uOut))
            {
                writer.Write($"{AbiTypeHelpers.GetMarshallerFullName(writer, context, uOut)}.ConvertToManaged(__{localName})");
            }
            else if (uOut.IsSystemType())
            {
                writer.Write($"global::ABI.System.TypeMarshaller.ConvertToManaged(__{localName})");
            }
            else if (AbiTypeHelpers.IsComplexStruct(context.Cache, uOut))
            {
                writer.Write($"{AbiTypeHelpers.GetMarshallerFullName(writer, context, uOut)}.ConvertToManaged(__{localName})");
            }
            else if (AbiTypeHelpers.IsAnyStruct(context.Cache, uOut))
            {
                writer.Write($"__{localName}");
            }
            else if (uOut is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlibBool && corlibBool.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Boolean)
            {
                writer.Write($"__{localName}");
            }
            else if (uOut is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlibChar && corlibChar.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Char)
            {
                writer.Write($"__{localName}");
            }
            else if (AbiTypeHelpers.IsEnumType(context.Cache, uOut))
            {
                // Enum out param: __<name> local is already the projected enum type (since the
                // function pointer signature uses the projected type). No cast needed.
                writer.Write($"__{localName}");
            }
            else
            {
                writer.Write($"__{localName}");
            }
            writer.WriteLine(";");
        }

        // Writeback for ReceiveArray params: emit a UnsafeAccessor + assign to the out param.
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.ReceiveArray) { continue; }
            string callName = AbiTypeHelpers.GetParamName(p, paramNameOverride);
            string localName = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);
            AsmResolver.DotNet.Signatures.SzArrayTypeSignature sza = (AsmResolver.DotNet.Signatures.SzArrayTypeSignature)AbiTypeHelpers.StripByRefAndCustomModifiers(p.Type);
            IndentedTextWriter __scratchElementProjected = new();
            TypedefNameWriter.WriteProjectionType(__scratchElementProjected, context, TypeSemanticsFactory.Get(sza.BaseType));
            string elementProjected = __scratchElementProjected.ToString();
            // Element ABI type: void* for ref types (string/runtime class/object); ABI struct
            // type for complex structs (e.g. authored BasicStruct); blittable struct ABI for
            // blittable structs; primitive ABI otherwise.
            string elementAbi = sza.BaseType.IsString() || AbiTypeHelpers.IsRuntimeClassOrInterface(context.Cache, sza.BaseType) || sza.BaseType.IsObject()
                ? "void*"
                : AbiTypeHelpers.IsComplexStruct(context.Cache, sza.BaseType)
                    ? AbiTypeHelpers.GetAbiStructTypeName(writer, context, sza.BaseType)
                    : AbiTypeHelpers.IsAnyStruct(context.Cache, sza.BaseType)
                        ? AbiTypeHelpers.GetBlittableStructAbiType(writer, context, sza.BaseType)
                        : AbiTypeHelpers.GetAbiPrimitiveType(context.Cache, sza.BaseType);
            string elementInteropArg = InteropTypeNameWriter.EncodeInteropTypeName(sza.BaseType, TypedefNameType.Projected);

            _ = elementInteropArg;
            string marshallerPath = ArrayElementEncoder.GetArrayMarshallerInteropPath(sza.BaseType);
            writer.Write(callIndent);
            writer.WriteLine("[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"ConvertToManaged\")]");
            writer.Write(callIndent);
            writer.Write("static extern ");
            writer.Write(elementProjected);
            writer.Write("[] ConvertToManaged_");
            writer.Write(localName);
            writer.Write("([UnsafeAccessorType(\"");
            writer.Write(marshallerPath);
            writer.Write("\")] object _, uint length, ");
            writer.Write(elementAbi);
            writer.WriteLine("* data);");
            writer.WriteLine($"{callIndent}{callName} = ConvertToManaged_{localName}(null, __{localName}_length, __{localName}_data);");
        }
        if (rt is not null)
        {
            if (returnIsReceiveArray)
            {
                AsmResolver.DotNet.Signatures.SzArrayTypeSignature retSz = (AsmResolver.DotNet.Signatures.SzArrayTypeSignature)rt;
                IndentedTextWriter __scratchElementProjected = new();
                TypedefNameWriter.WriteProjectionType(__scratchElementProjected, context, TypeSemanticsFactory.Get(retSz.BaseType));
                string elementProjected = __scratchElementProjected.ToString();
                string elementAbi = retSz.BaseType.IsString() || AbiTypeHelpers.IsRuntimeClassOrInterface(context.Cache, retSz.BaseType) || retSz.BaseType.IsObject()
                    ? "void*"
                    : AbiTypeHelpers.IsComplexStruct(context.Cache, retSz.BaseType)
                        ? AbiTypeHelpers.GetAbiStructTypeName(writer, context, retSz.BaseType)
                        : retSz.BaseType.IsHResultException()
                            ? "global::ABI.System.Exception"
                            : AbiTypeHelpers.IsMappedAbiValueType(retSz.BaseType)
                                ? AbiTypeHelpers.GetMappedAbiTypeName(retSz.BaseType)
                                : AbiTypeHelpers.IsAnyStruct(context.Cache, retSz.BaseType)
                                    ? AbiTypeHelpers.GetBlittableStructAbiType(writer, context, retSz.BaseType)
                                    : AbiTypeHelpers.GetAbiPrimitiveType(context.Cache, retSz.BaseType);
                string elementInteropArg = InteropTypeNameWriter.EncodeInteropTypeName(retSz.BaseType, TypedefNameType.Projected);

                _ = elementInteropArg;
                writer.Write(callIndent);
                writer.WriteLine("[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"ConvertToManaged\")]");
                writer.Write(callIndent);
                writer.Write("static extern ");
                writer.Write(elementProjected);
                writer.Write("[] ConvertToManaged_retval([UnsafeAccessorType(\"");
                writer.Write(ArrayElementEncoder.GetArrayMarshallerInteropPath(retSz.BaseType));
                writer.Write("\")] object _, uint length, ");
                writer.Write(elementAbi);
                writer.WriteLine("* data);");
                writer.WriteLine($"{callIndent}return ConvertToManaged_retval(null, __retval_length, __retval_data);");
            }
            else if (returnIsHResultException)
            {
                writer.WriteLine($"{callIndent}return global::ABI.System.ExceptionMarshaller.ConvertToManaged(__retval);");
            }
            else if (returnIsString)
            {
                writer.WriteLine($"{callIndent}return HStringMarshaller.ConvertToManaged(__retval);");
            }
            else if (returnIsRefType)
            {
                if (rt.IsNullableT())
                {
                    // Nullable<T> return: use <T>Marshaller.UnboxToManaged. Mirrors truth pattern;
                    // there is no Nullable<T>Marshaller, the inner-T marshaller has UnboxToManaged.
                    AsmResolver.DotNet.Signatures.TypeSignature inner = rt.GetNullableInnerType()!;
                    string innerMarshaller = AbiTypeHelpers.GetNullableInnerMarshallerName(writer, context, inner);
                    writer.WriteLine($"{callIndent}return {innerMarshaller}.UnboxToManaged(__retval);");
                }
                else if (rt.IsGenericInstance())
                {
                    string interopTypeName = InteropTypeNameWriter.EncodeInteropTypeName(rt, TypedefNameType.ABI) + ", WinRT.Interop";
                    IndentedTextWriter __scratchProjectedTypeName = new();
                    MethodFactory.WriteProjectedSignature(__scratchProjectedTypeName, context, rt, false);
                    string projectedTypeName = __scratchProjectedTypeName.ToString();
                    writer.Write(callIndent);
                    writer.WriteLine("[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"ConvertToManaged\")]");
                    writer.Write(callIndent);
                    writer.Write("static extern ");
                    writer.Write(projectedTypeName);
                    writer.Write(" ConvertToManaged_retval([UnsafeAccessorType(\"");
                    writer.Write(interopTypeName);
                    writer.WriteLine("\")] object _, void* value);");
                    writer.WriteLine($"{callIndent}return ConvertToManaged_retval(null, __retval);");
                }
                else
                {
                    writer.Write($"{callIndent}return ");
                    EmitMarshallerConvertToManaged(writer, context, rt, "__retval");
                    writer.WriteLine(";");
                }
            }
            else if (rt is not null && AbiTypeHelpers.IsMappedAbiValueType(rt))
            {
                // Mapped value type return (e.g. DateTime/TimeSpan): convert ABI struct back via marshaller.
                writer.WriteLine($"{callIndent}return {AbiTypeHelpers.GetMappedMarshallerName(rt)}.ConvertToManaged(__retval);");
            }
            else if (rt is not null && rt.IsSystemType())
            {
                // System.Type return: convert ABI Type struct back to System.Type via TypeMarshaller.
                writer.WriteLine($"{callIndent}return global::ABI.System.TypeMarshaller.ConvertToManaged(__retval);");
            }
            else if (returnIsAnyStruct)
            {
                writer.Write(callIndent);
                if (rt is not null && AbiTypeHelpers.IsMappedAbiValueType(rt))
                {
                    // Mapped value type return: convert ABI struct back to projected via marshaller.
                    writer.WriteLine($"return {AbiTypeHelpers.GetMappedMarshallerName(rt)}.ConvertToManaged(__retval);");
                }
                else
                {
                    writer.WriteLine("return __retval;");
                }
            }
            else if (returnIsComplexStruct)
            {
                writer.WriteLine($"{callIndent}return {AbiTypeHelpers.GetMarshallerFullName(writer, context, rt!)}.ConvertToManaged(__retval);");
            }
            else
            {
                writer.Write($"{callIndent}return ");
                IndentedTextWriter __scratchProjected = new();
                MethodFactory.WriteProjectedSignature(__scratchProjected, context, rt!, false);
                string projected = __scratchProjected.ToString();
                string abiType = AbiTypeHelpers.GetAbiPrimitiveType(context.Cache, rt!);
                if (projected == abiType) { writer.WriteLine("__retval;"); }
                else
                {
                    writer.WriteLine($"({projected})__retval;");
                }
            }
        }

        // Close fixed blocks (innermost first).
        for (int i = fixedNesting - 1; i >= 0; i--)
        {
            writer.WriteLine($"{indent}{new string(' ', i * 4)}}}");
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
                AsmResolver.DotNet.Signatures.TypeSignature pType = AbiTypeHelpers.StripByRefAndCustomModifiers(p.Type);
                if (!AbiTypeHelpers.IsComplexStruct(context.Cache, pType)) { continue; }
                string localName = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);
                writer.WriteLine($"            {AbiTypeHelpers.GetMarshallerFullName(writer, context, pType)}.Dispose(__{localName});");
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
                if (AbiTypeHelpers.IsBlittablePrimitive(context.Cache, szArr.BaseType) || AbiTypeHelpers.IsAnyStruct(context.Cache, szArr.BaseType)) { continue; }
                if (AbiTypeHelpers.IsMappedAbiValueType(szArr.BaseType)) { continue; }
                if (szArr.BaseType.IsHResultException())
                {
                    // HResultException ABI is just an int; per-element Dispose is a no-op (mirror
                    // the truth: no Dispose_<name> emitted). Just return the inline-array's pool
                    // using the correct element type (ABI.System.Exception, not nint).
                    string localNameH = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);
                    writer.Write($"\n            if (__{localNameH}_arrayFromPool is not null)\n            {{\n                global::System.Buffers.ArrayPool<global::ABI.System.Exception>.Shared.Return(__{localNameH}_arrayFromPool);\n            }}\n");
                    continue;
                }
                string localName = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);
                if (szArr.BaseType.IsString())
                {
                    // The HStringArrayMarshaller.Dispose + ArrayPool returns for strings only
                    // apply to PassArray (where we set up the pinned handles + headers in the
                    // first place). FillArray writes back HSTRING handles into the nint storage
                    // array directly, with no per-element pinned handle / header to release.
                    if (cat == ParamCategory.PassArray)
                    {
                        writer.Write($"            HStringArrayMarshaller.Dispose(__{localName}_pinnedHandleSpan);\n\n            if (__{localName}_pinnedHandleArrayFromPool is not null)\n            {{\n                global::System.Buffers.ArrayPool<nint>.Shared.Return(__{localName}_pinnedHandleArrayFromPool);\n            }}\n\n            if (__{localName}_headerArrayFromPool is not null)\n            {{\n                global::System.Buffers.ArrayPool<HStringHeader>.Shared.Return(__{localName}_headerArrayFromPool);\n            }}\n");
                    }
                    // Both PassArray and FillArray need the inline-array's nint pool returned.
                    writer.Write($"\n            if (__{localName}_arrayFromPool is not null)\n            {{\n                global::System.Buffers.ArrayPool<nint>.Shared.Return(__{localName}_arrayFromPool);\n            }}\n");
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
                    if (AbiTypeHelpers.IsComplexStruct(context.Cache, szArr.BaseType))
                    {
                        string abiStructName = AbiTypeHelpers.GetAbiStructTypeName(writer, context, szArr.BaseType);
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
                    writer.WriteLine("            [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"Dispose\")]");
                    writer.Write($"            static extern void Dispose_{localName}([UnsafeAccessorType(\"{ArrayElementEncoder.GetArrayMarshallerInteropPath(szArr.BaseType)}\")] object _, uint length, {disposeDataParamType}");
                    if (!disposeDataParamType.EndsWith("data", System.StringComparison.Ordinal)) { writer.Write(" data"); }
                    writer.Write($");\n\n            fixed({fixedPtrType} _{localName} = __{localName}_span)\n            {{\n                Dispose_{localName}(null, (uint) __{localName}_span.Length, {disposeCastType}_{localName});\n            }}\n");
                }
                // ArrayPool storage type matches the InlineArray storage (mapped ABI value type
                // for DateTime/TimeSpan; ABI struct for complex structs; nint otherwise).
                string poolStorageT = AbiTypeHelpers.IsMappedAbiValueType(szArr.BaseType)
                    ? AbiTypeHelpers.GetMappedAbiTypeName(szArr.BaseType)
                    : AbiTypeHelpers.IsComplexStruct(context.Cache, szArr.BaseType)
                        ? AbiTypeHelpers.GetAbiStructTypeName(writer, context, szArr.BaseType)
                        : "nint";
                writer.Write($"\n            if (__{localName}_arrayFromPool is not null)\n            {{\n                global::System.Buffers.ArrayPool<{poolStorageT}>.Shared.Return(__{localName}_arrayFromPool);\n            }}\n");
            }

            // 2. Free Out string/object/runtime-class params.
            for (int i = 0; i < sig.Params.Count; i++)
            {
                ParamInfo p = sig.Params[i];
                ParamCategory cat = ParamHelpers.GetParamCategory(p);
                if (cat != ParamCategory.Out) { continue; }
                AsmResolver.DotNet.Signatures.TypeSignature uOut = AbiTypeHelpers.StripByRefAndCustomModifiers(p.Type);
                string localName = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);
                if (uOut.IsString())
                {
                    writer.WriteLine($"            HStringMarshaller.Free(__{localName});");
                }
                else if (uOut.IsObject() || AbiTypeHelpers.IsRuntimeClassOrInterface(context.Cache, uOut) || uOut.IsGenericInstance())
                {
                    writer.WriteLine($"            WindowsRuntimeUnknownMarshaller.Free(__{localName});");
                }
                else if (uOut.IsSystemType())
                {
                    writer.WriteLine($"            global::ABI.System.TypeMarshaller.Dispose(__{localName});");
                }
                else if (AbiTypeHelpers.IsComplexStruct(context.Cache, uOut))
                {
                    writer.WriteLine($"            {AbiTypeHelpers.GetMarshallerFullName(writer, context, uOut)}.Dispose(__{localName});");
                }
            }

            // 3. Free ReceiveArray params via UnsafeAccessor.
            for (int i = 0; i < sig.Params.Count; i++)
            {
                ParamInfo p = sig.Params[i];
                ParamCategory cat = ParamHelpers.GetParamCategory(p);
                if (cat != ParamCategory.ReceiveArray) { continue; }
                string localName = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);
                AsmResolver.DotNet.Signatures.SzArrayTypeSignature sza = (AsmResolver.DotNet.Signatures.SzArrayTypeSignature)AbiTypeHelpers.StripByRefAndCustomModifiers(p.Type);
                // Element ABI type: void* for ref types; ABI struct for complex/blittable structs;
                // primitive ABI otherwise. (Same categorization as the ConvertToManaged_<name> path.)
                string elementAbi = sza.BaseType.IsString() || AbiTypeHelpers.IsRuntimeClassOrInterface(context.Cache, sza.BaseType) || sza.BaseType.IsObject()
                    ? "void*"
                    : AbiTypeHelpers.IsComplexStruct(context.Cache, sza.BaseType)
                        ? AbiTypeHelpers.GetAbiStructTypeName(writer, context, sza.BaseType)
                        : AbiTypeHelpers.IsAnyStruct(context.Cache, sza.BaseType)
                            ? AbiTypeHelpers.GetBlittableStructAbiType(writer, context, sza.BaseType)
                            : AbiTypeHelpers.GetAbiPrimitiveType(context.Cache, sza.BaseType);
                string elementInteropArg = InteropTypeNameWriter.EncodeInteropTypeName(sza.BaseType, TypedefNameType.Projected);

                _ = elementInteropArg;
                string marshallerPath = ArrayElementEncoder.GetArrayMarshallerInteropPath(sza.BaseType);
                writer.WriteLine("            [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"Free\")]");
                writer.WriteLine($"            static extern void Free_{localName}([UnsafeAccessorType(\"{marshallerPath}\")] object _, uint length, {elementAbi}* data);\n\n            Free_{localName}(null, __{localName}_length, __{localName}_data);");
            }

            // 4. Free return value (__retval) — emitted last to match truth ordering.
            if (returnIsString)
            {
                writer.WriteLine("            HStringMarshaller.Free(__retval);");
            }
            else if (returnIsRefType)
            {
                writer.WriteLine("            WindowsRuntimeUnknownMarshaller.Free(__retval);");
            }
            else if (returnIsComplexStruct)
            {
                writer.WriteLine($"            {AbiTypeHelpers.GetMarshallerFullName(writer, context, rt!)}.Dispose(__retval);");
            }
            else if (returnIsSystemTypeForCleanup)
            {
                // System.Type return: dispose the ABI.System.Type's HSTRING fields.
                writer.WriteLine("            global::ABI.System.TypeMarshaller.Dispose(__retval);");
            }
            else if (returnIsReceiveArray)
            {
                AsmResolver.DotNet.Signatures.SzArrayTypeSignature retSz = (AsmResolver.DotNet.Signatures.SzArrayTypeSignature)rt!;
                string elementAbi = retSz.BaseType.IsString() || AbiTypeHelpers.IsRuntimeClassOrInterface(context.Cache, retSz.BaseType) || retSz.BaseType.IsObject()
                    ? "void*"
                    : AbiTypeHelpers.IsComplexStruct(context.Cache, retSz.BaseType)
                        ? AbiTypeHelpers.GetAbiStructTypeName(writer, context, retSz.BaseType)
                        : retSz.BaseType.IsHResultException()
                            ? "global::ABI.System.Exception"
                            : AbiTypeHelpers.IsMappedAbiValueType(retSz.BaseType)
                                ? AbiTypeHelpers.GetMappedAbiTypeName(retSz.BaseType)
                                : AbiTypeHelpers.IsAnyStruct(context.Cache, retSz.BaseType)
                                    ? AbiTypeHelpers.GetBlittableStructAbiType(writer, context, retSz.BaseType)
                                    : AbiTypeHelpers.GetAbiPrimitiveType(context.Cache, retSz.BaseType);
                string elementInteropArg = InteropTypeNameWriter.EncodeInteropTypeName(retSz.BaseType, TypedefNameType.Projected);

                _ = elementInteropArg;
                writer.WriteLine("            [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"Free\")]");
                writer.Write("            static extern void Free_retval([UnsafeAccessorType(\"");
                writer.Write(ArrayElementEncoder.GetArrayMarshallerInteropPath(retSz.BaseType));
                writer.Write("\")] object _, uint length, ");
                writer.Write(elementAbi);
                writer.WriteLine("* data);");
                writer.WriteLine("            Free_retval(null, __retval_length, __retval_data);");
            }

            writer.WriteLine("        }");
        }

        writer.WriteLine("    }");
    }

    /// <summary>Emits the call to the appropriate marshaller's ConvertToUnmanaged for a runtime class / object input parameter.</summary>
    internal static void EmitMarshallerConvertToUnmanaged(IndentedTextWriter writer, ProjectionEmitContext context, AsmResolver.DotNet.Signatures.TypeSignature sig, string argName)
    {
        if (sig.IsObject())
        {
            writer.Write($"WindowsRuntimeObjectMarshaller.ConvertToUnmanaged({argName})");
            return;
        }
        // Runtime class / interface: use ABI.<NS>.<Name>Marshaller
        writer.Write($"{AbiTypeHelpers.GetMarshallerFullName(writer, context, sig)}.ConvertToUnmanaged({argName})");
    }

    /// <summary>Emits the call to the appropriate marshaller's ConvertToManaged for a runtime class / object return value.</summary>
    internal static void EmitMarshallerConvertToManaged(IndentedTextWriter writer, ProjectionEmitContext context, AsmResolver.DotNet.Signatures.TypeSignature sig, string argName)
    {
        if (sig.IsObject())
        {
            writer.Write($"WindowsRuntimeObjectMarshaller.ConvertToManaged({argName})");
            return;
        }
        writer.Write($"{AbiTypeHelpers.GetMarshallerFullName(writer, context, sig)}.ConvertToManaged({argName})");
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
        else if (AbiTypeHelpers.IsEnumType(context.Cache, p.Type))
        {
            writer.Write(pname);
        }
        else
        {
            writer.Write(pname);
        }
    }
}
