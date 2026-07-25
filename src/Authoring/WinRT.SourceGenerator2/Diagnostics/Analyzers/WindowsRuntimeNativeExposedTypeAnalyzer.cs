// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace WindowsRuntime.SourceGenerator.Diagnostics;

/// <summary>
/// A diagnostic analyzer that validates uses of <c>[WindowsRuntimeNativeExposedType]</c>. The attribute is only
/// meaningful when applied with a concrete, non generic projected Windows Runtime class type, as CCW marshalling
/// code is generated automatically for every other kind of type. It also reports redundant applications of the
/// attribute that target a type already used by another application in the same assembly.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class WindowsRuntimeNativeExposedTypeAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
    [
        DiagnosticDescriptors.NativeExposedTypeNotInstantiable,
        DiagnosticDescriptors.NativeExposedTypeNotProjectedClass,
        DiagnosticDescriptors.NativeExposedTypeDuplicate
    ];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationAction(static context =>
        {
            // Get the '[WindowsRuntimeNativeExposedType]' symbol
            if (context.Compilation.GetTypeByMetadataName("WindowsRuntime.InteropServices.WindowsRuntimeNativeExposedTypeAttribute") is not { } nativeExposedTypeAttributeType)
            {
                return;
            }

            // Get the '[WindowsRuntimeReferenceAssembly]' symbol, which is used to detect projected Windows Runtime types
            if (context.Compilation.GetTypeByMetadataName("WindowsRuntime.InteropServices.WindowsRuntimeReferenceAssemblyAttribute") is not { } referenceAssemblyAttributeType)
            {
                return;
            }

            // Also get the '[WindowsRuntimeType]' symbol, if available. This is only used as a fallback for the rare
            // case of compiling directly against an implementation projection (see 'IsProjectedWindowsRuntimeType').
            INamedTypeSymbol? windowsRuntimeTypeAttributeType = context.Compilation.GetTypeByMetadataName("WindowsRuntime.WindowsRuntimeTypeAttribute");

            List<ITypeSymbol> validTargetTypes = [];

            // Collect the valid target types across all applications of the attribute in the assembly (including
            // the ones in generated code), so that a type used both in user code and in generated code is still
            // detected as a duplicate. This list is only used to report duplicate applications of the attribute.
            foreach (AttributeData attribute in context.Compilation.Assembly.GetAttributes(nativeExposedTypeAttributeType))
            {
                if (attribute is { ConstructorArguments: [{ Value: ITypeSymbol targetType }] } &&
                    Classify(targetType, referenceAssemblyAttributeType, windowsRuntimeTypeAttributeType) is NativeExposedTypeKind.Valid)
                {
                    validTargetTypes.Add(targetType);
                }
            }

            // Classify and report each application of the attribute. Applications in generated code are
            // suppressed by the analysis framework, because this analyzer opts out of generated code analysis,
            // so only applications authored in user code are ever reported.
            foreach (AttributeData attribute in context.Compilation.Assembly.GetAttributes(nativeExposedTypeAttributeType))
            {
                // Skip malformed applications, eg. where the argument does not bind to a type
                if (attribute is not { ConstructorArguments: [{ Value: ITypeSymbol targetType }] })
                {
                    continue;
                }

                Location? location = attribute.GetArgumentLocation(0, context.CancellationToken) ?? attribute.GetLocation(context.CancellationToken);

                NativeExposedTypeKind kind = Classify(targetType, referenceAssemblyAttributeType, windowsRuntimeTypeAttributeType);

                // The type cannot be instantiated, so no CCW would ever be created for it
                if (kind is NativeExposedTypeKind.NotInstantiable)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.NativeExposedTypeNotInstantiable,
                        location,
                        targetType));
                }
                else if (kind is NativeExposedTypeKind.NotProjectedClass)
                {
                    // The type is not a projected class, so CCW marshalling code is already generated for it
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.NativeExposedTypeNotProjectedClass,
                        location,
                        targetType));
                }
                else if (CountOccurrences(validTargetTypes, targetType) > 1)
                {
                    // The type is valid, but it is also targeted by another application of the attribute
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.NativeExposedTypeDuplicate,
                        location,
                        targetType));
                }
            }
        });
    }

    /// <summary>
    /// Classifies a target type used with <c>[WindowsRuntimeNativeExposedType]</c>.
    /// </summary>
    /// <param name="type">The target type to classify.</param>
    /// <param name="referenceAssemblyAttributeType">The <c>[WindowsRuntimeReferenceAssembly]</c> symbol.</param>
    /// <param name="windowsRuntimeTypeAttributeType">The <c>[WindowsRuntimeType]</c> symbol, if it is available.</param>
    /// <returns>The <see cref="NativeExposedTypeKind"/> classification for <paramref name="type"/>.</returns>
    private static NativeExposedTypeKind Classify(
        ITypeSymbol type,
        INamedTypeSymbol referenceAssemblyAttributeType,
        INamedTypeSymbol? windowsRuntimeTypeAttributeType)
    {
        // A type that cannot be instantiated can never have a CCW created for it, so the attribute is meaningless
        if (!IsInstantiable(type))
        {
            return NativeExposedTypeKind.NotInstantiable;
        }

        // A valid target is a non generic projected Windows Runtime class. CCW marshalling code is generated
        // automatically for every other kind of type, so the attribute is only ever needed for projected classes.
        if (type is not INamedTypeSymbol { TypeKind: TypeKind.Class, IsGenericType: false } namedType ||
            !IsProjectedWindowsRuntimeType(namedType, referenceAssemblyAttributeType, windowsRuntimeTypeAttributeType))
        {
            return NativeExposedTypeKind.NotProjectedClass;
        }

        // The type is a valid, non-generic and projected runtime class
        return NativeExposedTypeKind.Valid;
    }

    /// <summary>
    /// Checks whether a given type is a projected Windows Runtime type.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <param name="referenceAssemblyAttributeType">The <c>[WindowsRuntimeReferenceAssembly]</c> symbol.</param>
    /// <param name="windowsRuntimeTypeAttributeType">The <c>[WindowsRuntimeType]</c> symbol, if it is available.</param>
    /// <returns>Whether <paramref name="type"/> is a projected Windows Runtime type.</returns>
    private static bool IsProjectedWindowsRuntimeType(
        INamedTypeSymbol type,
        INamedTypeSymbol referenceAssemblyAttributeType,
        INamedTypeSymbol? windowsRuntimeTypeAttributeType)
    {
        // Projections are consumed as reference projection assemblies, which are marked with
        // '[assembly: WindowsRuntimeReferenceAssembly]'. All types they contain are projected
        // Windows Runtime types, so this is the check being used to detect them. Note that the
        // per-type markers are not visible here, as they are implementation details that are
        // stripped from reference projections (the same is true for the centralized metadata
        // lookup type that carries all '[WindowsRuntimeMetadata]' entries).
        if (type.ContainingAssembly is { } containingAssembly &&
            containingAssembly.HasAttributeWithType(referenceAssemblyAttributeType))
        {
            return true;
        }

        // Fallback for the rare case of compiling directly against an implementation projection (eg. a local
        // project reference to a projection project that is not producing a reference projection). Those types
        // do not have the assembly-level marker, but they do carry the per-type '[WindowsRuntimeType]' marker.
        return windowsRuntimeTypeAttributeType is not null && type.HasAttributeWithType(windowsRuntimeTypeAttributeType);
    }

    /// <summary>
    /// Counts how many times a given type appears in a sequence of types.
    /// </summary>
    /// <param name="types">The sequence of types to search.</param>
    /// <param name="type">The type to count occurrences of.</param>
    /// <returns>The number of times <paramref name="type"/> appears in <paramref name="types"/>.</returns>
    private static int CountOccurrences(IEnumerable<ITypeSymbol> types, ITypeSymbol type)
    {
        int count = 0;

        foreach (ITypeSymbol candidate in types)
        {
            if (SymbolEqualityComparer.Default.Equals(candidate, type))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Checks whether a given type can be instantiated.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>Whether <paramref name="type"/> can be instantiated.</returns>
    private static bool IsInstantiable(ITypeSymbol type)
    {
        // Array types can always be instantiated
        if (type is IArrayTypeSymbol)
        {
            return true;
        }

        // Any other non named type (eg. a pointer type or a type parameter) cannot be instantiated
        if (type is not INamedTypeSymbol namedType)
        {
            return false;
        }

        // An unbound generic type definition (eg. 'typeof(List<>)') cannot be instantiated
        if (namedType.IsUnboundGenericType)
        {
            return false;
        }

        // Interfaces, abstract classes, and static classes cannot be instantiated
        if (namedType.TypeKind is TypeKind.Interface || namedType.IsAbstract || namedType.IsStatic)
        {
            return false;
        }

        // Classes, structs, enums, and delegates can be instantiated
        return namedType.TypeKind is TypeKind.Class or TypeKind.Struct or TypeKind.Enum or TypeKind.Delegate;
    }

    /// <summary>
    /// The classification of a target type used with <c>[WindowsRuntimeNativeExposedType]</c>.
    /// </summary>
    private enum NativeExposedTypeKind
    {
        /// <summary>
        /// The target type cannot be instantiated (eg. an interface, an abstract class, or a generic type definition).
        /// </summary>
        NotInstantiable,

        /// <summary>
        /// The target type can be instantiated, but it is not a projected Windows Runtime class.
        /// </summary>
        NotProjectedClass,

        /// <summary>
        /// The target type is a valid, non generic projected Windows Runtime class.
        /// </summary>
        Valid
    }
}
