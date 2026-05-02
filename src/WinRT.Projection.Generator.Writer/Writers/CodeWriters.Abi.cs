// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;

namespace WindowsRuntime.ProjectionGenerator.Writer;

/// <summary>
/// ABI emission helpers (structs, enums, delegates, interfaces, classes).
/// Mirrors the C++ <c>write_abi_*</c> family. Initial port: emits the foundational
/// ABI scaffolding only; full marshaller/vtable emission to be filled in later.
/// </summary>
internal static partial class CodeWriters
{
    /// <summary>Mirrors C++ <c>is_type_blittable</c> partially.</summary>
    public static bool IsTypeBlittable(TypeDefinition type)
    {
        TypeCategory cat = TypeCategorization.GetCategory(type);
        if (cat == TypeCategory.Enum) { return true; }
        if (cat != TypeCategory.Struct) { return false; }
        // Walk fields - all must be blittable
        foreach (FieldDefinition field in type.Fields)
        {
            if (field.IsStatic || field.Signature is null) { continue; }
            if (!IsFieldTypeBlittable(field.Signature.FieldType)) { return false; }
        }
        return true;
    }

    private static bool IsFieldTypeBlittable(AsmResolver.DotNet.Signatures.TypeSignature sig)
    {
        if (sig is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlib)
        {
            return corlib.ElementType switch
            {
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Boolean => false,
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Char => false,
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.String => false,
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Object => false,
                _ => true
            };
        }
        // For TypeRef/TypeDef, resolve and check blittability.
        if (sig is AsmResolver.DotNet.Signatures.TypeDefOrRefSignature todr)
        {
            if (todr.Type is TypeDefinition td)
            {
                return IsTypeBlittable(td);
            }
            // Cross-module: try metadata cache.
            if (todr.Type is TypeReference tr && _cacheRef is not null)
            {
                string ns = tr.Namespace?.Value ?? string.Empty;
                string name = tr.Name?.Value ?? string.Empty;
                TypeDefinition? resolved = _cacheRef.Find(ns + "." + name);
                if (resolved is not null) { return IsTypeBlittable(resolved); }
            }
            return false;
        }
        return false;
    }

    /// <summary>Mirrors C++ <c>write_abi_enum</c>.</summary>
    public static void WriteAbiEnum(TypeWriter w, TypeDefinition type)
    {
        // The C++ version emits: write_struct_and_enum_marshaller_class, write_interface_entries_impl,
        // write_struct_and_enum_com_wrappers_marshaller_attribute_impl, write_reference_impl.
        // For now, emit a minimal marshaller class so the ComWrappersMarshaller attribute reference resolves.
        string name = type.Name?.Value ?? string.Empty;
        WriteStructEnumMarshallerClass(w, type);
        WriteReferenceImpl(w, type);
    }

    /// <summary>Mirrors C++ <c>write_abi_struct</c>.</summary>
    public static void WriteAbiStruct(TypeWriter w, TypeDefinition type)
    {
        string name = type.Name?.Value ?? string.Empty;

        // Emit the underlying ABI struct only when not blittable
        bool blittable = IsTypeBlittable(type);
        if (!blittable && !w.Settings.Component)
        {
            WriteComWrapperMarshallerAttribute(w, type);
            WriteValueTypeWinRTClassNameAttribute(w, type);
            w.Write(Helpers.InternalAccessibility(w.Settings));
            w.Write(" unsafe struct ");
            WriteTypedefName(w, type, TypedefNameType.ABI, false);
            w.Write("\n{\n");
            foreach (FieldDefinition field in type.Fields)
            {
                if (field.IsStatic || field.Signature is null) { continue; }
                AsmResolver.DotNet.Signatures.TypeSignature ft = field.Signature.FieldType;
                w.Write("public ");
                // Truth uses void* for string and Nullable<T> fields, but keeps the projected type
                // for everything else (including enums and bool — their C# layout matches the WinRT
                // ABI directly).
                if (IsString(ft) || TryGetNullablePrimitiveMarshallerName(ft, out _))
                {
                    w.Write("void*");
                }
                else
                {
                    WriteProjectedSignature(w, ft, false);
                }
                w.Write(" ");
                w.Write(field.Name?.Value ?? string.Empty);
                w.Write(";\n");
            }
            w.Write("}\n\n");
        }

        WriteStructEnumMarshallerClass(w, type);
        WriteReferenceImpl(w, type);
    }

    /// <summary>Mirrors C++ <c>write_abi_delegate</c>.</summary>
    public static void WriteAbiDelegate(TypeWriter w, TypeDefinition type)
    {
        // Mirrors C++: emit the marshaller, vftbl, native delegate, ComWrappers callback,
        // InterfaceEntriesImpl, and ComWrappers marshaller attribute. Reference impl is also
        // emitted (for IReference<delegate>).
        WriteDelegateMarshallerStub(w, type);
        WriteDelegateVftbl(w, type);
        WriteNativeDelegate(w, type);
        WriteDelegateInterfaceEntriesImpl(w, type);
        WriteDelegateImpl(w, type);
        WriteReferenceImpl(w, type);
    }

    /// <summary>Emits the <c>&lt;DelegateName&gt;Impl</c> static class providing the CCW vtable for a delegate.</summary>
    private static void WriteDelegateImpl(TypeWriter w, TypeDefinition type)
    {
        if (type.GenericParameters.Count > 0) { return; }
        MethodDefinition? invoke = Helpers.GetDelegateInvoke(type);
        if (invoke is null) { return; }
        MethodSig sig = new(invoke);
        string name = type.Name?.Value ?? string.Empty;
        string nameStripped = Helpers.StripBackticks(name);
        string iidExpr = w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteIidExpression(w, type)));

        w.Write("\ninternal static unsafe class ");
        w.Write(nameStripped);
        w.Write("Impl\n{\n");
        w.Write("    [FixedAddressValueType]\n");
        w.Write("    private static readonly ");
        w.Write(nameStripped);
        w.Write("Vftbl Vftbl;\n\n");
        w.Write("    static ");
        w.Write(nameStripped);
        w.Write("Impl()\n    {\n");
        w.Write("        *(IUnknownVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IUnknownVftbl*)IUnknownImpl.Vtable;\n");
        w.Write("        Vftbl.Invoke = &Invoke;\n");
        w.Write("    }\n\n");
        w.Write("    public static nint Vtable\n    {\n        [MethodImpl(MethodImplOptions.AggressiveInlining)]\n        get => (nint)Unsafe.AsPointer(in Vftbl);\n    }\n\n");

        w.Write("    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]\n");
        w.Write("    private static int Invoke(");
        WriteAbiParameterTypesPointer(w, sig, includeParamNames: true);
        w.Write(")\n    {\n");
        EmitDelegateInvokeBody(w, type, sig);
        w.Write("    }\n\n");

        w.Write("    public static ref readonly Guid IID\n    {\n        [MethodImpl(MethodImplOptions.AggressiveInlining)]\n        get => ref ");
        w.Write(iidExpr);
        w.Write(";\n    }\n}\n");
    }

    /// <summary>Emits the body of the delegate Impl Invoke method (CCW dispatch).</summary>
    private static void EmitDelegateInvokeBody(TypeWriter w, TypeDefinition type, MethodSig sig)
    {
        // Check if we can emit a body for this signature.
        AsmResolver.DotNet.Signatures.TypeSignature? rt = sig.ReturnType;
        bool simple = rt is null
            || IsBlittablePrimitive(rt)
            || IsAnyStruct(rt)
            || IsString(rt)
            || IsRuntimeClassOrInterface(rt)
            || IsObject(rt);
        if (rt is not null && IsHResultException(rt)) { simple = false; }
        if (simple)
        {
            foreach (ParamInfo p in sig.Params)
            {
                ParamCategory cat = ParamHelpers.GetParamCategory(p);
                if (cat == ParamCategory.PassArray || cat == ParamCategory.FillArray)
                {
                    // Allow blittable primitive arrays only (Span<byte>, Span<int>, etc.).
                    if (p.Type is AsmResolver.DotNet.Signatures.SzArrayTypeSignature szP)
                    {
                        if (IsBlittablePrimitive(szP.BaseType)) { continue; }
                        if (IsAnyStruct(szP.BaseType)) { continue; }
                    }
                    simple = false; break;
                }
                if (cat != ParamCategory.In) { simple = false; break; }
                if (IsHResultException(p.Type)) { simple = false; break; }
                if (IsBlittablePrimitive(p.Type)) { continue; }
                if (IsAnyStruct(p.Type)) { continue; }
                if (IsString(p.Type)) { continue; }
                if (IsRuntimeClassOrInterface(p.Type)) { continue; }
                if (IsObject(p.Type)) { continue; }
                if (IsGenericInstance(p.Type)) { continue; }
                simple = false; break;
            }
        }

        if (!simple)
        {
            w.Write("        throw null!;\n");
            return;
        }

        string projectedName = w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteTypedefName(w, type, TypedefNameType.Projected, true)));
        if (!projectedName.StartsWith("global::", System.StringComparison.Ordinal)) { projectedName = "global::" + projectedName; }

        bool hasReturn = rt is not null;
        bool returnIsString = hasReturn && IsString(rt!);
        bool returnIsRefType = hasReturn && (IsRuntimeClassOrInterface(rt!) || IsObject(rt!));

        // Emit UnsafeAccessor static extern for each generic instance param.
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            if (!IsGenericInstance(p.Type)) { continue; }
            string raw = p.Parameter.Name ?? ("p" + i);
            string interopTypeName = EncodeInteropTypeName(p.Type, TypedefNameType.ABI) + ", WinRT.Interop";
            string projectedTypeName = w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteProjectedSignature(w, p.Type, false)));
            w.Write("        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"ConvertToManaged\")]\n");
            w.Write("        static extern ");
            w.Write(projectedTypeName);
            w.Write(" ConvertToManaged_");
            w.Write(raw);
            w.Write("([UnsafeAccessorType(\"");
            w.Write(interopTypeName);
            w.Write("\")] object _, void* value);\n");
        }

        if (hasReturn)
        {
            // Declare local for the managed result value.
            w.Write("        ");
            string projected = w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteProjectedSignature(w, rt!, false)));
            w.Write(projected);
            w.Write(" __result = default;\n");
            w.Write("        *");
            w.Write(GetReturnParamName(sig));
            w.Write(" = default;\n");
        }

        // Construct ReadOnlySpan<T> for each PassArray param.
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.PassArray && cat != ParamCategory.FillArray) { continue; }
            if (p.Type is not AsmResolver.DotNet.Signatures.SzArrayTypeSignature szArr) { continue; }
            string raw = p.Parameter.Name ?? ("p" + i);
            string callName = Helpers.IsKeyword(raw) ? "@" + raw : raw;
            string elementProjected = w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteProjectionType(w, TypeSemanticsFactory.Get(szArr.BaseType))));
            w.Write("        ");
            w.Write(cat == ParamCategory.PassArray ? "ReadOnlySpan<" : "Span<");
            w.Write(elementProjected);
            w.Write("> __");
            w.Write(raw);
            w.Write(" = new(");
            w.Write(callName);
            w.Write(", (int)__");
            w.Write(raw);
            w.Write("Length);\n");
        }
        w.Write("\n");

        w.Write("        try\n        {\n");
        if (hasReturn) { w.Write("            __result = "); }
        else { w.Write("            "); }
        w.Write("ComInterfaceDispatch.GetInstance<");
        w.Write(projectedName);
        w.Write(">((ComInterfaceDispatch*)thisPtr).Invoke(");
        for (int i = 0; i < sig.Params.Count; i++)
        {
            if (i > 0) { w.Write(", "); }
            ParamInfo p = sig.Params[i];
            string raw = p.Parameter.Name ?? ("p" + i);
            string callName = Helpers.IsKeyword(raw) ? "@" + raw : raw;
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat == ParamCategory.PassArray || cat == ParamCategory.FillArray)
            {
                w.Write("__");
                w.Write(raw);
                continue;
            }
            // bool: native bool -> managed bool (no conversion needed)
            if (p.Type is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlibB &&
                corlibB.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Boolean)
            {
                w.Write(callName);
            }
            // char: native char -> managed char (no conversion needed)
            else if (p.Type is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlibC &&
                     corlibC.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Char)
            {
                w.Write(callName);
            }
            // String: HStringMarshaller.ConvertToManaged
            else if (IsString(p.Type))
            {
                w.Write("HStringMarshaller.ConvertToManaged(");
                w.Write(callName);
                w.Write(")");
            }
            // Enum: native is the projected enum type, pass directly.
            else if (IsEnumType(p.Type))
            {
                w.Write(callName);
            }
            else if (IsBlittablePrimitive(p.Type) || IsAnyStruct(p.Type))
            {
                w.Write(callName);
            }
            // Generic instance: call ConvertToManaged_<name>
            else if (IsGenericInstance(p.Type))
            {
                w.Write("ConvertToManaged_");
                w.Write(raw);
                w.Write("(null, ");
                w.Write(callName);
                w.Write(")");
            }
            else
            {
                EmitMarshallerConvertToManaged(w, p.Type, callName);
            }
        }
        w.Write(");\n");

        if (hasReturn)
        {
            string retName = GetReturnParamName(sig);
            // Marshal the managed result back to the native pointer.
            if (returnIsString)
            {
                w.Write("            *");
                w.Write(retName);
                w.Write(" = HStringMarshaller.ConvertToUnmanaged(__result);\n");
            }
            else if (returnIsRefType)
            {
                w.Write("            *");
                w.Write(retName);
                w.Write(" = ");
                EmitMarshallerConvertToUnmanaged(w, rt!, "__result");
                w.Write(".DetachThisPtrUnsafe();\n");
            }
            else if (rt is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlibBoolRet &&
                     corlibBoolRet.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Boolean)
            {
                w.Write("            *");
                w.Write(retName);
                w.Write(" = __result;\n");
            }
            else if (rt is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlibCharRet &&
                     corlibCharRet.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Char)
            {
                w.Write("            *");
                w.Write(retName);
                w.Write(" = __result;\n");
            }
            else if (IsEnumType(rt!))
            {
                w.Write("            *");
                w.Write(retName);
                w.Write(" = __result;\n");
            }
            else
            {
                w.Write("            *");
                w.Write(retName);
                w.Write(" = __result;\n");
            }
        }

        w.Write("            return 0;\n        }\n");
        w.Write("        catch (Exception __exception__)\n        {\n");
        w.Write("            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(__exception__);\n        }\n");
    }

    /// <summary>
    /// Returns the interop assembly path for an array marshaller of a given element type.
    /// The interop generator names array marshallers <c>ABI.&lt;typeNamespace&gt;.&lt;&lt;assembly&gt;ElementName&gt;ArrayMarshaller</c>
    /// (typeNamespace prefix outside the brackets, and the element inside the brackets uses just the
    /// type name without its namespace because depth=0 in the interop generator's AppendRawTypeName).
    /// </summary>
    private static string GetArrayMarshallerInteropPath(TypeWriter w, AsmResolver.DotNet.Signatures.TypeSignature elementType, string encodedElement)
    {
        // The 'encodedElement' passed in uses the depth>0 form (assembly + hyphenated namespace + name),
        // but inside the array brackets the interop generator uses the depth=0 form (assembly + just name).
        // Re-encode the element with the top-level form for accurate matching.
        string topLevelElement = EncodeArrayElementName(elementType);
        // Resolve the element's namespace to determine the path prefix.
        string ns = GetMappedNamespace(elementType);
        if (string.IsNullOrEmpty(ns))
        {
            return "ABI.<" + topLevelElement + ">ArrayMarshaller, WinRT.Interop";
        }
        return "ABI." + ns + ".<" + topLevelElement + ">ArrayMarshaller, WinRT.Interop";
    }

    /// <summary>Returns the (possibly mapped) namespace of a type signature, or 'System' for fundamentals.</summary>
    private static string GetMappedNamespace(AsmResolver.DotNet.Signatures.TypeSignature sig)
    {
        // Fundamentals (string, bool, int, etc.) live in 'System' for ArrayMarshaller path purposes.
        if (sig is AsmResolver.DotNet.Signatures.CorLibTypeSignature) { return "System"; }
        AsmResolver.DotNet.ITypeDefOrRef? td = null;
        if (sig is AsmResolver.DotNet.Signatures.TypeDefOrRefSignature tds) { td = tds.Type; }
        else if (sig is AsmResolver.DotNet.Signatures.GenericInstanceTypeSignature gi) { td = gi.GenericType; }
        if (td is null) { return string.Empty; }
        string typeNs = td.Namespace?.Value ?? string.Empty;
        string typeName = td.Name?.Value ?? string.Empty;
        MappedType? mapped = MappedTypes.Get(typeNs, typeName);
        return mapped is not null ? mapped.MappedNamespace : typeNs;
    }

    /// <summary>
    /// Encodes the array element type name as the interop generator's AppendRawTypeName at depth=0:
    /// fundamentals use their short C# name; typedefs use just the type name (no namespace) prefixed
    /// with the assembly marker; generic instances include their assembly marker, name, and type arguments.
    /// </summary>
    private static string EncodeArrayElementName(AsmResolver.DotNet.Signatures.TypeSignature elementType)
    {
        System.Text.StringBuilder sb = new();
        EncodeArrayElementNameInto(sb, elementType);
        return sb.ToString();
    }

    private static void EncodeArrayElementNameInto(System.Text.StringBuilder sb, AsmResolver.DotNet.Signatures.TypeSignature sig)
    {
        switch (sig)
        {
            case AsmResolver.DotNet.Signatures.CorLibTypeSignature corlib:
                EncodeFundamental(sb, corlib, TypedefNameType.Projected);
                return;
            case AsmResolver.DotNet.Signatures.TypeDefOrRefSignature td:
                EncodeArrayElementForTypeDef(sb, td.Type, generic_args: null);
                return;
            case AsmResolver.DotNet.Signatures.GenericInstanceTypeSignature gi:
                EncodeArrayElementForTypeDef(sb, gi.GenericType, generic_args: gi.TypeArguments);
                return;
            default:
                sb.Append(sig.FullName);
                return;
        }
    }

    private static void EncodeArrayElementForTypeDef(System.Text.StringBuilder sb, AsmResolver.DotNet.ITypeDefOrRef type, System.Collections.Generic.IList<AsmResolver.DotNet.Signatures.TypeSignature>? generic_args)
    {
        string typeNs = type.Namespace?.Value ?? string.Empty;
        string typeName = type.Name?.Value ?? string.Empty;
        // Apply mapped-type remapping (e.g. Windows.Foundation.IReference -> System.Nullable).
        MappedType? mapped = MappedTypes.Get(typeNs, typeName);
        if (mapped is not null)
        {
            typeNs = mapped.MappedNamespace;
            typeName = mapped.MappedName;
        }
        // Replace generic arity backtick with apostrophe.
        typeName = typeName.Replace('`', '\'');

        // Assembly marker prefix.
        sb.Append(GetInteropAssemblyMarker(typeNs, typeName, mapped));
        // Top-level: just the type name (no namespace).
        sb.Append(typeName);

        // Generic arguments use the standard EncodeInteropTypeNameInto (depth > 0).
        if (generic_args is { Count: > 0 })
        {
            sb.Append('<');
            for (int i = 0; i < generic_args.Count; i++)
            {
                if (i > 0) { sb.Append('|'); }
                EncodeInteropTypeNameInto(sb, generic_args[i], TypedefNameType.Projected);
            }
            sb.Append('>');
        }
    }

    /// <summary>Mirrors C++ <c>write_delegate_vtbl</c>.</summary>
    private static void WriteDelegateVftbl(TypeWriter w, TypeDefinition type)
    {
        if (type.GenericParameters.Count > 0) { return; }
        MethodDefinition? invoke = Helpers.GetDelegateInvoke(type);
        if (invoke is null) { return; }
        MethodSig sig = new(invoke);
        string name = type.Name?.Value ?? string.Empty;
        string nameStripped = Helpers.StripBackticks(name);

        w.Write("\n[StructLayout(LayoutKind.Sequential)]\n");
        w.Write("internal unsafe struct ");
        w.Write(nameStripped);
        w.Write("Vftbl\n{\n");
        w.Write("    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, int> QueryInterface;\n");
        w.Write("    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;\n");
        w.Write("    public delegate* unmanaged[MemberFunction]<void*, uint> Release;\n");
        w.Write("    public delegate* unmanaged[MemberFunction]<");
        WriteAbiParameterTypesPointer(w, sig);
        w.Write(", int> Invoke;\n");
        w.Write("}\n");
    }

    /// <summary>Mirrors C++ <c>write_native_delegate</c>.</summary>
    private static void WriteNativeDelegate(TypeWriter w, TypeDefinition type)
    {
        if (type.GenericParameters.Count > 0) { return; }
        MethodDefinition? invoke = Helpers.GetDelegateInvoke(type);
        if (invoke is null) { return; }
        MethodSig sig = new(invoke);
        string name = type.Name?.Value ?? string.Empty;
        string nameStripped = Helpers.StripBackticks(name);

        w.Write("\npublic static unsafe class ");
        w.Write(nameStripped);
        w.Write("NativeDelegate\n{\n");
        w.Write("    public static ");
        WriteProjectionReturnType(w, sig);
        w.Write(" ");
        w.Write(nameStripped);
        w.Write("Invoke(this WindowsRuntimeObjectReference objectReference");
        if (sig.Params.Count > 0) { w.Write(", "); }
        WriteParameterList(w, sig);
        w.Write(")");

        // Use the same body emitter as ABI Methods, but with vtable slot 3 and using
        // 'objectReference' instead of 'thisReference'. Tweak by using slot=3.
        EmitNativeDelegateBody(w, sig);

        w.Write("}\n");
    }

    /// <summary>Emits the body of the native delegate's Invoke extension method.</summary>
    private static void EmitNativeDelegateBody(TypeWriter w, MethodSig sig)
    {
        AsmResolver.DotNet.Signatures.TypeSignature? rt = sig.ReturnType;

        // We support the same set of param/return types as EmitAbiMethodBodyIfSimple does for the
        // primary case (no Out/Ref/Array for now in delegates). Allow blittable/string/runtime class /
        // object/generic instance In params and these return types.
        bool simpleParamsAndReturn = rt is null
            || IsBlittablePrimitive(rt)
            || IsAnyStruct(rt)
            || IsString(rt)
            || IsRuntimeClassOrInterface(rt)
            || IsObject(rt);
        if (rt is not null && IsHResultException(rt)) { simpleParamsAndReturn = false; }
        if (simpleParamsAndReturn)
        {
            foreach (ParamInfo p in sig.Params)
            {
                ParamCategory cat = ParamHelpers.GetParamCategory(p);
                if (cat == ParamCategory.PassArray || cat == ParamCategory.FillArray)
                {
                    if (p.Type is AsmResolver.DotNet.Signatures.SzArrayTypeSignature szP)
                    {
                        if (IsBlittablePrimitive(szP.BaseType)) { continue; }
                        if (IsAnyStruct(szP.BaseType)) { continue; }
                    }
                    simpleParamsAndReturn = false; break;
                }
                if (cat != ParamCategory.In) { simpleParamsAndReturn = false; break; }
                if (IsHResultException(p.Type)) { simpleParamsAndReturn = false; break; }
                if (IsBlittablePrimitive(p.Type)) { continue; }
                if (IsAnyStruct(p.Type)) { continue; }
                if (IsString(p.Type)) { continue; }
                if (IsRuntimeClassOrInterface(p.Type)) { continue; }
                if (IsObject(p.Type)) { continue; }
                if (IsGenericInstance(p.Type)) { continue; }
                simpleParamsAndReturn = false; break;
            }
        }

        if (!simpleParamsAndReturn)
        {
            w.Write(" => throw null!;\n");
            return;
        }

        bool hasReturn = rt is not null;
        bool returnIsString = hasReturn && IsString(rt!);
        bool returnIsRefType = hasReturn && (IsRuntimeClassOrInterface(rt!) || IsObject(rt!));
        bool returnIsAnyStruct = hasReturn && IsAnyStruct(rt!);

        w.Write("\n    {\n");
        w.Write("        using WindowsRuntimeObjectReferenceValue objectValue = objectReference.AsValue();\n");
        w.Write("        void* ThisPtr = objectValue.GetThisPtrUnsafe();\n");

        // Marshal ref-type input params.
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            if (IsRuntimeClassOrInterface(p.Type) || IsObject(p.Type))
            {
                string raw = p.Parameter.Name ?? "param";
                string callName = Helpers.IsKeyword(raw) ? "@" + raw : raw;
                w.Write("        using WindowsRuntimeObjectReferenceValue __");
                w.Write(raw);
                w.Write(" = ");
                EmitMarshallerConvertToUnmanaged(w, p.Type, callName);
                w.Write(";\n");
            }
            else if (IsGenericInstance(p.Type))
            {
                string raw = p.Parameter.Name ?? "param";
                string callName = Helpers.IsKeyword(raw) ? "@" + raw : raw;
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
                w.Write(callName);
                w.Write(");\n");
            }
        }
        // String params: declare void* locals (initialized later inside try).
        bool hasStringParams = false;
        for (int i = 0; i < sig.Params.Count; i++)
        {
            if (IsString(sig.Params[i].Type))
            {
                string raw = sig.Params[i].Parameter.Name ?? "param";
                w.Write("        void* __");
                w.Write(raw);
                w.Write(" = default;\n");
                hasStringParams = true;
            }
        }

        // Declare return value local.
        if (hasReturn)
        {
            w.Write("        ");
            if (returnIsString || returnIsRefType) { w.Write("void*"); }
            else if (returnIsAnyStruct) { w.Write(GetBlittableStructAbiType(w, rt!)); }
            else { w.Write(GetAbiPrimitiveType(rt!)); }
            w.Write(" __retval = default;\n");
        }

        bool needsTryFinally = hasStringParams || returnIsString || returnIsRefType;
        if (needsTryFinally) { w.Write("        try\n        {\n"); }
        string indent = needsTryFinally ? "            " : "        ";

        for (int i = 0; i < sig.Params.Count; i++)
        {
            if (IsString(sig.Params[i].Type))
            {
                string raw = sig.Params[i].Parameter.Name ?? "param";
                string callName = Helpers.IsKeyword(raw) ? "@" + raw : raw;
                w.Write(indent);
                w.Write("__");
                w.Write(raw);
                w.Write(" = HStringMarshaller.ConvertToUnmanaged(");
                w.Write(callName);
                w.Write(");\n");
            }
        }

        // Open fixed blocks for PassArray/FillArray params (blittable element only).
        int fixedNesting = 0;
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.PassArray && cat != ParamCategory.FillArray) { continue; }
            string raw = p.Parameter.Name ?? "param";
            string callName = Helpers.IsKeyword(raw) ? "@" + raw : raw;
            w.Write(indent);
            w.Write(new string(' ', fixedNesting * 4));
            w.Write("fixed(void* _");
            w.Write(raw);
            w.Write(" = ");
            w.Write(callName);
            w.Write(")\n");
            w.Write(indent);
            w.Write(new string(' ', fixedNesting * 4));
            w.Write("{\n");
            fixedNesting++;
        }

        string callIndent = indent + new string(' ', fixedNesting * 4);

        // Function pointer call.
        w.Write(callIndent);
        w.Write("RestrictedErrorInfo.ThrowExceptionForHR((*(delegate* unmanaged[MemberFunction]<void*");
        foreach (ParamInfo p in sig.Params)
        {
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat == ParamCategory.PassArray || cat == ParamCategory.FillArray) { w.Write(", uint, void*"); continue; }
            w.Write(", ");
            if (IsString(p.Type) || IsRuntimeClassOrInterface(p.Type) || IsObject(p.Type) || IsGenericInstance(p.Type)) { w.Write("void*"); }
            else if (IsAnyStruct(p.Type)) { w.Write(GetBlittableStructAbiType(w, p.Type)); }
            else { w.Write(GetAbiPrimitiveType(p.Type)); }
        }
        if (hasReturn)
        {
            w.Write(", ");
            if (returnIsString || returnIsRefType) { w.Write("void**"); }
            else if (returnIsAnyStruct) { w.Write(GetBlittableStructAbiType(w, rt!)); w.Write('*'); }
            else { w.Write(GetAbiPrimitiveType(rt!)); w.Write('*'); }
        }
        w.Write(", int>**)ThisPtr)[3](ThisPtr");
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            string raw = p.Parameter.Name ?? "param";
            string callName = Helpers.IsKeyword(raw) ? "@" + raw : raw;
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat == ParamCategory.PassArray || cat == ParamCategory.FillArray)
            {
                w.Write(", (uint)");
                w.Write(callName);
                w.Write(".Length, _");
                w.Write(raw);
                continue;
            }
            w.Write(", ");
            if (IsString(p.Type))
            {
                w.Write("__");
                w.Write(raw);
            }
            else if (IsRuntimeClassOrInterface(p.Type) || IsObject(p.Type) || IsGenericInstance(p.Type))
            {
                w.Write("__");
                w.Write(raw);
                w.Write(".GetThisPtrUnsafe()");
            }
            else if (IsAnyStruct(p.Type))
            {
                w.Write(callName);
            }
            else if (p.Type is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlibBool && corlibBool.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Boolean)
            {
                w.Write(callName);
            }
            else if (p.Type is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlibChar && corlibChar.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Char)
            {
                w.Write(callName);
            }
            else if (IsEnumType(p.Type))
            {
                // Enum: function pointer signature uses the projected enum type, so pass directly.
                w.Write(callName);
            }
            else
            {
                w.Write(callName);
            }
        }
        if (hasReturn) { w.Write(", &__retval"); }
        w.Write("));\n");

        // Return value conversion.
        if (hasReturn)
        {
            w.Write(callIndent);
            if (returnIsString)
            {
                w.Write("return HStringMarshaller.ConvertToManaged(__retval);\n");
            }
            else if (returnIsRefType)
            {
                w.Write("return ");
                EmitMarshallerConvertToManaged(w, rt!, "__retval");
                w.Write(";\n");
            }
            else if (returnIsAnyStruct)
            {
                w.Write("return __retval;\n");
            }
            else if (rt is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlibBoolRet && corlibBoolRet.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Boolean)
            {
                w.Write("return __retval;\n");
            }
            else if (rt is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlibCharRet && corlibCharRet.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Char)
            {
                w.Write("return __retval;\n");
            }
            else if (IsEnumType(rt!))
            {
                // Enum: __retval is already the projected enum type, no cast needed.
                w.Write("return __retval;\n");
            }
            else
            {
                w.Write("return __retval;\n");
            }
        }

        // Close fixed blocks (innermost first).
        for (int i = fixedNesting - 1; i >= 0; i--)
        {
            w.Write(indent);
            w.Write(new string(' ', i * 4));
            w.Write("}\n");
        }

        if (needsTryFinally)
        {
            w.Write("        }\n        finally\n        {\n");
            for (int i = 0; i < sig.Params.Count; i++)
            {
                if (IsString(sig.Params[i].Type))
                {
                    string raw = sig.Params[i].Parameter.Name ?? "param";
                    w.Write("            HStringMarshaller.Free(__");
                    w.Write(raw);
                    w.Write(");\n");
                }
            }
            if (returnIsString)
            {
                w.Write("            HStringMarshaller.Free(__retval);\n");
            }
            else if (returnIsRefType)
            {
                w.Write("            WindowsRuntimeUnknownMarshaller.Free(__retval);\n");
            }
            w.Write("        }\n");
        }
        w.Write("    }\n");
    }

    /// <summary>Mirrors C++ <c>write_delegates_interface_entries_impl</c>.</summary>
    private static void WriteDelegateInterfaceEntriesImpl(TypeWriter w, TypeDefinition type)
    {
        if (type.GenericParameters.Count > 0) { return; }
        string name = type.Name?.Value ?? string.Empty;
        string nameStripped = Helpers.StripBackticks(name);
        string iidExpr = w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteIidExpression(w, type)));
        string iidRefExpr = w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteIidReferenceExpression(w, type)));

        w.Write("\nfile static class ");
        w.Write(nameStripped);
        w.Write("InterfaceEntriesImpl\n{\n");
        w.Write("    [FixedAddressValueType]\n");
        w.Write("    public static readonly DelegateReferenceInterfaceEntries Entries;\n\n");
        w.Write("    static ");
        w.Write(nameStripped);
        w.Write("InterfaceEntriesImpl()\n    {\n");
        w.Write("        Entries.Delegate.IID = ");
        w.Write(iidExpr);
        w.Write(";\n");
        w.Write("        Entries.Delegate.Vtable = ");
        w.Write(nameStripped);
        w.Write("Impl.Vtable;\n");
        w.Write("        Entries.DelegateReference.IID = ");
        w.Write(iidRefExpr);
        w.Write(";\n");
        w.Write("        Entries.DelegateReference.Vtable = ");
        w.Write(nameStripped);
        w.Write("ReferenceImpl.Vtable;\n");
        w.Write("        Entries.IPropertyValue.IID = global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IPropertyValue;\n");
        w.Write("        Entries.IPropertyValue.Vtable = global::WindowsRuntime.InteropServices.IPropertyValueImpl.OtherTypeVtable;\n");
        w.Write("        Entries.IStringable.IID = global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IStringable;\n");
        w.Write("        Entries.IStringable.Vtable = global::WindowsRuntime.InteropServices.IStringableImpl.Vtable;\n");
        w.Write("        Entries.IWeakReferenceSource.IID = global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IWeakReferenceSource;\n");
        w.Write("        Entries.IWeakReferenceSource.Vtable = global::WindowsRuntime.InteropServices.IWeakReferenceSourceImpl.Vtable;\n");
        w.Write("        Entries.IMarshal.IID = global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IMarshal;\n");
        w.Write("        Entries.IMarshal.Vtable = global::WindowsRuntime.InteropServices.IMarshalImpl.Vtable;\n");
        w.Write("        Entries.IAgileObject.IID = global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IAgileObject;\n");
        w.Write("        Entries.IAgileObject.Vtable = global::WindowsRuntime.InteropServices.IAgileObjectImpl.Vtable;\n");
        w.Write("        Entries.IInspectable.IID = global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IInspectable;\n");
        w.Write("        Entries.IInspectable.Vtable = global::WindowsRuntime.InteropServices.IInspectableImpl.Vtable;\n");
        w.Write("        Entries.IUnknown.IID = global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IUnknown;\n");
        w.Write("        Entries.IUnknown.Vtable = global::WindowsRuntime.InteropServices.IUnknownImpl.Vtable;\n");
        w.Write("    }\n}\n");
    }

    /// <summary>Mirrors C++ <c>write_temp_delegate_event_source_subclass</c>.</summary>
    public static void WriteTempDelegateEventSourceSubclass(TypeWriter w, TypeDefinition type)
    {
        // Skip generic delegates: only non-generic delegates get a per-delegate EventSource subclass.
        // Generic delegates (e.g. EventHandler<T>) use the generic EventHandlerEventSource<T> directly.
        if (type.GenericParameters.Count > 0) { return; }

        MethodDefinition? invoke = Helpers.GetDelegateInvoke(type);
        if (invoke is null) { return; }
        MethodSig sig = new(invoke);
        string name = type.Name?.Value ?? string.Empty;
        string nameStripped = Helpers.StripBackticks(name);

        // Compute the projected type name (with global::) used as the generic argument.
        string projectedName = w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteTypedefName(w, type, TypedefNameType.Projected, true)));
        if (!projectedName.StartsWith("global::", System.StringComparison.Ordinal))
        {
            projectedName = "global::" + projectedName;
        }

        w.Write("\npublic sealed unsafe class ");
        w.Write(nameStripped);
        w.Write("EventSource : EventSource<");
        w.Write(projectedName);
        w.Write(">\n{\n");
        w.Write("    /// <inheritdoc cref=\"EventSource{T}.EventSource\"/>\n");
        w.Write("    public ");
        w.Write(nameStripped);
        w.Write("EventSource(WindowsRuntimeObjectReference nativeObjectReference, int index)\n        : base(nativeObjectReference, index)\n    {\n    }\n\n");
        w.Write("    /// <inheritdoc/>\n");
        w.Write("    protected override WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(");
        w.Write(projectedName);
        w.Write(" value)\n    {\n        return ");
        w.Write(nameStripped);
        w.Write("Marshaller.ConvertToUnmanaged(value);\n    }\n\n");
        w.Write("    /// <inheritdoc/>\n");
        w.Write("    protected override EventSourceState<");
        w.Write(projectedName);
        w.Write("> CreateEventSourceState()\n    {\n        return new EventState(GetNativeObjectReferenceThisPtrUnsafe(), Index);\n    }\n\n");
        w.Write("    private sealed class EventState : EventSourceState<");
        w.Write(projectedName);
        w.Write(">\n    {\n");
        w.Write("        /// <inheritdoc cref=\"EventSourceState{T}.EventSourceState\"/>\n");
        w.Write("        public EventState(void* thisPtr, int index)\n            : base(thisPtr, index)\n        {\n        }\n\n");
        w.Write("        /// <inheritdoc/>\n");
        w.Write("        protected override ");
        w.Write(projectedName);
        w.Write(" GetEventInvoke()\n        {\n");
        // Build parameter name list for the lambda.
        w.Write("            return (");
        for (int i = 0; i < sig.Params.Count; i++)
        {
            if (i > 0) { w.Write(", "); }
            string raw = sig.Params[i].Parameter.Name ?? "p";
            w.Write(Helpers.IsKeyword(raw) ? "@" + raw : raw);
        }
        w.Write(") => TargetDelegate.Invoke(");
        for (int i = 0; i < sig.Params.Count; i++)
        {
            if (i > 0) { w.Write(", "); }
            string raw = sig.Params[i].Parameter.Name ?? "p";
            w.Write(Helpers.IsKeyword(raw) ? "@" + raw : raw);
        }
        w.Write(");\n");
        w.Write("        }\n    }\n}\n");
    }

    /// <summary>Mirrors C++ <c>write_abi_class</c>.</summary>
    public static void WriteAbiClass(TypeWriter w, TypeDefinition type)
    {
        // Static classes don't get a *Marshaller (no instances).
        if (TypeCategorization.IsStatic(type)) { return; }
        // Emit a ComWrappers marshaller class so the attribute reference resolves
        WriteClassMarshallerStub(w, type);
    }

    /// <summary>Mirrors C++ <c>write_abi_interface</c>.</summary>
    public static void WriteAbiInterface(TypeWriter w, TypeDefinition type)
    {
        // Generic interfaces are handled by interopgen
        if (type.GenericParameters.Count > 0) { return; }

        // The C++ also emits write_static_abi_classes here - we emit a basic stub for now
        WriteInterfaceMarshallerStub(w, type);

        // For internal projections, just the static ABI methods class is enough.
        if (TypeCategorization.IsProjectionInternal(type)) { return; }

        WriteInterfaceVftbl(w, type);
        WriteInterfaceImpl(w, type);
        WriteInterfaceIdicImpl(w, type);
        WriteInterfaceMarshaller(w, type);
    }

    /// <summary>Mirrors C++ <c>emit_impl_type</c>.</summary>
    public static bool EmitImplType(TypeWriter w, TypeDefinition type)
    {
        if (w.Settings.Component) { return true; }
        if (TypeCategorization.IsExclusiveTo(type) && !w.Settings.PublicExclusiveTo)
        {
            // Mirror C++ emit_impl_type: only emit Impl for exclusive-to interfaces if at least
            // one interface impl on the exclusive_to class is marked [Overridable] and matches
            // this interface. Otherwise the Impl wouldn't be reachable as a CCW.
            TypeDefinition? exclusiveToType = GetExclusiveToType(type);
            if (exclusiveToType is null) { return true; }
            bool hasOverridable = false;
            foreach (InterfaceImplementation impl in exclusiveToType.Interfaces)
            {
                if (impl.Interface is null) { continue; }
                TypeDefinition? ifaceTd = ResolveInterfaceTypeDef(impl.Interface);
                if (ifaceTd == type && Helpers.IsOverridable(impl)) { hasOverridable = true; break; }
            }
            return hasOverridable;
        }
        return true;
    }

    /// <summary>
    /// Returns the parent class for an interface marked <c>[ExclusiveToAttribute(typeof(T))]</c>.
    /// Mirrors C++ <c>get_exclusive_to_type</c>.
    /// </summary>
    internal static TypeDefinition? GetExclusiveToType(TypeDefinition iface)
    {
        if (_cacheRef is null) { return null; }
        for (int i = 0; i < iface.CustomAttributes.Count; i++)
        {
            CustomAttribute attr = iface.CustomAttributes[i];
            ITypeDefOrRef? attrType = attr.Constructor?.DeclaringType;
            if (attrType is null) { continue; }
            if (attrType.Namespace?.Value != "Windows.Foundation.Metadata" ||
                attrType.Name?.Value != "ExclusiveToAttribute") { continue; }
            if (attr.Signature is null) { continue; }
            for (int j = 0; j < attr.Signature.FixedArguments.Count; j++)
            {
                AsmResolver.DotNet.Signatures.CustomAttributeArgument arg = attr.Signature.FixedArguments[j];
                if (arg.Element is AsmResolver.DotNet.Signatures.TypeSignature sig)
                {
                    string fullName = sig.FullName ?? string.Empty;
                    TypeDefinition? td = _cacheRef.Find(fullName);
                    if (td is not null) { return td; }
                }
                else if (arg.Element is string s)
                {
                    TypeDefinition? td = _cacheRef.Find(s);
                    if (td is not null) { return td; }
                }
            }
        }
        return null;
    }

    /// <summary>Resolves an InterfaceImpl's interface reference to a TypeDefinition (same module or via metadata cache).</summary>
    private static TypeDefinition? ResolveInterfaceTypeDef(ITypeDefOrRef ifaceRef)
    {
        if (ifaceRef is TypeDefinition td) { return td; }
        if (ifaceRef is TypeSpecification ts && ts.Signature is AsmResolver.DotNet.Signatures.GenericInstanceTypeSignature gi)
        {
            ITypeDefOrRef? gen = gi.GenericType;
            if (gen is TypeDefinition gtd) { return gtd; }
            if (gen is TypeReference gtr && _cacheRef is not null)
            {
                string ns = gtr.Namespace?.Value ?? string.Empty;
                string nm = gtr.Name?.Value ?? string.Empty;
                return _cacheRef.Find(ns + "." + nm);
            }
        }
        if (ifaceRef is TypeReference tr && _cacheRef is not null)
        {
            string ns = tr.Namespace?.Value ?? string.Empty;
            string nm = tr.Name?.Value ?? string.Empty;
            return _cacheRef.Find(ns + "." + nm);
        }
        return null;
    }

    /// <summary>Mirrors C++ <c>get_vmethod_name</c>.</summary>
    public static string GetVMethodName(TypeDefinition type, MethodDefinition method)
    {
        // Index of method in the type's method list
        int index = 0;
        foreach (MethodDefinition m in type.Methods)
        {
            if (m == method) { break; }
            index++;
        }
        return (method.Name?.Value ?? string.Empty) + "_" + index.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>Mirrors C++ <c>write_abi_parameter_types_pointer</c>.</summary>
    public static void WriteAbiParameterTypesPointer(TypeWriter w, MethodSig sig)
    {
        WriteAbiParameterTypesPointer(w, sig, includeParamNames: false);
    }

    /// <summary>
    /// Writes the ABI parameter types for a vtable function pointer signature, optionally
    /// including parameter names (for method declarations vs. function pointer type lists).
    /// </summary>
    public static void WriteAbiParameterTypesPointer(TypeWriter w, MethodSig sig, bool includeParamNames)
    {
        // void* thisPtr, then each param's ABI type, then return type pointer
        w.Write("void*");
        if (includeParamNames) { w.Write(" thisPtr"); }
        for (int i = 0; i < sig.Params.Count; i++)
        {
            w.Write(", ");
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (p.Type is AsmResolver.DotNet.Signatures.SzArrayTypeSignature sz)
            {
                // length pointer + value pointer
                bool isRefElem = IsString(sz.BaseType) || IsRuntimeClassOrInterface(sz.BaseType) || IsObject(sz.BaseType) || IsGenericInstance(sz.BaseType);
                if (includeParamNames)
                {
                    w.Write("uint ");
                    w.Write("__");
                    w.Write(p.Parameter.Name ?? "param");
                    w.Write("Length, ");
                    if (isRefElem)
                    {
                        w.Write("void* ");
                    }
                    else
                    {
                        WriteAbiType(w, TypeSemanticsFactory.Get(sz.BaseType));
                        w.Write("* ");
                    }
                    Helpers.WriteEscapedIdentifier(w, p.Parameter.Name ?? "param");
                }
                else
                {
                    w.Write("uint, ");
                    if (isRefElem)
                    {
                        w.Write("void*");
                    }
                    else
                    {
                        WriteAbiType(w, TypeSemanticsFactory.Get(sz.BaseType));
                        w.Write("*");
                    }
                }
            }
            else if (p.Type is AsmResolver.DotNet.Signatures.ByReferenceTypeSignature br)
            {
                WriteAbiType(w, TypeSemanticsFactory.Get(br.BaseType));
                w.Write("*");
                if (includeParamNames)
                {
                    w.Write(" ");
                    Helpers.WriteEscapedIdentifier(w, p.Parameter.Name ?? "param");
                }
            }
            else
            {
                WriteAbiType(w, TypeSemanticsFactory.Get(p.Type));
                if (cat is ParamCategory.Out or ParamCategory.Ref) { w.Write("*"); }
                if (includeParamNames)
                {
                    w.Write(" ");
                    Helpers.WriteEscapedIdentifier(w, p.Parameter.Name ?? "param");
                }
            }
        }
        // Return parameter
        if (sig.ReturnType is not null)
        {
            w.Write(", ");
            string retName = GetReturnParamName(sig);
            string retSizeName = GetReturnSizeParamName(sig);
            // Special handling for SzArray return types: WinRT projects them as a (uint*, T**) pair.
            if (sig.ReturnType is AsmResolver.DotNet.Signatures.SzArrayTypeSignature retSz)
            {
                if (includeParamNames)
                {
                    w.Write("uint* ");
                    w.Write(retSizeName);
                    w.Write(", ");
                    WriteAbiType(w, TypeSemanticsFactory.Get(retSz.BaseType));
                    w.Write("** ");
                    w.Write(retName);
                }
                else
                {
                    w.Write("uint*, ");
                    WriteAbiType(w, TypeSemanticsFactory.Get(retSz.BaseType));
                    w.Write("**");
                }
            }
            else
            {
                WriteAbiType(w, TypeSemanticsFactory.Get(sig.ReturnType));
                w.Write("*");
                if (includeParamNames) { w.Write(' '); w.Write(retName); }
            }
        }
    }

    /// <summary>Returns the metadata-derived name for the return parameter (or '__retval' fallback).</summary>
    internal static string GetReturnParamName(MethodSig sig)
    {
        string? n = sig.ReturnParam?.Name?.Value;
        if (string.IsNullOrEmpty(n)) { return "__retval"; }
        return Helpers.IsKeyword(n) ? "@" + n : n;
    }

    /// <summary>Returns '__&lt;returnName&gt;Size' (matches C++ '__%Size' convention) or '__retvalLength' fallback.</summary>
    internal static string GetReturnSizeParamName(MethodSig sig)
    {
        string? n = sig.ReturnParam?.Name?.Value;
        if (string.IsNullOrEmpty(n)) { return "__retvalLength"; }
        return "__" + n + "Size";
    }

    /// <summary>Mirrors C++ <c>write_interface_vftbl</c>.</summary>
    public static void WriteInterfaceVftbl(TypeWriter w, TypeDefinition type)
    {
        if (!EmitImplType(w, type)) { return; }
        if (type.GenericParameters.Count > 0) { return; }
        string name = type.Name?.Value ?? string.Empty;
        string nameStripped = Helpers.StripBackticks(name);

        w.Write("\n[StructLayout(LayoutKind.Sequential)]\n");
        w.Write("internal unsafe struct ");
        w.Write(nameStripped);
        w.Write("Vftbl\n{\n");
        w.Write("public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, int> QueryInterface;\n");
        w.Write("public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;\n");
        w.Write("public delegate* unmanaged[MemberFunction]<void*, uint> Release;\n");
        w.Write("public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, int> GetIids;\n");
        w.Write("public delegate* unmanaged[MemberFunction]<void*, void**, int> GetRuntimeClassName;\n");
        w.Write("public delegate* unmanaged[MemberFunction]<void*, int*, int> GetTrustLevel;\n");

        foreach (MethodDefinition method in type.Methods)
        {
            string vm = GetVMethodName(type, method);
            MethodSig sig = new(method);
            w.Write("public delegate* unmanaged[MemberFunction]<");
            WriteAbiParameterTypesPointer(w, sig);
            w.Write(", int> ");
            w.Write(vm);
            w.Write(";\n");
        }
        w.Write("}\n");
    }

    /// <summary>Mirrors C++ <c>write_interface_impl</c> (simplified).</summary>
    public static void WriteInterfaceImpl(TypeWriter w, TypeDefinition type)
    {
        if (!EmitImplType(w, type)) { return; }
        if (type.GenericParameters.Count > 0) { return; }
        string name = type.Name?.Value ?? string.Empty;
        string nameStripped = Helpers.StripBackticks(name);

        w.Write("\npublic static unsafe class ");
        w.Write(nameStripped);
        w.Write("Impl\n{\n");
        w.Write("[FixedAddressValueType]\n");
        w.Write("private static readonly ");
        w.Write(nameStripped);
        w.Write("Vftbl Vftbl;\n\n");

        w.Write("static ");
        w.Write(nameStripped);
        w.Write("Impl()\n{\n");
        w.Write("    *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;\n");
        foreach (MethodDefinition method in type.Methods)
        {
            string vm = GetVMethodName(type, method);
            w.Write("    Vftbl.");
            w.Write(vm);
            w.Write(" = &Do_Abi_");
            w.Write(vm);
            w.Write(";\n");
        }
        w.Write("}\n\n");

        w.Write("public static ref readonly Guid IID\n{\n    [MethodImpl(MethodImplOptions.AggressiveInlining)]\n    get => ref ");
        WriteIidGuidReference(w, type);
        w.Write(";\n}\n\n");

        w.Write("public static nint Vtable\n{\n    [MethodImpl(MethodImplOptions.AggressiveInlining)]\n    get => (nint)Unsafe.AsPointer(in Vftbl);\n}\n\n");

        // Do_Abi_* implementations: emit real bodies for simple primitive cases,
        // throw null! for everything else (deferred — needs full per-parameter marshalling).
        string ifaceFullName = w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteTypedefName(w, type, TypedefNameType.Projected, true)));
        if (!ifaceFullName.StartsWith("global::", System.StringComparison.Ordinal)) { ifaceFullName = "global::" + ifaceFullName; }

        // Build a map of event add/remove methods to their event so we can emit the table field
        // and the proper Do_Abi_add_*/Do_Abi_remove_* bodies (mirrors C++ write_event_abi_invoke).
        System.Collections.Generic.Dictionary<MethodDefinition, EventDefinition>? eventMap = BuildEventMethodMap(type);

        foreach (MethodDefinition method in type.Methods)
        {
            string vm = GetVMethodName(type, method);
            MethodSig sig = new(method);
            string mname = method.Name?.Value ?? string.Empty;

            // If this method is an event add accessor, emit the per-event ConditionalWeakTable
            // before the Do_Abi method (mirrors C++ ordering).
            if (eventMap is not null && eventMap.TryGetValue(method, out EventDefinition? evt) && evt.AddMethod == method)
            {
                EmitEventTableField(w, evt, type);
            }

            w.Write("[UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]\n");
            w.Write("private static int Do_Abi_");
            w.Write(vm);
            w.Write("(");
            WriteAbiParameterTypesPointer(w, sig, includeParamNames: true);
            w.Write(")");

            if (eventMap is not null && eventMap.TryGetValue(method, out EventDefinition? evt2))
            {
                if (evt2.AddMethod == method)
                {
                    EmitDoAbiAddEvent(w, evt2, sig, ifaceFullName);
                }
                else
                {
                    EmitDoAbiRemoveEvent(w, evt2, sig, ifaceFullName);
                }
            }
            else
            {
                EmitDoAbiBodyIfSimple(w, sig, ifaceFullName, mname);
            }
        }
        w.Write("}\n");
    }

    /// <summary>Build a method-to-event map for add/remove accessors of a type.</summary>
    private static System.Collections.Generic.Dictionary<MethodDefinition, EventDefinition>? BuildEventMethodMap(TypeDefinition type)
    {
        if (type.Events.Count == 0) { return null; }
        System.Collections.Generic.Dictionary<MethodDefinition, EventDefinition> map = new();
        foreach (EventDefinition evt in type.Events)
        {
            if (evt.AddMethod is MethodDefinition add) { map[add] = evt; }
            if (evt.RemoveMethod is MethodDefinition rem) { map[rem] = evt; }
        }
        return map;
    }

    /// <summary>
    /// Emits the per-event <c>ConditionalWeakTable&lt;TInterface, EventRegistrationTokenTable&lt;THandler&gt;&gt;</c>
    /// backing field property. Mirrors the table emission in C++ <c>write_event_abi_invoke</c>.
    /// </summary>
    private static void EmitEventTableField(TypeWriter w, EventDefinition evt, TypeDefinition iface)
    {
        string evName = evt.Name?.Value ?? "Event";
        string ifaceProjected = w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteTypedefName(w, iface, TypedefNameType.Projected, true)));
        if (!ifaceProjected.StartsWith("global::", System.StringComparison.Ordinal)) { ifaceProjected = "global::" + ifaceProjected; }
        string evtType = w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteEventType(w, evt)));

        w.Write("\nprivate static ConditionalWeakTable<");
        w.Write(ifaceProjected);
        w.Write(", EventRegistrationTokenTable<");
        w.Write(evtType);
        w.Write(">> _");
        w.Write(evName);
        w.Write("\n{\n");
        w.Write("    [MethodImpl(MethodImplOptions.AggressiveInlining)]\n");
        w.Write("    get\n    {\n");
        w.Write("        [MethodImpl(MethodImplOptions.NoInlining)]\n");
        w.Write("        static ConditionalWeakTable<");
        w.Write(ifaceProjected);
        w.Write(", EventRegistrationTokenTable<");
        w.Write(evtType);
        w.Write(">> MakeTable()\n        {\n");
        w.Write("            _ = global::System.Threading.Interlocked.CompareExchange(ref field, [], null);\n\n");
        w.Write("            return global::System.Threading.Volatile.Read(in field);\n");
        w.Write("        }\n\n");
        w.Write("        return global::System.Threading.Volatile.Read(in field) ?? MakeTable();\n    }\n}\n");
    }

    /// <summary>
    /// Emits the body of the <c>Do_Abi_add_&lt;EventName&gt;_N</c> method. Mirrors the corresponding
    /// branch in C++ <c>write_event_abi_invoke</c>.
    /// </summary>
    private static void EmitDoAbiAddEvent(TypeWriter w, EventDefinition evt, MethodSig sig, string ifaceFullName)
    {
        string evName = evt.Name?.Value ?? "Event";
        // Handler is the (last) input parameter of the add method. The emitted parameter name in the
        // signature comes from WriteAbiParameterTypesPointer which uses the metadata name verbatim.
        string handlerRawName = sig.Params.Count > 0 ? (sig.Params[^1].Parameter.Name ?? "handler") : "handler";
        string handlerRef = Helpers.IsKeyword(handlerRawName) ? "@" + handlerRawName : handlerRawName;

        // The cookie/token return parameter takes the metadata return param name (matches truth).
        string cookieName = GetReturnParamName(sig);

        AsmResolver.DotNet.Signatures.TypeSignature evtTypeSig = evt.EventType!.ToTypeSignature(false);
        bool isGeneric = evtTypeSig is AsmResolver.DotNet.Signatures.GenericInstanceTypeSignature;

        w.Write("\n{\n");
        w.Write("    *");
        w.Write(cookieName);
        w.Write(" = default;\n");
        w.Write("    try\n    {\n");
        w.Write("        var __this = ComInterfaceDispatch.GetInstance<");
        w.Write(ifaceFullName);
        w.Write(">((ComInterfaceDispatch*)thisPtr);\n");

        if (isGeneric)
        {
            string interopTypeName = EncodeInteropTypeName(evtTypeSig, TypedefNameType.ABI) + ", WinRT.Interop";
            string projectedTypeName = w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteProjectedSignature(w, evtTypeSig, false)));
            w.Write("        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"ConvertToManaged\")]\n");
            w.Write("        static extern ");
            w.Write(projectedTypeName);
            w.Write(" ConvertToManaged([UnsafeAccessorType(\"");
            w.Write(interopTypeName);
            w.Write("\")] object _, void* value);\n");
            w.Write("        var __handler = ConvertToManaged(null, ");
            w.Write(handlerRef);
            w.Write(");\n");
        }
        else
        {
            w.Write("        var __handler = ");
            WriteTypeName(w, TypeSemanticsFactory.Get(evtTypeSig), TypedefNameType.ABI, false);
            w.Write("Marshaller.ConvertToManaged(");
            w.Write(handlerRef);
            w.Write(");\n");
        }

        w.Write("        *");
        w.Write(cookieName);
        w.Write(" = _");
        w.Write(evName);
        w.Write(".GetOrCreateValue(__this).AddEventHandler(__handler);\n");
        w.Write("        __this.");
        w.Write(evName);
        w.Write(" += __handler;\n");
        w.Write("        return 0;\n    }\n");
        w.Write("    catch (Exception __exception__)\n    {\n");
        w.Write("        return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(__exception__);\n    }\n}\n");
    }

    /// <summary>
    /// Emits the body of the <c>Do_Abi_remove_&lt;EventName&gt;_N</c> method. Mirrors the corresponding
    /// branch in C++ <c>write_event_abi_invoke</c>.
    /// </summary>
    private static void EmitDoAbiRemoveEvent(TypeWriter w, EventDefinition evt, MethodSig sig, string ifaceFullName)
    {
        string evName = evt.Name?.Value ?? "Event";
        string tokenRawName = sig.Params.Count > 0 ? (sig.Params[^1].Parameter.Name ?? "token") : "token";
        string tokenRef = Helpers.IsKeyword(tokenRawName) ? "@" + tokenRawName : tokenRawName;

        w.Write("\n{\n");
        w.Write("    try\n    {\n");
        w.Write("        var __this = ComInterfaceDispatch.GetInstance<");
        w.Write(ifaceFullName);
        w.Write(">((ComInterfaceDispatch*)thisPtr);\n");
        w.Write("        if(__this is not null && _");
        w.Write(evName);
        w.Write(".TryGetValue(__this, out var __table) && __table.RemoveEventHandler(");
        w.Write(tokenRef);
        w.Write(", out var __handler))\n        {\n");
        w.Write("            __this.");
        w.Write(evName);
        w.Write(" -= __handler;\n");
        w.Write("        }\n");
        w.Write("        return 0;\n    }\n");
        w.Write("    catch (Exception __exception__)\n    {\n");
        w.Write("        return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(__exception__);\n    }\n}\n");
    }

    /// <summary>
    /// Emits a real Do_Abi (CCW) body for the cases we can handle, else throw null!.
    /// </summary>
    private static void EmitDoAbiBodyIfSimple(TypeWriter w, MethodSig sig, string ifaceFullName, string methodName)
    {
        AsmResolver.DotNet.Signatures.TypeSignature? rt = sig.ReturnType;

        bool allParamsSimple = true;
        bool hasStringParams = false;
        foreach (ParamInfo p in sig.Params)
        {
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat == ParamCategory.Out || cat == ParamCategory.Ref)
            {
                // Allow Out/Ref for blittable primitive/enum/blittable-struct types,
                // strings, runtime classes, and objects.
                AsmResolver.DotNet.Signatures.TypeSignature underlying = StripByRefAndCustomModifiers(p.Type);
                if (IsHResultException(underlying)) { allParamsSimple = false; break; }
                if (IsBlittablePrimitive(underlying)) { continue; }
                if (IsAnyStruct(underlying)) { continue; }
                if (IsString(underlying)) { continue; }
                if (IsRuntimeClassOrInterface(underlying)) { continue; }
                if (IsObject(underlying)) { continue; }
                allParamsSimple = false;
                break;
            }
            if (cat == ParamCategory.PassArray || cat == ParamCategory.FillArray)
            {
                // Allow blittable primitive arrays, almost-blittable structs, strings, runtime classes, objects.
                if (p.Type is AsmResolver.DotNet.Signatures.SzArrayTypeSignature sz)
                {
                    if (IsBlittablePrimitive(sz.BaseType)) { continue; }
                    if (IsAnyStruct(sz.BaseType)) { continue; }
                    if (IsString(sz.BaseType)) { continue; }
                    if (IsRuntimeClassOrInterface(sz.BaseType)) { continue; }
                    if (IsObject(sz.BaseType)) { continue; }
                }
                allParamsSimple = false;
                break;
            }
            if (cat != ParamCategory.In) { allParamsSimple = false; break; }
            if (IsHResultException(p.Type)) { allParamsSimple = false; break; }
            if (IsBlittablePrimitive(p.Type)) { continue; }
            if (IsAnyStruct(p.Type)) { continue; }
            if (IsString(p.Type)) { hasStringParams = true; continue; }
            if (IsRuntimeClassOrInterface(p.Type)) { continue; }
            if (IsObject(p.Type)) { continue; }
            if (IsGenericInstance(p.Type)) { continue; }
            allParamsSimple = false;
            break;
        }
        bool returnIsReceiveArrayDoAbi = rt is AsmResolver.DotNet.Signatures.SzArrayTypeSignature retSzAbi
            && (IsBlittablePrimitive(retSzAbi.BaseType) || IsAnyStruct(retSzAbi.BaseType));
        bool returnIsHResultExceptionDoAbi = rt is not null && IsHResultException(rt);
        bool returnSimple = rt is null
            || (IsBlittablePrimitive(rt) && !IsHResultException(rt))
            || (IsAnyStruct(rt) && !IsHResultException(rt))
            || IsString(rt)
            || IsRuntimeClassOrInterface(rt)
            || IsObject(rt)
            || IsGenericInstance(rt)
            || returnIsReceiveArrayDoAbi
            || returnIsHResultExceptionDoAbi;
        bool returnIsString = rt is not null && IsString(rt);
        bool returnIsRefType = rt is not null && (IsRuntimeClassOrInterface(rt) || IsObject(rt) || IsGenericInstance(rt));
        bool returnIsGenericInstance = rt is not null && IsGenericInstance(rt);
        bool returnIsBlittableStruct = rt is not null && IsAnyStruct(rt);

        bool isGetter = methodName.StartsWith("get_", System.StringComparison.Ordinal);
        bool isSetter = methodName.StartsWith("put_", System.StringComparison.Ordinal);
        bool isAddEvent = methodName.StartsWith("add_", System.StringComparison.Ordinal);
        bool isRemoveEvent = methodName.StartsWith("remove_", System.StringComparison.Ordinal);

        if (isAddEvent || isRemoveEvent || !allParamsSimple || !returnSimple)
        {
            w.Write(" => throw null!;\n\n");
            return;
        }

        w.Write("\n{\n");
        string retParamName = GetReturnParamName(sig);
        string retSizeParamName = GetReturnSizeParamName(sig);
        if (rt is not null)
        {
            if (returnIsReceiveArrayDoAbi)
            {
                w.Write("    *");
                w.Write(retParamName);
                w.Write(" = default;\n");
                w.Write("    *");
                w.Write(retSizeParamName);
                w.Write(" = default;\n");
            }
            else
            {
                w.Write("    *");
                w.Write(retParamName);
                w.Write(" = default;\n");
            }
        }
        // For each out parameter, clear the destination and declare a local.
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.Out && cat != ParamCategory.Ref) { continue; }
            string raw = p.Parameter.Name ?? "param";
            string ptr = Helpers.IsKeyword(raw) ? "@" + raw : raw;
            w.Write("    *");
            w.Write(ptr);
            w.Write(" = default;\n");
        }
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.Out && cat != ParamCategory.Ref) { continue; }
            string raw = p.Parameter.Name ?? "param";
            // Use the projected (non-ABI) type for the local variable.
            // Strip ByRef and CustomModifier wrappers to get the underlying base type.
            AsmResolver.DotNet.Signatures.TypeSignature underlying = StripByRefAndCustomModifiers(p.Type);
            string projected = w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteProjectedSignature(w, underlying, false)));
            w.Write("    ");
            w.Write(projected);
            w.Write(" __");
            w.Write(raw);
            w.Write(" = default;\n");
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
            string ptr = Helpers.IsKeyword(raw) ? "@" + raw : raw;
            string elementProjected = w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteProjectionType(w, TypeSemanticsFactory.Get(sz.BaseType))));
            bool isBlittableElem = IsBlittablePrimitive(sz.BaseType) || IsAnyStruct(sz.BaseType);
            if (isBlittableElem)
            {
                w.Write("    ");
                w.Write(cat == ParamCategory.PassArray ? "global::System.ReadOnlySpan<" : "global::System.Span<");
                w.Write(elementProjected);
                w.Write("> __");
                w.Write(raw);
                w.Write(" = new(");
                w.Write(ptr);
                w.Write(", (int)__");
                w.Write(raw);
                w.Write("Length);\n");
            }
            else
            {
                // Non-blittable element: InlineArray16<T> + ArrayPool<T> with size from ABI.
                w.Write("\n    Unsafe.SkipInit(out InlineArray16<");
                w.Write(elementProjected);
                w.Write("> __");
                w.Write(raw);
                w.Write("_inlineArray);\n");
                w.Write("    ");
                w.Write(elementProjected);
                w.Write("[] __");
                w.Write(raw);
                w.Write("_arrayFromPool = null;\n");
                w.Write("    Span<");
                w.Write(elementProjected);
                w.Write("> __");
                w.Write(raw);
                w.Write(" = __");
                w.Write(raw);
                w.Write("Length <= 16\n        ? __");
                w.Write(raw);
                w.Write("_inlineArray[..(int)__");
                w.Write(raw);
                w.Write("Length]\n        : (__");
                w.Write(raw);
                w.Write("_arrayFromPool = global::System.Buffers.ArrayPool<");
                w.Write(elementProjected);
                w.Write(">.Shared.Rent((int)__");
                w.Write(raw);
                w.Write("Length));\n");
            }
        }
        w.Write("    try\n    {\n");

        // For non-blittable PassArray params, emit CopyToManaged_<name> via UnsafeAccessor.
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.PassArray && cat != ParamCategory.FillArray) { continue; }
            if (p.Type is not AsmResolver.DotNet.Signatures.SzArrayTypeSignature szArr) { continue; }
            if (IsBlittablePrimitive(szArr.BaseType) || IsAnyStruct(szArr.BaseType)) { continue; }
            string raw = p.Parameter.Name ?? "param";
            string ptr = Helpers.IsKeyword(raw) ? "@" + raw : raw;
            string elementProjected = w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteProjectionType(w, TypeSemanticsFactory.Get(szArr.BaseType))));
            string elementInteropArg = EncodeInteropTypeName(szArr.BaseType, TypedefNameType.Projected);
            w.Write("        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"CopyToManaged\")]\n");
            w.Write("        static extern void CopyToManaged_");
            w.Write(raw);
            w.Write("([UnsafeAccessorType(\"");
            w.Write(GetArrayMarshallerInteropPath(w, szArr.BaseType, elementInteropArg));
            w.Write("\")] object _, uint length, void** data, Span<");
            w.Write(elementProjected);
            w.Write("> span);\n");
            w.Write("        CopyToManaged_");
            w.Write(raw);
            w.Write("(null, __");
            w.Write(raw);
            w.Write("Length, (void**)");
            w.Write(ptr);
            w.Write(", __");
            w.Write(raw);
            w.Write(");\n");
        }

        // For generic instance ABI input parameters, emit local UnsafeAccessor delegates and locals
        // first so the call site can reference them.
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            if (IsGenericInstance(p.Type))
            {
                string rawName = p.Parameter.Name ?? "param";
                string callName = Helpers.IsKeyword(rawName) ? "@" + rawName : rawName;
                string interopTypeName = EncodeInteropTypeName(p.Type, TypedefNameType.ABI) + ", WinRT.Interop";
                string projectedTypeName = w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteProjectedSignature(w, p.Type, false)));
                w.Write("        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"ConvertToManaged\")]\n");
                w.Write("        static extern ");
                w.Write(projectedTypeName);
                w.Write(" ConvertToManaged_arg_");
                w.Write(rawName);
                w.Write("([UnsafeAccessorType(\"");
                w.Write(interopTypeName);
                w.Write("\")] object _, void* value);\n");
                w.Write("        var __arg_");
                w.Write(rawName);
                w.Write(" = ConvertToManaged_arg_");
                w.Write(rawName);
                w.Write("(null, ");
                w.Write(callName);
                w.Write(");\n");
            }
        }

        if (returnIsString)
        {
            w.Write("        string __result = ");
        }
        else if (returnIsRefType)
        {
            string projected = w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteProjectedSignature(w, rt!, false)));
            w.Write("        ");
            w.Write(projected);
            w.Write(" __result = ");
        }
        else if (returnIsReceiveArrayDoAbi)
        {
            // For T[] return: declare the projected array local.
            string projected = w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteProjectedSignature(w, rt!, false)));
            w.Write("        ");
            w.Write(projected);
            w.Write(" __result = ");
        }
        else if (rt is not null)
        {
            string projected = w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteProjectedSignature(w, rt, false)));
            w.Write("        ");
            w.Write(projected);
            w.Write(" __result = ");
        }
        else
        {
            w.Write("        ");
        }

        if (isGetter)
        {
            string propName = methodName.Substring(4);
            w.Write("ComInterfaceDispatch.GetInstance<");
            w.Write(ifaceFullName);
            w.Write(">((ComInterfaceDispatch*)thisPtr).");
            w.Write(propName);
            w.Write(";\n");
        }
        else if (isSetter)
        {
            string propName = methodName.Substring(4);
            w.Write("ComInterfaceDispatch.GetInstance<");
            w.Write(ifaceFullName);
            w.Write(">((ComInterfaceDispatch*)thisPtr).");
            w.Write(propName);
            w.Write(" = ");
            EmitDoAbiParamArgConversion(w, sig.Params[0]);
            w.Write(";\n");
        }
        else
        {
            w.Write("ComInterfaceDispatch.GetInstance<");
            w.Write(ifaceFullName);
            w.Write(">((ComInterfaceDispatch*)thisPtr).");
            w.Write(methodName);
            w.Write("(");
            for (int i = 0; i < sig.Params.Count; i++)
            {
                if (i > 0) { w.Write(", "); }
                ParamInfo p = sig.Params[i];
                ParamCategory cat = ParamHelpers.GetParamCategory(p);
                if (cat == ParamCategory.Out)
                {
                    string raw = p.Parameter.Name ?? "param";
                    w.Write("out __");
                    w.Write(raw);
                }
                else if (cat == ParamCategory.Ref)
                {
                    string raw = p.Parameter.Name ?? "param";
                    w.Write("ref __");
                    w.Write(raw);
                }
                else if (cat == ParamCategory.PassArray || cat == ParamCategory.FillArray)
                {
                    string raw = p.Parameter.Name ?? "param";
                    w.Write("__");
                    w.Write(raw);
                }
                else
                {
                    EmitDoAbiParamArgConversion(w, p);
                }
            }
            w.Write(");\n");
        }
        // After call: write back out/ref params.
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.Out && cat != ParamCategory.Ref) { continue; }
            string raw = p.Parameter.Name ?? "param";
            string ptr = Helpers.IsKeyword(raw) ? "@" + raw : raw;
            AsmResolver.DotNet.Signatures.TypeSignature underlying = StripByRefAndCustomModifiers(p.Type);
            w.Write("        *");
            w.Write(ptr);
            w.Write(" = ");
            // String: HStringMarshaller.ConvertToUnmanaged
            if (IsString(underlying))
            {
                w.Write("HStringMarshaller.ConvertToUnmanaged(__");
                w.Write(raw);
                w.Write(")");
            }
            // Object/runtime class: <Marshaller>.ConvertToUnmanaged(...).DetachThisPtrUnsafe()
            else if (IsObject(underlying))
            {
                w.Write("WindowsRuntimeObjectMarshaller.ConvertToUnmanaged(__");
                w.Write(raw);
                w.Write(").DetachThisPtrUnsafe()");
            }
            else if (IsRuntimeClassOrInterface(underlying))
            {
                w.Write(GetMarshallerFullName(w, underlying));
                w.Write(".ConvertToUnmanaged(__");
                w.Write(raw);
                w.Write(").DetachThisPtrUnsafe()");
            }
            // For enums, function pointer signature uses the projected enum type, no cast needed.
            // For bool, cast to byte. For char, cast to ushort.
            else if (IsEnumType(underlying))
            {
                w.Write("__");
                w.Write(raw);
            }
            else if (underlying is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlibBool &&
                     corlibBool.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Boolean)
            {
                w.Write("__");
                w.Write(raw);
            }
            else if (underlying is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlibChar &&
                     corlibChar.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Char)
            {
                w.Write("__");
                w.Write(raw);
            }
            else
            {
                w.Write("__");
                w.Write(raw);
            }
            w.Write(";\n");
        }
        if (rt is not null)
        {
            if (returnIsHResultExceptionDoAbi)
            {
                w.Write("        *");
                w.Write(retParamName);
                w.Write(" = global::ABI.System.ExceptionMarshaller.ConvertToUnmanaged(__result);\n");
            }
            else if (returnIsString)
            {
                w.Write("        *");
                w.Write(retParamName);
                w.Write(" = HStringMarshaller.ConvertToUnmanaged(__result);\n");
            }
            else if (returnIsRefType)
            {
                if (returnIsGenericInstance)
                {
                    // Generic instance return: emit local UnsafeAccessor delegate to ConvertToUnmanaged + .DetachThisPtrUnsafe()
                    string interopTypeName = EncodeInteropTypeName(rt!, TypedefNameType.ABI) + ", WinRT.Interop";
                    string projectedTypeName = w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteProjectedSignature(w, rt!, false)));
                    w.Write("        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"ConvertToUnmanaged\")]\n");
                    w.Write("        static extern WindowsRuntimeObjectReferenceValue ConvertToUnmanaged_result([UnsafeAccessorType(\"");
                    w.Write(interopTypeName);
                    w.Write("\")] object _, ");
                    w.Write(projectedTypeName);
                    w.Write(" value);\n");
                    w.Write("        *");
                    w.Write(retParamName);
                    w.Write(" = ConvertToUnmanaged_result(null, __result).DetachThisPtrUnsafe();\n");
                }
                else
                {
                    w.Write("        *");
                    w.Write(retParamName);
                    w.Write(" = ");
                    EmitMarshallerConvertToUnmanaged(w, rt!, "__result");
                    w.Write(".DetachThisPtrUnsafe();\n");
                }
            }
            else if (returnIsReceiveArrayDoAbi)
            {
                AsmResolver.DotNet.Signatures.SzArrayTypeSignature retSz = (AsmResolver.DotNet.Signatures.SzArrayTypeSignature)rt!;
                string elementProjected = w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteProjectionType(w, TypeSemanticsFactory.Get(retSz.BaseType))));
                string elementAbi = GetAbiPrimitiveType(retSz.BaseType);
                string elementInteropArg = EncodeInteropTypeName(retSz.BaseType, TypedefNameType.Projected);
                w.Write("        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"ConvertToUnmanaged\")]\n");
                w.Write("        static extern void ConvertToUnmanaged_result([UnsafeAccessorType(\"ABI.System.<");
                w.Write(elementInteropArg);
                w.Write(">ArrayMarshaller, WinRT.Interop\")] object _, global::System.ReadOnlySpan<");
                w.Write(elementProjected);
                w.Write("> span, out uint length, out ");
                w.Write(elementAbi);
                w.Write("* data);\n");
                w.Write("        ConvertToUnmanaged_result(null, __result, out *");
                w.Write(retSizeParamName);
                w.Write(", out *");
                w.Write(retParamName);
                w.Write(");\n");
            }
            else if (IsMappedAbiValueType(rt!))
            {
                // Mapped value type return (DateTime/TimeSpan): convert via marshaller.
                w.Write("        *");
                w.Write(retParamName);
                w.Write(" = ");
                w.Write(GetMappedMarshallerName(rt!));
                w.Write(".ConvertToUnmanaged(__result);\n");
            }
            else if (returnIsBlittableStruct)
            {
                w.Write("        *");
                w.Write(retParamName);
                w.Write(" = __result;\n");
            }
            else
            {
                string abiType = GetAbiPrimitiveType(rt);
                w.Write("        *");
                w.Write(retParamName);
                w.Write(" = ");
                if (rt is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlib &&
                    corlib.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Boolean)
                {
                    w.Write("__result;\n");
                }
                else if (rt is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlib2 &&
                         corlib2.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Char)
                {
                    w.Write("__result;\n");
                }
                else if (IsEnumType(rt))
                {
                    // Enum: function pointer signature uses the projected enum type, no cast needed.
                    w.Write("__result;\n");
                }
                else
                {
                    w.Write("__result;\n");
                }
            }
        }
        w.Write("        return 0;\n    }\n");
        w.Write("    catch (Exception __exception__)\n    {\n");
        w.Write("        return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(__exception__);\n    }\n");

        // For non-blittable PassArray params, emit finally block with ArrayPool<T>.Shared.Return.
        bool hasNonBlittableArrayDoAbi = false;
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.PassArray && cat != ParamCategory.FillArray) { continue; }
            if (p.Type is not AsmResolver.DotNet.Signatures.SzArrayTypeSignature szArr) { continue; }
            if (IsBlittablePrimitive(szArr.BaseType) || IsAnyStruct(szArr.BaseType)) { continue; }
            hasNonBlittableArrayDoAbi = true;
            break;
        }
        if (hasNonBlittableArrayDoAbi)
        {
            w.Write("    finally\n    {\n");
            for (int i = 0; i < sig.Params.Count; i++)
            {
                ParamInfo p = sig.Params[i];
                ParamCategory cat = ParamHelpers.GetParamCategory(p);
                if (cat != ParamCategory.PassArray && cat != ParamCategory.FillArray) { continue; }
                if (p.Type is not AsmResolver.DotNet.Signatures.SzArrayTypeSignature szArr) { continue; }
                if (IsBlittablePrimitive(szArr.BaseType) || IsAnyStruct(szArr.BaseType)) { continue; }
                string raw = p.Parameter.Name ?? "param";
                string elementProjected = w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteProjectionType(w, TypeSemanticsFactory.Get(szArr.BaseType))));
                w.Write("\n        if (__");
                w.Write(raw);
                w.Write("_arrayFromPool is not null)\n        {\n");
                w.Write("            global::System.Buffers.ArrayPool<");
                w.Write(elementProjected);
                w.Write(">.Shared.Return(__");
                w.Write(raw);
                w.Write("_arrayFromPool);\n        }\n");
            }
            w.Write("    }\n");
        }

        w.Write("}\n\n");
        _ = hasStringParams;
    }

    /// <summary>Converts an ABI parameter to its projected (managed) form for the Do_Abi call.</summary>
    private static void EmitDoAbiParamArgConversion(TypeWriter w, ParamInfo p)
    {
        string rawName = p.Parameter.Name ?? "param";
        string pname = Helpers.IsKeyword(rawName) ? "@" + rawName : rawName;
        if (p.Type is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlib &&
            corlib.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Boolean)
        {
            w.Write(pname);
        }
        else if (p.Type is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlib2 &&
                 corlib2.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Char)
        {
            w.Write(pname);
        }
        else if (p.Type is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlibStr &&
                 corlibStr.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.String)
        {
            w.Write("HStringMarshaller.ConvertToManaged(");
            w.Write(pname);
            w.Write(")");
        }
        else if (IsGenericInstance(p.Type))
        {
            // Generic instance ABI parameter: caller already declared a local UnsafeAccessor +
            // local var __arg_<name> that holds the converted value.
            w.Write("__arg_");
            w.Write(rawName);
        }
        else if (IsRuntimeClassOrInterface(p.Type) || IsObject(p.Type))
        {
            EmitMarshallerConvertToManaged(w, p.Type, pname);
        }
        else if (IsMappedAbiValueType(p.Type))
        {
            // Mapped value type input (DateTime/TimeSpan): the parameter is the ABI type;
            // convert to the projected managed type via the marshaller.
            w.Write(GetMappedMarshallerName(p.Type));
            w.Write(".ConvertToManaged(");
            w.Write(pname);
            w.Write(")");
        }
        else if (IsAnyStruct(p.Type))
        {
            // Blittable / almost-blittable struct: pass directly (projected type == ABI type).
            w.Write(pname);
        }
        else if (IsEnumType(p.Type))
        {
            // Enum: param signature is already the projected enum type, no cast needed.
            w.Write(pname);
        }
        else
        {
            w.Write(pname);
        }
    }

    /// <summary>Mirrors C++ <c>write_interface_idic_impl</c>.</summary>
    public static void WriteInterfaceIdicImpl(TypeWriter w, TypeDefinition type)
    {
        if (TypeCategorization.IsExclusiveTo(type) && !w.Settings.IdicExclusiveTo) { return; }
        if (type.GenericParameters.Count > 0) { return; }
        string name = type.Name?.Value ?? string.Empty;
        string nameStripped = Helpers.StripBackticks(name);

        w.Write("\n[DynamicInterfaceCastableImplementation]\n");
        WriteGuidAttribute(w, type);
        w.Write("\n");
        w.Write("file interface ");
        w.Write(nameStripped);
        w.Write(" : ");
        WriteTypedefName(w, type, TypedefNameType.Projected, false);
        WriteTypeParams(w, type);
        w.Write("\n{\n");
        // Emit DIM bodies that dispatch through the static ABI Methods class.
        WriteInterfaceIdicImplMembers(w, type);
        w.Write("\n}\n");
    }

    /// <summary>
    /// Emits explicit-interface DIM (default interface method) implementations for the IDIC
    /// file interface. Mirrors C++ <c>write_interface_members</c>.
    /// </summary>
    private static void WriteInterfaceIdicImplMembers(TypeWriter w, TypeDefinition type)
    {
        // The CCW interface name (the projected interface name with global:: prefix).
        string ccwIfaceName = w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteTypedefName(w, type, TypedefNameType.Projected, true)));
        if (!ccwIfaceName.StartsWith("global::", System.StringComparison.Ordinal)) { ccwIfaceName = "global::" + ccwIfaceName; }
        // The static ABI Methods class name.
        string abiClass = w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteTypedefName(w, type, TypedefNameType.StaticAbiClass, true)));
        if (!abiClass.StartsWith("global::", System.StringComparison.Ordinal)) { abiClass = "global::" + abiClass; }

        foreach (MethodDefinition method in type.Methods)
        {
            if (Helpers.IsSpecial(method)) { continue; }
            MethodSig sig = new(method);
            string mname = method.Name?.Value ?? string.Empty;

            w.Write("\nunsafe ");
            WriteProjectionReturnType(w, sig);
            w.Write(" ");
            w.Write(ccwIfaceName);
            w.Write(".");
            w.Write(mname);
            w.Write("(");
            WriteParameterList(w, sig);
            w.Write(")\n{\n");
            w.Write("    var _obj = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(");
            w.Write(ccwIfaceName);
            w.Write(").TypeHandle);\n    ");
            if (sig.ReturnType is not null) { w.Write("return "); }
            w.Write(abiClass);
            w.Write(".");
            w.Write(mname);
            w.Write("(_obj");
            for (int i = 0; i < sig.Params.Count; i++)
            {
                w.Write(", ");
                WriteParameterNameWithModifier(w, sig.Params[i]);
            }
            w.Write(");\n}\n");
        }

        foreach (PropertyDefinition prop in type.Properties)
        {
            (MethodDefinition? getter, MethodDefinition? setter) = Helpers.GetPropertyMethods(prop);
            string pname = prop.Name?.Value ?? string.Empty;
            string propType = WritePropType(w, prop);

            w.Write("\nunsafe ");
            w.Write(propType);
            w.Write(" ");
            w.Write(ccwIfaceName);
            w.Write(".");
            w.Write(pname);
            w.Write("\n{\n");
            if (getter is not null)
            {
                w.Write("    get\n    {\n");
                w.Write("        var _obj = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(");
                w.Write(ccwIfaceName);
                w.Write(").TypeHandle);\n");
                w.Write("        return ");
                w.Write(abiClass);
                w.Write(".");
                w.Write(pname);
                w.Write("(_obj);\n    }\n");
            }
            if (setter is not null)
            {
                w.Write("    set\n    {\n");
                w.Write("        var _obj = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(");
                w.Write(ccwIfaceName);
                w.Write(").TypeHandle);\n");
                w.Write("        ");
                w.Write(abiClass);
                w.Write(".");
                w.Write(pname);
                w.Write("(_obj, value);\n    }\n");
            }
            w.Write("}\n");
        }

        // Events: emit explicit interface event implementations on the IDIC interface that
        // dispatch through the static ABI Methods class's event accessor (returns an EventSource).
        // Mirrors C++ write_interface_members event handling (calls EventName(thisRef, _obj).Subscribe/Unsubscribe).
        foreach (EventDefinition evt in type.Events)
        {
            string evtName = evt.Name?.Value ?? string.Empty;
            w.Write("\nevent ");
            WriteEventType(w, evt);
            w.Write(" ");
            w.Write(ccwIfaceName);
            w.Write(".");
            w.Write(evtName);
            w.Write("\n{\n");
            // add accessor
            w.Write("    add\n    {\n");
            w.Write("        var _obj = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(");
            w.Write(ccwIfaceName);
            w.Write(").TypeHandle);\n        ");
            w.Write(abiClass);
            w.Write(".");
            w.Write(evtName);
            w.Write("((WindowsRuntimeObject)this, _obj).Subscribe(value);\n    }\n");
            // remove accessor
            w.Write("    remove\n    {\n");
            w.Write("        var _obj = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(");
            w.Write(ccwIfaceName);
            w.Write(").TypeHandle);\n        ");
            w.Write(abiClass);
            w.Write(".");
            w.Write(evtName);
            w.Write("((WindowsRuntimeObject)this, _obj).Unsubscribe(value);\n    }\n");
            w.Write("}\n");
        }
    }

    /// <summary>Mirrors C++ <c>write_interface_marshaller</c>.</summary>
    public static void WriteInterfaceMarshaller(TypeWriter w, TypeDefinition type)
    {
        if (TypeCategorization.IsExclusiveTo(type)) { return; }
        if (type.GenericParameters.Count > 0) { return; }
        string name = type.Name?.Value ?? string.Empty;
        string nameStripped = Helpers.StripBackticks(name);

        w.Write("\n#nullable enable\n");
        w.Write("public static unsafe class ");
        w.Write(nameStripped);
        w.Write("Marshaller\n{\n");
        w.Write("    public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(");
        WriteTypedefName(w, type, TypedefNameType.Projected, false);
        WriteTypeParams(w, type);
        w.Write(" value)\n    {\n");
        w.Write("        return WindowsRuntimeInterfaceMarshaller<");
        WriteTypedefName(w, type, TypedefNameType.Projected, false);
        WriteTypeParams(w, type);
        w.Write(">.ConvertToUnmanaged(value, ");
        WriteIidGuidReference(w, type);
        w.Write(");\n    }\n\n");
        w.Write("    public static ");
        WriteTypedefName(w, type, TypedefNameType.Projected, false);
        WriteTypeParams(w, type);
        w.Write("? ConvertToManaged(void* value)\n    {\n");
        w.Write("        return (");
        WriteTypedefName(w, type, TypedefNameType.Projected, false);
        WriteTypeParams(w, type);
        w.Write("?) WindowsRuntimeObjectMarshaller.ConvertToManaged(value);\n    }\n}\n");
        w.Write("#nullable disable\n");
    }

    /// <summary>Mirrors C++ <c>write_iid_guid</c> for use by ABI helpers.</summary>
    public static void WriteIidGuidReference(TypeWriter w, TypeDefinition type)
    {
        if (type.GenericParameters.Count != 0)
        {
            // Generic interface IID - call the unsafe accessor
            WriteIidGuidPropertyName(w, type);
            w.Write("(null)");
            return;
        }
        string ns = type.Namespace?.Value ?? string.Empty;
        string nm = type.Name?.Value ?? string.Empty;
        if (MappedTypes.Get(ns, nm) is { } m && m.MappedName == "IStringable")
        {
            w.Write("global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IStringable");
            return;
        }
        w.Write("global::ABI.InterfaceIIDs.");
        WriteIidGuidPropertyName(w, type);
    }

    /// <summary>
    /// Writes a marshaller class for a struct or enum (mirrors C++ write_struct_and_enum_marshaller_class).
    /// </summary>
    private static void WriteStructEnumMarshallerClass(TypeWriter w, TypeDefinition type)
    {
        string name = type.Name?.Value ?? string.Empty;
        string nameStripped = Helpers.StripBackticks(name);
        TypeCategory cat = TypeCategorization.GetCategory(type);
        bool blittable = IsTypeBlittable(type);
        // "Almost-blittable" includes blittable + bool/char fields. Excludes string/object fields.
        // Use the same predicate as IsAnyStruct (which is now scoped to almost-blittable).
        AsmResolver.DotNet.Signatures.TypeDefOrRefSignature sig = type.ToTypeSignature(false) is AsmResolver.DotNet.Signatures.TypeDefOrRefSignature td2 ? td2 : null!;
        bool almostBlittable = cat == TypeCategory.Struct && (sig is null || IsAnyStruct(sig));
        bool isEnum = cat == TypeCategory.Enum;
        // Complex structs are non-almost-blittable structs with reference fields (string, object, etc.).
        bool isComplexStruct = cat == TypeCategory.Struct && !almostBlittable;
        // For complex structs, check if all reference fields are types we can marshal:
        // strings (via HStringMarshaller) or Nullable<T> of supported primitive types
        // (via ABI.System.<T>Marshaller).
        bool allFieldsSupported = true;
        bool hasReferenceFields = false;
        if (isComplexStruct)
        {
            foreach (FieldDefinition field in type.Fields)
            {
                if (field.IsStatic || field.Signature is null) { continue; }
                AsmResolver.DotNet.Signatures.TypeSignature ft = field.Signature.FieldType;
                if (IsBlittablePrimitive(ft)) { continue; }
                if (IsAnyStruct(ft)) { continue; }
                if (IsString(ft)) { hasReferenceFields = true; continue; }
                if (TryGetNullablePrimitiveMarshallerName(ft, out _)) { hasReferenceFields = true; continue; }
                allFieldsSupported = false;
                break;
            }
        }
        bool emitComplexBodies = isComplexStruct && allFieldsSupported;

        w.Write("public static unsafe class ");
        w.Write(nameStripped);
        w.Write("Marshaller\n{\n");

        if (isComplexStruct)
        {
            // ConvertToUnmanaged: build ABI struct from projected struct via per-field marshalling.
            w.Write("    public static ");
            WriteTypedefName(w, type, TypedefNameType.ABI, true);
            w.Write(" ConvertToUnmanaged(");
            WriteTypedefName(w, type, TypedefNameType.Projected, true);
            if (!emitComplexBodies)
            {
                w.Write(" value) => throw null!;\n");
            }
            else
            {
                w.Write(" value)\n    {\n");
                w.Write("        return new() {\n");
                bool first = true;
                foreach (FieldDefinition field in type.Fields)
                {
                    if (field.IsStatic || field.Signature is null) { continue; }
                    string fname = field.Name?.Value ?? "";
                    AsmResolver.DotNet.Signatures.TypeSignature ft = field.Signature.FieldType;
                    if (!first) { w.Write(",\n"); }
                    first = false;
                    w.Write("            ");
                    w.Write(fname);
                    w.Write(" = ");
                    if (IsString(ft))
                    {
                        w.Write("HStringMarshaller.ConvertToUnmanaged(value.");
                        w.Write(fname);
                        w.Write(")");
                    }
                    else if (TryGetNullablePrimitiveMarshallerName(ft, out string? nullableMarshaller))
                    {
                        w.Write(nullableMarshaller!);
                        w.Write(".BoxToUnmanaged(value.");
                        w.Write(fname);
                        w.Write(").DetachThisPtrUnsafe()");
                    }
                    else
                    {
                        w.Write("value.");
                        w.Write(fname);
                    }
                }
                w.Write("\n        };\n    }\n");
            }

            // ConvertToManaged: construct projected struct via constructor accepting the marshalled fields.
            w.Write("    public static ");
            WriteTypedefName(w, type, TypedefNameType.Projected, true);
            w.Write(" ConvertToManaged(");
            WriteTypedefName(w, type, TypedefNameType.ABI, true);
            if (!emitComplexBodies)
            {
                w.Write(" value) => throw null!;\n");
            }
            else
            {
                w.Write(" value)\n    {\n");
                w.Write("        return new ");
                WriteTypedefName(w, type, TypedefNameType.Projected, true);
                w.Write("(\n");
                bool first = true;
                foreach (FieldDefinition field in type.Fields)
                {
                    if (field.IsStatic || field.Signature is null) { continue; }
                    string fname = field.Name?.Value ?? "";
                    AsmResolver.DotNet.Signatures.TypeSignature ft = field.Signature.FieldType;
                    if (!first) { w.Write(",\n"); }
                    first = false;
                    w.Write("            ");
                    if (IsString(ft))
                    {
                        w.Write("HStringMarshaller.ConvertToManaged(value.");
                        w.Write(fname);
                        w.Write(")");
                    }
                    else if (TryGetNullablePrimitiveMarshallerName(ft, out string? nullableMarshaller))
                    {
                        w.Write(nullableMarshaller!);
                        w.Write(".UnboxToManaged(value.");
                        w.Write(fname);
                        w.Write(")");
                    }
                    else
                    {
                        w.Write("value.");
                        w.Write(fname);
                    }
                }
                w.Write("\n        );\n    }\n");
            }

            // Dispose: free non-blittable fields.
            w.Write("    public static void Dispose(");
            WriteTypedefName(w, type, TypedefNameType.ABI, true);
            if (!emitComplexBodies)
            {
                w.Write(" value) => throw null!;\n");
            }
            else
            {
                w.Write(" value)\n    {\n");
                foreach (FieldDefinition field in type.Fields)
                {
                    if (field.IsStatic || field.Signature is null) { continue; }
                    string fname = field.Name?.Value ?? "";
                    AsmResolver.DotNet.Signatures.TypeSignature ft = field.Signature.FieldType;
                    if (IsString(ft))
                    {
                        w.Write("        HStringMarshaller.Free(value.");
                        w.Write(fname);
                        w.Write(");\n");
                    }
                    else if (TryGetNullablePrimitiveMarshallerName(ft, out _))
                    {
                        w.Write("        WindowsRuntimeUnknownMarshaller.Free(value.");
                        w.Write(fname);
                        w.Write(");\n");
                    }
                }
                w.Write("    }\n");
            }
        }

        // BoxToUnmanaged: same pattern for all (enum, almost-blittable, complex).
        // Truth uses CreateComInterfaceFlags.TrackerSupport when the struct has reference type
        // fields (Nullable<T>, etc.) to avoid GC issues with the boxed managed object reference.
        w.Write("    public static WindowsRuntimeObjectReferenceValue BoxToUnmanaged(");
        WriteTypedefName(w, type, TypedefNameType.Projected, true);
        if (isEnum || almostBlittable || emitComplexBodies)
        {
            w.Write("? value)\n    {\n");
            w.Write("        return WindowsRuntimeValueTypeMarshaller.BoxToUnmanaged(value, CreateComInterfaceFlags.");
            w.Write(hasReferenceFields ? "TrackerSupport" : "None");
            w.Write(", in ");
            WriteIidReferenceExpression(w, type);
            w.Write(");\n    }\n");
        }
        else
        {
            w.Write("? value) => throw null!;\n");
        }

        // UnboxToManaged: simple for almost-blittable; for complex, unbox to ABI struct then ConvertToManaged.
        w.Write("    public static ");
        WriteTypedefName(w, type, TypedefNameType.Projected, true);
        if (isEnum || almostBlittable)
        {
            w.Write("? UnboxToManaged(void* value)\n    {\n");
            w.Write("        return WindowsRuntimeValueTypeMarshaller.UnboxToManaged<");
            WriteTypedefName(w, type, TypedefNameType.Projected, true);
            w.Write(">(value);\n    }\n");
        }
        else if (emitComplexBodies)
        {
            w.Write("? UnboxToManaged(void* value)\n    {\n");
            w.Write("        ");
            WriteTypedefName(w, type, TypedefNameType.ABI, true);
            w.Write("? abi = WindowsRuntimeValueTypeMarshaller.UnboxToManaged<");
            WriteTypedefName(w, type, TypedefNameType.ABI, true);
            w.Write(">(value);\n");
            w.Write("        return abi.HasValue ? ConvertToManaged(abi.GetValueOrDefault()) : null;\n    }\n");
        }
        else
        {
            w.Write("? UnboxToManaged(void* value) => throw null!;\n");
        }

        w.Write("}\n\n");

        // Emit the InterfaceEntriesImpl static class and the proper ComWrappersMarshallerAttribute
        // class derived from WindowsRuntimeComWrappersMarshallerAttribute (matches truth).
        // For enums and almost-blittable structs, GetOrCreateComInterfaceForObject uses None.
        // For complex structs (with reference fields), it uses TrackerSupport.
        // For complex structs, CreateObject converts via the *Marshaller.ConvertToManaged after
        // unboxing to the ABI struct.
        if (isEnum || almostBlittable || emitComplexBodies)
        {
            string iidRefExpr = w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteIidReferenceExpression(w, type)));

            // InterfaceEntriesImpl
            w.Write("file static class ");
            w.Write(nameStripped);
            w.Write("InterfaceEntriesImpl\n{\n");
            w.Write("    [FixedAddressValueType]\n");
            w.Write("    public static readonly ReferenceInterfaceEntries Entries;\n\n");
            w.Write("    static ");
            w.Write(nameStripped);
            w.Write("InterfaceEntriesImpl()\n    {\n");
            w.Write("        Entries.IReferenceValue.IID = ");
            w.Write(iidRefExpr);
            w.Write(";\n");
            w.Write("        Entries.IReferenceValue.Vtable = ");
            w.Write(nameStripped);
            w.Write("ReferenceImpl.Vtable;\n");
            w.Write("        Entries.IPropertyValue.IID = global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IPropertyValue;\n");
            w.Write("        Entries.IPropertyValue.Vtable = global::WindowsRuntime.InteropServices.IPropertyValueImpl.OtherTypeVtable;\n");
            w.Write("        Entries.IStringable.IID = global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IStringable;\n");
            w.Write("        Entries.IStringable.Vtable = global::WindowsRuntime.InteropServices.IStringableImpl.Vtable;\n");
            w.Write("        Entries.IWeakReferenceSource.IID = global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IWeakReferenceSource;\n");
            w.Write("        Entries.IWeakReferenceSource.Vtable = global::WindowsRuntime.InteropServices.IWeakReferenceSourceImpl.Vtable;\n");
            w.Write("        Entries.IMarshal.IID = global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IMarshal;\n");
            w.Write("        Entries.IMarshal.Vtable = global::WindowsRuntime.InteropServices.IMarshalImpl.Vtable;\n");
            w.Write("        Entries.IAgileObject.IID = global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IAgileObject;\n");
            w.Write("        Entries.IAgileObject.Vtable = global::WindowsRuntime.InteropServices.IAgileObjectImpl.Vtable;\n");
            w.Write("        Entries.IInspectable.IID = global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IInspectable;\n");
            w.Write("        Entries.IInspectable.Vtable = global::WindowsRuntime.InteropServices.IInspectableImpl.Vtable;\n");
            w.Write("        Entries.IUnknown.IID = global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IUnknown;\n");
            w.Write("        Entries.IUnknown.Vtable = global::WindowsRuntime.InteropServices.IUnknownImpl.Vtable;\n");
            w.Write("    }\n}\n\n");

            // ComWrappersMarshallerAttribute (full body)
            w.Write("internal sealed unsafe class ");
            w.Write(nameStripped);
            w.Write("ComWrappersMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute\n{\n");
            w.Write("    public override void* GetOrCreateComInterfaceForObject(object value)\n    {\n");
            w.Write("        return WindowsRuntimeComWrappersMarshal.GetOrCreateComInterfaceForObject(value, CreateComInterfaceFlags.");
            w.Write(hasReferenceFields ? "TrackerSupport" : "None");
            w.Write(");\n    }\n\n");
            w.Write("    public override ComInterfaceEntry* ComputeVtables(out int count)\n    {\n");
            w.Write("        count = sizeof(ReferenceInterfaceEntries) / sizeof(ComInterfaceEntry);\n");
            w.Write("        return (ComInterfaceEntry*)Unsafe.AsPointer(in ");
            w.Write(nameStripped);
            w.Write("InterfaceEntriesImpl.Entries);\n    }\n\n");
            w.Write("    public override object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)\n    {\n");
            w.Write("        wrapperFlags = CreatedWrapperFlags.NonWrapping;\n");
            if (isComplexStruct && emitComplexBodies)
            {
                w.Write("        return ");
                w.Write(nameStripped);
                w.Write("Marshaller.ConvertToManaged(WindowsRuntimeValueTypeMarshaller.UnboxToManagedUnsafe<");
                WriteTypedefName(w, type, TypedefNameType.ABI, true);
                w.Write(">(value, in ");
                w.Write(iidRefExpr);
                w.Write("));\n");
            }
            else
            {
                w.Write("        return WindowsRuntimeValueTypeMarshaller.UnboxToManagedUnsafe<");
                WriteTypedefName(w, type, TypedefNameType.Projected, true);
                w.Write(">(value, in ");
                w.Write(iidRefExpr);
                w.Write(");\n");
            }
            w.Write("    }\n}\n");
        }
        else
        {
            // Fallback: keep the placeholder class so consumer attribute references resolve.
            w.Write("internal sealed class ");
            w.Write(nameStripped);
            w.Write("ComWrappersMarshallerAttribute : global::System.Attribute\n{\n}\n");
        }
    }

    /// <summary>
    /// Writes a marshaller stub for a delegate.
    /// </summary>
    private static void WriteDelegateMarshallerStub(TypeWriter w, TypeDefinition type)
    {
        string name = type.Name?.Value ?? string.Empty;
        string nameStripped = Helpers.StripBackticks(name);
        string typeNs = type.Namespace?.Value ?? string.Empty;
        string fullProjected = $"global::{typeNs}.{nameStripped}";
        bool isGeneric = type.GenericParameters.Count > 0;

        // Compute the IID expression for this delegate (uses the DelegateMarshaller's IID convention).
        string iidExpr = w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteIidExpression(w, type)));
        string iidRefExpr = w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteIidReferenceExpression(w, type)));

        // Public *Marshaller class
        w.Write("\npublic static unsafe class ");
        w.Write(nameStripped);
        w.Write("Marshaller\n{\n");
        w.Write("    public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(");
        w.Write(fullProjected);
        w.Write(" value)\n    {\n");
        w.Write("        return WindowsRuntimeDelegateMarshaller.ConvertToUnmanaged(value, in ");
        w.Write(iidExpr);
        w.Write(");\n    }\n\n");
        w.Write("    public static ");
        w.Write(fullProjected);
        w.Write("? ConvertToManaged(void* value)\n    {\n");
        w.Write("        return (");
        w.Write(fullProjected);
        w.Write("?)WindowsRuntimeDelegateMarshaller.ConvertToManaged<");
        w.Write(nameStripped);
        w.Write("ComWrappersCallback>(value);\n    }\n}\n\n");

        // ComWrappersMarshallerAttribute - full body for non-generic delegates.
        w.Write("internal sealed unsafe class ");
        w.Write(nameStripped);
        w.Write("ComWrappersMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute\n{\n");
        if (isGeneric)
        {
            w.Write("    public override object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags) => throw null!;\n");
        }
        else
        {
            w.Write("    /// <inheritdoc/>\n");
            w.Write("    public override void* GetOrCreateComInterfaceForObject(object value)\n    {\n");
            w.Write("        return WindowsRuntimeComWrappersMarshal.GetOrCreateComInterfaceForObject(value, CreateComInterfaceFlags.TrackerSupport);\n");
            w.Write("    }\n\n");
            w.Write("    /// <inheritdoc/>\n");
            w.Write("    public override ComInterfaceEntry* ComputeVtables(out int count)\n    {\n");
            w.Write("        count = sizeof(DelegateReferenceInterfaceEntries) / sizeof(ComInterfaceEntry);\n\n");
            w.Write("        return (ComInterfaceEntry*)Unsafe.AsPointer(in ");
            w.Write(nameStripped);
            w.Write("InterfaceEntriesImpl.Entries);\n    }\n\n");
            w.Write("    /// <inheritdoc/>\n");
            w.Write("    public override object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)\n    {\n");
            w.Write("        wrapperFlags = CreatedWrapperFlags.NonWrapping;\n");
            w.Write("        return WindowsRuntimeDelegateMarshaller.UnboxToManaged<");
            w.Write(nameStripped);
            w.Write("ComWrappersCallback>(value, in ");
            w.Write(iidRefExpr);
            w.Write(")!;\n    }\n");
        }
        w.Write("}\n\n");

        // file-scoped *ComWrappersCallback for delegate.
        // Truth uses 'file abstract' (not 'file sealed') because it's a marker type only.
        if (isGeneric)
        {
            w.Write("file sealed unsafe class ");
            w.Write(nameStripped);
            w.Write("ComWrappersCallback : IWindowsRuntimeObjectComWrappersCallback\n{\n");
            w.Write("    public static object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags) => throw null!;\n");
            w.Write("}\n");
        }
        else
        {
            // Determine if NativeDelegate Invoke extension is supported (no return + simple params)
            // by reusing the same checks as EmitNativeDelegateBody.
            MethodDefinition? invoke = Helpers.GetDelegateInvoke(type);
            bool nativeSupported = invoke is not null && IsDelegateInvokeNativeSupported(new MethodSig(invoke));

            w.Write("file abstract unsafe class ");
            w.Write(nameStripped);
            w.Write("ComWrappersCallback : IWindowsRuntimeObjectComWrappersCallback\n{\n");
            w.Write("    /// <inheritdoc/>\n");
            w.Write("    public static object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)\n    {\n");
            w.Write("        WindowsRuntimeObjectReference valueReference = WindowsRuntimeComWrappersMarshal.CreateObjectReferenceUnsafe(\n");
            w.Write("            externalComObject: value,\n");
            w.Write("            iid: in ");
            w.Write(iidExpr);
            w.Write(",\n            wrapperFlags: out wrapperFlags);\n\n");
            if (nativeSupported)
            {
                w.Write("        return new ");
                w.Write(fullProjected);
                w.Write("(valueReference.");
                w.Write(nameStripped);
                w.Write("Invoke);\n");
            }
            else
            {
                w.Write("        throw null!;\n");
            }
            w.Write("    }\n");
            w.Write("}\n");
        }
    }

    /// <summary>True if EmitNativeDelegateBody can emit a real (non-throw) body for this signature.</summary>
    private static bool IsDelegateInvokeNativeSupported(MethodSig sig)
    {
        AsmResolver.DotNet.Signatures.TypeSignature? rt = sig.ReturnType;
        if (rt is not null)
        {
            if (IsHResultException(rt)) { return false; }
            if (!(IsBlittablePrimitive(rt) || IsAnyStruct(rt) || IsString(rt) || IsRuntimeClassOrInterface(rt) || IsObject(rt))) { return false; }
        }
        foreach (ParamInfo p in sig.Params)
        {
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat == ParamCategory.PassArray || cat == ParamCategory.FillArray)
            {
                if (p.Type is AsmResolver.DotNet.Signatures.SzArrayTypeSignature szP)
                {
                    if (IsBlittablePrimitive(szP.BaseType)) { continue; }
                    if (IsAnyStruct(szP.BaseType)) { continue; }
                }
                return false;
            }
            if (cat != ParamCategory.In) { return false; }
            if (IsHResultException(p.Type)) { return false; }
            if (IsBlittablePrimitive(p.Type)) { continue; }
            if (IsAnyStruct(p.Type)) { continue; }
            if (IsString(p.Type)) { continue; }
            if (IsRuntimeClassOrInterface(p.Type)) { continue; }
            if (IsObject(p.Type)) { continue; }
            if (IsGenericInstance(p.Type)) { continue; }
            return false;
        }
        return true;
    }

    /// <summary>
    /// Writes the marshaller infrastructure for a runtime class:
    /// * Public *Marshaller class with real ConvertToUnmanaged/ConvertToManaged bodies
    /// * file-scoped *ComWrappersMarshallerAttribute (CreateObject implementation)
    /// * file-scoped *ComWrappersCallback (IWindowsRuntimeObjectComWrappersCallback for sealed,
    ///   IWindowsRuntimeUnsealedObjectComWrappersCallback for unsealed)
    /// Mirrors C++ <c>write_class_marshaller</c>, <c>write_class_comwrappers_marshaller_attribute</c>,
    /// and <c>write_class_comwrappers_callback</c>.
    /// </summary>
    private static void WriteClassMarshallerStub(TypeWriter w, TypeDefinition type)
    {
        string name = type.Name?.Value ?? string.Empty;
        string nameStripped = Helpers.StripBackticks(name);
        string typeNs = type.Namespace?.Value ?? string.Empty;
        string fullProjected = $"global::{typeNs}.{nameStripped}";

        // Get the IID expression for the default interface (used by CreateObject).
        ITypeDefOrRef? defaultIface = Helpers.GetDefaultInterface(type);
        string defaultIfaceIid = defaultIface is not null
            ? w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteIidExpression(w, defaultIface)))
            : "default(global::System.Guid)";

        // Determine the marshalingType expression from the class's [MarshalingBehaviorAttribute]
        // (mirrors C++ get_marshaling_type_name). This is used by both the marshaller attribute and the
        // callback (the C++ code uses the same value for both).
        string marshalingType = GetMarshalingTypeName(type);

        bool isSealed = type.IsSealed;

        // For unsealed classes, the ConvertToUnmanaged path needs to know whether the default interface is
        // exclusive-to (mirrors C++ logic).
        TypeDefinition? defaultIfaceTd = defaultIface is null ? null : ResolveInterfaceTypeDef(defaultIface);
        bool defaultIfaceIsExclusive = defaultIfaceTd is not null && TypeCategorization.IsExclusiveTo(defaultIfaceTd);

        // Public *Marshaller class
        w.Write("public static unsafe class ");
        w.Write(nameStripped);
        w.Write("Marshaller\n{\n");
        w.Write("    public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(");
        w.Write(fullProjected);
        w.Write(" value)\n    {\n");
        if (isSealed)
        {
            // For projected sealed runtime classes, the RCW type is always unwrappable.
            w.Write("        if (value is not null)\n        {\n");
            w.Write("            return WindowsRuntimeComWrappersMarshal.UnwrapObjectReferenceUnsafe(value).AsValue();\n");
            w.Write("        }\n");
        }
        else if (!defaultIfaceIsExclusive && defaultIface is not null)
        {
            string defIfaceTypeName = w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteTypeName(w, TypeSemanticsFactory.Get(defaultIface.ToTypeSignature(false)), TypedefNameType.Projected, false)));
            w.Write("        if (value is IWindowsRuntimeInterface<");
            w.Write(defIfaceTypeName);
            w.Write("> windowsRuntimeInterface)\n        {\n");
            w.Write("            return windowsRuntimeInterface.GetInterface();\n");
            w.Write("        }\n");
        }
        else
        {
            w.Write("        if (value is not null)\n        {\n");
            w.Write("            return value.GetDefaultInterface();\n");
            w.Write("        }\n");
        }
        w.Write("        return default;\n    }\n\n");
        w.Write("    public static ");
        w.Write(fullProjected);
        w.Write("? ConvertToManaged(void* value)\n    {\n");
        w.Write("        return (");
        w.Write(fullProjected);
        w.Write("?)");
        w.Write(isSealed ? "WindowsRuntimeObjectMarshaller" : "WindowsRuntimeUnsealedObjectMarshaller");
        w.Write(".ConvertToManaged<");
        w.Write(nameStripped);
        w.Write("ComWrappersCallback>(value);\n    }\n}\n\n");

        // file-scoped *ComWrappersMarshallerAttribute - implements WindowsRuntimeComWrappersMarshallerAttribute.CreateObject
        w.Write("file sealed unsafe class ");
        w.Write(nameStripped);
        w.Write("ComWrappersMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute\n{\n");
        w.Write("    public override object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)\n    {\n");
        w.Write("        WindowsRuntimeObjectReference valueReference = WindowsRuntimeComWrappersMarshal.CreateObjectReference(\n");
        w.Write("            externalComObject: value,\n");
        w.Write("            iid: ");
        w.Write(defaultIfaceIid);
        w.Write(",\n");
        w.Write("            marshalingType: ");
        w.Write(marshalingType);
        w.Write(",\n");
        w.Write("            wrapperFlags: out wrapperFlags);\n\n");
        w.Write("        return new ");
        w.Write(fullProjected);
        w.Write("(valueReference);\n    }\n}\n\n");

        if (isSealed)
        {
            // file-scoped *ComWrappersCallback - implements IWindowsRuntimeObjectComWrappersCallback
            w.Write("file sealed unsafe class ");
            w.Write(nameStripped);
            w.Write("ComWrappersCallback : IWindowsRuntimeObjectComWrappersCallback\n{\n");
            w.Write("    public static object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)\n    {\n");
            w.Write("        WindowsRuntimeObjectReference valueReference = WindowsRuntimeComWrappersMarshal.CreateObjectReferenceUnsafe(\n");
            w.Write("            externalComObject: value,\n");
            w.Write("            iid: ");
            w.Write(defaultIfaceIid);
            w.Write(",\n");
            w.Write("            marshalingType: ");
            w.Write(marshalingType);
            w.Write(",\n");
            w.Write("            wrapperFlags: out wrapperFlags);\n\n");
            w.Write("        return new ");
            w.Write(fullProjected);
            w.Write("(valueReference);\n    }\n}\n");
        }
        else
        {
            // file-scoped *ComWrappersCallback - implements IWindowsRuntimeUnsealedObjectComWrappersCallback
            string nonProjectedRcn = $"{typeNs}.{nameStripped}";
            w.Write("file sealed unsafe class ");
            w.Write(nameStripped);
            w.Write("ComWrappersCallback : IWindowsRuntimeUnsealedObjectComWrappersCallback\n{\n");

            // TryCreateObject (non-projected runtime class name match)
            w.Write("    public static unsafe bool TryCreateObject(\n");
            w.Write("        void* value,\n");
            w.Write("        ReadOnlySpan<char> runtimeClassName,\n");
            w.Write("        [global::System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out object? wrapperObject,\n");
            w.Write("        out CreatedWrapperFlags wrapperFlags)\n    {\n");
            w.Write("        if (runtimeClassName.SequenceEqual(\"");
            w.Write(nonProjectedRcn);
            w.Write("\".AsSpan()))\n        {\n");
            w.Write("            WindowsRuntimeObjectReference valueReference = WindowsRuntimeComWrappersMarshal.CreateObjectReferenceUnsafe(\n");
            w.Write("                externalComObject: value,\n");
            w.Write("                iid: ");
            w.Write(defaultIfaceIid);
            w.Write(",\n");
            w.Write("                marshalingType: ");
            w.Write(marshalingType);
            w.Write(",\n");
            w.Write("                wrapperFlags: out wrapperFlags);\n\n");
            w.Write("            wrapperObject = new ");
            w.Write(fullProjected);
            w.Write("(valueReference);\n            return true;\n        }\n\n");
            w.Write("        wrapperObject = null;\n        wrapperFlags = CreatedWrapperFlags.None;\n        return false;\n    }\n\n");

            // CreateObject (fallback)
            w.Write("    public static unsafe object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)\n    {\n");
            w.Write("        WindowsRuntimeObjectReference valueReference = WindowsRuntimeComWrappersMarshal.CreateObjectReferenceUnsafe(\n");
            w.Write("            externalComObject: value,\n");
            w.Write("            iid: ");
            w.Write(defaultIfaceIid);
            w.Write(",\n");
            w.Write("            marshalingType: ");
            w.Write(marshalingType);
            w.Write(",\n");
            w.Write("            wrapperFlags: out wrapperFlags);\n\n");
            w.Write("        return new ");
            w.Write(fullProjected);
            w.Write("(valueReference);\n    }\n}\n");
        }
    }

    /// <summary>
    /// Writes a minimal interface 'Methods' static class with method body emission.
    /// Mirrors C++ <c>write_static_abi_methods</c>: void/no-args methods and
    /// blittable-primitive-return/no-args methods get real implementations; everything else
    /// remains as 'throw null!' stubs (deferred — needs full per-parameter marshalling).
    /// </summary>
    private static void WriteInterfaceMarshallerStub(TypeWriter w, TypeDefinition type)
    {
        string name = type.Name?.Value ?? string.Empty;
        string nameStripped = Helpers.StripBackticks(name);
        // Mirrors C++ write_static_abi_classes: visibility is internal if the interface is
        // exclusive to a class (and not opted into PublicExclusiveTo) or if it's marked
        // [ProjectionInternal]; public otherwise.
        bool useInternal = (TypeCategorization.IsExclusiveTo(type) && !w.Settings.PublicExclusiveTo)
            || TypeCategorization.IsProjectionInternal(type);

        // Mirrors C++ skip_exclusive_events: events on exclusive interfaces (used by the class)
        // are inlined in the RCW class, so we skip emitting them in the Methods type.
        bool skipExclusiveEvents = false;
        if (TypeCategorization.IsExclusiveTo(type) && !w.Settings.PublicExclusiveTo)
        {
            TypeDefinition? classType = GetExclusiveToType(type);
            if (classType is not null)
            {
                foreach (InterfaceImplementation impl in classType.Interfaces)
                {
                    TypeDefinition? implDef = ResolveInterfaceTypeDef(impl.Interface!);
                    if (implDef is not null && implDef == type)
                    {
                        skipExclusiveEvents = true;
                        break;
                    }
                }
            }
        }

        // Skip emission for empty interfaces (no non-special methods, no properties, no events
        // — except events skipped due to skipExclusiveEvents). Mirrors C++ 'if (members.empty()) { return; }'.
        bool hasMembers = false;
        foreach (MethodDefinition m in type.Methods)
        {
            if (!Helpers.IsSpecial(m)) { hasMembers = true; break; }
        }
        if (!hasMembers)
        {
            foreach (PropertyDefinition _ in type.Properties) { hasMembers = true; break; }
        }
        if (!hasMembers && !skipExclusiveEvents)
        {
            foreach (EventDefinition _ in type.Events) { hasMembers = true; break; }
        }
        if (!hasMembers) { return; }

        w.Write(useInternal ? "internal static unsafe class " : "public static unsafe class ");
        w.Write(nameStripped);
        w.Write("Methods\n{\n");

        // Compute the index of each non-special method in the interface (for vtable slot calculation).
        // The first non-special method gets slot 6 (after the 6 IUnknown+IInspectable slots).
        int slot = 6;
        foreach (MethodDefinition method in type.Methods)
        {
            if (Helpers.IsSpecial(method)) { continue; }
            string mname = method.Name?.Value ?? string.Empty;
            MethodSig sig = new(method);

            w.Write("    public static ");
            WriteProjectionReturnType(w, sig);
            w.Write(" ");
            w.Write(mname);
            w.Write("(WindowsRuntimeObjectReference thisReference");
            if (sig.Params.Count > 0) { w.Write(", "); }
            WriteParameterList(w, sig);
            w.Write(")");

            // Emit the body if we can handle this case
            EmitAbiMethodBodyIfSimple(w, sig, slot);
            slot++;
        }

        // Emit property accessors. Each getter / setter consumes one vtable slot.
        foreach (PropertyDefinition prop in type.Properties)
        {
            string pname = prop.Name?.Value ?? string.Empty;
            (MethodDefinition? getter, MethodDefinition? setter) = Helpers.GetPropertyMethods(prop);
            string propType = WritePropType(w, prop);
            (MethodDefinition? gMethod, MethodDefinition? sMethod) = (getter, setter);
            if (gMethod is not null)
            {
                w.Write("    public static ");
                w.Write(propType);
                w.Write(" ");
                w.Write(pname);
                w.Write("(WindowsRuntimeObjectReference thisReference)");
                MethodSig getSig = new(gMethod);
                EmitAbiMethodBodyIfSimple(w, getSig, slot);
                slot++;
            }
            if (sMethod is not null)
            {
                MethodSig setSig = new(sMethod);
                w.Write("    public static void ");
                w.Write(pname);
                w.Write("(WindowsRuntimeObjectReference thisReference, ");
                w.Write(propType);
                w.Write(" value)");
                EmitAbiMethodBodyIfSimple(w, setSig, slot, paramNameOverride: "value");
                slot++;
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

            // Build the projected event source type name. For non-generic delegate handlers, the
            // EventSource subclass lives in the ABI namespace alongside this Methods class, so
            // we need to use the ABI-qualified name. For generic handlers (Windows.Foundation.*EventHandler),
            // it's mapped to global::WindowsRuntime.InteropServices.EventHandlerEventSource<...>.
            string eventSourceProjectedFull;
            if (isGenericEvent)
            {
                eventSourceProjectedFull = w.WriteTemp("%", new System.Action<TextWriter>(_ =>
                    WriteTypeName(w, TypeSemanticsFactory.Get(evtSig), TypedefNameType.EventSource, true)));
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
                    delegateName = Helpers.StripBackticks(delegateName);
                }
                eventSourceProjectedFull = delegateName + "EventSource";
            }
            string eventSourceInteropType = isGenericEvent
                ? EncodeInteropTypeName(evtSig, TypedefNameType.EventSource) + ", WinRT.Interop"
                : string.Empty;

            // Emit the per-event ConditionalWeakTable static field.
            w.Write("\n    private static ConditionalWeakTable<object, ");
            w.Write(eventSourceProjectedFull);
            w.Write("> _");
            w.Write(evtName);
            w.Write("\n    {\n");
            w.Write("        [MethodImpl(MethodImplOptions.AggressiveInlining)]\n");
            w.Write("        get\n        {\n");
            w.Write("            [MethodImpl(MethodImplOptions.NoInlining)]\n");
            w.Write("            static ConditionalWeakTable<object, ");
            w.Write(eventSourceProjectedFull);
            w.Write("> MakeTable()\n            {\n");
            w.Write("                _ = global::System.Threading.Interlocked.CompareExchange(ref field, [], null);\n\n");
            w.Write("                return global::System.Threading.Volatile.Read(in field);\n");
            w.Write("            }\n\n");
            w.Write("            return global::System.Threading.Volatile.Read(in field) ?? MakeTable();\n        }\n    }\n");

            // Emit the static method that returns the per-instance event source.
            w.Write("\n    public static ");
            w.Write(eventSourceProjectedFull);
            w.Write(" ");
            w.Write(evtName);
            w.Write("(object thisObject, WindowsRuntimeObjectReference thisReference)\n    {\n");
            if (isGenericEvent && !string.IsNullOrEmpty(eventSourceInteropType))
            {
                w.Write("        [UnsafeAccessor(UnsafeAccessorKind.Constructor)]\n");
                w.Write("        [return: UnsafeAccessorType(\"");
                w.Write(eventSourceInteropType);
                w.Write("\")]\n");
                w.Write("        static extern object ctor(WindowsRuntimeObjectReference nativeObjectReference, int index);\n\n");
                w.Write("        return _");
                w.Write(evtName);
                w.Write(".GetOrAdd(\n");
                w.Write("            key: thisObject,\n");
                w.Write("            valueFactory: static (_, thisReference) => Unsafe.As<");
                w.Write(eventSourceProjectedFull);
                w.Write(">(ctor(thisReference, ");
                w.Write(slot.ToString(System.Globalization.CultureInfo.InvariantCulture));
                w.Write(")),\n");
                w.Write("            factoryArgument: thisReference);\n");
            }
            else
            {
                // Non-generic delegate: directly construct.
                w.Write("        return _");
                w.Write(evtName);
                w.Write(".GetOrAdd(\n");
                w.Write("            key: thisObject,\n");
                w.Write("            valueFactory: static (_, thisReference) => new ");
                w.Write(eventSourceProjectedFull);
                w.Write("(thisReference, ");
                w.Write(slot.ToString(System.Globalization.CultureInfo.InvariantCulture));
                w.Write("),\n");
                w.Write("            factoryArgument: thisReference);\n");
            }
            w.Write("    }\n");
            // Each event consumes 2 vtable slots (add + remove).
            slot += 2;
        }

        w.Write("}\n");
    }

    /// <summary>
    /// Emits a real method body for the cases we can fully marshal, otherwise emits
    /// the 'throw null!' stub. Trailing newline is included.
    /// </summary>
    private static void EmitAbiMethodBodyIfSimple(TypeWriter w, MethodSig sig, int slot, string? paramNameOverride = null)
    {
        AsmResolver.DotNet.Signatures.TypeSignature? rt = sig.ReturnType;

        // Check that all parameters are types we can marshal.
        bool allParamsSimple = true;
        foreach (ParamInfo p in sig.Params)
        {
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat == ParamCategory.PassArray || cat == ParamCategory.FillArray)
            {
                // Allow blittable primitive arrays and blittable struct arrays.
                if (p.Type is AsmResolver.DotNet.Signatures.SzArrayTypeSignature sz)
                {
                    if (IsBlittablePrimitive(sz.BaseType)) { continue; }
                    if (IsAnyStruct(sz.BaseType)) { continue; }
                    if (IsString(sz.BaseType)) { continue; }
                    if (IsRuntimeClassOrInterface(sz.BaseType)) { continue; }
                    if (IsObject(sz.BaseType)) { continue; }
                }
                allParamsSimple = false; break;
            }
            if (cat == ParamCategory.Out)
            {
                AsmResolver.DotNet.Signatures.TypeSignature underlying = StripByRefAndCustomModifiers(p.Type);
                if (IsHResultException(underlying)) { allParamsSimple = false; break; }
                if (IsBlittablePrimitive(underlying)) { continue; }
                if (IsAnyStruct(underlying)) { continue; }
                if (IsString(underlying)) { continue; }
                if (IsRuntimeClassOrInterface(underlying)) { continue; }
                if (IsObject(underlying)) { continue; }
                allParamsSimple = false; break;
            }
            if (cat == ParamCategory.Ref)
            {
                AsmResolver.DotNet.Signatures.TypeSignature underlying = StripByRefAndCustomModifiers(p.Type);
                if (IsHResultException(underlying)) { allParamsSimple = false; break; }
                if (IsBlittablePrimitive(underlying)) { continue; }
                if (IsAnyStruct(underlying)) { continue; }
                allParamsSimple = false; break;
            }
            if (cat == ParamCategory.ReceiveArray)
            {
                // Allow blittable primitive arrays and almost-blittable struct arrays.
                AsmResolver.DotNet.Signatures.TypeSignature underlying = StripByRefAndCustomModifiers(p.Type);
                if (underlying is AsmResolver.DotNet.Signatures.SzArrayTypeSignature sza)
                {
                    if (IsBlittablePrimitive(sza.BaseType)) { continue; }
                    if (IsAnyStruct(sza.BaseType)) { continue; }
                }
                allParamsSimple = false; break;
            }
            if (cat != ParamCategory.In) { allParamsSimple = false; break; }
            if (IsHResultException(p.Type)) { continue; } // Handled via global::ABI.System.ExceptionMarshaller
            if (IsBlittablePrimitive(p.Type)) { continue; }
            if (IsAnyStruct(p.Type)) { continue; }
            if (IsString(p.Type)) { continue; }
            if (IsRuntimeClassOrInterface(p.Type)) { continue; }
            if (IsObject(p.Type)) { continue; }
            if (IsGenericInstance(p.Type)) { continue; }
            allParamsSimple = false;
            break;
        }
        // Determine return-type kind: scalar, ref-type (string/object/runtime class/generic instance), blittable struct, or receive-array.
        bool returnIsReceiveArray = rt is AsmResolver.DotNet.Signatures.SzArrayTypeSignature retSz0
            && (IsBlittablePrimitive(retSz0.BaseType) || IsAnyStruct(retSz0.BaseType)
                || IsString(retSz0.BaseType) || IsRuntimeClassOrInterface(retSz0.BaseType) || IsObject(retSz0.BaseType));
        bool returnIsHResultException = rt is not null && IsHResultException(rt);
        bool returnIsComplexStructLocal = rt is not null && IsComplexStruct(rt);
        bool returnSimple = rt is null
            || (IsBlittablePrimitive(rt) && !IsHResultException(rt))
            || (IsAnyStruct(rt) && !IsHResultException(rt))
            || returnIsComplexStructLocal
            || IsString(rt)
            || IsRuntimeClassOrInterface(rt)
            || IsObject(rt)
            || IsGenericInstance(rt)
            || returnIsReceiveArray
            || returnIsHResultException;

        if (!allParamsSimple || !returnSimple)
        {
            w.Write(" => throw null!;\n");
            return;
        }

        bool returnIsString = rt is not null && IsString(rt);
        bool returnIsRefType = rt is not null && (IsRuntimeClassOrInterface(rt) || IsObject(rt) || IsGenericInstance(rt));
        bool returnIsAnyStruct = rt is not null && IsAnyStruct(rt);
        bool returnIsComplexStruct = rt is not null && IsComplexStruct(rt);

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
                AsmResolver.DotNet.Signatures.TypeSignature uOut = StripByRefAndCustomModifiers(p.Type);
                fp.Append(", ");
                if (IsString(uOut) || IsRuntimeClassOrInterface(uOut) || IsObject(uOut)) { fp.Append("void**"); }
                else if (IsAnyStruct(uOut)) { fp.Append(GetBlittableStructAbiType(w, uOut)); fp.Append('*'); }
                else { fp.Append(GetAbiPrimitiveType(uOut)); fp.Append('*'); }
                continue;
            }
            if (cat == ParamCategory.Ref)
            {
                AsmResolver.DotNet.Signatures.TypeSignature uRef = StripByRefAndCustomModifiers(p.Type);
                fp.Append(", ");
                if (IsAnyStruct(uRef)) { fp.Append(GetBlittableStructAbiType(w, uRef)); fp.Append('*'); }
                else { fp.Append(GetAbiPrimitiveType(uRef)); fp.Append('*'); }
                continue;
            }
            if (cat == ParamCategory.ReceiveArray)
            {
                AsmResolver.DotNet.Signatures.SzArrayTypeSignature sza = (AsmResolver.DotNet.Signatures.SzArrayTypeSignature)StripByRefAndCustomModifiers(p.Type);
                fp.Append(", uint*, ");
                if (IsAnyStruct(sza.BaseType)) { fp.Append(GetBlittableStructAbiType(w, sza.BaseType)); }
                else { fp.Append(GetAbiPrimitiveType(sza.BaseType)); }
                fp.Append("**");
                continue;
            }
            fp.Append(", ");
            if (IsHResultException(p.Type)) { fp.Append("global::ABI.System.Exception"); }
            else if (IsString(p.Type) || IsRuntimeClassOrInterface(p.Type) || IsObject(p.Type) || IsGenericInstance(p.Type)) { fp.Append("void*"); }
            else if (IsAnyStruct(p.Type)) { fp.Append(GetBlittableStructAbiType(w, p.Type)); }
            else { fp.Append(GetAbiPrimitiveType(p.Type)); }
        }
        if (rt is not null)
        {
            if (returnIsReceiveArray)
            {
                AsmResolver.DotNet.Signatures.SzArrayTypeSignature retSz = (AsmResolver.DotNet.Signatures.SzArrayTypeSignature)rt;
                fp.Append(", uint*, ");
                if (IsString(retSz.BaseType) || IsRuntimeClassOrInterface(retSz.BaseType) || IsObject(retSz.BaseType))
                {
                    fp.Append("void*");
                }
                else if (IsAnyStruct(retSz.BaseType))
                {
                    fp.Append(GetBlittableStructAbiType(w, retSz.BaseType));
                }
                else
                {
                    fp.Append(GetAbiPrimitiveType(retSz.BaseType));
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
                else if (returnIsAnyStruct) { fp.Append(GetBlittableStructAbiType(w, rt)); fp.Append('*'); }
                else if (returnIsComplexStruct) { fp.Append(GetAbiStructTypeName(w, rt)); fp.Append('*'); }
                else { fp.Append(GetAbiPrimitiveType(rt)); fp.Append('*'); }
            }
        }
        fp.Append(", int");

        w.Write("\n    {\n");
        w.Write("        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();\n");
        w.Write("        void* ThisPtr = thisValue.GetThisPtrUnsafe();\n");

        // Declare 'using' marshaller values for ref-type parameters (these need disposing).
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            if (IsRuntimeClassOrInterface(p.Type) || IsObject(p.Type))
            {
                string localName = GetParamLocalName(p, paramNameOverride);
                string callName = GetParamName(p, paramNameOverride);
                w.Write("        using WindowsRuntimeObjectReferenceValue __");
                w.Write(localName);
                w.Write(" = ");
                EmitMarshallerConvertToUnmanaged(w, p.Type, callName);
                w.Write(";\n");
            }
            else if (IsGenericInstance(p.Type))
            {
                // Generic instance param: emit a local UnsafeAccessor delegate to get the marshaller method.
                string localName = GetParamLocalName(p, paramNameOverride);
                string callName = GetParamName(p, paramNameOverride);
                string interopTypeName = EncodeInteropTypeName(p.Type, TypedefNameType.ABI) + ", WinRT.Interop";
                string projectedTypeName = w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteProjectedSignature(w, p.Type, false)));
                w.Write("        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"ConvertToUnmanaged\")]\n");
                w.Write("        static extern WindowsRuntimeObjectReferenceValue ConvertToUnmanaged_");
                w.Write(localName);
                w.Write("([UnsafeAccessorType(\"");
                w.Write(interopTypeName);
                w.Write("\")] object _, ");
                w.Write(projectedTypeName);
                w.Write(" value);\n");
                w.Write("        using WindowsRuntimeObjectReferenceValue __");
                w.Write(localName);
                w.Write(" = ConvertToUnmanaged_");
                w.Write(localName);
                w.Write("(null, ");
                w.Write(callName);
                w.Write(");\n");
            }
        }
        // Declare locals for string parameters (input HSTRINGs to be freed)
        for (int i = 0; i < sig.Params.Count; i++)
        {
            if (IsString(sig.Params[i].Type))
            {
                w.Write("        void* __");
                w.Write(GetParamLocalName(sig.Params[i], paramNameOverride));
                w.Write(" = default;\n");
            }
        }
        // Declare locals for HResult/Exception input parameters (converted up-front).
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            if (ParamHelpers.GetParamCategory(p) != ParamCategory.In) { continue; }
            if (!IsHResultException(p.Type)) { continue; }
            string localName = GetParamLocalName(p, paramNameOverride);
            string callName = GetParamName(p, paramNameOverride);
            w.Write("        global::ABI.System.Exception __");
            w.Write(localName);
            w.Write(" = global::ABI.System.ExceptionMarshaller.ConvertToUnmanaged(");
            w.Write(callName);
            w.Write(");\n");
        }
        // Declare locals for Out parameters (need to be passed as &__<name> to the call).
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.Out) { continue; }
            string localName = GetParamLocalName(p, paramNameOverride);
            AsmResolver.DotNet.Signatures.TypeSignature uOut = StripByRefAndCustomModifiers(p.Type);
            w.Write("        ");
            if (IsString(uOut) || IsRuntimeClassOrInterface(uOut) || IsObject(uOut)) { w.Write("void*"); }
            else if (IsAnyStruct(uOut)) { w.Write(GetBlittableStructAbiType(w, uOut)); }
            else { w.Write(GetAbiPrimitiveType(uOut)); }
            w.Write(" __");
            w.Write(localName);
            w.Write(" = default;\n");
        }
        // Declare locals for ReceiveArray params (uint length + element pointer).
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.ReceiveArray) { continue; }
            string localName = GetParamLocalName(p, paramNameOverride);
            AsmResolver.DotNet.Signatures.SzArrayTypeSignature sza = (AsmResolver.DotNet.Signatures.SzArrayTypeSignature)StripByRefAndCustomModifiers(p.Type);
            w.Write("        uint __");
            w.Write(localName);
            w.Write("_length = default;\n");
            w.Write("        ");
            if (IsAnyStruct(sza.BaseType)) { w.Write(GetBlittableStructAbiType(w, sza.BaseType)); }
            else { w.Write(GetAbiPrimitiveType(sza.BaseType)); }
            w.Write("* __");
            w.Write(localName);
            w.Write("_data = default;\n");
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
            if (IsBlittablePrimitive(szArr.BaseType) || IsAnyStruct(szArr.BaseType)) { continue; }
            // Non-blittable element type: emit InlineArray16<nint> + ArrayPool<nint>.
            string localName = GetParamLocalName(p, paramNameOverride);
            string callName = GetParamName(p, paramNameOverride);
            w.Write("\n        Unsafe.SkipInit(out InlineArray16<nint> __");
            w.Write(localName);
            w.Write("_inlineArray);\n");
            w.Write("        nint[] __");
            w.Write(localName);
            w.Write("_arrayFromPool = null;\n");
            w.Write("        Span<nint> __");
            w.Write(localName);
            w.Write("_span = ");
            w.Write(callName);
            w.Write(".Length <= 16\n            ? __");
            w.Write(localName);
            w.Write("_inlineArray[..");
            w.Write(callName);
            w.Write(".Length]\n            : (__");
            w.Write(localName);
            w.Write("_arrayFromPool = global::System.Buffers.ArrayPool<nint>.Shared.Rent(");
            w.Write(callName);
            w.Write(".Length));\n");

            if (IsString(szArr.BaseType))
            {
                // Strings need an additional InlineArray16<HStringHeader> + InlineArray16<nint> (pinned handles).
                w.Write("\n        Unsafe.SkipInit(out InlineArray16<HStringHeader> __");
                w.Write(localName);
                w.Write("_inlineHeaderArray);\n");
                w.Write("        HStringHeader[] __");
                w.Write(localName);
                w.Write("_headerArrayFromPool = null;\n");
                w.Write("        Span<HStringHeader> __");
                w.Write(localName);
                w.Write("_headerSpan = ");
                w.Write(callName);
                w.Write(".Length <= 16\n            ? __");
                w.Write(localName);
                w.Write("_inlineHeaderArray[..");
                w.Write(callName);
                w.Write(".Length]\n            : (__");
                w.Write(localName);
                w.Write("_headerArrayFromPool = global::System.Buffers.ArrayPool<HStringHeader>.Shared.Rent(");
                w.Write(callName);
                w.Write(".Length));\n");

                w.Write("\n        Unsafe.SkipInit(out InlineArray16<nint> __");
                w.Write(localName);
                w.Write("_inlinePinnedHandleArray);\n");
                w.Write("        nint[] __");
                w.Write(localName);
                w.Write("_pinnedHandleArrayFromPool = null;\n");
                w.Write("        Span<nint> __");
                w.Write(localName);
                w.Write("_pinnedHandleSpan = ");
                w.Write(callName);
                w.Write(".Length <= 16\n            ? __");
                w.Write(localName);
                w.Write("_inlinePinnedHandleArray[..");
                w.Write(callName);
                w.Write(".Length]\n            : (__");
                w.Write(localName);
                w.Write("_pinnedHandleArrayFromPool = global::System.Buffers.ArrayPool<nint>.Shared.Rent(");
                w.Write(callName);
                w.Write(".Length));\n");
            }
        }
        if (returnIsReceiveArray)
        {
            AsmResolver.DotNet.Signatures.SzArrayTypeSignature retSz = (AsmResolver.DotNet.Signatures.SzArrayTypeSignature)rt!;
            w.Write("        uint __retval_length = default;\n");
            w.Write("        ");
            if (IsString(retSz.BaseType) || IsRuntimeClassOrInterface(retSz.BaseType) || IsObject(retSz.BaseType))
            {
                w.Write("void*");
            }
            else if (IsAnyStruct(retSz.BaseType))
            {
                w.Write(GetBlittableStructAbiType(w, retSz.BaseType));
            }
            else
            {
                w.Write(GetAbiPrimitiveType(retSz.BaseType));
            }
            w.Write("* __retval_data = default;\n");
        }
        else if (returnIsHResultException)
        {
            w.Write("        global::ABI.System.Exception __retval = default;\n");
        }
        else if (returnIsString || returnIsRefType)
        {
            w.Write("        void* __retval = default;\n");
        }
        else if (returnIsAnyStruct)
        {
            w.Write("        ");
            w.Write(GetBlittableStructAbiType(w, rt!));
            w.Write(" __retval = default;\n");
        }
        else if (returnIsComplexStruct)
        {
            w.Write("        ");
            w.Write(GetAbiStructTypeName(w, rt!));
            w.Write(" __retval = default;\n");
        }
        else if (rt is not null)
        {
            w.Write("        ");
            w.Write(GetAbiPrimitiveType(rt));
            w.Write(" __retval = default;\n");
        }

        // Determine if we need a try/finally (for cleanup of string params or string/refType return or receive array return or Out runtime class params).
        bool hasStringParams = false;
        for (int i = 0; i < sig.Params.Count; i++) { if (IsString(sig.Params[i].Type)) { hasStringParams = true; break; } }
        bool hasOutNeedsCleanup = false;
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.Out) { continue; }
            AsmResolver.DotNet.Signatures.TypeSignature uOut = StripByRefAndCustomModifiers(p.Type);
            if (IsString(uOut) || IsRuntimeClassOrInterface(uOut) || IsObject(uOut)) { hasOutNeedsCleanup = true; break; }
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
                && !IsBlittablePrimitive(szArrCheck.BaseType) && !IsAnyStruct(szArrCheck.BaseType))
            {
                hasNonBlittablePassArray = true; break;
            }
        }
        bool needsTryFinally = hasStringParams || returnIsString || returnIsRefType || returnIsReceiveArray || hasOutNeedsCleanup || hasReceiveArray || returnIsComplexStruct || hasNonBlittablePassArray;
        if (needsTryFinally) { w.Write("        try\n        {\n"); }

        string indent = needsTryFinally ? "            " : "        ";
        // First, marshal string params to local void* vars.
        for (int i = 0; i < sig.Params.Count; i++)
        {
            if (IsString(sig.Params[i].Type))
            {
                string callName = GetParamName(sig.Params[i], paramNameOverride);
                string localName = GetParamLocalName(sig.Params[i], paramNameOverride);
                w.Write(indent);
                w.Write("__");
                w.Write(localName);
                w.Write(" = HStringMarshaller.ConvertToUnmanaged(");
                w.Write(callName);
                w.Write(");\n");
            }
        }

        // For PassArray params, open a fixed block (one per param). The function pointer call
        // happens inside the innermost fixed block. Track nesting for indentation.
        // Also for Ref (in T) params, we need a fixed block to pin and pass the pointer.
        int fixedNesting = 0;
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat == ParamCategory.PassArray || cat == ParamCategory.FillArray)
            {
                string callName = GetParamName(p, paramNameOverride);
                string localName = GetParamLocalName(p, paramNameOverride);
                AsmResolver.DotNet.Signatures.TypeSignature elemT = ((AsmResolver.DotNet.Signatures.SzArrayTypeSignature)p.Type).BaseType;
                bool isBlittableElem = IsBlittablePrimitive(elemT) || IsAnyStruct(elemT);
                bool isStringElem = IsString(elemT);
                w.Write(indent);
                w.Write(new string(' ', fixedNesting * 4));
                w.Write("fixed(void* _");
                w.Write(localName);
                w.Write(" = ");
                if (isBlittableElem)
                {
                    w.Write(callName);
                }
                else
                {
                    w.Write("__");
                    w.Write(localName);
                    w.Write("_span");
                }
                if (isStringElem)
                {
                    w.Write(", _");
                    w.Write(localName);
                    w.Write("_inlineHeaderArray = __");
                    w.Write(localName);
                    w.Write("_headerSpan");
                }
                w.Write(")\n");
                w.Write(indent);
                w.Write(new string(' ', fixedNesting * 4));
                w.Write("{\n");
                fixedNesting++;
            }
            else if (cat == ParamCategory.Ref)
            {
                string callName = GetParamName(p, paramNameOverride);
                string localName = GetParamLocalName(p, paramNameOverride);
                AsmResolver.DotNet.Signatures.TypeSignature uRef = StripByRefAndCustomModifiers(p.Type);
                string abiType = IsAnyStruct(uRef) ? GetBlittableStructAbiType(w, uRef) : GetAbiPrimitiveType(uRef);
                w.Write(indent);
                w.Write(new string(' ', fixedNesting * 4));
                w.Write("fixed(");
                w.Write(abiType);
                w.Write("* _");
                w.Write(localName);
                w.Write(" = &");
                w.Write(callName);
                w.Write(")\n");
                w.Write(indent);
                w.Write(new string(' ', fixedNesting * 4));
                w.Write("{\n");
                fixedNesting++;
            }
        }

        string callIndent = indent + new string(' ', fixedNesting * 4);

        // For non-blittable PassArray params, emit CopyToUnmanaged_<name> (UnsafeAccessor) and call
        // it to populate the inline/pooled storage from the user-supplied span. For string arrays,
        // use HStringArrayMarshaller.ConvertToUnmanagedUnsafe instead.
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.PassArray && cat != ParamCategory.FillArray) { continue; }
            if (p.Type is not AsmResolver.DotNet.Signatures.SzArrayTypeSignature szArr) { continue; }
            if (IsBlittablePrimitive(szArr.BaseType) || IsAnyStruct(szArr.BaseType)) { continue; }
            string callName = GetParamName(p, paramNameOverride);
            string localName = GetParamLocalName(p, paramNameOverride);
            if (IsString(szArr.BaseType))
            {
                w.Write(callIndent);
                w.Write("HStringArrayMarshaller.ConvertToUnmanagedUnsafe(\n");
                w.Write(callIndent);
                w.Write("    source: ");
                w.Write(callName);
                w.Write(",\n");
                w.Write(callIndent);
                w.Write("    hstringHeaders: (HStringHeader*) _");
                w.Write(localName);
                w.Write("_inlineHeaderArray,\n");
                w.Write(callIndent);
                w.Write("    hstrings: __");
                w.Write(localName);
                w.Write("_span,\n");
                w.Write(callIndent);
                w.Write("    pinnedGCHandles: __");
                w.Write(localName);
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
                w.Write(localName);
                w.Write("([UnsafeAccessorType(\"");
                w.Write(GetArrayMarshallerInteropPath(w, szArr.BaseType, elementInteropArg));
                w.Write("\")] object _, ReadOnlySpan<");
                w.Write(elementProjected);
                w.Write("> span, uint length, void** data);\n");
                w.Write(callIndent);
                w.Write("CopyToUnmanaged_");
                w.Write(localName);
                w.Write("(null, ");
                w.Write(callName);
                w.Write(", (uint)");
                w.Write(callName);
                w.Write(".Length, (void**)_");
                w.Write(localName);
                w.Write(");\n");
            }
        }

        w.Write(callIndent);
        w.Write("RestrictedErrorInfo.ThrowExceptionForHR((*(delegate* unmanaged[MemberFunction]<");
        w.Write(fp.ToString());
        w.Write(">**)ThisPtr)[");
        w.Write(slot);
        w.Write("](ThisPtr");
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat == ParamCategory.PassArray || cat == ParamCategory.FillArray)
            {
                string callName = GetParamName(p, paramNameOverride);
                string localName = GetParamLocalName(p, paramNameOverride);
                w.Write(", (uint)");
                w.Write(callName);
                w.Write(".Length, _");
                w.Write(localName);
                continue;
            }
            if (cat == ParamCategory.Out)
            {
                string localName = GetParamLocalName(p, paramNameOverride);
                w.Write(", &__");
                w.Write(localName);
                continue;
            }
            if (cat == ParamCategory.ReceiveArray)
            {
                string localName = GetParamLocalName(p, paramNameOverride);
                w.Write(", &__");
                w.Write(localName);
                w.Write("_length, &__");
                w.Write(localName);
                w.Write("_data");
                continue;
            }
            if (cat == ParamCategory.Ref)
            {
                // 'in T' projected param: pass the pinned pointer.
                string localName = GetParamLocalName(p, paramNameOverride);
                w.Write(", _");
                w.Write(localName);
                continue;
            }
            w.Write(", ");
            if (IsHResultException(p.Type))
            {
                w.Write("__");
                w.Write(GetParamLocalName(p, paramNameOverride));
            }
            else if (IsString(p.Type))
            {
                w.Write("__");
                w.Write(GetParamLocalName(p, paramNameOverride));
            }
            else if (IsRuntimeClassOrInterface(p.Type) || IsObject(p.Type) || IsGenericInstance(p.Type))
            {
                w.Write("__");
                w.Write(GetParamLocalName(p, paramNameOverride));
                w.Write(".GetThisPtrUnsafe()");
            }
            else if (IsAnyStruct(p.Type))
            {
                w.Write(GetParamName(p, paramNameOverride));
            }
            else
            {
                EmitParamArgConversion(w, p, paramNameOverride);
            }
        }
        if (returnIsReceiveArray)
        {
            w.Write(", &__retval_length, &__retval_data");
        }
        else if (rt is not null)
        {
            w.Write(", &__retval");
        }
        w.Write("));\n");

        // After call: write back Out params to caller's 'out' var.
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.Out) { continue; }
            string callName = GetParamName(p, paramNameOverride);
            string localName = GetParamLocalName(p, paramNameOverride);
            AsmResolver.DotNet.Signatures.TypeSignature uOut = StripByRefAndCustomModifiers(p.Type);
            w.Write(callIndent);
            w.Write(callName);
            w.Write(" = ");
            if (IsString(uOut))
            {
                w.Write("HStringMarshaller.ConvertToManaged(__");
                w.Write(localName);
                w.Write(")");
            }
            else if (IsObject(uOut))
            {
                w.Write("WindowsRuntimeObjectMarshaller.ConvertToManaged(__");
                w.Write(localName);
                w.Write(")");
            }
            else if (IsRuntimeClassOrInterface(uOut))
            {
                w.Write(GetMarshallerFullName(w, uOut));
                w.Write(".ConvertToManaged(__");
                w.Write(localName);
                w.Write(")");
            }
            else if (IsAnyStruct(uOut))
            {
                w.Write("__");
                w.Write(localName);
            }
            else if (uOut is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlibBool && corlibBool.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Boolean)
            {
                w.Write("__");
                w.Write(localName);
            }
            else if (uOut is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlibChar && corlibChar.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Char)
            {
                w.Write("__");
                w.Write(localName);
            }
            else if (IsEnumType(uOut))
            {
                // Enum out param: __<name> local is already the projected enum type (since the
                // function pointer signature uses the projected type). No cast needed.
                w.Write("__");
                w.Write(localName);
            }
            else
            {
                w.Write("__");
                w.Write(localName);
            }
            w.Write(";\n");
        }

        // Writeback for ReceiveArray params: emit a UnsafeAccessor + assign to the out param.
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.ReceiveArray) { continue; }
            string callName = GetParamName(p, paramNameOverride);
            string localName = GetParamLocalName(p, paramNameOverride);
            AsmResolver.DotNet.Signatures.SzArrayTypeSignature sza = (AsmResolver.DotNet.Signatures.SzArrayTypeSignature)StripByRefAndCustomModifiers(p.Type);
            string elementProjected = w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteProjectionType(w, TypeSemanticsFactory.Get(sza.BaseType))));
            string elementAbi = IsAnyStruct(sza.BaseType)
                ? GetBlittableStructAbiType(w, sza.BaseType)
                : GetAbiPrimitiveType(sza.BaseType);
            string elementInteropArg = EncodeInteropTypeName(sza.BaseType, TypedefNameType.Projected);
            w.Write(callIndent);
            w.Write("[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"ConvertToManaged\")]\n");
            w.Write(callIndent);
            w.Write("static extern ");
            w.Write(elementProjected);
            w.Write("[] ConvertToManaged_");
            w.Write(localName);
            w.Write("([UnsafeAccessorType(\"ABI.System.<");
            w.Write(elementInteropArg);
            w.Write(">ArrayMarshaller, WinRT.Interop\")] object _, uint length, ");
            w.Write(elementAbi);
            w.Write("* data);\n");
            w.Write(callIndent);
            w.Write(callName);
            w.Write(" = ConvertToManaged_");
            w.Write(localName);
            w.Write("(null, __");
            w.Write(localName);
            w.Write("_length, __");
            w.Write(localName);
            w.Write("_data);\n");
        }
        if (rt is not null)
        {
            if (returnIsReceiveArray)
            {
                AsmResolver.DotNet.Signatures.SzArrayTypeSignature retSz = (AsmResolver.DotNet.Signatures.SzArrayTypeSignature)rt;
                string elementProjected = w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteProjectionType(w, TypeSemanticsFactory.Get(retSz.BaseType))));
                string elementAbi = IsString(retSz.BaseType) || IsRuntimeClassOrInterface(retSz.BaseType) || IsObject(retSz.BaseType)
                    ? "void*"
                    : IsAnyStruct(retSz.BaseType)
                        ? GetBlittableStructAbiType(w, retSz.BaseType)
                        : GetAbiPrimitiveType(retSz.BaseType);
                string elementInteropArg = EncodeInteropTypeName(retSz.BaseType, TypedefNameType.Projected);
                w.Write(callIndent);
                w.Write("[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"ConvertToManaged\")]\n");
                w.Write(callIndent);
                w.Write("static extern ");
                w.Write(elementProjected);
                w.Write("[] ConvertToManaged_retval([UnsafeAccessorType(\"");
                w.Write(GetArrayMarshallerInteropPath(w, retSz.BaseType, elementInteropArg));
                w.Write("\")] object _, uint length, ");
                w.Write(elementAbi);
                w.Write("* data);\n");
                w.Write(callIndent);
                w.Write("return ConvertToManaged_retval(null, __retval_length, __retval_data);\n");
            }
            else if (returnIsHResultException)
            {
                w.Write(callIndent);
                w.Write("return global::ABI.System.ExceptionMarshaller.ConvertToManaged(__retval);\n");
            }
            else if (returnIsString)
            {
                w.Write(callIndent);
                w.Write("return HStringMarshaller.ConvertToManaged(__retval);\n");
            }
            else if (returnIsRefType)
            {
                if (IsGenericInstance(rt))
                {
                    string interopTypeName = EncodeInteropTypeName(rt, TypedefNameType.ABI) + ", WinRT.Interop";
                    string projectedTypeName = w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteProjectedSignature(w, rt, false)));
                    w.Write(callIndent);
                    w.Write("[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"ConvertToManaged\")]\n");
                    w.Write(callIndent);
                    w.Write("static extern ");
                    w.Write(projectedTypeName);
                    w.Write(" ConvertToManaged_retval([UnsafeAccessorType(\"");
                    w.Write(interopTypeName);
                    w.Write("\")] object _, void* value);\n");
                    w.Write(callIndent);
                    w.Write("return ConvertToManaged_retval(null, __retval);\n");
                }
                else
                {
                    w.Write(callIndent);
                    w.Write("return ");
                    EmitMarshallerConvertToManaged(w, rt, "__retval");
                    w.Write(";\n");
                }
            }
            else if (returnIsAnyStruct)
            {
                w.Write(callIndent);
                w.Write("return __retval;\n");
            }
            else if (returnIsComplexStruct)
            {
                w.Write(callIndent);
                w.Write("return ");
                w.Write(GetMarshallerFullName(w, rt));
                w.Write(".ConvertToManaged(__retval);\n");
            }
            else
            {
                w.Write(callIndent);
                w.Write("return ");
                string projected = w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteProjectedSignature(w, rt, false)));
                string abiType = GetAbiPrimitiveType(rt);
                if (projected == abiType) { w.Write("__retval;\n"); }
                else
                {
                    w.Write("(");
                    w.Write(projected);
                    w.Write(")__retval;\n");
                }
            }
        }

        // Close fixed blocks (innermost first).
        for (int i = fixedNesting - 1; i >= 0; i--)
        {
            w.Write(indent);
            w.Write(new string(' ', i * 4));
            w.Write("}\n");
        }

        if (needsTryFinally)
        {
            w.Write("        }\n        finally\n        {\n");
            // Free string params (input)
            for (int i = 0; i < sig.Params.Count; i++)
            {
                if (IsString(sig.Params[i].Type))
                {
                    string localName = GetParamLocalName(sig.Params[i], paramNameOverride);
                    w.Write("            HStringMarshaller.Free(__");
                    w.Write(localName);
                    w.Write(");\n");
                }
            }
            // Free Out string/object/runtime-class params.
            for (int i = 0; i < sig.Params.Count; i++)
            {
                ParamInfo p = sig.Params[i];
                ParamCategory cat = ParamHelpers.GetParamCategory(p);
                if (cat != ParamCategory.Out) { continue; }
                AsmResolver.DotNet.Signatures.TypeSignature uOut = StripByRefAndCustomModifiers(p.Type);
                string localName = GetParamLocalName(p, paramNameOverride);
                if (IsString(uOut))
                {
                    w.Write("            HStringMarshaller.Free(__");
                    w.Write(localName);
                    w.Write(");\n");
                }
                else if (IsObject(uOut) || IsRuntimeClassOrInterface(uOut))
                {
                    w.Write("            WindowsRuntimeUnknownMarshaller.Free(__");
                    w.Write(localName);
                    w.Write(");\n");
                }
            }
            // Free string return
            if (returnIsString)
            {
                w.Write("            HStringMarshaller.Free(__retval);\n");
            }
            // Free runtime class / object return
            if (returnIsRefType)
            {
                w.Write("            WindowsRuntimeUnknownMarshaller.Free(__retval);\n");
            }
            // Dispose complex struct return via Marshaller.Dispose.
            if (returnIsComplexStruct)
            {
                w.Write("            ");
                w.Write(GetMarshallerFullName(w, rt!));
                w.Write(".Dispose(__retval);\n");
            }
            // Free receive-array return via UnsafeAccessor.
            if (returnIsReceiveArray)
            {
                AsmResolver.DotNet.Signatures.SzArrayTypeSignature retSz = (AsmResolver.DotNet.Signatures.SzArrayTypeSignature)rt!;
                string elementAbi = IsString(retSz.BaseType) || IsRuntimeClassOrInterface(retSz.BaseType) || IsObject(retSz.BaseType)
                    ? "void*"
                    : IsAnyStruct(retSz.BaseType)
                        ? GetBlittableStructAbiType(w, retSz.BaseType)
                        : GetAbiPrimitiveType(retSz.BaseType);
                string elementInteropArg = EncodeInteropTypeName(retSz.BaseType, TypedefNameType.Projected);
                w.Write("            [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"Free\")]\n");
                w.Write("            static extern void Free_retval([UnsafeAccessorType(\"");
                w.Write(GetArrayMarshallerInteropPath(w, retSz.BaseType, elementInteropArg));
                w.Write("\")] object _, uint length, ");
                w.Write(elementAbi);
                w.Write("* data);\n");
                w.Write("            Free_retval(null, __retval_length, __retval_data);\n");
            }
            // Free ReceiveArray params via UnsafeAccessor.
            for (int i = 0; i < sig.Params.Count; i++)
            {
                ParamInfo p = sig.Params[i];
                ParamCategory cat = ParamHelpers.GetParamCategory(p);
                if (cat != ParamCategory.ReceiveArray) { continue; }
                string localName = GetParamLocalName(p, paramNameOverride);
                AsmResolver.DotNet.Signatures.SzArrayTypeSignature sza = (AsmResolver.DotNet.Signatures.SzArrayTypeSignature)StripByRefAndCustomModifiers(p.Type);
                string elementAbi = IsAnyStruct(sza.BaseType)
                    ? GetBlittableStructAbiType(w, sza.BaseType)
                    : GetAbiPrimitiveType(sza.BaseType);
                string elementInteropArg = EncodeInteropTypeName(sza.BaseType, TypedefNameType.Projected);
                w.Write("\n            [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"Free\")]\n");
                w.Write("            static extern void Free_");
                w.Write(localName);
                w.Write("([UnsafeAccessorType(\"ABI.System.<");
                w.Write(elementInteropArg);
                w.Write(">ArrayMarshaller, WinRT.Interop\")] object _, uint length, ");
                w.Write(elementAbi);
                w.Write("* data);\n\n");
                w.Write("            Free_");
                w.Write(localName);
                w.Write("(null, __");
                w.Write(localName);
                w.Write("_length, __");
                w.Write(localName);
                w.Write("_data);\n");
            }
            // Cleanup non-blittable PassArray/FillArray params:
            // For strings: HStringArrayMarshaller.Dispose + return ArrayPools (3 of them).
            // For runtime classes/objects: Dispose_<name> (UnsafeAccessor) + return ArrayPool.
            for (int i = 0; i < sig.Params.Count; i++)
            {
                ParamInfo p = sig.Params[i];
                ParamCategory cat = ParamHelpers.GetParamCategory(p);
                if (cat != ParamCategory.PassArray && cat != ParamCategory.FillArray) { continue; }
                if (p.Type is not AsmResolver.DotNet.Signatures.SzArrayTypeSignature szArr) { continue; }
                if (IsBlittablePrimitive(szArr.BaseType) || IsAnyStruct(szArr.BaseType)) { continue; }
                string localName = GetParamLocalName(p, paramNameOverride);
                if (IsString(szArr.BaseType))
                {
                    w.Write("\n            HStringArrayMarshaller.Dispose(__");
                    w.Write(localName);
                    w.Write("_pinnedHandleSpan);\n\n");
                    w.Write("            if (__");
                    w.Write(localName);
                    w.Write("_pinnedHandleArrayFromPool is not null)\n            {\n");
                    w.Write("                global::System.Buffers.ArrayPool<nint>.Shared.Return(__");
                    w.Write(localName);
                    w.Write("_pinnedHandleArrayFromPool);\n            }\n\n");
                    w.Write("            if (__");
                    w.Write(localName);
                    w.Write("_headerArrayFromPool is not null)\n            {\n");
                    w.Write("                global::System.Buffers.ArrayPool<HStringHeader>.Shared.Return(__");
                    w.Write(localName);
                    w.Write("_headerArrayFromPool);\n            }\n");
                }
                else
                {
                    string elementInteropArg = EncodeInteropTypeName(szArr.BaseType, TypedefNameType.Projected);
                    w.Write("\n            [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"Dispose\")]\n");
                    w.Write("            static extern void Dispose_");
                    w.Write(localName);
                    w.Write("([UnsafeAccessorType(\"");
                    w.Write(GetArrayMarshallerInteropPath(w, szArr.BaseType, elementInteropArg));
                    w.Write("\")] object _, uint length, void** data);\n\n");
                    w.Write("            fixed(void* _");
                    w.Write(localName);
                    w.Write(" = __");
                    w.Write(localName);
                    w.Write("_span)\n            {\n");
                    w.Write("                Dispose_");
                    w.Write(localName);
                    w.Write("(null, (uint) __");
                    w.Write(localName);
                    w.Write("_span.Length, (void**)_");
                    w.Write(localName);
                    w.Write(");\n            }\n");
                }
                w.Write("\n            if (__");
                w.Write(localName);
                w.Write("_arrayFromPool is not null)\n            {\n");
                w.Write("                global::System.Buffers.ArrayPool<nint>.Shared.Return(__");
                w.Write(localName);
                w.Write("_arrayFromPool);\n            }\n");
            }
            w.Write("        }\n");
        }

        w.Write("    }\n");
    }

    /// <summary>True if the type signature is a Nullable&lt;T&gt; where T is a primitive
    /// supported by an ABI.System.&lt;T&gt;Marshaller (e.g. UInt64Marshaller, Int32Marshaller, etc.).
    /// Returns the fully-qualified marshaller name in <paramref name="marshallerName"/>.</summary>
    private static bool TryGetNullablePrimitiveMarshallerName(AsmResolver.DotNet.Signatures.TypeSignature sig, out string? marshallerName)
    {
        marshallerName = null;
        if (sig is not AsmResolver.DotNet.Signatures.GenericInstanceTypeSignature gi) { return false; }
        var gt = gi.GenericType;
        string ns = gt?.Namespace?.Value ?? string.Empty;
        string name = gt?.Name?.Value ?? string.Empty;
        // In WinMD metadata, Nullable<T> is encoded as Windows.Foundation.IReference<T>.
        // It only later gets projected to System.Nullable<T> by the projection layer.
        bool isNullable = (ns == "System" && name == "Nullable`1")
            || (ns == "Windows.Foundation" && name == "IReference`1");
        if (!isNullable) { return false; }
        if (gi.TypeArguments.Count != 1) { return false; }
        AsmResolver.DotNet.Signatures.TypeSignature arg = gi.TypeArguments[0];
        // Map primitive corlib element type to its ABI marshaller name.
        if (arg is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlib)
        {
            string? mn = corlib.ElementType switch
            {
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Boolean => "Boolean",
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Char => "Char",
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.I1 => "SByte",
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.U1 => "Byte",
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.I2 => "Int16",
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.U2 => "UInt16",
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.I4 => "Int32",
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.U4 => "UInt32",
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.I8 => "Int64",
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.U8 => "UInt64",
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.R4 => "Single",
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.R8 => "Double",
                _ => null
            };
            if (mn is null) { return false; }
            marshallerName = "ABI.System." + mn + "Marshaller";
            return true;
        }
        return false;
    }

    /// <summary>True if the type signature represents the System.Object root type.</summary>
    private static bool IsObject(AsmResolver.DotNet.Signatures.TypeSignature sig)
    {
        return sig is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlib &&
               corlib.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Object;
    }

    /// <summary>True if the type signature represents Windows.Foundation.HResult / System.Exception
    /// (special-cased: ABI is global::ABI.System.Exception (an HResult struct), projected is Exception,
    /// requires custom marshalling via ABI.System.ExceptionMarshaller).</summary>
    private static bool IsHResultException(AsmResolver.DotNet.Signatures.TypeSignature sig)
    {
        if (sig is not AsmResolver.DotNet.Signatures.TypeDefOrRefSignature td) { return false; }
        string ns = td.Type?.Namespace?.Value ?? string.Empty;
        string name = td.Type?.Name?.Value ?? string.Empty;
        return (ns == "System" && name == "Exception")
            || (ns == "Windows.Foundation" && name == "HResult");
    }

    /// <summary>
    /// True if the type is a mapped value type that requires marshalling between projected and ABI
    /// representations (e.g. Windows.Foundation.DateTime &lt;-&gt; System.DateTimeOffset,
    /// Windows.Foundation.TimeSpan &lt;-&gt; System.TimeSpan, Windows.Foundation.HResult &lt;-&gt; System.Exception).
    /// These types use 'global::ABI.&lt;MappedNamespace&gt;.&lt;MappedName&gt;' as their ABI representation
    /// and need an explicit marshaller call ('global::ABI.&lt;MappedNamespace&gt;.&lt;MappedName&gt;Marshaller.ConvertToUnmanaged'/
    /// 'ConvertToManaged') to convert values across the boundary.
    /// </summary>
    private static bool IsMappedMarshalingValueType(AsmResolver.DotNet.Signatures.TypeSignature sig, out string mappedNs, out string mappedName)
    {
        mappedNs = string.Empty;
        mappedName = string.Empty;
        AsmResolver.DotNet.ITypeDefOrRef? td = null;
        if (sig is AsmResolver.DotNet.Signatures.TypeDefOrRefSignature tds) { td = tds.Type; }
        if (td is null) { return false; }
        string ns = td.Namespace?.Value ?? string.Empty;
        string name = td.Name?.Value ?? string.Empty;
        // The set of mapped types that use the 'value-type marshaller' pattern (DateTime, TimeSpan, HResult).
        // Uri is also a mapped marshalling type but it's a reference type (handled via UriMarshaller separately).
        if (ns == "Windows.Foundation")
        {
            if (name == "DateTime") { mappedNs = "System"; mappedName = "DateTimeOffset"; return true; }
            if (name == "TimeSpan") { mappedNs = "System"; mappedName = "TimeSpan"; return true; }
            if (name == "HResult") { mappedNs = "System"; mappedName = "Exception"; return true; }
        }
        return false;
    }

    /// <summary>True if the type is a mapped value type that needs ABI marshalling (excluding HResult, handled separately).</summary>
    private static bool IsMappedAbiValueType(AsmResolver.DotNet.Signatures.TypeSignature sig)
    {
        if (!IsMappedMarshalingValueType(sig, out _, out string mappedName)) { return false; }
        // HResult/Exception is treated specially in many places; this helper is for DateTime/TimeSpan only.
        return mappedName != "Exception";
    }

    /// <summary>Returns the ABI type name for a mapped value type (e.g. 'global::ABI.System.TimeSpan').</summary>
    private static string GetMappedAbiTypeName(AsmResolver.DotNet.Signatures.TypeSignature sig)
    {
        if (!IsMappedMarshalingValueType(sig, out string ns, out string name)) { return string.Empty; }
        return "global::ABI." + ns + "." + name;
    }

    /// <summary>Returns the marshaller class name for a mapped value type (e.g. 'global::ABI.System.TimeSpanMarshaller').</summary>
    private static string GetMappedMarshallerName(AsmResolver.DotNet.Signatures.TypeSignature sig)
    {
        if (!IsMappedMarshalingValueType(sig, out string ns, out string name)) { return string.Empty; }
        return "global::ABI." + ns + "." + name + "Marshaller";
    }

    /// <summary>True if the type signature represents an enum (resolves cross-module typerefs).</summary>
    private static bool IsEnumType(AsmResolver.DotNet.Signatures.TypeSignature sig)
    {
        if (sig is not AsmResolver.DotNet.Signatures.TypeDefOrRefSignature td) { return false; }
        if (td.Type is TypeDefinition def)
        {
            return TypeCategorization.GetCategory(def) == TypeCategory.Enum;
        }
        if (td.Type is TypeReference tr && _cacheRef is not null)
        {
            string ns = tr.Namespace?.Value ?? string.Empty;
            string name = tr.Name?.Value ?? string.Empty;
            TypeDefinition? resolved = _cacheRef.Find(ns + "." + name);
            return resolved is not null && TypeCategorization.GetCategory(resolved) == TypeCategory.Enum;
        }
        return false;
    }

    /// <summary>True if the type signature represents a generic instantiation that needs WinRT.Interop UnsafeAccessor marshalling.</summary>
    private static bool IsGenericInstance(AsmResolver.DotNet.Signatures.TypeSignature sig)
    {
        return sig is AsmResolver.DotNet.Signatures.GenericInstanceTypeSignature;
    }

    /// <summary>Strips <c>ByReferenceTypeSignature</c> and <c>CustomModifierTypeSignature</c> wrappers
    /// to get the underlying type signature.</summary>
    private static AsmResolver.DotNet.Signatures.TypeSignature StripByRefAndCustomModifiers(AsmResolver.DotNet.Signatures.TypeSignature sig)
    {
        AsmResolver.DotNet.Signatures.TypeSignature current = sig;
        while (true)
        {
            if (current is AsmResolver.DotNet.Signatures.ByReferenceTypeSignature br) { current = br.BaseType; continue; }
            if (current is AsmResolver.DotNet.Signatures.CustomModifierTypeSignature cm) { current = cm.BaseType; continue; }
            return current;
        }
    }

    /// <summary>True if the type signature represents a WinRT runtime class, interface, or delegate (reference type marshallable via *Marshaller).</summary>
    private static bool IsRuntimeClassOrInterface(AsmResolver.DotNet.Signatures.TypeSignature sig)
    {
        if (sig is AsmResolver.DotNet.Signatures.TypeDefOrRefSignature td)
        {
            // Same-module: use the resolved category directly.
            if (td.Type is TypeDefinition def)
            {
                TypeCategory cat = TypeCategorization.GetCategory(def);
                return cat is TypeCategory.Class or TypeCategory.Interface or TypeCategory.Delegate;
            }
            // Cross-module typeref: try to resolve via the metadata cache to check category.
            string ns = td.Type?.Namespace?.Value ?? string.Empty;
            string name = td.Type?.Name?.Value ?? string.Empty;
            if (ns == "System")
            {
                return name switch
                {
                    "Uri" or "Type" or "IDisposable" or "Exception" => true,
                    _ => false,
                };
            }
            if (_cacheRef is not null)
            {
                TypeDefinition? resolved = _cacheRef.Find(ns + "." + name);
                if (resolved is not null)
                {
                    TypeCategory cat = TypeCategorization.GetCategory(resolved);
                    return cat is TypeCategory.Class or TypeCategory.Interface or TypeCategory.Delegate;
                }
            }
            return false;
        }
        return false;
    }

    /// <summary>Emits the call to the appropriate marshaller's ConvertToUnmanaged for a runtime class / object input parameter.</summary>
    private static void EmitMarshallerConvertToUnmanaged(TypeWriter w, AsmResolver.DotNet.Signatures.TypeSignature sig, string argName)
    {
        if (IsObject(sig))
        {
            w.Write("WindowsRuntimeObjectMarshaller.ConvertToUnmanaged(");
            w.Write(argName);
            w.Write(")");
            return;
        }
        // Runtime class / interface: use ABI.<NS>.<Name>Marshaller
        w.Write(GetMarshallerFullName(w, sig));
        w.Write(".ConvertToUnmanaged(");
        w.Write(argName);
        w.Write(")");
    }

    /// <summary>Emits the call to the appropriate marshaller's ConvertToManaged for a runtime class / object return value.</summary>
    private static void EmitMarshallerConvertToManaged(TypeWriter w, AsmResolver.DotNet.Signatures.TypeSignature sig, string argName)
    {
        if (IsObject(sig))
        {
            w.Write("WindowsRuntimeObjectMarshaller.ConvertToManaged(");
            w.Write(argName);
            w.Write(")");
            return;
        }
        w.Write(GetMarshallerFullName(w, sig));
        w.Write(".ConvertToManaged(");
        w.Write(argName);
        w.Write(")");
    }

    /// <summary>Returns the full marshaller name (e.g. <c>global::ABI.Windows.Foundation.UriMarshaller</c>).</summary>
    private static string GetMarshallerFullName(TypeWriter w, AsmResolver.DotNet.Signatures.TypeSignature sig)
    {
        if (sig is AsmResolver.DotNet.Signatures.TypeDefOrRefSignature td)
        {
            string ns = td.Type?.Namespace?.Value ?? string.Empty;
            string name = td.Type?.Name?.Value ?? string.Empty;
            // Apply mapped type remapping (e.g. System.Uri -> Windows.Foundation.Uri)
            MappedType? mapped = MappedTypes.Get(ns, name);
            if (mapped is not null)
            {
                ns = mapped.MappedNamespace;
                name = mapped.MappedName;
            }
            return "global::ABI." + ns + "." + Helpers.StripBackticks(name) + "Marshaller";
        }
        return "global::ABI.Object.Marshaller";
    }

    private static string GetParamName(ParamInfo p, string? paramNameOverride)
    {
        string name = paramNameOverride ?? p.Parameter.Name ?? "param";
        return Helpers.IsKeyword(name) ? "@" + name : name;
    }

    private static string GetParamLocalName(ParamInfo p, string? paramNameOverride)
    {
        // For local helper variables (e.g. __<name>), strip the @ escape since `__event` is valid.
        return paramNameOverride ?? p.Parameter.Name ?? "param";
    }

    private static bool IsString(AsmResolver.DotNet.Signatures.TypeSignature sig)
    {
        return sig is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlib &&
               corlib.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.String;
    }

    /// <summary>Emits the conversion of a parameter from its projected (managed) form to the ABI argument form.</summary>
    private static void EmitParamArgConversion(TypeWriter w, ParamInfo p, string? paramNameOverride = null)
    {
        string pname = paramNameOverride ?? p.Parameter.Name ?? "param";
        // bool: ABI is 'bool' directly; pass as-is.
        if (p.Type is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlib &&
            corlib.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Boolean)
        {
            w.Write(pname);
        }
        // char: ABI is 'char' directly; pass as-is.
        else if (p.Type is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlib2 &&
                 corlib2.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Char)
        {
            w.Write(pname);
        }
        // Enums: function pointer signature uses the projected enum type, so pass directly.
        else if (IsEnumType(p.Type))
        {
            w.Write(pname);
        }
        else
        {
            w.Write(pname);
        }
    }

    /// <summary>True if the type is a blittable primitive (or enum) directly representable
    /// at the ABI: bool/byte/sbyte/short/ushort/int/uint/long/ulong/float/double/char and enums.</summary>
    private static bool IsBlittablePrimitive(AsmResolver.DotNet.Signatures.TypeSignature sig)
    {
        if (sig is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlib)
        {
            return corlib.ElementType is
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Boolean or
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.I1 or
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.U1 or
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.I2 or
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.U2 or
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.I4 or
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.U4 or
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.I8 or
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.U8 or
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.R4 or
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.R8 or
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Char;
        }
        // Enum (TypeDefOrRef-based value type with non-Object base) - same module or cross-module
        if (sig is AsmResolver.DotNet.Signatures.TypeDefOrRefSignature td)
        {
            if (td.Type is TypeDefinition def && TypeCategorization.GetCategory(def) == TypeCategory.Enum)
            {
                return true;
            }
            // Cross-module enum: try to resolve via the metadata cache.
            if (td.Type is TypeReference tr && _cacheRef is not null)
            {
                string ns = tr.Namespace?.Value ?? string.Empty;
                string name = tr.Name?.Value ?? string.Empty;
                TypeDefinition? resolved = _cacheRef.Find(ns + "." + name);
                if (resolved is not null && TypeCategorization.GetCategory(resolved) == TypeCategory.Enum)
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>True if the type is a blittable struct (TypeDef with all blittable fields, no enum).
    /// These types have an identical ABI representation to their projected form.</summary>
    private static bool IsBlittableStruct(AsmResolver.DotNet.Signatures.TypeSignature sig)
    {
        if (sig is not AsmResolver.DotNet.Signatures.TypeDefOrRefSignature td) { return false; }
        TypeDefinition? def = td.Type as TypeDefinition;
        if (def is null && _cacheRef is not null && td.Type is TypeReference tr)
        {
            string ns = tr.Namespace?.Value ?? string.Empty;
            string name = tr.Name?.Value ?? string.Empty;
            // Well-known cross-assembly blittable structs
            if (ns == "System" && name == "Guid") { return true; }
            def = _cacheRef.Find(ns + "." + name);
        }
        if (def is null) { return false; }
        TypeCategory cat = TypeCategorization.GetCategory(def);
        if (cat == TypeCategory.Enum) { return false; }  // handled by IsBlittablePrimitive
        if (cat != TypeCategory.Struct) { return false; }
        return IsTypeBlittable(def);
    }

    /// <summary>True for any struct type that can be passed directly across the WinRT ABI
    /// (no per-field marshalling required). This includes blittable structs and "almost-blittable"
    /// structs that have only primitive fields like bool/char (whose C# layout matches the WinRT ABI).
    /// Excludes structs with reference type fields (string/object/runtime classes/etc.).</summary>
    /// <summary>True for structs that have at least one reference type field (string, generic
    /// instance Nullable&lt;T&gt;, etc.). These need per-field marshalling via the *Marshaller class
    /// (ConvertToUnmanaged/ConvertToManaged/Dispose).</summary>
    private static bool IsComplexStruct(AsmResolver.DotNet.Signatures.TypeSignature sig)
    {
        if (sig is not AsmResolver.DotNet.Signatures.TypeDefOrRefSignature td) { return false; }
        TypeDefinition? def = td.Type as TypeDefinition;
        if (def is null && _cacheRef is not null && td.Type is TypeReference tr)
        {
            string ns = tr.Namespace?.Value ?? string.Empty;
            string name = tr.Name?.Value ?? string.Empty;
            if (ns == "System" && name == "Guid") { return false; }
            def = _cacheRef.Find(ns + "." + name);
        }
        if (def is null) { return false; }
        TypeCategory cat = TypeCategorization.GetCategory(def);
        if (cat != TypeCategory.Struct) { return false; }
        // A struct is "complex" if it has any field that is not a blittable primitive nor an
        // almost-blittable struct (i.e. has a string/object/Nullable<T>/etc. field).
        foreach (FieldDefinition field in def.Fields)
        {
            if (field.IsStatic || field.Signature is null) { continue; }
            AsmResolver.DotNet.Signatures.TypeSignature ft = field.Signature.FieldType;
            if (IsBlittablePrimitive(ft)) { continue; }
            if (IsAnyStruct(ft)) { continue; }
            return true;
        }
        return false;
    }

    private static bool IsAnyStruct(AsmResolver.DotNet.Signatures.TypeSignature sig)
    {
        if (sig is not AsmResolver.DotNet.Signatures.TypeDefOrRefSignature td) { return false; }
        TypeDefinition? def = td.Type as TypeDefinition;
        if (def is null && _cacheRef is not null && td.Type is TypeReference tr)
        {
            string ns = tr.Namespace?.Value ?? string.Empty;
            string name = tr.Name?.Value ?? string.Empty;
            if (ns == "System" && name == "Guid") { return true; }
            def = _cacheRef.Find(ns + "." + name);
        }
        if (def is null) { return false; }
        TypeCategory cat = TypeCategorization.GetCategory(def);
        if (cat != TypeCategory.Struct) { return false; }
        // Reject if any instance field is a reference type (string/object/runtime class/etc.).
        foreach (FieldDefinition field in def.Fields)
        {
            if (field.IsStatic || field.Signature is null) { continue; }
            AsmResolver.DotNet.Signatures.TypeSignature ft = field.Signature.FieldType;
            if (ft is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlibField)
            {
                if (corlibField.ElementType is
                    AsmResolver.PE.DotNet.Metadata.Tables.ElementType.String or
                    AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Object)
                { return false; }
                continue;
            }
            // Recurse: nested struct must also pass IsAnyStruct, otherwise reject.
            if (IsBlittablePrimitive(ft)) { continue; }
            if (IsAnyStruct(ft)) { continue; }
            return false;
        }
        return true;
    }

    /// <summary>Returns the ABI type name for a blittable struct (the projected type name).</summary>
    private static string GetBlittableStructAbiType(TypeWriter w, AsmResolver.DotNet.Signatures.TypeSignature sig)
    {
        return w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteProjectedSignature(w, sig, false)));
    }

    /// <summary>Returns the ABI struct type name for a complex struct (e.g. global::ABI.Windows.Web.Http.HttpProgress).</summary>
    private static string GetAbiStructTypeName(TypeWriter w, AsmResolver.DotNet.Signatures.TypeSignature sig)
    {
        if (sig is AsmResolver.DotNet.Signatures.TypeDefOrRefSignature td)
        {
            string ns = td.Type?.Namespace?.Value ?? string.Empty;
            string name = td.Type?.Name?.Value ?? string.Empty;
            return "global::ABI." + ns + "." + Helpers.StripBackticks(name);
        }
        return "global::ABI.Object";
    }

    private static string GetAbiPrimitiveType(AsmResolver.DotNet.Signatures.TypeSignature sig)
    {
        if (sig is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlib)
        {
            return corlib.ElementType switch
            {
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Boolean => "bool",
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Char => "char",
                _ => GetAbiFundamentalTypeFromCorLib(corlib.ElementType),
            };
        }
        // Enum: use the projected enum type as the ABI signature (truth pattern).
        if (sig is AsmResolver.DotNet.Signatures.TypeDefOrRefSignature td)
        {
            TypeDefinition? def = td.Type as TypeDefinition;
            if (def is null && _cacheRef is not null && td.Type is TypeReference tr)
            {
                string ns = tr.Namespace?.Value ?? string.Empty;
                string name = tr.Name?.Value ?? string.Empty;
                def = _cacheRef.Find(ns + "." + name);
            }
            if (def is not null && TypeCategorization.GetCategory(def) == TypeCategory.Enum)
            {
                return _cacheRef is null ? "int" : GetProjectedEnumName(def);
            }
        }
        return "int";
    }

    private static string GetProjectedEnumName(TypeDefinition def)
    {
        string ns = def.Namespace?.Value ?? string.Empty;
        string name = def.Name?.Value ?? string.Empty;
        return string.IsNullOrEmpty(ns) ? "global::" + name : "global::" + ns + "." + name;
    }

    private static string GetAbiFundamentalTypeFromCorLib(AsmResolver.PE.DotNet.Metadata.Tables.ElementType et)
    {
        return et switch
        {
            AsmResolver.PE.DotNet.Metadata.Tables.ElementType.I1 => "sbyte",
            AsmResolver.PE.DotNet.Metadata.Tables.ElementType.U1 => "byte",
            AsmResolver.PE.DotNet.Metadata.Tables.ElementType.I2 => "short",
            AsmResolver.PE.DotNet.Metadata.Tables.ElementType.U2 => "ushort",
            AsmResolver.PE.DotNet.Metadata.Tables.ElementType.I4 => "int",
            AsmResolver.PE.DotNet.Metadata.Tables.ElementType.U4 => "uint",
            AsmResolver.PE.DotNet.Metadata.Tables.ElementType.I8 => "long",
            AsmResolver.PE.DotNet.Metadata.Tables.ElementType.U8 => "ulong",
            AsmResolver.PE.DotNet.Metadata.Tables.ElementType.R4 => "float",
            AsmResolver.PE.DotNet.Metadata.Tables.ElementType.R8 => "double",
            _ => "int",
        };
    }

    /// <summary>
    /// Writes the IReference&lt;T&gt; implementation for a struct/enum/delegate
    /// (mirrors C++ <c>write_reference_impl</c>).
    /// </summary>
    private static void WriteReferenceImpl(TypeWriter w, TypeDefinition type)
    {
        string name = type.Name?.Value ?? string.Empty;
        string nameStripped = Helpers.StripBackticks(name);
        string visibility = w.Settings.Component ? "public" : "file";
        bool blittable = IsTypeBlittable(type);

        w.Write("\n");
        w.Write(visibility);
        w.Write(" static unsafe class ");
        w.Write(nameStripped);
        w.Write("ReferenceImpl\n{\n");
        w.Write("    [FixedAddressValueType]\n");
        w.Write("    private static readonly ReferenceVftbl Vftbl;\n\n");
        w.Write("    static ");
        w.Write(nameStripped);
        w.Write("ReferenceImpl()\n    {\n");
        w.Write("        *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;\n");
        w.Write("        Vftbl.get_Value = &get_Value;\n");
        w.Write("    }\n\n");
        w.Write("    public static nint Vtable\n    {\n        [MethodImpl(MethodImplOptions.AggressiveInlining)]\n        get => (nint)Unsafe.AsPointer(in Vftbl);\n    }\n\n");
        w.Write("    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]\n");
        if (blittable || TypeCategorization.GetCategory(type) == TypeCategory.Struct)
        {
            // For both blittable and non-blittable structs, the body uses direct memcpy via
            // C# struct assignment. Even bool/char fields work because their managed layout
            // (1 byte / 2 bytes) matches the WinRT ABI.
            w.Write("    public static int get_Value(void* thisPtr, void* result)\n    {\n");
            w.Write("        if (result is null)\n        {\n");
            w.Write("            return unchecked((int)0x80004003);\n        }\n\n");
            w.Write("        try\n        {\n");
            w.Write("            var value = (");
            WriteTypedefName(w, type, TypedefNameType.Projected, true);
            w.Write(")(ComInterfaceDispatch.GetInstance<object>((ComInterfaceDispatch*)thisPtr));\n");
            w.Write("            *(");
            WriteTypedefName(w, type, TypedefNameType.Projected, true);
            w.Write("*)result = value;\n");
            w.Write("            return 0;\n        }\n");
            w.Write("        catch (Exception e)\n        {\n");
            w.Write("            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);\n        }\n");
            w.Write("    }\n");
        }
        else if (TypeCategorization.GetCategory(type) is TypeCategory.Class or TypeCategory.Delegate)
        {
            // Non-blittable runtime class / delegate: marshal via <Name>Marshaller and detach.
            w.Write("    public static int get_Value(void* thisPtr, void* result)\n    {\n");
            w.Write("        if (result is null)\n        {\n");
            w.Write("            return unchecked((int)0x80004003);\n        }\n\n");
            w.Write("        try\n        {\n");
            w.Write("            ");
            WriteTypedefName(w, type, TypedefNameType.Projected, true);
            w.Write(" unboxedValue = (");
            WriteTypedefName(w, type, TypedefNameType.Projected, true);
            w.Write(")ComInterfaceDispatch.GetInstance<object>((ComInterfaceDispatch*)thisPtr);\n");
            w.Write("            void* value = ");
            // Use the same-namespace short marshaller name (we're in the ABI namespace).
            w.Write(nameStripped);
            w.Write("Marshaller.ConvertToUnmanaged(unboxedValue).DetachThisPtrUnsafe();\n");
            w.Write("            *(void**)result = value;\n");
            w.Write("            return 0;\n        }\n");
            w.Write("        catch (Exception e)\n        {\n");
            w.Write("            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);\n        }\n");
            w.Write("    }\n");
        }
        else
        {
            w.Write("    public static int get_Value(void* thisPtr, void* result) => throw null!;\n");
        }
        // IID property: matches C++ write_reference_impl, which appends a 'public static ref readonly Guid IID'
        // property pointing at the reference type's IID (e.g. IID_Windows_AI_Actions_ActionEntityKindReference).
        w.Write("\n    public static ref readonly Guid IID\n    {\n");
        w.Write("        [MethodImpl(MethodImplOptions.AggressiveInlining)]\n");
        w.Write("        get => ref global::ABI.InterfaceIIDs.");
        WriteIidReferenceGuidPropertyName(w, type);
        w.Write(";\n    }\n");
        w.Write("}\n\n");
    }

    /// <summary>Mirrors C++ <c>write_abi_type</c>: writes the ABI type for a type semantics.</summary>
    public static void WriteAbiType(TypeWriter w, TypeSemantics semantics)
    {
        switch (semantics)
        {
            case TypeSemantics.Fundamental f:
                w.Write(GetAbiFundamentalType(f.Type));
                break;
            case TypeSemantics.Object_:
                w.Write("void*");
                break;
            case TypeSemantics.Guid_:
                w.Write("Guid");
                break;
            case TypeSemantics.Type_:
                w.Write("global::WindowsRuntime.InteropServices.WindowsRuntimeTypeName");
                break;
            case TypeSemantics.Definition d:
                if (TypeCategorization.GetCategory(d.Type) is TypeCategory.Enum)
                {
                    // Enums in WinRT ABI use the projected enum type directly (since their C#
                    // layout matches their underlying integer ABI representation 1:1).
                    WriteTypedefName(w, d.Type, TypedefNameType.Projected, true);
                }
                else if (TypeCategorization.GetCategory(d.Type) is TypeCategory.Struct)
                {
                    string dNs = d.Type.Namespace?.Value ?? string.Empty;
                    string dName = d.Type.Name?.Value ?? string.Empty;
                    // Special case: mapped value types that require ABI marshalling
                    // (DateTime/TimeSpan -> ABI.System.DateTimeOffset/TimeSpan).
                    if (dNs == "Windows.Foundation" && dName == "DateTime")
                    {
                        w.Write("global::ABI.System.DateTimeOffset");
                        break;
                    }
                    if (dNs == "Windows.Foundation" && dName == "TimeSpan")
                    {
                        w.Write("global::ABI.System.TimeSpan");
                        break;
                    }
                    if (dNs == "Windows.Foundation" && dName == "HResult")
                    {
                        w.Write("global::ABI.System.Exception");
                        break;
                    }
                    AsmResolver.DotNet.Signatures.TypeSignature dts = d.Type.ToTypeSignature();
                    // "Almost-blittable" structs (with bool/char fields but no reference-type
                    // fields) can pass through using the projected type since the C# layout
                    // matches the WinRT ABI directly. Truly complex structs (with string/object/
                    // Nullable<T> fields) need the ABI struct.
                    if (IsAnyStruct(dts))
                    {
                        WriteTypedefName(w, d.Type, TypedefNameType.Projected, true);
                    }
                    else
                    {
                        WriteTypedefName(w, d.Type, TypedefNameType.ABI, true);
                    }
                }
                else
                {
                    w.Write("void*");
                }
                break;
            case TypeSemantics.Reference r:
                // Cross-module typeref: try resolving the type, applying mapped-type translation
                // for the field/parameter type after resolution.
                if (_cacheRef is not null)
                {
                    string rns = r.Reference_.Namespace?.Value ?? string.Empty;
                    string rname = r.Reference_.Name?.Value ?? string.Empty;
                    // Special case: mapped value types that require ABI marshalling.
                    if (rns == "Windows.Foundation" && rname == "DateTime")
                    {
                        w.Write("global::ABI.System.DateTimeOffset");
                        break;
                    }
                    if (rns == "Windows.Foundation" && rname == "TimeSpan")
                    {
                        w.Write("global::ABI.System.TimeSpan");
                        break;
                    }
                    if (rns == "Windows.Foundation" && rname == "HResult")
                    {
                        w.Write("global::ABI.System.Exception");
                        break;
                    }
                    // Look up the type by its ORIGINAL (unmapped) name in the cache.
                    TypeDefinition? rd = _cacheRef.Find(rns + "." + rname);
                    // If not found, try the mapped name (for cases where the mapping target is in the cache).
                    if (rd is null)
                    {
                        MappedType? rmapped = MappedTypes.Get(rns, rname);
                        if (rmapped is not null)
                        {
                            rd = _cacheRef.Find(rmapped.MappedNamespace + "." + rmapped.MappedName);
                        }
                    }
                    if (rd is not null)
                    {
                        TypeCategory cat = TypeCategorization.GetCategory(rd);
                        if (cat == TypeCategory.Enum)
                        {
                            // Enums use the projected enum type directly (C# layout == ABI layout).
                            WriteTypedefName(w, rd, TypedefNameType.Projected, true);
                            break;
                        }
                        if (cat == TypeCategory.Struct)
                        {
                            // Special case: HResult is mapped to System.Exception (a reference type)
                            // but its ABI representation is the global::ABI.System.Exception struct
                            // (which wraps the underlying HRESULT int).
                            string rdNs = rd.Namespace?.Value ?? string.Empty;
                            string rdName = rd.Name?.Value ?? string.Empty;
                            if (rdNs == "Windows.Foundation" && rdName == "HResult")
                            {
                                w.Write("global::ABI.System.Exception");
                                break;
                            }
                            if (IsAnyStruct(rd.ToTypeSignature()))
                            {
                                WriteTypedefName(w, rd, TypedefNameType.Projected, true);
                            }
                            else
                            {
                                WriteTypedefName(w, rd, TypedefNameType.ABI, true);
                            }
                            break;
                        }
                    }
                }
                w.Write("void*");
                break;
            case TypeSemantics.GenericInstance:
                w.Write("void*");
                break;
            default:
                w.Write("void*");
                break;
        }
    }

    private static string GetAbiFundamentalType(FundamentalType t) => t switch
    {
        FundamentalType.Boolean => "bool",
        FundamentalType.Char => "char",
        FundamentalType.String => "void*",
        _ => FundamentalTypes.ToCSharpType(t)
    };
}
