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
#if WINDOWS_RUNTIME_IMPLEMENTATION_ASSEMBLY
[WindowsRuntimeMetadata("Windows.Foundation.FoundationContract")]
#endif
[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
#if WINDOWS_RUNTIME_REFERENCE_ASSEMBLY
[SupportedOSPlatform("Windows10.0.10240.0")]
[ContractVersion(typeof(FoundationContract), 65536u)]
#endif
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