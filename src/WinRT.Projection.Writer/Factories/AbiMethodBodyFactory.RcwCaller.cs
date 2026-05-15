// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Helpers;
using WindowsRuntime.ProjectionWriter.Metadata;
using WindowsRuntime.ProjectionWriter.Models;
using WindowsRuntime.ProjectionWriter.Resolvers;
using WindowsRuntime.ProjectionWriter.Writers;

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
    [SuppressMessage("Style", "IDE0045:Convert to conditional expression",
        Justification = "if/else if chains over type-class predicates are more readable than nested ternaries.")]
    internal static void EmitAbiMethodBodyIfSimple(IndentedTextWriter writer, ProjectionEmitContext context, MethodSignatureInfo sig, int slot, string? paramNameOverride = null, bool isNoExcept = false)
    {
        TypeSignature? rt = sig.ReturnType;

        AbiTypeShapeKind returnShape = rt is null ? AbiTypeShapeKind.Unknown : context.AbiTypeShapeResolver.Resolve(rt).Kind;

        bool returnIsString = returnShape == AbiTypeShapeKind.String;
        bool returnIsRefType = returnShape is AbiTypeShapeKind.RuntimeClassOrInterface or AbiTypeShapeKind.Delegate or AbiTypeShapeKind.Object or AbiTypeShapeKind.GenericInstance or AbiTypeShapeKind.NullableT;
        bool returnIsBlittableStruct = returnShape == AbiTypeShapeKind.BlittableStruct;
        bool returnIsComplexStruct = returnShape == AbiTypeShapeKind.ComplexStruct;
        bool returnIsReceiveArray = rt is SzArrayTypeSignature retSzCheck
            && (context.AbiTypeShapeResolver.IsBlittablePrimitive(retSzCheck.BaseType) || context.AbiTypeShapeResolver.IsBlittableStruct(retSzCheck.BaseType)
                || retSzCheck.BaseType.IsString() || context.AbiTypeShapeResolver.IsRuntimeClassOrInterface(retSzCheck.BaseType) || retSzCheck.BaseType.IsObject()
                || context.AbiTypeShapeResolver.IsComplexStruct(retSzCheck.BaseType)
                || retSzCheck.BaseType.IsHResultException()
                || context.AbiTypeShapeResolver.IsMappedAbiValueType(retSzCheck.BaseType));
        bool returnIsHResultException = returnShape == AbiTypeShapeKind.HResultException;

        // Build the function pointer signature: void*, [paramAbiType...,] [retAbiType*,] int
        StringBuilder fp = new();
        _ = fp.Append("void*");
        foreach (ParameterInfo p in sig.Parameters)
        {
            ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);

            if (cat is ParameterCategory.PassArray or ParameterCategory.FillArray)
            {
                _ = fp.Append(", uint, void*");
                continue;
            }

            if (cat == ParameterCategory.Out)
            {
                TypeSignature uOut = AbiTypeHelpers.StripByRefAndCustomModifiers(p.Type);
                _ = fp.Append(", ");

                if (uOut.IsString() || context.AbiTypeShapeResolver.IsRuntimeClassOrInterface(uOut) || uOut.IsObject() || uOut.IsGenericInstance())
                {
                    _ = fp.Append("void**");
                }
                else if (uOut.IsSystemType())
                {
                    _ = fp.Append("global::ABI.System.Type*");
                }
                else if (context.AbiTypeShapeResolver.IsComplexStruct(uOut))
                {
                    _ = fp.Append(AbiTypeHelpers.GetAbiStructTypeName(writer, context, uOut)); _ = fp.Append('*');
                }
                else if (context.AbiTypeShapeResolver.IsBlittableStruct(uOut))
                {
                    _ = fp.Append(AbiTypeHelpers.GetBlittableStructAbiType(writer, context, uOut)); _ = fp.Append('*');
                }
                else
                {
                    _ = fp.Append(AbiTypeHelpers.GetAbiPrimitiveType(context.Cache, uOut)); _ = fp.Append('*');
                }

                continue;
            }

            if (cat == ParameterCategory.Ref)
            {
                TypeSignature uRef = AbiTypeHelpers.StripByRefAndCustomModifiers(p.Type);
                _ = fp.Append(", ");

                if (context.AbiTypeShapeResolver.IsComplexStruct(uRef))
                {
                    _ = fp.Append(AbiTypeHelpers.GetAbiStructTypeName(writer, context, uRef)); _ = fp.Append('*');
                }
                else if (context.AbiTypeShapeResolver.IsBlittableStruct(uRef))
                {
                    _ = fp.Append(AbiTypeHelpers.GetBlittableStructAbiType(writer, context, uRef)); _ = fp.Append('*');
                }
                else
                {
                    _ = fp.Append(AbiTypeHelpers.GetAbiPrimitiveType(context.Cache, uRef)); _ = fp.Append('*');
                }

                continue;
            }

            if (cat == ParameterCategory.ReceiveArray)
            {
                SzArrayTypeSignature sza = (SzArrayTypeSignature)AbiTypeHelpers.StripByRefAndCustomModifiers(p.Type);
                _ = fp.Append(", uint*, ");

                if (sza.BaseType.IsString() || context.AbiTypeShapeResolver.IsRuntimeClassOrInterface(sza.BaseType) || sza.BaseType.IsObject())
                {
                    _ = fp.Append("void*");
                }
                else if (sza.BaseType.IsHResultException())
                {
                    _ = fp.Append("global::ABI.System.Exception");
                }
                else if (context.AbiTypeShapeResolver.IsMappedAbiValueType(sza.BaseType))
                {
                    _ = fp.Append(AbiTypeHelpers.GetMappedAbiTypeName(sza.BaseType));
                }
                else if (context.AbiTypeShapeResolver.IsComplexStruct(sza.BaseType))
                {
                    _ = fp.Append(AbiTypeHelpers.GetAbiStructTypeName(writer, context, sza.BaseType));
                }
                else
                {
                    _ = fp.Append(context.AbiTypeShapeResolver.IsBlittableStruct(sza.BaseType)
                        ? AbiTypeHelpers.GetBlittableStructAbiType(writer, context, sza.BaseType)
                        : AbiTypeHelpers.GetAbiPrimitiveType(context.Cache, sza.BaseType));
                }

                _ = fp.Append("**");
                continue;
            }

            _ = fp.Append(", ");

            if (p.Type.IsHResultException())
            {
                _ = fp.Append("global::ABI.System.Exception");
            }
            else if (p.Type.IsString() || context.AbiTypeShapeResolver.IsRuntimeClassOrInterface(p.Type) || p.Type.IsObject() || p.Type.IsGenericInstance())
            {
                _ = fp.Append("void*");
            }
            else if (p.Type.IsSystemType())
            {
                _ = fp.Append("global::ABI.System.Type");
            }
            else if (context.AbiTypeShapeResolver.IsBlittableStruct(p.Type))
            {
                _ = fp.Append(AbiTypeHelpers.GetBlittableStructAbiType(writer, context, p.Type));
            }
            else if (context.AbiTypeShapeResolver.IsMappedAbiValueType(p.Type))
            {
                _ = fp.Append(AbiTypeHelpers.GetMappedAbiTypeName(p.Type));
            }
            else
            {
                _ = fp.Append(context.AbiTypeShapeResolver.IsComplexStruct(p.Type)
                    ? AbiTypeHelpers.GetAbiStructTypeName(writer, context, p.Type)
                    : AbiTypeHelpers.GetAbiPrimitiveType(context.Cache, p.Type));
            }
        }

        if (rt is not null)
        {
            if (returnIsReceiveArray)
            {
                SzArrayTypeSignature retSz = (SzArrayTypeSignature)rt;
                _ = fp.Append(", uint*, ");

                if (retSz.BaseType.IsString() || context.AbiTypeShapeResolver.IsRuntimeClassOrInterface(retSz.BaseType) || retSz.BaseType.IsObject())
                {
                    _ = fp.Append("void*");
                }
                else if (context.AbiTypeShapeResolver.IsComplexStruct(retSz.BaseType))
                {
                    _ = fp.Append(AbiTypeHelpers.GetAbiStructTypeName(writer, context, retSz.BaseType));
                }
                else if (retSz.BaseType.IsHResultException())
                {
                    _ = fp.Append("global::ABI.System.Exception");
                }
                else if (context.AbiTypeShapeResolver.IsMappedAbiValueType(retSz.BaseType))
                {
                    _ = fp.Append(AbiTypeHelpers.GetMappedAbiTypeName(retSz.BaseType));
                }
                else if (context.AbiTypeShapeResolver.IsBlittableStruct(retSz.BaseType))
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

                if (returnIsString || returnIsRefType)
                {
                    _ = fp.Append("void**");
                }
                else if (rt is not null && rt.IsSystemType())
                {
                    _ = fp.Append("global::ABI.System.Type*");
                }
                else if (returnIsBlittableStruct)
                {
                    _ = fp.Append(AbiTypeHelpers.GetBlittableStructAbiType(writer, context, rt!)); _ = fp.Append('*');
                }
                else if (returnIsComplexStruct)
                {
                    _ = fp.Append(AbiTypeHelpers.GetAbiStructTypeName(writer, context, rt!)); _ = fp.Append('*');
                }
                else if (rt is not null && context.AbiTypeShapeResolver.IsMappedAbiValueType(rt))
                {
                    _ = fp.Append(AbiTypeHelpers.GetMappedAbiTypeName(rt)); _ = fp.Append('*');
                }
                else
                {
                    _ = fp.Append(AbiTypeHelpers.GetAbiPrimitiveType(context.Cache, rt!)); _ = fp.Append('*');
                }
            }
        }

        _ = fp.Append(", int");

        writer.WriteLine();
        writer.WriteLine(isMultiline: true, """
                {
                    using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();
                    void* ThisPtr = thisValue.GetThisPtrUnsafe();
            """);

        // Declare 'using' marshaller values for ref-type parameters (these need disposing).
        for (int i = 0; i < sig.Parameters.Count; i++)
        {
            ParameterInfo p = sig.Parameters[i];

            if (context.AbiTypeShapeResolver.IsRuntimeClassOrInterface(p.Type) || p.Type.IsObject())
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
                TypeSignature inner = p.Type.GetNullableInnerType()!;
                string innerMarshaller = AbiTypeHelpers.GetNullableInnerMarshallerName(writer, context, inner);
                writer.WriteLine($"        using WindowsRuntimeObjectReferenceValue __{localName} = {innerMarshaller}.BoxToUnmanaged({callName});");
            }
            else if (p.Type.IsGenericInstance())
            {
                // Generic instance param: emit a local UnsafeAccessor delegate to get the marshaller method.
                string localName = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);
                string callName = AbiTypeHelpers.GetParamName(p, paramNameOverride);
                string interopTypeName = InteropTypeNameWriter.EncodeInteropTypeName(p.Type, TypedefNameType.ABI) + ", WinRT.Interop";
                string projectedTypeName = MethodFactory.WriteProjectedSignature(context, p.Type, false);
                writer.WriteLine(isMultiline: true, $$"""
                            [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "ConvertToUnmanaged")]
                            static extern WindowsRuntimeObjectReferenceValue ConvertToUnmanaged_{{localName}}([UnsafeAccessorType("{{interopTypeName}}")] object _, {{projectedTypeName}} value);
                            using WindowsRuntimeObjectReferenceValue __{{localName}} = ConvertToUnmanaged_{{localName}}(null, {{callName}});
                    """);
            }
        }

        // (String input params are now stack-allocated via the fast-path pinning pattern below;
        //  no separate void* local declaration or up-front allocation is needed.)
        // Declare locals for HResult/Exception input parameters (converted up-front).
        for (int i = 0; i < sig.Parameters.Count; i++)
        {
            ParameterInfo p = sig.Parameters[i];

            if (ParameterCategoryResolver.GetParamCategory(p) != ParameterCategory.In)
            {
                continue;
            }

            if (!p.Type.IsHResultException())
            {
                continue;
            }

            string localName = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);
            string callName = AbiTypeHelpers.GetParamName(p, paramNameOverride);
            writer.WriteLine($"        global::ABI.System.Exception __{localName} = global::ABI.System.ExceptionMarshaller.ConvertToUnmanaged({callName});");
        }

        // Declare locals for mapped value-type input parameters (DateTime/TimeSpan): convert via marshaller up-front.
        for (int i = 0; i < sig.Parameters.Count; i++)
        {
            ParameterInfo p = sig.Parameters[i];

            if (ParameterCategoryResolver.GetParamCategory(p) != ParameterCategory.In)
            {
                continue;
            }

            if (!context.AbiTypeShapeResolver.IsMappedAbiValueType(p.Type))
            {
                continue;
            }

            string localName = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);
            string callName = AbiTypeHelpers.GetParamName(p, paramNameOverride);
            writer.WriteLine($"        {AbiTypeHelpers.GetMappedAbiTypeName(p.Type)} __{localName} = {AbiTypeHelpers.GetMappedMarshallerName(p.Type)}.ConvertToUnmanaged({callName});");
        }

        // Declare locals for complex-struct input parameters (e.g. ProfileUsage with nested
        // string/Nullable fields): default-initialize OUTSIDE try, assign inside try via marshaller,
        // dispose in finally.
        // Includes both 'in' (ParameterCategory.In) and 'in T' (ParameterCategory.Ref) forms.
        for (int i = 0; i < sig.Parameters.Count; i++)
        {
            ParameterInfo p = sig.Parameters[i];
            ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);

            if (cat is not (ParameterCategory.In or ParameterCategory.Ref))
            {
                continue;
            }

            TypeSignature pType = AbiTypeHelpers.StripByRefAndCustomModifiers(p.Type);

            if (!context.AbiTypeShapeResolver.IsComplexStruct(pType))
            {
                continue;
            }

            string localName = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);
            writer.WriteLine($"        {AbiTypeHelpers.GetAbiStructTypeName(writer, context, pType)} __{localName} = default;");
        }

        // Declare locals for Out parameters (need to be passed as &__<name> to the call).
        for (int i = 0; i < sig.Parameters.Count; i++)
        {
            ParameterInfo p = sig.Parameters[i];
            ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);

            if (cat != ParameterCategory.Out)
            {
                continue;
            }

            string localName = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);
            TypeSignature uOut = AbiTypeHelpers.StripByRefAndCustomModifiers(p.Type);
            writer.Write("        ");

            if (uOut.IsString() || context.AbiTypeShapeResolver.IsRuntimeClassOrInterface(uOut) || uOut.IsObject() || uOut.IsGenericInstance())
            {
                writer.Write("void*");
            }
            else if (uOut.IsSystemType())
            {
                writer.Write("global::ABI.System.Type");
            }
            else if (context.AbiTypeShapeResolver.IsComplexStruct(uOut))
            {
                writer.Write(AbiTypeHelpers.GetAbiStructTypeName(writer, context, uOut));
            }
            else if (context.AbiTypeShapeResolver.IsBlittableStruct(uOut))
            {
                writer.Write(AbiTypeHelpers.GetBlittableStructAbiType(writer, context, uOut));
            }
            else
            {
                writer.Write(AbiTypeHelpers.GetAbiPrimitiveType(context.Cache, uOut));
            }

            writer.WriteLine($" __{localName} = default;");
        }

        // Declare locals for ReceiveArray params (uint length + element pointer).
        for (int i = 0; i < sig.Parameters.Count; i++)
        {
            ParameterInfo p = sig.Parameters[i];
            ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);

            if (cat != ParameterCategory.ReceiveArray)
            {
                continue;
            }

            string localName = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);
            SzArrayTypeSignature sza = (SzArrayTypeSignature)AbiTypeHelpers.StripByRefAndCustomModifiers(p.Type);
            writer.WriteLine(isMultiline: true, $$"""
                        uint __{{localName}}_length = default;
                        
                """);
            // Element ABI type: void* for ref types; ABI struct for complex/blittable structs;
            // primitive ABI otherwise.
            if (sza.BaseType.IsString() || context.AbiTypeShapeResolver.IsRuntimeClassOrInterface(sza.BaseType) || sza.BaseType.IsObject())
            {
                writer.Write("void*");
            }
            else if (context.AbiTypeShapeResolver.IsComplexStruct(sza.BaseType))
            {
                writer.Write(AbiTypeHelpers.GetAbiStructTypeName(writer, context, sza.BaseType));
            }
            else if (context.AbiTypeShapeResolver.IsBlittableStruct(sza.BaseType))
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
        for (int i = 0; i < sig.Parameters.Count; i++)
        {
            ParameterInfo p = sig.Parameters[i];
            ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);

            if (cat is not (ParameterCategory.PassArray or ParameterCategory.FillArray))
            {
                continue;
            }

            if (p.Type is not SzArrayTypeSignature szArr)
            {
                continue;
            }

            if (context.AbiTypeShapeResolver.IsBlittablePrimitive(szArr.BaseType) || context.AbiTypeShapeResolver.IsBlittableStruct(szArr.BaseType))
            {
                continue;
            }

            // Non-blittable element type: emit InlineArray16<storageT> + ArrayPool<storageT>.
            // For mapped value types (DateTime/TimeSpan), use the ABI struct type.
            // For complex structs (e.g. authored BasicStruct with reference fields), use the ABI
            // struct type. For everything else (runtime classes, objects, strings), use nint.
            string localName = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);
            string callName = AbiTypeHelpers.GetParamName(p, paramNameOverride);
            string storageT = context.AbiTypeShapeResolver.IsMappedAbiValueType(szArr.BaseType)
                ? AbiTypeHelpers.GetMappedAbiTypeName(szArr.BaseType)
                : context.AbiTypeShapeResolver.IsComplexStruct(szArr.BaseType)
                    ? AbiTypeHelpers.GetAbiStructTypeName(writer, context, szArr.BaseType)
                    : szArr.BaseType.IsHResultException()
                        ? "global::ABI.System.Exception"
                        : "nint";
            writer.WriteLine();
            writer.WriteLine(isMultiline: true, $$"""
                        Unsafe.SkipInit(out InlineArray16<{{storageT}}> __{{localName}}_inlineArray);
                        {{storageT}}[] __{{localName}}_arrayFromPool = null;
                        Span<{{storageT}}> __{{localName}}_span = {{callName}}.Length <= 16
                            ? __{{localName}}_inlineArray[..{{callName}}.Length]
                            : (__{{localName}}_arrayFromPool = global::System.Buffers.ArrayPool<{{storageT}}>.Shared.Rent({{callName}}.Length));
                """);

            if (szArr.BaseType.IsString() && cat == ParameterCategory.PassArray)
            {
                // Strings need an additional InlineArray16<HStringHeader> + InlineArray16<nint> (pinned handles).
                // Only required for PassArray (managed -> HSTRING conversion); FillArray's native side
                // fills HSTRING handles directly into the nint storage.
                writer.WriteLine();
                writer.WriteLine(isMultiline: true, $$"""
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
                    """);
            }
        }

        if (returnIsReceiveArray)
        {
            SzArrayTypeSignature retSz = (SzArrayTypeSignature)rt!;
            writer.WriteLine(isMultiline: true, """
                        uint __retval_length = default;
                        
                """);
            if (retSz.BaseType.IsString() || context.AbiTypeShapeResolver.IsRuntimeClassOrInterface(retSz.BaseType) || retSz.BaseType.IsObject())
            {
                writer.Write("void*");
            }
            else if (context.AbiTypeShapeResolver.IsComplexStruct(retSz.BaseType))
            {
                writer.Write(AbiTypeHelpers.GetAbiStructTypeName(writer, context, retSz.BaseType));
            }
            else if (retSz.BaseType.IsHResultException())
            {
                writer.Write("global::ABI.System.Exception");
            }
            else if (context.AbiTypeShapeResolver.IsMappedAbiValueType(retSz.BaseType))
            {
                writer.Write(AbiTypeHelpers.GetMappedAbiTypeName(retSz.BaseType));
            }
            else if (context.AbiTypeShapeResolver.IsBlittableStruct(retSz.BaseType))
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
        else if (returnIsBlittableStruct)
        {
            writer.WriteLine($"        {AbiTypeHelpers.GetBlittableStructAbiType(writer, context, rt!)} __retval = default;");
        }
        else if (returnIsComplexStruct)
        {
            writer.WriteLine($"        {AbiTypeHelpers.GetAbiStructTypeName(writer, context, rt!)} __retval = default;");
        }
        else if (rt is not null && context.AbiTypeShapeResolver.IsMappedAbiValueType(rt))
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
        for (int i = 0; i < sig.Parameters.Count; i++)
        {
            ParameterInfo p = sig.Parameters[i];
            ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);

            if (cat != ParameterCategory.Out)
            {
                continue;
            }

            TypeSignature uOut = AbiTypeHelpers.StripByRefAndCustomModifiers(p.Type);

            if (uOut.IsString() || context.AbiTypeShapeResolver.IsRuntimeClassOrInterface(uOut) || uOut.IsObject() || uOut.IsSystemType() || context.AbiTypeShapeResolver.IsComplexStruct(uOut) || uOut.IsGenericInstance())
            {
                hasOutNeedsCleanup = true;
                break;
            }
        }
        bool hasReceiveArray = false;
        for (int i = 0; i < sig.Parameters.Count; i++)
        {
            if (ParameterCategoryResolver.GetParamCategory(sig.Parameters[i]) == ParameterCategory.ReceiveArray)
            {
                hasReceiveArray = true;
                break;
            }
        }
        bool hasNonBlittablePassArray = false;
        for (int i = 0; i < sig.Parameters.Count; i++)
        {
            ParameterInfo p = sig.Parameters[i];
            ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);

            if ((cat is ParameterCategory.PassArray or ParameterCategory.FillArray)
                && p.Type is SzArrayTypeSignature szArrCheck
                && !context.AbiTypeShapeResolver.IsBlittablePrimitive(szArrCheck.BaseType) && !context.AbiTypeShapeResolver.IsBlittableStruct(szArrCheck.BaseType)
                && !context.AbiTypeShapeResolver.IsMappedAbiValueType(szArrCheck.BaseType))
            {
                hasNonBlittablePassArray = true;
                break;
            }
        }
        bool hasComplexStructInput = false;
        for (int i = 0; i < sig.Parameters.Count; i++)
        {
            ParameterInfo p = sig.Parameters[i];
            ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);

            if ((cat is ParameterCategory.In or ParameterCategory.Ref) && context.AbiTypeShapeResolver.IsComplexStruct(AbiTypeHelpers.StripByRefAndCustomModifiers(p.Type)))
            {
                hasComplexStructInput = true;
                break;
            }
        }

        // System.Type return: ABI.System.Type contains an HSTRING that must be disposed
        // after marshalling to managed System.Type, otherwise the HSTRING leaks.
        bool returnIsSystemTypeForCleanup = rt is not null && rt.IsSystemType();
        bool needsTryFinally = returnIsString || returnIsRefType || returnIsReceiveArray || hasOutNeedsCleanup || hasReceiveArray || returnIsComplexStruct || hasNonBlittablePassArray || hasComplexStructInput || returnIsSystemTypeForCleanup;

        if (needsTryFinally)
        {
            writer.WriteLine(isMultiline: true, """
                        try
                        {
                """);
        }

        string indent = needsTryFinally ? "            " : "        ";

        // Inside try (if applicable): assign complex-struct input locals via marshaller.
        //.: '__value = ProfileUsageMarshaller.ConvertToUnmanaged(value);'
        // Includes both 'in' (ParameterCategory.In) and 'in T' (ParameterCategory.Ref) forms.
        for (int i = 0; i < sig.Parameters.Count; i++)
        {
            ParameterInfo p = sig.Parameters[i];
            ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);

            if (cat is not (ParameterCategory.In or ParameterCategory.Ref))
            {
                continue;
            }

            TypeSignature pType = AbiTypeHelpers.StripByRefAndCustomModifiers(p.Type);

            if (!context.AbiTypeShapeResolver.IsComplexStruct(pType))
            {
                continue;
            }

            string localName = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);
            string callName = AbiTypeHelpers.GetParamName(p, paramNameOverride);
            writer.WriteLine($"{indent}__{localName} = {AbiTypeHelpers.GetMarshallerFullName(writer, context, pType)}.ConvertToUnmanaged({callName});");
        }

        // Type input params: set up TypeReference locals before the fixed block:
        //   global::ABI.System.TypeMarshaller.ConvertToUnmanagedUnsafe(forType, out TypeReference __forType);
        for (int i = 0; i < sig.Parameters.Count; i++)
        {
            ParameterInfo p = sig.Parameters[i];

            if (ParameterCategoryResolver.GetParamCategory(p) != ParameterCategory.In)
            {
                continue;
            }

            if (!p.Type.IsSystemType())
            {
                continue;
            }

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
        for (int i = 0; i < sig.Parameters.Count; i++)
        {
            ParameterInfo p = sig.Parameters[i];
            ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);

            if (p.Type.IsString() || p.Type.IsSystemType())
            {
                hasAnyVoidStarPinnable = true;
                continue;
            }

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
        for (int i = 0; i < sig.Parameters.Count; i++)
        {
            ParameterInfo p = sig.Parameters[i];
            ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);

            if (cat == ParameterCategory.Ref)
            {
                TypeSignature uRefSkip = AbiTypeHelpers.StripByRefAndCustomModifiers(p.Type);

                if (context.AbiTypeShapeResolver.IsComplexStruct(uRefSkip))
                {
                    continue;
                }

                string callName = AbiTypeHelpers.GetParamName(p, paramNameOverride);
                string localName = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);
                TypeSignature uRef = uRefSkip;
                string abiType = context.AbiTypeShapeResolver.IsBlittableStruct(uRef) ? AbiTypeHelpers.GetBlittableStructAbiType(writer, context, uRef) : AbiTypeHelpers.GetAbiPrimitiveType(context.Cache, uRef);
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
            for (int i = 0; i < sig.Parameters.Count; i++)
            {
                ParameterInfo p = sig.Parameters[i];
                ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);
                bool isString = p.Type.IsString();
                bool isType = p.Type.IsSystemType();
                bool isPassArray = cat is ParameterCategory.PassArray or ParameterCategory.FillArray;

                if (!isString && !isType && !isPassArray)
                {
                    continue;
                }

                string callName = AbiTypeHelpers.GetParamName(p, paramNameOverride);
                string localName = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);

                writer.WriteIf(!first, ", ");

                first = false;
                writer.Write($"_{localName} = ");

                if (isType)
                {
                    writer.Write($"__{localName}");
                }
                else if (isPassArray)
                {
                    TypeSignature elemT = ((SzArrayTypeSignature)p.Type).BaseType;
                    bool isBlittableElem = context.AbiTypeShapeResolver.IsBlittablePrimitive(elemT) || context.AbiTypeShapeResolver.IsBlittableStruct(elemT);
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
            writer.WriteLine(isMultiline: true, $$"""
                )
                {{indent}}{{new string(' ', fixedNesting * 4)}}{
                """);
            fixedNesting++;
            // Inside the body: emit HStringMarshaller calls for input string params.
            for (int i = 0; i < sig.Parameters.Count; i++)
            {
                if (!sig.Parameters[i].Type.IsString())
                {
                    continue;
                }

                string callName = AbiTypeHelpers.GetParamName(sig.Parameters[i], paramNameOverride);
                string localName = AbiTypeHelpers.GetParamLocalName(sig.Parameters[i], paramNameOverride);
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
        for (int i = 0; i < sig.Parameters.Count; i++)
        {
            ParameterInfo p = sig.Parameters[i];
            ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);

            if (cat is not (ParameterCategory.PassArray or ParameterCategory.FillArray))
            {
                continue;
            }

            if (p.Type is not SzArrayTypeSignature szArr)
            {
                continue;
            }

            if (context.AbiTypeShapeResolver.IsBlittablePrimitive(szArr.BaseType) || context.AbiTypeShapeResolver.IsBlittableStruct(szArr.BaseType))
            {
                continue;
            }

            string callName = AbiTypeHelpers.GetParamName(p, paramNameOverride);
            string localName = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);

            if (szArr.BaseType.IsString())
            {
                // Skip pre-call ConvertToUnmanagedUnsafe for FillArray of strings — there's
                // nothing to convert (native fills the handles).
                if (cat == ParameterCategory.FillArray)
                {
                    continue;
                }

                writer.WriteLine(isMultiline: true, $$"""
                    {{callIndent}}HStringArrayMarshaller.ConvertToUnmanagedUnsafe(
                    {{callIndent}}    source: {{callName}},
                    {{callIndent}}    hstringHeaders: (HStringHeader*) _{{localName}}_inlineHeaderArray,
                    {{callIndent}}    hstrings: __{{localName}}_span,
                    {{callIndent}}    pinnedGCHandles: __{{localName}}_pinnedHandleSpan);
                    """);
            }
            else
            {
                // FillArray (Span<T>) of non-blittable element types: skip pre-call
                // CopyToUnmanaged. The buffer the native side gets (_<name>) is uninitialized
                // ABI-format storage; the native callee fills it. The post-call writeback loop
                // emits CopyToManaged_<name> to propagate the native fills into the user's
                // managed Span<T>.
                if (cat == ParameterCategory.FillArray)
                {
                    continue;
                }

                string elementProjected = TypedefNameWriter.WriteProjectionType(context, TypeSemanticsFactory.Get(szArr.BaseType));
                string elementInteropArg = InteropTypeNameWriter.EncodeInteropTypeName(szArr.BaseType, TypedefNameType.Projected);

                _ = elementInteropArg;
                // For mapped value types (DateTime/TimeSpan) and complex structs, the storage
                // element is the ABI struct type; the data pointer parameter type uses that
                // ABI struct. The fixed() opens with void* (per truth's pattern), so a cast
                // is required at the call site. For runtime classes/objects, use void**.
                string dataParamType;
                string dataCastType;

                if (context.AbiTypeShapeResolver.IsMappedAbiValueType(szArr.BaseType))
                {
                    dataParamType = AbiTypeHelpers.GetMappedAbiTypeName(szArr.BaseType) + "*";
                    dataCastType = "(" + AbiTypeHelpers.GetMappedAbiTypeName(szArr.BaseType) + "*)";
                }
                else if (szArr.BaseType.IsHResultException())
                {
                    dataParamType = "global::ABI.System.Exception*";
                    dataCastType = "(global::ABI.System.Exception*)";
                }
                else if (context.AbiTypeShapeResolver.IsComplexStruct(szArr.BaseType))
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

                writer.WriteLine(isMultiline: true, $$"""
                    {{callIndent}}[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "CopyToUnmanaged")]
                    {{callIndent}}static extern void CopyToUnmanaged_{{localName}}([UnsafeAccessorType("{{ArrayElementEncoder.GetArrayMarshallerInteropPath(szArr.BaseType)}}")] object _, ReadOnlySpan<{{elementProjected}}> span, uint length, {{dataParamType}} data);
                    {{callIndent}}CopyToUnmanaged_{{localName}}(null, {{callName}}, (uint){{callName}}.Length, {{dataCastType}}_{{localName}});
                    """);
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

        writer.Write($"{fp}>**)ThisPtr)[{slot.ToString(CultureInfo.InvariantCulture)}](ThisPtr");
        for (int i = 0; i < sig.Parameters.Count; i++)
        {
            ParameterInfo p = sig.Parameters[i];
            ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);

            if (cat is ParameterCategory.PassArray or ParameterCategory.FillArray)
            {
                string callName = AbiTypeHelpers.GetParamName(p, paramNameOverride);
                string localName = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);
                writer.Write(isMultiline: true, $$"""
                    ,
                      (uint){{callName}}.Length, _{{localName}}
                    """);
                continue;
            }

            if (cat == ParameterCategory.Out)
            {
                string localName = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);
                writer.Write(isMultiline: true, $$"""
                    ,
                      &__{{localName}}
                    """);
                continue;
            }

            if (cat == ParameterCategory.ReceiveArray)
            {
                string localName = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);
                writer.Write(isMultiline: true, $$"""
                    ,
                      &__{{localName}}_length, &__{{localName}}_data
                    """);
                continue;
            }

            if (cat == ParameterCategory.Ref)
            {
                string localName = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);
                TypeSignature uRefArg = AbiTypeHelpers.StripByRefAndCustomModifiers(p.Type);

                if (context.AbiTypeShapeResolver.IsComplexStruct(uRefArg))
                {
                    // Complex struct 'in' (Ref) param: pass &__local (the marshaled ABI struct).
                    writer.Write(isMultiline: true, $$"""
                        ,
                          &__{{localName}}
                        """);
                }
                else
                {
                    // 'in T' projected param: pass the pinned pointer.
                    writer.Write(isMultiline: true, $$"""
                        ,
                          _{{localName}}
                        """);
                }
                continue;
            }
            writer.Write(isMultiline: true, """
                ,
                  
                """);
            if (p.Type.IsHResultException())
            {
                writer.Write($"__{AbiTypeHelpers.GetParamLocalName(p, paramNameOverride)}");
            }
            else if (p.Type.IsString())
            {
                writer.Write($"__{AbiTypeHelpers.GetParamLocalName(p, paramNameOverride)}.HString");
            }
            else if (context.AbiTypeShapeResolver.IsRuntimeClassOrInterface(p.Type) || p.Type.IsObject() || p.Type.IsGenericInstance())
            {
                writer.Write($"__{AbiTypeHelpers.GetParamLocalName(p, paramNameOverride)}.GetThisPtrUnsafe()");
            }
            else if (p.Type.IsSystemType())
            {
                // System.Type input: pass the pre-converted ABI Type struct (via the local set up before the call).
                writer.Write($"__{AbiTypeHelpers.GetParamLocalName(p, paramNameOverride)}.ConvertToUnmanagedUnsafe()");
            }
            else if (context.AbiTypeShapeResolver.IsMappedAbiValueType(p.Type))
            {
                // Mapped value-type input: pass the pre-converted ABI local.
                writer.Write($"__{AbiTypeHelpers.GetParamLocalName(p, paramNameOverride)}");
            }
            else if (context.AbiTypeShapeResolver.IsComplexStruct(p.Type))
            {
                // Complex struct input: pass the pre-converted ABI struct local.
                writer.Write($"__{AbiTypeHelpers.GetParamLocalName(p, paramNameOverride)}");
            }
            else if (context.AbiTypeShapeResolver.IsBlittableStruct(p.Type))
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
            writer.Write(isMultiline: true, """
                ,
                  &__retval_length, &__retval_data
                """);
        }
        else if (rt is not null)
        {
            writer.Write(isMultiline: true, """
                ,
                  &__retval
                """);
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
        for (int i = 0; i < sig.Parameters.Count; i++)
        {
            ParameterInfo p = sig.Parameters[i];
            ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);

            if (cat != ParameterCategory.FillArray)
            {
                continue;
            }

            if (p.Type is not SzArrayTypeSignature szFA)
            {
                continue;
            }

            if (context.AbiTypeShapeResolver.IsBlittablePrimitive(szFA.BaseType) || context.AbiTypeShapeResolver.IsBlittableStruct(szFA.BaseType))
            {
                continue;
            }

            string callName = AbiTypeHelpers.GetParamName(p, paramNameOverride);
            string localName = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);
            string elementProjected = TypedefNameWriter.WriteProjectionType(context, TypeSemanticsFactory.Get(szFA.BaseType));
            string elementInteropArg = InteropTypeNameWriter.EncodeInteropTypeName(szFA.BaseType, TypedefNameType.Projected);

            _ = elementInteropArg;
            // Determine the ABI element type for the data pointer parameter.
            // - Strings / runtime classes / objects: void**
            // - HResult exception: global::ABI.System.Exception*
            // - Mapped value types: global::ABI.System.{DateTimeOffset|TimeSpan}*
            // - Complex structs: <ABI struct>*
            string dataParamType;
            string dataCastType;

            if (szFA.BaseType.IsString() || context.AbiTypeShapeResolver.IsRuntimeClassOrInterface(szFA.BaseType) || szFA.BaseType.IsObject())
            {
                dataParamType = "void** data";
                dataCastType = "(void**)";
            }
            else if (szFA.BaseType.IsHResultException())
            {
                dataParamType = "global::ABI.System.Exception* data";
                dataCastType = "(global::ABI.System.Exception*)";
            }
            else if (context.AbiTypeShapeResolver.IsMappedAbiValueType(szFA.BaseType))
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

            writer.WriteLine(isMultiline: true, $$"""
                {{callIndent}}[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "CopyToManaged")]
                {{callIndent}}static extern void CopyToManaged_{{localName}}([UnsafeAccessorType("{{ArrayElementEncoder.GetArrayMarshallerInteropPath(szFA.BaseType)}}")] object _, uint length, {{dataParamType}}, Span<{{elementProjected}}> span);
                {{callIndent}}CopyToManaged_{{localName}}(null, (uint)__{{localName}}_span.Length, {{dataCastType}}_{{localName}}, {{callName}});
                """);
        }

        // After call: write back Out params to caller's 'out' var.
        for (int i = 0; i < sig.Parameters.Count; i++)
        {
            ParameterInfo p = sig.Parameters[i];
            ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);

            if (cat != ParameterCategory.Out)
            {
                continue;
            }

            string callName = AbiTypeHelpers.GetParamName(p, paramNameOverride);
            string localName = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);
            TypeSignature uOut = AbiTypeHelpers.StripByRefAndCustomModifiers(p.Type);

            // For Out generic instance: emit inline UnsafeAccessor to ConvertToManaged_<name>
            // before the writeback. (e.g. Collection1HandlerInvoke
            // emits the accessor inside try, right before the assignment).
            if (uOut.IsGenericInstance())
            {
                string interopTypeName = InteropTypeNameWriter.EncodeInteropTypeName(uOut, TypedefNameType.ABI) + ", WinRT.Interop";
                string projectedTypeName = MethodFactory.WriteProjectedSignature(context, uOut, false);
                writer.WriteLine(isMultiline: true, $$"""
                    {{callIndent}}[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "ConvertToManaged")]
                    {{callIndent}}static extern {{projectedTypeName}} ConvertToManaged_{{localName}}([UnsafeAccessorType("{{interopTypeName}}")] object _, void* value);
                    {{callIndent}}{{callName}} = ConvertToManaged_{{localName}}(null, __{{localName}});
                    """);
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
            else if (context.AbiTypeShapeResolver.IsRuntimeClassOrInterface(uOut))
            {
                writer.Write($"{AbiTypeHelpers.GetMarshallerFullName(writer, context, uOut)}.ConvertToManaged(__{localName})");
            }
            else if (uOut.IsSystemType())
            {
                writer.Write($"global::ABI.System.TypeMarshaller.ConvertToManaged(__{localName})");
            }
            else if (context.AbiTypeShapeResolver.IsComplexStruct(uOut))
            {
                writer.Write($"{AbiTypeHelpers.GetMarshallerFullName(writer, context, uOut)}.ConvertToManaged(__{localName})");
            }
            else if (context.AbiTypeShapeResolver.IsBlittableStruct(uOut))
            {
                writer.Write($"__{localName}");
            }
            else if (uOut is CorLibTypeSignature corlibBool && corlibBool.ElementType == ElementType.Boolean)
            {
                writer.Write($"__{localName}");
            }
            else if (uOut is CorLibTypeSignature corlibChar && corlibChar.ElementType == ElementType.Char)
            {
                writer.Write($"__{localName}");
            }
            else if (context.AbiTypeShapeResolver.IsEnumType(uOut))
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
        for (int i = 0; i < sig.Parameters.Count; i++)
        {
            ParameterInfo p = sig.Parameters[i];
            ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);

            if (cat != ParameterCategory.ReceiveArray)
            {
                continue;
            }

            string callName = AbiTypeHelpers.GetParamName(p, paramNameOverride);
            string localName = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);
            SzArrayTypeSignature sza = (SzArrayTypeSignature)AbiTypeHelpers.StripByRefAndCustomModifiers(p.Type);
            string elementProjected = TypedefNameWriter.WriteProjectionType(context, TypeSemanticsFactory.Get(sza.BaseType));
            // Element ABI type: void* for ref types (string/runtime class/object); ABI struct
            // type for complex structs (e.g. authored BasicStruct); blittable struct ABI for
            // blittable structs; primitive ABI otherwise.
            string elementAbi = sza.BaseType.IsString() || context.AbiTypeShapeResolver.IsRuntimeClassOrInterface(sza.BaseType) || sza.BaseType.IsObject()
                ? "void*"
                : context.AbiTypeShapeResolver.IsComplexStruct(sza.BaseType)
                    ? AbiTypeHelpers.GetAbiStructTypeName(writer, context, sza.BaseType)
                    : context.AbiTypeShapeResolver.IsBlittableStruct(sza.BaseType)
                        ? AbiTypeHelpers.GetBlittableStructAbiType(writer, context, sza.BaseType)
                        : AbiTypeHelpers.GetAbiPrimitiveType(context.Cache, sza.BaseType);
            string elementInteropArg = InteropTypeNameWriter.EncodeInteropTypeName(sza.BaseType, TypedefNameType.Projected);

            _ = elementInteropArg;
            string marshallerPath = ArrayElementEncoder.GetArrayMarshallerInteropPath(sza.BaseType);
            writer.WriteLine(isMultiline: true, $$"""
                {{callIndent}}[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "ConvertToManaged")]
                {{callIndent}}static extern {{elementProjected}}[] ConvertToManaged_{{localName}}([UnsafeAccessorType("{{marshallerPath}}")] object _, uint length, {{elementAbi}}* data);
                {{callIndent}}{{callName}} = ConvertToManaged_{{localName}}(null, __{{localName}}_length, __{{localName}}_data);
                """);
        }

        if (rt is not null)
        {
            if (returnIsReceiveArray)
            {
                SzArrayTypeSignature retSz = (SzArrayTypeSignature)rt;
                string elementProjected = TypedefNameWriter.WriteProjectionType(context, TypeSemanticsFactory.Get(retSz.BaseType));
                string elementAbi = retSz.BaseType.IsString() || context.AbiTypeShapeResolver.IsRuntimeClassOrInterface(retSz.BaseType) || retSz.BaseType.IsObject()
                    ? "void*"
                    : context.AbiTypeShapeResolver.IsComplexStruct(retSz.BaseType)
                        ? AbiTypeHelpers.GetAbiStructTypeName(writer, context, retSz.BaseType)
                        : retSz.BaseType.IsHResultException()
                            ? "global::ABI.System.Exception"
                            : context.AbiTypeShapeResolver.IsMappedAbiValueType(retSz.BaseType)
                                ? AbiTypeHelpers.GetMappedAbiTypeName(retSz.BaseType)
                                : context.AbiTypeShapeResolver.IsBlittableStruct(retSz.BaseType)
                                    ? AbiTypeHelpers.GetBlittableStructAbiType(writer, context, retSz.BaseType)
                                    : AbiTypeHelpers.GetAbiPrimitiveType(context.Cache, retSz.BaseType);
                string elementInteropArg = InteropTypeNameWriter.EncodeInteropTypeName(retSz.BaseType, TypedefNameType.Projected);

                _ = elementInteropArg;
                writer.WriteLine(isMultiline: true, $$"""
                    {{callIndent}}[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "ConvertToManaged")]
                    {{callIndent}}static extern {{elementProjected}}[] ConvertToManaged_retval([UnsafeAccessorType("{{ArrayElementEncoder.GetArrayMarshallerInteropPath(retSz.BaseType)}}")] object _, uint length, {{elementAbi}}* data);
                    {{callIndent}}return ConvertToManaged_retval(null, __retval_length, __retval_data);
                    """);
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
                    TypeSignature inner = rt.GetNullableInnerType()!;
                    string innerMarshaller = AbiTypeHelpers.GetNullableInnerMarshallerName(writer, context, inner);
                    writer.WriteLine($"{callIndent}return {innerMarshaller}.UnboxToManaged(__retval);");
                }
                else if (rt.IsGenericInstance())
                {
                    string interopTypeName = InteropTypeNameWriter.EncodeInteropTypeName(rt, TypedefNameType.ABI) + ", WinRT.Interop";
                    string projectedTypeName = MethodFactory.WriteProjectedSignature(context, rt, false);
                    writer.WriteLine(isMultiline: true, $$"""
                        {{callIndent}}[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "ConvertToManaged")]
                        {{callIndent}}static extern {{projectedTypeName}} ConvertToManaged_retval([UnsafeAccessorType("{{interopTypeName}}")] object _, void* value);
                        {{callIndent}}return ConvertToManaged_retval(null, __retval);
                        """);
                }
                else
                {
                    writer.Write($"{callIndent}return ");
                    EmitMarshallerConvertToManaged(writer, context, rt, "__retval");
                    writer.WriteLine(";");
                }
            }
            else if (rt is not null && context.AbiTypeShapeResolver.IsMappedAbiValueType(rt))
            {
                // Mapped value type return (e.g. DateTime/TimeSpan): convert ABI struct back via marshaller.
                writer.WriteLine($"{callIndent}return {AbiTypeHelpers.GetMappedMarshallerName(rt)}.ConvertToManaged(__retval);");
            }
            else if (rt is not null && rt.IsSystemType())
            {
                // System.Type return: convert ABI Type struct back to System.Type via TypeMarshaller.
                writer.WriteLine($"{callIndent}return global::ABI.System.TypeMarshaller.ConvertToManaged(__retval);");
            }
            else if (returnIsBlittableStruct)
            {
                writer.Write(callIndent);

                if (rt is not null && context.AbiTypeShapeResolver.IsMappedAbiValueType(rt))
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
                string projected = MethodFactory.WriteProjectedSignature(context, rt!, false);
                string abiType = AbiTypeHelpers.GetAbiPrimitiveType(context.Cache, rt!);

                if (projected == abiType)
                {
                    writer.WriteLine("__retval;");
                }
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
            writer.WriteLine(isMultiline: true, """
                        }
                        finally
                        {
                """);

            // Order matches truth:
            // 0. Complex-struct input param Dispose (e.g. ProfileUsageMarshaller.Dispose(__value))
            // 1. Non-blittable PassArray/FillArray cleanup (Dispose + ArrayPools)
            // 2. Out param frees (HString / object / runtime class)
            // 3. ReceiveArray param frees (Free_<name> via UnsafeAccessor)
            // 4. Return free (__retval) — last

            // 0. Dispose complex-struct input params via marshaller (both 'in' and 'in T' forms).
            for (int i = 0; i < sig.Parameters.Count; i++)
            {
                ParameterInfo p = sig.Parameters[i];
                ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);

                if (cat is not (ParameterCategory.In or ParameterCategory.Ref))
                {
                    continue;
                }

                TypeSignature pType = AbiTypeHelpers.StripByRefAndCustomModifiers(p.Type);

                if (!context.AbiTypeShapeResolver.IsComplexStruct(pType))
                {
                    continue;
                }

                string localName = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);
                writer.WriteLine($"            {AbiTypeHelpers.GetMarshallerFullName(writer, context, pType)}.Dispose(__{localName});");
            }

            // 1. Cleanup non-blittable PassArray/FillArray params:
            // For strings: HStringArrayMarshaller.Dispose + return ArrayPools (3 of them).
            // For runtime classes/objects: Dispose_<name> (UnsafeAccessor) + return ArrayPool.
            // For mapped value types (DateTime/TimeSpan): no per-element disposal needed and truth
            // doesn't return the ArrayPool either, so skip entirely.
            for (int i = 0; i < sig.Parameters.Count; i++)
            {
                ParameterInfo p = sig.Parameters[i];
                ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);

                if (cat is not (ParameterCategory.PassArray or ParameterCategory.FillArray))
                {
                    continue;
                }

                if (p.Type is not SzArrayTypeSignature szArr)
                {
                    continue;
                }

                if (context.AbiTypeShapeResolver.IsBlittablePrimitive(szArr.BaseType) || context.AbiTypeShapeResolver.IsBlittableStruct(szArr.BaseType))
                {
                    continue;
                }

                if (context.AbiTypeShapeResolver.IsMappedAbiValueType(szArr.BaseType))
                {
                    continue;
                }

                if (szArr.BaseType.IsHResultException())
                {
                    // HResultException ABI is just an int; per-element Dispose is a no-op (mirror
                    // the truth: no Dispose_<name> emitted). Just return the inline-array's pool
                    // using the correct element type (ABI.System.Exception, not nint).
                    string localNameH = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);
                    writer.WriteLine();
                    writer.WriteLine(isMultiline: true, $$"""
                                    if (__{{localNameH}}_arrayFromPool is not null)
                                    {
                                        global::System.Buffers.ArrayPool<global::ABI.System.Exception>.Shared.Return(__{{localNameH}}_arrayFromPool);
                                    }
                        """);
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
                        writer.WriteLine(isMultiline: true, $$"""
                                        HStringArrayMarshaller.Dispose(__{{localName}}_pinnedHandleSpan);
                            
                                        if (__{{localName}}_pinnedHandleArrayFromPool is not null)
                                        {
                                            global::System.Buffers.ArrayPool<nint>.Shared.Return(__{{localName}}_pinnedHandleArrayFromPool);
                                        }
                            
                                        if (__{{localName}}_headerArrayFromPool is not null)
                                        {
                                            global::System.Buffers.ArrayPool<HStringHeader>.Shared.Return(__{{localName}}_headerArrayFromPool);
                                        }
                            """);
                    }

                    // Both PassArray and FillArray need the inline-array's nint pool returned.
                    writer.WriteLine();
                    writer.WriteLine(isMultiline: true, $$"""
                                    if (__{{localName}}_arrayFromPool is not null)
                                    {
                                        global::System.Buffers.ArrayPool<nint>.Shared.Return(__{{localName}}_arrayFromPool);
                                    }
                        """);
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

                    if (context.AbiTypeShapeResolver.IsComplexStruct(szArr.BaseType))
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
                    writer.WriteLine(isMultiline: true, $$"""
                                    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "Dispose")]
                                    static extern void Dispose_{{localName}}([UnsafeAccessorType("{{ArrayElementEncoder.GetArrayMarshallerInteropPath(szArr.BaseType)}}")] object _, uint length, {{disposeDataParamType}}
                        """);
                    writer.WriteIf(!disposeDataParamType.EndsWith("data", StringComparison.Ordinal), " data");

                    writer.WriteLine(isMultiline: true, $$"""
                        );
                        
                                    fixed({{fixedPtrType}} _{{localName}} = __{{localName}}_span)
                                    {
                                        Dispose_{{localName}}(null, (uint) __{{localName}}_span.Length, {{disposeCastType}}_{{localName}});
                                    }
                        """);
                }

                // ArrayPool storage type matches the InlineArray storage (mapped ABI value type
                // for DateTime/TimeSpan; ABI struct for complex structs; nint otherwise).
                string poolStorageT = context.AbiTypeShapeResolver.IsMappedAbiValueType(szArr.BaseType)
                    ? AbiTypeHelpers.GetMappedAbiTypeName(szArr.BaseType)
                    : context.AbiTypeShapeResolver.IsComplexStruct(szArr.BaseType)
                        ? AbiTypeHelpers.GetAbiStructTypeName(writer, context, szArr.BaseType)
                        : "nint";
                writer.WriteLine();
                writer.WriteLine(isMultiline: true, $$"""
                                if (__{{localName}}_arrayFromPool is not null)
                                {
                                    global::System.Buffers.ArrayPool<{{poolStorageT}}>.Shared.Return(__{{localName}}_arrayFromPool);
                                }
                    """);
            }

            // 2. Free Out string/object/runtime-class params.
            for (int i = 0; i < sig.Parameters.Count; i++)
            {
                ParameterInfo p = sig.Parameters[i];
                ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);

                if (cat != ParameterCategory.Out)
                {
                    continue;
                }

                TypeSignature uOut = AbiTypeHelpers.StripByRefAndCustomModifiers(p.Type);
                string localName = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);

                if (uOut.IsString())
                {
                    writer.WriteLine($"            HStringMarshaller.Free(__{localName});");
                }
                else if (uOut.IsObject() || context.AbiTypeShapeResolver.IsRuntimeClassOrInterface(uOut) || uOut.IsGenericInstance())
                {
                    writer.WriteLine($"            WindowsRuntimeUnknownMarshaller.Free(__{localName});");
                }
                else if (uOut.IsSystemType())
                {
                    writer.WriteLine($"            global::ABI.System.TypeMarshaller.Dispose(__{localName});");
                }
                else if (context.AbiTypeShapeResolver.IsComplexStruct(uOut))
                {
                    writer.WriteLine($"            {AbiTypeHelpers.GetMarshallerFullName(writer, context, uOut)}.Dispose(__{localName});");
                }
            }

            // 3. Free ReceiveArray params via UnsafeAccessor.
            for (int i = 0; i < sig.Parameters.Count; i++)
            {
                ParameterInfo p = sig.Parameters[i];
                ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);

                if (cat != ParameterCategory.ReceiveArray)
                {
                    continue;
                }

                string localName = AbiTypeHelpers.GetParamLocalName(p, paramNameOverride);
                SzArrayTypeSignature sza = (SzArrayTypeSignature)AbiTypeHelpers.StripByRefAndCustomModifiers(p.Type);
                // Element ABI type: void* for ref types; ABI struct for complex/blittable structs;
                // primitive ABI otherwise. (Same categorization as the ConvertToManaged_<name> path.)
                string elementAbi = sza.BaseType.IsString() || context.AbiTypeShapeResolver.IsRuntimeClassOrInterface(sza.BaseType) || sza.BaseType.IsObject()
                    ? "void*"
                    : context.AbiTypeShapeResolver.IsComplexStruct(sza.BaseType)
                        ? AbiTypeHelpers.GetAbiStructTypeName(writer, context, sza.BaseType)
                        : context.AbiTypeShapeResolver.IsBlittableStruct(sza.BaseType)
                            ? AbiTypeHelpers.GetBlittableStructAbiType(writer, context, sza.BaseType)
                            : AbiTypeHelpers.GetAbiPrimitiveType(context.Cache, sza.BaseType);
                string elementInteropArg = InteropTypeNameWriter.EncodeInteropTypeName(sza.BaseType, TypedefNameType.Projected);

                _ = elementInteropArg;
                string marshallerPath = ArrayElementEncoder.GetArrayMarshallerInteropPath(sza.BaseType);
                writer.WriteLine(isMultiline: true, $$"""
                                [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "Free")]
                                static extern void Free_{{localName}}([UnsafeAccessorType("{{marshallerPath}}")] object _, uint length, {{elementAbi}}* data);
                    
                                Free_{{localName}}(null, __{{localName}}_length, __{{localName}}_data);
                    """);
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
                SzArrayTypeSignature retSz = (SzArrayTypeSignature)rt!;
                string elementAbi = retSz.BaseType.IsString() || context.AbiTypeShapeResolver.IsRuntimeClassOrInterface(retSz.BaseType) || retSz.BaseType.IsObject()
                    ? "void*"
                    : context.AbiTypeShapeResolver.IsComplexStruct(retSz.BaseType)
                        ? AbiTypeHelpers.GetAbiStructTypeName(writer, context, retSz.BaseType)
                        : retSz.BaseType.IsHResultException()
                            ? "global::ABI.System.Exception"
                            : context.AbiTypeShapeResolver.IsMappedAbiValueType(retSz.BaseType)
                                ? AbiTypeHelpers.GetMappedAbiTypeName(retSz.BaseType)
                                : context.AbiTypeShapeResolver.IsBlittableStruct(retSz.BaseType)
                                    ? AbiTypeHelpers.GetBlittableStructAbiType(writer, context, retSz.BaseType)
                                    : AbiTypeHelpers.GetAbiPrimitiveType(context.Cache, retSz.BaseType);
                string elementInteropArg = InteropTypeNameWriter.EncodeInteropTypeName(retSz.BaseType, TypedefNameType.Projected);

                _ = elementInteropArg;
                writer.WriteLine(isMultiline: true, $$"""
                                [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "Free")]
                                static extern void Free_retval([UnsafeAccessorType("{{ArrayElementEncoder.GetArrayMarshallerInteropPath(retSz.BaseType)}}")] object _, uint length, {{elementAbi}}* data);
                                Free_retval(null, __retval_length, __retval_data);
                    """);
            }

            writer.WriteLine("        }");
        }

        writer.WriteLine("    }");
    }

    /// <summary>
    /// Emits the conversion of a parameter from its projected (managed) form to the ABI argument form.
    /// </summary>
    internal static void EmitParamArgConversion(IndentedTextWriter writer, ProjectionEmitContext context, ParameterInfo p, string? paramNameOverride = null)
    {
        string pname = paramNameOverride ?? p.Parameter.Name ?? "param";
        // bool: ABI is 'bool' directly; pass as-is.
        if (p.Type is CorLibTypeSignature corlib &&
            corlib.ElementType == ElementType.Boolean)
        {
            writer.Write(pname);
        }

        // char: ABI is 'char' directly; pass as-is.
        else if (p.Type is CorLibTypeSignature corlib2 &&
                 corlib2.ElementType == ElementType.Char)
        {
            writer.Write(pname);
        }

        // Enums: function pointer signature uses the projected enum type, so pass directly.
        else if (context.AbiTypeShapeResolver.IsEnumType(p.Type))
        {
            writer.Write(pname);
        }
        else
        {
            writer.Write(pname);
        }
    }
}
