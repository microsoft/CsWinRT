// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.PE.DotNet.Cil;
using WindowsRuntime.InteropGenerator.Errors;

namespace WindowsRuntime.InteropGenerator.Fixups;

/// <summary>
/// A type that can apply custom fixups to generated methods, as a last processing step.
/// </summary>
internal abstract partial class InteropMethodFixup
{
    /// <summary>
    /// Applies the current fixup to a target method.
    /// </summary>
    /// <param name="method">The target method to apply the fixup to.</param>
    public abstract void Apply(MethodDefinition method);

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
    /// Validates the branch instruction labels for the specified method.
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
