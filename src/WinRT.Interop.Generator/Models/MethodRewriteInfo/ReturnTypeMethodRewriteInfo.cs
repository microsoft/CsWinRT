// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet.Code.Cil;

namespace WindowsRuntime.InteropGenerator.Models;

/// <summary>
/// Contains info for a target method for two-pass IL generation, for a managed return value.
/// </summary>
/// <see cref="Factories.InteropMethodRewriteFactory.ReturnValue.RewriteMethod"/>
internal sealed class ReturnTypeMethodRewriteInfo : MethodRewriteInfo
{
    /// <summary><inheritdoc cref="Factories.InteropMethodRewriteFactory.ReturnValue.RewriteMethod" path="/param[@name='source']/node()"/></summary>
    public required CilLocalVariable Source { get; init; }

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
        if (other is not ReturnTypeMethodRewriteInfo info)
        {
            return typeof(ReturnTypeMethodRewriteInfo).FullName!.CompareTo(other.GetType().FullName!);
        }

        int result = CompareByMethodRewriteInfo(other);

        // If the two items are already not equal, we can stop here
        if (result != 0)
        {
            return result;
        }

        int leftIndex = Method.CilMethodBody?.LocalVariables.IndexOf(Source) ?? -1;
        int rightIndex = other.Method.CilMethodBody?.LocalVariables.IndexOf(info.Source) ?? -1;

        // Lastly, compare by order of instructions within the target method.
        // There's no concern about stable sorting with respect to objects
        // where the instructions are missing, as 'cswinrtgen' will fail.
        return leftIndex.CompareTo(rightIndex);
    }
}
