// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.InteropGenerator.Factories;

namespace WindowsRuntime.InteropGenerator.References;

/// <summary>
/// Well known references to APIs used in interop scenarios.
/// </summary>
internal sealed class WellKnownInteropReferences
{
    /// <summary>
    /// The <see cref="ModuleDefinition"/> for the interop assembly being produced.
    /// </summary>
    private readonly ModuleDefinition _interopModule;

    /// <summary>
    /// The <see cref="ModuleDefinition"/> for the Windows Runtime assembly.
    /// </summary>
    private readonly ModuleDefinition _windowsRuntimeModule;

    /// <summary>
    /// Creates a new <see cref="WellKnownInteropReferences"/> instance.
    /// </summary>
    /// <param name="interopModule">The <see cref="ModuleDefinition"/> for the interop assembly being produced.</param>
    /// <param name="windowsRuntimeModule">The <see cref="ModuleDefinition"/> for the Windows Runtime assembly.</param>
    public WellKnownInteropReferences(ModuleDefinition interopModule, ModuleDefinition windowsRuntimeModule)
    {
        _interopModule = interopModule;
        _windowsRuntimeModule = windowsRuntimeModule;
    }

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>System.Type</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference Type => field ??= _interopModule.CorLibTypeFactory.CorLibScope.CreateTypeReference("System", "Type");

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>System.ValueType</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference ValueType => field ??= _interopModule.CorLibTypeFactory.CorLibScope.CreateTypeReference("System", "ValueType");

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>System.ReadOnlySpan&lt;T&gt;</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference ReadOnlySpan => field ??= _interopModule.CorLibTypeFactory.CorLibScope.CreateTypeReference("System", "ReadOnlySpan`1");

    /// <summary>
    /// Gets the <see cref="ITypeDefOrRef"/> for <c>System.ReadOnlySpan&lt;T&gt;</c> of <see cref="char"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public ITypeDefOrRef ReadOnlySpanChar => field ??= ReadOnlySpan
        .MakeGenericInstanceType(_interopModule.CorLibTypeFactory.Char)
        .ToTypeDefOrRef();

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>System.Exception</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference Exception => field ??= _interopModule.CorLibTypeFactory.CorLibScope.CreateTypeReference("System", "Exception");

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>System.Guid</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference Guid => field ??= _interopModule.CorLibTypeFactory.CorLibScope.CreateTypeReference("System", "Guid");

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>System.MemoryExtensions</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference MemoryExtensions => field ??= _interopModule.CorLibTypeFactory.CorLibScope.CreateTypeReference("System", "MemoryExtensions");

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>System.Runtime.InteropServices.MemoryMarshal</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference MemoryMarshal => field ??= _interopModule.CorLibTypeFactory.CorLibScope.CreateTypeReference("System.Runtime.InteropServices", "MemoryMarshal");

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>System.Runtime.InteropServices.ComWrappers.ComInterfaceDispatch</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference ComInterfaceDispatch => field ??= _interopModule.CorLibTypeFactory.CorLibScope.CreateTypeReference("System.Runtime.InteropServices.ComWrappers", "ComWrappers/ComInterfaceDispatch");

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>System.Runtime.InteropServices.ComWrappers.ComInterfaceEntry</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference ComInterfaceEntry => field ??= _interopModule.CorLibTypeFactory.CorLibScope.CreateTypeReference("System.Runtime.InteropServices.ComWrappers", "ComWrappers/ComInterfaceEntry");

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>System.Runtime.InteropServices.InAttribute</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference InAttribute => field ??= _interopModule.CorLibTypeFactory.CorLibScope.CreateTypeReference("System.Runtime.InteropServices", "InAttribute");

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>System.Runtime.CompilerServices.IsReadOnlyAttribute</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference IsReadOnlyAttribute => field ??= _interopModule.CorLibTypeFactory.CorLibScope.CreateTypeReference("System.Runtime.CompilerServices", "IsReadOnlyAttribute");

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>System.Runtime.CompilerServices.FixedAddressValueTypeAttribute</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference FixedAddressValueTypeAttribute => field ??= _interopModule.CorLibTypeFactory.CorLibScope.CreateTypeReference("System.Runtime.CompilerServices", "FixedAddressValueTypeAttribute");

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>System.Runtime.CompilerServices.ScopedRefAttribute</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference ScopedRefAttribute => field ??= _interopModule.CorLibTypeFactory.CorLibScope.CreateTypeReference("System.Runtime.CompilerServices", "ScopedRefAttribute");

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>System.Runtime.CompilerServices.CallConvMemberFunction</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference CallConvMemberFunction => field ??= _interopModule.CorLibTypeFactory.CorLibScope.CreateTypeReference("System.Runtime.CompilerServices", "CallConvMemberFunction");

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference UnmanagedCallersOnlyAttribute => field ??= _interopModule.CorLibTypeFactory.CorLibScope.CreateTypeReference("System.Runtime.InteropServices", "UnmanagedCallersOnlyAttribute");

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.WindowsRuntimeClassNameAttribute</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference WindowsRuntimeClassNameAttribute => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime", "WindowsRuntimeClassNameAttribute");

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.IUnknownImpl</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference IUnknownImpl => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices", "IUnknownImpl");

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.IInspectableImpl</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference IInspectableImpl => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices", "IInspectableImpl");

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.IPropertyValueImpl</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference IPropertyValueImpl => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices", "IPropertyValueImpl");

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.IStringableImpl</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference IStringableImpl => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices", "IStringableImpl");

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.IMarshalImpl</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference IMarshalImpl => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices", "IMarshalImpl");

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.IWeakReferenceSourceImpl</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference IWeakReferenceSourceImpl => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices", "IWeakReferenceSourceImpl");

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.IAgileObjectImpl</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference IAgileObjectImpl => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices", "IAgileObjectImpl");

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.IWindowsRuntimeComWrappersCallback</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference IWindowsRuntimeComWrappersCallback => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices", "IWindowsRuntimeComWrappersCallback");

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeComWrappersMarshallerAttribute</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference WindowsRuntimeComWrappersMarshallerAttribute => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices", "WindowsRuntimeComWrappersMarshallerAttribute");

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeObjectReference</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference WindowsRuntimeObjectReference => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices", "WindowsRuntimeObjectReference");

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeObjectReferenceValue</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference WindowsRuntimeObjectReferenceValue => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices", "WindowsRuntimeObjectReferenceValue");

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeObjectMarshaller</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference WindowsRuntimeObjectMarshaller => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices", "WindowsRuntimeObjectMarshaller");

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeDelegateMarshaller</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference WindowsRuntimeDelegateMarshaller => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices", "WindowsRuntimeDelegateMarshaller");

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.RestrictedErrorInfo</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference RestrictedErrorInfo => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices", "RestrictedErrorInfo");

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.RestrictedErrorInfoExceptionMarshaller</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference RestrictedErrorInfoExceptionMarshaller => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices.Marshalling", "RestrictedErrorInfoExceptionMarshaller");

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.EventHandlerEventSource&lt;TEventArgs&gt;</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference EventHandler1EventSource => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices", "EventHandlerEventSource`1");

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.EventHandlerEventSource&lt;TSender, TEventArgs&gt;</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference EventHandler2EventSource => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices", "EventHandlerEventSource`2");

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>System.ReadOnlySpane&lt;T&gt;.this[int]</c> (of <see cref="char"/>).
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference ReadOnlySpanCharget_Item => field ??= ReadOnlySpanChar
        .CreateMemberReference("get_Item", MethodSignature.CreateInstance(
            returnType:
                new GenericParameterSignature(GenericParameterType.Type, index: 0)
                .MakeByReferenceType()
                .MakeModifierType(InAttribute, isRequired: true),
            parameterTypes: [_interopModule.CorLibTypeFactory.Int32]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>System.ReadOnlySpane&lt;T&gt;.Length</c> (of <see cref="char"/>).
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference ReadOnlySpanCharget_Length => field ??= ReadOnlySpanChar
        .CreateMemberReference("get_Length", MethodSignature.CreateInstance(_interopModule.CorLibTypeFactory.Int32));

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>System.MemoryExtensions.SequenceEqual&lt;T&gt;</c> (for <see cref="ReadOnlySpanChar"/>).
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MethodSpecification MemoryExtensionsSequenceEqualChar => field ??= MemoryExtensions
        .CreateMemberReference("SequenceEqual", MethodSignature.CreateStatic(
            returnType: _interopModule.CorLibTypeFactory.Boolean,
            genericParameterCount: 1,
            parameterTypes: [
                ReadOnlySpan.MakeGenericInstanceType(new GenericParameterSignature(GenericParameterType.Method, 0)),
                ReadOnlySpan.MakeGenericInstanceType(new GenericParameterSignature(GenericParameterType.Method, 0))]))
        .MakeGenericInstanceMethod(_interopModule.CorLibTypeFactory.Char);

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>System.Runtime.InteropServices.MemoryMarshal.CreateSpan</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference MemoryMarshalCreateSpan => field ??= MemoryMarshal
        .CreateMemberReference("CreateReadOnlySpan", MethodSignature.CreateStatic(
            returnType: ReadOnlySpan.MakeGenericInstanceType(new GenericParameterSignature(GenericParameterType.Method, 0)),
            genericParameterCount: 1,
            parameterTypes: [
                new GenericParameterSignature(GenericParameterType.Method, 0).MakeByReferenceType(),
                _interopModule.CorLibTypeFactory.Int32]));

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>System.Runtime.CompilerServices.FixedAddressValueTypeAttribute</c>'s constructor.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference FixedAddressValueTypeAttribute_ctor => field ??= FixedAddressValueTypeAttribute
        .CreateMemberReference(".ctor", MethodSignature.CreateInstance(returnType: _interopModule.CorLibTypeFactory.Void));

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>System.Runtime.CompilerServices.IsReadOnlyAttribute</c>'s constructor.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference IsReadOnlyAttribute_ctor => field ??= IsReadOnlyAttribute
        .CreateMemberReference(".ctor", MethodSignature.CreateInstance(returnType: _interopModule.CorLibTypeFactory.Void));

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>System.Runtime.CompilerServices.ScopedRefAttribute</c>'s constructor.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference ScopedRefAttribute_ctor => field ??= ScopedRefAttribute
        .CreateMemberReference(".ctor", MethodSignature.CreateInstance(returnType: _interopModule.CorLibTypeFactory.Void));

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute</c>'s constructor.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference UnmanagedCallersOnlyAttribute_ctor => field ??= UnmanagedCallersOnlyAttribute
        .CreateMemberReference(".ctor", MethodSignature.CreateInstance(returnType: _interopModule.CorLibTypeFactory.Void));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>System.Runtime.InteropServices.ComWrappers.ComInterfaceDispatch.GetInstance&lt;T&gt;(ComWrappers.ComInterfaceDispatch*)</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference ComInterfaceDispatchGetInstance => field ??= ComInterfaceDispatch
        .CreateMemberReference("GetInstance", MethodSignature.CreateStatic(
            returnType: new GenericParameterSignature(GenericParameterType.Method, index: 0),
            genericParameterCount: 1,
            parameterTypes: [_interopModule.CreateTypeReference("System.Runtime.InteropServices", "ComWrappers/ComInterfaceDispatch").MakePointerType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>System.Runtime.InteropServices.ComWrappers.ComInterfaceEntry.IID</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference ComInterfaceEntryIID => field ??= ComInterfaceEntry.CreateMemberReference("IID", new FieldSignature(Guid.ToTypeSignature(isValueType: true)));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>System.Runtime.InteropServices.ComWrappers.ComInterfaceEntry.IID</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference ComInterfaceEntryVtable => field ??= ComInterfaceEntry.CreateMemberReference("Vtable", new FieldSignature(_interopModule.CorLibTypeFactory.IntPtr));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IUnknownImpl.get_IID()</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference IUnknownImplget_IID => field ??= IUnknownImpl
        .CreateMemberReference("get_IID", MethodSignature.CreateStatic(
            returnType: WellKnownTypeSignatureFactory.InGuid(this)));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IUnknownImpl.get_Vtable()</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference IUnknownImplget_Vtable => field ??= IUnknownImpl
        .CreateMemberReference("get_Vtable", MethodSignature.CreateStatic(
            returnType: _windowsRuntimeModule.CorLibTypeFactory.IntPtr));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IInspectableImpl.get_IID()</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference IInspectableImplget_IID => field ??= IInspectableImpl
        .CreateMemberReference("get_IID", MethodSignature.CreateStatic(
            returnType: WellKnownTypeSignatureFactory.InGuid(this)));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IInspectableImpl.get_Vtable()</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference IInspectableImplget_Vtable => field ??= IInspectableImpl
        .CreateMemberReference("get_Vtable", MethodSignature.CreateStatic(
            returnType: _windowsRuntimeModule.CorLibTypeFactory.IntPtr));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IPropertyValueImpl.get_IID()</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference IPropertyValueImplget_IID => field ??= IPropertyValueImpl
        .CreateMemberReference("get_IID", MethodSignature.CreateStatic(
            returnType: WellKnownTypeSignatureFactory.InGuid(this)));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IPropertyValueImpl.get_OtherTypeVtable()</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference IPropertyValueImplget_OtherTypeVtable => field ??= IPropertyValueImpl
        .CreateMemberReference("get_OtherTypeVtable", MethodSignature.CreateStatic(
            returnType: _windowsRuntimeModule.CorLibTypeFactory.IntPtr));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IPropertyValueImpl.get_OtherTypeArrayVtable()</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference IPropertyValueImplget_OtherTypeArrayVtable => field ??= IPropertyValueImpl
        .CreateMemberReference("get_OtherTypeArrayVtable", MethodSignature.CreateStatic(
            returnType: _windowsRuntimeModule.CorLibTypeFactory.IntPtr));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IPropertyValueImpl.get_InspectableArrayVtable()</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference IPropertyValueImplget_InspectableArrayVtable => field ??= IPropertyValueImpl
        .CreateMemberReference("get_InspectableArrayVtable", MethodSignature.CreateStatic(
            returnType: _windowsRuntimeModule.CorLibTypeFactory.IntPtr));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IStringableImpl.get_IID()</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference IStringableImplget_IID => field ??= IStringableImpl
        .CreateMemberReference("get_IID", MethodSignature.CreateStatic(
            returnType: WellKnownTypeSignatureFactory.InGuid(this)));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IStringableImpl.get_Vtable()</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference IStringableImplget_Vtable => field ??= IStringableImpl
        .CreateMemberReference("get_Vtable", MethodSignature.CreateStatic(
            returnType: _windowsRuntimeModule.CorLibTypeFactory.IntPtr));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IMarshalImpl.get_IID()</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference IMarshalImplget_IID => field ??= IMarshalImpl
        .CreateMemberReference("get_IID", MethodSignature.CreateStatic(
            returnType: WellKnownTypeSignatureFactory.InGuid(this)));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IMarshalImpl.get_Vtable()</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference IMarshalImplget_Vtable => field ??= IMarshalImpl
        .CreateMemberReference("get_Vtable", MethodSignature.CreateStatic(
            returnType: _windowsRuntimeModule.CorLibTypeFactory.IntPtr));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IWeakReferenceSourceImpl.get_IID()</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference IWeakReferenceSourceImplget_IID => field ??= IWeakReferenceSourceImpl
        .CreateMemberReference("get_IID", MethodSignature.CreateStatic(
            returnType: WellKnownTypeSignatureFactory.InGuid(this)));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IWeakReferenceSourceImpl.get_Vtable()</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference IWeakReferenceSourceImplget_Vtable => field ??= IWeakReferenceSourceImpl
        .CreateMemberReference("get_Vtable", MethodSignature.CreateStatic(
            returnType: _windowsRuntimeModule.CorLibTypeFactory.IntPtr));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IAgileObjectImpl.get_IID()</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference IAgileObjectImplget_IID => field ??= IAgileObjectImpl
        .CreateMemberReference("get_IID", MethodSignature.CreateStatic(
            returnType: WellKnownTypeSignatureFactory.InGuid(this)));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IAgileObjectImpl.get_Vtable()</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference IAgileObjectImplget_Vtable => field ??= IAgileObjectImpl
        .CreateMemberReference("get_Vtable", MethodSignature.CreateStatic(
            returnType: _windowsRuntimeModule.CorLibTypeFactory.IntPtr));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeObjectReference.CreateUnsafe(void*, in Guid)</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference WindowsRuntimeObjectReferenceCreateUnsafe => field ??= WindowsRuntimeObjectReference
        .CreateMemberReference("CreateUnsafe", MethodSignature.CreateStatic(
            returnType: WindowsRuntimeObjectReference.ToTypeSignature(isValueType: false),
            parameterTypes: [
                _windowsRuntimeModule.CorLibTypeFactory.Void.MakePointerType(),
                WellKnownTypeSignatureFactory.InGuid(this)]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeObjectReference.AsValue()</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference WindowsRuntimeObjectReferenceAsValue => field ??= WindowsRuntimeObjectReference
        .CreateMemberReference("AsValue", MethodSignature.CreateInstance(WindowsRuntimeObjectReferenceValue.ToTypeSignature(isValueType: true)));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IWindowsRuntimeComWrappersCallback.CreateObject(void*)</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference IWindowsRuntimeComWrappersCallbackCreateObject => field ??= IWindowsRuntimeComWrappersCallback
        .CreateMemberReference("CreateObject", MethodSignature.CreateStatic(
            returnType: _windowsRuntimeModule.CorLibTypeFactory.Object,
            parameterTypes: [_windowsRuntimeModule.CorLibTypeFactory.Void.MakePointerType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeObjectReferenceValue.GetThisPtrUnsafe()</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference WindowsRuntimeObjectReferenceValueGetThisPtrUnsafe => field ??= WindowsRuntimeObjectReferenceValue
        .CreateMemberReference("GetThisPtrUnsafe", MethodSignature.CreateInstance(_windowsRuntimeModule.CorLibTypeFactory.Void.MakePointerType()));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeObjectReferenceValue.DetachThisPtrUnsafe()</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference WindowsRuntimeObjectReferenceValueDetachThisPtrUnsafe => field ??= WindowsRuntimeObjectReferenceValue
        .CreateMemberReference("DetachThisPtrUnsafe", MethodSignature.CreateInstance(_windowsRuntimeModule.CorLibTypeFactory.Void.MakePointerType()));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeObjectReferenceValue.Dispose()</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference WindowsRuntimeObjectReferenceValueDispose => field ??= WindowsRuntimeObjectReferenceValue
        .CreateMemberReference("Dispose", MethodSignature.CreateInstance(_windowsRuntimeModule.CorLibTypeFactory.Void));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeComWrappersMarshallerAttribute.ctor()</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference WindowsRuntimeComWrappersMarshallerAttribute_ctor => field ??= WindowsRuntimeComWrappersMarshallerAttribute
        .CreateMemberReference(".ctor", MethodSignature.CreateInstance(_windowsRuntimeModule.CorLibTypeFactory.Void));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.EventHandlerEventSource&lt;TEventArgs&gt;.ctor(...)</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference EventHandler1EventSource_ctor => field ??= EventHandler1EventSource
        .CreateMemberReference(".ctor", MethodSignature.CreateInstance(
            returnType: _windowsRuntimeModule.CorLibTypeFactory.Void,
            parameterTypes: [
                WindowsRuntimeObjectReference.ToTypeSignature(isValueType: false),
                _interopModule.CorLibTypeFactory.Int32]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.EventHandlerEventSource&lt;TSender, TEventArgs&gt;.ctor(...)</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference EventHandler2EventSource_ctor => field ??= EventHandler2EventSource
        .CreateMemberReference(".ctor", MethodSignature.CreateInstance(
            returnType: _windowsRuntimeModule.CorLibTypeFactory.Void,
            parameterTypes: [
                WindowsRuntimeObjectReference.ToTypeSignature(isValueType: false),
                _interopModule.CorLibTypeFactory.Int32]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeComWrappersMarshallerAttribute.GetOrCreateComInterfaceForObject(object)</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference WindowsRuntimeComWrappersMarshallerAttributeGetOrCreateComInterfaceForObject => field ??= WindowsRuntimeComWrappersMarshallerAttribute
        .CreateMemberReference("GetOrCreateComInterfaceForObject", MethodSignature.CreateStatic(
            returnType: _windowsRuntimeModule.CorLibTypeFactory.Void.MakePointerType(),
            parameterTypes: [_windowsRuntimeModule.CorLibTypeFactory.Object]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeComWrappersMarshallerAttribute.ComputeVtables(out int)</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference WindowsRuntimeComWrappersMarshallerAttributeComputeVtables => field ??= WindowsRuntimeComWrappersMarshallerAttribute
        .CreateMemberReference("ComputeVtables", MethodSignature.CreateStatic(
            returnType: new TypeReference(_windowsRuntimeModule.CorLibTypeFactory.CorLibScope, "System.Runtime.InteropServices"u8, "ComWrappers/ComInterfaceEntry"u8).MakePointerType(),
            parameterTypes: [_windowsRuntimeModule.CorLibTypeFactory.Int32.MakeByReferenceType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeComWrappersMarshallerAttribute.CreateObject(void*)</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference WindowsRuntimeComWrappersMarshallerAttributeCreateObject => field ??= WindowsRuntimeComWrappersMarshallerAttribute
        .CreateMemberReference("CreateObject", MethodSignature.CreateStatic(
            returnType: _windowsRuntimeModule.CorLibTypeFactory.Object,
            parameterTypes: [_windowsRuntimeModule.CorLibTypeFactory.Void.MakePointerType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeObjectMarshaller.ConvertToUnmanaged(object)</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference WindowsRuntimeObjectMarshallerConvertToUnmanaged => field ??= WindowsRuntimeObjectMarshaller
        .CreateMemberReference("ConvertToUnmanaged", MethodSignature.CreateStatic(
            returnType: WindowsRuntimeObjectReferenceValue.ToTypeSignature(isValueType: true),
            parameterTypes: [_windowsRuntimeModule.CorLibTypeFactory.Object]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeObjectMarshaller.ConvertToManaged(void*)</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference WindowsRuntimeObjectMarshallerConvertToManaged => field ??= WindowsRuntimeObjectMarshaller
        .CreateMemberReference("ConvertToManaged", MethodSignature.CreateStatic(
            returnType: new TypeReference(_windowsRuntimeModule.CorLibTypeFactory.CorLibScope, "System"u8, "Delegate"u8).ToTypeSignature(isValueType: false),
            parameterTypes: [_windowsRuntimeModule.CorLibTypeFactory.Void.MakePointerType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeDelegateMarshaller.ConvertToUnmanaged(Delegate, in Guid)</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference WindowsRuntimeDelegateMarshallerConvertToUnmanaged => field ??= WindowsRuntimeDelegateMarshaller
        .CreateMemberReference("ConvertToUnmanaged", MethodSignature.CreateStatic(
            returnType: WindowsRuntimeObjectReferenceValue.ToTypeSignature(isValueType: true),
            parameterTypes: [
                new TypeReference(_windowsRuntimeModule.CorLibTypeFactory.CorLibScope, "System"u8, "Delegate"u8).ToTypeSignature(isValueType: false),
                new TypeReference(_windowsRuntimeModule.CorLibTypeFactory.CorLibScope, "System"u8, "Guid"u8).MakeByReferenceType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeDelegateMarshaller.ConvertToManaged&lt;TCallback&gt;(void*)</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference WindowsRuntimeDelegateMarshallerConvertToManaged => field ??= WindowsRuntimeDelegateMarshaller
        .CreateMemberReference("ConvertToManaged", MethodSignature.CreateStatic(
            returnType: new TypeReference(_windowsRuntimeModule.CorLibTypeFactory.CorLibScope, "System"u8, "Delegate"u8).ToTypeSignature(isValueType: false),
            genericParameterCount: 1,
            parameterTypes: [_windowsRuntimeModule.CorLibTypeFactory.Void.MakePointerType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeDelegateMarshaller.BoxToUnmanaged(Delegate, in Guid)</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference WindowsRuntimeDelegateMarshallerBoxToUnmanaged => field ??= WindowsRuntimeDelegateMarshaller
        .CreateMemberReference("BoxToUnmanaged", MethodSignature.CreateStatic(
            returnType: WindowsRuntimeObjectReferenceValue.ToTypeSignature(isValueType: true),
            parameterTypes: [
                new TypeReference(_windowsRuntimeModule.CorLibTypeFactory.CorLibScope, "System"u8, "Delegate"u8).ToTypeSignature(isValueType: false),
                new TypeReference(_windowsRuntimeModule.CorLibTypeFactory.CorLibScope, "System"u8, "Guid"u8).MakeByReferenceType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeDelegateMarshaller.UnboxToManaged&lt;TCallback&gt;(void*)</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference WindowsRuntimeDelegateMarshallerUnboxToManaged => field ??= WindowsRuntimeDelegateMarshaller
        .CreateMemberReference("UnboxToManaged", MethodSignature.CreateStatic(
            returnType: new TypeReference(_windowsRuntimeModule.CorLibTypeFactory.CorLibScope, "System"u8, "Delegate"u8).ToTypeSignature(isValueType: false),
            genericParameterCount: 1,
            parameterTypes: [_windowsRuntimeModule.CorLibTypeFactory.Void.MakePointerType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeDelegateMarshaller.UnboxToManaged&lt;TCallback&gt;(void*, in Guid)</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference WindowsRuntimeDelegateMarshallerUnboxToManaged2 => field ??= WindowsRuntimeDelegateMarshaller
        .CreateMemberReference("UnboxToManaged", MethodSignature.CreateStatic(
            returnType: new TypeReference(_windowsRuntimeModule.CorLibTypeFactory.CorLibScope, "System"u8, "Delegate"u8).ToTypeSignature(isValueType: false),
            genericParameterCount: 1,
            parameterTypes: [
                _windowsRuntimeModule.CorLibTypeFactory.Void.MakePointerType(),
                new TypeReference(_windowsRuntimeModule.CorLibTypeFactory.CorLibScope, "System"u8, "Guid"u8).MakeByReferenceType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.RestrictedErrorInfo.ThrowExceptionForHR(int)</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference RestrictedErrorInfoThrowExceptionForHR => field ??= RestrictedErrorInfo
        .CreateMemberReference("ThrowExceptionForHR", MethodSignature.CreateStatic(
            returnType: _windowsRuntimeModule.CorLibTypeFactory.Void,
            parameterTypes: [_windowsRuntimeModule.CorLibTypeFactory.Int32]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(Exception)</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference RestrictedErrorInfoExceptionMarshallerConvertToUnmanaged => field ??= RestrictedErrorInfoExceptionMarshaller
        .CreateMemberReference("ConvertToUnmanaged", MethodSignature.CreateStatic(
            returnType: _windowsRuntimeModule.CorLibTypeFactory.Int32,
            parameterTypes: [new TypeReference(_windowsRuntimeModule.CorLibTypeFactory.CorLibScope, "System"u8, "Exception"u8).ToTypeSignature(isValueType: false)]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for the <c>.ctor</c> method of a given delegate type.
    /// </summary>
    /// <param name="delegateType">The input delegate type.</param>
    public MemberReference Delegate_ctor(TypeSignature delegateType)
    {
        // Get the special delegate constructor taking the target and function pointer. We leverage this to create
        // a delegate instance that directly wraps our 'WindowsRuntimeObjectReference' object and 'Invoke' method.
        return delegateType
            .ToTypeDefOrRef()
            .CreateMemberReference(".ctor", MethodSignature.CreateInstance(
                returnType: _interopModule.CorLibTypeFactory.Void,
                parameterTypes: [_interopModule.CorLibTypeFactory.Object, _interopModule.CorLibTypeFactory.IntPtr]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for the <c>Invoke</c> method of a given delegate type.
    /// </summary>
    /// <param name="delegateType">The input delegate type.</param>
    public MemberReference DelegateInvoke(TypeSignature delegateType)
    {
        // TODO: also handle non-generic delegate types
        return delegateType
            .ToTypeDefOrRef()
            .CreateMemberReference("Invoke", MethodSignature.CreateInstance(
                returnType: _interopModule.CorLibTypeFactory.Void,
                parameterTypes: ((GenericInstanceTypeSignature)delegateType).TypeArguments));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.EventHandlerEventSource&lt;TEventArgs&gt;.ctor(...)</c>.
    /// </summary>
    /// <param name="delegateType">The input delegate type.</param>
    public MemberReference EventHandler1EventSourceConvertToUnmanaged(TypeSignature delegateType)
    {
        return EventHandler1EventSource
            .CreateMemberReference("ConvertToUnmanaged", MethodSignature.CreateInstance(
                returnType: WindowsRuntimeObjectReferenceValue.ToTypeSignature(isValueType: true),
                parameterTypes: [delegateType]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.EventHandlerEventSource&lt;TSender, TEventArgs&gt;.ctor(...)</c>.
    /// </summary>
    /// <param name="delegateType">The input delegate type.</param>
    public MemberReference EventHandler2EventSourceConvertToUnmanaged(TypeSignature delegateType)
    {
        return EventHandler2EventSource
            .CreateMemberReference("ConvertToUnmanaged", MethodSignature.CreateInstance(
                returnType: WindowsRuntimeObjectReferenceValue.ToTypeSignature(isValueType: true),
                parameterTypes: [delegateType]));
    }
}
