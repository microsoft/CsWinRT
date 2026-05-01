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
        // For TypeRef/TypeDef, conservatively return false unless we resolve to an enum.
        if (sig is AsmResolver.DotNet.Signatures.TypeDefOrRefSignature todr)
        {
            if (todr.Type is TypeDefinition td)
            {
                return IsTypeBlittable(td);
            }
            // Can't resolve — be conservative.
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
                w.Write("public ");
                WriteAbiType(w, TypeSemanticsFactory.Get(field.Signature.FieldType));
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
        // Minimal: emit the marshaller class (full implementation requires full method-signature
        // marshalling support).
        WriteDelegateMarshallerStub(w, type);
        WriteReferenceImpl(w, type);
    }

    /// <summary>Mirrors C++ <c>write_temp_delegate_event_source_subclass</c>.</summary>
    public static void WriteTempDelegateEventSourceSubclass(TypeWriter w, TypeDefinition type)
    {
        // Minimal stub
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
            // Without resolving the exclusive_to class to check overridable status, we conservatively
            // emit. The full check requires walking the exclusive_to class's interfaces.
            // For simplified port: emit so that the impl is available.
            return true;
        }
        return true;
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
                if (includeParamNames)
                {
                    w.Write("uint ");
                    w.Write("__");
                    w.Write(p.Parameter.Name ?? "param");
                    w.Write("Length, ");
                    WriteAbiType(w, TypeSemanticsFactory.Get(sz.BaseType));
                    w.Write("* ");
                    Helpers.WriteEscapedIdentifier(w, p.Parameter.Name ?? "param");
                }
                else
                {
                    w.Write("uint, ");
                    WriteAbiType(w, TypeSemanticsFactory.Get(sz.BaseType));
                    w.Write("*");
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
            WriteAbiType(w, TypeSemanticsFactory.Get(sig.ReturnType));
            w.Write("*");
            if (includeParamNames) { w.Write(" __retval"); }
        }
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

        // Stubbed Do_Abi_* implementations for all methods - returns S_OK (0)
        foreach (MethodDefinition method in type.Methods)
        {
            string vm = GetVMethodName(type, method);
            MethodSig sig = new(method);
            w.Write("[UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]\n");
            w.Write("private static int Do_Abi_");
            w.Write(vm);
            w.Write("(");
            WriteAbiParameterTypesPointer(w, sig, includeParamNames: true);
            w.Write(") => throw null!;\n\n");
        }
        w.Write("}\n");
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
        // Emit interface members (stub bodies)
        WriteInterfaceMemberSignatures(w, type);
        w.Write("\n}\n");
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
        bool blittable = IsTypeBlittable(type);

        w.Write("public static unsafe class ");
        w.Write(nameStripped);
        w.Write("Marshaller\n{\n");

        if (!blittable)
        {
            // ConvertToUnmanaged/ConvertToManaged/Dispose stubs (full implementations would emit
            // per-field marshalling logic - we emit throw null! placeholders for now)
            w.Write("    public static ");
            WriteTypedefName(w, type, TypedefNameType.ABI, true);
            w.Write(" ConvertToUnmanaged(");
            WriteTypedefName(w, type, TypedefNameType.Projected, true);
            w.Write(" value) => throw null!;\n");

            w.Write("    public static ");
            WriteTypedefName(w, type, TypedefNameType.Projected, true);
            w.Write(" ConvertToManaged(");
            WriteTypedefName(w, type, TypedefNameType.ABI, true);
            w.Write(" value) => throw null!;\n");

            w.Write("    public static void Dispose(");
            WriteTypedefName(w, type, TypedefNameType.ABI, true);
            w.Write(" value) => throw null!;\n");
        }

        // BoxToUnmanaged - wraps the value as an IReference<T>
        w.Write("    public static WindowsRuntimeObjectReferenceValue BoxToUnmanaged(");
        WriteTypedefName(w, type, TypedefNameType.Projected, true);
        w.Write("? value) => throw null!;\n");

        // UnboxToManaged - unwraps an IReference<T> back to the value
        w.Write("    public static ");
        WriteTypedefName(w, type, TypedefNameType.Projected, true);
        w.Write("? UnboxToManaged(void* value) => throw null!;\n");

        w.Write("}\n\n");

        // Marshaller attribute class (for [TypeMap<WindowsRuntimeComWrappersTypeMapGroup>])
        w.Write("internal sealed class ");
        w.Write(nameStripped);
        w.Write("ComWrappersMarshaller : global::System.Attribute\n{\n}\n");
    }

    /// <summary>
    /// Writes a minimal marshaller stub for a delegate.
    /// </summary>
    private static void WriteDelegateMarshallerStub(TypeWriter w, TypeDefinition type)
    {
        string name = type.Name?.Value ?? string.Empty;
        string nameStripped = Helpers.StripBackticks(name);
        w.Write("internal sealed class ");
        w.Write(nameStripped);
        w.Write("ComWrappersMarshaller : global::System.Attribute\n{\n}\n");
    }

    /// <summary>
    /// Writes the marshaller infrastructure for a runtime class:
    /// * Public *Marshaller class with real ConvertToUnmanaged/ConvertToManaged bodies
    /// * file-scoped *ComWrappersMarshallerAttribute (CreateObject implementation)
    /// * file-scoped *ComWrappersCallback (IWindowsRuntimeObjectComWrappersCallback)
    /// Mirrors C++ <c>write_class_marshaller</c>, <c>write_marshaller_callback_class</c>, etc.
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

        // Public *Marshaller class
        w.Write("public static unsafe class ");
        w.Write(nameStripped);
        w.Write("Marshaller\n{\n");
        w.Write("    public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(");
        w.Write(fullProjected);
        w.Write(" value)\n    {\n");
        w.Write("        if (value is not null)\n        {\n");
        w.Write("            return WindowsRuntimeComWrappersMarshal.UnwrapObjectReferenceUnsafe(value).AsValue();\n");
        w.Write("        }\n");
        w.Write("        return default;\n    }\n\n");
        w.Write("    public static ");
        w.Write(fullProjected);
        w.Write("? ConvertToManaged(void* value)\n    {\n");
        w.Write("        return (");
        w.Write(fullProjected);
        w.Write("?)WindowsRuntimeObjectMarshaller.ConvertToManaged<");
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
        w.Write("            marshalingType: CreateObjectReferenceMarshalingType.Standard,\n");
        w.Write("            wrapperFlags: out wrapperFlags);\n\n");
        w.Write("        return new ");
        w.Write(fullProjected);
        w.Write("(valueReference);\n    }\n}\n\n");

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
        w.Write("            marshalingType: CreateObjectReferenceMarshalingType.Agile,\n");
        w.Write("            wrapperFlags: out wrapperFlags);\n\n");
        w.Write("        return new ");
        w.Write(fullProjected);
        w.Write("(valueReference);\n    }\n}\n");
    }

    /// <summary>
    /// Writes a minimal interface 'Methods' static class with method signature stubs (no bodies).
    /// Mirrors C++ <c>write_static_abi_methods</c> at signature level — bodies are 'throw null!'
    /// stubs so consumers (e.g. handcrafted ComInteropExtensions.cs) can compile against them.
    /// </summary>
    private static void WriteInterfaceMarshallerStub(TypeWriter w, TypeDefinition type)
    {
        string name = type.Name?.Value ?? string.Empty;
        string nameStripped = Helpers.StripBackticks(name);
        w.Write("internal static unsafe class ");
        w.Write(nameStripped);
        w.Write("Methods\n{\n");

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
            w.Write(") => throw null!;\n");
        }

        // Emit property accessors
        foreach (PropertyDefinition prop in type.Properties)
        {
            string pname = prop.Name?.Value ?? string.Empty;
            (MethodDefinition? getter, MethodDefinition? setter) = Helpers.GetPropertyMethods(prop);
            string propType = WritePropType(w, prop);
            if (getter is not null)
            {
                w.Write("    public static ");
                w.Write(propType);
                w.Write(" ");
                w.Write(pname);
                w.Write("(WindowsRuntimeObjectReference thisReference) => throw null!;\n");
            }
            if (setter is not null)
            {
                w.Write("    public static void ");
                w.Write(pname);
                w.Write("(WindowsRuntimeObjectReference thisReference, ");
                w.Write(propType);
                w.Write(" value) => throw null!;\n");
            }
        }

        w.Write("}\n");
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
        w.Write("    public static int get_Value(void* thisPtr, void* result) => throw null!;\n");
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
                w.Write("global::System.Guid");
                break;
            case TypeSemantics.Type_:
                w.Write("global::WindowsRuntime.InteropServices.WindowsRuntimeTypeName");
                break;
            case TypeSemantics.Definition d:
                if (TypeCategorization.GetCategory(d.Type) is TypeCategory.Enum or TypeCategory.Struct)
                {
                    if (IsTypeBlittable(d.Type))
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
        FundamentalType.Boolean => "byte",
        FundamentalType.Char => "ushort",
        FundamentalType.String => "void*",
        _ => FundamentalTypes.ToCSharpType(t)
    };
}
