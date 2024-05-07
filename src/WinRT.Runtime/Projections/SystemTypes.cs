// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;

namespace ABI.System
{
    [StructLayout(LayoutKind.Sequential)]
#if EMBED
    internal
#else
    public
#endif
    struct TimeSpan
    {
        // NOTE: both 'Windows.Foundation.TimeSpan.Duration' and 'System.TimeSpan.Ticks' are in units of 100ns
        public long Duration;

        public struct Marshaler
        {
            public TimeSpan __abi;
        }

        public static Marshaler CreateMarshaler(global::System.TimeSpan value)
        {
            return new Marshaler { __abi = new TimeSpan { Duration = value.Ticks } };
        }

        public static TimeSpan GetAbi(Marshaler m) => m.__abi;

        public static global::System.TimeSpan FromAbi(TimeSpan value)
        {
            return global::System.TimeSpan.FromTicks(value.Duration);
        }

        public static unsafe void CopyAbi(Marshaler arg, IntPtr dest) =>
            *(TimeSpan*)dest.ToPointer() = GetAbi(arg);

        public static TimeSpan FromManaged(global::System.TimeSpan value)
        {
            return new TimeSpan { Duration = value.Ticks };
        }

        public static unsafe void CopyManaged(global::System.TimeSpan arg, IntPtr dest) =>
            *(TimeSpan*)dest.ToPointer() = FromManaged(arg);

        public static void DisposeMarshaler(Marshaler m) { }
        public static void DisposeAbi(TimeSpan abi) { }

        public static string GetGuidSignature()
        {
            return "struct(Windows.Foundation.TimeSpan;i8)";
        }
    }

    [StructLayout(LayoutKind.Sequential)]
#if EMBED
    internal
#else
    public
#endif
    struct DateTimeOffset
    {
        // NOTE: 'Windows.Foundation.DateTime.UniversalTime' is a FILETIME value (relative to 01/01/1601), however
        // 'System.DateTimeOffset.Ticks' is relative to 01/01/0001
        public long UniversalTime;

        // Numer of ticks counted between 0001-01-01, 00:00:00 and 1601-01-01, 00:00:00.
        // You can get this through:  (new DateTimeOffset(1601, 1, 1, 0, 0, 1, TimeSpan.Zero)).Ticks;
        private const long ManagedUtcTicksAtNativeZero = 504911232000000000;
        // DO NOT use ToFileTime/FromFileTime, which don't support negative UniversalTime.

        public struct Marshaler
        {
            public DateTimeOffset __abi;
        }

        public static Marshaler CreateMarshaler(global::System.DateTimeOffset value)
        {
            return new Marshaler { __abi = new DateTimeOffset { UniversalTime = value.UtcTicks - ManagedUtcTicksAtNativeZero } };
        }

        public static DateTimeOffset GetAbi(Marshaler m) => m.__abi;

        public static global::System.DateTimeOffset FromAbi(DateTimeOffset value)
        {
            var utcTime = new global::System.DateTimeOffset(value.UniversalTime + ManagedUtcTicksAtNativeZero, global::System.TimeSpan.Zero);

            return utcTime.ToLocalTime();
        }

        public static unsafe void CopyAbi(Marshaler arg, IntPtr dest) =>
            *(DateTimeOffset*)dest.ToPointer() = GetAbi(arg);

        public static DateTimeOffset FromManaged(global::System.DateTimeOffset value)
        {
            return new DateTimeOffset { UniversalTime = value.UtcTicks - ManagedUtcTicksAtNativeZero };
        }

        public static unsafe void CopyManaged(global::System.DateTimeOffset arg, IntPtr dest) =>
            *(DateTimeOffset*)dest.ToPointer() = FromManaged(arg);

        public static void DisposeMarshaler(Marshaler m) { }
        public static void DisposeAbi(DateTimeOffset abi) { }

        public static string GetGuidSignature()
        {
            return "struct(Windows.Foundation.DateTime;i8)";
        }
    }

    [StructLayout(LayoutKind.Sequential)]
#if EMBED
    internal
#else
    public
#endif
    struct Exception
    {
        public int hr;

        public struct Marshaler
        {
            public Exception __abi;
        }

        public static Marshaler CreateMarshaler(global::System.Exception value)
        {
            return new Marshaler { __abi = new Exception { hr = value is object ? global::WinRT.ExceptionHelpers.GetHRForException(value) : 0 } };
        }

        public static Exception GetAbi(Marshaler m) => m.__abi;

        public static global::System.Exception FromAbi(Exception value)
        {
            return global::WinRT.ExceptionHelpers.GetExceptionForHR(value.hr);
        }

        public static unsafe void CopyAbi(Marshaler arg, IntPtr dest) =>
            *(Exception*)dest.ToPointer() = GetAbi(arg);

        public static Exception FromManaged(global::System.Exception value)
        {
            return new Exception { hr = value is object ? global::WinRT.ExceptionHelpers.GetHRForException(value) : 0 };
        }

        public static unsafe void CopyManaged(global::System.Exception arg, IntPtr dest) =>
            *(Exception*)dest.ToPointer() = FromManaged(arg);

        public static void DisposeMarshaler(Marshaler m) { }
        public static void DisposeAbi(Exception abi) { }

        public static string GetGuidSignature()
        {
            return "struct(Windows.Foundation.HResult;i4)";
        }
    }
}
