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
    // IActivationFactory
    [Guid("00000035-0000-0000-C000-000000000046")]
    public struct IActivationFactoryVftbl
    {
        public unsafe delegate int _ActivateInstance(IntPtr pThis, out IntPtr instance);

        public IInspectable.Vftbl IInspectableVftbl;
        public _ActivateInstance ActivateInstance;
    }

    // standard accessors/mutators
    public delegate int _get_PropertyAsBoolean(IntPtr thisPtr, out byte value);
    public delegate int _put_PropertyAsBoolean(IntPtr thisPtr, byte value);
    public delegate int _get_PropertyAsChar(IntPtr thisPtr, out ushort value);
    public delegate int _put_PropertyAsChar(IntPtr thisPtr, ushort value);
    public delegate int _get_PropertyAsSByte(IntPtr thisPtr, out sbyte value);
    public delegate int _put_PropertyAsSByte(IntPtr thisPtr, sbyte value);
    public delegate int _get_PropertyAsByte(IntPtr thisPtr, out byte value);
    public delegate int _put_PropertyAsByte(IntPtr thisPtr, byte value);
    public delegate int _get_PropertyAsInt16(IntPtr thisPtr, out short value);
    public delegate int _put_PropertyAsInt16(IntPtr thisPtr, short value);
    public delegate int _get_PropertyAsUInt16(IntPtr thisPtr, out ushort value);
    public delegate int _put_PropertyAsUInt16(IntPtr thisPtr, ushort value);
    public delegate int _get_PropertyAsInt32(IntPtr thisPtr, out int value);
    public delegate int _put_PropertyAsInt32(IntPtr thisPtr, int value);
    public delegate int _get_PropertyAsUInt32(IntPtr thisPtr, out uint value);
    public delegate int _put_PropertyAsUInt32(IntPtr thisPtr, uint value);
    public delegate int _get_PropertyAsInt64(IntPtr thisPtr, out long value);
    public delegate int _put_PropertyAsInt64(IntPtr thisPtr, long value);
    public delegate int _get_PropertyAsUInt64(IntPtr thisPtr, out ulong value);
    public delegate int _put_PropertyAsUInt64(IntPtr thisPtr, ulong value);
    public delegate int _get_PropertyAsFloat(IntPtr thisPtr, out float value);
    public delegate int _put_PropertyAsFloat(IntPtr thisPtr, float value);
    public delegate int _get_PropertyAsDouble(IntPtr thisPtr, out double value);
    public delegate int _put_PropertyAsDouble(IntPtr thisPtr, double value);
    public delegate int _get_PropertyAsObject(IntPtr thisPtr, out IntPtr value);
    public delegate int _put_PropertyAsObject(IntPtr thisPtr, IntPtr value);
    public delegate int _get_PropertyAsGuid(IntPtr thisPtr, out Guid value);
    public delegate int _put_PropertyAsGuid(IntPtr thisPtr, Guid value);
    public delegate int _get_PropertyAsString(IntPtr thisPtr, out IntPtr value);
    public delegate int _put_PropertyAsString(IntPtr thisPtr, IntPtr value);
    public delegate int _get_PropertyAsVector3(IntPtr thisPtr, out Windows.Foundation.Numerics.Vector3 value);
    public delegate int _put_PropertyAsVector3(IntPtr thisPtr, Windows.Foundation.Numerics.Vector3 value);
    public delegate int _get_PropertyAsQuaternion(IntPtr thisPtr, out Windows.Foundation.Numerics.Quaternion value);
    public delegate int _put_PropertyAsQuaternion(IntPtr thisPtr, Windows.Foundation.Numerics.Quaternion value);
    public delegate int _get_PropertyAsMatrix4x4(IntPtr thisPtr, out Windows.Foundation.Numerics.Matrix4x4 value);
    public delegate int _put_PropertyAsMatrix4x4(IntPtr thisPtr, Windows.Foundation.Numerics.Matrix4x4 value);
    public delegate int _add_EventHandler(IntPtr thisPtr, IntPtr handler, out Windows.Foundation.EventRegistrationToken token);
    public delegate int _remove_EventHandler(IntPtr thisPtr, Windows.Foundation.EventRegistrationToken token);

    // IDelegate
    public struct IDelegateVftbl
    {
        public IUnknownVftbl IUnknownVftbl;
        public IntPtr Invoke;
    }

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

    [Guid("1CF2B120-547D-101B-8E65-08002B2BD119")]
    internal class IErrorInfo : global::WinRT.Interop.IErrorInfo
    {
        [Guid("1CF2B120-547D-101B-8E65-08002B2BD119")]
        public struct Vftbl
        {
            internal global::WinRT.Interop.IUnknownVftbl IUnknownVftbl;
            public global::WinRT.Interop._get_PropertyAsGuid GetGuid_0;
            public global::WinRT.Interop._get_PropertyAsString GetSource_1;
            public global::WinRT.Interop._get_PropertyAsString GetDescription_2;
            public global::WinRT.Interop._get_PropertyAsString GetHelpFile_3;
            public global::WinRT.Interop._get_PropertyAsString GetHelpFileContent_4;

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;

            static unsafe Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IUnknownVftbl = global::WinRT.Interop.IUnknownVftbl.AbiToProjectionVftbl,
                    GetGuid_0 = Do_Abi_GetGuid_0,
                    GetSource_1 = Do_Abi_GetSource_1,
                    GetDescription_2 = Do_Abi_GetDescription_2,
                    GetHelpFile_3 = Do_Abi_GetHelpFile_3,
                    GetHelpFileContent_4 = Do_Abi_GetHelpFileContent_4
                };
                var nativeVftbl = (IntPtr*)Marshal.AllocCoTaskMem(Marshal.SizeOf<Vftbl>());
                Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

            private static int Do_Abi_GetGuid_0(IntPtr thisPtr, out Guid guid)
            {
                guid = default;
                try
                {
                    guid = ComWrappersSupport.FindObject<global::WinRT.Interop.IErrorInfo>(thisPtr).GetGuid();
                }
                catch (Exception ex)
                {
                    ExceptionHelpers.SetErrorInfo(ex);
                    return ExceptionHelpers.GetHRForException(ex);
                }
                return 0;
            }

            private static int Do_Abi_GetSource_1(IntPtr thisPtr, out IntPtr source)
            {
                source = IntPtr.Zero;
                string _source;
                try
                {
                    _source = ComWrappersSupport.FindObject<global::WinRT.Interop.IErrorInfo>(thisPtr).GetSource();
                    source = Marshal.StringToBSTR(_source);
                }
                catch (Exception ex)
                {
                    Marshal.FreeBSTR(source);
                    ExceptionHelpers.SetErrorInfo(ex);
                    return ExceptionHelpers.GetHRForException(ex);
                }
                return 0;
            }

            private static int Do_Abi_GetDescription_2(IntPtr thisPtr, out IntPtr description)
            {
                description = IntPtr.Zero;
                string _description;
                try
                {
                    _description = ComWrappersSupport.FindObject<global::WinRT.Interop.IErrorInfo>(thisPtr).GetDescription();
                    description = Marshal.StringToBSTR(_description);
                }
                catch (Exception ex)
                {
                    Marshal.FreeBSTR(description);
                    ExceptionHelpers.SetErrorInfo(ex);
                    return ExceptionHelpers.GetHRForException(ex);
                }
                return 0;
            }

            private static int Do_Abi_GetHelpFile_3(IntPtr thisPtr, out IntPtr helpFile)
            {
                helpFile = IntPtr.Zero;
                string _helpFile;
                try
                {
                    _helpFile = ComWrappersSupport.FindObject<global::WinRT.Interop.IErrorInfo>(thisPtr).GetHelpFile();
                    helpFile = Marshal.StringToBSTR(_helpFile);
                }
                catch (Exception ex)
                {
                    Marshal.FreeBSTR(helpFile);
                    ExceptionHelpers.SetErrorInfo(ex);
                    return ExceptionHelpers.GetHRForException(ex);
                }
                return 0;
            }

            private static int Do_Abi_GetHelpFileContent_4(IntPtr thisPtr, out IntPtr helpFileContent)
            {
                helpFileContent = IntPtr.Zero;
                string _helpFileContent;
                try
                {
                    _helpFileContent = ComWrappersSupport.FindObject<global::WinRT.Interop.IErrorInfo>(thisPtr).GetHelpFileContent();
                    helpFileContent = Marshal.StringToBSTR(_helpFileContent);
                }
                catch (Exception ex)
                {
                    Marshal.FreeBSTR(helpFileContent);
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
            Marshal.ThrowExceptionForHR(_obj.Vftbl.GetGuid_0(ThisPtr, out __return_value__));
            return __return_value__;
        }

        public string GetSource()
        {
            IntPtr __retval = default;
            try
            {
                Marshal.ThrowExceptionForHR(_obj.Vftbl.GetSource_1(ThisPtr, out __retval));
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
                Marshal.ThrowExceptionForHR(_obj.Vftbl.GetDescription_2(ThisPtr, out __retval));
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
                Marshal.ThrowExceptionForHR(_obj.Vftbl.GetHelpFile_3(ThisPtr, out __retval));
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
                Marshal.ThrowExceptionForHR(_obj.Vftbl.GetHelpFileContent_4(ThisPtr, out __retval));
                return __retval != IntPtr.Zero ? Marshal.PtrToStringBSTR(__retval) : string.Empty;
            }
            finally
            {
                Marshal.FreeBSTR(__retval);
            }
        }
    }

    [Guid("04a2dbf3-df83-116c-0946-0812abf6e07d")]
    internal class ILanguageExceptionErrorInfo : global::WinRT.Interop.ILanguageExceptionErrorInfo
    {
        [Guid("04a2dbf3-df83-116c-0946-0812abf6e07d")]
        public struct Vftbl
        {
            internal global::WinRT.Interop.IUnknownVftbl IUnknownVftbl;
            public global::WinRT.Interop._get_PropertyAsObject GetLanguageException_0;
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
                Marshal.ThrowExceptionForHR(_obj.Vftbl.GetLanguageException_0(ThisPtr, out __return_value__));
                return ObjectReference<global::WinRT.Interop.IUnknownVftbl>.Attach(ref __return_value__);
            }
            finally
            {
                MarshalInterfaceHelper<object>.DisposeAbi(__return_value__);
            }
        }
    }

    [Guid("DF0B3D60-548F-101B-8E65-08002B2BD119")]
    internal class ISupportErrorInfo : global::WinRT.Interop.ISupportErrorInfo
    {
        [Guid("DF0B3D60-548F-101B-8E65-08002B2BD119")]
        public struct Vftbl
        {
            public delegate int _InterfaceSupportsErrorInfo(IntPtr thisPtr, ref Guid riid);
            internal global::WinRT.Interop.IUnknownVftbl IUnknownVftbl;
            public _InterfaceSupportsErrorInfo InterfaceSupportsErrorInfo_0;

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;

            static unsafe Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IUnknownVftbl = global::WinRT.Interop.IUnknownVftbl.AbiToProjectionVftbl,
                    InterfaceSupportsErrorInfo_0 = Do_Abi_InterfaceSupportsErrorInfo_0
                };
                var nativeVftbl = (IntPtr*)Marshal.AllocCoTaskMem(Marshal.SizeOf<Vftbl>());
                Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

            private static int Do_Abi_InterfaceSupportsErrorInfo_0(IntPtr thisPtr, ref Guid guid)
            {
                try
                {
                    return global::WinRT.ComWrappersSupport.FindObject<global::WinRT.Interop.ISupportErrorInfo>(thisPtr).InterfaceSupportsErrorInfo(guid) ? 0 : 1;
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
            return _obj.Vftbl.InterfaceSupportsErrorInfo_0(ThisPtr, ref riid) == 0;
        }
    }

    [Guid("82BA7092-4C88-427D-A7BC-16DD93FEB67E")]
    internal class IRestrictedErrorInfo : global::WinRT.Interop.IRestrictedErrorInfo
    {
        [Guid("82BA7092-4C88-427D-A7BC-16DD93FEB67E")]
        public struct Vftbl
        {
            public delegate int _GetErrorDetails(IntPtr thisPtr, out IntPtr description, out int error, out IntPtr restrictedDescription, out IntPtr capabilitySid);

            internal global::WinRT.Interop.IUnknownVftbl unknownVftbl;
            public _GetErrorDetails GetErrorDetails_0;
            public global::WinRT.Interop._get_PropertyAsString GetReference_1;
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
                Marshal.ThrowExceptionForHR(_obj.Vftbl.GetErrorDetails_0(ThisPtr, out _description, out error, out _restrictedDescription, out _capabilitySid));
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
                Marshal.ThrowExceptionForHR(_obj.Vftbl.GetReference_1(ThisPtr, out __retval));
                return __retval != IntPtr.Zero ? Marshal.PtrToStringBSTR(__retval) : string.Empty;
            }
            finally
            {
                Marshal.FreeBSTR(__retval);
            }
        }
    }
}