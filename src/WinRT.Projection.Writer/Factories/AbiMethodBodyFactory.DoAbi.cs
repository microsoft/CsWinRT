// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using WindowsRuntime.ProjectionWriter.Extensions;
using WindowsRuntime.ProjectionWriter.Models;
using WindowsRuntime.ProjectionWriter.Writers;
using WindowsRuntime.ProjectionWriter.Helpers;
using WindowsRuntime.ProjectionWriter.Metadata;

namespace WindowsRuntime.ProjectionWriter.Factories;

internal static partial class AbiMethodBodyFactory
{
    /// <summary>
    /// Emits a real Do_Abi (CCW) body for the cases we can handle. This is a partial
    /// implementation that uses simple per-marshaller patterns inline rather than the
    /// fully-general <c>abi_marshaler</c> abstraction.
    /// </summary>
    internal static void EmitDoAbiBodyIfSimple(IndentedTextWriter writer, ProjectionEmitContext context, MethodSignatureInfo sig, string ifaceFullName, string methodName)
    {
        AsmResolver.DotNet.Signatures.TypeSignature? rt = sig.ReturnType;

        // String params drive whether we need HString header allocation in the body.
        bool hasStringParams = false;
        foreach (ParameterInfo p in sig.Params)
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

        writer.WriteLine("");
        using (writer.WriteBlock())
        {
            string retParamName = AbiTypeHelpers.GetReturnParamName(sig);
            string retSizeParamName = AbiTypeHelpers.GetReturnSizeParamName(sig);
            // The local name for the unmarshalled return value uses the standard pattern
            // of prefixing '__' to the param name. For the default '__return_value__' param
            // this becomes '____return_value__'.
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
                writer.Write($$"""
                [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "ConvertToUnmanaged")]
                    static extern WindowsRuntimeObjectReferenceValue ConvertToUnmanaged_{{retParamName}}([UnsafeAccessorType("{{interopTypeName}}")] object _, {{projectedTypeName}} value);
                """, isMultiline: true);
                writer.WriteLine("");
            }

            // Hoist [UnsafeAccessor] declarations for Out generic-instance params:
            // ConvertToUnmanaged_<name> wraps the projected value into a WindowsRuntimeObjectReferenceValue.
            // The body's writeback later references these already-declared accessors.
            for (int i = 0; i < sig.Params.Count; i++)
            {
                ParameterInfo p = sig.Params[i];
                ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);
                if (cat != ParameterCategory.Out) { continue; }
                AsmResolver.DotNet.Signatures.TypeSignature uOut = AbiTypeHelpers.StripByRefAndCustomModifiers(p.Type);
                if (!uOut.IsGenericInstance()) { continue; }
                string raw = p.Parameter.Name ?? "param";
                string interopTypeName = InteropTypeNameWriter.EncodeInteropTypeName(uOut, TypedefNameType.ABI) + ", WinRT.Interop";
                IndentedTextWriter __scratchProjectedTypeName = new();
                MethodFactory.WriteProjectedSignature(__scratchProjectedTypeName, context, uOut, false);
                string projectedTypeName = __scratchProjectedTypeName.ToString();
                writer.Write($$"""
                [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "ConvertToUnmanaged")]
                    static extern WindowsRuntimeObjectReferenceValue ConvertToUnmanaged_{{raw}}([UnsafeAccessorType("{{interopTypeName}}")] object _, {{projectedTypeName}} value);
                """, isMultiline: true);
                writer.WriteLine("");
            }
            // ConvertToUnmanaged_<param> and the return-array ConvertToUnmanaged_<retParam> to the
            // top of the method body, before locals and the try block. The actual call sites later
            // in the body reference these already-declared accessors.
            for (int i = 0; i < sig.Params.Count; i++)
            {
                ParameterInfo p = sig.Params[i];
                ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);
                if (cat != ParameterCategory.ReceiveArray) { continue; }
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
                writer.Write($$"""
                [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "ConvertToUnmanaged")]
                    static extern void ConvertToUnmanaged_{{raw}}([UnsafeAccessorType("{{marshallerPath}}")] object _, ReadOnlySpan<{{elementProjected}}> span, out uint length, out {{elementAbi}}* data);
                """, isMultiline: true);
                writer.WriteLine("");
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
                writer.Write($$"""
                [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "ConvertToUnmanaged")]
                    static extern void ConvertToUnmanaged_{{retParamName}}([UnsafeAccessorType("{{marshallerPath}}")] object _, ReadOnlySpan<{{elementProjected}}> span, out uint length, out {{elementAbi}}* data);
                """, isMultiline: true);
                writer.WriteLine("");
            }
            // the OUT pointer(s). The actual assignment happens inside the try block.
            if (rt is not null)
            {
                if (returnIsString)
                {
                    writer.WriteLine($"string {retLocalName} = default;");
                }
                else if (returnIsRefType)
                {
                    IndentedTextWriter __scratchProjected = new();
                    MethodFactory.WriteProjectedSignature(__scratchProjected, context, rt, false);
                    string projected = __scratchProjected.ToString();
                    writer.WriteLine($"{projected} {retLocalName} = default;");
                }
                else if (returnIsReceiveArrayDoAbi)
                {
                    IndentedTextWriter __scratchProjected = new();
                    MethodFactory.WriteProjectedSignature(__scratchProjected, context, rt, false);
                    string projected = __scratchProjected.ToString();
                    writer.WriteLine($"{projected} {retLocalName} = default;");
                }
                else
                {
                    IndentedTextWriter __scratchProjected = new();
                    MethodFactory.WriteProjectedSignature(__scratchProjected, context, rt, false);
                    string projected = __scratchProjected.ToString();
                    writer.WriteLine($"{projected} {retLocalName} = default;");
                }
            }

            if (rt is not null)
            {
                if (returnIsReceiveArrayDoAbi)
                {
                    writer.Write($$"""
                    *{{retParamName}} = default;
                        *{{retSizeParamName}} = default;
                    """, isMultiline: true);
                }
                else
                {
                    writer.WriteLine($"*{retParamName} = default;");
                }
            }
            // For each out parameter, clear the destination and declare a local.
            // NOTE: Ref params (WinRT 'in T' / 'ref const T') are READ-ONLY inputs from the caller's
            // perspective. Do NOT zero *<name> (it's the input value) and do NOT declare a local
            // (we read directly via *<name>).
            for (int i = 0; i < sig.Params.Count; i++)
            {
                ParameterInfo p = sig.Params[i];
                ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);
                if (cat != ParameterCategory.Out) { continue; }
                string raw = p.Parameter.Name ?? "param";
                string ptr = CSharpKeywords.IsKeyword(raw) ? "@" + raw : raw;
                writer.WriteLine($"*{ptr} = default;");
            }
            for (int i = 0; i < sig.Params.Count; i++)
            {
                ParameterInfo p = sig.Params[i];
                ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);
                if (cat != ParameterCategory.Out) { continue; }
                string raw = p.Parameter.Name ?? "param";
                // Use the projected (non-ABI) type for the local variable.
                // Strip ByRef and CustomModifier wrappers to get the underlying base type.
                AsmResolver.DotNet.Signatures.TypeSignature underlying = AbiTypeHelpers.StripByRefAndCustomModifiers(p.Type);
                IndentedTextWriter __scratchProjected = new();
                MethodFactory.WriteProjectedSignature(__scratchProjected, context, underlying, false);
                string projected = __scratchProjected.ToString();
                writer.WriteLine($"{projected} __{raw} = default;");
            }
            // For each ReceiveArray parameter (out T[]), zero the destination + size out pointers
            // and declare a managed array local. The managed call passes 'out __<name>' and after
            // the call we copy to the ABI buffer via UnsafeAccessor.
            for (int i = 0; i < sig.Params.Count; i++)
            {
                ParameterInfo p = sig.Params[i];
                ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);
                if (cat != ParameterCategory.ReceiveArray) { continue; }
                string raw = p.Parameter.Name ?? "param";
                string ptr = CSharpKeywords.IsKeyword(raw) ? "@" + raw : raw;
                AsmResolver.DotNet.Signatures.SzArrayTypeSignature sza = (AsmResolver.DotNet.Signatures.SzArrayTypeSignature)AbiTypeHelpers.StripByRefAndCustomModifiers(p.Type);
                IndentedTextWriter __scratchElementProjected = new();
                TypedefNameWriter.WriteProjectionType(__scratchElementProjected, context, TypeSemanticsFactory.Get(sza.BaseType));
                string elementProjected = __scratchElementProjected.ToString();
                writer.Write($$"""
                *{{ptr}} = default;
                    *__{{raw}}Size = default;
                    {{elementProjected}}[] __{{raw}} = default;
                """, isMultiline: true);
            }
            // For each blittable array (PassArray / FillArray) parameter, declare a Span<T> local that
            // wraps the (length, pointer) pair from the ABI signature.
            // For non-blittable element types (string/runtime class/object), declare InlineArray16<T> +
            // ArrayPool fallback then CopyToManaged via UnsafeAccessor.
            for (int i = 0; i < sig.Params.Count; i++)
            {
                ParameterInfo p = sig.Params[i];
                ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);
                if (cat is not (ParameterCategory.PassArray or ParameterCategory.FillArray)) { continue; }
                if (p.Type is not AsmResolver.DotNet.Signatures.SzArrayTypeSignature sz) { continue; }
                string raw = p.Parameter.Name ?? "param";
                string ptr = CSharpKeywords.IsKeyword(raw) ? "@" + raw : raw;
                IndentedTextWriter __scratchElementProjected = new();
                TypedefNameWriter.WriteProjectionType(__scratchElementProjected, context, TypeSemanticsFactory.Get(sz.BaseType));
                string elementProjected = __scratchElementProjected.ToString();
                bool isBlittableElem = AbiTypeHelpers.IsBlittablePrimitive(context.Cache, sz.BaseType) || AbiTypeHelpers.IsAnyStruct(context.Cache, sz.BaseType);
                if (isBlittableElem)
                {
                    writer.WriteLine($"{(cat == ParameterCategory.PassArray ? "ReadOnlySpan<" : "Span<")}{elementProjected}> __{raw} = new({ptr}, (int)__{raw}Size);");
                }
                else
                {
                    // Non-blittable element: InlineArray16<T> + ArrayPool<T> with size from ABI.
                    writer.WriteLine("");
                    writer.Write($$"""
                    Unsafe.SkipInit(out InlineArray16<{{elementProjected}}> __{{raw}}_inlineArray);
                        {{elementProjected}}[] __{{raw}}_arrayFromPool = null;
                        Span<{{elementProjected}}> __{{raw}} = __{{raw}}Size <= 16
                            ? __{{raw}}_inlineArray[..(int)__{{raw}}Size]
                            : (__{{raw}}_arrayFromPool = global::System.Buffers.ArrayPool<{{elementProjected}}>.Shared.Rent((int)__{{raw}}Size));
                    """, isMultiline: true);
                }
            }
            writer.Write("""
                try
                {
                """, isMultiline: true);

            // For non-blittable PassArray params (read-only input arrays), emit CopyToManaged_<name>
            // via UnsafeAccessor to convert the native ABI buffer into the managed Span<T> the
            // delegate sees. For FillArray params, the buffer is fresh storage the user delegate
            // fills — the post-call writeback loop handles that.
            for (int i = 0; i < sig.Params.Count; i++)
            {
                ParameterInfo p = sig.Params[i];
                ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);
                if (cat != ParameterCategory.PassArray) { continue; }
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
                writer.Write($$"""
                    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "CopyToManaged")]
                        static extern void CopyToManaged_{{raw}}([UnsafeAccessorType("{{ArrayElementEncoder.GetArrayMarshallerInteropPath(szArr.BaseType)}}")] object _, uint length, {{dataParamType}}, Span<{{elementProjected}}> span);
                        CopyToManaged_{{raw}}(null, __{{raw}}Size, {{dataCastExpr}}, __{{raw}});
                """, isMultiline: true);
            }

            // For generic instance ABI input parameters, emit local UnsafeAccessor delegates and locals
            // first so the call site can reference them.
            for (int i = 0; i < sig.Params.Count; i++)
            {
                ParameterInfo p = sig.Params[i];
                if (p.Type.IsNullableT())
                {
                    // Nullable<T> param (server-side): use <T>Marshaller.UnboxToManaged.
                    string rawName = p.Parameter.Name ?? "param";
                    string callName = CSharpKeywords.IsKeyword(rawName) ? "@" + rawName : rawName;
                    AsmResolver.DotNet.Signatures.TypeSignature inner = p.Type.GetNullableInnerType()!;
                    string innerMarshaller = AbiTypeHelpers.GetNullableInnerMarshallerName(writer, context, inner);
                    writer.WriteLine($"    var __arg_{rawName} = {innerMarshaller}.UnboxToManaged({callName});");
                }
                else if (p.Type.IsGenericInstance())
                {
                    string rawName = p.Parameter.Name ?? "param";
                    string callName = CSharpKeywords.IsKeyword(rawName) ? "@" + rawName : rawName;
                    string interopTypeName = InteropTypeNameWriter.EncodeInteropTypeName(p.Type, TypedefNameType.ABI) + ", WinRT.Interop";
                    IndentedTextWriter __scratchProjectedTypeName = new();
                    MethodFactory.WriteProjectedSignature(__scratchProjectedTypeName, context, p.Type, false);
                    string projectedTypeName = __scratchProjectedTypeName.ToString();
                    writer.Write($$"""
                        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "ConvertToManaged")]
                            static extern {{projectedTypeName}} ConvertToManaged_arg_{{rawName}}([UnsafeAccessorType("{{interopTypeName}}")] object _, void* value);
                            var __arg_{{rawName}} = ConvertToManaged_arg_{{rawName}}(null, {{callName}});
                    """, isMultiline: true);
                }
            }

            if (returnIsString)
            {
                writer.Write($"    {retLocalName} = ");
            }
            else if (returnIsRefType)
            {
                writer.Write($"    {retLocalName} = ");
            }
            else if (returnIsReceiveArrayDoAbi)
            {
                // For T[] return: assign to existing local.
                writer.Write($"    {retLocalName} = ");
            }
            else if (rt is not null)
            {
                writer.Write($"    {retLocalName} = ");
            }
            else
            {
                writer.Write("    ");
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
                    if (i > 0)
                    {
                        writer.Write("""
                        ,
                          
                        """, isMultiline: true);
                    }
                    ParameterInfo p = sig.Params[i];
                    ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);
                    if (cat == ParameterCategory.Out)
                    {
                        string raw = p.Parameter.Name ?? "param";
                        writer.Write($"out __{raw}");
                    }
                    else if (cat == ParameterCategory.Ref)
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
                    else if (cat is ParameterCategory.PassArray or ParameterCategory.FillArray)
                    {
                        string raw = p.Parameter.Name ?? "param";
                        writer.Write($"__{raw}");
                    }
                    else if (cat == ParameterCategory.ReceiveArray)
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
                ParameterInfo p = sig.Params[i];
                ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);
                if (cat != ParameterCategory.Out) { continue; }
                string raw = p.Parameter.Name ?? "param";
                string ptr = CSharpKeywords.IsKeyword(raw) ? "@" + raw : raw;
                AsmResolver.DotNet.Signatures.TypeSignature underlying = AbiTypeHelpers.StripByRefAndCustomModifiers(p.Type);
                writer.Write($"    *{ptr} = ");
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
                // writing it into the *out ABI struct slot.write_marshal_from_managed
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
                ParameterInfo p = sig.Params[i];
                ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);
                if (cat != ParameterCategory.ReceiveArray) { continue; }
                string raw = p.Parameter.Name ?? "param";
                string ptr = CSharpKeywords.IsKeyword(raw) ? "@" + raw : raw;
                writer.WriteLine($"    ConvertToUnmanaged_{raw}(null, __{raw}, out *__{raw}Size, out *{ptr});");
            }
            // After call: for non-blittable FillArray params (Span<T> where T is string/runtime
            // class/object/non-blittable struct), copy the managed delegate's writes back into the
            // native ABI buffer..
            // which emits 'CopyToUnmanaged_<name>(null, __<name>, __<name>Size, (T*)<name>)'.
            // Blittable element types don't need this — the Span wraps the native buffer directly.
            for (int i = 0; i < sig.Params.Count; i++)
            {
                ParameterInfo p = sig.Params[i];
                ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);
                if (cat != ParameterCategory.FillArray) { continue; }
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
                writer.Write($$"""
                    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "CopyToUnmanaged")]
                        static extern void CopyToUnmanaged_{{raw}}([UnsafeAccessorType("{{ArrayElementEncoder.GetArrayMarshallerInteropPath(szFA.BaseType)}}")] object _, ReadOnlySpan<{{elementProjected}}> span, uint length, {{dataParamType}});
                        CopyToUnmanaged_{{raw}}(null, __{{raw}}, __{{raw}}Size, {{dataCastType}}{{ptr}});
                """, isMultiline: true);
            }
            if (rt is not null)
            {
                if (returnIsHResultExceptionDoAbi)
                {
                    writer.WriteLine($"    *{retParamName} = global::ABI.System.ExceptionMarshaller.ConvertToUnmanaged({retLocalName});");
                }
                else if (returnIsString)
                {
                    writer.WriteLine($"    *{retParamName} = HStringMarshaller.ConvertToUnmanaged({retLocalName});");
                }
                else if (returnIsRefType)
                {
                    if (rt is not null && rt.IsNullableT())
                    {
                        // Nullable<T> return (server-side): use <T>Marshaller.BoxToUnmanaged.
                        AsmResolver.DotNet.Signatures.TypeSignature inner = rt.GetNullableInnerType()!;
                        string innerMarshaller = AbiTypeHelpers.GetNullableInnerMarshallerName(writer, context, inner);
                        writer.WriteLine($"    *{retParamName} = {innerMarshaller}.BoxToUnmanaged({retLocalName}).DetachThisPtrUnsafe();");
                    }
                    else if (returnIsGenericInstance)
                    {
                        // Generic instance return: use the UnsafeAccessor static local function declared at
                        // the top of the method body via the M12 hoisting pass; just emit the call here.
                        writer.WriteLine($"    *{retParamName} = ConvertToUnmanaged_{retParamName}(null, {retLocalName}).DetachThisPtrUnsafe();");
                    }
                    else
                    {
                        writer.Write($"    *{retParamName} = ");
                        EmitMarshallerConvertToUnmanaged(writer, context, rt!, retLocalName);
                        writer.WriteLine(".DetachThisPtrUnsafe();");
                    }
                }
                else if (returnIsReceiveArrayDoAbi)
                {
                    // Return-receive-array: emit ConvertToUnmanaged_<retParam> call (declaration
                    // was hoisted to the top of the method body).
                    writer.WriteLine($"    ConvertToUnmanaged_{retParamName}(null, {retLocalName}, out *{retSizeParamName}, out *{retParamName});");
                }
                else if (AbiTypeHelpers.IsMappedAbiValueType(rt))
                {
                    // Mapped value type return (DateTime/TimeSpan): convert via marshaller.
                    writer.WriteLine($"    *{retParamName} = {AbiTypeHelpers.GetMappedMarshallerName(rt)}.ConvertToUnmanaged({retLocalName});");
                }
                else if (rt.IsSystemType())
                {
                    // System.Type return (server-side): convert managed System.Type to ABI Type struct.
                    writer.WriteLine($"    *{retParamName} = global::ABI.System.TypeMarshaller.ConvertToUnmanaged({retLocalName});");
                }
                else if (AbiTypeHelpers.IsComplexStruct(context.Cache, rt))
                {
                    // Complex struct return (server-side): convert managed struct to ABI struct via marshaller.
                    writer.WriteLine($"    *{retParamName} = {AbiTypeHelpers.GetMarshallerFullName(writer, context, rt)}.ConvertToUnmanaged({retLocalName});");
                }
                else if (returnIsBlittableStruct)
                {
                    writer.WriteLine($"    *{retParamName} = {retLocalName};");
                }
                else
                {
                    writer.Write($"    *{retParamName} = ");
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
            writer.Write("""
                    return 0;
                }
                catch (Exception __exception__)
                {
                    return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(__exception__);
                }
                """, isMultiline: true);

            // For non-blittable PassArray params, emit finally block with ArrayPool<T>.Shared.Return.
            bool hasNonBlittableArrayDoAbi = false;
            for (int i = 0; i < sig.Params.Count; i++)
            {
                ParameterInfo p = sig.Params[i];
                ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);
                if (cat is not (ParameterCategory.PassArray or ParameterCategory.FillArray)) { continue; }
                if (p.Type is not AsmResolver.DotNet.Signatures.SzArrayTypeSignature szArr) { continue; }
                if (AbiTypeHelpers.IsBlittablePrimitive(context.Cache, szArr.BaseType) || AbiTypeHelpers.IsAnyStruct(context.Cache, szArr.BaseType)) { continue; }
                hasNonBlittableArrayDoAbi = true;
                break;
            }
            if (hasNonBlittableArrayDoAbi)
            {
                writer.Write("""
                    finally
                    {
                    """, isMultiline: true);
                for (int i = 0; i < sig.Params.Count; i++)
                {
                    ParameterInfo p = sig.Params[i];
                    ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);
                    if (cat is not (ParameterCategory.PassArray or ParameterCategory.FillArray)) { continue; }
                    if (p.Type is not AsmResolver.DotNet.Signatures.SzArrayTypeSignature szArr) { continue; }
                    if (AbiTypeHelpers.IsBlittablePrimitive(context.Cache, szArr.BaseType) || AbiTypeHelpers.IsAnyStruct(context.Cache, szArr.BaseType)) { continue; }
                    string raw = p.Parameter.Name ?? "param";
                    IndentedTextWriter __scratchElementProjected = new();
                    TypedefNameWriter.WriteProjectionType(__scratchElementProjected, context, TypeSemanticsFactory.Get(szArr.BaseType));
                    string elementProjected = __scratchElementProjected.ToString();
                    writer.WriteLine("");
                    writer.Write($$"""
                        if (__{{raw}}_arrayFromPool is not null)
                            {
                                global::System.Buffers.ArrayPool<{{elementProjected}}>.Shared.Return(__{{raw}}_arrayFromPool);
                            }
                    """, isMultiline: true);
                }
                writer.WriteLine("}");
            }
        }
        writer.WriteLine("");
        _ = hasStringParams;
    }

    /// <summary>Converts an ABI parameter to its projected (managed) form for the Do_Abi call.</summary>
    internal static void EmitDoAbiParamArgConversion(IndentedTextWriter writer, ProjectionEmitContext context, ParameterInfo p)
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
}
