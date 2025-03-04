// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using WindowsRuntime.InteropServices;

namespace ABI.WindowsRuntime;

/// <inheritdoc cref="global::WindowsRuntime.IWeakReferenceSource"/>
[DynamicInterfaceCastableImplementation]
internal interface IWeakReferenceSource : global::WindowsRuntime.IWeakReferenceSource
{
    /// <inheritdoc/>
    global::WindowsRuntime.IWeakReference global::WindowsRuntime.IWeakReferenceSource.GetWeakReference()
    {
        // TODO
        throw null!;
    }
}

/// <summary>
/// Marshalling stubs for <see cref="IWeakReferenceSource"/>.
/// </summary>
internal static unsafe class IWeakReferenceSourceMethods
{
    /// <summary>
    /// Implements <see cref="global::WindowsRuntime.IWeakReferenceSource.GetWeakReference"/>
    /// </summary>
    public static global::WindowsRuntime.IWeakReference GetWeakReference(WindowsRuntimeObjectReference _obj)
    {
        _obj.AddRefUnsafe();

        try
        {
            void* thisPtr = _obj.GetThisPtrUnsafe();
            void* weakReference;

            HRESULT hresult = IWeakReferenceSourceVftbl.GetWeakReferenceUnsafe(thisPtr, &weakReference);

            // TODO
            return null!;
        }
        finally
        {
            _obj.ReleaseUnsafe();
        }
    }
}
