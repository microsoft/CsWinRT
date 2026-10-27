// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.ProjectionWriter.Factories;
using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Metadata;
using WindowsRuntime.ProjectionWriter.Writers;
using static WindowsRuntime.ProjectionWriter.References.WellKnownNamespaces;

namespace WindowsRuntime.ProjectionWriter.Helpers;

/// <summary>
/// Analyzes required interfaces to determine support for typed enumeration.
/// </summary>
internal static class EnumerableInterfaceHelpers
{
    /// <summary>
    /// Finds the single closed element type supported by a projected interface's required-interface graph.
    /// </summary>
    /// <param name="context">The active projection context, including metadata resolution and type mappings.</param>
    /// <param name="type">The projected interface whose required interfaces should be inspected.</param>
    /// <returns>
    /// The fully qualified projected C# element type, or <see langword="null"/> if the graph is incomplete,
    /// has no unique closed element type, or requires bindable enumeration.
    /// </returns>
    /// <remarks>
    /// Follows the projection's inheritance filtering and substitutes generic arguments at each edge.
    /// Bindable requirements take priority over generic enumeration; unresolved or ambiguous requirements
    /// must not cause an arbitrary typed enumeration provider to be selected.
    /// </remarks>
    public static string? GetUnambiguousElementType(ProjectionEmitContext context, TypeDefinition type)
    {
        HashSet<string> visited = new(StringComparer.Ordinal);
        HashSet<string> elements = new(StringComparer.Ordinal);
        bool isBindable = false;
        bool isComplete = true;

        Visit(type);

        return isComplete && !isBindable && elements.Count == 1 ? elements.Single() : null;

        void Visit(ITypeDefOrRef interfaceType)
        {
            // Deduplicate diamonds by constructed name, not definition: IIterable<int> and
            // IIterable<string> must both be visited so conflicting element types remain visible.
            if (!visited.Add(interfaceType.FullName))
            {
                return;
            }

            TypeDefinition? definition = interfaceType.ResolveAsTypeDefinition(context.Cache);

            // A missing branch could introduce another element type or a bindable requirement,
            // so partial metadata is not sufficient to select a generic enumeration provider.
            if (definition is null)
            {
                isComplete = false;
                return;
            }

            (string ns, string name) = definition.Names();

            // Either XAML flavor can require the non-generic ABI; preserve its dispatch priority
            // even if another required interface also supplies a generic element type.
            isBindable |= (ns is "Microsoft.UI.Xaml.Interop" or WindowsUIXamlInterop) && name == "IBindableIterable";

            GenericContext? genericContext = null;

            if (interfaceType.TryGetGenericInstance(out GenericInstanceTypeSignature? instance))
            {
                genericContext = new GenericContext(instance, null);

                if (ns == WindowsFoundationCollections && name == "IIterable`1")
                {
                    if (instance.TypeArguments.Count != 1 || !IsClosed(instance.TypeArguments[0]))
                    {
                        isComplete = false;
                    }
                    else
                    {
                        // Compare the projected names that the provider will use, including
                        // custom mappings such as IKeyValuePair<K, V> to KeyValuePair<K, V>.
                        _ = elements.Add(TypedefNameWriter.WriteTypeName(
                            context, TypeSemanticsFactory.Get(instance.TypeArguments[0]), TypedefNameType.Projected, true).Format());
                    }
                }
            }
            else if (definition.GenericParameters.Count != 0)
            {
                // An uninstantiated generic definition cannot provide callable closed support
                isComplete = false;
            }

            foreach (InterfaceImplementation implementation in definition.Interfaces)
            {
                if (implementation.Interface is not { } requiredType ||
                    !implementation.TryResolveTypeDef(context.Cache, out TypeDefinition? requiredDefinition))
                {
                    isComplete = false;
                    continue;
                }

                // Analyze only inheritance edges that are actually exposed by the projection
                if (!InterfaceFactory.ShouldIncludeInterfaceInInheritance(implementation, requiredDefinition.IsExclusiveTo, includeExclusiveInterface: false))
                {
                    continue;
                }

                if (genericContext is { } constructedContext)
                {
                    // Carry arguments through every level, for example IObservableMap<int, string>
                    // -> IMap<int, string> -> IIterable<IKeyValuePair<int, string>>.
                    requiredType = requiredType.ToTypeSignature(false).InstantiateGenericTypes(constructedContext).ToTypeDefOrRef();
                }

                Visit(requiredType);
            }
        }

        static bool IsClosed(TypeSignature signature)
        {
            // Element types can themselves be generic; reject parameters at any nesting depth
            return signature is not GenericParameterSignature &&
                (signature is not GenericInstanceTypeSignature instance || instance.TypeArguments.All(IsClosed));
        }
    }
}
