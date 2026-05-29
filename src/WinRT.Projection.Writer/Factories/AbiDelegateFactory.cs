// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Generation;
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
    /// <summary>
    /// Emits the full ABI surface for a projected delegate type: marshaller class, vtable, native delegate, ComWrappers callback, interface entries, ComWrappers marshaller attribute, impl class, and IReference impl.
    /// </summary>
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
        ReferenceImplFactory.WriteReferenceImpl(writer, context, type);

        // In component mode, the original code also emits the authoring metadata wrapper for delegates.
        if (context.Settings.Component)
        {
            AbiClassFactory.WriteAuthoringMetadataType(writer, context, type);
        }
    }

    /// <summary>
    /// Emits the <c>&lt;DelegateName&gt;Impl</c> static class providing the CCW vtable for a delegate.
    /// </summary>
    private static void WriteDelegateImpl(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        if (type.GenericParameters.Count > 0)
        {
            return;
        }

        MethodDefinition? invoke = type.GetDelegateInvoke();

        if (invoke is null)
        {
            return;
        }

        MethodSignatureInfo sig = new(invoke);
        string nameStripped = type.GetStrippedName();
        IndentedTextWriterCallback iidExpr = ObjRefNameGenerator.WriteIidExpression(context, type);
        IndentedTextWriterCallback invokeParams = AbiInterfaceFactory.WriteAbiParameterTypesPointer(context, sig, includeParamNames: true);

        void WriteInvokeBody(IndentedTextWriter writer)
        {
            // Reuse the interface Do_Abi body emitter: delegates dispatch via __target.Invoke(...),
            // which is exactly the same shape as interface CCW dispatch. Pass the delegate's
            // projected name as 'ifaceFullName' and "Invoke" as 'methodName'.
            string projectedDelegateForBody = TypedefNameWriter.WriteTypedefName(context, type, TypedefNameType.Projected, true).Format();

            AbiMethodBodyFactory.EmitDoAbiBodyIfSimple(writer, context, sig, projectedDelegateForBody, "Invoke");
        }

        writer.WriteLine();
        writer.Write(isMultiline: true, $$"""
            internal static unsafe class {{nameStripped}}Impl
            {
                [FixedAddressValueType]
                private static readonly {{nameStripped}}Vftbl Vftbl;
            
                static {{nameStripped}}Impl()
                {
                    *(IUnknownVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IUnknownVftbl*)IUnknownImpl.Vtable;
                    Vftbl.Invoke = &Invoke;
                }

                public static ref readonly Guid IID
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get => ref {{iidExpr}};
                }
            
                public static nint Vtable
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get => (nint)Unsafe.AsPointer(in Vftbl);
                }
            
                [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
                private static int Invoke({{invokeParams}})
                {{WriteInvokeBody}}
            }
            """);
    }

    private static void WriteDelegateVftbl(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        if (type.GenericParameters.Count > 0)
        {
            return;
        }

        MethodDefinition? invoke = type.GetDelegateInvoke();

        if (invoke is null)
        {
            return;
        }

        string nameStripped = type.GetStrippedName();
        IndentedTextWriterCallback invokeParams = AbiInterfaceFactory.WriteAbiParameterTypesPointer(context, new MethodSignatureInfo(invoke));

        writer.WriteLine();
        writer.WriteLine(isMultiline: true, $$"""
            [StructLayout(LayoutKind.Sequential)]
            internal unsafe struct {{nameStripped}}Vftbl
            {
                public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, int> QueryInterface;
                public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
                public delegate* unmanaged[MemberFunction]<void*, uint> Release;
                public delegate* unmanaged[MemberFunction]<{{invokeParams}}, int> Invoke;
            }
            """);
    }

    private static void WriteNativeDelegate(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        if (type.GenericParameters.Count > 0)
        {
            return;
        }

        MethodDefinition? invoke = type.GetDelegateInvoke();

        if (invoke is null)
        {
            return;
        }

        MethodSignatureInfo sig = new(invoke);
        string nameStripped = type.GetStrippedName();

        writer.WriteLine();
        writer.Write(isMultiline: true, $$"""
            public static unsafe class {{nameStripped}}NativeDelegate
            {
                public static unsafe 
            """);
        IndentedTextWriterCallback ret = MethodFactory.WriteProjectionReturnType(context, sig);
        IndentedTextWriterCallback parms = MethodFactory.WriteParameterList(context, sig);
        string comma = sig.Parameters.Count > 0 ? ", " : "";
        writer.Write($"{ret} {nameStripped}Invoke(this WindowsRuntimeObjectReference thisReference{comma}{parms})");

        // Reuse the interface caller body emitter. Delegate Invoke is at vtable slot 3
        // (after QI/AddRef/Release). Functionally equivalent to the truth's
        // 'var abiInvoke = ((<Name>Vftbl*)*(void***)ThisPtr)->Invoke;' form, just routed
        // through the slot-indexed dispatch shared with interface CCW callers.
        AbiMethodBodyFactory.EmitAbiMethodBodyIfSimple(writer, context, sig, slot: 3, isNoExcept: invoke.IsNoExcept);

        writer.WriteLine("}");
    }

    private static void WriteDelegateInterfaceEntriesImpl(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        if (type.GenericParameters.Count > 0)
        {
            return;
        }

        string nameStripped = type.GetStrippedName();
        string iidExpr = ObjRefNameGenerator.WriteIidExpression(context, type).Format();
        IndentedTextWriterCallback iidRefExpr = ObjRefNameGenerator.WriteIidReferenceExpression(type);

        writer.WriteLine();
        writer.WriteLine(isMultiline: true, $$"""
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
                    Entries.IPropertyValue.IID = global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IPropertyValue;
                    Entries.IPropertyValue.Vtable = global::WindowsRuntime.InteropServices.IPropertyValueImpl.OtherTypeVtable;
                    Entries.IStringable.IID = global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IStringable;
                    Entries.IStringable.Vtable = global::WindowsRuntime.InteropServices.IStringableImpl.Vtable;
                    Entries.IWeakReferenceSource.IID = global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IWeakReferenceSource;
                    Entries.IWeakReferenceSource.Vtable = global::WindowsRuntime.InteropServices.IWeakReferenceSourceImpl.Vtable;
                    Entries.IMarshal.IID = global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IMarshal;
                    Entries.IMarshal.Vtable = global::WindowsRuntime.InteropServices.IMarshalImpl.Vtable;
                    Entries.IAgileObject.IID = global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IAgileObject;
                    Entries.IAgileObject.Vtable = global::WindowsRuntime.InteropServices.IAgileObjectImpl.Vtable;
                    Entries.IInspectable.IID = global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IInspectable;
                    Entries.IInspectable.Vtable = global::WindowsRuntime.InteropServices.IInspectableImpl.Vtable;
                    Entries.IUnknown.IID = global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IUnknown;
                    Entries.IUnknown.Vtable = global::WindowsRuntime.InteropServices.IUnknownImpl.Vtable;
                }
            }
            """);
    }

    /// <summary>
    /// Emits a per-delegate <c>EventSource</c> subclass that adapts the given non-generic
    /// delegate type to the runtime's <c>EventSource&lt;TDelegate&gt;</c> abstraction. Generic
    /// delegates (e.g. <c>EventHandler&lt;T&gt;</c>) are handled by the generic
    /// <c>EventHandlerEventSource&lt;T&gt;</c> instead and are skipped here.
    /// </summary>
    /// <param name="writer">The output writer.</param>
    /// <param name="context">The active emission context.</param>
    /// <param name="type">The delegate type to generate the EventSource subclass for.</param>
    public static void WriteDelegateEventSourceSubclass(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        // Skip generic delegates: only non-generic delegates get a per-delegate EventSource subclass.
        // Generic delegates (e.g. EventHandler<T>) use the generic EventHandlerEventSource<T> directly.
        if (type.GenericParameters.Count > 0)
        {
            return;
        }

        MethodDefinition? invoke = type.GetDelegateInvoke();

        if (invoke is null)
        {
            return;
        }

        MethodSignatureInfo sig = new(invoke);
        string nameStripped = type.GetStrippedName();

        // Compute the projected type name (with global::) used as the generic argument.
        string projectedName = TypedefNameWriter.WriteTypedefName(context, type, TypedefNameType.Projected, true).Format();

        // Emit the lambda parameter list / argument list (e.g. "a, b, out c") for the GetEventInvoke body.
        IndentedTextWriterCallback callArgs = MethodFactory.WriteCallArguments(context, sig, leadingComma: false);

        writer.WriteLine();
        writer.WriteLine(isMultiline: true, $$"""
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
                        return ({{callArgs}}) => TargetDelegate.Invoke({{callArgs}});
                    }
                }
            }
            """);
    }

    /// <summary>
    /// Emits just the <c>&lt;Name&gt;Marshaller</c> class for a delegate.
    /// </summary>
    private static void WriteDelegateMarshallerOnly(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        string nameStripped = type.GetStrippedName();
        string typeNs = type.GetRawNamespace();
        string fullProjected = TypedefNameWriter.BuildGlobalQualifiedName(typeNs, nameStripped);
        string iidExpr = ObjRefNameGenerator.WriteIidExpression(context, type).Format();

        writer.WriteLine();
        writer.WriteLine(isMultiline: true, $$"""
            public static unsafe class {{nameStripped}}Marshaller
            {
                public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged({{fullProjected}} value)
                {
                    return WindowsRuntimeDelegateMarshaller.ConvertToUnmanaged(value, in {{iidExpr}});
                }
            
                public static {{fullProjected}} ConvertToManaged(void* value)
                {
                    return ({{fullProjected}})WindowsRuntimeDelegateMarshaller.ConvertToManaged<{{nameStripped}}ComWrappersCallback>(value);
                }
            }
            """);
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
        string nameStripped = type.GetStrippedName();
        string typeNs = type.GetRawNamespace();
        string fullProjected = TypedefNameWriter.BuildGlobalQualifiedName(typeNs, nameStripped);
        string iidExpr = ObjRefNameGenerator.WriteIidExpression(context, type).Format();

        writer.WriteLine();
        writer.WriteLine(isMultiline: true, $$"""
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
            """);
    }

    /// <summary>
    /// Emits the <c>&lt;Name&gt;ComWrappersMarshallerAttribute</c> class.
    /// <c>write_delegate_com_wrappers_marshaller_attribute_impl</c>. Generic delegates are not
    /// emitted here at all (filtered out in <c>ProjectionGenerator</c>).
    /// </summary>
    private static void WriteDelegateComWrappersMarshallerAttribute(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        string nameStripped = type.GetStrippedName();
        IndentedTextWriterCallback iidRefExpr = ObjRefNameGenerator.WriteIidReferenceExpression(type);

        writer.WriteLine();
        writer.WriteLine(isMultiline: true, $$"""
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
            """);
    }

}
