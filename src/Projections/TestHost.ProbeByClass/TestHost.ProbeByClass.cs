using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Windows.Foundation;
using WinRT;


#region Temporary, until authoring support generates activation factory support

namespace Windows.Foundation
{
    [global::WinRT.WindowsRuntimeType]
    [Guid("00000035-0000-0000-c000-000000000046")]
    [WindowsRuntimeHelperType(typeof(global::ABI.Windows.Foundation.IActivationFactory))]
    internal interface IActivationFactory
    {
        Object ActivateInstance();
    }
}

namespace ABI.Windows.Foundation
{
#if !NET
    [global::WinRT.ObjectReferenceWrapper(nameof(_obj))]
#endif
    [Guid("00000035-0000-0000-c000-000000000046")]
    internal class IActivationFactory : global::Windows.Foundation.IActivationFactory
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
                    instance = MarshalInspectable<object>.FromManaged(__instance);
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
        }
        internal static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr, global::WinRT.Interop.IID.IID_IActivationFactory);

        public static implicit operator IActivationFactory(IObjectReference obj) => (obj != null) ? new IActivationFactory(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IObjectReference ObjRef { get => _obj; }
        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<
#if NET
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
#endif
            I>() => _obj.As<I>();
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
                return MarshalInspectable<object>.FromAbi(__retval);
            }
            finally
            {
                MarshalInspectable<object>.DisposeAbi(__retval);
            }
        }
    }
}

namespace WinRT.Host
{
    internal class ActivationFactory : IActivationFactory
    {
        public ConstructorInfo Constructor { get; private set; }

        public ActivationFactory(ConstructorInfo constructor) => Constructor = constructor;

        public object ActivateInstance() => Constructor.Invoke(null);
    }
}

#endregion

namespace WinRT
{
    public static class Module
    {
#if NET
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(TestHost.ProbeByClass))]
        [UnconditionalSuppressMessage("Trimming", "IL2057", Justification = "We're manually keeping the target type around.")]

#endif
        public static unsafe IntPtr GetActivationFactory(string runtimeClassId)
        {
            if (string.CompareOrdinal(runtimeClassId, "TestHost.ProbeByClass") == 0)
            {
                var type = Type.GetType(runtimeClassId);
                if (type != null)
                {
                    var ctor = type.GetConstructor(Type.EmptyTypes);
                    if (ctor != null)
                    {
                        var factory = new WinRT.Host.ActivationFactory(ctor);
                        return MarshalInspectable<WinRT.Host.ActivationFactory>.FromManaged(factory);
                    }
                }
            }
            return IntPtr.Zero;
        }
    }
}

namespace TestHost
{
    public class ProbeByClass : IStringable
    {
#if NET
        [UnconditionalSuppressMessage("SingleFile", "IL3000", Justification = "We're not publishing this test as single file.")]
#endif
        public override string ToString()
        {
            return new System.IO.FileInfo(Assembly.GetExecutingAssembly().Location).Name;
        }
    }
}
