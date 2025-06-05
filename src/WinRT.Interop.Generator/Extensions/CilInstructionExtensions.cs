// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.PE.DotNet.Cil;

namespace WindowsRuntime.InteropGenerator;

/// <summary>
/// Extensions for <see cref="CilInstruction"/>.
/// </summary>
internal static class CilInstructionExtensions
{
    extension(CilInstruction)
    {
        /// <summary>
        /// Create a new instruction loading a local with the provided index, using the smallest possible operation code and operand size.
        /// </summary>
        /// <param name="index">The index of the local to load.</param>
        /// <returns>The instruction.</returns>
        public static CilInstruction CreateLdloc(int index)
        {
            return index switch
            {
                0 => new CilInstruction(CilOpCodes.Ldloc_0),
                1 => new CilInstruction(CilOpCodes.Ldloc_1),
                2 => new CilInstruction(CilOpCodes.Ldloc_2),
                3 => new CilInstruction(CilOpCodes.Ldloc_3),
                _ when index < 256 => new CilInstruction(CilOpCodes.Ldloc_S, (byte)index),
                _ => new CilInstruction(CilOpCodes.Ldloc, index)
            };
        }
    }
}
