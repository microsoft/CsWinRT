// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices.Marshalling;

namespace WinRT.Interop.Marshalling
{
    /// <summary>
    /// A marshaller using <see cref="ExceptionHelpers"/> to marshal exceptions to and from the native side.
    /// </summary>
    /// <remarks>
    /// This type is only meant to be used in two scenarios:
    /// <list type="bullet">
    ///   <item>With <see cref="GeneratedComInterfaceAttribute"/>, when used on interfaces implemented by WinRT objects.</item>
    ///   <item>In <see langword="finally"/> blocks within generated or handwritten marshalling stubs.</item>
    /// </list>
    /// </remarks>
    [CustomMarshaller(typeof(Exception), MarshalMode.ManagedToUnmanagedOut, typeof(ExceptionHelpersMarshaller))]
#if EMBED
    internal
#else
    public
#endif
    static class ExceptionHelpersMarshaller
    {
        /// <summary>
        /// Converts an <see cref="Exception"/> to an unmanaged version.
        /// </summary>
        /// <param name="managed">The managed exception to convert.</param>
        /// <returns>The <c>HRESULT</c> for the exception.</returns>
        /// <remarks>This method also sets up <c>IErrorInfo</c> and <c>IRestrictedErrorInfo</c> for the input exception.</remarks>
        public static int ConvertToUnmanaged(Exception managed)
        {
            ExceptionHelpers.SetErrorInfo(managed);

            return ExceptionHelpers.GetHRForException(managed);
        }

        /// <summary>
        /// Converts an unmanaged <c>HRESULT</c> to a managed exception.
        /// </summary>
        /// <param name="unmanaged">The <c>HRESULT</c> to convert.</param>
        /// <returns>A managed exception.</returns>
        public static Exception ConvertToManaged(int unmanaged)
        {
            return ExceptionHelpers.GetExceptionForHR(unmanaged);
        }
    }
}
