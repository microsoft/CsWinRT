// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.InteropServices;

/// <summary>
/// The type map group placeholder for all <see cref="System.Runtime.InteropServices.DynamicInterfaceCastableImplementationAttribute"/>
/// interface types, for all projected Windows Runtime public interfaces.
/// </summary>
/// <remarks>
/// This type is only meant to be used as type map group for <see cref="System.Runtime.InteropServices.TypeMapping"/> APIs.
/// </remarks>
public abstract class DynamicInterfaceCastableImplementationTypeMapGroup
{
    /// <summary>
    /// This type should never be instantiated (it just can't be static because it needs to be used as a type argument).
    /// </summary>
    private DynamicInterfaceCastableImplementationTypeMapGroup()
    {
    }
}
