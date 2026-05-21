// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.ProjectionWriter.Metadata;
using static WindowsRuntime.ProjectionWriter.References.WellKnownAttributeNames;
using static WindowsRuntime.ProjectionWriter.References.WellKnownNamespaces;

namespace WindowsRuntime.ProjectionWriter.Helpers;

/// <summary>
/// Information about an <c>[Activatable]</c>/<c>[Static]</c>/<c>[Composable]</c> factory interface.
/// </summary>
/// <param name="Type">The factory-interface type definition (or <see langword="null"/> when the attribute did not carry one).</param>
/// <param name="Activatable">Whether the carrying type has an <c>[Activatable]</c> attribute pointing at <paramref name="Type"/>.</param>
/// <param name="Statics">Whether the carrying type has a <c>[Static]</c> attribute pointing at <paramref name="Type"/>.</param>
/// <param name="Composable">Whether the carrying type has a <c>[Composable]</c> attribute pointing at <paramref name="Type"/>.</param>
/// <param name="Visible">For composable attributes, whether the visibility argument is the public (<c>2</c>) value.</param>
internal sealed record AttributedType(
    TypeDefinition? Type = null,
    bool Activatable = false,
    bool Statics = false,
    bool Composable = false,
    bool Visible = false);

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

            if (attrType is null)
            {
                continue;
            }

            (string ns, string name) = attrType.Names();

            if (ns != WindowsFoundationMetadata)
            {
                continue;
            }

            AttributedType info;
            switch (name)
            {
                case ActivatableAttribute:
                    info = new AttributedType(Type: GetSystemType(attr, cache), Activatable: true);
                    break;
                case StaticAttribute:
                    info = new AttributedType(Type: GetSystemType(attr, cache), Statics: true);
                    break;
                case ComposableAttribute:
                    info = new AttributedType(Type: GetSystemType(attr, cache), Composable: true, Visible: GetVisibility(attr) == 2);
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
        if (attr.Signature is null)
        {
            return null;
        }

        for (int i = 0; i < attr.Signature.FixedArguments.Count; i++)
        {
            CustomAttributeArgument arg = attr.Signature.FixedArguments[i];
            // For System.Type args in WinMD, the value is typically a TypeSignature
            if (arg.Element is TypeSignature sig)
            {
                string typeName = sig.FullName ?? string.Empty;
                TypeDefinition? td = cache.Find(typeName);

                if (td is not null)
                {
                    return td;
                }
            }
            else if (arg.Element is string s)
            {
                TypeDefinition? td = cache.Find(s);

                if (td is not null)
                {
                    return td;
                }
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