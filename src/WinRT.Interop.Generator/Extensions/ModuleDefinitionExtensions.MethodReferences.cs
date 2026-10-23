// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Serialized;
using AsmResolver.PE.DotNet.Metadata;
using AsmResolver.PE.DotNet.Metadata.Tables;

namespace WindowsRuntime.InteropGenerator;

/// <inheritdoc cref="ModuleDefinitionExtensions"/>
internal partial class ModuleDefinitionExtensions
{
    /// <summary>
    /// Checks metadata references for a method on a non-generic type without decoding method bodies.
    /// </summary>
    /// <param name="module">The input module.</param>
    /// <param name="typeNamespace">The namespace of the declaring type.</param>
    /// <param name="typeName">The name of the declaring type.</param>
    /// <param name="methodName">The name of the method.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>Whether the module may reference the specified method.</returns>
    public static bool ReferencesMethod(
        this ModuleDefinition module,
        Utf8String typeNamespace,
        Utf8String typeName,
        Utf8String methodName,
        CancellationToken token)
    {
        // In-memory modules have no complete serialized reference tables to prove absence
        if (module is not SerializedModuleDefinition serialized)
        {
            return true;
        }

        TablesStream tables = serialized.DotNetDirectory.Metadata!.GetStream<TablesStream>()!;
        StringsStream strings = serialized.DotNetDirectory.Metadata.GetStream<StringsStream>()!;
        IndexEncoder parents = tables.GetIndexEncoder(CodedIndex.MemberRefParent);
        HashSet<uint>? typeReferences = null;

        // Ignore assembly spelling so forwarding facades cannot hide a compiled reference
        foreach ((int index, TypeReferenceRow row) in tables.GetTable<TypeReferenceRow>().Index())
        {
            token.ThrowIfCancellationRequested();

            // Match names in the 'string' heap, then cache encoded 'MemberRefParent' values
            // to identify matching method owners without materializing or resolving types
            if (strings.GetStringByIndex(row.Namespace) == typeNamespace &&
                strings.GetStringByIndex(row.Name) == typeName)
            {
                typeReferences ??= [];

                _ = typeReferences.Add(parents.EncodeToken(new MetadataToken(TableIndex.TypeRef, (uint)(index + 1))));
            }
        }

        // Without a matching type reference, none of its methods can be referenced
        if (typeReferences is null)
        {
            return false;
        }

        // Definitions do not count as usage (external callers have a 'MemberRef', even in generic helpers)
        foreach (MemberReferenceRow row in tables.GetTable<MemberReferenceRow>())
        {
            token.ThrowIfCancellationRequested();

            if (typeReferences.Contains(row.Parent) &&
                strings.GetStringByIndex(row.Name) == methodName)
            {
                return true;
            }
        }

        return false;
    }
}
