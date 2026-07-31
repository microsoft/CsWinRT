// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet.Signatures;

namespace WindowsRuntime.Generator;

/// <summary>
/// Extensions for <see cref="SignatureComparer"/>.
/// </summary>
internal static class SignatureComparerExtensions
{
    /// <summary>
    /// Backing field for the <see cref="IgnoreVersion"/> extension property.
    /// </summary>
    private static readonly SignatureComparer IgnoreVersion = new(SignatureComparisonFlags.VersionAgnostic);

    extension(SignatureComparer)
    {
        /// <summary>
        /// Gets a shared, version-agnostic <see cref="SignatureComparer"/>, suitable for comparing
        /// AsmResolver entities (e.g. as the <see cref="System.Collections.Generic.IEqualityComparer{T}"/>
        /// for a <see cref="System.Collections.Generic.HashSet{T}"/> of <see cref="AsmResolver.DotNet.TypeDefinition"/>).
        /// </summary>
        public static SignatureComparer IgnoreVersion => IgnoreVersion;
    }
}
