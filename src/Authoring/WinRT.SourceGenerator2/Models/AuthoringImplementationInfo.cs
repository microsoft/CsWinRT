// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.SourceGenerator.Models;

/// <summary>
/// Describes a type in the current compilation that implements a Windows Runtime class declared in existing metadata (by
/// deriving from the generated instance base for that class), and that CsWinRT can generate the activation factory for.
/// </summary>
/// <param name="RuntimeClassName">The Windows Runtime class name the type implements (e.g. <c>Ns.Foo</c>).</param>
/// <param name="ImplementationTypeName">The fully qualified name of the implementation type.</param>
/// <param name="ImplementableBaseTypeName">The fully qualified name of the generated instance base it extends.</param>
/// <param name="FactoryBaseTypeName">The fully qualified name of the generated activation factory base.</param>
internal sealed record AuthoringImplementationInfo(
    string RuntimeClassName,
    string ImplementationTypeName,
    string ImplementableBaseTypeName,
    string FactoryBaseTypeName);
