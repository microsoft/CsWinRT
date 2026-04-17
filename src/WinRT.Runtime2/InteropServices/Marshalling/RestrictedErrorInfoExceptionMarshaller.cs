// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices.Marshalling;

namespace WindowsRuntime.InteropServices.Marshalling;

/// <summary>
/// A marshaller using the <c>IRestrictedErrorInfo</c> infrastructure to marshal exceptions to and from the native side.
/// </summary>
/// <remarks>
/// This type is only meant to be used in two scenarios:
/// <list type="bullet">
///   <item>With <see cref="GeneratedComInterfaceAttribute"/>, when used on interfaces implemented by Windows Runtime objects.</item>
///   <item>In <see langword="finally"/> blocks within generated or handwritten marshalling stubs.</item>
/// </list>
/// </remarks>
/// <see href="https://learn.microsoft.com/windows/win32/api/restrictederrorinfo/nn-restrictederrorinfo-irestrictederrorinfo"/>.
[CustomMarshaller(typeof(Exception), MarshalMode.ManagedToUnmanagedOut, typeof(RestrictedErrorInfoExceptionMarshaller))]
public static class RestrictedErrorInfoExceptionMarshaller
{
    /// <summary>
    /// Converts an <see cref="Exception"/> to an unmanaged version.
    /// </summary>
    /// <param name="value">The managed exception to convert.</param>
    /// <returns>The <c>HRESULT</c> for the exception.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <para>
    /// This method also sets up <c>IErrorInfo</c> and <c>IRestrictedErrorInfo</c> for the input exception.
    /// </para>
    /// <para>
    /// The <paramref name="value"/> parameter is never expected to be <see langword="null"/>, as this method
    /// is only meant to be used from some marshalling stub, from within a <see langword="catch"/> clause.
    /// </para>
    /// </remarks>
    public static HRESULT ConvertToUnmanaged(Exception value)
    {
#if WINDOWS_RUNTIME_REFERENCE_ASSEMBLY
        throw null;
#else
        RestrictedErrorInfo.SetErrorInfo(value);

        return RestrictedErrorInfo.GetHRForException(value);
#endif
    }

    /// <summary>
    /// Converts an unmanaged <c>HRESULT</c> to a managed exception.
    /// </summary>
    /// <param name="value">The <c>HRESULT</c> to convert.</param>
    /// <returns>A managed exception.</returns>
    public static Exception? ConvertToManaged(HRESULT value)
    {
#if WINDOWS_RUNTIME_REFERENCE_ASSEMBLY
        throw null;
#else
        return RestrictedErrorInfo.GetExceptionForHR(value);
#endif
    }
}