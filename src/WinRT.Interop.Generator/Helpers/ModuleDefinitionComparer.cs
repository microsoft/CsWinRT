// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AsmResolver.DotNet;

namespace WindowsRuntime.InteropGenerator.Helpers;

/// <summary>
/// A comparer for <see cref="ModuleDefinition"/> values.
/// </summary>
internal sealed class ModuleDefinitionComparer : IComparer<ModuleDefinition>
{
    /// <summary>
    /// Creates a new <see cref="ModuleDefinitionComparer"/> instance.
    /// </summary>
    private ModuleDefinitionComparer()
    {
    }

    /// <summary>
    /// Gets the singleton <see cref="ModuleDefinitionComparer"/> instance.
    /// </summary>
    public static ModuleDefinitionComparer Instance { get; } = new();

    /// <inheritdoc/>
    public int Compare(ModuleDefinition? x, ModuleDefinition? y)
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
        static void AppendFullyQualifiedName(ModuleDefinition module, ref DefaultInterpolatedStringHandler handler)
        {
            handler.AppendFormatted(module.Name);

            if (module.Assembly is AssemblyDefinition assembly)
            {
                handler.AppendLiteral("[");
                handler.AppendFormatted(assembly);
                handler.AppendLiteral("]");
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
