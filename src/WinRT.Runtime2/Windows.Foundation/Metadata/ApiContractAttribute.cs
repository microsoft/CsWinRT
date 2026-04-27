// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
#if WINDOWS_RUNTIME_REFERENCE_ASSEMBLY
using System.Runtime.Versioning;
#endif
using WindowsRuntime;

namespace Windows.Foundation.Metadata;

/// <summary>
/// Specifies that the type represents an API contract.
/// </summary>
#if WINDOWS_RUNTIME_IMPLEMENTATION_ASSEMBLY
[WindowsRuntimeMetadata("Windows.Foundation.FoundationContract")]
#elif WINDOWS_RUNTIME_REFERENCE_ASSEMBLY
[SupportedOSPlatform("Windows10.0.10240.0")]
[ContractVersion(typeof(FoundationContract), 65536u)]
#endif
[AttributeUsage(AttributeTargets.Enum, AllowMultiple = false)]
public sealed class ApiContractAttribute : Attribute;
