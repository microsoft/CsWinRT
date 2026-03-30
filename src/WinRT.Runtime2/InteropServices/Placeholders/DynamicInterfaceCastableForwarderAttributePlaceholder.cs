// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System.Diagnostics.CodeAnalysis;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A placeholder <see cref="DynamicInterfaceCastableForwarderAttribute"/> type.
/// </summary>
internal sealed class DynamicInterfaceCastableForwarderAttributePlaceholder : DynamicInterfaceCastableForwarderAttribute
{
    /// <summary>
    /// The shared placeholder instance.
    /// </summary>
    public static DynamicInterfaceCastableForwarderAttributePlaceholder Instance = new();

    /// <inheritdoc/>
    public override bool IsInterfaceImplemented(WindowsRuntimeObject thisReference, [NotNullWhen(true)] out WindowsRuntimeObjectReference? interfaceReference)
    {
        interfaceReference = null;

        return false;
    }
}
#endif