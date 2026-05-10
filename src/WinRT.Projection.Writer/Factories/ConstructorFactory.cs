// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Models;
using WindowsRuntime.ProjectionWriter.Extensions;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter;

/// <summary>
/// Activator/composer constructor emission.
/// </summary>
internal static class ConstructorFactory
{
    /// <summary>
    /// Emits the activator and composer constructor wrappers for the given runtime class.
    /// </summary>
    public static void WriteAttributedTypes(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition classType)
    {
        if (context.Cache is null) { return; }

        // Track whether we need to emit the static _objRef_<RuntimeClassName> field (used by
        // default constructors). Emit it once per class if any [Activatable] factory exists.
        bool needsClassObjRef = false;

        foreach (KeyValuePair<string, AttributedType> kv in AttributedTypes.Get(classType, context.Cache))
        {
            AttributedType factory = kv.Value;
            if (factory.Activatable && factory.Type is null)
            {
                needsClassObjRef = true;
                break;
            }
        }

        if (needsClassObjRef)
        {
            string fullName = (classType.Namespace?.Value ?? string.Empty) + "." + (classType.Name?.Value ?? string.Empty);
            string objRefName = "_objRef_" + IIDExpressionWriter.EscapeTypeNameForIdentifier("global::" + fullName, stripGlobal: true);
            writer.WriteLine("");
            writer.Write($"private static WindowsRuntimeObjectReference {objRefName}");
            if (context.Settings.ReferenceProjection)
            {
                // in ref mode the activation factory objref getter body is just 'throw null;'.
                RefModeStubFactory.EmitRefModeObjRefGetterBody(writer);
            }
            else
            {
                writer.WriteLine("");
                writer.Write($$"""
                    {
                        get
                        {
                            var __{{objRefName}} = field;
                            if (__{{objRefName}} != null && __{{objRefName}}.IsInCurrentContext)
                            {
                                return __{{objRefName}};
                            }
                            return field = WindowsRuntimeObjectReference.GetActivationFactory("{{fullName}}");
                        }
                    }
                    """, isMultiline: true);
            }
        }

        foreach (KeyValuePair<string, AttributedType> kv in AttributedTypes.Get(classType, context.Cache))
        {
            AttributedType factory = kv.Value;
            if (factory.Activatable)
            {
                WriteFactoryConstructors(writer, context, factory.Type, classType);
            }
            else if (factory.Composable)
            {
                WriteComposableConstructors(writer, context, factory.Type, classType, factory.Visible ? "public" : "protected");
            }
        }
    }
    /// <summary>Emits the public constructors generated from a [Activatable] factory type.</summary>
    public static void WriteFactoryConstructors(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition? factoryType, TypeDefinition classType)
    {
        string typeName = classType.Name?.Value ?? string.Empty;
        int gcPressure = ClassFactory.GetGcPressureAmount(classType);
        if (factoryType is not null)
        {
            // Emit the factory objref property (lazy-initialized).
            string factoryRuntimeClassFullName = (classType.Namespace?.Value ?? string.Empty) + "." + typeName;
            string factoryObjRefName = ObjRefNameGenerator.GetObjRefName(context, factoryType);
            ClassFactory.WriteStaticFactoryObjRef(writer, context, factoryType, factoryRuntimeClassFullName, factoryObjRefName);

            string defaultIfaceIid = GetDefaultInterfaceIid(context, classType);
            string marshalingType = GetMarshalingTypeName(classType);
            // Compute the platform attribute string from the activation factory interface's
            // [ContractVersion] attribute
            IndentedTextWriter __scratchPlatform = new();
            CustomAttributeFactory.WritePlatformAttribute(__scratchPlatform, context, factoryType);
            string platformAttribute = __scratchPlatform.ToString();
            int methodIndex = 0;
            foreach (MethodDefinition method in factoryType.Methods)
            {
                if (method.IsSpecial()) { methodIndex++; continue; }
                MethodSig sig = new(method);
                string callbackName = (method.Name?.Value ?? "Create") + "_" + sig.Params.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
                string argsName = callbackName + "Args";

                // Emit the public constructor.
                writer.WriteLine("");
                if (!string.IsNullOrEmpty(platformAttribute)) { writer.Write(platformAttribute); }
                writer.Write($"public unsafe {typeName}(");
                MethodFactory.WriteParameterList(writer, context, sig);
                writer.Write("""
                    )
                      :base(
                    """, isMultiline: true);
                if (sig.Params.Count == 0)
                {
                    writer.Write("default");
                }
                else
                {
                    writer.Write($"{callbackName}.Instance, {defaultIfaceIid}, {marshalingType}, WindowsRuntimeActivationArgsReference.CreateUnsafe(new {argsName}(");
                    for (int i = 0; i < sig.Params.Count; i++)
                    {
                        if (i > 0) { writer.Write(", "); }
                        string raw = sig.Params[i].Parameter.Name ?? "param";
                        writer.Write(CSharpKeywords.IsKeyword(raw) ? "@" + raw : raw);
                    }
                    writer.Write("))");
                }
                writer.Write("""
                    )
                    {
                    """, isMultiline: true);
                if (gcPressure > 0)
                {
                    writer.WriteLine($"GC.AddMemoryPressure({gcPressure.ToString(System.Globalization.CultureInfo.InvariantCulture)});");
                }
                writer.WriteLine("}");

                if (sig.Params.Count > 0)
                {
                    EmitFactoryArgsStruct(writer, context, sig, argsName);
                    EmitFactoryCallbackClass(writer, context, sig, callbackName, argsName, factoryObjRefName, methodIndex);
                }

                methodIndex++;
            }
        }
        else
        {
            // No factory type means [Activatable(uint version)] - emit a default ctor that calls
            // the WindowsRuntimeObject base constructor with the activation factory objref.
            // The default interface IID is needed too.
            string fullName = (classType.Namespace?.Value ?? string.Empty) + "." + typeName;
            string objRefName = "_objRef_" + IIDExpressionWriter.EscapeTypeNameForIdentifier("global::" + fullName, stripGlobal: true);

            // Find the default interface IID to use.
            string defaultIfaceIid = GetDefaultInterfaceIid(context, classType);

            writer.WriteLine("");
            writer.Write($$"""
                public {{typeName}}()
                  :base(default(WindowsRuntimeActivationTypes.DerivedSealed), {{objRefName}}, {{defaultIfaceIid}}, {{GetMarshalingTypeName(classType)}})
                {
                """, isMultiline: true);
            if (gcPressure > 0)
            {
                writer.WriteLine($"GC.AddMemoryPressure({gcPressure.ToString(System.Globalization.CultureInfo.InvariantCulture)});");
            }
            writer.WriteLine("}");
        }
    }

    /// <summary>
    /// Reads the <c>[MarshalingBehaviorAttribute]</c> on the class and returns the corresponding
    /// <c>CreateObjectReferenceMarshalingType.*</c> expression.
    /// </summary>
    internal static string GetMarshalingTypeName(TypeDefinition classType)
    {
        for (int i = 0; i < classType.CustomAttributes.Count; i++)
        {
            CustomAttribute attr = classType.CustomAttributes[i];
            ITypeDefOrRef? attrType = attr.Constructor?.DeclaringType;
            if (attrType is null) { continue; }
            if (attrType.Namespace?.Value != "Windows.Foundation.Metadata" ||
                attrType.Name?.Value != "MarshalingBehaviorAttribute") { continue; }
            if (attr.Signature is null) { continue; }
            for (int j = 0; j < attr.Signature.FixedArguments.Count; j++)
            {
                AsmResolver.DotNet.Signatures.CustomAttributeArgument arg = attr.Signature.FixedArguments[j];
                if (arg.Element is int v)
                {
                    return v switch
                    {
                        2 => "CreateObjectReferenceMarshalingType.Agile",
                        3 => "CreateObjectReferenceMarshalingType.Standard",
                        _ => "CreateObjectReferenceMarshalingType.Unknown",
                    };
                }
            }
        }
        return "CreateObjectReferenceMarshalingType.Unknown";
    }

    /// <summary>Emits the <c>private readonly ref struct &lt;Name&gt;Args(args...) {...}</c>.</summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="context">The active emit context.</param>
    /// <param name="sig">The factory method signature whose parameters are turned into struct fields.</param>
    /// <param name="argsName">The simple name of the emitted args struct.</param>
    /// <param name="userParamCount">If &gt;= 0, only emit the first <paramref name="userParamCount"/>
    /// params (used for composable factories where the trailing baseInterface/innerInterface params
    /// are consumed by the callback Invoke signature directly, not stored in args).</param>
    private static void EmitFactoryArgsStruct(IndentedTextWriter writer, ProjectionEmitContext context, MethodSig sig, string argsName, int userParamCount = -1)
    {
        int count = userParamCount >= 0 ? userParamCount : sig.Params.Count;
        writer.WriteLine("");
        writer.Write($"private readonly ref struct {argsName}(");
        for (int i = 0; i < count; i++)
        {
            if (i > 0) { writer.Write(", "); }
            MethodFactory.WriteProjectionParameter(writer, context, sig.Params[i]);
        }
        writer.Write("""
            )
            {
            """, isMultiline: true);
        for (int i = 0; i < count; i++)
        {
            ParamInfo p = sig.Params[i];
            string raw = p.Parameter.Name ?? "param";
            string pname = CSharpKeywords.IsKeyword(raw) ? "@" + raw : raw;
            writer.Write("    public readonly ");
            // Use the parameter's projected type (matches the constructor parameter type, including
            // ReadOnlySpan<T>/Span<T> for array params).
            MethodFactory.WriteProjectionParameterType(writer, context, p);
            writer.WriteLine($" {pname} = {pname};");
        }
        writer.WriteLine("}");
    }

    /// <summary>Emits the <c>private sealed class &lt;Name&gt; : WindowsRuntimeActivationFactoryCallback.DerivedSealed</c>.</summary>
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
    private static void EmitFactoryCallbackClass(IndentedTextWriter writer, ProjectionEmitContext context, MethodSig sig, string callbackName, string argsName, string factoryObjRefName, int factoryMethodIndex, bool isComposable = false, int userParamCount = -1)
    {
        int paramCount = userParamCount >= 0 ? userParamCount : sig.Params.Count;
        string baseClass = isComposable
            ? "WindowsRuntimeActivationFactoryCallback.DerivedComposed"
            : "WindowsRuntimeActivationFactoryCallback.DerivedSealed";
        writer.WriteLine("");
        writer.Write($$"""
            private sealed class {{callbackName}} : {{baseClass}}
            {
                public static readonly {{callbackName}} Instance = new();
            
                [MethodImpl(MethodImplOptions.NoInlining)]
            """, isMultiline: true);
        if (isComposable)
        {
            // Composable Invoke signature is multi-line and includes baseInterface (in) +
            // innerInterface (out).
            writer.Write("""
                    public override unsafe void Invoke(
                      WindowsRuntimeActivationArgsReference additionalParameters,
                      WindowsRuntimeObject baseInterface,
                      out void* innerInterface,
                      out void* retval)
                    {
                """, isMultiline: true);
        }
        else
        {
            // Sealed Invoke signature is multi-line..
            writer.Write("""
                    public override unsafe void Invoke(
                      WindowsRuntimeActivationArgsReference additionalParameters,
                      out void* retval)
                    {
                """, isMultiline: true);
        }
        // Invoke body is just 'throw null;' (no factory dispatch, no marshalling).
        if (context.Settings.ReferenceProjection)
        {
            RefModeStubFactory.EmitRefModeInvokeBody(writer);
            return;
        }

        writer.Write($$"""
                    using WindowsRuntimeObjectReferenceValue activationFactoryValue = {{factoryObjRefName}}.AsValue();
                    void* ThisPtr = activationFactoryValue.GetThisPtrUnsafe();
                    ref readonly {{argsName}} args = ref additionalParameters.GetValueRefUnsafe<{{argsName}}>();
            """, isMultiline: true);

        // Bind each arg from the args struct to a local of its ABI-marshalable input type.
        // Bind arg locals.
        for (int i = 0; i < paramCount; i++)
        {
            ParamInfo p = sig.Params[i];
            string raw = p.Parameter.Name ?? "param";
            string pname = CSharpKeywords.IsKeyword(raw) ? "@" + raw : raw;
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            writer.Write("        ");
            // For array params, the bind type is ReadOnlySpan<T> / Span<T> (not the SzArray).
            if (cat == ParamCategory.PassArray)
            {
                writer.Write("ReadOnlySpan<");
                TypedefNameWriter.WriteProjectionType(writer, context, TypeSemanticsFactory.Get(((AsmResolver.DotNet.Signatures.SzArrayTypeSignature)p.Type).BaseType));
                writer.Write(">");
            }
            else if (cat == ParamCategory.FillArray)
            {
                writer.Write("Span<");
                TypedefNameWriter.WriteProjectionType(writer, context, TypeSemanticsFactory.Get(((AsmResolver.DotNet.Signatures.SzArrayTypeSignature)p.Type).BaseType));
                writer.Write(">");
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
            ParamInfo p = sig.Params[i];
            if (!p.Type.IsGenericInstance()) { continue; }
            string raw = p.Parameter.Name ?? "param";
            string pname = CSharpKeywords.IsKeyword(raw) ? "@" + raw : raw;
            if (p.Type.IsNullableT())
            {
                AsmResolver.DotNet.Signatures.TypeSignature inner = p.Type.GetNullableInnerType()!;
                string innerMarshaller = AbiTypeHelpers.GetNullableInnerMarshallerName(writer, context, inner);
                writer.WriteLine($"        using WindowsRuntimeObjectReferenceValue __{raw} = {innerMarshaller}.BoxToUnmanaged({pname});");
                continue;
            }
            string interopTypeName = InteropTypeNameWriter.EncodeInteropTypeName(p.Type, TypedefNameType.ABI) + ", WinRT.Interop";
            IndentedTextWriter __scratchProjType = new();
            MethodFactory.WriteProjectedSignature(__scratchProjType, context, p.Type, false);
            string projectedTypeName = __scratchProjType.ToString();
            writer.Write($$"""
                        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "ConvertToUnmanaged")]
                        static extern WindowsRuntimeObjectReferenceValue ConvertToUnmanaged_{{raw}}([UnsafeAccessorType("{{interopTypeName}}")] object _, {{projectedTypeName}} value);
                        using WindowsRuntimeObjectReferenceValue __{{raw}} = ConvertToUnmanaged_{{raw}}(null, {{pname}});
                """, isMultiline: true);
        }

        // For runtime class / object params, emit `using WindowsRuntimeObjectReferenceValue __<name> = ...ConvertToUnmanaged(<name>);`
        for (int i = 0; i < paramCount; i++)
        {
            ParamInfo p = sig.Params[i];
            if (p.Type.IsGenericInstance()) { continue; } // already handled above
            if (!AbiTypeHelpers.IsRuntimeClassOrInterface(context.Cache, p.Type) && !p.Type.IsObject()) { continue; }
            string raw = p.Parameter.Name ?? "param";
            string pname = CSharpKeywords.IsKeyword(raw) ? "@" + raw : raw;
            writer.Write($"        using WindowsRuntimeObjectReferenceValue __{raw} = ");
            AbiMethodBodyFactory.EmitMarshallerConvertToUnmanaged(writer, context, p.Type, pname);
            writer.WriteLine(";");
        }

        // For composable factories, marshal the additional `baseInterface` (which is a
        // WindowsRuntimeObject parameter on Invoke, not an args field).
        //   using WindowsRuntimeObjectReferenceValue __baseInterface = WindowsRuntimeObjectMarshaller.ConvertToUnmanaged(baseInterface);
        if (isComposable)
        {
            writer.Write("""
                        using WindowsRuntimeObjectReferenceValue __baseInterface = WindowsRuntimeObjectMarshaller.ConvertToUnmanaged(baseInterface);
                        void* __innerInterface = default;
                """, isMultiline: true);
        }

        // For mapped value-type params (DateTime, TimeSpan), emit ABI local + marshaller conversion.
        for (int i = 0; i < paramCount; i++)
        {
            ParamInfo p = sig.Params[i];
            if (!AbiTypeHelpers.IsMappedAbiValueType(p.Type)) { continue; }
            string raw = p.Parameter.Name ?? "param";
            string pname = CSharpKeywords.IsKeyword(raw) ? "@" + raw : raw;
            string abiType = AbiTypeHelpers.GetMappedAbiTypeName(p.Type);
            string marshaller = AbiTypeHelpers.GetMappedMarshallerName(p.Type);
            writer.WriteLine($"        {abiType} __{raw} = {marshaller}.ConvertToUnmanaged({pname});");
        }

        // For HResultException params, emit ABI local + ExceptionMarshaller conversion.
        // (HResult is excluded from IsMappedAbiValueType because it's "treated specially in many
        // places", but for activator factory ctor params the marshalling pattern is the same.)
        for (int i = 0; i < paramCount; i++)
        {
            ParamInfo p = sig.Params[i];
            if (!p.Type.IsHResultException()) { continue; }
            string raw = p.Parameter.Name ?? "param";
            string pname = CSharpKeywords.IsKeyword(raw) ? "@" + raw : raw;
            writer.WriteLine($"        global::ABI.System.Exception __{raw} = global::ABI.System.ExceptionMarshaller.ConvertToUnmanaged({pname});");
        }

        // Declare InlineArray16 + ArrayPool fallback for non-blittable PassArray params
        // (runtime classes, objects, strings).
        bool hasNonBlittableArray = false;
        for (int i = 0; i < paramCount; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat is not (ParamCategory.PassArray or ParamCategory.FillArray)) { continue; }
            if (p.Type is not AsmResolver.DotNet.Signatures.SzArrayTypeSignature szArr) { continue; }
            if (AbiTypeHelpers.IsBlittablePrimitive(context.Cache, szArr.BaseType) || AbiTypeHelpers.IsAnyStruct(context.Cache, szArr.BaseType)) { continue; }
            hasNonBlittableArray = true;
            string raw = p.Parameter.Name ?? "param";
            string callName = CSharpKeywords.IsKeyword(raw) ? "@" + raw : raw;
            writer.WriteLine("");
            writer.Write($$"""
                        Unsafe.SkipInit(out InlineArray16<nint> __{{raw}}_inlineArray);
                        nint[] __{{raw}}_arrayFromPool = null;
                        Span<nint> __{{raw}}_span = {{callName}}.Length <= 16
                            ? __{{raw}}_inlineArray[..{{callName}}.Length]
                            : (__{{raw}}_arrayFromPool = global::System.Buffers.ArrayPool<nint>.Shared.Rent({{callName}}.Length));
                """, isMultiline: true);

            if (szArr.BaseType.IsString())
            {
                writer.WriteLine("");
                writer.Write($$"""
                            Unsafe.SkipInit(out InlineArray16<HStringHeader> __{{raw}}_inlineHeaderArray);
                            HStringHeader[] __{{raw}}_headerArrayFromPool = null;
                            Span<HStringHeader> __{{raw}}_headerSpan = {{callName}}.Length <= 16
                                ? __{{raw}}_inlineHeaderArray[..{{callName}}.Length]
                                : (__{{raw}}_headerArrayFromPool = global::System.Buffers.ArrayPool<HStringHeader>.Shared.Rent({{callName}}.Length));
                    
                            Unsafe.SkipInit(out InlineArray16<nint> __{{raw}}_inlinePinnedHandleArray);
                            nint[] __{{raw}}_pinnedHandleArrayFromPool = null;
                            Span<nint> __{{raw}}_pinnedHandleSpan = {{callName}}.Length <= 16
                                ? __{{raw}}_inlinePinnedHandleArray[..{{callName}}.Length]
                                : (__{{raw}}_pinnedHandleArrayFromPool = global::System.Buffers.ArrayPool<nint>.Shared.Rent({{callName}}.Length));
                    """, isMultiline: true);
            }
        }

        writer.WriteLine("        void* __retval = default;");
        if (hasNonBlittableArray)
        {
            writer.Write("""
                        try
                        {
                """, isMultiline: true);
        }
        string baseIndent = hasNonBlittableArray ? "            " : "        ";

        // For System.Type params, pre-marshal to TypeReference (must be declared OUTSIDE the
        // fixed() block since the fixed block pins the resulting reference).
        for (int i = 0; i < paramCount; i++)
        {
            ParamInfo p = sig.Params[i];
            if (!p.Type.IsSystemType()) { continue; }
            string raw = p.Parameter.Name ?? "param";
            string pname = CSharpKeywords.IsKeyword(raw) ? "@" + raw : raw;
            writer.WriteLine($"{baseIndent}global::ABI.System.TypeMarshaller.ConvertToUnmanagedUnsafe({pname}, out TypeReference __{raw});");
        }

        // Open ONE combined "fixed(void* _a = ..., _b = ..., ...)" block for ALL pinnable
        // params (string, Type, PassArray)..
        // which emits a single combined fixed-block for all is_pinnable marshalers.
        int fixedNesting = 0;
        int pinnableCount = 0;
        for (int i = 0; i < paramCount; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (p.Type.IsString() || p.Type.IsSystemType()) { pinnableCount++; }
            else if (cat is ParamCategory.PassArray or ParamCategory.FillArray) { pinnableCount++; }
        }
        if (pinnableCount > 0)
        {
            string indent = baseIndent;
            writer.Write($"{indent}fixed(void* ");
            bool firstPin = true;
            for (int i = 0; i < paramCount; i++)
            {
                ParamInfo p = sig.Params[i];
                ParamCategory cat = ParamHelpers.GetParamCategory(p);
                bool isStr = p.Type.IsString();
                bool isType = p.Type.IsSystemType();
                bool isArr = cat is ParamCategory.PassArray or ParamCategory.FillArray;
                if (!isStr && !isType && !isArr) { continue; }
                string raw = p.Parameter.Name ?? "param";
                string pname = CSharpKeywords.IsKeyword(raw) ? "@" + raw : raw;
                if (!firstPin) { writer.Write(", "); }
                firstPin = false;
                writer.Write($"_{raw} = ");
                if (isType) { writer.Write($"__{raw}"); }
                else if (isArr)
                {
                    AsmResolver.DotNet.Signatures.TypeSignature elemT = ((AsmResolver.DotNet.Signatures.SzArrayTypeSignature)p.Type).BaseType;
                    bool isBlittableElem = AbiTypeHelpers.IsBlittablePrimitive(context.Cache, elemT) || AbiTypeHelpers.IsAnyStruct(context.Cache, elemT);
                    bool isStringElem = elemT.IsString();
                    if (isBlittableElem) { writer.Write(pname); }
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
            writer.Write($$"""
                )
                {{indent}}{
                """, isMultiline: true);
            fixedNesting = 1;
            // Inside the block: emit HStringMarshaller.ConvertToUnmanagedUnsafe for each
            // string input. The HStringReference local lives stack-only.
            string innerIndent = baseIndent + new string(' ', fixedNesting * 4);
            for (int i = 0; i < paramCount; i++)
            {
                ParamInfo p = sig.Params[i];
                if (!p.Type.IsString()) { continue; }
                string raw = p.Parameter.Name ?? "param";
                string pname = CSharpKeywords.IsKeyword(raw) ? "@" + raw : raw;
                writer.WriteLine($"{innerIndent}HStringMarshaller.ConvertToUnmanagedUnsafe((char*)_{raw}, {pname}?.Length, out HStringReference __{raw});");
            }
        }

        string callIndent = baseIndent + new string(' ', fixedNesting * 4);

        // Emit CopyToUnmanaged for non-blittable PassArray params.
        for (int i = 0; i < paramCount; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat is not (ParamCategory.PassArray or ParamCategory.FillArray)) { continue; }
            if (p.Type is not AsmResolver.DotNet.Signatures.SzArrayTypeSignature szArr) { continue; }
            if (AbiTypeHelpers.IsBlittablePrimitive(context.Cache, szArr.BaseType) || AbiTypeHelpers.IsAnyStruct(context.Cache, szArr.BaseType)) { continue; }
            string raw = p.Parameter.Name ?? "param";
            string pname = CSharpKeywords.IsKeyword(raw) ? "@" + raw : raw;
            if (szArr.BaseType.IsString())
            {
                writer.Write($$"""
                    {{callIndent}}HStringArrayMarshaller.ConvertToUnmanagedUnsafe(
                    {{callIndent}}    source: {{pname}},
                    {{callIndent}}    hstringHeaders: (HStringHeader*) _{{raw}}_inlineHeaderArray,
                    {{callIndent}}    hstrings: __{{raw}}_span,
                    {{callIndent}}    pinnedGCHandles: __{{raw}}_pinnedHandleSpan);
                    """, isMultiline: true);
            }
            else
            {
                IndentedTextWriter __scratchElement = new();
                TypedefNameWriter.WriteProjectionType(__scratchElement, context, TypeSemanticsFactory.Get(szArr.BaseType));
                string elementProjected = __scratchElement.ToString();
                string elementInteropArg = InteropTypeNameWriter.EncodeInteropTypeName(szArr.BaseType, TypedefNameType.Projected);
                _ = elementInteropArg;
                writer.Write($$"""
                    {{callIndent}}[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "CopyToUnmanaged")]
                    {{callIndent}}static extern void CopyToUnmanaged_{{raw}}([UnsafeAccessorType("{{ArrayElementEncoder.GetArrayMarshallerInteropPath(szArr.BaseType)}}")] object _, ReadOnlySpan<{{elementProjected}}> span, uint length, void** data);
                    {{callIndent}}CopyToUnmanaged_{{raw}}(null, {{pname}}, (uint){{pname}}.Length, (void**)_{{raw}});
                    """, isMultiline: true);
            }
        }

        writer.Write($"{callIndent}RestrictedErrorInfo.ThrowExceptionForHR((*(delegate* unmanaged[MemberFunction]<void*, ");
        for (int i = 0; i < paramCount; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat is ParamCategory.PassArray or ParamCategory.FillArray)
            {
                writer.Write("uint, void*, ");
                continue;
            }
            AbiTypeWriter.WriteAbiType(writer, context, TypeSemanticsFactory.Get(p.Type));
            writer.Write(", ");
        }
        if (isComposable)
        {
            // Composable extras: baseInterface (void*), out innerInterface (void**)
            writer.Write("void*, void**, ");
        }
        writer.Write($"void**, int>**)ThisPtr)[{(6 + factoryMethodIndex).ToString(System.Globalization.CultureInfo.InvariantCulture)}](ThisPtr");
        for (int i = 0; i < paramCount; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            string raw = p.Parameter.Name ?? "param";
            string pname = CSharpKeywords.IsKeyword(raw) ? "@" + raw : raw;
            writer.Write("""
                ,
                  
                """, isMultiline: true);
            if (cat is ParamCategory.PassArray or ParamCategory.FillArray)
            {
                writer.Write($"(uint){pname}.Length, _{raw}");
                continue;
            }
            // For enums, cast to underlying type. For bool, cast to byte. For char, cast to ushort.
            // For string params, use the marshalled HString from the fixed block.
            // For runtime class / object / generic instance params, use __<name>.GetThisPtrUnsafe().
            if (AbiTypeHelpers.IsEnumType(context.Cache, p.Type))
            {
                // No cast needed: function pointer signature uses the projected enum type.
                writer.Write(pname);
            }
            else if (p.Type is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlibBool &&
                     corlibBool.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Boolean)
            {
                writer.Write(pname);
            }
            else if (p.Type is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlibChar &&
                     corlibChar.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Char)
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
            else if (AbiTypeHelpers.IsRuntimeClassOrInterface(context.Cache, p.Type) || p.Type.IsObject() || p.Type.IsGenericInstance())
            {
                writer.Write($"__{raw}.GetThisPtrUnsafe()");
            }
            else if (AbiTypeHelpers.IsMappedAbiValueType(p.Type))
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
            writer.Write("""
                ,
                  __baseInterface.GetThisPtrUnsafe(),
                  &__innerInterface
                """, isMultiline: true);
        }
        writer.Write("""
            ,
              &__retval));
            """, isMultiline: true);
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
            writer.Write("""
                        }
                        finally
                        {
                """, isMultiline: true);
            for (int i = 0; i < paramCount; i++)
            {
                ParamInfo p = sig.Params[i];
                ParamCategory cat = ParamHelpers.GetParamCategory(p);
                if (cat is not (ParamCategory.PassArray or ParamCategory.FillArray)) { continue; }
                if (p.Type is not AsmResolver.DotNet.Signatures.SzArrayTypeSignature szArr) { continue; }
                if (AbiTypeHelpers.IsBlittablePrimitive(context.Cache, szArr.BaseType) || AbiTypeHelpers.IsAnyStruct(context.Cache, szArr.BaseType)) { continue; }
                string raw = p.Parameter.Name ?? "param";
                if (szArr.BaseType.IsString())
                {
                    writer.WriteLine("");
                    writer.Write($$"""
                                    HStringArrayMarshaller.Dispose(__{{raw}}_pinnedHandleSpan);
                        
                                    if (__{{raw}}_pinnedHandleArrayFromPool is not null)
                                    {
                                        global::System.Buffers.ArrayPool<nint>.Shared.Return(__{{raw}}_pinnedHandleArrayFromPool);
                                    }
                        
                                    if (__{{raw}}_headerArrayFromPool is not null)
                                    {
                                        global::System.Buffers.ArrayPool<HStringHeader>.Shared.Return(__{{raw}}_headerArrayFromPool);
                                    }
                        """, isMultiline: true);
                }
                else
                {
                    string elementInteropArg = InteropTypeNameWriter.EncodeInteropTypeName(szArr.BaseType, TypedefNameType.Projected);
                    _ = elementInteropArg;
                    writer.WriteLine("");
                    writer.Write($$"""
                                    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "Dispose")]
                                    static extern void Dispose_{{raw}}([UnsafeAccessorType("{{ArrayElementEncoder.GetArrayMarshallerInteropPath(szArr.BaseType)}}")] object _, uint length, void** data);
                        
                                    fixed(void* _{{raw}} = __{{raw}}_span)
                                    {
                                        Dispose_{{raw}}(null, (uint) __{{raw}}_span.Length, (void**)_{{raw}});
                                    }
                        """, isMultiline: true);
                }
                writer.WriteLine("");
                writer.Write($$"""
                                if (__{{raw}}_arrayFromPool is not null)
                                {
                                    global::System.Buffers.ArrayPool<nint>.Shared.Return(__{{raw}}_arrayFromPool);
                                }
                    """, isMultiline: true);
            }
            writer.WriteLine("        }");
        }

        writer.Write("""
                }
            }
            """, isMultiline: true);
    }

    /// <summary>Returns the IID expression for the class's default interface.</summary>
    private static string GetDefaultInterfaceIid(ProjectionEmitContext context, TypeDefinition classType)
    {
        ITypeDefOrRef? defaultIface = classType.GetDefaultInterface();
        if (defaultIface is null) { return "default(global::System.Guid)"; }
        IndentedTextWriter __scratchIid = new();
        ObjRefNameGenerator.WriteIidExpression(__scratchIid, context, defaultIface);
        return __scratchIid.ToString();
    }
    /// <summary>
    /// Emits:
    /// 1. Public/protected constructors for each composable factory method (with proper body).
    /// 2. Static factory callback class (per ctor) for parameterized composable activation.
    /// 3. Four protected base-chaining constructors used by derived projected types.
    /// </summary>
    public static void WriteComposableConstructors(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition? composableType, TypeDefinition classType, string visibility)
    {
        if (composableType is null) { return; }
        string typeName = classType.Name?.Value ?? string.Empty;

        // Emit the factory objref + IIDs at the top so the parameterized ctors can reference it.
        if (composableType.Methods.Count > 0)
        {
            string runtimeClassFullName = (classType.Namespace?.Value ?? string.Empty) + "." + typeName;
            string factoryObjRefName = ObjRefNameGenerator.GetObjRefName(context, composableType);
            ClassFactory.WriteStaticFactoryObjRef(writer, context, composableType, runtimeClassFullName, factoryObjRefName);
        }

        string defaultIfaceIid = GetDefaultInterfaceIid(context, classType);
        string marshalingType = GetMarshalingTypeName(classType);
        string defaultIfaceObjRef;
        ITypeDefOrRef? defaultIface = classType.GetDefaultInterface();
        defaultIfaceObjRef = defaultIface is not null ? ObjRefNameGenerator.GetObjRefName(context, defaultIface) : string.Empty;
        int gcPressure = ClassFactory.GetGcPressureAmount(classType);
        // Compute the platform attribute string from the composable factory interface's
        // [ContractVersion] attribute
        IndentedTextWriter __scratchPlatform = new();
        CustomAttributeFactory.WritePlatformAttribute(__scratchPlatform, context, composableType);
        string platformAttribute = __scratchPlatform.ToString();

        int methodIndex = 0;
        foreach (MethodDefinition method in composableType.Methods)
        {
            if (method.IsSpecial()) { methodIndex++; continue; }
            // Composable factory methods have signature like:
            //   T CreateInstance(args, object baseInterface, out object innerInterface)
            // For the constructor on the projected class, we exclude the trailing two params.
            MethodSig sig = new(method);
            int userParamCount = sig.Params.Count >= 2 ? sig.Params.Count - 2 : sig.Params.Count;
            // the callback / args type name suffix is the TOTAL ABI param count
            // (size(method.Signature().Params())), NOT the user-visible param count. Using the
            // total count guarantees uniqueness against other composable factory overloads that
            // might share the same user-param count but differ in trailing baseInterface shape.
            string callbackName = (method.Name?.Value ?? "Create") + "_" + sig.Params.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string argsName = callbackName + "Args";
            bool isParameterless = userParamCount == 0;

            writer.WriteLine("");
            if (!string.IsNullOrEmpty(platformAttribute)) { writer.Write(platformAttribute); }
            writer.Write(visibility);
            if (!isParameterless) { writer.Write(" unsafe "); } else { writer.Write(" "); }
            writer.Write($"{typeName}(");
            for (int i = 0; i < userParamCount; i++)
            {
                if (i > 0) { writer.Write(", "); }
                MethodFactory.WriteProjectionParameter(writer, context, sig.Params[i]);
            }
            writer.Write("""
                )
                  :base(
                """, isMultiline: true);
            if (isParameterless)
            {
                // base(default(WindowsRuntimeActivationTypes.DerivedComposed), <factoryObjRef>, <iid>, <marshalingType>)
                string factoryObjRef = ObjRefNameGenerator.GetObjRefName(context, composableType);
                writer.Write($"default(WindowsRuntimeActivationTypes.DerivedComposed), {factoryObjRef}, {defaultIfaceIid}, {marshalingType}");
            }
            else
            {
                writer.Write($"{callbackName}.Instance, {defaultIfaceIid}, {marshalingType}, WindowsRuntimeActivationArgsReference.CreateUnsafe(new {argsName}(");
                for (int i = 0; i < userParamCount; i++)
                {
                    if (i > 0) { writer.Write(", "); }
                    string raw = sig.Params[i].Parameter.Name ?? "param";
                    writer.Write(CSharpKeywords.IsKeyword(raw) ? "@" + raw : raw);
                }
                writer.Write("))");
            }
            writer.Write($$"""
                )
                {
                if (GetType() == typeof({{typeName}}))
                {
                """, isMultiline: true);
            if (!string.IsNullOrEmpty(defaultIfaceObjRef))
            {
                writer.WriteLine($"{defaultIfaceObjRef} = NativeObjectReference;");
            }
            writer.WriteLine("}");
            if (gcPressure > 0)
            {
                writer.WriteLine($"GC.AddMemoryPressure({gcPressure.ToString(System.Globalization.CultureInfo.InvariantCulture)});");
            }
            writer.WriteLine("}");

            // Emit args struct + callback class for parameterized composable factories.
            // skips both the args struct AND the callback class entirely in ref mode. The
            // public ctor above still references these types, but reference assemblies don't
            // need their bodies' references to resolve (only the public API surface matters).
            if (!isParameterless && !context.Settings.ReferenceProjection)
            {
                EmitFactoryArgsStruct(writer, context, sig, argsName, userParamCount);
                string factoryObjRefName = ObjRefNameGenerator.GetObjRefName(context, composableType);
                EmitFactoryCallbackClass(writer, context, sig, callbackName, argsName, factoryObjRefName, methodIndex, isComposable: true, userParamCount: userParamCount);
            }

            methodIndex++;
        }

        if (context.Settings.ReferenceProjection) { return; }

        // Emit the four base-chaining constructors used by derived projected types.
        string gcPressureBody = gcPressure > 0
            ? "GC.AddMemoryPressure(" + gcPressure.ToString(System.Globalization.CultureInfo.InvariantCulture) + ");"
            : string.Empty;

        // 1. WindowsRuntimeActivationTypes.DerivedComposed
        writer.WriteLine("");
        writer.Write($$"""
            protected {{typeName}}(WindowsRuntimeActivationTypes.DerivedComposed _, WindowsRuntimeObjectReference activationFactoryObjectReference, in Guid iid, CreateObjectReferenceMarshalingType marshalingType)
              :base(_, activationFactoryObjectReference, in iid, marshalingType)
            {
            """, isMultiline: true);
        if (!string.IsNullOrEmpty(gcPressureBody)) { writer.WriteLine(gcPressureBody); }
        writer.Write($$"""
            }
            
            protected {{typeName}}(WindowsRuntimeActivationTypes.DerivedSealed _, WindowsRuntimeObjectReference activationFactoryObjectReference, in Guid iid, CreateObjectReferenceMarshalingType marshalingType)
              :base(_, activationFactoryObjectReference, in iid, marshalingType)
            {
            """, isMultiline: true);
        if (!string.IsNullOrEmpty(gcPressureBody)) { writer.WriteLine(gcPressureBody); }
        writer.Write($$"""
            }
            
            protected {{typeName}}(WindowsRuntimeActivationFactoryCallback.DerivedComposed activationFactoryCallback, in Guid iid, CreateObjectReferenceMarshalingType marshalingType, WindowsRuntimeActivationArgsReference additionalParameters)
              :base(activationFactoryCallback, in iid, marshalingType, additionalParameters)
            {
            """, isMultiline: true);
        if (!string.IsNullOrEmpty(gcPressureBody)) { writer.WriteLine(gcPressureBody); }
        writer.Write($$"""
            }
            
            protected {{typeName}}(WindowsRuntimeActivationFactoryCallback.DerivedSealed activationFactoryCallback, in Guid iid, CreateObjectReferenceMarshalingType marshalingType, WindowsRuntimeActivationArgsReference additionalParameters)
              :base(activationFactoryCallback, in iid, marshalingType, additionalParameters)
            {
            """, isMultiline: true);
        if (!string.IsNullOrEmpty(gcPressureBody)) { writer.WriteLine(gcPressureBody); }
        writer.WriteLine("}");
    }
}