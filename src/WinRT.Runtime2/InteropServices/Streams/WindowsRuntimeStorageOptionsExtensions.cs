// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System;
using System.IO;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Provides extensions for converting between <see cref="System.IO"/> types and Windows Runtime handle option types.
/// </summary>
internal static class WindowsRuntimeStorageOptionsExtensions
{
    /// <param name="access">The input <see cref="FileAccess"/> value to convert.</param>
    extension(FileAccess access)
    {
        /// <summary>
        /// Converts a <see cref="FileAccess"/> value to a <see cref="HANDLE_ACCESS_OPTIONS"/> value.
        /// </summary>
        /// <returns>The corresponding <see cref="HANDLE_ACCESS_OPTIONS"/> value.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the value is not a valid <see cref="FileAccess"/>.</exception>
        public HANDLE_ACCESS_OPTIONS ToHandleAccessOptions()
        {
            return access switch
            {
                FileAccess.ReadWrite => HANDLE_ACCESS_OPTIONS.HAO_READ | HANDLE_ACCESS_OPTIONS.HAO_WRITE,
                FileAccess.Read => HANDLE_ACCESS_OPTIONS.HAO_READ,
                FileAccess.Write => HANDLE_ACCESS_OPTIONS.HAO_WRITE,
                _ => throw ArgumentOutOfRangeException.GetFileAccessOutOfRangeException(nameof(access), access),
            };
        }
    }

    /// <param name="share">The input <see cref="FileShare"/> value to convert.</param>
    extension(FileShare share)
    {
        /// <summary>
        /// Converts a <see cref="FileShare"/> value to a <see cref="HANDLE_SHARING_OPTIONS"/> value.
        /// </summary>
        /// <returns>The corresponding <see cref="HANDLE_SHARING_OPTIONS"/> value.</returns>
        /// <exception cref="NotSupportedException">Thrown if the value has the <see cref="FileShare.Inheritable"/> flag.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the value is not a valid combination of flags.</exception>
        public HANDLE_SHARING_OPTIONS ToHandleSharingOptions()
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

            return sharingOptions;
        }
    }

    /// <param name="options">The input <see cref="FileOptions"/> value to convert.</param>
    extension(FileOptions options)
    {
        /// <summary>
        /// Converts a <see cref="FileOptions"/> value to a <see cref="HANDLE_OPTIONS"/> value.
        /// </summary>
        /// <returns>The corresponding <see cref="HANDLE_OPTIONS"/> value.</returns>
        /// <exception cref="NotSupportedException">Thrown if the value has the <see cref="FileOptions.Encrypted"/> flag.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the value contains unsupported flags.</exception>
        public HANDLE_OPTIONS ToHandleOptions()
        {
            NotSupportedException.ThrowIfFileOptionsAreEncrypted(options);
            ArgumentOutOfRangeException.ThrowIfFileOptionsOutOfRange(options);

            return (HANDLE_OPTIONS)(uint)options;
        }
    }

    /// <param name="mode">The input <see cref="FileMode"/> value to convert.</param>
    extension(FileMode mode)
    {
        /// <summary>
        /// Converts a <see cref="FileMode"/> value to a <see cref="HANDLE_CREATION_OPTIONS"/> value.
        /// </summary>
        /// <returns>The corresponding <see cref="HANDLE_CREATION_OPTIONS"/> value.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the value is not a valid <see cref="FileMode"/>.</exception>
        public HANDLE_CREATION_OPTIONS ToHandleCreationOptions()
        {
            ArgumentOutOfRangeException.ThrowIfFileModeOutOfRange(mode);

            return mode == FileMode.Append
                ? HANDLE_CREATION_OPTIONS.HCO_CREATE_ALWAYS
                : (HANDLE_CREATION_OPTIONS)(uint)mode;
        }
    }
}
#endif