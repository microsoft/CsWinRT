// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using Microsoft.CodeAnalysis.Testing;

namespace WindowsRuntime.SourceGenerator;

/// <summary>
/// Extensions for the <see cref="ReferenceAssemblies"/> type.
/// </summary>
internal static class ReferenceAssembliesExtensions
{
    /// <summary>
    /// The lazy-loaded <see cref="ReferenceAssemblies"/> instance for .NET 10 assemblies.
    /// </summary>
    private static readonly Lazy<ReferenceAssemblies> Net100 = new(static () => new(
        targetFramework: "net10.0",
        referenceAssemblyPackage: new PackageIdentity("Microsoft.NETCore.App.Ref", "10.0.1"),
        referenceAssemblyPath: Path.Combine("ref", "net10.0")));

    extension(ReferenceAssemblies.Net)
    {
        /// <summary>
        /// Gets the <see cref="ReferenceAssemblies"/> value for .NET 10 reference assemblies.
        /// </summary>
        public static ReferenceAssemblies Net100 => Net100.Value; // TODO: remove when https://github.com/dotnet/roslyn-sdk/issues/1233 is resolved
    }
}