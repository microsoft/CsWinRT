// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using WindowsRuntime.InteropGenerator.References;

namespace WindowsRuntime.InteropGenerator.Models;

/// <summary>
/// An interop output module that imports resolved type references consistently.
/// </summary>
/// <param name="runtime">The target runtime.</param>
internal sealed class InteropModuleDefinition(DotNetRuntimeInfo runtime)
    : ModuleDefinition(InteropNames.WindowsRuntimeInteropDllNameUtf8, runtime.GetDefaultCorLib())
{
    /// <inheritdoc/>
    protected override ReferenceImporter GetDefaultImporter()
    {
        return new ResolvedTypeReferenceImporter(this);
    }

    /// <summary>
    /// Uses the same resolved references for IL operands, signatures, and type-valued attributes.
    /// </summary>
    /// <param name="module">The output module.</param>
    private sealed class ResolvedTypeReferenceImporter(ModuleDefinition module) : ReferenceImporter(module)
    {
        /// <inheritdoc/>
        protected override ITypeDefOrRef ImportType(TypeReference type)
        {
            return type.TryResolve(TargetModule.RuntimeContext, out TypeDefinition? definition)
                ? base.ImportType(definition)
                : base.ImportType(type);
        }
    }
}
