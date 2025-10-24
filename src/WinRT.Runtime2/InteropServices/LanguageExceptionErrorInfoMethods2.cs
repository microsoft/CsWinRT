// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using WindowsRuntime.InteropServices.Marshalling;

namespace WindowsRuntime.InteropServices;

internal static class ILanguageExceptionErrorInfo2Methods
{
    public static unsafe void CapturePropagationContext(void* thisPtr, Exception exceptionValue)
    {
        using WindowsRuntimeObjectReferenceValue managedExceptionWrapper = WindowsRuntimeObjectMarshaller.ConvertToUnmanaged(exceptionValue);
        ((ILanguageExceptionErrorInfo2Vftbl*)*(void***)thisPtr)->CapturePropagationContext(
            thisPtr,
            managedExceptionWrapper.GetThisPtrUnsafe()).Assert();
    }
}
