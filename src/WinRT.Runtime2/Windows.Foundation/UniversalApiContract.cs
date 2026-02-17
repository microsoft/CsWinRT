// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Windows.Foundation.Metadata;

namespace Windows.Foundation;

/// <summary>
/// Represents the Universal API contract.
/// </summary>
/// <remarks>
/// This type is required for ABI projection of the value types and delegates, but marshalling it is not supported.
/// </remarks>
[ContractVersion(1245184u)]
public enum UniversalApiContract;