// TODO: consider embedding this as a resource into WinRT.Host.dll, 
// to simplify deployment

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Windows.Foundation;
using WinRT;


namespace Windows.Foundation
{
    [global::WinRT.WindowsRuntimeType]
    [Guid("00000035-0000-0000-c000-000000000046")]
    internal interface IActivationFactory
    {
        Object ActivateInstance();
    }
}

namespace ABI.Windows.Foundation
{
    [global::WinRT.ObjectReferenceWrapper(nameof(_obj))]
    [Guid("00000035-0000-0000-c000-000000000046")]
    public class IActivationFactory : global::Windows.Foundation.IActivationFactory
    {
        public unsafe delegate int ActivateInstance_0(IntPtr thisPtr, out IntPtr instance);

        [Guid("00000035-0000-0000-c000-000000000046")]
        public struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            public ActivateInstance_0 ActivateInstance_0;

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;
            static unsafe Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,
                    ActivateInstance_0 = Do_Abi_ActivateInstance_0
                };
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 1);
                Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

            private static unsafe int Do_Abi_ActivateInstance_0(IntPtr thisPtr, out IntPtr instance)
            {
                object __instance = default;
                instance = default;
                try
                {
                    __instance = global::WinRT.ComWrappersSupport.FindObject<global::Windows.Foundation.IActivationFactory>(thisPtr).ActivateInstance();
                    instance = MarshalInspectable.FromManaged(__instance);
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
        }
        internal static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr);

        public static implicit operator IActivationFactory(IObjectReference obj) => (obj != null) ? new IActivationFactory(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IObjectReference ObjRef { get => _obj; }
        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public IActivationFactory(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        internal IActivationFactory(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
        }

        public unsafe object ActivateInstance()
        {
            IntPtr __retval = default;
            try
            {
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.ActivateInstance_0(ThisPtr, out __retval));
                return MarshalInspectable.FromAbi(__retval);
            }
            finally
            {
                MarshalInspectable.DisposeAbi(__retval);
            }
        }
    }
}

namespace WinRT
{
    internal class ActivationFactory : IActivationFactory
    {
        //private const int CLASS_E_CLASSNOTAVAILABLE = unchecked((int)0x80040111);

        public ConstructorInfo Constructor { get; private set; }

        public ActivationFactory(ConstructorInfo constructor) => Constructor = constructor;

        public object ActivateInstance() => Constructor.Invoke(null);
    }

    public static class Module
    {
        private const int REGDB_E_CLASSNOTREG = unchecked((int)0x80040154);

        public unsafe delegate int GetActivationFactoryDelegate(IntPtr hstrTargetAssembly, IntPtr hstrRuntimeClassId, IntPtr* activationFactory);
        public static unsafe int GetActivationFactory(IntPtr hstrTargetAssembly, IntPtr hstrRuntimeClassId, IntPtr* activationFactory)
        {
            *activationFactory = IntPtr.Zero;

            var targetAssembly = MarshalString.FromAbi(hstrTargetAssembly);
            var runtimeClassId = MarshalString.FromAbi(hstrRuntimeClassId);

            try
            {
                // Per .NET folks, this may be risky - alternatives to ensure same ALC as shim?
                var assembly = Assembly.LoadFrom(targetAssembly);
                if (assembly != null)
                {
                    var type = assembly.GetType(runtimeClassId);
                    if (type != null)
                    {
                        var ctor = type.GetConstructor(Type.EmptyTypes);
                        if (ctor != null)
                        {
                            var factory = new ActivationFactory(ctor);
                            *activationFactory = MarshalInspectable.FromManaged(factory);
                            return 0;
                        }
                    }
                }
                return REGDB_E_CLASSNOTREG;
            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
        }
    }
}
