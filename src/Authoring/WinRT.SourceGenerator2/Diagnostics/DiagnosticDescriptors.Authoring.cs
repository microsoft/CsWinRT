// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.CodeAnalysis;

namespace WindowsRuntime.SourceGenerator.Diagnostics;

internal static partial class DiagnosticDescriptors
{
    /// <summary>
    /// Gets a <see cref="DiagnosticDescriptor"/> for an authored property without a public getter.
    /// </summary>
    public static readonly DiagnosticDescriptor AuthoredPropertyRequiresGetter = CreateAuthoringRule(
        id: "CSWINRT2028",
        title: "Authored property requires a public getter",
        message: "The Windows Runtime property '{0}' must have a public getter",
        description: "Windows Runtime properties must have a public getter.");

    /// <summary>
    /// Gets a <see cref="DiagnosticDescriptor"/> for an exported namespace outside the component namespace.
    /// </summary>
    public static readonly DiagnosticDescriptor AuthoredTypeInvalidNamespace = CreateAuthoringRule(
        id: "CSWINRT2029",
        title: "Invalid authored type namespace",
        message: "The exported type '{0}' must be in the namespace '{1}' or one of its subnamespaces",
        description: "Exported types must be declared in a namespace matching the component assembly name or one of its subnamespaces.");

    /// <summary>
    /// Gets a <see cref="DiagnosticDescriptor"/> for exported namespaces differing only by case.
    /// </summary>
    public static readonly DiagnosticDescriptor AuthoredNamespaceCaseCollision = CreateAuthoringRule(
        id: "CSWINRT2030",
        title: "Authored namespaces differ only by case",
        message: "The exported namespace '{0}' differs only by case from '{1}'; Windows Runtime namespaces are case-insensitive",
        description: "Exported namespace names must not differ only by case because Windows Runtime namespaces are case-insensitive.",
        compilationEnd: true);

    /// <summary>
    /// Gets a <see cref="DiagnosticDescriptor"/> for a generic authored type or method.
    /// </summary>
    public static readonly DiagnosticDescriptor AuthoredGenericDeclaration = CreateAuthoringRule(
        id: "CSWINRT2031",
        title: "Generic authored declaration",
        message: "The exported declaration '{0}' cannot be generic; use closed instances of supported Windows Runtime generic types in signatures instead",
        description: "Components cannot declare generic Windows Runtime types or methods. Closed instances of supported Windows Runtime generic types can be used in signatures.");

    /// <summary>
    /// Gets a <see cref="DiagnosticDescriptor"/> for an unsealed authored runtime class.
    /// </summary>
    public static readonly DiagnosticDescriptor AuthoredClassRequiresSealed = CreateAuthoringRule(
        id: "CSWINRT2032",
        title: "Authored runtime class must be sealed",
        message: "The authored runtime class '{0}' must be sealed; CsWinRT cannot generate a composable base class from this declaration",
        description: "Authored runtime classes must be sealed because CsWinRT cannot generate composable base classes from managed declarations.");

    /// <summary>
    /// Gets a <see cref="DiagnosticDescriptor"/> for an unsupported type in an authored signature.
    /// </summary>
    public static readonly DiagnosticDescriptor AuthoredUnsupportedSignatureType = CreateAuthoringRule(
        id: "CSWINRT2033",
        title: "Unsupported authored signature type",
        message: "The exported member '{0}' uses '{1}', which has no supported Windows Runtime representation",
        description: "Exported member signatures must use types with a supported Windows Runtime representation.");

    /// <summary>
    /// Gets a <see cref="DiagnosticDescriptor"/> for an empty authored struct.
    /// </summary>
    public static readonly DiagnosticDescriptor AuthoredStructRequiresField = CreateAuthoringRule(
        id: "CSWINRT2034",
        title: "Authored struct requires a public instance field",
        message: "The authored struct '{0}' must contain at least one public instance field",
        description: "Windows Runtime structs must contain at least one public instance field.");

    /// <summary>
    /// Gets a <see cref="DiagnosticDescriptor"/> for authored constructors with the same arity.
    /// </summary>
    public static readonly DiagnosticDescriptor AuthoredConstructorArityCollision = CreateAuthoringRule(
        id: "CSWINRT2035",
        title: "Authored constructors have the same arity",
        message: "The authored class '{0}' has more than one public constructor with {1} input parameters",
        description: "Public constructors must have distinct input-parameter counts because Windows Runtime activation does not support constructor overloads with the same arity.");

    /// <summary>
    /// Gets a <see cref="DiagnosticDescriptor"/> for a non-public authored struct field.
    /// </summary>
    public static readonly DiagnosticDescriptor AuthoredStructNonPublicField = CreateAuthoringRule(
        id: "CSWINRT2036",
        title: "Non-public authored struct field",
        message: "The instance field '{0}' of a Windows Runtime struct must be public",
        description: "Every instance field of a Windows Runtime struct must be public.");

    /// <summary>
    /// Gets a <see cref="DiagnosticDescriptor"/> for a constant authored struct field.
    /// </summary>
    public static readonly DiagnosticDescriptor AuthoredStructConstantField = CreateAuthoringRule(
        id: "CSWINRT2037",
        title: "Constant authored struct field",
        message: "The constant field '{0}' cannot be exported as a Windows Runtime struct field",
        description: "Windows Runtime structs cannot expose constant fields.");

    /// <summary>
    /// Gets a <see cref="DiagnosticDescriptor"/> for an unsupported authored struct field type.
    /// </summary>
    public static readonly DiagnosticDescriptor AuthoredStructInvalidFieldType = CreateAuthoringRule(
        id: "CSWINRT2038",
        title: "Invalid authored struct field type",
        message: "The field '{0}' has type '{1}'; Windows Runtime struct fields must be supported primitives, strings, enums, structs, or supported nullable value types",
        description: "Windows Runtime struct fields must use supported primitives, strings, enums, structs, or nullable value types.");

    /// <summary>
    /// Gets a <see cref="DiagnosticDescriptor"/> for an authored operator.
    /// </summary>
    public static readonly DiagnosticDescriptor AuthoredOperatorNotSupported = CreateAuthoringRule(
        id: "CSWINRT2039",
        title: "Authored operator is not supported",
        message: "The operator '{0}' cannot be exported because Windows Runtime does not support operator overloads",
        description: "Windows Runtime does not support exported operator overloads or conversion operators.");

    /// <summary>
    /// Gets a <see cref="DiagnosticDescriptor"/> for duplicate default overloads.
    /// </summary>
    public static readonly DiagnosticDescriptor AuthoredDuplicateDefaultOverload = CreateAuthoringRule(
        id: "CSWINRT2040",
        title: "Multiple default authored overloads",
        message: "The overloads of '{0}' with {1} input parameters must not specify more than one '[DefaultOverload]'",
        description: "Each group of static or instance method overloads with the same name and input-parameter count can specify at most one DefaultOverload attribute.");

    /// <summary>
    /// Gets a <see cref="DiagnosticDescriptor"/> for an authored overload group without a default.
    /// </summary>
    public static readonly DiagnosticDescriptor AuthoredMissingDefaultOverload = CreateAuthoringRule(
        id: "CSWINRT2041",
        title: "Authored overload group requires a default",
        message: "The overloads of '{0}' with {1} input parameters must specify exactly one '[DefaultOverload]'",
        description: "Each group of static or instance method overloads with the same name and input-parameter count must specify a default using the DefaultOverload attribute when multiple overloads exist.");

    /// <summary>
    /// Gets a <see cref="DiagnosticDescriptor"/> for a jagged authored array.
    /// </summary>
    public static readonly DiagnosticDescriptor AuthoredJaggedArray = CreateAuthoringRule(
        id: "CSWINRT2042",
        title: "Jagged authored array",
        message: "The exported member '{0}' uses '{1}'; Windows Runtime does not support arrays of arrays",
        description: "Windows Runtime arrays cannot contain other arrays.");

    /// <summary>
    /// Gets a <see cref="DiagnosticDescriptor"/> for a multidimensional authored array.
    /// </summary>
    public static readonly DiagnosticDescriptor AuthoredMultidimensionalArray = CreateAuthoringRule(
        id: "CSWINRT2043",
        title: "Multidimensional authored array",
        message: "The exported member '{0}' uses '{1}'; Windows Runtime arrays must be single-dimensional, zero-based arrays",
        description: "Windows Runtime arrays must be single-dimensional and zero-based.");

    /// <summary>
    /// Gets a <see cref="DiagnosticDescriptor"/> for unsupported authored inheritance.
    /// </summary>
    public static readonly DiagnosticDescriptor AuthoredUnsupportedInheritance = CreateAuthoringRule(
        id: "CSWINRT2044",
        title: "Unsupported authored inheritance",
        message: "The exported type '{0}' cannot expose '{1}' as a Windows Runtime base type or interface",
        description: "Base classes and implemented interfaces exposed in Windows Runtime metadata must have a supported Windows Runtime representation.");

    /// <summary>
    /// Gets a <see cref="DiagnosticDescriptor"/> for an unsupported by-reference authored array.
    /// </summary>
    public static readonly DiagnosticDescriptor AuthoredByReferenceArray = CreateAuthoringRule(
        id: "CSWINRT2045",
        title: "Invalid by-reference authored array",
        message: "The parameter '{0}' must not be a 'ref' or 'in' array; use 'ReadOnlySpan<T>', 'Span<T>', or 'out T[]'",
        description: "Array parameters cannot be passed by ref or in. Use ReadOnlySpan<T> or Span<T> by value, or out T[], to express the Windows Runtime array direction.");

    /// <summary>
    /// Gets a <see cref="DiagnosticDescriptor"/> for an unsupported by-reference authored span.
    /// </summary>
    public static readonly DiagnosticDescriptor AuthoredByReferenceSpan = CreateAuthoringRule(
        id: "CSWINRT2046",
        title: "Invalid by-reference authored span",
        message: "The parameter '{0}' must pass its span by value; use 'ReadOnlySpan<T>', 'Span<T>', or 'out T[]'",
        description: "Span parameters must be passed by value. Use ReadOnlySpan<T>, Span<T>, or out T[] to express the Windows Runtime array direction.");

    /// <summary>
    /// Gets a <see cref="DiagnosticDescriptor"/> for a parameter name conflicting with generated interop code.
    /// </summary>
    public static readonly DiagnosticDescriptor AuthoredReservedParameterName = CreateAuthoringRule(
        id: "CSWINRT2047",
        title: "Authored parameter name conflicts with generated code",
        message: "The parameter '{0}' conflicts with the generated identifier '{1}'; choose a different parameter name",
        description: "Parameter names must not conflict with identifiers reserved or generated by CsWinRT marshalling code.");

    /// <summary>
    /// Gets a <see cref="DiagnosticDescriptor"/> for an exclusion that would break a metadata contract.
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidExcludeFromWindowsMetadata = CreateAuthoringRule(
        id: "CSWINRT2048",
        title: "Invalid Windows Runtime metadata exclusion",
        message: "The declaration '{0}' cannot use '[ExcludeFromWindowsMetadata]': {1}",
        description: "ExcludeFromWindowsMetadata can only be applied to declarations whose omission leaves the exported Windows Runtime metadata valid.");

    /// <summary>
    /// Gets a <see cref="DiagnosticDescriptor"/> for an exported signature referencing an excluded type.
    /// </summary>
    public static readonly DiagnosticDescriptor IgnoredTypeInExportedSignature = CreateAuthoringRule(
        id: "CSWINRT2049",
        title: "Ignored type in exported Windows Runtime signature",
        message: "The exported declaration '{0}' references ignored type '{1}'; exclude the declaration too or use an exported Windows Runtime type",
        description: "Exported declarations must not reference types excluded from Windows Runtime metadata.");

    /// <summary>
    /// Creates an error describing an invalid Windows Runtime authoring declaration.
    /// </summary>
    /// <param name="id">The diagnostic identifier.</param>
    /// <param name="title">The diagnostic title.</param>
    /// <param name="message">The diagnostic message format.</param>
    /// <param name="description">The standalone explanation of the diagnostic.</param>
    /// <param name="compilationEnd">Whether the diagnostic is reported from a compilation-end action.</param>
    /// <returns>The diagnostic descriptor.</returns>
    private static DiagnosticDescriptor CreateAuthoringRule(
        string id,
        string title,
        string message,
        string description,
        bool compilationEnd = false)
    {
        return new DiagnosticDescriptor(
            id: id,
            title: title,
            messageFormat: message,
            category: "WindowsRuntime.SourceGenerator",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: description,
            helpLinkUri: "https://github.com/microsoft/CsWinRT",
            customTags: compilationEnd ? [WellKnownDiagnosticTags.CompilationEnd] : []);
    }
}