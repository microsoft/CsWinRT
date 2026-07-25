// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Helpers for working with context callbacks.
/// </summary>
internal static unsafe class ContextCallback
{
    /// <summary>
    /// Storage for the user-provided state for <see cref="CallInContextUnsafe"/>.
    /// </summary>
    [ThreadStatic]
    private static object? LocalContextCallbackState;

    /// <summary>
    /// Calls the given callback in the right context, and returns the result of that invocation.
    /// </summary>
    /// <param name="contextCallbackPtr">The context callback instance.</param>
    /// <param name="contextToken">The context token for the original context.</param>
    /// <param name="callback">The callback to invoke.</param>
    /// <param name="state">The state to pass to the callback.</param>
    /// <returns>The <c>HRESULT</c> for the operation.</returns>
    /// <remarks>
    /// If <paramref name="contextCallbackPtr"/> is <see langword="null"/>, or if <paramref name="contextToken"/> matches
    /// the current context token, then <paramref name="callback"/> will be immediately executed in the current context.
    /// Any exceptions in this case will be thrown directly. Otherwise, the <c>HRESULT</c> from marshalling is returned.
    /// </remarks>
    public static HRESULT CallInContextUnsafe(
        void* contextCallbackPtr,
        nuint contextToken,
        delegate*<object, void> callback,
        object state)
    {
        // Check if we are already on the same context, if so we do not need to switch
        if (contextCallbackPtr is null || WindowsRuntimeImports.CoGetContextToken() == contextToken)
        {
            callback(state);

            return WellKnownErrorCodes.S_OK;
        }

        // Native method that invokes the callback on the target context. The state object is guaranteed to be pinned,
        // so we can access it from a pointer. Note that the object will be stored in a static field, and it will not
        // be on the stack of the original thread, so it's safe with respect to cross-thread access of managed objects.
        // See: https://github.com/dotnet/runtime/blob/main/docs/design/specs/Memory-model.md#cross-thread-access-to-local-variables.
        [UnmanagedCallersOnly]
        static int InvokeCallback(ComCallData* comCallData)
        {
            try
            {
                CallbackData* callbackData = (CallbackData*)comCallData->pUserDefined;

                callbackData->Callback(*callbackData->StatePtr);

                return WellKnownErrorCodes.S_OK;
            }
            catch (Exception e)
            {
                return e.HResult;
            }
        }

        ref object? localContextCallbackState = ref LocalContextCallbackState;

        // Store the state object in the thread static to pass to the callback.
        // A thread local is the most efficient solution for this, given that
        // we need the state to be somewhere on the managed heap to be valid.
        // The GC doesn't allow cross-thread access to managed stack variables.
        // We're not using a volatile write, as it wouldn't actually help at all.
        // Volatile writes disallow the write operations to be moved before memory
        // operations that precede them, but they have nothing to say with respect
        // to memory operations after them (whereas here we specifically need this
        // write to not be reordered before following reads from that static field).
        if (localContextCallbackState is null)
        {
            localContextCallbackState = state;
        }
        else
        {
            // In case we recursed on this thread, meaning the local storage already holds a state
            // for some other caller above us in the stack, we can use a throwaway array to store
            // the current state. This isn't very efficient, but in practice this case should not
            // ever happen (it was validated in our entire test suite as well as in stress tests
            // with the Microsoft Store app, and we never detected recursion on this code path).
            // However just to be extra safe, we still want to keep this branch functional too.
            object[] objects = [state];

            localContextCallbackState = ref MemoryMarshal.GetArrayDataReference(objects)!;
        }

        HRESULT hresult;

        // Pin the state storage, which we can now safely pass to the target thread
        fixed (object* statePtr = &localContextCallbackState)
        fixed (Guid* riid = &WellKnownWindowsInterfaceIIDs.IID_ICallbackWithNoReentrancyToApplicationSTA)
        {
            CallbackData callbackData;
            callbackData.Callback = callback;
            callbackData.StatePtr = statePtr;

            ComCallData comCallData;
            comCallData.dwDispid = 0;
            comCallData.dwReserved = 0;
            comCallData.pUserDefined = &callbackData;

            // Marshal the supplied callback on the target context
            hresult = IContextCallbackVftbl.ContextCallbackUnsafe(
                thisPtr: contextCallbackPtr,
                pfnCallback: (delegate* unmanaged<ComCallData*, int>)&InvokeCallback,
                pParam: &comCallData,
                riid: riid,
                iMethod: 5,
                pUnk: null);
        }

        // Reset the static field to avoid keeping the state alive for longer
        LocalContextCallbackState = null;

        return hresult;
    }

    /// <summary>
    /// Additional data for <see cref="CallInContextUnsafe"/>
    /// </summary>
    private struct CallbackData
    {
        /// <summary>
        /// The callback to invoke on the target context.
        /// </summary>
        public delegate*<object, void> Callback;

        /// <summary>
        /// A pointer to the additional argument to supply to <see cref="Callback"/>.
        /// </summary>
        public object* StatePtr;
    }
}