// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.ProjectionWriter.Factories.Callbacks;
using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Helpers;
using WindowsRuntime.ProjectionWriter.Metadata;
using WindowsRuntime.ProjectionWriter.Models;
using WindowsRuntime.ProjectionWriter.References;
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

        MethodSignatureMarshallingFacts facts = MethodSignatureMarshallingFacts.From(sig, context.AbiTypeKindResolver);

        bool returnIsString = facts.ReturnIsString;
        bool returnIsRefType = facts.ReturnIsRefType;
        bool returnIsBlittableStruct = facts.ReturnIsBlittableStruct;
        bool returnIsNonBlittableStruct = facts.ReturnIsNonBlittableStruct;
        bool returnIsReceiveArray = facts.ReturnIsReceiveArray;
        bool returnIsHResultException = facts.ReturnIsHResultException;

        // Build the function pointer signature: void*, [paramAbiType...,] [retAbiType*,] int
        StringBuilder fp = new();
        _ = fp.Append("void*");
        foreach (ParameterInfo p in sig.Parameters)
        {
            ParameterCategory cat = ParameterCategoryResolver.Resolve(p);

            if (cat.IsArrayInput())
            {
                _ = fp.Append(", uint, void*");
                continue;
            }

            if (cat == ParameterCategory.Out)
            {
                TypeSignature uOut = p.Type.StripByRefAndCustomModifiers();
                _ = fp.Append(", ");

                if (uOut.IsAbiRefLike(context.AbiTypeKindResolver))
                {
                    _ = fp.Append("void**");
                }
                else if (uOut.IsSystemType())
                {
                    _ = fp.Append(WellKnownAbiTypeNames.AbiSystemTypePointer);
                }
                else if (context.AbiTypeKindResolver.IsNonBlittableStruct(uOut))
                {
                    _ = fp.Append(AbiTypeHelpers.GetAbiStructTypeName(context, uOut)); _ = fp.Append('*');
                }
                else if (context.AbiTypeKindResolver.IsBlittableStruct(uOut))
                {
                    _ = fp.Append(AbiTypeHelpers.GetBlittableStructAbiType(context, uOut)); _ = fp.Append('*');
                }
                else
                {
                    _ = fp.Append(AbiTypeHelpers.GetAbiPrimitiveType(context.Cache, uOut)); _ = fp.Append('*');
                }

                continue;
            }

            if (cat == ParameterCategory.Ref)
            {
                TypeSignature uRef = p.Type.StripByRefAndCustomModifiers();
                _ = fp.Append(", ");

                if (context.AbiTypeKindResolver.IsNonBlittableStruct(uRef))
                {
                    _ = fp.Append(AbiTypeHelpers.GetAbiStructTypeName(context, uRef)); _ = fp.Append('*');
                }
                else if (context.AbiTypeKindResolver.IsBlittableStruct(uRef))
                {
                    _ = fp.Append(AbiTypeHelpers.GetBlittableStructAbiType(context, uRef)); _ = fp.Append('*');
                }
                else
                {
                    _ = fp.Append(AbiTypeHelpers.GetAbiPrimitiveType(context.Cache, uRef)); _ = fp.Append('*');
                }

                continue;
            }

            if (cat == ParameterCategory.ReceiveArray)
            {
                SzArrayTypeSignature sza = p.Type.AsSzArray()!;
                _ = fp.Append(", uint*, ");
                _ = fp.Append(AbiTypeHelpers.GetArrayElementAbiType(context, sza.BaseType));
                _ = fp.Append("**");
                continue;
            }

            _ = fp.Append(", ");

            if (p.Type.IsHResultException())
            {
                _ = fp.Append(WellKnownAbiTypeNames.AbiSystemException);
            }
            else if (p.Type.IsAbiRefLike(context.AbiTypeKindResolver))
            {
                _ = fp.Append("void*");
            }
            else if (p.Type.IsSystemType())
            {
                _ = fp.Append(WellKnownAbiTypeNames.AbiSystemType);
            }
            else if (context.AbiTypeKindResolver.IsBlittableStruct(p.Type))
            {
                _ = fp.Append(AbiTypeHelpers.GetBlittableStructAbiType(context, p.Type));
            }
            else if (context.AbiTypeKindResolver.IsMappedAbiValueType(p.Type))
            {
                _ = fp.Append(AbiTypeHelpers.GetMappedAbiTypeName(p.Type));
            }
            else
            {
                _ = fp.Append(context.AbiTypeKindResolver.IsNonBlittableStruct(p.Type)
                    ? AbiTypeHelpers.GetAbiStructTypeName(context, p.Type)
                    : AbiTypeHelpers.GetAbiPrimitiveType(context.Cache, p.Type));
            }
        }

        if (rt is not null)
        {
            if (returnIsReceiveArray)
            {
                SzArrayTypeSignature retSz = (SzArrayTypeSignature)rt;
                _ = fp.Append(", uint*, ");
                _ = fp.Append(AbiTypeHelpers.GetArrayElementAbiType(context, retSz.BaseType));
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
                    _ = fp.Append(WellKnownAbiTypeNames.AbiSystemTypePointer);
                }
                else if (returnIsBlittableStruct)
                {
                    _ = fp.Append(AbiTypeHelpers.GetBlittableStructAbiType(context, rt!)); _ = fp.Append('*');
                }
                else if (returnIsNonBlittableStruct)
                {
                    _ = fp.Append(AbiTypeHelpers.GetAbiStructTypeName(context, rt!)); _ = fp.Append('*');
                }
                else if (rt is not null && context.AbiTypeKindResolver.IsMappedAbiValueType(rt))
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
        writer.IncreaseIndent();
        writer.WriteLine(isMultiline: true, """
            {
                using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();
                void* ThisPtr = thisValue.GetThisPtrUnsafe();
            """);
        writer.IncreaseIndent();

        // Declare 'using' marshaller values for ref-type parameters (these need disposing).
        for (int i = 0; i < sig.Parameters.Count; i++)
        {
            ParameterInfo p = sig.Parameters[i];

            if (context.AbiTypeKindResolver.IsRuntimeClassOrInterface(p.Type) || p.Type.IsObject())
            {
                string localName = p.GetParamLocalName(paramNameOverride);
                string callName = p.GetParamName(paramNameOverride);
                EmitMarshallerConvertToUnmanagedCallback cvt = EmitMarshallerConvertToUnmanaged(context, p.Type, callName);
                writer.WriteLine($"using WindowsRuntimeObjectReferenceValue __{localName} = {cvt};");
            }
            else if (p.Type.IsNullableT())
            {
                // Nullable<T> param: use <T>Marshaller.BoxToUnmanaged.
                string localName = p.GetParamLocalName(paramNameOverride);
                string callName = p.GetParamName(paramNameOverride);
                (_, string innerMarshaller) = AbiTypeHelpers.GetNullableInnerInfo(writer, context, p.Type);
                writer.WriteLine($"using WindowsRuntimeObjectReferenceValue __{localName} = {innerMarshaller}.BoxToUnmanaged({callName});");
            }
            else if (p.Type.IsGenericInstance())
            {
                // Generic instance param: emit a local UnsafeAccessor delegate to get the marshaller method.
                string localName = p.GetParamLocalName(paramNameOverride);
                string callName = p.GetParamName(paramNameOverride);
                string interopTypeName = InteropTypeNameWriter.GetInteropAssemblyQualifiedName(p.Type, TypedefNameType.ABI);
                WriteProjectedSignatureCallback projectedTypeName = MethodFactory.WriteProjectedSignature(context, p.Type, false);
                UnsafeAccessorFactory.EmitStaticMethod(
                    writer,
                    accessName: "ConvertToUnmanaged",
                    returnType: "WindowsRuntimeObjectReferenceValue",
                    functionName: $"ConvertToUnmanaged_{localName}",
                    interopType: interopTypeName,
                    parameterList: $", {projectedTypeName.Format()} value");
                writer.WriteLine($"using WindowsRuntimeObjectReferenceValue __{localName} = ConvertToUnmanaged_{localName}(null, {callName});");
            }
        }

        // (String input params are now stack-allocated via the fast-path pinning pattern below;
        //  no separate void* local declaration or up-front allocation is needed.)
        // Declare locals for HResult/Exception input parameters (converted up-front).
        for (int i = 0; i < sig.Parameters.Count; i++)
        {
            ParameterInfo p = sig.Parameters[i];

            if (ParameterCategoryResolver.Resolve(p) != ParameterCategory.In)
            {
                continue;
            }

            if (!p.Type.IsHResultException())
            {
                continue;
            }

            string localName = p.GetParamLocalName(paramNameOverride);
            string callName = p.GetParamName(paramNameOverride);
            writer.WriteLine($"global::ABI.System.Exception __{localName} = global::ABI.System.ExceptionMarshaller.ConvertToUnmanaged({callName});");
        }

        // Declare locals for mapped value-type input parameters (DateTime/TimeSpan): convert via marshaller up-front.
        for (int i = 0; i < sig.Parameters.Count; i++)
        {
            ParameterInfo p = sig.Parameters[i];

            if (ParameterCategoryResolver.Resolve(p) != ParameterCategory.In)
            {
                continue;
            }

            if (!context.AbiTypeKindResolver.IsMappedAbiValueType(p.Type))
            {
                continue;
            }

            string localName = p.GetParamLocalName(paramNameOverride);
            string callName = p.GetParamName(paramNameOverride);
            writer.WriteLine($"{AbiTypeHelpers.GetMappedAbiTypeName(p.Type)} __{localName} = {AbiTypeHelpers.GetMappedMarshallerName(p.Type)}.ConvertToUnmanaged({callName});");
        }

        // Declare locals for complex-struct input parameters (e.g. ProfileUsage with nested
        // string/Nullable fields): default-initialize OUTSIDE try, assign inside try via marshaller,
        // dispose in finally.
        // Includes both 'in' (ParameterCategory.In) and 'in T' (ParameterCategory.Ref) forms.
        for (int i = 0; i < sig.Parameters.Count; i++)
        {
            ParameterInfo p = sig.Parameters[i];
            ParameterCategory cat = ParameterCategoryResolver.Resolve(p);

            if (!cat.IsScalarInput())
            {
                continue;
            }

            TypeSignature pType = p.Type.StripByRefAndCustomModifiers();

            if (!context.AbiTypeKindResolver.IsNonBlittableStruct(pType))
            {
                continue;
            }

            string localName = p.GetParamLocalName(paramNameOverride);
            writer.WriteLine($"{AbiTypeHelpers.GetAbiStructTypeName(context, pType)} __{localName} = default;");
        }

        // Declare locals for Out parameters (need to be passed as &__<name> to the call).
        foreach ((_, ParameterInfo p) in sig.ParametersByCategory(ParameterCategory.Out))

        {

            string localName = p.GetParamLocalName(paramNameOverride);
            TypeSignature uOut = p.Type.StripByRefAndCustomModifiers();
            string abi = AbiTypeHelpers.GetAbiLocalTypeName(context, uOut);
            writer.WriteLine($"{abi} __{localName} = default;");
        }

        // Declare locals for ReceiveArray params (uint length + element pointer).
        foreach ((_, ParameterInfo p) in sig.ParametersByCategory(ParameterCategory.ReceiveArray))

        {

            string localName = p.GetParamLocalName(paramNameOverride);
            SzArrayTypeSignature sza = p.Type.AsSzArray()!;
            writer.WriteLine($"uint __{localName}_length = default;");
            writer.WriteLine();

            // Element ABI type: void* for ref types; ABI struct for complex/blittable structs;
            // primitive ABI otherwise.
            string elemAbi = AbiTypeHelpers.GetAbiLocalTypeName(context, sza.BaseType);
            writer.WriteLine($"{elemAbi}* __{localName}_data = default;");
        }

        // Declare InlineArray16 + ArrayPool fallback for non-blittable PassArray params
        // (runtime classes, objects, strings). Runtime class/object: just one InlineArray16<nint>.
        // String: also needs InlineArray16<HStringHeader> + InlineArray16<nint> for pinned handles.
        for (int i = 0; i < sig.Parameters.Count; i++)
        {
            ParameterInfo p = sig.Parameters[i];
            ParameterCategory cat = ParameterCategoryResolver.Resolve(p);

            if (!cat.IsArrayInput())
            {
                continue;
            }

            if (p.Type is not SzArrayTypeSignature szArr)
            {
                continue;
            }

            if (context.AbiTypeKindResolver.IsBlittableAbiElement(szArr.BaseType))
            {
                continue;
            }

            // Non-blittable element type: emit InlineArray16<storageT> + ArrayPool<storageT>.
            // For mapped value types (DateTime/TimeSpan), use the ABI struct type.
            // For complex structs (e.g. authored BasicStruct with reference fields), use the ABI
            // struct type. For everything else (runtime classes, objects, strings), use nint.
            string localName = p.GetParamLocalName(paramNameOverride);
            string callName = p.GetParamName(paramNameOverride);
            ArrayTempNames names = new(localName);
            string storageT = AbiTypeHelpers.GetArrayElementStorageType(context, szArr.BaseType);
            writer.WriteLine();
            writer.WriteLine(isMultiline: true, $$"""
                Unsafe.SkipInit(out InlineArray16<{{storageT}}> {{names.InlineArray}});
                {{storageT}}[] {{names.ArrayFromPool}} = null;
                Span<{{storageT}}> {{names.Span}} = {{callName}}.Length <= 16
                    ? {{names.InlineArray}}[..{{callName}}.Length]
                    : ({{names.ArrayFromPool}} = global::System.Buffers.ArrayPool<{{storageT}}>.Shared.Rent({{callName}}.Length));
                """);

            if (szArr.BaseType.IsString() && cat == ParameterCategory.PassArray)
            {
                // Strings need an additional InlineArray16<HStringHeader> + InlineArray16<nint> (pinned handles).
                // Only required for PassArray (managed -> HSTRING conversion); FillArray's native side
                // fills HSTRING handles directly into the nint storage.
                writer.WriteLine();
                writer.WriteLine(isMultiline: true, $$"""
                    Unsafe.SkipInit(out InlineArray16<HStringHeader> {{names.InlineHeaderArray}});
                    HStringHeader[] {{names.HeaderArrayFromPool}} = null;
                    Span<HStringHeader> {{names.HeaderSpan}} = {{callName}}.Length <= 16
                        ? {{names.InlineHeaderArray}}[..{{callName}}.Length]
                        : ({{names.HeaderArrayFromPool}} = global::System.Buffers.ArrayPool<HStringHeader>.Shared.Rent({{callName}}.Length));
                    
                    Unsafe.SkipInit(out InlineArray16<nint> {{names.InlinePinnedHandleArray}});
                    nint[] {{names.PinnedHandleArrayFromPool}} = null;
                    Span<nint> {{names.PinnedHandleSpan}} = {{callName}}.Length <= 16
                        ? {{names.InlinePinnedHandleArray}}[..{{callName}}.Length]
                        : ({{names.PinnedHandleArrayFromPool}} = global::System.Buffers.ArrayPool<nint>.Shared.Rent({{callName}}.Length));
                    """);
            }
        }

        if (returnIsReceiveArray)
        {
            SzArrayTypeSignature retSz = (SzArrayTypeSignature)rt!;
            writer.WriteLine("uint __retval_length = default;");
            writer.WriteLine();
            string retElemAbi = AbiTypeHelpers.GetAbiLocalTypeName(context, retSz.BaseType);
            writer.WriteLine($"{retElemAbi}* __retval_data = default;");
        }
        else if (returnIsHResultException)
        {
            writer.WriteLine("global::ABI.System.Exception __retval = default;");
        }
        else if (returnIsString || returnIsRefType)
        {
            writer.WriteLine("void* __retval = default;");
        }
        else if (returnIsBlittableStruct)
        {
            writer.WriteLine($"{AbiTypeHelpers.GetBlittableStructAbiType(context, rt!)} __retval = default;");
        }
        else if (returnIsNonBlittableStruct)
        {
            writer.WriteLine($"{AbiTypeHelpers.GetAbiStructTypeName(context, rt!)} __retval = default;");
        }
        else if (rt is not null && context.AbiTypeKindResolver.IsMappedAbiValueType(rt))
        {
            // Mapped value type return (e.g. DateTime/TimeSpan): use the ABI struct as __retval.
            writer.WriteLine($"{AbiTypeHelpers.GetMappedAbiTypeName(rt)} __retval = default;");
        }
        else if (rt is not null && rt.IsSystemType())
        {
            // System.Type return: use ABI Type struct as __retval.
            writer.WriteLine("global::ABI.System.Type __retval = default;");
        }
        else if (rt is not null)
        {
            writer.WriteLine($"{AbiTypeHelpers.GetAbiPrimitiveType(context.Cache, rt)} __retval = default;");
        }

        // Determine if we need a try/finally (for cleanup of string/refType return or receive array
        // return or Out runtime class params). Input string params no longer need try/finally —
        // they use the HString fast-path (stack-allocated HStringReference, no free needed).
        bool needsTryFinally = facts.NeedsTryFinally;

        if (needsTryFinally)
        {
            writer.WriteLine(isMultiline: true, """
                try
                {
                """);
            writer.IncreaseIndent();
        }

        // Inside try (if applicable): assign complex-struct input locals via marshaller.
        //.: '__value = ProfileUsageMarshaller.ConvertToUnmanaged(value);'
        // Includes both 'in' (ParameterCategory.In) and 'in T' (ParameterCategory.Ref) forms.
        for (int i = 0; i < sig.Parameters.Count; i++)
        {
            ParameterInfo p = sig.Parameters[i];
            ParameterCategory cat = ParameterCategoryResolver.Resolve(p);

            if (!cat.IsScalarInput())
            {
                continue;
            }

            TypeSignature pType = p.Type.StripByRefAndCustomModifiers();

            if (!context.AbiTypeKindResolver.IsNonBlittableStruct(pType))
            {
                continue;
            }

            string localName = p.GetParamLocalName(paramNameOverride);
            string callName = p.GetParamName(paramNameOverride);
            writer.WriteLine($"__{localName} = {AbiTypeHelpers.GetMarshallerFullName(writer, context, pType)}.ConvertToUnmanaged({callName});");
        }

        // Type input params: set up TypeReference locals before the fixed block:
        //   global::ABI.System.TypeMarshaller.ConvertToUnmanagedUnsafe(forType, out TypeReference __forType);
        for (int i = 0; i < sig.Parameters.Count; i++)
        {
            ParameterInfo p = sig.Parameters[i];

            if (ParameterCategoryResolver.Resolve(p) != ParameterCategory.In)
            {
                continue;
            }

            if (!p.Type.IsSystemType())
            {
                continue;
            }

            string localName = p.GetParamLocalName(paramNameOverride);
            string callName = p.GetParamName(paramNameOverride);
            writer.WriteLine($"global::ABI.System.TypeMarshaller.ConvertToUnmanagedUnsafe({callName}, out TypeReference __{localName});");
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
            ParameterCategory cat = ParameterCategoryResolver.Resolve(p);

            if (p.Type.IsString() || p.Type.IsSystemType())
            {
                hasAnyVoidStarPinnable = true;
                continue;
            }

            if (cat.IsArrayInput())
            {
                // All PassArrays (including complex structs) go in the void* combined block,
                // matching truth's pattern. Complex structs use a (T*) cast at the call site.
                hasAnyVoidStarPinnable = true;
            }
        }

        // Emit typed fixed lines for Ref params.
        // Skip Ref+NonBlittableStruct: those are marshalled via __local (no fixed needed) and
        // passed as &__local at the call site (the is-value-type-in path).
        int typedFixedCount = 0;
        for (int i = 0; i < sig.Parameters.Count; i++)
        {
            ParameterInfo p = sig.Parameters[i];
            ParameterCategory cat = ParameterCategoryResolver.Resolve(p);

            if (cat == ParameterCategory.Ref)
            {
                TypeSignature uRefSkip = p.Type.StripByRefAndCustomModifiers();

                if (context.AbiTypeKindResolver.IsNonBlittableStruct(uRefSkip))
                {
                    continue;
                }

                string callName = p.GetParamName(paramNameOverride);
                string localName = p.GetParamLocalName(paramNameOverride);
                TypeSignature uRef = uRefSkip;
                string abiType = context.AbiTypeKindResolver.IsBlittableStruct(uRef) ? AbiTypeHelpers.GetBlittableStructAbiType(context, uRef) : AbiTypeHelpers.GetAbiPrimitiveType(context.Cache, uRef);
                writer.WriteLine($"fixed({abiType}* _{localName} = &{callName})");
                typedFixedCount++;
            }
        }

        // Step 2: Emit ONE combined fixed-void* block for all pinnables that share the
        // same scope. Each variable is "_localName = rhsExpr". Strings get an extra
        // "_localName_inlineHeaderArray = __localName_headerSpan" entry.
        bool stringPinnablesEmitted = false;

        if (hasAnyVoidStarPinnable)
        {
            writer.Write("fixed(void* ");
            bool first = true;
            for (int i = 0; i < sig.Parameters.Count; i++)
            {
                ParameterInfo p = sig.Parameters[i];
                ParameterCategory cat = ParameterCategoryResolver.Resolve(p);
                bool isString = p.Type.IsString();
                bool isType = p.Type.IsSystemType();
                bool isPassArray = cat.IsArrayInput();

                if (!isString && !isType && !isPassArray)
                {
                    continue;
                }

                string callName = p.GetParamName(paramNameOverride);
                string localName = p.GetParamLocalName(paramNameOverride);

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
                    bool isBlittableElem = context.AbiTypeKindResolver.IsBlittableAbiElement(elemT);
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
            writer.WriteLine(")");
            writer.WriteLine("{");
            fixedNesting++;
            writer.IncreaseIndent();

            // Inside the body: emit HStringMarshaller calls for input string params.
            for (int i = 0; i < sig.Parameters.Count; i++)
            {
                if (!sig.Parameters[i].Type.IsString())
                {
                    continue;
                }

                string callName = sig.Parameters[i].GetParamName(paramNameOverride);
                string localName = sig.Parameters[i].GetParamLocalName(paramNameOverride);
                writer.WriteLine($"HStringMarshaller.ConvertToUnmanagedUnsafe((char*)_{localName}, {callName}?.Length, out HStringReference __{localName});");
            }
            stringPinnablesEmitted = true;
        }
        else if (typedFixedCount > 0)
        {
            // Typed fixed lines exist but no void* combined block - we need a body block
            // to host them. Open a brace block after the last typed fixed line.
            writer.WriteLine("{");
            fixedNesting++;
            writer.IncreaseIndent();
        }

        // Suppress unused variable warning when block above doesn't fire.
        _ = stringPinnablesEmitted;

        // For non-blittable PassArray params, emit CopyToUnmanaged_<name> (UnsafeAccessor) and call
        // it to populate the inline/pooled storage from the user-supplied span. For string arrays,
        // use HStringArrayMarshaller.ConvertToUnmanagedUnsafe instead.
        // FillArray of strings is the exception: the native side fills the HSTRING handles, so
        // there's nothing to convert pre-call (the post-call CopyToManaged_<name> handles writeback).
        for (int i = 0; i < sig.Parameters.Count; i++)
        {
            ParameterInfo p = sig.Parameters[i];
            ParameterCategory cat = ParameterCategoryResolver.Resolve(p);

            if (!cat.IsArrayInput())
            {
                continue;
            }

            if (p.Type is not SzArrayTypeSignature szArr)
            {
                continue;
            }

            if (context.AbiTypeKindResolver.IsBlittableAbiElement(szArr.BaseType))
            {
                continue;
            }

            string callName = p.GetParamName(paramNameOverride);
            string localName = p.GetParamLocalName(paramNameOverride);
            ArrayTempNames names = new(localName);

            if (szArr.BaseType.IsString())
            {
                // Skip pre-call ConvertToUnmanagedUnsafe for FillArray of strings — there's
                // nothing to convert (native fills the handles).
                if (cat == ParameterCategory.FillArray)
                {
                    continue;
                }

                writer.WriteLine(isMultiline: true, $$"""
                    HStringArrayMarshaller.ConvertToUnmanagedUnsafe(
                        source: {{callName}},
                        hstringHeaders: (HStringHeader*) _{{localName}}_inlineHeaderArray,
                        hstrings: {{names.Span}},
                        pinnedGCHandles: {{names.PinnedHandleSpan}});
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

                WriteProjectionTypeCallback elementProjected = TypedefNameWriter.WriteProjectionType(context, TypeSemanticsFactory.Get(szArr.BaseType));

                // For mapped value types (DateTime/TimeSpan) and complex structs, the storage
                // element is the ABI struct type; the data pointer parameter type uses that
                // ABI struct. The fixed() opens with void* (per truth's pattern), so a cast
                // is required at the call site. For runtime classes/objects, use void**.
                string dataParamType;
                string dataCastType;

                if (context.AbiTypeKindResolver.IsMappedAbiValueType(szArr.BaseType))
                {
                    dataParamType = AbiTypeHelpers.GetMappedAbiTypeName(szArr.BaseType) + "*";
                    dataCastType = "(" + AbiTypeHelpers.GetMappedAbiTypeName(szArr.BaseType) + "*)";
                }
                else if (szArr.BaseType.IsHResultException())
                {
                    dataParamType = WellKnownAbiTypeNames.AbiSystemExceptionPointer;
                    dataCastType = "(global::ABI.System.Exception*)";
                }
                else if (context.AbiTypeKindResolver.IsNonBlittableStruct(szArr.BaseType))
                {
                    string abiStructName = AbiTypeHelpers.GetAbiStructTypeName(context, szArr.BaseType);
                    dataParamType = abiStructName + "*";
                    dataCastType = "(" + abiStructName + "*)";
                }
                else
                {
                    dataParamType = "void**";
                    dataCastType = "(void**)";
                }

                UnsafeAccessorFactory.EmitStaticMethod(
                    writer,
                    accessName: "CopyToUnmanaged",
                    returnType: "void",
                    functionName: $"CopyToUnmanaged_{localName}",
                    interopType: ArrayElementEncoder.GetArrayMarshallerInteropPath(szArr.BaseType),
                    parameterList: $", ReadOnlySpan<{elementProjected.Format()}> span, uint length, {dataParamType} data");
                writer.WriteLine($"CopyToUnmanaged_{localName}(null, {callName}, (uint){callName}.Length, {dataCastType}_{localName});");
            }
        }

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
            ParameterCategory cat = ParameterCategoryResolver.Resolve(p);

            if (cat.IsArrayInput())
            {
                string callName = p.GetParamName(paramNameOverride);
                string localName = p.GetParamLocalName(paramNameOverride);
                writer.Write(isMultiline: true, $$"""
                    ,
                      (uint){{callName}}.Length, _{{localName}}
                    """);
                continue;
            }

            if (cat == ParameterCategory.Out)
            {
                string localName = p.GetParamLocalName(paramNameOverride);
                writer.Write(isMultiline: true, $$"""
                    ,
                      &__{{localName}}
                    """);
                continue;
            }

            if (cat == ParameterCategory.ReceiveArray)
            {
                string localName = p.GetParamLocalName(paramNameOverride);
                writer.Write(isMultiline: true, $$"""
                    ,
                      &__{{localName}}_length, &__{{localName}}_data
                    """);
                continue;
            }

            if (cat == ParameterCategory.Ref)
            {
                string localName = p.GetParamLocalName(paramNameOverride);
                TypeSignature uRefArg = p.Type.StripByRefAndCustomModifiers();

                if (context.AbiTypeKindResolver.IsNonBlittableStruct(uRefArg))
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
                writer.Write($"__{p.GetParamLocalName(paramNameOverride)}");
            }
            else if (p.Type.IsString())
            {
                writer.Write($"__{p.GetParamLocalName(paramNameOverride)}.HString");
            }
            else if (context.AbiTypeKindResolver.IsReferenceTypeOrGenericInstance(p.Type))
            {
                writer.Write($"__{p.GetParamLocalName(paramNameOverride)}.GetThisPtrUnsafe()");
            }
            else if (p.Type.IsSystemType())
            {
                // System.Type input: pass the pre-converted ABI Type struct (via the local set up before the call).
                writer.Write($"__{p.GetParamLocalName(paramNameOverride)}.ConvertToUnmanagedUnsafe()");
            }
            else if (context.AbiTypeKindResolver.IsMappedAbiValueType(p.Type))
            {
                // Mapped value-type input: pass the pre-converted ABI local.
                writer.Write($"__{p.GetParamLocalName(paramNameOverride)}");
            }
            else if (context.AbiTypeKindResolver.IsNonBlittableStruct(p.Type))
            {
                // Complex struct input: pass the pre-converted ABI struct local.
                writer.Write($"__{p.GetParamLocalName(paramNameOverride)}");
            }
            else if (context.AbiTypeKindResolver.IsBlittableStruct(p.Type))
            {
                writer.Write(p.GetParamName(paramNameOverride));
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
        // Blittable element types (primitives and blittable structs) don't need this
        // because the user's Span wraps the same memory the native side wrote to.
        foreach ((_, ParameterInfo p) in sig.ParametersByCategory(ParameterCategory.FillArray))

        {

            if (p.Type is not SzArrayTypeSignature szFA)
            {
                continue;
            }

            if (context.AbiTypeKindResolver.IsBlittableAbiElement(szFA.BaseType))
            {
                continue;
            }

            string callName = p.GetParamName(paramNameOverride);
            string localName = p.GetParamLocalName(paramNameOverride);
            ArrayTempNames names = new(localName);
            WriteProjectionTypeCallback elementProjected = TypedefNameWriter.WriteProjectionType(context, TypeSemanticsFactory.Get(szFA.BaseType));

            // Determine the ABI element type for the data pointer parameter (e.g. "void*" for
            // ref-like elements -> "void** data"/"(void**)", or "global::ABI.Foo.Bar" for complex
            // structs -> "global::ABI.Foo.Bar* data"/"(global::ABI.Foo.Bar*)").
            string elementAbi = AbiTypeHelpers.GetArrayElementAbiType(context, szFA.BaseType);

            UnsafeAccessorFactory.EmitStaticMethod(
                writer,
                accessName: "CopyToManaged",
                returnType: "void",
                functionName: $"CopyToManaged_{localName}",
                interopType: ArrayElementEncoder.GetArrayMarshallerInteropPath(szFA.BaseType),
                parameterList: $", uint length, {elementAbi}* data, Span<{elementProjected.Format()}> span");
            writer.WriteLine($"CopyToManaged_{localName}(null, (uint){names.Span}.Length, ({elementAbi}*)_{localName}, {callName});");
        }

        // After call: write back Out params to caller's 'out' var.
        foreach ((_, ParameterInfo p) in sig.ParametersByCategory(ParameterCategory.Out))

        {

            string callName = p.GetParamName(paramNameOverride);
            string localName = p.GetParamLocalName(paramNameOverride);
            TypeSignature uOut = p.Type.StripByRefAndCustomModifiers();

            // For Out generic instance: emit inline UnsafeAccessor to ConvertToManaged_<name>
            // before the writeback. (e.g. Collection1HandlerInvoke
            // emits the accessor inside try, right before the assignment).
            if (uOut.IsGenericInstance())
            {
                string interopTypeName = InteropTypeNameWriter.GetInteropAssemblyQualifiedName(uOut, TypedefNameType.ABI);
                WriteProjectedSignatureCallback projectedTypeName = MethodFactory.WriteProjectedSignature(context, uOut, false);
                UnsafeAccessorFactory.EmitStaticMethod(
                    writer,
                    accessName: "ConvertToManaged",
                    returnType: projectedTypeName.Format(),
                    functionName: $"ConvertToManaged_{localName}",
                    interopType: interopTypeName,
                    parameterList: ", void* value");
                writer.WriteLine($"{callName} = ConvertToManaged_{localName}(null, __{localName});");
                continue;
            }

            writer.Write($"{callName} = ");

            if (uOut.IsString())
            {
                writer.Write($"HStringMarshaller.ConvertToManaged(__{localName})");
            }
            else if (uOut.IsObject())
            {
                writer.Write($"WindowsRuntimeObjectMarshaller.ConvertToManaged(__{localName})");
            }
            else if (context.AbiTypeKindResolver.IsRuntimeClassOrInterface(uOut))
            {
                writer.Write($"{AbiTypeHelpers.GetMarshallerFullName(writer, context, uOut)}.ConvertToManaged(__{localName})");
            }
            else if (uOut.IsSystemType())
            {
                writer.Write($"global::ABI.System.TypeMarshaller.ConvertToManaged(__{localName})");
            }
            else if (context.AbiTypeKindResolver.IsNonBlittableStruct(uOut))
            {
                writer.Write($"{AbiTypeHelpers.GetMarshallerFullName(writer, context, uOut)}.ConvertToManaged(__{localName})");
            }
            else if (context.AbiTypeKindResolver.IsBlittableStruct(uOut))
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
            else if (context.AbiTypeKindResolver.IsEnumType(uOut))
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
        foreach ((_, ParameterInfo p) in sig.ParametersByCategory(ParameterCategory.ReceiveArray))

        {

            string callName = p.GetParamName(paramNameOverride);
            string localName = p.GetParamLocalName(paramNameOverride);
            SzArrayTypeSignature sza = p.Type.AsSzArray()!;
            WriteProjectionTypeCallback elementProjected = TypedefNameWriter.WriteProjectionType(context, TypeSemanticsFactory.Get(sza.BaseType));

            // Element ABI type for the `data` parameter (void* for ref types, ABI struct for
            // complex structs, blittable struct ABI for blittable structs, primitive ABI otherwise).
            string elementAbi = AbiTypeHelpers.GetArrayElementAbiType(context, sza.BaseType);
            string marshallerPath = ArrayElementEncoder.GetArrayMarshallerInteropPath(sza.BaseType);
            UnsafeAccessorFactory.EmitStaticMethod(
                writer,
                accessName: "ConvertToManaged",
                returnType: $"{elementProjected.Format()}[]",
                functionName: $"ConvertToManaged_{localName}",
                interopType: marshallerPath,
                parameterList: $", uint length, {elementAbi}* data");
            writer.WriteLine($"{callName} = ConvertToManaged_{localName}(null, __{localName}_length, __{localName}_data);");
        }

        if (rt is not null)
        {
            if (returnIsReceiveArray)
            {
                SzArrayTypeSignature retSz = (SzArrayTypeSignature)rt;
                WriteProjectionTypeCallback elementProjected = TypedefNameWriter.WriteProjectionType(context, TypeSemanticsFactory.Get(retSz.BaseType));
                string elementAbi = AbiTypeHelpers.GetArrayElementAbiType(context, retSz.BaseType);
                UnsafeAccessorFactory.EmitStaticMethod(
                    writer,
                    accessName: "ConvertToManaged",
                    returnType: $"{elementProjected.Format()}[]",
                    functionName: "ConvertToManaged_retval",
                    interopType: ArrayElementEncoder.GetArrayMarshallerInteropPath(retSz.BaseType),
                    parameterList: $", uint length, {elementAbi}* data");
                writer.WriteLine("return ConvertToManaged_retval(null, __retval_length, __retval_data);");
            }
            else if (returnIsHResultException)
            {
                writer.WriteLine("return global::ABI.System.ExceptionMarshaller.ConvertToManaged(__retval);");
            }
            else if (returnIsString)
            {
                writer.WriteLine("return HStringMarshaller.ConvertToManaged(__retval);");
            }
            else if (returnIsRefType)
            {
                if (rt.IsNullableT())
                {
                    // Nullable<T> return: use <T>Marshaller.UnboxToManaged.;
                    // there is no Nullable<T>Marshaller, the inner-T marshaller has UnboxToManaged.
                    (_, string innerMarshaller) = AbiTypeHelpers.GetNullableInnerInfo(writer, context, rt);
                    writer.WriteLine($"return {innerMarshaller}.UnboxToManaged(__retval);");
                }
                else if (rt.IsGenericInstance())
                {
                    string interopTypeName = InteropTypeNameWriter.GetInteropAssemblyQualifiedName(rt, TypedefNameType.ABI);
                    WriteProjectedSignatureCallback projectedTypeName = MethodFactory.WriteProjectedSignature(context, rt, false);
                    UnsafeAccessorFactory.EmitStaticMethod(
                        writer,
                        accessName: "ConvertToManaged",
                        returnType: projectedTypeName.Format(),
                        functionName: "ConvertToManaged_retval",
                        interopType: interopTypeName,
                        parameterList: ", void* value");
                    writer.WriteLine("return ConvertToManaged_retval(null, __retval);");
                }
                else
                {
                    EmitMarshallerConvertToManagedCallback cvt = EmitMarshallerConvertToManaged(context, rt, "__retval");
                    writer.WriteLine($"return {cvt};");
                }
            }
            else if (rt is not null && context.AbiTypeKindResolver.IsMappedAbiValueType(rt))
            {
                // Mapped value type return (e.g. DateTime/TimeSpan): convert ABI struct back via marshaller.
                writer.WriteLine($"return {AbiTypeHelpers.GetMappedMarshallerName(rt)}.ConvertToManaged(__retval);");
            }
            else if (rt is not null && rt.IsSystemType())
            {
                // System.Type return: convert ABI Type struct back to System.Type via TypeMarshaller.
                writer.WriteLine("return global::ABI.System.TypeMarshaller.ConvertToManaged(__retval);");
            }
            else if (returnIsBlittableStruct)
            {
                if (rt is not null && context.AbiTypeKindResolver.IsMappedAbiValueType(rt))
                {
                    // Mapped value type return: convert ABI struct back to projected via marshaller.
                    writer.WriteLine($"return {AbiTypeHelpers.GetMappedMarshallerName(rt)}.ConvertToManaged(__retval);");
                }
                else
                {
                    writer.WriteLine("return __retval;");
                }
            }
            else if (returnIsNonBlittableStruct)
            {
                writer.WriteLine($"return {AbiTypeHelpers.GetMarshallerFullName(writer, context, rt!)}.ConvertToManaged(__retval);");
            }
            else
            {
                writer.Write("return ");
                string projected = MethodFactory.WriteProjectedSignature(context, rt!, false).Format();
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
            writer.DecreaseIndent();
            writer.WriteLine("}");
        }

        if (needsTryFinally)
        {
            writer.DecreaseIndent();
            writer.WriteLine(isMultiline: true, """
                }
                finally
                """);
            using IndentedTextWriter.Block __finallyBlock = writer.WriteBlock();

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
                ParameterCategory cat = ParameterCategoryResolver.Resolve(p);

                if (!cat.IsScalarInput())
                {
                    continue;
                }

                TypeSignature pType = p.Type.StripByRefAndCustomModifiers();

                if (!context.AbiTypeKindResolver.IsNonBlittableStruct(pType))
                {
                    continue;
                }

                string localName = p.GetParamLocalName(paramNameOverride);
                writer.WriteLine($"{AbiTypeHelpers.GetMarshallerFullName(writer, context, pType)}.Dispose(__{localName});");
            }

            // 1. Cleanup non-blittable PassArray/FillArray params:
            // For strings: HStringArrayMarshaller.Dispose + return ArrayPools (3 of them).
            // For runtime classes/objects: Dispose_<name> (UnsafeAccessor) + return ArrayPool.
            // For mapped value types (DateTime/TimeSpan): no per-element disposal needed and truth
            // doesn't return the ArrayPool either, so skip entirely.
            for (int i = 0; i < sig.Parameters.Count; i++)
            {
                ParameterInfo p = sig.Parameters[i];
                ParameterCategory cat = ParameterCategoryResolver.Resolve(p);

                if (!cat.IsArrayInput())
                {
                    continue;
                }

                if (p.Type is not SzArrayTypeSignature szArr)
                {
                    continue;
                }

                if (context.AbiTypeKindResolver.IsBlittableAbiElement(szArr.BaseType))
                {
                    continue;
                }

                if (context.AbiTypeKindResolver.IsMappedAbiValueType(szArr.BaseType))
                {
                    continue;
                }

                if (szArr.BaseType.IsHResultException())
                {
                    // HResultException ABI is just an int; per-element Dispose is a no-op (mirror
                    // the truth: no Dispose_<name> emitted). Just return the inline-array's pool
                    // using the correct element type (ABI.System.Exception, not nint).
                    string localNameH = p.GetParamLocalName(paramNameOverride);
                    writer.WriteLine();
                    writer.WriteLine(isMultiline: true, $$"""
                        if (__{{localNameH}}_arrayFromPool is not null)
                        {
                            global::System.Buffers.ArrayPool<global::ABI.System.Exception>.Shared.Return(__{{localNameH}}_arrayFromPool);
                        }
                        """);
                    continue;
                }
                string localName = p.GetParamLocalName(paramNameOverride);
                ArrayTempNames names = new(localName);

                if (szArr.BaseType.IsString())
                {
                    // The HStringArrayMarshaller.Dispose + ArrayPool returns for strings only
                    // apply to PassArray (where we set up the pinned handles + headers in the
                    // first place). FillArray writes back HSTRING handles into the nint storage
                    // array directly, with no per-element pinned handle / header to release.
                    if (cat == ParameterCategory.PassArray)
                    {
                        writer.WriteLine(isMultiline: true, $$"""
                            HStringArrayMarshaller.Dispose({{names.PinnedHandleSpan}});
                            
                            if ({{names.PinnedHandleArrayFromPool}} is not null)
                            {
                                global::System.Buffers.ArrayPool<nint>.Shared.Return({{names.PinnedHandleArrayFromPool}});
                            }
                            
                            if ({{names.HeaderArrayFromPool}} is not null)
                            {
                                global::System.Buffers.ArrayPool<HStringHeader>.Shared.Return({{names.HeaderArrayFromPool}});
                            }
                            """);
                    }

                    // Both PassArray and FillArray need the inline-array's nint pool returned.
                    writer.WriteLine();
                    writer.WriteLine(isMultiline: true, $$"""
                        if ({{names.ArrayFromPool}} is not null)
                        {
                            global::System.Buffers.ArrayPool<nint>.Shared.Return({{names.ArrayFromPool}});
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

                    if (context.AbiTypeKindResolver.IsNonBlittableStruct(szArr.BaseType))
                    {
                        string abiStructName = AbiTypeHelpers.GetAbiStructTypeName(context, szArr.BaseType);
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
                    writer.WriteLine(isMultiline: true, $$"""
                        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "Dispose")]
                        static extern void Dispose_{{localName}}([UnsafeAccessorType("{{ArrayElementEncoder.GetArrayMarshallerInteropPath(szArr.BaseType)}}")] object _, uint length, {{disposeDataParamType}}
                        """);
                    writer.WriteIf(!disposeDataParamType.EndsWith("data", StringComparison.Ordinal), " data");

                    writer.WriteLine(isMultiline: true, $$"""
                        );
                        
                        fixed({{fixedPtrType}} _{{localName}} = {{names.Span}})
                        {
                            Dispose_{{localName}}(null, (uint) {{names.Span}}.Length, {{disposeCastType}}_{{localName}});
                        }
                        """);
                }

                // ArrayPool storage type matches the InlineArray storage (mapped ABI value type
                // for DateTime/TimeSpan; ABI struct for complex structs; ABI Exception for
                // HResult; nint otherwise). Same dispatch as the InlineArray16<storageT> setup.
                string poolStorageT = AbiTypeHelpers.GetArrayElementStorageType(context, szArr.BaseType);
                writer.WriteLine();
                writer.WriteLine(isMultiline: true, $$"""
                    if ({{names.ArrayFromPool}} is not null)
                    {
                        global::System.Buffers.ArrayPool<{{poolStorageT}}>.Shared.Return({{names.ArrayFromPool}});
                    }
                    """);
            }

            // 2. Free Out string/object/runtime-class params.
            foreach ((_, ParameterInfo p) in sig.ParametersByCategory(ParameterCategory.Out))

            {

                TypeSignature uOut = p.Type.StripByRefAndCustomModifiers();
                string localName = p.GetParamLocalName(paramNameOverride);

                if (uOut.IsString())
                {
                    writer.WriteLine($"HStringMarshaller.Free(__{localName});");
                }
                else if (uOut.IsObject() || context.AbiTypeKindResolver.IsRuntimeClassOrInterface(uOut) || uOut.IsGenericInstance())
                {
                    writer.WriteLine($"WindowsRuntimeUnknownMarshaller.Free(__{localName});");
                }
                else if (uOut.IsSystemType())
                {
                    writer.WriteLine($"global::ABI.System.TypeMarshaller.Dispose(__{localName});");
                }
                else if (context.AbiTypeKindResolver.IsNonBlittableStruct(uOut))
                {
                    writer.WriteLine($"{AbiTypeHelpers.GetMarshallerFullName(writer, context, uOut)}.Dispose(__{localName});");
                }
            }

            // 3. Free ReceiveArray params via UnsafeAccessor.
            foreach ((_, ParameterInfo p) in sig.ParametersByCategory(ParameterCategory.ReceiveArray))

            {

                string localName = p.GetParamLocalName(paramNameOverride);
                SzArrayTypeSignature sza = p.Type.AsSzArray()!;

                // Element ABI type: same dispatch as the ConvertToManaged_<name> path.
                string elementAbi = AbiTypeHelpers.GetArrayElementAbiType(context, sza.BaseType);
                string marshallerPath = ArrayElementEncoder.GetArrayMarshallerInteropPath(sza.BaseType);
                UnsafeAccessorFactory.EmitStaticMethod(
                    writer,
                    accessName: "Free",
                    returnType: "void",
                    functionName: $"Free_{localName}",
                    interopType: marshallerPath,
                    parameterList: $", uint length, {elementAbi}* data");
                writer.WriteLine();
                writer.WriteLine($"Free_{localName}(null, __{localName}_length, __{localName}_data);");
            }

            // 4. Free return value (__retval) — emitted last to match truth ordering.
            if (returnIsString)
            {
                writer.WriteLine("HStringMarshaller.Free(__retval);");
            }
            else if (returnIsRefType)
            {
                writer.WriteLine("WindowsRuntimeUnknownMarshaller.Free(__retval);");
            }
            else if (returnIsNonBlittableStruct)
            {
                writer.WriteLine($"{AbiTypeHelpers.GetMarshallerFullName(writer, context, rt!)}.Dispose(__retval);");
            }
            else if (facts.ReturnIsSystemTypeForCleanup)
            {
                // System.Type return: dispose the ABI.System.Type's HSTRING fields.
                writer.WriteLine("global::ABI.System.TypeMarshaller.Dispose(__retval);");
            }
            else if (returnIsReceiveArray)
            {
                SzArrayTypeSignature retSz = (SzArrayTypeSignature)rt!;
                string elementAbi = AbiTypeHelpers.GetArrayElementAbiType(context, retSz.BaseType);
                UnsafeAccessorFactory.EmitStaticMethod(
                    writer,
                    accessName: "Free",
                    returnType: "void",
                    functionName: "Free_retval",
                    interopType: ArrayElementEncoder.GetArrayMarshallerInteropPath(retSz.BaseType),
                    parameterList: $", uint length, {elementAbi}* data");
                writer.WriteLine("Free_retval(null, __retval_length, __retval_data);");
            }

        }

        writer.DecreaseIndent();
        writer.WriteLine("}");
        writer.DecreaseIndent();
    }

    /// <summary>
    /// Emits the conversion of a parameter from its projected (managed) form to the ABI argument form.
    /// </summary>
    internal static void EmitParamArgConversion(IndentedTextWriter writer, ProjectionEmitContext context, ParameterInfo p, string? paramNameOverride = null)
    {
        string pname = paramNameOverride ?? p.GetRawName();

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
        else if (context.AbiTypeKindResolver.IsEnumType(p.Type))
        {
            writer.Write(pname);
        }
        else
        {
            writer.Write(pname);
        }
    }
}