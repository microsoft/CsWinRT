// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Generation;

namespace WindowsRuntime.ProjectionWriter.Helpers;

/// <summary>
/// Helpers for composable (unsealed) authored runtime classes and the COM aggregation support they need.
/// </summary>
/// <remarks>
/// A composable runtime class carries <c>[Composable]</c> and can be derived from by native code (or by another
/// language projection). When that happens, the instance produced by the composition factory becomes the inner
/// object of a COM aggregate, and every interface pointer that aggregate hands out has to delegate <c>IUnknown</c>
/// to the controlling outer object. CsWinRT does that entirely at runtime, by giving the CCW of the aggregated
/// object a private copy of the vtable of each interface it can expose (see <c>WindowsRuntimeAggregationInner</c>),
/// which leaves the CCW-s of every other (non aggregated) object completely untouched. These helpers identify the
/// composition factory interfaces, and enumerate the interfaces the runtime needs those vtable copies for.
/// </remarks>
internal static class ComposableTypeHelpers
{
    /// <summary>
    /// Checks whether a given interface is the composition factory interface of an authored runtime class.
    /// </summary>
    /// <param name="context">The active projection emit context.</param>
    /// <param name="interfaceType">The interface to check.</param>
    /// <returns>Whether <paramref name="interfaceType"/> is a composition factory interface.</returns>
    public static bool IsComposableFactoryInterface(ProjectionEmitContext context, TypeDefinition interfaceType)
    {
        if (!context.Settings.Component || !interfaceType.IsExclusiveTo)
        {
            return false;
        }

        TypeDefinition? owner = AbiTypeHelpers.GetExclusiveToType(context.Cache, interfaceType);

        if (owner is null)
        {
            return false;
        }

        foreach (KeyValuePair<string, AttributedType> pair in AttributedTypes.Get(owner, context.Cache))
        {
            if (pair.Value.Composable && pair.Value.Type == interfaceType)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks whether a given method on a composition factory interface is a composition factory method.
    /// </summary>
    /// <param name="method">The method to check.</param>
    /// <returns>Whether <paramref name="method"/> is a composition factory method.</returns>
    /// <remarks>
    /// Composition factory methods always have at least the trailing <c>baseInterface</c> (the controlling outer)
    /// and <c>innerInterface</c> (the non-delegating inner) parameters, on top of the authored constructor ones.
    /// </remarks>
    public static bool IsComposableFactoryMethod(MethodDefinition method)
    {
        return !method.IsSpecialName && method.Signature is { ParameterTypes.Count: >= 2 };
    }

    /// <summary>
    /// Checks whether a given interface is the <c>[Protected]</c> or <c>[Overridable]</c> interface of an
    /// authored runtime class.
    /// </summary>
    /// <param name="context">The active projection emit context.</param>
    /// <param name="interfaceType">The interface to check.</param>
    /// <returns>Whether <paramref name="interfaceType"/> carries members that are not public on the authored class.</returns>
    /// <remarks>
    /// <para>
    /// The members of these two interfaces are <c>protected</c> (and, for the overridable one, possibly
    /// <c>public virtual</c>) on the authored C# class, and the generated projection lives in a separate
    /// assembly (<c>WinRT.Component.dll</c>), so their CCW bodies cannot call them directly. They dispatch
    /// through an <c>[UnsafeAccessor]</c> instead, which works regardless of accessibility and still
    /// resolves virtual members through the vtable of the actual instance (so a C# class deriving from the
    /// composable base still gets its override called).
    /// </para>
    /// <para>
    /// Note that <c>[Protected]</c> and <c>[Overridable]</c> live on the interface implementation of the
    /// runtime class (this is where MIDL puts them too), not on the interface itself, so the owning class
    /// has to be resolved through <c>[ExclusiveTo]</c> first.
    /// </para>
    /// </remarks>
    public static bool IsProtectedOrOverridableInterface(ProjectionEmitContext context, TypeDefinition interfaceType)
    {
        if (!context.Settings.Component || !interfaceType.IsExclusiveTo)
        {
            return false;
        }

        TypeDefinition? owner = AbiTypeHelpers.GetExclusiveToType(context.Cache, interfaceType);

        if (owner is null)
        {
            return false;
        }

        foreach (InterfaceImplementation interfaceImplementation in owner.Interfaces)
        {
            if (!interfaceImplementation.TryResolveTypeDef(context.Cache, out TypeDefinition? resolvedInterface) ||
                resolvedInterface != interfaceType)
            {
                continue;
            }

            if (interfaceImplementation.IsOverridable() || interfaceImplementation.IsProtected())
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets all interfaces the CCW of a given composable runtime class can hand out while it is aggregated.
    /// </summary>
    /// <param name="context">The active projection emit context.</param>
    /// <param name="classType">The composable runtime class.</param>
    /// <returns>The resulting interfaces, in a deterministic order.</returns>
    /// <remarks>
    /// <para>
    /// This is the transitive closure of the interfaces implemented by the runtime class and by all of its
    /// authored base classes (interface inheritance included, since the CCW carries an entry for every required
    /// interface as well). Those are exactly the interfaces the non-delegating inner object of the aggregate can
    /// be asked for, so they are the ones the runtime needs a per-aggregate delegating vtable copy for.
    /// </para>
    /// <para>
    /// The interfaces every CCW carries on top of these (e.g. <c>IStringable</c>, <c>IWeakReferenceSource</c>,
    /// <c>IMarshal</c>, and <c>IAgileObject</c>) are deliberately not included: their vtables are shared across the
    /// whole application, so they cannot be handed out by an aggregate without giving it a second COM identity.
    /// Exactly like in C++/WinRT, the controlling outer object is the one that implements them.
    /// </para>
    /// </remarks>
    public static List<TypeDefinition> GetAggregableInterfaces(ProjectionEmitContext context, TypeDefinition classType)
    {
        List<TypeDefinition> result = [];
        HashSet<TypeDefinition> visitedInterfaces = [];
        HashSet<TypeDefinition> visitedClasses = [];

        // Adds an interface and all the interfaces it requires (transitively) to the result set
        void AddInterfaceClosure(TypeDefinition interfaceType)
        {
            if (!visitedInterfaces.Add(interfaceType))
            {
                return;
            }

            result.Add(interfaceType);

            foreach (InterfaceImplementation requiredInterface in interfaceType.Interfaces)
            {
                if (requiredInterface.TryResolveTypeDef(context.Cache, out TypeDefinition? requiredInterfaceType))
                {
                    AddInterfaceClosure(requiredInterfaceType);
                }
            }
        }

        // Walk the runtime class hierarchy: an aggregated object exposes the interfaces of its own
        // runtime class, plus all the ones inherited from its authored base classes.
        TypeDefinition? currentType = classType;

        while (currentType is not null &&
               currentType.DeclaringModule == classType.DeclaringModule &&
               visitedClasses.Add(currentType))
        {
            foreach (InterfaceImplementation interfaceImplementation in currentType.Interfaces)
            {
                if (interfaceImplementation.TryResolveTypeDef(context.Cache, out TypeDefinition? interfaceType))
                {
                    AddInterfaceClosure(interfaceType);
                }
            }

            currentType = currentType.BaseType?.ResolveAsTypeDefinition(context.Cache);
        }

        return result;
    }
}
