// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.Versioning;
using WindowsRuntime;

namespace Windows.Foundation.Metadata;

/// <summary>
/// Specifies that the type represents an API contract.
/// </summary>
[WindowsRuntimeMetadata("Windows.Foundation.FoundationContract")]
[AttributeUsage(AttributeTargets.Enum, AllowMultiple = false)]
[SupportedOSPlatform("Windows10.0.10240.0")]
[ContractVersion(typeof(FoundationContract), 65536u)]
public sealed class ApiContractAttribute : Attribute;
