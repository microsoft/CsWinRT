// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.SourceGenerator.Models;

/// <summary>
/// Describes a user-authored activation factory discovered via <c>[WindowsRuntimeActivationFactory]</c>.
/// </summary>
/// <param name="RuntimeClassName">The Windows Runtime class name the factory activates (e.g. <c>Ns.Foo</c>).</param>
/// <param name="FactoryTypeName">The fully qualified name of the authored factory type.</param>
internal record AuthoringActivationFactoryInfo(
    string RuntimeClassName,
    string FactoryTypeName);
