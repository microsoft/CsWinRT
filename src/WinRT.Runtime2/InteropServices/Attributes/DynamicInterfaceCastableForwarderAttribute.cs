// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// An attribute that allows forwarding calls to the <see cref="IDynamicInterfaceCastable.IsInterfaceImplemented"/> method.
/// </summary>
/// <remarks>
/// This attribute is used to forward calls to the <see cref="IDynamicInterfaceCastable.IsInterfaceImplemented"/> for cases where
/// a given custom-mapped Windows Runtime interface can be implemented via multiple native Windows Runtime interfaces. This allows
/// implementations of this attribute to perform the correct <c>QueryInterface</c> calls to check whether the custom-mapped interface
/// is supported. Implementations are also expected to pre-cache the resulting object references while performing these checks.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public abstract class DynamicInterfaceCastableForwarderAttribute : Attribute
{
    /// <inheritdoc cref="IDynamicInterfaceCastable.IsInterfaceImplemented"/>
    /// <param name="thisObject">The <see cref="WindowsRuntimeObject"/> instance to use to perform <c>QueryInterface</c> calls.</param>
    /// <param name="interfaceReference">The resulting <see cref="WindowsRuntimeObjectReference"/> for the effective interface, if available.</param>
    public abstract bool IsInterfaceImplemented(WindowsRuntimeObject thisObject, [NotNullWhen(true)] out WindowsRuntimeObjectReference? interfaceReference);
}
#endif
