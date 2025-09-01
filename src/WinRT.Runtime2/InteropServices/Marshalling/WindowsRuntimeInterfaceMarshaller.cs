// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace WindowsRuntime.InteropServices.Marshalling;

/// <summary>
/// A marshaller for Windows Runtime interfaces.
/// </summary>
public static unsafe class WindowsRuntimeInterfaceMarshaller
{
    /// <summary>
    /// Marshals a Windows Runtime interface to a <see cref="WindowsRuntimeObjectReferenceValue"/> instance.
    /// </summary>
    /// <typeparam name="T">The type of the interface being marshalled.</typeparam>
    /// <param name="value">The input <typeparamref name="T"/> object to marshal.</param>
    /// <param name="iid">The IID for the interface being marshalled.</param>
    /// <returns>A <see cref="WindowsRuntimeObjectReferenceValue"/> instance for the requested interface.</returns>
    /// <remarks>
    /// <para>
    /// This method does not validate the <paramref name="iid"/> value. It is responsibility of the caller to
    /// ensure the parameter matches the actual IID of the <typeparamref name="T"/> interface being marshalled.
    /// </para>
    /// <para>
    /// Furthermore, the returned <see cref="WindowsRuntimeObjectReferenceValue"/> value will own an additional
    /// reference for the marshalled <paramref name="value"/> instance (either its underlying native object, or
    /// a runtime-provided CCW for the managed object instance). It is responsibility of the caller to always
    /// make sure that the returned <see cref="WindowsRuntimeObjectReferenceValue"/> instance is disposed.
    /// </para>
    /// <para>
    /// This method is only meant to be used for interface types. For other types, use the appropriate marshaller.
    /// Calling this method with <typeparamref name="T"/> being a non-interface type results in undefined behavior.
    /// </para>
    /// </remarks>
    public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged<T>(T? value, scoped in Guid iid)
        where T : class
    {
        if (value is null)
        {
            return default;
        }

        // If 'value' is a projected runtime class, get the cached object reference for the interface.
        // This is a critical optimization that avoids 'QueryInterface' for interface method parameters.
        if (value is IWindowsRuntimeInterface<T> windowsRuntimeInterface)
        {
            return windowsRuntimeInterface.GetInterface();
        }

        // It is possible to call this method with an anonymous inspectable object retrieved via a dynamic interface cast.
        // That is, where 'value' would just be some 'WindowsRuntimeInspectable' instance. In that case, it would not
        // implement the generic interface above, but we'd still want to just unwrap the native object reference, rather
        // than creating a CCW around that managed object. So, we check for this scenario next.
        if (value is WindowsRuntimeObject { HasUnwrappableNativeObjectReference: true } windowsRuntimeObject)
        {
            // Manually do 'QueryInterface' so we can provide a better exception message in case of failures
            HRESULT hresult = windowsRuntimeObject.NativeObjectReference.TryAsNative(in iid, out void* interfacePtr);

            // This is unlikely to fail, since we'd only get here with an object that passed a cast to the interface type
            if (!WellKnownErrorCodes.Succeeded(hresult))
            {
                // Helper to throw the exception without increasing the codegen in the fast path.
                // The JIT can already optimize throw helpers, but that's only when they contain
                // a 'throw new Exception(...)' body. Because this method is more complex, chances
                // are that optimization would not kick in. So we're just manually not inlining it.
                [StackTraceHidden]
                [MethodImpl(MethodImplOptions.NoInlining)]
                static void ThrowException(object value, in Guid iid, HRESULT hresult)
                {
                    // Special case 'E_NOINTERFACE' to provide a better exception message. It is intentional that this exception is
                    // thrown inline like this and without setting up the 'IErrorInfo' infrastructure. That is because APIs do not
                    // typically originate the 'IErrorInfo' data for 'E_NOINTERFACE', so that is not necessary here for consistency.
                    if (hresult == WellKnownErrorCodes.E_NOINTERFACE)
                    {
                        throw new InvalidCastException(
                            $"Failed to resolve a native interface pointer from an object of type '{value.GetType()}' for interface " +
                            $"'{typeof(T)}' (IID '{iid.ToString().ToUpperInvariant()}'): the specified cast is not valid.");
                    }

                    RestrictedErrorInfo.ThrowExceptionForHR(hresult);
                }

                ThrowException(value, in iid, hresult);
            }

            return new(interfacePtr);
        }
        else
        {
            // Marshal 'value' as an 'IInspectable' (same as in 'WindowsRuntimeObjectMarshaller')
            void* thisPtr = (void*)WindowsRuntimeComWrappers.Default.GetOrCreateComInterfaceForObject(value);

            // We need an interface pointer, so in this scenario we can't really avoid a 'QueryInterface' call.
            // The local cache for object references only applies to projected runtime classes, not managed types.
            HRESULT hresult = IUnknownVftbl.QueryInterfaceUnsafe(thisPtr, in iid, out void* interfacePtr);

            // We can release the 'IUnknown' reference now, it's no longer needed. We would normally just
            // use the 'GetOrCreateComInterfaceForObject' overload taking care of all of this 'QueryInterface'
            // logic automatically, but here we specifically want to throw a custom exception in case of failure.
            _ = IUnknownVftbl.ReleaseUnsafe(thisPtr);

            // It is very unlikely for this 'QueryInterface' to fail (it means either a managed object has an invalid vtable,
            // or something else happened that is not really supported). Still, we can produce a nice error message for it.
            if (!WellKnownErrorCodes.Succeeded(hresult))
            {
                // Same exception logic as above, see notes there
                [StackTraceHidden]
                [MethodImpl(MethodImplOptions.NoInlining)]
                static void ThrowException(object value, in Guid iid, HRESULT hresult)
                {
                    if (hresult == WellKnownErrorCodes.E_NOINTERFACE)
                    {
                        throw new InvalidCastException(
                            $"Failed to create a CCW for object of type '{value.GetType()}' for interface '{typeof(T)}' " +
                            $"(IID '{iid.ToString().ToUpperInvariant()}'): the specified cast is not valid.");
                    }

                    RestrictedErrorInfo.ThrowExceptionForHR(hresult);
                }

                ThrowException(value, in iid, hresult);
            }

            return new(interfacePtr);
        }
    }
}
