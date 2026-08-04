// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using WindowsRuntime.ProjectionWriter.Models;

namespace WindowsRuntime.ProjectionWriter.Factories;

/// <summary>
/// Emits the ABI method body shapes for runtime interface vtable invocations: simple/forwarding
/// bodies, parameter conversion glue, and per-method UnsafeAccessor accessors for generic vtables.
/// </summary>
/// <remarks>
/// The implementation is split across several partial files:
/// <list type="bullet">
///   <item><description><c>AbiMethodBodyFactory.DoAbi.cs</c> - CCW Do_Abi_* method body emission.</description></item>
///   <item><description><c>AbiMethodBodyFactory.RcwCaller.cs</c> - RCW caller (instance-method) body emission.</description></item>
///   <item><description><c>AbiMethodBodyFactory.MethodsClass.cs</c> - The static <c>*Methods</c> class members (caller dispatch hub).</description></item>
///   <item><description><c>AbiMethodBodyFactory.MarshallerDispatch.cs</c> - Per-marshaller ConvertToManaged/Unmanaged dispatch helpers.</description></item>
/// </list>
/// </remarks>
internal static partial class AbiMethodBodyFactory
{
    /// <summary>
    /// Returns whether the method's return type or any parameter type involves a generic
    /// instantiation over <c>IReference&lt;TypeName&gt;</c> / <c>IReference&lt;HResult&gt;</c>, which
    /// cannot be marshalled (see <see cref="TypeSignatureExtensions.ContainsNestedNullableTOfReferenceType"/>).
    /// When this is the case, the projected member's body is replaced with a <c>throw</c> instead of
    /// emitting a reference to a <c>WinRT.Interop</c> marshaller that the interop generator cannot produce.
    /// </summary>
    /// <param name="sig">The interface method signature being emitted.</param>
    /// <returns><see langword="true"/> if the method cannot be marshalled; otherwise <see langword="false"/>.</returns>
    private static bool RequiresUnsupportedNullableTOfReferenceTypeMarshalling(MethodSignatureInfo sig)
    {
        if (sig.ReturnType is { } rt && rt.ContainsNestedNullableTOfReferenceType())
        {
            return true;
        }

        foreach (ParameterInfo p in sig.Parameters)
        {
            if (p.Type.StripByRefAndCustomModifiers().ContainsNestedNullableTOfReferenceType())
            {
                return true;
            }
        }

        return false;
    }
}
