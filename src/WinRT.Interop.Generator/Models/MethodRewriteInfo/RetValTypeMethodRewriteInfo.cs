// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.InteropGenerator.Models;

/// <summary>
/// Contains info for a target method for two-pass IL generation, for an unmanaged <c>[retval]</c> value.
/// </summary>
/// <see cref="Factories.InteropMethodRewriteFactory.ReturnValue.RewriteMethod"/>
internal sealed class RetValTypeMethodRewriteInfo : MethodRewriteInfo
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

        // Same logic as in 'ReturnTypeMethodRewriteInfo', or just compare the base state
        return other is not RetValTypeMethodRewriteInfo
            ? typeof(RetValTypeMethodRewriteInfo).FullName!.CompareTo(other.GetType().FullName!)
            : CompareByMethodRewriteInfo(other);
    }
}
