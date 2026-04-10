// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.Versioning;
using WindowsRuntime;

namespace Windows.Foundation.Metadata;

/// <summary>
/// Specifies that the type represents an API contract.
/// </summary>
/// <remarks>
/// This type is required for ABI projection of the value types and delegates, but marshalling it is not supported.
/// </remarks>
[WindowsRuntimeMetadata("Windows.Foundation.FoundationContract")]
[AttributeUsage(AttributeTargets.Enum, AllowMultiple = false)]
[SupportedOSPlatform("Windows10.0.10240.0")]
[ContractVersion(typeof(FoundationContract), 65536u)]
public sealed class ApiContractAttribute : Attribute;
