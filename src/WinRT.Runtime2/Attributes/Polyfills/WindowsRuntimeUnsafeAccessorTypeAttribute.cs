// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;

namespace WindowsRuntime;

/// <summary>
/// Allows specifying an unaccessible type when using <see cref="System.Runtime.CompilerServices.UnsafeAccessorAttribute"/>.
/// </summary>
/// <remarks>
/// This type is a temporary polyfill leveraging IL rewriting provided by CsWinRT, until official support in .NET 11 is available.
/// See <see href="https://github.com/dotnet/runtime/issues/90081"/> for the tracking issue and related discussion.
/// </remarks>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = false, Inherited = false)]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class WindowsRuntimeUnsafeAccessorTypeAttribute : Attribute
{
    /// <summary>
    /// Instantiates an <see cref="WindowsRuntimeUnsafeAccessorTypeAttribute"/> providing access to a type supplied by <paramref name="typeName"/>.
    /// </summary>
    /// <param name="typeName">A fully qualified or partially qualified type name.</param>
    /// <remarks>
    /// <paramref name="typeName"/> is expected to follow the same rules as if it were being passed to <see cref="Type.GetType(string)"/>.
    /// </remarks>
    public WindowsRuntimeUnsafeAccessorTypeAttribute(string typeName)
    {
        TypeName = typeName;
    }

    /// <summary>
    /// Fully qualified or partially qualified type name to target.
    /// </summary>
    public string TypeName { get; }
}
