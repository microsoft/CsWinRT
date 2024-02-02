﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// This file was generated by cswinrt.exe

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WinRT.Interop
{
#if NET
    [WinRTExposedType(typeof(ManagedExceptionErrorInfoTypeDetails))]
#endif
    internal sealed class ManagedExceptionErrorInfo
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

#if NET
    internal sealed class ManagedExceptionErrorInfoTypeDetails : IWinRTExposedTypeDetails
    {
        public ComWrappers.ComInterfaceEntry[] GetExposedInterfaces()
        {
            return new ComWrappers.ComInterfaceEntry[]
            {
                new ComWrappers.ComInterfaceEntry
                {
                    IID = IID.IID_IErrorInfo,
                    Vtable = ABI.WinRT.Interop.IErrorInfoVftbl.AbiToProjectionVftablePtr
                },
                new ComWrappers.ComInterfaceEntry
                {
                    IID = IID.IID_ISupportErrorInfo,
                    Vtable = ABI.WinRT.Interop.ISupportErrorInfoVftbl.AbiToProjectionVftablePtr
                }
            };
        }
    }
#endif
}

#pragma warning disable CS0649

namespace ABI.WinRT.Interop
{
    using global::WinRT;
    using global::WinRT.Interop;

#if NET
    internal unsafe struct IErrorInfoVftbl
    {
        public IUnknownVftbl IUnknownVftbl;
        public delegate* unmanaged[Stdcall]<IntPtr, Guid*, int> GetGuid_0;
        public delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int> GetSource_1;
        public delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int> GetDescription_2;
        public delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int> GetHelpFile_3;
        public delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int> GetHelpFileContent_4;

        public static readonly IErrorInfoVftbl AbiToProjectionVftable;
        public static readonly IntPtr AbiToProjectionVftablePtr;

        static IErrorInfoVftbl()
        {
            AbiToProjectionVftable = new IErrorInfoVftbl
            {
                IUnknownVftbl = IUnknownVftbl.AbiToProjectionVftbl,
                GetGuid_0 = &Do_Abi_GetGuid_0,
                GetSource_1 = &Do_Abi_GetSource_1,
                GetDescription_2 = &Do_Abi_GetDescription_2,
                GetHelpFile_3 = &Do_Abi_GetHelpFile_3,
                GetHelpFileContent_4 = &Do_Abi_GetHelpFileContent_4
            };

            var nativeVftbl = (IntPtr*)Marshal.AllocCoTaskMem(sizeof(IErrorInfoVftbl));

            *(IErrorInfoVftbl*)nativeVftbl = AbiToProjectionVftable;

            AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
        private static int Do_Abi_GetGuid_0(IntPtr thisPtr, Guid* guid)
        {
            try
            {
                *guid = ComWrappersSupport.FindObject<ManagedExceptionErrorInfo>(thisPtr).GetGuid();
            }
            catch (Exception ex)
            {
                ExceptionHelpers.SetErrorInfo(ex);
                return ExceptionHelpers.GetHRForException(ex);
            }
            return 0;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
        private static int Do_Abi_GetSource_1(IntPtr thisPtr, IntPtr* source)
        {
            *source = IntPtr.Zero;
            string _source;
            try
            {
                _source = ComWrappersSupport.FindObject<ManagedExceptionErrorInfo>(thisPtr).GetSource();
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

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
        private static int Do_Abi_GetDescription_2(IntPtr thisPtr, IntPtr* description)
        {
            *description = IntPtr.Zero;
            string _description;
            try
            {
                _description = ComWrappersSupport.FindObject<ManagedExceptionErrorInfo>(thisPtr).GetDescription();
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

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
        private static int Do_Abi_GetHelpFile_3(IntPtr thisPtr, IntPtr* helpFile)
        {
            *helpFile = IntPtr.Zero;
            string _helpFile;
            try
            {
                _helpFile = ComWrappersSupport.FindObject<ManagedExceptionErrorInfo>(thisPtr).GetHelpFile();
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

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
        private static int Do_Abi_GetHelpFileContent_4(IntPtr thisPtr, IntPtr* helpFileContent)
        {
            *helpFileContent = IntPtr.Zero;
            string _helpFileContent;
            try
            {
                _helpFileContent = ComWrappersSupport.FindObject<ManagedExceptionErrorInfo>(thisPtr).GetHelpFileContent();
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

    internal unsafe struct ISupportErrorInfoVftbl
    {
        public IUnknownVftbl IUnknownVftbl;
        public delegate* unmanaged[Stdcall]<IntPtr, Guid*, int> InterfaceSupportsErrorInfo_0;

        public static readonly ISupportErrorInfoVftbl AbiToProjectionVftable;
        public static readonly IntPtr AbiToProjectionVftablePtr;

        static ISupportErrorInfoVftbl()
        {
            AbiToProjectionVftable = new ISupportErrorInfoVftbl
            {
                IUnknownVftbl = IUnknownVftbl.AbiToProjectionVftbl,
                InterfaceSupportsErrorInfo_0 = &Do_Abi_InterfaceSupportsErrorInfo_0
            };

            var nativeVftbl = (IntPtr*)Marshal.AllocCoTaskMem(sizeof(ISupportErrorInfoVftbl));

            *(ISupportErrorInfoVftbl*)nativeVftbl = AbiToProjectionVftable;

            AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
        private static int Do_Abi_InterfaceSupportsErrorInfo_0(IntPtr thisPtr, Guid* guid)
        {
            try
            {
                return global::WinRT.ComWrappersSupport.FindObject<ManagedExceptionErrorInfo>(thisPtr).InterfaceSupportsErrorInfo(*guid) ? 0 : 1;
            }
            catch (Exception ex)
            {
                ExceptionHelpers.SetErrorInfo(ex);
                return ExceptionHelpers.GetHRForException(ex);
            }
        }
    }
#endif

    [Guid("04A2DBF3-DF83-116C-0946-0812ABF6E07D")]
    internal unsafe class ILanguageExceptionErrorInfo
    {
        protected readonly ObjectReference<IUnknownVftbl> _obj;

        public ILanguageExceptionErrorInfo(ObjectReference<IUnknownVftbl> obj)
        {
            _obj = obj;
        }

        public IObjectReference GetLanguageException()
        {
            IntPtr __return_value__ = IntPtr.Zero;

            try
            {
                IntPtr thisPtr = _obj.ThisPtr;

                Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)(*(void***)thisPtr)[3])(thisPtr, &__return_value__));

                GC.KeepAlive(_obj);

                return ObjectReference<IUnknownVftbl>.Attach(ref __return_value__);
            }
            finally
            {
                if (__return_value__ != IntPtr.Zero)
                {
                    (*(IUnknownVftbl**)__return_value__)->Release(__return_value__);
                }
            }
        }
    }

    [Guid("82BA7092-4C88-427D-A7BC-16DD93FEB67E")]
    internal unsafe class IRestrictedErrorInfo
    {
        [Guid("82BA7092-4C88-427D-A7BC-16DD93FEB67E")]
        public struct Vftbl
        {
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
