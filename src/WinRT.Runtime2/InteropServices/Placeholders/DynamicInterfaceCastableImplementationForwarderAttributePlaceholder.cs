// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A placeholder <see cref="DynamicInterfaceCastableImplementationForwarderAttribute"/> type.
/// </summary>
internal sealed class DynamicInterfaceCastableImplementationForwarderAttributePlaceholder : DynamicInterfaceCastableImplementationForwarderAttribute
{
    /// <summary>
    /// The shared placeholder instance.
    /// </summary>
    public static DynamicInterfaceCastableImplementationForwarderAttributePlaceholder Instance = new();

    /// <inheritdoc/>
    public override bool TryGetImplementationType(
        WindowsRuntimeObjectReference thisReference,
        [NotNullWhen(true)] out WindowsRuntimeObjectReference? interfaceReference,
        [NotNullWhen(true)] out Type? implementationType)
    {
        interfaceReference = null;
        implementationType = null;

        return false;
    }
}
