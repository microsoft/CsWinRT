// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.PE.DotNet.Cil;

namespace WindowsRuntime.InteropGenerator;

/// <summary>
/// Extensions for <see cref="CilInstructionCollection"/>.
/// </summary>
internal static class CilInstructionCollectionExtensions
{
    extension(CilInstructionCollection instructions)
    {
        /// <summary>
        /// Replaces a target instruction with a collection of new instructions.
        /// </summary>
        /// <param name="target">The instruction to replace.</param>
        /// <param name="values">The new instructions to emit.</param>
        public void ReplaceRange(CilInstruction target, params IEnumerable<CilInstruction> values)
        {
            int index = instructions.ReferenceIndexOf(target);

            // Ensure we did find the target instruction
            if (index == -1)
            {
                throw new ArgumentException("The target instruction was not found in the collection.", nameof(target));
            }

            instructions.RemoveAt(index);
            instructions.InsertRange(index, values);
        }
    }
}