// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet.Signatures;

namespace WindowsRuntime.InteropGenerator;

/// <summary>
/// Extensions for <see cref="SignatureComparer"/>.
/// </summary>
internal static class SignatureComparerExtensions
{
    /// <summary>
    /// Backing field for <see cref="get_IgnoreVersion"/>.
    /// </summary>
    private static readonly SignatureComparer IgnoreVersion = new(SignatureComparisonFlags.VersionAgnostic);

    extension(SignatureComparer)
    {
        /// <summary>
        /// An immutable default instance of <see cref="SignatureComparer"/>, with <see cref="SignatureComparisonFlags.VersionAgnostic"/>.
        /// </summary>
        public static SignatureComparer IgnoreVersion => IgnoreVersion;
    }
}
