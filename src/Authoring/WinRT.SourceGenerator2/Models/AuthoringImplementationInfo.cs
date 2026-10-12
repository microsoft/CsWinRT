// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.SourceGenerator.Models;

/// <summary>
/// Describes a type in the current compilation implementing a Windows Runtime class declared in existing metadata,
/// by deriving from the generated instance base for that class.
/// </summary>
/// <param name="RuntimeClassName">The Windows Runtime class name the type implements (e.g. <c>Ns.Foo</c>).</param>
/// <param name="ImplementationTypeName">The fully qualified name of the implementation type.</param>
/// <param name="ImplementableBaseTypeName">The fully qualified name of the generated instance base it extends.</param>
/// <param name="FactoryBaseTypeName">
/// The fully qualified name of the generated activation factory base, if CsWinRT can supply the factory itself: the
/// class is only activated without arguments, and the type and a parameterless constructor are accessible. Otherwise
/// <see langword="null"/>, and the author has to declare the factory.
/// </param>
internal sealed record AuthoringImplementationInfo(
    string RuntimeClassName,
    string ImplementationTypeName,
    string ImplementableBaseTypeName,
    string? FactoryBaseTypeName);
