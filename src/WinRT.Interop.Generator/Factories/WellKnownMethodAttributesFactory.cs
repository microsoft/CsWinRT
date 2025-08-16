// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.PE.DotNet.Metadata.Tables;

namespace WindowsRuntime.InteropGenerator.Factories;

/// <summary>
/// A factory for well known method attributes.
/// </summary>
internal static class WellKnownMethodAttributesFactory
{
    /// <summary>
    /// The method attributes for an explicit interface implementation of an instance method.
    /// </summary>
    public const MethodAttributes ExplicitInterfaceImplementationInstanceMethod =
        MethodAttributes.Private |
        MethodAttributes.Final |
        MethodAttributes.HideBySig |
        MethodAttributes.Virtual;

    /// <summary>
    /// The method attributes for an explicit interface implementation of an instance accessor method.
    /// </summary>
    public const MethodAttributes ExplicitInterfaceImplementationInstanceAccessorMethod =
        MethodAttributes.Private |
        MethodAttributes.Final |
        MethodAttributes.HideBySig |
        MethodAttributes.SpecialName |
        MethodAttributes.Virtual;
}
