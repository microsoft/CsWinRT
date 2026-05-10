// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;

namespace WindowsRuntime.ProjectionWriter.Extensions;

/// <summary>
/// Extension methods for <see cref="InterfaceImplementation"/>.
/// </summary>
internal static class InterfaceImplementationExtensions
{
    extension(InterfaceImplementation impl)
    {
        /// <summary>
        /// Returns whether the implemented interface is the runtime class's <c>[Default]</c> interface
        /// (i.e. the one whose vtable backs the class's <c>IInspectable</c> identity).
        /// </summary>
        /// <returns><see langword="true"/> if the interface is the default interface; otherwise <see langword="false"/>.</returns>
        public bool IsDefaultInterface()
            => impl.HasAttribute("Windows.Foundation.Metadata", "DefaultAttribute");

        /// <summary>
        /// Returns whether the implemented interface is marked <c>[Overridable]</c> (i.e. derived
        /// classes are allowed to override its members).
        /// </summary>
        /// <returns><see langword="true"/> if the interface is overridable; otherwise <see langword="false"/>.</returns>
        public bool IsOverridable()
            => impl.HasAttribute("Windows.Foundation.Metadata", "OverridableAttribute");
    }
}