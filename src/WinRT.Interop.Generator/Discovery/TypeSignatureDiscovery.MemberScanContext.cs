// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.InteropGenerator.Visitors;

namespace WindowsRuntime.InteropGenerator.Discovery;

/// <inheritdoc cref="TypeSignatureDiscovery{TResult}"/>
internal partial class TypeSignatureDiscovery<TResult>
{
    /// <summary>
    /// Checks whether an ordinary member transition reuses its caller's module and closed substitution.
    /// </summary>
    /// <param name="scanContext">The member scan that exposed this context, or null for independent work.</param>
    /// <param name="module">The resolved declaring module of the candidate.</param>
    /// <param name="arguments">The candidate's ordered type and method arguments.</param>
    /// <param name="isConstructed">Whether the candidate arguments are already known to be fully constructed.</param>
    /// <returns>Whether the candidate can share its caller's reservation.</returns>
    private bool CanShareMemberReservation(
        MemberScanContext? scanContext,
        ModuleDefinition? module,
        IList<TypeSignature> arguments,
        bool isConstructed = false)
    {
        // Independent work cannot borrow a reservation from an ordinary member scan
        if (scanContext is null)
        {
            return false;
        }

        // Adding or removing arguments introduces a different substitution
        if (arguments.Count != scanContext.ArgumentCount)
        {
            return false;
        }

        // Sharing must stay within the finite metadata of the module that is already being scanned
        if (!ReferenceEquals(scanContext.Module, module))
        {
            return false;
        }

        for (int i = 0; i < arguments.Count; i++)
        {
            // Preserve argument identity and order so expanding substitutions still consume reservations
            if (!_signatureComparer.Equals(arguments[i], scanContext[i]))
            {
                return false;
            }

            // Partially open method contexts still require normal accounting
            if (!isConstructed && !arguments[i].AcceptVisitor(IsConstructedGenericTypeVisitor.Instance))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Creates shared provenance once per work item, not once per scanned method or signature.
    /// </summary>
    /// <param name="module">The declaring module of the member being expanded.</param>
    /// <param name="context">The type and method arguments applied to its members.</param>
    /// <returns>The scan provenance, or <see langword="null"/> if the substitution cannot be shared.</returns>
    /// <remarks>
    /// Missing modules and empty or partially open substitutions cannot share reservations, so they return <see langword="null"/> without allocating.
    /// </remarks>
    private static MemberScanContext? CreateMemberScanContext(
        ModuleDefinition? module,
        GenericContext context)
    {
        // Avoid allocating provenance when there is no module or generic substitution to share
        if (module is null || (context.Type?.TypeArguments.Count ?? 0) + (context.Method?.TypeArguments.Count ?? 0) == 0)
        {
            return null;
        }

        // Unbound declaring-type parameters prevent the complete substitution from being shared
        foreach (TypeSignature argument in context.Type?.TypeArguments ?? [])
        {
            if (!argument.AcceptVisitor(IsConstructedGenericTypeVisitor.Instance))
            {
                return null;
            }
        }

        // Closed type arguments are insufficient if the method still contains unbound parameters
        foreach (TypeSignature argument in context.Method?.TypeArguments ?? [])
        {
            if (!argument.AcceptVisitor(IsConstructedGenericTypeVisitor.Instance))
            {
                return null;
            }
        }

        // Reuse one immutable provenance object across this work item's methods and signature iterators
        return new(module, context);
    }

    /// <summary>
    /// Identifies an active member scan without granting its descendants any metadata-root exemption.
    /// </summary>
    /// <param name="module">The declaring module of the member being scanned.</param>
    /// <param name="context">The type and method arguments applied to that body.</param>
    private sealed class MemberScanContext(ModuleDefinition module, GenericContext context)
    {
        /// <summary>The module of the active scan, whose finite metadata bounds same-substitution member transitions.</summary>
        public ModuleDefinition Module { get; } = module;

        /// <summary>The declaring-type arguments, without allocating a combined vector.</summary>
        private IList<TypeSignature>? TypeArguments { get; } = context.Type?.TypeArguments;

        /// <summary>The method arguments following the declaring-type arguments.</summary>
        private IList<TypeSignature>? MethodArguments { get; } = context.Method?.TypeArguments;

        /// <summary>The total number of arguments in the substituted context.</summary>
        public int ArgumentCount { get; } = (context.Type?.TypeArguments.Count ?? 0) + (context.Method?.TypeArguments.Count ?? 0);

        /// <summary>Gets an argument from the flattened, ordered substitution.</summary>
        /// <param name="index">The argument index.</param>
        public TypeSignature this[int index] => index < (TypeArguments?.Count ?? 0)
            ? TypeArguments![index]
            : MethodArguments![index - (TypeArguments?.Count ?? 0)];
    }
}
