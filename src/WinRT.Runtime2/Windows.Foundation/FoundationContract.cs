// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Windows.Foundation.Metadata;

namespace Windows.Foundation;

/// <summary>
/// Represents the Windows Foundation API contract.
/// </summary>
/// <remarks>
/// This type is required for ABI projection of the value types and delegates, but marshalling it is not supported.
/// </remarks>
[ContractVersion(262144u)]
public enum FoundationContract;