// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
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
    private sealed class ResolvedTypeReferenceImporter : ReferenceImporter
    {
        /// <summary>The imported assembly identities, avoiding enumeration of a concurrently growing list.</summary>
        private readonly ConcurrentDictionary<AssemblyDescriptor, Lazy<AssemblyReference>> _assemblies = new(new SignatureComparer());

        /// <summary>Creates the importer before parallel emission starts.</summary>
        /// <param name="module">The output module.</param>
        public ResolvedTypeReferenceImporter(ModuleDefinition module)
            : base(module)
        {
            foreach (AssemblyReference reference in module.AssemblyReferences)
            {
                _ = _assemblies.TryAdd(reference, new(reference));
            }
        }

        /// <inheritdoc/>
        protected override AssemblyReference ImportAssembly(AssemblyDescriptor assembly)
        {
            ArgumentNullException.ThrowIfNull(assembly);

            // Preserve references already owned by this module, including ones added after importer creation
            if (assembly is AssemblyReference reference && reference.IsImportedInModule(TargetModule))
            {
                _ = _assemblies.TryAdd(reference, new(reference));

                return reference;
            }

            // A dictionary factory can run more than once, but only the published lazy may attach a reference
            return _assemblies.GetOrAdd(
                key: assembly,
                valueFactory: static (descriptor, target) => new(() =>
                {
                    AssemblyReference imported = new(descriptor);

                    target.AssemblyReferences.Add(imported);

                    return imported;
                }),
                factoryArgument: TargetModule).Value;
        }

        /// <inheritdoc/>
        protected override ITypeDefOrRef ImportType(TypeReference type)
        {
            return type.TryResolve(TargetModule.RuntimeContext, out TypeDefinition? definition)
                ? base.ImportType(definition)
                : base.ImportType(type);
        }
    }
}
