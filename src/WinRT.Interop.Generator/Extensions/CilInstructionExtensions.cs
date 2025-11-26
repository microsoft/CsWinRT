// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet.Code.Cil;
using AsmResolver.PE.DotNet.Cil;
using static AsmResolver.PE.DotNet.Cil.CilOpCodes;

namespace WindowsRuntime.InteropGenerator;

/// <summary>
/// Extensions for <see cref="CilInstruction"/>.
/// </summary>
internal static class CilInstructionExtensions
{
    extension(CilInstruction)
    {
        /// <summary>
        /// Create a new instruction loading an argument with the provided index, using the smallest possible operation code and operand size.
        /// </summary>
        /// <param name="index">The index of the argument to load.</param>
        /// <returns>The instruction.</returns>
        public static CilInstruction CreateLdarg(int index)
        {
            return index switch
            {
                0 => new CilInstruction(Ldarg_0),
                1 => new CilInstruction(Ldarg_1),
                2 => new CilInstruction(Ldarg_2),
                3 => new CilInstruction(Ldarg_3),
                < 256 => new CilInstruction(Ldarg_S, (byte)index),
                _ => new CilInstruction(Ldarg, index)
            };
        }

        /// <summary>
        /// Create a new instruction loading a local from a given method, using the smallest possible operation code and operand size.
        /// </summary>
        /// <param name="local">The local to load.</param>
        /// <param name="method">The containing method body.</param>
        /// <returns>The instruction.</returns>
        public static CilInstruction CreateLdloc(CilLocalVariable local, CilMethodBody method)
        {
            return method.LocalVariables.IndexOf(local) switch
            {
                0 => new CilInstruction(Ldloc_0),
                1 => new CilInstruction(Ldloc_1),
                2 => new CilInstruction(Ldloc_2),
                3 => new CilInstruction(Ldloc_3),
                < 256 and int i => new CilInstruction(Ldloc_S, (byte)i),
                int i => new CilInstruction(Ldloc, i)
            };
        }
    }
}