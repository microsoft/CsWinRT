// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime.InteropGenerator.Errors;

/// <summary>
/// Well known exceptions for the interop generator.
/// </summary>
internal static class WellKnownInteropExceptions
{
    /// <summary>
    /// The prefix for all errors produced by this tool.
    /// </summary>
    public const string ErrorPrefix = "CSWINRTGEN";

    /// <summary>
    /// A runtime class name is too long.
    /// </summary>
    public static Exception RuntimeClassNameTooLong(string name)
    {
        return Exception(1, $"The runtime class name '{name}' is too long. The maximum length is {ushort.MaxValue} characters.");
    }

    /// <summary>
    /// There are too many runtime class names to build the lookup.
    /// </summary>
    public static Exception RuntimeClassNameLookupSizeLimitExceeded()
    {
        return Exception(2, "The runtime class name lookup size limit was exceeded.");
    }

    /// <summary>
    /// Creates a new exception with the specified id and message.
    /// </summary>
    /// <param name="id">The exception id.</param>
    /// <param name="message">The exception message.</param>
    /// <returns>The resulting exception.</returns>
    private static Exception Exception(int id, string message)
    {
        return new WellKnownInteropException($"{ErrorPrefix}{id:0000}", message);
    }
}

