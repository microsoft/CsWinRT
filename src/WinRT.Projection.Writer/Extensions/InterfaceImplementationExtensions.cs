// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;

namespace WindowsRuntime.ProjectionWriter.Extensions;

/// <summary>
/// Extension methods for <see cref="InterfaceImplementation"/>.
/// </summary>
internal static class InterfaceImplementationExtensions
{
    /// <summary>
    /// Returns whether the implemented interface is the runtime class's <c>[Default]</c> interface
    /// (i.e. the one whose vtable backs the class's <c>IInspectable</c> identity).
    /// </summary>
    /// <param name="impl">The interface implementation to inspect.</param>
    /// <returns><see langword="true"/> if the interface is the default interface; otherwise <see langword="false"/>.</returns>
    public static bool IsDefaultInterface(this InterfaceImplementation impl)
        => impl.HasAttribute("Windows.Foundation.Metadata", "DefaultAttribute");

    /// <summary>
    /// Returns whether the implemented interface is marked <c>[Overridable]</c> (i.e. derived
    /// classes are allowed to override its members).
    /// </summary>
    /// <param name="impl">The interface implementation to inspect.</param>
    /// <returns><see langword="true"/> if the interface is overridable; otherwise <see langword="false"/>.</returns>
    public static bool IsOverridable(this InterfaceImplementation impl)
        => impl.HasAttribute("Windows.Foundation.Metadata", "OverridableAttribute");
}