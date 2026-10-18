// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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

    /// <summary>Assembly display names cached only for finalized, single-threaded emission.</summary>
    private Dictionary<AssemblyDescriptor, string>? _assemblyNames;

    /// <summary>Resolved definition display names cached only for finalized, single-threaded emission.</summary>
    private Dictionary<TypeDefinition, string>? _definitionNames;

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
    /// Creates an invocation-local comparer that reuses display names during single-threaded emission.
    /// </summary>
    /// <param name="runtimeContext">The context whose type and assembly identities have been normalized.</param>
    /// <returns>A comparer with emission-local display-name caches.</returns>
    /// <remarks>Type names and assembly identities must remain unchanged for this comparer's lifetime.</remarks>
    public static TypeDescriptorComparer CreateForEmission(RuntimeContext runtimeContext)
    {
        return new(runtimeContext)
        {
            _assemblyNames = new(ReferenceEqualityComparer.Instance),
            _definitionNames = new(ReferenceEqualityComparer.Instance)
        };
    }

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

    /// <summary>
    /// Gets the same fully qualified name used as the primary ordering key by <see cref="Compare"/>.
    /// </summary>
    /// <param name="type">The descriptor to format.</param>
    /// <returns>The primary ordering key.</returns>
    public string GetOrderKey(ITypeDescriptor type)
    {
        DefaultInterpolatedStringHandler handler = new(0, 0, null, stackalloc char[256]);

        try
        {
            AppendFullyQualifiedName(type, _runtimeContext, ref handler, _assemblyNames, _definitionNames);

            return handler.Text.ToString();
        }
        finally
        {
            handler.Clear();
        }
    }

    /// <summary>
    /// Appends the resolved identity of a type, including constructed arguments and array shape.
    /// </summary>
    private static void AppendFullyQualifiedName(
        ITypeDescriptor type,
        RuntimeContext? runtimeContext,
        ref DefaultInterpolatedStringHandler handler,
        Dictionary<AssemblyDescriptor, string>? assemblyNames,
        Dictionary<TypeDefinition, string>? definitionNames)
    {
        // Resolving the outer type loses generic arguments or array shape, so format those recursively
        if (type is GenericInstanceTypeSignature generic)
        {
            AppendFullyQualifiedName(generic.GenericType, runtimeContext, ref handler, assemblyNames, definitionNames);
            handler.AppendLiteral("<");

            foreach (TypeSignature argument in generic.TypeArguments)
            {
                AppendFullyQualifiedName(argument, runtimeContext, ref handler, assemblyNames, definitionNames);
                handler.AppendLiteral(";");
            }

            handler.AppendLiteral(">");

            return;
        }

        if (type is SzArrayTypeSignature array)
        {
            AppendFullyQualifiedName(array.BaseType, runtimeContext, ref handler, assemblyNames, definitionNames);
            handler.AppendLiteral("[]");

            return;
        }

        // Resolve forwarded aliases to the same declaring assembly so reference scope cannot affect ordering
        if (runtimeContext is not null && type.TryResolve(runtimeContext, out TypeDefinition? definition))
        {
            type = definition;
        }

        if (definitionNames is not null && type is TypeDefinition resolved)
        {
            ref string? name = ref CollectionsMarshal.GetValueRefOrAddDefault(definitionNames, resolved, out _);
            name ??= FormatDefinitionName(resolved, assemblyNames);
            handler.AppendLiteral(name);

            return;
        }

        AppendNamedType(type, ref handler, assemblyNames);
    }

    /// <summary>Formats a resolved definition after its normal type resolution.</summary>
    /// <param name="type">The resolved definition.</param>
    /// <param name="assemblyNames">The optional assembly display-name cache.</param>
    /// <returns>The definition's fully qualified display name.</returns>
    private static string FormatDefinitionName(TypeDefinition type, Dictionary<AssemblyDescriptor, string>? assemblyNames)
    {
        DefaultInterpolatedStringHandler handler = new(0, 0, null, stackalloc char[256]);

        try
        {
            AppendNamedType(type, ref handler, assemblyNames);

            return handler.Text.ToString();
        }
        finally
        {
            handler.Clear();
        }
    }

    /// <summary>Appends a named type and its assembly identity.</summary>
    /// <param name="type">The named descriptor.</param>
    /// <param name="handler">The target text handler.</param>
    /// <param name="assemblyNames">The optional assembly display-name cache.</param>
    private static void AppendNamedType(
        ITypeDescriptor type,
        ref DefaultInterpolatedStringHandler handler,
        Dictionary<AssemblyDescriptor, string>? assemblyNames)
    {
        handler.AppendFormatted(type);

        // Definitions use a module scope, while references use an assembly scope.
        // Compare their assembly identities rather than their different display forms.
        if (type.Scope?.GetAssembly() is AssemblyDescriptor assembly)
        {
            handler.AppendLiteral(", ");
            if (assemblyNames is null)
            {
                handler.AppendFormatted(assembly);
            }
            else
            {
                ref string? name = ref CollectionsMarshal.GetValueRefOrAddDefault(assemblyNames, assembly, out _);
                name ??= assembly.ToString();
                handler.AppendLiteral(name);
            }
        }
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

        DefaultInterpolatedStringHandler xHandler = new(0, 0, null, stackalloc char[256]);
        DefaultInterpolatedStringHandler yHandler = new(0, 0, null, stackalloc char[256]);

        AppendFullyQualifiedName(x, _runtimeContext, ref xHandler, _assemblyNames, _definitionNames);
        AppendFullyQualifiedName(y, _runtimeContext, ref yHandler, _assemblyNames, _definitionNames);

        // Compare alphabetically without allocating the resulting 'string'
        int result = xHandler.Text.CompareTo(yHandler.Text, StringComparison.Ordinal);

        // If the scratch buffer wasn't enough and an array was rented, return it to the pool
        xHandler.Clear();
        yHandler.Clear();

        return result != 0 ? result : CompareEqualOrderKeys(x, y);
    }

    /// <summary>
    /// Applies the structural tie-break when descriptors have equal primary keys from this comparer.
    /// </summary>
    /// <param name="x">The first descriptor.</param>
    /// <param name="y">The second descriptor with the same primary key.</param>
    /// <returns>The structural ordering of the descriptors.</returns>
    public int CompareEqualOrderKeys(ITypeDescriptor x, ITypeDescriptor y)
    {
        if (ReferenceEquals(x, y))
        {
            return 0;
        }

        // Matching display keys can still hide different generic arguments or specification shapes
        if (x is GenericInstanceTypeSignature xGeneric && y is GenericInstanceTypeSignature yGeneric)
        {
            int result = xGeneric.TypeArguments.Count.CompareTo(yGeneric.TypeArguments.Count);

            for (int i = 0; result == 0 && i < xGeneric.TypeArguments.Count; i++)
            {
                result = Compare(xGeneric.TypeArguments[i], yGeneric.TypeArguments[i]);
            }

            return result;
        }

        return x is TypeSpecificationSignature xSpecification && y is TypeSpecificationSignature ySpecification
            ? Compare(xSpecification.BaseType, ySpecification.BaseType)
            : 0;
    }
}
