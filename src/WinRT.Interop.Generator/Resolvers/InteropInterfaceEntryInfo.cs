// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using WindowsRuntime.InteropGenerator.References;

namespace WindowsRuntime.InteropGenerator.Resolvers;

/// <summary>
/// A base type to abstract inserting interface entries information into a static constructor.
/// </summary>
internal abstract class InteropInterfaceEntryInfo
{
    /// <summary>
    /// Loads the IID for the interface onto the evaluation stack.
    /// </summary>
    /// <param name="instructions">The target <see cref="CilInstructionCollection"/>.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The <see cref="ModuleDefinition"/> in use.</param>
    public abstract void LoadIID(CilInstructionCollection instructions, InteropReferences interopReferences, ModuleDefinition module);

    /// <summary>
    /// Loads the vtable for the interface onto the evaluation stack.
    /// </summary>
    /// <param name="instructions">The target <see cref="CilInstructionCollection"/>.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The <see cref="ModuleDefinition"/> in use.</param>
    public abstract void LoadVtable(CilInstructionCollection instructions, InteropReferences interopReferences, ModuleDefinition module);
}
