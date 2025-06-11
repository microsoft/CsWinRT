// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;

#pragma warning disable CS8909

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Interop helpers for interacting with Windows Runtime types.
/// </summary>
public static unsafe class WindowsRuntimeHelpers
{
    /// <summary>
    /// Checks whether a pointer to a COM object is actually a reference to a CCW produced for a managed object that was marshalled to native code.
    /// </summary>
    /// <param name="unknown">The pointer to a COM object.</param>
    /// <returns>Whether <paramref name="unknown"/> refers to a CCW for a managed object, rather than a native COM object.</returns>
    /// <remarks>
    /// This method does not validate the input <paramref name="unknown"/> pointer not being <see langword="null"/>.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsReferenceToManagedObject(void* unknown)
    {
        IUnknownVftbl* unknownVftbl = (IUnknownVftbl*)*(void***)unknown;
        IUnknownVftbl* runtimeVftbl = (IUnknownVftbl*)IUnknownImpl.Vtable;

        return
            unknownVftbl->QueryInterface == runtimeVftbl->QueryInterface &&
            unknownVftbl->AddRef == runtimeVftbl->AddRef &&
            unknownVftbl->Release == runtimeVftbl->Release;
    }
}