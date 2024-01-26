using System;using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace WinRT
{
    internal static class Platform
    {
        [DllImport("api-ms-win-core-com-l1-1-0.dll")]
        internal static extern unsafe int CoCreateInstance(Guid* clsid, IntPtr outer, uint clsContext, Guid* iid, IntPtr* instance);

        [DllImport("api-ms-win-core-com-l1-1-0.dll")]
        internal static extern int CoDecrementMTAUsage(IntPtr cookie);

        [DllImport("api-ms-win-core-com-l1-1-0.dll")]
        internal static extern unsafe int CoIncrementMTAUsage(IntPtr* cookie);

#if NET6_0_OR_GREATER
        internal static bool FreeLibrary(IntPtr moduleHandle)
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

        internal static unsafe void* TryGetProcAddress(IntPtr moduleHandle, sbyte* functionName)
        {
            int lastError;
            void* returnValue;
            {
                Marshal.SetLastSystemError(0);
                returnValue = PInvoke(moduleHandle, functionName);
                lastError = Marshal.GetLastSystemError();
            }

            Marshal.SetLastPInvokeError(lastError);
            return returnValue;

            // Local P/Invoke
            [DllImport("kernel32.dll", EntryPoint = "GetProcAddress", ExactSpelling = true)]
            static extern unsafe void* PInvoke(IntPtr nativeModuleHandle, sbyte* nativeFunctionName);
        }
#else
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool FreeLibrary(IntPtr moduleHandle);

        [DllImport("kernel32.dll", EntryPoint = "GetProcAddress", SetLastError = true, BestFitMapping = false)]
        internal static unsafe extern void* TryGetProcAddress(IntPtr moduleHandle, sbyte* functionName);
#endif

        internal static unsafe void* TryGetProcAddress(IntPtr moduleHandle, ReadOnlySpan<byte> functionName)
        {
            fixed (byte* lpFunctionName = functionName)
            {
                return TryGetProcAddress(moduleHandle, (sbyte*)lpFunctionName);
            }
        }

        internal static unsafe void* TryGetProcAddress(IntPtr moduleHandle, string functionName)
        {
            bool allocated = false;
            Span<byte> buffer = stackalloc byte[0x100];
            if (functionName.Length * 3 >= 0x100) // Maximum of 3 bytes per UTF-8 character, stack allocation limit of 256 bytes (including the null terminator)
            {
                // Calculate accurate byte count when the provided stack-allocated buffer is not sufficient
                int exactByteCount = checked(Encoding.UTF8.GetByteCount(functionName) + 1); // + 1 for null terminator
                if (exactByteCount > 0x100)
                {
#if NET6_0_OR_GREATER
                    buffer = new((byte*)NativeMemory.Alloc((nuint)exactByteCount), exactByteCount);
#else
                    buffer = new((byte*)Marshal.AllocHGlobal(exactByteCount), exactByteCount);
#endif
                    allocated = true;
                }
            }

            var rawByte = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(buffer));

            int byteCount;

#if NET
            byteCount = Encoding.UTF8.GetBytes(functionName, buffer);
#else
            fixed (char* lpFunctionName = functionName)
            {
                byteCount = Encoding.UTF8.GetBytes(lpFunctionName, functionName.Length, rawByte, buffer.Length);
            }
#endif
            buffer[byteCount] = 0;

            void* functionPtr = TryGetProcAddress(moduleHandle, (sbyte*)rawByte);

            if (allocated)
#if NET6_0_OR_GREATER
                NativeMemory.Free(rawByte);
#else
                Marshal.FreeHGlobal((IntPtr)rawByte);
#endif

            return functionPtr;
        }

        internal static unsafe void* GetProcAddress(IntPtr moduleHandle, ReadOnlySpan<byte> functionName)
        {
            fixed (byte* lpFunctionName = functionName)
            {
                void* functionPtr = Platform.TryGetProcAddress(moduleHandle, (sbyte*)lpFunctionName);
                if (functionPtr == null)
                {
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error(), new IntPtr(-1));
                }
                return functionPtr;
            }
        }

        internal static unsafe void* GetProcAddress(IntPtr moduleHandle, string functionName)
        {
            void* functionPtr = Platform.TryGetProcAddress(moduleHandle, functionName);
            if (functionPtr == null)
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error(), new IntPtr(-1));
            }
            return functionPtr;
        }

#if NET6_0_OR_GREATER
        internal static unsafe IntPtr LoadLibraryExW(ushort* fileName, IntPtr fileHandle, uint flags)
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
#else
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static unsafe extern IntPtr LoadLibraryExW(ushort* fileName, IntPtr fileHandle, uint flags);
#endif
        internal static unsafe IntPtr LoadLibraryExW(string fileName, IntPtr fileHandle, uint flags)
        {
            fixed (char* lpFileName = fileName)
                return LoadLibraryExW((ushort*)lpFileName, fileHandle, flags);
        }

        [DllImport("api-ms-win-core-winrt-l1-1-0.dll")]
        internal static extern unsafe int RoGetActivationFactory(IntPtr runtimeClassId, Guid* iid, IntPtr* factory);

        [DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe int WindowsCreateString(ushort* sourceString, int length, IntPtr* hstring);

        [DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe int WindowsCreateStringReference(
            ushort* sourceString,
            int length,
            IntPtr* hstring_header,
            IntPtr* hstring);

        [DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern int WindowsDeleteString(IntPtr hstring);

        [DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe char* WindowsGetStringRawBuffer(IntPtr hstring, uint* length);

        [DllImport("api-ms-win-core-com-l1-1-1.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe int RoGetAgileReference(uint options, Guid* iid, IntPtr unknown, IntPtr* agileReference);
    }
}
