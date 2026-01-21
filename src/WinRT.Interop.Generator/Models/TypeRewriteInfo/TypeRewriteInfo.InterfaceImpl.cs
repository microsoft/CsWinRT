// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.InteropGenerator.Models;

/// <inheritdoc cref="TypeRewriteInfo"/>
internal partial class TypeRewriteInfo
{
    /// <summary>
    /// Contains info for a target type for two-pass IL generation, for interface implementation types.
    /// </summary>
    /// <see cref="Factories.InteropMethodRewriteFactory.Dispose.RewriteMethod"/>
    public sealed class InterfaceImpl : TypeRewriteInfo
    {
        /// <inheritdoc/>
        public override int CompareTo(TypeRewriteInfo? other)
        {
            // 'InterfaceImpl' objects have no additional state, so just compare with the base state
            return ReferenceEquals(this, other)
                ? 0
                : CompareByTypeRewriteInfo<InterfaceImpl>(other);
        }
    }
}
