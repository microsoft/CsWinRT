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
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0045:Convert to conditional expression",
        Justification = "if/else if chains over type-class predicates are more readable than nested ternaries.")]
    internal static void EmitAbiMethodBodyIfSimple(IndentedTextWriter writer, ProjectionEmitContext context, MethodSignatureInfo sig, int slot, string? paramNameOverride = null, bool isNoExcept = false)
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
        _ = fp.Append("void*");
        foreach (ParameterInfo p in sig.Params)
        {
            ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);
            if (cat is ParameterCategory.PassArray or ParameterCategory.FillArray)
            {
                _ = fp.Append(", uint, void*");
                continue;
            }
            if (cat == ParameterCategory.Out)
            {
                AsmResolver.DotNet.Signatures.TypeSignature uOut = AbiTypeHelpers.StripByRefAndCustomModifiers(p.Type);
                _ = fp.Append(", ");
                if (uOut.IsString() || AbiTypeHelpers.IsRuntimeClassOrInterface(context.Cache, uOut) || uOut.IsObject() || uOut.IsGenericInstance()) { _ = fp.Append("void**"); }
                else if (uOut.IsSystemType()) { _ = fp.Append("global::ABI.System.Type*"); }
                else if (AbiTypeHelpers.IsComplexStruct(context.Cache, uOut)) { _ = fp.Append(AbiTypeHelpers.GetAbiStructTypeName(writer, context, uOut)); _ = fp.Append('*'); }
                else if (AbiTypeHelpers.IsAnyStruct(context.Cache, uOut)) { _ = fp.Append(AbiTypeHelpers.GetBlittableStructAbiType(writer, context, uOut)); _ = fp.Append('*'); }
                else { _ = fp.Append(AbiTypeHelpers.GetAbiPrimitiveType(context.Cache, uOut)); _ = fp.Append('*'); }
                continue;
            }
            if (cat == ParameterCategory.Ref)
            {
                AsmResolver.DotNet.Signatures.TypeSignature uRef = AbiTypeHelpers.StripByRefAndCustomModifiers(p.Type);
                _ = fp.Append(", ");
                if (AbiTypeHelpers.IsComplexStruct(context.Cache, uRef)) { _ = fp.Append(AbiTypeHelpers.GetAbiStructTypeName(writer, context, uRef)); _ = fp.Append('*'); }
                else if (AbiTypeHelpers.IsAnyStruct(context.Cache, uRef)) { _ = fp.Append(AbiTypeHelpers.GetBlittableStructAbiType(writer, context, uRef)); _ = fp.Append('*'); }
                else { _ = fp.Append(AbiTypeHelpers.GetAbiPrimitiveType(context.Cache, uRef)); _ = fp.Append('*'); }
                continue;
            }
            if (cat == ParameterCategory.ReceiveArray)
            {
                AsmResolver.DotNet.Signatures.SzArrayTypeSignature sza = (AsmResolver.DotNet.Signatures.SzArrayTypeSignature)AbiTypeHelpers.StripByRefAndCustomModifiers(p.Type);
                _ = fp.Append(", uint*, ");
                if (sza.BaseType.IsString() || AbiTypeHelpers.IsRuntimeClassOrInterface(context.Cache, sza.BaseType) || sza.BaseType.IsObject())
                {
                    _ = fp.Append("void*");
                }
                else if (sza.BaseType.IsHResultException())
                {
                    _ = fp.Append("global::ABI.System.Exception");
                }
                else if (AbiTypeHelpers.IsMappedAbiValueType(sza.BaseType))
                {
                    _ = fp.Append(AbiTypeHelpers.GetMappedAbiTypeName(sza.BaseType));
                }
                else if (AbiTypeHelpers.IsComplexStruct(context.Cache, sza.BaseType)) { _ = fp.Append(AbiTypeHelpers.GetAbiStructTypeName(writer, context, sza.BaseType)); }
                else
                {
                    _ = fp.Append(AbiTypeHelpers.IsAnyStruct(context.Cache, sza.BaseType)
                        ? AbiTypeHelpers.GetBlittableStructAbiType(writer, context, sza.BaseType)
                        : AbiTypeHelpers.GetAbiPrimitiveType(context.Cache, sza.BaseType));
                }
                _ = fp.Append("**");
                continue;
            }
            _ = fp.Append(", ");
            if (p.Type.IsHResultException()) { _ = fp.Append("global::ABI.System.Exception"); }
            else if (p.Type.IsString() || AbiTypeHelpers.IsRuntimeClassOrInterface(context.Cache, p.Type) || p.Type.IsObject() || p.Type.IsGenericInstance()) { _ = fp.Append("void*"); }
            else if (p.Type.IsSystemType()) { _ = fp.Append("global::ABI.System.Type"); }
            else if (AbiTypeHelpers.IsAnyStruct(context.Cache, p.Type)) { _ = fp.Append(AbiTypeHelpers.GetBlittableStructAbiType(writer, context, p.Type)); }
            else if (AbiTypeHelpers.IsMappedAbiValueType(p.Type)) { _ = fp.Append(AbiTypeHelpers.GetMappedAbiTypeName(p.Type)); }
            else
            {
                _ = fp.Append(AbiTypeHelpers.IsComplexStruct(context.Cache, p.Type)
                    ? AbiTypeHelpers.GetAbiStructTypeName(writer, context, p.Type)
                    : AbiTypeHelpers.GetAbiPrimitiveType(context.Cache, p.Type));
            }
        }
        if (rt is not null)
        {
            if (returnIsReceiveArray)
            {
                AsmResolver.DotNet.Signatures.SzArrayTypeSignature retSz = (AsmResolver.DotNet.Signatures.SzArrayTypeSignature)rt;
                _ = fp.Append(", uint*, ");
                if (retSz.BaseType.IsString() || AbiTypeHelpers.IsRuntimeClassOrInterface(context.Cache, retSz.BaseType) || retSz.BaseType.IsObject())
                {
                    _ = fp.Append("void*");
                }
                else if (AbiTypeHelpers.IsComplexStruct(context.Cache, retSz.BaseType))
                {
                    _ = fp.Append(AbiTypeHelpers.GetAbiStructTypeName(writer, context, retSz.BaseType));
                }
                else if (retSz.BaseType.IsHResultException())
                {
                    _ = fp.Append("global::ABI.System.Exception");
                }
                else if (AbiTypeHelpers.IsMappedAbiValueType(retSz.BaseType))
                {
                    _ = fp.Append(AbiTypeHelpers.GetMappedAbiTypeName(retSz.BaseType));
                }
                else if (AbiTypeHelpers.IsAnyStruct(context.Cache, retSz.BaseType))
                {
                    _ = fp.Append(AbiTypeHelpers.GetBlittableStructAbiType(writer, context, retSz.BaseType));
                }
                else
                {
                    _ = fp.Append(AbiTypeHelpers.GetAbiPrimitiveType(context.Cache, retSz.BaseType));
                }
                _ = fp.Append("**");
            }
            else if (returnIsHResultException)
            {
                _ = fp.Append(", global::ABI.System.Exception*");
            }
            else
            {
                _ = fp.Append(", ");
                if (returnIsString || returnIsRefType) { _ = fp.Append("void**"); }
                else if (rt is not null && rt.IsSystemType()) { _ = fp.Append("global::ABI.System.Type*"); }
                else if (returnIsAnyStruct) { _ = fp.Append(AbiTypeHelpers.GetBlittableStructAbiType(writer, context, rt!)); _ = fp.Append('*'); }
                else if (returnIsComplexStruct) { _ = fp.Append(AbiTypeHelpers.GetAbiStructTypeName(writer, context, rt!)); _ = fp.Append('*'); }
                else if (rt is not null && AbiTypeHelpers.IsMappedAbiValueType(rt)) { _ = fp.Append(AbiTypeHelpers.GetMappedAbiTypeName(rt)); _ = fp.Append('*'); }
                else { _ = fp.Append(AbiTypeHelpers.GetAbiPrimitiveType(context.Cache, rt!)); _ = fp.Append('*'); }
            }
        }
        _ = fp.Append(", int");

        writer.WriteLine("");
        writer.Write("""
                {
                    using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();
                    void* ThisPtr = thisValue.GetThisPtrUnsafe();
            """, isMultiline: true);

        // Declare 'using' marshaller values for ref-type parameters (these need disposing).
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParameterInfo p = sig.Params[i];
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
                // Nullable<T> param: use <T>Marshaller.BoxToUnmanaged.
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
                writer.Write($$"""
                            [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "ConvertToUnmanaged")]
                            static extern WindowsRuntimeObjectReferenceValue ConvertToUnmanaged_{{localName}}([UnsafeAccessorType("{{interopTypeName}}")] object _, {{projectedTypeName}} value);
                            using WindowsRuntimeObjectReferenceValue __{{localName}} = ConvertToUnmanaged_{{localName}}(null, {{callName}});
                    """, isMultiline: true);
            }
        }
        // (String input params are now stack-allocated via the fast-path pinning pattern below;
        //  no separate void* local declaration or up-front allocation is needed.)
        // Declare locals for HResult/Exception input parameters (converted up-front).
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParameterInfo p = sig.Params[i];
            if (ParameterCategoryResolver.GetParamCategory(p) != ParameterCategory.In) { continue; }
            if (!p.Type.IsHResultException()) { continue; }
            string localName = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);
            string callName = AbiTypeHelpers.GetParamName(p, paramNameOverride);
            writer.WriteLine($"        global::ABI.System.Exception __{localName} = global::ABI.System.ExceptionMarshaller.ConvertToUnmanaged({callName});");
        }
        // Declare locals for mapped value-type input parameters (DateTime/TimeSpan): convert via marshaller up-front.
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParameterInfo p = sig.Params[i];
            if (ParameterCategoryResolver.GetParamCategory(p) != ParameterCategory.In) { continue; }
            if (!AbiTypeHelpers.IsMappedAbiValueType(p.Type)) { continue; }
            string localName = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);
            string callName = AbiTypeHelpers.GetParamName(p, paramNameOverride);
            writer.WriteLine($"        {AbiTypeHelpers.GetMappedAbiTypeName(p.Type)} __{localName} = {AbiTypeHelpers.GetMappedMarshallerName(p.Type)}.ConvertToUnmanaged({callName});");
        }
        // Declare locals for complex-struct input parameters (e.g. ProfileUsage with nested
        // string/Nullable fields): default-initialize OUTSIDE try, assign inside try via marshaller,
        // dispose in finally.
        // Includes both 'in' (ParameterCategory.In) and 'in T' (ParameterCategory.Ref) forms.
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParameterInfo p = sig.Params[i];
            ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);
            if (cat is not (ParameterCategory.In or ParameterCategory.Ref)) { continue; }
            AsmResolver.DotNet.Signatures.TypeSignature pType = AbiTypeHelpers.StripByRefAndCustomModifiers(p.Type);
            if (!AbiTypeHelpers.IsComplexStruct(context.Cache, pType)) { continue; }
            string localName = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);
            writer.WriteLine($"        {AbiTypeHelpers.GetAbiStructTypeName(writer, context, pType)} __{localName} = default;");
        }
        // Declare locals for Out parameters (need to be passed as &__<name> to the call).
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParameterInfo p = sig.Params[i];
            ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);
            if (cat != ParameterCategory.Out) { continue; }
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
            ParameterInfo p = sig.Params[i];
            ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);
            if (cat != ParameterCategory.ReceiveArray) { continue; }
            string localName = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);
            AsmResolver.DotNet.Signatures.SzArrayTypeSignature sza = (AsmResolver.DotNet.Signatures.SzArrayTypeSignature)AbiTypeHelpers.StripByRefAndCustomModifiers(p.Type);
            writer.Write($$"""
                        uint __{{localName}}_length = default;
                        
                """, isMultiline: true);
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
            ParameterInfo p = sig.Params[i];
            ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);
            if (cat is not (ParameterCategory.PassArray or ParameterCategory.FillArray)) { continue; }
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
            writer.WriteLine("");
            writer.Write($$"""
                        Unsafe.SkipInit(out InlineArray16<{{storageT}}> __{{localName}}_inlineArray);
                        {{storageT}}[] __{{localName}}_arrayFromPool = null;
                        Span<{{storageT}}> __{{localName}}_span = {{callName}}.Length <= 16
                            ? __{{localName}}_inlineArray[..{{callName}}.Length]
                            : (__{{localName}}_arrayFromPool = global::System.Buffers.ArrayPool<{{storageT}}>.Shared.Rent({{callName}}.Length));
                """, isMultiline: true);

            if (szArr.BaseType.IsString() && cat == ParameterCategory.PassArray)
            {
                // Strings need an additional InlineArray16<HStringHeader> + InlineArray16<nint> (pinned handles).
                // Only required for PassArray (managed -> HSTRING conversion); FillArray's native side
                // fills HSTRING handles directly into the nint storage.
                writer.WriteLine("");
                writer.Write($$"""
                            Unsafe.SkipInit(out InlineArray16<HStringHeader> __{{localName}}_inlineHeaderArray);
                            HStringHeader[] __{{localName}}_headerArrayFromPool = null;
                            Span<HStringHeader> __{{localName}}_headerSpan = {{callName}}.Length <= 16
                                ? __{{localName}}_inlineHeaderArray[..{{callName}}.Length]
                                : (__{{localName}}_headerArrayFromPool = global::System.Buffers.ArrayPool<HStringHeader>.Shared.Rent({{callName}}.Length));
                    
                            Unsafe.SkipInit(out InlineArray16<nint> __{{localName}}_inlinePinnedHandleArray);
                            nint[] __{{localName}}_pinnedHandleArrayFromPool = null;
                            Span<nint> __{{localName}}_pinnedHandleSpan = {{callName}}.Length <= 16
                                ? __{{localName}}_inlinePinnedHandleArray[..{{callName}}.Length]
                                : (__{{localName}}_pinnedHandleArrayFromPool = global::System.Buffers.ArrayPool<nint>.Shared.Rent({{callName}}.Length));
                    """, isMultiline: true);
            }
        }
        if (returnIsReceiveArray)
        {
            AsmResolver.DotNet.Signatures.SzArrayTypeSignature retSz = (AsmResolver.DotNet.Signatures.SzArrayTypeSignature)rt!;
            writer.Write("""
                        uint __retval_length = default;
                        
                """, isMultiline: true);
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
            ParameterInfo p = sig.Params[i];
            ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);
            if (cat != ParameterCategory.Out) { continue; }
            AsmResolver.DotNet.Signatures.TypeSignature uOut = AbiTypeHelpers.StripByRefAndCustomModifiers(p.Type);
            if (uOut.IsString() || AbiTypeHelpers.IsRuntimeClassOrInterface(context.Cache, uOut) || uOut.IsObject() || uOut.IsSystemType() || AbiTypeHelpers.IsComplexStruct(context.Cache, uOut) || uOut.IsGenericInstance()) { hasOutNeedsCleanup = true; break; }
        }
        bool hasReceiveArray = false;
        for (int i = 0; i < sig.Params.Count; i++)
        {
            if (ParameterCategoryResolver.GetParamCategory(sig.Params[i]) == ParameterCategory.ReceiveArray) { hasReceiveArray = true; break; }
        }
        bool hasNonBlittablePassArray = false;
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParameterInfo p = sig.Params[i];
            ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);
            if ((cat is ParameterCategory.PassArray or ParameterCategory.FillArray)
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
            ParameterInfo p = sig.Params[i];
            ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);
            if ((cat is ParameterCategory.In or ParameterCategory.Ref) && AbiTypeHelpers.IsComplexStruct(context.Cache, AbiTypeHelpers.StripByRefAndCustomModifiers(p.Type))) { hasComplexStructInput = true; break; }
        }
        // System.Type return: ABI.System.Type contains an HSTRING that must be disposed
        // after marshalling to managed System.Type, otherwise the HSTRING leaks.
        bool returnIsSystemTypeForCleanup = rt is not null && rt.IsSystemType();
        bool needsTryFinally = returnIsString || returnIsRefType || returnIsReceiveArray || hasOutNeedsCleanup || hasReceiveArray || returnIsComplexStruct || hasNonBlittablePassArray || hasComplexStructInput || returnIsSystemTypeForCleanup;
        if (needsTryFinally)
        {
            writer.Write("""
                        try
                        {
                """, isMultiline: true);
        }

        string indent = needsTryFinally ? "            " : "        ";

        // Inside try (if applicable): assign complex-struct input locals via marshaller.
        //.: '__value = ProfileUsageMarshaller.ConvertToUnmanaged(value);'
        // Includes both 'in' (ParameterCategory.In) and 'in T' (ParameterCategory.Ref) forms.
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParameterInfo p = sig.Params[i];
            ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);
            if (cat is not (ParameterCategory.In or ParameterCategory.Ref)) { continue; }
            AsmResolver.DotNet.Signatures.TypeSignature pType = AbiTypeHelpers.StripByRefAndCustomModifiers(p.Type);
            if (!AbiTypeHelpers.IsComplexStruct(context.Cache, pType)) { continue; }
            string localName = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);
            string callName = AbiTypeHelpers.GetParamName(p, paramNameOverride);
            writer.WriteLine($"{indent}__{localName} = {AbiTypeHelpers.GetMarshallerFullName(writer, context, pType)}.ConvertToUnmanaged({callName});");
        }
        // Type input params: set up TypeReference locals before the fixed block:
        //   global::ABI.System.TypeMarshaller.ConvertToUnmanagedUnsafe(forType, out TypeReference __forType);
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParameterInfo p = sig.Params[i];
            if (ParameterCategoryResolver.GetParamCategory(p) != ParameterCategory.In) { continue; }
            if (!p.Type.IsSystemType()) { continue; }
            string localName = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);
            string callName = AbiTypeHelpers.GetParamName(p, paramNameOverride);
            writer.WriteLine($"{indent}global::ABI.System.TypeMarshaller.ConvertToUnmanagedUnsafe({callName}, out TypeReference __{localName});");
        }
        // Open a SINGLE fixed-block for ALL pinnable inputs:
        //   1. Ref params (typed ptr, separate "fixed(T* _x = &x)\n" lines, no braces)
        //   2. Complex-struct PassArrays (typed ptr, separate fixed line)
        //   3. All other "void*"-style pinnables (strings, Type[], blittable PassArrays,
        //      reference-type PassArrays via inline-pool span) merged into ONE
        //      "fixed(void* _a = ..., _b = ..., ...) {\n" block.
        // C# allows multiple chained "fixed(...)" without braces to share the next braced
        // body, which is what the original code emits. This avoids the deep nesting mine had
        // when emitting a separate fixed block per PassArray.
        int fixedNesting = 0;

        // Step 1: Emit typed-pointer fixed lines for Ref params and complex-struct PassArrays
        // (no braces - they share the body of the upcoming combined fixed-void* block, OR
        // each other if no void* block is needed).
        bool hasAnyVoidStarPinnable = false;
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParameterInfo p = sig.Params[i];
            ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);
            if (p.Type.IsString() || p.Type.IsSystemType()) { hasAnyVoidStarPinnable = true; continue; }
            if (cat is ParameterCategory.PassArray or ParameterCategory.FillArray)
            {
                // All PassArrays (including complex structs) go in the void* combined block,
                // matching truth's pattern. Complex structs use a (T*) cast at the call site.
                hasAnyVoidStarPinnable = true;
            }
        }
        // Emit typed fixed lines for Ref params.
        // Skip Ref+ComplexStruct: those are marshalled via __local (no fixed needed) and
        // passed as &__local at the call site (the is-value-type-in path).
        int typedFixedCount = 0;
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParameterInfo p = sig.Params[i];
            ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);
            if (cat == ParameterCategory.Ref)
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
                ParameterInfo p = sig.Params[i];
                ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);
                bool isString = p.Type.IsString();
                bool isType = p.Type.IsSystemType();
                bool isPassArray = cat is ParameterCategory.PassArray or ParameterCategory.FillArray;
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
                    if (isStringElem && cat == ParameterCategory.PassArray)
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
            writer.Write($$"""
                )
                {{indent}}{{new string(' ', fixedNesting * 4)}}{
                """, isMultiline: true);
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
            ParameterInfo p = sig.Params[i];
            ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);
            if (cat is not (ParameterCategory.PassArray or ParameterCategory.FillArray)) { continue; }
            if (p.Type is not AsmResolver.DotNet.Signatures.SzArrayTypeSignature szArr) { continue; }
            if (AbiTypeHelpers.IsBlittablePrimitive(context.Cache, szArr.BaseType) || AbiTypeHelpers.IsAnyStruct(context.Cache, szArr.BaseType)) { continue; }
            string callName = AbiTypeHelpers.GetParamName(p, paramNameOverride);
            string localName = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);
            if (szArr.BaseType.IsString())
            {
                // Skip pre-call ConvertToUnmanagedUnsafe for FillArray of strings — there's
                // nothing to convert (native fills the handles).
                if (cat == ParameterCategory.FillArray) { continue; }
                writer.Write($$"""
                    {{callIndent}}HStringArrayMarshaller.ConvertToUnmanagedUnsafe(
                    {{callIndent}}    source: {{callName}},
                    {{callIndent}}    hstringHeaders: (HStringHeader*) _{{localName}}_inlineHeaderArray,
                    {{callIndent}}    hstrings: __{{localName}}_span,
                    {{callIndent}}    pinnedGCHandles: __{{localName}}_pinnedHandleSpan);
                    """, isMultiline: true);
            }
            else
            {
                // FillArray (Span<T>) of non-blittable element types: skip pre-call
                // CopyToUnmanaged. The buffer the native side gets (_<name>) is uninitialized
                // ABI-format storage; the native callee fills it. The post-call writeback loop
                // emits CopyToManaged_<name> to propagate the native fills into the user's
                // managed Span<T>.
                if (cat == ParameterCategory.FillArray) { continue; }
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
                writer.Write($$"""
                    {{callIndent}}[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "CopyToUnmanaged")]
                    {{callIndent}}static extern void CopyToUnmanaged_{{localName}}([UnsafeAccessorType("{{ArrayElementEncoder.GetArrayMarshallerInteropPath(szArr.BaseType)}}")] object _, ReadOnlySpan<{{elementProjected}}> span, uint length, {{dataParamType}} data);
                    {{callIndent}}CopyToUnmanaged_{{localName}}(null, {{callName}}, (uint){{callName}}.Length, {{dataCastType}}_{{localName}});
                    """, isMultiline: true);
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
            ParameterInfo p = sig.Params[i];
            ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);
            if (cat is ParameterCategory.PassArray or ParameterCategory.FillArray)
            {
                string callName = AbiTypeHelpers.GetParamName(p, paramNameOverride);
                string localName = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);
                writer.Write($$"""
                    ,
                      (uint){{callName}}.Length, _{{localName}}
                    """, isMultiline: true);
                continue;
            }
            if (cat == ParameterCategory.Out)
            {
                string localName = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);
                writer.Write($$"""
                    ,
                      &__{{localName}}
                    """, isMultiline: true);
                continue;
            }
            if (cat == ParameterCategory.ReceiveArray)
            {
                string localName = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);
                writer.Write($$"""
                    ,
                      &__{{localName}}_length, &__{{localName}}_data
                    """, isMultiline: true);
                continue;
            }
            if (cat == ParameterCategory.Ref)
            {
                string localName = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);
                AsmResolver.DotNet.Signatures.TypeSignature uRefArg = AbiTypeHelpers.StripByRefAndCustomModifiers(p.Type);
                if (AbiTypeHelpers.IsComplexStruct(context.Cache, uRefArg))
                {
                    // Complex struct 'in' (Ref) param: pass &__local (the marshaled ABI struct).
                    writer.Write($$"""
                        ,
                          &__{{localName}}
                        """, isMultiline: true);
                }
                else
                {
                    // 'in T' projected param: pass the pinned pointer.
                    writer.Write($$"""
                        ,
                          _{{localName}}
                        """, isMultiline: true);
                }
                continue;
            }
            writer.Write("""
                ,
                  
                """, isMultiline: true);
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
            writer.Write("""
                ,
                  &__retval_length, &__retval_data
                """, isMultiline: true);
        }
        else if (rt is not null)
        {
            writer.Write("""
                ,
                  &__retval
                """, isMultiline: true);
        }
        // Close the vtable call. One less ')' when noexcept (no ThrowExceptionForHR wrap).
        writer.WriteLine(isNoExcept ? ");" : "));");

        // After call: copy native-filled values back into the user's managed Span<T> for
        // FillArray of non-blittable element types. The native callee wrote into our
        // ABI-format buffer (_<name>) which is separate from the user's Span<T>; we need to
        // CopyToManaged_<name> to convert each ABI element back to the projected form and
        // store it in the user's Span.write_marshal_from_abi
        // Blittable element types (primitives and almost-blittable structs) don't need this
        // because the user's Span wraps the same memory the native side wrote to.
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParameterInfo p = sig.Params[i];
            ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);
            if (cat != ParameterCategory.FillArray) { continue; }
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
            writer.Write($$"""
                {{callIndent}}[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "CopyToManaged")]
                {{callIndent}}static extern void CopyToManaged_{{localName}}([UnsafeAccessorType("{{ArrayElementEncoder.GetArrayMarshallerInteropPath(szFA.BaseType)}}")] object _, uint length, {{dataParamType}}, Span<{{elementProjected}}> span);
                {{callIndent}}CopyToManaged_{{localName}}(null, (uint)__{{localName}}_span.Length, {{dataCastType}}_{{localName}}, {{callName}});
                """, isMultiline: true);
        }

        // After call: write back Out params to caller's 'out' var.
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParameterInfo p = sig.Params[i];
            ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);
            if (cat != ParameterCategory.Out) { continue; }
            string callName = AbiTypeHelpers.GetParamName(p, paramNameOverride);
            string localName = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);
            AsmResolver.DotNet.Signatures.TypeSignature uOut = AbiTypeHelpers.StripByRefAndCustomModifiers(p.Type);

            // For Out generic instance: emit inline UnsafeAccessor to ConvertToManaged_<name>
            // before the writeback. (e.g. Collection1HandlerInvoke
            // emits the accessor inside try, right before the assignment).
            if (uOut.IsGenericInstance())
            {
                string interopTypeName = InteropTypeNameWriter.EncodeInteropTypeName(uOut, TypedefNameType.ABI) + ", WinRT.Interop";
                IndentedTextWriter __scratchProjectedTypeName = new();
                MethodFactory.WriteProjectedSignature(__scratchProjectedTypeName, context, uOut, false);
                string projectedTypeName = __scratchProjectedTypeName.ToString();
                writer.Write($$"""
                    {{callIndent}}[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "ConvertToManaged")]
                    {{callIndent}}static extern {{projectedTypeName}} ConvertToManaged_{{localName}}([UnsafeAccessorType("{{interopTypeName}}")] object _, void* value);
                    {{callIndent}}{{callName}} = ConvertToManaged_{{localName}}(null, __{{localName}});
                    """, isMultiline: true);
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
            ParameterInfo p = sig.Params[i];
            ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);
            if (cat != ParameterCategory.ReceiveArray) { continue; }
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
            writer.Write($$"""
                {{callIndent}}[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "ConvertToManaged")]
                {{callIndent}}static extern {{elementProjected}}[] ConvertToManaged_{{localName}}([UnsafeAccessorType("{{marshallerPath}}")] object _, uint length, {{elementAbi}}* data);
                {{callIndent}}{{callName}} = ConvertToManaged_{{localName}}(null, __{{localName}}_length, __{{localName}}_data);
                """, isMultiline: true);
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
                writer.Write($$"""
                    {{callIndent}}[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "ConvertToManaged")]
                    {{callIndent}}static extern {{elementProjected}}[] ConvertToManaged_retval([UnsafeAccessorType("{{ArrayElementEncoder.GetArrayMarshallerInteropPath(retSz.BaseType)}}")] object _, uint length, {{elementAbi}}* data);
                    {{callIndent}}return ConvertToManaged_retval(null, __retval_length, __retval_data);
                    """, isMultiline: true);
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
                    // Nullable<T> return: use <T>Marshaller.UnboxToManaged.;
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
                    writer.Write($$"""
                        {{callIndent}}[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "ConvertToManaged")]
                        {{callIndent}}static extern {{projectedTypeName}} ConvertToManaged_retval([UnsafeAccessorType("{{interopTypeName}}")] object _, void* value);
                        {{callIndent}}return ConvertToManaged_retval(null, __retval);
                        """, isMultiline: true);
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
            writer.Write("""
                        }
                        finally
                        {
                """, isMultiline: true);

            // Order matches truth:
            // 0. Complex-struct input param Dispose (e.g. ProfileUsageMarshaller.Dispose(__value))
            // 1. Non-blittable PassArray/FillArray cleanup (Dispose + ArrayPools)
            // 2. Out param frees (HString / object / runtime class)
            // 3. ReceiveArray param frees (Free_<name> via UnsafeAccessor)
            // 4. Return free (__retval) — last

            // 0. Dispose complex-struct input params via marshaller (both 'in' and 'in T' forms).
            for (int i = 0; i < sig.Params.Count; i++)
            {
                ParameterInfo p = sig.Params[i];
                ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);
                if (cat is not (ParameterCategory.In or ParameterCategory.Ref)) { continue; }
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
                ParameterInfo p = sig.Params[i];
                ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);
                if (cat is not (ParameterCategory.PassArray or ParameterCategory.FillArray)) { continue; }
                if (p.Type is not AsmResolver.DotNet.Signatures.SzArrayTypeSignature szArr) { continue; }
                if (AbiTypeHelpers.IsBlittablePrimitive(context.Cache, szArr.BaseType) || AbiTypeHelpers.IsAnyStruct(context.Cache, szArr.BaseType)) { continue; }
                if (AbiTypeHelpers.IsMappedAbiValueType(szArr.BaseType)) { continue; }
                if (szArr.BaseType.IsHResultException())
                {
                    // HResultException ABI is just an int; per-element Dispose is a no-op (mirror
                    // the truth: no Dispose_<name> emitted). Just return the inline-array's pool
                    // using the correct element type (ABI.System.Exception, not nint).
                    string localNameH = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);
                    writer.WriteLine("");
                    writer.Write($$"""
                                    if (__{{localNameH}}_arrayFromPool is not null)
                                    {
                                        global::System.Buffers.ArrayPool<global::ABI.System.Exception>.Shared.Return(__{{localNameH}}_arrayFromPool);
                                    }
                        """, isMultiline: true);
                    continue;
                }
                string localName = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);
                if (szArr.BaseType.IsString())
                {
                    // The HStringArrayMarshaller.Dispose + ArrayPool returns for strings only
                    // apply to PassArray (where we set up the pinned handles + headers in the
                    // first place). FillArray writes back HSTRING handles into the nint storage
                    // array directly, with no per-element pinned handle / header to release.
                    if (cat == ParameterCategory.PassArray)
                    {
                        writer.Write($$"""
                                        HStringArrayMarshaller.Dispose(__{{localName}}_pinnedHandleSpan);
                            
                                        if (__{{localName}}_pinnedHandleArrayFromPool is not null)
                                        {
                                            global::System.Buffers.ArrayPool<nint>.Shared.Return(__{{localName}}_pinnedHandleArrayFromPool);
                                        }
                            
                                        if (__{{localName}}_headerArrayFromPool is not null)
                                        {
                                            global::System.Buffers.ArrayPool<HStringHeader>.Shared.Return(__{{localName}}_headerArrayFromPool);
                                        }
                            """, isMultiline: true);
                    }
                    // Both PassArray and FillArray need the inline-array's nint pool returned.
                    writer.WriteLine("");
                    writer.Write($$"""
                                    if (__{{localName}}_arrayFromPool is not null)
                                    {
                                        global::System.Buffers.ArrayPool<nint>.Shared.Return(__{{localName}}_arrayFromPool);
                                    }
                        """, isMultiline: true);
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
                    writer.Write($$"""
                                    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "Dispose")]
                                    static extern void Dispose_{{localName}}([UnsafeAccessorType("{{ArrayElementEncoder.GetArrayMarshallerInteropPath(szArr.BaseType)}}")] object _, uint length, {{disposeDataParamType}}
                        """, isMultiline: true);
                    if (!disposeDataParamType.EndsWith("data", System.StringComparison.Ordinal)) { writer.Write(" data"); }
                    writer.Write($$"""
                        );
                        
                                    fixed({{fixedPtrType}} _{{localName}} = __{{localName}}_span)
                                    {
                                        Dispose_{{localName}}(null, (uint) __{{localName}}_span.Length, {{disposeCastType}}_{{localName}});
                                    }
                        """, isMultiline: true);
                }
                // ArrayPool storage type matches the InlineArray storage (mapped ABI value type
                // for DateTime/TimeSpan; ABI struct for complex structs; nint otherwise).
                string poolStorageT = AbiTypeHelpers.IsMappedAbiValueType(szArr.BaseType)
                    ? AbiTypeHelpers.GetMappedAbiTypeName(szArr.BaseType)
                    : AbiTypeHelpers.IsComplexStruct(context.Cache, szArr.BaseType)
                        ? AbiTypeHelpers.GetAbiStructTypeName(writer, context, szArr.BaseType)
                        : "nint";
                writer.WriteLine("");
                writer.Write($$"""
                                if (__{{localName}}_arrayFromPool is not null)
                                {
                                    global::System.Buffers.ArrayPool<{{poolStorageT}}>.Shared.Return(__{{localName}}_arrayFromPool);
                                }
                    """, isMultiline: true);
            }

            // 2. Free Out string/object/runtime-class params.
            for (int i = 0; i < sig.Params.Count; i++)
            {
                ParameterInfo p = sig.Params[i];
                ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);
                if (cat != ParameterCategory.Out) { continue; }
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
                ParameterInfo p = sig.Params[i];
                ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);
                if (cat != ParameterCategory.ReceiveArray) { continue; }
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
                writer.Write($$"""
                                [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "Free")]
                                static extern void Free_{{localName}}([UnsafeAccessorType("{{marshallerPath}}")] object _, uint length, {{elementAbi}}* data);
                    
                                Free_{{localName}}(null, __{{localName}}_length, __{{localName}}_data);
                    """, isMultiline: true);
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
                writer.Write($$"""
                                [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "Free")]
                                static extern void Free_retval([UnsafeAccessorType("{{ArrayElementEncoder.GetArrayMarshallerInteropPath(retSz.BaseType)}}")] object _, uint length, {{elementAbi}}* data);
                                Free_retval(null, __retval_length, __retval_data);
                    """, isMultiline: true);
            }

            writer.WriteLine("        }");
        }

        writer.WriteLine("    }");
    }

    /// <summary>Emits the conversion of a parameter from its projected (managed) form to the ABI argument form.</summary>
    internal static void EmitParamArgConversion(IndentedTextWriter writer, ProjectionEmitContext context, ParameterInfo p, string? paramNameOverride = null)
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
