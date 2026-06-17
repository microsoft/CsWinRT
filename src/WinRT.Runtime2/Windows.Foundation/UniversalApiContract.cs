// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if WINDOWS_RUNTIME_REFERENCE_ASSEMBLY
using Windows.Foundation.Metadata;
#endif

namespace Windows.Foundation;

/// <summary>
/// Represents the Universal API contract.
/// </summary>
#if WINDOWS_RUNTIME_REFERENCE_ASSEMBLY
[ApiContract]
[ContractVersion(1245184u)]
#endif
public enum UniversalApiContract;