// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices;

internal static class ILanguageExceptionErrorInfoMethods
{
    public static unsafe void* GetLanguageException(void* thisPtr)
    {
        void* __return_value__ = null;
        Marshal.ThrowExceptionForHR(((ILanguageExceptionErrorInfoVftbl*)*(void***)thisPtr)->GetLanguageException(
            thisPtr,
            &__return_value__));
        return __return_value__;
    }
}
