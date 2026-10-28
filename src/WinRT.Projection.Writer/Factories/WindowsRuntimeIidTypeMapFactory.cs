// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.ProjectionWriter.Errors;
using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Helpers;
using WindowsRuntime.ProjectionWriter.Metadata;
using WindowsRuntime.ProjectionWriter.Models;
using WindowsRuntime.ProjectionWriter.Resolvers;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter.Factories;

/// <summary>
/// Emits conditional IID registrations for the types an implementation projection actually defines.
/// </summary>
internal static class WindowsRuntimeIidTypeMapFactory
{
    /// <summary>
    /// Writes the projection's fixed IID map from its completed metadata catalog.
    /// </summary>
    /// <param name="context">The context for this implementation projection.</param>
    /// <param name="metadataEntries">The emitted types and their source metadata.</param>
    /// <param name="token">The cancellation token for this invocation.</param>
    public static void Write(
        ProjectionEmitContext context,
        IReadOnlyList<KeyValuePair<string, WindowsRuntimeMetadataTypeEntry>> metadataEntries,
        CancellationToken token)
    {
        Dictionary<string, (Guid Iid, string TrimTarget)> entries = new(StringComparer.Ordinal);

        // Collect fixed identities from the emitted catalog (constructed types belong to the interop generator)
        foreach ((string projectedTypeName, WindowsRuntimeMetadataTypeEntry metadata) in metadataEntries)
        {
            token.ThrowIfCancellationRequested();
            TypeDefinition type = metadata.Type;

            if (type.IsGeneric ||
                type.IsAttributeType ||
                type.IsStatic ||
                type.IsApiContractType)
            {
                continue;
            }

            TypeKind kind = TypeKindResolver.Resolve(type);
            bool isAbiType = projectedTypeName.StartsWith("global::ABI.", StringComparison.Ordinal);

            // ABI structs and class proxies are not projections, but exclusive interfaces have their own IID
            if (isAbiType && kind != TypeKind.Interface)
            {
                continue;
            }

            (string ns, string name) = type.Names();
            MappedType? mapped = MappedTypes.Get(ns, name);

            // Skip mapped types whose ABI is supplied outside this projection
            if (mapped is { EmitAbi: false })
            {
                continue;
            }

            string assemblyName = context.Settings.OutputAssemblyName;

            // Authored types stay in their component, while generated 'ABI.Impl' interfaces live here
            if (context.Settings.Component && !isAbiType)
            {
                assemblyName = metadata.SourceStem;

                if (context.Settings.ComponentAssemblyNames.Count > 0 &&
                    !context.Settings.ComponentAssemblyNames.Contains(assemblyName))
                {
                    throw WellKnownProjectionWriterExceptions.UnreachableEmissionState(
                        $"The authored type '{type.FullName}' belongs to an unregistered component '{assemblyName}'.");
                }
            }

            // Escape literal assembly names that could collide with the reserved '#System' scope
            if (assemblyName.StartsWith('#'))
            {
                assemblyName = "#" + assemblyName;
            }

            // Lookup keys use raw metadata names, not C#-escaped identifiers
            string typeName = type.FullName;

            if (mapped is MappedType mapping)
            {
                typeName = string.IsNullOrEmpty(mapping.MappedNamespace)
                    ? mapping.MappedName
                    : $"{mapping.MappedNamespace}.{mapping.MappedName}";
            }

            if (context.Settings.Component && isAbiType)
            {
                typeName = "ABI.Impl." + typeName;
            }

            string key = $"[{assemblyName}]{typeName}";
            bool isValueType = kind is TypeKind.Enum or TypeKind.Struct;
            Guid iid;

            if (isValueType)
            {
                iid = IidExpressionGenerator.GetReferenceIid(context, type);
            }
            else if (!TryGetInterfaceIid(context, type, kind, out iid))
            {
                continue;
            }

            // Nullable keys represent 'IReference<T>', while the raw type remains the trim dependency
            if (isValueType)
            {
                key = $"[#System]System.Nullable`1[{key}]";
            }

            if (!entries.TryAdd(key, (iid, projectedTypeName)) &&
                entries[key] != (iid, projectedTypeName))
            {
                throw WellKnownProjectionWriterExceptions.ConflictingIidTypeNames(key);
            }
        }

        if (entries.Count == 0)
        {
            return;
        }

        using IndentedTextWriterOwner owner = IndentedTextWriterPool.GetOrCreate();
        IndentedTextWriter writer = owner.Writer;

        MetadataAttributeFactory.WriteFileHeader(writer);
        writer.WriteLine(isMultiline: true, """

            using System.Runtime.InteropServices;
            using WindowsRuntime.InteropServices;

            #pragma warning disable IL2026
            """);

        Dictionary<Guid, int> providers = [];

        // Stable key ordering also makes shared provider numbering deterministic
        foreach ((string key, (Guid iid, string trimTarget)) in entries.OrderBy(static entry => entry.Key, StringComparer.Ordinal))
        {
            token.ThrowIfCancellationRequested();

            if (!providers.TryGetValue(iid, out int provider))
            {
                provider = providers.Count;
                providers.Add(iid, provider);
            }

            string escapedKey = key.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
            writer.WriteLine();
            writer.WriteLine(isMultiline: true, $$"""
                [assembly: TypeMap<WindowsRuntimeIidTypeMapGroup>(
                    value: "{{escapedKey}}",
                    target: typeof(global::ABI.IidProvider{{provider}}),
                    trimTarget: typeof({{trimTarget}}))]
                """);
        }

        writer.WriteLine();
        writer.WriteLine("namespace ABI;");

        // GUID-only providers avoid rooting marshalling code or initializing vtables for an IID lookup
        foreach ((Guid iid, int provider) in providers)
        {
            token.ThrowIfCancellationRequested();
            writer.WriteLine();
            writer.WriteLine($"""[Guid("{iid.ToString("D").ToUpperInvariant()}")]""");
            writer.WriteLine($"file static class IidProvider{provider};");
        }

        writer.FlushToFile(Path.Combine(context.Settings.OutputFolder, "WindowsRuntimeIidTypeMap.cs"));
    }

    /// <summary>
    /// Resolves a type's own interface IID or its runtime class's default interface.
    /// </summary>
    /// <param name="context">The metadata-resolution context.</param>
    /// <param name="type">The projected type.</param>
    /// <param name="kind">The projected type's kind.</param>
    /// <param name="iid">The native interface identity, when available.</param>
    /// <returns>Whether the type represents a native interface identity.</returns>
    private static bool TryGetInterfaceIid(ProjectionEmitContext context, TypeDefinition type, TypeKind kind, out Guid iid)
    {
        ITypeDefOrRef interfaceType = type;

        if (kind == TypeKind.Class)
        {
            if (type.GetDefaultInterface() is not { } defaultInterface)
            {
                // A class without a default interface has no IID, but can still contribute other projection metadata
                iid = default;

                return false;
            }

            interfaceType = defaultInterface;
        }

        if (interfaceType is TypeSpecification { Signature: GenericInstanceTypeSignature signature })
        {
            iid = GuidGenerator.Generate(SignatureGenerator.GetSignature(context, TypeSemanticsFactory.Get(signature)));

            return true;
        }

        (string ns, string name) = interfaceType.Names();
        TypeDefinition? definition = interfaceType.TryResolve(context.Cache.RuntimeContext) ?? context.Cache.Find(ns, name);

        if (definition is null || !definition.TryGetWindowsRuntimeGuid(out iid))
        {
            throw WellKnownProjectionWriterExceptions.MissingGuidAttribute(interfaceType.FullName);
        }

        return true;
    }
}
