// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Extensions;
using WindowsRuntime.ProjectionWriter.Models;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter;

/// <summary>
/// Emits the full ABI surface for a projected delegate type:
/// the marshaller class, vtable, native delegate, ComWrappers callback, interface entries,
/// ComWrappers marshaller attribute, the impl class, and the IReference impl.
/// </summary>
internal static class AbiDelegateFactory
{
    public static void WriteAbiDelegate(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        //   write_delegate_marshaller
        //   write_delegate_vtbl
        //   write_native_delegate
        //   write_delegate_comwrappers_callback
        //   write_delegates_interface_entries_impl
        //   write_delegate_com_wrappers_marshaller_attribute_impl
        //   write_delegate_impl
        //   write_reference_impl
        //   (component) write_authoring_metadata_type
        WriteDelegateMarshallerOnly(writer, context, type);
        WriteDelegateVftbl(writer, context, type);
        WriteNativeDelegate(writer, context, type);
        WriteDelegateComWrappersCallback(writer, context, type);
        WriteDelegateInterfaceEntriesImpl(writer, context, type);
        WriteDelegateComWrappersMarshallerAttribute(writer, context, type);
        WriteDelegateImpl(writer, context, type);
        ReferenceImplFactory.Write(writer, context, type);

        // In component mode, the original code also emits the authoring metadata wrapper for delegates.
        if (context.Settings.Component)
        {
            AbiClassFactory.WriteAuthoringMetadataType(writer, context, type);
        }
    }

    /// <summary>Emits the <c>&lt;DelegateName&gt;Impl</c> static class providing the CCW vtable for a delegate.</summary>
    private static void WriteDelegateImpl(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        if (type.GenericParameters.Count > 0) { return; }
        MethodDefinition? invoke = type.GetDelegateInvoke();
        if (invoke is null) { return; }
        MethodSig sig = new(invoke);
        string name = type.Name?.Value ?? string.Empty;
        string nameStripped = IdentifierEscaping.StripBackticks(name);
        IndentedTextWriter __scratchIidExpr = new();
        ObjRefNameGenerator.WriteIidExpression(__scratchIidExpr, context, type);
        string iidExpr = __scratchIidExpr.ToString();

        writer.Write("\ninternal static unsafe class ");
        writer.Write(nameStripped);
        writer.Write("Impl\n{\n");
        writer.WriteLine("    [FixedAddressValueType]");
        writer.Write("    private static readonly ");
        writer.Write(nameStripped);
        writer.Write("Vftbl Vftbl;\n\n");
        writer.Write("    static ");
        writer.Write(nameStripped);
        writer.Write("Impl()\n    {\n");
        writer.WriteLine("        *(IUnknownVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IUnknownVftbl*)IUnknownImpl.Vtable;");
        writer.WriteLine("        Vftbl.Invoke = &Invoke;");
        writer.Write("    }\n\n");
        writer.Write("    public static nint Vtable\n    {\n        [MethodImpl(MethodImplOptions.AggressiveInlining)]\n        get => (nint)Unsafe.AsPointer(in Vftbl);\n    }\n\n");

        writer.WriteLine("[UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]");
        writer.Write("private static int Invoke(");
        AbiInterfaceFactory.WriteAbiParameterTypesPointer(writer, context, sig, includeParamNames: true);
        writer.Write(")");

        // Reuse the interface Do_Abi body emitter: delegates dispatch via __target.Invoke(...),
        // which is exactly the same shape as interface CCW dispatch. Pass the delegate's
        // projected name as 'ifaceFullName' and "Invoke" as 'methodName'.
        IndentedTextWriter __scratchProjectedDelegateForBody = new();
        TypedefNameWriter.WriteTypedefName(__scratchProjectedDelegateForBody, context, type, TypedefNameType.Projected, true);
        string projectedDelegateForBody = __scratchProjectedDelegateForBody.ToString();
        if (!projectedDelegateForBody.StartsWith("global::", System.StringComparison.Ordinal)) { projectedDelegateForBody = "global::" + projectedDelegateForBody; }
        AbiMethodBodyFactory.EmitDoAbiBodyIfSimple(writer, context, sig, projectedDelegateForBody, "Invoke");
        writer.Write($"\n    public static ref readonly Guid IID\n    {{\n        [MethodImpl(MethodImplOptions.AggressiveInlining)]\n        get => ref {iidExpr};\n    }}\n}}\n");
    }

    private static void WriteDelegateVftbl(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        if (type.GenericParameters.Count > 0) { return; }
        MethodDefinition? invoke = type.GetDelegateInvoke();
        if (invoke is null) { return; }
        MethodSig sig = new(invoke);
        string name = type.Name?.Value ?? string.Empty;
        string nameStripped = IdentifierEscaping.StripBackticks(name);

        writer.Write("\n[StructLayout(LayoutKind.Sequential)]\n");
        writer.Write("internal unsafe struct ");
        writer.Write(nameStripped);
        writer.Write("Vftbl\n{\n");
        writer.WriteLine("    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, int> QueryInterface;");
        writer.WriteLine("    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;");
        writer.WriteLine("    public delegate* unmanaged[MemberFunction]<void*, uint> Release;");
        writer.Write("    public delegate* unmanaged[MemberFunction]<");
        AbiInterfaceFactory.WriteAbiParameterTypesPointer(writer, context, sig);
        writer.WriteLine(", int> Invoke;");
        writer.WriteLine("}");
    }

    private static void WriteNativeDelegate(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        if (type.GenericParameters.Count > 0) { return; }
        MethodDefinition? invoke = type.GetDelegateInvoke();
        if (invoke is null) { return; }
        MethodSig sig = new(invoke);
        string name = type.Name?.Value ?? string.Empty;
        string nameStripped = IdentifierEscaping.StripBackticks(name);

        writer.Write($"\npublic static unsafe class {nameStripped}NativeDelegate\n{{\n    public static unsafe ");
        MethodFactory.WriteProjectionReturnType(writer, context, sig);
        writer.Write($" {nameStripped}Invoke(this WindowsRuntimeObjectReference thisReference");
        if (sig.Params.Count > 0) { writer.Write(", "); }
        MethodFactory.WriteParameterList(writer, context, sig);
        writer.Write(")");

        // Reuse the interface caller body emitter. Delegate Invoke is at vtable slot 3
        // (after QI/AddRef/Release). Functionally equivalent to the truth's
        // 'var abiInvoke = ((<Name>Vftbl*)*(void***)ThisPtr)->Invoke;' form, just routed
        // through the slot-indexed dispatch shared with interface CCW callers.
        AbiMethodBodyFactory.EmitAbiMethodBodyIfSimple(writer, context, sig, slot: 3, isNoExcept: invoke.IsNoExcept());

        writer.WriteLine("}");
    }

    private static void WriteDelegateInterfaceEntriesImpl(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        if (type.GenericParameters.Count > 0) { return; }
        string name = type.Name?.Value ?? string.Empty;
        string nameStripped = IdentifierEscaping.StripBackticks(name);
        IndentedTextWriter __scratchIidExpr = new();
        ObjRefNameGenerator.WriteIidExpression(__scratchIidExpr, context, type);
        string iidExpr = __scratchIidExpr.ToString();
        IndentedTextWriter __scratchIidRefExpr = new();
        ObjRefNameGenerator.WriteIidReferenceExpression(__scratchIidRefExpr, type);
        string iidRefExpr = __scratchIidRefExpr.ToString();

        writer.Write("\nfile static class ");
        writer.Write(nameStripped);
        writer.Write("InterfaceEntriesImpl\n{\n");
        writer.WriteLine("    [FixedAddressValueType]");
        writer.Write("    public static readonly DelegateReferenceInterfaceEntries Entries;\n\n");
        writer.Write("    static ");
        writer.Write(nameStripped);
        writer.Write("InterfaceEntriesImpl()\n    {\n");
        writer.Write("        Entries.Delegate.IID = ");
        writer.Write(iidExpr);
        writer.WriteLine(";");
        writer.Write("        Entries.Delegate.Vtable = ");
        writer.Write(nameStripped);
        writer.WriteLine("Impl.Vtable;");
        writer.Write("        Entries.DelegateReference.IID = ");
        writer.Write(iidRefExpr);
        writer.WriteLine(";");
        writer.Write("        Entries.DelegateReference.Vtable = ");
        writer.Write(nameStripped);
        writer.WriteLine("ReferenceImpl.Vtable;");
        writer.WriteLine("        Entries.IPropertyValue.IID = global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IPropertyValue;");
        writer.WriteLine("        Entries.IPropertyValue.Vtable = global::WindowsRuntime.InteropServices.IPropertyValueImpl.OtherTypeVtable;");
        writer.WriteLine("        Entries.IStringable.IID = global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IStringable;");
        writer.WriteLine("        Entries.IStringable.Vtable = global::WindowsRuntime.InteropServices.IStringableImpl.Vtable;");
        writer.WriteLine("        Entries.IWeakReferenceSource.IID = global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IWeakReferenceSource;");
        writer.WriteLine("        Entries.IWeakReferenceSource.Vtable = global::WindowsRuntime.InteropServices.IWeakReferenceSourceImpl.Vtable;");
        writer.WriteLine("        Entries.IMarshal.IID = global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IMarshal;");
        writer.WriteLine("        Entries.IMarshal.Vtable = global::WindowsRuntime.InteropServices.IMarshalImpl.Vtable;");
        writer.WriteLine("        Entries.IAgileObject.IID = global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IAgileObject;");
        writer.WriteLine("        Entries.IAgileObject.Vtable = global::WindowsRuntime.InteropServices.IAgileObjectImpl.Vtable;");
        writer.WriteLine("        Entries.IInspectable.IID = global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IInspectable;");
        writer.WriteLine("        Entries.IInspectable.Vtable = global::WindowsRuntime.InteropServices.IInspectableImpl.Vtable;");
        writer.WriteLine("        Entries.IUnknown.IID = global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IUnknown;");
        writer.WriteLine("        Entries.IUnknown.Vtable = global::WindowsRuntime.InteropServices.IUnknownImpl.Vtable;");
        writer.Write("    }\n}\n");
    }

    public static void WriteTempDelegateEventSourceSubclass(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        // Skip generic delegates: only non-generic delegates get a per-delegate EventSource subclass.
        // Generic delegates (e.g. EventHandler<T>) use the generic EventHandlerEventSource<T> directly.
        if (type.GenericParameters.Count > 0) { return; }

        MethodDefinition? invoke = type.GetDelegateInvoke();
        if (invoke is null) { return; }
        MethodSig sig = new(invoke);
        string name = type.Name?.Value ?? string.Empty;
        string nameStripped = IdentifierEscaping.StripBackticks(name);

        // Compute the projected type name (with global::) used as the generic argument.
        IndentedTextWriter __scratchProjectedName = new();
        TypedefNameWriter.WriteTypedefName(__scratchProjectedName, context, type, TypedefNameType.Projected, true);
        string projectedName = __scratchProjectedName.ToString();
        if (!projectedName.StartsWith("global::", System.StringComparison.Ordinal))
        {
            projectedName = "global::" + projectedName;
        }

        writer.Write("\npublic sealed unsafe class ");
        writer.Write(nameStripped);
        writer.Write("EventSource : EventSource<");
        writer.Write(projectedName);
        writer.Write(">\n{\n");
        writer.WriteLine("    /// <inheritdoc cref=\"EventSource{T}.EventSource\"/>");
        writer.Write("    public ");
        writer.Write(nameStripped);
        writer.Write("EventSource(WindowsRuntimeObjectReference nativeObjectReference, int index)\n        : base(nativeObjectReference, index)\n    {\n    }\n\n");
        writer.WriteLine("    /// <inheritdoc/>");
        writer.Write("    protected override WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(");
        writer.Write(projectedName);
        writer.Write(" value)\n    {\n        return ");
        writer.Write(nameStripped);
        writer.Write("Marshaller.ConvertToUnmanaged(value);\n    }\n\n");
        writer.WriteLine("    /// <inheritdoc/>");
        writer.Write("    protected override EventSourceState<");
        writer.Write(projectedName);
        writer.Write("> CreateEventSourceState()\n    {\n        return new EventState(GetNativeObjectReferenceThisPtrUnsafe(), Index);\n    }\n\n");
        writer.Write("    private sealed class EventState : EventSourceState<");
        writer.Write(projectedName);
        writer.Write(">\n    {\n");
        writer.WriteLine("        /// <inheritdoc cref=\"EventSourceState{T}.EventSourceState\"/>");
        writer.Write("        public EventState(void* thisPtr, int index)\n            : base(thisPtr, index)\n        {\n        }\n\n");
        writer.WriteLine("        /// <inheritdoc/>");
        writer.Write($"        protected override {projectedName} GetEventInvoke()\n        {{\n            return (");
        for (int i = 0; i < sig.Params.Count; i++)
        {
            if (i > 0) { writer.Write(", "); }
            ParamCategory pc = ParamHelpers.GetParamCategory(sig.Params[i]);
            if (pc == ParamCategory.Ref) { writer.Write("in "); }
            else if (pc == ParamCategory.Out || pc == ParamCategory.ReceiveArray) { writer.Write("out "); }
            string raw = sig.Params[i].Parameter.Name ?? "p";
            writer.Write(CSharpKeywords.IsKeyword(raw) ? "@" + raw : raw);
        }
        writer.Write(") => TargetDelegate.Invoke(");
        for (int i = 0; i < sig.Params.Count; i++)
        {
            if (i > 0) { writer.Write(", "); }
            ParamCategory pc = ParamHelpers.GetParamCategory(sig.Params[i]);
            if (pc == ParamCategory.Ref) { writer.Write("in "); }
            else if (pc == ParamCategory.Out || pc == ParamCategory.ReceiveArray) { writer.Write("out "); }
            string raw = sig.Params[i].Parameter.Name ?? "p";
            writer.Write(CSharpKeywords.IsKeyword(raw) ? "@" + raw : raw);
        }
        writer.WriteLine(");");
        writer.Write("        }\n    }\n}\n");
    }

    /// <summary>
    /// Writes a marshaller stub for a delegate.
    /// </summary>
    /// <summary>
    /// Emits just the <c>&lt;Name&gt;Marshaller</c> class for a delegate.
    /// </summary>
    private static void WriteDelegateMarshallerOnly(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        string name = type.Name?.Value ?? string.Empty;
        string nameStripped = IdentifierEscaping.StripBackticks(name);
        string typeNs = type.Namespace?.Value ?? string.Empty;
        string fullProjected = $"global::{typeNs}.{nameStripped}";
        IndentedTextWriter __scratchIidExpr = new();
        ObjRefNameGenerator.WriteIidExpression(__scratchIidExpr, context, type);
        string iidExpr = __scratchIidExpr.ToString();

        writer.Write("\npublic static unsafe class ");
        writer.Write(nameStripped);
        writer.Write("Marshaller\n{\n");
        writer.Write("    public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(");
        writer.Write(fullProjected);
        writer.Write(" value)\n    {\n");
        writer.Write("        return WindowsRuntimeDelegateMarshaller.ConvertToUnmanaged(value, in ");
        writer.Write(iidExpr);
        writer.Write(");\n    }\n\n");
        writer.WriteLine("#nullable enable");
        writer.Write("    public static ");
        writer.Write(fullProjected);
        writer.Write("? ConvertToManaged(void* value)\n    {\n");
        writer.Write("        return (");
        writer.Write(fullProjected);
        writer.Write("?)WindowsRuntimeDelegateMarshaller.ConvertToManaged<");
        writer.Write(nameStripped);
        writer.Write("ComWrappersCallback>(value);\n    }\n");
        writer.WriteLine("#nullable disable");
        writer.WriteLine("}");
    }

    /// <summary>
    /// Emits the <c>&lt;Name&gt;ComWrappersCallback</c> file-scoped class for a delegate.
    /// here at all — the higher-level dispatch in <c>ProjectionGenerator</c> filters out generic
    /// types from ABI emission . Open generic delegates
    /// can't compile this body anyway because the projected type would have unbound generic
    /// parameters.
    /// </summary>
    private static void WriteDelegateComWrappersCallback(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        string name = type.Name?.Value ?? string.Empty;
        string nameStripped = IdentifierEscaping.StripBackticks(name);
        string typeNs = type.Namespace?.Value ?? string.Empty;
        string fullProjected = $"global::{typeNs}.{nameStripped}";
        IndentedTextWriter __scratchIidExpr = new();
        ObjRefNameGenerator.WriteIidExpression(__scratchIidExpr, context, type);
        string iidExpr = __scratchIidExpr.ToString();

        MethodDefinition? invoke = type.GetDelegateInvoke();
        bool nativeSupported = invoke is not null && AbiTypeHelpers.IsDelegateInvokeNativeSupported(context.Cache, new MethodSig(invoke));

        writer.Write("\nfile abstract unsafe class ");
        writer.Write(nameStripped);
        writer.Write("ComWrappersCallback : IWindowsRuntimeObjectComWrappersCallback\n{\n");
        writer.WriteLine("    /// <inheritdoc/>");
        writer.Write("    public static object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)\n    {\n");
        writer.WriteLine("        WindowsRuntimeObjectReference valueReference = WindowsRuntimeComWrappersMarshal.CreateObjectReferenceUnsafe(");
        writer.WriteLine("            externalComObject: value,");
        writer.WriteLine($"            iid: in {iidExpr},\n            wrapperFlags: out wrapperFlags);\n\n        return new {fullProjected}(valueReference.{nameStripped}Invoke);");
        _ = nativeSupported;
        writer.Write("    }\n}\n");
    }

    /// <summary>
    /// Emits the <c>&lt;Name&gt;ComWrappersMarshallerAttribute</c> class. Mirrors C++
    /// <c>write_delegate_com_wrappers_marshaller_attribute_impl</c>. Generic delegates are not
    /// emitted here at all (filtered out in <c>ProjectionGenerator</c>).
    /// </summary>
    private static void WriteDelegateComWrappersMarshallerAttribute(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        string name = type.Name?.Value ?? string.Empty;
        string nameStripped = IdentifierEscaping.StripBackticks(name);
        IndentedTextWriter __scratchIidRefExpr = new();
        ObjRefNameGenerator.WriteIidReferenceExpression(__scratchIidRefExpr, type);
        string iidRefExpr = __scratchIidRefExpr.ToString();

        writer.Write("\ninternal sealed unsafe class ");
        writer.Write(nameStripped);
        writer.Write("ComWrappersMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute\n{\n");
        writer.WriteLine("    /// <inheritdoc/>");
        writer.Write("    public override void* GetOrCreateComInterfaceForObject(object value)\n    {\n");
        writer.WriteLine("        return WindowsRuntimeComWrappersMarshal.GetOrCreateComInterfaceForObject(value, CreateComInterfaceFlags.TrackerSupport);");
        writer.Write("    }\n\n");
        writer.WriteLine("    /// <inheritdoc/>");
        writer.Write("    public override ComInterfaceEntry* ComputeVtables(out int count)\n    {\n");
        writer.Write("        count = sizeof(DelegateReferenceInterfaceEntries) / sizeof(ComInterfaceEntry);\n\n");
        writer.Write("        return (ComInterfaceEntry*)Unsafe.AsPointer(in ");
        writer.Write(nameStripped);
        writer.Write("InterfaceEntriesImpl.Entries);\n    }\n\n");
        writer.WriteLine("    /// <inheritdoc/>");
        writer.Write("    public override object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)\n    {\n");
        writer.WriteLine("        wrapperFlags = CreatedWrapperFlags.NonWrapping;");
        writer.WriteLine($"        return WindowsRuntimeDelegateMarshaller.UnboxToManaged<{nameStripped}ComWrappersCallback>(value, in {iidRefExpr})!;\n    }}\n}}");
    }

}
