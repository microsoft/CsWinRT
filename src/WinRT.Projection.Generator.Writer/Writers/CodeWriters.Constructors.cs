// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using AsmResolver.DotNet;

namespace WindowsRuntime.ProjectionGenerator.Writer;

/// <summary>
/// Activator/composer constructor emission. Mirrors C++ <c>write_factory_constructors</c>
/// and <c>write_composable_constructors</c>.
/// </summary>
internal static partial class CodeWriters
{
    /// <summary>
    /// Mirrors C++ <c>write_attributed_types</c>: emits constructors and static members
    /// for the given runtime class.
    /// </summary>
    public static void WriteAttributedTypes(TypeWriter w, TypeDefinition classType)
    {
        if (_cacheRef is null) { return; }

        // Track whether we need to emit the static _objRef_<RuntimeClassName> field (used by
        // default constructors). Emit it once per class if any [Activatable] factory exists.
        bool needsClassObjRef = false;

        foreach (KeyValuePair<string, AttributedType> kv in AttributedTypes.Get(classType, _cacheRef))
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
            string objRefName = "_objRef_" + EscapeTypeNameForIdentifier("global::" + fullName, stripGlobal: true);
            w.Write("\nprivate static WindowsRuntimeObjectReference ");
            w.Write(objRefName);
            w.Write("\n{\n    get\n    {\n        var __");
            w.Write(objRefName);
            w.Write(" = field;\n        if (__");
            w.Write(objRefName);
            w.Write(" != null && __");
            w.Write(objRefName);
            w.Write(".IsInCurrentContext)\n        {\n            return __");
            w.Write(objRefName);
            w.Write(";\n        }\n        return field = WindowsRuntimeObjectReference.GetActivationFactory(\"");
            w.Write(fullName);
            w.Write("\");\n    }\n}\n");
        }

        foreach (KeyValuePair<string, AttributedType> kv in AttributedTypes.Get(classType, _cacheRef))
        {
            AttributedType factory = kv.Value;
            if (factory.Activatable)
            {
                WriteFactoryConstructors(w, factory.Type, classType);
            }
            else if (factory.Composable)
            {
                WriteComposableConstructors(w, factory.Type, classType, factory.Visible ? "public" : "protected");
            }
        }
    }

    /// <summary>
    /// Mirrors C++ <c>write_factory_constructors</c>.
    /// </summary>
    public static void WriteFactoryConstructors(TypeWriter w, TypeDefinition? factoryType, TypeDefinition classType)
    {
        string typeName = classType.Name?.Value ?? string.Empty;
        if (factoryType is not null)
        {
            // Emit the factory objref property (lazy-initialized).
            string factoryRuntimeClassFullName = (classType.Namespace?.Value ?? string.Empty) + "." + typeName;
            string factoryObjRefName = GetObjRefName(w, factoryType);
            WriteStaticFactoryObjRef(w, factoryType, factoryRuntimeClassFullName, factoryObjRefName);

            string defaultIfaceIid = GetDefaultInterfaceIid(w, classType);
            string marshalingType = GetMarshalingTypeName(classType);
            int methodIndex = 0;
            foreach (MethodDefinition method in factoryType.Methods)
            {
                if (Helpers.IsSpecial(method)) { methodIndex++; continue; }
                MethodSig sig = new(method);
                string callbackName = (method.Name?.Value ?? "Create") + "_" + sig.Params.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
                string argsName = callbackName + "Args";

                // Emit the public constructor.
                w.Write("\npublic unsafe ");
                w.Write(typeName);
                w.Write("(");
                WriteParameterList(w, sig);
                w.Write(")\n  : base(");
                if (sig.Params.Count == 0)
                {
                    w.Write("default");
                }
                else
                {
                    w.Write(callbackName);
                    w.Write(".Instance, ");
                    w.Write(defaultIfaceIid);
                    w.Write(", ");
                    w.Write(marshalingType);
                    w.Write(", WindowsRuntimeActivationArgsReference.CreateUnsafe(new ");
                    w.Write(argsName);
                    w.Write("(");
                    for (int i = 0; i < sig.Params.Count; i++)
                    {
                        if (i > 0) { w.Write(", "); }
                        string raw = sig.Params[i].Parameter.Name ?? "param";
                        w.Write(Helpers.IsKeyword(raw) ? "@" + raw : raw);
                    }
                    w.Write("))");
                }
                w.Write(")\n{\n}\n");

                if (sig.Params.Count > 0)
                {
                    EmitFactoryArgsStruct(w, sig, argsName);
                    EmitFactoryCallbackClass(w, sig, callbackName, argsName, factoryObjRefName, methodIndex);
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
            string objRefName = "_objRef_" + EscapeTypeNameForIdentifier("global::" + fullName, stripGlobal: true);

            // Find the default interface IID to use.
            string defaultIfaceIid = GetDefaultInterfaceIid(w, classType);

            w.Write("\npublic ");
            w.Write(typeName);
            w.Write("()\n  : base(default(WindowsRuntimeActivationTypes.DerivedSealed), ");
            w.Write(objRefName);
            w.Write(", ");
            w.Write(defaultIfaceIid);
            w.Write(", CreateObjectReferenceMarshalingType.Agile)\n{\n}\n");
        }
    }

    /// <summary>
    /// Reads the <c>[MarshalingBehaviorAttribute]</c> on the class and returns the corresponding
    /// <c>CreateObjectReferenceMarshalingType.*</c> expression. Mirrors C++
    /// <c>get_marshaling_type_name</c>.
    /// </summary>
    private static string GetMarshalingTypeName(TypeDefinition classType)
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
    private static void EmitFactoryArgsStruct(TypeWriter w, MethodSig sig, string argsName)
    {
        w.Write("\nprivate readonly ref struct ");
        w.Write(argsName);
        w.Write("(");
        WriteParameterList(w, sig);
        w.Write(")\n{\n");
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            string raw = p.Parameter.Name ?? "param";
            string pname = Helpers.IsKeyword(raw) ? "@" + raw : raw;
            w.Write("    public readonly ");
            // Use the parameter's projected type (matches the constructor parameter type, including
            // ReadOnlySpan<T>/Span<T> for array params).
            WriteProjectionParameterType(w, p);
            w.Write(" ");
            w.Write(pname);
            w.Write(" = ");
            w.Write(pname);
            w.Write(";\n");
        }
        w.Write("}\n");
    }

    /// <summary>Emits the <c>private sealed class &lt;Name&gt; : WindowsRuntimeActivationFactoryCallback.DerivedSealed</c>.</summary>
    private static void EmitFactoryCallbackClass(TypeWriter w, MethodSig sig, string callbackName, string argsName, string factoryObjRefName, int factoryMethodIndex)
    {
        w.Write("\nprivate sealed class ");
        w.Write(callbackName);
        w.Write(" : WindowsRuntimeActivationFactoryCallback.DerivedSealed\n{\n");
        w.Write("    public static readonly ");
        w.Write(callbackName);
        w.Write(" Instance = new();\n\n");
        w.Write("    [MethodImpl(MethodImplOptions.NoInlining)]\n");
        w.Write("    public override unsafe void Invoke(WindowsRuntimeActivationArgsReference additionalParameters, out void* retval)\n    {\n");
        w.Write("        using WindowsRuntimeObjectReferenceValue activationFactoryValue = ");
        w.Write(factoryObjRefName);
        w.Write(".AsValue();\n");
        w.Write("        void* ThisPtr = activationFactoryValue.GetThisPtrUnsafe();\n");
        w.Write("        ref readonly ");
        w.Write(argsName);
        w.Write(" args = ref additionalParameters.GetValueRefUnsafe<");
        w.Write(argsName);
        w.Write(">();\n");

        // Bind each arg from the args struct to a local of its ABI-marshalable input type.
        // For simple cases (primitives, blittable structs, enums) this is a direct copy. For
        // string params we marshal via HStringMarshaller. For runtime classes we marshal via
        // the appropriate marshaller. For unsupported parameter kinds we emit throw null!.
        bool canEmit = true;
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            AsmResolver.DotNet.Signatures.TypeSignature pt = p.Type;
            if (cat == ParamCategory.PassArray || cat == ParamCategory.FillArray)
            {
                if (pt is AsmResolver.DotNet.Signatures.SzArrayTypeSignature szP)
                {
                    if (IsBlittablePrimitive(szP.BaseType) || IsAnyStruct(szP.BaseType)) { continue; }
                    if (IsString(szP.BaseType) || IsRuntimeClassOrInterface(szP.BaseType) || IsObject(szP.BaseType)) { continue; }
                }
                canEmit = false; break;
            }
            if (cat != ParamCategory.In) { canEmit = false; break; }
            if (IsHResultException(pt)) { canEmit = false; break; }
            if (IsBlittablePrimitive(pt) || IsBlittableStruct(pt) || IsEnumType(pt) || IsString(pt))
            {
                continue;
            }
            if (IsRuntimeClassOrInterface(pt) || IsObject(pt) || IsGenericInstance(pt))
            {
                continue;
            }
            canEmit = false;
            break;
        }

        if (!canEmit)
        {
            w.Write("        throw null!;\n    }\n}\n");
            return;
        }

        // Bind arg locals.
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            string raw = p.Parameter.Name ?? "param";
            string pname = Helpers.IsKeyword(raw) ? "@" + raw : raw;
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            w.Write("        ");
            // For array params, the bind type is ReadOnlySpan<T> / Span<T> (not the SzArray).
            if (cat == ParamCategory.PassArray)
            {
                w.Write("ReadOnlySpan<");
                WriteProjectionType(w, TypeSemanticsFactory.Get(((AsmResolver.DotNet.Signatures.SzArrayTypeSignature)p.Type).BaseType));
                w.Write(">");
            }
            else if (cat == ParamCategory.FillArray)
            {
                w.Write("Span<");
                WriteProjectionType(w, TypeSemanticsFactory.Get(((AsmResolver.DotNet.Signatures.SzArrayTypeSignature)p.Type).BaseType));
                w.Write(">");
            }
            else
            {
                WriteProjectedSignature(w, p.Type, true);
            }
            w.Write(" ");
            w.Write(pname);
            w.Write(" = args.");
            w.Write(pname);
            w.Write(";\n");
        }

        // For generic instance params, emit local UnsafeAccessor delegates.
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            if (!IsGenericInstance(p.Type)) { continue; }
            string raw = p.Parameter.Name ?? "param";
            string pname = Helpers.IsKeyword(raw) ? "@" + raw : raw;
            string interopTypeName = EncodeInteropTypeName(p.Type, TypedefNameType.ABI) + ", WinRT.Interop";
            string projectedTypeName = w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteProjectedSignature(w, p.Type, false)));
            w.Write("        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"ConvertToUnmanaged\")]\n");
            w.Write("        static extern WindowsRuntimeObjectReferenceValue ConvertToUnmanaged_");
            w.Write(raw);
            w.Write("([UnsafeAccessorType(\"");
            w.Write(interopTypeName);
            w.Write("\")] object _, ");
            w.Write(projectedTypeName);
            w.Write(" value);\n");
            w.Write("        using WindowsRuntimeObjectReferenceValue __");
            w.Write(raw);
            w.Write(" = ConvertToUnmanaged_");
            w.Write(raw);
            w.Write("(null, ");
            w.Write(pname);
            w.Write(");\n");
        }

        // For runtime class / object params, emit `using WindowsRuntimeObjectReferenceValue __<name> = ...ConvertToUnmanaged(<name>);`
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            if (IsGenericInstance(p.Type)) { continue; } // already handled above
            if (!IsRuntimeClassOrInterface(p.Type) && !IsObject(p.Type)) { continue; }
            string raw = p.Parameter.Name ?? "param";
            string pname = Helpers.IsKeyword(raw) ? "@" + raw : raw;
            w.Write("        using WindowsRuntimeObjectReferenceValue __");
            w.Write(raw);
            w.Write(" = ");
            EmitMarshallerConvertToUnmanaged(w, p.Type, pname);
            w.Write(";\n");
        }

        // Declare InlineArray16 + ArrayPool fallback for non-blittable PassArray params
        // (runtime classes, objects, strings).
        bool hasNonBlittableArray = false;
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.PassArray && cat != ParamCategory.FillArray) { continue; }
            if (p.Type is not AsmResolver.DotNet.Signatures.SzArrayTypeSignature szArr) { continue; }
            if (IsBlittablePrimitive(szArr.BaseType) || IsAnyStruct(szArr.BaseType)) { continue; }
            hasNonBlittableArray = true;
            string raw = p.Parameter.Name ?? "param";
            string callName = Helpers.IsKeyword(raw) ? "@" + raw : raw;
            w.Write("\n        Unsafe.SkipInit(out InlineArray16<nint> __");
            w.Write(raw);
            w.Write("_inlineArray);\n");
            w.Write("        nint[] __");
            w.Write(raw);
            w.Write("_arrayFromPool = null;\n");
            w.Write("        Span<nint> __");
            w.Write(raw);
            w.Write("_span = ");
            w.Write(callName);
            w.Write(".Length <= 16\n            ? __");
            w.Write(raw);
            w.Write("_inlineArray[..");
            w.Write(callName);
            w.Write(".Length]\n            : (__");
            w.Write(raw);
            w.Write("_arrayFromPool = global::System.Buffers.ArrayPool<nint>.Shared.Rent(");
            w.Write(callName);
            w.Write(".Length));\n");

            if (IsString(szArr.BaseType))
            {
                w.Write("\n        Unsafe.SkipInit(out InlineArray16<HStringHeader> __");
                w.Write(raw);
                w.Write("_inlineHeaderArray);\n");
                w.Write("        HStringHeader[] __");
                w.Write(raw);
                w.Write("_headerArrayFromPool = null;\n");
                w.Write("        Span<HStringHeader> __");
                w.Write(raw);
                w.Write("_headerSpan = ");
                w.Write(callName);
                w.Write(".Length <= 16\n            ? __");
                w.Write(raw);
                w.Write("_inlineHeaderArray[..");
                w.Write(callName);
                w.Write(".Length]\n            : (__");
                w.Write(raw);
                w.Write("_headerArrayFromPool = global::System.Buffers.ArrayPool<HStringHeader>.Shared.Rent(");
                w.Write(callName);
                w.Write(".Length));\n");

                w.Write("\n        Unsafe.SkipInit(out InlineArray16<nint> __");
                w.Write(raw);
                w.Write("_inlinePinnedHandleArray);\n");
                w.Write("        nint[] __");
                w.Write(raw);
                w.Write("_pinnedHandleArrayFromPool = null;\n");
                w.Write("        Span<nint> __");
                w.Write(raw);
                w.Write("_pinnedHandleSpan = ");
                w.Write(callName);
                w.Write(".Length <= 16\n            ? __");
                w.Write(raw);
                w.Write("_inlinePinnedHandleArray[..");
                w.Write(callName);
                w.Write(".Length]\n            : (__");
                w.Write(raw);
                w.Write("_pinnedHandleArrayFromPool = global::System.Buffers.ArrayPool<nint>.Shared.Rent(");
                w.Write(callName);
                w.Write(".Length));\n");
            }
        }

        w.Write("        void* __retval = default;\n");
        if (hasNonBlittableArray) { w.Write("        try\n        {\n"); }
        string baseIndent = hasNonBlittableArray ? "            " : "        ";

        // For string and array params, open a `fixed(void* _<name> = <name>)` block. Each adds nesting.
        int fixedNesting = 0;
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            string raw = p.Parameter.Name ?? "param";
            string pname = Helpers.IsKeyword(raw) ? "@" + raw : raw;
            string indent = baseIndent + new string(' ', fixedNesting * 4);
            if (IsString(p.Type))
            {
                w.Write(indent);
                w.Write("fixed(void* _");
                w.Write(raw);
                w.Write(" = ");
                w.Write(pname);
                w.Write(")\n");
                w.Write(indent);
                w.Write("{\n");
                fixedNesting++;
                string innerIndent = baseIndent + new string(' ', fixedNesting * 4);
                w.Write(innerIndent);
                w.Write("HStringMarshaller.ConvertToUnmanagedUnsafe((char*)_");
                w.Write(raw);
                w.Write(", ");
                w.Write(pname);
                w.Write("?.Length, out HStringReference __");
                w.Write(raw);
                w.Write(");\n");
            }
            else if (cat == ParamCategory.PassArray || cat == ParamCategory.FillArray)
            {
                AsmResolver.DotNet.Signatures.TypeSignature elemT = ((AsmResolver.DotNet.Signatures.SzArrayTypeSignature)p.Type).BaseType;
                bool isBlittableElem = IsBlittablePrimitive(elemT) || IsAnyStruct(elemT);
                bool isStringElem = IsString(elemT);
                w.Write(indent);
                w.Write("fixed(void* _");
                w.Write(raw);
                w.Write(" = ");
                if (isBlittableElem) { w.Write(pname); }
                else { w.Write("__"); w.Write(raw); w.Write("_span"); }
                if (isStringElem)
                {
                    w.Write(", _");
                    w.Write(raw);
                    w.Write("_inlineHeaderArray = __");
                    w.Write(raw);
                    w.Write("_headerSpan");
                }
                w.Write(")\n");
                w.Write(indent);
                w.Write("{\n");
                fixedNesting++;
            }
        }

        string callIndent = baseIndent + new string(' ', fixedNesting * 4);

        // Emit CopyToUnmanaged for non-blittable PassArray params.
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.PassArray && cat != ParamCategory.FillArray) { continue; }
            if (p.Type is not AsmResolver.DotNet.Signatures.SzArrayTypeSignature szArr) { continue; }
            if (IsBlittablePrimitive(szArr.BaseType) || IsAnyStruct(szArr.BaseType)) { continue; }
            string raw = p.Parameter.Name ?? "param";
            string pname = Helpers.IsKeyword(raw) ? "@" + raw : raw;
            if (IsString(szArr.BaseType))
            {
                w.Write(callIndent);
                w.Write("HStringArrayMarshaller.ConvertToUnmanagedUnsafe(\n");
                w.Write(callIndent);
                w.Write("    source: ");
                w.Write(pname);
                w.Write(",\n");
                w.Write(callIndent);
                w.Write("    hstringHeaders: (HStringHeader*) _");
                w.Write(raw);
                w.Write("_inlineHeaderArray,\n");
                w.Write(callIndent);
                w.Write("    hstrings: __");
                w.Write(raw);
                w.Write("_span,\n");
                w.Write(callIndent);
                w.Write("    pinnedGCHandles: __");
                w.Write(raw);
                w.Write("_pinnedHandleSpan);\n");
            }
            else
            {
                string elementProjected = w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteProjectionType(w, TypeSemanticsFactory.Get(szArr.BaseType))));
                string elementInteropArg = EncodeInteropTypeName(szArr.BaseType, TypedefNameType.Projected);
                w.Write(callIndent);
                w.Write("[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"CopyToUnmanaged\")]\n");
                w.Write(callIndent);
                w.Write("static extern void CopyToUnmanaged_");
                w.Write(raw);
                w.Write("([UnsafeAccessorType(\"");
                w.Write(GetArrayMarshallerInteropPath(w, szArr.BaseType, elementInteropArg));
                w.Write("\")] object _, ReadOnlySpan<");
                w.Write(elementProjected);
                w.Write("> span, uint length, void** data);\n");
                w.Write(callIndent);
                w.Write("CopyToUnmanaged_");
                w.Write(raw);
                w.Write("(null, ");
                w.Write(pname);
                w.Write(", (uint)");
                w.Write(pname);
                w.Write(".Length, (void**)_");
                w.Write(raw);
                w.Write(");\n");
            }
        }

        w.Write(callIndent);
        // delegate* signature: void*, then each ABI param type, then void**, then int.
        w.Write("RestrictedErrorInfo.ThrowExceptionForHR((*(delegate* unmanaged[MemberFunction]<void*, ");
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat == ParamCategory.PassArray || cat == ParamCategory.FillArray)
            {
                w.Write("uint, void*, ");
                continue;
            }
            WriteAbiType(w, TypeSemanticsFactory.Get(p.Type));
            w.Write(", ");
        }
        w.Write("void**, int>**)ThisPtr)[");
        w.Write((6 + factoryMethodIndex).ToString(System.Globalization.CultureInfo.InvariantCulture));
        w.Write("](ThisPtr");
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            string raw = p.Parameter.Name ?? "param";
            string pname = Helpers.IsKeyword(raw) ? "@" + raw : raw;
            w.Write(", ");
            if (cat == ParamCategory.PassArray || cat == ParamCategory.FillArray)
            {
                w.Write("(uint)");
                w.Write(pname);
                w.Write(".Length, _");
                w.Write(raw);
                continue;
            }
            // For enums, cast to underlying type. For bool, cast to byte. For char, cast to ushort.
            // For string params, use the marshalled HString from the fixed block.
            // For runtime class / object / generic instance params, use __<name>.GetThisPtrUnsafe().
            if (IsEnumType(p.Type))
            {
                // No cast needed: function pointer signature uses the projected enum type.
                w.Write(pname);
            }
            else if (p.Type is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlibBool &&
                     corlibBool.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Boolean)
            {
                w.Write("(byte)(");
                w.Write(pname);
                w.Write(" ? 1 : 0)");
            }
            else if (p.Type is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlibChar &&
                     corlibChar.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Char)
            {
                w.Write("(ushort)");
                w.Write(pname);
            }
            else if (IsString(p.Type))
            {
                w.Write("__");
                w.Write(raw);
                w.Write(".HString");
            }
            else if (IsRuntimeClassOrInterface(p.Type) || IsObject(p.Type) || IsGenericInstance(p.Type))
            {
                w.Write("__");
                w.Write(raw);
                w.Write(".GetThisPtrUnsafe()");
            }
            else
            {
                w.Write(pname);
            }
        }
        w.Write(", &__retval));\n");
        w.Write(callIndent);
        w.Write("retval = __retval;\n");

        // Close fixed blocks (innermost first).
        for (int i = fixedNesting - 1; i >= 0; i--)
        {
            string indent = baseIndent + new string(' ', i * 4);
            w.Write(indent);
            w.Write("}\n");
        }

        // Close try and emit finally with cleanup for non-blittable PassArray params.
        if (hasNonBlittableArray)
        {
            w.Write("        }\n        finally\n        {\n");
            for (int i = 0; i < sig.Params.Count; i++)
            {
                ParamInfo p = sig.Params[i];
                ParamCategory cat = ParamHelpers.GetParamCategory(p);
                if (cat != ParamCategory.PassArray && cat != ParamCategory.FillArray) { continue; }
                if (p.Type is not AsmResolver.DotNet.Signatures.SzArrayTypeSignature szArr) { continue; }
                if (IsBlittablePrimitive(szArr.BaseType) || IsAnyStruct(szArr.BaseType)) { continue; }
                string raw = p.Parameter.Name ?? "param";
                if (IsString(szArr.BaseType))
                {
                    w.Write("\n            HStringArrayMarshaller.Dispose(__");
                    w.Write(raw);
                    w.Write("_pinnedHandleSpan);\n\n");
                    w.Write("            if (__");
                    w.Write(raw);
                    w.Write("_pinnedHandleArrayFromPool is not null)\n            {\n");
                    w.Write("                global::System.Buffers.ArrayPool<nint>.Shared.Return(__");
                    w.Write(raw);
                    w.Write("_pinnedHandleArrayFromPool);\n            }\n\n");
                    w.Write("            if (__");
                    w.Write(raw);
                    w.Write("_headerArrayFromPool is not null)\n            {\n");
                    w.Write("                global::System.Buffers.ArrayPool<HStringHeader>.Shared.Return(__");
                    w.Write(raw);
                    w.Write("_headerArrayFromPool);\n            }\n");
                }
                else
                {
                    string elementInteropArg = EncodeInteropTypeName(szArr.BaseType, TypedefNameType.Projected);
                    w.Write("\n            [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"Dispose\")]\n");
                    w.Write("            static extern void Dispose_");
                    w.Write(raw);
                    w.Write("([UnsafeAccessorType(\"");
                    w.Write(GetArrayMarshallerInteropPath(w, szArr.BaseType, elementInteropArg));
                    w.Write("\")] object _, uint length, void** data);\n\n");
                    w.Write("            fixed(void* _");
                    w.Write(raw);
                    w.Write(" = __");
                    w.Write(raw);
                    w.Write("_span)\n            {\n");
                    w.Write("                Dispose_");
                    w.Write(raw);
                    w.Write("(null, (uint) __");
                    w.Write(raw);
                    w.Write("_span.Length, (void**)_");
                    w.Write(raw);
                    w.Write(");\n            }\n");
                }
                w.Write("\n            if (__");
                w.Write(raw);
                w.Write("_arrayFromPool is not null)\n            {\n");
                w.Write("                global::System.Buffers.ArrayPool<nint>.Shared.Return(__");
                w.Write(raw);
                w.Write("_arrayFromPool);\n            }\n");
            }
            w.Write("        }\n");
        }

        w.Write("    }\n}\n");
    }

    /// <summary>Returns the IID expression for the class's default interface.</summary>
    private static string GetDefaultInterfaceIid(TypeWriter w, TypeDefinition classType)
    {
        ITypeDefOrRef? defaultIface = Helpers.GetDefaultInterface(classType);
        if (defaultIface is null) { return "default(global::System.Guid)"; }
        return w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteIidExpression(w, defaultIface)));
    }

    /// <summary>
    /// Mirrors C++ <c>write_composable_constructors</c>.
    /// Emits:
    /// 1. Public/protected constructors for each composable factory method (with proper body).
    /// 2. Static factory callback class (per ctor) for parameterized composable activation.
    /// 3. Four protected base-chaining constructors used by derived projected types.
    /// </summary>
    public static void WriteComposableConstructors(TypeWriter w, TypeDefinition? composableType, TypeDefinition classType, string visibility)
    {
        if (composableType is null) { return; }
        string typeName = classType.Name?.Value ?? string.Empty;

        // Emit the factory objref + IIDs at the top so the parameterized ctors can reference it.
        if (composableType.Methods.Count > 0)
        {
            string runtimeClassFullName = (classType.Namespace?.Value ?? string.Empty) + "." + typeName;
            string factoryObjRefName = GetObjRefName(w, composableType);
            WriteStaticFactoryObjRef(w, composableType, runtimeClassFullName, factoryObjRefName);
        }

        string defaultIfaceIid = GetDefaultInterfaceIid(w, classType);
        string marshalingType = GetMarshalingTypeName(classType);
        string defaultIfaceObjRef;
        ITypeDefOrRef? defaultIface = Helpers.GetDefaultInterface(classType);
        defaultIfaceObjRef = defaultIface is not null ? GetObjRefName(w, defaultIface) : string.Empty;
        int gcPressure = GetGcPressureAmount(classType);

        int methodIndex = 0;
        foreach (MethodDefinition method in composableType.Methods)
        {
            if (Helpers.IsSpecial(method)) { methodIndex++; continue; }
            // Composable factory methods have signature like:
            //   T CreateInstance(args, object baseInterface, out object innerInterface)
            // For the constructor on the projected class, we exclude the trailing two params.
            MethodSig sig = new(method);
            int userParamCount = sig.Params.Count >= 2 ? sig.Params.Count - 2 : sig.Params.Count;
            string callbackName = (method.Name?.Value ?? "Create") + "_" + userParamCount.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string argsName = callbackName + "Args";
            bool isParameterless = userParamCount == 0;

            w.Write("\n");
            w.Write(visibility);
            if (!isParameterless) { w.Write(" unsafe "); } else { w.Write(" "); }
            w.Write(typeName);
            w.Write("(");
            for (int i = 0; i < userParamCount; i++)
            {
                if (i > 0) { w.Write(", "); }
                WriteProjectionParameter(w, sig.Params[i]);
            }
            w.Write(")\n  : base(");
            if (isParameterless)
            {
                // base(default(WindowsRuntimeActivationTypes.DerivedComposed), <factoryObjRef>, <iid>, <marshalingType>)
                string factoryObjRef = GetObjRefName(w, composableType);
                w.Write("default(WindowsRuntimeActivationTypes.DerivedComposed), ");
                w.Write(factoryObjRef);
                w.Write(", ");
                w.Write(defaultIfaceIid);
                w.Write(", ");
                w.Write(marshalingType);
            }
            else
            {
                w.Write(callbackName);
                w.Write(".Instance, ");
                w.Write(defaultIfaceIid);
                w.Write(", ");
                w.Write(marshalingType);
                w.Write(", WindowsRuntimeActivationArgsReference.CreateUnsafe(new ");
                w.Write(argsName);
                w.Write("(");
                for (int i = 0; i < userParamCount; i++)
                {
                    if (i > 0) { w.Write(", "); }
                    string raw = sig.Params[i].Parameter.Name ?? "param";
                    w.Write(Helpers.IsKeyword(raw) ? "@" + raw : raw);
                }
                w.Write("))");
            }
            w.Write(")\n{\n");
            w.Write("if (GetType() == typeof(");
            w.Write(typeName);
            w.Write("))\n{\n");
            if (!string.IsNullOrEmpty(defaultIfaceObjRef))
            {
                w.Write(defaultIfaceObjRef);
                w.Write(" = NativeObjectReference;\n");
            }
            w.Write("}\n");
            if (gcPressure > 0)
            {
                w.Write("GC.AddMemoryPressure(");
                w.Write(gcPressure.ToString(System.Globalization.CultureInfo.InvariantCulture));
                w.Write(");\n");
            }
            w.Write("}\n");

            // Emit args struct + callback class for parameterized composable factories.
            if (!isParameterless)
            {
                EmitFactoryArgsStruct(w, sig, argsName);
                string factoryObjRefName = GetObjRefName(w, composableType);
                EmitFactoryCallbackClass(w, sig, callbackName, argsName, factoryObjRefName, methodIndex);
            }

            methodIndex++;
        }

        if (w.Settings.ReferenceProjection) { return; }

        // Emit the four base-chaining constructors used by derived projected types.
        string gcPressureBody = gcPressure > 0
            ? "GC.AddMemoryPressure(" + gcPressure.ToString(System.Globalization.CultureInfo.InvariantCulture) + ");\n"
            : string.Empty;

        // 1. WindowsRuntimeActivationTypes.DerivedComposed
        w.Write("\nprotected ");
        w.Write(typeName);
        w.Write("(WindowsRuntimeActivationTypes.DerivedComposed _, WindowsRuntimeObjectReference activationFactoryObjectReference, in Guid iid, CreateObjectReferenceMarshalingType marshalingType)\n");
        w.Write("  :base(_, activationFactoryObjectReference, in iid, marshalingType)\n");
        w.Write("{\n");
        if (!string.IsNullOrEmpty(gcPressureBody)) { w.Write(gcPressureBody); }
        w.Write("}\n");

        // 2. WindowsRuntimeActivationTypes.DerivedSealed
        w.Write("\nprotected ");
        w.Write(typeName);
        w.Write("(WindowsRuntimeActivationTypes.DerivedSealed _, WindowsRuntimeObjectReference activationFactoryObjectReference, in Guid iid, CreateObjectReferenceMarshalingType marshalingType)\n");
        w.Write("  :base(_, activationFactoryObjectReference, in iid, marshalingType)\n");
        w.Write("{\n");
        if (!string.IsNullOrEmpty(gcPressureBody)) { w.Write(gcPressureBody); }
        w.Write("}\n");

        // 3. WindowsRuntimeActivationFactoryCallback.DerivedComposed
        w.Write("\nprotected ");
        w.Write(typeName);
        w.Write("(WindowsRuntimeActivationFactoryCallback.DerivedComposed activationFactoryCallback, in Guid iid, CreateObjectReferenceMarshalingType marshalingType, WindowsRuntimeActivationArgsReference additionalParameters)\n");
        w.Write("  :base(activationFactoryCallback, in iid, marshalingType, additionalParameters)\n");
        w.Write("{\n");
        if (!string.IsNullOrEmpty(gcPressureBody)) { w.Write(gcPressureBody); }
        w.Write("}\n");

        // 4. WindowsRuntimeActivationFactoryCallback.DerivedSealed
        w.Write("\nprotected ");
        w.Write(typeName);
        w.Write("(WindowsRuntimeActivationFactoryCallback.DerivedSealed activationFactoryCallback, in Guid iid, CreateObjectReferenceMarshalingType marshalingType, WindowsRuntimeActivationArgsReference additionalParameters)\n");
        w.Write("  :base(activationFactoryCallback, in iid, marshalingType, additionalParameters)\n");
        w.Write("{\n");
        if (!string.IsNullOrEmpty(gcPressureBody)) { w.Write(gcPressureBody); }
        w.Write("}\n");
    }
}
