// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Buffers;
using System.IO;
using System.Security.Cryptography;

namespace WindowsRuntime.InteropGenerator;

/// <summary>
/// Extensions for the <see cref="IncrementalHash"/> type.
/// </summary>
internal static class IncrementalHashExtensions
{
    extension(IncrementalHash hash)
    {
        /// <summary>
        /// Appends data from an input <see cref="Stream"/> to the data already processed in the hash or HMAC.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to process.</param>
        public void AppendData(Stream stream)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(4096);

            int read;

            // Read the contents in chunks and append them to the hash
            while ((read = stream.Read(buffer)) > 0)
            {
                hash.AppendData(buffer, 0, read);
            }

            // Always return the pooled array
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}