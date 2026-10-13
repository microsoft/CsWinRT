// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

#pragma warning disable IDE0046

namespace WindowsRuntime.SourceGenerator.Diagnostics;

/// <summary>
/// A diagnostic analyzer that validates types annotated with <c>[WindowsRuntimeActivationFactory]</c>. The attribute supports two shapes:
/// <list type="bullet">
///   <item>A non-abstract class deriving from a generated activation factory base, implementing the activation factory of a Windows Runtime class defined in existing metadata.</item>
///   <item>A public abstract class in a Windows Runtime component, adding interfaces to the activation factory generated for an authored Windows Runtime class.</item>
/// </list>
/// It also validates implementations annotated with <c>[GeneratedWindowsRuntimeActivationFactory]</c>, which request a generated
/// activation factory instead, so that a Windows Runtime class getting more than one activation factory either way is reported.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class WindowsRuntimeActivationFactoryAttributeAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
    [
        DiagnosticDescriptors.InvalidActivationFactoryType,
        DiagnosticDescriptors.ActivationFactoryRuntimeClassMismatch,
        DiagnosticDescriptors.DuplicateActivationFactory,
        DiagnosticDescriptors.InvalidGeneratedActivationFactoryType
    ];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static context =>
        {
            // Get the '[WindowsRuntimeActivationFactory]' symbol
            if (context.Compilation.GetTypeByMetadataName("WindowsRuntime.InteropServices.WindowsRuntimeActivationFactoryAttribute") is not { } activationFactoryAttributeType)
            {
                return;
            }

            // Get the '[GeneratedWindowsRuntimeActivationFactory]' symbol
            INamedTypeSymbol? generatedActivationFactoryAttributeType = context.Compilation.GetTypeByMetadataName("WindowsRuntime.InteropServices.GeneratedWindowsRuntimeActivationFactoryAttribute");

            bool isComponent = context.Options.AnalyzerConfigOptionsProvider.GlobalOptions.GetCsWinRTComponent();
            ImplementableClassLookup implementableClasses = new();

            // The valid activation factories (declared or requested), keyed by the Windows Runtime class each one is for
            ConcurrentBag<(string RuntimeClassName, INamedTypeSymbol Type, Location Location)> activationFactories = [];

            context.RegisterSymbolAction(context =>
            {
                if (context.Symbol is not INamedTypeSymbol { TypeKind: TypeKind.Class } type)
                {
                    return;
                }

                if (generatedActivationFactoryAttributeType is not null &&
                    type.TryGetAttributeWithType(generatedActivationFactoryAttributeType, out AttributeData? generatedAttribute))
                {
                    Location generatedLocation = generatedAttribute.GetLocation(context.CancellationToken) ?? type.Locations.FirstOrDefault() ?? Location.None;

                    if (GetInvalidGeneratedFactoryReason(type, context.Compilation, implementableClasses, out string? implementedRuntimeClassName) is string generatedInvalidReason)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.InvalidGeneratedActivationFactoryType,
                            generatedLocation,
                            type,
                            generatedInvalidReason));
                    }
                    else
                    {
                        activationFactories.Add((implementedRuntimeClassName!, type, generatedLocation));
                    }
                }

                if (!type.TryGetAttributeWithType(activationFactoryAttributeType, out AttributeData? attribute))
                {
                    return;
                }

                Location location = attribute.GetLocation(context.CancellationToken) ?? type.Locations.FirstOrDefault() ?? Location.None;

                // A factory deriving from a generated activation factory base implements a class from existing metadata,
                // whereas an abstract one extends the activation factory generated for an authored class.
                INamedTypeSymbol? factoryBase = implementableClasses.GetFactoryBase(type.BaseType);

                string? invalidReason = factoryBase is not null
                    ? GetInvalidImplementedFactoryReason(type, factoryBase, context.Compilation)
                    : GetInvalidFactoryExtensionReason(type, isComponent);

                if (invalidReason is not null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.InvalidActivationFactoryType,
                        location,
                        type,
                        invalidReason));

                    return;
                }

                // Skip malformed applications, eg. where the argument does not bind to a type (the compiler reports those)
                if (attribute.ConstructorArguments is not [{ Kind: TypedConstantKind.Type, Value: INamedTypeSymbol runtimeClassType }] ||
                    runtimeClassType.TypeKind is TypeKind.Error)
                {
                    return;
                }

                string runtimeClassName;
                string? mismatchReason;

                if (factoryBase is not null)
                {
                    runtimeClassName = implementableClasses.GetFactoryRuntimeClassName(factoryBase)!;
                    mismatchReason = GetImplementedRuntimeClassMismatchReason(runtimeClassType, runtimeClassName, factoryBase, implementableClasses);
                }
                else
                {
                    runtimeClassName = runtimeClassType.ToDisplayString();
                    mismatchReason = GetAuthoredRuntimeClassMismatchReason(runtimeClassType, context.Compilation, activationFactoryAttributeType, implementableClasses);
                }

                if (mismatchReason is not null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.ActivationFactoryRuntimeClassMismatch,
                        attribute.GetArgumentLocation(0, context.CancellationToken) ?? location,
                        type,
                        runtimeClassType,
                        mismatchReason));

                    return;
                }

                activationFactories.Add((runtimeClassName, type, location));
            }, SymbolKind.NamedType);

            context.RegisterCompilationEndAction(context =>
            {
                foreach (IGrouping<string, (string RuntimeClassName, INamedTypeSymbol Type, Location Location)> group in activationFactories.GroupBy(
                    static factory => factory.RuntimeClassName,
                    StringComparer.Ordinal))
                {
                    if (group.Take(2).Count() < 2)
                    {
                        continue;
                    }

                    string factoryNames = string.Join(", ", group
                        .Select(static factory => $"'{factory.Type.ToDisplayString()}'")
                        .OrderBy(static name => name, StringComparer.Ordinal));

                    foreach ((_, INamedTypeSymbol type, Location location) in group)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.DuplicateActivationFactory,
                            location,
                            type,
                            group.Key,
                            factoryNames));
                    }
                }
            });
        });
    }

    /// <summary>
    /// Gets why a type deriving from a generated activation factory base cannot be used as an activation factory.
    /// </summary>
    /// <param name="type">The annotated type.</param>
    /// <param name="factoryBase">The generated activation factory base <paramref name="type"/> derives from.</param>
    /// <param name="compilation">The current <see cref="Compilation"/>.</param>
    /// <returns>The reason, or <see langword="null"/> if <paramref name="type"/> is valid.</returns>
    private static string? GetInvalidImplementedFactoryReason(INamedTypeSymbol type, INamedTypeSymbol factoryBase, Compilation compilation)
    {
        // CsWinRT instantiates the factory from the generated activation entry point in the same assembly
        if (type.IsAbstract)
        {
            return $"it derives from the generated activation factory base '{factoryBase}', so it implements the activation factory itself, and must not be abstract.";
        }

        if (type.IsGenericOrNestedInGenericType)
        {
            return "it is generic, or nested in a generic type, so CsWinRT cannot instantiate it.";
        }

        if (!compilation.IsSymbolAccessibleWithin(type, compilation.Assembly))
        {
            return "it is not accessible from the rest of its assembly, so CsWinRT cannot instantiate it.";
        }

        if (!type.InstanceConstructors.Any(constructor => constructor.Parameters.IsEmpty && compilation.IsSymbolAccessibleWithin(constructor, compilation.Assembly)))
        {
            return "it has no parameterless constructor accessible from the rest of its assembly, so CsWinRT cannot instantiate it.";
        }

        return null;
    }

    /// <summary>
    /// Gets why CsWinRT cannot generate the activation factory for a type annotated with <c>[GeneratedWindowsRuntimeActivationFactory]</c>.
    /// </summary>
    /// <param name="type">The annotated type.</param>
    /// <param name="compilation">The current <see cref="Compilation"/>.</param>
    /// <param name="implementableClasses">The lookup for the generated bases.</param>
    /// <param name="runtimeClassName">The Windows Runtime class <paramref name="type"/> implements, if it is valid.</param>
    /// <returns>The reason, or <see langword="null"/> if <paramref name="type"/> is valid.</returns>
    private static string? GetInvalidGeneratedFactoryReason(
        INamedTypeSymbol type,
        Compilation compilation,
        ImplementableClassLookup implementableClasses,
        out string? runtimeClassName)
    {
        runtimeClassName = null;

        if (implementableClasses.GetInstanceBase(type.BaseType) is not INamedTypeSymbol instanceBase ||
            implementableClasses.GetInstanceRuntimeClassName(instanceBase) is not string implementedRuntimeClassName)
        {
            return "it does not derive from a generated implementable base class (such as 'ABI.<Namespace>.<Class>'), so it does not implement a Windows Runtime class defined in existing metadata.";
        }

        // The generated activation factory constructs the annotated type from the generated activation entry point
        if (type.IsAbstract || type.IsStatic)
        {
            return "it is abstract, so CsWinRT cannot instantiate it.";
        }

        if (type.IsGenericOrNestedInGenericType)
        {
            return "it is generic, or nested in a generic type, so CsWinRT cannot instantiate it.";
        }

        if (!compilation.IsSymbolAccessibleWithin(type, compilation.Assembly))
        {
            return "it is not accessible from the rest of its assembly, so CsWinRT cannot instantiate it.";
        }

        if (!type.InstanceConstructors.Any(constructor => constructor.Parameters.IsEmpty && compilation.IsSymbolAccessibleWithin(constructor, compilation.Assembly)))
        {
            return "it has no parameterless constructor accessible from the rest of its assembly, so CsWinRT cannot instantiate it.";
        }

        if (implementableClasses.GetFactoryBaseForInstanceBase(instanceBase) is not INamedTypeSymbol factoryBase)
        {
            return $"the Windows Runtime class '{implementedRuntimeClassName}' it implements is not activatable.";
        }

        if (!implementableClasses.HasDefaultActivationOnly(factoryBase))
        {
            return
                $"the Windows Runtime class '{implementedRuntimeClassName}' it implements has statics or constructors with parameters, which only a declared activation factory can implement. " +
                $"Declare one deriving from '{factoryBase}' with '[WindowsRuntimeActivationFactory]' instead.";
        }

        runtimeClassName = implementedRuntimeClassName;

        return null;
    }

    /// <summary>
    /// Gets why a type not deriving from a generated activation factory base cannot extend a generated activation factory.
    /// </summary>
    /// <param name="type">The annotated type.</param>
    /// <param name="isComponent">Whether the current project is a Windows Runtime component.</param>
    /// <returns>The reason, or <see langword="null"/> if <paramref name="type"/> is valid.</returns>
    private static string? GetInvalidFactoryExtensionReason(INamedTypeSymbol type, bool isComponent)
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
        for (INamedTypeSymbol? current = type; current is not null; current = current.ContainingType)
        {
            if (current.DeclaredAccessibility is not Accessibility.Public)
            {
                return "it or one of its containing types is not public, so the generated activation factory cannot derive from it.";
            }
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
    /// Gets why the type argument of the attribute on a factory deriving from a generated activation factory base does not match it.
    /// </summary>
    /// <param name="runtimeClassType">The type argument of the attribute.</param>
    /// <param name="runtimeClassName">The Windows Runtime class the factory base activates.</param>
    /// <param name="factoryBase">The generated activation factory base.</param>
    /// <param name="implementableClasses">The lookup for the generated bases.</param>
    /// <returns>The reason, or <see langword="null"/> if <paramref name="runtimeClassType"/> matches.</returns>
    private static string? GetImplementedRuntimeClassMismatchReason(
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
    /// Gets why the type argument of the attribute on an abstract factory is not an activatable authored Windows Runtime class.
    /// </summary>
    /// <param name="runtimeClassType">The type argument of the attribute.</param>
    /// <param name="compilation">The current <see cref="Compilation"/>.</param>
    /// <param name="activationFactoryAttributeType">The <c>[WindowsRuntimeActivationFactory]</c> symbol.</param>
    /// <param name="implementableClasses">The lookup for the generated bases.</param>
    /// <returns>The reason, or <see langword="null"/> if <paramref name="runtimeClassType"/> is valid.</returns>
    private static string? GetAuthoredRuntimeClassMismatchReason(
        INamedTypeSymbol runtimeClassType,
        Compilation compilation,
        INamedTypeSymbol activationFactoryAttributeType,
        ImplementableClassLookup implementableClasses)
    {
        if (!SymbolEqualityComparer.Default.Equals(runtimeClassType.ContainingAssembly, compilation.Assembly))
        {
            return "is not declared in this project, so it is not a Windows Runtime class authored by this component.";
        }

        // These mirror the types the WinMD generator exports as authored Windows Runtime classes
        if (runtimeClassType.TypeKind is not TypeKind.Class ||
            runtimeClassType.DeclaredAccessibility is not Accessibility.Public ||
            runtimeClassType.ContainingType is not null ||
            runtimeClassType.IsGenericType ||
            IsInAbiNamespace(runtimeClassType) ||
            runtimeClassType.HasAttributeWithType(activationFactoryAttributeType) ||
            implementableClasses.GetInstanceBase(runtimeClassType) is not null)
        {
            return "is not a Windows Runtime class authored by this component (a public, non-generic, top-level class).";
        }

        if (!HasActivationFactory(runtimeClassType))
        {
            return "has no public constructors and no public static members, so no activation factory is generated for it.";
        }

        return null;
    }

    /// <summary>
    /// Checks whether a type is declared in the <c>ABI</c> namespace or in one of its child namespaces.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>Whether <paramref name="type"/> is declared in the <c>ABI</c> namespace.</returns>
    private static bool IsInAbiNamespace(INamedTypeSymbol type)
    {
        INamespaceSymbol? rootNamespace = null;

        for (INamespaceSymbol? current = type.ContainingNamespace; current is { IsGlobalNamespace: false }; current = current.ContainingNamespace)
        {
            rootNamespace = current;
        }

        return rootNamespace?.Name == "ABI";
    }

    /// <summary>
    /// Checks whether an authored Windows Runtime class gets an activation factory, i.e. whether it has public constructors or public static members.
    /// </summary>
    /// <param name="type">The authored Windows Runtime class.</param>
    /// <returns>Whether <paramref name="type"/> gets an activation factory.</returns>
    private static bool HasActivationFactory(INamedTypeSymbol type)
    {
        if (!type.IsAbstract && !type.IsStatic && type.InstanceConstructors.Any(static constructor => constructor.DeclaredAccessibility is Accessibility.Public))
        {
            return true;
        }

        foreach (ISymbol member in type.GetMembers())
        {
            if (!member.IsStatic || member.DeclaredAccessibility is not Accessibility.Public)
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
}
