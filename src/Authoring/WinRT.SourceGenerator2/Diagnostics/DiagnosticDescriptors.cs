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
        messageFormat: """The 'ICustomPropertyProvider' interface is not available in the compilation, but it is required to use '[GeneratedCustomPropertyProvider]' (make sure to either reference 'WindowsAppSDK.WinUI' or set the 'UseUwp' property in your .csproj file)""",
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
}