// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.Generator;
using WindowsRuntime.InteropGenerator.Discovery;
using WindowsRuntime.InteropGenerator.Helpers;
using WindowsRuntime.InteropGenerator.Visitors;

namespace WindowsRuntime.InteropGenerator;

/// <summary>
/// Extensions for the <see cref="ModuleDefinition"/> type.
/// </summary>
internal static partial class ModuleDefinitionExtensions
{
    /// <summary>
    /// Gets the first type with a given namespace and name from the specified type.
    /// </summary>
    /// <param name="module">The input <see cref="ModuleDefinition"/> instance.</param>
    /// <param name="ns">The namespace of the type.</param>
    /// <param name="name">The name of the type to get.</param>
    /// <returns>The resulting type.</returns>
    /// <exception cref="ArgumentException">Thrown if the type couldn't be found.</exception>
    public static TypeDefinition GetType(this ModuleDefinition module, Utf8String ns, Utf8String name)
    {
        return TryGetType(module, ns, name, out TypeDefinition? type)
            ? type
            : throw new ArgumentException($"Type with name '{ns}.{name}' not found.");
    }

    /// <summary>
    /// Tries to get the first type with a given namespace and name from the specified type.
    /// </summary>
    /// <param name="module">The input <see cref="ModuleDefinition"/> instance.</param>
    /// <param name="ns">The namespace of the type.</param>
    /// <param name="name">The name of the type to get.</param>
    /// <param name="type">The resulting type, if found.</param>
    /// <returns>Whether <paramref name="type"/> was found.</returns>
    public static bool TryGetType(this ModuleDefinition module, Utf8String? ns, Utf8String? name, [NotNullWhen(true)] out TypeDefinition? type)
    {
        foreach (TypeDefinition item in module.TopLevelTypes)
        {
            if (item.Namespace == ns && item.Name == name)
            {
                type = item;

                return true;
            }
        }

        type = null;

        return false;
    }

    /// <summary>
    /// Checks whether a <see cref="ModuleDefinition"/> references the Windows Runtime assembly.
    /// </summary>
    /// <param name="module">The input <see cref="ModuleDefinition"/> instance.</param>
    /// <param name="assemblyName">The name of the assembly to check for references to.</param>
    /// <returns>Whether the module references the Windows Runtime assembly.</returns>
    public static bool ReferencesAssembly(this ModuleDefinition module, Utf8String assemblyName)
    {
        // Use a visited set to guard against cycles in the transitive assembly reference graph.
        // Such cycles are possible (eg. mutual references between assemblies) and would otherwise
        // cause this method to recurse infinitely, leading to a stack overflow.
        static bool ReferencesAssemblyCore(ModuleDefinition module, Utf8String assemblyName, HashSet<ModuleDefinition> visitedModules)
        {
            // Skip modules we've already visited, to break reference cycles
            if (!visitedModules.Add(module))
            {
                return false;
            }

            // Check all direct assembly references and check if they match
            foreach (AssemblyReference reference in module.AssemblyReferences)
            {
                if (reference.Name == assemblyName)
                {
                    return true;
                }

                // Try to resolve the current assembly, skip it if it fails
                if (!reference.TryResolve(module.RuntimeContext, out AssemblyDefinition? assembly))
                {
                    continue;
                }

                // Also traverse the entire transitive dependency graph and check those assemblies
                foreach (ModuleDefinition transitiveModule in assembly.Modules ?? [])
                {
                    if (ReferencesAssemblyCore(transitiveModule, assemblyName, visitedModules))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        return ReferencesAssemblyCore(module, assemblyName, new HashSet<ModuleDefinition>(SignatureComparer.IgnoreVersion));
    }

    /// <summary>
    /// Enumerates all (transitive) assembly references for a given <see cref="ModuleDefinition"/>.
    /// </summary>
    /// <param name="module">The input <see cref="ModuleDefinition"/> instance.</param>
    /// <returns>All (transitive) assembly references for <paramref name="module"/>.</returns>
    public static IEnumerable<AssemblyReference> EnumerateAssemblyReferences(this ModuleDefinition module)
    {
        // Use a visited set to guard against cycles in the transitive assembly reference graph,
        // which would otherwise cause infinite recursion and a stack overflow (see 'ReferencesAssembly').
        static IEnumerable<AssemblyReference> EnumerateAssemblyReferencesCore(ModuleDefinition module, HashSet<ModuleDefinition> visitedModules)
        {
            // Skip modules we've already visited, to break reference cycles
            if (!visitedModules.Add(module))
            {
                yield break;
            }

            foreach (AssemblyReference reference in module.AssemblyReferences)
            {
                yield return reference;

                // Try to resolve the current assembly, skip it if it fails
                if (!reference.TryResolve(module.RuntimeContext, out AssemblyDefinition? assembly))
                {
                    continue;
                }

                // Also enumerate all transitive references as well
                foreach (ModuleDefinition transitiveModule in assembly.Modules ?? [])
                {
                    foreach (AssemblyReference transitiveReference in EnumerateAssemblyReferencesCore(transitiveModule, visitedModules))
                    {
                        yield return transitiveReference;
                    }
                }
            }
        }

        return EnumerateAssemblyReferencesCore(module, new HashSet<ModuleDefinition>(SignatureComparer.IgnoreVersion));
    }

    /// <summary>
    /// Enumerates all generic instance type signatures in the module.
    /// </summary>
    /// <param name="module">The input <see cref="ModuleDefinition"/> instance.</param>
    /// <param name="signatureComparer">The comparer for discovered signatures.</param>
    /// <param name="shouldProcessModule">Determines whether to transitively discover members in a referenced module.</param>
    /// <param name="isMarshallingDisabledModule">Determines whether a module was explicitly excluded from member discovery.</param>
    /// <param name="treatWarningsAsErrors">Whether to promote discovery warnings to errors.</param>
    /// <param name="token">The cancellation token for discovery.</param>
    /// <returns>All (unique) generic type signatures in the module.</returns>
    public static IEnumerable<GenericInstanceTypeSignature> EnumerateGenericInstanceTypeSignatures(
        this ModuleDefinition module,
        SignatureComparer signatureComparer,
        Func<ModuleDefinition, bool> shouldProcessModule,
        Func<ModuleDefinition, bool> isMarshallingDisabledModule,
        bool treatWarningsAsErrors,
        CancellationToken token)
    {
        return TypeSignatureDiscovery<GenericInstanceTypeSignature>.Enumerate(
            module: module,
            visitor: AllGenericTypesVisitor.Instance,
            signatureComparer: signatureComparer,
            shouldProcessModule: shouldProcessModule,
            isMarshallingDisabledModule: isMarshallingDisabledModule,
            treatWarningsAsErrors: treatWarningsAsErrors,
            token: token);
    }

    /// <summary>
    /// Enumerates all SZ array type signatures in the module.
    /// </summary>
    /// <param name="module">The input <see cref="ModuleDefinition"/> instance.</param>
    /// <param name="signatureComparer">The comparer for discovered signatures.</param>
    /// <param name="shouldProcessModule">Determines whether to transitively discover members in a referenced module.</param>
    /// <param name="isMarshallingDisabledModule">Determines whether a module was explicitly excluded from member discovery.</param>
    /// <param name="treatWarningsAsErrors">Whether to promote discovery warnings to errors.</param>
    /// <param name="token">The cancellation token for discovery.</param>
    /// <returns>All (unique) generic type signatures in the module.</returns>
    public static IEnumerable<SzArrayTypeSignature> EnumerateSzArrayTypeSignatures(
        this ModuleDefinition module,
        SignatureComparer signatureComparer,
        Func<ModuleDefinition, bool> shouldProcessModule,
        Func<ModuleDefinition, bool> isMarshallingDisabledModule,
        bool treatWarningsAsErrors,
        CancellationToken token)
    {
        return TypeSignatureDiscovery<SzArrayTypeSignature>.Enumerate(
            module: module,
            visitor: AllSzArrayTypesVisitor.Instance,
            signatureComparer: signatureComparer,
            shouldProcessModule: shouldProcessModule,
            isMarshallingDisabledModule: isMarshallingDisabledModule,
            treatWarningsAsErrors: treatWarningsAsErrors,
            token: token);
    }

    /// <summary>
    /// Sorts the <see cref="ModuleDefinition"/> values of a sequence in ascending order, based on their fully qualified names.
    /// </summary>
    /// <returns>An <see cref="IEnumerable{T}"/> whose elements are sorted.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="modules"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// This method is implemented by using deferred execution. The immediate return value is an object that stores all the
    /// information that is required to perform the action. The query represented by this method is not executed until the
    /// object is enumerated by calling its <see cref="IEnumerable{T}.GetEnumerator"/> method.
    /// </remarks>
    public static IEnumerable<ModuleDefinition> OrderByFullyQualifiedName(this IEnumerable<ModuleDefinition> modules)
    {
        return modules.Order(ModuleDefinitionComparer.Instance);
    }
}
