// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

#pragma warning disable CS1734

namespace WindowsRuntime.SourceGenerator;

/// <summary>
/// Extensions for <see cref="AttributeData"/>.
/// </summary>
internal static class AttributeDataExtensions
{
    /// <param name="attribute">The input <see cref="AttributeData"/> instance.</param>
    extension(AttributeData attribute)
    {
        /// <summary>
        /// Gets the location of the syntax node where the attribute is applied.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token to use.</param>
        /// <returns>The location of the attribute application, or <see langword="null"/> if it cannot be determined.</returns>
        public Location? GetLocation(CancellationToken cancellationToken)
        {
            return attribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken).GetLocation();
        }

        /// <summary>
        /// Gets the location of a specific positional argument of the attribute application.
        /// </summary>
        /// <param name="argumentIndex">The index of the positional argument.</param>
        /// <param name="cancellationToken">The cancellation token to use.</param>
        /// <returns>The location of the argument, or <see langword="null"/> if it cannot be determined.</returns>
        public Location? GetArgumentLocation(int argumentIndex, CancellationToken cancellationToken)
        {
            return attribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken) is AttributeSyntax { ArgumentList.Arguments: { } arguments } && argumentIndex < arguments.Count
                ? arguments[argumentIndex].GetLocation()
                : null;
        }
    }
}
