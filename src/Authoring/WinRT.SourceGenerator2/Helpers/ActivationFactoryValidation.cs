// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.CodeAnalysis;

#pragma warning disable IDE0046

namespace WindowsRuntime.SourceGenerator;

/// <summary>
/// Validation of the activation factories for Windows Runtime classes defined in existing metadata, shared by the source
/// generator and the analyzers, so that the generator registers exactly the factories the analyzers consider valid.
/// </summary>
internal static class ActivationFactoryValidation
{
    /// <summary>
    /// Gets the Windows Runtime class activated by a valid factory declared with <c>[WindowsRuntimeActivationFactory]</c>
    /// that derives from a generated activation factory base.
    /// </summary>
    /// <param name="type">The annotated type.</param>
    /// <param name="attribute">The <c>[WindowsRuntimeActivationFactory]</c> attribute on <paramref name="type"/>.</param>
    /// <param name="compilation">The current <see cref="Compilation"/>.</param>
    /// <param name="implementableClasses">The lookup for the generated bases.</param>
    /// <param name="factoryBase">The generated activation factory base <paramref name="type"/> derives from, if valid.</param>
    /// <returns>The Windows Runtime class name, or <see langword="null"/> if <paramref name="type"/> is not a valid such factory.</returns>
    public static string? GetImplementedFactoryRuntimeClassName(
        INamedTypeSymbol type,
        AttributeData attribute,
        Compilation compilation,
        ImplementableClassLookup implementableClasses,
        out INamedTypeSymbol? factoryBase)
    {
        factoryBase = null;

        if (type.TypeKind is not TypeKind.Class ||
            implementableClasses.GetFactoryBase(type.BaseType) is not INamedTypeSymbol candidateFactoryBase ||
            implementableClasses.GetFactoryRuntimeClassName(candidateFactoryBase) is not string runtimeClassName ||
            GetInvalidImplementedFactoryReason(type, candidateFactoryBase, compilation) is not null)
        {
            return null;
        }

        if (attribute.ConstructorArguments is not [{ Kind: TypedConstantKind.Type, Value: INamedTypeSymbol runtimeClassType }] ||
            runtimeClassType.TypeKind is TypeKind.Error ||
            GetImplementedRuntimeClassMismatchReason(runtimeClassType, runtimeClassName, candidateFactoryBase, implementableClasses) is not null)
        {
            return null;
        }

        factoryBase = candidateFactoryBase;

        return runtimeClassName;
    }

    /// <summary>
    /// Gets why a type deriving from a generated activation factory base cannot be used as an activation factory.
    /// </summary>
    /// <param name="type">The annotated type.</param>
    /// <param name="factoryBase">The generated activation factory base <paramref name="type"/> derives from.</param>
    /// <param name="compilation">The current <see cref="Compilation"/>.</param>
    /// <returns>The reason, or <see langword="null"/> if <paramref name="type"/> is valid.</returns>
    public static string? GetInvalidImplementedFactoryReason(INamedTypeSymbol type, INamedTypeSymbol factoryBase, Compilation compilation)
    {
        // CsWinRT instantiates the factory from the generated activation entry point in the same assembly
        if (type.IsAbstract)
        {
            return $"it derives from the generated activation factory base '{factoryBase}', so it implements the activation factory itself, and must not be abstract.";
        }

        if (type.GetNotInstantiableFromGeneratedCodeReason(compilation) is string reason)
        {
            return $"{reason}, so CsWinRT cannot instantiate it.";
        }

        return null;
    }

    /// <summary>
    /// Gets why the type argument of the attribute on a factory deriving from a generated activation factory base does not match it.
    /// </summary>
    /// <param name="runtimeClassType">The type argument of the attribute.</param>
    /// <param name="runtimeClassName">The Windows Runtime class the factory base activates.</param>
    /// <param name="factoryBase">The generated activation factory base.</param>
    /// <param name="implementableClasses">The lookup for the generated bases.</param>
    /// <returns>The reason, or <see langword="null"/> if <paramref name="runtimeClassType"/> matches.</returns>
    public static string? GetImplementedRuntimeClassMismatchReason(
        INamedTypeSymbol runtimeClassType,
        string runtimeClassName,
        INamedTypeSymbol factoryBase,
        ImplementableClassLookup implementableClasses)
    {
        // An implementation of the class (the common case)
        if (implementableClasses.GetInstanceBase(runtimeClassType) is INamedTypeSymbol instanceBase &&
            implementableClasses.GetInstanceRuntimeClassName(instanceBase) == runtimeClassName)
        {
            return null;
        }

        // The projected class itself (needed for statics-only classes, which have no implementation)
        if (runtimeClassType is { ContainingType: null, ContainingNamespace.IsGlobalNamespace: false } &&
            $"{runtimeClassType.ContainingNamespace.ToDisplayString()}.{runtimeClassType.MetadataName}" == runtimeClassName)
        {
            return null;
        }

        return
            $"is neither the Windows Runtime class '{runtimeClassName}' activated by the generated activation factory base '{factoryBase}', " +
            $"nor an implementation of it (a class deriving from 'ABI.{runtimeClassName}').";
    }

    /// <summary>
    /// Gets why CsWinRT cannot generate the activation factory for a type annotated with <c>[GeneratedWindowsRuntimeActivationFactory]</c>.
    /// </summary>
    /// <param name="type">The annotated type.</param>
    /// <param name="compilation">The current <see cref="Compilation"/>.</param>
    /// <param name="implementableClasses">The lookup for the generated bases.</param>
    /// <param name="runtimeClassName">The Windows Runtime class <paramref name="type"/> implements, if it is valid.</param>
    /// <param name="instanceBase">The generated implementable base <paramref name="type"/> derives from, if it is valid.</param>
    /// <param name="factoryBase">The generated activation factory base for the implemented class, if it is valid.</param>
    /// <returns>The reason, or <see langword="null"/> if <paramref name="type"/> is valid.</returns>
    public static string? GetInvalidGeneratedFactoryReason(
        INamedTypeSymbol type,
        Compilation compilation,
        ImplementableClassLookup implementableClasses,
        out string? runtimeClassName,
        out INamedTypeSymbol? instanceBase,
        out INamedTypeSymbol? factoryBase)
    {
        runtimeClassName = null;
        instanceBase = null;
        factoryBase = null;

        if (type.TypeKind is not TypeKind.Class ||
            implementableClasses.GetInstanceBase(type.BaseType) is not INamedTypeSymbol candidateInstanceBase ||
            implementableClasses.GetInstanceRuntimeClassName(candidateInstanceBase) is not string implementedRuntimeClassName)
        {
            return "it does not derive from a generated implementable base class (such as 'ABI.<Namespace>.<Class>'), so it does not implement a Windows Runtime class defined in existing metadata.";
        }

        // The generated activation factory constructs the annotated type from the generated activation entry point
        if (type.IsAbstract || type.IsStatic)
        {
            return "it is abstract, so CsWinRT cannot instantiate it.";
        }

        if (type.GetNotInstantiableFromGeneratedCodeReason(compilation) is string reason)
        {
            return $"{reason}, so CsWinRT cannot instantiate it.";
        }

        if (implementableClasses.GetFactoryBaseForInstanceBase(candidateInstanceBase) is not INamedTypeSymbol candidateFactoryBase)
        {
            return $"the Windows Runtime class '{implementedRuntimeClassName}' it implements is not activatable.";
        }

        if (!implementableClasses.HasDefaultActivationOnly(candidateFactoryBase))
        {
            return
                $"the Windows Runtime class '{implementedRuntimeClassName}' it implements has statics or constructors with parameters, which only a declared activation factory can implement. " +
                $"Declare one deriving from '{candidateFactoryBase}' with '[WindowsRuntimeActivationFactory]' instead.";
        }

        runtimeClassName = implementedRuntimeClassName;
        instanceBase = candidateInstanceBase;
        factoryBase = candidateFactoryBase;

        return null;
    }
}
