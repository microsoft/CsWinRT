// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.Generator;
using WindowsRuntime.ProjectionWriter.References;

namespace WindowsRuntime.ProjectionWriter.Metadata;

/// <summary>
/// Decides whether the activation factory generated for an authored Windows Runtime class must
/// force the class constructor to run before activation, i.e. whether the class (or an authored
/// base class) registers any XAML dependency properties.
/// </summary>
/// <remarks>
/// Dependency properties are registered in <c>static</c> fields of type <c>DependencyProperty</c>,
/// so they only exist in the managed implementation assemblies, not in the <c>.winmd</c> metadata.
/// The analyzer resolves each queried type lazily via <see cref="ComponentImplementationMetadata"/>
/// and walks its base chain on demand, memoizing the result per visited type so that shared
/// hierarchies (e.g. several controls deriving from a common authored base) are traversed once. A
/// single instance is shared by all per-namespace emit contexts and queried from the parallel
/// emission work items, so the memoization cache is concurrent.
/// </remarks>
internal sealed class ComponentStaticConstructorAnalyzer
{
    /// <summary>The resolver used to look up authored types (and their base types) by full name.</summary>
    private readonly ComponentImplementationMetadata _metadata;

    /// <summary>
    /// Memoizes, per visited type, whether it requires the class constructor to run. Concurrent
    /// because the analyzer is queried from the parallel emission work items. Keyed with the
    /// version-agnostic <see cref="SignatureComparer"/>, the standard comparer for AsmResolver entities.
    /// </summary>
    private readonly ConcurrentDictionary<TypeDefinition, bool> _cache = new(SignatureComparer.IgnoreVersion);

    /// <summary>
    /// Creates a new <see cref="ComponentStaticConstructorAnalyzer"/> over the given metadata.
    /// </summary>
    /// <param name="metadata">The resolver for the component's managed implementation types.</param>
    public ComponentStaticConstructorAnalyzer(ComponentImplementationMetadata metadata)
    {
        _metadata = metadata;
    }

    /// <summary>
    /// Determines whether the activation factory for the authored type identified by
    /// <paramref name="typeFullName"/> must force the type's class constructor to run before
    /// activation.
    /// </summary>
    /// <param name="typeFullName">The full name of the authored type.</param>
    /// <returns><see langword="true"/> if the static constructor is required; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// If the type cannot be resolved from the scanned implementation assemblies, this returns
    /// <see langword="true"/>: we cannot prove the constructor is unnecessary, so it is kept (this
    /// also covers the case where no implementation assemblies are available).
    /// </remarks>
    public bool RequiresStaticConstructor(string typeFullName)
    {
        TypeDefinition? type = _metadata.Resolve(typeFullName);

        return type is null || RequiresStaticConstructor(type);
    }

    /// <summary>
    /// Memoized core: whether <paramref name="type"/> (or an authored base type) registers a
    /// dependency property.
    /// </summary>
    /// <param name="type">The resolved authored type to inspect.</param>
    /// <returns><see langword="true"/> if the static constructor is required; otherwise, <see langword="false"/>.</returns>
    private bool RequiresStaticConstructor(TypeDefinition type)
    {
        if (_cache.TryGetValue(type, out bool cached))
        {
            return cached;
        }

        // The type itself registers a dependency property, or an authored base type does. The base
        // walk stops at the first type outside the scanned assemblies (e.g. a framework base type),
        // because framework dependency properties are registered by the framework itself. Inheritance
        // can't cycle, so a plain recursion (memoized below) always terminates.
        bool result =
            DeclaresDependencyProperty(type) ||
            (type.BaseType is { } baseType &&
             _metadata.Resolve(baseType.FullName) is { } baseDefinition &&
             RequiresStaticConstructor(baseDefinition));

        _cache[type] = result;

        return result;
    }

    /// <summary>
    /// Returns whether <paramref name="type"/> declares any <c>static</c> field whose type is the
    /// XAML <c>DependencyProperty</c> (in either <c>Microsoft.UI.Xaml</c> or <c>Windows.UI.Xaml</c>).
    /// </summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns><see langword="true"/> if the type declares such a field; otherwise, <see langword="false"/>.</returns>
    private static bool DeclaresDependencyProperty(TypeDefinition type)
    {
        foreach (FieldDefinition field in type.Fields)
        {
            if (!field.IsStatic)
            {
                continue;
            }

            TypeSignature? fieldType = field.Signature?.FieldType;

            if (fieldType is null)
            {
                continue;
            }

            (string ns, string name) = fieldType.Names();

            if (name == WellKnownTypeNames.DependencyProperty &&
                (ns == WellKnownNamespaces.MicrosoftUIXaml || ns == WellKnownNamespaces.WindowsUIXaml))
            {
                return true;
            }
        }

        return false;
    }
}
