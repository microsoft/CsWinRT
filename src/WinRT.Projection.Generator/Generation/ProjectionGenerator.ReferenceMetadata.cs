// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using AsmResolver;
using AsmResolver.DotNet;
using WindowsRuntime.Generator.References;
using WindowsRuntime.ProjectionGenerator.Errors;

namespace WindowsRuntime.ProjectionGenerator.Generation;

/// <inheritdoc cref="ProjectionGenerator"/>
internal partial class ProjectionGenerator
{
    /// <summary>
    /// Reads the producer's effective exclusive-to IDIC selection. Missing metadata never implies opt-in.
    /// </summary>
    private static HashSet<string> ReadIdicExclusiveToTypes(ModuleDefinition module, string referencePath)
    {
        HashSet<string> types = new(StringComparer.Ordinal);
        foreach (CustomAttribute attribute in module.Assembly!.CustomAttributes)
        {
            if (attribute.Constructor?.DeclaringType?.FullName != WindowsRuntimeReferenceAssemblyMetadata.AttributeTypeName)
            {
                continue;
            }

            if (attribute.Signature is not { FixedArguments.Count: 2, NamedArguments.Count: 0 } signature ||
                signature.FixedArguments[0].ArgumentType.FullName != "System.String" ||
                signature.FixedArguments[1].ArgumentType.FullName != "System.String" ||
                GetString(signature.FixedArguments[0].Element) is not string key)
            {
                throw WellKnownProjectionGeneratorExceptions.InvalidReferenceAssemblyMetadata(referencePath);
            }

            if (key != WindowsRuntimeReferenceAssemblyMetadata.IdicExclusiveTo)
            {
                continue;
            }

            string? typeName = GetString(signature.FixedArguments[1].Element);

            if (string.IsNullOrWhiteSpace(typeName) || typeName != typeName.Trim())
            {
                throw WellKnownProjectionGeneratorExceptions.InvalidReferenceAssemblyMetadata(referencePath);
            }

            _ = types.Add(typeName);
        }

        return types;
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
    /// Rejects duplicate projected type identities instead of combining incompatible package requirements.
    /// </summary>
    private static void RegisterProjectedTypes(
        ModuleDefinition module,
        HashSet<string> idicExclusiveToTypes,
        string referencePath,
        Dictionary<string, string> projectedTypeOwners)
    {
        // Compiler-generated internal helpers are not projected APIs. Selected internal exclusive
        // interfaces are represented by their metadata names even if the compiler stripped them.
        IEnumerable<string> types = module.TopLevelTypes
            .Where(static type => type.IsPublic)
            .Select(static type => type.FullName)
            .Concat(idicExclusiveToTypes)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal);

        foreach (string typeName in types)
        {
            if (!projectedTypeOwners.TryAdd(typeName, referencePath))
            {
                throw WellKnownProjectionGeneratorExceptions.DuplicateProjectedType(
                    typeName, projectedTypeOwners[typeName], referencePath);
            }
        }
    }
}
