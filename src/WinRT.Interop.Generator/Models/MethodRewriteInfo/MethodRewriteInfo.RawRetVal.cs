// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.InteropGenerator.Models;

/// <inheritdoc cref="MethodRewriteInfo"/>
internal partial class MethodRewriteInfo
{
    /// <summary>
    /// Contains info for a target method for two-pass IL generation, for for emitting direct calls to <c>ConvertToUnmanaged</c>.
    /// </summary>
    /// <see cref="Factories.InteropMethodRewriteFactory.RawRetVal.RewriteMethod"/>
    public sealed class RawRetVal : MethodRewriteInfo
    {
        /// <inheritdoc/>
        public override int CompareTo(MethodRewriteInfo? other)
        {
            // 'RawRetVal' objects have no additional state, so just compare with the base state
            return ReferenceEquals(this, other)
                ? 0
                : CompareByMethodRewriteInfo<RawRetVal>(other);
        }
    }
}
