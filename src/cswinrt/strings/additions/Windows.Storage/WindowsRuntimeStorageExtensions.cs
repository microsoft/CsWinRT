// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Windows.Storage.IO
{
    using global::System.Diagnostics;
    using global::System.IO;
    using global::System.Runtime.InteropServices;
    using global::System.Threading.Tasks;
    using global::Microsoft.Win32.SafeHandles;
    using global::Windows.Storage;
    using global::Windows.Storage.FileProperties;
    using global::Windows.Storage.Streams;

    /// <summary>
    /// Provides extension methods for working with Windows Runtime storage files and folders.
    /// </summary>
    public static class WindowsRuntimeStorageExtensions
    {
        /// <summary>
        /// Opens a <see cref="Stream"/> for reading from the specified <see cref="IStorageFile"/>.
        /// </summary>
        /// <param name="windowsRuntimeFile">The <see cref="IStorageFile"/> to read from.</param>
        /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous operation, with a <see cref="Stream"/> as the result.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="windowsRuntimeFile"/> is <see langword="null"/>.</exception>
        /// <exception cref="IOException">Thrown if the file could not be opened or retrieved as a stream.</exception>
        [global::System.Runtime.Versioning.SupportedOSPlatform("windows10.0.10240.0")]
        public static Task<Stream> OpenStreamForReadAsync(this IStorageFile windowsRuntimeFile)
        {
            ArgumentNullException.ThrowIfNull(windowsRuntimeFile);

            return OpenStreamForReadAsyncCore(windowsRuntimeFile);
        }

        [global::System.Runtime.Versioning.SupportedOSPlatform("windows10.0.10240.0")]
        private static async Task<Stream> OpenStreamForReadAsyncCore(this IStorageFile windowsRuntimeFile)
        {
            Debug.Assert(windowsRuntimeFile != null);

            try
            {
                IRandomAccessStream windowsRuntimeStream = await windowsRuntimeFile.OpenAsync(FileAccessMode.Read)
                                                                 .AsTask().ConfigureAwait(continueOnCapturedContext: false);
                Stream managedStream = windowsRuntimeStream.AsStreamForRead();
                return managedStream;
            }
            catch (Exception ex)
            {
                // From this API, managed dev expect IO exceptions for "something wrong":
                WindowsRuntimeIOHelpers.GetExceptionDispatchInfo(ex).Throw();
                return null;
            }
        }

        /// <summary>
        /// Opens a <see cref="Stream"/> for writing to the specified <see cref="IStorageFile"/>.
        /// </summary>
        /// <param name="windowsRuntimeFile">The <see cref="IStorageFile"/> to write to.</param>
        /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous operation, with a <see cref="Stream"/> as the result.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="windowsRuntimeFile"/> is <see langword="null"/>.</exception>
        /// <exception cref="IOException">Thrown if the file could not be opened or retrieved as a stream.</exception>
        [global::System.Runtime.Versioning.SupportedOSPlatform("windows10.0.10240.0")]
        public static Task<Stream> OpenStreamForWriteAsync(this IStorageFile windowsRuntimeFile)
        {
            ArgumentNullException.ThrowIfNull(windowsRuntimeFile);

            return OpenStreamForWriteAsyncCore(windowsRuntimeFile, 0);
        }

        [global::System.Runtime.Versioning.SupportedOSPlatform("windows10.0.10240.0")]
        private static async Task<Stream> OpenStreamForWriteAsyncCore(this IStorageFile windowsRuntimeFile, long offset)
        {
            Debug.Assert(windowsRuntimeFile != null);
            Debug.Assert(offset >= 0);

            try
            {
                IRandomAccessStream windowsRuntimeStream = await windowsRuntimeFile.OpenAsync(FileAccessMode.ReadWrite)
                                                                 .AsTask().ConfigureAwait(continueOnCapturedContext: false);
                Stream managedStream = windowsRuntimeStream.AsStreamForWrite();
                managedStream.Position = offset;
                return managedStream;
            }
            catch (Exception ex)
            {
                // From this API, managed dev expect IO exceptions for "something wrong":
                WindowsRuntimeIOHelpers.GetExceptionDispatchInfo(ex).Throw();
                return null;
            }
        }

        /// <summary>
        /// Opens a <see cref="Stream"/> for reading from a file in the specified <see cref="IStorageFolder"/>.
        /// </summary>
        /// <param name="rootDirectory">The <see cref="IStorageFolder"/> that contains the file to read from.</param>
        /// <param name="relativePath">The path, relative to <paramref name="rootDirectory"/>, of the file to read from.</param>
        /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous operation, with a <see cref="Stream"/> as the result.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="rootDirectory"/> or <paramref name="relativePath"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="relativePath"/> is empty or contains only whitespace.</exception>
        /// <exception cref="IOException">Thrown if the file could not be opened or retrieved as a stream.</exception>
        [global::System.Runtime.Versioning.SupportedOSPlatform("windows10.0.10240.0")]
        public static Task<Stream> OpenStreamForReadAsync(this IStorageFolder rootDirectory, string relativePath)
        {
            ArgumentNullException.ThrowIfNull(rootDirectory);
            ArgumentNullException.ThrowIfNull(relativePath);

            if (string.IsNullOrWhiteSpace(relativePath))
            {
                throw new ArgumentException(global::Windows.Storage.SR.Argument_RelativePathMayNotBeWhitespaceOnly, nameof(relativePath));
            }

            return OpenStreamForReadAsyncCore(rootDirectory, relativePath);
        }

        [global::System.Runtime.Versioning.SupportedOSPlatform("windows10.0.10240.0")]
        private static async Task<Stream> OpenStreamForReadAsyncCore(this IStorageFolder rootDirectory, string relativePath)
        {
            Debug.Assert(rootDirectory != null);
            Debug.Assert(!string.IsNullOrWhiteSpace(relativePath));

            try
            {
                IStorageFile windowsRuntimeFile = await rootDirectory.GetFileAsync(relativePath)
                                                        .AsTask().ConfigureAwait(continueOnCapturedContext: false);
                Stream managedStream = await windowsRuntimeFile.OpenStreamForReadAsync()
                                             .ConfigureAwait(continueOnCapturedContext: false);
                return managedStream;
            }
            catch (Exception ex)
            {
                // From this API, managed dev expect IO exceptions for "something wrong":
                WindowsRuntimeIOHelpers.GetExceptionDispatchInfo(ex).Throw();
                return null;
            }
        }

        /// <summary>
        /// Opens a <see cref="Stream"/> for writing to a file in the specified <see cref="IStorageFolder"/>.
        /// </summary>
        /// <param name="rootDirectory">The <see cref="IStorageFolder"/> that contains or will contain the file to write to.</param>
        /// <param name="relativePath">The path, relative to <paramref name="rootDirectory"/>, of the file to write to.</param>
        /// <param name="creationCollisionOption">The <see cref="CreationCollisionOption"/> value that specifies how to handle the situation when the file already exists.</param>
        /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous operation, with a <see cref="Stream"/> as the result.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="rootDirectory"/> or <paramref name="relativePath"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="relativePath"/> is empty or contains only whitespace.</exception>
        /// <exception cref="IOException">Thrown if the file could not be opened or retrieved as a stream.</exception>
        [global::System.Runtime.Versioning.SupportedOSPlatform("windows10.0.10240.0")]
        public static Task<Stream> OpenStreamForWriteAsync(this IStorageFolder rootDirectory, string relativePath,
                                                           CreationCollisionOption creationCollisionOption)
        {
            ArgumentNullException.ThrowIfNull(rootDirectory);
            ArgumentNullException.ThrowIfNull(relativePath);

            if (string.IsNullOrWhiteSpace(relativePath))
            {
                throw new ArgumentException(global::Windows.Storage.SR.Argument_RelativePathMayNotBeWhitespaceOnly, nameof(relativePath));
            }

            return OpenStreamForWriteAsyncCore(rootDirectory, relativePath, creationCollisionOption);
        }

        [global::System.Runtime.Versioning.SupportedOSPlatform("windows10.0.10240.0")]
        private static async Task<Stream> OpenStreamForWriteAsyncCore(this IStorageFolder rootDirectory, string relativePath,
                                                                       CreationCollisionOption creationCollisionOption)
        {
            Debug.Assert(rootDirectory != null);
            Debug.Assert(!string.IsNullOrWhiteSpace(relativePath));

            Debug.Assert(creationCollisionOption == CreationCollisionOption.FailIfExists
                                    || creationCollisionOption == CreationCollisionOption.GenerateUniqueName
                                    || creationCollisionOption == CreationCollisionOption.OpenIfExists
                                    || creationCollisionOption == CreationCollisionOption.ReplaceExisting,
                              "The specified creationCollisionOption has a value that is not a value we considered when devising the"
                            + " policy about Append-On-OpenIfExists used in this method. Apparently a new enum value was added to the"
                            + " CreationCollisionOption type and we need to make sure that the policy still makes sense.");

            try
            {
                // Open file and set up default options for opening it:

                IStorageFile windowsRuntimeFile = await rootDirectory.CreateFileAsync(relativePath, creationCollisionOption)
                                                                     .AsTask().ConfigureAwait(continueOnCapturedContext: false);
                long offset = 0;

                // If the specified creationCollisionOption was OpenIfExists, then we will try to APPEND, otherwise we will OVERWRITE:

                if (creationCollisionOption == CreationCollisionOption.OpenIfExists)
                {
                    BasicProperties fileProperties = await windowsRuntimeFile.GetBasicPropertiesAsync()
                                                           .AsTask().ConfigureAwait(continueOnCapturedContext: false);
                    ulong fileSize = fileProperties.Size;

                    Debug.Assert(fileSize <= long.MaxValue, ".NET streams assume that file sizes are not larger than Int64.MaxValue,"
                                                              + " so we are not supporting the situation where this is not the case.");
                    offset = checked((long)fileSize);
                }

                // Now open a file with the correct options:

                Stream managedStream = await OpenStreamForWriteAsyncCore(windowsRuntimeFile, offset).ConfigureAwait(continueOnCapturedContext: false);
                return managedStream;
            }
            catch (Exception ex)
            {
                // From this API, managed dev expect IO exceptions for "something wrong":
                WindowsRuntimeIOHelpers.GetExceptionDispatchInfo(ex).Throw();
                return null;
            }
        }

        /// <summary>
        /// Creates a <see cref="SafeFileHandle"/> for the specified <see cref="IStorageFile"/>.
        /// </summary>
        /// <param name="windowsRuntimeFile">The <see cref="IStorageFile"/> to create a file handle for.</param>
        /// <param name="access">The <see cref="FileAccess"/> mode to open the file with.</param>
        /// <param name="share">The <see cref="FileShare"/> mode to open the file with.</param>
        /// <param name="options">The <see cref="FileOptions"/> to open the file with.</param>
        /// <returns>A <see cref="SafeFileHandle"/> for the specified storage file.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="windowsRuntimeFile"/> is <see langword="null"/>.</exception>
        public static SafeFileHandle CreateSafeFileHandle(
            this IStorageFile windowsRuntimeFile,
            FileAccess access = FileAccess.ReadWrite,
            FileShare share = FileShare.Read,
            FileOptions options = FileOptions.None)
        {
            ArgumentNullException.ThrowIfNull(windowsRuntimeFile);

            return global::WindowsRuntime.InteropServices.IStorageItemHandleAccessMethods.Create(
                (global::WindowsRuntime.WindowsRuntimeObject)windowsRuntimeFile,
                access,
                share,
                options);
        }

        /// <summary>
        /// Creates a <see cref="SafeFileHandle"/> for a file in the specified <see cref="IStorageFolder"/>.
        /// </summary>
        /// <param name="rootDirectory">The <see cref="IStorageFolder"/> that contains the file.</param>
        /// <param name="relativePath">The path, relative to <paramref name="rootDirectory"/>, of the file to create a handle for.</param>
        /// <param name="mode">The <see cref="FileMode"/> to use when opening the file.</param>
        /// <returns>A <see cref="SafeFileHandle"/> for the specified file.</returns>
        public static SafeFileHandle CreateSafeFileHandle(
            this IStorageFolder rootDirectory,
            string relativePath,
            FileMode mode)
        {
            return rootDirectory.CreateSafeFileHandle(relativePath, mode, (mode == FileMode.Append ? FileAccess.Write : FileAccess.ReadWrite));
        }

        /// <summary>
        /// Creates a <see cref="SafeFileHandle"/> for a file in the specified <see cref="IStorageFolder"/>.
        /// </summary>
        /// <param name="rootDirectory">The <see cref="IStorageFolder"/> that contains the file.</param>
        /// <param name="relativePath">The path, relative to <paramref name="rootDirectory"/>, of the file to create a handle for.</param>
        /// <param name="mode">The <see cref="FileMode"/> to use when opening the file.</param>
        /// <param name="access">The <see cref="FileAccess"/> mode to open the file with.</param>
        /// <param name="share">The <see cref="FileShare"/> mode to open the file with.</param>
        /// <param name="options">The <see cref="FileOptions"/> to open the file with.</param>
        /// <returns>A <see cref="SafeFileHandle"/> for the specified file.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="rootDirectory"/> or <paramref name="relativePath"/> is <see langword="null"/>.</exception>
        public static SafeFileHandle CreateSafeFileHandle(
            this IStorageFolder rootDirectory,
            string relativePath,
            FileMode mode,
            FileAccess access,
            FileShare share = FileShare.Read,
            FileOptions options = FileOptions.None)
        {
            ArgumentNullException.ThrowIfNull(rootDirectory);
            ArgumentNullException.ThrowIfNull(relativePath);

            return global::WindowsRuntime.InteropServices.IStorageFolderHandleAccessMethods.Create(
                (global::WindowsRuntime.WindowsRuntimeObject)rootDirectory,
                relativePath,
                mode,
                access,
                share,
                options);
        }
    }
}
