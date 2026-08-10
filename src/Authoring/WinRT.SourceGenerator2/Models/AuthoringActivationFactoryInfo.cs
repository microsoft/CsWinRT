// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.SourceGenerator.Models;

/// <summary>
/// Describes an activation factory for an authored runtime class: either one the author declared via
/// <c>[WindowsRuntimeActivationFactory]</c>, or one generated on their behalf.
/// </summary>
/// <param name="RuntimeClassName">The Windows Runtime class name the factory activates (e.g. <c>Ns.Foo</c>).</param>
/// <param name="FactoryTypeName">The fully qualified name of the factory type.</param>
/// <param name="FactoryBaseTypeName">The fully qualified name of the generated factory base class it extends.</param>
/// <param name="GeneratedForImplementationTypeName">
/// The fully qualified name of the implementation type to activate, when CsWinRT is supplying the factory itself;
/// <see langword="null"/> when the author declared the factory.
/// </param>
internal record AuthoringActivationFactoryInfo(
    string RuntimeClassName,
    string FactoryTypeName,
    string FactoryBaseTypeName,
    string? GeneratedForImplementationTypeName = null);
