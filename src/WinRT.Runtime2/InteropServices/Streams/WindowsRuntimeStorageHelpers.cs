// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.IO;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Provides helpers for converting between <see cref="System.IO"/> types and Windows Runtime handle option types.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class WindowsRuntimeStorageHelpers
{
    /// <summary>
    /// Converts a <see cref="FileAccess"/> value to a <see cref="HandleAccessOptions"/> value.
    /// </summary>
    /// <param name="access">The <see cref="FileAccess"/> value to convert.</param>
    /// <returns>The corresponding <see cref="HandleAccessOptions"/> value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="access"/> is not a valid value.</exception>
    public static HandleAccessOptions FileAccessToHandleAccessOptions(FileAccess access)
    {
        return access switch
        {
            FileAccess.ReadWrite => HandleAccessOptions.Read | HandleAccessOptions.Write,
            FileAccess.Read => HandleAccessOptions.Read,
            FileAccess.Write => HandleAccessOptions.Write,
            _ => throw new ArgumentOutOfRangeException(nameof(access), access, null),
        };
    }

    /// <summary>
    /// Converts a <see cref="FileShare"/> value to a <see cref="HandleSharingOptions"/> value.
    /// </summary>
    /// <param name="share">The <see cref="FileShare"/> value to convert.</param>
    /// <returns>The corresponding <see cref="HandleSharingOptions"/> value.</returns>
    /// <exception cref="NotSupportedException">Thrown if <paramref name="share"/> has the <see cref="FileShare.Inheritable"/> flag.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="share"/> is not a valid combination of flags.</exception>
    public static HandleSharingOptions FileShareToHandleSharingOptions(FileShare share)
    {
        if ((share & FileShare.Inheritable) != 0)
        {
            throw new NotSupportedException(WindowsRuntimeExceptionMessages.NotSupported_InheritableIsNotSupportedOption);
        }

        if (share is < FileShare.None or > (FileShare.ReadWrite | FileShare.Delete))
        {
            throw new ArgumentOutOfRangeException(nameof(share), share, null);
        }

        HandleSharingOptions sharingOptions = HandleSharingOptions.ShareNone;

        if ((share & FileShare.Read) != 0)
        {
            sharingOptions |= HandleSharingOptions.ShareRead;
        }

        if ((share & FileShare.Write) != 0)
        {
            sharingOptions |= HandleSharingOptions.ShareWrite;
        }

        if ((share & FileShare.Delete) != 0)
        {
            sharingOptions |= HandleSharingOptions.ShareDelete;
        }

        return sharingOptions;
    }

    /// <summary>
    /// Converts a <see cref="FileOptions"/> value to a <see cref="HandleOptions"/> value.
    /// </summary>
    /// <param name="options">The <see cref="FileOptions"/> value to convert.</param>
    /// <returns>The corresponding <see cref="HandleOptions"/> value.</returns>
    /// <exception cref="NotSupportedException">Thrown if <paramref name="options"/> has the <see cref="FileOptions.Encrypted"/> flag.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="options"/> contains unsupported flags.</exception>
    public static HandleOptions FileOptionsToHandleOptions(FileOptions options)
    {
        return (options & FileOptions.Encrypted) != 0
            ? throw new NotSupportedException(WindowsRuntimeExceptionMessages.NotSupported_EncryptedIsNotSupportedOption)
            : options != FileOptions.None && (options &
                ~(FileOptions.WriteThrough | FileOptions.Asynchronous | FileOptions.RandomAccess | FileOptions.DeleteOnClose |
                  FileOptions.SequentialScan | (FileOptions)0x20000000 /* NoBuffering */)) != 0
                ? throw new ArgumentOutOfRangeException(nameof(options), options, null)
                : (HandleOptions)(uint)options;
    }

    /// <summary>
    /// Converts a <see cref="FileMode"/> value to a <see cref="HandleCreationOptions"/> value.
    /// </summary>
    /// <param name="mode">The <see cref="FileMode"/> value to convert.</param>
    /// <returns>The corresponding <see cref="HandleCreationOptions"/> value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="mode"/> is not a valid value.</exception>
    public static HandleCreationOptions FileModeToHandleCreationOptions(FileMode mode)
    {
        return mode is < FileMode.CreateNew or > FileMode.Append
            ? throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
            : mode == FileMode.Append
                ? HandleCreationOptions.CreateAlways
                : (HandleCreationOptions)(uint)mode;
    }
}
