// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Indicates a key value pair in the global hierarchy lookup to use to create managed instances of derived Windows Runtime types.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
public sealed class WindowsRuntimeTypeHierarchyKeyValuePairAttribute : Attribute
{
    /// <summary>
    /// Creates a new <see cref="WindowsRuntimeTypeHierarchyKeyValuePairAttribute"/> instance with the specified parameters.
    /// </summary>
    /// <param name="runtimeClassName">The runtime class name of the input Windows Runtime type.</param>
    /// <param name="baseRuntimeClassName">The resulting runtime class name of the base type for <paramref name="baseRuntimeClassName"/>.</param>
    public WindowsRuntimeTypeHierarchyKeyValuePairAttribute(string runtimeClassName, string baseRuntimeClassName)
    {
        RuntimeClassName = runtimeClassName;
        BaseRuntimeClassName = baseRuntimeClassName;
    }

    /// <summary>
    /// Gets the runtime class name of the input Windows Runtime type.
    /// </summary>
    public string RuntimeClassName { get; }

    /// <summary>
    /// Gets the resulting runtime class name of the base type for <see cref="RuntimeClassName"/>.
    /// </summary>
    public string BaseRuntimeClassName { get; }
}
