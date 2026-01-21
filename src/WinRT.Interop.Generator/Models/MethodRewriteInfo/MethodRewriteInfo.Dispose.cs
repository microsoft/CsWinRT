// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.InteropGenerator.Models;

/// <inheritdoc cref="MethodRewriteInfo"/>
internal partial class MethodRewriteInfo
{
    /// <summary>
    /// Contains info for a target method for two-pass IL generation, to dispose (or release) a given value.
    /// </summary>
    /// <see cref="Rewriters.InteropMethodRewriter.Dispose.RewriteMethod"/>
    public sealed class Dispose : MethodRewriteInfo
    {
        /// <inheritdoc/>
        public override int CompareTo(MethodRewriteInfo? other)
        {
            // 'Dispose' objects have no additional state, so just compare with the base state
            return ReferenceEquals(this, other)
                ? 0
                : CompareByMethodRewriteInfo<Dispose>(other);
        }
    }
}
