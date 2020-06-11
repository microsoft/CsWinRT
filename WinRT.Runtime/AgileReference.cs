using System;
using System.Runtime.InteropServices;
using WinRT.Interop;

namespace WinRT
{
    public class AgileReference : IDisposable
    {
        private readonly static Guid CLSID_StdGlobalInterfaceTable = Guid.Parse("00000323-0000-0000-c000-000000000046");
        private readonly IAgileReference _agileReference;
        private readonly IGlobalInterfaceTable _git;
        private readonly IntPtr _cookie;
        private bool disposed;

        public unsafe AgileReference(IObjectReference instance)
        {
            if(instance?.ThisPtr == null)
            {
                return;
            }   

            IntPtr agileReference = default;
            Guid iid = typeof(IUnknownVftbl).GUID;
            try
            {
                Marshal.ThrowExceptionForHR(Platform.RoGetAgileReference(
                    0 /*AGILEREFERENCE_DEFAULT*/,
                    ref iid,
                    instance.ThisPtr,
                    &agileReference));
                _agileReference = ABI.WinRT.Interop.IAgileReference.FromAbi(agileReference).AsType<ABI.WinRT.Interop.IAgileReference>();
            }
            catch(TypeLoadException)
            {
                _git = GetGitTable();
                _cookie = _git.RegisterInterfaceInGlobal(instance, iid);
            }
            finally
            {
                MarshalInterface<IAgileReference>.DisposeAbi(agileReference);
            }
        }

        public IObjectReference Get() => _agileReference?.Resolve(typeof(IUnknownVftbl).GUID) ?? _git?.GetInterfaceFromGlobal(_cookie, typeof(IUnknownVftbl).GUID);

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (_cookie != IntPtr.Zero)
                {
                    // Obtaining new reference to git table in finalizer case to avoid race with finalizer cleaning up instance variables.
                    var git = disposing ? _git : GetGitTable();
                    git.RevokeInterfaceFromGlobal(_cookie);
                }
                disposed = true;
            }
        }

        private unsafe IGlobalInterfaceTable GetGitTable()
        {
            Guid gitClsid = CLSID_StdGlobalInterfaceTable;
            Guid gitIid = typeof(IGlobalInterfaceTable).GUID;
            IntPtr gitPtr = default;

            try
            {
                Marshal.ThrowExceptionForHR(Platform.CoCreateInstance(
                    ref gitClsid,
                    IntPtr.Zero,
                    1 /*CLSCTX_INPROC_SERVER*/,
                    ref gitIid,
                    &gitPtr));
                return ABI.WinRT.Interop.IGlobalInterfaceTable.FromAbi(gitPtr).AsType<ABI.WinRT.Interop.IGlobalInterfaceTable>();
            }
            finally
            {
                MarshalInterface<IGlobalInterfaceTable>.DisposeAbi(gitPtr);
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

    public sealed class AgileReference<T> : AgileReference
        where T : class
    {
        public unsafe AgileReference(IObjectReference instance)
            : base(instance)
        {
        }

        public new T Get() 
        {
            using var objRef = base.Get();
            return (T) ComWrappersSupport.CreateRcwForComObject(objRef?.ThisPtr ?? IntPtr.Zero);
        }
    }
}