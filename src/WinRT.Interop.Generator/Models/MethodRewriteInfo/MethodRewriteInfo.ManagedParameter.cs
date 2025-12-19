// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.InteropGenerator.Models;

/// <inheritdoc cref="MethodRewriteInfo"/>
internal partial class MethodRewriteInfo
{
    /// <summary>
    /// Contains info for a target method for two-pass IL generation, for a managed parameter.
    /// </summary>
    /// <see cref="Factories.InteropMethodRewriteFactory.ManagedParameter.RewriteMethod"/>
    public sealed class ManagedParameter : MethodRewriteInfo
    {
        /// <summary><inheritdoc cref="Factories.InteropMethodRewriteFactory.ManagedParameter.RewriteMethod" path="/param[@name='parameterIndex']/node()"/></summary>
        public required int ParameterIndex { get; init; }

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

            // If the input object is of a different type, just sort alphabetically based on the type name
            if (other is not ManagedParameter info)
            {
                return typeof(ManagedParameter).FullName!.CompareTo(other.GetType().FullName!);
            }

            int result = CompareByMethodRewriteInfo(other);

            // If the two items are already not equal, we can stop here
            if (result != 0)
            {
                return result;
            }

            // Lastly, compare by parameter index
            return ParameterIndex.CompareTo(info.ParameterIndex);
        }
    }
}
