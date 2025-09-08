// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Marker types to control the activation of Windows Runtime types deriving from <see cref="WindowsRuntimeObject"/>.
/// </summary>
/// <remarks>
/// Because all activation logic is centralized in <see cref="WindowsRuntimeObject"/> (for performance, maintainability,
/// and binary size reasons), all derived runtime class types must forward activation parameters to the right base
/// constructor. Activation works differently for composed and sealed types, so these marker types are used to select
/// the correct constructor overload to invoke.
/// </remarks>
public static class WindowsRuntimeActivationTypes
{
    /// <summary>
    /// The derived type is a composed Windows Runtime type.
    /// </summary>
    public readonly ref struct DerivedComposed;

    /// <summary>
    /// The derived type is a sealed Windows Runtime type.
    /// </summary>
    public readonly ref struct DerivedSealed;
}
