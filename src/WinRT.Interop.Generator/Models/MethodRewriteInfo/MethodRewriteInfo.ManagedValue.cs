// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.InteropGenerator.Models;

/// <inheritdoc cref="MethodRewriteInfo"/>
internal partial class MethodRewriteInfo
{
    /// <summary>
    /// Contains info for a target method for two-pass IL generation, for a managed value.
    /// </summary>
    /// <see cref="Factories.InteropMethodRewriteFactory.ManagedParameter.RewriteMethod"/>
    public sealed class ManagedValue : MethodRewriteInfo
    {
        /// <inheritdoc/>
        public override int CompareTo(MethodRewriteInfo? other)
        {
            // 'ManagedValue' objects have no additional state, so just compare with the base state
            return ReferenceEquals(this, other)
                ? 0
                : CompareByMethodRewriteInfo<ManagedValue>(other);
        }
    }
}
