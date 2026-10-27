// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace WindowsRuntime.SourceGenerator.Diagnostics;

/// <summary>
/// Validates the API surface exported by Windows Runtime components.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ComponentAuthoringAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
    [
        DiagnosticDescriptors.AuthoredPropertyRequiresGetter,
        DiagnosticDescriptors.AuthoredTypeInvalidNamespace,
        DiagnosticDescriptors.AuthoredNamespaceCaseCollision,
        DiagnosticDescriptors.AuthoredGenericDeclaration,
        DiagnosticDescriptors.AuthoredClassRequiresSealed,
        DiagnosticDescriptors.AuthoredUnsupportedSignatureType,
        DiagnosticDescriptors.AuthoredStructRequiresField,
        DiagnosticDescriptors.AuthoredConstructorArityCollision,
        DiagnosticDescriptors.AuthoredStructNonPublicField,
        DiagnosticDescriptors.AuthoredStructConstantField,
        DiagnosticDescriptors.AuthoredStructInvalidFieldType,
        DiagnosticDescriptors.AuthoredOperatorNotSupported,
        DiagnosticDescriptors.AuthoredDuplicateDefaultOverload,
        DiagnosticDescriptors.AuthoredMissingDefaultOverload,
        DiagnosticDescriptors.AuthoredJaggedArray,
        DiagnosticDescriptors.AuthoredMultidimensionalArray,
        DiagnosticDescriptors.AuthoredUnsupportedInheritance,
        DiagnosticDescriptors.AuthoredByReferenceArray,
        DiagnosticDescriptors.AuthoredByReferenceSpan,
        DiagnosticDescriptors.AuthoredReservedParameterName
    ];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        // The WinMD generator exports the compiled API surface, including declarations from generated source
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static context =>
        {
            if (!context.Options.AnalyzerConfigOptionsProvider.GlobalOptions.GetCsWinRTComponent())
            {
                return;
            }

            AuthoringTypeValidation validation = new(
                context.Compilation,
                context.Options.AnalyzerConfigOptionsProvider.GlobalOptions.GetCsWinRTUseWindowsUIXamlProjections());
            INamedTypeSymbol? defaultOverloadAttributeType =
                context.Compilation.GetTypeByMetadataName("Windows.Foundation.Metadata.DefaultOverloadAttribute");
            ConcurrentDictionary<string, INamedTypeSymbol> namespaces = new(StringComparer.Ordinal);

            context.RegisterSymbolAction(context =>
            {
                INamedTypeSymbol type = (INamedTypeSymbol)context.Symbol;

                if (!validation.IsExported(type))
                {
                    return;
                }

                string ns = type.ContainingNamespace.IsGlobalNamespace ? "" : type.ContainingNamespace.ToDisplayString();
                string assemblyName = context.Compilation.AssemblyName!;

                if (ns != assemblyName && !ns.StartsWith(assemblyName + ".", StringComparison.Ordinal))
                {
                    Report(context, DiagnosticDescriptors.AuthoredTypeInvalidNamespace, type, type, assemblyName);
                }

                // Record this namespace and its parents to detect casing conflicts such as
                // Component.Foo.A versus Component.foo.B at their Foo/foo parent
                // The type is only a representative declaration used to locate the diagnostic
                for (string current = ns; current.Length > 0;)
                {
                    _ = namespaces.TryAdd(current, type);
                    int separator = current.LastIndexOf('.');
                    current = separator < 0 ? "" : current[..separator];
                }

                // CsWinRT cannot author generic Windows Runtime types; skip redundant member diagnostics
                if (type.IsGenericType)
                {
                    Report(context, DiagnosticDescriptors.AuthoredGenericDeclaration, type, type);

                    return;
                }

                if (type.TypeKind is TypeKind.Struct)
                {
                    AnalyzeStruct(context, type, validation);

                    return;
                }

                if (type.TypeKind is TypeKind.Delegate)
                {
                    // Validate the delegate's declared return type and parameters through its Invoke method
                    if (type.DelegateInvokeMethod is { } invoke)
                    {
                        AnalyzeMethod(context, invoke, validation);
                    }

                    return;
                }

                if (type.TypeKind is not (TypeKind.Class or TypeKind.Interface))
                {
                    return;
                }

                if (type is { TypeKind: TypeKind.Class, IsStatic: false, IsSealed: false })
                {
                    Report(context, DiagnosticDescriptors.AuthoredClassRequiresSealed, type, type);
                }

                // The WinMD generator replaces abstract managed bases with System.Object, so allow them
                if (type.BaseType is { SpecialType: not SpecialType.System_Object, TypeKind: not TypeKind.Error } baseType &&
                    !baseType.IsAbstract &&
                    !validation.IsProjected(baseType))
                {
                    Report(context, DiagnosticDescriptors.AuthoredUnsupportedInheritance, type, type, baseType);
                }

                foreach (INamedTypeSymbol iface in type.Interfaces)
                {
                    // Non-public managed implementation interfaces are not emitted as Windows Runtime contracts
                    if (type.TypeKind is TypeKind.Class && !iface.IsPubliclyAccessible)
                    {
                        continue;
                    }

                    if (!AuthoringTypeValidation.IsOmittedInterface(iface) && !validation.IsSupported(iface, allowArray: false))
                    {
                        Report(context, DiagnosticDescriptors.AuthoredUnsupportedInheritance, type, type, iface);
                    }
                }

                // Skip members replaced by mapped contracts, but validate other exported members
                HashSet<ISymbol> omittedMembers = GetOmittedMembers(type, validation);
                List<IMethodSymbol> methods = [];

                foreach (ISymbol member in type.GetMembers())
                {
                    context.CancellationToken.ThrowIfCancellationRequested();

                    if (member.IsImplicitlyDeclared || member.DeclaredAccessibility is not Accessibility.Public || omittedMembers.Contains(member))
                    {
                        continue;
                    }

                    switch (member)
                    {
                        case IPropertySymbol property:
                            if (property.GetMethod is not { DeclaredAccessibility: Accessibility.Public })
                            {
                                Report(context, DiagnosticDescriptors.AuthoredPropertyRequiresGetter, property, property);
                            }

                            AnalyzeSignatureType(context, property, property.Type, validation);

                            foreach (IParameterSymbol parameter in property.Parameters)
                            {
                                AnalyzeParameter(context, property, parameter, validation);
                            }

                            break;
                        case IEventSymbol @event:
                            AnalyzeSignatureType(context, @event, @event.Type, validation);
                            break;
                        case IMethodSymbol { MethodKind: MethodKind.UserDefinedOperator or MethodKind.Conversion } method:
                            Report(context, DiagnosticDescriptors.AuthoredOperatorNotSupported, method, method);
                            break;
                        case IMethodSymbol { MethodKind: MethodKind.Ordinary or MethodKind.Constructor } method:
                            AnalyzeMethod(context, method, validation);
                            methods.Add(method);
                            break;
                        default:
                            break;
                    }
                }

                AnalyzeOverloads(context, type, methods, defaultOverloadAttributeType);
            }, SymbolKind.NamedType);

            context.RegisterCompilationEndAction(context =>
            {
                Dictionary<string, string> seen = new(StringComparer.OrdinalIgnoreCase);

                // Sort namespace spellings so collision diagnostics report casing in a stable order
                foreach ((string ns, INamedTypeSymbol type) in namespaces.OrderBy(static pair => pair.Key, StringComparer.Ordinal))
                {
                    if (seen.TryGetValue(ns, out string? previous))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.AuthoredNamespaceCaseCollision, type.Locations.FirstOrDefault(), ns, previous));
                    }
                    else
                    {
                        seen.Add(ns, ns);
                    }
                }
            });
        });
    }

    /// <summary>
    /// Validates the fields that define an authored struct's Windows Runtime layout.
    /// </summary>
    private static void AnalyzeStruct(
        SymbolAnalysisContext context,
        INamedTypeSymbol type,
        AuthoringTypeValidation validation)
    {
        bool hasPublicField = false;

        foreach (IFieldSymbol field in type.GetMembers().OfType<IFieldSymbol>())
        {
            if (field.IsImplicitlyDeclared)
            {
                continue;
            }

            if (field.IsConst)
            {
                Report(context, DiagnosticDescriptors.AuthoredStructConstantField, field, field);
            }
            else if (!field.IsStatic)
            {
                if (field.DeclaredAccessibility is not Accessibility.Public)
                {
                    Report(context, DiagnosticDescriptors.AuthoredStructNonPublicField, field, field);
                }
                else
                {
                    hasPublicField = true;

                    if (!validation.IsStructFieldType(field.Type))
                    {
                        Report(context, DiagnosticDescriptors.AuthoredStructInvalidFieldType, field, field, field.Type);
                    }
                }
            }
        }

        if (!hasPublicField)
        {
            Report(context, DiagnosticDescriptors.AuthoredStructRequiresField, type, type);
        }
    }

    /// <summary>
    /// Validates exported signatures and rejects parameter names that conflict with generated marshalling identifiers.
    /// </summary>
    private static void AnalyzeMethod(
        SymbolAnalysisContext context,
        IMethodSymbol method,
        AuthoringTypeValidation validation)
    {
        if (method.IsGenericMethod)
        {
            Report(context, DiagnosticDescriptors.AuthoredGenericDeclaration, method, method);

            return;
        }

        if (method.ReturnsByRef || method.ReturnsByRefReadonly)
        {
            Report(context, DiagnosticDescriptors.AuthoredUnsupportedSignatureType, method, method, method.ReturnType);
        }
        else
        {
            AnalyzeSignatureType(context, method, method.ReturnType, validation);
        }

        HashSet<string> names = new(method.Parameters.Select(static parameter => parameter.Name), StringComparer.Ordinal);

        foreach (IParameterSymbol parameter in method.Parameters)
        {
            AnalyzeParameter(context, method, parameter, validation);

            // Constructors return an instance at the ABI, so they also reserve return-value identifiers
            bool hasReturn = !method.ReturnsVoid || method.MethodKind is MethodKind.Constructor;
            string? collision = parameter.Name switch
            {
                "thisReference" or "thisValue" or "thisPtr" or "ThisPtr" => parameter.Name,
                "__retval" or "__return_value__" or "____return_value__" when hasReturn => parameter.Name,
                "__retval_length" or "__retval_data" or "____return_value__Size" when method.ReturnType is IArrayTypeSymbol => parameter.Name,
                _ => null
            };

            collision ??= GetDerivedNameCollision(parameter, names);

            if (collision is not null)
            {
                Report(context, DiagnosticDescriptors.AuthoredReservedParameterName, parameter, parameter, collision);
            }
        }
    }

    /// <summary>
    /// Finds parameter names that collide with generated marshalling identifiers.
    /// </summary>
    private static string? GetDerivedNameCollision(
        IParameterSymbol parameter,
        HashSet<string> names)
    {
        bool isArray = parameter.Type is IArrayTypeSymbol || AuthoringTypeValidation.IsSpan(parameter.Type);
        string prefix = "__" + parameter.Name;

        // Array marshalling, value conversions, and out parameters can introduce a '__name' local
        if (names.Contains(prefix) &&
            (isArray || parameter.RefKind is RefKind.Out || AuthoringTypeValidation.NeedsMarshallingLocal(parameter.Type)))
        {
            return prefix;
        }

        // Some inputs also need a '_name' alias for pinning or passing native pointers
        if (parameter.RefKind is not RefKind.Out &&
            (isArray || parameter.Type.SpecialType is SpecialType.System_String ||
             parameter.Type.GetFullyQualifiedMetadataName() == "System.Type" ||
             parameter.RefKind is RefKind.Ref or RefKind.In or RefKind.RefReadOnlyParameter) &&
            names.Contains("_" + parameter.Name))
        {
            return "_" + parameter.Name;
        }

        // Parameters with constructed generic types also introduce named conversion callbacks
        if (parameter.RefKind is RefKind.None &&
            parameter.Type is INamedTypeSymbol { IsGenericType: true } &&
            !AuthoringTypeValidation.IsSpan(parameter.Type))
        {
            foreach (string candidate in new[]
            {
                "__arg_" + parameter.Name, "ConvertToUnmanaged_" + parameter.Name,
                "ConvertToManaged_arg_" + parameter.Name
            })
            {
                if (names.Contains(candidate))
                {
                    return candidate;
                }
            }
        }

        if (!isArray)
        {
            return null;
        }

        if (names.Contains(prefix + "Size"))
        {
            return prefix + "Size";
        }

        if (parameter.RefKind is RefKind.Out)
        {
            // Receive arrays use separate native length and data locals
            return names.Contains(prefix + "_length")
                ? prefix + "_length"
                : names.Contains(prefix + "_data") ? prefix + "_data" : null;
        }

        ITypeSymbol element = parameter.Type is IArrayTypeSymbol array
            ? array.ElementType
            : ((INamedTypeSymbol)parameter.Type).TypeArguments[0];

        if (AuthoringTypeValidation.NeedsMarshallingLocal(element))
        {
            // Array conversion buffers use the projection writer's 'ArrayTempNames' naming scheme
            foreach (string suffix in new[] { "_span", "_inlineArray", "_arrayFromPool" })
            {
                if (names.Contains(prefix + suffix))
                {
                    return prefix + suffix;
                }
            }
        }

        bool isPassArray = parameter.Type is IArrayTypeSymbol or INamedTypeSymbol { MetadataName: "ReadOnlySpan`1" };

        if (isPassArray && element.SpecialType is SpecialType.System_String)
        {
            // String pass arrays pin HSTRING headers separately from the managed elements
            string headerPin = "_" + parameter.Name + "_inlineHeaderArray";

            if (names.Contains(headerPin))
            {
                return headerPin;
            }

            foreach (string suffix in new[]
            {
                "_inlineHeaderArray", "_headerArrayFromPool", "_headerSpan",
                "_inlinePinnedHandleArray", "_pinnedHandleArrayFromPool", "_pinnedHandleSpan"
            })
            {
                if (names.Contains(prefix + suffix))
                {
                    return prefix + suffix;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Validates parameter types and array/span calling conventions.
    /// </summary>
    private static void AnalyzeParameter(
        SymbolAnalysisContext context,
        ISymbol member,
        IParameterSymbol parameter,
        AuthoringTypeValidation validation)
    {
        if (AuthoringTypeValidation.IsSpan(parameter.Type))
        {
            if (parameter.RefKind is not RefKind.None)
            {
                Report(context, DiagnosticDescriptors.AuthoredByReferenceSpan, parameter, parameter);
            }

            INamedTypeSymbol span = (INamedTypeSymbol)parameter.Type;
            AnalyzeSignatureType(context, member, span.TypeArguments[0], validation, arrayElement: true);
        }
        else
        {
            if (parameter.Type is IArrayTypeSymbol && parameter.RefKind is not (RefKind.None or RefKind.Out))
            {
                Report(context, DiagnosticDescriptors.AuthoredByReferenceArray, parameter, parameter);
            }

            AnalyzeSignatureType(context, member, parameter.Type, validation);
        }
    }

    /// <summary>
    /// Reports a specific array-shape error or an unsupported signature type.
    /// </summary>
    private static void AnalyzeSignatureType(
        SymbolAnalysisContext context,
        ISymbol member,
        ITypeSymbol type,
        AuthoringTypeValidation validation,
        bool arrayElement = false)
    {
        if (type is IArrayTypeSymbol array)
        {
            if (!array.IsSZArray)
            {
                Report(context, DiagnosticDescriptors.AuthoredMultidimensionalArray, member, member, type);

                return;
            }

            // Span<T[]> is also jagged, since the span represents the outer Windows Runtime array
            if (array.ElementType is IArrayTypeSymbol || arrayElement)
            {
                Report(context, DiagnosticDescriptors.AuthoredJaggedArray, member, member, type);

                return;
            }
        }

        if (!validation.IsSupported(type, allowArray: !arrayElement))
        {
            Report(context, DiagnosticDescriptors.AuthoredUnsupportedSignatureType, member, member, type);
        }
    }

    /// <summary>
    /// Validates constructor arity collisions and default overload annotations, counting only input parameters.
    /// </summary>
    private static void AnalyzeOverloads(
        SymbolAnalysisContext context,
        INamedTypeSymbol type,
        List<IMethodSymbol> methods,
        INamedTypeSymbol? defaultOverloadAttributeType)
    {
        // Group constructors by input parameter count
        foreach (IGrouping<int, IMethodSymbol> group in methods
            .Where(static method => method.MethodKind is MethodKind.Constructor)
            .GroupBy(static method => method.GetWindowsRuntimeInputArity()))
        {
            // Report later declarations because the first constructor already occupies this input count
            foreach (IMethodSymbol duplicate in group.Skip(1))
            {
                Report(context, DiagnosticDescriptors.AuthoredConstructorArityCollision, duplicate, type, group.Key);
            }
        }

        // Overloads need one default per name and input count, separately for instance and static calls
        foreach (IGrouping<(string Name, int Arity, bool Static), IMethodSymbol> group in methods
            .Where(static method => method.MethodKind is MethodKind.Ordinary && !method.IsGenericMethod)
            .GroupBy(static method => (method.Name, method.GetWindowsRuntimeInputArity(), method.IsStatic)))
        {
            IMethodSymbol[] defaults = [.. group.Where(method => type.HasDefaultOverload(method, defaultOverloadAttributeType))];

            if (defaults.Length > 1)
            {
                // Report conflicting annotations after the first default declaration
                foreach (IMethodSymbol duplicate in defaults.Skip(1))
                {
                    Report(context, DiagnosticDescriptors.AuthoredDuplicateDefaultOverload, duplicate, group.Key.Name, group.Key.Arity);
                }
            }
            else if (defaults.Length == 0 && group.Skip(1).Any())
            {
                Report(context, DiagnosticDescriptors.AuthoredMissingDefaultOverload, group.First(), group.Key.Name, group.Key.Arity);
            }
        }
    }

    /// <summary>
    /// Collects interface implementations that the WinMD generator replaces or omits.
    /// </summary>
    private static HashSet<ISymbol> GetOmittedMembers(
        INamedTypeSymbol type,
        AuthoringTypeValidation validation)
    {
        HashSet<ISymbol> members = new(SymbolEqualityComparer.Default);

        foreach (INamedTypeSymbol iface in type.AllInterfaces)
        {
            if (!validation.IsMapped(iface) && !AuthoringTypeValidation.IsOmittedInterface(iface))
            {
                continue;
            }

            foreach (ISymbol member in iface.GetMembers())
            {
                if (type.FindImplementationForInterfaceMember(member) is { } implementation)
                {
                    _ = members.Add(implementation);
                }
            }
        }

        return members;
    }

    /// <summary>
    /// Reports an authoring diagnostic at the declaration's identifier.
    /// </summary>
    private static void Report(
        SymbolAnalysisContext context,
        DiagnosticDescriptor descriptor,
        ISymbol symbol,
        params object[] arguments)
    {
        context.ReportDiagnostic(Diagnostic.Create(descriptor, symbol.Locations.FirstOrDefault(), arguments));
    }
}