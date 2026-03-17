// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
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
    /// The <see cref="ModuleDefinition"/> for the Windows Runtime assembly (i.e. <c>WinRT.Runtime.dll</c>).
    /// </summary>
    private readonly IResolutionScope _windowsRuntimeModule;

    /// <summary>
    /// The <see cref="ModuleDefinition"/> for the Windows SDK projection assembly.
    /// </summary>
    private readonly IResolutionScope _windowsSdkProjectionModule;

    /// <summary>
    /// Creates a new <see cref="InteropReferences"/> instance.
    /// </summary>
    /// <param name="corLibTypeFactory">The <see cref="AsmResolver.DotNet.Signatures.CorLibTypeFactory"/> currently in use.</param>
    /// <param name="windowsRuntimeModule">The <see cref="IResolutionScope"/> for the Windows Runtime assembly (i.e. <c>WinRT.Runtime.dll</c>).</param>
    /// <param name="windowsSdkProjectionModule">The <see cref="IResolutionScope"/> for the Windows SDK projection assembly.</param>
    public InteropReferences(
        CorLibTypeFactory corLibTypeFactory,
        IResolutionScope windowsRuntimeModule,
        IResolutionScope windowsSdkProjectionModule)
    {
        _corLibTypeFactory = corLibTypeFactory;
        _windowsRuntimeModule = windowsRuntimeModule;
        _windowsSdkProjectionModule = windowsSdkProjectionModule;
    }

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.Signatures.CorLibTypeFactory"/> instance associated with this instance.
    /// </summary>
    public CorLibTypeFactory CorLibTypeFactory => _corLibTypeFactory;

    /// <inheritdoc cref="CorLibTypeFactory.Void"/>
    public CorLibTypeSignature Void => _corLibTypeFactory.Void;

    /// <inheritdoc cref="CorLibTypeFactory.Boolean"/>
    public CorLibTypeSignature Boolean => _corLibTypeFactory.Boolean;

    /// <inheritdoc cref="CorLibTypeFactory.Char"/>
    public CorLibTypeSignature Char => _corLibTypeFactory.Char;

    /// <inheritdoc cref="CorLibTypeFactory.SByte"/>
    public CorLibTypeSignature SByte => _corLibTypeFactory.SByte;

    /// <inheritdoc cref="CorLibTypeFactory.Byte"/>
    public CorLibTypeSignature Byte => _corLibTypeFactory.Byte;

    /// <inheritdoc cref="CorLibTypeFactory.Int16"/>
    public CorLibTypeSignature Int16 => _corLibTypeFactory.Int16;

    /// <inheritdoc cref="CorLibTypeFactory.UInt16"/>
    public CorLibTypeSignature UInt16 => _corLibTypeFactory.UInt16;

    /// <inheritdoc cref="CorLibTypeFactory.Int32"/>
    public CorLibTypeSignature Int32 => _corLibTypeFactory.Int32;

    /// <inheritdoc cref="CorLibTypeFactory.UInt32"/>
    public CorLibTypeSignature UInt32 => _corLibTypeFactory.UInt32;

    /// <inheritdoc cref="CorLibTypeFactory.Int64"/>
    public CorLibTypeSignature Int64 => _corLibTypeFactory.Int64;

    /// <inheritdoc cref="CorLibTypeFactory.UInt64"/>
    public CorLibTypeSignature UInt64 => _corLibTypeFactory.UInt64;

    /// <inheritdoc cref="CorLibTypeFactory.Single"/>
    public CorLibTypeSignature Single => _corLibTypeFactory.Single;

    /// <inheritdoc cref="CorLibTypeFactory.Double"/>
    public CorLibTypeSignature Double => _corLibTypeFactory.Double;

    /// <inheritdoc cref="CorLibTypeFactory.String"/>
    public CorLibTypeSignature String => _corLibTypeFactory.String;

    /// <inheritdoc cref="CorLibTypeFactory.IntPtr"/>
    public CorLibTypeSignature IntPtr => _corLibTypeFactory.IntPtr;

    /// <inheritdoc cref="CorLibTypeFactory.UIntPtr"/>
    public CorLibTypeSignature UIntPtr => _corLibTypeFactory.UIntPtr;

    /// <inheritdoc cref="CorLibTypeFactory.Object"/>
    public CorLibTypeSignature Object => _corLibTypeFactory.Object;

    /// <summary>
    /// Gets the <see cref="ModuleDefinition"/> for the Windows Runtime assembly (i.e. <c>WinRT.Runtime.dll</c>).
    /// </summary>
    public IResolutionScope WindowsRuntimeModule => _windowsRuntimeModule;

    /// <summary>
    /// Gets the <see cref="ModuleDefinition"/> for the Windows Runtine foundation projection assembly.
    /// </summary>
    public IResolutionScope WindowsFoundationModule => _windowsSdkProjectionModule;

    /// <summary>
    /// Gets the <see cref="AssemblyReference"/> for <c>System.Runtime.InteropServices.dll</c>.
    /// </summary>
    /// <remarks>
    /// This <see cref="AssemblyReference"/> will use the same scope as <see cref="_corLibTypeFactory"/>, to enable correct resolution.
    /// </remarks>
    public AssemblyReference SystemRuntimeInteropServices => field ??= new AssemblyReference(
        name: "System.Runtime.InteropServices"u8,
        version: new Version(10, 0, 0, 0),
        publicKey: false,
        publicKeyOrToken: WellKnownPublicKeyTokens.SystemRuntimeInteropServices).Import(_corLibTypeFactory.CorLibScope);

    /// <summary>
    /// Gets the <see cref="AssemblyReference"/> for <c>System.ObjectModel.dll</c>.
    /// </summary>
    /// <remarks><inheritdoc cref="SystemRuntimeInteropServices" path="/remarks/node()"/></remarks>
    public AssemblyReference SystemObjectModel => field ??= new AssemblyReference(
        name: "System.ObjectModel"u8,
        version: new Version(10, 0, 0, 0),
        publicKey: false,
        publicKeyOrToken: WellKnownPublicKeyTokens.SystemObjectModel).Import(_corLibTypeFactory.CorLibScope);

    /// <summary>
    /// Gets the <see cref="AssemblyReference"/> for <c>System.Memory.dll</c>.
    /// </summary>
    /// <remarks><inheritdoc cref="SystemRuntimeInteropServices" path="/remarks/node()"/></remarks>
    public AssemblyReference SystemMemory => field ??= new AssemblyReference(
        name: "System.Memory"u8,
        version: new Version(10, 0, 0, 0),
        publicKey: false,
        publicKeyOrToken: WellKnownPublicKeyTokens.SystemMemory).Import(_corLibTypeFactory.CorLibScope);

    /// <summary>
    /// Gets the <see cref="AssemblyReference"/> for <c>System.Numerics.Vectors.dll</c>.
    /// </summary>
    /// <remarks><inheritdoc cref="SystemRuntimeInteropServices" path="/remarks/node()"/></remarks>
    public AssemblyReference SystemNumericsVectors => field ??= new AssemblyReference(
        name: "System.Numerics.Vectors"u8,
        version: new Version(10, 0, 0, 0),
        publicKey: false,
        publicKeyOrToken: WellKnownPublicKeyTokens.SystemNumericsVectors).Import(_corLibTypeFactory.CorLibScope);

    /// <summary>
    /// Gets the <see cref="AssemblyReference"/> for <c>System.Threading.dll</c>.
    /// </summary>
    /// <remarks><inheritdoc cref="SystemThreading" path="/remarks/node()"/></remarks>
    public AssemblyReference SystemThreading => field ??= new AssemblyReference(
        name: "System.Threading"u8,
        version: new Version(10, 0, 0, 0),
        publicKey: false,
        publicKeyOrToken: WellKnownPublicKeyTokens.SystemThreading).Import(_corLibTypeFactory.CorLibScope);

    /// <summary>
    /// Gets the <see cref="AssemblyReference"/> for <c>WinRT.Sdk.Projection.dll</c>.
    /// </summary>
    /// <remarks><inheritdoc cref="SystemRuntimeInteropServices" path="/remarks/node()"/></remarks>
    public AssemblyReference WinRTSdkProjection => field ??= new AssemblyReference(
        name: InteropNames.WindowsRuntimeSdkProjectionAssemblyNameUtf8,
        version: new Version(0, 0, 0, 0),
        publicKey: false,
        publicKeyOrToken: default).Import(_corLibTypeFactory.CorLibScope);

    /// <summary>
    /// Gets the <see cref="AssemblyReference"/> for <c>WinRT.Sdk.Xaml.Projection.dll</c>.
    /// </summary>
    /// <remarks><inheritdoc cref="SystemRuntimeInteropServices" path="/remarks/node()"/></remarks>
    public AssemblyReference WinRTSdkXamlProjection => field ??= new AssemblyReference(
        name: InteropNames.WindowsRuntimeSdkXamlProjectionAssemblyNameUtf8,
        version: new Version(0, 0, 0, 0),
        publicKey: false,
        publicKeyOrToken: default).Import(_corLibTypeFactory.CorLibScope);

    /// <summary>
    /// Gets the <see cref="AssemblyReference"/> for <c>WinRT.Projection.dll</c>.
    /// </summary>
    /// <remarks><inheritdoc cref="SystemRuntimeInteropServices" path="/remarks/node()"/></remarks>
    public AssemblyReference WinRTProjection => field ??= new AssemblyReference(
        name: InteropNames.WindowsRuntimeProjectionAssemblyNameUtf8,
        version: new Version(0, 0, 0, 0),
        publicKey: false,
        publicKeyOrToken: default).Import(_corLibTypeFactory.CorLibScope);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Attribute"/>.
    /// </summary>
    public TypeReference Attribute => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System"u8, "Attribute"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.AttributeTargets"/>.
    /// </summary>
    public TypeReference AttributeTargets => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System"u8, "AttributeTargets"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.AttributeUsageAttribute"/>.
    /// </summary>
    public TypeReference AttributeUsageAttribute => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System"u8, "AttributeUsageAttribute"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Runtime.InteropServices.TypeMapAttribute{TTypeMapGroup}"/>.
    /// </summary>
    public TypeReference TypeMapAttribute1 => field ??= SystemRuntimeInteropServices.CreateTypeReference("System.Runtime.InteropServices"u8, "TypeMapAttribute`1"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Runtime.InteropServices.TypeMapAttribute{TTypeMapGroup}"/>.
    /// </summary>
    public TypeReference GuidAttribute => field ??= SystemRuntimeInteropServices.CreateTypeReference("System.Runtime.InteropServices"u8, "GuidAttribute"u8);

    /// <summary>
    /// Gets the <see cref="GenericInstanceTypeSignature"/> for <see cref="System.Runtime.InteropServices.TypeMapAttribute{TTypeMapGroup}"/> of <see cref="WindowsRuntimeComWrappersTypeMapGroup"/>.
    /// </summary>
    public GenericInstanceTypeSignature TypeMapAttributeWindowsRuntimeComWrappersTypeMapGroup => field ??= TypeMapAttribute1.MakeGenericReferenceType([WindowsRuntimeComWrappersTypeMapGroup.ToReferenceTypeSignature()]);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Runtime.InteropServices.TypeMapAssociationAttribute{TTypeMapGroup}"/>.
    /// </summary>
    public TypeReference TypeMapAssociationAttribute1 => field ??= SystemRuntimeInteropServices.CreateTypeReference("System.Runtime.InteropServices"u8, "TypeMapAssociationAttribute`1"u8);

    /// <summary>
    /// Gets the <see cref="GenericInstanceTypeSignature"/> for <see cref="System.Runtime.InteropServices.TypeMapAssociationAttribute{TTypeMapGroup}"/> of <see cref="DynamicInterfaceCastableImplementationTypeMapGroup"/>.
    /// </summary>
    public GenericInstanceTypeSignature TypeMapAssociationAttributeDynamicInterfaceCastableImplementationTypeMapGroup => field ??= TypeMapAttribute1.MakeGenericReferenceType([DynamicInterfaceCastableImplementationTypeMapGroup.ToReferenceTypeSignature()]);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Array"/>.
    /// </summary>
    public TypeReference Array => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System"u8, "Array"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="ArraySegment{T}"/>.
    /// </summary>
    public TypeReference ArraySegment1 => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System"u8, "ArraySegment`1"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="ArraySegment{T}.Enumerator"/>.
    /// </summary>
    public TypeReference ArraySegment1Enumerator => field ??= ArraySegment1.CreateTypeReference("Enumerator"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="Nullable"/>.
    /// </summary>
    public TypeReference Nullable1 => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System"u8, "Nullable`1"u8);

    /// <summary>
    /// Gets the <see cref="TypeSignature"/> for <see cref="Nullable{T}"/> of <see cref="int"/>.
    /// </summary>
    public GenericInstanceTypeSignature NullableInt32 => field ??= Nullable1.MakeGenericValueType([_corLibTypeFactory.Int32]);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Type"/>.
    /// </summary>
    public TypeReference Type => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System"u8, "Type"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.RuntimeTypeHandle"/>.
    /// </summary>
    public TypeReference RuntimeTypeHandle => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System"u8, "RuntimeTypeHandle"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Delegate"/>.
    /// </summary>
    public TypeReference Delegate => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System"u8, "Delegate"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.ValueType"/>.
    /// </summary>
    public TypeReference ValueType => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System"u8, "ValueType"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.FlagsAttribute"/>.
    /// </summary>
    public TypeReference FlagsAttribute => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System"u8, "FlagsAttribute"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="Span{T}"/>.
    /// </summary>
    public TypeReference Span1 => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System"u8, "Span`1"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="ReadOnlySpan{T}"/>.
    /// </summary>
    public TypeReference ReadOnlySpan1 => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System"u8, "ReadOnlySpan`1"u8);

    /// <summary>
    /// Gets the <see cref="TypeSignature"/> for <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/>.
    /// </summary>
    public GenericInstanceTypeSignature ReadOnlySpanByte => field ??= ReadOnlySpan1.MakeGenericValueType([_corLibTypeFactory.Byte]);

    /// <summary>
    /// Gets the <see cref="TypeSignature"/> for <see cref="ReadOnlySpan{T}"/> of <see cref="char"/>.
    /// </summary>
    public GenericInstanceTypeSignature ReadOnlySpanChar => field ??= ReadOnlySpan1.MakeGenericValueType([_corLibTypeFactory.Char]);

    /// <summary>
    /// Gets the <see cref="TypeSignature"/> for <see cref="ReadOnlySpan{T}"/> of <see cref="ushort"/>.
    /// </summary>
    public GenericInstanceTypeSignature ReadOnlySpanUInt16 => field ??= ReadOnlySpan1.MakeGenericValueType([_corLibTypeFactory.UInt16]);

    /// <summary>
    /// Gets the <see cref="TypeSignature"/> for <see cref="ReadOnlySpan{T}"/> of <see cref="int"/>.
    /// </summary>
    public GenericInstanceTypeSignature ReadOnlySpanInt32 => field ??= ReadOnlySpan1.MakeGenericValueType([_corLibTypeFactory.Int32]);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="System.Threading.Tasks.Task{TResult}"/>.
    /// </summary>
    public TypeReference Task1 => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System.Threading.Tasks"u8, "Task`1"u8);

    /// <summary>
    /// Gets the <see cref="TypeReference"/> for <see cref="Func{T1, T2, TResult}"/>.
    /// </summary>
    public TypeReference Func3 => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System"u8, "Func`3"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Exception"/>.
    /// </summary>
    public TypeReference Exception => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System"u8, "Exception"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.NotSupportedException"/>.
    /// </summary>
    public TypeReference NotSupportedException => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System"u8, "NotSupportedException"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Guid"/>.
    /// </summary>
    public TypeReference Guid => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System"u8, "Guid"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.EventHandler"/>.
    /// </summary>
    public TypeReference EventHandler => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System"u8, "EventHandler"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="EventHandler{TEventArgs}"/>.
    /// </summary>
    public TypeReference EventHandler1 => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System"u8, "EventHandler`1"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="EventHandler{TSender, TEventArgs}"/>.
    /// </summary>
    public TypeReference EventHandler2 => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System"u8, "EventHandler`2"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.IDisposable"/>.
    /// </summary>
    public TypeReference IDisposable => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System"u8, "IDisposable"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.TimeSpan"/>.
    /// </summary>
    public TypeReference TimeSpan => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System"u8, "TimeSpan"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.DateTimeOffset"/>.
    /// </summary>
    public TypeReference DateTimeOffset => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System"u8, "DateTimeOffset"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Uri"/>.
    /// </summary>
    public TypeReference Uri => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System"u8, "Uri"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Numerics.Matrix3x2"/>.
    /// </summary>
    public TypeReference Matrix3x2 => field ??= SystemNumericsVectors.CreateTypeReference("System.Numerics"u8, "Matrix3x2"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Numerics.Matrix4x4"/>.
    /// </summary>
    public TypeReference Matrix4x4 => field ??= SystemNumericsVectors.CreateTypeReference("System.Numerics"u8, "Matrix4x4"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Numerics.Plane"/>.
    /// </summary>
    public TypeReference Plane => field ??= SystemNumericsVectors.CreateTypeReference("System.Numerics"u8, "Plane"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Numerics.Quaternion"/>.
    /// </summary>
    public TypeReference Quaternion => field ??= SystemNumericsVectors.CreateTypeReference("System.Numerics"u8, "Quaternion"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Numerics.Vector2"/>.
    /// </summary>
    public TypeReference Vector2 => field ??= SystemNumericsVectors.CreateTypeReference("System.Numerics"u8, "Vector2"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Numerics.Vector3"/>.
    /// </summary>
    public TypeReference Vector3 => field ??= SystemNumericsVectors.CreateTypeReference("System.Numerics"u8, "Vector3"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Numerics.Vector4"/>.
    /// </summary>
    public TypeReference Vector4 => field ??= SystemNumericsVectors.CreateTypeReference("System.Numerics"u8, "Vector4"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.IServiceProvider"/>.
    /// </summary>
    public TypeReference IServiceProvider => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System"u8, "IServiceProvider"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Windows.Input.ICommand"/>.
    /// </summary>
    public TypeReference ICommand => field ??= SystemObjectModel.CreateTypeReference("System.Windows.Input"u8, "ICommand"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Collections.Specialized.INotifyCollectionChanged"/>.
    /// </summary>
    public TypeReference INotifyCollectionChanged => field ??= SystemObjectModel.CreateTypeReference("System.Collections.Specialized"u8, "INotifyCollectionChanged"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.ComponentModel.INotifyDataErrorInfo"/>.
    /// </summary>
    public TypeReference INotifyDataErrorInfo => field ??= SystemObjectModel.CreateTypeReference("System.ComponentModel"u8, "INotifyDataErrorInfo"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.ComponentModel.INotifyPropertyChanged"/>.
    /// </summary>
    public TypeReference INotifyPropertyChanged => field ??= SystemObjectModel.CreateTypeReference("System.ComponentModel"u8, "INotifyPropertyChanged"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Collections.IEnumerator"/>.
    /// </summary>
    public TypeReference IEnumerator => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System.Collections"u8, "IEnumerator"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Collections.Generic.IEnumerator{T}"/>.
    /// </summary>
    public TypeReference IEnumerator1 => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System.Collections.Generic"u8, "IEnumerator`1"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Collections.IEnumerable"/>.
    /// </summary>
    public TypeReference IEnumerable => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System.Collections"u8, "IEnumerable"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Collections.Generic.IEnumerable{T}"/>.
    /// </summary>
    public TypeReference IEnumerable1 => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System.Collections.Generic"u8, "IEnumerable`1"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Collections.ICollection"/>.
    /// </summary>
    public TypeReference ICollection => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System.Collections"u8, "ICollection"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Collections.Generic.ICollection{T}"/>.
    /// </summary>
    public TypeReference ICollection1 => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System.Collections.Generic"u8, "ICollection`1"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Collections.Generic.IReadOnlyCollection{T}"/>.
    /// </summary>
    public TypeReference IReadOnlyCollection1 => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System.Collections.Generic"u8, "IReadOnlyCollection`1"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Collections.IList"/>.
    /// </summary>
    public TypeReference IList => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System.Collections"u8, "IList"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Collections.Generic.IList{T}"/>.
    /// </summary>
    public TypeReference IList1 => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System.Collections.Generic"u8, "IList`1"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Collections.Generic.IReadOnlyList{T}"/>.
    /// </summary>
    public TypeReference IReadOnlyList1 => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System.Collections.Generic"u8, "IReadOnlyList`1"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Collections.Generic.IDictionary{TKey, TValue}"/>.
    /// </summary>
    public TypeReference IDictionary2 => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System.Collections.Generic"u8, "IDictionary`2"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Collections.Generic.IReadOnlyDictionary{TKey, TValue}"/>.
    /// </summary>
    public TypeReference IReadOnlyDictionary2 => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System.Collections.Generic"u8, "IReadOnlyDictionary`2"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Collections.Generic.KeyValuePair"/>.
    /// </summary>
    public TypeReference KeyValuePair => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System.Collections.Generic"u8, "KeyValuePair"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}"/>.
    /// </summary>
    public TypeReference KeyValuePair2 => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System.Collections.Generic"u8, "KeyValuePair`2"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Collections.ObjectModel.ReadOnlyCollection{T}"/>.
    /// </summary>
    public TypeReference ReadOnlyCollection1 => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System.Collections.ObjectModel"u8, "ReadOnlyCollection`1"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Collections.Specialized.NotifyCollectionChangedAction"/>.
    /// </summary>
    public TypeReference NotifyCollectionChangedAction => field ??= SystemObjectModel.CreateTypeReference("System.Collections.Specialized"u8, "NotifyCollectionChangedAction"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Collections.Specialized.NotifyCollectionChangedEventHandler"/>.
    /// </summary>
    public TypeReference NotifyCollectionChangedEventHandler => field ??= SystemObjectModel.CreateTypeReference("System.Collections.Specialized"u8, "NotifyCollectionChangedEventHandler"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Collections.Specialized.NotifyCollectionChangedEventArgs"/>.
    /// </summary>
    public TypeReference NotifyCollectionChangedEventArgs => field ??= SystemObjectModel.CreateTypeReference("System.Collections.Specialized"u8, "NotifyCollectionChangedEventArgs"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.ComponentModel.PropertyChangedEventHandler"/>.
    /// </summary>
    public TypeReference PropertyChangedEventHandler => field ??= SystemObjectModel.CreateTypeReference("System.ComponentModel"u8, "PropertyChangedEventHandler"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.ComponentModel.PropertyChangedEventArgs"/>.
    /// </summary>
    public TypeReference PropertyChangedEventArgs => field ??= SystemObjectModel.CreateTypeReference("System.ComponentModel"u8, "PropertyChangedEventArgs"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.ComponentModel.DataErrorsChangedEventArgs"/>.
    /// </summary>
    public TypeReference DataErrorsChangedEventArgs => field ??= SystemObjectModel.CreateTypeReference("System.ComponentModel"u8, "DataErrorsChangedEventArgs"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.MemoryExtensions"/>.
    /// </summary>
    public TypeReference MemoryExtensions => field ??= SystemMemory.CreateTypeReference("System"u8, "MemoryExtensions"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Threading.Interlocked"/>.
    /// </summary>
    public TypeReference Interlocked => field ??= SystemThreading.CreateTypeReference("System.Threading"u8, "Interlocked"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Runtime.InteropServices.MemoryMarshal"/>.
    /// </summary>
    public TypeReference MemoryMarshal => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System.Runtime.InteropServices"u8, "MemoryMarshal"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Runtime.InteropServices.ComWrappers"/>.
    /// </summary>
    public TypeReference ComWrappers => field ??= SystemRuntimeInteropServices.CreateTypeReference("System.Runtime.InteropServices"u8, "ComWrappers"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Runtime.InteropServices.ComWrappers.ComInterfaceDispatch"/>.
    /// </summary>
    public TypeReference ComInterfaceDispatch => field ??= ComWrappers.CreateTypeReference("ComInterfaceDispatch"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Runtime.InteropServices.ComWrappers.ComInterfaceEntry"/>.
    /// </summary>
    public TypeReference ComInterfaceEntry => field ??= ComWrappers.CreateTypeReference("ComInterfaceEntry"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Runtime.InteropServices.CreateComInterfaceFlags"/>.
    /// </summary>
    public TypeReference CreateComInterfaceFlags => field ??= SystemRuntimeInteropServices.CreateTypeReference("System.Runtime.InteropServices"u8, "CreateComInterfaceFlags"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Runtime.InteropServices.CreatedWrapperFlags"/>.
    /// </summary>
    public TypeReference CreatedWrapperFlags => field ??= SystemRuntimeInteropServices.CreateTypeReference("System.Runtime.InteropServices"u8, "CreatedWrapperFlags"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Runtime.InteropServices.InAttribute"/>.
    /// </summary>
    public TypeReference InAttribute => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System.Runtime.InteropServices"u8, "InAttribute"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Runtime.InteropServices.DynamicInterfaceCastableImplementationAttribute"/>.
    /// </summary>
    public TypeReference DynamicInterfaceCastableImplementationAttribute => field ??= SystemRuntimeInteropServices.CreateTypeReference("System.Runtime.InteropServices"u8, "DynamicInterfaceCastableImplementationAttribute"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Runtime.InteropServices.Marshalling.IUnknownDerivedAttribute{T, TImpl}"/>.
    /// </summary>
    public TypeReference IUnknownDerivedAttribute2 => field ??= SystemRuntimeInteropServices.CreateTypeReference("System.Runtime.InteropServices.Marshalling"u8, "IUnknownDerivedAttribute`2"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Runtime.InteropServices.Marshalling.IIUnknownInterfaceType"/>.
    /// </summary>
    public TypeReference IIUnknownInterfaceType => field ??= SystemRuntimeInteropServices.CreateTypeReference("System.Runtime.InteropServices.Marshalling"u8, "IIUnknownInterfaceType"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Runtime.CompilerServices.IsVolatile"/>.
    /// </summary>
    public TypeReference IsVolatile => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System.Runtime.CompilerServices"u8, "IsVolatile"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Runtime.CompilerServices.IsReadOnlyAttribute"/>.
    /// </summary>
    public TypeReference IsReadOnlyAttribute => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System.Runtime.CompilerServices"u8, "IsReadOnlyAttribute"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Runtime.CompilerServices.FixedAddressValueTypeAttribute"/>.
    /// </summary>
    public TypeReference FixedAddressValueTypeAttribute => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System.Runtime.CompilerServices"u8, "FixedAddressValueTypeAttribute"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Runtime.CompilerServices.ScopedRefAttribute"/>.
    /// </summary>
    public TypeReference ScopedRefAttribute => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System.Runtime.CompilerServices"u8, "ScopedRefAttribute"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Runtime.CompilerServices.CallConvMemberFunction"/>.
    /// </summary>
    public TypeReference CallConvMemberFunction => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System.Runtime.CompilerServices"u8, "CallConvMemberFunction"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Runtime.CompilerServices.ConditionalWeakTable{TKey, TValue}"/>.
    /// </summary>
    public TypeReference ConditionalWeakTable2 => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System.Runtime.CompilerServices"u8, "ConditionalWeakTable`2"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute"/>.
    /// </summary>
    public TypeReference UnmanagedCallersOnlyAttribute => field ??= SystemRuntimeInteropServices.CreateTypeReference("System.Runtime.InteropServices"u8, "UnmanagedCallersOnlyAttribute"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Runtime.CompilerServices.DisableRuntimeMarshallingAttribute"/>.
    /// </summary>
    public TypeReference DisableRuntimeMarshallingAttribute => field ??= SystemRuntimeInteropServices.CreateTypeReference("System.Runtime.CompilerServices"u8, "DisableRuntimeMarshallingAttribute"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Reflection.AssemblyMetadataAttribute"/>.
    /// </summary>
    public TypeReference AssemblyMetadataAttribute => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System.Reflection"u8, "AssemblyMetadataAttribute"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>ABI.System.Type</c>.
    /// </summary>
    public TypeReference AbiType => field ??= _windowsRuntimeModule.CreateTypeReference("ABI.System"u8, "Type"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>ABI.System.Exception</c>.
    /// </summary>
    public TypeReference AbiException => field ??= _windowsRuntimeModule.CreateTypeReference("ABI.System"u8, "Exception"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>ABI.System.TimeSpan</c>.
    /// </summary>
    public TypeReference AbiTimeSpan => field ??= _windowsRuntimeModule.CreateTypeReference("ABI.System"u8, "TimeSpan"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>ABI.System.DateTimeOffset</c>.
    /// </summary>
    public TypeReference AbiDateTimeOffset => field ??= _windowsRuntimeModule.CreateTypeReference("ABI.System"u8, "DateTimeOffset"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>ABI.System.TypeMarshaller</c>.
    /// </summary>
    public TypeReference TypeMarshaller => field ??= _windowsRuntimeModule.CreateTypeReference("ABI.System"u8, "TypeMarshaller"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>ABI.System.TypeMarshaller</c>.
    /// </summary>
    public TypeReference ExceptionMarshaller => field ??= _windowsRuntimeModule.CreateTypeReference("ABI.System"u8, "ExceptionMarshaller"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>ABI.System.TimeSpanMarshaller</c>.
    /// </summary>
    public TypeReference TimeSpanMarshaller => field ??= _windowsRuntimeModule.CreateTypeReference("ABI.System"u8, "TimeSpanMarshaller"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>ABI.System.DateTimeOffsetMarshaller</c>.
    /// </summary>
    public TypeReference DateTimeOffsetMarshaller => field ??= _windowsRuntimeModule.CreateTypeReference("ABI.System"u8, "DateTimeOffsetMarshaller"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.WindowsRuntimeClassNameAttribute</c>.
    /// </summary>
    public TypeReference WindowsRuntimeClassNameAttribute => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime"u8, "WindowsRuntimeClassNameAttribute"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.WindowsRuntimeMetadataTypeNameAttribute</c>.
    /// </summary>
    public TypeReference WindowsRuntimeMetadataTypeNameAttribute => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime"u8, "WindowsRuntimeMetadataTypeNameAttribute"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.WindowsRuntimeDefaultInterfaceAttribute</c>.
    /// </summary>
    public TypeReference WindowsRuntimeDefaultInterfaceAttribute => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime"u8, "WindowsRuntimeDefaultInterfaceAttribute"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.WindowsRuntimeExclusiveToInterfaceAttribute</c>.
    /// </summary>
    public TypeReference WindowsRuntimeExclusiveToInterfaceAttribute => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime"u8, "WindowsRuntimeExclusiveToInterfaceAttribute"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.WindowsRuntimeMetadataAttribute</c>.
    /// </summary>
    public TypeReference WindowsRuntimeMetadataAttribute => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime"u8, "WindowsRuntimeMetadataAttribute"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.WindowsRuntimeMappedMetadataAttribute</c>.
    /// </summary>
    public TypeReference WindowsRuntimeMappedMetadataAttribute => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime"u8, "WindowsRuntimeMappedMetadataAttribute"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.WindowsRuntimeReferenceTypeAttribute</c>.
    /// </summary>
    public TypeReference WindowsRuntimeReferenceTypeAttribute => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime"u8, "WindowsRuntimeReferenceTypeAttribute"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeManagedOnlyTypeAttribute</c>.
    /// </summary>
    public TypeReference WindowsRuntimeManagedOnlyTypeAttribute => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "WindowsRuntimeManagedOnlyTypeAttribute"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeMappedTypeAttribute</c>.
    /// </summary>
    public TypeReference WindowsRuntimeMappedTypeAttribute => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "WindowsRuntimeMappedTypeAttribute"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeComWrappersTypeMapGroup</c>.
    /// </summary>
    public TypeReference WindowsRuntimeComWrappersTypeMapGroup => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "WindowsRuntimeComWrappersTypeMapGroup"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeMetadataTypeMapGroup</c>.
    /// </summary>
    public TypeReference WindowsRuntimeMetadataTypeMapGroup => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "WindowsRuntimeMetadataTypeMapGroup"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.DynamicInterfaceCastableImplementationTypeMapGroup</c>.
    /// </summary>
    public TypeReference DynamicInterfaceCastableImplementationTypeMapGroup => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "DynamicInterfaceCastableImplementationTypeMapGroup"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.WellKnownInterfaceIIDs</c>.
    /// </summary>
    public TypeReference WellKnownInterfaceIIDs => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "WellKnownInterfaceIIDs"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IUnknownImpl</c>.
    /// </summary>
    public TypeReference IUnknownImpl => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IUnknownImpl"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IInspectableImpl</c>.
    /// </summary>
    public TypeReference IInspectableImpl => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IInspectableImpl"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IPropertyValueImpl</c>.
    /// </summary>
    public TypeReference IPropertyValueImpl => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IPropertyValueImpl"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IStringableImpl</c>.
    /// </summary>
    public TypeReference IStringableImpl => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IStringableImpl"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IMarshalImpl</c>.
    /// </summary>
    public TypeReference IMarshalImpl => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IMarshalImpl"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IWeakReferenceSourceImpl</c>.
    /// </summary>
    public TypeReference IWeakReferenceSourceImpl => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IWeakReferenceSourceImpl"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IAgileObjectImpl</c>.
    /// </summary>
    public TypeReference IAgileObjectImpl => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IAgileObjectImpl"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IAsyncActionWithProgressMethods</c>.
    /// </summary>
    public TypeReference IAsyncActionWithProgressMethods => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IAsyncActionWithProgressMethods"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IAsyncActionWithProgressMethodsImpl&lt;TProgress&gt;</c>.
    /// </summary>
    public TypeReference IAsyncActionWithProgressMethodsImpl1 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IAsyncActionWithProgressMethodsImpl`1"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IAsyncOperationMethodsImpl&lt;TResult&gt;</c>.
    /// </summary>
    public TypeReference IAsyncOperationMethodsImpl1 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IAsyncOperationMethodsImpl`1"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IAsyncOperationWithProgressMethodsImpl&lt;TResult, TProgress&gt;</c>.
    /// </summary>
    public TypeReference IAsyncOperationWithProgressMethodsImpl2 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IAsyncOperationWithProgressMethodsImpl`2"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IIterableMethodsImpl&lt;T&gt;</c>.
    /// </summary>
    public TypeReference IIterableMethodsImpl1 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IIterableMethodsImpl`1"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IIteratorMethods</c>.
    /// </summary>
    public TypeReference IIteratorMethods => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IIteratorMethods"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IIteratorMethodsImpl&lt;T&gt;</c>.
    /// </summary>
    public TypeReference IIteratorMethodsImpl1 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IIteratorMethodsImpl`1"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IEnumerableAdapter&lt;T&gt;</c>.
    /// </summary>
    public TypeReference IEnumerableAdapter1 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IEnumerableAdapter`1"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IEnumerableMethods&lt;T&gt;</c>.
    /// </summary>
    public TypeReference IEnumerableMethods1 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IEnumerableMethods`1"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IEnumeratorAdapter&lt;T&gt;</c>.
    /// </summary>
    public TypeReference IEnumeratorAdapter1 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IEnumeratorAdapter`1"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IEnumeratorAdapterExtensions</c>.
    /// </summary>
    public TypeReference IEnumeratorAdapterExtensions => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IEnumeratorAdapterExtensions"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IEnumeratorAdapterBlittableValueTypeExtensions</c>.
    /// </summary>
    public TypeReference IEnumeratorAdapterBlittableValueTypeExtensions => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IEnumeratorAdapterBlittableValueTypeExtensions"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IEnumeratorAdapterUnmanagedValueTypeExtensions</c>.
    /// </summary>
    public TypeReference IEnumeratorAdapterUnmanagedValueTypeExtensions => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IEnumeratorAdapterUnmanagedValueTypeExtensions"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IEnumeratorAdapterManagedValueTypeExtensions</c>.
    /// </summary>
    public TypeReference IEnumeratorAdapterManagedValueTypeExtensions => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IEnumeratorAdapterManagedValueTypeExtensions"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IEnumeratorAdapterKeyValuePairTypeExtensions</c>.
    /// </summary>
    public TypeReference IEnumeratorAdapterKeyValuePairTypeExtensions => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IEnumeratorAdapterKeyValuePairTypeExtensions"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IEnumeratorAdapterNullableTypeExtensions</c>.
    /// </summary>
    public TypeReference IEnumeratorAdapterNullableTypeExtensions => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IEnumeratorAdapterNullableTypeExtensions"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IEnumeratorAdapterReferenceTypeExtensions</c>.
    /// </summary>
    public TypeReference IEnumeratorAdapterReferenceTypeExtensions => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IEnumeratorAdapterReferenceTypeExtensions"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IVectorMethodsImpl&lt;T&gt;</c>.
    /// </summary>
    public TypeReference IVectorMethodsImpl1 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IVectorMethodsImpl`1"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IVectorMethodsImpl&lt;T&gt;</c>.
    /// </summary>
    public TypeReference IVectorMethods1 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IVectorMethodsImpl`1"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IVectorViewMethodsImpl&lt;T&gt;</c>.
    /// </summary>
    public TypeReference IVectorViewMethods1 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IVectorViewMethodsImpl`1"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IMapMethodsImpl&lt;K, V&gt;</c>.
    /// </summary>
    public TypeReference IMapMethodsImpl2 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IMapMethodsImpl`2"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IMapViewMethodsImpl&lt;K, V&gt;</c>.
    /// </summary>
    public TypeReference IMapViewMethodsImpl2 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IMapViewMethodsImpl`2"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IObservableVectorEventSourceFactory&lt;T&gt;</c>.
    /// </summary>
    public TypeReference IObservableVectorEventSourceFactory1 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IObservableVectorEventSourceFactory`1"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IObservableMapEventSourceFactory&lt;TKey, TValue&gt;</c>.
    /// </summary>
    public TypeReference IObservableMapEventSourceFactory2 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IObservableMapEventSourceFactory`2"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IMapChangedEventArgsMethodsImpl&lt;K&gt;</c>.
    /// </summary>
    public TypeReference IMapChangedEventArgsMethodsImpl1 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IMapChangedEventArgsMethodsImpl`1"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IListMethods</c>.
    /// </summary>
    public TypeReference IListMethods => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IListMethods"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IReadOnlyListMethods</c>.
    /// </summary>
    public TypeReference IReadOnlyListMethods => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IReadOnlyListMethods"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IListMethods&lt;T&gt;</c>.
    /// </summary>
    public TypeReference IListMethods1 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IListMethods`1"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IListAdapter&lt;T&gt;</c>.
    /// </summary>
    public TypeReference IListAdapter1 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IListAdapter`1"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IListAdapterExtensions</c>.
    /// </summary>
    public TypeReference IListAdapterExtensions => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IListAdapterExtensions"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IListAdapterBlittableValueTypeExtensions</c>.
    /// </summary>
    public TypeReference IListAdapterBlittableValueTypeExtensions => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IListAdapterBlittableValueTypeExtensions"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IListAdapterUnmanagedValueTypeExtensions</c>.
    /// </summary>
    public TypeReference IListAdapterUnmanagedValueTypeExtensions => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IListAdapterUnmanagedValueTypeExtensions"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IListAdapterManagedValueTypeExtensions</c>.
    /// </summary>
    public TypeReference IListAdapterManagedValueTypeExtensions => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IListAdapterManagedValueTypeExtensions"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IListAdapterKeyValuePairTypeExtensions</c>.
    /// </summary>
    public TypeReference IListAdapterKeyValuePairTypeExtensions => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IListAdapterKeyValuePairTypeExtensions"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IListAdapterNullableTypeExtensions</c>.
    /// </summary>
    public TypeReference IListAdapterNullableTypeExtensions => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IListAdapterNullableTypeExtensions"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IListAdapterReferenceTypeExtensions</c>.
    /// </summary>
    public TypeReference IListAdapterReferenceTypeExtensions => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IListAdapterReferenceTypeExtensions"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IReadOnlyListAdapter&lt;T&gt;</c>.
    /// </summary>
    public TypeReference IReadOnlyListAdapter1 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IReadOnlyListAdapter`1"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IReadOnlyListAdapterExtensions</c>.
    /// </summary>
    public TypeReference IReadOnlyListAdapterExtensions => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IReadOnlyListAdapterExtensions"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IReadOnlyListAdapterBlittableValueTypeExtensions</c>.
    /// </summary>
    public TypeReference IReadOnlyListAdapterBlittableValueTypeExtensions => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IReadOnlyListAdapterBlittableValueTypeExtensions"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IReadOnlyListAdapterUnmanagedValueTypeExtensions</c>.
    /// </summary>
    public TypeReference IReadOnlyListAdapterUnmanagedValueTypeExtensions => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IReadOnlyListAdapterUnmanagedValueTypeExtensions"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IReadOnlyListAdapterManagedValueTypeExtensions</c>.
    /// </summary>
    public TypeReference IReadOnlyListAdapterManagedValueTypeExtensions => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IReadOnlyListAdapterManagedValueTypeExtensions"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IReadOnlyListAdapterKeyValuePairTypeExtensions</c>.
    /// </summary>
    public TypeReference IReadOnlyListAdapterKeyValuePairTypeExtensions => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IReadOnlyListAdapterKeyValuePairTypeExtensions"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IReadOnlyListAdapterNullableTypeExtensions</c>.
    /// </summary>
    public TypeReference IReadOnlyListAdapterNullableTypeExtensions => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IReadOnlyListAdapterNullableTypeExtensions"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IReadOnlyListAdapterReferenceTypeExtensions</c>.
    /// </summary>
    public TypeReference IReadOnlyListAdapterReferenceTypeExtensions => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IReadOnlyListAdapterReferenceTypeExtensions"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IReadOnlyListMethods&lt;T&gt;</c>.
    /// </summary>
    public TypeReference IReadOnlyListMethods1 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IReadOnlyListMethods`1"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IDictionaryAdapter&lt;TKey, TValue&gt;</c>.
    /// </summary>
    public TypeReference IDictionaryAdapter2 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IDictionaryAdapter`2"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IDictionaryAdapterExtensions</c>.
    /// </summary>
    public TypeReference IDictionaryAdapterExtensions => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IDictionaryAdapterExtensions"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IDictionaryMethods</c>.
    /// </summary>
    public TypeReference IDictionaryMethods => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IDictionaryMethods"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IDictionaryMethods&lt;TKey, TValue&gt;</c>.
    /// </summary>
    public TypeReference IDictionaryMethods2 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IDictionaryMethods`2"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IReadOnlyDictionaryAdapter&lt;TKey, TValue&gt;</c>.
    /// </summary>
    public TypeReference IReadOnlyDictionaryAdapter2 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IReadOnlyDictionaryAdapter`2"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IReadOnlyDictionaryAdapterExtensions</c>.
    /// </summary>
    public TypeReference IReadOnlyDictionaryAdapterExtensions => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IReadOnlyDictionaryAdapterExtensions"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IReadOnlyDictionarySplitAdapter2&lt;TKey, TValue&gt;</c>.
    /// </summary>
    public TypeReference IReadOnlyDictionarySplitAdapter2 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IReadOnlyDictionarySplitAdapter`2"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IReadOnlyDictionaryMethods</c>.
    /// </summary>
    public TypeReference IReadOnlyDictionaryMethods => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IReadOnlyDictionaryMethods"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IReadOnlyDictionaryMethods&lt;TKey, TValue&gt;</c>.
    /// </summary>
    public TypeReference IReadOnlyDictionaryMethods2 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IReadOnlyDictionaryMethods`2"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IMapChangedEventArgsMethods</c>.
    /// </summary>
    public TypeReference IMapChangedEventArgsMethods => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IMapChangedEventArgsMethods"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeObject</c>.
    /// </summary>
    public TypeReference WindowsRuntimeObject => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime"u8, "WindowsRuntimeObject"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.WindowsRuntimeAsyncActionWithProgress&lt;T, ...&gt;</c>.
    /// </summary>
    public TypeReference WindowsRuntimeAsyncActionWithProgress2 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime"u8, "WindowsRuntimeAsyncActionWithProgress`2"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.WindowsRuntimeAsyncOperation&lt;T, ...&gt;</c>.
    /// </summary>
    public TypeReference WindowsRuntimeAsyncOperation2 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime"u8, "WindowsRuntimeAsyncOperation`2"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.WindowsRuntimeAsyncOperationWithProgress&lt;T, ...&gt;</c>.
    /// </summary>
    public TypeReference WindowsRuntimeAsyncOperationWithProgress3 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime"u8, "WindowsRuntimeAsyncOperationWithProgress`3"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.WindowsRuntimeEnumerator&lt;T, ...&gt;</c>.
    /// </summary>
    public TypeReference WindowsRuntimeEnumerator2 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime"u8, "WindowsRuntimeEnumerator`2"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.WindowsRuntimeEnumerable&lt;T, ...&gt;</c>.
    /// </summary>
    public TypeReference WindowsRuntimeEnumerable2 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime"u8, "WindowsRuntimeEnumerable`2"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.WindowsRuntimeList&lt;T, ...&gt;</c>.
    /// </summary>
    public TypeReference WindowsRuntimeList4 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime"u8, "WindowsRuntimeList`4"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.WindowsRuntimeReadOnlyList&lt;T, ...&gt;</c>.
    /// </summary>
    public TypeReference WindowsRuntimeReadOnlyList4 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime"u8, "WindowsRuntimeReadOnlyList`4"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.WindowsRuntimeDictionary&lt;TKey, TValue, ...&gt;</c>.
    /// </summary>
    public TypeReference WindowsRuntimeDictionary5 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime"u8, "WindowsRuntimeDictionary`5"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.WindowsRuntimeReadOnlyDictionary&lt;TKey, TValue, ...&gt;</c>.
    /// </summary>
    public TypeReference WindowsRuntimeReadOnlyDictionary5 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime"u8, "WindowsRuntimeReadOnlyDictionary`5"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.WindowsRuntimeObservableVector&lt;T, ...&gt;</c>.
    /// </summary>
    public TypeReference WindowsRuntimeObservableVector6 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime"u8, "WindowsRuntimeObservableVector`6"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.WindowsRuntimeObservableMap&lt;TKey, TValue, ...&gt;</c>.
    /// </summary>
    public TypeReference WindowsRuntimeObservableMap7 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime"u8, "WindowsRuntimeObservableMap`7"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.WindowsRuntimeMapChangedEventArgs&lt;TKey, ...&gt;</c>.
    /// </summary>
    public TypeReference WindowsRuntimeMapChangedEventArgs2 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime"u8, "WindowsRuntimeMapChangedEventArgs`2"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.DictionaryKeyCollection2&lt;TKey, TValue&gt;</c>.
    /// </summary>
    public TypeReference DictionaryKeyCollection2 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime"u8, "DictionaryKeyCollection`2"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.DictionaryKeyCollection2&lt;TKey, TValue&gt;.Enumerator</c>.
    /// </summary>
    public TypeReference DictionaryKeyCollection2Enumerator => field ??= DictionaryKeyCollection2.CreateTypeReference("Enumerator"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.DictionaryValueCollection2&lt;TKey, TValue&gt;</c>.
    /// </summary>
    public TypeReference DictionaryValueCollection2 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime"u8, "DictionaryValueCollection`2"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.DictionaryValueCollection2&lt;TKey, TValue&gt;.Enumerator</c>.
    /// </summary>
    public TypeReference DictionaryValueCollection2Enumerator => field ??= DictionaryValueCollection2.CreateTypeReference("Enumerator"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Collections.ObjectModel.ReadOnlyDictionary{TKey, TValue}"/>.
    /// </summary>
    public TypeReference ReadOnlyDictionary2 => field ??= _corLibTypeFactory.CorLibScope.CreateTypeReference("System.Collections.ObjectModel"u8, "ReadOnlyDictionary`2"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.ReadOnlyDictionaryKeyCollection2&lt;TKey, TValue&gt;</c>.
    /// </summary>
    public TypeReference ReadOnlyDictionaryKeyCollection2 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime"u8, "ReadOnlyDictionaryKeyCollection`2"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.ReadOnlyDictionaryKeyCollection2&lt;TKey, TValue&gt;.Enumerator</c>.
    /// </summary>
    public TypeReference ReadOnlyDictionaryKeyCollection2Enumerator => field ??= ReadOnlyDictionaryKeyCollection2.CreateTypeReference("Enumerator"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.ReadOnlyDictionaryValueCollection2&lt;TKey, TValue&gt;</c>.
    /// </summary>
    public TypeReference ReadOnlyDictionaryValueCollection2 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime"u8, "ReadOnlyDictionaryValueCollection`2"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.ReadOnlyDictionaryValueCollection2&lt;TKey, TValue&gt;.Enumerator</c>.
    /// </summary>
    public TypeReference ReadOnlyDictionaryValueCollection2Enumerator => field ??= ReadOnlyDictionaryValueCollection2.CreateTypeReference("Enumerator"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.BindableIReadOnlyListAdapter</c>.
    /// </summary>
    public TypeReference BindableIReadOnlyListAdapter => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "BindableIReadOnlyListAdapter"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IWindowsRuntimeInterface</c>.
    /// </summary>
    public TypeReference IWindowsRuntimeInterface => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IWindowsRuntimeInterface"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IWindowsRuntimeObjectComWrappersCallback</c>.
    /// </summary>
    public TypeReference IWindowsRuntimeObjectComWrappersCallback => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IWindowsRuntimeObjectComWrappersCallback"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IWindowsRuntimeUnsealedObjectComWrappersCallback</c>.
    /// </summary>
    public TypeReference IWindowsRuntimeUnsealedObjectComWrappersCallback => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IWindowsRuntimeUnsealedObjectComWrappersCallback"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IWindowsRuntimeArrayComWrappersCallback</c>.
    /// </summary>
    public TypeReference IWindowsRuntimeArrayComWrappersCallback => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "IWindowsRuntimeArrayComWrappersCallback"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.DynamicInterfaceCastableForwarderAttribute</c>.
    /// </summary>
    public TypeReference DynamicInterfaceCastableForwarderAttribute => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "DynamicInterfaceCastableForwarderAttribute"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeComWrappersMarshallerAttribute</c>.
    /// </summary>
    public TypeReference WindowsRuntimeComWrappersMarshallerAttribute => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "WindowsRuntimeComWrappersMarshallerAttribute"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeObjectReference</c>.
    /// </summary>
    public TypeReference WindowsRuntimeObjectReference => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "WindowsRuntimeObjectReference"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeObjectReferenceValue</c>.
    /// </summary>
    public TypeReference WindowsRuntimeObjectReferenceValue => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "WindowsRuntimeObjectReferenceValue"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeComWrappersMarshal</c>.
    /// </summary>
    public TypeReference WindowsRuntimeComWrappersMarshal => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "WindowsRuntimeComWrappersMarshal"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.WindowsRuntimeUnknownMarshaller</c>.
    /// </summary>
    public TypeReference WindowsRuntimeUnknownMarshaller => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices.Marshalling"u8, "WindowsRuntimeUnknownMarshaller"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.WindowsRuntimeObjectMarshaller</c>.
    /// </summary>
    public TypeReference WindowsRuntimeObjectMarshaller => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices.Marshalling"u8, "WindowsRuntimeObjectMarshaller"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeUnsealedObjectMarshaller</c>.
    /// </summary>
    public TypeReference WindowsRuntimeUnsealedObjectMarshaller => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices.Marshalling"u8, "WindowsRuntimeUnsealedObjectMarshaller"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeInterfaceMarshaller&lt;T&gt;</c>.
    /// </summary>
    public TypeReference WindowsRuntimeInterfaceMarshaller1 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices.Marshalling"u8, "WindowsRuntimeInterfaceMarshaller`1"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeDelegateMarshaller</c>.
    /// </summary>
    public TypeReference WindowsRuntimeDelegateMarshaller => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices.Marshalling"u8, "WindowsRuntimeDelegateMarshaller"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeArrayMarshaller</c>.
    /// </summary>
    public TypeReference WindowsRuntimeArrayMarshaller => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices.Marshalling"u8, "WindowsRuntimeArrayMarshaller"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.WindowsRuntimeBlittableValueTypeArrayMarshaller</c>.
    /// </summary>
    public TypeReference WindowsRuntimeBlittableValueTypeArrayMarshaller => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices.Marshalling"u8, "WindowsRuntimeBlittableValueTypeArrayMarshaller"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.WindowsRuntimeBlittableValueTypeArrayMarshaller&lt;T&gt;</c>.
    /// </summary>
    public TypeReference WindowsRuntimeBlittableValueTypeArrayMarshaller1 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices.Marshalling"u8, "WindowsRuntimeBlittableValueTypeArrayMarshaller`1"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.WindowsRuntimeManagedValueTypeArrayMarshaller&lt;T, TAbi&gt;</c>.
    /// </summary>
    public TypeReference WindowsRuntimeManagedValueTypeArrayMarshaller2 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices.Marshalling"u8, "WindowsRuntimeManagedValueTypeArrayMarshaller`2"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.WindowsRuntimeUnmanagedValueTypeArrayMarshaller&lt;T, TAbi&gt;</c>.
    /// </summary>
    public TypeReference WindowsRuntimeUnmanagedValueTypeArrayMarshaller2 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices.Marshalling"u8, "WindowsRuntimeUnmanagedValueTypeArrayMarshaller`2"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.WindowsRuntimeReferenceTypeArrayMarshaller&lt;T&gt;</c>.
    /// </summary>
    public TypeReference WindowsRuntimeReferenceTypeArrayMarshaller1 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices.Marshalling"u8, "WindowsRuntimeReferenceTypeArrayMarshaller`1"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.WindowsRuntimeUnknownArrayMarshaller</c>.
    /// </summary>
    public TypeReference WindowsRuntimeUnknownArrayMarshaller => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices.Marshalling"u8, "WindowsRuntimeUnknownArrayMarshaller"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.WindowsRuntimeObjectArrayMarshaller</c>.
    /// </summary>
    public TypeReference WindowsRuntimeObjectArrayMarshaller => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices.Marshalling"u8, "WindowsRuntimeObjectArrayMarshaller"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.WindowsRuntimeKeyValuePairTypeArrayMarshaller&lt;TKey, TValue&gt;</c>.
    /// </summary>
    public TypeReference WindowsRuntimeKeyValuePairTypeArrayMarshaller2 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices.Marshalling"u8, "WindowsRuntimeKeyValuePairTypeArrayMarshaller`2"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.IWindowsRuntimeReferenceTypeArrayElementMarshaller&lt;T&gt;</c>.
    /// </summary>
    public TypeReference IWindowsRuntimeReferenceTypeArrayElementMarshaller1 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices.Marshalling"u8, "IWindowsRuntimeReferenceTypeArrayElementMarshaller`1"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.IWindowsRuntimeManagedValueTypeArrayElementMarshaller&lt;T, TAbi&gt;</c>.
    /// </summary>
    public TypeReference IWindowsRuntimeManagedValueTypeArrayElementMarshaller2 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices.Marshalling"u8, "IWindowsRuntimeManagedValueTypeArrayElementMarshaller`2"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.IWindowsRuntimeUnmanagedValueTypeArrayElementMarshaller&lt;T, TAbi&gt;</c>.
    /// </summary>
    public TypeReference IWindowsRuntimeUnmanagedValueTypeArrayElementMarshaller2 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices.Marshalling"u8, "IWindowsRuntimeUnmanagedValueTypeArrayElementMarshaller`2"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.IWindowsRuntimeKeyValuePairTypeArrayElementMarshaller&lt;TKey, TValue&gt;</c>.
    /// </summary>
    public TypeReference IWindowsRuntimeKeyValuePairTypeArrayElementMarshaller2 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices.Marshalling"u8, "IWindowsRuntimeKeyValuePairTypeArrayElementMarshaller`2"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.IWindowsRuntimeReferenceTypeElementMarshaller&lt;T&gt;</c>.
    /// </summary>
    public TypeReference IWindowsRuntimeReferenceTypeElementMarshaller1 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices.Marshalling"u8, "IWindowsRuntimeReferenceTypeElementMarshaller`1"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.IWindowsRuntimeManagedValueTypeElementMarshaller&lt;T, TAbi&gt;</c>.
    /// </summary>
    public TypeReference IWindowsRuntimeManagedValueTypeElementMarshaller2 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices.Marshalling"u8, "IWindowsRuntimeManagedValueTypeElementMarshaller`2"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.IWindowsRuntimeUnmanagedValueTypeElementMarshaller&lt;T, TAbi&gt;</c>.
    /// </summary>
    public TypeReference IWindowsRuntimeUnmanagedValueTypeElementMarshaller2 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices.Marshalling"u8, "IWindowsRuntimeUnmanagedValueTypeElementMarshaller`2"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.IWindowsRuntimeKeyValuePairTypeElementMarshaller&lt;TKey, TValue&gt;</c>.
    /// </summary>
    public TypeReference IWindowsRuntimeKeyValuePairTypeElementMarshaller2 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices.Marshalling"u8, "IWindowsRuntimeKeyValuePairTypeElementMarshaller`2"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.WindowsRuntimeNullableTypeArrayMarshaller&lt;T&gt;</c>.
    /// </summary>
    public TypeReference WindowsRuntimeNullableTypeArrayMarshaller1 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices.Marshalling"u8, "WindowsRuntimeNullableTypeArrayMarshaller`1"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.IWindowsRuntimeNullableTypeArrayElementMarshaller&lt;T&gt;</c>.
    /// </summary>
    public TypeReference IWindowsRuntimeNullableTypeArrayElementMarshaller1 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices.Marshalling"u8, "IWindowsRuntimeNullableTypeArrayElementMarshaller`1"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.IWindowsRuntimeNullableTypeElementMarshaller&lt;T&gt;</c>.
    /// </summary>
    public TypeReference IWindowsRuntimeNullableTypeElementMarshaller1 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices.Marshalling"u8, "IWindowsRuntimeNullableTypeElementMarshaller`1"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.TypeArrayMarshaller</c>.
    /// </summary>
    public TypeReference TypeArrayMarshaller => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices.Marshalling"u8, "TypeArrayMarshaller"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.HStringArrayMarshaller</c>.
    /// </summary>
    public TypeReference HStringArrayMarshaller => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices.Marshalling"u8, "HStringArrayMarshaller"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.ExceptionArrayMarshaller</c>.
    /// </summary>
    public TypeReference ExceptionArrayMarshaller => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices.Marshalling"u8, "ExceptionArrayMarshaller"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeKeyValuePairTypeMarshaller</c>.
    /// </summary>
    public TypeReference WindowsRuntimeKeyValuePairTypeMarshaller => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices.Marshalling"u8, "WindowsRuntimeKeyValuePairTypeMarshaller"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeValueTypeMarshaller</c>.
    /// </summary>
    public TypeReference WindowsRuntimeValueTypeMarshaller => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices.Marshalling"u8, "WindowsRuntimeValueTypeMarshaller"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.TypeReference</c>.
    /// </summary>
    public TypeReference TypeReference => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices.Marshalling"u8, "TypeReference"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.HStringReference</c>.
    /// </summary>
    public TypeReference HStringReference => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices.Marshalling"u8, "HStringReference"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.HStringMarshaller</c>.
    /// </summary>
    public TypeReference HStringMarshaller => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices.Marshalling"u8, "HStringMarshaller"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.RestrictedErrorInfo</c>.
    /// </summary>
    public TypeReference RestrictedErrorInfo => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "RestrictedErrorInfo"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.RestrictedErrorInfoExceptionMarshaller</c>.
    /// </summary>
    public TypeReference RestrictedErrorInfoExceptionMarshaller => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices.Marshalling"u8, "RestrictedErrorInfoExceptionMarshaller"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.EventRegistrationToken</c>.
    /// </summary>
    public TypeReference EventRegistrationToken => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "EventRegistrationToken"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.EventRegistrationTokenTable&lt;T&gt;</c>.
    /// </summary>
    public TypeReference EventRegistrationTokenTable1 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "EventRegistrationTokenTable`1"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.EventSource&lt;T&gt;</c>.
    /// </summary>
    public TypeReference EventSource1 => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "EventSource`1"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.EventHandlerEventSource&lt;TEventArgs&gt;</c>.
    /// </summary>
    public TypeReference EventHandler1EventSource => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "EventHandlerEventSource`1"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.EventHandlerEventSource&lt;TSender, TEventArgs&gt;</c>.
    /// </summary>
    public TypeReference EventHandler2EventSource => field ??= _windowsRuntimeModule.CreateTypeReference("WindowsRuntime.InteropServices"u8, "EventHandlerEventSource`2"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>Windows.Foundation.Collections.IObservableVector&lt;T&gt;</c>.
    /// </summary>
    public TypeReference IObservableVector1 => field ??= _windowsRuntimeModule.CreateTypeReference("Windows.Foundation.Collections"u8, "IObservableVector`1"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>Windows.Foundation.Collections.IObservableMap&lt;K, V&gt;</c>.
    /// </summary>
    public TypeReference IObservableMap2 => field ??= _windowsRuntimeModule.CreateTypeReference("Windows.Foundation.Collections"u8, "IObservableMap`2"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>Windows.Foundation.Collections.IMapChangedEventArgs&lt;K&gt;</c>.
    /// </summary>
    public TypeReference IMapChangedEventArgs1 => field ??= _windowsRuntimeModule.CreateTypeReference("Windows.Foundation.Collections"u8, "IMapChangedEventArgs`1"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>Windows.Foundation.Collections.IVectorChangedEventArgs&lt;T&gt;</c>.
    /// </summary>
    public TypeReference IVectorChangedEventArgs => field ??= _windowsRuntimeModule.CreateTypeReference("Windows.Foundation.Collections"u8, "IVectorChangedEventArgs"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>Windows.Foundation.Collections.CollectionChange</c>.
    /// </summary>
    public TypeReference CollectionChange => field ??= _windowsRuntimeModule.CreateTypeReference("Windows.Foundation.Collections"u8, "CollectionChange"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>Windows.Foundation.Collections.VectorChangedEventHandler&lt;T&gt;</c>.
    /// </summary>
    public TypeReference VectorChangedEventHandler1 => field ??= _windowsRuntimeModule.CreateTypeReference("Windows.Foundation.Collections"u8, "VectorChangedEventHandler`1"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for the event source type for <see cref="VectorChangedEventHandler1"/>.
    /// </summary>
    public TypeReference VectorChangedEventHandler1EventSource => field ??= _windowsRuntimeModule.CreateTypeReference("ABI.Windows.Foundation.Collections"u8, "VectorChangedEventHandlerEventSource`1"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>Windows.Foundation.Collections.MapChangedEventHandler&lt;K, V&gt;</c>.
    /// </summary>
    public TypeReference MapChangedEventHandler2 => field ??= _windowsRuntimeModule.CreateTypeReference("Windows.Foundation.Collections"u8, "MapChangedEventHandler`2"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for the event source type for <see cref="MapChangedEventHandler2"/>.
    /// </summary>
    public TypeReference MapChangedEventHandler2EventSource => field ??= _windowsRuntimeModule.CreateTypeReference("ABI.Windows.Foundation.Collections"u8, "MapChangedEventHandlerEventSource`2"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>Windows.Foundation.TrustLevel</c>.
    /// </summary>
    public TypeReference TrustLevel => field ??= _windowsRuntimeModule.CreateTypeReference("Windows.Foundation"u8, "TrustLevel"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>Windows.Foundation.PropertyType</c>.
    /// </summary>
    public TypeReference PropertyType => field ??= _windowsRuntimeModule.CreateTypeReference("Windows.Foundation"u8, "PropertyType"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>Windows.Foundation.Point</c>.
    /// </summary>
    public TypeReference Point => field ??= _windowsRuntimeModule.CreateTypeReference("Windows.Foundation"u8, "Point"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>Windows.Foundation.Rect</c>.
    /// </summary>
    public TypeReference Rect => field ??= _windowsRuntimeModule.CreateTypeReference("Windows.Foundation"u8, "Rect"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>Windows.Foundation.Size</c>.
    /// </summary>
    public TypeReference Size => field ??= _windowsRuntimeModule.CreateTypeReference("Windows.Foundation"u8, "Size"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>Windows.Foundation.IStringable</c>.
    /// </summary>
    public TypeReference IStringable => field ??= _windowsRuntimeModule.CreateTypeReference("Windows.Foundation"u8, "IStringable"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>Windows.Foundation.AsyncStatus</c>.
    /// </summary>
    public TypeReference AsyncStatus => field ??= _windowsRuntimeModule.CreateTypeReference("Windows.Foundation"u8, "AsyncStatus"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>Windows.Foundation.IAsyncInfo</c>.
    /// </summary>
    public TypeReference IAsyncInfo => field ??= _windowsRuntimeModule.CreateTypeReference("Windows.Foundation"u8, "IAsyncInfo"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>Windows.Foundation.IAsyncAction</c>.
    /// </summary>
    public TypeReference IAsyncAction => field ??= _windowsRuntimeModule.CreateTypeReference("Windows.Foundation"u8, "IAsyncAction"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>Windows.Foundation.AsyncActionCompletedHandler</c>.
    /// </summary>
    public TypeReference AsyncActionCompletedHandler => field ??= _windowsRuntimeModule.CreateTypeReference("Windows.Foundation"u8, "AsyncActionCompletedHandler"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>Windows.Foundation.IAsyncActionWithProgress&lt;TProgress&gt;</c>.
    /// </summary>
    public TypeReference IAsyncActionWithProgress1 => field ??= _windowsRuntimeModule.CreateTypeReference("Windows.Foundation"u8, "IAsyncActionWithProgress`1"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>Windows.Foundation.AsyncActionProgressHandler&lt;TProgress&gt;</c>.
    /// </summary>
    public TypeReference AsyncActionProgressHandler1 => field ??= _windowsRuntimeModule.CreateTypeReference("Windows.Foundation"u8, "AsyncActionProgressHandler`1"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>Windows.Foundation.AsyncActionWithProgressCompletedHandler&lt;TProgress&gt;</c>.
    /// </summary>
    public TypeReference AsyncActionWithProgressCompletedHandler1 => field ??= _windowsRuntimeModule.CreateTypeReference("Windows.Foundation"u8, "AsyncActionWithProgressCompletedHandler`1"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>Windows.Foundation.IAsyncOperation&lt;TResult&gt;</c>.
    /// </summary>
    public TypeReference IAsyncOperation1 => field ??= _windowsRuntimeModule.CreateTypeReference("Windows.Foundation"u8, "IAsyncOperation`1"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>Windows.Foundation.AsyncOperationCompletedHandler&lt;TResult&gt;</c>.
    /// </summary>
    public TypeReference AsyncOperationCompletedHandler1 => field ??= _windowsRuntimeModule.CreateTypeReference("Windows.Foundation"u8, "AsyncOperationCompletedHandler`1"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>Windows.Foundation.IAsyncOperationWithProgress&lt;TResult, TProgress&gt;</c>.
    /// </summary>
    public TypeReference IAsyncOperationWithProgress2 => field ??= _windowsRuntimeModule.CreateTypeReference("Windows.Foundation"u8, "IAsyncOperationWithProgress`2"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>Windows.Foundation.AsyncOperationProgressHandler&lt;TResult, TProgress&gt;</c>.
    /// </summary>
    public TypeReference AsyncOperationProgressHandler2 => field ??= _windowsRuntimeModule.CreateTypeReference("Windows.Foundation"u8, "AsyncOperationProgressHandler`2"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>Windows.Foundation.AsyncOperationWithProgressCompletedHandler&lt;TResult&gt;</c>.
    /// </summary>
    public TypeReference AsyncOperationWithProgressCompletedHandler2 => field ??= _windowsRuntimeModule.CreateTypeReference("Windows.Foundation"u8, "AsyncOperationWithProgressCompletedHandler`2"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>Windows.Foundation.IMemoryBufferReference</c>.
    /// </summary>
    public TypeReference IMemoryBufferReference => field ??= _windowsRuntimeModule.CreateTypeReference("Windows.Foundation"u8, "IMemoryBufferReference"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>Windows.Storage.Streams.IBuffer</c>.
    /// </summary>
    public TypeReference IBuffer => field ??= _windowsRuntimeModule.CreateTypeReference("Windows.Storage.Streams"u8, "IBuffer"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>Windows.Storage.Streams.IInputStream</c>.
    /// </summary>
    public TypeReference IInputStream => field ??= _windowsRuntimeModule.CreateTypeReference("Windows.Storage.Streams"u8, "IInputStream"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>Windows.Storage.Streams.IOutputStream</c>.
    /// </summary>
    public TypeReference IOutputStream => field ??= _windowsRuntimeModule.CreateTypeReference("Windows.Storage.Streams"u8, "IOutputStream"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>Windows.Storage.Streams.IRandomAccessStream</c>.
    /// </summary>
    public TypeReference IRandomAccessStream => field ??= _windowsRuntimeModule.CreateTypeReference("Windows.Storage.Streams"u8, "IRandomAccessStream"u8);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>Windows.Storage.Streams.InputStreamOptions</c>.
    /// </summary>
    public TypeReference InputStreamOptions => field ??= _windowsRuntimeModule.CreateTypeReference("Windows.Storage.Streams"u8, "InputStreamOptions"u8);

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="string.Length"/>.
    /// </summary>
    public MemberReference Stringget_Length => field ??= _corLibTypeFactory.String
        .ToTypeDefOrRef()
        .CreateMemberReference("get_Length"u8, MethodSignature.CreateInstance(_corLibTypeFactory.Int32));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="string.GetPinnableReference"/>.
    /// </summary>
    public MemberReference StringGetPinnableReference => field ??= _corLibTypeFactory.String
        .ToTypeDefOrRef()
        .CreateMemberReference("GetPinnableReference"u8, MethodSignature.CreateInstance(
            returnType: _corLibTypeFactory.Char.MakeByReferenceType().MakeModifierType(InAttribute, isRequired: true)));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="Attribute.Attribute()"/>.
    /// </summary>
    public MemberReference Attribute_ctor => field ??= Attribute.CreateConstructorReference(_corLibTypeFactory);

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="Exception.HResult"/>.
    /// </summary>
    public MemberReference Exceptionget_HResult => field ??= Exception.CreateMemberReference("get_HResult"u8, MethodSignature.CreateInstance(_corLibTypeFactory.Int32));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="NotSupportedException.NotSupportedException()"/>.
    /// </summary>
    public MemberReference NotSupportedException_ctor => field ??= NotSupportedException.CreateConstructorReference(_corLibTypeFactory);

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="Type.GetTypeFromHandle"/>.
    /// </summary>
    public MemberReference TypeGetTypeFromHandle => field ??= Type
        .CreateMemberReference("GetTypeFromHandle"u8, MethodSignature.CreateStatic(
            returnType: Type.ToReferenceTypeSignature(),
            parameterTypes: [RuntimeTypeHandle.ToValueTypeSignature()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="Type.TypeHandle"/>.
    /// </summary>
    public MemberReference Typeget_TypeHandle => field ??= Type
        .CreateMemberReference("get_TypeHandle"u8, MethodSignature.CreateInstance(
            returnType: RuntimeTypeHandle.ToValueTypeSignature()));

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="AttributeUsageAttribute.AttributeUsageAttribute(System.AttributeTargets)"/>.
    /// </summary>
    public MemberReference AttributeUsageAttribute_ctor_AttributeTargets => field ??= AttributeUsageAttribute.CreateConstructorReference(
        corLibTypeFactory: _corLibTypeFactory,
        parameterTypes: [AttributeTargets.ToValueTypeSignature()]);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Runtime.InteropServices.TypeMapAttribute{TTypeMapGroup}.TypeMapAttribute(string, System.Type, System.Type)"/>, using <see cref="WindowsRuntimeComWrappersTypeMapGroup"/>.
    /// </summary>
    public MemberReference TypeMapAttributeWindowsRuntimeComWrappersTypeMapGroup_ctor_TrimTarget => field ??= TypeMapAttribute1_ctor_TrimTarget(WindowsRuntimeComWrappersTypeMapGroup.ToReferenceTypeSignature());

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Runtime.InteropServices.TypeMapAssociationAttribute{TTypeMapGroup}.TypeMapAssociationAttribute(System.Type, System.Type)"/>, using <see cref="WindowsRuntimeComWrappersTypeMapGroup"/>.
    /// </summary>
    public MemberReference TypeMapAssociationAttributeWindowsRuntimeComWrappersTypeMapGroup_ctor => field ??= TypeMapAssociationAttribute1_ctor(WindowsRuntimeComWrappersTypeMapGroup.ToReferenceTypeSignature());

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Runtime.InteropServices.TypeMapAttribute{TTypeMapGroup}.TypeMapAttribute(string, System.Type, System.Type)"/>, using <see cref="WindowsRuntimeMetadataTypeMapGroup"/>.
    /// </summary>
    public MemberReference TypeMapAttributeWindowsRuntimeMetadataTypeMapGroup_ctor_TrimTarget => field ??= TypeMapAttribute1_ctor_TrimTarget(WindowsRuntimeMetadataTypeMapGroup.ToReferenceTypeSignature());

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Runtime.InteropServices.TypeMapAssociationAttribute{TTypeMapGroup}.TypeMapAssociationAttribute(System.Type, System.Type)"/>, using <see cref="WindowsRuntimeMetadataTypeMapGroup"/>.
    /// </summary>
    public MemberReference TypeMapAssociationAttributeWindowsRuntimeMetadataTypeMapGroup_ctor => field ??= TypeMapAssociationAttribute1_ctor(WindowsRuntimeMetadataTypeMapGroup.ToReferenceTypeSignature());

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Runtime.InteropServices.TypeMapAssociationAttribute{TTypeMapGroup}.TypeMapAssociationAttribute(System.Type, System.Type)"/>, using <see cref="DynamicInterfaceCastableImplementationTypeMapGroup"/>.
    /// </summary>
    public MemberReference TypeMapAssociationAttributeDynamicInterfaceCastableImplementationTypeMapGroup_ctor => field ??= TypeMapAssociationAttribute1_ctor(DynamicInterfaceCastableImplementationTypeMapGroup.ToReferenceTypeSignature());

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="IDisposable.Dispose"/>.
    /// </summary>
    public MemberReference IDisposableDispose => field ??= IDisposable
        .CreateMemberReference("Dispose"u8, MethodSignature.CreateInstance(_corLibTypeFactory.Void));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Windows.Input.ICommand.CanExecuteChanged"/>'s adder.
    /// </summary>
    public MemberReference ICommandadd_CanExecuteChanged => field ??= ICommand
        .CreateMemberReference("add_CanExecuteChanged"u8, MethodSignature.CreateInstance(
            returnType: _corLibTypeFactory.Void,
            parameterTypes: [EventHandler.ToReferenceTypeSignature()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Windows.Input.ICommand.CanExecuteChanged"/>'s remover.
    /// </summary>
    public MemberReference ICommandremove_CanExecuteChanged => field ??= ICommand
        .CreateMemberReference("remove_CanExecuteChanged"u8, MethodSignature.CreateInstance(
            returnType: _corLibTypeFactory.Void,
            parameterTypes: [EventHandler.ToReferenceTypeSignature()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Windows.Input.ICommand.CanExecute"/>.
    /// </summary>
    public MemberReference ICommandCanExecute => field ??= ICommand
        .CreateMemberReference("CanExecute"u8, MethodSignature.CreateInstance(
            returnType: _corLibTypeFactory.Boolean,
            parameterTypes: [_corLibTypeFactory.Object]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Windows.Input.ICommand.Execute"/>.
    /// </summary>
    public MemberReference ICommandExecute => field ??= ICommand
        .CreateMemberReference("Execute"u8, MethodSignature.CreateInstance(
            returnType: _corLibTypeFactory.Void,
            parameterTypes: [_corLibTypeFactory.Object]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Collections.Specialized.INotifyCollectionChanged.CollectionChanged"/>'s adder.
    /// </summary>
    public MemberReference INotifyCollectionChangedadd_CollectionChanged => field ??= INotifyCollectionChanged
        .CreateMemberReference("add_CollectionChanged"u8, MethodSignature.CreateInstance(
            returnType: _corLibTypeFactory.Void,
            parameterTypes: [NotifyCollectionChangedEventHandler.ToReferenceTypeSignature()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Collections.Specialized.INotifyCollectionChanged.CollectionChanged"/>'s remover.
    /// </summary>
    public MemberReference INotifyCollectionChangedremove_CollectionChanged => field ??= INotifyCollectionChanged
        .CreateMemberReference("remove_CollectionChanged"u8, MethodSignature.CreateInstance(
            returnType: _corLibTypeFactory.Void,
            parameterTypes: [NotifyCollectionChangedEventHandler.ToReferenceTypeSignature()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.ComponentModel.INotifyPropertyChanged.PropertyChanged"/>'s adder.
    /// </summary>
    public MemberReference INotifyPropertyChangedadd_PropertyChanged => field ??= INotifyPropertyChanged
        .CreateMemberReference("add_PropertyChanged"u8, MethodSignature.CreateInstance(
            returnType: _corLibTypeFactory.Void,
            parameterTypes: [PropertyChangedEventHandler.ToReferenceTypeSignature()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.ComponentModel.INotifyPropertyChanged.PropertyChanged"/>'s remover.
    /// </summary>
    public MemberReference INotifyPropertyChangedremove_PropertyChanged => field ??= INotifyPropertyChanged
        .CreateMemberReference("remove_PropertyChanged"u8, MethodSignature.CreateInstance(
            returnType: _corLibTypeFactory.Void,
            parameterTypes: [PropertyChangedEventHandler.ToReferenceTypeSignature()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Collections.IEnumerator.Current"/>.
    /// </summary>
    public MemberReference IEnumeratorget_Current => field ??= IEnumerator
        .CreateMemberReference("get_Current"u8, MethodSignature.CreateInstance(_corLibTypeFactory.Object));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Collections.IEnumerator.MoveNext"/>.
    /// </summary>
    public MemberReference IEnumeratorMoveNext => field ??= IEnumerator
        .CreateMemberReference("MoveNext"u8, MethodSignature.CreateInstance(_corLibTypeFactory.Boolean));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Collections.IEnumerator.Reset"/>.
    /// </summary>
    public MemberReference IEnumeratorReset => field ??= IEnumerator
        .CreateMemberReference("Reset"u8, MethodSignature.CreateInstance(_corLibTypeFactory.Void));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IEnumeratorAdapterExtensions.GetMany</c>.
    /// </summary>
    public MemberReference IEnumeratorAdapterOfStringGetMany => field ??= IEnumeratorAdapterExtensions
        .CreateMemberReference("GetMany"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.UInt32,
            parameterTypes: [
                IEnumeratorAdapter1.MakeGenericReferenceType([_corLibTypeFactory.String]),
                _corLibTypeFactory.UInt32,
                _corLibTypeFactory.Void.MakePointerType().MakePointerType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IEnumeratorAdapterExtensions.GetMany</c>.
    /// </summary>
    public MemberReference IEnumeratorAdapterOfExceptionGetMany => field ??= IEnumeratorAdapterExtensions
        .CreateMemberReference("GetMany"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.UInt32,
            parameterTypes: [
                IEnumeratorAdapter1.MakeGenericReferenceType([Exception.ToReferenceTypeSignature()]),
                _corLibTypeFactory.UInt32,
                AbiException.ToValueTypeSignature().MakePointerType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IEnumeratorAdapterExtensions.GetMany</c>.
    /// </summary>
    public MemberReference IEnumeratorAdapterOfTypeGetMany => field ??= IEnumeratorAdapterExtensions
        .CreateMemberReference("GetMany"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.UInt32,
            parameterTypes: [
                IEnumeratorAdapter1.MakeGenericReferenceType([Type.ToReferenceTypeSignature()]),
                _corLibTypeFactory.UInt32,
                AbiType.ToValueTypeSignature().MakePointerType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IEnumeratorAdapterExtensions.GetMany</c>.
    /// </summary>
    public MemberReference IEnumeratorAdapterOfObjectGetMany => field ??= IEnumeratorAdapterExtensions
        .CreateMemberReference("GetMany"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.UInt32,
            parameterTypes: [
                IEnumeratorAdapter1.MakeGenericReferenceType([_corLibTypeFactory.Object]),
                _corLibTypeFactory.UInt32,
                _corLibTypeFactory.Void.MakePointerType().MakePointerType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Collections.IEnumerable.GetEnumerator"/>.
    /// </summary>
    public MemberReference IEnumerableGetEnumerator => field ??= IEnumerable
        .CreateMemberReference("GetEnumerator"u8, MethodSignature.CreateInstance(IEnumerator.ToReferenceTypeSignature()));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="ReadOnlySpan{T}"/>'s constructor (of <see cref="byte"/>).
    /// </summary>
    public MemberReference ReadOnlySpanByte_ctor => field ??= ReadOnlySpanByte.ToTypeDefOrRef().CreateConstructorReference(
        corLibTypeFactory: _corLibTypeFactory,
        parameterTypes: [_corLibTypeFactory.Void.MakePointerType(), _corLibTypeFactory.Int32]);

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="ReadOnlySpan{T}"/>'s constructor (of <see cref="int"/>).
    /// </summary>
    public MemberReference ReadOnlySpanInt32_ctor => field ??= ReadOnlySpanInt32.ToTypeDefOrRef().CreateConstructorReference(
        corLibTypeFactory: _corLibTypeFactory,
        parameterTypes: [_corLibTypeFactory.Void.MakePointerType(), _corLibTypeFactory.Int32]);

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="ReadOnlySpan{T}"/>'s indexer (of <see cref="char"/>).
    /// </summary>
    public MemberReference ReadOnlySpanCharget_Item => field ??= ReadOnlySpanChar
        .ToTypeDefOrRef()
        .CreateMemberReference("get_Item"u8, MethodSignature.CreateInstance(
            returnType:
                new GenericParameterSignature(GenericParameterType.Type, index: 0)
                .MakeByReferenceType()
                .MakeModifierType(InAttribute, isRequired: true),
            parameterTypes: [_corLibTypeFactory.Int32]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="ReadOnlySpan{T}.Length"/> (of <see cref="char"/>).
    /// </summary>
    public MemberReference ReadOnlySpanCharget_Length => field ??= ReadOnlySpanChar
        .ToTypeDefOrRef()
        .CreateMemberReference("get_Length"u8, MethodSignature.CreateInstance(_corLibTypeFactory.Int32));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="ReadOnlySpan{T}"/>'s constructor (of <see cref="ushort"/>).
    /// </summary>
    public MemberReference ReadOnlySpanUInt16_ctor => field ??= ReadOnlySpanUInt16.ToTypeDefOrRef().CreateConstructorReference(
        corLibTypeFactory: _corLibTypeFactory,
        parameterTypes: [_corLibTypeFactory.Void.MakePointerType(), _corLibTypeFactory.Int32]);

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <see cref="System.MemoryExtensions.SequenceEqual{T}(Span{T}, ReadOnlySpan{T})"/> (for <see cref="ReadOnlySpanChar"/>).
    /// </summary>
    public MethodSpecification MemoryExtensionsSequenceEqualChar => field ??= MemoryExtensions
        .CreateMemberReference("SequenceEqual"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.Boolean,
            genericParameterCount: 1,
            parameterTypes: [
                ReadOnlySpan1.MakeGenericValueType([new GenericParameterSignature(GenericParameterType.Method, 0)]),
                ReadOnlySpan1.MakeGenericValueType([new GenericParameterSignature(GenericParameterType.Method, 0)])]))
        .MakeGenericInstanceMethod([_corLibTypeFactory.Char]);

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <see cref="System.MemoryExtensions.AsSpan(string?)"/>.
    /// </summary>
    public MemberReference MemoryExtensionsAsSpanCharString => field ??= MemoryExtensions
        .CreateMemberReference("AsSpan"u8, MethodSignature.CreateStatic(
            returnType: ReadOnlySpanChar,
            parameterTypes: [_corLibTypeFactory.String]));

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <see cref="System.Threading.Interlocked.CompareExchange{T}(ref T, T, T)"/>.
    /// </summary>
    public MemberReference InterlockedCompareExchange1 => field ??= Interlocked
        .CreateMemberReference("CompareExchange"u8, MethodSignature.CreateStatic(
            returnType: new GenericParameterSignature(GenericParameterType.Method, 0),
            genericParameterCount: 1,
            parameterTypes: [
                new GenericParameterSignature(GenericParameterType.Method, 0).MakeByReferenceType(),
                new GenericParameterSignature(GenericParameterType.Method, 0),
                new GenericParameterSignature(GenericParameterType.Method, 0)]));

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <see cref="System.Runtime.InteropServices.MemoryMarshal.CreateSpan"/>.
    /// </summary>
    public MemberReference MemoryMarshalCreateSpan => field ??= MemoryMarshal
        .CreateMemberReference("CreateReadOnlySpan"u8, MethodSignature.CreateStatic(
            returnType: ReadOnlySpan1.MakeGenericValueType([new GenericParameterSignature(GenericParameterType.Method, 0)]),
            genericParameterCount: 1,
            parameterTypes: [
                new GenericParameterSignature(GenericParameterType.Method, 0).MakeByReferenceType(),
                _corLibTypeFactory.Int32]));

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Runtime.CompilerServices.FixedAddressValueTypeAttribute.FixedAddressValueTypeAttribute()"/>.
    /// </summary>
    public MemberReference FixedAddressValueTypeAttribute_ctor => field ??= FixedAddressValueTypeAttribute.CreateConstructorReference(_corLibTypeFactory);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Runtime.InteropServices.DynamicInterfaceCastableImplementationAttribute.DynamicInterfaceCastableImplementationAttribute()"/>.
    /// </summary>
    public MemberReference DynamicInterfaceCastableImplementationAttribute_ctor => field ??= DynamicInterfaceCastableImplementationAttribute.CreateConstructorReference(_corLibTypeFactory);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Runtime.CompilerServices.IsReadOnlyAttribute.IsReadOnlyAttribute()"/>.
    /// </summary>
    public MemberReference IsReadOnlyAttribute_ctor => field ??= IsReadOnlyAttribute.CreateConstructorReference(_corLibTypeFactory);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Runtime.CompilerServices.ScopedRefAttribute.ScopedRefAttribute()"/>.
    /// </summary>
    public MemberReference ScopedRefAttribute_ctor => field ??= ScopedRefAttribute.CreateConstructorReference(_corLibTypeFactory);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Runtime.InteropServices.GuidAttribute.GuidAttribute(string)"/>.
    /// </summary>
    public MemberReference GuidAttribute_ctor => field ??= GuidAttribute.CreateConstructorReference(_corLibTypeFactory, [_corLibTypeFactory.String]);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute.UnmanagedCallersOnlyAttribute()"/>.
    /// </summary>
    public MemberReference UnmanagedCallersOnlyAttribute_ctor => field ??= UnmanagedCallersOnlyAttribute.CreateConstructorReference(_corLibTypeFactory);

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Runtime.CompilerServices.DisableRuntimeMarshallingAttribute.DisableRuntimeMarshallingAttribute()"/>.
    /// </summary>
    public MemberReference DisableRuntimeMarshallingAttribute_ctor => field ??= DisableRuntimeMarshallingAttribute.CreateConstructorReference(_corLibTypeFactory);

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Reflection.AssemblyMetadataAttribute.AssemblyMetadataAttribute(string, string)"/>.
    /// </summary>
    public MemberReference AssemblyMetadataAttribute_ctor => field ??= AssemblyMetadataAttribute.CreateConstructorReference(_corLibTypeFactory, [_corLibTypeFactory.String, _corLibTypeFactory.String]);

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Runtime.InteropServices.ComWrappers.ComInterfaceDispatch.GetInstance"/>.
    /// </summary>
    public MemberReference ComInterfaceDispatchGetInstance => field ??= ComInterfaceDispatch
        .CreateMemberReference("GetInstance"u8, MethodSignature.CreateStatic(
            returnType: new GenericParameterSignature(GenericParameterType.Method, index: 0),
            genericParameterCount: 1,
            parameterTypes: [ComInterfaceDispatch.ToValueTypeSignature().MakePointerType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Runtime.InteropServices.ComWrappers.ComInterfaceEntry.IID"/>.
    /// </summary>
    public MemberReference ComInterfaceEntryIID => field ??= ComInterfaceEntry.CreateMemberReference("IID"u8, new FieldSignature(Guid.ToValueTypeSignature()));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Runtime.InteropServices.ComWrappers.ComInterfaceEntry.Vtable"/>.
    /// </summary>
    public MemberReference ComInterfaceEntryVtable => field ??= ComInterfaceEntry.CreateMemberReference("Vtable"u8, new FieldSignature(_corLibTypeFactory.IntPtr));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Runtime.InteropServices.Marshalling.IIUnknownInterfaceType.Iid"/>.
    /// </summary>
    public MemberReference IIUnknownInterfaceTypeget_Iid => field ??= IIUnknownInterfaceType.CreateMemberReference("get_Iid"u8, MethodSignature.CreateStatic(Guid.ToValueTypeSignature()));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Runtime.InteropServices.Marshalling.IIUnknownInterfaceType.ManagedVirtualMethodTable"/>.
    /// </summary>
    public MemberReference IIUnknownInterfaceTypeget_ManagedVirtualMethodTable => field ??= IIUnknownInterfaceType
        .CreateMemberReference("get_ManagedVirtualMethodTable"u8, MethodSignature.CreateStatic(
            returnType: CorLibTypeFactory.Void.MakePointerType().MakePointerType()));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.get_IID_IUnknown()</c>.
    /// </summary>
    public MemberReference WellKnownInterfaceIIDsget_IID_IUnknown => field ??= WellKnownInterfaceIIDs
        .CreateMemberReference("get_IID_IUnknown"u8, MethodSignature.CreateStatic(
            returnType: WellKnownTypeSignatureFactory.InGuid(this)));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.get_IID_IInspectable()</c>.
    /// </summary>
    public MemberReference WellKnownInterfaceIIDsget_IID_IInspectable => field ??= WellKnownInterfaceIIDs
        .CreateMemberReference("get_IID_IInspectable"u8, MethodSignature.CreateStatic(
            returnType: WellKnownTypeSignatureFactory.InGuid(this)));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.get_IID_IPropertyValue()</c>.
    /// </summary>
    public MemberReference WellKnownInterfaceIIDsget_IID_IPropertyValue => field ??= WellKnownInterfaceIIDs
        .CreateMemberReference("get_IID_IPropertyValue"u8, MethodSignature.CreateStatic(
            returnType: WellKnownTypeSignatureFactory.InGuid(this)));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.get_IID_IStringable()</c>.
    /// </summary>
    public MemberReference WellKnownInterfaceIIDsget_IID_IStringable => field ??= WellKnownInterfaceIIDs
        .CreateMemberReference("get_IID_IStringable"u8, MethodSignature.CreateStatic(
            returnType: WellKnownTypeSignatureFactory.InGuid(this)));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.get_IID_IMarshal()</c>.
    /// </summary>
    public MemberReference WellKnownInterfaceIIDsget_IID_IMarshal => field ??= WellKnownInterfaceIIDs
        .CreateMemberReference("get_IID_IMarshal"u8, MethodSignature.CreateStatic(
            returnType: WellKnownTypeSignatureFactory.InGuid(this)));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.get_IID_IWeakReferenceSource()</c>.
    /// </summary>
    public MemberReference WellKnownInterfaceIIDsget_IID_IWeakReferenceSource => field ??= WellKnownInterfaceIIDs
        .CreateMemberReference("get_IID_IWeakReferenceSource"u8, MethodSignature.CreateStatic(
            returnType: WellKnownTypeSignatureFactory.InGuid(this)));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.get_IID_IAgileObject()</c>.
    /// </summary>
    public MemberReference WellKnownInterfaceIIDsget_IID_IAgileObject => field ??= WellKnownInterfaceIIDs
        .CreateMemberReference("get_IID_IAgileObject"u8, MethodSignature.CreateStatic(
            returnType: WellKnownTypeSignatureFactory.InGuid(this)));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IUnknownImpl.get_Vtable()</c>.
    /// </summary>
    public MemberReference IUnknownImplget_Vtable => field ??= IUnknownImpl
        .CreateMemberReference("get_Vtable"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.IntPtr));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IInspectableImpl.get_Vtable()</c>.
    /// </summary>
    public MemberReference IInspectableImplget_Vtable => field ??= IInspectableImpl
        .CreateMemberReference("get_Vtable"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.IntPtr));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IPropertyValueImpl.get_OtherTypeVtable()</c>.
    /// </summary>
    public MemberReference IPropertyValueImplget_OtherTypeVtable => field ??= IPropertyValueImpl
        .CreateMemberReference("get_OtherTypeVtable"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.IntPtr));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IStringableImpl.get_Vtable()</c>.
    /// </summary>
    public MemberReference IStringableImplget_Vtable => field ??= IStringableImpl
        .CreateMemberReference("get_Vtable"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.IntPtr));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IMarshalImpl.get_Vtable()</c>.
    /// </summary>
    public MemberReference IMarshalImplget_Vtable => field ??= IMarshalImpl
        .CreateMemberReference("get_Vtable"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.IntPtr));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IWeakReferenceSourceImpl.get_Vtable()</c>.
    /// </summary>
    public MemberReference IWeakReferenceSourceImplget_Vtable => field ??= IWeakReferenceSourceImpl
        .CreateMemberReference("get_Vtable"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.IntPtr));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IAgileObjectImpl.get_Vtable()</c>.
    /// </summary>
    public MemberReference IAgileObjectImplget_Vtable => field ??= IAgileObjectImpl
        .CreateMemberReference("get_Vtable"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.IntPtr));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeObjectReference.AsValue()</c>.
    /// </summary>
    public MemberReference WindowsRuntimeObjectReferenceAsValue => field ??= WindowsRuntimeObjectReference
        .CreateMemberReference("AsValue"u8, MethodSignature.CreateInstance(WindowsRuntimeObjectReferenceValue.ToValueTypeSignature()));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IAsyncActionWithProgress.GetResults()</c>.
    /// </summary>
    public MemberReference IAsyncActionWithProgressGetResults => field ??= IAsyncActionWithProgressMethods
        .CreateMemberReference("GetResults"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.Void,
            parameterTypes: [WindowsRuntimeObjectReference.ToReferenceTypeSignature()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IIteratorMethods.HasCurrent</c>.
    /// </summary>
    public MemberReference IIteratorMethodsHasCurrent => field ??= IIteratorMethods
        .CreateMemberReference("HasCurrent"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.Boolean,
            parameterTypes: [WindowsRuntimeObjectReference.ToReferenceTypeSignature()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IIteratorMethods.MoveNext</c>.
    /// </summary>
    public MemberReference IIteratorMethodsMoveNext => field ??= IIteratorMethods
        .CreateMemberReference("MoveNext"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.Boolean,
            parameterTypes: [WindowsRuntimeObjectReference.ToReferenceTypeSignature()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IListMethods.Count</c>.
    /// </summary>
    public MemberReference IListMethodsCount => field ??= IListMethods
        .CreateMemberReference("Count"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.Int32,
            parameterTypes: [WindowsRuntimeObjectReference.ToReferenceTypeSignature()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IListMethods.Clear</c>.
    /// </summary>
    public MemberReference IListMethodsClear => field ??= IListMethods
        .CreateMemberReference("Clear"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.Void,
            parameterTypes: [WindowsRuntimeObjectReference.ToReferenceTypeSignature()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IListMethods.RemoveAt</c>.
    /// </summary>
    public MemberReference IListMethodsRemoveAt => field ??= IListMethods
        .CreateMemberReference("RemoveAt"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.Void,
            parameterTypes: [
                WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                _corLibTypeFactory.Int32]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IListAdapter&lt;string&gt;.IndexOf</c>.
    /// </summary>
    public MemberReference IListAdapterOfStringIndexOf => field ??= IListAdapterExtensions
        .CreateMemberReference("IndexOf"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.Boolean,
            parameterTypes: [
                IList1.MakeGenericReferenceType([_corLibTypeFactory.String]),
                ReadOnlySpanChar,
                _corLibTypeFactory.UInt32.MakeByReferenceType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IListAdapterExtensions.GetMany</c>.
    /// </summary>
    public MemberReference IListAdapterOfStringGetMany => field ??= IListAdapterExtensions
        .CreateMemberReference("GetMany"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.UInt32,
            parameterTypes: [
                IList1.MakeGenericReferenceType([_corLibTypeFactory.String]),
                _corLibTypeFactory.UInt32,
                _corLibTypeFactory.UInt32,
                _corLibTypeFactory.Void.MakePointerType().MakePointerType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IListAdapterExtensions.GetMany</c>.
    /// </summary>
    public MemberReference IListAdapterOfExceptionGetMany => field ??= IListAdapterExtensions
        .CreateMemberReference("GetMany"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.UInt32,
            parameterTypes: [
                IList1.MakeGenericReferenceType([Exception.ToReferenceTypeSignature()]),
                _corLibTypeFactory.UInt32,
                _corLibTypeFactory.UInt32,
                AbiException.ToValueTypeSignature().MakePointerType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IListAdapterExtensions.GetMany</c>.
    /// </summary>
    public MemberReference IListAdapterOfTypeGetMany => field ??= IListAdapterExtensions
        .CreateMemberReference("GetMany"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.UInt32,
            parameterTypes: [
                IList1.MakeGenericReferenceType([Type.ToReferenceTypeSignature()]),
                _corLibTypeFactory.UInt32,
                _corLibTypeFactory.UInt32,
                AbiType.ToValueTypeSignature().MakePointerType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IListAdapterExtensions.GetMany</c>.
    /// </summary>
    public MemberReference IListAdapterOfObjectGetMany => field ??= IListAdapterExtensions
        .CreateMemberReference("GetMany"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.UInt32,
            parameterTypes: [
                IList1.MakeGenericReferenceType([_corLibTypeFactory.Object]),
                _corLibTypeFactory.UInt32,
                _corLibTypeFactory.UInt32,
                _corLibTypeFactory.Void.MakePointerType().MakePointerType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IReadOnlyListMethods.Count</c>.
    /// </summary>
    public MemberReference IReadOnlyListMethodsCount => field ??= IReadOnlyListMethods
        .CreateMemberReference("Count"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.Int32,
            parameterTypes: [WindowsRuntimeObjectReference.ToReferenceTypeSignature()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IReadOnlyListAdapter&lt;string&gt;.IndexOf</c>.
    /// </summary>
    public MemberReference IReadOnlyListAdapterOfStringIndexOf => field ??= IReadOnlyListAdapterExtensions
        .CreateMemberReference("IndexOf"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.Boolean,
            parameterTypes: [
                IReadOnlyList1.MakeGenericReferenceType([_corLibTypeFactory.String]),
                ReadOnlySpanChar,
                _corLibTypeFactory.UInt32.MakeByReferenceType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IReadOnlyListAdapterExtensions.GetMany</c>.
    /// </summary>
    public MemberReference IReadOnlyListAdapterOfStringGetMany => field ??= IReadOnlyListAdapterExtensions
        .CreateMemberReference("GetMany"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.UInt32,
            parameterTypes: [
                IReadOnlyList1.MakeGenericReferenceType([_corLibTypeFactory.String]),
                _corLibTypeFactory.UInt32,
                _corLibTypeFactory.UInt32,
                _corLibTypeFactory.Void.MakePointerType().MakePointerType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IReadOnlyListAdapterExtensions.GetMany</c>.
    /// </summary>
    public MemberReference IReadOnlyListAdapterOfExceptionGetMany => field ??= IReadOnlyListAdapterExtensions
        .CreateMemberReference("GetMany"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.UInt32,
            parameterTypes: [
                IReadOnlyList1.MakeGenericReferenceType([Exception.ToReferenceTypeSignature()]),
                _corLibTypeFactory.UInt32,
                _corLibTypeFactory.UInt32,
                AbiException.ToValueTypeSignature().MakePointerType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IReadOnlyListAdapterExtensions.GetMany</c>.
    /// </summary>
    public MemberReference IReadOnlyListAdapterOfTypeGetMany => field ??= IReadOnlyListAdapterExtensions
        .CreateMemberReference("GetMany"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.UInt32,
            parameterTypes: [
                IReadOnlyList1.MakeGenericReferenceType([Type.ToReferenceTypeSignature()]),
                _corLibTypeFactory.UInt32,
                _corLibTypeFactory.UInt32,
                AbiType.ToValueTypeSignature().MakePointerType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IReadOnlyListAdapterExtensions.GetMany</c>.
    /// </summary>
    public MemberReference IReadOnlyListAdapterOfObjectGetMany => field ??= IReadOnlyListAdapterExtensions
        .CreateMemberReference("GetMany"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.UInt32,
            parameterTypes: [
                IReadOnlyList1.MakeGenericReferenceType([_corLibTypeFactory.Object]),
                _corLibTypeFactory.UInt32,
                _corLibTypeFactory.UInt32,
                _corLibTypeFactory.Void.MakePointerType().MakePointerType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IDictionary.Count</c>.
    /// </summary>
    public MemberReference IDictionaryMethodsCount => field ??= IDictionaryMethods
        .CreateMemberReference("Count"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.Int32,
            parameterTypes: [WindowsRuntimeObjectReference.ToReferenceTypeSignature()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IDictionary.Clear</c>.
    /// </summary>
    public MemberReference IDictionaryMethodsClear => field ??= IDictionaryMethods
        .CreateMemberReference("Clear"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.Void,
            parameterTypes: [WindowsRuntimeObjectReference.ToReferenceTypeSignature()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IReadOnlyDictionary.Count</c>.
    /// </summary>
    public MemberReference IReadOnlyDictionaryMethodsCount => field ??= IReadOnlyDictionaryMethods
        .CreateMemberReference("Count"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.Int32,
            parameterTypes: [WindowsRuntimeObjectReference.ToReferenceTypeSignature()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IMapChangedEventArgsMethods.CollectionChange</c>.
    /// </summary>
    public MemberReference IMapChangedEventArgsMethodsCollectionChange => field ??= IMapChangedEventArgsMethods
        .CreateMemberReference("CollectionChange"u8, MethodSignature.CreateStatic(
            returnType: CollectionChange.ToValueTypeSignature(),
            parameterTypes: [WindowsRuntimeObjectReference.ToReferenceTypeSignature()]));

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="WindowsRuntimeObject"/>'s <c>get_NativeObjectReference</c> method.
    /// </summary>
    public MemberReference WindowsRuntimeObjectget_NativeObjectReference => field ??= WindowsRuntimeObject
        .CreateMemberReference("get_NativeObjectReference"u8, MethodSignature.CreateInstance(
            returnType: WindowsRuntimeObjectReference.ToReferenceTypeSignature()));

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="WindowsRuntimeObject"/>'s <c>GetObjectReferenceForInterface</c> method.
    /// </summary>
    public MemberReference WindowsRuntimeObjectGetObjectReferenceForInterface => field ??= WindowsRuntimeObject
        .CreateMemberReference("GetObjectReferenceForInterface"u8, MethodSignature.CreateInstance(
            returnType: WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
            parameterTypes: [RuntimeTypeHandle.ToValueTypeSignature()]));

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="WindowsRuntimeObject"/>'s <c>TryGetObjectReferenceForInterface</c> method.
    /// </summary>
    public MemberReference WindowsRuntimeObjectTryGetObjectReferenceForInterface => field ??= WindowsRuntimeObject
        .CreateMemberReference("TryGetObjectReferenceForInterface"u8, MethodSignature.CreateInstance(
            returnType: _corLibTypeFactory.Boolean,
            parameterTypes: [
                RuntimeTypeHandle.ToValueTypeSignature(),
                WindowsRuntimeObjectReference.ToReferenceTypeSignature().MakeByReferenceType()]));

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <c>WindowsRuntime.InteropServices.IWindowsRuntimeInterface.get_IID()</c>.
    /// </summary>
    public MemberReference IWindowsRuntimeInterfaceget_IID => field ??= IWindowsRuntimeInterface
        .CreateMemberReference("get_IID"u8, MethodSignature.CreateStatic(
            returnType: WellKnownTypeSignatureFactory.InGuid(this)));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IWindowsRuntimeObjectComWrappersCallback.CreateObject</c>.
    /// </summary>
    public MemberReference IWindowsRuntimeObjectComWrappersCallbackCreateObject => field ??= IWindowsRuntimeObjectComWrappersCallback
        .CreateMemberReference("CreateObject"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.Object,
            parameterTypes: [
                _corLibTypeFactory.Void.MakePointerType(),
                CreatedWrapperFlags.ToValueTypeSignature().MakeByReferenceType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IWindowsRuntimeUnsealedObjectComWrappersCallback.TryCreateObject</c>.
    /// </summary>
    public MemberReference IWindowsRuntimeUnsealedObjectComWrappersCallbackTryCreateObject => field ??= IWindowsRuntimeUnsealedObjectComWrappersCallback
        .CreateMemberReference("TryCreateObject"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.Boolean,
            parameterTypes: [
                _corLibTypeFactory.Void.MakePointerType(),
                ReadOnlySpanChar,
                _corLibTypeFactory.Object.MakeByReferenceType(),
                CreatedWrapperFlags.ToValueTypeSignature().MakeByReferenceType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IWindowsRuntimeUnsealedObjectComWrappersCallback.CreateObject</c>.
    /// </summary>
    public MemberReference IWindowsRuntimeUnsealedObjectComWrappersCallbackCreateObject => field ??= IWindowsRuntimeUnsealedObjectComWrappersCallback
        .CreateMemberReference("CreateObject"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.Object,
            parameterTypes: [
                _corLibTypeFactory.Void.MakePointerType(),
                CreatedWrapperFlags.ToValueTypeSignature().MakeByReferenceType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IWindowsRuntimeArrayComWrappersCallback.CreateArray</c>.
    /// </summary>
    public MemberReference IWindowsRuntimeArrayComWrappersCallbackCreateArray => field ??= IWindowsRuntimeArrayComWrappersCallback
        .CreateMemberReference("CreateArray"u8, MethodSignature.CreateStatic(
            returnType: Array.ToReferenceTypeSignature(),
            parameterTypes: [
                _corLibTypeFactory.UInt32,
                _corLibTypeFactory.Void.MakePointerType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeObjectReferenceValue.GetThisPtrUnsafe()</c>.
    /// </summary>
    public MemberReference WindowsRuntimeObjectReferenceValueGetThisPtrUnsafe => field ??= WindowsRuntimeObjectReferenceValue
        .CreateMemberReference("GetThisPtrUnsafe"u8, MethodSignature.CreateInstance(_corLibTypeFactory.Void.MakePointerType()));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeObjectReferenceValue.DetachThisPtrUnsafe()</c>.
    /// </summary>
    public MemberReference WindowsRuntimeObjectReferenceValueDetachThisPtrUnsafe => field ??= WindowsRuntimeObjectReferenceValue
        .CreateMemberReference("DetachThisPtrUnsafe"u8, MethodSignature.CreateInstance(_corLibTypeFactory.Void.MakePointerType()));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeObjectReferenceValue.Dispose()</c>.
    /// </summary>
    public MemberReference WindowsRuntimeObjectReferenceValueDispose => field ??= WindowsRuntimeObjectReferenceValue
        .CreateMemberReference("Dispose"u8, MethodSignature.CreateInstance(_corLibTypeFactory.Void));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.DynamicInterfaceCastableForwarderAttribute.ctor()</c>.
    /// </summary>
    public MemberReference DynamicInterfaceCastableForwarderAttribute_ctor => field ??= DynamicInterfaceCastableForwarderAttribute.CreateConstructorReference(_corLibTypeFactory);

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeComWrappersMarshallerAttribute.ctor()</c>.
    /// </summary>
    public MemberReference WindowsRuntimeComWrappersMarshallerAttribute_ctor => field ??= WindowsRuntimeComWrappersMarshallerAttribute.CreateConstructorReference(_corLibTypeFactory);

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeComWrappersMarshallerAttribute.GetOrCreateComInterfaceForObject(object)</c>.
    /// </summary>
    public MemberReference WindowsRuntimeComWrappersMarshallerAttributeGetOrCreateComInterfaceForObject => field ??= WindowsRuntimeComWrappersMarshallerAttribute
        .CreateMemberReference("GetOrCreateComInterfaceForObject"u8, MethodSignature.CreateInstance(
            returnType: _corLibTypeFactory.Void.MakePointerType(),
            parameterTypes: [_corLibTypeFactory.Object]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeComWrappersMarshallerAttribute.ComputeVtables(out int)</c>.
    /// </summary>
    public MemberReference WindowsRuntimeComWrappersMarshallerAttributeComputeVtables => field ??= WindowsRuntimeComWrappersMarshallerAttribute
        .CreateMemberReference("ComputeVtables"u8, MethodSignature.CreateInstance(
            returnType: ComInterfaceEntry.ToValueTypeSignature().MakePointerType(),
            parameterTypes: [_corLibTypeFactory.Int32.MakeByReferenceType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeComWrappersMarshallerAttribute.CreateObject(void*)</c>.
    /// </summary>
    public MemberReference WindowsRuntimeComWrappersMarshallerAttributeCreateObject => field ??= WindowsRuntimeComWrappersMarshallerAttribute
        .CreateMemberReference("CreateObject"u8, MethodSignature.CreateInstance(
            returnType: _corLibTypeFactory.Object,
            parameterTypes: [
                _corLibTypeFactory.Void.MakePointerType(),
                CreatedWrapperFlags.ToValueTypeSignature().MakeByReferenceType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeComWrappersMarshal.GetOrCreateComInterfaceForObject</c>.
    /// </summary>
    public MemberReference WindowsRuntimeComWrappersMarshalGetOrCreateComInterfaceForObject => field ??= WindowsRuntimeComWrappersMarshal
        .CreateMemberReference("GetOrCreateComInterfaceForObject"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.Void.MakePointerType(),
            parameterTypes: [
                _corLibTypeFactory.Object,
                CreateComInterfaceFlags.ToValueTypeSignature()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeComWrappersMarshal.CreateObjectReference</c>.
    /// </summary>
    public MemberReference WindowsRuntimeComWrappersMarshalCreateObjectReference => field ??= WindowsRuntimeComWrappersMarshal
        .CreateMemberReference("CreateObjectReference"u8, MethodSignature.CreateStatic(
            returnType: WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
            parameterTypes: [
                _corLibTypeFactory.Void.MakePointerType(),
                Guid.ToValueTypeSignature().MakeByReferenceType(),
                CreatedWrapperFlags.ToValueTypeSignature().MakeByReferenceType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeComWrappersMarshal.CreateObjectReferenceUnsafe</c>.
    /// </summary>
    public MemberReference WindowsRuntimeComWrappersMarshalCreateObjectReferenceUnsafe => field ??= WindowsRuntimeComWrappersMarshal
        .CreateMemberReference("CreateObjectReferenceUnsafe"u8, MethodSignature.CreateStatic(
            returnType: WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
            parameterTypes: [
                _corLibTypeFactory.Void.MakePointerType(),
                Guid.ToValueTypeSignature().MakeByReferenceType(),
                CreatedWrapperFlags.ToValueTypeSignature().MakeByReferenceType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeComWrappersMarshal.CreateObjectReferenceValue</c>.
    /// </summary>
    public MemberReference WindowsRuntimeComWrappersMarshalCreateObjectReferenceValue => field ??= WindowsRuntimeComWrappersMarshal
        .CreateMemberReference("CreateObjectReferenceValue"u8, MethodSignature.CreateStatic(
            returnType: WindowsRuntimeObjectReferenceValue.ToValueTypeSignature(),
            parameterTypes: [
                _corLibTypeFactory.Void.MakePointerType(),
                Guid.ToValueTypeSignature().MakeByReferenceType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeUnknownMarshaller.Free(void*)</c>.
    /// </summary>
    public MemberReference WindowsRuntimeUnknownMarshallerFree => field ??= WindowsRuntimeUnknownMarshaller
        .CreateMemberReference("Free"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.Void,
            parameterTypes: [_corLibTypeFactory.Void.MakePointerType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeObjectMarshaller.ConvertToUnmanaged(object)</c>.
    /// </summary>
    public MemberReference WindowsRuntimeObjectMarshallerConvertToUnmanaged => field ??= WindowsRuntimeObjectMarshaller
        .CreateMemberReference("ConvertToUnmanaged"u8, MethodSignature.CreateStatic(
            returnType: WindowsRuntimeObjectReferenceValue.ToValueTypeSignature(),
            parameterTypes: [_corLibTypeFactory.Object]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>ABI.System.TypeMarshaller.ConvertToManaged</c>.
    /// </summary>
    public MemberReference TypeMarshallerConvertToManaged => field ??= TypeMarshaller
        .CreateMemberReference("ConvertToManaged"u8, MethodSignature.CreateStatic(
            returnType: Type.ToReferenceTypeSignature(),
            parameterTypes: [AbiType.ToValueTypeSignature()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>ABI.System.TypeMarshaller.ConvertToUnmanaged</c>.
    /// </summary>
    public MemberReference TypeMarshallerConvertToUnmanaged => field ??= TypeMarshaller
        .CreateMemberReference("ConvertToUnmanaged"u8, MethodSignature.CreateStatic(
            returnType: AbiType.ToValueTypeSignature(),
            parameterTypes: [Type.ToReferenceTypeSignature()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>ABI.System.TypeMarshaller.ConvertToUnmanagedUnsafe</c>.
    /// </summary>
    public MemberReference TypeMarshallerConvertToUnmanagedUnsafe => field ??= TypeMarshaller
        .CreateMemberReference("ConvertToUnmanagedUnsafe"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.Void,
            parameterTypes: [
                Type.ToReferenceTypeSignature(),
                TypeReference.MakeByReferenceType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>ABI.System.TypeMarshaller.Dispose</c>.
    /// </summary>
    public MemberReference TypeMarshallerDispose => field ??= TypeMarshaller
        .CreateMemberReference("Dispose"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.Void,
            parameterTypes: [AbiType.ToValueTypeSignature()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>ABI.System.ExceptionMarshaller.ConvertToManaged</c>.
    /// </summary>
    public MemberReference ExceptionMarshallerConvertToManaged => field ??= ExceptionMarshaller
        .CreateMemberReference("ConvertToManaged"u8, MethodSignature.CreateStatic(
            returnType: Exception.ToReferenceTypeSignature(),
            parameterTypes: [AbiException.ToValueTypeSignature()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>ABI.System.ExceptionMarshaller.ConvertToUnmanaged</c>.
    /// </summary>
    public MemberReference ExceptionMarshallerConvertToUnmanaged => field ??= ExceptionMarshaller
        .CreateMemberReference("ConvertToUnmanaged"u8, MethodSignature.CreateStatic(
            returnType: AbiException.ToValueTypeSignature(),
            parameterTypes: [Exception.ToReferenceTypeSignature()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="WindowsRuntimeClassNameAttribute"/>'s constructor.
    /// </summary>
    public MemberReference WindowsRuntimeClassNameAttribute_ctor => field ??= WindowsRuntimeClassNameAttribute
        .CreateMemberReference(".ctor", MethodSignature.CreateInstance(
            returnType: _corLibTypeFactory.Void,
            parameterTypes: [_corLibTypeFactory.String]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="WindowsRuntimeMetadataTypeNameAttribute"/>'s constructor.
    /// </summary>
    public MemberReference WindowsRuntimeMetadataTypeNameAttribute_ctor => field ??= WindowsRuntimeMetadataTypeNameAttribute
        .CreateMemberReference(".ctor", MethodSignature.CreateInstance(
            returnType: _corLibTypeFactory.Void,
            parameterTypes: [_corLibTypeFactory.String]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="WindowsRuntimeMappedMetadataAttribute"/>'s constructor.
    /// </summary>
    public MemberReference WindowsRuntimeMappedMetadataAttribute_ctor => field ??= WindowsRuntimeMappedMetadataAttribute
        .CreateMemberReference(".ctor", MethodSignature.CreateInstance(
            returnType: _corLibTypeFactory.Void,
            parameterTypes: [_corLibTypeFactory.String]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="WindowsRuntimeMappedTypeAttribute"/>'s constructor.
    /// </summary>
    public MemberReference WindowsRuntimeMappedTypeAttribute_ctor => field ??= WindowsRuntimeMappedTypeAttribute
        .CreateMemberReference(".ctor", MethodSignature.CreateInstance(
            returnType: _corLibTypeFactory.Void,
            parameterTypes: [Type.ToReferenceTypeSignature()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="WindowsRuntimeReferenceTypeAttribute"/>'s constructor.
    /// </summary>
    public MemberReference WindowsRuntimeReferenceTypeAttribute_ctor => field ??= WindowsRuntimeReferenceTypeAttribute
        .CreateMemberReference(".ctor", MethodSignature.CreateInstance(
            returnType: _corLibTypeFactory.Void,
            parameterTypes: [Type.ToReferenceTypeSignature()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeObjectMarshaller.ConvertToManaged(void*)</c>.
    /// </summary>
    public MemberReference WindowsRuntimeObjectMarshallerConvertToManaged => field ??= WindowsRuntimeObjectMarshaller
        .CreateMemberReference("ConvertToManaged"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.Object,
            parameterTypes: [_corLibTypeFactory.Void.MakePointerType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeUnsealedObjectMarshaller&lt;TCallback&gt;.ConvertToManaged</c>.
    /// </summary>
    public MemberReference WindowsRuntimeUnsealedObjectMarshallerConvertToManaged => field ??= WindowsRuntimeUnsealedObjectMarshaller
        .CreateMemberReference("ConvertToManaged"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.Object,
            genericParameterCount: 1,
            parameterTypes: [_corLibTypeFactory.Void.MakePointerType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeDelegateMarshaller.ConvertToUnmanaged(Delegate, in Guid)</c>.
    /// </summary>
    public MemberReference WindowsRuntimeDelegateMarshallerConvertToUnmanaged => field ??= WindowsRuntimeDelegateMarshaller
        .CreateMemberReference("ConvertToUnmanaged"u8, MethodSignature.CreateStatic(
            returnType: WindowsRuntimeObjectReferenceValue.ToValueTypeSignature(),
            parameterTypes: [
                Delegate.ToReferenceTypeSignature(),
                Guid.ToValueTypeSignature().MakeByReferenceType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeDelegateMarshaller.ConvertToManaged&lt;TCallback&gt;(void*)</c>.
    /// </summary>
    public MemberReference WindowsRuntimeDelegateMarshallerConvertToManaged => field ??= WindowsRuntimeDelegateMarshaller
        .CreateMemberReference("ConvertToManaged"u8, MethodSignature.CreateStatic(
            returnType: Delegate.ToReferenceTypeSignature(),
            genericParameterCount: 1,
            parameterTypes: [_corLibTypeFactory.Void.MakePointerType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeDelegateMarshaller.BoxToUnmanaged(Delegate, in Guid)</c>.
    /// </summary>
    public MemberReference WindowsRuntimeDelegateMarshallerBoxToUnmanaged => field ??= WindowsRuntimeDelegateMarshaller
        .CreateMemberReference("BoxToUnmanaged"u8, MethodSignature.CreateStatic(
            returnType: WindowsRuntimeObjectReferenceValue.ToValueTypeSignature(),
            parameterTypes: [
                Delegate.ToReferenceTypeSignature(),
                Guid.ToValueTypeSignature().MakeByReferenceType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeDelegateMarshaller.UnboxToManaged&lt;TCallback&gt;(void*)</c>.
    /// </summary>
    public MemberReference WindowsRuntimeDelegateMarshallerUnboxToManaged => field ??= WindowsRuntimeDelegateMarshaller
        .CreateMemberReference("UnboxToManaged"u8, MethodSignature.CreateStatic(
            returnType: Delegate.ToReferenceTypeSignature(),
            genericParameterCount: 1,
            parameterTypes: [_corLibTypeFactory.Void.MakePointerType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeDelegateMarshaller.UnboxToManaged&lt;TCallback&gt;(void*, in Guid)</c>.
    /// </summary>
    public MemberReference WindowsRuntimeDelegateMarshallerUnboxToManaged2 => field ??= WindowsRuntimeDelegateMarshaller
        .CreateMemberReference("UnboxToManaged"u8, MethodSignature.CreateStatic(
            returnType: Delegate.ToReferenceTypeSignature(),
            genericParameterCount: 1,
            parameterTypes: [
                _corLibTypeFactory.Void.MakePointerType(),
                Guid.ToValueTypeSignature().MakeByReferenceType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeArrayMarshaller.UnboxToManaged&lt;TCallback&gt;(void*, in Guid)</c>.
    /// </summary>
    public MemberReference WindowsRuntimeArrayMarshallerUnboxToManaged => field ??= WindowsRuntimeArrayMarshaller
        .CreateMemberReference("UnboxToManaged"u8, MethodSignature.CreateStatic(
            returnType: Array.ToReferenceTypeSignature(),
            genericParameterCount: 1,
            parameterTypes: [
                _corLibTypeFactory.Void.MakePointerType(),
                Guid.ToValueTypeSignature().MakeByReferenceType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.WindowsRuntimeBlittableValueTypeArrayMarshaller.Free</c>.
    /// </summary>
    public MemberReference WindowsRuntimeBlittableValueTypeArrayMarshallerFree => field ??= WindowsRuntimeBlittableValueTypeArrayMarshaller
        .CreateMemberReference("Free"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.Void,
            parameterTypes: [
                _corLibTypeFactory.UInt32,
                _corLibTypeFactory.Void.MakePointerType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.WindowsRuntimeUnknownArrayMarshaller.Dispose</c>.
    /// </summary>
    public MemberReference WindowsRuntimeUnknownArrayMarshallerDispose => field ??= WindowsRuntimeUnknownArrayMarshaller
        .CreateMemberReference("Dispose"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.Void,
            parameterTypes: [
                _corLibTypeFactory.UInt32,
                _corLibTypeFactory.Void.MakePointerType().MakePointerType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.WindowsRuntimeUnknownArrayMarshaller.Free</c>.
    /// </summary>
    public MemberReference WindowsRuntimeUnknownArrayMarshallerFree => field ??= WindowsRuntimeUnknownArrayMarshaller
        .CreateMemberReference("Free"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.Void,
            parameterTypes: [
                _corLibTypeFactory.UInt32,
                _corLibTypeFactory.Void.MakePointerType().MakePointerType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.WindowsRuntimeObjectArrayMarshaller.ConvertToUnmanaged</c>.
    /// </summary>
    public MemberReference WindowsRuntimeObjectArrayMarshallerConvertToUnmanaged => field ??= WindowsRuntimeObjectArrayMarshaller
        .CreateMemberReference("ConvertToUnmanaged"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.Void,
            parameterTypes: [
                ReadOnlySpan1.MakeGenericValueType([_corLibTypeFactory.Object]),
                _corLibTypeFactory.UInt32.MakeByReferenceType(),
                _corLibTypeFactory.Void.MakePointerType().MakePointerType().MakeByReferenceType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.WindowsRuntimeObjectArrayMarshaller.ConvertToManaged</c>.
    /// </summary>
    public MemberReference WindowsRuntimeObjectArrayMarshallerConvertToManaged => field ??= WindowsRuntimeObjectArrayMarshaller
        .CreateMemberReference("ConvertToManaged"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.Object.MakeSzArrayType(),
            parameterTypes: [
                _corLibTypeFactory.UInt32,
                _corLibTypeFactory.Void.MakePointerType().MakePointerType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.WindowsRuntimeObjectArrayMarshaller.CopyToUnmanaged</c>.
    /// </summary>
    public MemberReference WindowsRuntimeObjectArrayMarshallerCopyToUnmanaged => field ??= WindowsRuntimeObjectArrayMarshaller
        .CreateMemberReference("CopyToUnmanaged"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.Void,
            parameterTypes: [
                ReadOnlySpan1.MakeGenericValueType([_corLibTypeFactory.Object]),
                _corLibTypeFactory.UInt32,
                _corLibTypeFactory.Void.MakePointerType().MakePointerType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.WindowsRuntimeObjectArrayMarshaller.CopyToManaged</c>.
    /// </summary>
    public MemberReference WindowsRuntimeObjectArrayMarshallerCopyToManaged => field ??= WindowsRuntimeObjectArrayMarshaller
        .CreateMemberReference("CopyToManaged"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.Void,
            parameterTypes: [
                _corLibTypeFactory.UInt32,
                _corLibTypeFactory.Void.MakePointerType().MakePointerType(),
                Span1.MakeGenericValueType([_corLibTypeFactory.Object])]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.TypeArrayMarshaller.ConvertToUnmanaged</c>.
    /// </summary>
    public MemberReference TypeArrayMarshallerConvertToUnmanaged => field ??= TypeArrayMarshaller
        .CreateMemberReference("ConvertToUnmanaged"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.Void,
            parameterTypes: [
                ReadOnlySpan1.MakeGenericValueType([Type.ToTypeSignature()]),
                _corLibTypeFactory.UInt32.MakeByReferenceType(),
                AbiType.ToValueTypeSignature().MakePointerType().MakeByReferenceType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.TypeArrayMarshaller.ConvertToManaged</c>.
    /// </summary>
    public MemberReference TypeArrayMarshallerConvertToManaged => field ??= TypeArrayMarshaller
        .CreateMemberReference("ConvertToManaged"u8, MethodSignature.CreateStatic(
            returnType: Type.ToReferenceTypeSignature().MakeSzArrayType(),
            parameterTypes: [
                _corLibTypeFactory.UInt32,
                AbiType.ToValueTypeSignature().MakePointerType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.TypeArrayMarshaller.CopyToUnmanaged</c>.
    /// </summary>
    public MemberReference TypeArrayMarshallerCopyToUnmanaged => field ??= TypeArrayMarshaller
        .CreateMemberReference("CopyToUnmanaged"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.Void,
            parameterTypes: [
                ReadOnlySpan1.MakeGenericValueType([Type.ToTypeSignature()]),
                _corLibTypeFactory.UInt32,
                AbiType.ToValueTypeSignature().MakePointerType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.TypeArrayMarshaller.CopyToManaged</c>.
    /// </summary>
    public MemberReference TypeArrayMarshallerCopyToManaged => field ??= TypeArrayMarshaller
        .CreateMemberReference("CopyToManaged"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.Void,
            parameterTypes: [
                _corLibTypeFactory.UInt32,
                AbiType.ToValueTypeSignature().MakePointerType(),
                Span1.MakeGenericValueType([Type.ToTypeSignature()])]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.TypeArrayMarshaller.Dispose</c>.
    /// </summary>
    public MemberReference TypeArrayMarshallerDispose => field ??= TypeArrayMarshaller
        .CreateMemberReference("Dispose"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.Void,
            parameterTypes: [
                _corLibTypeFactory.UInt32,
                AbiType.ToValueTypeSignature().MakePointerType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.TypeArrayMarshaller.Free</c>.
    /// </summary>
    public MemberReference TypeArrayMarshallerFree => field ??= TypeArrayMarshaller
        .CreateMemberReference("Free"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.Void,
            parameterTypes: [
                _corLibTypeFactory.UInt32,
                AbiType.ToValueTypeSignature().MakePointerType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.HStringArrayMarshaller.ConvertToUnmanaged</c>.
    /// </summary>
    public MemberReference HStringArrayMarshallerConvertToUnmanaged => field ??= HStringArrayMarshaller
        .CreateMemberReference("ConvertToUnmanaged"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.Void,
            parameterTypes: [
                ReadOnlySpan1.MakeGenericValueType([_corLibTypeFactory.String]),
                _corLibTypeFactory.UInt32.MakeByReferenceType(),
                _corLibTypeFactory.Void.MakePointerType().MakePointerType().MakeByReferenceType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.HStringArrayMarshaller.ConvertToManaged</c>.
    /// </summary>
    public MemberReference HStringArrayMarshallerConvertToManaged => field ??= HStringArrayMarshaller
        .CreateMemberReference("ConvertToManaged"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.String.MakeSzArrayType(),
            parameterTypes: [
                _corLibTypeFactory.UInt32,
                _corLibTypeFactory.Void.MakePointerType().MakePointerType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.HStringArrayMarshaller.CopyToUnmanaged</c>.
    /// </summary>
    public MemberReference HStringArrayMarshallerCopyToUnmanaged => field ??= HStringArrayMarshaller
        .CreateMemberReference("CopyToUnmanaged"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.Void,
            parameterTypes: [
                ReadOnlySpan1.MakeGenericValueType([_corLibTypeFactory.String]),
                _corLibTypeFactory.UInt32,
                _corLibTypeFactory.Void.MakePointerType().MakePointerType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.HStringArrayMarshaller.CopyToManaged</c>.
    /// </summary>
    public MemberReference HStringArrayMarshallerCopyToManaged => field ??= HStringArrayMarshaller
        .CreateMemberReference("CopyToManaged"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.Void,
            parameterTypes: [
                _corLibTypeFactory.UInt32,
                _corLibTypeFactory.Void.MakePointerType().MakePointerType(),
                Span1.MakeGenericValueType([_corLibTypeFactory.String])]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.HStringArrayMarshaller.Dispose</c>.
    /// </summary>
    public MemberReference HStringArrayMarshallerDispose => field ??= HStringArrayMarshaller
        .CreateMemberReference("Dispose"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.Void,
            parameterTypes: [
                _corLibTypeFactory.UInt32,
                _corLibTypeFactory.Void.MakePointerType().MakePointerType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.HStringArrayMarshaller.Free</c>.
    /// </summary>
    public MemberReference HStringArrayMarshallerFree => field ??= HStringArrayMarshaller
        .CreateMemberReference("Free"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.Void,
            parameterTypes: [
                _corLibTypeFactory.UInt32,
                _corLibTypeFactory.Void.MakePointerType().MakePointerType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.ExceptionArrayMarshaller.ConvertToUnmanaged</c>.
    /// </summary>
    public MemberReference ExceptionArrayMarshallerConvertToUnmanaged => field ??= ExceptionArrayMarshaller
        .CreateMemberReference("ConvertToUnmanaged"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.Void,
            parameterTypes: [
                ReadOnlySpan1.MakeGenericValueType([Exception.ToReferenceTypeSignature()]),
                _corLibTypeFactory.UInt32.MakeByReferenceType(),
                AbiException.ToValueTypeSignature().MakePointerType().MakeByReferenceType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.ExceptionArrayMarshaller.ConvertToManaged</c>.
    /// </summary>
    public MemberReference ExceptionArrayMarshallerConvertToManaged => field ??= ExceptionArrayMarshaller
        .CreateMemberReference("ConvertToManaged"u8, MethodSignature.CreateStatic(
            returnType: Exception.ToReferenceTypeSignature().MakeSzArrayType(),
            parameterTypes: [
                _corLibTypeFactory.UInt32,
                AbiException.ToValueTypeSignature().MakePointerType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.ExceptionArrayMarshaller.CopyToUnmanaged</c>.
    /// </summary>
    public MemberReference ExceptionArrayMarshallerCopyToUnmanaged => field ??= ExceptionArrayMarshaller
        .CreateMemberReference("CopyToUnmanaged"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.Void,
            parameterTypes: [
                ReadOnlySpan1.MakeGenericValueType([Exception.ToReferenceTypeSignature()]),
                _corLibTypeFactory.UInt32,
                AbiException.ToValueTypeSignature().MakePointerType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.ExceptionArrayMarshaller.CopyToManaged</c>.
    /// </summary>
    public MemberReference ExceptionArrayMarshallerCopyToManaged => field ??= ExceptionArrayMarshaller
        .CreateMemberReference("CopyToManaged"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.Void,
            parameterTypes: [
                _corLibTypeFactory.UInt32,
                AbiException.ToValueTypeSignature().MakePointerType(),
                Span1.MakeGenericValueType([Exception.ToReferenceTypeSignature()])]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeKeyValuePairTypeMarshaller.ConvertToUnmanagedUnsafe</c>.
    /// </summary>
    public MemberReference WindowsRuntimeKeyValuePairTypeMarshallerConvertToUnmanagedUnsafe => field ??= WindowsRuntimeKeyValuePairTypeMarshaller
        .CreateMemberReference("ConvertToUnmanagedUnsafe"u8, MethodSignature.CreateStatic(
            returnType: WindowsRuntimeObjectReferenceValue.ToValueTypeSignature(),
            parameterTypes: [
                _corLibTypeFactory.Object,
                CreateComInterfaceFlags.ToValueTypeSignature(),
                Guid.ToValueTypeSignature().MakeByReferenceType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.TypeReference.GetPinnableReference</c>.
    /// </summary>
    public MemberReference TypeReferenceGetPinnableReference => field ??= TypeReference
        .CreateMemberReference("GetPinnableReference"u8, MethodSignature.CreateInstance(
            returnType: _corLibTypeFactory.Byte.MakeByReferenceType()));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.TypeReference.ConvertToUnmanagedUnsafe</c>.
    /// </summary>
    public MemberReference TypeReferenceConvertToUnmanagedUnsafe => field ??= TypeReference
        .CreateMemberReference("ConvertToUnmanagedUnsafe"u8, MethodSignature.CreateInstance(
            returnType: AbiType.ToValueTypeSignature()));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.HStringReference.get_HString</c>.
    /// </summary>
    public MemberReference HStringReferenceget_HString => field ??= HStringReference
        .CreateMemberReference("get_HString"u8, MethodSignature.CreateInstance(
            returnType: _corLibTypeFactory.Void.MakePointerType()));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.HStringMarshaller.ConvertToUnmanaged</c>.
    /// </summary>
    public MemberReference HStringMarshallerConvertToUnmanaged => field ??= HStringMarshaller
        .CreateMemberReference("ConvertToUnmanaged"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.Void.MakePointerType(),
            parameterTypes: [ReadOnlySpanChar]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.HStringMarshaller.ConvertToUnmanagedUnsafe</c>.
    /// </summary>
    public MemberReference HStringMarshallerConvertToUnmanagedUnsafe => field ??= HStringMarshaller
        .CreateMemberReference("ConvertToUnmanagedUnsafe"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.Void,
            parameterTypes: [
                _corLibTypeFactory.Char.MakePointerType(),
                Nullable1.MakeGenericValueType([_corLibTypeFactory.Int32]),
                HStringReference.ToValueTypeSignature().MakeByReferenceType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.HStringMarshaller.ConvertToManaged</c>.
    /// </summary>
    public MemberReference HStringMarshallerConvertToManaged => field ??= HStringMarshaller
        .CreateMemberReference("ConvertToManaged"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.String,
            parameterTypes: [_corLibTypeFactory.Void.MakePointerType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.HStringMarshaller.ConvertToManagedUnsafe</c>.
    /// </summary>
    public MemberReference HStringMarshallerConvertToManagedUnsafe => field ??= HStringMarshaller
        .CreateMemberReference("ConvertToManagedUnsafe"u8, MethodSignature.CreateStatic(
            returnType: ReadOnlySpanChar,
            parameterTypes: [_corLibTypeFactory.Void.MakePointerType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.HStringMarshaller.Free</c>.
    /// </summary>
    public MemberReference HStringMarshallerFree => field ??= HStringMarshaller
        .CreateMemberReference("Free"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.Void,
            parameterTypes: [_corLibTypeFactory.Void.MakePointerType()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.RestrictedErrorInfo.ThrowExceptionForHR(int)</c>.
    /// </summary>
    public MemberReference RestrictedErrorInfoThrowExceptionForHR => field ??= RestrictedErrorInfo
        .CreateMemberReference("ThrowExceptionForHR"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.Void,
            parameterTypes: [_corLibTypeFactory.Int32]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(Exception)</c>.
    /// </summary>
    public MemberReference RestrictedErrorInfoExceptionMarshallerConvertToUnmanaged => field ??= RestrictedErrorInfoExceptionMarshaller
        .CreateMemberReference("ConvertToUnmanaged"u8, MethodSignature.CreateStatic(
            returnType: _corLibTypeFactory.Int32,
            parameterTypes: [new TypeReference(_corLibTypeFactory.CorLibScope, "System"u8, "Exception"u8).ToReferenceTypeSignature()]));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.IWindowsRuntimeReferenceTypeArrayElementMarshaller&lt;T&gt;.ConvertToUnmanaged</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference IWindowsRuntimeReferenceTypeArrayElementMarshallerConvertToUnmanaged(TypeSignature elementType)
    {
        return IWindowsRuntimeReferenceTypeArrayElementMarshaller1
            .MakeGenericReferenceType([elementType])
            .ToTypeDefOrRef()
            .CreateMemberReference("ConvertToUnmanaged"u8, MethodSignature.CreateStatic(
                returnType: WindowsRuntimeObjectReferenceValue.ToValueTypeSignature(),
                parameterTypes: [new GenericParameterSignature(GenericParameterType.Type, 0)]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.IWindowsRuntimeReferenceTypeArrayElementMarshaller&lt;T&gt;.ConvertToManaged</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference IWindowsRuntimeReferenceTypeArrayElementMarshallerConvertToManaged(TypeSignature elementType)
    {
        return IWindowsRuntimeReferenceTypeArrayElementMarshaller1
            .MakeGenericReferenceType([elementType])
            .ToTypeDefOrRef()
            .CreateMemberReference("ConvertToManaged"u8, MethodSignature.CreateStatic(
                returnType: new GenericParameterSignature(GenericParameterType.Type, 0),
                parameterTypes: [_corLibTypeFactory.Void.MakePointerType()]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.IWindowsRuntimeManagedValueTypeArrayElementMarshaller&lt;T, TAbi&gt;.ConvertToUnmanaged</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    /// <param name="abiType">The ABI type.</param>
    public MemberReference IWindowsRuntimeManagedValueTypeArrayElementMarshallerConvertToUnmanaged(TypeSignature elementType, TypeSignature abiType)
    {
        return IWindowsRuntimeManagedValueTypeArrayElementMarshaller2
            .MakeGenericReferenceType([elementType, abiType])
            .ToTypeDefOrRef()
            .CreateMemberReference("ConvertToUnmanaged"u8, MethodSignature.CreateStatic(
                returnType: new GenericParameterSignature(GenericParameterType.Type, 1),
                parameterTypes: [new GenericParameterSignature(GenericParameterType.Type, 0)]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.IWindowsRuntimeManagedValueTypeArrayElementMarshaller&lt;T, TAbi&gt;.ConvertToManaged</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    /// <param name="abiType">The ABI type.</param>
    public MemberReference IWindowsRuntimeManagedValueTypeArrayElementMarshallerConvertToManaged(TypeSignature elementType, TypeSignature abiType)
    {
        return IWindowsRuntimeManagedValueTypeArrayElementMarshaller2
            .MakeGenericReferenceType([elementType, abiType])
            .ToTypeDefOrRef()
            .CreateMemberReference("ConvertToManaged"u8, MethodSignature.CreateStatic(
                returnType: new GenericParameterSignature(GenericParameterType.Type, 0),
                parameterTypes: [new GenericParameterSignature(GenericParameterType.Type, 1)]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.IWindowsRuntimeManagedValueTypeArrayElementMarshaller&lt;T, TAbi&gt;.Dispose</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    /// <param name="abiType">The ABI type.</param>
    public MemberReference IWindowsRuntimeManagedValueTypeArrayElementMarshallerDispose(TypeSignature elementType, TypeSignature abiType)
    {
        return IWindowsRuntimeManagedValueTypeArrayElementMarshaller2
            .MakeGenericReferenceType([elementType, abiType])
            .ToTypeDefOrRef()
            .CreateMemberReference("Dispose"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Void,
                parameterTypes: [new GenericParameterSignature(GenericParameterType.Type, 1)]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.IWindowsRuntimeUnmanagedValueTypeArrayElementMarshaller&lt;T, TAbi&gt;.ConvertToUnmanaged</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    /// <param name="abiType">The ABI type.</param>
    public MemberReference IWindowsRuntimeUnmanagedValueTypeArrayElementMarshallerConvertToUnmanaged(TypeSignature elementType, TypeSignature abiType)
    {
        return IWindowsRuntimeUnmanagedValueTypeArrayElementMarshaller2
            .MakeGenericReferenceType([elementType, abiType])
            .ToTypeDefOrRef()
            .CreateMemberReference("ConvertToUnmanaged"u8, MethodSignature.CreateStatic(
                returnType: new GenericParameterSignature(GenericParameterType.Type, 1),
                parameterTypes: [new GenericParameterSignature(GenericParameterType.Type, 0)]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.IWindowsRuntimeUnmanagedValueTypeArrayElementMarshaller&lt;T, TAbi&gt;.ConvertToManaged</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    /// <param name="abiType">The ABI type.</param>
    public MemberReference IWindowsRuntimeUnmanagedValueTypeArrayElementMarshallerConvertToManaged(TypeSignature elementType, TypeSignature abiType)
    {
        return IWindowsRuntimeUnmanagedValueTypeArrayElementMarshaller2
            .MakeGenericReferenceType([elementType, abiType])
            .ToTypeDefOrRef()
            .CreateMemberReference("ConvertToManaged"u8, MethodSignature.CreateStatic(
                returnType: new GenericParameterSignature(GenericParameterType.Type, 0),
                parameterTypes: [new GenericParameterSignature(GenericParameterType.Type, 1)]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.IWindowsRuntimeKeyValuePairTypeArrayElementMarshaller&lt;TKey, TValue&gt;.ConvertToUnmanaged</c>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    public MemberReference IWindowsRuntimeKeyValuePairTypeArrayElementMarshallerConvertToUnmanaged(TypeSignature keyType, TypeSignature valueType)
    {
        return IWindowsRuntimeKeyValuePairTypeArrayElementMarshaller2
            .MakeGenericReferenceType([keyType, valueType])
            .ToTypeDefOrRef()
            .CreateMemberReference("ConvertToUnmanaged"u8, MethodSignature.CreateStatic(
                returnType: WindowsRuntimeObjectReferenceValue.ToValueTypeSignature(),
                parameterTypes: [
                    KeyValuePair2.MakeGenericValueType([
                        new GenericParameterSignature(GenericParameterType.Type, 0),
                        new GenericParameterSignature(GenericParameterType.Type, 1)])]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.IWindowsRuntimeKeyValuePairTypeArrayElementMarshaller&lt;TKey, TValue&gt;.ConvertToManaged</c>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    public MemberReference IWindowsRuntimeKeyValuePairTypeArrayElementMarshallerConvertToManaged(TypeSignature keyType, TypeSignature valueType)
    {
        return IWindowsRuntimeKeyValuePairTypeArrayElementMarshaller2
            .MakeGenericReferenceType([keyType, valueType])
            .ToTypeDefOrRef()
            .CreateMemberReference("ConvertToManaged"u8, MethodSignature.CreateStatic(
                returnType: KeyValuePair2.MakeGenericValueType([
                    new GenericParameterSignature(GenericParameterType.Type, 0),
                    new GenericParameterSignature(GenericParameterType.Type, 1)]),
                parameterTypes: [_corLibTypeFactory.Void.MakePointerType()]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.IWindowsRuntimeNullableTypeArrayElementMarshaller&lt;T&gt;.ConvertToUnmanaged</c>.
    /// </summary>
    /// <param name="underlyingType">The underlying value type.</param>
    public MemberReference IWindowsRuntimeNullableTypeArrayElementMarshallerConvertToUnmanaged(TypeSignature underlyingType)
    {
        return IWindowsRuntimeNullableTypeArrayElementMarshaller1
            .MakeGenericReferenceType([underlyingType])
            .ToTypeDefOrRef()
            .CreateMemberReference("ConvertToUnmanaged"u8, MethodSignature.CreateStatic(
                returnType: WindowsRuntimeObjectReferenceValue.ToValueTypeSignature(),
                parameterTypes: [Nullable1.MakeGenericValueType([new GenericParameterSignature(GenericParameterType.Type, 0)])]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.IWindowsRuntimeNullableTypeArrayElementMarshaller&lt;T&gt;.ConvertToManaged</c>.
    /// </summary>
    /// <param name="underlyingType">The underlying value type.</param>
    public MemberReference IWindowsRuntimeNullableTypeArrayElementMarshallerConvertToManaged(TypeSignature underlyingType)
    {
        return IWindowsRuntimeNullableTypeArrayElementMarshaller1
            .MakeGenericReferenceType([underlyingType])
            .ToTypeDefOrRef()
            .CreateMemberReference("ConvertToManaged"u8, MethodSignature.CreateStatic(
                returnType: Nullable1.MakeGenericValueType([new GenericParameterSignature(GenericParameterType.Type, 0)]),
                parameterTypes: [_corLibTypeFactory.Void.MakePointerType()]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.IWindowsRuntimeReferenceTypeElementMarshaller&lt;T&gt;.ConvertToUnmanaged</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference IWindowsRuntimeReferenceTypeElementMarshallerConvertToUnmanaged(TypeSignature elementType)
    {
        return IWindowsRuntimeReferenceTypeElementMarshaller1
            .MakeGenericReferenceType([elementType])
            .ToTypeDefOrRef()
            .CreateMemberReference("ConvertToUnmanaged"u8, MethodSignature.CreateStatic(
                returnType: WindowsRuntimeObjectReferenceValue.ToValueTypeSignature(),
                parameterTypes: [new GenericParameterSignature(GenericParameterType.Type, 0)]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.IWindowsRuntimeManagedValueTypeElementMarshaller&lt;T, TAbi&gt;.ConvertToUnmanaged</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    /// <param name="abiType">The ABI type.</param>
    public MemberReference IWindowsRuntimeManagedValueTypeElementMarshallerConvertToUnmanaged(TypeSignature elementType, TypeSignature abiType)
    {
        return IWindowsRuntimeManagedValueTypeElementMarshaller2
            .MakeGenericReferenceType([elementType, abiType])
            .ToTypeDefOrRef()
            .CreateMemberReference("ConvertToUnmanaged"u8, MethodSignature.CreateStatic(
                returnType: new GenericParameterSignature(GenericParameterType.Type, 1),
                parameterTypes: [new GenericParameterSignature(GenericParameterType.Type, 0)]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.IWindowsRuntimeManagedValueTypeElementMarshaller&lt;T, TAbi&gt;.Dispose</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    /// <param name="abiType">The ABI type.</param>
    public MemberReference IWindowsRuntimeManagedValueTypeElementMarshallerDispose(TypeSignature elementType, TypeSignature abiType)
    {
        return IWindowsRuntimeManagedValueTypeElementMarshaller2
            .MakeGenericReferenceType([elementType, abiType])
            .ToTypeDefOrRef()
            .CreateMemberReference("Dispose"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Void,
                parameterTypes: [new GenericParameterSignature(GenericParameterType.Type, 1)]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.IWindowsRuntimeUnmanagedValueTypeElementMarshaller&lt;T, TAbi&gt;.ConvertToUnmanaged</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    /// <param name="abiType">The ABI type.</param>
    public MemberReference IWindowsRuntimeUnmanagedValueTypeElementMarshallerConvertToUnmanaged(TypeSignature elementType, TypeSignature abiType)
    {
        return IWindowsRuntimeUnmanagedValueTypeElementMarshaller2
            .MakeGenericReferenceType([elementType, abiType])
            .ToTypeDefOrRef()
            .CreateMemberReference("ConvertToUnmanaged"u8, MethodSignature.CreateStatic(
                returnType: new GenericParameterSignature(GenericParameterType.Type, 1),
                parameterTypes: [new GenericParameterSignature(GenericParameterType.Type, 0)]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.IWindowsRuntimeKeyValuePairTypeElementMarshaller&lt;TKey, TValue&gt;.ConvertToUnmanaged</c>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    public MemberReference IWindowsRuntimeKeyValuePairTypeElementMarshallerConvertToUnmanaged(TypeSignature keyType, TypeSignature valueType)
    {
        return IWindowsRuntimeKeyValuePairTypeElementMarshaller2
            .MakeGenericReferenceType([keyType, valueType])
            .ToTypeDefOrRef()
            .CreateMemberReference("ConvertToUnmanaged"u8, MethodSignature.CreateStatic(
                returnType: WindowsRuntimeObjectReferenceValue.ToValueTypeSignature(),
                parameterTypes: [
                    KeyValuePair2.MakeGenericValueType([
                        new GenericParameterSignature(GenericParameterType.Type, 0),
                        new GenericParameterSignature(GenericParameterType.Type, 1)])]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.IWindowsRuntimeNullableTypeElementMarshaller&lt;T&gt;.ConvertToUnmanaged</c>.
    /// </summary>
    /// <param name="underlyingType">The underlying value type.</param>
    public MemberReference IWindowsRuntimeNullableTypeElementMarshallerConvertToUnmanaged(TypeSignature underlyingType)
    {
        return IWindowsRuntimeNullableTypeElementMarshaller1
            .MakeGenericReferenceType([underlyingType])
            .ToTypeDefOrRef()
            .CreateMemberReference("ConvertToUnmanaged"u8, MethodSignature.CreateStatic(
                returnType: WindowsRuntimeObjectReferenceValue.ToValueTypeSignature(),
                parameterTypes: [Nullable1.MakeGenericValueType([new GenericParameterSignature(GenericParameterType.Type, 0)])]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.WindowsRuntimeBlittableValueTypeArrayMarshaller&lt;T&gt;.ConvertToUnmanaged</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference WindowsRuntimeBlittableValueTypeArrayMarshallerConvertToUnmanaged(TypeSignature elementType)
    {
        return WindowsRuntimeBlittableValueTypeArrayMarshaller1
            .MakeGenericReferenceType([elementType])
            .ToTypeDefOrRef()
            .CreateMemberReference("ConvertToUnmanaged"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Void,
                parameterTypes: [
                    ReadOnlySpan1.MakeGenericValueType([new GenericParameterSignature(GenericParameterType.Type, 0)]),
                    _corLibTypeFactory.UInt32.MakeByReferenceType(),
                    new GenericParameterSignature(GenericParameterType.Type, 0).MakePointerType().MakeByReferenceType()]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.WindowsRuntimeBlittableValueTypeArrayMarshaller&lt;T&gt;.ConvertToManaged</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference WindowsRuntimeBlittableValueTypeArrayMarshallerConvertToManaged(TypeSignature elementType)
    {
        return WindowsRuntimeBlittableValueTypeArrayMarshaller1
            .MakeGenericReferenceType([elementType])
            .ToTypeDefOrRef()
            .CreateMemberReference("ConvertToManaged"u8, MethodSignature.CreateStatic(
                returnType: new GenericParameterSignature(GenericParameterType.Type, 0).MakeSzArrayType(),
                parameterTypes: [
                    _corLibTypeFactory.UInt32,
                    new GenericParameterSignature(GenericParameterType.Type, 0).MakePointerType()]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.WindowsRuntimeBlittableValueTypeArrayMarshaller&lt;T&gt;.CopyToUnmanaged</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference WindowsRuntimeBlittableValueTypeArrayMarshallerCopyToUnmanaged(TypeSignature elementType)
    {
        return WindowsRuntimeBlittableValueTypeArrayMarshaller1
            .MakeGenericReferenceType([elementType])
            .ToTypeDefOrRef()
            .CreateMemberReference("CopyToUnmanaged"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Void,
                parameterTypes: [
                    ReadOnlySpan1.MakeGenericValueType([new GenericParameterSignature(GenericParameterType.Type, 0)]),
                    _corLibTypeFactory.UInt32,
                    new GenericParameterSignature(GenericParameterType.Type, 0).MakePointerType()]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.Marshalling.WindowsRuntimeBlittableValueTypeArrayMarshaller&lt;T&gt;.CopyToManaged</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference WindowsRuntimeBlittableValueTypeArrayMarshallerCopyToManaged(TypeSignature elementType)
    {
        return WindowsRuntimeBlittableValueTypeArrayMarshaller1
            .MakeGenericReferenceType([elementType])
            .ToTypeDefOrRef()
            .CreateMemberReference("CopyToManaged"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Void,
                parameterTypes: [
                    _corLibTypeFactory.UInt32,
                    new GenericParameterSignature(GenericParameterType.Type, 0).MakePointerType(),
                    Span1.MakeGenericValueType([new GenericParameterSignature(GenericParameterType.Type, 0)])]));
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.Marshalling.WindowsRuntimeManagedValueTypeArrayMarshaller&lt;T, TAbi&gt;.ConvertToUnmanaged&lt;TElementMarshaller&gt;</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    /// <param name="abiType">The ABI type.</param>
    /// <param name="elementMarshallerType">The element marshaller type.</param>
    public MethodSpecification WindowsRuntimeManagedValueTypeArrayMarshallerConvertToUnmanaged(TypeSignature elementType, TypeSignature abiType, TypeSignature elementMarshallerType)
    {
        return WindowsRuntimeManagedValueTypeArrayMarshaller2
            .MakeGenericReferenceType([elementType, abiType])
            .ToTypeDefOrRef()
            .CreateMemberReference("ConvertToUnmanaged"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Void,
                genericParameterCount: 1,
                parameterTypes: [
                    ReadOnlySpan1.MakeGenericValueType([new GenericParameterSignature(GenericParameterType.Type, 0)]),
                    _corLibTypeFactory.UInt32.MakeByReferenceType(),
                    new GenericParameterSignature(GenericParameterType.Type, 1).MakePointerType().MakeByReferenceType()]))
            .MakeGenericInstanceMethod([elementMarshallerType]);
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.Marshalling.WindowsRuntimeManagedValueTypeArrayMarshaller&lt;T, TAbi&gt;.ConvertToManaged&lt;TElementMarshaller&gt;</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    /// <param name="abiType">The ABI type.</param>
    /// <param name="elementMarshallerType">The element marshaller type.</param>
    public MethodSpecification WindowsRuntimeManagedValueTypeArrayMarshallerConvertToManaged(TypeSignature elementType, TypeSignature abiType, TypeSignature elementMarshallerType)
    {
        return WindowsRuntimeManagedValueTypeArrayMarshaller2
            .MakeGenericReferenceType([elementType, abiType])
            .ToTypeDefOrRef()
            .CreateMemberReference("ConvertToManaged"u8, MethodSignature.CreateStatic(
                returnType: new GenericParameterSignature(GenericParameterType.Type, 0).MakeSzArrayType(),
                genericParameterCount: 1,
                parameterTypes: [
                    _corLibTypeFactory.UInt32,
                    new GenericParameterSignature(GenericParameterType.Type, 1).MakePointerType()]))
            .MakeGenericInstanceMethod([elementMarshallerType]);
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.Marshalling.WindowsRuntimeManagedValueTypeArrayMarshaller&lt;T, TAbi&gt;.CopyToUnmanaged&lt;TElementMarshaller&gt;</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    /// <param name="abiType">The ABI type.</param>
    /// <param name="elementMarshallerType">The element marshaller type.</param>
    public MethodSpecification WindowsRuntimeManagedValueTypeArrayMarshallerCopyToUnmanaged(TypeSignature elementType, TypeSignature abiType, TypeSignature elementMarshallerType)
    {
        return WindowsRuntimeManagedValueTypeArrayMarshaller2
            .MakeGenericReferenceType([elementType, abiType])
            .ToTypeDefOrRef()
            .CreateMemberReference("CopyToUnmanaged"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Void,
                genericParameterCount: 1,
                parameterTypes: [
                    ReadOnlySpan1.MakeGenericValueType([new GenericParameterSignature(GenericParameterType.Type, 0)]),
                    _corLibTypeFactory.UInt32,
                    new GenericParameterSignature(GenericParameterType.Type, 1).MakePointerType()]))
            .MakeGenericInstanceMethod([elementMarshallerType]);
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.Marshalling.WindowsRuntimeManagedValueTypeArrayMarshaller&lt;T, TAbi&gt;.CopyToManaged&lt;TElementMarshaller&gt;</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    /// <param name="abiType">The ABI type.</param>
    /// <param name="elementMarshallerType">The element marshaller type.</param>
    public MethodSpecification WindowsRuntimeManagedValueTypeArrayMarshallerCopyToManaged(TypeSignature elementType, TypeSignature abiType, TypeSignature elementMarshallerType)
    {
        return WindowsRuntimeManagedValueTypeArrayMarshaller2
            .MakeGenericReferenceType([elementType, abiType])
            .ToTypeDefOrRef()
            .CreateMemberReference("CopyToManaged"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Void,
                genericParameterCount: 1,
                parameterTypes: [
                    _corLibTypeFactory.UInt32,
                    new GenericParameterSignature(GenericParameterType.Type, 1).MakePointerType(),
                    Span1.MakeGenericValueType([new GenericParameterSignature(GenericParameterType.Type, 0)])]))
            .MakeGenericInstanceMethod([elementMarshallerType]);
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.Marshalling.WindowsRuntimeManagedValueTypeArrayMarshaller&lt;T, TAbi&gt;.Dispose&lt;TElementMarshaller&gt;</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    /// <param name="abiType">The ABI type.</param>
    /// <param name="elementMarshallerType">The element marshaller type.</param>
    public MethodSpecification WindowsRuntimeManagedValueTypeArrayMarshallerDispose(TypeSignature elementType, TypeSignature abiType, TypeSignature elementMarshallerType)
    {
        return WindowsRuntimeManagedValueTypeArrayMarshaller2
            .MakeGenericReferenceType([elementType, abiType])
            .ToTypeDefOrRef()
            .CreateMemberReference("Dispose"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Void,
                genericParameterCount: 1,
                parameterTypes: [
                    _corLibTypeFactory.UInt32,
                    new GenericParameterSignature(GenericParameterType.Type, 1).MakePointerType()]))
            .MakeGenericInstanceMethod([elementMarshallerType]);
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.Marshalling.WindowsRuntimeManagedValueTypeArrayMarshaller&lt;T, TAbi&gt;.Free&lt;TElementMarshaller&gt;</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    /// <param name="abiType">The ABI type.</param>
    /// <param name="elementMarshallerType">The element marshaller type.</param>
    public MethodSpecification WindowsRuntimeManagedValueTypeArrayMarshallerFree(TypeSignature elementType, TypeSignature abiType, TypeSignature elementMarshallerType)
    {
        return WindowsRuntimeManagedValueTypeArrayMarshaller2
            .MakeGenericReferenceType([elementType, abiType])
            .ToTypeDefOrRef()
            .CreateMemberReference("Free"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Void,
                genericParameterCount: 1,
                parameterTypes: [
                    _corLibTypeFactory.UInt32,
                    new GenericParameterSignature(GenericParameterType.Type, 1).MakePointerType()]))
            .MakeGenericInstanceMethod([elementMarshallerType]);
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.Marshalling.WindowsRuntimeUnmanagedValueTypeArrayMarshaller&lt;T, TAbi&gt;.ConvertToUnmanaged&lt;TElementMarshaller&gt;</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    /// <param name="abiType">The ABI type.</param>
    /// <param name="elementMarshallerType">The element marshaller type.</param>
    public MethodSpecification WindowsRuntimeUnmanagedValueTypeArrayMarshallerConvertToUnmanaged(TypeSignature elementType, TypeSignature abiType, TypeSignature elementMarshallerType)
    {
        return WindowsRuntimeUnmanagedValueTypeArrayMarshaller2
            .MakeGenericReferenceType([elementType, abiType])
            .ToTypeDefOrRef()
            .CreateMemberReference("ConvertToUnmanaged"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Void,
                genericParameterCount: 1,
                parameterTypes: [
                    ReadOnlySpan1.MakeGenericValueType([new GenericParameterSignature(GenericParameterType.Type, 0)]),
                    _corLibTypeFactory.UInt32.MakeByReferenceType(),
                    new GenericParameterSignature(GenericParameterType.Type, 1).MakePointerType().MakeByReferenceType()]))
            .MakeGenericInstanceMethod([elementMarshallerType]);
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.Marshalling.WindowsRuntimeUnmanagedValueTypeArrayMarshaller&lt;T, TAbi&gt;.ConvertToManaged&lt;TElementMarshaller&gt;</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    /// <param name="abiType">The ABI type.</param>
    /// <param name="elementMarshallerType">The element marshaller type.</param>
    public MethodSpecification WindowsRuntimeUnmanagedValueTypeArrayMarshallerConvertToManaged(TypeSignature elementType, TypeSignature abiType, TypeSignature elementMarshallerType)
    {
        return WindowsRuntimeUnmanagedValueTypeArrayMarshaller2
            .MakeGenericReferenceType([elementType, abiType])
            .ToTypeDefOrRef()
            .CreateMemberReference("ConvertToManaged"u8, MethodSignature.CreateStatic(
                returnType: new GenericParameterSignature(GenericParameterType.Type, 0).MakeSzArrayType(),
                genericParameterCount: 1,
                parameterTypes: [
                    _corLibTypeFactory.UInt32,
                    new GenericParameterSignature(GenericParameterType.Type, 1).MakePointerType()]))
            .MakeGenericInstanceMethod([elementMarshallerType]);
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.Marshalling.WindowsRuntimeUnmanagedValueTypeArrayMarshaller&lt;T, TAbi&gt;.CopyToUnmanaged&lt;TElementMarshaller&gt;</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    /// <param name="abiType">The ABI type.</param>
    /// <param name="elementMarshallerType">The element marshaller type.</param>
    public MethodSpecification WindowsRuntimeUnmanagedValueTypeArrayMarshallerCopyToUnmanaged(TypeSignature elementType, TypeSignature abiType, TypeSignature elementMarshallerType)
    {
        return WindowsRuntimeUnmanagedValueTypeArrayMarshaller2
            .MakeGenericReferenceType([elementType, abiType])
            .ToTypeDefOrRef()
            .CreateMemberReference("CopyToUnmanaged"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Void,
                genericParameterCount: 1,
                parameterTypes: [
                    ReadOnlySpan1.MakeGenericValueType([new GenericParameterSignature(GenericParameterType.Type, 0)]),
                    _corLibTypeFactory.UInt32,
                    new GenericParameterSignature(GenericParameterType.Type, 1).MakePointerType()]))
            .MakeGenericInstanceMethod([elementMarshallerType]);
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.Marshalling.WindowsRuntimeUnmanagedValueTypeArrayMarshaller&lt;T, TAbi&gt;.CopyToManaged&lt;TElementMarshaller&gt;</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    /// <param name="abiType">The ABI type.</param>
    /// <param name="elementMarshallerType">The element marshaller type.</param>
    public MethodSpecification WindowsRuntimeUnmanagedValueTypeArrayMarshallerCopyToManaged(TypeSignature elementType, TypeSignature abiType, TypeSignature elementMarshallerType)
    {
        return WindowsRuntimeUnmanagedValueTypeArrayMarshaller2
            .MakeGenericReferenceType([elementType, abiType])
            .ToTypeDefOrRef()
            .CreateMemberReference("CopyToManaged"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Void,
                genericParameterCount: 1,
                parameterTypes: [
                    _corLibTypeFactory.UInt32,
                    new GenericParameterSignature(GenericParameterType.Type, 1).MakePointerType(),
                    Span1.MakeGenericValueType([new GenericParameterSignature(GenericParameterType.Type, 0)])]))
            .MakeGenericInstanceMethod([elementMarshallerType]);
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.Marshalling.WindowsRuntimeReferenceTypeArrayMarshaller&lt;T&gt;.ConvertToUnmanaged&lt;TElementMarshaller&gt;</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    /// <param name="elementMarshallerType">The element marshaller type.</param>
    public MethodSpecification WindowsRuntimeReferenceTypeArrayMarshallerConvertToUnmanaged(TypeSignature elementType, TypeSignature elementMarshallerType)
    {
        return WindowsRuntimeReferenceTypeArrayMarshaller1
            .MakeGenericReferenceType([elementType])
            .ToTypeDefOrRef()
            .CreateMemberReference("ConvertToUnmanaged"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Void,
                genericParameterCount: 1,
                parameterTypes: [
                    ReadOnlySpan1.MakeGenericValueType([new GenericParameterSignature(GenericParameterType.Type, 0)]),
                    _corLibTypeFactory.UInt32.MakeByReferenceType(),
                    _corLibTypeFactory.Void.MakePointerType().MakePointerType().MakeByReferenceType()]))
            .MakeGenericInstanceMethod([elementMarshallerType]);
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.Marshalling.WindowsRuntimeReferenceTypeArrayMarshaller&lt;T&gt;.ConvertToManaged&lt;TElementMarshaller&gt;</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    /// <param name="elementMarshallerType">The element marshaller type.</param>
    public MethodSpecification WindowsRuntimeReferenceTypeArrayMarshallerConvertToManaged(TypeSignature elementType, TypeSignature elementMarshallerType)
    {
        return WindowsRuntimeReferenceTypeArrayMarshaller1
            .MakeGenericReferenceType([elementType])
            .ToTypeDefOrRef()
            .CreateMemberReference("ConvertToManaged"u8, MethodSignature.CreateStatic(
                returnType: new GenericParameterSignature(GenericParameterType.Type, 0).MakeSzArrayType(),
                genericParameterCount: 1,
                parameterTypes: [
                    _corLibTypeFactory.UInt32,
                    _corLibTypeFactory.Void.MakePointerType().MakePointerType()]))
            .MakeGenericInstanceMethod([elementMarshallerType]);
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.Marshalling.WindowsRuntimeReferenceTypeArrayMarshaller&lt;T&gt;.CopyToUnmanaged&lt;TElementMarshaller&gt;</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    /// <param name="elementMarshallerType">The element marshaller type.</param>
    public MethodSpecification WindowsRuntimeReferenceTypeArrayMarshallerCopyToUnmanaged(TypeSignature elementType, TypeSignature elementMarshallerType)
    {
        return WindowsRuntimeReferenceTypeArrayMarshaller1
            .MakeGenericReferenceType([elementType])
            .ToTypeDefOrRef()
            .CreateMemberReference("CopyToUnmanaged"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Void,
                genericParameterCount: 1,
                parameterTypes: [
                    ReadOnlySpan1.MakeGenericValueType([new GenericParameterSignature(GenericParameterType.Type, 0)]),
                    _corLibTypeFactory.UInt32,
                    _corLibTypeFactory.Void.MakePointerType().MakePointerType()]))
            .MakeGenericInstanceMethod([elementMarshallerType]);
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.Marshalling.WindowsRuntimeReferenceTypeArrayMarshaller&lt;T&gt;.CopyToManaged&lt;TElementMarshaller&gt;</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    /// <param name="elementMarshallerType">The element marshaller type.</param>
    public MethodSpecification WindowsRuntimeReferenceTypeArrayMarshallerCopyToManaged(TypeSignature elementType, TypeSignature elementMarshallerType)
    {
        return WindowsRuntimeReferenceTypeArrayMarshaller1
            .MakeGenericReferenceType([elementType])
            .ToTypeDefOrRef()
            .CreateMemberReference("CopyToManaged"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Void,
                genericParameterCount: 1,
                parameterTypes: [
                    _corLibTypeFactory.UInt32,
                    _corLibTypeFactory.Void.MakePointerType().MakePointerType(),
                    Span1.MakeGenericValueType([new GenericParameterSignature(GenericParameterType.Type, 0)])]))
            .MakeGenericInstanceMethod([elementMarshallerType]);
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.Marshalling.WindowsRuntimeKeyValuePairTypeArrayMarshaller&lt;TKey, TValue&gt;.ConvertToUnmanaged&lt;TElementMarshaller&gt;</c>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    /// <param name="elementMarshallerType">The element marshaller type.</param>
    public MethodSpecification WindowsRuntimeKeyValuePairTypeArrayMarshallerConvertToUnmanaged(TypeSignature keyType, TypeSignature valueType, TypeSignature elementMarshallerType)
    {
        return WindowsRuntimeKeyValuePairTypeArrayMarshaller2
            .MakeGenericReferenceType([keyType, valueType])
            .ToTypeDefOrRef()
            .CreateMemberReference("ConvertToUnmanaged"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Void,
                genericParameterCount: 1,
                parameterTypes: [
                    ReadOnlySpan1.MakeGenericValueType([
                        KeyValuePair2.MakeGenericValueType([
                            new GenericParameterSignature(GenericParameterType.Type, 0),
                            new GenericParameterSignature(GenericParameterType.Type, 1)])]),
                    _corLibTypeFactory.UInt32.MakeByReferenceType(),
                    _corLibTypeFactory.Void.MakePointerType().MakePointerType().MakeByReferenceType()]))
            .MakeGenericInstanceMethod([elementMarshallerType]);
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.Marshalling.WindowsRuntimeKeyValuePairTypeArrayMarshaller&lt;TKey, TValue&gt;.ConvertToManaged&lt;TElementMarshaller&gt;</c>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    /// <param name="elementMarshallerType">The element marshaller type.</param>
    public MethodSpecification WindowsRuntimeKeyValuePairTypeArrayMarshallerConvertToManaged(TypeSignature keyType, TypeSignature valueType, TypeSignature elementMarshallerType)
    {
        return WindowsRuntimeKeyValuePairTypeArrayMarshaller2
            .MakeGenericReferenceType([keyType, valueType])
            .ToTypeDefOrRef()
            .CreateMemberReference("ConvertToManaged"u8, MethodSignature.CreateStatic(
                returnType: KeyValuePair2.MakeGenericValueType([
                    new GenericParameterSignature(GenericParameterType.Type, 0),
                    new GenericParameterSignature(GenericParameterType.Type, 1)]).MakeSzArrayType(),
                genericParameterCount: 1,
                parameterTypes: [
                    _corLibTypeFactory.UInt32,
                    _corLibTypeFactory.Void.MakePointerType().MakePointerType()]))
            .MakeGenericInstanceMethod([elementMarshallerType]);
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.Marshalling.WindowsRuntimeKeyValuePairTypeArrayMarshaller&lt;TKey, TValue&gt;.CopyToUnmanaged&lt;TElementMarshaller&gt;</c>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    /// <param name="elementMarshallerType">The element marshaller type.</param>
    public MethodSpecification WindowsRuntimeKeyValuePairTypeArrayMarshallerCopyToUnmanaged(TypeSignature keyType, TypeSignature valueType, TypeSignature elementMarshallerType)
    {
        return WindowsRuntimeKeyValuePairTypeArrayMarshaller2
            .MakeGenericReferenceType([keyType, valueType])
            .ToTypeDefOrRef()
            .CreateMemberReference("CopyToUnmanaged"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Void,
                genericParameterCount: 1,
                parameterTypes: [
                    ReadOnlySpan1.MakeGenericValueType([
                        KeyValuePair2.MakeGenericValueType([
                            new GenericParameterSignature(GenericParameterType.Type, 0),
                            new GenericParameterSignature(GenericParameterType.Type, 1)])]),
                    _corLibTypeFactory.UInt32,
                    _corLibTypeFactory.Void.MakePointerType().MakePointerType()]))
            .MakeGenericInstanceMethod([elementMarshallerType]);
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.Marshalling.WindowsRuntimeKeyValuePairTypeArrayMarshaller&lt;TKey, TValue&gt;.CopyToManaged&lt;TElementMarshaller&gt;</c>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    /// <param name="elementMarshallerType">The element marshaller type.</param>
    public MethodSpecification WindowsRuntimeKeyValuePairTypeArrayMarshallerCopyToManaged(TypeSignature keyType, TypeSignature valueType, TypeSignature elementMarshallerType)
    {
        return WindowsRuntimeKeyValuePairTypeArrayMarshaller2
            .MakeGenericReferenceType([keyType, valueType])
            .ToTypeDefOrRef()
            .CreateMemberReference("CopyToManaged"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Void,
                genericParameterCount: 1,
                parameterTypes: [
                    _corLibTypeFactory.UInt32,
                    _corLibTypeFactory.Void.MakePointerType().MakePointerType(),
                    Span1.MakeGenericValueType([
                        KeyValuePair2.MakeGenericValueType([
                            new GenericParameterSignature(GenericParameterType.Type, 0),
                            new GenericParameterSignature(GenericParameterType.Type, 1)])])]))
            .MakeGenericInstanceMethod([elementMarshallerType]);
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.Marshalling.WindowsRuntimeNullableTypeArrayMarshaller&lt;T&gt;.ConvertToUnmanaged&lt;TElementMarshaller&gt;</c>.
    /// </summary>
    /// <param name="underlyingType">The underlying value type.</param>
    /// <param name="elementMarshallerType">The element marshaller type.</param>
    public MethodSpecification WindowsRuntimeNullableTypeArrayMarshallerConvertToUnmanaged(TypeSignature underlyingType, TypeSignature elementMarshallerType)
    {
        return WindowsRuntimeNullableTypeArrayMarshaller1
            .MakeGenericReferenceType([underlyingType])
            .ToTypeDefOrRef()
            .CreateMemberReference("ConvertToUnmanaged"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Void,
                genericParameterCount: 1,
                parameterTypes: [
                    ReadOnlySpan1.MakeGenericValueType([Nullable1.MakeGenericValueType([new GenericParameterSignature(GenericParameterType.Type, 0)])]),
                    _corLibTypeFactory.UInt32.MakeByReferenceType(),
                    _corLibTypeFactory.Void.MakePointerType().MakePointerType().MakeByReferenceType()]))
            .MakeGenericInstanceMethod([elementMarshallerType]);
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.Marshalling.WindowsRuntimeNullableTypeArrayMarshaller&lt;T&gt;.ConvertToManaged&lt;TElementMarshaller&gt;</c>.
    /// </summary>
    /// <param name="underlyingType">The underlying value type.</param>
    /// <param name="elementMarshallerType">The element marshaller type.</param>
    public MethodSpecification WindowsRuntimeNullableTypeArrayMarshallerConvertToManaged(TypeSignature underlyingType, TypeSignature elementMarshallerType)
    {
        return WindowsRuntimeNullableTypeArrayMarshaller1
            .MakeGenericReferenceType([underlyingType])
            .ToTypeDefOrRef()
            .CreateMemberReference("ConvertToManaged"u8, MethodSignature.CreateStatic(
                returnType: Nullable1.MakeGenericValueType([new GenericParameterSignature(GenericParameterType.Type, 0)]).MakeSzArrayType(),
                genericParameterCount: 1,
                parameterTypes: [
                    _corLibTypeFactory.UInt32,
                    _corLibTypeFactory.Void.MakePointerType().MakePointerType()]))
            .MakeGenericInstanceMethod([elementMarshallerType]);
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.Marshalling.WindowsRuntimeNullableTypeArrayMarshaller&lt;T&gt;.CopyToUnmanaged&lt;TElementMarshaller&gt;</c>.
    /// </summary>
    /// <param name="underlyingType">The underlying value type.</param>
    /// <param name="elementMarshallerType">The element marshaller type.</param>
    public MethodSpecification WindowsRuntimeNullableTypeArrayMarshallerCopyToUnmanaged(TypeSignature underlyingType, TypeSignature elementMarshallerType)
    {
        return WindowsRuntimeNullableTypeArrayMarshaller1
            .MakeGenericReferenceType([underlyingType])
            .ToTypeDefOrRef()
            .CreateMemberReference("CopyToUnmanaged"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Void,
                genericParameterCount: 1,
                parameterTypes: [
                    ReadOnlySpan1.MakeGenericValueType([Nullable1.MakeGenericValueType([new GenericParameterSignature(GenericParameterType.Type, 0)])]),
                    _corLibTypeFactory.UInt32,
                    _corLibTypeFactory.Void.MakePointerType().MakePointerType()]))
            .MakeGenericInstanceMethod([elementMarshallerType]);
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.Marshalling.WindowsRuntimeNullableTypeArrayMarshaller&lt;T&gt;.CopyToManaged&lt;TElementMarshaller&gt;</c>.
    /// </summary>
    /// <param name="underlyingType">The underlying value type.</param>
    /// <param name="elementMarshallerType">The element marshaller type.</param>
    public MethodSpecification WindowsRuntimeNullableTypeArrayMarshallerCopyToManaged(TypeSignature underlyingType, TypeSignature elementMarshallerType)
    {
        return WindowsRuntimeNullableTypeArrayMarshaller1
            .MakeGenericReferenceType([underlyingType])
            .ToTypeDefOrRef()
            .CreateMemberReference("CopyToManaged"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Void,
                genericParameterCount: 1,
                parameterTypes: [
                    _corLibTypeFactory.UInt32,
                    _corLibTypeFactory.Void.MakePointerType().MakePointerType(),
                    Span1.MakeGenericValueType([Nullable1.MakeGenericValueType([new GenericParameterSignature(GenericParameterType.Type, 0)])])]))
            .MakeGenericInstanceMethod([elementMarshallerType]);
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>AddEventHandler</c> for <see cref="EventRegistrationTokenTable1"/>.
    /// </summary>
    /// <param name="eventRegistrationTokenTableType">The input table type.</param>
    public MemberReference EventRegistrationTokenTableAddEventHandler(TypeSignature eventRegistrationTokenTableType)
    {
        return eventRegistrationTokenTableType
            .ToTypeDefOrRef()
            .CreateMemberReference("AddEventHandler"u8, MethodSignature.CreateInstance(
                returnType: EventRegistrationToken.ToValueTypeSignature(),
                parameterTypes: [new GenericParameterSignature(GenericParameterType.Type, 0)]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>RemoveEventHandler</c> for <see cref="EventRegistrationTokenTable1"/>.
    /// </summary>
    /// <param name="eventRegistrationTokenTableType">The input table type.</param>
    public MemberReference EventRegistrationTokenTableRemoveEventHandler(TypeSignature eventRegistrationTokenTableType)
    {
        return eventRegistrationTokenTableType
            .ToTypeDefOrRef()
            .CreateMemberReference("RemoveEventHandler"u8, MethodSignature.CreateInstance(
                returnType: _corLibTypeFactory.Boolean,
                parameterTypes: [
                    EventRegistrationToken.ToValueTypeSignature(),
                    new GenericParameterSignature(GenericParameterType.Type, 0).MakeByReferenceType()]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for the <see cref="EventHandler1EventSource"/>'s constructor.
    /// </summary>
    /// <param name="eventArgsType">The type of event arguments.</param>
    public MemberReference EventHandler1EventSource_ctor(TypeSignature eventArgsType)
    {
        return EventHandler1EventSource
            .MakeGenericReferenceType([eventArgsType])
            .ToTypeDefOrRef()
            .CreateConstructorReference(
                corLibTypeFactory: _corLibTypeFactory,
                parameterTypes: [WindowsRuntimeObjectReference.ToReferenceTypeSignature(), _corLibTypeFactory.Int32]);
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for the <see cref="EventHandler2EventSource"/>'s constructor.
    /// </summary>
    /// <param name="senderType">The sender type.</param>
    /// <param name="eventArgsType">The type of event arguments.</param>
    public MemberReference EventHandler2EventSource_ctor(TypeSignature senderType, TypeSignature eventArgsType)
    {
        return EventHandler2EventSource
            .MakeGenericReferenceType([senderType, eventArgsType])
            .ToTypeDefOrRef()
            .CreateConstructorReference(
                corLibTypeFactory: _corLibTypeFactory,
                parameterTypes: [WindowsRuntimeObjectReference.ToReferenceTypeSignature(), _corLibTypeFactory.Int32]);
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="VectorChangedEventHandler1EventSource"/>'s constructor.
    /// </summary>
    /// <param name="elementType">The type of elements in the observable vector.</param>
    public MemberReference VectorChangedEventHandler1EventSource_ctor(TypeSignature elementType)
    {
        return VectorChangedEventHandler1EventSource
            .MakeGenericReferenceType([elementType])
            .ToTypeDefOrRef()
            .CreateConstructorReference(
                corLibTypeFactory: _corLibTypeFactory,
                parameterTypes: [WindowsRuntimeObjectReference.ToReferenceTypeSignature(), _corLibTypeFactory.Int32]);
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="MapChangedEventHandler2EventSource"/>'s constructor.
    /// </summary>
    /// <param name="keyType">The type of keys in the observable map.</param>
    /// <param name="valueType">The type of values in the observable map.</param>
    public MemberReference MapChangedEventHandler2EventSource_ctor(TypeSignature keyType, TypeSignature valueType)
    {
        return MapChangedEventHandler2EventSource
            .MakeGenericReferenceType([keyType, valueType])
            .ToTypeDefOrRef()
            .CreateConstructorReference(
                corLibTypeFactory: _corLibTypeFactory,
                parameterTypes: [WindowsRuntimeObjectReference.ToReferenceTypeSignature(), _corLibTypeFactory.Int32]);
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for the <c>.ctor</c> method of a given nullable value type.
    /// </summary>
    /// <param name="valueType">The input value type.</param>
    public MemberReference Nullable1_ctor(TypeSignature valueType)
    {
        // Get the special delegate constructor taking the target and function pointer. We leverage this to create
        // a delegate instance that directly wraps our 'WindowsRuntimeObjectReference' object and 'Invoke' method.
        return Nullable1
            .MakeGenericValueType([valueType])
            .ToTypeDefOrRef()
            .CreateConstructorReference(
                corLibTypeFactory: _corLibTypeFactory,
                parameterTypes: [new GenericParameterSignature(GenericParameterType.Type, 0)]);
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="ReadOnlySpan{T}"/>'s constructor (of an SZ array type).
    /// </summary>
    public MemberReference ReadOnlySpan1_ctor(SzArrayTypeSignature arrayType)
    {
        return ReadOnlySpan1
            .MakeGenericValueType([arrayType.BaseType])
            .ToTypeDefOrRef()
            .CreateConstructorReference(
                _corLibTypeFactory,
                [new GenericParameterSignature(GenericParameterType.Type, 0).MakeSzArrayType()]);
    }

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Runtime.InteropServices.TypeMapAttribute{TTypeMapGroup}.TypeMapAttribute(string, System.Type, System.Type)"/>.
    /// </summary>
    /// <param name="typeMapGroup">The type map group to use.</param>
    public MemberReference TypeMapAttribute1_ctor_TrimTarget(TypeSignature typeMapGroup)
    {
        return TypeMapAttribute1
            .MakeGenericReferenceType([typeMapGroup])
            .ToTypeDefOrRef()
            .CreateConstructorReference(
                corLibTypeFactory: _corLibTypeFactory,
                parameterTypes: [
                    _corLibTypeFactory.String,
                    Type.ToReferenceTypeSignature(),
                    Type.ToReferenceTypeSignature()]);
    }

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.TypeReference"/> for <see cref="System.Runtime.InteropServices.TypeMapAssociationAttribute{TTypeMapGroup}.TypeMapAssociationAttribute(System.Type, System.Type)"/>.
    /// </summary>
    /// <param name="typeMapGroup">The type map group to use.</param>
    public MemberReference TypeMapAssociationAttribute1_ctor(TypeSignature typeMapGroup)
    {
        return TypeMapAssociationAttribute1
            .MakeGenericReferenceType([typeMapGroup])
            .ToTypeDefOrRef()
            .CreateConstructorReference(
                corLibTypeFactory: _corLibTypeFactory,
                parameterTypes: [
                    Type.ToReferenceTypeSignature(),
                    Type.ToReferenceTypeSignature()]);
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
            .CreateConstructorReference(
                corLibTypeFactory: _corLibTypeFactory,
                parameterTypes: [_corLibTypeFactory.Object, _corLibTypeFactory.IntPtr]);
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for the <c>Invoke</c> method of a given delegate type.
    /// </summary>
    /// <param name="delegateType">The input delegate type.</param>
    /// <param name="module">The <see cref="ModuleDefinition"/> to use to import <paramref name="delegateType"/> before resolving it.</param>
    public MemberReference DelegateInvoke(TypeSignature delegateType, ModuleDefinition module)
    {
        // Get the 'Invoke' method of the delegate type (this will remove the type arguments)
        MethodDefinition invokeMethod = delegateType.Resolve(module)!.GetMethod("Invoke"u8);

        // Create the actual member reference to use when emitting calls to the 'Invoke' method.
        // This has to be on the input (potentially constructed) delegate type, but not using
        // any constructed type arguments (as they're just derived from the constructed type).
        // For instance, if the input delegate type were an 'EventHandler<Foo, BarArgs>' generic
        // instantiation, this 'Invoke' reference would be 'void EventHandler<Foo, BarArgs>::Invoke(!0, !1)'.
        // This is also why the method reference can directly reuse the signature from the method definition.
        return delegateType.ToTypeDefOrRef().CreateMemberReference("Invoke"u8, invokeMethod.Signature!);
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Runtime.CompilerServices.ConditionalWeakTable{TKey, TValue}.GetOrCreateValue"/>.
    /// </summary>
    /// <param name="conditionalWeakTableType">The input table type.</param>
    public MemberReference ConditionalWeakTable2GetOrCreateValue(TypeSignature conditionalWeakTableType)
    {
        return conditionalWeakTableType
            .ToTypeDefOrRef()
            .CreateMemberReference("GetOrCreateValue"u8, MethodSignature.CreateInstance(
                returnType: new GenericParameterSignature(GenericParameterType.Type, 1),
                parameterTypes: [new GenericParameterSignature(GenericParameterType.Type, 0)]));
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <see cref="System.Runtime.CompilerServices.ConditionalWeakTable{TKey, TValue}.TryGetValue"/>.
    /// </summary>
    /// <param name="conditionalWeakTableType">The input table type.</param>
    public MemberReference ConditionalWeakTable2TryGetValue(TypeSignature conditionalWeakTableType)
    {
        return conditionalWeakTableType
            .ToTypeDefOrRef()
            .CreateMemberReference("TryGetValue"u8, MethodSignature.CreateInstance(
                returnType: _corLibTypeFactory.Boolean,
                parameterTypes: [
                    new GenericParameterSignature(GenericParameterType.Type, 0),
                    new GenericParameterSignature(GenericParameterType.Type, 1).MakeByReferenceType()]));
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <see cref="System.Runtime.CompilerServices.ConditionalWeakTable{TKey, TValue}.GetOrAdd{TArg}(TKey, Func{TKey, TArg, TValue}, TArg)"/>.
    /// </summary>
    /// <param name="conditionalWeakTableType">The input table type.</param>
    /// <param name="argType">The argument type.</param>
    public MethodSpecification ConditionalWeakTable2GetOrAdd(TypeSignature conditionalWeakTableType, TypeSignature argType)
    {
        return conditionalWeakTableType
            .ToTypeDefOrRef()
            .CreateMemberReference("GetOrAdd"u8, MethodSignature.CreateInstance(
                returnType: new GenericParameterSignature(GenericParameterType.Type, 1),
                genericParameterCount: 1,
                parameterTypes: [
                    new GenericParameterSignature(GenericParameterType.Type, 0),
                    Func3.MakeGenericReferenceType([
                        new GenericParameterSignature(GenericParameterType.Type, 0),
                        new GenericParameterSignature(GenericParameterType.Method, 0),
                        new GenericParameterSignature(GenericParameterType.Type, 1)]),
                    new GenericParameterSignature(GenericParameterType.Method, 0)]))
            .MakeGenericInstanceMethod([argType]);
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for the <c>.ctor</c> method of a <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}"/> type.
    /// </summary>
    /// <param name="keyValuePairType">The input <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}"/> type.</param>
    public MemberReference KeyValuePair2_ctor(TypeSignature keyValuePairType)
    {
        return keyValuePairType
            .ToTypeDefOrRef()
            .CreateConstructorReference(
                corLibTypeFactory: _corLibTypeFactory,
                parameterTypes: [
                    new GenericParameterSignature(GenericParameterType.Type, 0),
                    new GenericParameterSignature(GenericParameterType.Type, 1)]);
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Collections.Generic.IEnumerator{T}.Current"/>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference IEnumerator1get_Current(TypeSignature elementType)
    {
        return IEnumerator1
            .MakeGenericReferenceType([elementType])
            .ToTypeDefOrRef()
            .CreateMemberReference("get_Current"u8, MethodSignature.CreateInstance(new GenericParameterSignature(GenericParameterType.Type, 0)));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IIterableMethodsImpl&lt;T&gt;.First</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference IIterableMethodsImpl1First(TypeSignature elementType)
    {
        return IIterableMethodsImpl1
            .MakeGenericReferenceType([elementType])
            .ToTypeDefOrRef()
            .CreateMemberReference("First"u8, MethodSignature.CreateStatic(
                returnType: IEnumerator1.MakeGenericReferenceType([new GenericParameterSignature(GenericParameterType.Type, 0)]),
                parameterTypes: [WindowsRuntimeObjectReference.ToReferenceTypeSignature()]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IIteratorMethodsImpl&lt;T&gt;.Current</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference IIteratorMethodsImpl1Current(TypeSignature elementType)
    {
        return IIteratorMethodsImpl1
            .MakeGenericReferenceType([elementType])
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
            .MakeGenericReferenceType([elementType])
            .ToTypeDefOrRef()
            .CreateMemberReference("GetEnumerator"u8, MethodSignature.CreateInstance(
                returnType: IEnumerator1.MakeGenericReferenceType([new GenericParameterSignature(GenericParameterType.Type, 0)])));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.WindowsRuntimeInterfaceMarshaller.ConvertToUnmanaged</c>.
    /// </summary>
    /// <param name="interfaceType">The input interface type.</param>
    public MemberReference WindowsRuntimeInterfaceMarshallerConvertToUnmanaged(TypeSignature interfaceType)
    {
        return WindowsRuntimeInterfaceMarshaller1
            .MakeGenericReferenceType([interfaceType])
            .ToTypeDefOrRef()
            .CreateMemberReference("ConvertToUnmanaged"u8, MethodSignature.CreateStatic(
                returnType: WindowsRuntimeObjectReferenceValue.ToValueTypeSignature(),
                parameterTypes: [
                    new GenericParameterSignature(GenericParameterType.Type, 0),
                    Guid.ToValueTypeSignature().MakeByReferenceType()]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>Windows.Foundation.IAsyncActionWithProgress&lt;TProgress&gt;.get_Progress</c>.
    /// </summary>
    /// <param name="progressType">The input progress type.</param>
    public MemberReference IAsyncActionWithProgress1get_Progress(TypeSignature progressType)
    {
        return IAsyncActionWithProgress1
            .MakeGenericReferenceType([progressType])
            .ToTypeDefOrRef()
            .CreateMemberReference("get_Progress"u8, MethodSignature.CreateInstance(
                returnType: AsyncActionProgressHandler1.MakeGenericReferenceType([new GenericParameterSignature(GenericParameterType.Type, 0)])));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>Windows.Foundation.IAsyncActionWithProgress&lt;TProgress&gt;.set_Progress</c>.
    /// </summary>
    /// <param name="progressType">The input progress type.</param>
    public MemberReference IAsyncActionWithProgress1set_Progress(TypeSignature progressType)
    {
        return IAsyncActionWithProgress1
            .MakeGenericReferenceType([progressType])
            .ToTypeDefOrRef()
            .CreateMemberReference("set_Progress"u8, MethodSignature.CreateInstance(
                returnType: _corLibTypeFactory.Void,
                parameterTypes: [AsyncActionProgressHandler1.MakeGenericReferenceType([new GenericParameterSignature(GenericParameterType.Type, 0)])]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>Windows.Foundation.IAsyncActionWithProgress&lt;TProgress&gt;.get_Completed</c>.
    /// </summary>
    /// <param name="progressType">The input progress type.</param>
    public MemberReference IAsyncActionWithProgress1get_Completed(TypeSignature progressType)
    {
        return IAsyncActionWithProgress1
            .MakeGenericReferenceType([progressType])
            .ToTypeDefOrRef()
            .CreateMemberReference("get_Completed"u8, MethodSignature.CreateInstance(
                returnType: AsyncActionWithProgressCompletedHandler1.MakeGenericReferenceType([new GenericParameterSignature(GenericParameterType.Type, 0)])));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>Windows.Foundation.IAsyncActionWithProgress&lt;TProgress&gt;.set_Completed</c>.
    /// </summary>
    /// <param name="progressType">The input progress type.</param>
    public MemberReference IAsyncActionWithProgress1set_Completed(TypeSignature progressType)
    {
        return IAsyncActionWithProgress1
            .MakeGenericReferenceType([progressType])
            .ToTypeDefOrRef()
            .CreateMemberReference("set_Completed"u8, MethodSignature.CreateInstance(
                returnType: _corLibTypeFactory.Void,
                parameterTypes: [AsyncActionWithProgressCompletedHandler1.MakeGenericReferenceType([new GenericParameterSignature(GenericParameterType.Type, 0)])]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>Windows.Foundation.IAsyncActionWithProgress&lt;TProgress&gt;.GetResults</c>.
    /// </summary>
    /// <param name="progressType">The input progress type.</param>
    public MemberReference IAsyncActionWithProgress1GetResults(TypeSignature progressType)
    {
        return IAsyncActionWithProgress1
            .MakeGenericReferenceType([progressType])
            .ToTypeDefOrRef()
            .CreateMemberReference("GetResults"u8, MethodSignature.CreateInstance(_corLibTypeFactory.Void));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IAsyncActionWithProgressMethodsImpl&lt;TProgress&gt;.Progress</c>.
    /// </summary>
    /// <param name="progressType">The input progress type.</param>
    public MemberReference IAsyncActionWithProgressMethodsImpl1get_Progress(TypeSignature progressType)
    {
        return IAsyncActionWithProgressMethodsImpl1
            .MakeGenericReferenceType([progressType])
            .ToTypeDefOrRef()
            .CreateMemberReference("Progress"u8, MethodSignature.CreateStatic(
                returnType: AsyncActionProgressHandler1.MakeGenericReferenceType([new GenericParameterSignature(GenericParameterType.Type, 0)]),
                parameterTypes: [WindowsRuntimeObjectReference.ToReferenceTypeSignature()]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IAsyncActionWithProgressMethodsImpl&lt;TProgress&gt;.Progress</c>.
    /// </summary>
    /// <param name="resultType">The input result type.</param>
    public MemberReference IAsyncActionWithProgressMethodsImpl1set_Progress(TypeSignature resultType)
    {
        return IAsyncActionWithProgressMethodsImpl1
            .MakeGenericReferenceType([resultType])
            .ToTypeDefOrRef()
            .CreateMemberReference("Progress"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Void,
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    AsyncActionProgressHandler1.MakeGenericReferenceType([new GenericParameterSignature(GenericParameterType.Type, 0)])]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IAsyncActionWithProgressMethodsImpl&lt;TProgress&gt;.Completed</c>.
    /// </summary>
    /// <param name="progressType">The input progress type.</param>
    public MemberReference IAsyncActionWithProgressMethodsImpl1get_Completed(TypeSignature progressType)
    {
        return IAsyncActionWithProgressMethodsImpl1
            .MakeGenericReferenceType([progressType])
            .ToTypeDefOrRef()
            .CreateMemberReference("Completed"u8, MethodSignature.CreateStatic(
                returnType: AsyncActionWithProgressCompletedHandler1.MakeGenericReferenceType([new GenericParameterSignature(GenericParameterType.Type, 0)]),
                parameterTypes: [WindowsRuntimeObjectReference.ToReferenceTypeSignature()]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IAsyncActionWithProgressMethodsImpl&lt;TProgress&gt;.Completed</c>.
    /// </summary>
    /// <param name="resultType">The input result type.</param>
    public MemberReference IAsyncActionWithProgressMethodsImpl1set_Completed(TypeSignature resultType)
    {
        return IAsyncActionWithProgressMethodsImpl1
            .MakeGenericReferenceType([resultType])
            .ToTypeDefOrRef()
            .CreateMemberReference("Completed"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Void,
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    AsyncActionWithProgressCompletedHandler1.MakeGenericReferenceType([new GenericParameterSignature(GenericParameterType.Type, 0)])]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IAsyncActionWithProgressMethodsImpl&lt;TProgress&gt;.GetResults()</c>.
    /// </summary>
    /// <param name="resultType">The input result type.</param>
    public MemberReference IAsyncActionWithProgressMethodsImpl1GetResults(TypeSignature resultType)
    {
        return IAsyncActionWithProgressMethodsImpl1
            .MakeGenericReferenceType([resultType])
            .ToTypeDefOrRef()
            .CreateMemberReference("GetResults"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Void,
                parameterTypes: [WindowsRuntimeObjectReference.ToReferenceTypeSignature()]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>Windows.Foundation.IAsyncOperation&lt;TResult&gt;.get_Completed</c>.
    /// </summary>
    /// <param name="resultType">The input result type.</param>
    public MemberReference IAsyncOperation1get_Completed(TypeSignature resultType)
    {
        return IAsyncOperation1
            .MakeGenericReferenceType([resultType])
            .ToTypeDefOrRef()
            .CreateMemberReference("get_Completed"u8, MethodSignature.CreateInstance(
                returnType: AsyncOperationCompletedHandler1.MakeGenericReferenceType([new GenericParameterSignature(GenericParameterType.Type, 0)])));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>Windows.Foundation.IAsyncOperation&lt;TResult&gt;.set_Completed</c>.
    /// </summary>
    /// <param name="resultType">The input element type.</param>
    public MemberReference IAsyncOperation1set_Completed(TypeSignature resultType)
    {
        return IAsyncOperation1
            .MakeGenericReferenceType([resultType])
            .ToTypeDefOrRef()
            .CreateMemberReference("set_Completed"u8, MethodSignature.CreateInstance(
                returnType: _corLibTypeFactory.Void,
                parameterTypes: [AsyncOperationCompletedHandler1.MakeGenericReferenceType([new GenericParameterSignature(GenericParameterType.Type, 0)])]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>Windows.Foundation.IAsyncOperation&lt;TResult&gt;.GetResults</c>.
    /// </summary>
    /// <param name="resultType">The input element type.</param>
    public MemberReference IAsyncOperation1GetResults(TypeSignature resultType)
    {
        return IAsyncOperation1
            .MakeGenericReferenceType([resultType])
            .ToTypeDefOrRef()
            .CreateMemberReference("GetResults"u8, MethodSignature.CreateInstance(
                returnType: new GenericParameterSignature(GenericParameterType.Type, 0)));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IAsyncOperationMethodsImpl&lt;TResult&gt;.Completed</c>.
    /// </summary>
    /// <param name="resultType">The input result type.</param>
    public MemberReference IAsyncOperationMethodsImpl1get_Completed(TypeSignature resultType)
    {
        return IAsyncOperationMethodsImpl1
            .MakeGenericReferenceType([resultType])
            .ToTypeDefOrRef()
            .CreateMemberReference("Completed"u8, MethodSignature.CreateStatic(
                returnType: AsyncOperationCompletedHandler1.MakeGenericReferenceType([new GenericParameterSignature(GenericParameterType.Type, 0)]),
                parameterTypes: [WindowsRuntimeObjectReference.ToReferenceTypeSignature()]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IAsyncOperationMethodsImpl&lt;TResult&gt;.Completed</c>.
    /// </summary>
    /// <param name="resultType">The input result type.</param>
    public MemberReference IAsyncOperationMethodsImpl1set_Completed(TypeSignature resultType)
    {
        return IAsyncOperationMethodsImpl1
            .MakeGenericReferenceType([resultType])
            .ToTypeDefOrRef()
            .CreateMemberReference("Completed"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Void,
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    AsyncOperationCompletedHandler1.MakeGenericReferenceType([new GenericParameterSignature(GenericParameterType.Type, 0)])]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IAsyncOperationMethodsImpl&lt;TResult&gt;.GetResults</c>.
    /// </summary>
    /// <param name="resultType">The input result type.</param>
    public MemberReference IAsyncOperationMethodsImpl1GetResults(TypeSignature resultType)
    {
        return IAsyncOperationMethodsImpl1
            .MakeGenericReferenceType([resultType])
            .ToTypeDefOrRef()
            .CreateMemberReference("GetResults"u8, MethodSignature.CreateStatic(
                returnType: new GenericParameterSignature(GenericParameterType.Type, 0),
                parameterTypes: [WindowsRuntimeObjectReference.ToReferenceTypeSignature()]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>Windows.Foundation.IAsyncOperationWithProgress&lt;TResult, TProgress&gt;.get_Progress</c>.
    /// </summary>
    /// <param name="resultType">The input result type.</param>
    /// <param name="progressType">The input progress type.</param>
    public MemberReference IAsyncOperationWithProgress2get_Progress(TypeSignature resultType, TypeSignature progressType)
    {
        return IAsyncOperationWithProgress2
            .MakeGenericReferenceType([resultType, progressType])
            .ToTypeDefOrRef()
            .CreateMemberReference("get_Progress"u8, MethodSignature.CreateInstance(
                returnType: AsyncOperationProgressHandler2.MakeGenericReferenceType([
                    new GenericParameterSignature(GenericParameterType.Type, 0),
                    new GenericParameterSignature(GenericParameterType.Type, 1)])));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>Windows.Foundation.IAsyncOperationWithProgress&lt;TResult, TProgress&gt;.set_Progress</c>.
    /// </summary>
    /// <param name="resultType">The input result type.</param>
    /// <param name="progressType">The input progress type.</param>
    public MemberReference IAsyncOperationWithProgress2set_Progress(TypeSignature resultType, TypeSignature progressType)
    {
        return IAsyncOperationWithProgress2
            .MakeGenericReferenceType([resultType, progressType])
            .ToTypeDefOrRef()
            .CreateMemberReference("set_Progress"u8, MethodSignature.CreateInstance(
                returnType: _corLibTypeFactory.Void,
                parameterTypes: [AsyncOperationProgressHandler2.MakeGenericReferenceType([
                    new GenericParameterSignature(GenericParameterType.Type, 0),
                    new GenericParameterSignature(GenericParameterType.Type, 1)])]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>Windows.Foundation.IAsyncOperationWithProgress&lt;TResult, TProgress&gt;.get_Completed</c>.
    /// </summary>
    /// <param name="resultType">The input result type.</param>
    /// <param name="progressType">The input progress type.</param>
    public MemberReference IAsyncOperationWithProgress2get_Completed(TypeSignature resultType, TypeSignature progressType)
    {
        return IAsyncOperationWithProgress2
            .MakeGenericReferenceType([resultType, progressType])
            .ToTypeDefOrRef()
            .CreateMemberReference("get_Completed"u8, MethodSignature.CreateInstance(
                returnType: AsyncOperationWithProgressCompletedHandler2.MakeGenericReferenceType([
                    new GenericParameterSignature(GenericParameterType.Type, 0),
                    new GenericParameterSignature(GenericParameterType.Type, 1)])));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>Windows.Foundation.IAsyncOperationWithProgress&lt;TResult, TProgress&gt;.set_Completed</c>.
    /// </summary>
    /// <param name="resultType">The input result type.</param>
    /// <param name="progressType">The input progress type.</param>
    public MemberReference IAsyncOperationWithProgress2set_Completed(TypeSignature resultType, TypeSignature progressType)
    {
        return IAsyncOperationWithProgress2
            .MakeGenericReferenceType([resultType, progressType])
            .ToTypeDefOrRef()
            .CreateMemberReference("set_Completed"u8, MethodSignature.CreateInstance(
                returnType: _corLibTypeFactory.Void,
                parameterTypes: [AsyncOperationWithProgressCompletedHandler2.MakeGenericReferenceType([
                    new GenericParameterSignature(GenericParameterType.Type, 0),
                    new GenericParameterSignature(GenericParameterType.Type, 1)])]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>Windows.Foundation.IAsyncOperationWithProgress&lt;TResult, TProgress&gt;.GetResults</c>.
    /// </summary>
    /// <param name="resultType">The input result type.</param>
    /// <param name="progressType">The input progress type.</param>
    public MemberReference IAsyncOperationWithProgress2GetResults(TypeSignature resultType, TypeSignature progressType)
    {
        return IAsyncOperationWithProgress2
            .MakeGenericReferenceType([resultType, progressType])
            .ToTypeDefOrRef()
            .CreateMemberReference("GetResults"u8, MethodSignature.CreateInstance(
                returnType: new GenericParameterSignature(GenericParameterType.Type, 0)));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IAsyncOperationWithProgressMethodsImpl&lt;TResult, TProgress&gt;.Progress</c>.
    /// </summary>
    /// <param name="resultType">The input result type.</param>
    /// <param name="progressType">The input progress type.</param>
    public MemberReference IAsyncOperationWithProgressMethodsImpl2get_Progress(TypeSignature resultType, TypeSignature progressType)
    {
        return IAsyncOperationWithProgressMethodsImpl2
            .MakeGenericReferenceType([resultType, progressType])
            .ToTypeDefOrRef()
            .CreateMemberReference("Progress"u8, MethodSignature.CreateStatic(
                returnType: AsyncOperationProgressHandler2.MakeGenericReferenceType([
                    new GenericParameterSignature(GenericParameterType.Type, 0),
                    new GenericParameterSignature(GenericParameterType.Type, 1)]),
                parameterTypes: [WindowsRuntimeObjectReference.ToReferenceTypeSignature()]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IAsyncOperationWithProgressMethodsImpl&lt;TResult, TProgress&gt;.Progress</c>.
    /// </summary>
    /// <param name="resultType">The input result type.</param>
    /// <param name="progressType">The input progress type.</param>
    public MemberReference IAsyncOperationWithProgressMethodsImpl2set_Progress(TypeSignature resultType, TypeSignature progressType)
    {
        return IAsyncOperationWithProgressMethodsImpl2
            .MakeGenericReferenceType([resultType, progressType])
            .ToTypeDefOrRef()
            .CreateMemberReference("Progress"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Void,
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    AsyncOperationProgressHandler2.MakeGenericReferenceType([
                        new GenericParameterSignature(GenericParameterType.Type, 0),
                        new GenericParameterSignature(GenericParameterType.Type, 1)])]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IAsyncOperationWithProgressMethodsImpl&lt;TResult, TProgress&gt;.Completed</c>.
    /// </summary>
    /// <param name="resultType">The input result type.</param>
    /// <param name="progressType">The input progress type.</param>
    public MemberReference IAsyncOperationWithProgressMethodsImpl2get_Completed(TypeSignature resultType, TypeSignature progressType)
    {
        return IAsyncOperationWithProgressMethodsImpl2
            .MakeGenericReferenceType([resultType, progressType])
            .ToTypeDefOrRef()
            .CreateMemberReference("Completed"u8, MethodSignature.CreateStatic(
                returnType: AsyncOperationWithProgressCompletedHandler2.MakeGenericReferenceType([
                    new GenericParameterSignature(GenericParameterType.Type, 0),
                    new GenericParameterSignature(GenericParameterType.Type, 1)]),
                parameterTypes: [WindowsRuntimeObjectReference.ToReferenceTypeSignature()]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IAsyncOperationWithProgressMethodsImpl&lt;TResult, TProgress&gt;.Completed</c>.
    /// </summary>
    /// <param name="resultType">The input result type.</param>
    /// <param name="progressType">The input progress type.</param>
    public MemberReference IAsyncOperationWithProgressMethodsImpl2set_Completed(TypeSignature resultType, TypeSignature progressType)
    {
        return IAsyncOperationWithProgressMethodsImpl2
            .MakeGenericReferenceType([resultType, progressType])
            .ToTypeDefOrRef()
            .CreateMemberReference("Completed"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Void,
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    AsyncOperationWithProgressCompletedHandler2.MakeGenericReferenceType([
                        new GenericParameterSignature(GenericParameterType.Type, 0),
                        new GenericParameterSignature(GenericParameterType.Type, 1)])]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IAsyncOperationWithProgressMethodsImpl&lt;TResult, TProgress&gt;.GetResults()</c>.
    /// </summary>
    /// <param name="resultType">The input result type.</param>
    /// <param name="progressType">The input progress type.</param>
    public MemberReference IAsyncOperationWithProgressMethodsImpl2GetResults(TypeSignature resultType, TypeSignature progressType)
    {
        return IAsyncOperationWithProgressMethodsImpl2
            .MakeGenericReferenceType([resultType, progressType])
            .ToTypeDefOrRef()
            .CreateMemberReference("GetResults"u8, MethodSignature.CreateStatic(
                returnType: new GenericParameterSignature(GenericParameterType.Type, 0),
                parameterTypes: [WindowsRuntimeObjectReference.ToReferenceTypeSignature()]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IEnumerableAdapter&lt;T&gt;.First</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference IEnumerableAdapter1First(TypeSignature elementType)
    {
        return IEnumerableAdapter1
            .MakeGenericReferenceType([elementType])
            .ToTypeDefOrRef()
            .CreateMemberReference("First"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Void,
                parameterTypes: [
                    IEnumerable1.MakeGenericReferenceType([new GenericParameterSignature(GenericParameterType.Type, 0)]),
                    Guid.ToValueTypeSignature().MakeByReferenceType(),
                    _corLibTypeFactory.Void.MakePointerType().MakePointerType()]));
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.IEnumerableMethods&lt;T&gt;.GetEnumerator</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    /// <param name="iterableMethods">The <see cref="IIterableMethodsImpl1"/> type.</param>
    public MethodSpecification IEnumerableMethods1GetEnumerator(TypeSignature elementType, TypeSignature iterableMethods)
    {
        return IEnumerableMethods1
            .MakeGenericReferenceType([elementType])
            .ToTypeDefOrRef()
            .CreateMemberReference("GetEnumerator"u8, MethodSignature.CreateStatic(
                returnType: IEnumerator1.MakeGenericReferenceType([new GenericParameterSignature(GenericParameterType.Type, 0)]),
                genericParameterCount: 1,
                parameterTypes: [WindowsRuntimeObjectReference.ToReferenceTypeSignature()]))
            .MakeGenericInstanceMethod([iterableMethods]);
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IEnumeratorAdapter&lt;T&gt;.GetInstance</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference IEnumeratorAdapter1GetInstance(TypeSignature elementType)
    {
        return IEnumeratorAdapter1
            .MakeGenericReferenceType([elementType])
            .ToTypeDefOrRef()
            .CreateMemberReference("GetInstance"u8, MethodSignature.CreateStatic(
                returnType: IEnumeratorAdapter1.MakeGenericReferenceType([new GenericParameterSignature(GenericParameterType.Type, 0)]),
                parameterTypes: [IEnumerator1.MakeGenericReferenceType([new GenericParameterSignature(GenericParameterType.Type, 0)])]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IEnumeratorAdapter&lt;T&gt;.Current</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference IEnumeratorAdapter1get_Current(TypeSignature elementType)
    {
        return IEnumeratorAdapter1
            .MakeGenericReferenceType([elementType])
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
            .MakeGenericReferenceType([elementType])
            .ToTypeDefOrRef()
            .CreateMemberReference("get_HasCurrent"u8, MethodSignature.CreateInstance(
                returnType: _corLibTypeFactory.Boolean));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IEnumeratorAdapter&lt;T&gt;.MoveNext</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference IEnumeratorAdapter1MoveNext(TypeSignature elementType)
    {
        return IEnumeratorAdapter1
            .MakeGenericReferenceType([elementType])
            .ToTypeDefOrRef()
            .CreateMemberReference("MoveNext"u8, MethodSignature.CreateInstance(
                returnType: _corLibTypeFactory.Boolean));
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.IEnumeratorAdapterBlittableValueTypeExtensions.GetMany&lt;T&gt;</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MethodSpecification IEnumeratorAdapterBlittableValueTypeGetMany(TypeSignature elementType)
    {
        return IEnumeratorAdapterBlittableValueTypeExtensions
            .CreateMemberReference("GetMany"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.UInt32,
                genericParameterCount: 1,
                parameterTypes: [
                    IEnumeratorAdapter1.MakeGenericReferenceType([new GenericParameterSignature(GenericParameterType.Method, 0)]),
                    _corLibTypeFactory.UInt32,
                    new GenericParameterSignature(GenericParameterType.Method, 0).MakePointerType()]))
            .MakeGenericInstanceMethod([elementType]);
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.IEnumeratorAdapterUnmanagedValueTypeExtensions.GetMany&lt;T, TAbi, TElementMarshaller&gt;</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    /// <param name="abiType">The ABI type.</param>
    /// <param name="elementMarshallerType">The element marshaller type.</param>
    public MethodSpecification IEnumeratorAdapterUnmanagedValueTypeGetMany(TypeSignature elementType, TypeSignature abiType, TypeSignature elementMarshallerType)
    {
        return IEnumeratorAdapterUnmanagedValueTypeExtensions
            .CreateMemberReference("GetMany"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.UInt32,
                genericParameterCount: 3,
                parameterTypes: [
                    IEnumeratorAdapter1.MakeGenericReferenceType([new GenericParameterSignature(GenericParameterType.Method, 0)]),
                    _corLibTypeFactory.UInt32,
                    new GenericParameterSignature(GenericParameterType.Method, 1).MakePointerType()]))
            .MakeGenericInstanceMethod([elementType, abiType, elementMarshallerType]);
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.IEnumeratorAdapterManagedValueTypeExtensions.GetMany&lt;T, TAbi, TElementMarshaller&gt;</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    /// <param name="abiType">The ABI type.</param>
    /// <param name="elementMarshallerType">The element marshaller type.</param>
    public MethodSpecification IEnumeratorAdapterManagedValueTypeGetMany(TypeSignature elementType, TypeSignature abiType, TypeSignature elementMarshallerType)
    {
        return IEnumeratorAdapterManagedValueTypeExtensions
            .CreateMemberReference("GetMany"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.UInt32,
                genericParameterCount: 3,
                parameterTypes: [
                    IEnumeratorAdapter1.MakeGenericReferenceType([new GenericParameterSignature(GenericParameterType.Method, 0)]),
                    _corLibTypeFactory.UInt32,
                    new GenericParameterSignature(GenericParameterType.Method, 1).MakePointerType()]))
            .MakeGenericInstanceMethod([elementType, abiType, elementMarshallerType]);
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.IEnumeratorAdapterKeyValuePairTypeExtensions.GetMany&lt;TKey, TValue, TElementMarshaller&gt;</c>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    /// <param name="elementMarshallerType">The element marshaller type.</param>
    public MethodSpecification IEnumeratorAdapterKeyValuePairTypeGetMany(TypeSignature keyType, TypeSignature valueType, TypeSignature elementMarshallerType)
    {
        return IEnumeratorAdapterKeyValuePairTypeExtensions
            .CreateMemberReference("GetMany"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.UInt32,
                genericParameterCount: 3,
                parameterTypes: [
                    IEnumeratorAdapter1.MakeGenericReferenceType([KeyValuePair2.MakeGenericValueType([
                        new GenericParameterSignature(GenericParameterType.Method, 0),
                        new GenericParameterSignature(GenericParameterType.Method, 1)])]),
                    _corLibTypeFactory.UInt32,
                    _corLibTypeFactory.Void.MakePointerType().MakePointerType()]))
            .MakeGenericInstanceMethod([keyType, valueType, elementMarshallerType]);
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.IEnumeratorAdapterNullableTypeExtensions.GetMany&lt;T, TElementMarshaller&gt;</c>.
    /// </summary>
    /// <param name="underlyingType">The underlying value type.</param>
    /// <param name="elementMarshallerType">The element marshaller type.</param>
    public MethodSpecification IEnumeratorAdapterNullableTypeGetMany(TypeSignature underlyingType, TypeSignature elementMarshallerType)
    {
        return IEnumeratorAdapterNullableTypeExtensions
            .CreateMemberReference("GetMany"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.UInt32,
                genericParameterCount: 2,
                parameterTypes: [
                    IEnumeratorAdapter1.MakeGenericReferenceType([Nullable1.MakeGenericValueType([new GenericParameterSignature(GenericParameterType.Method, 0)])]),
                    _corLibTypeFactory.UInt32,
                    _corLibTypeFactory.Void.MakePointerType().MakePointerType()]))
            .MakeGenericInstanceMethod(underlyingType, elementMarshallerType);
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.IEnumeratorAdapterReferenceTypeExtensions.GetMany&lt;T, TElementMarshaller&gt;</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    /// <param name="elementMarshallerType">The element marshaller type.</param>
    public MethodSpecification IEnumeratorAdapterReferenceTypeGetMany(TypeSignature elementType, TypeSignature elementMarshallerType)
    {
        return IEnumeratorAdapterReferenceTypeExtensions
            .CreateMemberReference("GetMany"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.UInt32,
                genericParameterCount: 2,
                parameterTypes: [
                    IEnumeratorAdapter1.MakeGenericReferenceType([new GenericParameterSignature(GenericParameterType.Method, 0)]),
                    _corLibTypeFactory.UInt32,
                    _corLibTypeFactory.Void.MakePointerType().MakePointerType()]))
            .MakeGenericInstanceMethod([elementType, elementMarshallerType]);
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IVectorMethodsImpl&lt;T&gt;.GetAt</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference IVectorMethodsImpl1GetAt(TypeSignature elementType)
    {
        return IVectorMethodsImpl1
            .MakeGenericReferenceType([elementType])
            .ToTypeDefOrRef()
            .CreateMemberReference("GetAt"u8, MethodSignature.CreateStatic(
                returnType: new GenericParameterSignature(GenericParameterType.Type, 0),
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    _corLibTypeFactory.UInt32]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IVectorMethodsImpl&lt;T&gt;.SetAt</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference IVectorMethodsImpl1SetAt(TypeSignature elementType)
    {
        return IVectorMethodsImpl1
            .MakeGenericReferenceType([elementType])
            .ToTypeDefOrRef()
            .CreateMemberReference("SetAt"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Void,
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    _corLibTypeFactory.UInt32,
                    new GenericParameterSignature(GenericParameterType.Type, 0)]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IVectorMethodsImpl&lt;T&gt;.Append</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference IVectorMethodsImpl1Append(TypeSignature elementType)
    {
        return IVectorMethodsImpl1
            .MakeGenericReferenceType([elementType])
            .ToTypeDefOrRef()
            .CreateMemberReference("Append"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Void,
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
            .MakeGenericReferenceType([elementType])
            .ToTypeDefOrRef()
            .CreateMemberReference("IndexOf"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Boolean,
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    new GenericParameterSignature(GenericParameterType.Type, 0),
                    _corLibTypeFactory.UInt32.MakeByReferenceType()]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IVectorMethodsImpl&lt;T&gt;.InsertAt</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference IVectorMethodsImpl1InsertAt(TypeSignature elementType)
    {
        return IVectorMethodsImpl1
            .MakeGenericReferenceType([elementType])
            .ToTypeDefOrRef()
            .CreateMemberReference("InsertAt"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Void,
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    _corLibTypeFactory.UInt32,
                    new GenericParameterSignature(GenericParameterType.Type, 0)]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IVectorViewMethods&lt;T&gt;.GetAt</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference IVectorViewMethods1GetAt(TypeSignature elementType)
    {
        return IVectorViewMethods1
            .MakeGenericReferenceType([elementType])
            .ToTypeDefOrRef()
            .CreateMemberReference("GetAt"u8, MethodSignature.CreateStatic(
                returnType: new GenericParameterSignature(GenericParameterType.Type, 0),
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    _corLibTypeFactory.UInt32]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Collections.Generic.ICollection{T}.Count"/>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference ICollection1get_Count(TypeSignature elementType)
    {
        return ICollection1
            .MakeGenericReferenceType([elementType])
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
            .MakeGenericReferenceType([elementType])
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
            .MakeGenericReferenceType([elementType])
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
            .MakeGenericReferenceType([elementType])
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
            .MakeGenericReferenceType([elementType])
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
            .MakeGenericReferenceType([elementType])
            .ToTypeDefOrRef()
            .CreateMemberReference("CopyTo"u8, MethodSignature.CreateInstance(
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
            .MakeGenericReferenceType([elementType])
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
            .MakeGenericReferenceType([elementType])
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
            .MakeGenericReferenceType([elementType])
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
            .MakeGenericReferenceType([elementType])
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
            .MakeGenericReferenceType([elementType])
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
            .MakeGenericReferenceType([elementType])
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
            .MakeGenericReferenceType([elementType])
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
            .MakeGenericReferenceType([elementType])
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
            .MakeGenericReferenceType([elementType])
            .ToTypeDefOrRef()
            .CreateMemberReference("Item"u8, MethodSignature.CreateStatic(
                returnType: new GenericParameterSignature(GenericParameterType.Type, 0),
                genericParameterCount: 1,
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    _corLibTypeFactory.Int32]))
            .MakeGenericInstanceMethod([vectorMethods.ToReferenceTypeSignature()]);
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.IListMethods&lt;T&gt;.Item</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    /// <param name="vectorMethods">The <see cref="IVectorMethods1"/> type.</param>
    public MethodSpecification IListMethods1set_Item(TypeSignature elementType, TypeDefinition vectorMethods)
    {
        return IListMethods1
            .MakeGenericReferenceType([elementType])
            .ToTypeDefOrRef()
            .CreateMemberReference("Item"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Void,
                genericParameterCount: 1,
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    _corLibTypeFactory.Int32,
                    new GenericParameterSignature(GenericParameterType.Type, 0)]))
            .MakeGenericInstanceMethod([vectorMethods.ToReferenceTypeSignature()]);
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.IListMethods&lt;T&gt;.Add</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    /// <param name="vectorMethods">The <see cref="IVectorMethods1"/> type.</param>
    public MethodSpecification IListMethods1Add(TypeSignature elementType, TypeDefinition vectorMethods)
    {
        return IListMethods1
            .MakeGenericReferenceType([elementType])
            .ToTypeDefOrRef()
            .CreateMemberReference("Add"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Void,
                genericParameterCount: 1,
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    new GenericParameterSignature(GenericParameterType.Type, 0)]))
            .MakeGenericInstanceMethod([vectorMethods.ToReferenceTypeSignature()]);
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.IListMethods&lt;T&gt;.Contains</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    /// <param name="vectorMethods">The <see cref="IVectorMethods1"/> type.</param>
    public MethodSpecification IListMethods1Contains(TypeSignature elementType, TypeDefinition vectorMethods)
    {
        return IListMethods1
            .MakeGenericReferenceType([elementType])
            .ToTypeDefOrRef()
            .CreateMemberReference("Contains"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Boolean,
                genericParameterCount: 1,
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    new GenericParameterSignature(GenericParameterType.Type, 0)]))
            .MakeGenericInstanceMethod([vectorMethods.ToReferenceTypeSignature()]);
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.IListMethods&lt;T&gt;.CopyTo</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    /// <param name="vectorMethods">The <see cref="IVectorMethods1"/> type.</param>
    public MethodSpecification IListMethods1CopyTo(TypeSignature elementType, TypeDefinition vectorMethods)
    {
        return IListMethods1
            .MakeGenericReferenceType([elementType])
            .ToTypeDefOrRef()
            .CreateMemberReference("CopyTo"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Void,
                genericParameterCount: 1,
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    new GenericParameterSignature(GenericParameterType.Type, 0).MakeSzArrayType(),
                    _corLibTypeFactory.Int32]))
            .MakeGenericInstanceMethod([vectorMethods.ToReferenceTypeSignature()]);
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.IListMethods&lt;T&gt;.Remove</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    /// <param name="vectorMethods">The <see cref="IVectorMethods1"/> type.</param>
    public MethodSpecification IListMethods1Remove(TypeSignature elementType, TypeDefinition vectorMethods)
    {
        return IListMethods1
            .MakeGenericReferenceType([elementType])
            .ToTypeDefOrRef()
            .CreateMemberReference("Remove"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Boolean,
                genericParameterCount: 1,
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    new GenericParameterSignature(GenericParameterType.Type, 0)]))
            .MakeGenericInstanceMethod([vectorMethods.ToReferenceTypeSignature()]);
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.IListMethods&lt;T&gt;.IndexOf</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    /// <param name="vectorMethods">The <see cref="IVectorMethods1"/> type.</param>
    public MethodSpecification IListMethods1IndexOf(TypeSignature elementType, TypeDefinition vectorMethods)
    {
        return IListMethods1
            .MakeGenericReferenceType([elementType])
            .ToTypeDefOrRef()
            .CreateMemberReference("IndexOf"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Int32,
                genericParameterCount: 1,
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    new GenericParameterSignature(GenericParameterType.Type, 0)]))
            .MakeGenericInstanceMethod([vectorMethods.ToReferenceTypeSignature()]);
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.IListMethods&lt;T&gt;.Insert</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    /// <param name="vectorMethods">The <see cref="IVectorMethods1"/> type.</param>
    public MethodSpecification IListMethods1Insert(TypeSignature elementType, TypeDefinition vectorMethods)
    {
        return IListMethods1
            .MakeGenericReferenceType([elementType])
            .ToTypeDefOrRef()
            .CreateMemberReference("Insert"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Void,
                genericParameterCount: 1,
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    _corLibTypeFactory.Int32,
                    new GenericParameterSignature(GenericParameterType.Type, 0)]))
            .MakeGenericInstanceMethod([vectorMethods.ToReferenceTypeSignature()]);
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IListAdapter&lt;T&gt;.GetAt</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference IListAdapter1GetAt(TypeSignature elementType)
    {
        return IListAdapter1
            .MakeGenericReferenceType([elementType])
            .ToTypeDefOrRef()
            .CreateMemberReference("GetAt"u8, MethodSignature.CreateStatic(
                returnType: new GenericParameterSignature(GenericParameterType.Type, 0),
                parameterTypes: [
                    IList1.MakeGenericReferenceType([new GenericParameterSignature(GenericParameterType.Type, 0)]),
                    _corLibTypeFactory.UInt32]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IListAdapter&lt;T&gt;.Size</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference IListAdapter1Size(TypeSignature elementType)
    {
        return IListAdapter1
            .MakeGenericReferenceType([elementType])
            .ToTypeDefOrRef()
            .CreateMemberReference("Size"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.UInt32,
                parameterTypes: [IList1.MakeGenericReferenceType([new GenericParameterSignature(GenericParameterType.Type, 0)])]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IListAdapter&lt;T&gt;.GetView</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference IListAdapter1GetView(TypeSignature elementType)
    {
        return IListAdapter1
            .MakeGenericReferenceType([elementType])
            .ToTypeDefOrRef()
            .CreateMemberReference("GetView"u8, MethodSignature.CreateStatic(
                returnType: IReadOnlyList1.MakeGenericReferenceType([new GenericParameterSignature(GenericParameterType.Type, 0)]),
                parameterTypes: [IList1.MakeGenericReferenceType([new GenericParameterSignature(GenericParameterType.Type, 0)])]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IListAdapter&lt;T&gt;.IndexOf</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference IListAdapter1IndexOf(TypeSignature elementType)
    {
        return IListAdapter1
            .MakeGenericReferenceType([elementType])
            .ToTypeDefOrRef()
            .CreateMemberReference("IndexOf"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Boolean,
                parameterTypes: [
                    IList1.MakeGenericReferenceType([new GenericParameterSignature(GenericParameterType.Type, 0)]),
                    new GenericParameterSignature(GenericParameterType.Type, 0),
                    _corLibTypeFactory.UInt32.MakeByReferenceType()]));
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.IListAdapterBlittableValueTypeExtensions.GetMany&lt;T&gt;</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MethodSpecification IListAdapterBlittableValueTypeGetMany(TypeSignature elementType)
    {
        return IListAdapterBlittableValueTypeExtensions
            .CreateMemberReference("GetMany"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.UInt32,
                genericParameterCount: 1,
                parameterTypes: [
                    IList1.MakeGenericReferenceType([new GenericParameterSignature(GenericParameterType.Method, 0)]),
                    _corLibTypeFactory.UInt32,
                    _corLibTypeFactory.UInt32,
                    new GenericParameterSignature(GenericParameterType.Method, 0).MakePointerType()]))
            .MakeGenericInstanceMethod([elementType]);
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.IListAdapterUnmanagedValueTypeExtensions.GetMany&lt;T, TAbi, TElementMarshaller&gt;</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    /// <param name="abiType">The ABI type.</param>
    /// <param name="elementMarshallerType">The element marshaller type.</param>
    public MethodSpecification IListAdapterUnmanagedValueTypeGetMany(TypeSignature elementType, TypeSignature abiType, TypeSignature elementMarshallerType)
    {
        return IListAdapterUnmanagedValueTypeExtensions
            .CreateMemberReference("GetMany"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.UInt32,
                genericParameterCount: 3,
                parameterTypes: [
                    IList1.MakeGenericReferenceType([new GenericParameterSignature(GenericParameterType.Method, 0)]),
                    _corLibTypeFactory.UInt32,
                    _corLibTypeFactory.UInt32,
                    new GenericParameterSignature(GenericParameterType.Method, 1).MakePointerType()]))
            .MakeGenericInstanceMethod([elementType, abiType, elementMarshallerType]);
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.IListAdapterManagedValueTypeExtensions.GetMany&lt;T, TAbi, TElementMarshaller&gt;</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    /// <param name="abiType">The ABI type.</param>
    /// <param name="elementMarshallerType">The element marshaller type.</param>
    public MethodSpecification IListAdapterManagedValueTypeGetMany(TypeSignature elementType, TypeSignature abiType, TypeSignature elementMarshallerType)
    {
        return IListAdapterManagedValueTypeExtensions
            .CreateMemberReference("GetMany"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.UInt32,
                genericParameterCount: 3,
                parameterTypes: [
                    IList1.MakeGenericReferenceType([new GenericParameterSignature(GenericParameterType.Method, 0)]),
                    _corLibTypeFactory.UInt32,
                    _corLibTypeFactory.UInt32,
                    new GenericParameterSignature(GenericParameterType.Method, 1).MakePointerType()]))
            .MakeGenericInstanceMethod([elementType, abiType, elementMarshallerType]);
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.IListAdapterKeyValuePairTypeExtensions.GetMany&lt;TKey, TValue, TElementMarshaller&gt;</c>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    /// <param name="elementMarshallerType">The element marshaller type.</param>
    public MethodSpecification IListAdapterKeyValuePairTypeGetMany(TypeSignature keyType, TypeSignature valueType, TypeSignature elementMarshallerType)
    {
        return IListAdapterKeyValuePairTypeExtensions
            .CreateMemberReference("GetMany"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.UInt32,
                genericParameterCount: 3,
                parameterTypes: [
                    IList1.MakeGenericReferenceType([KeyValuePair2.MakeGenericValueType([
                        new GenericParameterSignature(GenericParameterType.Method, 0),
                        new GenericParameterSignature(GenericParameterType.Method, 1)])]),
                    _corLibTypeFactory.UInt32,
                    _corLibTypeFactory.UInt32,
                    _corLibTypeFactory.Void.MakePointerType().MakePointerType()]))
            .MakeGenericInstanceMethod([keyType, valueType, elementMarshallerType]);
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.IListAdapterNullableTypeExtensions.GetMany&lt;T, TElementMarshaller&gt;</c>.
    /// </summary>
    /// <param name="underlyingType">The underlying value type.</param>
    /// <param name="elementMarshallerType">The element marshaller type.</param>
    public MethodSpecification IListAdapterNullableTypeGetMany(TypeSignature underlyingType, TypeSignature elementMarshallerType)
    {
        return IListAdapterNullableTypeExtensions
            .CreateMemberReference("GetMany"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.UInt32,
                genericParameterCount: 2,
                parameterTypes: [
                    IList1.MakeGenericReferenceType([Nullable1.MakeGenericValueType([new GenericParameterSignature(GenericParameterType.Method, 0)])]),
                    _corLibTypeFactory.UInt32,
                    _corLibTypeFactory.UInt32,
                    _corLibTypeFactory.Void.MakePointerType().MakePointerType()]))
            .MakeGenericInstanceMethod(underlyingType, elementMarshallerType);
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.IListAdapterReferenceTypeExtensions.GetMany&lt;T, TElementMarshaller&gt;</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    /// <param name="elementMarshallerType">The element marshaller type.</param>
    public MethodSpecification IListAdapterReferenceTypeGetMany(TypeSignature elementType, TypeSignature elementMarshallerType)
    {
        return IListAdapterReferenceTypeExtensions
            .CreateMemberReference("GetMany"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.UInt32,
                genericParameterCount: 2,
                parameterTypes: [
                    IList1.MakeGenericReferenceType([new GenericParameterSignature(GenericParameterType.Method, 0)]),
                    _corLibTypeFactory.UInt32,
                    _corLibTypeFactory.UInt32,
                    _corLibTypeFactory.Void.MakePointerType().MakePointerType()]))
            .MakeGenericInstanceMethod([elementType, elementMarshallerType]);
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IListAdapter&lt;T&gt;.SetAt</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference IListAdapter1SetAt(TypeSignature elementType)
    {
        return IListAdapter1
            .MakeGenericReferenceType([elementType])
            .ToTypeDefOrRef()
            .CreateMemberReference("SetAt"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Void,
                parameterTypes: [
                    IList1.MakeGenericReferenceType([new GenericParameterSignature(GenericParameterType.Type, 0)]),
                    _corLibTypeFactory.UInt32,
                    new GenericParameterSignature(GenericParameterType.Type, 0)]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IListAdapter&lt;T&gt;.InsertAt</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference IListAdapter1InsertAt(TypeSignature elementType)
    {
        return IListAdapter1
            .MakeGenericReferenceType([elementType])
            .ToTypeDefOrRef()
            .CreateMemberReference("InsertAt"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Void,
                parameterTypes: [
                    IList1.MakeGenericReferenceType([new GenericParameterSignature(GenericParameterType.Type, 0)]),
                    _corLibTypeFactory.UInt32,
                    new GenericParameterSignature(GenericParameterType.Type, 0)]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IListAdapter&lt;T&gt;.RemoveAt</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference IListAdapter1RemoveAt(TypeSignature elementType)
    {
        return IListAdapter1
            .MakeGenericReferenceType([elementType])
            .ToTypeDefOrRef()
            .CreateMemberReference("RemoveAt"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Void,
                parameterTypes: [
                    IList1.MakeGenericReferenceType([new GenericParameterSignature(GenericParameterType.Type, 0)]),
                    _corLibTypeFactory.UInt32]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IListAdapter&lt;T&gt;.RemoveAtEnd</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference IListAdapter1RemoveAtEnd(TypeSignature elementType)
    {
        return IListAdapter1
            .MakeGenericReferenceType([elementType])
            .ToTypeDefOrRef()
            .CreateMemberReference("RemoveAtEnd"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Void,
                parameterTypes: [IList1.MakeGenericReferenceType([new GenericParameterSignature(GenericParameterType.Type, 0)])]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IReadOnlyListAdapter&lt;T&gt;.GetAt</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference IReadOnlyListAdapter1GetAt(TypeSignature elementType)
    {
        return IReadOnlyListAdapter1
            .MakeGenericReferenceType([elementType])
            .ToTypeDefOrRef()
            .CreateMemberReference("GetAt"u8, MethodSignature.CreateStatic(
                returnType: new GenericParameterSignature(GenericParameterType.Type, 0),
                parameterTypes: [
                    IReadOnlyList1.MakeGenericReferenceType([new GenericParameterSignature(GenericParameterType.Type, 0)]),
                    _corLibTypeFactory.UInt32]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IReadOnlyListAdapter&lt;T&gt;.Size</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference IReadOnlyListAdapter1Size(TypeSignature elementType)
    {
        return IReadOnlyListAdapter1
            .MakeGenericReferenceType([elementType])
            .ToTypeDefOrRef()
            .CreateMemberReference("Size"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.UInt32,
                parameterTypes: [IReadOnlyList1.MakeGenericReferenceType([new GenericParameterSignature(GenericParameterType.Type, 0)])]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IReadOnlyListAdapter&lt;T&gt;.IndexOf</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference IReadOnlyListAdapter1IndexOf(TypeSignature elementType)
    {
        return IReadOnlyListAdapter1
            .MakeGenericReferenceType([elementType])
            .ToTypeDefOrRef()
            .CreateMemberReference("IndexOf"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Boolean,
                parameterTypes: [
                    IReadOnlyList1.MakeGenericReferenceType([new GenericParameterSignature(GenericParameterType.Type, 0)]),
                    new GenericParameterSignature(GenericParameterType.Type, 0),
                    _corLibTypeFactory.UInt32.MakeByReferenceType()]));
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.IReadOnlyListAdapterBlittableValueTypeExtensions.GetMany&lt;T&gt;</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MethodSpecification IReadOnlyListAdapterBlittableValueTypeGetMany(TypeSignature elementType)
    {
        return IReadOnlyListAdapterBlittableValueTypeExtensions
            .CreateMemberReference("GetMany"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.UInt32,
                genericParameterCount: 1,
                parameterTypes: [
                    IReadOnlyList1.MakeGenericReferenceType([new GenericParameterSignature(GenericParameterType.Method, 0)]),
                    _corLibTypeFactory.UInt32,
                    _corLibTypeFactory.UInt32,
                    new GenericParameterSignature(GenericParameterType.Method, 0).MakePointerType()]))
            .MakeGenericInstanceMethod([elementType]);
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.IReadOnlyListAdapterUnmanagedValueTypeExtensions.GetMany&lt;T, TAbi, TElementMarshaller&gt;</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    /// <param name="abiType">The ABI type.</param>
    /// <param name="elementMarshallerType">The element marshaller type.</param>
    public MethodSpecification IReadOnlyListAdapterUnmanagedValueTypeGetMany(TypeSignature elementType, TypeSignature abiType, TypeSignature elementMarshallerType)
    {
        return IReadOnlyListAdapterUnmanagedValueTypeExtensions
            .CreateMemberReference("GetMany"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.UInt32,
                genericParameterCount: 3,
                parameterTypes: [
                    IReadOnlyList1.MakeGenericReferenceType([new GenericParameterSignature(GenericParameterType.Method, 0)]),
                    _corLibTypeFactory.UInt32,
                    _corLibTypeFactory.UInt32,
                    new GenericParameterSignature(GenericParameterType.Method, 1).MakePointerType()]))
            .MakeGenericInstanceMethod([elementType, abiType, elementMarshallerType]);
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.IReadOnlyListAdapterManagedValueTypeExtensions.GetMany&lt;T, TAbi, TElementMarshaller&gt;</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    /// <param name="abiType">The ABI type.</param>
    /// <param name="elementMarshallerType">The element marshaller type.</param>
    public MethodSpecification IReadOnlyListAdapterManagedValueTypeGetMany(TypeSignature elementType, TypeSignature abiType, TypeSignature elementMarshallerType)
    {
        return IReadOnlyListAdapterManagedValueTypeExtensions
            .CreateMemberReference("GetMany"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.UInt32,
                genericParameterCount: 3,
                parameterTypes: [
                    IReadOnlyList1.MakeGenericReferenceType([new GenericParameterSignature(GenericParameterType.Method, 0)]),
                    _corLibTypeFactory.UInt32,
                    _corLibTypeFactory.UInt32,
                    new GenericParameterSignature(GenericParameterType.Method, 1).MakePointerType()]))
            .MakeGenericInstanceMethod([elementType, abiType, elementMarshallerType]);
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.IReadOnlyListAdapterKeyValuePairTypeExtensions.GetMany&lt;TKey, TValue, TElementMarshaller&gt;</c>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    /// <param name="elementMarshallerType">The element marshaller type.</param>
    public MethodSpecification IReadOnlyListAdapterKeyValuePairTypeGetMany(TypeSignature keyType, TypeSignature valueType, TypeSignature elementMarshallerType)
    {
        return IReadOnlyListAdapterKeyValuePairTypeExtensions
            .CreateMemberReference("GetMany"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.UInt32,
                genericParameterCount: 3,
                parameterTypes: [
                    IReadOnlyList1.MakeGenericReferenceType([KeyValuePair2.MakeGenericValueType([
                        new GenericParameterSignature(GenericParameterType.Method, 0),
                        new GenericParameterSignature(GenericParameterType.Method, 1)])]),
                    _corLibTypeFactory.UInt32,
                    _corLibTypeFactory.UInt32,
                    _corLibTypeFactory.Void.MakePointerType().MakePointerType()]))
            .MakeGenericInstanceMethod([keyType, valueType, elementMarshallerType]);
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.IReadOnlyListAdapterNullableTypeExtensions.GetMany&lt;T, TElementMarshaller&gt;</c>.
    /// </summary>
    /// <param name="underlyingType">The underlying value type.</param>
    /// <param name="elementMarshallerType">The element marshaller type.</param>
    public MethodSpecification IReadOnlyListAdapterNullableTypeGetMany(TypeSignature underlyingType, TypeSignature elementMarshallerType)
    {
        return IReadOnlyListAdapterNullableTypeExtensions
            .CreateMemberReference("GetMany"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.UInt32,
                genericParameterCount: 2,
                parameterTypes: [
                    IReadOnlyList1.MakeGenericReferenceType([Nullable1.MakeGenericValueType([new GenericParameterSignature(GenericParameterType.Method, 0)])]),
                    _corLibTypeFactory.UInt32,
                    _corLibTypeFactory.UInt32,
                    _corLibTypeFactory.Void.MakePointerType().MakePointerType()]))
            .MakeGenericInstanceMethod(underlyingType, elementMarshallerType);
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.IReadOnlyListAdapterReferenceTypeExtensions.GetMany&lt;T, TElementMarshaller&gt;</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    /// <param name="elementMarshallerType">The element marshaller type.</param>
    public MethodSpecification IReadOnlyListAdapterReferenceTypeGetMany(TypeSignature elementType, TypeSignature elementMarshallerType)
    {
        return IReadOnlyListAdapterReferenceTypeExtensions
            .CreateMemberReference("GetMany"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.UInt32,
                genericParameterCount: 2,
                parameterTypes: [
                    IReadOnlyList1.MakeGenericReferenceType([new GenericParameterSignature(GenericParameterType.Method, 0)]),
                    _corLibTypeFactory.UInt32,
                    _corLibTypeFactory.UInt32,
                    _corLibTypeFactory.Void.MakePointerType().MakePointerType()]))
            .MakeGenericInstanceMethod([elementType, elementMarshallerType]);
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.IReadOnlyListMethods&lt;T&gt;.Item</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    /// <param name="vectorViewMethods">The <see cref="IVectorViewMethods1"/> type.</param>
    public MethodSpecification IReadOnlyListMethods1get_Item(TypeSignature elementType, TypeDefinition vectorViewMethods)
    {
        return IReadOnlyListMethods1
            .MakeGenericReferenceType([elementType])
            .ToTypeDefOrRef()
            .CreateMemberReference("Item"u8, MethodSignature.CreateStatic(
                returnType: new GenericParameterSignature(GenericParameterType.Type, 0),
                genericParameterCount: 1,
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    _corLibTypeFactory.Int32]))
            .MakeGenericInstanceMethod([vectorViewMethods.ToReferenceTypeSignature()]);
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IMapMethodsImpl&lt;K, V&gt;.HasKey</c>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    public MemberReference IMapMethodsImpl2HasKey(TypeSignature keyType, TypeSignature valueType)
    {
        return IMapMethodsImpl2
            .MakeGenericReferenceType([keyType, valueType])
            .ToTypeDefOrRef()
            .CreateMemberReference("HasKey"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Boolean,
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
            .MakeGenericReferenceType([keyType, valueType])
            .ToTypeDefOrRef()
            .CreateMemberReference("Lookup"u8, MethodSignature.CreateStatic(
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
            .MakeGenericReferenceType([keyType, valueType])
            .ToTypeDefOrRef()
            .CreateMemberReference("Insert"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Boolean,
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
            .MakeGenericReferenceType([keyType, valueType])
            .ToTypeDefOrRef()
            .CreateMemberReference("Remove"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Void,
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
            .MakeGenericReferenceType([keyType, valueType])
            .ToTypeDefOrRef()
            .CreateMemberReference("HasKey"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Boolean,
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
            .MakeGenericReferenceType([keyType, valueType])
            .ToTypeDefOrRef()
            .CreateMemberReference("Lookup"u8, MethodSignature.CreateStatic(
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
            .MakeGenericReferenceType([keyType, valueType])
            .ToTypeDefOrRef()
            .CreateMemberReference("Item"u8, MethodSignature.CreateStatic(
                returnType: new GenericParameterSignature(GenericParameterType.Type, 1),
                genericParameterCount: 1,
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    new GenericParameterSignature(GenericParameterType.Type, 0)]))
            .MakeGenericInstanceMethod([mapMethods.ToReferenceTypeSignature()]);
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
            .MakeGenericReferenceType([keyType, valueType])
            .ToTypeDefOrRef()
            .CreateMemberReference("Item"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Void,
                genericParameterCount: 1,
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    new GenericParameterSignature(GenericParameterType.Type, 0),
                    new GenericParameterSignature(GenericParameterType.Type, 1)]))
            .MakeGenericInstanceMethod([mapMethods.ToReferenceTypeSignature()]);
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
            .MakeGenericReferenceType([keyType, valueType])
            .ToTypeDefOrRef()
            .CreateMemberReference("Add"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Void,
                genericParameterCount: 1,
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    new GenericParameterSignature(GenericParameterType.Type, 0),
                    new GenericParameterSignature(GenericParameterType.Type, 1)]))
            .MakeGenericInstanceMethod([mapMethods.ToReferenceTypeSignature()]);
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
            .MakeGenericReferenceType([keyType, valueType])
            .ToTypeDefOrRef()
            .CreateMemberReference("ContainsKey"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Boolean,
                genericParameterCount: 1,
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    new GenericParameterSignature(GenericParameterType.Type, 0)]))
            .MakeGenericInstanceMethod([mapMethods.ToReferenceTypeSignature()]);
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
            .MakeGenericReferenceType([keyType, valueType])
            .ToTypeDefOrRef()
            .CreateMemberReference("Remove"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Boolean,
                genericParameterCount: 1,
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    new GenericParameterSignature(GenericParameterType.Type, 0)]))
            .MakeGenericInstanceMethod([mapMethods.ToReferenceTypeSignature()]);
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
            .MakeGenericReferenceType([keyType, valueType])
            .ToTypeDefOrRef()
            .CreateMemberReference("TryGetValue"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Boolean,
                genericParameterCount: 1,
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    new GenericParameterSignature(GenericParameterType.Type, 0),
                    new GenericParameterSignature(GenericParameterType.Type, 1).MakeByReferenceType()]))
            .MakeGenericInstanceMethod([mapMethods.ToReferenceTypeSignature()]);
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
            .MakeGenericReferenceType([keyType, valueType])
            .ToTypeDefOrRef()
            .CreateMemberReference("Add"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Void,
                genericParameterCount: 1,
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    KeyValuePair2.MakeGenericValueType([
                        new GenericParameterSignature(GenericParameterType.Type, 0),
                        new GenericParameterSignature(GenericParameterType.Type, 1)])]))
            .MakeGenericInstanceMethod([mapMethods.ToReferenceTypeSignature()]);
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
            .MakeGenericReferenceType([keyType, valueType])
            .ToTypeDefOrRef()
            .CreateMemberReference("Contains"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Boolean,
                genericParameterCount: 1,
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    KeyValuePair2.MakeGenericValueType([
                        new GenericParameterSignature(GenericParameterType.Type, 0),
                        new GenericParameterSignature(GenericParameterType.Type, 1)])]))
            .MakeGenericInstanceMethod([mapMethods.ToReferenceTypeSignature()]);
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.IDictionaryMethods&lt;TKey, TValue&gt;.CopyTo</c>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    /// <param name="iterableMethods">The <see cref="IIterableMethodsImpl1"/> type.</param>
    public MethodSpecification IDictionaryMethods2CopyTo(
        TypeSignature keyType,
        TypeSignature valueType,
        TypeDefinition iterableMethods)
    {
        return IDictionaryMethods2
            .MakeGenericReferenceType([keyType, valueType])
            .ToTypeDefOrRef()
            .CreateMemberReference("CopyTo"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Void,
                genericParameterCount: 1,
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    KeyValuePair2.MakeGenericValueType([
                        new GenericParameterSignature(GenericParameterType.Type, 0),
                        new GenericParameterSignature(GenericParameterType.Type, 1)]).MakeSzArrayType(),
                    _corLibTypeFactory.Int32]))
            .MakeGenericInstanceMethod([iterableMethods.ToReferenceTypeSignature()]);
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
            .MakeGenericReferenceType([keyType, valueType])
            .ToTypeDefOrRef()
            .CreateMemberReference("Remove"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Boolean,
                genericParameterCount: 1,
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    KeyValuePair2.MakeGenericValueType([
                        new GenericParameterSignature(GenericParameterType.Type, 0),
                        new GenericParameterSignature(GenericParameterType.Type, 1)])]))
            .MakeGenericInstanceMethod([mapMethods.ToReferenceTypeSignature()]);
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IDictionaryAdapter&lt;TKey, TValue&gt;.Lookup</c>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    public MemberReference IDictionaryAdapter2Lookup(TypeSignature keyType, TypeSignature valueType)
    {
        return IDictionaryAdapter2
            .MakeGenericReferenceType([keyType, valueType])
            .ToTypeDefOrRef()
            .CreateMemberReference("Lookup"u8, MethodSignature.CreateStatic(
                returnType: new GenericParameterSignature(GenericParameterType.Type, 1),
                parameterTypes: [
                    IDictionary2.MakeGenericReferenceType([
                        new GenericParameterSignature(GenericParameterType.Type, 0),
                        new GenericParameterSignature(GenericParameterType.Type, 1)]),
                    new GenericParameterSignature(GenericParameterType.Type, 0)]));
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.IDictionaryAdapter&lt;string, TValue&gt;.Lookup</c>.
    /// </summary>
    /// <param name="valueType">The input value type.</param>
    public MethodSpecification IDictionaryAdapterOfStringLookup(TypeSignature valueType)
    {
        return IDictionaryAdapterExtensions
            .CreateMemberReference("Lookup"u8, MethodSignature.CreateStatic(
                returnType: new GenericParameterSignature(GenericParameterType.Method, 0),
                genericParameterCount: 1,
                parameterTypes: [
                    IDictionary2.MakeGenericReferenceType([_corLibTypeFactory.String, new GenericParameterSignature(GenericParameterType.Method, 0)]),
                    ReadOnlySpanChar]))
            .MakeGenericInstanceMethod([valueType]);
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IDictionaryAdapter&lt;TKey, TValue&gt;.Size</c>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    public MemberReference IDictionaryAdapter2Size(TypeSignature keyType, TypeSignature valueType)
    {
        return IDictionaryAdapter2
            .MakeGenericReferenceType([keyType, valueType])
            .ToTypeDefOrRef()
            .CreateMemberReference("Size"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.UInt32,
                parameterTypes: [IDictionary2.MakeGenericReferenceType([
                    new GenericParameterSignature(GenericParameterType.Type, 0),
                    new GenericParameterSignature(GenericParameterType.Type, 1)])]));
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.IDictionaryAdapter&lt;string, TValue&gt;.HasKey</c>.
    /// </summary>
    /// <param name="valueType">The input value type.</param>
    public MethodSpecification IDictionaryAdapterOfStringHasKey(TypeSignature valueType)
    {
        return IDictionaryAdapterExtensions
            .CreateMemberReference("HasKey"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Boolean,
                genericParameterCount: 1,
                parameterTypes: [
                    IDictionary2.MakeGenericReferenceType([_corLibTypeFactory.String, new GenericParameterSignature(GenericParameterType.Method, 0)]),
                    ReadOnlySpanChar]))
            .MakeGenericInstanceMethod([valueType]);
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IDictionaryAdapter&lt;string, TValue&gt;.GetView</c>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    public MemberReference IDictionaryAdapter2GetView(TypeSignature keyType, TypeSignature valueType)
    {
        return IDictionaryAdapter2
            .MakeGenericReferenceType([keyType, valueType])
            .ToTypeDefOrRef()
            .CreateMemberReference("GetView"u8, MethodSignature.CreateStatic(
                returnType: IReadOnlyDictionary2.MakeGenericReferenceType([
                    new GenericParameterSignature(GenericParameterType.Type, 0),
                    new GenericParameterSignature(GenericParameterType.Type, 1)]),
                parameterTypes: [IDictionary2.MakeGenericReferenceType([
                    new GenericParameterSignature(GenericParameterType.Type, 0),
                    new GenericParameterSignature(GenericParameterType.Type, 1)])]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IDictionaryAdapter&lt;string, TValue&gt;.Insert</c>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    public MemberReference IDictionaryAdapter2Insert(TypeSignature keyType, TypeSignature valueType)
    {
        return IDictionaryAdapter2
            .MakeGenericReferenceType([keyType, valueType])
            .ToTypeDefOrRef()
            .CreateMemberReference("Insert"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Boolean,
                parameterTypes: [IDictionary2.MakeGenericReferenceType([
                    new GenericParameterSignature(GenericParameterType.Type, 0),
                    new GenericParameterSignature(GenericParameterType.Type, 1)]),
                    new GenericParameterSignature(GenericParameterType.Type, 0),
                    new GenericParameterSignature(GenericParameterType.Type, 1)]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IDictionaryAdapter&lt;string, TValue&gt;.Remove</c>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    public MemberReference IDictionaryAdapter2Remove(TypeSignature keyType, TypeSignature valueType)
    {
        return IDictionaryAdapter2
            .MakeGenericReferenceType([keyType, valueType])
            .ToTypeDefOrRef()
            .CreateMemberReference("Remove"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Void,
                parameterTypes: [IDictionary2.MakeGenericReferenceType([
                    new GenericParameterSignature(GenericParameterType.Type, 0),
                    new GenericParameterSignature(GenericParameterType.Type, 1)]),
                    new GenericParameterSignature(GenericParameterType.Type, 0)]));
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.IDictionaryAdapter&lt;string, TValue&gt;.Remove</c>.
    /// </summary>
    /// <param name="valueType">The input value type.</param>
    public MethodSpecification IDictionaryAdapterOfStringRemove(TypeSignature valueType)
    {
        return IDictionaryAdapterExtensions
            .CreateMemberReference("Remove"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Void,
                genericParameterCount: 1,
                parameterTypes: [
                    IDictionary2.MakeGenericReferenceType([_corLibTypeFactory.String, new GenericParameterSignature(GenericParameterType.Method, 0)]),
                    ReadOnlySpanChar]))
            .MakeGenericInstanceMethod([valueType]);
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
            .MakeGenericReferenceType([keyType, valueType])
            .ToTypeDefOrRef()
            .CreateMemberReference("Item"u8, MethodSignature.CreateStatic(
                returnType: new GenericParameterSignature(GenericParameterType.Type, 1),
                genericParameterCount: 1,
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    new GenericParameterSignature(GenericParameterType.Type, 0)]))
            .MakeGenericInstanceMethod([mapViewMethods.ToReferenceTypeSignature()]);
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
            .MakeGenericReferenceType([keyType, valueType])
            .ToTypeDefOrRef()
            .CreateMemberReference("ContainsKey"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Boolean,
                genericParameterCount: 1,
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    new GenericParameterSignature(GenericParameterType.Type, 0)]))
            .MakeGenericInstanceMethod([mapViewMethods.ToReferenceTypeSignature()]);
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
            .MakeGenericReferenceType([keyType, valueType])
            .ToTypeDefOrRef()
            .CreateMemberReference("TryGetValue"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Boolean,
                genericParameterCount: 1,
                parameterTypes: [
                    WindowsRuntimeObjectReference.ToReferenceTypeSignature(),
                    new GenericParameterSignature(GenericParameterType.Type, 0),
                    new GenericParameterSignature(GenericParameterType.Type, 1).MakeByReferenceType()]))
            .MakeGenericInstanceMethod([mapViewMethods.ToReferenceTypeSignature()]);
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IReadOnlyDictionaryAdapter&lt;TKey, TValue&gt;.Lookup</c>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    public MemberReference IReadOnlyDictionaryAdapter2Lookup(TypeSignature keyType, TypeSignature valueType)
    {
        return IReadOnlyDictionaryAdapter2
            .MakeGenericReferenceType([keyType, valueType])
            .ToTypeDefOrRef()
            .CreateMemberReference("Lookup"u8, MethodSignature.CreateStatic(
                returnType: new GenericParameterSignature(GenericParameterType.Type, 1),
                parameterTypes: [
                    IReadOnlyDictionary2.MakeGenericReferenceType([
                        new GenericParameterSignature(GenericParameterType.Type, 0),
                        new GenericParameterSignature(GenericParameterType.Type, 1)]),
                    new GenericParameterSignature(GenericParameterType.Type, 0)]));
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.IReadOnlyDictionaryAdapter&lt;string, TValue&gt;.Lookup</c>.
    /// </summary>
    /// <param name="valueType">The input value type.</param>
    public MethodSpecification IReadOnlyDictionaryAdapterOfStringLookup(TypeSignature valueType)
    {
        return IReadOnlyDictionaryAdapterExtensions
            .CreateMemberReference("Lookup"u8, MethodSignature.CreateStatic(
                returnType: new GenericParameterSignature(GenericParameterType.Method, 0),
                genericParameterCount: 1,
                parameterTypes: [
                    IReadOnlyDictionary2.MakeGenericReferenceType([_corLibTypeFactory.String, new GenericParameterSignature(GenericParameterType.Method, 0)]),
                    ReadOnlySpanChar]))
            .MakeGenericInstanceMethod([valueType]);
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IReadOnlyDictionaryAdapter&lt;TKey, TValue&gt;.Size</c>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    public MemberReference IReadOnlyDictionaryAdapter2Size(TypeSignature keyType, TypeSignature valueType)
    {
        return IReadOnlyDictionaryAdapter2
            .MakeGenericReferenceType([keyType, valueType])
            .ToTypeDefOrRef()
            .CreateMemberReference("Size"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.UInt32,
                parameterTypes: [IReadOnlyDictionary2.MakeGenericReferenceType([
                    new GenericParameterSignature(GenericParameterType.Type, 0),
                    new GenericParameterSignature(GenericParameterType.Type, 1)])]));
    }

    /// <summary>
    /// Gets the <see cref="MethodSpecification"/> for <c>WindowsRuntime.InteropServices.IReadOnlyDictionaryAdapter&lt;string, TValue&gt;.HasKey</c>.
    /// </summary>
    /// <param name="valueType">The input value type.</param>
    public MethodSpecification IReadOnlyDictionaryAdapterOfStringHasKey(TypeSignature valueType)
    {
        return IReadOnlyDictionaryAdapterExtensions
            .CreateMemberReference("HasKey"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Boolean,
                genericParameterCount: 1,
                parameterTypes: [
                    IReadOnlyDictionary2.MakeGenericReferenceType([_corLibTypeFactory.String, new GenericParameterSignature(GenericParameterType.Method, 0)]),
                    ReadOnlySpanChar]))
            .MakeGenericInstanceMethod([valueType]);
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IReadOnlyDictionaryAdapter&lt;TKey, TValue&gt;.Split</c>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    public MemberReference IReadOnlyDictionaryAdapter2Split(TypeSignature keyType, TypeSignature valueType)
    {
        TypeSignature readOnlyDictionaryType = IReadOnlyDictionary2.MakeGenericReferenceType([
            new GenericParameterSignature(GenericParameterType.Type, 0),
            new GenericParameterSignature(GenericParameterType.Type, 1)]);

        return IReadOnlyDictionaryAdapter2
            .MakeGenericReferenceType([keyType, valueType])
            .ToTypeDefOrRef()
            .CreateMemberReference("Split"u8, MethodSignature.CreateStatic(
                returnType: _corLibTypeFactory.Void,
                parameterTypes: [
                    readOnlyDictionaryType,
                    readOnlyDictionaryType.MakeByReferenceType(),
                    readOnlyDictionaryType.MakeByReferenceType()]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Collections.Generic.IDictionary{TKey, TValue}.this"/>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    public MemberReference IDictionary2get_Item(TypeSignature keyType, TypeSignature valueType)
    {
        return IDictionary2
            .MakeGenericReferenceType([keyType, valueType])
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
            .MakeGenericReferenceType([keyType, valueType])
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
            .MakeGenericReferenceType([keyType, valueType])
            .ToTypeDefOrRef()
            .CreateMemberReference("get_Keys"u8, MethodSignature.CreateInstance(
                ICollection1.MakeGenericReferenceType([new GenericParameterSignature(GenericParameterType.Type, 0)])));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Collections.Generic.IDictionary{TKey, TValue}.Values"/>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    public MemberReference IDictionary2get_Values(TypeSignature keyType, TypeSignature valueType)
    {
        return IDictionary2
            .MakeGenericReferenceType([keyType, valueType])
            .ToTypeDefOrRef()
            .CreateMemberReference("get_Values"u8, MethodSignature.CreateInstance(
                ICollection1.MakeGenericReferenceType([new GenericParameterSignature(GenericParameterType.Type, 1)])));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Collections.Generic.IDictionary{TKey, TValue}.Add"/>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    public MemberReference IDictionary2Add(TypeSignature keyType, TypeSignature valueType)
    {
        return IDictionary2
            .MakeGenericReferenceType([keyType, valueType])
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
            .MakeGenericReferenceType([keyType, valueType])
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
            .MakeGenericReferenceType([keyType, valueType])
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
            .MakeGenericReferenceType([keyType, valueType])
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
            .MakeGenericReferenceType([keyType, valueType])
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
            .MakeGenericReferenceType([keyType, valueType])
            .ToTypeDefOrRef()
            .CreateMemberReference("get_Keys"u8, MethodSignature.CreateInstance(
                IEnumerable1.MakeGenericReferenceType([new GenericParameterSignature(GenericParameterType.Type, 0)])));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Collections.Generic.IReadOnlyDictionary{TKey, TValue}.Values"/>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    public MemberReference IReadOnlyDictionary2get_Values(TypeSignature keyType, TypeSignature valueType)
    {
        return IReadOnlyDictionary2
            .MakeGenericReferenceType([keyType, valueType])
            .ToTypeDefOrRef()
            .CreateMemberReference("get_Values"u8, MethodSignature.CreateInstance(
                IEnumerable1.MakeGenericReferenceType([new GenericParameterSignature(GenericParameterType.Type, 1)])));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <see cref="System.Collections.Generic.IReadOnlyDictionary{TKey, TValue}.ContainsKey"/>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    public MemberReference IReadOnlyDictionary2ContainsKey(TypeSignature keyType, TypeSignature valueType)
    {
        return IReadOnlyDictionary2
            .MakeGenericReferenceType([keyType, valueType])
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
            .MakeGenericReferenceType([keyType, valueType])
            .ToTypeDefOrRef()
            .CreateMemberReference("TryGetValue"u8, MethodSignature.CreateInstance(
                returnType: _corLibTypeFactory.Boolean,
                parameterTypes: [
                    new GenericParameterSignature(GenericParameterType.Type, 0),
                    new GenericParameterSignature(GenericParameterType.Type, 1).MakeByReferenceType()]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>Windows.Foundation.Collections.IObservableVector&lt;T&gt;.VectorChanged</c>'s adder.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference IObservableVector1add_VectorChanged(TypeSignature elementType)
    {
        return IObservableVector1
            .MakeGenericReferenceType([elementType])
            .ToTypeDefOrRef()
            .CreateMemberReference("add_VectorChanged"u8, MethodSignature.CreateInstance(
                returnType: _corLibTypeFactory.Void,
                parameterTypes: [VectorChangedEventHandler1.MakeGenericReferenceType([new GenericParameterSignature(GenericParameterType.Type, 0)])]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>Windows.Foundation.Collections.IObservableVector&lt;T&gt;.VectorChanged</c>'s remover.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference IObservableVector1remove_VectorChanged(TypeSignature elementType)
    {
        return IObservableVector1
            .MakeGenericReferenceType([elementType])
            .ToTypeDefOrRef()
            .CreateMemberReference("remove_VectorChanged"u8, MethodSignature.CreateInstance(
                returnType: _corLibTypeFactory.Void,
                parameterTypes: [VectorChangedEventHandler1.MakeGenericReferenceType([new GenericParameterSignature(GenericParameterType.Type, 0)])]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>Windows.Foundation.Collections.IObservableVector&lt;T&gt;.VectorChanged</c>'s getter.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference IObservableVectorEventSourceFactory1VectorChanged(TypeSignature elementType)
    {
        return IObservableVectorEventSourceFactory1
            .MakeGenericReferenceType([elementType])
            .ToTypeDefOrRef()
            .CreateMemberReference("VectorChanged"u8, MethodSignature.CreateStatic(
                returnType: VectorChangedEventHandler1EventSource.MakeGenericReferenceType([new GenericParameterSignature(GenericParameterType.Type, 0)]),
                parameterTypes: [WindowsRuntimeObjectReference.ToReferenceTypeSignature()]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>Windows.Foundation.Collections.IObservableMap&lt;K,V&gt;.MapChanged</c>'s adder.
    /// </summary>
    /// <param name="keyType">The type of keys.</param>
    /// <param name="valueType">The type of values.</param>
    public MemberReference IObservableMap2add_MapChanged(TypeSignature keyType, TypeSignature valueType)
    {
        return IObservableMap2
            .MakeGenericReferenceType([keyType, valueType])
            .ToTypeDefOrRef()
            .CreateMemberReference("add_MapChanged"u8, MethodSignature.CreateInstance(
                returnType: _corLibTypeFactory.Void,
                parameterTypes: [MapChangedEventHandler2.MakeGenericReferenceType([
                    new GenericParameterSignature(GenericParameterType.Type, 0),
                    new GenericParameterSignature(GenericParameterType.Type, 1)])]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>Windows.Foundation.Collections.IObservableMap&lt;K,V&gt;.MapChanged</c>'s remover.
    /// </summary>
    /// <param name="keyType">The type of keys.</param>
    /// <param name="valueType">The type of values.</param>
    public MemberReference IObservableMap2remove_MapChanged(TypeSignature keyType, TypeSignature valueType)
    {
        return IObservableMap2
            .MakeGenericReferenceType([keyType, valueType])
            .ToTypeDefOrRef()
            .CreateMemberReference("remove_MapChanged"u8, MethodSignature.CreateInstance(
                returnType: _corLibTypeFactory.Void,
                parameterTypes: [MapChangedEventHandler2.MakeGenericReferenceType([
                    new GenericParameterSignature(GenericParameterType.Type, 0),
                    new GenericParameterSignature(GenericParameterType.Type, 1)])]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>Windows.Foundation.Collections.IObservableMap&lt;K, V&gt;.MapChanged</c>'s getter.
    /// </summary>
    /// <param name="keyType">The type of keys.</param>
    /// <param name="valueType">The type of values.</param>
    public MemberReference IObservableMapEventSourceFactory2MapChanged(TypeSignature keyType, TypeSignature valueType)
    {
        return IObservableMapEventSourceFactory2
            .MakeGenericReferenceType([keyType, valueType])
            .ToTypeDefOrRef()
            .CreateMemberReference("MapChanged"u8, MethodSignature.CreateStatic(
                returnType: MapChangedEventHandler2EventSource.MakeGenericReferenceType([
                    new GenericParameterSignature(GenericParameterType.Type, 0),
                    new GenericParameterSignature(GenericParameterType.Type, 1)]),
                parameterTypes: [WindowsRuntimeObjectReference.ToReferenceTypeSignature()]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>Windows.Foundation.Collections.IMapChangedEventArgs&lt;K&gt;.CollectionChange</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference IMapChangedEventArgs1get_CollectionChange(TypeSignature elementType)
    {
        return IMapChangedEventArgs1
            .MakeGenericReferenceType([elementType])
            .ToTypeDefOrRef()
            .CreateMemberReference("get_CollectionChange"u8, MethodSignature.CreateInstance(CollectionChange.ToValueTypeSignature()));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>Windows.Foundation.Collections.IMapChangedEventArgs&lt;K&gt;.Key</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference IMapChangedEventArgs1get_Key(TypeSignature elementType)
    {
        return IMapChangedEventArgs1
            .MakeGenericReferenceType([elementType])
            .ToTypeDefOrRef()
            .CreateMemberReference("get_Key"u8, MethodSignature.CreateInstance(new GenericParameterSignature(GenericParameterType.Type, 0)));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IMapChangedEventArgsMethodsImpl&lt;K&gt;.Key</c>.
    /// </summary>
    /// <param name="elementType">The input element type.</param>
    public MemberReference IMapChangedEventArgsMethodsImpl1Key(TypeSignature elementType)
    {
        return IMapChangedEventArgsMethodsImpl1
            .MakeGenericReferenceType([elementType])
            .ToTypeDefOrRef()
            .CreateMemberReference("Key"u8, MethodSignature.CreateStatic(
                returnType: new GenericParameterSignature(GenericParameterType.Type, 0),
                parameterTypes: [WindowsRuntimeObjectReference.ToReferenceTypeSignature()]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.EventSource&lt;T&gt;.Subscribe(...)</c>.
    /// </summary>
    /// <param name="delegateType">The input delegate type.</param>
    public MemberReference EventSource1Subscribe(TypeSignature delegateType)
    {
        return EventSource1
            .MakeGenericReferenceType([delegateType])
            .ToTypeDefOrRef()
            .CreateMemberReference("Subscribe"u8, MethodSignature.CreateInstance(
                returnType: _corLibTypeFactory.Void,
                parameterTypes: [new GenericParameterSignature(GenericParameterType.Type, 0)]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.EventSource&lt;T&gt;.Unsubscribe(...)</c>.
    /// </summary>
    /// <param name="delegateType">The input delegate type.</param>
    public MemberReference EventSource1Unsubscribe(TypeSignature delegateType)
    {
        return EventSource1
            .MakeGenericReferenceType([delegateType])
            .ToTypeDefOrRef()
            .CreateMemberReference("Unsubscribe"u8, MethodSignature.CreateInstance(
                returnType: _corLibTypeFactory.Void,
                parameterTypes: [new GenericParameterSignature(GenericParameterType.Type, 0)]));
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for the <c>.ctor</c> method of a given base type for a <c>NativeObject</c> type.
    /// </summary>
    /// <param name="enumeratorType">The input native object base type.</param>
    public MemberReference WindowsRuntimeNativeObjectBaseType_ctor(TypeSignature enumeratorType)
    {
        return enumeratorType
            .ToTypeDefOrRef()
            .CreateConstructorReference(_corLibTypeFactory, [WindowsRuntimeObjectReference.ToReferenceTypeSignature()]);
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for the <c>.ctor</c> method for <see cref="DictionaryKeyCollection2"/>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    public MemberReference DictionaryKeyCollection2_ctor(TypeSignature keyType, TypeSignature valueType)
    {
        return DictionaryKeyCollection2
            .MakeGenericReferenceType([keyType, valueType])
            .ToTypeDefOrRef()
            .CreateConstructorReference(
                corLibTypeFactory: _corLibTypeFactory,
                parameterTypes: [IDictionary2.MakeGenericReferenceType([
                    new GenericParameterSignature(GenericParameterType.Type, 0),
                    new GenericParameterSignature(GenericParameterType.Type, 1)])]);
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for the <c>.ctor</c> method for <see cref="DictionaryValueCollection2"/>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    public MemberReference DictionaryValueCollection2_ctor(TypeSignature keyType, TypeSignature valueType)
    {
        return DictionaryValueCollection2
            .MakeGenericReferenceType([keyType, valueType])
            .ToTypeDefOrRef()
            .CreateConstructorReference(
                corLibTypeFactory: _corLibTypeFactory,
                parameterTypes: [IDictionary2.MakeGenericReferenceType([
                    new GenericParameterSignature(GenericParameterType.Type, 0),
                    new GenericParameterSignature(GenericParameterType.Type, 1)])]);
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for the <c>.ctor</c> method for <see cref="ReadOnlyDictionaryKeyCollection2"/>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    public MemberReference ReadOnlyDictionaryKeyCollection2_ctor(TypeSignature keyType, TypeSignature valueType)
    {
        return ReadOnlyDictionaryKeyCollection2
            .MakeGenericReferenceType([keyType, valueType])
            .ToTypeDefOrRef()
            .CreateConstructorReference(
                corLibTypeFactory: _corLibTypeFactory,
                parameterTypes: [IEnumerable1.MakeGenericReferenceType([KeyValuePair2.MakeGenericValueType([
                    new GenericParameterSignature(GenericParameterType.Type, 0),
                    new GenericParameterSignature(GenericParameterType.Type, 1)])])]);
    }

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for the <c>.ctor</c> method for <see cref="ReadOnlyDictionaryValueCollection2"/>.
    /// </summary>
    /// <param name="keyType">The input key type.</param>
    /// <param name="valueType">The input value type.</param>
    public MemberReference ReadOnlyDictionaryValueCollection2_ctor(TypeSignature keyType, TypeSignature valueType)
    {
        return ReadOnlyDictionaryValueCollection2
            .MakeGenericReferenceType([keyType, valueType])
            .ToTypeDefOrRef()
            .CreateConstructorReference(
                corLibTypeFactory: _corLibTypeFactory,
                parameterTypes: [IEnumerable1.MakeGenericReferenceType([KeyValuePair2.MakeGenericValueType([
                    new GenericParameterSignature(GenericParameterType.Type, 0),
                    new GenericParameterSignature(GenericParameterType.Type, 1)])])]);
    }
}