// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Indicates the read optimization to employ for a given <see cref="System.IO.Stream"/> object.
/// </summary>
internal enum StreamReadOperationOptimization
{
    // We may want to define different behaviors for different types of streams.
    // For instance, 'ReadAsync' treats 'MemoryStream' as special for performance
    // reasons. The enum 'StreamReadOperationOptimization' describes the read
    // optimization to employ for a given 'Stream' instance. In the future, we might
    // define other enums to follow a similar pattern, e.g.:
    //   - 'StreamWriteOperationOptimization'
    //   - 'StreamFlushOperationOptimization'
    //   - etc.

    /// <summary>
    /// Specifies a generic <see cref="System.IO.Stream"/> of some unknown
    /// type, for which no specific optimizations can be applied.
    /// </summary>
    AbstractStream = 0,

    /// <summary>
    /// Specifies a <see cref="System.IO.MemoryStream"/> instance, meaning its underlying buffer can be unwrapped.
    /// </summary>
    MemoryStream = 1
}