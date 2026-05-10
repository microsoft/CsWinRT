// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.ProjectionWriter.Metadata;
using static WindowsRuntime.ProjectionWriter.References.WellKnownNamespaces;
using static WindowsRuntime.ProjectionWriter.References.WellKnownAttributeNames;

namespace WindowsRuntime.ProjectionWriter.Helpers;

/// <summary>
/// Information about an [Activatable]/[Static]/[Composable] factory interface.
/// </summary>
internal sealed class AttributedType
{
    public TypeDefinition? Type { get; set; }
    public bool Activatable { get; set; }
    public bool Statics { get; set; }
    public bool Composable { get; set; }
    public bool Visible { get; set; }
}

/// <summary>
/// Helpers for activator/static/composable factory enumeration.
/// </summary>
internal static class AttributedTypes
{
    /// <summary>
    /// Returns the (interface_name, AttributedType) entries for the given runtime class type.
    /// </summary>
    public static IEnumerable<KeyValuePair<string, AttributedType>> Get(TypeDefinition type, MetadataCache cache)
    {
        Dictionary<string, AttributedType> result = [];

        for (int i = 0; i < type.CustomAttributes.Count; i++)
        {
            CustomAttribute attr = type.CustomAttributes[i];
            ITypeDefOrRef? attrType = attr.Constructor?.DeclaringType;
            if (attrType is null) { continue; }
            (string ns, string name) = attrType.Names();
            if (ns != WindowsFoundationMetadata) { continue; }

            AttributedType info = new();
            switch (name)
            {
                case ActivatableAttribute:
                    info.Type = GetSystemType(attr, cache);
                    info.Activatable = true;
                    break;
                case StaticAttribute:
                    info.Type = GetSystemType(attr, cache);
                    info.Statics = true;
                    break;
                case ComposableAttribute:
                    info.Type = GetSystemType(attr, cache);
                    info.Composable = true;
                    info.Visible = GetVisibility(attr) == 2;
                    break;
                default:
                    continue;
            }

            string key = info.Type?.Name?.Value ?? string.Empty;
            result[key] = info;
        }

        // Sort by key (the factory-interface type name, e.g. 'IButtonUtilsStatic') so the
        // inheritance order in the generated code is alphabetical by interface name.
        SortedDictionary<string, AttributedType> sorted = [];
        foreach (KeyValuePair<string, AttributedType> kv in result)
        {
            sorted[kv.Key] = kv.Value;
        }
        return sorted;
    }

    /// <summary>
    /// Extracts the System.Type argument from the attribute's fixed args.
    /// </summary>
    private static TypeDefinition? GetSystemType(CustomAttribute attr, MetadataCache cache)
    {
        if (attr.Signature is null) { return null; }
        for (int i = 0; i < attr.Signature.FixedArguments.Count; i++)
        {
            CustomAttributeArgument arg = attr.Signature.FixedArguments[i];
            // For System.Type args in WinMD, the value is typically a TypeSignature
            if (arg.Element is TypeSignature sig)
            {
                string typeName = sig.FullName ?? string.Empty;
                TypeDefinition? td = cache.Find(typeName);
                if (td is not null) { return td; }
            }
            else if (arg.Element is string s)
            {
                TypeDefinition? td = cache.Find(s);
                if (td is not null) { return td; }
            }
        }
        return null;
    }

    /// <summary>
    /// Extracts the visibility int from a [ComposableAttribute] (the enum value arg).
    /// </summary>
    private static int GetVisibility(CustomAttribute attr)
    {
        if (attr.Signature is { FixedArguments: [_, { Element: int e }, ..] })
        {
            return e;
        }
        return 0;
    }
}