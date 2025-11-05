namespace ABI.Windows.Storage.Streams
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct IMemoryBufferByteAccessVftbl
    {
        public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, int> QueryInterface;
        public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
        public delegate* unmanaged[MemberFunction]<void*, uint> Release;
        public delegate* unmanaged[MemberFunction]<void*, byte**, uint*, int> GetBuffer;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetBufferUnsafe(
            void* thisPtr,
            byte** result,
            uint* capacity)
        {
            return ((IMemoryBufferByteAccessVftbl*)*(void***)thisPtr)->GetBuffer(thisPtr, result, capacity);
        }
    }
}
