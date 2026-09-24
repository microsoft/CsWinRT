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
    /// <summary>The context used to order equivalent forwarded names consistently.</summary>
    private readonly RuntimeContext? _runtimeContext;

    /// <summary>
    /// Creates a new <see cref="TypeDescriptorComparer"/> instance.
    /// </summary>
    /// <param name="runtimeContext">The context for ordering resolved type identities.</param>
    public TypeDescriptorComparer(RuntimeContext? runtimeContext)
    {
        _runtimeContext = runtimeContext;
    }

    /// <summary>
    /// Gets a shared comparer that orders type descriptors without resolving them.
    /// </summary>
    /// <remarks>
    /// This instance has no associated <see cref="RuntimeContext"/> and does not follow type forwarding.
    /// Equivalent types referenced through different assembly scopes can therefore sort differently.
    /// Use a comparer constructed with a runtime context when ordering resolved type identities.
    /// </remarks>
    public static TypeDescriptorComparer Default { get; } = new(null);

    /// <summary>
    /// Creates a new comparer for the specified <see cref="ITypeDescriptor"/> type.
    /// </summary>
    /// <typeparam name="T">The <see cref="ITypeDescriptor"/> type to compare.</typeparam>
    /// <param name="runtimeContext">The context for ordering resolved type identities, or <see langword="null"/> to use the context-free <see cref="Default"/> comparer.</param>
    /// <returns>The resulting <see cref="IComparer{T}"/> instance.</returns>
    public static IComparer<T> Create<T>(RuntimeContext? runtimeContext)
        where T : ITypeDescriptor
    {
        return (IComparer<T>)(IComparer<ITypeDescriptor>)(runtimeContext is null ? Default : new TypeDescriptorComparer(runtimeContext));
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
        static void AppendFullyQualifiedName(ITypeDescriptor type, RuntimeContext? runtimeContext, ref DefaultInterpolatedStringHandler handler)
        {
            // Resolving the outer type loses generic arguments or array shape, so format those recursively
            if (type is GenericInstanceTypeSignature generic)
            {
                AppendFullyQualifiedName(generic.GenericType, runtimeContext, ref handler);
                handler.AppendLiteral("<");

                foreach (TypeSignature argument in generic.TypeArguments)
                {
                    AppendFullyQualifiedName(argument, runtimeContext, ref handler);
                    handler.AppendLiteral(";");
                }

                handler.AppendLiteral(">");

                return;
            }

            if (type is SzArrayTypeSignature array)
            {
                AppendFullyQualifiedName(array.BaseType, runtimeContext, ref handler);
                handler.AppendLiteral("[]");

                return;
            }

            // Resolve forwarded aliases to the same declaring assembly so reference scope cannot affect ordering
            if (runtimeContext is not null && type.TryResolve(runtimeContext, out TypeDefinition? definition))
            {
                type = definition;
            }

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

        AppendFullyQualifiedName(x, _runtimeContext, ref xHandler);
        AppendFullyQualifiedName(y, _runtimeContext, ref yHandler);

        // Compare alphabetically without allocating the resulting 'string'
        int result = xHandler.Text.CompareTo(yHandler.Text, StringComparison.Ordinal);

        // If the scratch buffer wasn't enough and an array was rented, return it to the pool
        xHandler.Clear();
        yHandler.Clear();

        return result;
    }
}
