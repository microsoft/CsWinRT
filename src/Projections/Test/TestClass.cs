using System;
using System.Collections.Generic;
using System.Text;
using WinRT;
using WinRT.Interop;

#if NET5_0
namespace test_component_fast
{
    public class Simple2
    {

    }
}

namespace Test
{
    public class TestClass
    {
        public unsafe void TestNonFastAbi()
        {
            var obj = ((IWinRTObject)new test_component_base.HierarchyA()).NativeObject;
            var isimpleObjRef = obj.As(new Guid(2933667680u, 50922, 22718, 184, 164, 140, 64, 124, 50, 106, 202));
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, out IntPtr, int>**)isimpleObjRef.ThisPtr)[7](isimpleObjRef.ThisPtr, out var __retval));
            var x = MarshalString.FromAbi(__retval);
        }

        public unsafe void TestFastAbi()
        {
            var simpleObjRef = ActivationFactory<test_component_fast.Simple>.ActivateInstance<IUnknownVftbl>();
            //var isimpleObjRef = simpleObjRef.As(new Guid(3524833624u, 45974, 22850, 165, 252, 100, 16, 68, 200, 237, 121));
            var isimpleObjRef = simpleObjRef.As(new Guid(1159756813u, 34571, 24076, 162, 100, 42, 197, 9, 2, 210, 77));
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, out IntPtr, int>**)isimpleObjRef.ThisPtr)[7](isimpleObjRef.ThisPtr, out var __retval));
            var x = MarshalString.FromAbi(__retval);
        }

        public void TestSimpleNonFast()
        {
            var x = new test_component_fast.Simple().Method1();
            var x2 = new test_component_fast.Simple().Method2();
            var x3 = new test_component_fast.Simple().Method3();
        }

        public unsafe void Main()
        {
            TestSimpleNonFast();
        }
    }
}
#endif