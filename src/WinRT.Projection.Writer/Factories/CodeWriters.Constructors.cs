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
internal static partial class CodeWriters
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
            writer.Write("\nprivate static WindowsRuntimeObjectReference ");
            writer.Write(objRefName);
            if (context.Settings.ReferenceProjection)
            {
                // in ref mode the activation factory objref getter body is just 'throw null;'.
                EmitRefModeObjRefGetterBody(writer);
            }
            else
            {
                writer.Write("\n{\n    get\n    {\n        var __");
                writer.Write(objRefName);
                writer.Write(" = field;\n        if (__");
                writer.Write(objRefName);
                writer.Write(" != null && __");
                writer.Write(objRefName);
                writer.Write(".IsInCurrentContext)\n        {\n            return __");
                writer.Write(objRefName);
                writer.Write(";\n        }\n        return field = WindowsRuntimeObjectReference.GetActivationFactory(\"");
                writer.Write(fullName);
                writer.Write("\");\n    }\n}\n");
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
        int gcPressure = GetGcPressureAmount(classType);
        if (factoryType is not null)
        {
            // Emit the factory objref property (lazy-initialized).
            string factoryRuntimeClassFullName = (classType.Namespace?.Value ?? string.Empty) + "." + typeName;
            string factoryObjRefName = ObjRefNameGenerator.GetObjRefName(context, factoryType);
            WriteStaticFactoryObjRef(writer, context, factoryType, factoryRuntimeClassFullName, factoryObjRefName);

            string defaultIfaceIid = GetDefaultInterfaceIid(context, classType);
            string marshalingType = GetMarshalingTypeName(classType);
            // Compute the platform attribute string from the activation factory interface's
            // [ContractVersion] attribute. Mirrors C++
            // 'auto platform_attribute = write_platform_attribute_temp(w, factory_type);'
            // emitted at line 2872 before the public ctor.
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
                writer.Write("\n");
                if (!string.IsNullOrEmpty(platformAttribute)) { writer.Write(platformAttribute); }
                writer.Write("public unsafe ");
                writer.Write(typeName);
                writer.Write("(");
                MethodFactory.WriteParameterList(writer, context, sig);
                writer.Write(")\n  :base(");
                if (sig.Params.Count == 0)
                {
                    writer.Write("default");
                }
                else
                {
                    writer.Write(callbackName);
                    writer.Write(".Instance, ");
                    writer.Write(defaultIfaceIid);
                    writer.Write(", ");
                    writer.Write(marshalingType);
                    writer.Write(", WindowsRuntimeActivationArgsReference.CreateUnsafe(new ");
                    writer.Write(argsName);
                    writer.Write("(");
                    for (int i = 0; i < sig.Params.Count; i++)
                    {
                        if (i > 0) { writer.Write(", "); }
                        string raw = sig.Params[i].Parameter.Name ?? "param";
                        writer.Write(CSharpKeywords.IsKeyword(raw) ? "@" + raw : raw);
                    }
                    writer.Write("))");
                }
                writer.Write(")\n{\n");
                if (gcPressure > 0)
                {
                    writer.Write("GC.AddMemoryPressure(");
                    writer.Write(gcPressure.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    writer.Write(");\n");
                }
                writer.Write("}\n");

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

            writer.Write("\npublic ");
            writer.Write(typeName);
            writer.Write("()\n  :base(default(WindowsRuntimeActivationTypes.DerivedSealed), ");
            writer.Write(objRefName);
            writer.Write(", ");
            writer.Write(defaultIfaceIid);
            writer.Write(", ");
            writer.Write(GetMarshalingTypeName(classType));
            writer.Write(")\n{\n");
            if (gcPressure > 0)
            {
                writer.Write("GC.AddMemoryPressure(");
                writer.Write(gcPressure.ToString(System.Globalization.CultureInfo.InvariantCulture));
                writer.Write(");\n");
            }
            writer.Write("}\n");
        }
    }

    /// <summary>
    /// Reads the <c>[MarshalingBehaviorAttribute]</c> on the class and returns the corresponding
    /// <c>CreateObjectReferenceMarshalingType.*</c> expression. Mirrors C++
    /// <c>get_marshaling_type_name</c>.
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
    /// <param name="userParamCount">If &gt;= 0, only emit the first <paramref name="userParamCount"/>
    /// params (used for composable factories where the trailing baseInterface/innerInterface params
    /// are consumed by the callback Invoke signature directly, not stored in args).</param>
    private static void EmitFactoryArgsStruct(IndentedTextWriter writer, ProjectionEmitContext context, MethodSig sig, string argsName, int userParamCount = -1)
    {
        int count = userParamCount >= 0 ? userParamCount : sig.Params.Count;
        writer.Write("\nprivate readonly ref struct ");
        writer.Write(argsName);
        writer.Write("(");
        for (int i = 0; i < count; i++)
        {
            if (i > 0) { writer.Write(", "); }
            MethodFactory.WriteProjectionParameter(writer, context, sig.Params[i]);
        }
        writer.Write(")\n{\n");
        for (int i = 0; i < count; i++)
        {
            ParamInfo p = sig.Params[i];
            string raw = p.Parameter.Name ?? "param";
            string pname = CSharpKeywords.IsKeyword(raw) ? "@" + raw : raw;
            writer.Write("    public readonly ");
            // Use the parameter's projected type (matches the constructor parameter type, including
            // ReadOnlySpan<T>/Span<T> for array params).
            MethodFactory.WriteProjectionParameterType(writer, context, p);
            writer.Write(" ");
            writer.Write(pname);
            writer.Write(" = ");
            writer.Write(pname);
            writer.Write(";\n");
        }
        writer.Write("}\n");
    }

    /// <summary>Emits the <c>private sealed class &lt;Name&gt; : WindowsRuntimeActivationFactoryCallback.DerivedSealed</c>.</summary>
    /// <param name="isComposable">When true, emit the DerivedComposed callback variant whose
    /// Invoke signature includes the additional <c>WindowsRuntimeObject baseInterface</c> +
    /// <c>out void* innerInterface</c> params. Iteration over user params is bounded by
    /// <paramref name="userParamCount"/> (defaults to all params).</param>
    private static void EmitFactoryCallbackClass(IndentedTextWriter writer, ProjectionEmitContext context, MethodSig sig, string callbackName, string argsName, string factoryObjRefName, int factoryMethodIndex, bool isComposable = false, int userParamCount = -1)
    {
        int paramCount = userParamCount >= 0 ? userParamCount : sig.Params.Count;
        writer.Write("\nprivate sealed class ");
        writer.Write(callbackName);
        writer.Write(isComposable
            ? " : WindowsRuntimeActivationFactoryCallback.DerivedComposed\n{\n"
            : " : WindowsRuntimeActivationFactoryCallback.DerivedSealed\n{\n");
        writer.Write("    public static readonly ");
        writer.Write(callbackName);
        writer.Write(" Instance = new();\n\n");
        writer.Write("    [MethodImpl(MethodImplOptions.NoInlining)]\n");
        if (isComposable)
        {
            // Composable Invoke signature is multi-line and includes baseInterface (in) +
            // innerInterface (out). Mirrors truth output exactly.
            writer.Write("    public override unsafe void Invoke(\n");
            writer.Write("      WindowsRuntimeActivationArgsReference additionalParameters,\n");
            writer.Write("      WindowsRuntimeObject baseInterface,\n");
            writer.Write("      out void* innerInterface,\n");
            writer.Write("      out void* retval)\n    {\n");
        }
        else
        {
            // Sealed Invoke signature is multi-line. Mirrors C++ at.
            writer.Write("    public override unsafe void Invoke(\n");
            writer.Write("      WindowsRuntimeActivationArgsReference additionalParameters,\n");
            writer.Write("      out void* retval)\n    {\n");
        }
        // Invoke body is just 'throw null;' (no factory dispatch, no marshalling).
        if (context.Settings.ReferenceProjection)
        {
            EmitRefModeInvokeBody(writer);
            return;
        }

        writer.Write("        using WindowsRuntimeObjectReferenceValue activationFactoryValue = ");
        writer.Write(factoryObjRefName);
        writer.Write(".AsValue();\n");
        writer.Write("        void* ThisPtr = activationFactoryValue.GetThisPtrUnsafe();\n");
        writer.Write("        ref readonly ");
        writer.Write(argsName);
        writer.Write(" args = ref additionalParameters.GetValueRefUnsafe<");
        writer.Write(argsName);
        writer.Write(">();\n");

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
            writer.Write(" ");
            writer.Write(pname);
            writer.Write(" = args.");
            writer.Write(pname);
            writer.Write(";\n");
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
                string innerMarshaller = GetNullableInnerMarshallerName(writer, context, inner);
                writer.Write("        using WindowsRuntimeObjectReferenceValue __");
                writer.Write(raw);
                writer.Write(" = ");
                writer.Write(innerMarshaller);
                writer.Write(".BoxToUnmanaged(");
                writer.Write(pname);
                writer.Write(");\n");
                continue;
            }
            string interopTypeName = InteropTypeNameWriter.EncodeInteropTypeName(p.Type, TypedefNameType.ABI) + ", WinRT.Interop";
            IndentedTextWriter __scratchProjType = new();
            MethodFactory.WriteProjectedSignature(__scratchProjType, context, p.Type, false);
            string projectedTypeName = __scratchProjType.ToString();
            writer.Write("        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"ConvertToUnmanaged\")]\n");
            writer.Write("        static extern WindowsRuntimeObjectReferenceValue ConvertToUnmanaged_");
            writer.Write(raw);
            writer.Write("([UnsafeAccessorType(\"");
            writer.Write(interopTypeName);
            writer.Write("\")] object _, ");
            writer.Write(projectedTypeName);
            writer.Write(" value);\n");
            writer.Write("        using WindowsRuntimeObjectReferenceValue __");
            writer.Write(raw);
            writer.Write(" = ConvertToUnmanaged_");
            writer.Write(raw);
            writer.Write("(null, ");
            writer.Write(pname);
            writer.Write(");\n");
        }

        // For runtime class / object params, emit `using WindowsRuntimeObjectReferenceValue __<name> = ...ConvertToUnmanaged(<name>);`
        for (int i = 0; i < paramCount; i++)
        {
            ParamInfo p = sig.Params[i];
            if (p.Type.IsGenericInstance()) { continue; } // already handled above
            if (!IsRuntimeClassOrInterface(context.Cache, p.Type) && !p.Type.IsObject()) { continue; }
            string raw = p.Parameter.Name ?? "param";
            string pname = CSharpKeywords.IsKeyword(raw) ? "@" + raw : raw;
            writer.Write("        using WindowsRuntimeObjectReferenceValue __");
            writer.Write(raw);
            writer.Write(" = ");
            AbiMethodBodyFactory.EmitMarshallerConvertToUnmanaged(writer, context, p.Type, pname);
            writer.Write(";\n");
        }

        // For composable factories, marshal the additional `baseInterface` (which is a
        // WindowsRuntimeObject parameter on Invoke, not an args field). Truth pattern:
        //   using WindowsRuntimeObjectReferenceValue __baseInterface = WindowsRuntimeObjectMarshaller.ConvertToUnmanaged(baseInterface);
        if (isComposable)
        {
            writer.Write("        using WindowsRuntimeObjectReferenceValue __baseInterface = WindowsRuntimeObjectMarshaller.ConvertToUnmanaged(baseInterface);\n");
            writer.Write("        void* __innerInterface = default;\n");
        }

        // For mapped value-type params (DateTime, TimeSpan), emit ABI local + marshaller conversion.
        for (int i = 0; i < paramCount; i++)
        {
            ParamInfo p = sig.Params[i];
            if (!IsMappedAbiValueType(p.Type)) { continue; }
            string raw = p.Parameter.Name ?? "param";
            string pname = CSharpKeywords.IsKeyword(raw) ? "@" + raw : raw;
            string abiType = GetMappedAbiTypeName(p.Type);
            string marshaller = GetMappedMarshallerName(p.Type);
            writer.Write("        ");
            writer.Write(abiType);
            writer.Write(" __");
            writer.Write(raw);
            writer.Write(" = ");
            writer.Write(marshaller);
            writer.Write(".ConvertToUnmanaged(");
            writer.Write(pname);
            writer.Write(");\n");
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
            writer.Write("        global::ABI.System.Exception __");
            writer.Write(raw);
            writer.Write(" = global::ABI.System.ExceptionMarshaller.ConvertToUnmanaged(");
            writer.Write(pname);
            writer.Write(");\n");
        }

        // Declare InlineArray16 + ArrayPool fallback for non-blittable PassArray params
        // (runtime classes, objects, strings).
        bool hasNonBlittableArray = false;
        for (int i = 0; i < paramCount; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.PassArray && cat != ParamCategory.FillArray) { continue; }
            if (p.Type is not AsmResolver.DotNet.Signatures.SzArrayTypeSignature szArr) { continue; }
            if (IsBlittablePrimitive(context.Cache, szArr.BaseType) || IsAnyStruct(context.Cache, szArr.BaseType)) { continue; }
            hasNonBlittableArray = true;
            string raw = p.Parameter.Name ?? "param";
            string callName = CSharpKeywords.IsKeyword(raw) ? "@" + raw : raw;
            writer.Write("\n        Unsafe.SkipInit(out InlineArray16<nint> __");
            writer.Write(raw);
            writer.Write("_inlineArray);\n");
            writer.Write("        nint[] __");
            writer.Write(raw);
            writer.Write("_arrayFromPool = null;\n");
            writer.Write("        Span<nint> __");
            writer.Write(raw);
            writer.Write("_span = ");
            writer.Write(callName);
            writer.Write(".Length <= 16\n            ? __");
            writer.Write(raw);
            writer.Write("_inlineArray[..");
            writer.Write(callName);
            writer.Write(".Length]\n            : (__");
            writer.Write(raw);
            writer.Write("_arrayFromPool = global::System.Buffers.ArrayPool<nint>.Shared.Rent(");
            writer.Write(callName);
            writer.Write(".Length));\n");

            if (szArr.BaseType.IsString())
            {
                writer.Write("\n        Unsafe.SkipInit(out InlineArray16<HStringHeader> __");
                writer.Write(raw);
                writer.Write("_inlineHeaderArray);\n");
                writer.Write("        HStringHeader[] __");
                writer.Write(raw);
                writer.Write("_headerArrayFromPool = null;\n");
                writer.Write("        Span<HStringHeader> __");
                writer.Write(raw);
                writer.Write("_headerSpan = ");
                writer.Write(callName);
                writer.Write(".Length <= 16\n            ? __");
                writer.Write(raw);
                writer.Write("_inlineHeaderArray[..");
                writer.Write(callName);
                writer.Write(".Length]\n            : (__");
                writer.Write(raw);
                writer.Write("_headerArrayFromPool = global::System.Buffers.ArrayPool<HStringHeader>.Shared.Rent(");
                writer.Write(callName);
                writer.Write(".Length));\n");

                writer.Write("\n        Unsafe.SkipInit(out InlineArray16<nint> __");
                writer.Write(raw);
                writer.Write("_inlinePinnedHandleArray);\n");
                writer.Write("        nint[] __");
                writer.Write(raw);
                writer.Write("_pinnedHandleArrayFromPool = null;\n");
                writer.Write("        Span<nint> __");
                writer.Write(raw);
                writer.Write("_pinnedHandleSpan = ");
                writer.Write(callName);
                writer.Write(".Length <= 16\n            ? __");
                writer.Write(raw);
                writer.Write("_inlinePinnedHandleArray[..");
                writer.Write(callName);
                writer.Write(".Length]\n            : (__");
                writer.Write(raw);
                writer.Write("_pinnedHandleArrayFromPool = global::System.Buffers.ArrayPool<nint>.Shared.Rent(");
                writer.Write(callName);
                writer.Write(".Length));\n");
            }
        }

        writer.Write("        void* __retval = default;\n");
        if (hasNonBlittableArray) { writer.Write("        try\n        {\n"); }
        string baseIndent = hasNonBlittableArray ? "            " : "        ";

        // For System.Type params, pre-marshal to TypeReference (must be declared OUTSIDE the
        // fixed() block since the fixed block pins the resulting reference).
        for (int i = 0; i < paramCount; i++)
        {
            ParamInfo p = sig.Params[i];
            if (!p.Type.IsSystemType()) { continue; }
            string raw = p.Parameter.Name ?? "param";
            string pname = CSharpKeywords.IsKeyword(raw) ? "@" + raw : raw;
            writer.Write(baseIndent);
            writer.Write("global::ABI.System.TypeMarshaller.ConvertToUnmanagedUnsafe(");
            writer.Write(pname);
            writer.Write(", out TypeReference __");
            writer.Write(raw);
            writer.Write(");\n");
        }

        // Open ONE combined "fixed(void* _a = ..., _b = ..., ...)" block for ALL pinnable
        // params (string, Type, PassArray). Mirrors C++ write_abi_method_call_marshalers
        // which emits a single combined fixed-block for all is_pinnable marshalers.
        int fixedNesting = 0;
        int pinnableCount = 0;
        for (int i = 0; i < paramCount; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (p.Type.IsString() || p.Type.IsSystemType()) { pinnableCount++; }
            else if (cat == ParamCategory.PassArray || cat == ParamCategory.FillArray) { pinnableCount++; }
        }
        if (pinnableCount > 0)
        {
            string indent = baseIndent;
            writer.Write(indent);
            writer.Write("fixed(void* ");
            bool firstPin = true;
            for (int i = 0; i < paramCount; i++)
            {
                ParamInfo p = sig.Params[i];
                ParamCategory cat = ParamHelpers.GetParamCategory(p);
                bool isStr = p.Type.IsString();
                bool isType = p.Type.IsSystemType();
                bool isArr = cat == ParamCategory.PassArray || cat == ParamCategory.FillArray;
                if (!isStr && !isType && !isArr) { continue; }
                string raw = p.Parameter.Name ?? "param";
                string pname = CSharpKeywords.IsKeyword(raw) ? "@" + raw : raw;
                if (!firstPin) { writer.Write(", "); }
                firstPin = false;
                writer.Write("_");
                writer.Write(raw);
                writer.Write(" = ");
                if (isType) { writer.Write("__"); writer.Write(raw); }
                else if (isArr)
                {
                    AsmResolver.DotNet.Signatures.TypeSignature elemT = ((AsmResolver.DotNet.Signatures.SzArrayTypeSignature)p.Type).BaseType;
                    bool isBlittableElem = IsBlittablePrimitive(context.Cache, elemT) || IsAnyStruct(context.Cache, elemT);
                    bool isStringElem = elemT.IsString();
                    if (isBlittableElem) { writer.Write(pname); }
                    else { writer.Write("__"); writer.Write(raw); writer.Write("_span"); }
                    if (isStringElem)
                    {
                        writer.Write(", _");
                        writer.Write(raw);
                        writer.Write("_inlineHeaderArray = __");
                        writer.Write(raw);
                        writer.Write("_headerSpan");
                    }
                }
                else
                {
                    // string param: pin the input string itself.
                    writer.Write(pname);
                }
            }
            writer.Write(")\n");
            writer.Write(indent);
            writer.Write("{\n");
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
                writer.Write(innerIndent);
                writer.Write("HStringMarshaller.ConvertToUnmanagedUnsafe((char*)_");
                writer.Write(raw);
                writer.Write(", ");
                writer.Write(pname);
                writer.Write("?.Length, out HStringReference __");
                writer.Write(raw);
                writer.Write(");\n");
            }
        }

        string callIndent = baseIndent + new string(' ', fixedNesting * 4);

        // Emit CopyToUnmanaged for non-blittable PassArray params.
        for (int i = 0; i < paramCount; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.PassArray && cat != ParamCategory.FillArray) { continue; }
            if (p.Type is not AsmResolver.DotNet.Signatures.SzArrayTypeSignature szArr) { continue; }
            if (IsBlittablePrimitive(context.Cache, szArr.BaseType) || IsAnyStruct(context.Cache, szArr.BaseType)) { continue; }
            string raw = p.Parameter.Name ?? "param";
            string pname = CSharpKeywords.IsKeyword(raw) ? "@" + raw : raw;
            if (szArr.BaseType.IsString())
            {
                writer.Write(callIndent);
                writer.Write("HStringArrayMarshaller.ConvertToUnmanagedUnsafe(\n");
                writer.Write(callIndent);
                writer.Write("    source: ");
                writer.Write(pname);
                writer.Write(",\n");
                writer.Write(callIndent);
                writer.Write("    hstringHeaders: (HStringHeader*) _");
                writer.Write(raw);
                writer.Write("_inlineHeaderArray,\n");
                writer.Write(callIndent);
                writer.Write("    hstrings: __");
                writer.Write(raw);
                writer.Write("_span,\n");
                writer.Write(callIndent);
                writer.Write("    pinnedGCHandles: __");
                writer.Write(raw);
                writer.Write("_pinnedHandleSpan);\n");
            }
            else
            {
                IndentedTextWriter __scratchElement = new();
                TypedefNameWriter.WriteProjectionType(__scratchElement, context, TypeSemanticsFactory.Get(szArr.BaseType));
                string elementProjected = __scratchElement.ToString();
                string elementInteropArg = InteropTypeNameWriter.EncodeInteropTypeName(szArr.BaseType, TypedefNameType.Projected);
                _ = elementInteropArg;
                writer.Write(callIndent);
                writer.Write("[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"CopyToUnmanaged\")]\n");
                writer.Write(callIndent);
                writer.Write("static extern void CopyToUnmanaged_");
                writer.Write(raw);
                writer.Write("([UnsafeAccessorType(\"");
                writer.Write(ArrayElementEncoder.GetArrayMarshallerInteropPath(szArr.BaseType));
                writer.Write("\")] object _, ReadOnlySpan<");
                writer.Write(elementProjected);
                writer.Write("> span, uint length, void** data);\n");
                writer.Write(callIndent);
                writer.Write("CopyToUnmanaged_");
                writer.Write(raw);
                writer.Write("(null, ");
                writer.Write(pname);
                writer.Write(", (uint)");
                writer.Write(pname);
                writer.Write(".Length, (void**)_");
                writer.Write(raw);
                writer.Write(");\n");
            }
        }

        writer.Write(callIndent);
        // delegate* signature: void*, then each ABI param type, then [void*, void**] (composable),
        // then void**, then int.
        writer.Write("RestrictedErrorInfo.ThrowExceptionForHR((*(delegate* unmanaged[MemberFunction]<void*, ");
        for (int i = 0; i < paramCount; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat == ParamCategory.PassArray || cat == ParamCategory.FillArray)
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
        writer.Write("void**, int>**)ThisPtr)[");
        writer.Write((6 + factoryMethodIndex).ToString(System.Globalization.CultureInfo.InvariantCulture));
        writer.Write("](ThisPtr");
        for (int i = 0; i < paramCount; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            string raw = p.Parameter.Name ?? "param";
            string pname = CSharpKeywords.IsKeyword(raw) ? "@" + raw : raw;
            writer.Write(",\n  ");
            if (cat == ParamCategory.PassArray || cat == ParamCategory.FillArray)
            {
                writer.Write("(uint)");
                writer.Write(pname);
                writer.Write(".Length, _");
                writer.Write(raw);
                continue;
            }
            // For enums, cast to underlying type. For bool, cast to byte. For char, cast to ushort.
            // For string params, use the marshalled HString from the fixed block.
            // For runtime class / object / generic instance params, use __<name>.GetThisPtrUnsafe().
            if (IsEnumType(context.Cache, p.Type))
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
                writer.Write("__");
                writer.Write(raw);
                writer.Write(".HString");
            }
            else if (p.Type.IsSystemType())
            {
                writer.Write("__");
                writer.Write(raw);
                writer.Write(".ConvertToUnmanagedUnsafe()");
            }
            else if (IsRuntimeClassOrInterface(context.Cache, p.Type) || p.Type.IsObject() || p.Type.IsGenericInstance())
            {
                writer.Write("__");
                writer.Write(raw);
                writer.Write(".GetThisPtrUnsafe()");
            }
            else if (IsMappedAbiValueType(p.Type))
            {
                writer.Write("__");
                writer.Write(raw);
            }
            else if (p.Type.IsHResultException())
            {
                writer.Write("__");
                writer.Write(raw);
            }
            else
            {
                writer.Write(pname);
            }
        }
        if (isComposable)
        {
            // Pass __baseInterface.GetThisPtrUnsafe() and &__innerInterface.
            writer.Write(",\n  __baseInterface.GetThisPtrUnsafe(),\n  &__innerInterface");
        }
        writer.Write(",\n  &__retval));\n");
        if (isComposable)
        {
            writer.Write(callIndent);
            writer.Write("innerInterface = __innerInterface;\n");
        }
        writer.Write(callIndent);
        writer.Write("retval = __retval;\n");

        // Close fixed blocks (innermost first).
        for (int i = fixedNesting - 1; i >= 0; i--)
        {
            string indent = baseIndent + new string(' ', i * 4);
            writer.Write(indent);
            writer.Write("}\n");
        }

        // Close try and emit finally with cleanup for non-blittable PassArray params.
        if (hasNonBlittableArray)
        {
            writer.Write("        }\n        finally\n        {\n");
            for (int i = 0; i < paramCount; i++)
            {
                ParamInfo p = sig.Params[i];
                ParamCategory cat = ParamHelpers.GetParamCategory(p);
                if (cat != ParamCategory.PassArray && cat != ParamCategory.FillArray) { continue; }
                if (p.Type is not AsmResolver.DotNet.Signatures.SzArrayTypeSignature szArr) { continue; }
                if (IsBlittablePrimitive(context.Cache, szArr.BaseType) || IsAnyStruct(context.Cache, szArr.BaseType)) { continue; }
                string raw = p.Parameter.Name ?? "param";
                if (szArr.BaseType.IsString())
                {
                    writer.Write("\n            HStringArrayMarshaller.Dispose(__");
                    writer.Write(raw);
                    writer.Write("_pinnedHandleSpan);\n\n");
                    writer.Write("            if (__");
                    writer.Write(raw);
                    writer.Write("_pinnedHandleArrayFromPool is not null)\n            {\n");
                    writer.Write("                global::System.Buffers.ArrayPool<nint>.Shared.Return(__");
                    writer.Write(raw);
                    writer.Write("_pinnedHandleArrayFromPool);\n            }\n\n");
                    writer.Write("            if (__");
                    writer.Write(raw);
                    writer.Write("_headerArrayFromPool is not null)\n            {\n");
                    writer.Write("                global::System.Buffers.ArrayPool<HStringHeader>.Shared.Return(__");
                    writer.Write(raw);
                    writer.Write("_headerArrayFromPool);\n            }\n");
                }
                else
                {
                    string elementInteropArg = InteropTypeNameWriter.EncodeInteropTypeName(szArr.BaseType, TypedefNameType.Projected);
                    _ = elementInteropArg;
                    writer.Write("\n            [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"Dispose\")]\n");
                    writer.Write("            static extern void Dispose_");
                    writer.Write(raw);
                    writer.Write("([UnsafeAccessorType(\"");
                    writer.Write(ArrayElementEncoder.GetArrayMarshallerInteropPath(szArr.BaseType));
                    writer.Write("\")] object _, uint length, void** data);\n\n");
                    writer.Write("            fixed(void* _");
                    writer.Write(raw);
                    writer.Write(" = __");
                    writer.Write(raw);
                    writer.Write("_span)\n            {\n");
                    writer.Write("                Dispose_");
                    writer.Write(raw);
                    writer.Write("(null, (uint) __");
                    writer.Write(raw);
                    writer.Write("_span.Length, (void**)_");
                    writer.Write(raw);
                    writer.Write(");\n            }\n");
                }
                writer.Write("\n            if (__");
                writer.Write(raw);
                writer.Write("_arrayFromPool is not null)\n            {\n");
                writer.Write("                global::System.Buffers.ArrayPool<nint>.Shared.Return(__");
                writer.Write(raw);
                writer.Write("_arrayFromPool);\n            }\n");
            }
            writer.Write("        }\n");
        }

        writer.Write("    }\n}\n");
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
            WriteStaticFactoryObjRef(writer, context, composableType, runtimeClassFullName, factoryObjRefName);
        }

        string defaultIfaceIid = GetDefaultInterfaceIid(context, classType);
        string marshalingType = GetMarshalingTypeName(classType);
        string defaultIfaceObjRef;
        ITypeDefOrRef? defaultIface = classType.GetDefaultInterface();
        defaultIfaceObjRef = defaultIface is not null ? ObjRefNameGenerator.GetObjRefName(context, defaultIface) : string.Empty;
        int gcPressure = GetGcPressureAmount(classType);
        // Compute the platform attribute string from the composable factory interface's
        // [ContractVersion] attribute. Mirrors C++
        // 'auto platform_attribute = write_platform_attribute_temp(w, composable_type);'
        // emitted at line 3179 before the public ctor.
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

            writer.Write("\n");
            if (!string.IsNullOrEmpty(platformAttribute)) { writer.Write(platformAttribute); }
            writer.Write(visibility);
            if (!isParameterless) { writer.Write(" unsafe "); } else { writer.Write(" "); }
            writer.Write(typeName);
            writer.Write("(");
            for (int i = 0; i < userParamCount; i++)
            {
                if (i > 0) { writer.Write(", "); }
                MethodFactory.WriteProjectionParameter(writer, context, sig.Params[i]);
            }
            writer.Write(")\n  :base(");
            if (isParameterless)
            {
                // base(default(WindowsRuntimeActivationTypes.DerivedComposed), <factoryObjRef>, <iid>, <marshalingType>)
                string factoryObjRef = ObjRefNameGenerator.GetObjRefName(context, composableType);
                writer.Write("default(WindowsRuntimeActivationTypes.DerivedComposed), ");
                writer.Write(factoryObjRef);
                writer.Write(", ");
                writer.Write(defaultIfaceIid);
                writer.Write(", ");
                writer.Write(marshalingType);
            }
            else
            {
                writer.Write(callbackName);
                writer.Write(".Instance, ");
                writer.Write(defaultIfaceIid);
                writer.Write(", ");
                writer.Write(marshalingType);
                writer.Write(", WindowsRuntimeActivationArgsReference.CreateUnsafe(new ");
                writer.Write(argsName);
                writer.Write("(");
                for (int i = 0; i < userParamCount; i++)
                {
                    if (i > 0) { writer.Write(", "); }
                    string raw = sig.Params[i].Parameter.Name ?? "param";
                    writer.Write(CSharpKeywords.IsKeyword(raw) ? "@" + raw : raw);
                }
                writer.Write("))");
            }
            writer.Write(")\n{\n");
            writer.Write("if (GetType() == typeof(");
            writer.Write(typeName);
            writer.Write("))\n{\n");
            if (!string.IsNullOrEmpty(defaultIfaceObjRef))
            {
                writer.Write(defaultIfaceObjRef);
                writer.Write(" = NativeObjectReference;\n");
            }
            writer.Write("}\n");
            if (gcPressure > 0)
            {
                writer.Write("GC.AddMemoryPressure(");
                writer.Write(gcPressure.ToString(System.Globalization.CultureInfo.InvariantCulture));
                writer.Write(");\n");
            }
            writer.Write("}\n");

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
            ? "GC.AddMemoryPressure(" + gcPressure.ToString(System.Globalization.CultureInfo.InvariantCulture) + ");\n"
            : string.Empty;

        // 1. WindowsRuntimeActivationTypes.DerivedComposed
        writer.Write("\nprotected ");
        writer.Write(typeName);
        writer.Write("(WindowsRuntimeActivationTypes.DerivedComposed _, WindowsRuntimeObjectReference activationFactoryObjectReference, in Guid iid, CreateObjectReferenceMarshalingType marshalingType)\n");
        writer.Write("  :base(_, activationFactoryObjectReference, in iid, marshalingType)\n");
        writer.Write("{\n");
        if (!string.IsNullOrEmpty(gcPressureBody)) { writer.Write(gcPressureBody); }
        writer.Write("}\n");

        // 2. WindowsRuntimeActivationTypes.DerivedSealed
        writer.Write("\nprotected ");
        writer.Write(typeName);
        writer.Write("(WindowsRuntimeActivationTypes.DerivedSealed _, WindowsRuntimeObjectReference activationFactoryObjectReference, in Guid iid, CreateObjectReferenceMarshalingType marshalingType)\n");
        writer.Write("  :base(_, activationFactoryObjectReference, in iid, marshalingType)\n");
        writer.Write("{\n");
        if (!string.IsNullOrEmpty(gcPressureBody)) { writer.Write(gcPressureBody); }
        writer.Write("}\n");

        // 3. WindowsRuntimeActivationFactoryCallback.DerivedComposed
        writer.Write("\nprotected ");
        writer.Write(typeName);
        writer.Write("(WindowsRuntimeActivationFactoryCallback.DerivedComposed activationFactoryCallback, in Guid iid, CreateObjectReferenceMarshalingType marshalingType, WindowsRuntimeActivationArgsReference additionalParameters)\n");
        writer.Write("  :base(activationFactoryCallback, in iid, marshalingType, additionalParameters)\n");
        writer.Write("{\n");
        if (!string.IsNullOrEmpty(gcPressureBody)) { writer.Write(gcPressureBody); }
        writer.Write("}\n");

        // 4. WindowsRuntimeActivationFactoryCallback.DerivedSealed
        writer.Write("\nprotected ");
        writer.Write(typeName);
        writer.Write("(WindowsRuntimeActivationFactoryCallback.DerivedSealed activationFactoryCallback, in Guid iid, CreateObjectReferenceMarshalingType marshalingType, WindowsRuntimeActivationArgsReference additionalParameters)\n");
        writer.Write("  :base(activationFactoryCallback, in iid, marshalingType, additionalParameters)\n");
        writer.Write("{\n");
        if (!string.IsNullOrEmpty(gcPressureBody)) { writer.Write(gcPressureBody); }
        writer.Write("}\n");
    }
}
