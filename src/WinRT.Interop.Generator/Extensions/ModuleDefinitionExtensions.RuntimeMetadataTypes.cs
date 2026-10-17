// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;

namespace WindowsRuntime.InteropGenerator;

/// <inheritdoc cref="ModuleDefinitionExtensions"/>
internal partial class ModuleDefinitionExtensions
{
    /// <summary>
    /// Gets the manually projected and custom-mapped public types from the runtime's metadata providers.
    /// </summary>
    /// <param name="module">The implementation runtime module.</param>
    /// <returns>The cached runtime metadata catalog.</returns>
    public static IReadOnlyCollection<TypeSignature> GetRuntimeMetadataTypes(this ModuleDefinition module)
    {
        return RuntimeMetadataTypesCache.Entries.GetValue(module, static module =>
        {
            List<TypeSignature> types = [];

            foreach (TypeDefinition type in module.TopLevelTypes)
            {
                if (!type.IsProjectedWindowsRuntimeType)
                {
                    continue;
                }

                if (type.TryGetCustomAttribute("WindowsRuntime.InteropServices"u8, "WindowsRuntimeMappedTypeAttribute"u8, out CustomAttribute? attribute) &&
                    attribute.Signature is { FixedArguments: [{ Element: TypeSignature mappedType }] })
                {
                    types.Add(mappedType);
                }
                else
                {
                    types.Add(type.ToTypeSignature());
                }
            }

            return [.. types];
        });
    }
}

/// <summary>
/// Caches the fixed metadata catalog of each loaded runtime implementation.
/// </summary>
file static class RuntimeMetadataTypesCache
{
    /// <summary>
    /// The per-module metadata catalog.
    /// </summary>
    public static readonly ConditionalWeakTable<ModuleDefinition, TypeSignature[]> Entries = [];
}
