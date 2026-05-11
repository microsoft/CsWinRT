// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.Versioning;
using WindowsRuntime;

#pragma warning disable IDE0060

namespace Windows.Foundation.Metadata;

/// <summary>
/// Indicates the version of the API contract.
/// </summary>
[WindowsRuntimeMetadata("Windows.Foundation.FoundationContract")]
[AttributeUsage(
    AttributeTargets.Delegate |
    AttributeTargets.Enum |
    AttributeTargets.Event |
    AttributeTargets.Field |
    AttributeTargets.Interface |
    AttributeTargets.Method |
    AttributeTargets.Property |
    AttributeTargets.Class |
    AttributeTargets.Struct, AllowMultiple = true)]
[SupportedOSPlatform("Windows10.0.10240.0")]
[ContractVersion(typeof(FoundationContract), 65536u)]
public sealed class ContractVersionAttribute : Attribute
{
    /// <summary>
    /// Creates a new <see cref="ContractVersionAttribute"/> instance with the specified parameters.
    /// </summary>
    /// <param name="version">The version of the API contract.</param>
    /// <remarks>
    /// This constructor applies to a type with the <see cref="ApiContractAttribute"/> and specifies the contract version of that API contract.
    /// </remarks>
    public ContractVersionAttribute(uint version)
    {
    }

    /// <summary>
    /// Creates a new <see cref="ContractVersionAttribute"/> instance with the specified parameters.
    /// </summary>
    /// <param name="contract">The type to associate with the API contract.</param>
    /// <param name="version">The version of the API contract.</param>
    /// <remarks>
    /// This constructor applies to a type with the <see cref="ApiContractAttribute"/> and specifies the contract version of that API contract.
    /// </remarks>
    public ContractVersionAttribute(string contract, uint version)
    {
    }

    /// <summary>
    /// Creates a new <see cref="ContractVersionAttribute"/> instance with the specified parameters.
    /// </summary>
    /// <param name="contract">The type to associate with the API contract.</param>
    /// <param name="version">The version of the API contract.</param>
    /// <remarks>
    /// This constructor applies to any type that does not have the <see cref="ApiContractAttribute"/> and
    /// indicates the API contract version in which this type was added to the specified API contract.
    /// </remarks>
    public ContractVersionAttribute(Type contract, uint version)
    {
    }
}