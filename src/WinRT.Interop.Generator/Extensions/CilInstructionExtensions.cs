// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables;
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
        /// Create a new instruction storing a local from a given method, using the smallest possible operation code and operand size.
        /// </summary>
        /// <param name="local">The local to store.</param>
        /// <param name="method">The containing method body.</param>
        /// <returns>The instruction.</returns>
        public static CilInstruction CreateStloc(CilLocalVariable local, CilMethodBody method)
        {
            return method.LocalVariables.IndexOf(local) switch
            {
                0 => new CilInstruction(Stloc_0),
                1 => new CilInstruction(Stloc_1),
                2 => new CilInstruction(Stloc_2),
                3 => new CilInstruction(Stloc_3),
                < 256 and int i => new CilInstruction(Stloc_S, (byte)i),
                int i => new CilInstruction(Stloc, i)
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

        /// <summary>
        /// Create a new instruction storing a value indirectly to a target location.
        /// </summary>
        /// <param name="type">The type of value to store.</param>
        /// <returns>The instruction.</returns>
        [SuppressMessage("Style", "IDE0072", Justification = "We use 'stobj' for all other possible types.")]
        public static CilInstruction CreateStind(TypeSignature type)
        {
            return type.ElementType switch
            {
                ElementType.Boolean => new CilInstruction(Stind_I1),
                ElementType.Char => new CilInstruction(Stind_I2),
                ElementType.I1 => new CilInstruction(Stind_I1),
                ElementType.U1 => new CilInstruction(Stind_I1),
                ElementType.I2 => new CilInstruction(Stind_I2),
                ElementType.U2 => new CilInstruction(Stind_I2),
                ElementType.I4 => new CilInstruction(Stind_I4),
                ElementType.U4 => new CilInstruction(Stind_I4),
                ElementType.I8 => new CilInstruction(Stind_I8),
                ElementType.U8 => new CilInstruction(Stind_I8),
                ElementType.R4 => new CilInstruction(Stind_R4),
                ElementType.R8 => new CilInstruction(Stind_R8),
                ElementType.ValueType when type.Resolve() is { IsClass: true, IsEnum: true } => new CilInstruction(Stind_I4),
                ElementType.I => new CilInstruction(Stind_I),
                _ => new CilInstruction(Stobj, type.ToTypeDefOrRef()),
            };
        }

        /// <summary>
        /// Create a new instruction loading a value indirectly from a target location.
        /// </summary>
        /// <param name="type">The type of value to load.</param>
        /// <returns>The instruction.</returns>
        [SuppressMessage("Style", "IDE0072", Justification = "We use 'ldobj' for all other possible types.")]
        public static CilInstruction CreateLdind(TypeSignature type)
        {
            return type.ElementType switch
            {
                ElementType.Boolean => new CilInstruction(Ldind_I1),
                ElementType.Char => new CilInstruction(Ldind_I2),
                ElementType.I1 => new CilInstruction(Ldind_I1),
                ElementType.U1 => new CilInstruction(Ldind_I1),
                ElementType.I2 => new CilInstruction(Ldind_I2),
                ElementType.U2 => new CilInstruction(Ldind_I2),
                ElementType.I4 => new CilInstruction(Ldind_I4),
                ElementType.U4 => new CilInstruction(Ldind_I4),
                ElementType.I8 => new CilInstruction(Ldind_I8),
                ElementType.U8 => new CilInstruction(Ldind_I8),
                ElementType.R4 => new CilInstruction(Ldind_R4),
                ElementType.R8 => new CilInstruction(Ldind_R8),
                ElementType.ValueType when type.Resolve() is { IsClass: true, IsEnum: true } => new CilInstruction(Ldind_I4),
                ElementType.I => new CilInstruction(Ldind_I),
                _ => new CilInstruction(Ldobj, type.ToTypeDefOrRef()),
            };
        }
    }
}