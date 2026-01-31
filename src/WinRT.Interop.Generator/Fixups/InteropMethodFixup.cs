// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;

namespace WindowsRuntime.InteropGenerator.Fixups;

/// <summary>
/// A type that can apply custom fixups to generated methods, as a last processing step.
/// </summary>
internal abstract class InteropMethodFixup
{
    /// <summary>
    /// Applies the current fixup to a target method.
    /// </summary>
    /// <param name="method">The target method to apply the fixup to.</param>
    public abstract void Apply(MethodDefinition method);
}
