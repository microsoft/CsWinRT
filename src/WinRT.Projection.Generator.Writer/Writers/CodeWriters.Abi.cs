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
    private static TypeDefinition? GetExclusiveToType(TypeDefinition iface)
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

        // The cookie return parameter is emitted as "__retval" by WriteAbiParameterTypesPointer.
        const string cookieName = "__retval";

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
                // Allow Out/Ref for blittable primitive/enum/blittable-struct types only.
                // Peel ByRef and CustomModifier wrappers to get the underlying type.
                AsmResolver.DotNet.Signatures.TypeSignature underlying = StripByRefAndCustomModifiers(p.Type);
                if (IsHResultException(underlying)) { allParamsSimple = false; break; }
                if (IsBlittablePrimitive(underlying)) { continue; }
                if (IsBlittableStruct(underlying)) { continue; }
                allParamsSimple = false;
                break;
            }
            if (cat == ParamCategory.PassArray || cat == ParamCategory.FillArray)
            {
                // Allow blittable primitive arrays only.
                if (p.Type is AsmResolver.DotNet.Signatures.SzArrayTypeSignature sz)
                {
                    if (IsBlittablePrimitive(sz.BaseType)) { continue; }
                }
                allParamsSimple = false;
                break;
            }
            if (cat != ParamCategory.In) { allParamsSimple = false; break; }
            if (IsHResultException(p.Type)) { allParamsSimple = false; break; }
            if (IsBlittablePrimitive(p.Type)) { continue; }
            if (IsBlittableStruct(p.Type)) { continue; }
            if (IsString(p.Type)) { hasStringParams = true; continue; }
            if (IsRuntimeClassOrInterface(p.Type)) { continue; }
            if (IsObject(p.Type)) { continue; }
            if (IsGenericInstance(p.Type)) { continue; }
            allParamsSimple = false;
            break;
        }
        bool returnSimple = rt is null
            || (IsBlittablePrimitive(rt) && !IsHResultException(rt))
            || (IsBlittableStruct(rt) && !IsHResultException(rt))
            || IsString(rt)
            || IsRuntimeClassOrInterface(rt)
            || IsObject(rt)
            || IsGenericInstance(rt);
        bool returnIsString = rt is not null && IsString(rt);
        bool returnIsRefType = rt is not null && (IsRuntimeClassOrInterface(rt) || IsObject(rt) || IsGenericInstance(rt));
        bool returnIsGenericInstance = rt is not null && IsGenericInstance(rt);
        bool returnIsBlittableStruct = rt is not null && IsBlittableStruct(rt);

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
        if (rt is not null)
        {
            w.Write("    *__retval = default;\n");
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
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.PassArray && cat != ParamCategory.FillArray) { continue; }
            if (p.Type is not AsmResolver.DotNet.Signatures.SzArrayTypeSignature sz) { continue; }
            string raw = p.Parameter.Name ?? "param";
            string ptr = Helpers.IsKeyword(raw) ? "@" + raw : raw;
            string elementProjected = w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteProjectionType(w, TypeSemanticsFactory.Get(sz.BaseType))));
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
        w.Write("    try\n    {\n");

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
            // For enums, cast to the underlying ABI primitive type.
            // For bool, cast to byte. For char, cast to ushort.
            if (IsEnumType(underlying))
            {
                w.Write("(");
                w.Write(GetAbiPrimitiveType(underlying));
                w.Write(")");
                w.Write("__");
                w.Write(raw);
            }
            else if (underlying is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlibBool &&
                     corlibBool.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Boolean)
            {
                w.Write("(byte)(");
                w.Write("__");
                w.Write(raw);
                w.Write(" ? 1 : 0)");
            }
            else if (underlying is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlibChar &&
                     corlibChar.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Char)
            {
                w.Write("(ushort)__");
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
            if (returnIsString)
            {
                w.Write("        *__retval = HStringMarshaller.ConvertToUnmanaged(__result);\n");
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
                    w.Write("        *__retval = ConvertToUnmanaged_result(null, __result).DetachThisPtrUnsafe();\n");
                }
                else
                {
                    w.Write("        *__retval = ");
                    EmitMarshallerConvertToUnmanaged(w, rt!, "__result");
                    w.Write(".DetachThisPtrUnsafe();\n");
                }
            }
            else if (returnIsBlittableStruct)
            {
                w.Write("        *__retval = __result;\n");
            }
            else
            {
                string abiType = GetAbiPrimitiveType(rt);
                w.Write("        *__retval = ");
                if (rt is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlib &&
                    corlib.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Boolean)
                {
                    w.Write("(byte)(__result ? 1 : 0);\n");
                }
                else if (rt is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlib2 &&
                         corlib2.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Char)
                {
                    w.Write("(ushort)__result;\n");
                }
                else if (IsEnumType(rt))
                {
                    w.Write("(");
                    w.Write(abiType);
                    w.Write(")__result;\n");
                }
                else
                {
                    w.Write("__result;\n");
                }
            }
        }
        w.Write("        return 0;\n    }\n");
        w.Write("    catch (Exception __exception__)\n    {\n");
        w.Write("        return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(__exception__);\n    }\n}\n\n");
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
            w.Write(" != 0");
        }
        else if (p.Type is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlib2 &&
                 corlib2.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Char)
        {
            w.Write("(char)");
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
        else if (IsBlittableStruct(p.Type))
        {
            // Blittable struct: pass directly (projected type == ABI type)
            w.Write(pname);
        }
        else if (IsEnumType(p.Type))
        {
            w.Write("(");
            string projected = w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteProjectedSignature(w, p.Type, false)));
            w.Write(projected);
            w.Write(")");
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

        // Events: leave as abstract event declarations (no DIM body). The corresponding ABI helper
        // method on the static abi class is currently not generated; revisit when class events on
        // generic interfaces are fully ported.
        foreach (EventDefinition evt in type.Events)
        {
            w.Write("\nevent ");
            WriteEventType(w, evt);
            w.Write(" ");
            w.Write(evt.Name?.Value ?? string.Empty);
            w.Write(";\n");
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
        // (Real implementation only for blittable types — non-blittable structs need
        // per-field marshalling via *Marshaller.ConvertToUnmanaged before boxing.)
        w.Write("    public static WindowsRuntimeObjectReferenceValue BoxToUnmanaged(");
        WriteTypedefName(w, type, TypedefNameType.Projected, true);
        if (blittable)
        {
            w.Write("? value)\n    {\n");
            w.Write("        return WindowsRuntimeValueTypeMarshaller.BoxToUnmanaged(value, CreateComInterfaceFlags.None, in ");
            WriteIidReferenceExpression(w, type);
            w.Write(");\n    }\n");
        }
        else
        {
            w.Write("? value) => throw null!;\n");
        }

        // UnboxToManaged - unwraps an IReference<T> back to the value
        w.Write("    public static ");
        WriteTypedefName(w, type, TypedefNameType.Projected, true);
        if (blittable)
        {
            w.Write("? UnboxToManaged(void* value)\n    {\n");
            w.Write("        return WindowsRuntimeValueTypeMarshaller.UnboxToManaged<");
            WriteTypedefName(w, type, TypedefNameType.Projected, true);
            w.Write(">(value);\n    }\n");
        }
        else
        {
            w.Write("? UnboxToManaged(void* value) => throw null!;\n");
        }

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
        string typeNs = type.Namespace?.Value ?? string.Empty;
        string fullProjected = $"global::{typeNs}.{nameStripped}";

        // Compute the IID expression for this delegate (uses the DelegateMarshaller's IID convention).
        string iidExpr = w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteIidExpression(w, type)));

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

        // The *ComWrappersMarshallerAttribute class — referenced via [ABI.NS.NameComWrappersMarshaller]
        // on the delegate definition. For now keep an empty attribute that derives from the base.
        w.Write("internal sealed unsafe class ");
        w.Write(nameStripped);
        w.Write("ComWrappersMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute\n{\n");
        w.Write("    public override object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags) => throw null!;\n");
        w.Write("}\n\n");

        // file-scoped *ComWrappersCallback for delegate
        w.Write("file sealed unsafe class ");
        w.Write(nameStripped);
        w.Write("ComWrappersCallback : IWindowsRuntimeObjectComWrappersCallback\n{\n");
        w.Write("    public static object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags) => throw null!;\n");
        w.Write("}\n");
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
    /// Writes a minimal interface 'Methods' static class with method body emission.
    /// Mirrors C++ <c>write_static_abi_methods</c>: void/no-args methods and
    /// blittable-primitive-return/no-args methods get real implementations; everything else
    /// remains as 'throw null!' stubs (deferred — needs full per-parameter marshalling).
    /// </summary>
    private static void WriteInterfaceMarshallerStub(TypeWriter w, TypeDefinition type)
    {
        string name = type.Name?.Value ?? string.Empty;
        string nameStripped = Helpers.StripBackticks(name);
        w.Write("internal static unsafe class ");
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

        w.Write("}\n");
    }

    /// <summary>
    /// Emits a real method body for the cases we can fully marshal, otherwise emits
    /// the 'throw null!' stub. Trailing newline is included.
    /// </summary>
    private static void EmitAbiMethodBodyIfSimple(TypeWriter w, MethodSig sig, int slot, string? paramNameOverride = null)
    {
        AsmResolver.DotNet.Signatures.TypeSignature? rt = sig.ReturnType;

        // Check that all parameters are types we can marshal (blittable primitives, blittable struct, string, runtime class, object, or generic instance).
        bool allParamsSimple = true;
        foreach (ParamInfo p in sig.Params)
        {
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            // Only support 'In' parameters
            if (cat != ParamCategory.In) { allParamsSimple = false; break; }
            if (IsHResultException(p.Type)) { allParamsSimple = false; break; }
            if (IsBlittablePrimitive(p.Type)) { continue; }
            if (IsBlittableStruct(p.Type)) { continue; }
            if (IsString(p.Type)) { continue; }
            if (IsRuntimeClassOrInterface(p.Type)) { continue; }
            if (IsObject(p.Type)) { continue; }
            if (IsGenericInstance(p.Type)) { continue; }
            allParamsSimple = false;
            break;
        }
        bool returnSimple = rt is null
            || (IsBlittablePrimitive(rt) && !IsHResultException(rt))
            || (IsBlittableStruct(rt) && !IsHResultException(rt))
            || IsString(rt)
            || IsRuntimeClassOrInterface(rt)
            || IsObject(rt)
            || IsGenericInstance(rt);

        if (!allParamsSimple || !returnSimple)
        {
            w.Write(" => throw null!;\n");
            return;
        }

        bool returnIsString = rt is not null && IsString(rt);
        bool returnIsRefType = rt is not null && (IsRuntimeClassOrInterface(rt) || IsObject(rt) || IsGenericInstance(rt));
        bool returnIsBlittableStruct = rt is not null && IsBlittableStruct(rt);

        // Build the function pointer signature: void*, [paramAbiType...,] [retAbiType*,] int
        System.Text.StringBuilder fp = new();
        fp.Append("void*");
        foreach (ParamInfo p in sig.Params)
        {
            fp.Append(", ");
            if (IsString(p.Type) || IsRuntimeClassOrInterface(p.Type) || IsObject(p.Type) || IsGenericInstance(p.Type)) { fp.Append("void*"); }
            else if (IsBlittableStruct(p.Type)) { fp.Append(GetBlittableStructAbiType(w, p.Type)); }
            else { fp.Append(GetAbiPrimitiveType(p.Type)); }
        }
        if (rt is not null)
        {
            fp.Append(", ");
            if (returnIsString || returnIsRefType) { fp.Append("void**"); }
            else if (returnIsBlittableStruct) { fp.Append(GetBlittableStructAbiType(w, rt)); fp.Append('*'); }
            else { fp.Append(GetAbiPrimitiveType(rt)); fp.Append('*'); }
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
        if (returnIsString || returnIsRefType)
        {
            w.Write("        void* __retval = default;\n");
        }
        else if (returnIsBlittableStruct)
        {
            w.Write("        ");
            w.Write(GetBlittableStructAbiType(w, rt!));
            w.Write(" __retval = default;\n");
        }
        else if (rt is not null)
        {
            w.Write("        ");
            w.Write(GetAbiPrimitiveType(rt));
            w.Write(" __retval = default;\n");
        }

        // Determine if we need a try/finally (for cleanup of string params or string/refType return).
        bool hasStringParams = false;
        for (int i = 0; i < sig.Params.Count; i++) { if (IsString(sig.Params[i].Type)) { hasStringParams = true; break; } }
        bool needsTryFinally = hasStringParams || returnIsString || returnIsRefType;
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

        w.Write(indent);
        w.Write("RestrictedErrorInfo.ThrowExceptionForHR((*(delegate* unmanaged[MemberFunction]<");
        w.Write(fp.ToString());
        w.Write(">**)ThisPtr)[");
        w.Write(slot);
        w.Write("](ThisPtr");
        for (int i = 0; i < sig.Params.Count; i++)
        {
            w.Write(", ");
            ParamInfo p = sig.Params[i];
            if (IsString(p.Type))
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
            else if (IsBlittableStruct(p.Type))
            {
                // Blittable struct: pass directly (projected type == ABI type)
                w.Write(GetParamName(p, paramNameOverride));
            }
            else
            {
                EmitParamArgConversion(w, p, paramNameOverride);
            }
        }
        if (rt is not null)
        {
            w.Write(", &__retval");
        }
        w.Write("));\n");

        // Return value
        if (rt is not null)
        {
            if (returnIsString)
            {
                w.Write(indent);
                w.Write("return HStringMarshaller.ConvertToManaged(__retval);\n");
            }
            else if (returnIsRefType)
            {
                if (IsGenericInstance(rt))
                {
                    // Generic instance return: use a local UnsafeAccessor delegate.
                    string interopTypeName = EncodeInteropTypeName(rt, TypedefNameType.ABI) + ", WinRT.Interop";
                    string projectedTypeName = w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteProjectedSignature(w, rt, false)));
                    w.Write(indent);
                    w.Write("[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"ConvertToManaged\")]\n");
                    w.Write(indent);
                    w.Write("static extern ");
                    w.Write(projectedTypeName);
                    w.Write(" ConvertToManaged_retval([UnsafeAccessorType(\"");
                    w.Write(interopTypeName);
                    w.Write("\")] object _, void* value);\n");
                    w.Write(indent);
                    w.Write("return ConvertToManaged_retval(null, __retval);\n");
                }
                else
                {
                    w.Write(indent);
                    w.Write("return ");
                    EmitMarshallerConvertToManaged(w, rt, "__retval");
                    w.Write(";\n");
                }
            }
            else if (returnIsBlittableStruct)
            {
                w.Write(indent);
                w.Write("return __retval;\n");
            }
            else
            {
                w.Write(indent);
                w.Write("return ");
                string projected = w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteProjectedSignature(w, rt, false)));
                string abiType = GetAbiPrimitiveType(rt);
                if (projected == "bool") { w.Write("__retval != 0;\n"); }
                else if (projected == abiType) { w.Write("__retval;\n"); }
                else
                {
                    w.Write("(");
                    w.Write(projected);
                    w.Write(")__retval;\n");
                }
            }
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
            w.Write("        }\n");
        }

        w.Write("    }\n");
    }

    /// <summary>True if the type signature represents the System.Object root type.</summary>
    private static bool IsObject(AsmResolver.DotNet.Signatures.TypeSignature sig)
    {
        return sig is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlib &&
               corlib.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Object;
    }

    /// <summary>True if the type signature represents Windows.Foundation.HResult / System.Exception
    /// (special-cased: ABI is int but projected is Exception, requires custom marshalling).</summary>
    private static bool IsHResultException(AsmResolver.DotNet.Signatures.TypeSignature sig)
    {
        if (sig is not AsmResolver.DotNet.Signatures.TypeDefOrRefSignature td) { return false; }
        string ns = td.Type?.Namespace?.Value ?? string.Empty;
        string name = td.Type?.Name?.Value ?? string.Empty;
        return (ns == "System" && name == "Exception")
            || (ns == "Windows.Foundation" && name == "HResult");
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
        // bool -> byte (truthy = 1, false = 0)
        if (p.Type is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlib &&
            corlib.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Boolean)
        {
            w.Write("(byte)(");
            w.Write(pname);
            w.Write(" ? 1 : 0)");
        }
        // char -> ushort (no conversion needed; cast)
        else if (p.Type is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlib2 &&
                 corlib2.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Char)
        {
            w.Write("(ushort)");
            w.Write(pname);
        }
        // Enums -> their underlying numeric type (cast). Handles both same-module and cross-module enums.
        else if (IsEnumType(p.Type))
        {
            w.Write("(");
            w.Write(GetAbiPrimitiveType(p.Type));
            w.Write(")");
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

    /// <summary>Returns the ABI type name for a blittable struct (the projected type name).</summary>
    private static string GetBlittableStructAbiType(TypeWriter w, AsmResolver.DotNet.Signatures.TypeSignature sig)
    {
        return w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteProjectedSignature(w, sig, false)));
    }

    private static string GetAbiPrimitiveType(AsmResolver.DotNet.Signatures.TypeSignature sig)
    {
        if (sig is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlib)
        {
            return corlib.ElementType switch
            {
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Boolean => "byte",
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Char => "ushort",
                _ => GetAbiFundamentalTypeFromCorLib(corlib.ElementType),
            };
        }
        // Enum: use its underlying numeric type
        if (sig is AsmResolver.DotNet.Signatures.TypeDefOrRefSignature td)
        {
            TypeDefinition? def = td.Type as TypeDefinition;
            if (def is null && _cacheRef is not null && td.Type is TypeReference tr)
            {
                string ns = tr.Namespace?.Value ?? string.Empty;
                string name = tr.Name?.Value ?? string.Empty;
                def = _cacheRef.Find(ns + "." + name);
            }
            if (def is not null)
            {
                // Find the enum's value__ field for the underlying type
                foreach (FieldDefinition f in def.Fields)
                {
                    if (!f.IsStatic && f.Signature?.FieldType is AsmResolver.DotNet.Signatures.CorLibTypeSignature ut)
                    {
                        return GetAbiFundamentalTypeFromCorLib(ut.ElementType);
                    }
                }
            }
        }
        return "int";
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
        if (blittable)
        {
            // Real implementation for blittable types: extract managed instance from CCW,
            // dereference into the result pointer, return S_OK.
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
        else
        {
            w.Write("    public static int get_Value(void* thisPtr, void* result) => throw null!;\n");
        }
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
                if (TypeCategorization.GetCategory(d.Type) is TypeCategory.Enum)
                {
                    // For enums, use the underlying primitive type at the ABI (matches truth output).
                    w.Write(GetAbiPrimitiveType(d.Type.ToTypeSignature()));
                }
                else if (TypeCategorization.GetCategory(d.Type) is TypeCategory.Struct)
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
            case TypeSemantics.Reference r:
                // Cross-module typeref: try resolving the type, applying mapped-type translation
                // for the field/parameter type after resolution.
                if (_cacheRef is not null)
                {
                    string rns = r.Reference_.Namespace?.Value ?? string.Empty;
                    string rname = r.Reference_.Name?.Value ?? string.Empty;
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
                            // Use the underlying primitive type for enums.
                            w.Write(GetAbiPrimitiveType(rd.ToTypeSignature()));
                            break;
                        }
                        if (cat == TypeCategory.Struct)
                        {
                            // Special case: HResult is mapped to System.Exception (a reference type)
                            // but its ABI representation is int (the underlying value).
                            string rdNs = rd.Namespace?.Value ?? string.Empty;
                            string rdName = rd.Name?.Value ?? string.Empty;
                            if (rdNs == "Windows.Foundation" && rdName == "HResult")
                            {
                                w.Write("int");
                                break;
                            }
                            if (IsTypeBlittable(rd))
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
        FundamentalType.Boolean => "byte",
        FundamentalType.Char => "ushort",
        FundamentalType.String => "void*",
        _ => FundamentalTypes.ToCSharpType(t)
    };
}
