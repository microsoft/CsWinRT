// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.InteropGenerator.Models;

/// <inheritdoc cref="MethodRewriteInfo"/>
internal partial class MethodRewriteInfo
{
    /// <summary>
    /// Contains info for a target method for two-pass IL generation, for an unmanaged <c>[retval]</c> value.
    /// </summary>
    /// <see cref="Rewriters.InteropMethodRewriter.ReturnValue.RewriteMethod"/>
    public sealed class RetVal : MethodRewriteInfo
    {
        /// <inheritdoc/>
        public override int CompareTo(MethodRewriteInfo? other)
        {
            // 'RetVal' objects have no additional state, so just compare with the base state
            return ReferenceEquals(this, other)
                ? 0
                : CompareByMethodRewriteInfo<RetVal>(other);
        }
    }
}
