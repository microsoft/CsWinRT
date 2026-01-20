// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using WindowsRuntime.InteropGenerator.Helpers;

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

            // If the input object is of a different type, just sort alphabetically based on the type name
            if (other is not ManagedParameter info)
            {
                return typeof(ManagedParameter).FullName!.CompareTo(other.GetType().FullName!);
            }

            int result = MemberDefinitionComparer.Instance.Compare(Method, other.Method);

            // First, sort by target method
            if (result != 0)
            {
                return result;
            }

            // Next, sort by parameter index
            result = ParameterIndex.CompareTo(info.ParameterIndex);

            if (result != 0)
            {
                return result;
            }

            // Next, compare by order of instructions within the target method
            int leftIndex = Method.CilMethodBody?.Instructions.ReferenceIndexOf(Marker) ?? -1;
            int rightIndex = other.Method.CilMethodBody?.Instructions.ReferenceIndexOf(other.Marker) ?? -1;

            result = leftIndex.CompareTo(rightIndex);

            if (result != 0)
            {
                return result;
            }

            // Lastly, compare by target type (this shouldn't be reached for valid objects)
            return TypeDescriptorComparer.Instance.Compare(Type, other.Type);
        }
    }
}
