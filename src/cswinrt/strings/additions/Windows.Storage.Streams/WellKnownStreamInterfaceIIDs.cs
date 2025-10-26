namespace ABI.Windows.Storage.Streams
{
    internal static class WellKnownStreamInterfaceIIDs
    {
        public static ref readonly Guid IID_IBufferByteAccess
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ReadOnlySpan<byte> data =
                [
                    0xEF, 0x0F, 0x5A, 0x90, 0x53, 0xBC, 0xDF, 0x11, 0x8C, 0x49, 0x00, 0x1E, 0x4F, 0xC6, 0x86, 0xDA
                ];
                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        public static ref readonly Guid IID_IMarshal
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ReadOnlySpan<byte> data =
                [
                    0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46
                ];

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        public static ref readonly Guid IID_IMemoryBufferByteAccess
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ReadOnlySpan<byte> data =
                [
                    0x35, 0x32, 0x0D, 0x5B, 0xBA, 0x4D, 0x44, 0x4D, 0x86, 0x5E, 0x8F, 0x1D, 0x0E, 0x4F, 0xD0, 0x4D
                ];
                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }
    }
}
