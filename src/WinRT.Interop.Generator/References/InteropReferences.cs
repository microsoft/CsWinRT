// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.InteropGenerator.Factories;

#pragma warning disable IDE0032

namespace WindowsRuntime.InteropGenerator.References;

/// <summary>
/// Well known references to APIs used in interop scenarios.
/// </summary>
internal sealed class InteropReferences
{
    /// <summary>
    /// The <see cref="AsmResolver.DotNet.Signatures.CorLibTypeFactory"/> currently in use.
    /// </summary>
    private readonly CorLibTypeFactory _corLibTypeFactory;

    /// <summary>
    /// The <see cref="ModuleDefinition"/> for the Windows Runtime assembly.
    /// </summary>
    private readonly ModuleDefinition _windowsRuntimeModule;

    /// <summary>
    /// The <see cref="ModuleDefinition"/> for the Windows Runtine foundation projection assembly.
    /// </summary>
    private readonly ModuleDefinition _windowsFoundationModule;

    /// <summary>
    /// Creates a new <see cref="InteropReferences"/> instance.
    /// </summary>
    /// <param name="corLibTypeFactory">The <see cref="AsmResolver.DotNet.Signatures.CorLibTypeFactory"/> currently in use.</param>
    /// <param name="windowsRuntimeModule">The <see cref="ModuleDefinition"/> for the Windows Runtime assembly.</param>
    /// <param name="windowsFoundationModule">The <see cref="ModuleDefinition"/> for the Windows Runtine foundation projection assembly.</param>
    public InteropReferences(
        CorLibTypeFactory corLibTypeFactory,
        ModuleDefinition windowsRuntimeModule,
        ModuleDefinition windowsFoundationModule)
    {
        _corLibTypeFactory = corLibTypeFactory;
        _windowsRuntimeModule = windowsRuntimeModule;
        _windowsFoundationModule = windowsFoundationModule;
    }

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.Signatures.CorLibTypeFactory"/> instance associated with this instance.
    /// </summary>
    public CorLibTypeFactory CorLibTypeFactory => _corLibTypeFactory;

    /// <summary>
    /// Gets the <see cref="ModuleDefinition"/> for the Windows Runtime assembly.
    /// </summary>
    public ModuleDefinition WindowsRuntimeModule => _windowsRuntimeModule;

    /// <summary>
    /// Gets the <see cref="ModuleDefinition"/> for the Windows Runtine foundation projection assembly.
    /// </summary>
    public ModuleDefinition WindowsFoundationModule => _windowsFoundationModule;

    /// <summary>
    /// Gets the <see cref="AssemblyReference"/> for <c>System.Runtime.InteropServices.dll</c>.
    /// </summary>
    public AssemblyReference SystemRuntimeInteropServices => field ??= new AssemblyReference("System.Runtime.InteropServices"u8, new Version(10, 0, 0, 0));

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="System.Attribute"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference Attribute => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System"u8, "Attribute"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="System.AttributeTargets"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference AttributeTargets => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System"u8, "AttributeTargets"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="System.AttributeUsageAttribute"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference AttributeUsageAttribute => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System"u8, "AttributeUsageAttribute"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="System.Runtime.InteropServices.TypeMapAttribute{TTypeMapGroup}"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference TypeMapAttribute1 => field ??= SystemRuntimeInteropServices.CreateTypeReference("System.Runtime.InteropServices"u8, "TypeMapAttribute`1"u8);

    /// <summary>
    /// Gets the <see cref="GenericInstanceTypeSignature"/> for <see cref="System.Runtime.InteropServices.TypeMapAttribute{TTypeMapGroup}"/> of <see cref="WindowsRuntimeComWrappersTypeMapGroup"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public GenericInstanceTypeSignature TypeMapAttributeWindowsRuntimeComWrappersTypeMapGroup => field ??= TypeMapAttribute1.MakeGenericReferenceType(WindowsRuntimeComWrappersTypeMapGroup.ToReferenceTypeSignature());

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="System.Runtime.InteropServices.TypeMapAssociationAttribute{TTypeMapGroup}"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference TypeMapAssociationAttribute1 => field ??= SystemRuntimeInteropServices.CreateTypeReference("System.Runtime.InteropServices"u8, "TypeMapAssociationAttribute`1"u8);

    /// <summary>
    /// Gets the <see cref="GenericInstanceTypeSignature"/> for <see cref="System.Runtime.InteropServices.TypeMapAssociationAttribute{TTypeMapGroup}"/> of <see cref="DynamicInterfaceCastableImplementationTypeMapGroup"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public GenericInstanceTypeSignature TypeMapAssociationAttributeDynamicInterfaceCastableImplementationTypeMapGroup => field ??= TypeMapAttribute1.MakeGenericReferenceType(DynamicInterfaceCastableImplementationTypeMapGroup.ToReferenceTypeSignature());

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="System.Array"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference Array => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System"u8, "Array"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="System.Type"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference Type => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System"u8, "Type"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="System.RuntimeTypeHandle"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference RuntimeTypeHandle => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System"u8, "RuntimeTypeHandle"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="System.Delegate"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference Delegate => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System"u8, "Delegate"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="System.ValueType"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference ValueType => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System"u8, "ValueType"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="System.Span{T}"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference Span1 => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System"u8, "Span`1"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="System.ReadOnlySpan{T}"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference ReadOnlySpan1 => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System"u8, "ReadOnlySpan`1"u8);

    /// <summary>
    /// Gets the <see cref="ITypeDefOrRef"/> for <see cref="System.ReadOnlySpan{T}"/> of <see cref="byte"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public GenericInstanceTypeSignature ReadOnlySpanByte => field ??= ReadOnlySpan1.MakeGenericValueType(_corLibTypeFactory.Byte);

    /// <summary>
    /// Gets the <see cref="ITypeDefOrRef"/> for <see cref="System.ReadOnlySpan{T}"/> of <see cref="char"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public GenericInstanceTypeSignature ReadOnlySpanChar => field ??= ReadOnlySpan1.MakeGenericValueType(_corLibTypeFactory.Char);

    /// <summary>
    /// Gets the <see cref="ITypeDefOrRef"/> for <see cref="System.ReadOnlySpan{T}"/> of <see cref="ushort"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public GenericInstanceTypeSignature ReadOnlySpanUInt16 => field ??= ReadOnlySpan1.MakeGenericValueType(_corLibTypeFactory.UInt16);

    /// <summary>
    /// Gets the <see cref="ITypeDefOrRef"/> for <see cref="System.ReadOnlySpan{T}"/> of <see cref="int"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public GenericInstanceTypeSignature ReadOnlySpanInt32 => field ??= ReadOnlySpan1.MakeGenericValueType(_corLibTypeFactory.Int32);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="System.Exception"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference Exception => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System"u8, "Exception"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="System.NotSupportedException"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference NotSupportedException => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System"u8, "NotSupportedException"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="System.Guid"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference Guid => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System"u8, "Guid"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="System.EventHandler"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference EventHandler => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System"u8, "EventHandler"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="System.EventHandler{TEventArgs}"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference EventHandler1 => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System"u8, "EventHandler`1"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="System.EventHandler{TSender, TEventArgs}"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference EventHandler2 => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System"u8, "EventHandler`2"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="System.IDisposable"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference IDisposable => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System"u8, "IDisposable"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="System.IServiceProvider"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference IServiceProvider => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System"u8, "IServiceProvider"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="System.Windows.Input.ICommand"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference ICommand => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System.Windows.Input"u8, "ICommand"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="System.Collections.Specialized.INotifyCollectionChanged"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference INotifyCollectionChanged => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System.Collections.Specialized"u8, "INotifyCollectionChanged"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="System.ComponentModel.INotifyDataErrorInfo"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference INotifyDataErrorInfo => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System.ComponentModel"u8, "INotifyDataErrorInfo"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="System.ComponentModel.INotifyPropertyChanged"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference INotifyPropertyChanged => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System.ComponentModel"u8, "INotifyPropertyChanged"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="System.Collections.IEnumerator"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference IEnumerator => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System.Collections"u8, "IEnumerator"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="System.Collections.Generic.IEnumerator{T}"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference IEnumerator1 => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System.Collections.Generic"u8, "IEnumerator`1"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="System.Collections.IEnumerable"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference IEnumerable => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System.Collections"u8, "IEnumerable"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="System.Collections.Generic.IEnumerable{T}"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference IEnumerable1 => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System.Collections.Generic"u8, "IEnumerable`1"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="System.Collections.Generic.ICollection{T}"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference ICollection1 => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System.Collections.Generic"u8, "ICollection`1"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="System.Collections.Generic.IReadOnlyCollection{T}"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference IReadOnlyCollection1 => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System.Collections.Generic"u8, "IReadOnlyCollection`1"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="System.Collections.Generic.IList{T}"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference IList1 => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System.Collections.Generic"u8, "IList`1"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="System.Collections.Generic.IReadOnlyList{T}"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference IReadOnlyList1 => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System.Collections.Generic"u8, "IReadOnlyList`1"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="System.Collections.Generic.IDictionary{TKey, TValue}"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference IDictionary2 => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System.Collections.Generic"u8, "IDictionary`2"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="System.Collections.Generic.IReadOnlyDictionary{TKey, TValue}"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference IReadOnlyDictionary2 => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System.Collections.Generic"u8, "IReadOnlyDictionary`2"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference KeyValuePair => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System.Collections.Generic"u8, "KeyValuePair`2"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="System.MemoryExtensions"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference MemoryExtensions => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System"u8, "MemoryExtensions"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="System.Runtime.InteropServices.MemoryMarshal"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference MemoryMarshal => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System.Runtime.InteropServices"u8, "MemoryMarshal"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="System.Runtime.InteropServices.ComWrappers.ComInterfaceDispatch"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference ComInterfaceDispatch => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System.Runtime.InteropServices.ComWrappers"u8, "ComWrappers/ComInterfaceDispatch"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="System.Runtime.InteropServices.ComWrappers.ComInterfaceEntry"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference ComInterfaceEntry => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System.Runtime.InteropServices.ComWrappers"u8, "ComWrappers/ComInterfaceEntry"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="System.Runtime.InteropServices.CreateComInterfaceFlags"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference CreateComInterfaceFlags => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System.Runtime.InteropServices"u8, "CreateComInterfaceFlags"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="System.Runtime.InteropServices.CreatedWrapperFlags"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference CreatedWrapperFlags => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System.Runtime.InteropServices"u8, "CreatedWrapperFlags"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="System.Runtime.InteropServices.InAttribute"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference InAttribute => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System.Runtime.InteropServices"u8, "InAttribute"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="System.Runtime.InteropServices.DynamicInterfaceCastableImplementationAttribute"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference DynamicInterfaceCastableImplementationAttribute => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System.Runtime.InteropServices"u8, "DynamicInterfaceCastableImplementationAttribute"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="System.Runtime.CompilerServices.IsReadOnlyAttribute"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference IsReadOnlyAttribute => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System.Runtime.CompilerServices"u8, "IsReadOnlyAttribute"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="System.Runtime.CompilerServices.FixedAddressValueTypeAttribute"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference FixedAddressValueTypeAttribute => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System.Runtime.CompilerServices"u8, "FixedAddressValueTypeAttribute"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="System.Runtime.CompilerServices.ScopedRefAttribute"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference ScopedRefAttribute => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System.Runtime.CompilerServices"u8, "ScopedRefAttribute"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="System.Runtime.CompilerServices.CallConvMemberFunction"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference CallConvMemberFunction => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System.Runtime.CompilerServices"u8, "CallConvMemberFunction"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference UnmanagedCallersOnlyAttribute => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System.Runtime.InteropServices"u8, "UnmanagedCallersOnlyAttribute"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>ABI.System.Type</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference ABIType => field ??= _windowsRuntimeModule.CreateTypeReference("ABI.System"u8, "Type"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.WindowsRuntimeClassNameAttribute</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference WindowsRuntimeClassNameAttribute => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime"u8, "WindowsRuntimeClassNameAttribute"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeManagedOnlyTypeAttribute</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference WindowsRuntimeManagedOnlyTypeAttribute => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "WindowsRuntimeManagedOnlyTypeAttribute"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeComWrappersTypeMapGroup</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference WindowsRuntimeComWrappersTypeMapGroup => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "WindowsRuntimeComWrappersTypeMapGroup"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.DynamicInterfaceCastableImplementationTypeMapGroup</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference DynamicInterfaceCastableImplementationTypeMapGroup => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "DynamicInterfaceCastableImplementationTypeMapGroup"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.IUnknownImpl</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference IUnknownImpl => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IUnknownImpl"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.IInspectableImpl</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference IInspectableImpl => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IInspectableImpl"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.IPropertyValueImpl</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference IPropertyValueImpl => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IPropertyValueImpl"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.IStringableImpl</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference IStringableImpl => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IStringableImpl"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.IMarshalImpl</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference IMarshalImpl => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IMarshalImpl"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.IWeakReferenceSourceImpl</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference IWeakReferenceSourceImpl => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IWeakReferenceSourceImpl"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.IAgileObjectImpl</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference IAgileObjectImpl => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IAgileObjectImpl"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.IIteratorMethods</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference IIteratorMethods => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IIteratorMethods"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.IIteratorMethodsImpl&lt;T&gt;</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference IIteratorMethodsImpl1 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IIteratorMethodsImpl`1"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.IEnumeratorAdapter&lt;T&gt;</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference IEnumeratorAdapter1 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IEnumeratorAdapter`1"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.IVectorMethodsImpl&lt;T&gt;</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference IVectorMethodsImpl1 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IVectorMethodsImpl`1"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.IVectorMethodsImpl&lt;T&gt;</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference IVectorMethods1 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IVectorMethodsImpl`1"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.IVectorViewMethodsImpl&lt;T&gt;</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference IVectorViewMethods1 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IVectorViewMethodsImpl`1"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.IMapMethodsImpl&lt;K, V&gt;</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference IMapMethodsImpl2 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IMapMethodsImpl`2"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.IMapViewMethodsImpl&lt;K, V&gt;</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference IMapViewMethodsImpl2 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IMapViewMethodsImpl`2"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.IListMethods</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference IListMethods => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IListMethods"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.IReadOnlyListMethods</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference IReadOnlyListMethods => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IReadOnlyListMethods"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.IListMethods&lt;T&gt;</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference IListMethods1 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IListMethods`1"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.IReadOnlyListMethods&lt;T&gt;</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference IReadOnlyListMethods1 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IReadOnlyListMethods`1"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.IDictionaryMethods</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference IDictionaryMethods => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IDictionaryMethods"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.IDictionaryMethods&lt;TKey, TValue&gt;</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference IDictionaryMethods2 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IDictionaryMethods`2"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.IReadOnlyDictionaryMethods</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference IReadOnlyDictionaryMethods => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IReadOnlyDictionaryMethods"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.IReadOnlyDictionaryMethods&lt;TKey, TValue&gt;</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference IReadOnlyDictionaryMethods2 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IReadOnlyDictionaryMethods`2"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeObject</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference WindowsRuntimeObject => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime"u8, "WindowsRuntimeObject"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.WindowsRuntimeEnumerator&lt;T, ...&gt;</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference WindowsRuntimeEnumerator2 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime"u8, "WindowsRuntimeEnumerator`2"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.WindowsRuntimeEnumerable&lt;T, ...&gt;</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference WindowsRuntimeEnumerable2 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime"u8, "WindowsRuntimeEnumerable`2"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.WindowsRuntimeList&lt;T, ...&gt;</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference WindowsRuntimeList4 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime"u8, "WindowsRuntimeList`4"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.WindowsRuntimeReadOnlyList&lt;T, ...&gt;</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference WindowsRuntimeReadOnlyList4 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime"u8, "WindowsRuntimeReadOnlyList`4"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.WindowsRuntimeDictionary&lt;TKey, TValue, ...&gt;</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference WindowsRuntimeDictionary5 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime"u8, "WindowsRuntimeDictionary`5"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.WindowsRuntimeReadOnlyDictionary&lt;TKey, TValue, ...&gt;</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference WindowsRuntimeReadOnlyDictionary5 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime"u8, "WindowsRuntimeReadOnlyDictionary`5"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.DictionaryKeyCollection2&lt;TKey, TValue&gt;</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference DictionaryKeyCollection2 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime"u8, "DictionaryKeyCollection`2"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.DictionaryValueCollection2&lt;TKey, TValue&gt;</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference DictionaryValueCollection2 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime"u8, "DictionaryValueCollection`2"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.ReadOnlyDictionaryKeyCollection2&lt;TKey, TValue&gt;</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference ReadOnlyDictionaryKeyCollection2 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime"u8, "ReadOnlyDictionaryKeyCollection`2"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.ReadOnlyDictionaryValueCollection2&lt;TKey, TValue&gt;</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference ReadOnlyDictionaryValueCollection2 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime"u8, "ReadOnlyDictionaryValueCollection`2"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.IWindowsRuntimeInterface</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference IWindowsRuntimeInterface => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IWindowsRuntimeInterface"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.IWindowsRuntimeComWrappersCallback</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference IWindowsRuntimeComWrappersCallback => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IWindowsRuntimeComWrappersCallback"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.IWindowsRuntimeUnsealedObjectComWrappersCallback</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference IWindowsRuntimeUnsealedObjectComWrappersCallback => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IWindowsRuntimeUnsealedObjectComWrappersCallback"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.IWindowsRuntimeArrayComWrappersCallback</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference IWindowsRuntimeArrayComWrappersCallback => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IWindowsRuntimeArrayComWrappersCallback"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeComWrappersMarshallerAttribute</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference WindowsRuntimeComWrappersMarshallerAttribute => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "WindowsRuntimeComWrappersMarshallerAttribute"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeObjectReference</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference WindowsRuntimeObjectReference => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "WindowsRuntimeObjectReference"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeObjectReferenceValue</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference WindowsRuntimeObjectReferenceValue => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "WindowsRuntimeObjectReferenceValue"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeMarshal</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference WindowsRuntimeMarshal => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "WindowsRuntimeMarshal"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeObjectMarshaller</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference WindowsRuntimeObjectMarshaller => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "WindowsRuntimeObjectMarshaller"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeUnsealedObject</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference WindowsRuntimeUnsealedObjectMarshaller => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "WindowsRuntimeUnsealedObject"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeInterfaceMarshaller</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference WindowsRuntimeInterfaceMarshaller => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "WindowsRuntimeInterfaceMarshaller"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeDelegateMarshaller</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference WindowsRuntimeDelegateMarshaller => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "WindowsRuntimeDelegateMarshaller"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeArrayMarshaller</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference WindowsRuntimeArrayMarshaller => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "WindowsRuntimeArrayMarshaller"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.HStringMarshaller</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference HStringMarshaller => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "HStringMarshaller"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.RestrictedErrorInfo</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference RestrictedErrorInfo => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "RestrictedErrorInfo"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeArrayHelpers</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference WindowsRuntimeArrayHelpers => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "WindowsRuntimeArrayHelpers"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.RestrictedErrorInfoExceptionMarshaller</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference RestrictedErrorInfoExceptionMarshaller => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices.Marshalling"u8, "RestrictedErrorInfoExceptionMarshaller"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.EventHandlerEventSource&lt;TEventArgs&gt;</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference EventHandler1EventSource => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "EventHandlerEventSource`1"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.EventHandlerEventSource&lt;TSender, TEventArgs&gt;</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference EventHandler2EventSource => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "EventHandlerEventSource`2"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>Windows.Foundation.Collections.IObservableVector&lt;T&gt;</c> .
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference IObservableVector1 => field ??= _windowsFoundationModule.CreateTypeReference("Windows.Foundation.Collections"u8, "IObservableVector`1"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>Windows.Foundation.Collections.IMapChangedEventArgs&lt;K&gt;</c> .
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeReference IMapChangedEventArgs1 => field ??= _windowsFoundationModule.CreateTypeReference("Windows.Foundation.Collections"u8, "IMapChangedEventArgs`1"u8);

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Attribute.Attribute()"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference Attribute_ctor => field ??= Attribute
        .CreateMemberReference(".ctor"u8, MethodSignature.CreateInstance(_windowsRuntimeModule.CorLibTypeFactory.Void));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.NotSupportedException.NotSupportedException()"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference NotSupportedException_ctor => field ??= NotSupportedException
        .CreateMemberReference(".ctor"u8, MethodSignature.CreateInstance(_windowsRuntimeModule.CorLibTypeFactory.Void));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Type.GetTypeFromHandle"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference TypeGetTypeFromHandle => field ??= Type
        .CreateMemberReference("GetTypeFromHandle"u8, MethodSignature.CreateStatic(
            returnType: Type.ToReferenceTypeSignature(),
            parameterTypes: [RuntimeTypeHandle.ToValueTypeSignature()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Type.TypeHandle"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference Typeget_TypeHandle => field ??= Type
        .CreateMemberReference("get_TypeHandle"u8, MethodSignature.CreateInstance(
            returnType: RuntimeTypeHandle.ToValueTypeSignature()));

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="System.AttributeUsageAttribute.AttributeUsageAttribute(System.AttributeTargets)"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference AttributeUsageAttribute_ctor_AttributeTargets => field ??= AttributeUsageAttribute
        .CreateMemberReference(".ctor"u8, MethodSignature.CreateInstance(
            returnType: _corLibTypeFactory.Void,
            parameterTypes: [AttributeTargets.ToValueTypeSignature()]));

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="System.Runtime.InteropServices.TypeMapAttribute{TTypeMapGroup}.TypeMapAttribute(string, System.Type, System.Type)"/>, using <see cref="WindowsRuntimeComWrappersTypeMapGroup"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference TypeMapAttributeWindowsRuntimeComWrappersTypeMapGroup_ctor_TrimTarget => field ??= TypeMapAttribute1_ctor_TrimTarget(WindowsRuntimeComWrappersTypeMapGroup.ToReferenceTypeSignature());

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="System.Runtime.InteropServices.TypeMapAssociationAttribute{TTypeMapGroup}.TypeMapAssociationAttribute(System.Type, System.Type)"/>, using <see cref="WindowsRuntimeComWrappersTypeMapGroup"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference TypeMapAssociationAttributeWindowsRuntimeComWrappersTypeMapGroup_ctor => field ??= TypeMapAssociationAttribute1_ctor(WindowsRuntimeComWrappersTypeMapGroup.ToReferenceTypeSignature());

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="System.Runtime.InteropServices.TypeMapAssociationAttribute{TTypeMapGroup}.TypeMapAssociationAttribute(System.Type, System.Type)"/>, using <see cref="DynamicInterfaceCastableImplementationTypeMapGroup"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference TypeMapAssociationAttributeDynamicInterfaceCastableImplementationTypeMapGroup_ctor => field ??= TypeMapAssociationAttribute1_ctor(DynamicInterfaceCastableImplementationTypeMapGroup.ToReferenceTypeSignature());

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.IDisposable.Dispose"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference IDisposableDispose => field ??= IDisposable
        .CreateMemberReference("Dispose"u8, MethodSignature.CreateInstance(_corLibTypeFactory.Void));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Collections.IEnumerator.Current"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference IEnumeratorget_Current => field ??= IEnumerator
        .CreateMemberReference("get_Current"u8, MethodSignature.CreateInstance(_corLibTypeFactory.Object));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Collections.IEnumerator.MoveNext"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference IEnumeratorMoveNext => field ??= IEnumerator
        .CreateMemberReference("MoveNext"u8, MethodSignature.CreateInstance(_corLibTypeFactory.Boolean));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Collections.IEnumerator.Reset"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference IEnumeratorReset => field ??= IEnumerator
        .CreateMemberReference("Reset"u8, MethodSignature.CreateInstance(_corLibTypeFactory.Void));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Collections.IEnumerable.GetEnumerator"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference IEnumerableGetEnumerator => field ??= IEnumerable
        .CreateMemberReference("GetEnumerator"u8, MethodSignature.CreateInstance(IEnumerator.ToReferenceTypeSignature()));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.ReadOnlySpan{T}"/>'s constructor (of <see cref="byte"/>).
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference ReadOnlySpanByte_ctor => field ??= ReadOnlySpanByte
        .ToTypeDefOrRef()
        .CreateMemberReference(".ctor"u8, MethodSignature.CreateInstance(
            returnType: _windowsRuntimeModule.CorLibTypeFactory.Void,
            parameterTypes: [
                _windowsRuntimeModule.CorLibTypeFactory.Void.MakePointerType(),
                _windowsRuntimeModule.CorLibTypeFactory.Int32]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.ReadOnlySpan{T}"/>'s indexer (of <see cref="char"/>).
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference ReadOnlySpanCharget_Item => field ??= ReadOnlySpanChar
        .ToTypeDefOrRef()
        .CreateMemberReference("get_Item"u8, MethodSignature.CreateInstance(
            returnType:
                new GenericParameterSignature(GenericParameterType.Type, index: 0)
                .MakeByReferenceType()
                .MakeModifierType(InAttribute, isRequired: true),
            parameterTypes: [_corLibTypeFactory.Int32]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.ReadOnlySpan{T}.Length"/> (of <see cref="char"/>).
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference ReadOnlySpanCharget_Length => field ??= ReadOnlySpanChar
        .ToTypeDefOrRef()
        .CreateMemberReference("get_Length"u8, MethodSignature.CreateInstance(_corLibTypeFactory.Int32));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.ReadOnlySpan{T}"/>'s constructor (of <see cref="ushort"/>).
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference ReadOnlySpanUInt16_ctor => field ??= ReadOnlySpanUInt16
        .ToTypeDefOrRef()
        .CreateMemberReference(".ctor"u8, MethodSignature.CreateInstance(
            returnType: _windowsRuntimeModule.CorLibTypeFactory.Void,
            parameterTypes: [
                _windowsRuntimeModule.CorLibTypeFactory.Void.MakePointerType(),
                _windowsRuntimeModule.CorLibTypeFactory.Int32]));

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <see cref="System.MemoryExtensions.SequenceEqual{T}(System.Span{T}, System.ReadOnlySpan{T})"/> (for <see cref="ReadOnlySpanChar"/>).
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MethodSpecification MemoryExtensionsSequenceEqualChar => field ??= MemoryExtensions
        .CreateMemberReference("SequenceEqual"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.Boolean,
            genericParameterCount: 1,
            parameterTypes: [
                ReadOnlySpan1.MakeGenericValueType(new GenericParameterSignature(GenericParameterType.Method, 0)),
                ReadOnlySpan1.MakeGenericValueType(new GenericParameterSignature(GenericParameterType.Method, 0))]))
        .MakeGenericInstanceMethod(_corLibTypeFactory.Char);

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <see cref="System.MemoryExtensions.AsSpan(string?)"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference MemoryExtensionsAsSpanCharString => field ??= MemoryExtensions
        .CreateMemberReference("AsSpan"u8, MethodSignature.CreateStatic(
            returnType: ReadOnlySpanChar,
            parameterTypes: [_corLibTypeFactory.String]));

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <see cref="System.Runtime.InteropServices.MemoryMarshal.CreateSpan"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference MemoryMarshalCreateSpan => field ??= MemoryMarshal
        .CreateMemberReference("CreateReadOnlySpan"u8, MethodSignature.CreateStatic(
            returnType: ReadOnlySpan1.MakeGenericValueType(new GenericParameterSignature(GenericParameterType.Method, 0)),
            genericParameterCount: 1,
            parameterTypes: [
                new GenericParameterSignature(GenericParameterType.Method, 0).MakeByReferenceType(),
                _corLibTypeFactory.Int32]));

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="System.Runtime.CompilerServices.FixedAddressValueTypeAttribute.FixedAddressValueTypeAttribute()"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference FixedAddressValueTypeAttribute_ctor => field ??= FixedAddressValueTypeAttribute
        .CreateMemberReference(".ctor"u8, MethodSignature.CreateInstance(returnType: _corLibTypeFactory.Void));

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="System.Runtime.InteropServices.DynamicInterfaceCastableImplementationAttribute.DynamicInterfaceCastableImplementationAttribute()"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference DynamicInterfaceCastableImplementationAttribute_ctor => field ??= DynamicInterfaceCastableImplementationAttribute
        .CreateMemberReference(".ctor"u8, MethodSignature.CreateInstance(returnType: _corLibTypeFactory.Void));

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="System.Runtime.CompilerServices.IsReadOnlyAttribute.IsReadOnlyAttribute()"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference IsReadOnlyAttribute_ctor => field ??= IsReadOnlyAttribute
        .CreateMemberReference(".ctor"u8, MethodSignature.CreateInstance(returnType: _corLibTypeFactory.Void));

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="System.Runtime.CompilerServices.ScopedRefAttribute.ScopedRefAttribute()"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference ScopedRefAttribute_ctor => field ??= ScopedRefAttribute
        .CreateMemberReference(".ctor"u8, MethodSignature.CreateInstance(returnType: _corLibTypeFactory.Void));

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute.UnmanagedCallersOnlyAttribute()"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference UnmanagedCallersOnlyAttribute_ctor => field ??= UnmanagedCallersOnlyAttribute
        .CreateMemberReference(".ctor"u8, MethodSignature.CreateInstance(returnType: _corLibTypeFactory.Void));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Runtime.InteropServices.ComWrappers.ComInterfaceDispatch.GetInstance"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference ComInterfaceDispatchGetInstance => field ??= ComInterfaceDispatch
        .CreateMemberReference("GetInstance"u8, MethodSignature.CreateStatic(
            returnType: new GenericParameterSignature(GenericParameterType.Method, index: 0),
            genericParameterCount: 1,
            parameterTypes: [ComInterfaceDispatch.MakePointerType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Runtime.InteropServices.ComWrappers.ComInterfaceEntry.IID"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference ComInterfaceEntryIID => field ??= ComInterfaceEntry.CreateMemberReference("IID"u8, new FieldSignature(Guid.ToValueTypeSignature()));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Runtime.InteropServices.ComWrappers.ComInterfaceEntry.Vtable"/>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference ComInterfaceEntryVtable => field ??= ComInterfaceEntry.CreateMemberReference("Vtable"u8, new FieldSignature(_corLibTypeFactory.IntPtr));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IUnknownImpl.get_IID()</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference IUnknownImplget_IID => field ??= IUnknownImpl
        .CreateMemberReference("get_IID"u8, MethodSignature.CreateStatic(
            returnType: WellKnownTypeSignatureFactory.InGuid(this)));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IUnknownImpl.get_Vtable()</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference IUnknownImplget_Vtable => field ??= IUnknownImpl
        .CreateMemberReference("get_Vtable"u8, MethodSignature.CreateStatic(
            returnType: _windowsRuntimeModule.CorLibTypeFactory.IntPtr));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IInspectableImpl.get_IID()</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference IInspectableImplget_IID => field ??= IInspectableImpl
        .CreateMemberReference("get_IID"u8, MethodSignature.CreateStatic(
            returnType: WellKnownTypeSignatureFactory.InGuid(this)));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IInspectableImpl.get_Vtable()</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference IInspectableImplget_Vtable => field ??= IInspectableImpl
        .CreateMemberReference("get_Vtable"u8, MethodSignature.CreateStatic(
            returnType: _windowsRuntimeModule.CorLibTypeFactory.IntPtr));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IPropertyValueImpl.get_IID()</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference IPropertyValueImplget_IID => field ??= IPropertyValueImpl
        .CreateMemberReference("get_IID"u8, MethodSignature.CreateStatic(
            returnType: WellKnownTypeSignatureFactory.InGuid(this)));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IPropertyValueImpl.get_OtherTypeVtable()</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference IPropertyValueImplget_OtherTypeVtable => field ??= IPropertyValueImpl
        .CreateMemberReference("get_OtherTypeVtable"u8, MethodSignature.CreateStatic(
            returnType: _windowsRuntimeModule.CorLibTypeFactory.IntPtr));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IPropertyValueImpl.get_OtherTypeArrayVtable()</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference IPropertyValueImplget_OtherTypeArrayVtable => field ??= IPropertyValueImpl
        .CreateMemberReference("get_OtherTypeArrayVtable"u8, MethodSignature.CreateStatic(
            returnType: _windowsRuntimeModule.CorLibTypeFactory.IntPtr));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IPropertyValueImpl.get_InspectableArrayVtable()</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference IPropertyValueImplget_InspectableArrayVtable => field ??= IPropertyValueImpl
        .CreateMemberReference("get_InspectableArrayVtable"u8, MethodSignature.CreateStatic(
            returnType: _windowsRuntimeModule.CorLibTypeFactory.IntPtr));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IStringableImpl.get_IID()</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference IStringableImplget_IID => field ??= IStringableImpl
        .CreateMemberReference("get_IID"u8, MethodSignature.CreateStatic(
            returnType: WellKnownTypeSignatureFactory.InGuid(this)));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IStringableImpl.get_Vtable()</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference IStringableImplget_Vtable => field ??= IStringableImpl
        .CreateMemberReference("get_Vtable"u8, MethodSignature.CreateStatic(
            returnType: _windowsRuntimeModule.CorLibTypeFactory.IntPtr));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IMarshalImpl.get_IID()</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference IMarshalImplget_IID => field ??= IMarshalImpl
        .CreateMemberReference("get_IID"u8, MethodSignature.CreateStatic(
            returnType: WellKnownTypeSignatureFactory.InGuid(this)));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IMarshalImpl.get_Vtable()</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference IMarshalImplget_Vtable => field ??= IMarshalImpl
        .CreateMemberReference("get_Vtable"u8, MethodSignature.CreateStatic(
            returnType: _windowsRuntimeModule.CorLibTypeFactory.IntPtr));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IWeakReferenceSourceImpl.get_IID()</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference IWeakReferenceSourceImplget_IID => field ??= IWeakReferenceSourceImpl
        .CreateMemberReference("get_IID"u8, MethodSignature.CreateStatic(
            returnType: WellKnownTypeSignatureFactory.InGuid(this)));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IWeakReferenceSourceImpl.get_Vtable()</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference IWeakReferenceSourceImplget_Vtable => field ??= IWeakReferenceSourceImpl
        .CreateMemberReference("get_Vtable"u8, MethodSignature.CreateStatic(
            returnType: _windowsRuntimeModule.CorLibTypeFactory.IntPtr));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IAgileObjectImpl.get_IID()</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference IAgileObjectImplget_IID => field ??= IAgileObjectImpl
        .CreateMemberReference("get_IID"u8, MethodSignature.CreateStatic(
            returnType: WellKnownTypeSignatureFactory.InGuid(this)));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IAgileObjectImpl.get_Vtable()</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference IAgileObjectImplget_Vtable => field ??= IAgileObjectImpl
        .CreateMemberReference("get_Vtable"u8, MethodSignature.CreateStatic(
            returnType: _windowsRuntimeModule.CorLibTypeFactory.IntPtr));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeObjectReference.AsValue()</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference WindowsRuntimeObjectReferenceAsValue => field ??= WindowsRuntimeObjectReference
        .CreateMemberReference("AsValue"u8, MethodSignature.CreateInstance(WindowsRuntimeObjectReferenceValue.ToValueTypeSignature()));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IIteratorMethods.HasCurrent</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference IIteratorMethodsHasCurrent => field ??= IIteratorMethods
        .CreateMemberReference("HasCurrent"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.Boolean,
            parameterTypes: [WindowsRuntimeObjectReference.ToReferenceTypeSignature()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IIteratorMethods.MoveNext</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference IIteratorMethodsMoveNext => field ??= IIteratorMethods
        .CreateMemberReference("MoveNext"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.Boolean,
            parameterTypes: [WindowsRuntimeObjectReference.ToReferenceTypeSignature()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IListMethods.Count</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference IListMethodsCount => field ??= IListMethods
        .CreateMemberReference("Count"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.Int32,
            parameterTypes: [WindowsRuntimeObjectReference.ToReferenceTypeSignature()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IListMethods.Clear</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference IListMethodsClear => field ??= IListMethods
        .CreateMemberReference("Clear"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.Void,
            parameterTypes: [WindowsRuntimeObjectReference.ToReferenceTypeSignature()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IListMethods.RemoveAt</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference IListMethodsRemoveAt => field ??= IListMethods
        .CreateMemberReference("RemoveAt"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.Void,
            parameterTypes: [
                WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                _windowsRuntimeModule.CorLibTypeFactory.Int32]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IReadOnlyListMethods.Count</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference IReadOnlyListMethodsCount => field ??= IReadOnlyListMethods
        .CreateMemberReference("Count"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.Int32,
            parameterTypes: [WindowsRuntimeObjectReference.ToReferenceTypeSignature()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IDictionary.Count</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference IDictionaryMethodsCount => field ??= IDictionaryMethods
        .CreateMemberReference("Count"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.Int32,
            parameterTypes: [WindowsRuntimeObjectReference.ToReferenceTypeSignature()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IDictionary.Clear</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference IDictionaryMethodsClear => field ??= IDictionaryMethods
        .CreateMemberReference("Clear"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.Void,
            parameterTypes: [WindowsRuntimeObjectReference.ToReferenceTypeSignature()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IReadOnlyDictionary.Count</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference IReadOnlyDictionaryMethodsCount => field ??= IReadOnlyDictionaryMethods
        .CreateMemberReference("Count"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.Int32,
            parameterTypes: [WindowsRuntimeObjectReference.ToReferenceTypeSignature()]));

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="WindowsRuntimeObject"/>'s <c>get_NativeObjectReference</c> method.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference WindowsRuntimeObjectget_NativeObjectReference => field ??= WindowsRuntimeObject
        .CreateMemberReference("get_NativeObjectReference"u8, MethodSignature.CreateInstance(
            returnType: WindowsRuntimeObjectReference.ToReferenceTypeSignature()));

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="WindowsRuntimeObject"/>'s <c>GetObjectReferenceForInterface</c> method.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference WindowsRuntimeObjectGetObjectReferenceForInterface => field ??= WindowsRuntimeObject
        .CreateMemberReference("GetObjectReferenceForInterface"u8, MethodSignature.CreateInstance(
            returnType: WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
            parameterTypes: [RuntimeTypeHandle.ToValueTypeSignature()]));

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <c>WindowsRuntime.InteropServices.IWindowsRuntimeInterface.get_IID()</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference IWindowsRuntimeInterfaceget_IID => field ??= IWindowsRuntimeInterface
        .CreateMemberReference("get_IID"u8, MethodSignature.CreateStatic(
            returnType: WellKnownTypeSignatureFactory.InGuid(this)));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IWindowsRuntimeComWrappersCallback.CreateObject(void*)</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference IWindowsRuntimeComWrappersCallbackCreateObject => field ??= IWindowsRuntimeComWrappersCallback
        .CreateMemberReference("CreateObject"u8, MethodSignature.CreateStatic(
            returnType: _windowsRuntimeModule.CorLibTypeFactory.Object,
            parameterTypes: [_windowsRuntimeModule.CorLibTypeFactory.Void.MakePointerType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IWindowsRuntimeComWrappersCallback.CreateObject(void*)</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference IWindowsRuntimeUnsealedObjectComWrappersCallbackTryCreateObject => field ??= IWindowsRuntimeUnsealedObjectComWrappersCallback
        .CreateMemberReference("TryCreateObject"u8, MethodSignature.CreateStatic(
            returnType: _windowsRuntimeModule.CorLibTypeFactory.Boolean,
            parameterTypes: [
                _windowsRuntimeModule.CorLibTypeFactory.Void.MakePointerType(),
                ReadOnlySpanChar,
                _windowsRuntimeModule.CorLibTypeFactory.Object.MakeByReferenceType(),
                CreatedWrapperFlags.MakeByReferenceType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IWindowsRuntimeArrayComWrappersCallback.CreateArray</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference IWindowsRuntimeArrayComWrappersCallbackCreateArray => field ??= IWindowsRuntimeArrayComWrappersCallback
        .CreateMemberReference("CreateArray"u8, MethodSignature.CreateStatic(
            returnType: Array.ToReferenceTypeSignature(),
            parameterTypes: [
                _windowsRuntimeModule.CorLibTypeFactory.UInt32,
                _windowsRuntimeModule.CorLibTypeFactory.Void.MakePointerType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeObjectReferenceValue.GetThisPtrUnsafe()</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference WindowsRuntimeObjectReferenceValueGetThisPtrUnsafe => field ??= WindowsRuntimeObjectReferenceValue
        .CreateMemberReference("GetThisPtrUnsafe"u8, MethodSignature.CreateInstance(_windowsRuntimeModule.CorLibTypeFactory.Void.MakePointerType()));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeObjectReferenceValue.DetachThisPtrUnsafe()</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference WindowsRuntimeObjectReferenceValueDetachThisPtrUnsafe => field ??= WindowsRuntimeObjectReferenceValue
        .CreateMemberReference("DetachThisPtrUnsafe"u8, MethodSignature.CreateInstance(_windowsRuntimeModule.CorLibTypeFactory.Void.MakePointerType()));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeObjectReferenceValue.Dispose()</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference WindowsRuntimeObjectReferenceValueDispose => field ??= WindowsRuntimeObjectReferenceValue
        .CreateMemberReference("Dispose"u8, MethodSignature.CreateInstance(_windowsRuntimeModule.CorLibTypeFactory.Void));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeComWrappersMarshallerAttribute.ctor()</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference WindowsRuntimeComWrappersMarshallerAttribute_ctor => field ??= WindowsRuntimeComWrappersMarshallerAttribute
        .CreateMemberReference(".ctor"u8, MethodSignature.CreateInstance(_windowsRuntimeModule.CorLibTypeFactory.Void));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.EventHandlerEventSource&lt;TEventArgs&gt;.ctor(...)</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference EventHandler1EventSource_ctor => field ??= EventHandler1EventSource
        .CreateMemberReference(".ctor"u8, MethodSignature.CreateInstance(
            returnType: _windowsRuntimeModule.CorLibTypeFactory.Void,
            parameterTypes: [
                WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                _windowsRuntimeModule.CorLibTypeFactory.Int32]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.EventHandlerEventSource&lt;TSender, TEventArgs&gt;.ctor(...)</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference EventHandler2EventSource_ctor => field ??= EventHandler2EventSource
        .CreateMemberReference(".ctor"u8, MethodSignature.CreateInstance(
            returnType: _windowsRuntimeModule.CorLibTypeFactory.Void,
            parameterTypes: [
                WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                _windowsRuntimeModule.CorLibTypeFactory.Int32]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeComWrappersMarshallerAttribute.GetOrCreateComInterfaceForObject(object)</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference WindowsRuntimeComWrappersMarshallerAttributeGetOrCreateComInterfaceForObject => field ??= WindowsRuntimeComWrappersMarshallerAttribute
        .CreateMemberReference("GetOrCreateComInterfaceForObject"u8, MethodSignature.CreateStatic(
            returnType: _windowsRuntimeModule.CorLibTypeFactory.Void.MakePointerType(),
            parameterTypes: [_windowsRuntimeModule.CorLibTypeFactory.Object]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeComWrappersMarshallerAttribute.ComputeVtables(out int)</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference WindowsRuntimeComWrappersMarshallerAttributeComputeVtables => field ??= WindowsRuntimeComWrappersMarshallerAttribute
        .CreateMemberReference("ComputeVtables"u8, MethodSignature.CreateStatic(
            returnType: new TypeReference(_windowsRuntimeModule.CorLibTypeFactory.CorLibScope, "System.Runtime.InteropServices"u8, "ComWrappers/ComInterfaceEntry"u8).MakePointerType(),
            parameterTypes: [_windowsRuntimeModule.CorLibTypeFactory.Int32.MakeByReferenceType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeComWrappersMarshallerAttribute.CreateObject(void*)</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference WindowsRuntimeComWrappersMarshallerAttributeCreateObject => field ??= WindowsRuntimeComWrappersMarshallerAttribute
        .CreateMemberReference("CreateObject"u8, MethodSignature.CreateStatic(
            returnType: _windowsRuntimeModule.CorLibTypeFactory.Object,
            parameterTypes: [
                _windowsRuntimeModule.CorLibTypeFactory.Void.MakePointerType(),
                CreatedWrapperFlags.ToValueTypeSignature()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeMarshal.GetOrCreateComInterfaceForObject</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference WindowsRuntimeMarshalGetOrCreateComInterfaceForObject => field ??= WindowsRuntimeMarshal
        .CreateMemberReference("GetOrCreateComInterfaceForObject"u8, MethodSignature.CreateStatic(
            returnType: _windowsRuntimeModule.CorLibTypeFactory.Void.MakePointerType(),
            parameterTypes: [
                _windowsRuntimeModule.CorLibTypeFactory.Object,
                CreateComInterfaceFlags.ToValueTypeSignature()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeMarshal.CreateObjectReference</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference WindowsRuntimeMarshalCreateObjectReference => field ??= WindowsRuntimeMarshal
        .CreateMemberReference("CreateObjectReference"u8, MethodSignature.CreateStatic(
            returnType: WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
            parameterTypes: [
                _windowsRuntimeModule.CorLibTypeFactory.Void.MakePointerType(),
                Guid.MakeByReferenceType(),
                CreatedWrapperFlags.MakeByReferenceType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeMarshal.CreateObjectReferenceUnsafe</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference WindowsRuntimeMarshalCreateObjectReferenceUnsafe => field ??= WindowsRuntimeMarshal
        .CreateMemberReference("CreateObjectReferenceUnsafe"u8, MethodSignature.CreateStatic(
            returnType: WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
            parameterTypes: [
                _windowsRuntimeModule.CorLibTypeFactory.Void.MakePointerType(),
                Guid.MakeByReferenceType(),
                CreatedWrapperFlags.MakeByReferenceType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeObjectMarshaller.ConvertToUnmanaged(object)</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference WindowsRuntimeObjectMarshallerConvertToUnmanaged => field ??= WindowsRuntimeObjectMarshaller
        .CreateMemberReference("ConvertToUnmanaged"u8, MethodSignature.CreateStatic(
            returnType: WindowsRuntimeObjectReferenceValue.ToValueTypeSignature(),
            parameterTypes: [_windowsRuntimeModule.CorLibTypeFactory.Object]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeObjectMarshaller.ConvertToManaged(void*)</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference WindowsRuntimeObjectMarshallerConvertToManaged => field ??= WindowsRuntimeObjectMarshaller
        .CreateMemberReference("ConvertToManaged"u8, MethodSignature.CreateStatic(
            returnType: Delegate.ToReferenceTypeSignature(),
            parameterTypes: [_windowsRuntimeModule.CorLibTypeFactory.Void.MakePointerType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeObjectMarshaller.Free(void*)</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference WindowsRuntimeObjectMarshallerFree => field ??= WindowsRuntimeObjectMarshaller
        .CreateMemberReference("Free"u8, MethodSignature.CreateStatic(
            returnType: _windowsRuntimeModule.CorLibTypeFactory.Void,
            parameterTypes: [_windowsRuntimeModule.CorLibTypeFactory.Void.MakePointerType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeUnsealedObject.ConvertToManaged</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference WindowsRuntimeUnsealedObjectMarshallerConvertToManaged => field ??= WindowsRuntimeUnsealedObjectMarshaller
        .CreateMemberReference("ConvertToManaged"u8, MethodSignature.CreateStatic(
            returnType: Delegate.ToReferenceTypeSignature(),
            genericParameterCount: 1,
            parameterTypes: [_windowsRuntimeModule.CorLibTypeFactory.Void.MakePointerType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeInterfaceMarshaller.ConvertToUnmanaged</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference WindowsRuntimeInterfaceMarshallerConvertToUnmanaged => field ??= WindowsRuntimeInterfaceMarshaller
        .CreateMemberReference("ConvertToUnmanaged"u8, MethodSignature.CreateStatic(
            returnType: WindowsRuntimeObjectReferenceValue.ToValueTypeSignature(),
            genericParameterCount: 1,
            parameterTypes: [
                new GenericParameterSignature(GenericParameterType.Method, 0),
                Guid.MakeByReferenceType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeDelegateMarshaller.ConvertToUnmanaged(Delegate, in Guid)</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference WindowsRuntimeDelegateMarshallerConvertToUnmanaged => field ??= WindowsRuntimeDelegateMarshaller
        .CreateMemberReference("ConvertToUnmanaged"u8, MethodSignature.CreateStatic(
            returnType: WindowsRuntimeObjectReferenceValue.ToValueTypeSignature(),
            parameterTypes: [
                Delegate.ToReferenceTypeSignature(),
                Guid.MakeByReferenceType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeDelegateMarshaller.ConvertToManaged&lt;TCallback&gt;(void*)</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference WindowsRuntimeDelegateMarshallerConvertToManaged => field ??= WindowsRuntimeDelegateMarshaller
        .CreateMemberReference("ConvertToManaged"u8, MethodSignature.CreateStatic(
            returnType: Delegate.ToReferenceTypeSignature(),
            genericParameterCount: 1,
            parameterTypes: [_windowsRuntimeModule.CorLibTypeFactory.Void.MakePointerType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeDelegateMarshaller.BoxToUnmanaged(Delegate, in Guid)</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference WindowsRuntimeDelegateMarshallerBoxToUnmanaged => field ??= WindowsRuntimeDelegateMarshaller
        .CreateMemberReference("BoxToUnmanaged"u8, MethodSignature.CreateStatic(
            returnType: WindowsRuntimeObjectReferenceValue.ToValueTypeSignature(),
            parameterTypes: [
                Delegate.ToReferenceTypeSignature(),
                Guid.MakeByReferenceType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeDelegateMarshaller.UnboxToManaged&lt;TCallback&gt;(void*)</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference WindowsRuntimeDelegateMarshallerUnboxToManaged => field ??= WindowsRuntimeDelegateMarshaller
        .CreateMemberReference("UnboxToManaged"u8, MethodSignature.CreateStatic(
            returnType: Delegate.ToReferenceTypeSignature(),
            genericParameterCount: 1,
            parameterTypes: [_windowsRuntimeModule.CorLibTypeFactory.Void.MakePointerType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeDelegateMarshaller.UnboxToManaged&lt;TCallback&gt;(void*, in Guid)</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference WindowsRuntimeDelegateMarshallerUnboxToManaged2 => field ??= WindowsRuntimeDelegateMarshaller
        .CreateMemberReference("UnboxToManaged"u8, MethodSignature.CreateStatic(
            returnType: Delegate.ToReferenceTypeSignature(),
            genericParameterCount: 1,
            parameterTypes: [
                _windowsRuntimeModule.CorLibTypeFactory.Void.MakePointerType(),
                Guid.MakeByReferenceType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeArrayMarshaller.UnboxToManaged&lt;TCallback&gt;(void*, in Guid)</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference WindowsRuntimeArrayMarshallerUnboxToManaged => field ??= WindowsRuntimeArrayMarshaller
        .CreateMemberReference("UnboxToManaged"u8, MethodSignature.CreateStatic(
            returnType: Array.ToReferenceTypeSignature(),
            genericParameterCount: 1,
            parameterTypes: [
                _windowsRuntimeModule.CorLibTypeFactory.Void.MakePointerType(),
                Guid.MakeByReferenceType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.HStringMarshaller.ConvertToUnmanaged</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference HStringMarshallerConvertToUnmanaged => field ??= HStringMarshaller
        .CreateMemberReference("ConvertToUnmanaged"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.Void.MakePointerType(),
            parameterTypes: [ReadOnlySpanChar]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.HStringMarshaller.ConvertToManaged</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference HStringMarshallerConvertToManaged => field ??= HStringMarshaller
        .CreateMemberReference("ConvertToManaged"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.String,
            parameterTypes: [_corLibTypeFactory.Void.MakePointerType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.HStringMarshaller.Free</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference HStringMarshallerFree => field ??= HStringMarshaller
        .CreateMemberReference("Free"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.Void,
            parameterTypes: [_corLibTypeFactory.Void.MakePointerType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.RestrictedErrorInfo.ThrowExceptionForHR(int)</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference RestrictedErrorInfoThrowExceptionForHR => field ??= RestrictedErrorInfo
        .CreateMemberReference("ThrowExceptionForHR"u8, MethodSignature.CreateStatic(
            returnType: _windowsRuntimeModule.CorLibTypeFactory.Void,
            parameterTypes: [_windowsRuntimeModule.CorLibTypeFactory.Int32]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeArrayHelpers.FreeHStringArrayUnsafe</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference WindowsRuntimeArrayHelpersFreeHStringArrayUnsafe => field ??= WindowsRuntimeArrayHelpers
        .CreateMemberReference("FreeHStringArrayUnsafe"u8, MethodSignature.CreateStatic(
            returnType: _windowsRuntimeModule.CorLibTypeFactory.Void,
            parameterTypes: [
                _windowsRuntimeModule.CorLibTypeFactory.UInt32,
                _windowsRuntimeModule.CorLibTypeFactory.Void.MakePointerType().MakePointerType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeArrayHelpers.FreeObjectArrayUnsafe</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference WindowsRuntimeArrayHelpersFreeObjectArrayUnsafe => field ??= WindowsRuntimeArrayHelpers
        .CreateMemberReference("FreeObjectArrayUnsafe"u8, MethodSignature.CreateStatic(
            returnType: _windowsRuntimeModule.CorLibTypeFactory.Void,
            parameterTypes: [
                _windowsRuntimeModule.CorLibTypeFactory.UInt32,
                _windowsRuntimeModule.CorLibTypeFactory.Void.MakePointerType().MakePointerType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeArrayHelpers.FreeTypeArrayUnsafe</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference WindowsRuntimeArrayHelpersFreeTypeArrayUnsafe => field ??= WindowsRuntimeArrayHelpers
        .CreateMemberReference("FreeTypeArrayUnsafe"u8, MethodSignature.CreateStatic(
            returnType: _windowsRuntimeModule.CorLibTypeFactory.Void,
            parameterTypes: [
                _windowsRuntimeModule.CorLibTypeFactory.UInt32,
                ABIType.MakePointerType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeArrayHelpers.FreeBlittableArrayUnsafe</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference WindowsRuntimeArrayHelpersFreeBlittableArrayUnsafe => field ??= WindowsRuntimeArrayHelpers
        .CreateMemberReference("FreeBlittableArrayUnsafe"u8, MethodSignature.CreateStatic(
            returnType: _windowsRuntimeModule.CorLibTypeFactory.Void,
            parameterTypes: [
                _windowsRuntimeModule.CorLibTypeFactory.UInt32,
                _windowsRuntimeModule.CorLibTypeFactory.Void.MakePointerType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(Exception)</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference RestrictedErrorInfoExceptionMarshallerConvertToUnmanaged => field ??= RestrictedErrorInfoExceptionMarshaller
        .CreateMemberReference("ConvertToUnmanaged"u8, MethodSignature.CreateStatic(
            returnType: _windowsRuntimeModule.CorLibTypeFactory.Int32,
            parameterTypes: [new TypeReference(_windowsRuntimeModule.CorLibTypeFactory.CorLibScope, "System"u8, "Exception"u8).ToReferenceTypeSignature()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.ReadOnlySpan{T}"/>'s constructor (of an SZ array type).
    /// </summary>
    public MemberReference ReadOnlySpan1_ctor(SzArrayTypeSignature arrayType)
    {
        return ReadOnlySpan1
            .MakeGenericValueType(arrayType.BaseType)
            .ToTypeDefOrRef()
            .CreateMemberReference(".ctor"u8, MethodSignature.CreateInstance(
                returnType: _windowsRuntimeModule.CorLibTypeFactory.Void,
                parameterTypes: [arrayType]));
    }

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="System.Runtime.InteropServices.TypeMapAttribute{TTypeMapGroup}.TypeMapAttribute(string, System.Type, System.Type)"/>.
    /// </summary>
    /// <param name="typeMapGroup">The type map group to use.</param>
    public MemberReference TypeMapAttribute1_ctor_TrimTarget(TypeSignature typeMapGroup)
    {
        return TypeMapAttribute1
            .MakeGenericReferenceType(typeMapGroup)
            .ToTypeDefOrRef()
            .CreateMemberReference(".ctor"u8, MethodSignature.CreateInstance(
                returnType: _corLibTypeFactory.Void,
                parameterTypes: [
                    _corLibTypeFactory.String,
                    Type.ToReferenceTypeSignature(),
                    Type.ToReferenceTypeSignature()]));
    }

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="System.Runtime.InteropServices.TypeMapAssociationAttribute{TTypeMapGroup}.TypeMapAssociationAttribute(System.Type, System.Type)"/>.
    /// </summary>
    /// <param name="typeMapGroup">The type map group to use.</param>
    public MemberReference TypeMapAssociationAttribute1_ctor(TypeSignature typeMapGroup)
    {
        return TypeMapAssociationAttribute1
            .MakeGenericReferenceType(typeMapGroup)
            .ToTypeDefOrRef()
            .CreateMemberReference(".ctor"u8, MethodSignature.CreateInstance(
            returnType: _corLibTypeFactory.Void,
            parameterTypes: [
                Type.ToReferenceTypeSignature(),
                Type.ToReferenceTypeSignature()]));
    }

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
            .CreateMemberReference(".ctor"u8, MethodSignature.CreateInstance(
                returnType: _corLibTypeFactory.Void,
                parameterTypes: [_corLibTypeFactory.Object, _corLibTypeFactory.IntPtr]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for the <c>Invoke</c> method of a given delegate type.
    /// </summary>
    /// <param name="delegateType">The input delegate type.</param>
    public MemberReference DelegateInvoke(GenericInstanceTypeSignature delegateType)
    {
        // Get the 'Invoke' method of the delegate type (this will remove the type arguments)
        MethodDefinition invokeMethod = delegateType.Resolve()!.GetMethod("Invoke"u8);

        // Construct the generic signature for the method with the context of the input delegate.
        // We can use this to get all the parameters, which might be any combination of explicitly
        // declared types, and constructed generic type parameters. Also, any number of them.
        MethodSignature invokeSignature = invokeMethod.Signature!.InstantiateGenericTypes(new GenericContext(delegateType, null));

        // Create the actual member reference to use when emitting calls to the 'Invoke' method
        return delegateType
            .ToTypeDefOrRef()
            .CreateMemberReference("Invoke"u8, MethodSignature.CreateInstance(
                returnType: invokeSignature.ReturnType,
                parameterTypes: invokeSignature.ParameterTypes));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Collections.Generic.IEnumerator{T}.Current"/>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference IEnumerator1get_Current(TypeSignature elementType)
    {
        return IEnumerator1
            .MakeGenericReferenceType(elementType)
            .ToTypeDefOrRef()
            .CreateMemberReference("get_Current"u8, MethodSignature.CreateInstance(new GenericParameterSignature(GenericParameterType.Type, 0)));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IIteratorMethodsImpl&lt;T&gt;.Current</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference IIteratorMethodsImpl1Current(TypeSignature elementType)
    {
        return IIteratorMethodsImpl1
            .MakeGenericReferenceType(elementType)
            .ToTypeDefOrRef()
            .CreateMemberReference("Current"u8, MethodSignature.CreateStatic(
                returnType: new GenericParameterSignature(GenericParameterType.Type, 0),
                parameterTypes: [WindowsRuntimeObjectReference.ToReferenceTypeSignature()]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Collections.Generic.IEnumerable{T}.GetEnumerator"/>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference IEnumerable1GetEnumerator(TypeSignature elementType)
    {
        return IEnumerable1
            .MakeGenericReferenceType(elementType)
            .ToTypeDefOrRef()
            .CreateMemberReference("GetEnumerator"u8, MethodSignature.CreateInstance(
                returnType: IEnumerator1.MakeGenericReferenceType(new GenericParameterSignature(GenericParameterType.Type, 0))));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IEnumeratorAdapter&lt;T&gt;.GetInstance</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference IEnumeratorAdapter1GetInstance(TypeSignature elementType)
    {
        return IEnumeratorAdapter1
            .MakeGenericReferenceType(elementType)
            .ToTypeDefOrRef()
            .CreateMemberReference("GetInstance"u8, MethodSignature.CreateStatic(
                returnType: IEnumeratorAdapter1.MakeGenericReferenceType(new GenericParameterSignature(GenericParameterType.Type, 0)),
                parameterTypes: [IEnumerator1.MakeGenericReferenceType(new GenericParameterSignature(GenericParameterType.Type, 0))]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IEnumeratorAdapter&lt;T&gt;.Current</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference IEnumeratorAdapter1get_Current(TypeSignature elementType)
    {
        return IEnumeratorAdapter1
            .MakeGenericReferenceType(elementType)
            .ToTypeDefOrRef()
            .CreateMemberReference("get_Current"u8, MethodSignature.CreateInstance(
                returnType: new GenericParameterSignature(GenericParameterType.Type, 0)));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IEnumeratorAdapter&lt;T&gt;.HasCurrent</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference IEnumeratorAdapter1get_HasCurrent(TypeSignature elementType)
    {
        return IEnumeratorAdapter1
            .MakeGenericReferenceType(elementType)
            .ToTypeDefOrRef()
            .CreateMemberReference("get_HasCurrent"u8, MethodSignature.CreateInstance(
                returnType: _windowsRuntimeModule.CorLibTypeFactory.Boolean));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IEnumeratorAdapter&lt;T&gt;.MoveNext</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference IEnumeratorAdapter1MoveNext(TypeSignature elementType)
    {
        return IEnumeratorAdapter1
            .MakeGenericReferenceType(elementType)
            .ToTypeDefOrRef()
            .CreateMemberReference("MoveNext"u8, MethodSignature.CreateInstance(
                returnType: _windowsRuntimeModule.CorLibTypeFactory.Boolean));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IVectorMethodsImpl&lt;T&gt;.GetAt</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference IVectorMethodsImpl1GetAt(TypeSignature elementType)
    {
        return IVectorMethodsImpl1
            .MakeGenericReferenceType(elementType)
            .ToTypeDefOrRef()
            .CreateMemberReference("GetAt"u8, MethodSignature.CreateInstance(
                returnType: new GenericParameterSignature(GenericParameterType.Type, 0),
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    _windowsRuntimeModule.CorLibTypeFactory.UInt32]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IVectorMethodsImpl&lt;T&gt;.SetAt</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference IVectorMethodsImpl1SetAt(TypeSignature elementType)
    {
        return IVectorMethodsImpl1
            .MakeGenericReferenceType(elementType)
            .ToTypeDefOrRef()
            .CreateMemberReference("SetAt"u8, MethodSignature.CreateInstance(
                returnType: _windowsRuntimeModule.CorLibTypeFactory.Void,
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    _windowsRuntimeModule.CorLibTypeFactory.UInt32,
                    new GenericParameterSignature(GenericParameterType.Type, 0)]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IVectorMethodsImpl&lt;T&gt;.Append</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference IVectorMethodsImpl1Append(TypeSignature elementType)
    {
        return IVectorMethodsImpl1
            .MakeGenericReferenceType(elementType)
            .ToTypeDefOrRef()
            .CreateMemberReference("Append"u8, MethodSignature.CreateInstance(
                returnType: _windowsRuntimeModule.CorLibTypeFactory.Void,
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    new GenericParameterSignature(GenericParameterType.Type, 0)]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IVectorMethodsImpl&lt;T&gt;.IndexOf</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference IVectorMethodsImpl1IndexOf(TypeSignature elementType)
    {
        return IVectorMethodsImpl1
            .MakeGenericReferenceType(elementType)
            .ToTypeDefOrRef()
            .CreateMemberReference("IndexOf"u8, MethodSignature.CreateInstance(
                returnType: _windowsRuntimeModule.CorLibTypeFactory.Boolean,
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    new GenericParameterSignature(GenericParameterType.Type, 0),
                    _windowsRuntimeModule.CorLibTypeFactory.UInt32.MakeByReferenceType()]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IVectorMethodsImpl&lt;T&gt;.InsertAt</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference IVectorMethodsImpl1InsertAt(TypeSignature elementType)
    {
        return IVectorMethodsImpl1
            .MakeGenericReferenceType(elementType)
            .ToTypeDefOrRef()
            .CreateMemberReference("InsertAt"u8, MethodSignature.CreateInstance(
                returnType: _windowsRuntimeModule.CorLibTypeFactory.Boolean,
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    _windowsRuntimeModule.CorLibTypeFactory.UInt32,
                    new GenericParameterSignature(GenericParameterType.Type, 0)]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IVectorViewMethods&lt;T&gt;.GetAt</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference IVectorViewMethods1GetAt(TypeSignature elementType)
    {
        return IVectorViewMethods1
            .MakeGenericReferenceType(elementType)
            .ToTypeDefOrRef()
            .CreateMemberReference("GetAt"u8, MethodSignature.CreateInstance(
                returnType: new GenericParameterSignature(GenericParameterType.Type, 0),
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    _windowsRuntimeModule.CorLibTypeFactory.UInt32]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Collections.Generic.ICollection{T}.Count"/>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference ICollection1get_Count(TypeSignature elementType)
    {
        return ICollection1
            .MakeGenericReferenceType(elementType)
            .ToTypeDefOrRef()
            .CreateMemberReference("get_Count"u8, MethodSignature.CreateInstance(_corLibTypeFactory.Int32));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Collections.Generic.ICollection{T}.IsReadOnly"/>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference ICollection1get_IsReadOnly(TypeSignature elementType)
    {
        return ICollection1
            .MakeGenericReferenceType(elementType)
            .ToTypeDefOrRef()
            .CreateMemberReference("get_IsReadOnly"u8, MethodSignature.CreateInstance(_corLibTypeFactory.Boolean));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Collections.Generic.ICollection{T}.Add"/>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference ICollection1Add(TypeSignature elementType)
    {
        return ICollection1
            .MakeGenericReferenceType(elementType)
            .ToTypeDefOrRef()
            .CreateMemberReference("Add"u8, MethodSignature.CreateInstance(
                returnType: _corLibTypeFactory.Void,
                parameterTypes: [new GenericParameterSignature(GenericParameterType.Type, 0)]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Collections.Generic.ICollection{T}.Clear"/>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference ICollection1Clear(TypeSignature elementType)
    {
        return ICollection1
            .MakeGenericReferenceType(elementType)
            .ToTypeDefOrRef()
            .CreateMemberReference("Clear"u8, MethodSignature.CreateInstance(_corLibTypeFactory.Void));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Collections.Generic.ICollection{T}.Contains"/>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference ICollection1Contains(TypeSignature elementType)
    {
        return ICollection1
            .MakeGenericReferenceType(elementType)
            .ToTypeDefOrRef()
            .CreateMemberReference("Contains"u8, MethodSignature.CreateInstance(
                returnType: _corLibTypeFactory.Boolean,
                parameterTypes: [new GenericParameterSignature(GenericParameterType.Type, 0)]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Collections.Generic.ICollection{T}.CopyTo"/>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference ICollection1CopyTo(TypeSignature elementType)
    {
        return ICollection1
            .MakeGenericReferenceType(elementType)
            .ToTypeDefOrRef()
            .CreateMemberReference("Contains"u8, MethodSignature.CreateInstance(
                returnType: _corLibTypeFactory.Void,
                parameterTypes: [
                    new GenericParameterSignature(GenericParameterType.Type, 0).MakeSzArrayType(),
                    _corLibTypeFactory.Int32]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Collections.Generic.ICollection{T}.Remove"/>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference ICollection1Remove(TypeSignature elementType)
    {
        return ICollection1
            .MakeGenericReferenceType(elementType)
            .ToTypeDefOrRef()
            .CreateMemberReference("Remove"u8, MethodSignature.CreateInstance(
                returnType: _corLibTypeFactory.Boolean,
                parameterTypes: [new GenericParameterSignature(GenericParameterType.Type, 0)]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Collections.Generic.IReadOnlyCollection{T}.Count"/>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference IReadOnlyCollection1get_Count(TypeSignature elementType)
    {
        return IReadOnlyCollection1
            .MakeGenericReferenceType(elementType)
            .ToTypeDefOrRef()
            .CreateMemberReference("get_Count"u8, MethodSignature.CreateInstance(_corLibTypeFactory.Int32));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Collections.Generic.IList{T}.this"/>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference IList1get_Item(TypeSignature elementType)
    {
        return IList1
            .MakeGenericReferenceType(elementType)
            .ToTypeDefOrRef()
            .CreateMemberReference("get_Item"u8, MethodSignature.CreateInstance(
                returnType: new GenericParameterSignature(GenericParameterType.Type, 0),
                parameterTypes: [_corLibTypeFactory.Int32]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Collections.Generic.IList{T}.this"/>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference IList1set_Item(TypeSignature elementType)
    {
        return IList1
            .MakeGenericReferenceType(elementType)
            .ToTypeDefOrRef()
            .CreateMemberReference("set_Item"u8, MethodSignature.CreateInstance(
                returnType: _corLibTypeFactory.Void,
                parameterTypes: [
                    _corLibTypeFactory.Int32,
                    new GenericParameterSignature(GenericParameterType.Type, 0)]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Collections.Generic.IList{T}.IndexOf"/>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference IList1IndexOf(TypeSignature elementType)
    {
        return IList1
            .MakeGenericReferenceType(elementType)
            .ToTypeDefOrRef()
            .CreateMemberReference("IndexOf"u8, MethodSignature.CreateInstance(
                returnType: _corLibTypeFactory.Int32,
                parameterTypes: [new GenericParameterSignature(GenericParameterType.Type, 0)]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Collections.Generic.IList{T}.Insert"/>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference IList1Insert(TypeSignature elementType)
    {
        return IList1
            .MakeGenericReferenceType(elementType)
            .ToTypeDefOrRef()
            .CreateMemberReference("Insert"u8, MethodSignature.CreateInstance(
                returnType: _corLibTypeFactory.Void,
                parameterTypes: [
                    _corLibTypeFactory.Int32,
                    new GenericParameterSignature(GenericParameterType.Type, 0)]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Collections.Generic.IList{T}.RemoveAt"/>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference IList1RemoveAt(TypeSignature elementType)
    {
        return IList1
            .MakeGenericReferenceType(elementType)
            .ToTypeDefOrRef()
            .CreateMemberReference("RemoveAt"u8, MethodSignature.CreateInstance(
                returnType: _corLibTypeFactory.Void,
                parameterTypes: [_corLibTypeFactory.Int32]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Collections.Generic.IReadOnlyList{T}.this"/>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference IReadOnlyList1get_Item(TypeSignature elementType)
    {
        return IReadOnlyList1
            .MakeGenericReferenceType(elementType)
            .ToTypeDefOrRef()
            .CreateMemberReference("get_Item"u8, MethodSignature.CreateInstance(
                returnType: new GenericParameterSignature(GenericParameterType.Type, 0),
                parameterTypes: [_corLibTypeFactory.Int32]));
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.IListMethods&lt;T&gt;.Item</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    /// <param name="vectorMethods">The <see cref="IVectorMethods1"/> type.</param>
    public MethodSpecification IListMethods1get_Item(TypeSignature elementType, TypeDefinition vectorMethods)
    {
        return IListMethods1
            .MakeGenericReferenceType(elementType)
            .ToTypeDefOrRef()
            .CreateMemberReference("Item"u8, MethodSignature.CreateStatic(
                returnType: new GenericParameterSignature(GenericParameterType.Type, 0),
                genericParameterCount: 1,
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    _windowsRuntimeModule.CorLibTypeFactory.Int32]))
            .MakeGenericInstanceMethod(vectorMethods.ToReferenceTypeSignature());
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.IListMethods&lt;T&gt;.Item</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    /// <param name="vectorMethods">The <see cref="IVectorMethods1"/> type.</param>
    public MethodSpecification IListMethods1set_Item(TypeSignature elementType, TypeDefinition vectorMethods)
    {
        return IListMethods1
            .MakeGenericReferenceType(elementType)
            .ToTypeDefOrRef()
            .CreateMemberReference("Item"u8, MethodSignature.CreateStatic(
                returnType: _windowsRuntimeModule.CorLibTypeFactory.Void,
                genericParameterCount: 1,
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    _windowsRuntimeModule.CorLibTypeFactory.Int32,
                    new GenericParameterSignature(GenericParameterType.Type, 0)]))
            .MakeGenericInstanceMethod(vectorMethods.ToReferenceTypeSignature());
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.IListMethods&lt;T&gt;.Add</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    /// <param name="vectorMethods">The <see cref="IVectorMethods1"/> type.</param>
    public MethodSpecification IListMethods1Add(TypeSignature elementType, TypeDefinition vectorMethods)
    {
        return IListMethods1
            .MakeGenericReferenceType(elementType)
            .ToTypeDefOrRef()
            .CreateMemberReference("Add"u8, MethodSignature.CreateStatic(
                returnType: _windowsRuntimeModule.CorLibTypeFactory.Void,
                genericParameterCount: 1,
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    new GenericParameterSignature(GenericParameterType.Type, 0)]))
            .MakeGenericInstanceMethod(vectorMethods.ToReferenceTypeSignature());
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.IListMethods&lt;T&gt;.Contains</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    /// <param name="vectorMethods">The <see cref="IVectorMethods1"/> type.</param>
    public MethodSpecification IListMethods1Contains(TypeSignature elementType, TypeDefinition vectorMethods)
    {
        return IListMethods1
            .MakeGenericReferenceType(elementType)
            .ToTypeDefOrRef()
            .CreateMemberReference("Contains"u8, MethodSignature.CreateStatic(
                returnType: _windowsRuntimeModule.CorLibTypeFactory.Boolean,
                genericParameterCount: 1,
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    new GenericParameterSignature(GenericParameterType.Type, 0)]))
            .MakeGenericInstanceMethod(vectorMethods.ToReferenceTypeSignature());
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.IListMethods&lt;T&gt;.CopyTo</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    /// <param name="vectorMethods">The <see cref="IVectorMethods1"/> type.</param>
    public MethodSpecification IListMethods1CopyTo(TypeSignature elementType, TypeDefinition vectorMethods)
    {
        return IListMethods1
            .MakeGenericReferenceType(elementType)
            .ToTypeDefOrRef()
            .CreateMemberReference("CopyTo"u8, MethodSignature.CreateStatic(
                returnType: _windowsRuntimeModule.CorLibTypeFactory.Void,
                genericParameterCount: 1,
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    new GenericParameterSignature(GenericParameterType.Type, 0).MakeSzArrayType(),
                    _windowsRuntimeModule.CorLibTypeFactory.Int32]))
            .MakeGenericInstanceMethod(vectorMethods.ToReferenceTypeSignature());
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.IListMethods&lt;T&gt;.Remove</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    /// <param name="vectorMethods">The <see cref="IVectorMethods1"/> type.</param>
    public MethodSpecification IListMethods1Remove(TypeSignature elementType, TypeDefinition vectorMethods)
    {
        return IListMethods1
            .MakeGenericReferenceType(elementType)
            .ToTypeDefOrRef()
            .CreateMemberReference("Remove"u8, MethodSignature.CreateStatic(
                returnType: _windowsRuntimeModule.CorLibTypeFactory.Boolean,
                genericParameterCount: 1,
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    new GenericParameterSignature(GenericParameterType.Type, 0)]))
            .MakeGenericInstanceMethod(vectorMethods.ToReferenceTypeSignature());
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.IListMethods&lt;T&gt;.IndexOf</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    /// <param name="vectorMethods">The <see cref="IVectorMethods1"/> type.</param>
    public MethodSpecification IListMethods1IndexOf(TypeSignature elementType, TypeDefinition vectorMethods)
    {
        return IListMethods1
            .MakeGenericReferenceType(elementType)
            .ToTypeDefOrRef()
            .CreateMemberReference("IndexOf"u8, MethodSignature.CreateStatic(
                returnType: _windowsRuntimeModule.CorLibTypeFactory.Int32,
                genericParameterCount: 1,
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    new GenericParameterSignature(GenericParameterType.Type, 0)]))
            .MakeGenericInstanceMethod(vectorMethods.ToReferenceTypeSignature());
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.IListMethods&lt;T&gt;.Insert</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    /// <param name="vectorMethods">The <see cref="IVectorMethods1"/> type.</param>
    public MethodSpecification IListMethods1Insert(TypeSignature elementType, TypeDefinition vectorMethods)
    {
        return IListMethods1
            .MakeGenericReferenceType(elementType)
            .ToTypeDefOrRef()
            .CreateMemberReference("Insert"u8, MethodSignature.CreateStatic(
                returnType: _windowsRuntimeModule.CorLibTypeFactory.Void,
                genericParameterCount: 1,
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    _windowsRuntimeModule.CorLibTypeFactory.Int32,
                    new GenericParameterSignature(GenericParameterType.Type, 0)]))
            .MakeGenericInstanceMethod(vectorMethods.ToReferenceTypeSignature());
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.IReadOnlyListMethods&lt;T&gt;.Item</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    /// <param name="vectorViewMethods">The <see cref="IVectorViewMethods1"/> type.</param>
    public MethodSpecification IReadOnlyListMethods1get_Item(TypeSignature elementType, TypeDefinition vectorViewMethods)
    {
        return IReadOnlyListMethods1
            .MakeGenericReferenceType(elementType)
            .ToTypeDefOrRef()
            .CreateMemberReference("Item"u8, MethodSignature.CreateStatic(
                returnType: new GenericParameterSignature(GenericParameterType.Type, 0),
                genericParameterCount: 1,
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    _windowsRuntimeModule.CorLibTypeFactory.Int32]))
            .MakeGenericInstanceMethod(vectorViewMethods.ToReferenceTypeSignature());
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IMapMethodsImpl&lt;K, V&gt;.HasKey</c>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    public MemberReference IMapMethodsImpl2HasKey(TypeSignature keyType, TypeSignature valueType)
    {
        return IMapMethodsImpl2
            .MakeGenericReferenceType(keyType, valueType)
            .ToTypeDefOrRef()
            .CreateMemberReference("HasKey"u8, MethodSignature.CreateInstance(
                returnType: _windowsRuntimeModule.CorLibTypeFactory.Boolean,
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    new GenericParameterSignature(GenericParameterType.Type, 0)]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IMapMethodsImpl&lt;K, V&gt;.Lookup</c>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    public MemberReference IMapMethodsImpl2Lookup(TypeSignature keyType, TypeSignature valueType)
    {
        return IMapMethodsImpl2
            .MakeGenericReferenceType(keyType, valueType)
            .ToTypeDefOrRef()
            .CreateMemberReference("Lookup"u8, MethodSignature.CreateInstance(
                returnType: new GenericParameterSignature(GenericParameterType.Type, 1),
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    new GenericParameterSignature(GenericParameterType.Type, 0)]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IMapMethodsImpl&lt;K, V&gt;.Insert</c>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    public MemberReference IMapMethodsImpl2Insert(TypeSignature keyType, TypeSignature valueType)
    {
        return IMapMethodsImpl2
            .MakeGenericReferenceType(keyType, valueType)
            .ToTypeDefOrRef()
            .CreateMemberReference("Insert"u8, MethodSignature.CreateInstance(
                returnType: _windowsRuntimeModule.CorLibTypeFactory.Boolean,
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    new GenericParameterSignature(GenericParameterType.Type, 0),
                    new GenericParameterSignature(GenericParameterType.Type, 1)]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IMapMethodsImpl&lt;K, V&gt;.Remove</c>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    public MemberReference IMapMethodsImpl2Remove(TypeSignature keyType, TypeSignature valueType)
    {
        return IMapMethodsImpl2
            .MakeGenericReferenceType(keyType, valueType)
            .ToTypeDefOrRef()
            .CreateMemberReference("Remove"u8, MethodSignature.CreateInstance(
                returnType: _windowsRuntimeModule.CorLibTypeFactory.Void,
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    new GenericParameterSignature(GenericParameterType.Type, 0)]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IMapViewMethodsImpl&lt;K, V&gt;.HasKey</c>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    public MemberReference IMapViewMethodsImpl2HasKey(TypeSignature keyType, TypeSignature valueType)
    {
        return IMapViewMethodsImpl2
            .MakeGenericReferenceType(keyType, valueType)
            .ToTypeDefOrRef()
            .CreateMemberReference("HasKey"u8, MethodSignature.CreateInstance(
                returnType: _windowsRuntimeModule.CorLibTypeFactory.Boolean,
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    new GenericParameterSignature(GenericParameterType.Type, 0)]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IMapViewMethodsImpl&lt;K, V&gt;.Lookup</c>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    public MemberReference IMapViewMethodsImpl2Lookup(TypeSignature keyType, TypeSignature valueType)
    {
        return IMapViewMethodsImpl2
            .MakeGenericReferenceType(keyType, valueType)
            .ToTypeDefOrRef()
            .CreateMemberReference("Lookup"u8, MethodSignature.CreateInstance(
                returnType: new GenericParameterSignature(GenericParameterType.Type, 1),
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    new GenericParameterSignature(GenericParameterType.Type, 0)]));
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.IDictionaryMethods&lt;TKey, TValue&gt;.Item</c>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    /// <param name="mapMethods">The <see cref="IMapMethodsImpl2"/> type.</param>
    public MethodSpecification IDictionaryMethods2get_Item(TypeSignature keyType, TypeSignature valueType, TypeDefinition mapMethods)
    {
        return IDictionaryMethods2
            .MakeGenericReferenceType(keyType, valueType)
            .ToTypeDefOrRef()
            .CreateMemberReference("Item"u8, MethodSignature.CreateStatic(
                returnType: new GenericParameterSignature(GenericParameterType.Type, 1),
                genericParameterCount: 1,
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    new GenericParameterSignature(GenericParameterType.Type, 0)]))
            .MakeGenericInstanceMethod(mapMethods.ToReferenceTypeSignature());
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.IDictionaryMethods&lt;TKey, TValue&gt;.Item</c>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    /// <param name="mapMethods">The <see cref="IMapMethodsImpl2"/> type.</param>
    public MethodSpecification IDictionaryMethods2set_Item(TypeSignature keyType, TypeSignature valueType, TypeDefinition mapMethods)
    {
        return IDictionaryMethods2
            .MakeGenericReferenceType(keyType, valueType)
            .ToTypeDefOrRef()
            .CreateMemberReference("Item"u8, MethodSignature.CreateStatic(
                returnType: _windowsRuntimeModule.CorLibTypeFactory.Void,
                genericParameterCount: 1,
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    new GenericParameterSignature(GenericParameterType.Type, 0),
                    new GenericParameterSignature(GenericParameterType.Type, 1)]))
            .MakeGenericInstanceMethod(mapMethods.ToReferenceTypeSignature());
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.IDictionaryMethods&lt;TKey, TValue&gt;.Add</c>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    /// <param name="mapMethods">The <see cref="IMapMethodsImpl2"/> type.</param>
    public MethodSpecification IDictionaryMethods2Add(TypeSignature keyType, TypeSignature valueType, TypeDefinition mapMethods)
    {
        return IDictionaryMethods2
            .MakeGenericReferenceType(keyType, valueType)
            .ToTypeDefOrRef()
            .CreateMemberReference("Add"u8, MethodSignature.CreateStatic(
                returnType: _windowsRuntimeModule.CorLibTypeFactory.Void,
                genericParameterCount: 1,
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    new GenericParameterSignature(GenericParameterType.Type, 0),
                    new GenericParameterSignature(GenericParameterType.Type, 1)]))
            .MakeGenericInstanceMethod(mapMethods.ToReferenceTypeSignature());
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.IDictionaryMethods&lt;TKey, TValue&gt;.ContainsKey</c>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    /// <param name="mapMethods">The <see cref="IMapMethodsImpl2"/> type.</param>
    public MethodSpecification IDictionaryMethods2ContainsKey(TypeSignature keyType, TypeSignature valueType, TypeDefinition mapMethods)
    {
        return IDictionaryMethods2
            .MakeGenericReferenceType(keyType, valueType)
            .ToTypeDefOrRef()
            .CreateMemberReference("ContainsKey"u8, MethodSignature.CreateStatic(
                returnType: _windowsRuntimeModule.CorLibTypeFactory.Boolean,
                genericParameterCount: 1,
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    new GenericParameterSignature(GenericParameterType.Type, 0)]))
            .MakeGenericInstanceMethod(mapMethods.ToReferenceTypeSignature());
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.IDictionaryMethods&lt;TKey, TValue&gt;.Remove</c>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    /// <param name="mapMethods">The <see cref="IMapMethodsImpl2"/> type.</param>
    public MethodSpecification IDictionaryMethods2Remove(TypeSignature keyType, TypeSignature valueType, TypeDefinition mapMethods)
    {
        return IDictionaryMethods2
            .MakeGenericReferenceType(keyType, valueType)
            .ToTypeDefOrRef()
            .CreateMemberReference("Remove"u8, MethodSignature.CreateStatic(
                returnType: _windowsRuntimeModule.CorLibTypeFactory.Boolean,
                genericParameterCount: 1,
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    new GenericParameterSignature(GenericParameterType.Type, 0)]))
            .MakeGenericInstanceMethod(mapMethods.ToReferenceTypeSignature());
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.IDictionaryMethods&lt;TKey, TValue&gt;.TryGetValue</c>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    /// <param name="mapMethods">The <see cref="IMapMethodsImpl2"/> type.</param>
    public MethodSpecification IDictionaryMethods2TryGetValue(TypeSignature keyType, TypeSignature valueType, TypeDefinition mapMethods)
    {
        return IDictionaryMethods2
            .MakeGenericReferenceType(keyType, valueType)
            .ToTypeDefOrRef()
            .CreateMemberReference("TryGetValue"u8, MethodSignature.CreateStatic(
                returnType: _windowsRuntimeModule.CorLibTypeFactory.Boolean,
                genericParameterCount: 1,
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    new GenericParameterSignature(GenericParameterType.Type, 0),
                    new GenericParameterSignature(GenericParameterType.Type, 1).MakeByReferenceType()]))
            .MakeGenericInstanceMethod(mapMethods.ToReferenceTypeSignature());
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.IDictionaryMethods&lt;TKey, TValue&gt;.Add</c>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    /// <param name="mapMethods">The <see cref="IMapMethodsImpl2"/> type.</param>
    public MethodSpecification IDictionaryMethods2AddKeyValuePair(TypeSignature keyType, TypeSignature valueType, TypeDefinition mapMethods)
    {
        return IDictionaryMethods2
            .MakeGenericReferenceType(keyType, valueType)
            .ToTypeDefOrRef()
            .CreateMemberReference("Add"u8, MethodSignature.CreateStatic(
                returnType: _windowsRuntimeModule.CorLibTypeFactory.Void,
                genericParameterCount: 1,
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    KeyValuePair.MakeGenericValueType(
                        new GenericParameterSignature(GenericParameterType.Type, 0),
                        new GenericParameterSignature(GenericParameterType.Type, 1))]))
            .MakeGenericInstanceMethod(mapMethods.ToReferenceTypeSignature());
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.IDictionaryMethods&lt;TKey, TValue&gt;.Contains</c>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    /// <param name="mapMethods">The <see cref="IMapMethodsImpl2"/> type.</param>
    public MethodSpecification IDictionaryMethods2Contains(TypeSignature keyType, TypeSignature valueType, TypeDefinition mapMethods)
    {
        return IDictionaryMethods2
            .MakeGenericReferenceType(keyType, valueType)
            .ToTypeDefOrRef()
            .CreateMemberReference("Contains"u8, MethodSignature.CreateStatic(
                returnType: _windowsRuntimeModule.CorLibTypeFactory.Boolean,
                genericParameterCount: 1,
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    KeyValuePair.MakeGenericValueType(
                        new GenericParameterSignature(GenericParameterType.Type, 0),
                        new GenericParameterSignature(GenericParameterType.Type, 1))]))
            .MakeGenericInstanceMethod(mapMethods.ToReferenceTypeSignature());
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.IDictionaryMethods&lt;TKey, TValue&gt;.CopyTo</c>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    /// <param name="mapMethods">The <see cref="IMapMethodsImpl2"/> type.</param>
    /// <param name="iterableMethods">The <see cref="IIteratorMethodsImpl1"/> type.</param>
    public MethodSpecification IDictionaryMethods2CopyTo(
        TypeSignature keyType,
        TypeSignature valueType,
        TypeDefinition mapMethods,
        TypeDefinition iterableMethods)
    {
        return IDictionaryMethods2
            .MakeGenericReferenceType(keyType, valueType)
            .ToTypeDefOrRef()
            .CreateMemberReference("CopyTo"u8, MethodSignature.CreateStatic(
                returnType: _windowsRuntimeModule.CorLibTypeFactory.Void,
                genericParameterCount: 2,
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    KeyValuePair.MakeGenericValueType(
                        new GenericParameterSignature(GenericParameterType.Type, 0),
                        new GenericParameterSignature(GenericParameterType.Type, 1)).MakeSzArrayType(),
                    _windowsRuntimeModule.CorLibTypeFactory.Int32]))
            .MakeGenericInstanceMethod(mapMethods.ToReferenceTypeSignature(), iterableMethods.ToReferenceTypeSignature());
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.IDictionaryMethods&lt;TKey, TValue&gt;.Remove</c>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    /// <param name="mapMethods">The <see cref="IMapMethodsImpl2"/> type.</param>
    public MethodSpecification IDictionaryMethods2RemoveKeyValuePair(TypeSignature keyType, TypeSignature valueType, TypeDefinition mapMethods)
    {
        return IDictionaryMethods2
            .MakeGenericReferenceType(keyType, valueType)
            .ToTypeDefOrRef()
            .CreateMemberReference("Remove"u8, MethodSignature.CreateStatic(
                returnType: _windowsRuntimeModule.CorLibTypeFactory.Boolean,
                genericParameterCount: 1,
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    KeyValuePair.MakeGenericValueType(
                        new GenericParameterSignature(GenericParameterType.Type, 0),
                        new GenericParameterSignature(GenericParameterType.Type, 1))]))
            .MakeGenericInstanceMethod(mapMethods.ToReferenceTypeSignature());
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.IReadOnlyDictionaryMethods&lt;TKey, TValue&gt;.Item</c>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    /// <param name="mapViewMethods">The <see cref="IMapViewMethodsImpl2"/> type.</param>
    public MethodSpecification IReadOnlyDictionaryMethods2get_Item(TypeSignature keyType, TypeSignature valueType, TypeDefinition mapViewMethods)
    {
        return IReadOnlyDictionaryMethods2
            .MakeGenericReferenceType(keyType, valueType)
            .ToTypeDefOrRef()
            .CreateMemberReference("Item"u8, MethodSignature.CreateStatic(
                returnType: new GenericParameterSignature(GenericParameterType.Type, 1),
                genericParameterCount: 1,
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    new GenericParameterSignature(GenericParameterType.Type, 0)]))
            .MakeGenericInstanceMethod(mapViewMethods.ToReferenceTypeSignature());
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.IReadOnlyDictionaryMethods&lt;TKey, TValue&gt;.ContainsKey</c>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    /// <param name="mapViewMethods">The <see cref="IMapViewMethodsImpl2"/> type.</param>
    public MethodSpecification IReadOnlyDictionaryMethods2ContainsKey(TypeSignature keyType, TypeSignature valueType, TypeDefinition mapViewMethods)
    {
        return IReadOnlyDictionaryMethods2
            .MakeGenericReferenceType(keyType, valueType)
            .ToTypeDefOrRef()
            .CreateMemberReference("ContainsKey"u8, MethodSignature.CreateStatic(
                returnType: _windowsRuntimeModule.CorLibTypeFactory.Boolean,
                genericParameterCount: 1,
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    new GenericParameterSignature(GenericParameterType.Type, 0)]))
            .MakeGenericInstanceMethod(mapViewMethods.ToReferenceTypeSignature());
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.IReadOnlyDictionaryMethods&lt;TKey, TValue&gt;.TryGetValue</c>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    /// <param name="mapViewMethods">The <see cref="IMapViewMethodsImpl2"/> type.</param>
    public MethodSpecification IReadOnlyDictionaryMethods2TryGetValue(TypeSignature keyType, TypeSignature valueType, TypeDefinition mapViewMethods)
    {
        return IReadOnlyDictionaryMethods2
            .MakeGenericReferenceType(keyType, valueType)
            .ToTypeDefOrRef()
            .CreateMemberReference("TryGetValue"u8, MethodSignature.CreateStatic(
                returnType: _windowsRuntimeModule.CorLibTypeFactory.Boolean,
                genericParameterCount: 1,
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    new GenericParameterSignature(GenericParameterType.Type, 0),
                    new GenericParameterSignature(GenericParameterType.Type, 1).MakeByReferenceType()]))
            .MakeGenericInstanceMethod(mapViewMethods.ToReferenceTypeSignature());
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Collections.Generic.IDictionary{TKey, TValue}.this"/>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    public MemberReference IDictionary2get_Item(TypeSignature keyType, TypeSignature valueType)
    {
        return IDictionary2
            .MakeGenericReferenceType(keyType, valueType)
            .ToTypeDefOrRef()
            .CreateMemberReference("get_Item"u8, MethodSignature.CreateInstance(
                returnType: new GenericParameterSignature(GenericParameterType.Type, 1),
                parameterTypes: [new GenericParameterSignature(GenericParameterType.Type, 0)]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Collections.Generic.IDictionary{TKey, TValue}.this"/>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    public MemberReference IDictionary2set_Item(TypeSignature keyType, TypeSignature valueType)
    {
        return IDictionary2
            .MakeGenericReferenceType(keyType, valueType)
            .ToTypeDefOrRef()
            .CreateMemberReference("set_Item"u8, MethodSignature.CreateInstance(
                returnType: _corLibTypeFactory.Void,
                parameterTypes: [
                    new GenericParameterSignature(GenericParameterType.Type, 0),
                    new GenericParameterSignature(GenericParameterType.Type, 1)]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Collections.Generic.IDictionary{TKey, TValue}.Keys"/>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    public MemberReference IDictionary2get_Keys(TypeSignature keyType, TypeSignature valueType)
    {
        return IDictionary2
            .MakeGenericReferenceType(keyType, valueType)
            .ToTypeDefOrRef()
            .CreateMemberReference("get_Keys"u8, MethodSignature.CreateInstance(
                ICollection1.MakeGenericReferenceType(new GenericParameterSignature(GenericParameterType.Type, 0))));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Collections.Generic.IDictionary{TKey, TValue}.Values"/>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    public MemberReference IDictionary2get_Values(TypeSignature keyType, TypeSignature valueType)
    {
        return IDictionary2
            .MakeGenericReferenceType(keyType, valueType)
            .ToTypeDefOrRef()
            .CreateMemberReference("get_Values"u8, MethodSignature.CreateInstance(
                ICollection1.MakeGenericReferenceType(new GenericParameterSignature(GenericParameterType.Type, 1))));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Collections.Generic.IDictionary{TKey, TValue}.Add"/>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    public MemberReference IDictionary2Add(TypeSignature keyType, TypeSignature valueType)
    {
        return IDictionary2
            .MakeGenericReferenceType(keyType, valueType)
            .ToTypeDefOrRef()
            .CreateMemberReference("Add"u8, MethodSignature.CreateInstance(
                returnType: _corLibTypeFactory.Void,
                parameterTypes: [
                    new GenericParameterSignature(GenericParameterType.Type, 0),
                    new GenericParameterSignature(GenericParameterType.Type, 1)]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Collections.Generic.IDictionary{TKey, TValue}.ContainsKey"/>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    public MemberReference IDictionary2ContainsKey(TypeSignature keyType, TypeSignature valueType)
    {
        return IDictionary2
            .MakeGenericReferenceType(keyType, valueType)
            .ToTypeDefOrRef()
            .CreateMemberReference("ContainsKey"u8, MethodSignature.CreateInstance(
                returnType: _corLibTypeFactory.Boolean,
                parameterTypes: [new GenericParameterSignature(GenericParameterType.Type, 0)]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Collections.Generic.IDictionary{TKey, TValue}.Remove"/>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    public MemberReference IDictionary2Remove(TypeSignature keyType, TypeSignature valueType)
    {
        return IDictionary2
            .MakeGenericReferenceType(keyType, valueType)
            .ToTypeDefOrRef()
            .CreateMemberReference("Remove"u8, MethodSignature.CreateInstance(
                returnType: _corLibTypeFactory.Boolean,
                parameterTypes: [new GenericParameterSignature(GenericParameterType.Type, 0)]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Collections.Generic.IDictionary{TKey, TValue}.TryGetValue"/>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    public MemberReference IDictionary2TryGetValue(TypeSignature keyType, TypeSignature valueType)
    {
        return IDictionary2
            .MakeGenericReferenceType(keyType, valueType)
            .ToTypeDefOrRef()
            .CreateMemberReference("TryGetValue"u8, MethodSignature.CreateInstance(
                returnType: _corLibTypeFactory.Boolean,
                parameterTypes: [
                    new GenericParameterSignature(GenericParameterType.Type, 0),
                    new GenericParameterSignature(GenericParameterType.Type, 1).MakeByReferenceType()]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Collections.Generic.IReadOnlyDictionary{TKey, TValue}.this"/>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    public MemberReference IReadOnlyDictionary2get_Item(TypeSignature keyType, TypeSignature valueType)
    {
        return IReadOnlyDictionary2
            .MakeGenericReferenceType(keyType, valueType)
            .ToTypeDefOrRef()
            .CreateMemberReference("get_Item"u8, MethodSignature.CreateInstance(
                returnType: new GenericParameterSignature(GenericParameterType.Type, 1),
                parameterTypes: [new GenericParameterSignature(GenericParameterType.Type, 0)]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Collections.Generic.IReadOnlyDictionary{TKey, TValue}.Keys"/>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    public MemberReference IReadOnlyDictionary2get_Keys(TypeSignature keyType, TypeSignature valueType)
    {
        return IReadOnlyDictionary2
            .MakeGenericReferenceType(keyType, valueType)
            .ToTypeDefOrRef()
            .CreateMemberReference("get_Keys"u8, MethodSignature.CreateInstance(
                IEnumerable1.MakeGenericReferenceType(new GenericParameterSignature(GenericParameterType.Type, 0))));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Collections.Generic.IReadOnlyDictionary{TKey, TValue}.Values"/>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    public MemberReference IReadOnlyDictionary2get_Values(TypeSignature keyType, TypeSignature valueType)
    {
        return IReadOnlyDictionary2
            .MakeGenericReferenceType(keyType, valueType)
            .ToTypeDefOrRef()
            .CreateMemberReference("get_Values"u8, MethodSignature.CreateInstance(
                IEnumerable1.MakeGenericReferenceType(new GenericParameterSignature(GenericParameterType.Type, 1))));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Collections.Generic.IReadOnlyDictionary{TKey, TValue}.ContainsKey"/>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    public MemberReference IReadOnlyDictionary2ContainsKey(TypeSignature keyType, TypeSignature valueType)
    {
        return IReadOnlyDictionary2
            .MakeGenericReferenceType(keyType, valueType)
            .ToTypeDefOrRef()
            .CreateMemberReference("ContainsKey"u8, MethodSignature.CreateInstance(
                returnType: _corLibTypeFactory.Boolean,
                parameterTypes: [new GenericParameterSignature(GenericParameterType.Type, 0)]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Collections.Generic.IReadOnlyDictionary{TKey, TValue}.TryGetValue"/>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    public MemberReference IReadOnlyDictionary2TryGetValue(TypeSignature keyType, TypeSignature valueType)
    {
        return IReadOnlyDictionary2
            .MakeGenericReferenceType(keyType, valueType)
            .ToTypeDefOrRef()
            .CreateMemberReference("TryGetValue"u8, MethodSignature.CreateInstance(
                returnType: _corLibTypeFactory.Boolean,
                parameterTypes: [
                    new GenericParameterSignature(GenericParameterType.Type, 0),
                    new GenericParameterSignature(GenericParameterType.Type, 1).MakeByReferenceType()]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.EventHandlerEventSource&lt;TEventArgs&gt;.ctor(...)</c>.
    /// </summary>
    /// <param name="delegateType">The input delegate type.</param>
    public MemberReference EventHandler1EventSourceConvertToUnmanaged(TypeSignature delegateType)
    {
        return EventHandler1EventSource
            .CreateMemberReference("ConvertToUnmanaged"u8, MethodSignature.CreateInstance(
                returnType: WindowsRuntimeObjectReferenceValue.ToValueTypeSignature(),
                parameterTypes: [delegateType]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.EventHandlerEventSource&lt;TSender, TEventArgs&gt;.ctor(...)</c>.
    /// </summary>
    /// <param name="delegateType">The input delegate type.</param>
    public MemberReference EventHandler2EventSourceConvertToUnmanaged(TypeSignature delegateType)
    {
        return EventHandler2EventSource
            .CreateMemberReference("ConvertToUnmanaged"u8, MethodSignature.CreateInstance(
                returnType: WindowsRuntimeObjectReferenceValue.ToValueTypeSignature(),
                parameterTypes: [delegateType]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for the <c>.ctor</c> method of a given base type for a <c>NativeObject</c> type.
    /// </summary>
    /// <param name="enumeratorType">The input native object base type.</param>
    public MemberReference WindowsRuntimeNativeObjectBaseType_ctor(TypeSignature enumeratorType)
    {
        return enumeratorType
            .ToTypeDefOrRef()
            .CreateMemberReference(".ctor"u8, MethodSignature.CreateInstance(
                returnType: _corLibTypeFactory.Void,
                parameterTypes: [WindowsRuntimeObjectReference.ToReferenceTypeSignature()]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for the <c>.ctor</c> method for <see cref="DictionaryKeyCollection2"/>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    public MemberReference DictionaryKeyCollection2_ctor(TypeSignature keyType, TypeSignature valueType)
    {
        return DictionaryKeyCollection2
            .MakeGenericReferenceType(keyType, valueType)
            .ToTypeDefOrRef()
            .CreateMemberReference(".ctor"u8, MethodSignature.CreateInstance(
                returnType: _corLibTypeFactory.Void,
                parameterTypes: [ICollection1.MakeGenericReferenceType(KeyValuePair.MakeGenericValueType(
                    new GenericParameterSignature(GenericParameterType.Type, 0),
                    new GenericParameterSignature(GenericParameterType.Type, 1)))]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for the <c>.ctor</c> method for <see cref="DictionaryValueCollection2"/>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    public MemberReference DictionaryValueCollection2_ctor(TypeSignature keyType, TypeSignature valueType)
    {
        return DictionaryValueCollection2
            .MakeGenericReferenceType(keyType, valueType)
            .ToTypeDefOrRef()
            .CreateMemberReference(".ctor"u8, MethodSignature.CreateInstance(
                returnType: _corLibTypeFactory.Void,
                parameterTypes: [ICollection1.MakeGenericReferenceType(KeyValuePair.MakeGenericValueType(
                    new GenericParameterSignature(GenericParameterType.Type, 0),
                    new GenericParameterSignature(GenericParameterType.Type, 1)))]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for the <c>.ctor</c> method for <see cref="ReadOnlyDictionaryKeyCollection2"/>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    public MemberReference ReadOnlyDictionaryKeyCollection2_ctor(TypeSignature keyType, TypeSignature valueType)
    {
        return ReadOnlyDictionaryKeyCollection2
            .MakeGenericReferenceType(keyType, valueType)
            .ToTypeDefOrRef()
            .CreateMemberReference(".ctor"u8, MethodSignature.CreateInstance(
                returnType: _corLibTypeFactory.Void,
                parameterTypes: [IEnumerable1.MakeGenericReferenceType(KeyValuePair.MakeGenericValueType(
                    new GenericParameterSignature(GenericParameterType.Type, 0),
                    new GenericParameterSignature(GenericParameterType.Type, 1)))]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for the <c>.ctor</c> method for <see cref="ReadOnlyDictionaryValueCollection2"/>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    public MemberReference ReadOnlyDictionaryValueCollection2_ctor(TypeSignature keyType, TypeSignature valueType)
    {
        return ReadOnlyDictionaryValueCollection2
            .MakeGenericReferenceType(keyType, valueType)
            .ToTypeDefOrRef()
            .CreateMemberReference(".ctor"u8, MethodSignature.CreateInstance(
                returnType: _corLibTypeFactory.Void,
                parameterTypes: [IEnumerable1.MakeGenericReferenceType(KeyValuePair.MakeGenericValueType(
                    new GenericParameterSignature(GenericParameterType.Type, 0),
                    new GenericParameterSignature(GenericParameterType.Type, 1)))]));
    }
}
