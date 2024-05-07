// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WinRT.Interop;

namespace WinRT
{
#if EMBED
    internal
#else
    public
#endif 
    class AgileReference : IDisposable
    {
        private readonly static Guid CLSID_StdGlobalInterfaceTable = new(0x00000323, 0, 0, 0xc0, 0, 0, 0, 0, 0, 0, 0x46);
        private static readonly object _lock = new();
        private static volatile IGlobalInterfaceTable _git;

        // Simple singleton lazy-initialization scheme (and saving the Lazy<T> size)
        internal static IGlobalInterfaceTable Git
        {
            get
            {
                return _git ?? Git_Slow();

                [MethodImpl(MethodImplOptions.NoInlining)]
                static IGlobalInterfaceTable Git_Slow()
                {
                    lock (_lock)
                    {
                        return _git ??= GetGitTable();
                    }
                }
            }
        }

        private readonly IObjectReference _agileReference;
        private readonly IntPtr _cookie;
        private bool disposed;

        public AgileReference(IObjectReference instance)
            : this(instance?.ThisPtr ?? IntPtr.Zero)
        {
        }

        internal AgileReference(in ObjectReferenceValue instance)
            : this(instance.GetAbi())
        {
        }

        internal unsafe AgileReference(IntPtr thisPtr)
        {
            if (thisPtr == IntPtr.Zero)
            {
                return;
            }

            IntPtr agileReference = default;
            Guid iid = IUnknownVftbl.IID;
            try
            {
                Marshal.ThrowExceptionForHR(Platform.RoGetAgileReference(
                    0 /*AGILEREFERENCE_DEFAULT*/,
                    &iid,
                    thisPtr,
                    &agileReference));
                _agileReference = ObjectReference<IUnknownVftbl>.Attach(ref agileReference, IID.IID_IUnknown);
            }
            catch (TypeLoadException)
            {
                _cookie = Git.RegisterInterfaceInGlobal(thisPtr, iid);
            }
            finally
            {
                MarshalInterface<IAgileReference>.DisposeAbi(agileReference);
            }
        }


        public IObjectReference Get() => _cookie == IntPtr.Zero ? ABI.WinRT.Interop.IAgileReferenceMethods.Resolve(_agileReference, IUnknownVftbl.IID) : Git.GetInterfaceFromGlobal(_cookie, IUnknownVftbl.IID);

        internal ObjectReference<T> Get<T>(Guid iid) => _cookie == IntPtr.Zero ? ABI.WinRT.Interop.IAgileReferenceMethods.Resolve<T>(_agileReference, iid) : Git.GetInterfaceFromGlobal(_cookie, IUnknownVftbl.IID)?.As<T>(iid);

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (_cookie != IntPtr.Zero)
                {
                    try
                    {
                        Git.RevokeInterfaceFromGlobal(_cookie);
                    }
                    catch (ArgumentException)
                    {
                        // Revoking cookie from GIT table may fail if apartment is gone.
                    }
                }
                disposed = true;
            }
        }

        private static unsafe IGlobalInterfaceTable GetGitTable()
        {
            Guid gitClsid = CLSID_StdGlobalInterfaceTable;
            Guid gitIid = IID.IID_IGlobalInterfaceTable;
            IntPtr gitPtr = default;

            try
            {
                Marshal.ThrowExceptionForHR(Platform.CoCreateInstance(
                    &gitClsid,
                    IntPtr.Zero,
                    1 /*CLSCTX_INPROC_SERVER*/,
                    &gitIid,
                    &gitPtr));

#if NET
                return new ABI.WinRT.Interop.IGlobalInterfaceTable(gitPtr);
#else
                return new ABI.WinRT.Interop.IGlobalInterfaceTable(ABI.WinRT.Interop.IGlobalInterfaceTable.FromAbi(gitPtr));
#endif
            }
            finally
            {
                Marshal.Release(gitPtr);
            }
        }

        ~AgileReference()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

#if EMBED
    internal
#else
    public 
#endif
    sealed class AgileReference<T> : AgileReference
        where T : class
    {
        public AgileReference(IObjectReference instance)
            : base(instance)
        {
        }

        internal AgileReference(in ObjectReferenceValue instance)
            : base(instance)
        {
        }

        public new T Get() 
        {
            using var objRef = base.Get();
            return ComWrappersSupport.CreateRcwForComObject<T>(objRef?.ThisPtr ?? IntPtr.Zero);
        }
    }
}