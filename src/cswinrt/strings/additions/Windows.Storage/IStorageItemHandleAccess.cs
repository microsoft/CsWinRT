// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Windows.Storage
{
    using Microsoft.Win32.SafeHandles;
    // Available in 14393 (RS1) and later
    [Guid("5CA296B2-2C25-4D22-B785-B885C8201E6A")]
    internal interface IStorageItemHandleAccess
    {
        SafeFileHandle Create(
            HANDLE_ACCESS_OPTIONS accessOptions,
            HANDLE_SHARING_OPTIONS sharingOptions,
            HANDLE_OPTIONS options,
            IntPtr oplockBreakingHandler);
    }
}

namespace ABI.Windows.Storage
{
    using global::Microsoft.Win32.SafeHandles;
    using global::System.ComponentModel;
    
#if !NET
    [global::WinRT.ObjectReferenceWrapper(nameof(_obj)), EditorBrowsable(EditorBrowsableState.Never)]
    [Guid("5CA296B2-2C25-4D22-B785-B885C8201E6A")]
    internal unsafe class IStorageItemHandleAccess : global::Windows.Storage.IStorageItemHandleAccess
    {
        [Guid("5CA296B2-2C25-4D22-B785-B885C8201E6A")]
        public struct Vftbl
        {
            public IUnknownVftbl IUnknownVftbl;
            private void* _Create_0;
            public delegate* unmanaged[Stdcall]<IntPtr, global::Windows.Storage.HANDLE_ACCESS_OPTIONS, global::Windows.Storage.HANDLE_SHARING_OPTIONS, global::Windows.Storage.HANDLE_OPTIONS, IntPtr, out IntPtr, int> Create_0 => (delegate* unmanaged[Stdcall]<IntPtr, global::Windows.Storage.HANDLE_ACCESS_OPTIONS, global::Windows.Storage.HANDLE_SHARING_OPTIONS, global::Windows.Storage.HANDLE_OPTIONS, IntPtr, out IntPtr, int>)_Create_0;
        }

        internal static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr);

        public static implicit operator IStorageItemHandleAccess(IObjectReference obj) => (obj != null) ? new IStorageItemHandleAccess(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IObjectReference ObjRef { get => _obj; }
        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public IStorageItemHandleAccess(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        internal IStorageItemHandleAccess(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
        }

        public SafeFileHandle Create(
            global::Windows.Storage.HANDLE_ACCESS_OPTIONS accessOptions,
            global::Windows.Storage.HANDLE_SHARING_OPTIONS sharingOptions,
            global::Windows.Storage.HANDLE_OPTIONS options,
            IntPtr oplockBreakingHandler)
        {
            SafeFileHandle interopHandle = default;
            IntPtr _interopHandle = default;
            try
            {
                ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.Create_0(ThisPtr, accessOptions, sharingOptions, options, oplockBreakingHandler, out _interopHandle));
            }
            finally
            {
                interopHandle = new SafeFileHandle(_interopHandle, true);
            }
            return interopHandle;
        }
    }
#else

    [DynamicInterfaceCastableImplementation]
    [Guid("5CA296B2-2C25-4D22-B785-B885C8201E6A")]
    internal unsafe interface IStorageItemHandleAccess : global::Windows.Storage.IStorageItemHandleAccess
    {
        [Guid("5CA296B2-2C25-4D22-B785-B885C8201E6A")]
        public struct Vftbl
        {
            public IUnknownVftbl IUnknownVftbl;
            private void* _Create_0;
            public delegate* unmanaged[Stdcall]<IntPtr, global::Windows.Storage.HANDLE_ACCESS_OPTIONS, global::Windows.Storage.HANDLE_SHARING_OPTIONS, global::Windows.Storage.HANDLE_OPTIONS, IntPtr, out IntPtr, int> Create_0 => (delegate* unmanaged[Stdcall]<IntPtr, global::Windows.Storage.HANDLE_ACCESS_OPTIONS, global::Windows.Storage.HANDLE_SHARING_OPTIONS, global::Windows.Storage.HANDLE_OPTIONS, IntPtr, out IntPtr, int>)_Create_0;
        }

        internal static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr);

        SafeFileHandle global::Windows.Storage.IStorageItemHandleAccess.Create(
            global::Windows.Storage.HANDLE_ACCESS_OPTIONS accessOptions,
            global::Windows.Storage.HANDLE_SHARING_OPTIONS sharingOptions,
            global::Windows.Storage.HANDLE_OPTIONS options,
            IntPtr oplockBreakingHandler)
        {
            SafeFileHandle interopHandle = default;
            IntPtr _interopHandle = default;
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::Windows.Storage.IStorageItemHandleAccess).TypeHandle));
            var ThisPtr = _obj.ThisPtr;
            try
            {
                ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.Create_0(ThisPtr, accessOptions, sharingOptions, options, oplockBreakingHandler, out _interopHandle));
            }
            finally
            {
                interopHandle = new SafeFileHandle(_interopHandle, true);
            }
            return interopHandle;
        }
    }
#endif
}
