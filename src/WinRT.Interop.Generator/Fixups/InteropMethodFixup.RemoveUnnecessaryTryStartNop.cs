// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.PE.DotNet.Cil;
using WindowsRuntime.InteropGenerator.Errors;

namespace WindowsRuntime.InteropGenerator.Fixups;

/// <inheritdoc cref="InteropMethodFixup"/>
internal partial class InteropMethodFixup
{
    /// <summary>
    /// A fixup that removes unnecessary <see langword="nop"/> instructions at the start of <see langword="try"/> regions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This fixup finds and removes <see langword="nop"/> instructions that are the starting instruction of a
    /// <see langword="try"/> region. These instructions are unnecessary and can be safely removed to reduce IL size.
    /// Normally, such instructions wouldn't be emitted in the first place, but they might appear as leftover
    /// artifacts of two-pass method rewriting. For instance, <see langword="nop"/>-s are usually used as markers.
    /// </para>
    /// <para>
    /// When a <see langword="nop"/> instruction is removed, all labels pointing to it (from exception handlers
    /// or branch instructions) are adjusted to point to the instruction immediately following the removed one.
    /// </para>
    /// </remarks>
    public sealed class RemoveUnnecessaryTryStartNop : InteropMethodFixup
    {
        /// <summary>
        /// The singleton <see cref="RemoveUnnecessaryTryStartNop"/> instance.
        /// </summary>
        public static readonly RemoveUnnecessaryTryStartNop Instance = new();

        /// <inheritdoc/>
        public override void Apply(MethodDefinition method)
        {
            // Validate that we do have some IL body for the input method (this should always be the case)
            if (method.CilMethodBody is not CilMethodBody body)
            {
                throw WellKnownInteropExceptions.MethodFixupMissingBodyError(method);
            }

            CilInstructionCollection instructions = body.Instructions;

            // Ignore empty methods (they're invalid and will fail on emit anyway)
            if (instructions.Count == 0)
            {
                return;
            }

            // If there are no exception handlers, there are no 'try' regions to check
            if (body.ExceptionHandlers.Count == 0)
            {
                return;
            }

            bool areExceptionHandlersValid = false;
            bool areJumpTargetsValid = false;

            // Process instructions in reverse order to avoid index shifting issues when removing
            for (int i = instructions.Count - 1; i >= 0; i--)
            {
                CilInstruction instruction = instructions[i];

                // Check if this is a 'nop' instruction, otherwise ignore it
                if (instruction.OpCode != CilOpCodes.Nop)
                {
                    continue;
                }

                // Validate exception handlers only once, before checking for problematic 'nop'-s
                if (!areExceptionHandlersValid)
                {
                    ValidateExceptionHandlerLabels(method, body);

                    areExceptionHandlersValid = true;
                }

                // Check if this 'nop' is the start of any 'try' region
                if (!IsTryStartInstruction(body, instruction))
                {
                    continue;
                }

                // Validate branch instructions only once, before patching their labels below
                if (!areJumpTargetsValid)
                {
                    ValidateBranchInstructionLabels(method, body);

                    areJumpTargetsValid = true;
                }

                // Get the next instruction to redirect labels to. There should always be one,
                // as a 'try' block cannot be empty. If missing, we let the emitter fail later.
                CilInstruction? nextInstruction = i + 1 < instructions.Count ? instructions[i + 1] : null;

                // Redirect all labels pointing to this 'nop' to the next instruction
                if (nextInstruction is not null)
                {
                    RedirectLabels(body, instruction, nextInstruction);
                }

                // Remove the unnecessary 'nop' instruction
                instructions.RemoveAt(i);
            }
        }

        /// <summary>
        /// Checks whether the specified instruction is the start of a <see langword="try"/> region.
        /// </summary>
        /// <param name="body">The method body containing the instruction.</param>
        /// <param name="instruction">The instruction to check.</param>
        /// <returns>Whether <paramref name="instruction"/> is the start of a <see langword="try"/> region.</returns>
        private static bool IsTryStartInstruction(CilMethodBody body, CilInstruction instruction)
        {
            foreach (CilExceptionHandler handler in body.ExceptionHandlers)
            {
                // Check if this instruction is the starting instruction of any handler.
                // We can directly cast to 'CilInstructionLabel' as we've already validated.
                if (((CilInstructionLabel)handler.TryStart!).Instruction == instruction)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
