// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using Microsoft.CodeAnalysis;

#pragma warning disable IDE0046

namespace WindowsRuntime.SourceGenerator;

/// <summary>
/// Validation of activation factories, both for Windows Runtime classes defined in existing metadata and for authored ones,
/// shared by the source generator and the analyzers, so that the generator registers exactly the factories the analyzers
/// consider valid.
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

        if (GetNotInstantiableFromGeneratedCodeReason(type, compilation) is string reason)
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

        if (GetNotInstantiableFromGeneratedCodeReason(type, compilation) is string reason)
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

    /// <summary>
    /// Gets why a type not deriving from a generated activation factory base cannot extend a generated activation factory.
    /// </summary>
    /// <param name="type">The annotated type.</param>
    /// <param name="isComponent">Whether the current project is a Windows Runtime component.</param>
    /// <returns>The reason, or <see langword="null"/> if <paramref name="type"/> is valid.</returns>
    public static string? GetInvalidFactoryExtensionReason(INamedTypeSymbol type, bool isComponent)
    {
        if (type.IsStatic || !type.IsAbstract)
        {
            return
                "it must either derive from a generated activation factory base, to implement the activation factory of a Windows Runtime class " +
                "defined in existing metadata, or be an abstract class, to add interfaces to the activation factory generated for an authored Windows Runtime class.";
        }

        // The generated activation factory, which derives from this type, only exists for Windows Runtime components
        if (!isComponent)
        {
            return
                "an abstract activation factory adds interfaces to the activation factory generated for an authored Windows Runtime class, " +
                "which only exists in a Windows Runtime component (set 'CsWinRTComponent' to 'true').";
        }

        // The generated activation factory derives from this type from another assembly ('WinRT.Component.dll')
        if (!type.IsPubliclyAccessible)
        {
            return "it or one of its containing types is not public, so the generated activation factory cannot derive from it.";
        }

        if (type.IsGenericOrNestedInGenericType)
        {
            return "it is generic, or nested in a generic type, so the generated activation factory cannot derive from it.";
        }

        if (!type.InstanceConstructors.Any(static constructor =>
            constructor.Parameters.IsEmpty &&
            constructor.DeclaredAccessibility is Accessibility.Public or Accessibility.Protected or Accessibility.ProtectedOrInternal))
        {
            return "it has no public or protected parameterless constructor, so the generated activation factory cannot derive from it.";
        }

        if (type.GetMembers().FirstOrDefault(static member => member.IsAbstract) is ISymbol abstractMember)
        {
            return $"it declares the abstract member '{abstractMember.Name}', which the generated activation factory deriving from it cannot implement.";
        }

        return null;
    }

    /// <summary>
    /// Gets why the type argument of the attribute on an abstract factory is not an activatable authored Windows Runtime class.
    /// </summary>
    /// <param name="runtimeClassType">The type argument of the attribute.</param>
    /// <param name="compilation">The current <see cref="Compilation"/>.</param>
    /// <param name="activationFactoryAttributeType">The <c>[WindowsRuntimeActivationFactory]</c> symbol.</param>
    /// <param name="implementableClasses">The lookup for the generated bases.</param>
    /// <returns>The reason, or <see langword="null"/> if <paramref name="runtimeClassType"/> is valid.</returns>
    public static string? GetAuthoredRuntimeClassMismatchReason(
        INamedTypeSymbol runtimeClassType,
        Compilation compilation,
        INamedTypeSymbol activationFactoryAttributeType,
        ImplementableClassLookup implementableClasses)
    {
        if (!SymbolEqualityComparer.Default.Equals(runtimeClassType.ContainingAssembly, compilation.Assembly))
        {
            return "is not declared in this project, so it is not a Windows Runtime class authored by this component.";
        }

        INamedTypeSymbol? excludeAttributeType = compilation.GetTypeByMetadataName("WindowsRuntime.ExcludeFromWindowsMetadataAttribute");

        // These mirror the types the WinMD generator exports as authored Windows Runtime classes
        if (runtimeClassType.TypeKind is not TypeKind.Class ||
            runtimeClassType.DeclaredAccessibility is not Accessibility.Public ||
            runtimeClassType.ContainingType is not null ||
            runtimeClassType.IsGenericType ||
            runtimeClassType.IsInAbiNamespace ||
            runtimeClassType.IsExcludedFromWindowsMetadata(excludeAttributeType) ||
            runtimeClassType.HasAttributeWithType(activationFactoryAttributeType) ||
            implementableClasses.GetInstanceBase(runtimeClassType) is not null)
        {
            return "is not a Windows Runtime class authored by this component (a public, non-generic, top-level class).";
        }

        if (!HasActivationFactory(runtimeClassType, excludeAttributeType))
        {
            return "has no public constructors and no public static members, so no activation factory is generated for it.";
        }

        return null;
    }

    /// <summary>
    /// Checks whether an authored Windows Runtime class gets an activation factory, i.e. whether it has public constructors or public static members.
    /// </summary>
    /// <param name="type">The authored Windows Runtime class.</param>
    /// <param name="excludeAttributeType">The metadata exclusion marker, if available.</param>
    /// <returns>Whether <paramref name="type"/> gets an activation factory.</returns>
    private static bool HasActivationFactory(
        INamedTypeSymbol type,
        INamedTypeSymbol? excludeAttributeType)
    {
        if (!type.IsAbstract && !type.IsStatic)
        {
            foreach (IMethodSymbol constructor in type.InstanceConstructors)
            {
                if (constructor.DeclaredAccessibility is Accessibility.Public &&
                    !constructor.IsExcludedFromWindowsMetadata(excludeAttributeType))
                {
                    return true;
                }
            }
        }

        foreach (ISymbol member in type.GetMembers())
        {
            if (!member.IsStatic ||
                member.DeclaredAccessibility is not Accessibility.Public ||
                member.IsExcludedFromWindowsMetadata(excludeAttributeType))
            {
                continue;
            }

            // Static constructors, accessors and operators are not exposed as Windows Runtime statics
            if (member is IMethodSymbol { MethodKind: MethodKind.Ordinary } or IPropertySymbol or IEventSymbol)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets why code generated into another file of the same assembly cannot instantiate a type with <c>new T()</c>.
    /// </summary>
    /// <param name="type">The type to instantiate.</param>
    /// <param name="compilation">The current <see cref="Compilation"/>.</param>
    /// <returns>The reason, or <see langword="null"/> if generated code can instantiate <paramref name="type"/>.</returns>
    /// <remarks>
    /// This does not check whether the type is abstract or static, which callers report with more specific messages.
    /// </remarks>
    public static string? GetNotInstantiableFromGeneratedCodeReason(INamedTypeSymbol type, Compilation compilation)
    {
        if (type.IsGenericOrNestedInGenericType)
        {
            return "it is generic, or nested in a generic type";
        }

        for (INamedTypeSymbol? current = type; current is not null; current = current.ContainingType)
        {
            if (current.IsFileLocal)
            {
                return "it is a file-local type, or nested in one";
            }
        }

        if (!compilation.IsSymbolAccessibleWithin(type, compilation.Assembly))
        {
            return "it is not accessible from the rest of its assembly";
        }

        if (type.InstanceConstructors.FirstOrDefault(constructor =>
            constructor.Parameters.IsEmpty &&
            compilation.IsSymbolAccessibleWithin(constructor, compilation.Assembly)) is not IMethodSymbol constructor)
        {
            return "it has no parameterless constructor accessible from the rest of its assembly";
        }

        // 'new T()' does not compile if 'T' has required members that the constructor does not declare it sets
        return type.HasRequiredMembers && !constructor.GetAttributes().Any(static attribute =>
            attribute.AttributeClass?.ToDisplayString() == "System.Diagnostics.CodeAnalysis.SetsRequiredMembersAttribute")
            ? "it has required members, which its parameterless constructor does not set (as declared with '[SetsRequiredMembers]')"
            : null;
    }
}