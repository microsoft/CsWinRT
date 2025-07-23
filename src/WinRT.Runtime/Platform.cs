using System;
using System.Runtime.InteropServices;

namespace WinRT.Interop
{
    // Direct P/Invoke for platform helpers
    internal static partial class Platform
    {
        [DllImport("api-ms-win-core-com-l1-1-0.dll")]
        public static extern unsafe int CoCreateInstance(Guid* clsid, IntPtr outer, uint clsContext, Guid* iid, IntPtr* instance);

        [DllImport("api-ms-win-core-com-l1-1-0.dll")]
        public static extern int CoDecrementMTAUsage(IntPtr cookie);

        [DllImport("api-ms-win-core-com-l1-1-0.dll")]
        public static extern unsafe int CoIncrementMTAUsage(IntPtr* cookie);

        [DllImport("api-ms-win-core-winrt-l1-1-0.dll")]
        public static extern unsafe int RoGetActivationFactory(IntPtr runtimeClassId, Guid* iid, IntPtr* factory);

        [DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe int WindowsCreateString(ushort* sourceString, int length, IntPtr* hstring);

        [DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe int WindowsCreateStringReference(
            ushort* sourceString,
            int length,
            IntPtr* hstring_header,
            IntPtr* hstring);

        [DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern int WindowsDeleteString(IntPtr hstring);

        [DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe char* WindowsGetStringRawBuffer(IntPtr hstring, uint* length);

        [DllImport("api-ms-win-core-com-l1-1-1.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe int RoGetAgileReference(uint options, Guid* iid, IntPtr unknown, IntPtr* agileReference);

        [DllImport("api-ms-win-core-com-l1-1-0.dll")]
        public static extern unsafe int CoGetContextToken(IntPtr* contextToken);

        [DllImport("api-ms-win-core-com-l1-1-0.dll")]
        public static extern unsafe int CoGetObjectContext(Guid* riid, IntPtr* ppv);

        [DllImport("oleaut32.dll")]
        public static extern int SetErrorInfo(uint dwReserved, IntPtr perrinfo);

        [DllImport("api-ms-win-core-com-l1-1-0.dll")]
        public static extern unsafe int CoCreateFreeThreadedMarshaler(IntPtr outer, IntPtr* marshalerPtr);

        [DllImport("Kernel32.dll")]
        public static extern unsafe uint FormatMessageW(uint dwFlags, void* lpSource, uint dwMessageId, uint dwLanguageId, char** lpBuffer, uint nSize, void* pArguments);

        [DllImport("Kernel32.dll")]
        public static extern unsafe void* LocalFree(void* hMem);
    }

    // Handcrafted P/Invoke with TFM-specific handling, or thin high-level abstractions (eg. 'TryGetProcAddress'/'GetProcAddress')
    partial class Platform
    {
        public static bool FreeLibrary(IntPtr moduleHandle)
        {
#if NET6_0_OR_GREATER
            return LibraryImportStubs.FreeLibrary(moduleHandle);
#else
            return FreeLibrary(moduleHandle);

            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            static extern bool FreeLibrary(IntPtr moduleHandle);
#endif
        }

        public static unsafe IntPtr TryGetProcAddress(IntPtr moduleHandle, ReadOnlySpan<byte> functionName)
        {
            fixed (byte* lpFunctionName = functionName)
            {
#if NET6_0_OR_GREATER
                return LibraryImportStubs.GetProcAddress(moduleHandle, (sbyte*)lpFunctionName);
#else
                return GetProcAddress(moduleHandle, (sbyte*)lpFunctionName);

                [DllImport("kernel32.dll", SetLastError = true)]
                static extern unsafe IntPtr GetProcAddress(IntPtr nativeModuleHandle, sbyte* nativeFunctionName);
#endif
            }
        }

        public static IntPtr GetProcAddress(IntPtr moduleHandle, ReadOnlySpan<byte> functionName)
        {
            IntPtr functionPtr = TryGetProcAddress(moduleHandle, functionName);

            if (functionPtr == IntPtr.Zero)
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error(), new IntPtr(-1));
            }

            return functionPtr;
        }
        
        public static unsafe IntPtr LoadLibraryExW(string fileName, IntPtr fileHandle, uint flags)
        {
            fixed (char* lpFileName = fileName)
            {
#if NET6_0_OR_GREATER
                return LibraryImportStubs.LoadLibraryExW((ushort*)lpFileName, fileHandle, flags);
#else
                return LoadLibraryExW((ushort*)lpFileName, fileHandle, flags);

                [DllImport("kernel32.dll", SetLastError = true)]
                static unsafe extern IntPtr LoadLibraryExW(ushort* fileName, IntPtr fileHandle, uint flags);
#endif
            }
        }
    }

#if NET6_0_OR_GREATER
    // Marshalling stubs from [LibraryImport], which are used to get the same semantics (eg. for setting
    // the last P/Invoke errors, etc.) on .NET 6 as well ([LibraryImport] was only introduced in .NET 7).
    internal static class LibraryImportStubs
    {
        public static bool FreeLibrary(IntPtr moduleHandle)
        {
            int lastError;
            bool returnValue;
            int nativeReturnValue;
            {
                Marshal.SetLastSystemError(0);
                nativeReturnValue = PInvoke(moduleHandle);
                lastError = Marshal.GetLastSystemError();
            }

            // Unmarshal - Convert native data to managed data.
            returnValue = nativeReturnValue != 0;
            Marshal.SetLastPInvokeError(lastError);
            return returnValue;

            // Local P/Invoke
            [DllImport("kernel32.dll", EntryPoint = "FreeLibrary", ExactSpelling = true)]
            static extern unsafe int PInvoke(IntPtr nativeModuleHandle);
        }

        public static unsafe IntPtr GetProcAddress(IntPtr moduleHandle, sbyte* functionName)
        {
            int lastError;
            IntPtr returnValue;
            {
                Marshal.SetLastSystemError(0);
                returnValue = PInvoke(moduleHandle, functionName);
                lastError = Marshal.GetLastSystemError();
            }

            Marshal.SetLastPInvokeError(lastError);
            return returnValue;

            // Local P/Invoke
            [DllImport("kernel32.dll", EntryPoint = "GetProcAddress", ExactSpelling = true)]
            static extern unsafe IntPtr PInvoke(IntPtr nativeModuleHandle, sbyte* nativeFunctionName);
        }

        public static unsafe IntPtr LoadLibraryExW(ushort* fileName, IntPtr fileHandle, uint flags)
        {
            int lastError;
            IntPtr returnValue;
            {
                Marshal.SetLastSystemError(0);
                returnValue = PInvoke(fileName, fileHandle, flags);
                lastError = Marshal.GetLastSystemError();
            }

            Marshal.SetLastPInvokeError(lastError);
            return returnValue;

            // Local P/Invoke
            [DllImport("kernel32.dll", EntryPoint = "LoadLibraryExW", ExactSpelling = true)]
            static extern unsafe IntPtr PInvoke(ushort* nativeFileName, IntPtr nativeFileHandle, uint nativeFlags);
        }
    }
#endif
}
