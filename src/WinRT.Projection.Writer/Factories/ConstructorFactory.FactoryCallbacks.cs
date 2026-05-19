// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.ProjectionWriter.Factories.Callbacks;
using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Helpers;
using WindowsRuntime.ProjectionWriter.Metadata;
using WindowsRuntime.ProjectionWriter.Models;
using WindowsRuntime.ProjectionWriter.Resolvers;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter.Factories;

internal static partial class ConstructorFactory
{
    /// <summary>
    /// Emits the <c>private readonly ref struct &lt;Name&gt;Args(args...) {...}</c>.
    /// </summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="context">The active emit context.</param>
    /// <param name="sig">The factory method signature whose parameters are turned into struct fields.</param>
    /// <param name="argsName">The simple name of the emitted args struct.</param>
    /// <param name="userParamCount">If &gt;= 0, only emit the first <paramref name="userParamCount"/>
    /// params (used for composable factories where the trailing baseInterface/innerInterface params
    /// are consumed by the callback Invoke signature directly, not stored in args).</param>
    private static void EmitFactoryArgsStruct(IndentedTextWriter writer, ProjectionEmitContext context, MethodSignatureInfo sig, string argsName, int userParamCount = -1)
    {
        int count = userParamCount >= 0 ? userParamCount : sig.Parameters.Count;
        writer.WriteLine();
        writer.Write($"private readonly ref struct {argsName}(");
        for (int i = 0; i < count; i++)
        {
            WriteProjectionParameterCallback p = MethodFactory.WriteProjectionParameter(context, sig.Parameters[i]);
            writer.Write($"{(i > 0 ? ", " : "")}{p}");
        }
        writer.WriteLine(isMultiline: true, """
            )
            {
            """);
        for (int i = 0; i < count; i++)
        {
            ParameterInfo p = sig.Parameters[i];
            string pname = p.GetEscapedName();
            WriteProjectionParameterTypeCallback paramType = MethodFactory.WriteProjectionParameterType(context, p);
            writer.WriteLine($"    public readonly {paramType} {pname} = {pname};");
        }
        writer.WriteLine("}");
    }

    /// <summary>
    /// Emits the <c>private sealed class &lt;Name&gt; : WindowsRuntimeActivationFactoryCallback.DerivedSealed</c>.
    /// </summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="context">The active emit context.</param>
    /// <param name="sig">The factory method signature.</param>
    /// <param name="callbackName">The simple name of the emitted callback class.</param>
    /// <param name="argsName">The simple name of the args struct previously emitted by <see cref="EmitFactoryArgsStruct"/>.</param>
    /// <param name="factoryObjRefName">The name of the static lazy <c>WindowsRuntimeObjectReference</c> property holding the activation factory.</param>
    /// <param name="factoryMethodIndex">The vtable slot of the factory method on the activation factory interface.</param>
    /// <param name="isComposable">When true, emit the DerivedComposed callback variant whose
    /// Invoke signature includes the additional <c>WindowsRuntimeObject baseInterface</c> +
    /// <c>out void* innerInterface</c> params. Iteration over user params is bounded by
    /// <paramref name="userParamCount"/> (defaults to all params).</param>
    /// <param name="userParamCount">If &gt;= 0, only emit the first <paramref name="userParamCount"/> user params (used for composable factories).</param>
    private static void EmitFactoryCallbackClass(IndentedTextWriter writer, ProjectionEmitContext context, MethodSignatureInfo sig, string callbackName, string argsName, string factoryObjRefName, int factoryMethodIndex, bool isComposable = false, int userParamCount = -1)
    {
        int paramCount = userParamCount >= 0 ? userParamCount : sig.Parameters.Count;
        string baseClass = isComposable
            ? "WindowsRuntimeActivationFactoryCallback.DerivedComposed"
            : "WindowsRuntimeActivationFactoryCallback.DerivedSealed";
        writer.WriteLine();
        writer.WriteLine(isMultiline: true, $$"""
            private sealed class {{callbackName}} : {{baseClass}}
            {
                public static readonly {{callbackName}} Instance = new();
            
                [MethodImpl(MethodImplOptions.NoInlining)]
            """);
        if (isComposable)
        {
            // Composable Invoke signature is multi-line and includes baseInterface (in) +
            // innerInterface (out).
            writer.WriteLine(isMultiline: true, """
                    public override unsafe void Invoke(
                      WindowsRuntimeActivationArgsReference additionalParameters,
                      WindowsRuntimeObject baseInterface,
                      out void* innerInterface,
                      out void* retval)
                    {
                """);
        }
        else
        {
            // Sealed Invoke signature is multi-line..
            writer.WriteLine(isMultiline: true, """
                    public override unsafe void Invoke(
                      WindowsRuntimeActivationArgsReference additionalParameters,
                      out void* retval)
                    {
                """);
        }

        // Invoke body is just 'throw null;' (no factory dispatch, no marshalling).
        if (context.Settings.ReferenceProjection)
        {
            RefModeStubFactory.EmitRefModeInvokeBody(writer);
            return;
        }

        writer.WriteLine(isMultiline: true, $$"""
                    using WindowsRuntimeObjectReferenceValue activationFactoryValue = {{factoryObjRefName}}.AsValue();
                    void* ThisPtr = activationFactoryValue.GetThisPtrUnsafe();
                    ref readonly {{argsName}} args = ref additionalParameters.GetValueRefUnsafe<{{argsName}}>();
            """);

        // Bind each arg from the args struct to a local of its ABI-marshalable input type.
        // Bind arg locals.
        for (int i = 0; i < paramCount; i++)
        {
            ParameterInfo p = sig.Parameters[i];
            string raw = p.GetRawName();
            string pname = IdentifierEscaping.EscapeIdentifier(raw);
            ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);
            writer.Write("        ");
            // For array params, the bind type is ReadOnlySpan<T> / Span<T> (not the SzArray).
            if (cat == ParameterCategory.PassArray)
            {
                WriteProjectionTypeCallback elem = TypedefNameWriter.WriteProjectionType(context, TypeSemanticsFactory.Get(((SzArrayTypeSignature)p.Type).BaseType));
                writer.Write($"ReadOnlySpan<{elem}>");
            }
            else if (cat == ParameterCategory.FillArray)
            {
                WriteProjectionTypeCallback elem = TypedefNameWriter.WriteProjectionType(context, TypeSemanticsFactory.Get(((SzArrayTypeSignature)p.Type).BaseType));
                writer.Write($"Span<{elem}>");
            }
            else
            {
                MethodFactory.WriteProjectedSignature(writer, context, p.Type, true);
            }

            writer.WriteLine($" {pname} = args.{pname};");
        }

        // For generic instance params, emit local UnsafeAccessor delegates (or Nullable<T> -> BoxToUnmanaged).
        for (int i = 0; i < paramCount; i++)
        {
            ParameterInfo p = sig.Parameters[i];

            if (!p.Type.IsGenericInstance())
            {
                continue;
            }

            string raw = p.GetRawName();
            string pname = IdentifierEscaping.EscapeIdentifier(raw);

            if (p.Type.IsNullableT())
            {
                (_, string innerMarshaller) = AbiTypeHelpers.GetNullableInnerInfo(writer, context, p.Type);
                writer.WriteLine($"        using WindowsRuntimeObjectReferenceValue __{raw} = {innerMarshaller}.BoxToUnmanaged({pname});");
                continue;
            }

            string interopTypeName = InteropTypeNameWriter.GetInteropAssemblyQualifiedName(p.Type, TypedefNameType.ABI);
            WriteProjectedSignatureCallback projectedTypeName = MethodFactory.WriteProjectedSignature(context, p.Type, false);
            writer.WriteLine(isMultiline: true, $$"""
                        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "ConvertToUnmanaged")]
                        static extern WindowsRuntimeObjectReferenceValue ConvertToUnmanaged_{{raw}}([UnsafeAccessorType("{{interopTypeName}}")] object _, {{projectedTypeName}} value);
                        using WindowsRuntimeObjectReferenceValue __{{raw}} = ConvertToUnmanaged_{{raw}}(null, {{pname}});
                """);
        }

        // For runtime class / object params, emit `using WindowsRuntimeObjectReferenceValue __<name> = ...ConvertToUnmanaged(<name>);`
        for (int i = 0; i < paramCount; i++)
        {
            ParameterInfo p = sig.Parameters[i];

            // already handled above
            if (p.Type.IsGenericInstance())
            {
                continue;
            }

            if (!context.AbiTypeShapeResolver.IsRuntimeClassOrInterface(p.Type) && !p.Type.IsObject())
            {
                continue;
            }

            string raw = p.GetRawName();
            string pname = IdentifierEscaping.EscapeIdentifier(raw);
            EmitMarshallerConvertToUnmanagedCallback cvt = AbiMethodBodyFactory.EmitMarshallerConvertToUnmanaged(context, p.Type, pname);
            writer.WriteLine($"        using WindowsRuntimeObjectReferenceValue __{raw} = {cvt};");
        }

        // For composable factories, marshal the additional `baseInterface` (which is a
        // WindowsRuntimeObject parameter on Invoke, not an args field).
        //   using WindowsRuntimeObjectReferenceValue __baseInterface = WindowsRuntimeObjectMarshaller.ConvertToUnmanaged(baseInterface);
        if (isComposable)
        {
            writer.WriteLine(isMultiline: true, """
                        using WindowsRuntimeObjectReferenceValue __baseInterface = WindowsRuntimeObjectMarshaller.ConvertToUnmanaged(baseInterface);
                        void* __innerInterface = default;
                """);
        }

        // For mapped value-type params (DateTime, TimeSpan), emit ABI local + marshaller conversion.
        for (int i = 0; i < paramCount; i++)
        {
            ParameterInfo p = sig.Parameters[i];

            if (!context.AbiTypeShapeResolver.IsMappedAbiValueType(p.Type))
            {
                continue;
            }

            string raw = p.GetRawName();
            string pname = IdentifierEscaping.EscapeIdentifier(raw);
            string abiType = AbiTypeHelpers.GetMappedAbiTypeName(p.Type);
            string marshaller = AbiTypeHelpers.GetMappedMarshallerName(p.Type);
            writer.WriteLine($"        {abiType} __{raw} = {marshaller}.ConvertToUnmanaged({pname});");
        }

        // For HResultException params, emit ABI local + ExceptionMarshaller conversion.
        // (HResult is excluded from IsMappedAbiValueType because it's "treated specially in many
        // places", but for activator factory ctor params the marshalling pattern is the same.)
        for (int i = 0; i < paramCount; i++)
        {
            ParameterInfo p = sig.Parameters[i];

            if (!p.Type.IsHResultException())
            {
                continue;
            }

            string raw = p.GetRawName();
            string pname = IdentifierEscaping.EscapeIdentifier(raw);
            writer.WriteLine($"        global::ABI.System.Exception __{raw} = global::ABI.System.ExceptionMarshaller.ConvertToUnmanaged({pname});");
        }

        // Declare InlineArray16 + ArrayPool fallback for non-blittable PassArray params
        // (runtime classes, objects, strings).
        bool hasNonBlittableArray = false;
        for (int i = 0; i < paramCount; i++)
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

            hasNonBlittableArray = true;
            string raw = p.GetRawName();
            string callName = IdentifierEscaping.EscapeIdentifier(raw);
            ArrayTempNames names = new(raw);
            writer.WriteLine();
            writer.WriteLine(isMultiline: true, $$"""
                        Unsafe.SkipInit(out InlineArray16<nint> {{names.InlineArray}});
                        nint[] {{names.ArrayFromPool}} = null;
                        Span<nint> {{names.Span}} = {{callName}}.Length <= 16
                            ? {{names.InlineArray}}[..{{callName}}.Length]
                            : ({{names.ArrayFromPool}} = global::System.Buffers.ArrayPool<nint>.Shared.Rent({{callName}}.Length));
                """);

            if (szArr.BaseType.IsString())
            {
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

        writer.WriteLine("        void* __retval = default;");

        if (hasNonBlittableArray)
        {
            writer.WriteLine(isMultiline: true, """
                        try
                        {
                """);
        }
        string baseIndent = hasNonBlittableArray ? "            " : "        ";

        // For System.Type params, pre-marshal to TypeReference (must be declared OUTSIDE the
        // fixed() block since the fixed block pins the resulting reference).
        for (int i = 0; i < paramCount; i++)
        {
            ParameterInfo p = sig.Parameters[i];

            if (!p.Type.IsSystemType())
            {
                continue;
            }

            string raw = p.GetRawName();
            string pname = IdentifierEscaping.EscapeIdentifier(raw);
            writer.WriteLine($"{baseIndent}global::ABI.System.TypeMarshaller.ConvertToUnmanagedUnsafe({pname}, out TypeReference __{raw});");
        }

        // Open ONE combined "fixed(void* _a = ..., _b = ..., ...)" block for ALL pinnable
        // params (string, Type, PassArray)..
        // which emits a single combined fixed-block for all is_pinnable marshalers.
        int fixedNesting = 0;
        int pinnableCount = 0;
        for (int i = 0; i < paramCount; i++)
        {
            ParameterInfo p = sig.Parameters[i];
            ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);

            if (p.Type.IsString() || p.Type.IsSystemType())
            {
                pinnableCount++;
            }
            else if (cat.IsArrayInput())
            {
                pinnableCount++;
            }
        }

        if (pinnableCount > 0)
        {
            string indent = baseIndent;
            writer.Write($"{indent}fixed(void* ");
            bool firstPin = true;
            for (int i = 0; i < paramCount; i++)
            {
                ParameterInfo p = sig.Parameters[i];
                ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);
                bool isStr = p.Type.IsString();
                bool isType = p.Type.IsSystemType();
                bool isArr = cat.IsArrayInput();

                if (!isStr && !isType && !isArr)
                {
                    continue;
                }

                string raw = p.GetRawName();
                string pname = IdentifierEscaping.EscapeIdentifier(raw);

                writer.WriteIf(!firstPin, ", ");

                firstPin = false;
                writer.Write($"_{raw} = ");

                if (isType)
                {
                    writer.Write($"__{raw}");
                }
                else if (isArr)
                {
                    TypeSignature elemT = ((SzArrayTypeSignature)p.Type).BaseType;
                    bool isBlittableElem = context.AbiTypeShapeResolver.IsBlittableAbiElement(elemT);
                    bool isStringElem = elemT.IsString();

                    if (isBlittableElem)
                    {
                        writer.Write(pname);
                    }
                    else { writer.Write($"__{raw}_span"); }

                    if (isStringElem)
                    {
                        writer.Write($", _{raw}_inlineHeaderArray = __{raw}_headerSpan");
                    }
                }
                else
                {
                    // string param: pin the input string itself.
                    writer.Write(pname);
                }
            }
            writer.WriteLine(isMultiline: true, $$"""
                )
                {{indent}}{
                """);
            fixedNesting = 1;
            // Inside the block: emit HStringMarshaller.ConvertToUnmanagedUnsafe for each
            // string input. The HStringReference local lives stack-only.
            string innerIndent = baseIndent + new string(' ', fixedNesting * 4);
            for (int i = 0; i < paramCount; i++)
            {
                ParameterInfo p = sig.Parameters[i];

                if (!p.Type.IsString())
                {
                    continue;
                }

                string raw = p.GetRawName();
                string pname = IdentifierEscaping.EscapeIdentifier(raw);
                writer.WriteLine($"{innerIndent}HStringMarshaller.ConvertToUnmanagedUnsafe((char*)_{raw}, {pname}?.Length, out HStringReference __{raw});");
            }
        }

        string callIndent = baseIndent + new string(' ', fixedNesting * 4);

        // Emit CopyToUnmanaged for non-blittable PassArray params.
        for (int i = 0; i < paramCount; i++)
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
            string pname = IdentifierEscaping.EscapeIdentifier(raw);
            ArrayTempNames names = new(raw);

            if (szArr.BaseType.IsString())
            {
                writer.WriteLine(isMultiline: true, $$"""
                    {{callIndent}}HStringArrayMarshaller.ConvertToUnmanagedUnsafe(
                    {{callIndent}}    source: {{pname}},
                    {{callIndent}}    hstringHeaders: (HStringHeader*) _{{raw}}_inlineHeaderArray,
                    {{callIndent}}    hstrings: {{names.Span}},
                    {{callIndent}}    pinnedGCHandles: {{names.PinnedHandleSpan}});
                    """);
            }
            else
            {
                WriteProjectionTypeCallback elementProjected = TypedefNameWriter.WriteProjectionType(context, TypeSemanticsFactory.Get(szArr.BaseType));
                writer.WriteLine(isMultiline: true, $$"""
                    {{callIndent}}[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "CopyToUnmanaged")]
                    {{callIndent}}static extern void CopyToUnmanaged_{{raw}}([UnsafeAccessorType("{{ArrayElementEncoder.GetArrayMarshallerInteropPath(szArr.BaseType)}}")] object _, ReadOnlySpan<{{elementProjected}}> span, uint length, void** data);
                    {{callIndent}}CopyToUnmanaged_{{raw}}(null, {{pname}}, (uint){{pname}}.Length, (void**)_{{raw}});
                    """);
            }
        }

        writer.Write($"{callIndent}RestrictedErrorInfo.ThrowExceptionForHR((*(delegate* unmanaged[MemberFunction]<void*, ");
        for (int i = 0; i < paramCount; i++)
        {
            ParameterInfo p = sig.Parameters[i];
            ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);

            if (cat.IsArrayInput())
            {
                writer.Write("uint, void*, ");
                continue;
            }

            WriteAbiTypeCallback abi = AbiTypeWriter.WriteAbiType(context, TypeSemanticsFactory.Get(p.Type));
            writer.Write($"{abi}, ");
        }

        if (isComposable)
        {
            // Composable extras: baseInterface (void*), out innerInterface (void**)
            writer.Write("void*, void**, ");
        }

        writer.Write($"void**, int>**)ThisPtr)[{(6 + factoryMethodIndex).ToString(CultureInfo.InvariantCulture)}](ThisPtr");
        for (int i = 0; i < paramCount; i++)
        {
            ParameterInfo p = sig.Parameters[i];
            ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);
            string raw = p.GetRawName();
            string pname = IdentifierEscaping.EscapeIdentifier(raw);
            writer.Write(isMultiline: true, """
                ,
                  
                """);
            if (cat.IsArrayInput())
            {
                writer.Write($"(uint){pname}.Length, _{raw}");
                continue;
            }

            // For enums, cast to underlying type. For bool, cast to byte. For char, cast to ushort.
            // For string params, use the marshalled HString from the fixed block.
            // For runtime class / object / generic instance params, use __<name>.GetThisPtrUnsafe().
            if (context.AbiTypeShapeResolver.IsEnumType(p.Type))
            {
                // No cast needed: function pointer signature uses the projected enum type.
                writer.Write(pname);
            }
            else if (p.Type is CorLibTypeSignature corlibBool &&
                     corlibBool.ElementType == ElementType.Boolean)
            {
                writer.Write(pname);
            }
            else if (p.Type is CorLibTypeSignature corlibChar &&
                     corlibChar.ElementType == ElementType.Char)
            {
                writer.Write(pname);
            }
            else if (p.Type.IsString())
            {
                writer.Write($"__{raw}.HString");
            }
            else if (p.Type.IsSystemType())
            {
                writer.Write($"__{raw}.ConvertToUnmanagedUnsafe()");
            }
            else if (context.AbiTypeShapeResolver.IsRuntimeClassOrInterface(p.Type) || p.Type.IsObject() || p.Type.IsGenericInstance())
            {
                writer.Write($"__{raw}.GetThisPtrUnsafe()");
            }
            else if (context.AbiTypeShapeResolver.IsMappedAbiValueType(p.Type))
            {
                writer.Write($"__{raw}");
            }
            else if (p.Type.IsHResultException())
            {
                writer.Write($"__{raw}");
            }
            else
            {
                writer.Write(pname);
            }
        }

        if (isComposable)
        {
            // Pass __baseInterface.GetThisPtrUnsafe() and &__innerInterface.
            writer.Write(isMultiline: true, """
                ,
                  __baseInterface.GetThisPtrUnsafe(),
                  &__innerInterface
                """);
        }
        writer.WriteLine(isMultiline: true, """
            ,
              &__retval));
            """);
        if (isComposable)
        {
            writer.WriteLine($"{callIndent}innerInterface = __innerInterface;");
        }

        writer.WriteLine($"{callIndent}retval = __retval;");

        // Close fixed blocks (innermost first).
        for (int i = fixedNesting - 1; i >= 0; i--)
        {
            string indent = baseIndent + new string(' ', i * 4);
            writer.WriteLine($"{indent}}}");
        }

        // Close try and emit finally with cleanup for non-blittable PassArray params.
        if (hasNonBlittableArray)
        {
            writer.WriteLine(isMultiline: true, """
                        }
                        finally
                        {
                """);
            for (int i = 0; i < paramCount; i++)
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
                ArrayTempNames names = new(raw);

                if (szArr.BaseType.IsString())
                {
                    writer.WriteLine();
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
                else
                {
                    writer.WriteLine();
                    writer.WriteLine(isMultiline: true, $$"""
                                    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "Dispose")]
                                    static extern void Dispose_{{raw}}([UnsafeAccessorType("{{ArrayElementEncoder.GetArrayMarshallerInteropPath(szArr.BaseType)}}")] object _, uint length, void** data);
                        
                                    fixed(void* _{{raw}} = {{names.Span}})
                                    {
                                        Dispose_{{raw}}(null, (uint) {{names.Span}}.Length, (void**)_{{raw}});
                                    }
                        """);
                }
                writer.WriteLine();
                writer.WriteLine(isMultiline: true, $$"""
                                if ({{names.ArrayFromPool}} is not null)
                                {
                                    global::System.Buffers.ArrayPool<nint>.Shared.Return({{names.ArrayFromPool}});
                                }
                    """);
            }
            writer.WriteLine("        }");
        }

        writer.WriteLine(isMultiline: true, """
                }
            }
            """);
    }

    /// <summary>
    /// Returns the IID expression for the class's default interface.
    /// </summary>
    private static string GetDefaultInterfaceIid(ProjectionEmitContext context, TypeDefinition classType)
    {
        ITypeDefOrRef? defaultIface = classType.GetDefaultInterface();

        if (defaultIface is null)
        {
            return "default(global::System.Guid)";
        }

        string result = ObjRefNameGenerator.WriteIidExpression(context, defaultIface).Format();
        return result;
    }
}
