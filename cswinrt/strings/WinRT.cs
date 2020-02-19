using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Linq.Expressions;

#pragma warning disable 0169 // The field 'xxx' is never used
#pragma warning disable 0649 // Field 'xxx' is never assigned to, and will always have its default value

namespace WinRT
{
    using WinRT.Interop;

    public enum TrustLevel
    {
        BaseTrust = 0,
        PartialTrust = BaseTrust + 1,
        FullTrust = PartialTrust + 1
    };


    public static class TypeExtensions
    {
        public static Type FindHelperType(this Type type)
        {
            return Type.GetType($"ABI.{type.FullName}");
        }

        public static Type GetHelperType(this Type type)
        {
            return type.FindHelperType() ?? throw new InvalidOperationException("Target type is not a projected type.");
        }

        public static Type GetAbiType(this Type type)
        {
            return type.GetHelperType().GetMethod("GetAbi").ReturnType;
        }

        public static Type GetMarshalerType(this Type type)
        {
            return type.GetHelperType().GetMethod("CreateMarshaler").ReturnType;
        }
    }

    public static class DelegateExtensions
    {
        public static bool IsDelegate(this Type type)
        {
            return typeof(MulticastDelegate).IsAssignableFrom(type.BaseType);
        }

        public static void DynamicInvokeAbi(this System.Delegate del, object[] invoke_params)
        {
            Marshal.ThrowExceptionForHR((int)del.DynamicInvoke(invoke_params));
        }

        public static T AsDelegate<T>(this MulticastDelegate del)
        {
            return Marshal.GetDelegateForFunctionPointer<T>(
                Marshal.GetFunctionPointerForDelegate(del));
        }
    }

    public static class CastExtensions
    {
        public static TInterface As<TInterface>(this object value)
        {
            IntPtr GetThisPointer()
            {
                PropertyInfo thisPtrProperty = value.GetType().GetProperty("ThisPtr", BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
                if (thisPtrProperty is null)
                {
                    throw new ArgumentException("Source type is not a projected type.", nameof(TInterface));
                }
                return (IntPtr)thisPtrProperty.GetValue(value);
            }
            if (typeof(TInterface) == typeof(object))
            {
                // Use MarshalInspectable to get the default interface pointer.
                return (TInterface)MarshalInspectable.FromAbi(GetThisPointer());
            }

            if (value is TInterface convertableInMetadata)
            {
                return convertableInMetadata;
            }

            return (TInterface)typeof(MarshalInterface<>).MakeGenericType(typeof(TInterface)).GetMethod("FromAbi").Invoke(null, new[] { (object)GetThisPointer()  });
        }
    }

    public static class ExceptionHelpers
    {
        private const int COR_E_OBJECTDISPOSED = unchecked((int)0x80131622);
        private const int RO_E_CLOSED = unchecked((int)0x80000013);
        private const int E_ILLEGAL_STATE_CHANGE = unchecked((int)0x8000000d);
        private const int E_ILLEGAL_METHOD_CALL = unchecked((int)0x8000000e);
        private const int E_ILLEGAL_DELEGATE_ASSIGNMENT = unchecked((int)0x80000018);
        private const int APPMODEL_ERROR_NO_PACKAGE = unchecked((int)0x80073D54);
        internal const int E_XAMLPARSEFAILED = unchecked((int)0x802B000A);
        internal const int E_LAYOUTCYCLE = unchecked((int)0x802B0014);
        internal const int E_ELEMENTNOTENABLED = unchecked((int)0x802B001E);
        internal const int E_ELEMENTNOTAVAILABLE = unchecked((int)0x802B001F);

        internal delegate int GetRestrictedErrorInfo(out IntPtr ppRestrictedErrorInfo);
        private static GetRestrictedErrorInfo getRestrictedErrorInfo;

        internal delegate int SetRestrictedErrorInfo(IntPtr pRestrictedErrorInfo);
        private static SetRestrictedErrorInfo setRestrictedErrorInfo;

        internal delegate int RoOriginateLanguageException(int error, IntPtr message, IntPtr langaugeException);
        private static RoOriginateLanguageException roOriginateLanguageException;

        static ExceptionHelpers()
        {
            // TODO: Determine minimum expected platform support (aka do we care about platforms that don't support RoOriginateLanguageException?)
            IntPtr winRTErrorModule = Platform.LoadLibraryExW("api-ms-win-core-winrt-error-l1-1-1.dll", IntPtr.Zero, (uint)DllImportSearchPath.System32);
            if (winRTErrorModule != IntPtr.Zero)
            {
                getRestrictedErrorInfo = Platform.GetProcAddress<GetRestrictedErrorInfo>(winRTErrorModule);
                setRestrictedErrorInfo = Platform.GetProcAddress<SetRestrictedErrorInfo>(winRTErrorModule);
                roOriginateLanguageException = Platform.GetProcAddress<RoOriginateLanguageException>(winRTErrorModule);
            }
            else
            {
                winRTErrorModule = Platform.LoadLibraryExW("api-ms-win-core-winrt-error-l1-1-0.dll", IntPtr.Zero, (uint)DllImportSearchPath.System32);
                if (winRTErrorModule != IntPtr.Zero)
                {
                    getRestrictedErrorInfo = Platform.GetProcAddress<GetRestrictedErrorInfo>(winRTErrorModule);
                    setRestrictedErrorInfo = Platform.GetProcAddress<SetRestrictedErrorInfo>(winRTErrorModule);
                }
            }
        }

        public static void ThrowExceptionForHR(int hr)
        {
            Exception ex = GetExceptionForHR(hr, useGlobalErrorState : true, out bool restoredExceptionFromGlobalState);
            if (restoredExceptionFromGlobalState)
            {
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
            else
            {
                throw ex;
            }
        }

        public static Exception GetExceptionForHR(int hr) => GetExceptionForHR(hr, false, out _);

        private static Exception GetExceptionForHR(int hr, bool useGlobalErrorState, out bool restoredExceptionFromGlobalState)
        {
            restoredExceptionFromGlobalState = false;

            IObjectReference iErrorInfo = null;
            IObjectReference restrictedErrorInfoToSave = null;
            Exception ex;
            string description = null;
            string restrictedError = null;
            string restrictedErrorReference = null;
            string restrictedCapabilitySid = null;
            bool hasOtherLanguageException = false;

            if (useGlobalErrorState && getRestrictedErrorInfo != null)
            {
                Marshal.ThrowExceptionForHR(getRestrictedErrorInfo(out IntPtr restrictedErrorInfoPtr));

                IObjectReference restrictedErrorInfoRef = ObjectReference<ABI.WinRT.Interop.IRestrictedErrorInfo.Vftbl>.Attach(ref restrictedErrorInfoPtr);
                restrictedErrorInfoToSave = restrictedErrorInfoRef.As<ABI.WinRT.Interop.IRestrictedErrorInfo.Vftbl>();

                ABI.WinRT.Interop.IRestrictedErrorInfo restrictedErrorInfo = new ABI.WinRT.Interop.IRestrictedErrorInfo(restrictedErrorInfoRef);
                restrictedErrorInfo.GetErrorDetails(out description, out int hrLocal, out restrictedError, out restrictedCapabilitySid);
                restrictedErrorReference = restrictedErrorInfo.GetReference();
                try
                {
                    ILanguageExceptionErrorInfo languageErrorInfo = restrictedErrorInfo.As<ABI.WinRT.Interop.ILanguageExceptionErrorInfo>();
                    using (IObjectReference languageException = languageErrorInfo.GetLanguageException())
                    {
                        if (languageException is object)
                        {
                            if (languageException.IsReferenceToManagedObject)
                            {
                                ex = ComWrappersSupport.FindObject<Exception>(languageException.ThisPtr);
                                if (GetHRForException(ex) == hr)
                                {
                                    restoredExceptionFromGlobalState = true;
                                    return ex;
                                }
                            }
                            else
                            {
                                hasOtherLanguageException = true;
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    if (hr == hrLocal)
                    {
                        try
                        {
                            iErrorInfo = restrictedErrorInfoRef.As<ABI.WinRT.Interop.IErrorInfo.Vftbl>();
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }

            using (iErrorInfo)
            {
                switch (hr)
                {
                    case E_ILLEGAL_STATE_CHANGE:
                    case E_ILLEGAL_METHOD_CALL:
                    case E_ILLEGAL_DELEGATE_ASSIGNMENT:
                    case APPMODEL_ERROR_NO_PACKAGE:
                        ex = new InvalidOperationException(description);
                        break;
                    case E_XAMLPARSEFAILED:
                        ex = new Windows.UI.Xaml.Markup.XamlParseException();
                        break;
                    case E_LAYOUTCYCLE:
                        ex = new Windows.UI.Xaml.LayoutCycleException();
                        break;
                    case E_ELEMENTNOTAVAILABLE:
                        ex = new Windows.UI.Xaml.Automation.ElementNotAvailableException();
                        break;
                    case E_ELEMENTNOTENABLED:
                        ex = new Windows.UI.Xaml.Automation.ElementNotEnabledException();
                        break;
                    default:
                        ex = Marshal.GetExceptionForHR(hr, iErrorInfo?.ThisPtr ?? (IntPtr)(-1));
                        break;
                }
            }

            ex.AddExceptionDataForRestrictedErrorInfo(
                description,
                restrictedError,
                restrictedErrorReference,
                restrictedCapabilitySid,
                restrictedErrorInfoToSave,
                hasOtherLanguageException);

            return ex;
        }

        public static unsafe void SetErrorInfo(Exception ex)
        {
            if (getRestrictedErrorInfo != null && setRestrictedErrorInfo != null && roOriginateLanguageException != null)
            {
                // If the exception has information for an IRestrictedErrorInfo, use that
                // as our error so as to propagate the error through WinRT end-to-end.
                if (ex.TryGetRestrictedLanguageErrorObject(out var restrictedErrorObject))
                {
                    setRestrictedErrorInfo(restrictedErrorObject.GetRef());
                }
                else
                {
                    string message = ex.Message;
                    if (string.IsNullOrEmpty(message))
                    {
                        message = ex.GetType().FullName;
                    }

                    IntPtr hstring;

                    if (Platform.WindowsCreateString(message, message.Length, &hstring) != 0)
                    {
                        hstring = IntPtr.Zero;
                    }

                    using (var managedExceptionWrapper = ComWrappersSupport.CreateCCWForObject(ex))
                    {
                        roOriginateLanguageException(GetHRForException(ex), hstring, managedExceptionWrapper.ThisPtr);
                    }
                }
            }
            else
            {
                using (var iErrorInfo = ComWrappersSupport.CreateCCWForObject(new ManagedExceptionErrorInfo(ex)))
                {
                    Platform.SetErrorInfo(0, iErrorInfo.ThisPtr);
                }
            }
        }

        public static int GetHRForException(Exception ex)
        {
            int hr = ex.HResult;
            if (ex.TryGetRestrictedLanguageErrorObject(out var restrictedErrorObject))
            {
                restrictedErrorObject.AsType<ABI.WinRT.Interop.IRestrictedErrorInfo>().GetErrorDetails(out _, out hr, out _, out _);
            }
            if (hr == COR_E_OBJECTDISPOSED)
            {
                return RO_E_CLOSED;
            }
            return hr;
        }

        //
        // Exception requires anything to be added into Data dictionary is serializable
        // This wrapper is made serializable to satisfy this requirement but does NOT serialize
        // the object and simply ignores it during serialization, because we only need
        // the exception instance in the app to hold the error object alive.
        //
        [Serializable]
        internal class __RestrictedErrorObject
        {
            // Hold the error object instance but don't serialize/deserialize it
            [NonSerialized]
            private readonly IObjectReference _realErrorObject;

            internal __RestrictedErrorObject(IObjectReference errorObject)
            {
                _realErrorObject = errorObject;
            }

            public IObjectReference RealErrorObject
            {
                get
                {
                    return _realErrorObject;
                }
            }
        }

        internal static void AddExceptionDataForRestrictedErrorInfo(
            this Exception ex,
            string description,
            string restrictedError,
            string restrictedErrorReference,
            string restrictedCapabilitySid,
            IObjectReference restrictedErrorObject,
            bool hasRestrictedLanguageErrorObject = false)
        {
            IDictionary dict = ex.Data;
            if (dict != null)
            {
                dict.Add("Description", description);
                dict.Add("RestrictedDescription", restrictedError);
                dict.Add("RestrictedErrorReference", restrictedErrorReference);
                dict.Add("RestrictedCapabilitySid", restrictedCapabilitySid);

                // Keep the error object alive so that user could retrieve error information
                // using Data["RestrictedErrorReference"]
                dict.Add("__RestrictedErrorObjectReference", restrictedErrorObject == null ? null : new __RestrictedErrorObject(restrictedErrorObject));
                dict.Add("__HasRestrictedLanguageErrorObject", hasRestrictedLanguageErrorObject);
            }
        }

        internal static bool TryGetRestrictedLanguageErrorObject(
            this Exception ex,
            out IObjectReference restrictedErrorObject)
        {
            restrictedErrorObject = null;
            IDictionary dict = ex.Data;
            if (dict != null && dict.Contains("__HasRestrictedLanguageErrorObject"))
            {
                if (dict.Contains("__RestrictedErrorObjectReference"))
                {
                    if (dict["__RestrictedErrorObjectReference"] is __RestrictedErrorObject restrictedObject)
                        restrictedErrorObject = restrictedObject.RealErrorObject;
                }
                return (bool)dict["__HasRestrictedLanguageErrorObject"]!;
            }

            return false;
        }
    }

    // IInspectable
    [Guid("AF86E2E0-B12D-4c6a-9C5A-D7AA65101E90")]
    public class IInspectable
    {
        [Guid("AF86E2E0-B12D-4c6a-9C5A-D7AA65101E90")]
        public struct Vftbl
        {
            public delegate int _GetIids(IntPtr pThis, out uint iidCount, out Guid[] iids);
            public delegate int _GetRuntimeClassName(IntPtr pThis, out IntPtr className);
            public delegate int _GetTrustLevel(IntPtr pThis, out TrustLevel trustLevel);

            public IUnknownVftbl IUnknownVftbl;
            public _GetIids GetIids;
            public _GetRuntimeClassName GetRuntimeClassName;
            public _GetTrustLevel GetTrustLevel;

            public static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;

            static Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IUnknownVftbl = IUnknownVftbl.AbiToProjectionVftbl,
                    GetIids = Do_Abi_GetIids,
                    GetRuntimeClassName = Do_Abi_GetRuntimeClassName,
                    GetTrustLevel = Do_Abi_GetTrustLevel
                };
                AbiToProjectionVftablePtr = Marshal.AllocHGlobal(Marshal.SizeOf<Vftbl>());
                Marshal.StructureToPtr(AbiToProjectionVftable, AbiToProjectionVftablePtr, false);
            }

            private static int Do_Abi_GetIids(IntPtr pThis, out uint iidCount, out Guid[] iids)
            {
                iidCount = 0u;
                iids = null;
                try
                {
                    iids = ComWrappersSupport.GetInspectableInfo(pThis).IIDs;
                    iidCount = (uint)iids.Length;
                }
                catch (Exception ex)
                {
                    ExceptionHelpers.SetErrorInfo(ex);
                    return ExceptionHelpers.GetHRForException(ex);
                }
                return 0;
            }

            private unsafe static int Do_Abi_GetRuntimeClassName(IntPtr pThis, out IntPtr className)
            {
                className = default;
                try
                {
                    className = MarshalString.FromManaged(ComWrappersSupport.GetInspectableInfo(pThis).RuntimeClassName);
                }
                catch (Exception ex)
                {
                    ExceptionHelpers.SetErrorInfo(ex);
                    return ExceptionHelpers.GetHRForException(ex);
                }
                return 0;
            }

            private static int Do_Abi_GetTrustLevel(IntPtr pThis, out TrustLevel trustLevel)
            {
                trustLevel = TrustLevel.BaseTrust;
                return 0;
            }
        }

        public static IInspectable FromAbi(IntPtr thisPtr) =>
            new IInspectable(ObjectReference<Vftbl>.FromAbi(thisPtr));

        private readonly ObjectReference<Vftbl> _obj;
        public IntPtr ThisPtr => _obj.ThisPtr;
        public static implicit operator IInspectable(IObjectReference obj) => obj.As<Vftbl>();
        public static implicit operator IInspectable(ObjectReference<Vftbl> obj) => new IInspectable(obj);
        public ObjectReference<I> As<I>() => _obj.As<I>();
        public IInspectable(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        public IInspectable(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
        }

        public string GetRuntimeClassName()
        {
            IntPtr __retval = default;
            try
            {
                ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetRuntimeClassName(ThisPtr, out __retval));
                return MarshalString.FromAbi(__retval);
            }
            finally
            {
                MarshalString.DisposeAbi(__retval);
            }
        }
    }

    internal class Platform
    {
        [DllImport("api-ms-win-core-com-l1-1-0.dll")]
        internal static extern int CoDecrementMTAUsage(IntPtr cookie);

        [DllImport("api-ms-win-core-com-l1-1-0.dll")]
        internal static extern unsafe int CoIncrementMTAUsage(IntPtr* cookie);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool FreeLibrary(IntPtr moduleHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr GetProcAddress(IntPtr moduleHandle, [MarshalAs(UnmanagedType.LPStr)] string functionName);

        internal static T GetProcAddress<T>(IntPtr moduleHandle)
        {
            IntPtr functionPtr = Platform.GetProcAddress(moduleHandle, typeof(T).Name);
            if (functionPtr == IntPtr.Zero)
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
            return Marshal.GetDelegateForFunctionPointer<T>(functionPtr);
        }

        [DllImport("oleaut32.dll")]
        internal static extern int SetErrorInfo(uint dwReserved, IntPtr perrinfo);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr LoadLibraryExW([MarshalAs(UnmanagedType.LPWStr)] string fileName, IntPtr fileHandle, uint flags);

        [DllImport("api-ms-win-core-winrt-l1-1-0.dll")]
        internal static extern unsafe int RoGetActivationFactory(IntPtr runtimeClassId, ref Guid iid, IntPtr* factory);

        [DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe int WindowsCreateString([MarshalAs(UnmanagedType.LPWStr)] string sourceString,
                                                  int length,
                                                  IntPtr* hstring);

        [DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe int WindowsCreateStringReference(char* sourceString,
                                                  int length,
                                                  IntPtr* hstring_header,
                                                  IntPtr* hstring);

        [DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern int WindowsDeleteString(IntPtr hstring);

        [DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe int WindowsDuplicateString(IntPtr sourceString,
                                                  IntPtr* hstring);

        [DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe char* WindowsGetStringRawBuffer(IntPtr hstring, uint* length);
    }

    internal class Mono
    {
        static Lazy<bool> _usingMono = new Lazy<bool>(() =>
        {
            var modulePtr = Platform.LoadLibraryExW("mono-2.0-bdwgc.dll", IntPtr.Zero, 0);
            if (modulePtr == IntPtr.Zero) return false;

            if (!Platform.FreeLibrary(modulePtr))
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
            return true;
        });

        [DllImport("mono-2.0-bdwgc.dll")]
        static extern IntPtr mono_thread_current();

        [DllImport("mono-2.0-bdwgc.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool mono_thread_is_foreign(IntPtr threadPtr);

        [DllImport("mono-2.0-bdwgc.dll")]
        static extern void mono_unity_thread_fast_attach(IntPtr domainPtr);

        [DllImport("mono-2.0-bdwgc.dll")]
        static extern void mono_unity_thread_fast_detach();

        [DllImport("mono-2.0-bdwgc.dll")]
        static extern void mono_thread_pop_appdomain_ref();

        [DllImport("mono-2.0-bdwgc.dll")]
        static extern IntPtr mono_domain_get();

        struct MonoObject
        {
            IntPtr vtable;
            IntPtr synchronisation; // sic
        }

        unsafe struct MonoThread
        {
            MonoObject obj;
            public MonoInternalThread_x64* internal_thread;
            IntPtr start_obj;
            IntPtr pending_exception;
        }

        [Flags]
        enum MonoThreadFlag : int
        {
            MONO_THREAD_FLAG_DONT_MANAGE = 1,
            MONO_THREAD_FLAG_NAME_SET = 2,
            MONO_THREAD_FLAG_APPDOMAIN_ABORT = 4,
        }

        [StructLayout(LayoutKind.Explicit)]
        struct MonoInternalThread_x64
        {
            [FieldOffset(0xd0)]
            public MonoThreadFlag flags;
        }

        public class ThreadContext : IDisposable
        {
            static Lazy<HashSet<IntPtr>> _foreignThreads = new Lazy<HashSet<IntPtr>>();

            readonly IntPtr _threadPtr = IntPtr.Zero;

            public ThreadContext()
            {
                if (_usingMono.Value)
                {
                    // nothing to do for Mono-native threads
                    var threadPtr = mono_thread_current();
                    if (mono_thread_is_foreign(threadPtr))
                    {
                        // initialize this thread the first time it runs managed code, and remember it for future reference
                        if (_foreignThreads.Value.Add(threadPtr))
                        {
                            // clear initial appdomain ref for new foreign threads to avoid deadlock on domain unload
                            mono_thread_pop_appdomain_ref();

                            unsafe
                            {
                                // tell Mono to ignore the thread on process shutdown since there's nothing to synchronize with
                                ((MonoThread*)threadPtr)->internal_thread->flags |= MonoThreadFlag.MONO_THREAD_FLAG_DONT_MANAGE;
                            }
                        }

                        unsafe
                        {
                            // attach as Unity does to set up the proper domain for the call
                            mono_unity_thread_fast_attach(mono_domain_get());
                            _threadPtr = threadPtr;
                        }
                    }
                }
            }

            public void Dispose()
            {
                if (_threadPtr != IntPtr.Zero)
                {
                    // detach as Unity does to properly reset the domain context
                    mono_unity_thread_fast_detach();
                }
            }
        }
    }

    internal struct VftblPtr
    {
        public IntPtr Vftbl;
    }

    public abstract class IObjectReference : IDisposable
    {
        protected bool disposed;
        public readonly IntPtr ThisPtr;
        protected virtual Interop.IUnknownVftbl VftblIUnknown { get; }

        protected IObjectReference(IntPtr thisPtr)
        {
            if (thisPtr == IntPtr.Zero)
            {
                throw new ArgumentNullException(nameof(thisPtr));
            }
            ThisPtr = thisPtr;
        }

        ~IObjectReference()
        {
            Dispose(false);
        }

        public ObjectReference<T> As<T>() => As<T>(GuidGenerator.GetIID(typeof(T)));
        public unsafe ObjectReference<T> As<T>(Guid iid)
        {
            ThrowIfDisposed();
            IntPtr thatPtr;
            Marshal.ThrowExceptionForHR(VftblIUnknown.QueryInterface(ThisPtr, ref iid, out thatPtr));
            return ObjectReference<T>.Attach(ref thatPtr);
        }

        public unsafe IObjectReference As(Guid iid)
        {
            ThrowIfDisposed();
            IntPtr thatPtr;
            Marshal.ThrowExceptionForHR(VftblIUnknown.QueryInterface(ThisPtr, ref iid, out thatPtr));
            return ObjectReference<Interop.IUnknownVftbl>.Attach(ref thatPtr);
        }

        public T AsType<T>()
        {
            ThrowIfDisposed();
            var ctor = typeof(T).GetConstructor(new[] { typeof(IObjectReference) });
            if (ctor != null)
            {
                return (T)ctor.Invoke(new[] { this });
            }
            throw new InvalidOperationException("Target type is not a projected interface.");
        }

        public IntPtr GetRef()
        {
            ThrowIfDisposed();
            VftblIUnknown.AddRef(ThisPtr);
            return ThisPtr;
        }

        private void ThrowIfDisposed()
        {
            if (disposed) throw new ObjectDisposedException("ObjectReference");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }
            VftblIUnknown.Release(ThisPtr);
            disposed = true;
        }

        internal unsafe bool IsReferenceToManagedObject
        {
            get
            {
                using (var unknownObjRef = this.As<IUnknownVftbl>())
                {
                    return ((VftblPtr*)unknownObjRef.ThisPtr.ToPointer())->Vftbl == IUnknownVftbl.AbiToProjectionVftblPtr;
                }
            }
        }
    }

    public class ObjectReference<T> : IObjectReference
    {
        protected override IUnknownVftbl VftblIUnknown => _vftblIUnknown;
        readonly IUnknownVftbl _vftblIUnknown;
        public readonly T Vftbl;

        public static ObjectReference<T> Attach(ref IntPtr thisPtr)
        {
            var obj = new ObjectReference<T>(thisPtr);
            thisPtr = IntPtr.Zero;
            return obj;
        }

        ObjectReference(IntPtr thisPtr, IUnknownVftbl vftblIUnknown, T vftblT) :
            base(thisPtr)
        {
            _vftblIUnknown = vftblIUnknown;
            Vftbl = vftblT;
        }

        ObjectReference(IntPtr thisPtr) :
            this(thisPtr, GetVtables(thisPtr))
        {
        }

        ObjectReference(IntPtr thisPtr, (IUnknownVftbl vftblIUnknown, T vftblT) vtables) :
            this(thisPtr, vtables.vftblIUnknown, vtables.vftblT)
        {
        }

        public static ObjectReference<T> FromAbi(IntPtr thisPtr, IUnknownVftbl vftblIUnknown, T vftblT)
        {
            if (thisPtr == IntPtr.Zero)
            {
                return null;
            }
            var obj = new ObjectReference<T>(thisPtr, vftblIUnknown, vftblT);
            obj._vftblIUnknown.AddRef(obj.ThisPtr);
            return obj;
        }

        public static ObjectReference<T> FromAbi(IntPtr thisPtr)
        {
            if (thisPtr == IntPtr.Zero)
            {
                return null;
            }
            var (vftblIUnknown, vftblT) = GetVtables(thisPtr);
            return FromAbi(thisPtr, vftblIUnknown, vftblT);
        }

        // C# doesn't allow us to express that T contains IUnknownVftbl, so we'll use a tuple
        private static (IUnknownVftbl vftblIUnknown, T vftblT) GetVtables(IntPtr thisPtr)
        {
            var vftblPtr = Marshal.PtrToStructure<VftblPtr>(thisPtr);
            var vftblIUnknown = Marshal.PtrToStructure<IUnknownVftbl>(vftblPtr.Vftbl);
            T vftblT;
            if (typeof(T).IsGenericType)
            {
                vftblT = (T)typeof(T).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.CreateInstance, null, new[] { typeof(IntPtr) }, null).Invoke(new object[] { thisPtr });
            }
            else
            {
                vftblT = Marshal.PtrToStructure<T>(vftblPtr.Vftbl);
            }
            return (vftblIUnknown, vftblT);
        }
    }

    internal class DllModule
    {
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public unsafe delegate int DllGetActivationFactory(
            IntPtr activatableClassId,
            out IntPtr activationFactory);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public unsafe delegate int DllCanUnloadNow();

        readonly string _fileName;
        readonly IntPtr _moduleHandle;
        readonly DllGetActivationFactory _GetActivationFactory;
        readonly DllCanUnloadNow _CanUnloadNow; // TODO: Eventually periodically call

        static readonly string _currentModuleDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

        static Dictionary<string, DllModule> _cache = new System.Collections.Generic.Dictionary<string, DllModule>();

        public static DllModule Load(string fileName)
        {
            lock (_cache)
            {
                DllModule module;
                if (!_cache.TryGetValue(fileName, out module))
                {
                    module = new DllModule(fileName);
                    _cache[fileName] = module;
                }
                return module;
            }
        }

        DllModule(string fileName)
        {
            _fileName = fileName;

            // Explicitly look for module in the same directory as this one, and
            // use altered search path to ensure any dependencies in the same directory are found.
            _moduleHandle = Platform.LoadLibraryExW(System.IO.Path.Combine(_currentModuleDirectory, fileName), IntPtr.Zero, /* LOAD_WITH_ALTERED_SEARCH_PATH */ 8);
            if (_moduleHandle == IntPtr.Zero)
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }

            _GetActivationFactory = Platform.GetProcAddress<DllGetActivationFactory>(_moduleHandle);

            var canUnloadNow = Platform.GetProcAddress(_moduleHandle, "DllCanUnloadNow");
            if (canUnloadNow != IntPtr.Zero)
            {
                _CanUnloadNow = Marshal.GetDelegateForFunctionPointer<DllCanUnloadNow>(canUnloadNow);
            }
        }

        public unsafe (ObjectReference<IActivationFactoryVftbl> obj, int hr) GetActivationFactory(string runtimeClassId)
        {
            IntPtr instancePtr;
            var hstrRuntimeClassId = MarshalString.CreateMarshaler(runtimeClassId);
            int hr = _GetActivationFactory(MarshalString.GetAbi(hstrRuntimeClassId), out instancePtr);
            return (hr == 0 ? ObjectReference<IActivationFactoryVftbl>.Attach(ref instancePtr) : null, hr);
        }

        ~DllModule()
        {
            System.Diagnostics.Debug.Assert(_CanUnloadNow == null || _CanUnloadNow() == 0); // S_OK
            lock (_cache)
            {
                _cache.Remove(_fileName);
            }
            if ((_moduleHandle != IntPtr.Zero) && !Platform.FreeLibrary(_moduleHandle))
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
        }
    }

    internal class WeakLazy<T> where T : class, new()
    {
        WeakReference<T> _instance = new WeakReference<T>(null);
        public T Value
        {
            get
            {
                lock (_instance)
                {
                    T value;
                    if (!_instance.TryGetTarget(out value))
                    {
                        value = new T();
                        _instance.SetTarget(value);
                    }
                    return value;
                }
            }
        }
    }

    internal class WinrtModule
    {
        readonly IntPtr _mtaCookie;
        static Lazy<WinrtModule> _instance = new Lazy<WinrtModule>();
        public static WinrtModule Instance => _instance.Value;

        public unsafe WinrtModule()
        {
            IntPtr mtaCookie;
            Marshal.ThrowExceptionForHR(Platform.CoIncrementMTAUsage(&mtaCookie));
            _mtaCookie = mtaCookie;
        }

        public static unsafe (ObjectReference<IActivationFactoryVftbl> obj, int hr) GetActivationFactory(string runtimeClassId)
        {
            var module = Instance; // Ensure COM is initialized
            Guid iid = typeof(IActivationFactoryVftbl).GUID;
            IntPtr instancePtr;
            var hstrRuntimeClassId = MarshalString.CreateMarshaler(runtimeClassId);
            int hr = Platform.RoGetActivationFactory(MarshalString.GetAbi(hstrRuntimeClassId), ref iid, &instancePtr);
            return (hr == 0 ? ObjectReference<IActivationFactoryVftbl>.Attach(ref instancePtr) : null, hr);
        }

        ~WinrtModule()
        {
            Marshal.ThrowExceptionForHR(Platform.CoDecrementMTAUsage(_mtaCookie));
        }
    }

    internal class BaseActivationFactory
    {
        private ObjectReference<IActivationFactoryVftbl> _IActivationFactory;

        public BaseActivationFactory(string typeNamespace, string typeFullName)
        {
            var runtimeClassId = typeFullName.Replace("WinRT", "Windows");

            // Prefer the RoGetActivationFactory HRESULT failure over the LoadLibrary/etc. failure
            int hr;
            (_IActivationFactory, hr) = WinrtModule.GetActivationFactory(runtimeClassId);
            if (_IActivationFactory != null) { return; }

            var moduleName = typeNamespace;
            while (true)
            {
                try
                {
                    (_IActivationFactory, _) = DllModule.Load(moduleName + ".dll").GetActivationFactory(runtimeClassId);
                    if (_IActivationFactory != null) { return; }
                }
                catch (Exception) { }

                var lastSegment = moduleName.LastIndexOf(".");
                if (lastSegment <= 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }
                moduleName = moduleName.Remove(lastSegment);
            }
        }

        public unsafe ObjectReference<I> _ActivateInstance<I>()
        {
            IntPtr instancePtr = IntPtr.Zero;
            Marshal.ThrowExceptionForHR(_IActivationFactory.Vftbl.ActivateInstance(_IActivationFactory.ThisPtr, out instancePtr));
            return ObjectReference<IInspectable.Vftbl>.Attach(ref instancePtr).As<I>();
        }

        public ObjectReference<I> _As<I>() => _IActivationFactory.As<I>();
    }

    internal class ActivationFactory<T> : BaseActivationFactory
    {
        public ActivationFactory() : base(typeof(T).Namespace, typeof(T).FullName) { }

        static WeakLazy<ActivationFactory<T>> _factory = new WeakLazy<ActivationFactory<T>>();
        public static ObjectReference<I> As<I>() => _factory.Value._As<I>();
        public static ObjectReference<I> ActivateInstance<I>() => _factory.Value._ActivateInstance<I>();
    }

    internal class EventSource<TDelegate>
        where TDelegate : class, MulticastDelegate
    {
        readonly IObjectReference _obj;
        readonly _add_EventHandler _addHandler;
        readonly _remove_EventHandler _removeHandler;

        private Windows.Foundation.EventRegistrationToken _token;
        private TDelegate _event;

        public void Subscribe(TDelegate del)
        {
            lock (this)
            {
                if (_event == null)
                {
                    var marshaler = Marshaler<TDelegate>.CreateMarshaler((TDelegate)EventInvoke);
                    try
                    {
                        var nativeDelegate = (IntPtr)Marshaler<TDelegate>.GetAbi(marshaler);
                        Marshal.ThrowExceptionForHR(_addHandler(_obj.ThisPtr, nativeDelegate, out Windows.Foundation.EventRegistrationToken token));
                        _token = token;
                    }
                    finally
                    {
                        // Dispose our managed reference to the delegate's CCW.
                        // The either native event holds a reference now or the _addHandler call failed.
                        Marshaler<TDelegate>.DisposeMarshaler(marshaler);
                    }
                }
                _event = (TDelegate)global::System.Delegate.Combine(_event, del);
            }
        }

        public void Unsubscribe(TDelegate del)
        {
            lock (this)
            {
                _event = (TDelegate)global::System.Delegate.Remove(_event, del);
            }
            if (_event == null)
            {
                _UnsubscribeFromNative();
            }
        }

        private System.Delegate _eventInvoke;
        private System.Delegate EventInvoke
        {
            get
            {
                if (_eventInvoke is object)
                {
                    return _eventInvoke;
                }

                MethodInfo invoke = typeof(TDelegate).GetMethod("Invoke");
                ParameterInfo[] invokeParameters = invoke.GetParameters();
                ParameterExpression[] parameters = new ParameterExpression[invokeParameters.Length];
                for (int i = 0; i < invokeParameters.Length; i++)
                {
                    parameters[i] = Expression.Parameter(invokeParameters[i].ParameterType, invokeParameters[i].Name);
                }

                ParameterExpression delegateLocal = Expression.Parameter(typeof(TDelegate), "event");

                _eventInvoke = Expression.Lambda(typeof(TDelegate),
                    Expression.Block(
                        new[] { delegateLocal },
                        Expression.Assign(delegateLocal, Expression.Field(Expression.Constant(this), typeof(EventSource<TDelegate>).GetField(nameof(_event), BindingFlags.Instance | BindingFlags.NonPublic))),
                        Expression.IfThen(
                            Expression.ReferenceNotEqual(delegateLocal, Expression.Constant(null, typeof(TDelegate))), Expression.Call(delegateLocal, invoke, parameters))),
                    parameters).Compile();

                return _eventInvoke;
            }
        }

        internal EventSource(IObjectReference obj, _add_EventHandler addHandler, _remove_EventHandler removeHandler)
        {
            _obj = obj;
            _addHandler = addHandler;
            _removeHandler = removeHandler;
        }

        void _UnsubscribeFromNative()
        {
            Marshal.ThrowExceptionForHR(_removeHandler(_obj.ThisPtr, _token));
            _token.Value = 0;
        }
    }

    // An event registration token table stores mappings from delegates to event tokens, in order to support
    // sourcing WinRT style events from managed code.
    internal sealed class EventRegistrationTokenTable<T> where T : class, global::System.Delegate
    {
        // Note this dictionary is also used as the synchronization object for this table
        private readonly Dictionary<Windows.Foundation.EventRegistrationToken, T> m_tokens = new Dictionary<Windows.Foundation.EventRegistrationToken, T>();

        public Windows.Foundation.EventRegistrationToken AddEventHandler(T handler)
        {
            // Windows Runtime allows null handlers.  Assign those the default token (token value 0) for simplicity
            if (handler == null)
            {
                return default;
            }

            lock (m_tokens)
            {
                return AddEventHandlerNoLock(handler);
            }
        }

        private Windows.Foundation.EventRegistrationToken AddEventHandlerNoLock(T handler)
        {
            Debug.Assert(handler != null);

            // Get a registration token, making sure that we haven't already used the value.  This should be quite
            // rare, but in the case it does happen, just keep trying until we find one that's unused.
            Windows.Foundation.EventRegistrationToken token = GetPreferredToken(handler);
            while (m_tokens.ContainsKey(token))
            {
                token = new Windows.Foundation.EventRegistrationToken { Value = token.Value + 1 };
            }
            m_tokens[token] = handler;

            return token;
        }

        // Generate a token that may be used for a particular event handler.  We will frequently be called
        // upon to look up a token value given only a delegate to start from.  Therefore, we want to make
        // an initial token value that is easily determined using only the delegate instance itself.  Although
        // in the common case this token value will be used to uniquely identify the handler, it is not
        // the only possible token that can represent the handler.
        //
        // This means that both:
        //  * if there is a handler assigned to the generated initial token value, it is not necessarily
        //    this handler.
        //  * if there is no handler assigned to the generated initial token value, the handler may still
        //    be registered under a different token
        //
        // Effectively the only reasonable thing to do with this value is either to:
        //  1. Use it as a good starting point for generating a token for handler
        //  2. Use it as a guess to quickly see if the handler was really assigned this token value
        private static Windows.Foundation.EventRegistrationToken GetPreferredToken(T handler)
        {
            Debug.Assert(handler != null);

            // We want to generate a token value that has the following properties:
            //  1. is quickly obtained from the handler instance
            //  2. uses bits in the upper 32 bits of the 64 bit value, in order to avoid bugs where code
            //     may assume the value is really just 32 bits
            //  3. uses bits in the bottom 32 bits of the 64 bit value, in order to ensure that code doesn't
            //     take a dependency on them always being 0.
            //
            // The simple algorithm chosen here is to simply assign the upper 32 bits the metadata token of the
            // event handler type, and the lower 32 bits the hash code of the handler instance itself. Using the
            // metadata token for the upper 32 bits gives us at least a small chance of being able to identify a
            // totally corrupted token if we ever come across one in a minidump or other scenario.
            //
            // The hash code of a unicast delegate is not tied to the method being invoked, so in the case
            // of a unicast delegate, the hash code of the target method is used instead of the full delegate
            // hash code.
            //
            // While calculating this initial value will be somewhat more expensive than just using a counter
            // for events that have few registrations, it will also give us a shot at preventing unregistration
            // from becoming an O(N) operation.
            //
            // We should feel free to change this algorithm as other requirements / optimizations become
            // available.  This implementation is sufficiently random that code cannot simply guess the value to
            // take a dependency upon it.  (Simply applying the hash-value algorithm directly won't work in the
            // case of collisions, where we'll use a different token value).

            uint handlerHashCode;
            global::System.Delegate[] invocationList = ((global::System.Delegate)(object)handler).GetInvocationList();
            if (invocationList.Length == 1)
            {
                handlerHashCode = (uint)invocationList[0].Method.GetHashCode();
            }
            else
            {
                handlerHashCode = (uint)handler.GetHashCode();
            }

            ulong tokenValue = ((ulong)(uint)typeof(T).MetadataToken << 32) | handlerHashCode;
            return new Windows.Foundation.EventRegistrationToken { Value = (long)tokenValue };
        }

        // Remove the event handler from the table and
        // Get the delegate associated with an event registration token if it exists
        // If the event registration token is not registered, returns false
        public bool RemoveEventHandler(Windows.Foundation.EventRegistrationToken token, out T handler)
        {
            lock (m_tokens)
            {
                if (m_tokens.TryGetValue(token, out handler))
                {
                    RemoveEventHandlerNoLock(token);
                    return true;
                }
            }

            return false;
        }

        private void RemoveEventHandlerNoLock(Windows.Foundation.EventRegistrationToken token)
        {
            if (m_tokens.TryGetValue(token, out T handler))
            {
                m_tokens.Remove(token);
            }
        }
    }
}

namespace Windows.UI.Xaml
{
    using System.Runtime.Serialization;
    namespace Automation
    {
        [Serializable]
        public class ElementNotAvailableException : Exception
        {
            public ElementNotAvailableException()
                : base("The element is not available.")
            {
                HResult = WinRT.ExceptionHelpers.E_ELEMENTNOTAVAILABLE;
            }

            public ElementNotAvailableException(string message)
                : base(message)
            {
                HResult = WinRT.ExceptionHelpers.E_ELEMENTNOTAVAILABLE;
            }

            public ElementNotAvailableException(string message, Exception innerException)
                : base(message, innerException)
            {
                HResult = WinRT.ExceptionHelpers.E_ELEMENTNOTAVAILABLE;
            }

            protected ElementNotAvailableException(SerializationInfo serializationInfo, StreamingContext streamingContext)
                : base(serializationInfo, streamingContext)
            {
            }
        }

        public class ElementNotEnabledException : Exception
        {
            public ElementNotEnabledException()
                : base("The element is not enabled.")
            {
                HResult = WinRT.ExceptionHelpers.E_ELEMENTNOTENABLED;
            }

            public ElementNotEnabledException(string message)
                : base(message)
            {
                HResult = WinRT.ExceptionHelpers.E_ELEMENTNOTENABLED;
            }

            public ElementNotEnabledException(string message, Exception innerException)
                : base(message, innerException)
            {
                HResult = WinRT.ExceptionHelpers.E_ELEMENTNOTENABLED;
            }
        }
    }
    namespace Markup
    {
        public class XamlParseException : Exception
        {
            public XamlParseException()
                : base("XAML parsing failed.")
            {
                HResult = WinRT.ExceptionHelpers.E_XAMLPARSEFAILED;
            }

            public XamlParseException(string message)
                : base(message)
            {
                HResult = WinRT.ExceptionHelpers.E_XAMLPARSEFAILED;
            }

            public XamlParseException(string message, Exception innerException)
                : base(message, innerException)
            {
                HResult = WinRT.ExceptionHelpers.E_XAMLPARSEFAILED;
            }
        }
    }
    [Serializable]
    public class LayoutCycleException : Exception
    {
        public LayoutCycleException()
            : base("A cycle occurred while laying out the GUI.")
        {
            HResult = WinRT.ExceptionHelpers.E_LAYOUTCYCLE;
        }

        public LayoutCycleException(string message)
            : base(message)
        {
            HResult = WinRT.ExceptionHelpers.E_LAYOUTCYCLE;
        }

        public LayoutCycleException(string message, Exception innerException)
            : base(message, innerException)
        {
            HResult = WinRT.ExceptionHelpers.E_LAYOUTCYCLE;
        }

        protected LayoutCycleException(SerializationInfo serializationInfo, StreamingContext streamingContext)
            : base(serializationInfo, streamingContext)
        {
        }
    }
}
