// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Methods for <see cref="Windows.Foundation.IAsyncActionWithProgress{TProgress}"/> types.
/// </summary>
[WindowsRuntimeImplementationOnlyMember]
public static unsafe class IAsyncActionWithProgressMethods
{
    /// <inheritdoc cref="Windows.Foundation.IAsyncActionWithProgress{TProgress}.GetResults"/>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void GetResults(WindowsRuntimeObjectReference thisReference)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        void* thisPtr = thisValue.GetThisPtrUnsafe();

        RestrictedErrorInfo.ThrowExceptionForHR(((IAsyncActionWithProgressVftbl*)*(void***)thisPtr)->GetResults(thisPtr));
    }
}
