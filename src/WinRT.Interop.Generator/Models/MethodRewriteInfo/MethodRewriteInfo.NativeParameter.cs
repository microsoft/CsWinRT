// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.PE.DotNet.Cil;

namespace WindowsRuntime.InteropGenerator.Models;

/// <inheritdoc cref="MethodRewriteInfo"/>
internal partial class MethodRewriteInfo
{
    /// <summary>
    /// Contains info for a target method for two-pass IL generation, for a native parameter.
    /// </summary>
    /// <see cref="Factories.InteropMethodRewriteFactory.NativeParameter.RewriteMethod"/>
    public sealed class NativeParameter : MethodRewriteInfo
    {
        /// <summary><inheritdoc cref="Factories.InteropMethodRewriteFactory.ReturnValue.RewriteMethod" path="/param[@name='tryMarker']/node()"/></summary>
        public required CilInstruction TryMarker { get; init; }

        /// <summary><inheritdoc cref="Factories.InteropMethodRewriteFactory.ReturnValue.RewriteMethod" path="/param[@name='finallyMarker']/node()"/></summary>
        public required CilInstruction FinallyMarker { get; init; }

        /// <summary><inheritdoc cref="Factories.InteropMethodRewriteFactory.NativeParameter.RewriteMethod" path="/param[@name='parameterIndex']/node()"/></summary>
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
            if (other is not NativeParameter info)
            {
                return typeof(NativeParameter).FullName!.CompareTo(other.GetType().FullName!);
            }

            int result = CompareByMethodRewriteInfo(other);

            // If the two items are already not equal, we can stop here
            if (result != 0)
            {
                return result;
            }

            int leftIndex = Method.CilMethodBody?.Instructions.IndexOf(TryMarker) ?? -1;
            int rightIndex = other.Method.CilMethodBody?.Instructions.IndexOf(info.TryMarker) ?? -1;

            result = leftIndex.CompareTo(rightIndex);

            // Compare by the position of the marker for the 'try' block
            if (result != 0)
            {
                return result;
            }

            leftIndex = Method.CilMethodBody?.Instructions.IndexOf(FinallyMarker) ?? -1;
            rightIndex = other.Method.CilMethodBody?.Instructions.IndexOf(info.FinallyMarker) ?? -1;

            result = leftIndex.CompareTo(rightIndex);

            // Next, compare by the position of the marker for the 'finally' block
            if (result != 0)
            {
                return result;
            }

            // Lastly, compare by parameter index
            return ParameterIndex.CompareTo(info.ParameterIndex);
        }
    }
}
