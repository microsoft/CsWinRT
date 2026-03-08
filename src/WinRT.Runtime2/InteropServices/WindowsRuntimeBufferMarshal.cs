// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Provides low-level marshalling functionality for Windows Runtime buffer types.
/// </summary>
public static partial class WindowsRuntimeBufferMarshal
{
    /// <summary>
    /// Tries to get a pointer to the underlying data representation of the <see cref="IBuffer"/>.
    /// </summary>
    /// <param name="buffer">The buffer to get the data pointer for.</param>
    /// <param name="data">The pointer to the underlying data representation of the buffer.</param>
    /// <returns>Whether the data was successfully retrieved.</returns>
    /// <exception cref="InvalidCastException">Thrown if <paramref name="buffer"/> is a native object that doesn't implement <see href="https://learn.microsoft.com/windows/win32/api/robuffer/nf-robuffer-ibufferbyteaccess"><c>IBufferByteAccess</c></see>.</exception>
    /// <exception cref="Exception">Thrown if invoking <see href="https://learn.microsoft.com/windows/win32/api/robuffer/nf-robuffer-ibufferbyteaccess-buffer"><c>IBufferByteAccess.Buffer</c></see> on the input buffer fails.</exception>
    /// <remarks>
    /// Callers are responsible for ensuring that the buffer is kept alive while the pointer is in use. For instance,
    /// they can use <see cref="GC.KeepAlive"/> after the last use of the pointer, to ensure that this is the case.
    /// </remarks>
    public static unsafe bool TryGetDataUnsafe([NotNullWhen(true)] IBuffer? buffer, out byte* data)
    {
        if (buffer is null)
        {
            goto Failure;
        }

        // If the input buffer is a Windows Runtime object wrapper, unwrap it and use it to access the underlying buffer.
        // This will be the case for both projected Windows Runtime types, or for 'IBuffer' references obtained via IDIC.
        if (WindowsRuntimeBufferHelpers.TryGetNativeData(buffer, out data))
        {
            return true;
        }

        // Also handle managed buffer implementations from 'WinRT.Runtime.dll'
        if (WindowsRuntimeBufferHelpers.TryGetManagedData(buffer, out data))
        {
            return true;
        }

    Failure:
        data = null;

        return false;
    }

    /// <summary>
    /// Tries to get a pointer to the underlying data representation of the <see cref="IMemoryBufferReference"/>.
    /// </summary>
    /// <param name="buffer">The buffer to get the data pointer for.</param>
    /// <param name="data">The pointer to the underlying data representation of the buffer.</param>
    /// <param name="capacity">The capacity of the buffer.</param>
    /// <returns>Whether the data was successfully retrieved.</returns>
    /// <exception cref="InvalidCastException">Thrown if <paramref name="buffer"/> is a native object that doesn't implement <see href="https://learn.microsoft.com/previous-versions//mt297505(v=vs.85)"><c>IMemoryBufferByteAccess</c></see>.</exception>
    /// <exception cref="Exception">Thrown if invoking <see href="https://learn.microsoft.com/windows/win32/winrt/imemorybufferbyteaccess-getbuffer"><c>IMemoryBufferByteAccess.GetBuffer</c></see> on the input buffer fails.</exception>
    /// <remarks>
    /// Callers are responsible for ensuring that the buffer is kept alive while the pointer is in use. For instance,
    /// they can use <see cref="GC.KeepAlive"/> after the last use of the pointer, to ensure that this is the case.
    /// </remarks>
    public static unsafe bool TryGetDataUnsafe([NotNullWhen(true)] IMemoryBufferReference? buffer, out byte* data, out uint capacity)
    {
        if (buffer is null)
        {
            goto Failure;
        }

        // Similar handling as in 'WindowsRuntimeBufferHelpers.TryGetNativeData', but for native 'IMemoryBufferByteAccess' instances
        if (buffer is WindowsRuntimeObject { HasUnwrappableNativeObjectReference: true } bufferObject)
        {
            using WindowsRuntimeObjectReferenceValue bufferByteAccessValue = bufferObject.NativeObjectReference.AsValue(WellKnownInterfaceIIDs.IID_IMemoryBufferByteAccess);

            fixed (byte** dataPtr = &data)
            fixed (uint* capacityPtr = &capacity)
            {
                HRESULT hresult = IMemoryBufferByteAccessVftbl.GetBufferUnsafe(bufferByteAccessValue.GetThisPtrUnsafe(), dataPtr, capacityPtr);

                RestrictedErrorInfo.ThrowExceptionForHR(hresult);
            }

            return true;
        }

    Failure:
        data = null;
        capacity = 0;

        return false;
    }

    /// <summary>
    /// Tries to get an <see cref="ArraySegment{T}"/> value for the underlying data representation of the <see cref="IBuffer"/>.
    /// </summary>
    /// <param name="buffer">The buffer to get the <see cref="ArraySegment{T}"/> value for.</param>
    /// <param name="array">The resulting <see cref="ArraySegment{T}"/> value, if successfully retrieved.</param>
    /// <returns>Whether <paramref name="array"/> was successfully retrieved.</returns>
    public static bool TryGetArray([NotNullWhen(true)] IBuffer? buffer, out ArraySegment<byte> array)
    {
        if (buffer is null)
        {
            goto Failure;
        }

        // If buffer is backed by a managed array, return it
        if (buffer is WindowsRuntimeExternalArrayBuffer externalArrayBuffer)
        {
            array = externalArrayBuffer.GetArraySegment();

            return true;
        }

        // Same as above for pinned arrays as well
        if (buffer is WindowsRuntimePinnedArrayBuffer pinnedArrayBuffer)
        {
            array = pinnedArrayBuffer.GetArraySegment();

            return true;
        }

    Failure:
        array = default;

        return false;
    }
}
