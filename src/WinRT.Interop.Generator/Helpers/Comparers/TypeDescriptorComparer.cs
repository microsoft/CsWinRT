// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;

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

            // Definitions use a module scope, while references use an assembly scope.
            // Compare their assembly identities rather than their different display forms.
            if (type.Scope?.GetAssembly() is AssemblyDescriptor assembly)
            {
                handler.AppendLiteral(", ");
                handler.AppendFormatted(assembly);
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

        if (result == 0)
        {
            // Display names omit the assembly scopes of generic arguments. For example, a type
            // and a reference through its forwarding assembly can otherwise sort as equal.
            if (x is GenericInstanceTypeSignature xGeneric && y is GenericInstanceTypeSignature yGeneric)
            {
                result = xGeneric.TypeArguments.Count.CompareTo(yGeneric.TypeArguments.Count);

                for (int i = 0; result == 0 && i < xGeneric.TypeArguments.Count; i++)
                {
                    result = Compare(xGeneric.TypeArguments[i], yGeneric.TypeArguments[i]);
                }
            }
            else if (x is TypeSpecificationSignature xSpecification && y is TypeSpecificationSignature ySpecification)
            {
                result = Compare(xSpecification.BaseType, ySpecification.BaseType);
            }
        }

        return result;
    }
}
