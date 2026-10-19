// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;

namespace WindowsRuntime.InteropGenerator.Models;

/// <summary>A method operand together with the dispatch information encoded by its instruction.</summary>
/// <param name="Descriptor">The referenced method.</param>
/// <param name="Constraint">The receiver constraint from a preceding 'constrained.' prefix, if any.</param>
/// <param name="IsVirtual">Whether the instruction performs virtual dispatch.</param>
/// <param name="IsStaticDispatch">Whether a constrained 'call' or 'ldftn' can select a static implementation.</param>
internal readonly record struct MethodOperand(
    IMethodDescriptor Descriptor,
    ITypeDefOrRef? Constraint,
    bool IsVirtual,
    bool IsStaticDispatch);
