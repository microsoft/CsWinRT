// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Windows.Storage.IO
{
    using global::System.Diagnostics;
    using global::System.IO;
    using global::System.Runtime.InteropServices;
    using global::System.Runtime.Versioning;
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
        [SupportedOSPlatform("windows10.0.10240.0")]
        public static Task<Stream> OpenStreamForReadAsync(this IStorageFile windowsRuntimeFile)
        {
            ArgumentNullException.ThrowIfNull(windowsRuntimeFile);

            // Helper with the actual read logic
            [SupportedOSPlatform("windows10.0.10240.0")]
            static async Task<Stream> OpenStreamForReadCoreAsync(IStorageFile windowsRuntimeFile)
            {
                try
                {
                    IRandomAccessStream windowsRuntimeStream = await windowsRuntimeFile
                        .OpenAsync(FileAccessMode.Read)
                        .AsTask().ConfigureAwait(false);

                    return windowsRuntimeStream.AsStreamForRead();
                }
                catch (Exception exception)
                {
                    // From this API, callers expect an 'IOException' if something went wrong
                    WindowsRuntimeIOHelpers.GetExceptionDispatchInfo(exception).Throw();

                    return null;
                }
            }

            return OpenStreamForReadCoreAsync(windowsRuntimeFile);
        }

        /// <summary>
        /// Opens a <see cref="Stream"/> for writing to the specified <see cref="IStorageFile"/>.
        /// </summary>
        /// <param name="windowsRuntimeFile">The <see cref="IStorageFile"/> to write to.</param>
        /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous operation, with a <see cref="Stream"/> as the result.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="windowsRuntimeFile"/> is <see langword="null"/>.</exception>
        /// <exception cref="IOException">Thrown if the file could not be opened or retrieved as a stream.</exception>
        [SupportedOSPlatform("windows10.0.10240.0")]
        public static Task<Stream> OpenStreamForWriteAsync(this IStorageFile windowsRuntimeFile)
        {
            ArgumentNullException.ThrowIfNull(windowsRuntimeFile);

            return OpenStreamForWriteCoreAsync(windowsRuntimeFile, offset: 0);
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
        [SupportedOSPlatform("windows10.0.10240.0")]
        public static Task<Stream> OpenStreamForReadAsync(this IStorageFolder rootDirectory, string relativePath)
        {
            ArgumentNullException.ThrowIfNull(rootDirectory);
            ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

            // Helper with the actual read logic
            [SupportedOSPlatform("windows10.0.10240.0")]
            static async Task<Stream> OpenStreamForReadCoreAsync(IStorageFolder rootDirectory, string relativePath)
            {
                try
                {
                    IStorageFile windowsRuntimeFile = await rootDirectory
                        .GetFileAsync(relativePath)
                        .AsTask()
                        .ConfigureAwait(false);

                    return await windowsRuntimeFile
                        .OpenStreamForReadAsync()
                        .ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    // Throw an 'IOException' (see notes above)
                    WindowsRuntimeIOHelpers.GetExceptionDispatchInfo(exception).Throw();

                    return null;
                }
            }

            return OpenStreamForReadCoreAsync(rootDirectory, relativePath);
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
        [SupportedOSPlatform("windows10.0.10240.0")]
        public static Task<Stream> OpenStreamForWriteAsync(
            this IStorageFolder rootDirectory,
            string relativePath,
            CreationCollisionOption creationCollisionOption)
        {
            ArgumentNullException.ThrowIfNull(rootDirectory);
            ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

            // Helper with the actual write logic
            [SupportedOSPlatform("windows10.0.10240.0")]
            private static async Task<Stream> OpenStreamForWriteCoreAsync(
                IStorageFolder rootDirectory,
                string relativePath,
                CreationCollisionOption creationCollisionOption)
            {
                Debug.Assert(creationCollisionOption is
                    CreationCollisionOption.FailIfExists or
                    CreationCollisionOption.GenerateUniqueName or
                    CreationCollisionOption.OpenIfExists or
                    CreationCollisionOption.ReplaceExisting,
                    "The specified 'creationCollisionOption' argument has a value that is not a value we considered when devising the " +
                    "policy about 'Append-On-OpenIfExists' used in this method. Apparently a new enum value was added to the " +
                    "'CreationCollisionOption' type and we need to make sure that the policy still makes sense.");

                try
                {
                    // Open file and set up default options for opening it
                    IStorageFile windowsRuntimeFile = await rootDirectory
                        .CreateFileAsync(relativePath, creationCollisionOption)
                        .AsTask()
                        .ConfigureAwait(false);

                    long offset = 0;

                    // If the specified option was 'OpenIfExists', then we will try to append, otherwise we will overwrite
                    if (creationCollisionOption is CreationCollisionOption.OpenIfExists)
                    {
                        BasicProperties fileProperties = await windowsRuntimeFile
                            .GetBasicPropertiesAsync()
                            .AsTask()
                            .ConfigureAwait(false);

                        ulong fileSize = fileProperties.Size;

                        Debug.Assert(fileSize <= long.MaxValue, ".NET streams assume that file sizes are not larger than 'long.MaxValue.");

                        offset = checked((long)fileSize);
                    }

                    // Now open a file with the correct options
                    return await OpenStreamForWriteCoreAsync(windowsRuntimeFile, offset).ConfigureAwait(false);
                }
                catch (Exception exception) when (exception is not IOException)
                {
                    // Throw an 'IOException' (see notes above). We use a filter here to avoid unnecessarily
                    // re-throwing if we already have an 'IOException', since here we're also calling the
                    // 'OpenStreamForWriteCoreAsync' helper, which will always throw that exception already.
                    WindowsRuntimeIOHelpers.GetExceptionDispatchInfo(exception).Throw();

                    return null;
                }
            }

            return OpenStreamForWriteCoreAsync(rootDirectory, relativePath, creationCollisionOption);
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
                windowsRuntimeFile,
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
            return rootDirectory.CreateSafeFileHandle(relativePath, mode, (mode is FileMode.Append ? FileAccess.Write : FileAccess.ReadWrite));
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
                rootDirectory,
                relativePath,
                mode,
                access,
                share,
                options);
        }

        /// <inheritdoc cref="OpenStreamForWriteAsync(IStorageFile)"/>
        /// <param name="offset">The offset to set in the returned stream.</param>
        [SupportedOSPlatform("windows10.0.10240.0")]
        private static async Task<Stream> OpenStreamForWriteCoreAsync(IStorageFile windowsRuntimeFile, long offset)
        {
            Debug.Assert(windowsRuntimeFile is not null);
            Debug.Assert(offset >= 0);

            try
            {
                IRandomAccessStream windowsRuntimeStream = await windowsRuntimeFile
                    .OpenAsync(FileAccessMode.ReadWrite)
                    .AsTask()
                    .ConfigureAwait(false);

                Stream managedStream = windowsRuntimeStream.AsStreamForWrite();

                managedStream.Position = offset;

                return managedStream;
            }
            catch (Exception exception)
            {
                // Throw an 'IOException' (see notes above)
                WindowsRuntimeIOHelpers.GetExceptionDispatchInfo(exception).Throw();

                return null;
            }
        }
    }
}
