// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A handler type to provide custom implementations of Windows Runtime activation.
/// </summary>
/// <param name="runtimeClassName">The runtime class name for the type to activate.</param>
/// <param name="iid">The IID of the interface to retrieve for the given class.</param>
/// <param name="activationFactory">The resulting activation factory instance.</param>
/// <returns>The <c>HRESULT</c> for the operation.</returns>
/// <remarks>
/// Instances of this type can be assigned to <see cref="WindowsRuntimeActivationFactory.ActivationHandler"/>.
/// </remarks>
public unsafe delegate HRESULT WindowsRuntimeActivationHandler(string runtimeClassName, in Guid iid, out void* activationFactory);

