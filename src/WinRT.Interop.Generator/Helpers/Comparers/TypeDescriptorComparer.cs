// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AsmResolver.DotNet;

namespace WindowsRuntime.InteropGenerator.Helpers;

/// <summary>
/// A comparer for <see cref="ITypeDescriptor"/> values.
/// </summary>
internal sealed class TypeDescriptorComparer : IComparer<ITypeDescriptor>
{
    /// <summary>
    /// Creates a new <see cref="TypeDescriptorComparer"/> instance.
    /// </summary>
    private TypeDescriptorComparer()
    {
    }

    /// <summary>
    /// Gets the singleton <see cref="TypeDescriptorComparer"/> instance.
    /// </summary>
    public static TypeDescriptorComparer Instance { get; } = new();

    /// <summary>
    /// Creates a new comparer for the specified <see cref="ITypeDescriptor"/> type.
    /// </summary>
    /// <typeparam name="T">The <see cref="ITypeDescriptor"/> type to compare.</typeparam>
    /// <returns>The resulting <see cref="IComparer{T}"/> instance.</returns>
    public static IComparer<T> Create<T>()
        where T : ITypeDescriptor
    {
        return (IComparer<T>)(IComparer<ITypeDescriptor>)Instance;
    }

    /// <inheritdoc/>
    public int Compare(ITypeDescriptor? x, ITypeDescriptor? y)
    {
        if (ReferenceEquals(x, y))
        {
            return 0;
        }

        if (x is null)
        {
            return -1;
        }

        if (y is null)
        {
            return 1;
        }

        // Appends the fully qualified name of a type to a target handler
        static void AppendFullyQualifiedName(ITypeDescriptor type, ref DefaultInterpolatedStringHandler handler)
        {
            handler.AppendFormatted(type);

            if (type.Scope is IResolutionScope scope)
            {
                handler.AppendLiteral(", ");
                handler.AppendFormatted(scope);
            }
        }

        DefaultInterpolatedStringHandler xHandler = new(0, 0, null, stackalloc char[256]);
        DefaultInterpolatedStringHandler yHandler = new(0, 0, null, stackalloc char[256]);

        AppendFullyQualifiedName(x, ref xHandler);
        AppendFullyQualifiedName(y, ref yHandler);

        // Compare alphabetically without allocating the resulting 'string'
        int result = xHandler.Text.CompareTo(yHandler.Text, StringComparison.Ordinal);

        // If the scratch buffer wasn't enough and an array was rented, return it to the pool
        xHandler.Clear();
        yHandler.Clear();

        return result;
    }
}
