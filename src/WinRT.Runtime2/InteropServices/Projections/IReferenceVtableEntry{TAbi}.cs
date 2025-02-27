// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.InteropServices;

/// <summary>
/// An interface for <c>IReference`1</c> implementations.
/// </summary>
/// <typeparam name="TAbi">The ABI for the <c>IReference`1</c> implementation.</typeparam>
internal unsafe interface IReferenceVtableEntry<TAbi>
    where TAbi : unmanaged
{
    /// <summary>
    /// Gets the function pointer for <c>IReference`1::Value</c>.
    /// </summary>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.ireference-1.value"/>
    static abstract delegate* unmanaged[MemberFunction]<void*, TAbi*, HRESULT> Value { get; }
}
