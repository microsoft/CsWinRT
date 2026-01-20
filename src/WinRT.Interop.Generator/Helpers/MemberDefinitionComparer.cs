// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AsmResolver.DotNet;

namespace WindowsRuntime.InteropGenerator.Helpers;

/// <summary>
/// A comparer for <see cref="IMemberDefinition"/> values.
/// </summary>
internal sealed class MemberDefinitionComparer : IComparer<IMemberDefinition>
{
    /// <summary>
    /// Creates a new <see cref="MemberDefinitionComparer"/> instance.
    /// </summary>
    private MemberDefinitionComparer()
    {
    }

    /// <summary>
    /// Gets the singleton <see cref="MemberDefinitionComparer"/> instance.
    /// </summary>
    public static MemberDefinitionComparer Instance { get; } = new();

    /// <summary>
    /// Creates a new comparer for the specified <see cref="IMemberDefinition"/> type.
    /// </summary>
    /// <typeparam name="T">The <see cref="IMemberDefinition"/> type to compare.</typeparam>
    /// <returns>The resulting <see cref="IComparer{T}"/> instance.</returns>
    public static IComparer<T> Create<T>()
        where T : IMemberDefinition
    {
        return (IComparer<T>)(IComparer<IMemberDefinition>)Instance;
    }

    /// <inheritdoc/>
    public int Compare(IMemberDefinition? x, IMemberDefinition? y)
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

        // Appends the fully qualified name of a member to a target handler
        static void AppendFullyQualifiedName(IMemberDefinition member, ref DefaultInterpolatedStringHandler handler)
        {
            handler.AppendFormatted(member);

            if (member.DeclaringModule is ModuleDefinition module)
            {
                handler.AppendLiteral(", ");
                handler.AppendFormatted(module);
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
