﻿// This file was generated by cswinrt.exe
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Linq.Expressions;

namespace WinRT.Interop
{
    [Guid("1CF2B120-547D-101B-8E65-08002B2BD119")]
    internal interface IErrorInfo
    {
        Guid GetGuid();
        string GetSource();
        string GetDescription();
        string GetHelpFile();
        string GetHelpFileContent();
    }

    [Guid("DF0B3D60-548F-101B-8E65-08002B2BD119")]
    internal interface ISupportErrorInfo
    {
        bool InterfaceSupportsErrorInfo(Guid riid);
    }

    [Guid("04a2dbf3-df83-116c-0946-0812abf6e07d")]
    internal interface ILanguageExceptionErrorInfo
    {
        IObjectReference GetLanguageException();
    }

    [Guid("82BA7092-4C88-427D-A7BC-16DD93FEB67E")]
    internal interface IRestrictedErrorInfo
    {
        void GetErrorDetails(
            out string description,
            out int error,
            out string restrictedDescription,
            out string capabilitySid);

        string GetReference();
    }

    internal class ManagedExceptionErrorInfo : IErrorInfo, ISupportErrorInfo
    {
        private readonly Exception _exception;

        public ManagedExceptionErrorInfo(Exception ex)
        {
            _exception = ex;
        }

        public bool InterfaceSupportsErrorInfo(Guid riid) => true;

        public Guid GetGuid() => default;

        public string GetSource() => _exception.Source;

        public string GetDescription()
        {
            string desc = _exception.Message;
            if (string.IsNullOrEmpty(desc))
            {
                desc = _exception.GetType().FullName;
            }
            return desc;
        }

        public string GetHelpFile() => _exception.HelpLink;

        public string GetHelpFileContent() => string.Empty;
    }
}

#pragma warning disable CS0649

namespace ABI.WinRT.Interop
{
    using global::WinRT;
    using WinRT.Interop;

    [Guid("1CF2B120-547D-101B-8E65-08002B2BD119")]
    internal unsafe class IErrorInfo : global::WinRT.Interop.IErrorInfo
    {
        [Guid("1CF2B120-547D-101B-8E65-08002B2BD119")]
        public struct Vftbl
        {
            internal global::WinRT.Interop.IUnknownVftbl IUnknownVftbl;
            private void* _GetGuid_0;
            public delegate* unmanaged[Stdcall]<IntPtr, Guid*, int> GetGuid_0 { get => (delegate* unmanaged[Stdcall]<IntPtr, Guid*, int>)_GetGuid_0; set => _GetGuid_0 = value; }
            private void* _GetSource_1;
            public delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int> GetSource_1 { get => (delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)_GetSource_1; set => _GetSource_1 = value; }
            private void* _GetDescription_2;
            public delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int> GetDescription_2 { get => (delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)_GetDescription_2; set => _GetDescription_2 = value; }
            private void* _GetHelpFile_3;
            public delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int> GetHelpFile_3 { get => (delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)_GetHelpFile_3; set => _GetHelpFile_3 = value; }
            private void* _GetHelpFileContent_4;
            public delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int> GetHelpFileContent_4 { get => (delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)_GetHelpFileContent_4; set => _GetHelpFileContent_4 = value; }

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;

#if NETSTANDARD2_0
            public delegate int GetGuidDelegate(IntPtr thisPtr, Guid* guid);
            public delegate int GetBstrDelegate(IntPtr thisPtr, IntPtr* bstr);
            private static readonly Delegate[] DelegateCache = new Delegate[5];
#endif

            static unsafe Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IUnknownVftbl = global::WinRT.Interop.IUnknownVftbl.AbiToProjectionVftbl,
#if NETSTANDARD2_0
                    _GetGuid_0 = Marshal.GetFunctionPointerForDelegate(DelegateCache[0] = new GetGuidDelegate(Do_Abi_GetGuid_0)).ToPointer(),
                    _GetSource_1 = Marshal.GetFunctionPointerForDelegate(DelegateCache[1] = new GetBstrDelegate(Do_Abi_GetSource_1)).ToPointer(),
                    _GetDescription_2 = Marshal.GetFunctionPointerForDelegate(DelegateCache[2] = new GetBstrDelegate(Do_Abi_GetDescription_2)).ToPointer(),
                    _GetHelpFile_3 = Marshal.GetFunctionPointerForDelegate(DelegateCache[3] = new GetBstrDelegate(Do_Abi_GetHelpFile_3)).ToPointer(),
                    _GetHelpFileContent_4 = Marshal.GetFunctionPointerForDelegate(DelegateCache[4] = new GetBstrDelegate(Do_Abi_GetHelpFileContent_4)).ToPointer(),
#else
                    _GetGuid_0 = (delegate* unmanaged<IntPtr, Guid*, int>)&Do_Abi_GetGuid_0,
                    _GetSource_1 = (delegate* unmanaged<IntPtr, IntPtr*, int>)&Do_Abi_GetSource_1,
                    _GetDescription_2 = (delegate* unmanaged<IntPtr, IntPtr*, int>)&Do_Abi_GetDescription_2,
                    _GetHelpFile_3 = (delegate* unmanaged<IntPtr, IntPtr*, int>)&Do_Abi_GetHelpFile_3,
                    _GetHelpFileContent_4 = (delegate* unmanaged<IntPtr, IntPtr*, int>)&Do_Abi_GetHelpFileContent_4
#endif
                };
                var nativeVftbl = (IntPtr*)Marshal.AllocCoTaskMem(Marshal.SizeOf<Vftbl>());
                Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

#if !NETSTANDARD2_0
            [UnmanagedCallersOnly]
#endif
            private static int Do_Abi_GetGuid_0(IntPtr thisPtr, Guid* guid)
            {
                try
                {
                    *guid = ComWrappersSupport.FindObject<global::WinRT.Interop.IErrorInfo>(thisPtr).GetGuid();
                }
                catch (Exception ex)
                {
                    ExceptionHelpers.SetErrorInfo(ex);
                    return ExceptionHelpers.GetHRForException(ex);
                }
                return 0;
            }

#if !NETSTANDARD2_0
            [UnmanagedCallersOnly]
#endif
            private static int Do_Abi_GetSource_1(IntPtr thisPtr, IntPtr* source)
            {
                *source = IntPtr.Zero;
                string _source;
                try
                {
                    _source = ComWrappersSupport.FindObject<global::WinRT.Interop.IErrorInfo>(thisPtr).GetSource();
                    *source = Marshal.StringToBSTR(_source);
                }
                catch (Exception ex)
                {
                    Marshal.FreeBSTR(*source);
                    ExceptionHelpers.SetErrorInfo(ex);
                    return ExceptionHelpers.GetHRForException(ex);
                }
                return 0;
            }

#if !NETSTANDARD2_0
            [UnmanagedCallersOnly]
#endif
            private static int Do_Abi_GetDescription_2(IntPtr thisPtr, IntPtr* description)
            {
                *description = IntPtr.Zero;
                string _description;
                try
                {
                    _description = ComWrappersSupport.FindObject<global::WinRT.Interop.IErrorInfo>(thisPtr).GetDescription();
                    *description = Marshal.StringToBSTR(_description);
                }
                catch (Exception ex)
                {
                    Marshal.FreeBSTR(*description);
                    ExceptionHelpers.SetErrorInfo(ex);
                    return ExceptionHelpers.GetHRForException(ex);
                }
                return 0;
            }

#if !NETSTANDARD2_0
            [UnmanagedCallersOnly]
#endif
            private static int Do_Abi_GetHelpFile_3(IntPtr thisPtr, IntPtr* helpFile)
            {
                *helpFile = IntPtr.Zero;
                string _helpFile;
                try
                {
                    _helpFile = ComWrappersSupport.FindObject<global::WinRT.Interop.IErrorInfo>(thisPtr).GetHelpFile();
                    *helpFile = Marshal.StringToBSTR(_helpFile);
                }
                catch (Exception ex)
                {
                    Marshal.FreeBSTR(*helpFile);
                    ExceptionHelpers.SetErrorInfo(ex);
                    return ExceptionHelpers.GetHRForException(ex);
                }
                return 0;
            }

#if !NETSTANDARD2_0
            [UnmanagedCallersOnly]
#endif
            private static int Do_Abi_GetHelpFileContent_4(IntPtr thisPtr, IntPtr* helpFileContent)
            {
                *helpFileContent = IntPtr.Zero;
                string _helpFileContent;
                try
                {
                    _helpFileContent = ComWrappersSupport.FindObject<global::WinRT.Interop.IErrorInfo>(thisPtr).GetHelpFileContent();
                    *helpFileContent = Marshal.StringToBSTR(_helpFileContent);
                }
                catch (Exception ex)
                {
                    Marshal.FreeBSTR(*helpFileContent);
                    ExceptionHelpers.SetErrorInfo(ex);
                    return ExceptionHelpers.GetHRForException(ex);
                }
                return 0;
            }
        }

        public static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr);

        public static implicit operator IErrorInfo(IObjectReference obj) => (obj != null) ? new IErrorInfo(obj) : null;
        public static implicit operator IErrorInfo(ObjectReference<Vftbl> obj) => (obj != null) ? new IErrorInfo(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public IErrorInfo(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        public IErrorInfo(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
        }

        public Guid GetGuid()
        {
            Guid __return_value__;
            Marshal.ThrowExceptionForHR(_obj.Vftbl.GetGuid_0(ThisPtr, &__return_value__));
            return __return_value__;
        }

        public string GetSource()
        {
            IntPtr __retval = default;
            try
            {
                Marshal.ThrowExceptionForHR(_obj.Vftbl.GetSource_1(ThisPtr, &__retval));
                return __retval != IntPtr.Zero ? Marshal.PtrToStringBSTR(__retval) : string.Empty;
            }
            finally
            {
                Marshal.FreeBSTR(__retval);
            }
        }

        public string GetDescription()
        {
            IntPtr __retval = default;
            try
            {
                Marshal.ThrowExceptionForHR(_obj.Vftbl.GetDescription_2(ThisPtr, &__retval));
                return __retval != IntPtr.Zero ? Marshal.PtrToStringBSTR(__retval) : string.Empty;
            }
            finally
            {
                Marshal.FreeBSTR(__retval);
            }
        }

        public string GetHelpFile()
        {
            IntPtr __retval = default;
            try
            {
                Marshal.ThrowExceptionForHR(_obj.Vftbl.GetHelpFile_3(ThisPtr, &__retval));
                return __retval != IntPtr.Zero ? Marshal.PtrToStringBSTR(__retval) : string.Empty;
            }
            finally
            {
                Marshal.FreeBSTR(__retval);
            }
        }

        public string GetHelpFileContent()
        {
            IntPtr __retval = default;
            try
            {
                Marshal.ThrowExceptionForHR(_obj.Vftbl.GetHelpFileContent_4(ThisPtr, &__retval));
                return __retval != IntPtr.Zero ? Marshal.PtrToStringBSTR(__retval) : string.Empty;
            }
            finally
            {
                Marshal.FreeBSTR(__retval);
            }
        }
    }

    [Guid("04a2dbf3-df83-116c-0946-0812abf6e07d")]
    internal unsafe class ILanguageExceptionErrorInfo : global::WinRT.Interop.ILanguageExceptionErrorInfo
    {
        [Guid("04a2dbf3-df83-116c-0946-0812abf6e07d")]
        public struct Vftbl
        {
            internal global::WinRT.Interop.IUnknownVftbl IUnknownVftbl;
            private void* _GetLanguageException_0;
            public delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int> GetLanguageException_0 => (delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)_GetLanguageException_0;
        }

        public static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr);

        public static implicit operator ILanguageExceptionErrorInfo(IObjectReference obj) => (obj != null) ? new ILanguageExceptionErrorInfo(obj) : null;
        public static implicit operator ILanguageExceptionErrorInfo(ObjectReference<Vftbl> obj) => (obj != null) ? new ILanguageExceptionErrorInfo(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public ILanguageExceptionErrorInfo(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        public ILanguageExceptionErrorInfo(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
        }

        public IObjectReference GetLanguageException()
        {
            IntPtr __return_value__ = IntPtr.Zero;

            try
            {
                Marshal.ThrowExceptionForHR(_obj.Vftbl.GetLanguageException_0(ThisPtr, &__return_value__));
                return ObjectReference<global::WinRT.Interop.IUnknownVftbl>.Attach(ref __return_value__);
            }
            finally
            {
                if (__return_value__ != IntPtr.Zero)
                {
                    (*(global::WinRT.Interop.IUnknownVftbl**)__return_value__)->Release(__return_value__);
                }
            }
        }
    }

    [Guid("DF0B3D60-548F-101B-8E65-08002B2BD119")]
    internal unsafe class ISupportErrorInfo : global::WinRT.Interop.ISupportErrorInfo
    {
        [Guid("DF0B3D60-548F-101B-8E65-08002B2BD119")]
        public struct Vftbl
        {
            internal global::WinRT.Interop.IUnknownVftbl IUnknownVftbl;
            private void* _InterfaceSupportsErrorInfo_0;
            public delegate* unmanaged[Stdcall]<IntPtr, Guid*, int> InterfaceSupportsErrorInfo_0 { get => (delegate* unmanaged[Stdcall]<IntPtr, Guid*, int>)_InterfaceSupportsErrorInfo_0; set => _InterfaceSupportsErrorInfo_0 = value; }

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;
#if NETSTANDARD2_0
            public delegate int _InterfaceSupportsErrorInfo(IntPtr thisPtr, Guid* riid);
            private static readonly _InterfaceSupportsErrorInfo DelegateCache;
#endif
            static unsafe Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IUnknownVftbl = global::WinRT.Interop.IUnknownVftbl.AbiToProjectionVftbl,
#if NETSTANDARD2_0
                    _InterfaceSupportsErrorInfo_0 = Marshal.GetFunctionPointerForDelegate(DelegateCache = Do_Abi_InterfaceSupportsErrorInfo_0).ToPointer()
#else
                    _InterfaceSupportsErrorInfo_0 = (delegate* unmanaged<IntPtr, Guid*, int>)&Do_Abi_InterfaceSupportsErrorInfo_0
#endif
                };
                var nativeVftbl = (IntPtr*)Marshal.AllocCoTaskMem(Marshal.SizeOf<Vftbl>());
                Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }
#if !NETSTANDARD2_0
            [UnmanagedCallersOnly]
#endif
            private static int Do_Abi_InterfaceSupportsErrorInfo_0(IntPtr thisPtr, Guid* guid)
            {
                try
                {
                    return global::WinRT.ComWrappersSupport.FindObject<global::WinRT.Interop.ISupportErrorInfo>(thisPtr).InterfaceSupportsErrorInfo(*guid) ? 0 : 1;
                }
                catch (Exception ex)
                {
                    ExceptionHelpers.SetErrorInfo(ex);
                    return ExceptionHelpers.GetHRForException(ex);
                }
            }
        }

        public static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr);

        public static implicit operator ISupportErrorInfo(IObjectReference obj) => (obj != null) ? new ISupportErrorInfo(obj) : null;
        public static implicit operator ISupportErrorInfo(ObjectReference<Vftbl> obj) => (obj != null) ? new ISupportErrorInfo(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public ISupportErrorInfo(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        public ISupportErrorInfo(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
        }

        public bool InterfaceSupportsErrorInfo(Guid riid)
        {
            return _obj.Vftbl.InterfaceSupportsErrorInfo_0(ThisPtr, &riid) == 0;
        }
    }

    [Guid("82BA7092-4C88-427D-A7BC-16DD93FEB67E")]
    internal unsafe class IRestrictedErrorInfo : global::WinRT.Interop.IRestrictedErrorInfo
    {
        [Guid("82BA7092-4C88-427D-A7BC-16DD93FEB67E")]
        public struct Vftbl
        {
            public delegate int _GetErrorDetails(IntPtr thisPtr, out IntPtr description, out int error, out IntPtr restrictedDescription, out IntPtr capabilitySid);
            public delegate int _GetReference(IntPtr thisPtr, out IntPtr reference);

            internal global::WinRT.Interop.IUnknownVftbl unknownVftbl;
            private void* _GetErrorDetails_0;
            public delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int*, IntPtr*, IntPtr*, int> GetErrorDetails_0 => (delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int*, IntPtr*, IntPtr*, int>)_GetErrorDetails_0;
            private void* _GetReference_1;
            public delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int> GetReference_1 => (delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)_GetReference_1;
        }

        public static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr);

        public static implicit operator IRestrictedErrorInfo(IObjectReference obj) => (obj != null) ? new IRestrictedErrorInfo(obj) : null;
        public static implicit operator IRestrictedErrorInfo(ObjectReference<Vftbl> obj) => (obj != null) ? new IRestrictedErrorInfo(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public IRestrictedErrorInfo(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        public IRestrictedErrorInfo(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
        }

        public void GetErrorDetails(
            out string description,
            out int error,
            out string restrictedDescription,
            out string capabilitySid)
        {
            IntPtr _description = IntPtr.Zero;
            IntPtr _restrictedDescription = IntPtr.Zero;
            IntPtr _capabilitySid = IntPtr.Zero;
            try
            {
                fixed (int* pError = &error)
                {
                    Marshal.ThrowExceptionForHR(_obj.Vftbl.GetErrorDetails_0(ThisPtr, &_description, pError, &_restrictedDescription, &_capabilitySid));
                }
                description = _description != IntPtr.Zero ? Marshal.PtrToStringBSTR(_description) : string.Empty;
                restrictedDescription = _restrictedDescription != IntPtr.Zero ? Marshal.PtrToStringBSTR(_restrictedDescription) : string.Empty;
                capabilitySid = _capabilitySid != IntPtr.Zero ? Marshal.PtrToStringBSTR(_capabilitySid) : string.Empty;
            }
            finally
            {
                Marshal.FreeBSTR(_description);
                Marshal.FreeBSTR(_restrictedDescription);
                Marshal.FreeBSTR(_capabilitySid);
            }
        }

        public string GetReference()
        {
            IntPtr __retval = default;
            try
            {
                Marshal.ThrowExceptionForHR(_obj.Vftbl.GetReference_1(ThisPtr, &__retval));
                return __retval != IntPtr.Zero ? Marshal.PtrToStringBSTR(__retval) : string.Empty;
            }
            finally
            {
                Marshal.FreeBSTR(__retval);
            }
        }
    }
}
