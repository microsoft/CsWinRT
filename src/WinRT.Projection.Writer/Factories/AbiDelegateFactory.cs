// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using static WindowsRuntime.ProjectionWriter.References.ProjectionNames;
using System;
using WindowsRuntime.ProjectionWriter.Extensions;
using WindowsRuntime.ProjectionWriter.Helpers;
using WindowsRuntime.ProjectionWriter.Metadata;
using WindowsRuntime.ProjectionWriter.Models;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter.Factories;

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
        MethodSignatureInfo sig = new(invoke);
        string name = type.Name?.Value ?? string.Empty;
        string nameStripped = IdentifierEscaping.StripBackticks(name);
        IndentedTextWriter __scratchIidExpr = new();
        ObjRefNameGenerator.WriteIidExpression(__scratchIidExpr, context, type);
        string iidExpr = __scratchIidExpr.ToString();

        writer.WriteLine("");
        writer.Write($$"""
            internal static unsafe class {{nameStripped}}Impl
            {
                [FixedAddressValueType]
                private static readonly {{nameStripped}}Vftbl Vftbl;
            
                static {{nameStripped}}Impl()
                {
                    *(IUnknownVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IUnknownVftbl*)IUnknownImpl.Vtable;
                    Vftbl.Invoke = &Invoke;
                }
            
                public static nint Vtable
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get => (nint)Unsafe.AsPointer(in Vftbl);
                }
            
            [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
            private static int Invoke(
            """, isMultiline: true);
        AbiInterfaceFactory.WriteAbiParameterTypesPointer(writer, context, sig, includeParamNames: true);
        writer.Write(")");

        // Reuse the interface Do_Abi body emitter: delegates dispatch via __target.Invoke(...),
        // which is exactly the same shape as interface CCW dispatch. Pass the delegate's
        // projected name as 'ifaceFullName' and "Invoke" as 'methodName'.
        IndentedTextWriter __scratchProjectedDelegateForBody = new();
        TypedefNameWriter.WriteTypedefName(__scratchProjectedDelegateForBody, context, type, TypedefNameType.Projected, true);
        string projectedDelegateForBody = __scratchProjectedDelegateForBody.ToString();
        if (!projectedDelegateForBody.StartsWith(GlobalPrefix, StringComparison.Ordinal)) { projectedDelegateForBody = GlobalPrefix + projectedDelegateForBody; }
        AbiMethodBodyFactory.EmitDoAbiBodyIfSimple(writer, context, sig, projectedDelegateForBody, "Invoke");
        writer.WriteLine("");
        writer.Write($$"""
                public static ref readonly Guid IID
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get => ref {{iidExpr}};
                }
            }
            """, isMultiline: true);
    }

    private static void WriteDelegateVftbl(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        if (type.GenericParameters.Count > 0) { return; }
        MethodDefinition? invoke = type.GetDelegateInvoke();
        if (invoke is null) { return; }
        MethodSignatureInfo sig = new(invoke);
        string name = type.Name?.Value ?? string.Empty;
        string nameStripped = IdentifierEscaping.StripBackticks(name);

        writer.WriteLine("");
        writer.Write($$"""
            [StructLayout(LayoutKind.Sequential)]
            internal unsafe struct {{nameStripped}}Vftbl
            {
                public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, int> QueryInterface;
                public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
                public delegate* unmanaged[MemberFunction]<void*, uint> Release;
                public delegate* unmanaged[MemberFunction]<
            """, isMultiline: true);
        AbiInterfaceFactory.WriteAbiParameterTypesPointer(writer, context, sig);
        writer.Write("""
            , int> Invoke;
            }
            """, isMultiline: true);
    }

    private static void WriteNativeDelegate(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        if (type.GenericParameters.Count > 0) { return; }
        MethodDefinition? invoke = type.GetDelegateInvoke();
        if (invoke is null) { return; }
        MethodSignatureInfo sig = new(invoke);
        string name = type.Name?.Value ?? string.Empty;
        string nameStripped = IdentifierEscaping.StripBackticks(name);

        writer.WriteLine("");
        writer.Write($$"""
            public static unsafe class {{nameStripped}}NativeDelegate
            {
                public static unsafe 
            """, isMultiline: true);
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

        writer.WriteLine("");
        writer.Write($$"""
            file static class {{nameStripped}}InterfaceEntriesImpl
            {
                [FixedAddressValueType]
                public static readonly DelegateReferenceInterfaceEntries Entries;
            
                static {{nameStripped}}InterfaceEntriesImpl()
                {
                    Entries.Delegate.IID = {{iidExpr}};
                    Entries.Delegate.Vtable = {{nameStripped}}Impl.Vtable;
                    Entries.DelegateReference.IID = {{iidRefExpr}};
                    Entries.DelegateReference.Vtable = {{nameStripped}}ReferenceImpl.Vtable;
            """, isMultiline: true);
        writer.IncreaseIndent();
        writer.IncreaseIndent();
        WellKnownInterfaceEntriesEmitter.EmitDelegateReferenceWellKnownEntries(writer);
        writer.DecreaseIndent();
        writer.DecreaseIndent();
        writer.Write("""
                }
            }
            """, isMultiline: true);
    }

    public static void WriteTempDelegateEventSourceSubclass(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        // Skip generic delegates: only non-generic delegates get a per-delegate EventSource subclass.
        // Generic delegates (e.g. EventHandler<T>) use the generic EventHandlerEventSource<T> directly.
        if (type.GenericParameters.Count > 0) { return; }

        MethodDefinition? invoke = type.GetDelegateInvoke();
        if (invoke is null) { return; }
        MethodSignatureInfo sig = new(invoke);
        string name = type.Name?.Value ?? string.Empty;
        string nameStripped = IdentifierEscaping.StripBackticks(name);

        // Compute the projected type name (with global::) used as the generic argument.
        IndentedTextWriter __scratchProjectedName = new();
        TypedefNameWriter.WriteTypedefName(__scratchProjectedName, context, type, TypedefNameType.Projected, true);
        string projectedName = __scratchProjectedName.ToString();
        if (!projectedName.StartsWith(GlobalPrefix, StringComparison.Ordinal))
        {
            projectedName = GlobalPrefix + projectedName;
        }

        writer.WriteLine("");
        writer.Write($$"""
            public sealed unsafe class {{nameStripped}}EventSource : EventSource<{{projectedName}}>
            {
                /// <inheritdoc cref="EventSource{T}.EventSource"/>
                public {{nameStripped}}EventSource(WindowsRuntimeObjectReference nativeObjectReference, int index)
                    : base(nativeObjectReference, index)
                {
                }
            
                /// <inheritdoc/>
                protected override WindowsRuntimeObjectReferenceValue ConvertToUnmanaged({{projectedName}} value)
                {
                    return {{nameStripped}}Marshaller.ConvertToUnmanaged(value);
                }
            
                /// <inheritdoc/>
                protected override EventSourceState<{{projectedName}}> CreateEventSourceState()
                {
                    return new EventState(GetNativeObjectReferenceThisPtrUnsafe(), Index);
                }
            
                private sealed class EventState : EventSourceState<{{projectedName}}>
                {
                    /// <inheritdoc cref="EventSourceState{T}.EventSourceState"/>
                    public EventState(void* thisPtr, int index)
                        : base(thisPtr, index)
                    {
                    }
            
                    /// <inheritdoc/>
                    protected override {{projectedName}} GetEventInvoke()
                    {
                        return (
            """, isMultiline: true);
        for (int i = 0; i < sig.Params.Count; i++)
        {
            if (i > 0) { writer.Write(", "); }
            ParameterCategory pc = ParameterCategoryResolver.GetParamCategory(sig.Params[i]);
            if (pc == ParameterCategory.Ref) { writer.Write("in "); }
            else if (pc is ParameterCategory.Out or ParameterCategory.ReceiveArray) { writer.Write("out "); }
            string raw = sig.Params[i].Parameter.Name ?? "p";
            writer.Write(CSharpKeywords.IsKeyword(raw) ? "@" + raw : raw);
        }
        writer.Write(") => TargetDelegate.Invoke(");
        for (int i = 0; i < sig.Params.Count; i++)
        {
            if (i > 0) { writer.Write(", "); }
            ParameterCategory pc = ParameterCategoryResolver.GetParamCategory(sig.Params[i]);
            if (pc == ParameterCategory.Ref) { writer.Write("in "); }
            else if (pc is ParameterCategory.Out or ParameterCategory.ReceiveArray) { writer.Write("out "); }
            string raw = sig.Params[i].Parameter.Name ?? "p";
            writer.Write(CSharpKeywords.IsKeyword(raw) ? "@" + raw : raw);
        }
        writer.Write("""
            );
                    }
                }
            }
            """, isMultiline: true);
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

        writer.WriteLine("");
        writer.Write($$"""
            public static unsafe class {{nameStripped}}Marshaller
            {
                public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged({{fullProjected}} value)
                {
                    return WindowsRuntimeDelegateMarshaller.ConvertToUnmanaged(value, in {{iidExpr}});
                }
            
            #nullable enable
                public static {{fullProjected}}? ConvertToManaged(void* value)
                {
                    return ({{fullProjected}}?)WindowsRuntimeDelegateMarshaller.ConvertToManaged<{{nameStripped}}ComWrappersCallback>(value);
                }
            #nullable disable
            }
            """, isMultiline: true);
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

        writer.WriteLine();
        writer.Write($$"""
            file abstract unsafe class {{nameStripped}}ComWrappersCallback : IWindowsRuntimeObjectComWrappersCallback
            {
                /// <inheritdoc/>
                public static object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)
                {
                    WindowsRuntimeObjectReference valueReference = WindowsRuntimeComWrappersMarshal.CreateObjectReferenceUnsafe(
                        externalComObject: value,
                        iid: in {{iidExpr}},
                        wrapperFlags: out wrapperFlags);
            
                    return new {{fullProjected}}(valueReference.{{nameStripped}}Invoke);
                }
            }
            """, isMultiline: true);
    }

    /// <summary>
    /// Emits the <c>&lt;Name&gt;ComWrappersMarshallerAttribute</c> class.
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

        writer.WriteLine("");
        writer.Write($$"""
            internal sealed unsafe class {{nameStripped}}ComWrappersMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute
            {
                /// <inheritdoc/>
                public override void* GetOrCreateComInterfaceForObject(object value)
                {
                    return WindowsRuntimeComWrappersMarshal.GetOrCreateComInterfaceForObject(value, CreateComInterfaceFlags.TrackerSupport);
                }
            
                /// <inheritdoc/>
                public override ComInterfaceEntry* ComputeVtables(out int count)
                {
                    count = sizeof(DelegateReferenceInterfaceEntries) / sizeof(ComInterfaceEntry);
            
                    return (ComInterfaceEntry*)Unsafe.AsPointer(in {{nameStripped}}InterfaceEntriesImpl.Entries);
                }
            
                /// <inheritdoc/>
                public override object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)
                {
                    wrapperFlags = CreatedWrapperFlags.NonWrapping;
                    return WindowsRuntimeDelegateMarshaller.UnboxToManaged<{{nameStripped}}ComWrappersCallback>(value, in {{iidRefExpr}})!;
                }
            }
            """, isMultiline: true);
    }

}
