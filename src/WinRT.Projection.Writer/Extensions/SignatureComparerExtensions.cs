// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet.Signatures;

namespace WindowsRuntime.ProjectionWriter;

/// <summary>
/// Extensions for <see cref="SignatureComparer"/>.
/// </summary>
internal static class SignatureComparerExtensions
{
#pragma warning disable IDE0052 // TODO: remove this once Roslyn bug is fixed
    /// <summary>
    /// Backing field for the <see cref="IgnoreVersion"/> extension property.
    /// </summary>
    private static readonly SignatureComparer IgnoreVersion = new(SignatureComparisonFlags.VersionAgnostic);
#pragma warning restore IDE0052

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
