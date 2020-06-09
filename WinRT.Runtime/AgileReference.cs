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
            IntPtr agileReference = default;
            IntPtr gitPtr = default;
            Guid iid = typeof(IUnknownVftbl).GUID;
            try
            {
                Marshal.ThrowExceptionForHR(Platform.RoGetAgileReference(
                    Platform.AgileReferenceOptions.AGILEREFERENCE_DEFAULT,
                    ref iid,
                    instance.ThisPtr,
                    &agileReference));
                _agileReference = ABI.WinRT.Interop.IAgileReference.FromAbi(agileReference).AsType<ABI.WinRT.Interop.IAgileReference>();
            }
            catch(TypeLoadException)
            {
                Guid gitClsid = CLSID_StdGlobalInterfaceTable;
                Guid gitIid = typeof(IGlobalInterfaceTable).GUID;
                Marshal.ThrowExceptionForHR(Platform.CoCreateInstance(
                    ref gitClsid,
                    IntPtr.Zero,
                    1 /*CLSCTX_INPROC_SERVER*/,
                    ref gitIid,
                    &gitPtr));
                _git = ABI.WinRT.Interop.IGlobalInterfaceTable.FromAbi(gitPtr).AsType<ABI.WinRT.Interop.IGlobalInterfaceTable>();
                _cookie = _git.RegisterInterfaceInGlobal(instance, iid);

            }
            finally
            {
                MarshalInterface<IAgileReference>.DisposeAbi(agileReference);
                MarshalInterface<IGlobalInterfaceTable>.DisposeAbi(gitPtr);
            }
        }

        public IObjectReference Get() => _agileReference?.Resolve(typeof(IUnknownVftbl).GUID) ?? _git?.GetInterfaceFromGlobal(_cookie, typeof(IUnknownVftbl).GUID);

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                try
                {
                    if (_git != null && _cookie != IntPtr.Zero)
                    {
                        _git.RevokeInterfaceFromGlobal(_cookie);
                    }
                }
                catch(ObjectDisposedException)
                {
                    // TODO: How to handle removing from git when it has already been disposed.
                }
                disposed = true;
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