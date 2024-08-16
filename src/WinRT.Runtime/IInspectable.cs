// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using WinRT.Interop;

namespace WinRT
{
#if EMBED
    internal
#else 
    public
#endif
    enum TrustLevel
    {
        BaseTrust = 0,
        PartialTrust = BaseTrust + 1,
        FullTrust = PartialTrust + 1
    }

    /// <summary>
    /// Values that are used in activation calls to indicate the execution contexts in which an object is to be run.
    /// </summary>
    public enum ClassContext : uint
    {
        /// <summary>
        /// The code that creates and manages objects of this class is a DLL that runs in the same process as the caller of the function specifying the class context.
        /// </summary>
        InProcServer = 0x1,

        /// <summary>
        /// The EXE code that creates and manages objects of this class runs on same machine but is loaded in a separate process space.
        /// </summary>
        LocalServer = 0x4
    }

    // IInspectable
#if !NET
    [global::WinRT.ObjectReferenceWrapper(nameof(_obj))]
#endif
    [Guid("AF86E2E0-B12D-4c6a-9C5A-D7AA65101E90")]
#if EMBED
    internal
#else
    public
#endif
    partial class IInspectable
    {
        [Guid("AF86E2E0-B12D-4c6a-9C5A-D7AA65101E90")]
        public unsafe struct Vftbl
        {
            public IUnknownVftbl IUnknownVftbl;
            private void* _GetIids;
            public delegate* unmanaged[Stdcall]<IntPtr, int*, IntPtr*, int> GetIids { get => (delegate* unmanaged[Stdcall]<IntPtr, int*, IntPtr*, int>)_GetIids; set => _GetIids = (void*)value; }
            
            private void* _GetRuntimeClassName;
            public delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int> GetRuntimeClassName { get => (delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)_GetRuntimeClassName; set => _GetRuntimeClassName = (void*)value; }
            
            private void* _GetTrustLevel;
            public delegate* unmanaged[Stdcall]<IntPtr, TrustLevel*, int> GetTrustLevel { get => (delegate* unmanaged[Stdcall]<IntPtr, TrustLevel*, int>)_GetTrustLevel; set => _GetTrustLevel = (void*)value; }

            public static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;

#if !NET
            private static readonly Delegate[] DelegateCache = new Delegate[3];
            private delegate int _GetIidsDelegate(IntPtr pThis, int* iidCount, IntPtr* iids);
            private delegate int _GetRuntimeClassNameDelegate(IntPtr pThis, IntPtr* className);
            private delegate int _GetTrustLevelDelegate(IntPtr pThis, TrustLevel* trustLevel);
#endif

            static Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IUnknownVftbl = IUnknownVftbl.AbiToProjectionVftbl,
#if !NET
                    _GetIids = (void*)Marshal.GetFunctionPointerForDelegate(DelegateCache[0] = new _GetIidsDelegate(Do_Abi_GetIids)),
                    _GetRuntimeClassName = (void*)Marshal.GetFunctionPointerForDelegate(DelegateCache[1] = new _GetRuntimeClassNameDelegate(Do_Abi_GetRuntimeClassName)),
                    _GetTrustLevel = (void*)Marshal.GetFunctionPointerForDelegate(DelegateCache[2] = new _GetTrustLevelDelegate(Do_Abi_GetTrustLevel))
#else
                    _GetIids = (void*)(delegate* unmanaged<IntPtr, int*, IntPtr*, int>)&Do_Abi_GetIids,
                    _GetRuntimeClassName = (void*)(delegate* unmanaged<IntPtr, IntPtr*, int>)&Do_Abi_GetRuntimeClassName,
                    _GetTrustLevel = (void*)(delegate* unmanaged<IntPtr, TrustLevel*, int>)&Do_Abi_GetTrustLevel
#endif
                };
                AbiToProjectionVftablePtr = Marshal.AllocHGlobal(sizeof(Vftbl));
                *(Vftbl*)AbiToProjectionVftablePtr = AbiToProjectionVftable;
            }

#if NET
            [UnmanagedCallersOnly]
#endif
            private static int Do_Abi_GetIids(IntPtr pThis, int* iidCount, IntPtr* iids)
            {
                *iidCount = 0;
                *iids = IntPtr.Zero;
                try
                {
                    (*iidCount, *iids) = MarshalBlittable<Guid>.FromManagedArray(ComWrappersSupport.GetInspectableInfo(pThis).IIDs);
                }
                catch (Exception ex)
                {
                    return ex.HResult;
                }
                return 0;
            }

#if NET
            [UnmanagedCallersOnly]
#endif
            private unsafe static int Do_Abi_GetRuntimeClassName(IntPtr pThis, IntPtr* className)
            {
                *className = default;
                try
                {
                    string runtimeClassName = ComWrappersSupport.GetInspectableInfo(pThis).RuntimeClassName;
                    *className = MarshalString.FromManaged(runtimeClassName);
                }
                catch (Exception ex)
                {
                    return ex.HResult;
                }
                return 0;
            }

#if NET
            [UnmanagedCallersOnly]
#endif
            private static int Do_Abi_GetTrustLevel(IntPtr pThis, TrustLevel* trustLevel)
            {
                *trustLevel = TrustLevel.BaseTrust;
                return 0;
            }
        }

        public static IInspectable FromAbi(IntPtr thisPtr) =>
            new IInspectable(ObjectReference<IUnknownVftbl>.FromAbi(thisPtr, IID.IID_IInspectable));

        private readonly ObjectReference<IUnknownVftbl> _obj;
        public IntPtr ThisPtr => _obj.ThisPtr;
#if !NET
        public static implicit operator IInspectable(IObjectReference obj) => obj.As<Vftbl>(IID.IID_IInspectable);
        public static implicit operator IInspectable(ObjectReference<Vftbl> obj) => new IInspectable(obj);
#endif

#if NET
        [RequiresUnreferencedCode(AttributeMessages.GenericRequiresUnreferencedCodeMessage)]
        [Obsolete(AttributeMessages.GenericDeprecatedMessage)]
        [EditorBrowsable(EditorBrowsableState.Never)]
#endif
        public ObjectReference<I> As<I>() => _obj.As<I>();
        public IObjectReference ObjRef { get => _obj; }
        public IInspectable(IObjectReference obj) : this(obj.As<IUnknownVftbl>(IID.IID_IInspectable)) { }

#if NET
        [Obsolete(AttributeMessages.GenericDeprecatedMessage)]
        [EditorBrowsable(EditorBrowsableState.Never)]
#endif
        public IInspectable(ObjectReference<Vftbl> obj) : this(obj.As<IUnknownVftbl>(IID.IID_IInspectable)) { }

        // Note: callers have to ensure to perform QI for 'IInspectable' when using this constructor
        internal IInspectable(ObjectReference<IUnknownVftbl> obj)
        {
            _obj = obj;
        }

        public unsafe string GetRuntimeClassName(bool noThrow = false)
        {
            IntPtr __retval = default;
            try
            {
                IntPtr thisPtr = ThisPtr;
                var hr = ((delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)(*(void***)thisPtr)[4])(thisPtr, &__retval);
                if (hr != 0)
                {
                    if (noThrow)
                        return null;
                    Marshal.ThrowExceptionForHR(hr);
                }
                uint length;
                char* buffer = Platform.WindowsGetStringRawBuffer(__retval, &length);
                return new string(buffer, 0, (int)length);
            }
            finally
            {
                Platform.WindowsDeleteString(__retval);
            }
        }

        /// <summary>
        /// Activates and marshals a projected WinRT type that does not participate in WinRT activation, by using <c>CoCreateInstance</c>.
        /// This method can be used to activate WinRT types from out-of-proc COM servers (eg. Windows Package Manager types).
        /// </summary>
        /// <typeparam name="T">The type of the projected WinRT object to activate and marshal.</typeparam>
        /// <param name="classId">The CLSID associated with the data and code that will be used to create the object.</param>
        /// <param name="interfaceId">The identifier of the interface to be used to communicate with the object.</param>
        /// <param name="classContext">The context in which the code that manages the newly created object will run.</param>
        /// <returns>The resulting marshalled instance.</returns>
        public static unsafe T CoCreateInstance<T>(in Guid classId, in Guid interfaceId, ClassContext classContext)
            where T : class
        {
            IntPtr thisPtr = IntPtr.Zero;

            try
            {
                fixed (Guid* rclsid = &classId)
                fixed (Guid* riid = &interfaceId)
                {
                    int hresult = Platform.CoCreateInstance(
                        clsid: rclsid,
                        outer: IntPtr.Zero,
                        clsContext: (uint)classContext,
                        iid: riid,
                        instance: &thisPtr);

                    Marshal.ThrowExceptionForHR(hresult);
                }

                return MarshalInspectable<T>.FromAbi(thisPtr);
            }
            finally
            {
                if (thisPtr != IntPtr.Zero)
                {
                    Marshal.Release(thisPtr);
                }
            }
        }
    }
}
