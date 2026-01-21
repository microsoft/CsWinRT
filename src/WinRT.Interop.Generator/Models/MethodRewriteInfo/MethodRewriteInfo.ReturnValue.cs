// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet.Code.Cil;

namespace WindowsRuntime.InteropGenerator.Models;

/// <inheritdoc cref="MethodRewriteInfo"/>
internal partial class MethodRewriteInfo
{
    /// <summary>
    /// Contains info for a target method for two-pass IL generation, for a managed return value.
    /// </summary>
    /// <see cref="Rewriters.InteropMethodRewriter.ReturnValue.RewriteMethod"/>
    public sealed class ReturnValue : MethodRewriteInfo
    {
        /// <summary><inheritdoc cref="Rewriters.InteropMethodRewriter.ReturnValue.RewriteMethod" path="/param[@name='source']/node()"/></summary>
        public required CilLocalVariable Source { get; init; }

        /// <inheritdoc/>
        public override int CompareTo(MethodRewriteInfo? other)
        {
            if (ReferenceEquals(this, other))
            {
                return 0;
            }

            int result = CompareByMethodRewriteInfo<ReturnValue>(other);

            // First, compare with the base state
            if (result != 0)
            {
                return result;
            }

            int leftIndex = Method.CilMethodBody?.LocalVariables.IndexOf(Source) ?? -1;
            int rightIndex = other!.Method.CilMethodBody?.LocalVariables.IndexOf(((ReturnValue)other).Source) ?? -1;

            // Lastly, compare by the order of the source variable
            return leftIndex.CompareTo(rightIndex);
        }
    }
}
