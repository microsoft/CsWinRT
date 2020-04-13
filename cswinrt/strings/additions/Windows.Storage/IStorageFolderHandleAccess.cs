﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace Windows.Storage
{
    using Microsoft.Win32.SafeHandles;
    // Available in 14393 (RS1) and later
    [Guid("DF19938F-5462-48A0-BE65-D2A3271A08D6")]
    internal interface IStorageFolderHandleAccess
    {
        SafeFileHandle Create(
            [MarshalAs(UnmanagedType.LPWStr)] string fileName,
            HANDLE_CREATION_OPTIONS creationOptions,
            HANDLE_ACCESS_OPTIONS accessOptions,
            HANDLE_SHARING_OPTIONS sharingOptions,
            HANDLE_OPTIONS options,
            IntPtr oplockBreakingHandler);
    }
}

namespace ABI.Windows.Storage
{
    using global::Microsoft.Win32.SafeHandles;
    using global::System;
    [Guid("DF19938F-5462-48A0-BE65-D2A3271A08D6")]
    internal class IStorageFolderHandleAccess : global::Windows.Storage.IStorageFolderHandleAccess
    {
        [Guid("DF19938F-5462-48A0-BE65-D2A3271A08D6")]
        public struct Vftbl
        {
            public delegate int _Create_0(
                IntPtr thisPtr,
                IntPtr fileName,
                global::Windows.Storage.HANDLE_CREATION_OPTIONS creationOptions,
                global::Windows.Storage.HANDLE_ACCESS_OPTIONS accessOptions,
                global::Windows.Storage.HANDLE_SHARING_OPTIONS sharingOptions,
                global::Windows.Storage.HANDLE_OPTIONS options,
                IntPtr oplockBreakingHandler,
                out IntPtr interopHandle);
            public IUnknownVftbl IUnknownVftbl;
            public _Create_0 Create_0;
        }

        internal static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr);

        public static implicit operator IStorageFolderHandleAccess(IObjectReference obj) => (obj != null) ? new IStorageFolderHandleAccess(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IObjectReference ObjRef { get => _obj; }
        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public IStorageFolderHandleAccess(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        internal IStorageFolderHandleAccess(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
        }

        public unsafe SafeFileHandle Create(
            string fileName,
            global::Windows.Storage.HANDLE_CREATION_OPTIONS creationOptions,
            global::Windows.Storage.HANDLE_ACCESS_OPTIONS accessOptions,
            global::Windows.Storage.HANDLE_SHARING_OPTIONS sharingOptions,
            global::Windows.Storage.HANDLE_OPTIONS options,
            IntPtr oplockBreakingHandler)
        {
            SafeFileHandle interopHandle = default;
            IntPtr _interopHandle = default;
            try
            {
                fixed (char* fileNamePtr = fileName)
                {
                    ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.Create_0(ThisPtr, (IntPtr)fileNamePtr, creationOptions, accessOptions, sharingOptions, options, oplockBreakingHandler, out _interopHandle));
                }
            }
            finally
            {
                interopHandle = new SafeFileHandle(_interopHandle, true);
            }
            return interopHandle;
        }
    }
}
