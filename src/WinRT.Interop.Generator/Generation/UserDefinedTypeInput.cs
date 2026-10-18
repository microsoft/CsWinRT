// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet.Signatures;
using WindowsRuntime.InteropGenerator.Models;

namespace WindowsRuntime.InteropGenerator.Generation;

/// <summary>
/// A canonical user-defined type and its association with a shared vtable group for proxy emission.
/// </summary>
/// <param name="type">The user-defined type in global canonical type order.</param>
/// <param name="vtableTypes">The type's own canonical interface set.</param>
/// <param name="group">The shared group that provides the generated marshaller attribute type.</param>
internal readonly struct UserDefinedTypeInput(TypeSignature type, TypeSignatureEquatableSet vtableTypes, UserDefinedVtableGroup group)
{
    /// <summary>Gets the user-defined type to emit a proxy for.</summary>
    public TypeSignature Type { get; } = type;

    /// <summary>Gets the type's own canonical interface set, which can be reference-distinct from its group's set.</summary>
    public TypeSignatureEquatableSet VtableTypes { get; } = vtableTypes;

    /// <summary>Gets the shared group that provides the generated marshaller attribute type.</summary>
    public UserDefinedVtableGroup Group { get; } = group;
}
