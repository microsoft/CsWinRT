// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// An attribute that allows forwarding the resolution of <see cref="IDynamicInterfaceCastable"/> implementation types.
/// </summary>
/// <remarks>
/// This attribute is used to resolve implementation types for projected interfaces that can be implemented via more than
/// one native interface type. Implementations can perform <c>QueryInterface</c> calls to determine the right implementation.
/// </remarks>
[AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Struct |
    AttributeTargets.Enum |
    AttributeTargets.Interface |
    AttributeTargets.Delegate,
    AllowMultiple = false,
    Inherited = false)]
public abstract class DynamicInterfaceCastableImplementationForwarderAttribute : Attribute
{
    /// <summary>
    /// Tries to get the correct implementation type for a given interface type, for a given <see cref="WindowsRuntimeObjectReference"/> instance.
    /// </summary>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to perform <c>QueryInterface</c> calls.</param>
    /// <param name="interfaceReference">The resulting <see cref="WindowsRuntimeObjectReference"/> object for the resolved interface.</param>
    /// <param name="implementationType">The implementation type to use.</param>
    /// <returns>Whether an implementation type could be resolved.</returns>
    public abstract bool TryGetImplementationType(
        WindowsRuntimeObjectReference thisReference,
        [NotNullWhen(true)] out WindowsRuntimeObjectReference? interfaceReference,
        [NotNullWhen(true)] out Type? implementationType);
}
