// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.InteropGenerator.Helpers;
using WindowsRuntime.InteropGenerator.Models;

namespace WindowsRuntime.InteropGenerator.Generation;

/// <summary>
/// A group of user-defined types that share a canonical interface set and its generated marshaller.
/// </summary>
/// <param name="representative">The first type in canonical type order, which names the shared generated types.</param>
/// <param name="vtableTypes">The representative's canonical interface set.</param>
/// <param name="comparer">The comparer shared by this invocation's type and interface ordering.</param>
internal sealed class UserDefinedVtableGroup(TypeSignature representative, TypeSignatureEquatableSet vtableTypes, TypeDescriptorComparer comparer)
{
    /// <summary>
    /// Gets the first type in canonical type order with this interface set.
    /// </summary>
    public TypeSignature Representative { get; } = representative;

    /// <summary>
    /// Gets the representative's canonical interface set, which can be reference-distinct from other members' equal sets.
    /// </summary>
    public TypeSignatureEquatableSet VtableTypes { get; } = vtableTypes;

    /// <summary>
    /// Gets the interface order prepared once for this group.
    /// </summary>
    public OrderedVtableTypes OrderedTypes { get; } = new(vtableTypes, comparer);

    /// <summary>
    /// Gets or sets the shared marshaller attribute type, populated during group emission before emitting proxies.
    /// </summary>
    public TypeDefinition? MarshallerAttributeType { get; set; }
}
