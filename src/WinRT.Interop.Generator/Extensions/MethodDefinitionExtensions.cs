// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;

namespace WindowsRuntime.InteropGenerator;

/// <summary>
/// Extensions for the <see cref="MethodDefinition"/> type.
/// </summary>
internal static class MethodDefinitionExtensions
{
    /// <summary>
    /// Creates a new <see cref="CilMethodBody"/> instance and binds it to the specified method.
    /// </summary>
    /// <param name="method">The target <see cref="MethodDefinition"/> instance.</param>
    /// <returns>The bound <see cref="CilMethodBody"/> instance.</returns>
    public static CilMethodBody CreateAndBindCilMethodBody(this MethodDefinition method)
    {
        return method.CilMethodBody = new CilMethodBody(method);
    }
}
