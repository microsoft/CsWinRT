// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using AsmResolver.PE.DotNet.Cil;
using WindowsRuntime.InteropGenerator.Helpers;

namespace WindowsRuntime.InteropGenerator.Models;

/// <inheritdoc cref="MethodRewriteInfo"/>
internal partial class MethodRewriteInfo
{
    /// <summary>
    /// Contains info for a target method for two-pass IL generation, for a native parameter.
    /// </summary>
    /// <see cref="Rewriters.InteropMethodRewriter.NativeParameter.RewriteMethod"/>
    public sealed class NativeParameter : MethodRewriteInfo
    {
        /// <summary><inheritdoc cref="Rewriters.InteropMethodRewriter.ReturnValue.RewriteMethod" path="/param[@name='tryMarker']/node()"/></summary>
        public required CilInstruction TryMarker { get; init; }

        /// <summary><inheritdoc cref="Rewriters.InteropMethodRewriter.ReturnValue.RewriteMethod" path="/param[@name='finallyMarker']/node()"/></summary>
        public required CilInstruction FinallyMarker { get; init; }

        /// <summary><inheritdoc cref="Rewriters.InteropMethodRewriter.NativeParameter.RewriteMethod" path="/param[@name='parameterIndex']/node()"/></summary>
        public required int ParameterIndex { get; init; }

        /// <inheritdoc/>
        public override int CompareTo(MethodRewriteInfo? other)
        {
            if (other is null)
            {
                return 1;
            }

            // If the input object is of a different type, just sort alphabetically based on the type name
            if (other is not NativeParameter info)
            {
                return typeof(NativeParameter).FullName!.CompareTo(other.GetType().FullName!);
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

            ReadOnlySpan<CilInstruction> markers = [TryMarker, Marker, FinallyMarker];
            ReadOnlySpan<CilInstruction> otherMarkers = [info.TryMarker, info.Marker, info.FinallyMarker];

            // Next, compare by order of instructions within the target method
            for (int i = 0; i < markers.Length; i++)
            {
                int leftIndex = Method.CilMethodBody?.Instructions.ReferenceIndexOf(markers[i]) ?? -1;
                int rightIndex = other.Method.CilMethodBody?.Instructions.ReferenceIndexOf(otherMarkers[i]) ?? -1;

                result = leftIndex.CompareTo(rightIndex);

                if (result != 0)
                {
                    return result;
                }
            }

            // Lastly, compare by target type (this shouldn't be reached for valid objects)
            return TypeDescriptorComparer.Instance.Compare(Type, other.Type);
        }
    }
}
