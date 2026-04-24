// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.CodeAnalysis;

namespace WindowsRuntime.SourceGenerator.Diagnostics;

/// <summary>
/// A container for all <see cref="DiagnosticDescriptor"/> instances for errors reported by analyzers in this project.
/// </summary>
internal static partial class DiagnosticDescriptors
{
    /// <summary>
    /// Gets a <see cref="DiagnosticDescriptor"/> for an invalid target type for <c>[GeneratedCustomPropertyProvider]</c>.
    /// </summary>
    public static readonly DiagnosticDescriptor GeneratedCustomPropertyProviderInvalidTargetType = new(
        id: "CSWINRT2000",
        title: "Invalid '[GeneratedCustomPropertyProvider]' target type",
        messageFormat: """The type '{0}' is not a valid target for '[GeneratedCustomPropertyProvider]': it must be a 'class' or 'struct' type, and it can't be 'static', 'abstract', or 'ref'""",
        category: "WindowsRuntime.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Types annotated with '[GeneratedCustomPropertyProvider]' must be 'class' or 'struct' types, and they can't be 'static', 'abstract', or 'ref'.",
        helpLinkUri: "https://github.com/microsoft/CsWinRT");

    /// <summary>
    /// Gets a <see cref="DiagnosticDescriptor"/> for a target type for <c>[GeneratedCustomPropertyProvider]</c> missing <see langword="partial"/>.
    /// </summary>
    public static readonly DiagnosticDescriptor GeneratedCustomPropertyProviderMissingPartialModifier = new(
        id: "CSWINRT2001",
        title: "Missing 'partial' for '[GeneratedCustomPropertyProvider]' target type",
        messageFormat: """The type '{0}' (or one of its containing types) is missing the 'partial' modifier, which is required to be used as a target for '[GeneratedCustomPropertyProvider]'""",
        category: "WindowsRuntime.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Types annotated with '[GeneratedCustomPropertyProvider]' must be marked as 'partial' across their whole type hierarchy.",
        helpLinkUri: "https://github.com/microsoft/CsWinRT");

    /// <summary>
    /// Gets a <see cref="DiagnosticDescriptor"/> for when <c>[GeneratedCustomPropertyProvider]</c> can't resolve the interface type.
    /// </summary>
    public static readonly DiagnosticDescriptor GeneratedCustomPropertyProviderNoAvailableInterfaceType = new(
        id: "CSWINRT2002",
        title: "'ICustomPropertyProvider' interface type not available",
        messageFormat: """The 'ICustomPropertyProvider' interface is not available in the compilation, but it is required to use '[GeneratedCustomPropertyProvider]' (make sure to either reference 'Microsoft.WindowsAppSDK.WinUI' or set the 'UseUwp' property in your .csproj file)""",
        category: "WindowsRuntime.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Using '[GeneratedCustomPropertyProvider]' requires the 'ICustomPropertyProvider' interface type to be available in the compilation, which can be done by either referencing 'WindowsAppSDK.WinUI' or by setting the 'UseUwp' property in the .csproj file.",
        helpLinkUri: "https://github.com/microsoft/CsWinRT");

    /// <summary>
    /// Gets a <see cref="DiagnosticDescriptor"/> for when <c>[GeneratedCustomPropertyProvider]</c> is used on a type that already implements <c>ICustomPropertyProvider</c> members.
    /// </summary>
    public static readonly DiagnosticDescriptor GeneratedCustomPropertyProviderExistingMemberImplementation = new(
        id: "CSWINRT2003",
        title: "Existing 'ICustomPropertyProvider' member implementation",
        messageFormat: """The type '{0}' cannot use '[GeneratedCustomPropertyProvider]' because it already has or inherits implementations for one or more 'ICustomPropertyProvider' members""",
        category: "WindowsRuntime.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Types annotated with '[GeneratedCustomPropertyProvider]' must not already have or inherit implementations for any 'ICustomPropertyProvider' members, as the generator will provide them.",
        helpLinkUri: "https://github.com/microsoft/CsWinRT");

    /// <summary>
    /// Gets a <see cref="DiagnosticDescriptor"/> for when a <see langword="null"/> property name is specified in <c>[GeneratedCustomPropertyProvider]</c>.
    /// </summary>
    public static readonly DiagnosticDescriptor GeneratedCustomPropertyProviderNullPropertyName = new(
        id: "CSWINRT2004",
        title: "Null property name in '[GeneratedCustomPropertyProvider]'",
        messageFormat: """A null property name was specified in '[GeneratedCustomPropertyProvider]' on type '{0}'""",
        category: "WindowsRuntime.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Property names specified in '[GeneratedCustomPropertyProvider]' must not be null.",
        helpLinkUri: "https://github.com/microsoft/CsWinRT");

    /// <summary>
    /// Gets a <see cref="DiagnosticDescriptor"/> for when a <see langword="null"/> indexer type is specified in <c>[GeneratedCustomPropertyProvider]</c>.
    /// </summary>
    public static readonly DiagnosticDescriptor GeneratedCustomPropertyProviderNullIndexerType = new(
        id: "CSWINRT2005",
        title: "Null indexer type in '[GeneratedCustomPropertyProvider]'",
        messageFormat: """A null indexer type was specified in '[GeneratedCustomPropertyProvider]' on type '{0}'""",
        category: "WindowsRuntime.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Indexer types specified in '[GeneratedCustomPropertyProvider]' must not be null.",
        helpLinkUri: "https://github.com/microsoft/CsWinRT");

    /// <summary>
    /// Gets a <see cref="DiagnosticDescriptor"/> for when a property name in <c>[GeneratedCustomPropertyProvider]</c> doesn't match any accessible property.
    /// </summary>
    public static readonly DiagnosticDescriptor GeneratedCustomPropertyProviderPropertyNameNotFound = new(
        id: "CSWINRT2006",
        title: "Property name not found for '[GeneratedCustomPropertyProvider]'",
        messageFormat: """The property name '{0}' specified in '[GeneratedCustomPropertyProvider]' on type '{1}' does not match any accessible property""",
        category: "WindowsRuntime.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Property names specified in '[GeneratedCustomPropertyProvider]' must match the name of a public, non-override, non-indexer property on the annotated type.",
        helpLinkUri: "https://github.com/microsoft/CsWinRT");

    /// <summary>
    /// Gets a <see cref="DiagnosticDescriptor"/> for when an indexer type in <c>[GeneratedCustomPropertyProvider]</c> doesn't match any accessible indexer.
    /// </summary>
    public static readonly DiagnosticDescriptor GeneratedCustomPropertyProviderIndexerTypeNotFound = new(
        id: "CSWINRT2007",
        title: "Indexer type not found for '[GeneratedCustomPropertyProvider]'",
        messageFormat: """The indexer type '{0}' specified in '[GeneratedCustomPropertyProvider]' on type '{1}' does not match any accessible indexer""",
        category: "WindowsRuntime.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Indexer types specified in '[GeneratedCustomPropertyProvider]' must match the parameter type of a public, non-override, non-static, single-parameter indexer on the annotated type.",
        helpLinkUri: "https://github.com/microsoft/CsWinRT");

    /// <summary>
    /// Gets a <see cref="DiagnosticDescriptor"/> for when an indexer type in <c>[GeneratedCustomPropertyProvider]</c> matches a static indexer.
    /// </summary>
    public static readonly DiagnosticDescriptor GeneratedCustomPropertyProviderStaticIndexer = new(
        id: "CSWINRT2008",
        title: "Static indexer for '[GeneratedCustomPropertyProvider]'",
        messageFormat: """The indexer type '{0}' specified in '[GeneratedCustomPropertyProvider]' on type '{1}' matches a static indexer, which is not supported""",
        category: "WindowsRuntime.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Indexers used with '[GeneratedCustomPropertyProvider]' must be instance indexers, not static indexers.",
        helpLinkUri: "https://github.com/microsoft/CsWinRT");

    /// <summary>
    /// Gets a <see cref="DiagnosticDescriptor"/> for a cast to a <see cref="System.Runtime.InteropServices.ComImportAttribute"/> interface type.
    /// </summary>
    public static readonly DiagnosticDescriptor ComImportInterfaceCast = new(
        id: "CSWINRT2009",
        title: "Cast to '[ComImport]' type not supported",
        messageFormat: """The type '{0}' used in a cast operation is a '[ComImport]' interface, which is not compatible with Windows Runtime objects marshalled by CsWinRT. Consider using the COM generators to define the interface, or manually handling the interface query on the underlying native object.""",
        category: "WindowsRuntime.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Types used in cast operations must not be '[ComImport]' interfaces, as they are not compatible with Windows Runtime objects marshalled by CsWinRT.",
        helpLinkUri: "https://github.com/microsoft/CsWinRT");

    /// <summary>
    /// Gets a <see cref="DiagnosticDescriptor"/> for an <c>[ApiContract]</c> enum type that defines enum cases.
    /// </summary>
    public static readonly DiagnosticDescriptor ApiContractEnumWithCases = new(
        id: "CSWINRT2010",
        title: "API contract enum type with enum cases",
        messageFormat: """The type '{0}' is annotated with '[ApiContract]', but it defines one or more enum cases. API contract types are represented by empty struct types in the Windows Runtime type system, and as such defining any enum cases is invalid. The enum cases will be ignored when generating the resulting .winmd file.""",
        category: "WindowsRuntime.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Enum types annotated with '[ApiContract]' must not define any enum cases, as API contract types are represented by empty struct types in the Windows Runtime type system. Any enum cases will be ignored when generating the resulting .winmd file.",
        helpLinkUri: "https://github.com/microsoft/CsWinRT");
}