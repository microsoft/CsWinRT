// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WinRT.Interop
{
    /// <summary>IIDs for common COM/WinRT interfaces.</summary>
#if EMBED
    internal
#else 
    public
#endif
    static class IID
    {
        /// <summary>The IID for <c>IUnknown</c> (00000000-0000-0000-C000-000000000046).</summary>
        public static ref readonly Guid IID_IUnknown
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00,
                    0x00, 0x00,
                    0xC0,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x46
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>IInspectable</c> (AF86E2E0-B12D-4C6A-9C5A-D7AA65101E90).</summary>
        public static ref readonly Guid IID_IInspectable
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0xE0, 0xE2, 0x86, 0xAF,
                    0x2D, 0xB1,
                    0x6A, 0x4C,
                    0x9C,
                    0x5A,
                    0xD7,
                    0xAA,
                    0x65,
                    0x10,
                    0x1E,
                    0x90
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>IWeakReference</c> (00000037-0000-0000-C000-000000000046).</summary>
        internal static ref readonly Guid IID_IWeakReference
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x37, 0x00, 0x00, 0x00,
                    0x00, 0x00,
                    0x00, 0x00,
                    0xC0,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x46
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>IWeakReferenceSource</c> (00000038-0000-0000-C000-000000000046).</summary>
        internal static ref readonly Guid IID_IWeakReferenceSource
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x38, 0x00, 0x00, 0x00,
                    0x00, 0x00,
                    0x00, 0x00,
                    0xC0,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x46
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>IReferenceTracker</c> (11D3B13A-180E-4789-A8BE-7712882893E6).</summary>
        internal static ref readonly Guid IID_IReferenceTracker
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x3A, 0xB1, 0xD3, 0x11,
                    0x0E, 0x18,
                    0x89, 0x47,
                    0xA8,
                    0xBE,
                    0x77,
                    0x12,
                    0x88,
                    0x28,
                    0x93,
                    0xE6
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>IReferenceTrackerTarget</c> (64BD43F8-BFEE-4EC4-B7EB-2935158DAE21).</summary>
        internal static ref readonly Guid IID_IReferenceTrackerTarget
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0xF8, 0x43, 0xBD, 0x64,
                    0xEE, 0xBF,
                    0xC4, 0x4E,
                    0xB7,
                    0xEB,
                    0x29,
                    0x35,
                    0x15,
                    0x8D,
                    0xAE,
                    0x21
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>IActivationFactory</c> (00000035-0000-0000-C000-000000000046).</summary>
        public static ref readonly Guid IID_IActivationFactory
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x35, 0x00, 0x00, 0x00,
                    0x00, 0x00,
                    0x00, 0x00,
                    0xC0,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x46
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>IAgileObject</c> (94EA2B94-E9CC-49E0-C0FF-EE64CA8F5B90).</summary>
        internal static ref readonly Guid IID_IAgileObject
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x94, 0x2B, 0xEA, 0x94,
                    0xCC, 0xE9,
                    0xE0, 0x49,
                    0xC0,
                    0xFF,
                    0xEE,
                    0x64,
                    0xCA,
                    0x8F,
                    0x5B,
                    0x90
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>IMarshal</c> (00000003-0000-0000-C000-000000000046).</summary>
        internal static ref readonly Guid IID_IMarshal
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x03, 0x00, 0x00, 0x00,
                    0x00, 0x00,
                    0x00, 0x00,
                    0xC0,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x46
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>IContextCallback</c> (000001DA-0000-0000-C000-000000000046).</summary>
        internal static ref readonly Guid IID_IContextCallback
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0xDA, 0x01, 0x00, 0x00,
                    0x00, 0x00,
                    0x00, 0x00,
                    0xC0,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x46
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>ICallbackWithNoReentrancyToApplicationSTA</c> (0A299774-3E4E-FC42-1D9D-72CEE105CA57).</summary>
        internal static ref readonly Guid IID_ICallbackWithNoReentrancyToApplicationSTA
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x74, 0x97, 0x29, 0x0A,
                    0x4E, 0x3E,
                    0x42, 0xFC,
                    0x1D,
                    0x9D,
                    0x72,
                    0xCE,
                    0xE1,
                    0x05,
                    0xCA,
                    0x57
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>IErrorInfo</c> (1CF2B120-547D-101B-8E65-08002B2BD119).</summary>
        public static ref readonly Guid IID_IErrorInfo
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x20, 0xB1, 0xF2, 0x1C,
                    0x7D, 0x54,
                    0x1B, 0x10,
                    0x8E,
                    0x65,
                    0x08,
                    0x00,
                    0x2B,
                    0x2B,
                    0xD1,
                    0x19
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>ISupportErrorInfo</c> (DF0B3D60-548F-101B-8E65-08002B2BD119).</summary>
        public static ref readonly Guid IID_ISupportErrorInfo
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x60, 0x3D, 0x0B, 0xDF,
                    0x8F, 0x54,
                    0x1B, 0x10,
                    0x8E,
                    0x65,
                    0x08,
                    0x00,
                    0x2B,
                    0x2B,
                    0xD1,
                    0x19
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }
    }
}