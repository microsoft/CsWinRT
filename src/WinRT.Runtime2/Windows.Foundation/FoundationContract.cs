// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if WINDOWS_RUNTIME_REFERENCE_ASSEMBLY
using Windows.Foundation.Metadata;
#endif

namespace Windows.Foundation;

/// <summary>
/// Represents the Windows Foundation API contract.
/// </summary>
#if WINDOWS_RUNTIME_REFERENCE_ASSEMBLY
[ApiContract]
[ContractVersion(262144u)]
#endif
public enum FoundationContract;