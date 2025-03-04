// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;

namespace WindowsRuntime;

/// <summary>
/// Represents a source object to which a weak reference can be retrieved.
/// </summary>
/// <see href="https://learn.microsoft.com/windows/win32/api/weakreference/nn-weakreference-iweakreferencesource"/>
[Guid("00000038-0000-0000-C000-000000000046")]
public interface IWeakReferenceSource
{
    /// <summary>
    /// Retrieves a weak reference from an <see cref="IWeakReferenceSource"/> instance.
    /// </summary>
    /// <returns>The weak reference.</returns>
    /// <see href="https://learn.microsoft.com/windows/win32/api/weakreference/nf-weakreference-iweakreferencesource-getweakreference"/>
    IWeakReference GetWeakReference();
}
