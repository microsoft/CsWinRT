// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using WindowsRuntime.Generator.Helpers;

namespace WindowsRuntime.SourceGenerator;

/// <summary>
/// Classifies declarations and type mappings on the surface exported by the WinMD generator.
/// </summary>
/// <param name="compilation">The compilation being analyzed.</param>
/// <param name="useWindowsUIXamlProjections">Whether UWP XAML is selected instead of WinUI.</param>
internal sealed class AuthoringTypeValidation(
    Compilation compilation,
    bool useWindowsUIXamlProjections)
{
    /// <summary>
    /// The lookup for implementations of runtime classes declared in existing metadata.
    /// </summary>
    private readonly ImplementableClassLookup _implementableClasses = new();

    /// <summary>
    /// The activation factory marker used to exclude factory declarations from the authored surface.
    /// </summary>
    private readonly INamedTypeSymbol? _activationFactory =
        compilation.GetTypeByMetadataName("WindowsRuntime.InteropServices.WindowsRuntimeActivationFactoryAttribute");

    /// <summary>
    /// The reference projection assembly marker.
    /// </summary>
    private readonly INamedTypeSymbol? _referenceAssembly =
        compilation.GetTypeByMetadataName("WindowsRuntime.InteropServices.WindowsRuntimeReferenceAssemblyAttribute");

    /// <summary>
    /// The implementation-only Windows Runtime type marker.
    /// </summary>
    private readonly INamedTypeSymbol? _runtimeType = compilation.GetTypeByMetadataName("WindowsRuntime.WindowsRuntimeTypeAttribute");

    /// <summary>
    /// The shared mappings for the selected XAML projection mode.
    /// </summary>
    private readonly FrozenDictionary<string, WindowsRuntimeTypeMappings.Mapping> _mappedTypes =
        WindowsRuntimeTypeMappings.GetMappings(useWindowsUIXamlProjections);

    /// <summary>
    /// Gets the metadata exclusion marker for the compilation, if available.
    /// </summary>
    public INamedTypeSymbol? ExcludeAttributeType { get; } =
        compilation.GetTypeByMetadataName("WindowsRuntime.InteropServices.ExcludeFromWindowsMetadataAttribute");

    /// <summary>
    /// Gets whether a type is exported as a new declaration in the component's WinMD.
    /// </summary>
    /// <param name="type">The type to classify.</param>
    /// <returns>Whether the type is exported as a new declaration.</returns>
    public bool IsExported(INamedTypeSymbol type)
    {
        return SymbolEqualityComparer.Default.Equals(type.ContainingAssembly, compilation.Assembly) &&
               type is { DeclaredAccessibility: Accessibility.Public, ContainingType: null, IsImplicitlyDeclared: false } &&
               !type.IsInAbiNamespace &&
               !type.IsExcludedFromWindowsMetadata(ExcludeAttributeType) &&
               (_activationFactory is null || !type.HasAttributeWithType(_activationFactory)) &&
               _implementableClasses.GetInstanceBase(type.BaseType) is null;
    }

    /// <summary>
    /// Gets whether a type belongs to a Windows Runtime projection or the runtime itself.
    /// </summary>
    /// <param name="type">The type to classify.</param>
    /// <returns>Whether the type is a projected Windows Runtime type.</returns>
    public bool IsProjected(INamedTypeSymbol type)
    {
        string ns = type.ContainingNamespace.ToDisplayString();

        return !SymbolEqualityComparer.Default.Equals(type.ContainingAssembly, compilation.Assembly) &&
               (type.IsProjectedWindowsRuntimeType(_referenceAssembly, _runtimeType) ||
                (type.ContainingAssembly.Identity.Name == "WinRT.Runtime" &&
                 (ns == "Windows.Foundation" || ns.StartsWith("Windows.Foundation.", StringComparison.Ordinal) ||
                  ns == "Windows.Storage.Streams" || ns == "Windows.UI.Xaml.Interop")));
    }

    /// <summary>
    /// Gets whether a type is a span used to author a Windows Runtime array parameter.
    /// </summary>
    /// <param name="type">The type to classify.</param>
    /// <returns>Whether the type is <see cref="Span{T}"/> or <see cref="ReadOnlySpan{T}"/>.</returns>
    public static bool IsSpan(ITypeSymbol type)
    {
        return type is INamedTypeSymbol { Arity: 1 } named &&
               named.ContainingNamespace.ToDisplayString() == "System" &&
               named.MetadataName is "Span`1" or "ReadOnlySpan`1";
    }

    /// <summary>
    /// Gets whether a type has a custom mapping valid in an authored API signature.
    /// </summary>
    /// <param name="type">The type to classify.</param>
    /// <returns>Whether the type has a supported signature mapping.</returns>
    public bool IsMapped(INamedTypeSymbol type)
    {
        string name = type.OriginalDefinition.GetFullyQualifiedMetadataName();

        // These mappings describe attribute metadata, not types allowed in public API signatures
        return name is not ("System.FlagsAttribute" or "System.AttributeUsageAttribute") &&
               _mappedTypes.ContainsKey(name);
    }

    /// <summary>
    /// Gets whether an implemented interface and its members are omitted from the WinMD.
    /// </summary>
    /// <param name="type">The interface type to classify.</param>
    /// <returns>Whether the interface is omitted from the WinMD.</returns>
    public static bool IsOmittedInterface(INamedTypeSymbol type)
    {
        return WindowsRuntimeTypeMappings.ImplementedInterfacesWithoutMapping.Contains(type.OriginalDefinition.GetFullyQualifiedMetadataName());
    }

    /// <summary>
    /// Enumerates interface declarations without crossing an explicitly excluded base class.
    /// </summary>
    /// <param name="type">The type whose projected contracts to inspect.</param>
    /// <returns>The declared interfaces, with constructed base arguments already substituted by Roslyn.</returns>
    public IEnumerable<INamedTypeSymbol> EnumerateProjectedInterfaceDeclarations(INamedTypeSymbol type)
    {
        for (INamedTypeSymbol? current = type;
             current is not null && !current.IsExcludedFromWindowsMetadata(ExcludeAttributeType);
             current = current.BaseType)
        {
            foreach (INamedTypeSymbol iface in current.Interfaces)
            {
                yield return iface;
            }
        }
    }

    /// <summary>
    /// Enumerates publicly accessible projected interface candidates and their required interfaces.
    /// </summary>
    /// <param name="type">The type whose projected contracts to inspect.</param>
    /// <returns>The distinct interface closure, excluding contracts inherited only through excluded bases.</returns>
    public IEnumerable<INamedTypeSymbol> EnumerateProjectedInterfaces(INamedTypeSymbol type)
    {
        HashSet<INamedTypeSymbol> seen = new(SymbolEqualityComparer.Default);

        foreach (INamedTypeSymbol iface in EnumerateProjectedInterfaceDeclarations(type))
        {
            if (iface.IsPubliclyAccessible && seen.Add(iface))
            {
                yield return iface;
            }

            foreach (INamedTypeSymbol ancestor in iface.AllInterfaces)
            {
                if (ancestor.IsPubliclyAccessible && seen.Add(ancestor))
                {
                    yield return ancestor;
                }
            }
        }
    }

    /// <summary>
    /// Gets whether an input needs a named marshalling local in the projection writer.
    /// </summary>
    /// <param name="type">The parameter type to classify.</param>
    /// <returns>Whether marshalling the type requires a named local.</returns>
    public static bool NeedsMarshallingLocal(ITypeSymbol type)
    {
        static bool Visit(
            ITypeSymbol type,
            HashSet<ITypeSymbol> visited)
        {
            return type.IsReferenceType || type is INamedTypeSymbol { IsGenericType: true } ||
                   type.GetFullyQualifiedMetadataName() is "System.DateTimeOffset" or "System.TimeSpan" ||
                   (type.TypeKind is TypeKind.Struct && visited.Add(type) &&
                    type.GetMembers().OfType<IFieldSymbol>().Any(field =>
                        !field.IsStatic && field.DeclaredAccessibility is Accessibility.Public &&
                        Visit(field.Type, visited)));
        }

        return Visit(type, new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default));
    }

    /// <summary>
    /// Gets whether a signature type has a Windows Runtime representation.
    /// </summary>
    /// <param name="type">The type to classify.</param>
    /// <param name="allowArray">Whether a single-dimensional array is allowed in this position.</param>
    /// <returns>Whether the signature type is supported.</returns>
    public bool IsSupported(
        ITypeSymbol type,
        bool allowArray = true)
    {
        if (FindIgnoredType(type) is not null)
        {
            return false;
        }

        if (type.TypeKind is TypeKind.Error)
        {
            return true;
        }

        if (type is IArrayTypeSymbol array)
        {
            return allowArray && array.IsSZArray && array.ElementType is not IArrayTypeSymbol &&
                   IsSupported(array.ElementType, allowArray: false);
        }

        if (type.SpecialType is SpecialType.System_Void or SpecialType.System_Object ||
            IsPrimitive(type))
        {
            return true;
        }

        if (type is not INamedTypeSymbol named || named.IsRefLikeType)
        {
            return false;
        }

        if (named.OriginalDefinition.SpecialType is SpecialType.System_Nullable_T &&
            named.TypeArguments[0] is { IsValueType: true } element)
        {
            return IsStructFieldType(element);
        }

        if (!IsMapped(named) && !IsProjected(named) &&
            !(IsExported(named) && !named.IsGenericType))
        {
            return false;
        }

        foreach (ITypeSymbol argument in named.TypeArguments)
        {
            if (!IsSupported(argument, allowArray: false))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Finds an excluded type in a signature, including array elements and generic arguments.
    /// </summary>
    /// <param name="type">The signature type to inspect.</param>
    /// <returns>The excluded type, or <see langword="null"/> if no type is excluded.</returns>
    public INamedTypeSymbol? FindIgnoredType(ITypeSymbol type)
    {
        if (type is IArrayTypeSymbol array)
        {
            return FindIgnoredType(array.ElementType);
        }

        if (type is IPointerTypeSymbol pointer)
        {
            return FindIgnoredType(pointer.PointedAtType);
        }

        if (type is INamedTypeSymbol named)
        {
            if (named.IsExcludedFromWindowsMetadata(ExcludeAttributeType))
            {
                return named;
            }

            foreach (ITypeSymbol argument in named.TypeArguments)
            {
                if (FindIgnoredType(argument) is { } ignored)
                {
                    return ignored;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Gets whether a type can be represented as a Windows Runtime struct field.
    /// </summary>
    /// <param name="type">The field type to classify.</param>
    /// <returns>Whether the field type is supported.</returns>
    public bool IsStructFieldType(ITypeSymbol type)
    {
        return type switch
        {
            { TypeKind: TypeKind.Error } => true,
            _ when IsPrimitive(type) => true,
            INamedTypeSymbol named when named.OriginalDefinition.SpecialType is SpecialType.System_Nullable_T =>
                IsStructFieldType(named.TypeArguments[0]),
            INamedTypeSymbol named when IsMapped(named) =>
                _mappedTypes[named.OriginalDefinition.GetFullyQualifiedMetadataName()].IsValueType && IsSupported(named, allowArray: false),
            { TypeKind: TypeKind.Struct or TypeKind.Enum } => IsSupported(type, allowArray: false),
            _ => false
        };
    }

    /// <summary>
    /// Gets whether a type is one of the fundamental Windows Runtime field types.
    /// </summary>
    /// <param name="type">The type to classify.</param>
    /// <returns>Whether the type is a fundamental Windows Runtime type.</returns>
    private static bool IsPrimitive(ITypeSymbol type)
    {
        return type.SpecialType is SpecialType.System_Boolean or SpecialType.System_Byte or SpecialType.System_Char or
            SpecialType.System_Int16 or SpecialType.System_UInt16 or SpecialType.System_Int32 or SpecialType.System_UInt32 or
            SpecialType.System_Int64 or SpecialType.System_UInt64 or SpecialType.System_Single or SpecialType.System_Double or
            SpecialType.System_String ||
            type.GetFullyQualifiedMetadataName() == "System.Guid";
    }
}