// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.ProjectionWriter.Models;

/// <summary>
/// Categorization of how a parameter is passed across the WinRT ABI boundary.
/// </summary>
internal enum ParameterCategory
{
    /// <summary>
    /// By-value input parameter (the default).
    /// </summary>
    In,

    /// <summary>
    /// By-reference parameter (read/write).
    /// </summary>
    Ref,

    /// <summary>
    /// By-reference output-only parameter.
    /// </summary>
    Out,

    /// <summary>
    /// An input array passed by value (caller fills the array).
    /// </summary>
    PassArray,

    /// <summary>
    /// An array of fixed length whose contents the callee fills.
    /// </summary>
    FillArray,

    /// <summary>
    /// An output array allocated by the callee and returned to the caller.
    /// </summary>
    ReceiveArray,
}
