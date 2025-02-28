// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices.Marshalling;

namespace WindowsRuntime.InteropServices.Marshalling;

/// <summary>
/// A marshaller using <see cref="RestrictedErrorInfo"/> to marshal exceptions to and from the native side.
/// </summary>
/// <remarks>
/// This type is only meant to be used in two scenarios:
/// <list type="bullet">
///   <item>With <see cref="GeneratedComInterfaceAttribute"/>, when used on interfaces implemented by WinRT objects.</item>
///   <item>In <see langword="finally"/> blocks within generated or handwritten marshalling stubs.</item>
/// </list>
/// </remarks>
[CustomMarshaller(typeof(Exception), MarshalMode.ManagedToUnmanagedOut, typeof(RestrictedErrorInfoExceptionMarshaller))]
public static class RestrictedErrorInfoExceptionMarshaller
{
    /// <summary>
    /// Converts an <see cref="Exception"/> to an unmanaged version.
    /// </summary>
    /// <param name="managed">The managed exception to convert.</param>
    /// <returns>The <c>HRESULT</c> for the exception.</returns>
    /// <remarks>This method also sets up <c>IErrorInfo</c> and <c>IRestrictedErrorInfo</c> for the input exception.</remarks>
    public static HRESULT ConvertToUnmanaged(Exception managed)
    {
        RestrictedErrorInfo.SetErrorInfo(managed);

        return RestrictedErrorInfo.GetHRForException(managed);
    }

    /// <summary>
    /// Converts an unmanaged <c>HRESULT</c> to a managed exception.
    /// </summary>
    /// <param name="unmanaged">The <c>HRESULT</c> to convert.</param>
    /// <returns>A managed exception.</returns>
    public static Exception? ConvertToManaged(HRESULT unmanaged)
    {
        return RestrictedErrorInfo.GetExceptionForHR(unmanaged);
    }
}