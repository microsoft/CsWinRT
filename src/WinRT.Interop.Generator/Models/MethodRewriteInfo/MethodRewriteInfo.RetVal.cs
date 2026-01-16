// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.InteropGenerator.Models;

/// <inheritdoc cref="MethodRewriteInfo"/>
internal partial class MethodRewriteInfo
{
    /// <summary>
    /// Contains info for a target method for two-pass IL generation, for an unmanaged <c>[retval]</c> value.
    /// </summary>
    /// <see cref="Factories.InteropMethodRewriteFactory.ReturnValue.RewriteMethod"/>
    public sealed class RetVal : MethodRewriteInfo
    {
        /// <inheritdoc/>
        public override int CompareTo(MethodRewriteInfo? other)
        {
            if (other is null)
            {
                return 1;
            }

            if (ReferenceEquals(this, other))
            {
                return 0;
            }

            // Same logic as in 'ReturnValue', or just compare the base state
            return other is not RetVal
                ? typeof(RetVal).FullName!.CompareTo(other.GetType().FullName!)
                : CompareByMethodRewriteInfo(other);
        }
    }
}
