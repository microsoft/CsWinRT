// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices
{
    /// <summary>IIDs for common COM/WinRT interfaces.</summary>
#if EMBED
    internal
#else 
    public
#endif
    static class WellKnownInterfaceIds
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
        public static ref readonly Guid IID_IAgileObject
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
        public static ref readonly Guid IID_IMarshal
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

        /// <summary>The IID for <c>IBuffer</c> (905A0FE0-BC53-11DF-8C49-001E4FC686DA).</summary>
        public static ref readonly Guid IID_IBuffer
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0xE0, 0x0F, 0x5A, 0x90,
                    0x53, 0xBC,
                    0xDF, 0x11,
                    0x8C,
                    0x49,
                    0x00,
                    0x1E,
                    0x4F,
                    0xC6,
                    0x86,
                    0xDA
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>IBufferByteAccess</c> (905A0FEF-BC53-11DF-8C49-001E4FC686DA).</summary>
        public static ref readonly Guid IID_IBufferByteAccess
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0xEF, 0x0F, 0x5A, 0x90,
                    0x53, 0xBC,
                    0xDF, 0x11,
                    0x8C,
                    0x49,
                    0x00,
                    0x1E,
                    0x4F,
                    0xC6,
                    0x86,
                    0xDA
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>IMemoryBufferByteAccess</c> (5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D).</summary>
        public static ref readonly Guid IID_IMemoryBufferByteAccess
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x35, 0x32, 0x0D, 0x5B,
                    0xBA, 0x4D,
                    0x44, 0x4D,
                    0x86,
                    0x5E,
                    0x8F,
                    0x1D,
                    0x0E,
                    0x4F,
                    0xD0,
                    0x4D
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
        internal static ref readonly Guid IID_IErrorInfo
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
        internal static ref readonly Guid IID_ISupportErrorInfo
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

        /// <summary>The IID for <c>ILanguageExceptionErrorInfo</c> (04A2DBF3-DF83-116C-0946-0812ABF6E07D).</summary>
        internal static ref readonly Guid IID_ILanguageExceptionErrorInfo
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0xF3, 0xDB, 0xA2, 0x04,
                    0x83, 0xDF,
                    0x6C, 0x11,
                    0x09,
                    0x46,
                    0x08,
                    0x12,
                    0xAB,
                    0xF6,
                    0xE0,
                    0x7D
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>ILanguageExceptionErrorInfo2</c> (5746E5C4-5B97-424C-B620-2822915734DD).</summary>
        internal static ref readonly Guid IID_ILanguageExceptionErrorInfo2
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0xC4, 0xE5, 0x46, 0x57,
                    0x97, 0x5B,
                    0x4C, 0x42,
                    0xB6,
                    0x20,
                    0x28,
                    0x22,
                    0x91,
                    0x57,
                    0x34,
                    0xDD
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>IRestrictedErrorInfo</c> (82BA7092-4C88-427D-A7BC-16DD93FEB67E).</summary>
        internal static ref readonly Guid IID_IRestrictedErrorInfo
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x92, 0x70, 0xBA, 0x82,
                    0x88, 0x4C,
                    0x7D, 0x42,
                    0xA7,
                    0xBC,
                    0x16,
                    0xDD,
                    0x93,
                    0xFE,
                    0xB6,
                    0x7E
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>MUX_INotifyPropertyChanged</c> (90B17601-B065-586E-83D9-9ADC3A695284).</summary>
        internal static ref readonly Guid IID_MUX_INotifyPropertyChanged
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x01, 0x76, 0xB1, 0x90,
                    0x65, 0xB0,
                    0x6E, 0x58,
                    0x83,
                    0xD9,
                    0x9A,
                    0xDC,
                    0x3A,
                    0x69,
                    0x52,
                    0x84
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>WUX_INotifyPropertyChanged</c> (CF75D69C-F2F4-486B-B302-BB4C09BAEBFA).</summary>
        internal static ref readonly Guid IID_WUX_INotifyPropertyChanged
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x9C, 0xD6, 0x75, 0xCF,
                    0xF4, 0xF2,
                    0x6B, 0x48,
                    0xB3,
                    0x02,
                    0xBB,
                    0x4C,
                    0x09,
                    0xBA,
                    0xEB,
                    0xFA
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>MUX_INotifyCollectionChanged</c> (530155E1-28A5-5693-87CE-30724D95A06D).</summary>
        internal static ref readonly Guid IID_MUX_INotifyCollectionChanged
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0xE1, 0x55, 0x01, 0x53,
                    0xA5, 0x28,
                    0x93, 0x56,
                    0x87,
                    0xCE,
                    0x30,
                    0x72,
                    0x4D,
                    0x95,
                    0xA0,
                    0x6D
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>WUX_INotifyCollectionChanged</c> (28B167D5-1A31-465B-9B25-D5C3AE686C40).</summary>
        internal static ref readonly Guid IID_WUX_INotifyCollectionChanged
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0xD5, 0x67, 0xB1, 0x28,
                    0x31, 0x1A,
                    0x5B, 0x46,
                    0x9B,
                    0x25,
                    0xD5,
                    0xC3,
                    0xAE,
                    0x68,
                    0x6C,
                    0x40
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>MUX_INotifyCollectionChangedEventArgsFactory</c> (5108EBA4-4892-5A20-8374-A96815E0FD27).</summary>
        internal static ref readonly Guid IID_MUX_INotifyCollectionChangedEventArgsFactory
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0xA4, 0xEB, 0x08, 0x51,
                    0x92, 0x48,
                    0x20, 0x5A,
                    0x83,
                    0x74,
                    0xA9,
                    0x68,
                    0x15,
                    0xE0,
                    0xFD,
                    0x27
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>WUX_INotifyCollectionChangedEventArgsFactory</c> (B30C3E3A-DF8D-44A5-9A38-7AC0D08CE63D).</summary>
        internal static ref readonly Guid IID_WUX_INotifyCollectionChangedEventArgsFactory
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x3A, 0x3E, 0x0C, 0xB3,
                    0x8D, 0xDF,
                    0xA5, 0x44,
                    0x9A,
                    0x38,
                    0x7A,
                    0xC0,
                    0xD0,
                    0x8C,
                    0xE6,
                    0x3D
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>MUX_INotifyCollectionChangedEventArgs</c> (DA049FF2-D2E0-5FE8-8C7B-F87F26060B6F).</summary>
        internal static ref readonly Guid IID_MUX_INotifyCollectionChangedEventArgs
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0xF2, 0x9F, 0x04, 0xDA,
                    0xE0, 0xD2,
                    0xE8, 0x5F,
                    0x8C,
                    0x7B,
                    0xF8,
                    0x7F,
                    0x26,
                    0x06,
                    0x0B,
                    0x6F
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>WUX_INotifyCollectionChangedEventArgs</c> (4CF68D33-E3F2-4964-B85E-945B4F7E2F21).</summary>
        internal static ref readonly Guid IID_WUX_INotifyCollectionChangedEventArgs
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x33, 0x8D, 0xF6, 0x4C,
                    0xF2, 0xE3,
                    0x64, 0x49,
                    0xB8,
                    0x5E,
                    0x94,
                    0x5B,
                    0x4F,
                    0x7E,
                    0x2F,
                    0x21
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>MUX_NotifyCollectionChangedEventHandler</c> (8B0909DC-2005-5D93-BF8A-725F017BAA8D).</summary>
        internal static ref readonly Guid IID_MUX_NotifyCollectionChangedEventHandler
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0xDC, 0x09, 0x09, 0x8B,
                    0x05, 0x20,
                    0x93, 0x5D,
                    0xBF,
                    0x8A,
                    0x72,
                    0x5F,
                    0x01,
                    0x7B,
                    0xAA,
                    0x8D
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>WUX_NotifyCollectionChangedEventHandler</c> (CA10B37C-F382-4591-8557-5E24965279B0).</summary>
        internal static ref readonly Guid IID_WUX_NotifyCollectionChangedEventHandler
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x7C, 0xB3, 0x10, 0xCA,
                    0x82, 0xF3,
                    0x91, 0x45,
                    0x85,
                    0x57,
                    0x5E,
                    0x24,
                    0x96,
                    0x52,
                    0x79,
                    0xB0
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>MUX_PropertyChangedEventArgsRuntimeClassFactory</c> (7C0C27A8-0B41-5070-B160-FC9AE960A36C).</summary>
        internal static ref readonly Guid IID_MUX_PropertyChangedEventArgsRuntimeClassFactory
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0xA8, 0x27, 0x0C, 0x7C,
                    0x41, 0x0B,
                    0x70, 0x50,
                    0xB1,
                    0x60,
                    0xFC,
                    0x9A,
                    0xE9,
                    0x60,
                    0xA3,
                    0x6C
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>WUX_PropertyChangedEventArgsRuntimeClassFactory</c> (6DCC9C03-E0C7-4EEE-8EA9-37E3406EEB1C).</summary>
        internal static ref readonly Guid IID_WUX_PropertyChangedEventArgsRuntimeClassFactory
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x03, 0x9C, 0xCC, 0x6D,
                    0xC7, 0xE0,
                    0xEE, 0x4E,
                    0x8E,
                    0xA9,
                    0x37,
                    0xE3,
                    0x40,
                    0x6E,
                    0xEB,
                    0x1C
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>MUX_PropertyChangedEventHandler</c> (E3DE52F6-1E32-5DA6-BB2D-B5B6096C962D).</summary>
        internal static ref readonly Guid IID_MUX_PropertyChangedEventHandler
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0xF6, 0x52, 0xDE, 0xE3,
                    0x32, 0x1E,
                    0xA6, 0x5D,
                    0xBB,
                    0x2D,
                    0xB5,
                    0xB6,
                    0x09,
                    0x6C,
                    0x96,
                    0x2D
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>WUX_PropertyChangedEventHandler</c> (50F19C16-0A22-4D8E-A089-1EA9951657D2).</summary>
        internal static ref readonly Guid IID_WUX_PropertyChangedEventHandler
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x16, 0x9C, 0xF1, 0x50,
                    0x22, 0x0A,
                    0x8E, 0x4D,
                    0xA0,
                    0x89,
                    0x1E,
                    0xA9,
                    0x95,
                    0x16,
                    0x57,
                    0xD2
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>DataErrorsChangedEventArgsRuntimeClassFactory</c> (62D0BD1E-B85F-5FCC-842A-7CB0DDA37FE5).</summary>
        internal static ref readonly Guid IID_DataErrorsChangedEventArgsRuntimeClassFactory
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x1E, 0xBD, 0xD0, 0x62,
                    0x5F, 0xB8,
                    0xCC, 0x5F,
                    0x84,
                    0x2A,
                    0x7C,
                    0xB0,
                    0xDD,
                    0xA3,
                    0x7F,
                    0xE5
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>UriRuntimeClassFactory</c> (44A9796F-723E-4FDF-A218-033E75B0C084).</summary>
        internal static ref readonly Guid IID_UriRuntimeClassFactory
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x6F, 0x79, 0xA9, 0x44,
                    0x3E, 0x72,
                    0xDF, 0x4F,
                    0xA2,
                    0x18,
                    0x03,
                    0x3E,
                    0x75,
                    0xB0,
                    0xC0,
                    0x84
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>INotifyDataErrorInfo</c> (0EE6C2CC-273E-567D-BC0A-1DD87EE51EBA).</summary>
        internal static ref readonly Guid IID_INotifyDataErrorInfo
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0xCC, 0xC2, 0xE6, 0x0E,
                    0x3E, 0x27,
                    0x7D, 0x56,
                    0xBC,
                    0x0A,
                    0x1D,
                    0xD8,
                    0x7E,
                    0xE5,
                    0x1E,
                    0xBA
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>ICommand</c> (E5AF3542-CA67-4081-995B-709DD13792DF).</summary>
        internal static ref readonly Guid IID_ICommand
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x42, 0x35, 0xAF, 0xE5,
                    0x67, 0xCA,
                    0x81, 0x40,
                    0x99,
                    0x5B,
                    0x70,
                    0x9D,
                    0xD1,
                    0x37,
                    0x92,
                    0xDF
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>IGlobalInterfaceTable</c> (00000146-0000-0000-C000-000000000046).</summary>
        internal static ref readonly Guid IID_IGlobalInterfaceTable
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x46, 0x01, 0x00, 0x00,
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

        /// <summary>The IID for <c>EventHandler</c> (C50898F6-C536-5F47-8583-8B2C2438A13B).</summary>
        internal static ref readonly Guid IID_EventHandler
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0xF6, 0x98, 0x08, 0xC5,
                    0x36, 0xC5,
                    0x47, 0x5F,
                    0x85,
                    0x83,
                    0x8B,
                    0x2C,
                    0x24,
                    0x38,
                    0xA1,
                    0x3B
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>IBindableVectorView</c> (346DD6E7-976E-4BC3-815D-ECE243BC0F33).</summary>
        internal static ref readonly Guid IID_IBindableVectorView
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0xE7, 0xD6, 0x6D, 0x34,
                    0x6E, 0x97,
                    0xC3, 0x4B,
                    0x81,
                    0x5D,
                    0xEC,
                    0xE2,
                    0x43,
                    0xBC,
                    0x0F,
                    0x33
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>IEnumerable</c> (036D2C08-DF29-41AF-8AA2-D774BE62BA6F).</summary>
        internal static ref readonly Guid IID_IEnumerable
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x08, 0x2C, 0x6D, 0x03,
                    0x29, 0xDF,
                    0xAF, 0x41,
                    0x8A,
                    0xA2,
                    0xD7,
                    0x74,
                    0xBE,
                    0x62,
                    0xBA,
                    0x6F
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>IList</c> (393DE7DE-6FD0-4C0D-BB71-47244A113E93).</summary>
        internal static ref readonly Guid IID_IList
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0xDE, 0xE7, 0x3D, 0x39,
                    0xD0, 0x6F,
                    0x0D, 0x4C,
                    0xBB,
                    0x71,
                    0x47,
                    0x24,
                    0x4A,
                    0x11,
                    0x3E,
                    0x93
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>ICustomProperty</c> (30DA92C0-23E8-42A0-AE7C-734A0E5D2782).</summary>
        internal static ref readonly Guid IID_ICustomProperty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0xC0, 0x92, 0xDA, 0x30,
                    0xE8, 0x23,
                    0xA0, 0x42,
                    0xAE,
                    0x7C,
                    0x73,
                    0x4A,
                    0x0E,
                    0x5D,
                    0x27,
                    0x82
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>ICustomPropertyProvider</c> (7C925755-3E48-42B4-8677-76372267033F).</summary>
        internal static ref readonly Guid IID_ICustomPropertyProvider
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x55, 0x57, 0x92, 0x7C,
                    0x48, 0x3E,
                    0xB4, 0x42,
                    0x86,
                    0x77,
                    0x76,
                    0x37,
                    0x22,
                    0x67,
                    0x03,
                    0x3F
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>IPropertyValue</c> (4BD682DD-7554-40E9-9A9B-82654EDE7E62).</summary>
        internal static ref readonly Guid IID_IPropertyValue
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0xDD, 0x82, 0xD6, 0x4B,
                    0x54, 0x75,
                    0xE9, 0x40,
                    0x9A,
                    0x9B,
                    0x82,
                    0x65,
                    0x4E,
                    0xDE,
                    0x7E,
                    0x62
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>IDisposable</c> (30D5A829-7FA4-4026-83BB-D75BAE4EA99E).</summary>
        internal static ref readonly Guid IID_IDisposable
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x29, 0xA8, 0xD5, 0x30,
                    0xA4, 0x7F,
                    0x26, 0x40,
                    0x83,
                    0xBB,
                    0xD7,
                    0x5B,
                    0xAE,
                    0x4E,
                    0xA9,
                    0x9E
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>IStringable</c> (96369F54-8EB6-48F0-ABCE-C1B211E627C3).</summary>
        internal static ref readonly Guid IID_IStringable
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x54, 0x9F, 0x36, 0x96,
                    0xB6, 0x8E,
                    0xF0, 0x48,
                    0xAB,
                    0xCE,
                    0xC1,
                    0xB2,
                    0x11,
                    0xE6,
                    0x27,
                    0xC3
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>IServiceProvider</c> (68B3A2DF-8173-539F-B524-C8A2348F5AFB).</summary>
        internal static ref readonly Guid IID_IServiceProvider
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0xDF, 0xA2, 0xB3, 0x68,
                    0x73, 0x81,
                    0x9F, 0x53,
                    0xB5,
                    0x24,
                    0xC8,
                    0xA2,
                    0x34,
                    0x8F,
                    0x5A,
                    0xFB
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>IReferenceOfPoint</c> (84F14C22-A00A-5272-8D3D-82112E66DF00).</summary>
        internal static ref readonly Guid IID_IReferenceOfPoint
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x22, 0x4C, 0xF1, 0x84,
                    0x0A, 0xA0,
                    0x72, 0x52,
                    0x8D,
                    0x3D,
                    0x82,
                    0x11,
                    0x2E,
                    0x66,
                    0xDF,
                    0x00
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>IReferenceOfSize</c> (61723086-8E53-5276-9F36-2A4BB93E2B75).</summary>
        internal static ref readonly Guid IID_IReferenceOfSize
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x86, 0x30, 0x72, 0x61,
                    0x53, 0x8E,
                    0x76, 0x52,
                    0x9F,
                    0x36,
                    0x2A,
                    0x4B,
                    0xB9,
                    0x3E,
                    0x2B,
                    0x75
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>IReferenceOfRect</c> (80423F11-054F-5EAC-AFD3-63B6CE15E77B).</summary>
        internal static ref readonly Guid IID_IReferenceOfRect
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x11, 0x3F, 0x42, 0x80,
                    0x4F, 0x05,
                    0xAC, 0x5E,
                    0xAF,
                    0xD3,
                    0x63,
                    0xB6,
                    0xCE,
                    0x15,
                    0xE7,
                    0x7B
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>IReferenceMatrix3x2</c> (76358CFD-2CBD-525B-A49E-90EE18247B71).</summary>
        internal static ref readonly Guid IID_IReferenceMatrix3x2
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0xFD, 0x8C, 0x35, 0x76,
                    0xBD, 0x2C,
                    0x5B, 0x52,
                    0xA4,
                    0x9E,
                    0x90,
                    0xEE,
                    0x18,
                    0x24,
                    0x7B,
                    0x71
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>IReferenceMatrix4x4</c> (DACBFFDC-68EF-5FD0-B657-782D0AC9807E).</summary>
        internal static ref readonly Guid IID_IReferenceMatrix4x4
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0xDC, 0xFF, 0xCB, 0xDA,
                    0xEF, 0x68,
                    0xD0, 0x5F,
                    0xB6,
                    0x57,
                    0x78,
                    0x2D,
                    0x0A,
                    0xC9,
                    0x80,
                    0x7E
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>IReferencePlane</c> (46D542A1-52F7-58E7-ACFC-9A6D364DA022).</summary>
        internal static ref readonly Guid IID_IReferencePlane
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0xA1, 0x42, 0xD5, 0x46,
                    0xF7, 0x52,
                    0xE7, 0x58,
                    0xAC,
                    0xFC,
                    0x9A,
                    0x6D,
                    0x36,
                    0x4D,
                    0xA0,
                    0x22
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>IReferenceQuaternion</c> (B27004BB-C014-5DCE-9A21-799C5A3C1461).</summary>
        internal static ref readonly Guid IID_IReferenceQuaternion
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0xBB, 0x04, 0x70, 0xB2,
                    0x14, 0xC0,
                    0xCE, 0x5D,
                    0x9A,
                    0x21,
                    0x79,
                    0x9C,
                    0x5A,
                    0x3C,
                    0x14,
                    0x61
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>IReferenceVector2</c> (48F6A69E-8465-57AE-9400-9764087F65AD).</summary>
        internal static ref readonly Guid IID_IReferenceVector2
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x9E, 0xA6, 0xF6, 0x48,
                    0x65, 0x84,
                    0xAE, 0x57,
                    0x94,
                    0x00,
                    0x97,
                    0x64,
                    0x08,
                    0x7F,
                    0x65,
                    0xAD
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>IReferenceVector3</c> (1EE770FF-C954-59CA-A754-6199A9BE282C).</summary>
        internal static ref readonly Guid IID_IReferenceVector3
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0xFF, 0x70, 0xE7, 0x1E,
                    0x54, 0xC9,
                    0xCA, 0x59,
                    0xA7,
                    0x54,
                    0x61,
                    0x99,
                    0xA9,
                    0xBE,
                    0x28,
                    0x2C
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>IReferenceVector4</c> (A5E843C9-ED20-5339-8F8D-9FE404CF3654).</summary>
        internal static ref readonly Guid IID_IReferenceVector4
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0xC9, 0x43, 0xE8, 0xA5,
                    0x20, 0xED,
                    0x39, 0x53,
                    0x8F,
                    0x8D,
                    0x9F,
                    0xE4,
                    0x04,
                    0xCF,
                    0x36,
                    0x54
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>IReferenceArrayOfInt32</c> (A6D080A5-B087-5BC2-9A9F-5CD687B4D1F7).</summary>
        internal static ref readonly Guid IID_IReferenceArrayOfInt32
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0xA5, 0x80, 0xD0, 0xA6,
                    0x87, 0xB0,
                    0xC2, 0x5B,
                    0x9A,
                    0x9F,
                    0x5C,
                    0xD6,
                    0x87,
                    0xB4,
                    0xD1,
                    0xF7
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>IReferenceArrayOfString</c> (0385688E-E3C7-5C5E-A389-5524EDE349F1).</summary>
        internal static ref readonly Guid IID_IReferenceArrayOfString
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x8E, 0x68, 0x85, 0x03,
                    0xC7, 0xE3,
                    0x5E, 0x5C,
                    0xA3,
                    0x89,
                    0x55,
                    0x24,
                    0xED,
                    0xE3,
                    0x49,
                    0xF1
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>IReferenceArrayOfByte</c> (2AF22683-3734-56D0-A60E-688CC85D1619).</summary>
        internal static ref readonly Guid IID_IReferenceArrayOfByte
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x83, 0x26, 0xF2, 0x2A,
                    0x34, 0x37,
                    0xD0, 0x56,
                    0xA6,
                    0x0E,
                    0x68,
                    0x8C,
                    0xC8,
                    0x5D,
                    0x16,
                    0x19
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>IReferenceArrayOfInt16</c> (912F8FD7-ADC0-5D60-A896-7ED76089CC5B).</summary>
        internal static ref readonly Guid IID_IReferenceArrayOfInt16
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0xD7, 0x8F, 0x2F, 0x91,
                    0xC0, 0xAD,
                    0x60, 0x5D,
                    0xA8,
                    0x96,
                    0x7E,
                    0xD7,
                    0x60,
                    0x89,
                    0xCC,
                    0x5B
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>IReferenceArrayOfUInt16</c> (6624A2DD-83F7-519C-9D55-BB1F6560456B).</summary>
        internal static ref readonly Guid IID_IReferenceArrayOfUInt16
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0xDD, 0xA2, 0x24, 0x66,
                    0xF7, 0x83,
                    0x9C, 0x51,
                    0x9D,
                    0x55,
                    0xBB,
                    0x1F,
                    0x65,
                    0x60,
                    0x45,
                    0x6B
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>IReferenceArrayOfUInt32</c> (97374B68-EB87-56CC-B18E-27EF0F9CFC0C).</summary>
        internal static ref readonly Guid IID_IReferenceArrayOfUInt32
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x68, 0x4B, 0x37, 0x97,
                    0x87, 0xEB,
                    0xCC, 0x56,
                    0xB1,
                    0x8E,
                    0x27,
                    0xEF,
                    0x0F,
                    0x9C,
                    0xFC,
                    0x0C
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>IReferenceArrayOfInt64</c> (6E333271-2E2A-5955-8790-836C76EE53B6).</summary>
        internal static ref readonly Guid IID_IReferenceArrayOfInt64
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x71, 0x32, 0x33, 0x6E,
                    0x2A, 0x2E,
                    0x55, 0x59,
                    0x87,
                    0x90,
                    0x83,
                    0x6C,
                    0x76,
                    0xEE,
                    0x53,
                    0xB6
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>IReferenceArrayOfUInt64</c> (38B60434-D67C-523E-9D0E-24D643411073).</summary>
        internal static ref readonly Guid IID_IReferenceArrayOfUInt64
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x34, 0x04, 0xB6, 0x38,
                    0x7C, 0xD6,
                    0x3E, 0x52,
                    0x9D,
                    0x0E,
                    0x24,
                    0xD6,
                    0x43,
                    0x41,
                    0x10,
                    0x73
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>IReferenceArrayOfSingle</c> (6AB1EA83-CB41-5F99-92CC-23BD4336A1FB).</summary>
        internal static ref readonly Guid IID_IReferenceArrayOfSingle
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x83, 0xEA, 0xB1, 0x6A,
                    0x41, 0xCB,
                    0x99, 0x5F,
                    0x92,
                    0xCC,
                    0x23,
                    0xBD,
                    0x43,
                    0x36,
                    0xA1,
                    0xFB
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>IReferenceArrayOfDouble</c> (D301F253-E0A3-5D2B-9A41-A4D62BEC4623).</summary>
        internal static ref readonly Guid IID_IReferenceArrayOfDouble
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x53, 0xF2, 0x01, 0xD3,
                    0xA3, 0xE0,
                    0x2B, 0x5D,
                    0x9A,
                    0x41,
                    0xA4,
                    0xD6,
                    0x2B,
                    0xEC,
                    0x46,
                    0x23
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>IReferenceArrayOfChar</c> (A4095AAB-EB7D-5782-8FAD-1609DEA249AD).</summary>
        internal static ref readonly Guid IID_IReferenceArrayOfChar
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0xAB, 0x5A, 0x09, 0xA4,
                    0x7D, 0xEB,
                    0x82, 0x57,
                    0x8F,
                    0xAD,
                    0x16,
                    0x09,
                    0xDE,
                    0xA2,
                    0x49,
                    0xAD
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>IReferenceArrayOfBoolean</c> (E8E72666-48CC-593F-BA85-2663496956E3).</summary>
        internal static ref readonly Guid IID_IReferenceArrayOfBoolean
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x66, 0x26, 0xE7, 0xE8,
                    0xCC, 0x48,
                    0x3F, 0x59,
                    0xBA,
                    0x85,
                    0x26,
                    0x63,
                    0x49,
                    0x69,
                    0x56,
                    0xE3
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>IReferenceArrayOfGuid</c> (EECF9838-C1C2-5B4A-976F-CEC261AE1D55).</summary>
        internal static ref readonly Guid IID_IReferenceArrayOfGuid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x38, 0x98, 0xCF, 0xEE,
                    0xC2, 0xC1,
                    0x4A, 0x5B,
                    0x97,
                    0x6F,
                    0xCE,
                    0xC2,
                    0x61,
                    0xAE,
                    0x1D,
                    0x55
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>IReferenceArrayOfDateTimeOffset</c> (1B8E9594-588E-5A07-9E65-0731A4C9A2DB).</summary>
        internal static ref readonly Guid IID_IReferenceArrayOfDateTimeOffset
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x94, 0x95, 0x8E, 0x1B,
                    0x8E, 0x58,
                    0x07, 0x5A,
                    0x9E,
                    0x65,
                    0x07,
                    0x31,
                    0xA4,
                    0xC9,
                    0xA2,
                    0xDB
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>IReferenceArrayOfTimeSpan</c> (AD73197D-2CFA-57A6-8993-9FAC40FEB791).</summary>
        internal static ref readonly Guid IID_IReferenceArrayOfTimeSpan
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x7D, 0x19, 0x73, 0xAD,
                    0xFA, 0x2C,
                    0xA6, 0x57,
                    0x89,
                    0x93,
                    0x9F,
                    0xAC,
                    0x40,
                    0xFE,
                    0xB7,
                    0x91
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>IReferenceArrayOfObject</c> (9CD7A84F-0C80-59C5-B44E-977841BB43D9).</summary>
        internal static ref readonly Guid IID_IReferenceArrayOfObject
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x4F, 0xA8, 0xD7, 0x9C,
                    0x80, 0x0C,
                    0xC5, 0x59,
                    0xB4,
                    0x4E,
                    0x97,
                    0x78,
                    0x41,
                    0xBB,
                    0x43,
                    0xD9
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>IReferenceArrayOfType</c> (DA8457A7-C2EB-5DA1-80BE-7132A2E1BFA4).</summary>
        internal static ref readonly Guid IID_IReferenceArrayOfType
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0xA7, 0x57, 0x84, 0xDA,
                    0xEB, 0xC2,
                    0xA1, 0x5D,
                    0x80,
                    0xBE,
                    0x71,
                    0x32,
                    0xA2,
                    0xE1,
                    0xBF,
                    0xA4
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>IReferenceArrayOfMatrix3x2</c> (A525D9FF-C09B-501A-A785-4D1ED9E102B8).</summary>
        internal static ref readonly Guid IID_IReferenceArrayOfMatrix3x2
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0xFF, 0xD9, 0x25, 0xA5,
                    0x9B, 0xC0,
                    0x1A, 0x50,
                    0xA7,
                    0x85,
                    0x4D,
                    0x1E,
                    0xD9,
                    0xE1,
                    0x02,
                    0xB8
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>IReferenceArrayOfMatrix4x4</c> (FC0D5A15-8F9D-5E8F-8828-AEF2C2E25BAD).</summary>
        internal static ref readonly Guid IID_IReferenceArrayOfMatrix4x4
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x15, 0x5A, 0x0D, 0xFC,
                    0x9D, 0x8F,
                    0x8F, 0x5E,
                    0x88,
                    0x28,
                    0xAE,
                    0xF2,
                    0xC2,
                    0xE2,
                    0x5B,
                    0xAD
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>IReferenceArrayOfPlane</c> (F9CF7F7D-5459-5F98-91B9-F2632A9EC298).</summary>
        internal static ref readonly Guid IID_IReferenceArrayOfPlane
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x7D, 0x7F, 0xCF, 0xF9,
                    0x59, 0x54,
                    0x98, 0x5F,
                    0x91,
                    0xB9,
                    0xF2,
                    0x63,
                    0x2A,
                    0x9E,
                    0xC2,
                    0x98
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>IReferenceArrayOfQuaternion</c> (E9BA76BE-2C31-5E1D-98A4-EBDB625AEE93).</summary>
        internal static ref readonly Guid IID_IReferenceArrayOfQuaternion
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0xBE, 0x76, 0xBA, 0xE9,
                    0x31, 0x2C,
                    0x1D, 0x5E,
                    0x98,
                    0xA4,
                    0xEB,
                    0xDB,
                    0x62,
                    0x5A,
                    0xEE,
                    0x93
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>IReferenceArrayOfVector2</c> (29DF2178-FFDB-563E-88DB-3869A007305E).</summary>
        internal static ref readonly Guid IID_IReferenceArrayOfVector2
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x78, 0x21, 0xDF, 0x29,
                    0xDB, 0xFF,
                    0x3E, 0x56,
                    0x88,
                    0xDB,
                    0x38,
                    0x69,
                    0xA0,
                    0x07,
                    0x30,
                    0x5E
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>IReferenceArrayOfVector3</c> (AA1A35FA-0B4E-5248-BD79-FFD47CFE4027).</summary>
        internal static ref readonly Guid IID_IReferenceArrayOfVector3
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0xFA, 0x35, 0x1A, 0xAA,
                    0x4E, 0x0B,
                    0x48, 0x52,
                    0xBD,
                    0x79,
                    0xFF,
                    0xD4,
                    0x7C,
                    0xFE,
                    0x40,
                    0x27
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>IReferenceArrayOfVector4</c> (68757250-5849-5772-90E3-AADB4C970BFF).</summary>
        internal static ref readonly Guid IID_IReferenceArrayOfVector4
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x50, 0x72, 0x75, 0x68,
                    0x49, 0x58,
                    0x72, 0x57,
                    0x90,
                    0xE3,
                    0xAA,
                    0xDB,
                    0x4C,
                    0x97,
                    0x0B,
                    0xFF
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>IReferenceArrayOfException</c> (401AE4CC-4AB9-5A8F-B993-E327900C364D).</summary>
        internal static ref readonly Guid IID_IReferenceArrayOfException
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0xCC, 0xE4, 0x1A, 0x40,
                    0xB9, 0x4A,
                    0x8F, 0x5A,
                    0xB9,
                    0x93,
                    0xE3,
                    0x27,
                    0x90,
                    0x0C,
                    0x36,
                    0x4D
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>NullableByte</c> (E5198CC8-2873-55F5-B0A1-84FF9E4AAD62).</summary>
        internal static ref readonly Guid IID_NullableByte
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0xC8, 0x8C, 0x19, 0xE5,
                    0x73, 0x28,
                    0xF5, 0x55,
                    0xB0,
                    0xA1,
                    0x84,
                    0xFF,
                    0x9E,
                    0x4A,
                    0xAD,
                    0x62
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>NullableSByte</c> (95500129-FBF6-5AFC-89DF-70642D741990).</summary>
        internal static ref readonly Guid IID_NullableSByte
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x29, 0x01, 0x50, 0x95,
                    0xF6, 0xFB,
                    0xFC, 0x5A,
                    0x89,
                    0xDF,
                    0x70,
                    0x64,
                    0x2D,
                    0x74,
                    0x19,
                    0x90
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>NullableShort</c> (6EC9E41B-6709-5647-9918-A1270110FC4E).</summary>
        internal static ref readonly Guid IID_NullableShort
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x1B, 0xE4, 0xC9, 0x6E,
                    0x09, 0x67,
                    0x47, 0x56,
                    0x99,
                    0x18,
                    0xA1,
                    0x27,
                    0x01,
                    0x10,
                    0xFC,
                    0x4E
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>NullableUShort</c> (5AB7D2C3-6B62-5E71-A4B6-2D49C4F238FD).</summary>
        internal static ref readonly Guid IID_NullableUShort
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0xC3, 0xD2, 0xB7, 0x5A,
                    0x62, 0x6B,
                    0x71, 0x5E,
                    0xA4,
                    0xB6,
                    0x2D,
                    0x49,
                    0xC4,
                    0xF2,
                    0x38,
                    0xFD
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>NullableInt</c> (548CEFBD-BC8A-5FA0-8DF2-957440FC8BF4).</summary>
        internal static ref readonly Guid IID_NullableInt
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0xBD, 0xEF, 0x8C, 0x54,
                    0x8A, 0xBC,
                    0xA0, 0x5F,
                    0x8D,
                    0xF2,
                    0x95,
                    0x74,
                    0x40,
                    0xFC,
                    0x8B,
                    0xF4
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>NullableUInt</c> (513EF3AF-E784-5325-A91E-97C2B8111CF3).</summary>
        internal static ref readonly Guid IID_NullableUInt
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0xAF, 0xF3, 0x3E, 0x51,
                    0x84, 0xE7,
                    0x25, 0x53,
                    0xA9,
                    0x1E,
                    0x97,
                    0xC2,
                    0xB8,
                    0x11,
                    0x1C,
                    0xF3
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>NullableLong</c> (4DDA9E24-E69F-5C6A-A0A6-93427365AF2A).</summary>
        internal static ref readonly Guid IID_NullableLong
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x24, 0x9E, 0xDA, 0x4D,
                    0x9F, 0xE6,
                    0x6A, 0x5C,
                    0xA0,
                    0xA6,
                    0x93,
                    0x42,
                    0x73,
                    0x65,
                    0xAF,
                    0x2A
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>NullableULong</c> (6755E376-53BB-568B-A11D-17239868309E).</summary>
        internal static ref readonly Guid IID_NullableULong
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x76, 0xE3, 0x55, 0x67,
                    0xBB, 0x53,
                    0x8B, 0x56,
                    0xA1,
                    0x1D,
                    0x17,
                    0x23,
                    0x98,
                    0x68,
                    0x30,
                    0x9E
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>NullableFloat</c> (719CC2BA-3E76-5DEF-9F1A-38D85A145EA8).</summary>
        internal static ref readonly Guid IID_NullableFloat
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0xBA, 0xC2, 0x9C, 0x71,
                    0x76, 0x3E,
                    0xEF, 0x5D,
                    0x9F,
                    0x1A,
                    0x38,
                    0xD8,
                    0x5A,
                    0x14,
                    0x5E,
                    0xA8
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>NullableDouble</c> (2F2D6C29-5473-5F3E-92E7-96572BB990E2).</summary>
        internal static ref readonly Guid IID_NullableDouble
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x29, 0x6C, 0x2D, 0x2F,
                    0x73, 0x54,
                    0x3E, 0x5F,
                    0x92,
                    0xE7,
                    0x96,
                    0x57,
                    0x2B,
                    0xB9,
                    0x90,
                    0xE2
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>NullableChar</c> (FB393EF3-BBAC-5BD5-9144-84F23576F415).</summary>
        internal static ref readonly Guid IID_NullableChar
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0xF3, 0x3E, 0x39, 0xFB,
                    0xAC, 0xBB,
                    0xD5, 0x5B,
                    0x91,
                    0x44,
                    0x84,
                    0xF2,
                    0x35,
                    0x76,
                    0xF4,
                    0x15
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>NullableBool</c> (3C00FD60-2950-5939-A21A-2D12C5A01B8A).</summary>
        internal static ref readonly Guid IID_NullableBool
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x60, 0xFD, 0x00, 0x3C,
                    0x50, 0x29,
                    0x39, 0x59,
                    0xA2,
                    0x1A,
                    0x2D,
                    0x12,
                    0xC5,
                    0xA0,
                    0x1B,
                    0x8A
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>NullableGuid</c> (7D50F649-632C-51F9-849A-EE49428933EA).</summary>
        internal static ref readonly Guid IID_NullableGuid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x49, 0xF6, 0x50, 0x7D,
                    0x2C, 0x63,
                    0xF9, 0x51,
                    0x84,
                    0x9A,
                    0xEE,
                    0x49,
                    0x42,
                    0x89,
                    0x33,
                    0xEA
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>NullableDateTimeOffset</c> (5541D8A7-497C-5AA4-86FC-7713ADBF2A2C).</summary>
        internal static ref readonly Guid IID_NullableDateTimeOffset
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0xA7, 0xD8, 0x41, 0x55,
                    0x7C, 0x49,
                    0xA4, 0x5A,
                    0x86,
                    0xFC,
                    0x77,
                    0x13,
                    0xAD,
                    0xBF,
                    0x2A,
                    0x2C
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>NullableTimeSpan</c> (604D0C4C-91DE-5C2A-935F-362F13EAF800).</summary>
        internal static ref readonly Guid IID_NullableTimeSpan
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x4C, 0x0C, 0x4D, 0x60,
                    0xDE, 0x91,
                    0x2A, 0x5C,
                    0x93,
                    0x5F,
                    0x36,
                    0x2F,
                    0x13,
                    0xEA,
                    0xF8,
                    0x00
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>NullableObject</c> (06DCCC90-A058-5C88-87B7-6F3360A2FC16).</summary>
        internal static ref readonly Guid IID_NullableObject
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x90, 0xCC, 0xDC, 0x06,
                    0x58, 0xA0,
                    0x88, 0x5C,
                    0x87,
                    0xB7,
                    0x6F,
                    0x33,
                    0x60,
                    0xA2,
                    0xFC,
                    0x16
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>NullableType</c> (3830AD99-D8DA-53F3-989B-FC92AD222778).</summary>
        internal static ref readonly Guid IID_NullableType
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x99, 0xAD, 0x30, 0x38,
                    0xDA, 0xD8,
                    0xF3, 0x53,
                    0x98,
                    0x9B,
                    0xFC,
                    0x92,
                    0xAD,
                    0x22,
                    0x27,
                    0x78
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>NullableException</c> (6FF27A1E-4B6A-59B7-B2C3-D1F2EE474593).</summary>
        internal static ref readonly Guid IID_NullableException
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x1E, 0x7A, 0xF2, 0x6F,
                    0x6A, 0x4B,
                    0xB7, 0x59,
                    0xB2,
                    0xC3,
                    0xD1,
                    0xF2,
                    0xEE,
                    0x47,
                    0x45,
                    0x93
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>NullableEventHandler</c> (25230F05-B49C-57EE-8961-5373D98E1AB1).</summary>
        internal static ref readonly Guid IID_NullableEventHandler
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x05, 0x0F, 0x23, 0x25,
                    0x9C, 0xB4,
                    0xEE, 0x57,
                    0x89,
                    0x61,
                    0x53,
                    0x73,
                    0xD9,
                    0x8E,
                    0x1A,
                    0xB1
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>NullableString</c> (FD416DFB-2A07-52EB-AAE3-DFCE14116C05).</summary>
        internal static ref readonly Guid IID_NullableString
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0xFB, 0x6D, 0x41, 0xFD,
                    0x07, 0x2A,
                    0xEB, 0x52,
                    0xAA,
                    0xE3,
                    0xDF,
                    0xCE,
                    0x14,
                    0x11,
                    0x6C,
                    0x05
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>MUX_NullablePropertyChangedEventHandler</c> (1EEAE0CB-8F57-5C37-A087-A55d46E2FE3F).</summary>
        internal static ref readonly Guid IID_MUX_NullablePropertyChangedEventHandler
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0xCB, 0xE0, 0xEA, 0x1E,
                    0x57, 0x8F,
                    0x37, 0x5C,
                    0xA0,
                    0x87,
                    0xA5,
                    0x5D,
                    0x46,
                    0xE2,
                    0xFE,
                    0x3F
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>WUX_NullablePropertyChangedEventHandler</c> (B1A920A9-C2F2-5453-A53E-66B1294A8BFE).</summary>
        internal static ref readonly Guid IID_WUX_NullablePropertyChangedEventHandler
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0xA9, 0x20, 0xA9, 0xB1,
                    0xF2, 0xC2,
                    0x53, 0x54,
                    0xA5,
                    0x3E,
                    0x66,
                    0xB1,
                    0x29,
                    0x4A,
                    0x8B,
                    0xFE
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>MUX_NullableNotifyCollectionChangedEventHandler</c> (779D5A21-0E7d-5476-BB90-27FA3B4B8DE5).</summary>
        internal static ref readonly Guid IID_MUX_NullableNotifyCollectionChangedEventHandler
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x21, 0x5A, 0x9D, 0x77,
                    0x7D, 0x0E,
                    0x76, 0x54,
                    0xBB,
                    0x90,
                    0x27,
                    0xFA,
                    0x3B,
                    0x4B,
                    0x8D,
                    0xE5
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        /// <summary>The IID for <c>WUX_NullableNotifyCollectionChangedEventHandler</c> (A4FD5C6E-6549-59A9-86EF-5A490A1875D9).</summary>
        internal static ref readonly Guid IID_WUX_NullableNotifyCollectionChangedEventHandler
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data = new byte[]
                {
                    0x6E, 0x5C, 0xFD, 0xA4,
                    0x49, 0x65,
                    0xA9, 0x59,
                    0x86,
                    0xEF,
                    0x5A,
                    0x49,
                    0x0A,
                    0x18,
                    0x75,
                    0xD9
                };

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }
    }
}