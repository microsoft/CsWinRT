// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.ProjectionWriter.Errors;
using WindowsRuntime.ProjectionWriter.Factories.Callbacks;
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
    /// Emits a real Do_Abi (CCW) body for the cases we can handle. This is a partial
    /// implementation that uses simple per-marshaller patterns inline rather than the
    /// fully-general <c>abi_marshaler</c> abstraction.
    /// </summary>
    internal static void EmitDoAbiBodyIfSimple(IndentedTextWriter writer, ProjectionEmitContext context, MethodSignatureInfo sig, string ifaceFullName, string methodName)
    {
        TypeSignature? rt = sig.ReturnType;

        bool returnIsReceiveArrayDoAbi = rt is SzArrayTypeSignature retSzAbi
            && (context.AbiTypeShapeResolver.IsBlittableAbiElement(retSzAbi.BaseType)
                || retSzAbi.BaseType.IsAbiArrayElementRefLike(context.AbiTypeShapeResolver)
                || context.AbiTypeShapeResolver.IsComplexStruct(retSzAbi.BaseType));
        bool returnIsHResultExceptionDoAbi = rt is not null && rt.IsHResultException();
        bool returnIsString = rt is not null && rt.IsString();
        bool returnIsRefType = rt is not null && (context.AbiTypeShapeResolver.IsRuntimeClassOrInterface(rt) || rt.IsObject() || rt.IsGenericInstance());
        bool returnIsGenericInstance = rt is not null && rt.IsGenericInstance();
        bool returnIsBlittableStruct = rt is not null && context.AbiTypeShapeResolver.IsBlittableStruct(rt);

        bool isGetter = sig.Method.IsGetter;
        bool isSetter = sig.Method.IsSetter;
        bool isAddEvent = sig.Method.IsAdder;
        bool isRemoveEvent = sig.Method.IsRemover;

        if (isAddEvent || isRemoveEvent)
        {
            // Events go through dedicated EmitDoAbiAddEvent / EmitDoAbiRemoveEvent paths
            // upstream. If we reach here for an event accessor it's a generator bug.
            // Defensive guard against future regressions.
            throw WellKnownProjectionWriterExceptions.UnreachableEmissionState(
                $"EmitDoAbiBodyIfSimple: unexpectedly called for event accessor '{methodName}' " +
                $"on '{ifaceFullName}'. Events should dispatch through EmitDoAbiAddEvent / EmitDoAbiRemoveEvent.");
        }

        writer.WriteLine();
        using (writer.WriteBlock())
        {
            MethodSignatureInfo.ReturnNameInfo retNames = sig.GetReturnNameInfo();
            string retParamName = retNames.ValuePointer;
            string retSizeParamName = retNames.SizePointer;

            // The local name for the unmarshalled return value uses the standard pattern
            // of prefixing '__' to the param name. For the default '__return_value__' param
            // this becomes '____return_value__'.
            string retLocalName = retNames.Local;

            // at the TOP of the method body (before local declarations and the try block). The
            // actual call sites later in the body just reference the already-declared accessor.
            // For a generic-instance return type, the accessor is named ConvertToUnmanaged_<retParamName>.
            // Skip Nullable<T> returns: those use <T>Marshaller.BoxToUnmanaged at the call site
            // instead of the generic-instance UnsafeAccessor (V3-M7).
            if (returnIsGenericInstance && !(rt is not null && rt.IsNullableT()))
            {
                string interopTypeName = InteropTypeNameWriter.GetInteropAssemblyQualifiedName(rt!, TypedefNameType.ABI);
                WriteProjectedSignatureCallback projectedTypeName = MethodFactory.WriteProjectedSignature(context, rt!, false);
                writer.WriteLine(isMultiline: true, $$"""
                    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "ConvertToUnmanaged")]
                    static extern WindowsRuntimeObjectReferenceValue ConvertToUnmanaged_{{retParamName}}([UnsafeAccessorType("{{interopTypeName}}")] object _, {{projectedTypeName}} value);
                    """);
                writer.WriteLine();
            }

            // Hoist [UnsafeAccessor] declarations for Out generic-instance params:
            // ConvertToUnmanaged_<name> wraps the projected value into a WindowsRuntimeObjectReferenceValue.
            // The body's writeback later references these already-declared accessors.
            foreach ((_, ParameterInfo p) in sig.ParametersByCategory(ParameterCategory.Out))
            {

                TypeSignature uOut = AbiTypeHelpers.StripByRefAndCustomModifiers(p.Type);

                if (!uOut.IsGenericInstance())
                {
                    continue;
                }

                string raw = p.GetRawName();
                string interopTypeName = InteropTypeNameWriter.GetInteropAssemblyQualifiedName(uOut, TypedefNameType.ABI);
                WriteProjectedSignatureCallback projectedTypeName = MethodFactory.WriteProjectedSignature(context, uOut, false);
                writer.WriteLine(isMultiline: true, $$"""
                    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "ConvertToUnmanaged")]
                    static extern WindowsRuntimeObjectReferenceValue ConvertToUnmanaged_{{raw}}([UnsafeAccessorType("{{interopTypeName}}")] object _, {{projectedTypeName}} value);
                    """);
                writer.WriteLine();
            }

            // ConvertToUnmanaged_<param> and the return-array ConvertToUnmanaged_<retParam> to the
            // top of the method body, before locals and the try block. The actual call sites later
            // in the body reference these already-declared accessors.
            foreach ((_, ParameterInfo p) in sig.ParametersByCategory(ParameterCategory.ReceiveArray))
            {

                string raw = p.GetRawName();
                SzArrayTypeSignature sza = p.Type.AsSzArray()!;
                WriteProjectionTypeCallback elementProjected = TypedefNameWriter.WriteProjectionType(context, TypeSemanticsFactory.Get(sza.BaseType));

                string marshallerPath = ArrayElementEncoder.GetArrayMarshallerInteropPath(sza.BaseType);
                string elementAbi = AbiTypeHelpers.GetAbiLocalTypeName(context, sza.BaseType);
                writer.WriteLine(isMultiline: true, $$"""
                    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "ConvertToUnmanaged")]
                    static extern void ConvertToUnmanaged_{{raw}}([UnsafeAccessorType("{{marshallerPath}}")] object _, ReadOnlySpan<{{elementProjected}}> span, out uint length, out {{elementAbi}}* data);
                    """);
                writer.WriteLine();
            }

            if (returnIsReceiveArrayDoAbi && rt is SzArrayTypeSignature retSzHoist)
            {
                WriteProjectionTypeCallback elementProjected = TypedefNameWriter.WriteProjectionType(context, TypeSemanticsFactory.Get(retSzHoist.BaseType));
                string elementAbi = AbiTypeHelpers.GetAbiLocalTypeName(context, retSzHoist.BaseType);
                string marshallerPath = ArrayElementEncoder.GetArrayMarshallerInteropPath(retSzHoist.BaseType);
                writer.WriteLine(isMultiline: true, $$"""
                    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "ConvertToUnmanaged")]
                    static extern void ConvertToUnmanaged_{{retParamName}}([UnsafeAccessorType("{{marshallerPath}}")] object _, ReadOnlySpan<{{elementProjected}}> span, out uint length, out {{elementAbi}}* data);
                    """);
                writer.WriteLine();
            }

            // the OUT pointer(s). The actual assignment happens inside the try block.
            if (rt is not null)
            {
                if (returnIsString)
                {
                    writer.WriteLine($"string {retLocalName} = default;");
                }
                else
                {
                    // returnIsRefType, returnIsReceiveArrayDoAbi, and the default branch all emit
                    // the same projected type for the default-initialized return local.
                    WriteProjectedSignatureCallback projected = MethodFactory.WriteProjectedSignature(context, rt, false);
                    writer.WriteLine($"{projected} {retLocalName} = default;");
                }
            }

            if (rt is not null)
            {
                if (returnIsReceiveArrayDoAbi)
                {
                    writer.WriteLine(isMultiline: true, $$"""
                        *{{retParamName}} = default;
                        *{{retSizeParamName}} = default;
                        """);
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
            foreach ((_, ParameterInfo p) in sig.ParametersByCategory(ParameterCategory.Out))
            {

                string raw = p.GetRawName();
                string ptr = IdentifierEscaping.EscapeIdentifier(raw);
                writer.WriteLine($"*{ptr} = default;");
            }

            foreach ((_, ParameterInfo p) in sig.ParametersByCategory(ParameterCategory.Out))
            {

                string raw = p.GetRawName();

                // Use the projected (non-ABI) type for the local variable.
                // Strip ByRef and CustomModifier wrappers to get the underlying base type.
                TypeSignature underlying = AbiTypeHelpers.StripByRefAndCustomModifiers(p.Type);
                WriteProjectedSignatureCallback projected = MethodFactory.WriteProjectedSignature(context, underlying, false);
                writer.WriteLine($"{projected} __{raw} = default;");
            }

            // For each ReceiveArray parameter (out T[]), zero the destination + size out pointers
            // and declare a managed array local. The managed call passes 'out __<name>' and after
            // the call we copy to the ABI buffer via UnsafeAccessor.
            foreach ((_, ParameterInfo p) in sig.ParametersByCategory(ParameterCategory.ReceiveArray))
            {

                string raw = p.GetRawName();
                string ptr = IdentifierEscaping.EscapeIdentifier(raw);
                SzArrayTypeSignature sza = p.Type.AsSzArray()!;
                WriteProjectionTypeCallback elementProjected = TypedefNameWriter.WriteProjectionType(context, TypeSemanticsFactory.Get(sza.BaseType));
                writer.WriteLine(isMultiline: true, $$"""
                *{{ptr}} = default;
                    *__{{raw}}Size = default;
                    {{elementProjected}}[] __{{raw}} = default;
                """);
            }

            // For each blittable array (PassArray / FillArray) parameter, declare a Span<T> local that
            // wraps the (length, pointer) pair from the ABI signature.
            // For non-blittable element types (string/runtime class/object), declare InlineArray16<T> +
            // ArrayPool fallback then CopyToManaged via UnsafeAccessor.
            for (int i = 0; i < sig.Parameters.Count; i++)
            {
                ParameterInfo p = sig.Parameters[i];
                ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);

                if (cat is not (ParameterCategory.PassArray or ParameterCategory.FillArray))
                {
                    continue;
                }

                if (p.Type is not SzArrayTypeSignature sz)
                {
                    continue;
                }

                string raw = p.GetRawName();
                string ptr = IdentifierEscaping.EscapeIdentifier(raw);
                WriteProjectionTypeCallback elementProjected = TypedefNameWriter.WriteProjectionType(context, TypeSemanticsFactory.Get(sz.BaseType));
                bool isBlittableElem = context.AbiTypeShapeResolver.IsBlittableAbiElement(sz.BaseType);

                if (isBlittableElem)
                {
                    writer.WriteLine($"{(cat == ParameterCategory.PassArray ? "ReadOnlySpan<" : "Span<")}{elementProjected}> __{raw} = new({ptr}, (int)__{raw}Size);");
                }
                else
                {
                    // Non-blittable element: InlineArray16<T> + ArrayPool<T> with size from ABI.
                    writer.WriteLine();
                    writer.WriteLine(isMultiline: true, $$"""
                        Unsafe.SkipInit(out InlineArray16<{{elementProjected}}> __{{raw}}_inlineArray);
                            {{elementProjected}}[] __{{raw}}_arrayFromPool = null;
                            Span<{{elementProjected}}> __{{raw}} = __{{raw}}Size <= 16
                                ? __{{raw}}_inlineArray[..(int)__{{raw}}Size]
                                : (__{{raw}}_arrayFromPool = global::System.Buffers.ArrayPool<{{elementProjected}}>.Shared.Rent((int)__{{raw}}Size));
                        """);
                }
            }
            writer.WriteLine(isMultiline: true, """
                try
                {
                """);
            writer.IncreaseIndent();

            // For non-blittable PassArray params (read-only input arrays), emit CopyToManaged_<name>
            // via UnsafeAccessor to convert the native ABI buffer into the managed Span<T> the
            // delegate sees. For FillArray params, the buffer is fresh storage the user delegate
            // fills — the post-call writeback loop handles that.
            foreach ((_, ParameterInfo p) in sig.ParametersByCategory(ParameterCategory.PassArray))
            {

                if (p.Type is not SzArrayTypeSignature szArr)
                {
                    continue;
                }

                if (context.AbiTypeShapeResolver.IsBlittableAbiElement(szArr.BaseType))
                {
                    continue;
                }

                string raw = p.GetRawName();
                string ptr = IdentifierEscaping.EscapeIdentifier(raw);
                WriteProjectionTypeCallback elementProjected = TypedefNameWriter.WriteProjectionType(context, TypeSemanticsFactory.Get(szArr.BaseType));

                // For complex structs, the data param is the ABI struct pointer (e.g. BasicStruct*).
                // The Do_Abi parameter we receive is void* (per V3R3-M8), so the call-site needs an
                // explicit (T*) cast to bridge the type. For ref-types (string/runtime-class/object),
                // the data param is void** and the cast is (void**).
                string dataParamType;
                string dataCastExpr;

                if (context.AbiTypeShapeResolver.IsComplexStruct(szArr.BaseType))
                {
                    string abiStructName = AbiTypeHelpers.GetAbiStructTypeName(context, szArr.BaseType);
                    dataParamType = abiStructName + "* data";
                    dataCastExpr = "(" + abiStructName + "*)" + ptr;
                }
                else
                {
                    dataParamType = "void** data";
                    dataCastExpr = "(void**)" + ptr;
                }

                writer.IncreaseIndent();
                writer.WriteLine(isMultiline: true, $$"""
                    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "CopyToManaged")]
                        static extern void CopyToManaged_{{raw}}([UnsafeAccessorType("{{ArrayElementEncoder.GetArrayMarshallerInteropPath(szArr.BaseType)}}")] object _, uint length, {{dataParamType}}, Span<{{elementProjected}}> span);
                        CopyToManaged_{{raw}}(null, __{{raw}}Size, {{dataCastExpr}}, __{{raw}});
                    """);
                writer.DecreaseIndent();
            }

            // For generic instance ABI input parameters, emit local UnsafeAccessor delegates and locals
            // first so the call site can reference them.
            for (int i = 0; i < sig.Parameters.Count; i++)
            {
                ParameterInfo p = sig.Parameters[i];

                if (p.Type.IsNullableT())
                {
                    // Nullable<T> param (server-side): use <T>Marshaller.UnboxToManaged.
                    string rawName = p.GetRawName();
                    string callName = IdentifierEscaping.EscapeIdentifier(rawName);
                    (_, string innerMarshaller) = AbiTypeHelpers.GetNullableInnerInfo(writer, context, p.Type);
                    writer.WriteLine($"var __arg_{rawName} = {innerMarshaller}.UnboxToManaged({callName});");
                }
                else if (p.Type.IsGenericInstance())
                {
                    string rawName = p.GetRawName();
                    string callName = IdentifierEscaping.EscapeIdentifier(rawName);
                    string interopTypeName = InteropTypeNameWriter.GetInteropAssemblyQualifiedName(p.Type, TypedefNameType.ABI);
                    WriteProjectedSignatureCallback projectedTypeName = MethodFactory.WriteProjectedSignature(context, p.Type, false);
                    writer.IncreaseIndent();
                    writer.WriteLine(isMultiline: true, $$"""
                        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "ConvertToManaged")]
                            static extern {{projectedTypeName}} ConvertToManaged_arg_{{rawName}}([UnsafeAccessorType("{{interopTypeName}}")] object _, void* value);
                            var __arg_{{rawName}} = ConvertToManaged_arg_{{rawName}}(null, {{callName}});
                        """);
                    writer.DecreaseIndent();
                }
            }

            if (returnIsString)
            {
                writer.Write($"{retLocalName} = ");
            }
            else if (returnIsRefType)
            {
                writer.Write($"{retLocalName} = ");
            }
            else if (returnIsReceiveArrayDoAbi)
            {
                // For T[] return: assign to existing local.
                writer.Write($"{retLocalName} = ");
            }
            else if (rt is not null)
            {
                writer.Write($"{retLocalName} = ");
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
                EmitDoAbiParamArgConversion(writer, context, sig.Parameters[0]);
                writer.WriteLine(";");
            }
            else
            {
                writer.Write($"ComInterfaceDispatch.GetInstance<{ifaceFullName}>((ComInterfaceDispatch*)thisPtr).{methodName}(");
                for (int i = 0; i < sig.Parameters.Count; i++)
                {
                    if (i > 0)
                    {
                        writer.Write(isMultiline: true, """
                        ,
                          
                        """);
                    }
                    ParameterInfo p = sig.Parameters[i];
                    ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);

                    if (cat == ParameterCategory.Out)
                    {
                        string raw = p.GetRawName();
                        writer.Write($"out __{raw}");
                    }
                    else if (cat == ParameterCategory.Ref)
                    {
                        // WinRT 'in T' / 'ref const T' is a read-only by-ref input on the ABI side
                        // (pointer to a value the native caller owns). On the C# delegate / interface
                        // side it's projected as 'in T'. Read directly from *<name> via the appropriate
                        // marshaller — DO NOT zero or write back.
                        string raw = p.GetRawName();
                        string ptr = IdentifierEscaping.EscapeIdentifier(raw);
                        TypeSignature uRef = AbiTypeHelpers.StripByRefAndCustomModifiers(p.Type);

                        if (uRef.IsString())
                        {
                            writer.Write($"HStringMarshaller.ConvertToManaged(*{ptr})");
                        }
                        else if (uRef.IsObject())
                        {
                            writer.Write($"WindowsRuntimeObjectMarshaller.ConvertToManaged(*{ptr})");
                        }
                        else if (context.AbiTypeShapeResolver.IsRuntimeClassOrInterface(uRef))
                        {
                            writer.Write($"{AbiTypeHelpers.GetMarshallerFullName(writer, context, uRef)}.ConvertToManaged(*{ptr})");
                        }
                        else if (context.AbiTypeShapeResolver.IsMappedAbiValueType(uRef))
                        {
                            writer.Write($"{AbiTypeHelpers.GetMappedMarshallerName(uRef)}.ConvertToManaged(*{ptr})");
                        }
                        else if (uRef.IsHResultException())
                        {
                            writer.Write($"global::ABI.System.ExceptionMarshaller.ConvertToManaged(*{ptr})");
                        }
                        else if (context.AbiTypeShapeResolver.IsComplexStruct(uRef))
                        {
                            writer.Write($"{AbiTypeHelpers.GetMarshallerFullName(writer, context, uRef)}.ConvertToManaged(*{ptr})");
                        }
                        else if (context.AbiTypeShapeResolver.IsBlittableStruct(uRef) || context.AbiTypeShapeResolver.IsBlittablePrimitive(uRef) || context.AbiTypeShapeResolver.IsEnumType(uRef))
                        {
                            // Blittable/almost-blittable: ABI layout matches projected layout.
                            writer.Write($"*{ptr}");
                        }
                        else
                        {
                            writer.Write($"*{ptr}");
                        }
                    }
                    else if (cat.IsArrayInput())
                    {
                        string raw = p.GetRawName();
                        writer.Write($"__{raw}");
                    }
                    else if (cat == ParameterCategory.ReceiveArray)
                    {
                        string raw = p.GetRawName();
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
            foreach ((_, ParameterInfo p) in sig.ParametersByCategory(ParameterCategory.Out))
            {

                string raw = p.GetRawName();
                string ptr = IdentifierEscaping.EscapeIdentifier(raw);
                TypeSignature underlying = AbiTypeHelpers.StripByRefAndCustomModifiers(p.Type);
                string rhs;

                // String: HStringMarshaller.ConvertToUnmanaged
                if (underlying.IsString())
                {
                    rhs = $"HStringMarshaller.ConvertToUnmanaged(__{raw})";
                }

                // Object/runtime class: <Marshaller>.ConvertToUnmanaged(...).DetachThisPtrUnsafe()
                else if (underlying.IsObject())
                {
                    rhs = $"WindowsRuntimeObjectMarshaller.ConvertToUnmanaged(__{raw}).DetachThisPtrUnsafe()";
                }
                else if (context.AbiTypeShapeResolver.IsRuntimeClassOrInterface(underlying))
                {
                    rhs = $"{AbiTypeHelpers.GetMarshallerFullName(writer, context, underlying)}.ConvertToUnmanaged(__{raw}).DetachThisPtrUnsafe()";
                }

                // Generic instance (e.g. IEnumerable<string>): use the hoisted UnsafeAccessor
                // 'ConvertToUnmanaged_<name>' declared at the top of the method body.
                else if (underlying.IsGenericInstance())
                {
                    rhs = $"ConvertToUnmanaged_{raw}(null, __{raw}).DetachThisPtrUnsafe()";
                }

                // For enums, function pointer signature uses the projected enum type, no cast needed.
                // For bool, cast to byte. For char, cast to ushort.
                // For the local managed value, marshal through <Type>Marshaller.ConvertToUnmanaged
                // before writing it into the *out ABI struct slot.

                // Non-blittable struct (e.g. authored BasicStruct with string fields): marshal
                // the local managed value through <Type>Marshaller.ConvertToUnmanaged before
                // writing it into the *out ABI struct slot.write_marshal_from_managed
                //: "Marshaller.ConvertToUnmanaged(local)".
                else if (context.AbiTypeShapeResolver.IsComplexStruct(underlying))
                {
                    rhs = $"{AbiTypeHelpers.GetMarshallerFullName(writer, context, underlying)}.ConvertToUnmanaged(__{raw})";
                }
                else
                {
                    // Enums, bool, char, and primitives: the local __<raw> is already the ABI form.
                    rhs = $"__{raw}";
                }
                writer.WriteLine($"*{ptr} = {rhs};");
            }

            // After call: for ReceiveArray params, emit ConvertToUnmanaged_<name> call (the
            // [UnsafeAccessor] declaration was hoisted to the top of the method body).
            foreach ((_, ParameterInfo p) in sig.ParametersByCategory(ParameterCategory.ReceiveArray))
            {

                string raw = p.GetRawName();
                string ptr = IdentifierEscaping.EscapeIdentifier(raw);
                writer.WriteLine($"ConvertToUnmanaged_{raw}(null, __{raw}, out *__{raw}Size, out *{ptr});");
            }

            // After call: for non-blittable FillArray params (Span<T> where T is string/runtime
            // class/object/non-blittable struct), copy the managed delegate's writes back into the
            // native ABI buffer..
            // which emits 'CopyToUnmanaged_<name>(null, __<name>, __<name>Size, (T*)<name>)'.
            // Blittable element types don't need this — the Span wraps the native buffer directly.
            foreach ((_, ParameterInfo p) in sig.ParametersByCategory(ParameterCategory.FillArray))
            {

                if (p.Type is not SzArrayTypeSignature szFA)
                {
                    continue;
                }

                // Blittable element types: Span wraps the native buffer; no copy-back needed.
                if (context.AbiTypeShapeResolver.IsBlittableAbiElement(szFA.BaseType))
                {
                    continue;
                }

                string raw = p.GetRawName();
                string ptr = IdentifierEscaping.EscapeIdentifier(raw);
                WriteProjectionTypeCallback elementProjected = TypedefNameWriter.WriteProjectionType(context, TypeSemanticsFactory.Get(szFA.BaseType));

                // Determine the ABI element type for the data pointer cast (e.g. "void*" for
                // ref-like elements -> "void** data"/"(void**)", or "global::ABI.Foo.Bar" for
                // complex structs -> "global::ABI.Foo.Bar* data"/"(global::ABI.Foo.Bar*)").
                string elementAbi = AbiTypeHelpers.GetArrayElementAbiType(context, szFA.BaseType);

                writer.IncreaseIndent();
                writer.WriteLine(isMultiline: true, $$"""
                        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "CopyToUnmanaged")]
                        static extern void CopyToUnmanaged_{{raw}}([UnsafeAccessorType("{{ArrayElementEncoder.GetArrayMarshallerInteropPath(szFA.BaseType)}}")] object _, ReadOnlySpan<{{elementProjected}}> span, uint length, {{elementAbi}}* data);
                        CopyToUnmanaged_{{raw}}(null, __{{raw}}, __{{raw}}Size, ({{elementAbi}}*){{ptr}});
                    """);
                writer.DecreaseIndent();
            }

            if (rt is not null)
            {
                if (returnIsHResultExceptionDoAbi)
                {
                    writer.WriteLine($"*{retParamName} = global::ABI.System.ExceptionMarshaller.ConvertToUnmanaged({retLocalName});");
                }
                else if (returnIsString)
                {
                    writer.WriteLine($"*{retParamName} = HStringMarshaller.ConvertToUnmanaged({retLocalName});");
                }
                else if (returnIsRefType)
                {
                    if (rt is not null && rt.IsNullableT())
                    {
                        // Nullable<T> return (server-side): use <T>Marshaller.BoxToUnmanaged.
                        (_, string innerMarshaller) = AbiTypeHelpers.GetNullableInnerInfo(writer, context, rt);
                        writer.WriteLine($"*{retParamName} = {innerMarshaller}.BoxToUnmanaged({retLocalName}).DetachThisPtrUnsafe();");
                    }
                    else if (returnIsGenericInstance)
                    {
                        // Generic instance return: use the UnsafeAccessor static local function declared at
                        // the top of the method body via the M12 hoisting pass; just emit the call here.
                        writer.WriteLine($"*{retParamName} = ConvertToUnmanaged_{retParamName}(null, {retLocalName}).DetachThisPtrUnsafe();");
                    }
                    else
                    {
                        EmitMarshallerConvertToUnmanagedCallback cvt = EmitMarshallerConvertToUnmanaged(context, rt!, retLocalName);
                        writer.WriteLine($"*{retParamName} = {cvt}.DetachThisPtrUnsafe();");
                    }
                }
                else if (returnIsReceiveArrayDoAbi)
                {
                    // Return-receive-array: emit ConvertToUnmanaged_<retParam> call (declaration
                    // was hoisted to the top of the method body).
                    writer.WriteLine($"ConvertToUnmanaged_{retParamName}(null, {retLocalName}, out *{retSizeParamName}, out *{retParamName});");
                }
                else if (context.AbiTypeShapeResolver.IsMappedAbiValueType(rt))
                {
                    // Mapped value type return (DateTime/TimeSpan): convert via marshaller.
                    writer.WriteLine($"*{retParamName} = {AbiTypeHelpers.GetMappedMarshallerName(rt)}.ConvertToUnmanaged({retLocalName});");
                }
                else if (rt.IsSystemType())
                {
                    // System.Type return (server-side): convert managed System.Type to ABI Type struct.
                    writer.WriteLine($"*{retParamName} = global::ABI.System.TypeMarshaller.ConvertToUnmanaged({retLocalName});");
                }
                else if (context.AbiTypeShapeResolver.IsComplexStruct(rt))
                {
                    // Complex struct return (server-side): convert managed struct to ABI struct via marshaller.
                    writer.WriteLine($"*{retParamName} = {AbiTypeHelpers.GetMarshallerFullName(writer, context, rt)}.ConvertToUnmanaged({retLocalName});");
                }
                else if (returnIsBlittableStruct)
                {
                    writer.WriteLine($"*{retParamName} = {retLocalName};");
                }
                else
                {
                    writer.WriteLine($"*{retParamName} = {retLocalName};");
                }
            }

            writer.DecreaseIndent();
            writer.WriteLine(isMultiline: true, """
                    return 0;
                }
                catch (Exception __exception__)
                {
                    return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(__exception__);
                }
                """);

            // For non-blittable PassArray params, emit finally block with ArrayPool<T>.Shared.Return.
            bool hasNonBlittableArrayDoAbi = false;
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

                if (context.AbiTypeShapeResolver.IsBlittableAbiElement(szArr.BaseType))
                {
                    continue;
                }

                hasNonBlittableArrayDoAbi = true;
                break;
            }

            if (hasNonBlittableArrayDoAbi)
            {
                writer.WriteLine(isMultiline: true, """
                    finally
                    {
                    """);
                writer.IncreaseIndent();
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

                    if (context.AbiTypeShapeResolver.IsBlittableAbiElement(szArr.BaseType))
                    {
                        continue;
                    }

                    string raw = p.GetRawName();
                    WriteProjectionTypeCallback elementProjected = TypedefNameWriter.WriteProjectionType(context, TypeSemanticsFactory.Get(szArr.BaseType));
                    writer.WriteLine();
                    writer.WriteLine(isMultiline: true, $$"""
                        if (__{{raw}}_arrayFromPool is not null)
                        {
                            global::System.Buffers.ArrayPool<{{elementProjected}}>.Shared.Return(__{{raw}}_arrayFromPool);
                        }
                        """);
                }
                writer.DecreaseIndent();
                writer.WriteLine("}");
            }
        }
        writer.WriteLine();
    }

    /// <summary>
    /// Converts an ABI parameter to its projected (managed) form for the Do_Abi call.
    /// </summary>
    internal static void EmitDoAbiParamArgConversion(IndentedTextWriter writer, ProjectionEmitContext context, ParameterInfo p)
    {
        string rawName = p.GetRawName();
        string pname = IdentifierEscaping.EscapeIdentifier(rawName);

        if (p.Type is CorLibTypeSignature corlib &&
            corlib.ElementType == ElementType.Boolean)
        {
            writer.Write(pname);
        }
        else if (p.Type is CorLibTypeSignature corlib2 &&
                 corlib2.ElementType == ElementType.Char)
        {
            writer.Write(pname);
        }
        else if (p.Type is CorLibTypeSignature corlibStr &&
                 corlibStr.ElementType == ElementType.String)
        {
            writer.Write($"HStringMarshaller.ConvertToManaged({pname})");
        }
        else if (p.Type.IsGenericInstance())
        {
            // Generic instance ABI parameter: caller already declared a local UnsafeAccessor +
            // local var __arg_<name> that holds the converted value.
            writer.Write($"__arg_{rawName}");
        }
        else if (context.AbiTypeShapeResolver.IsRuntimeClassOrInterface(p.Type) || p.Type.IsObject())
        {
            EmitMarshallerConvertToManaged(writer, context, p.Type, pname);
        }
        else if (context.AbiTypeShapeResolver.IsMappedAbiValueType(p.Type))
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
        else if (context.AbiTypeShapeResolver.IsComplexStruct(p.Type))
        {
            // Complex struct input (server-side): convert ABI struct to managed via marshaller.
            writer.Write($"{AbiTypeHelpers.GetMarshallerFullName(writer, context, p.Type)}.ConvertToManaged({pname})");
        }
        else if (context.AbiTypeShapeResolver.IsBlittableStruct(p.Type))
        {
            // Blittable / almost-blittable struct: pass directly (projected type == ABI type).
            writer.Write(pname);
        }
        else if (context.AbiTypeShapeResolver.IsEnumType(p.Type))
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