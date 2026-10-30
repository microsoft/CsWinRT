// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.CodeAnalysis;

namespace WindowsRuntime.SourceGenerator;

/// <summary>
/// Extensions for <see cref="ParseOptions"/>.
/// </summary>
internal static class ParseOptionsExtensions
{
    extension(ParseOptions options)
    {
        /// <summary>
        /// Checks whether the compiler allows interceptors to be declared in a given namespace.
        /// </summary>
        /// <param name="namespaceName">The namespace to check (e.g. <c>"WindowsRuntime.Generated"</c>).</param>
        /// <returns>Whether interceptors are allowed in <paramref name="namespaceName"/>.</returns>
        /// <remarks>
        /// <para>
        /// The compiler reads the allowed namespaces from the <c>InterceptorsNamespaces</c> feature (the <c>InterceptorsPreviewNamespaces</c>
        /// MSBuild property is an alias that the compiler task merges into it). It's a <c>';'</c>-separated list, where each entry enables
        /// interceptors in that namespace and all its nested ones. Entries are compared by namespace segment, so <c>"WindowsRuntime"</c>
        /// enables <c>"WindowsRuntime.Generated"</c>, but <c>"Windows"</c> does not.
        /// </para>
        /// <para>
        /// Interceptors declared in a namespace that isn't allowed are a compiler error, so this is a cheap precondition for generating
        /// them. It can be checked from a syntax node, whereas the analyzer config options cannot. The compiler checks the options of the
        /// generated tree (the driver's parse options), which are the same as the ones of the source trees in all supported hosts.
        /// </para>
        /// </remarks>
        public bool IsInterceptorsNamespaceEnabled(ReadOnlySpan<char> namespaceName)
        {
            if (!options.Features.TryGetValue("InterceptorsNamespaces", out string? namespaces))
            {
                return false;
            }

            foreach (Range range in namespaces.AsSpan().Split(';'))
            {
                ReadOnlySpan<char> entry = namespaces.AsSpan()[range];

                if (entry.IsEmpty)
                {
                    continue;
                }

                // The entry is enabled if it's the namespace itself, or one of its parents (matched on whole segments)
                if (entry.SequenceEqual("global") ||
                    (namespaceName.StartsWith(entry, StringComparison.Ordinal) &&
                     (namespaceName.Length == entry.Length || namespaceName[entry.Length] == '.')))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
