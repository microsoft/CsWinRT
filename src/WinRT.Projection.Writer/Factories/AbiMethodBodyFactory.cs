// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
}