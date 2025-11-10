namespace Windows.Storage.Streams
{
    [WindowsRuntimeMetadata("Windows.Foundation.UniversalApiContract")]
    [Guid("905a0fef-bc53-11df-8c49-001e4fc686da")]
    internal unsafe interface IBufferByteAccess
    {
        byte* Buffer { get; }
    }
}

namespace ABI.Windows.Storage.Streams
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct IBufferByteAccessVftbl
    {
        public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, int> QueryInterface;
        public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
        public delegate* unmanaged[MemberFunction]<void*, uint> Release;
        public delegate* unmanaged[MemberFunction]<void*, byte**, int> get_Buffer;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetBufferUnsafe(
            void* thisPtr,
            byte** result)
        {
            return ((IBufferByteAccessVftbl*)*(void***)thisPtr)->get_Buffer(thisPtr, result);
        }
    }

    internal static unsafe class IBufferByteAccessImpl
    {
        [FixedAddressValueType]
        private static readonly IBufferByteAccessVftbl Vftbl;

        static IBufferByteAccessImpl()
        {
            *(IUnknownVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IUnknownVftbl*)IUnknownImpl.Vtable;
            Vftbl.get_Buffer = &Do_Abi_get_Buffer;
        }

        public static nint Vtable
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (nint)Unsafe.AsPointer(in Vftbl);
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvMemberFunction) })]
        private static unsafe int Do_Abi_get_Buffer(void* thisPtr, byte** result)
        {
            *result = default;

            try
            {
                *result = ComInterfaceDispatch.GetInstance<global::Windows.Storage.Streams.IBufferByteAccess>((ComInterfaceDispatch*)thisPtr).Buffer;
                return 0;
            }
            catch (Exception __exception__)
            {
                return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(__exception__);
            }
        }
    }
}
