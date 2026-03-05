// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Provides helpers for converting between <see cref="System.IO"/> types and Windows Runtime handle option types.
/// </summary>
internal static class WindowsRuntimeStorageHelpers
{
    /// <summary>
    /// Converts a <see cref="FileAccess"/> value to a <see cref="HANDLE_ACCESS_OPTIONS"/> value.
    /// </summary>
    /// <param name="access">The <see cref="FileAccess"/> value to convert.</param>
    /// <returns>The corresponding <see cref="HANDLE_ACCESS_OPTIONS"/> value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="access"/> is not a valid value.</exception>
    public static uint FileAccessToHandleAccessOptions(FileAccess access)
    {
        return access switch
        {
            FileAccess.ReadWrite => (uint)(HANDLE_ACCESS_OPTIONS.HAO_READ | HANDLE_ACCESS_OPTIONS.HAO_WRITE),
            FileAccess.Read => (uint)HANDLE_ACCESS_OPTIONS.HAO_READ,
            FileAccess.Write => (uint)HANDLE_ACCESS_OPTIONS.HAO_WRITE,
            _ => throw new ArgumentOutOfRangeException(nameof(access), access, null),
        };
    }

    /// <summary>
    /// Converts a <see cref="FileShare"/> value to a <see cref="HANDLE_SHARING_OPTIONS"/> value.
    /// </summary>
    /// <param name="share">The <see cref="FileShare"/> value to convert.</param>
    /// <returns>The corresponding <see cref="HANDLE_SHARING_OPTIONS"/> value.</returns>
    /// <exception cref="NotSupportedException">Thrown if <paramref name="share"/> has the <see cref="FileShare.Inheritable"/> flag.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="share"/> is not a valid combination of flags.</exception>
    public static uint FileShareToHandleSharingOptions(FileShare share)
    {
        NotSupportedException.ThrowIfFileShareIsInheritable(share);
        ArgumentOutOfRangeException.ThrowIfFileShareOutOfRange(share);

        HANDLE_SHARING_OPTIONS sharingOptions = HANDLE_SHARING_OPTIONS.HSO_SHARE_NONE;

        if ((share & FileShare.Read) != 0)
        {
            sharingOptions |= HANDLE_SHARING_OPTIONS.HSO_SHARE_READ;
        }

        if ((share & FileShare.Write) != 0)
        {
            sharingOptions |= HANDLE_SHARING_OPTIONS.HSO_SHARE_WRITE;
        }

        if ((share & FileShare.Delete) != 0)
        {
            sharingOptions |= HANDLE_SHARING_OPTIONS.HSO_SHARE_DELETE;
        }

        return (uint)sharingOptions;
    }

    /// <summary>
    /// Converts a <see cref="FileOptions"/> value to a <see cref="HANDLE_OPTIONS"/> value.
    /// </summary>
    /// <param name="options">The <see cref="FileOptions"/> value to convert.</param>
    /// <returns>The corresponding <see cref="HANDLE_OPTIONS"/> value.</returns>
    /// <exception cref="NotSupportedException">Thrown if <paramref name="options"/> has the <see cref="FileOptions.Encrypted"/> flag.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="options"/> contains unsupported flags.</exception>
    public static uint FileOptionsToHandleOptions(FileOptions options)
    {
        NotSupportedException.ThrowIfFileOptionsAreEncrypted(options);
        ArgumentOutOfRangeException.ThrowIfFileOptionsOutOfRange(options);

        return (uint)options;
    }

    /// <summary>
    /// Converts a <see cref="FileMode"/> value to a <see cref="HANDLE_CREATION_OPTIONS"/> value.
    /// </summary>
    /// <param name="mode">The <see cref="FileMode"/> value to convert.</param>
    /// <returns>The corresponding <see cref="HANDLE_CREATION_OPTIONS"/> value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="mode"/> is not a valid value.</exception>
    public static uint FileModeToHandleCreationOptions(FileMode mode)
    {
        ArgumentOutOfRangeException.ThrowIfFileModeOutOfRange(mode);

        return mode == FileMode.Append
            ? (uint)HANDLE_CREATION_OPTIONS.HCO_CREATE_ALWAYS
            : (uint)mode;
    }
}
