// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using AsmResolver;
using AsmResolver.DotNet;
using WindowsRuntime.Generator.References;

namespace WindowsRuntime.Generator.Helpers;

/// <summary>
/// Reads the implementable bases a reference projection built with <c>CsWinRTImplementWinMDTypes</c> declares, from
/// its <see cref="WindowsRuntimeReferenceAssemblyMetadata"/> entries.
/// </summary>
/// <remarks>
/// The bases themselves carry no marker: the reference assembly records the Windows Runtime classes they stand for,
/// and each base has a well-known name derived from its class (see <see cref="WindowsRuntimeReferenceAssemblyMetadata"/>).
/// </remarks>
internal static class ImplementableClassMetadata
{
    /// <summary>
    /// The implementable bases declared by each module, read once per module.
    /// </summary>
    private static readonly ConditionalWeakTable<ModuleDefinition, ModuleInfo> ModuleInfos = [];

    /// <summary>
    /// Tries to get the Windows Runtime class that a generated instance base stands for.
    /// </summary>
    /// <param name="type">The type to inspect.</param>
    /// <param name="runtimeClassName">The fully qualified Windows Runtime class name, if <paramref name="type"/> is an instance base.</param>
    /// <returns>Whether <paramref name="type"/> is a generated instance base.</returns>
    /// <remarks>
    /// Activation factory bases are deliberately not matched: an activation factory is not an instance of the class
    /// it activates, so it must not take on that class's identity.
    /// </remarks>
    public static bool TryGetImplementableClassName(TypeDefinition type, [NotNullWhen(true)] out string? runtimeClassName)
    {
        runtimeClassName = null;

        return IsBaseShape(type) &&
            type.DeclaringModule is ModuleDefinition module &&
            GetModuleInfo(module).InstanceBases.TryGetValue(type.FullName, out runtimeClassName);
    }

    /// <summary>
    /// Checks whether a type is a generated base, either an instance base or an activation factory base.
    /// </summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns>Whether <paramref name="type"/> is a generated base.</returns>
    public static bool IsImplementableBase(TypeDefinition type)
    {
        if (!IsBaseShape(type) || type.DeclaringModule is not ModuleDefinition module)
        {
            return false;
        }

        ModuleInfo info = GetModuleInfo(module);

        return info.InstanceBases.ContainsKey(type.FullName) || info.FactoryBases.Contains(type.FullName);
    }

    /// <summary>
    /// Gets the implementable bases declared by a module.
    /// </summary>
    private static ModuleInfo GetModuleInfo(ModuleDefinition module)
    {
        return ModuleInfos.GetValue(module, static module =>
        {
            Dictionary<string, string> instanceBases = new(StringComparer.Ordinal);
            HashSet<string> factoryBases = new(StringComparer.Ordinal);

            foreach (CustomAttribute attribute in module.Assembly?.CustomAttributes ?? [])
            {
                if (attribute.Constructor?.DeclaringType?.FullName != WindowsRuntimeReferenceAssemblyMetadata.AttributeTypeName ||
                    attribute.Signature is not { FixedArguments: [{ Element: var keyElement }, { Element: var valueElement }] } ||
                    GetString(keyElement) is not string key ||
                    GetString(valueElement) is not string value)
                {
                    continue;
                }

                // A class may only have one of the two bases, but checking the type asked about is enough: a name
                // that is not a base names no type at all, since the bases are all the reference projection declares
                // in the 'ABI' namespace.
                if (key == WindowsRuntimeReferenceAssemblyMetadata.ImplementableClass)
                {
                    instanceBases[WindowsRuntimeReferenceAssemblyMetadata.GetImplementableClassBaseTypeName(value)] = value;
                    _ = factoryBases.Add(WindowsRuntimeReferenceAssemblyMetadata.GetImplementableClassFactoryBaseTypeName(value));
                }
            }

            return new ModuleInfo(instanceBases.ToFrozenDictionary(StringComparer.Ordinal), factoryBases.ToFrozenSet(StringComparer.Ordinal));
        });
    }

    /// <summary>
    /// Checks whether a type has the shape of a generated base (an abstract, non-static class).
    /// </summary>
    private static bool IsBaseShape(TypeDefinition type)
    {
        return type is { IsClass: true, IsAbstract: true, IsSealed: false };
    }

    /// <summary>
    /// Reads a string custom-attribute argument without converting values of other types.
    /// </summary>
    private static string? GetString(object? value)
    {
        return value switch
        {
            Utf8String text => text.Value,
            string text => text,
            _ => null
        };
    }

    /// <summary>
    /// The implementable bases declared by a module.
    /// </summary>
    /// <param name="InstanceBases">The full names of the instance bases, mapped to the Windows Runtime class each stands for.</param>
    /// <param name="FactoryBases">The full names of the activation factory bases.</param>
    private sealed record ModuleInfo(FrozenDictionary<string, string> InstanceBases, FrozenSet<string> FactoryBases);
}
