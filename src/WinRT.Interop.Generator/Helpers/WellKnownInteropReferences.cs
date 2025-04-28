// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;

namespace WindowsRuntime.InteropGenerator.References;

/// <summary>
/// Well known references to APIs from the Windows Runtime assembly.
/// </summary>
internal sealed class WellKnownInteropReferences
{
    /// <summary>
    /// The <see cref="ModuleDefinition"/> for the Windows Runtime assembly.
    /// </summary>
    private readonly ModuleDefinition _windowsRuntimeModule;

    /// <summary>
    /// Creates a new <see cref="WellKnownInteropReferences"/> instance.
    /// </summary>
    /// <param name="windowsRuntimeModule">The <see cref="ModuleDefinition"/> for the Windows Runtime assembly.</param>
    public WellKnownInteropReferences(ModuleDefinition windowsRuntimeModule)
    {
        _windowsRuntimeModule = windowsRuntimeModule;
    }

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
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IUnknownImpl.get_Vtable()</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference IUnknownImplget_Vtable => field ??= IUnknownImpl
        .CreateMemberReference("get_Vtable", MethodSignature.CreateStatic(
            returnType: _windowsRuntimeModule.CorLibTypeFactory.IntPtr,
            parameterTypes: []));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IInspectableImpl.get_Vtable()</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference IInspectableImplget_Vtable => field ??= IInspectableImpl
        .CreateMemberReference("get_Vtable", MethodSignature.CreateStatic(
            returnType: _windowsRuntimeModule.CorLibTypeFactory.IntPtr,
            parameterTypes: []));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IStringableImpl.get_Vtable()</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference IStringableImplget_Vtable => field ??= IStringableImpl
        .CreateMemberReference("get_Vtable", MethodSignature.CreateStatic(
            returnType: _windowsRuntimeModule.CorLibTypeFactory.IntPtr,
            parameterTypes: []));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IMarshalImpl.get_Vtable()</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference IMarshalImplget_Vtable => field ??= IMarshalImpl
        .CreateMemberReference("get_Vtable", MethodSignature.CreateStatic(
            returnType: _windowsRuntimeModule.CorLibTypeFactory.IntPtr,
            parameterTypes: []));

    /// <summary>
    /// Gets the <see cref="MemberReference"/> for <c>WindowsRuntime.InteropServices.IWeakReferenceSourceImpl.get_Vtable()</c>.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public MemberReference IWeakReferenceSourceImplget_Vtable => field ??= IWeakReferenceSourceImpl
        .CreateMemberReference("get_Vtable", MethodSignature.CreateStatic(
            returnType: _windowsRuntimeModule.CorLibTypeFactory.IntPtr,
            parameterTypes: []));
}
