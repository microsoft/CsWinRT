// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Methods for <see cref="Windows.Foundation.IAsyncActionWithProgress{TProgress}"/> types.
/// </summary>
/// <remarks>
/// This type should only be used by generated code.
/// </remarks>
[Obsolete("This type is an implementation detail, and it's only meant to be consumed by 'cswinrtgen'")]
[EditorBrowsable(EditorBrowsableState.Never)]
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
