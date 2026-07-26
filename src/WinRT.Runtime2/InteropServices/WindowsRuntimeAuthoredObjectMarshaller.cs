// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
#if !WINDOWS_RUNTIME_REFERENCE_ASSEMBLY
using WindowsRuntime.InteropServices.Marshalling;
#endif

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Marshalling helpers for Windows Runtime types implemented (authored) in C# by extending a CsWinRT generated
/// <c>ABI.&lt;Namespace&gt;.&lt;Class&gt;</c> abstract base class.
/// </summary>
/// <remarks>
/// This type is used by the conversion operators CsWinRT generates on those abstract base classes. Unlike the rest of
/// the marshalling infrastructure, it is present in the <c>WinRT.Runtime.dll</c> reference assembly, because that
/// generated code is compiled into the component's own assembly (which only sees the reference assembly).
/// </remarks>
public static unsafe class WindowsRuntimeAuthoredObjectMarshaller
{
    /// <summary>
    /// Converts an authored Windows Runtime object to the projected runtime class type it implements.
    /// </summary>
    /// <typeparam name="T">The projected runtime class type to convert to.</typeparam>
    /// <param name="value">The authored object to convert. Can be <see langword="null"/>.</param>
    /// <returns>
    /// The projected instance wrapping <paramref name="value"/>, or <see langword="null"/> if <paramref name="value"/>
    /// is <see langword="null"/>.
    /// </returns>
    /// <exception cref="InvalidCastException">
    /// Thrown if <paramref name="value"/> does not implement the Windows Runtime interfaces required by <typeparamref name="T"/>.
    /// </exception>
    public static T? ConvertToProjectedType<T>(object? value)
        where T : WindowsRuntimeObject
    {
#if WINDOWS_RUNTIME_REFERENCE_ASSEMBLY
        throw null;
#elif WINDOWS_RUNTIME_IMPLEMENTATION_ASSEMBLY
        if (value is null)
        {
            return null;
        }

        // Create the CCW for the authored object, then resolve the RCW for it. The RCW resolution goes
        // through the usual 'ComWrappers' callback, which produces the projected type for the object.
        using WindowsRuntimeObjectReferenceValue objectReferenceValue = WindowsRuntimeObjectMarshaller.ConvertToUnmanaged(value);

        return (T)WindowsRuntimeObjectMarshaller.ConvertToManaged(objectReferenceValue.GetThisPtrUnsafe())!;
#endif
    }
}
