using System;
using System.Runtime.InteropServices;
using WinRT.Interop;

namespace WinRT
{
    public class AgileReference
    {
        private readonly IAgileReference _agileReference;

        public unsafe AgileReference(IObjectReference instance)
        {
            Guid iid = typeof(IUnknownVftbl).GUID;
            IntPtr agileReference = IntPtr.Zero;
            try
            {
                Marshal.ThrowExceptionForHR(Platform.RoGetAgileReference(
                    Platform.AgileReferenceOptions.AGILEREFERENCE_DEFAULT,
                    ref iid,
                    instance.ThisPtr,
                    &agileReference));
                _agileReference = ABI.WinRT.Interop.IAgileReference.FromAbi(agileReference).AsType<ABI.WinRT.Interop.IAgileReference>();
            }
            finally
            {
                MarshalInterface<IAgileReference>.DisposeAbi(agileReference);
            }
        }

        public IObjectReference Get() => _agileReference?.Resolve(typeof(IUnknownVftbl).GUID);
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