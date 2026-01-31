// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.PE.DotNet.Cil;
using WindowsRuntime.InteropGenerator.Errors;

namespace WindowsRuntime.InteropGenerator.Fixups;

/// <inheritdoc cref="InteropMethodFixup"/>
internal partial class InteropMethodFixup
{
    /// <summary>
    /// A fixup that removes invalid <see langword="nop"/> instructions within protected regions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This fixup finds and removes <see langword="nop"/> instructions that are located within a protected region
    /// and that cause the method body to be invalid. This happens when they follow an instructions that does not
    /// fall through (i.e. <see langword="leave"/>, <see langword="endfinally"/>, <see langword="endfilter"/>, or
    /// <see langword="throw"/>), within the same protected region. According to ECMA-335 rules, instructions cannot
    /// follow those non fall through instructions that delimit a protected region.
    /// </para>
    /// <para>
    /// When a <see langword="nop"/> instruction is removed, all labels pointing to it (from exception handlers
    /// or branch instructions) are adjusted to point to the instruction immediately following the removed one.
    /// </para>
    /// </remarks>
    public sealed class RemoveLeftoverNopAfterLeave : InteropMethodFixup
    {
        /// <summary>
        /// The singleton <see cref="RemoveLeftoverNopAfterLeave"/> instance.
        /// </summary>
        public static readonly RemoveLeftoverNopAfterLeave Instance = new();

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

                // Make sure that we do have some instruction before this. If that's not the case,
                // we can't possibly follow a problematic instruction, so there's nothing to do.
                if (i == 0)
                {
                    continue;
                }

                // Validate exception handlers only once, before checking for problematic 'nop'-s
                if (!areExceptionHandlersValid)
                {
                    ValidateExceptionHandlerLabels(method, body);

                    areExceptionHandlersValid = true;
                }

                // Check if this 'nop' is within a protected region ('try' block or handler)
                if (!IsWithinProtectedRegion(body, instruction))
                {
                    continue;
                }

                // Check if the previous instruction does not fall through
                if (!IsNonFallThroughInstruction(instructions[i - 1]))
                {
                    continue;
                }

                // Validate branch instructions only once, before patching their labels below
                if (!areJumpTargetsValid)
                {
                    ValidateBranchInstructionLabels(method, body);

                    areJumpTargetsValid = true;
                }

                // Get the next instruction to redirect labels to (if any). There should always
                // be one, otherwise the method would be invalid. If that's the case, we don't
                // need to validate here, we can just let the emitter fail later.
                CilInstruction? nextInstruction = i + 1 < instructions.Count ? instructions[i + 1] : null;

                // Redirect all labels pointing to this 'nop' to the next instruction
                if (nextInstruction is not null)
                {
                    RedirectLabels(body, instruction, nextInstruction);
                }

                // Remove the invalid 'nop' instruction
                instructions.RemoveAt(i);
            }
        }

        /// <summary>
        /// Validates the exception handler labels for the specified method.
        /// </summary>
        /// <param name="method">The method to validate.</param>
        /// <param name="body">The method body to validate.</param>
        private static void ValidateExceptionHandlerLabels(MethodDefinition method, CilMethodBody body)
        {
            // We only support handlers that use instruction labels. We never manually patch
            // labels that use explicit offsets, as those haven't been computed at this point.
            // Additionally, filters should never be used by any generated interop methods.
            foreach (CilExceptionHandler handler in body.ExceptionHandlers)
            {
                if (handler is not
                    {
                        TryStart: CilInstructionLabel { Instruction: not null },
                        TryEnd: CilInstructionLabel { Instruction: not null },
                        HandlerStart: CilInstructionLabel { Instruction: not null },
                        HandlerEnd: CilInstructionLabel { Instruction: not null },
                        FilterStart: null
                    })
                {
                    throw WellKnownInteropExceptions.MethodFixupInvalidExceptionHandlerLabels(method);
                }
            }
        }

        /// <summary>
        /// Validates the exception handler labels for the specified method.
        /// </summary>
        /// <param name="method">The method to validate.</param>
        /// <param name="body">The method body to validate.</param>
        private static void ValidateBranchInstructionLabels(MethodDefinition method, CilMethodBody body)
        {
            foreach (CilInstruction instruction in body.Instructions)
            {
                // Make sure that the operand of all branch instruction is a valid instruction label.
                // This is the same exact validation we also did above for all exception handlers.
                if (instruction.Operand is ICilLabel and not CilInstructionLabel { Instruction: not null })
                {
                    throw WellKnownInteropExceptions.MethodFixupInvalidBranchInstructionLabels(method);
                }

                // Handle 'switch' instructions too (their operands are multiple branch targets)
                if (instruction.Operand is IEnumerable<ICilLabel> labels)
                {
                    foreach (ICilLabel label in labels)
                    {
                        if (label is not CilInstructionLabel { Instruction: not null })
                        {
                            throw WellKnownInteropExceptions.MethodFixupInvalidBranchInstructionLabels(method);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Checks whether the specified instruction does not fall through to the next instruction.
        /// </summary>
        /// <param name="instruction">The instruction to check.</param>
        /// <returns>Whether <paramref name="instruction"/> does not fall through.</returns>
        private static bool IsNonFallThroughInstruction(CilInstruction instruction)
        {
            CilOpCode opCode = instruction.OpCode;

            // Note: we only care about checking for problematic instructions for protected regions
            return opCode == CilOpCodes.Leave ||
                   opCode == CilOpCodes.Leave_S ||
                   opCode == CilOpCodes.Endfinally ||
                   opCode == CilOpCodes.Endfilter ||
                   opCode == CilOpCodes.Throw ||
                   opCode == CilOpCodes.Rethrow;
        }

        /// <summary>
        /// Checks whether the specified instruction is within a protected region (e.g. a <see langword="try"/> block).
        /// </summary>
        /// <param name="body">The method body containing the instruction.</param>
        /// <param name="instruction">The instruction to check.</param>
        /// <returns>Whether <paramref name="instruction"/> is within a protected region.</returns>
        private static bool IsWithinProtectedRegion(CilMethodBody body, CilInstruction instruction)
        {
            foreach (CilExceptionHandler handler in body.ExceptionHandlers)
            {
                // Check if instruction is within the 'try' block. Note that we can directly cast
                // to 'CilInstructionLabel' here, as we've already validated the exception handlers.
                if (IsInstructionInRange(body, instruction, (CilInstructionLabel)handler.TryStart!, (CilInstructionLabel)handler.TryEnd!))
                {
                    return true;
                }

                // Check if instruction is within the handler block
                if (IsInstructionInRange(body, instruction, (CilInstructionLabel)handler.HandlerStart!, (CilInstructionLabel)handler.HandlerEnd!))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks whether an instruction falls within a specified range.
        /// </summary>
        /// <param name="body">The method body containing the instruction.</param>
        /// <param name="instruction">The instruction to check.</param>
        /// <param name="start">The start label of the range.</param>
        /// <param name="end">The end label of the range (exclusive).</param>
        /// <returns>Whether <paramref name="instruction"/> is between the <paramref name="start"/> and <paramref name="end"/> labels.</returns>
        private static bool IsInstructionInRange(
            CilMethodBody body,
            CilInstruction instruction,
            CilInstructionLabel start,
            CilInstructionLabel end)
        {
            int instructionIndex = body.Instructions.ReferenceIndexOf(instruction);
            int startIndex = body.Instructions.ReferenceIndexOf(start.Instruction!);
            int endIndex = body.Instructions.ReferenceIndexOf(end.Instruction!);

            return instructionIndex >= startIndex && instructionIndex < endIndex;
        }

        /// <summary>
        /// Redirects all labels pointing to the old instruction to point to the new instruction instead.
        /// </summary>
        /// <param name="body">The method body to update.</param>
        /// <param name="oldInstruction">The instruction that labels currently point to.</param>
        /// <param name="newInstruction">The instruction that labels should point to after redirection.</param>
        private static void RedirectLabels(CilMethodBody body, CilInstruction oldInstruction, CilInstruction newInstruction)
        {
            ICilLabel newLabel = newInstruction.CreateLabel();

            // Update exception handler labels (they've been validated before already)
            foreach (CilExceptionHandler handler in body.ExceptionHandlers)
            {
                if (((CilInstructionLabel)handler.TryStart!).Instruction == oldInstruction)
                {
                    handler.TryStart = newLabel;
                }

                if (((CilInstructionLabel)handler.TryEnd!).Instruction == oldInstruction)
                {
                    handler.TryEnd = newLabel;
                }

                if (((CilInstructionLabel)handler.HandlerStart!).Instruction == oldInstruction)
                {
                    handler.HandlerStart = newLabel;
                }

                if (((CilInstructionLabel)handler.HandlerEnd!).Instruction == oldInstruction)
                {
                    handler.HandlerEnd = newLabel;
                }
            }

            // Update branch instruction operands
            foreach (CilInstruction instruction in body.Instructions)
            {
                // Handle single branch target
                if (instruction.Operand is CilInstructionLabel label && label.Instruction == oldInstruction)
                {
                    instruction.Operand = newLabel;
                }
                // Handle switch instruction (multiple branch targets)
                else if (instruction.Operand is System.Collections.Generic.IList<ICilLabel> labels)
                {
                    for (int i = 0; i < labels.Count; i++)
                    {
                        if (labels[i] is CilInstructionLabel switchLabel && switchLabel.Instruction == oldInstruction)
                        {
                            labels[i] = newLabel;
                        }
                    }
                }
            }
        }
    }
}
