// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A placeholder for implementation types without a typed enumeration provider.
/// </summary>
internal sealed class DynamicInterfaceCastableEnumerableAttributePlaceholder : DynamicInterfaceCastableEnumerableAttribute
{
    /// <summary>
    /// The shared placeholder instance.
    /// </summary>
    public static readonly DynamicInterfaceCastableEnumerableAttributePlaceholder Instance = new();

    /// <inheritdoc/>
    public override bool IsInterfaceImplemented(WindowsRuntimeObject thisObject)
    {
        return false;
    }

    /// <inheritdoc/>
    protected override IEnumerator GetEnumeratorCore(WindowsRuntimeObject thisObject)
    {
        return null!;
    }
}
