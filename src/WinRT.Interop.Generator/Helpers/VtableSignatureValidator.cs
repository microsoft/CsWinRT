// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.Generator;

namespace WindowsRuntime.InteropGenerator.Helpers;

/// <summary>
/// A class that provides logic to validate the vtable types being emitted.
/// </summary>
internal static class VtableSignatureValidator
{
    /// <summary>
    /// Validates that a vtable slot is declared with the same signature as the method being stored into it.
    /// </summary>
    /// <param name="vtableSlot">The vtable slot (i.e. the function pointer field) being initialized.</param>
    /// <param name="method">The <see cref="MethodDefinition"/> whose address is being stored into <paramref name="vtableSlot"/>.</param>
    /// <exception cref="InvalidOperationException">Thrown if the two signatures don't match.</exception>
    /// <remarks>
    /// <para>
    /// Storing a function pointer into a vtable slot is a plain <c>ldftn</c>/<c>stfld</c> pair, which the runtime does
    /// not type check. That means the declared signature of a slot and the signature of the method actually placed in
    /// it can silently disagree. That is harmless for the CCW itself (native callers only ever see the signature of the
    /// method), but the declared signature of the slot is also what any <c>calli</c> through that same vtable is emitted
    /// with. A mismatch is a latent bug that only manifests as stack corruption once someone calls through the slot.
    /// </para>
    /// <para>
    /// Only the arity and the return type are validated, as the slot and the method are allowed to spell an individual
    /// parameter differently. Shared (i.e. non specialized) vtables use <c>void*</c> where the implementation method
    /// uses the exact ABI type of the element, which is ABI identical.
    /// </para>
    /// </remarks>
    public static void ValidateSlot(FieldDefinition vtableSlot, MethodDefinition method)
    {
        MethodSignature slotSignature = ((FunctionPointerTypeSignature)vtableSlot.Signature!.FieldType).Signature;
        MethodSignature methodSignature = method.Signature!;

        // The slot declares the 'this' pointer explicitly, and the implementation methods are all static (they are
        // marked with '[UnmanagedCallersOnly]'), so the two parameter counts should match exactly. A mismatch means
        // the vtable declaration is out of sync with the Windows Runtime ABI of that interface method, which in
        // practice is almost always a missing '[out, retval]' parameter on the vtable declaration.
        if (slotSignature.ParameterTypes.Count != methodSignature.ParameterTypes.Count)
        {
            throw new InvalidOperationException(
                $"The vtable slot '{vtableSlot.Name}' is declared with {slotSignature.ParameterTypes.Count} parameter(s), but the method " +
                $"'{method.Name}' being stored into it has {methodSignature.ParameterTypes.Count}. The declared signature of a vtable slot " +
                $"must match the Windows Runtime ABI of that method exactly, as it is also used to emit 'calli' instructions.");
        }

        // The return type of the slot carries a 'modopt(CallConvMemberFunction)' that the implementation method
        // expresses through '[UnmanagedCallersOnly]' instead, so only compare the underlying return types here.
        if (!SignatureComparer.Default.Equals(
            slotSignature.ReturnType.StripByRefAndCustomModifiers(),
            methodSignature.ReturnType.StripByRefAndCustomModifiers()))
        {
            throw new InvalidOperationException(
                $"The vtable slot '{vtableSlot.Name}' is declared with return type '{slotSignature.ReturnType}', but the method " +
                $"'{method.Name}' being stored into it returns '{methodSignature.ReturnType}'.");
        }
    }
}
