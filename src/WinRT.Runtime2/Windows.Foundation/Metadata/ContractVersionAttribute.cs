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
/// <remarks>
/// This type is required for ABI projection of the value types and delegates, but marshalling it is not supported.
/// </remarks>
[WindowsRuntimeMetadata("Windows.Foundation.FoundationContract")]
[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
[SupportedOSPlatform("Windows10.0.10240.0")]
[ContractVersion(typeof(FoundationContract), 65536u)]
public sealed class ContractVersionAttribute : Attribute
{
    /// <summary>
    /// Creates a new <see cref="ContractVersionAttribute"/> instance with the specified parameters.
    /// </summary>
    /// <param name="version">The version of the API contract.</param>
    public ContractVersionAttribute(uint version)
    {
    }

    /// <summary>
    /// Creates a new <see cref="ContractVersionAttribute"/> instance with the specified parameters.
    /// </summary>
    /// <param name="contract">The type to associate with the API contract.</param>
    /// <param name="version">The version of the API contract.</param>
    public ContractVersionAttribute(Type contract, uint version)
    {
    }

    /// <summary>
    /// Creates a new <see cref="ContractVersionAttribute"/> instance with the specified parameters.
    /// </summary>
    /// <param name="contract">The type to associate with the API contract.</param>
    /// <param name="version">The version of the API contract.</param>
    public ContractVersionAttribute(string contract, uint version)
    {
    }
}