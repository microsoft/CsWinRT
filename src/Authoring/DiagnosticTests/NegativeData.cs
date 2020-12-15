namespace DiagnosticTests
{
    public partial class TestDiagnostics
    {
        // namespace tests -- WIP
        private const string _NamespaceTest1 = @"
namespace Test
{
    namespace OtherNamespace_Valid
    {
        public sealed class Class1
        {
            int x;
            public Class1(int a) { x = a; }
        }
    }

    // WME1068
    public sealed class TestDiagnostics
    {
        bool b;
        public TestDiagnostics(bool x) { b = x; }
    }
}
}";
        private const string _NamespaceTest2 = @"
namespace OtherNamespace
{

// WME1044 ?
    public sealed class Class1
    {
        int x;

        public Class1(int a)
        {
            x = a;
        }
    }
}";
        private const string _NamespaceTest3 = @"
namespace Test
{ 
// WME1067 ??
    namespace InnerNamespace
    {
        public sealed class Class1
        {
            int x;
            public Class1(int a) { x = a; }
        }
    }
}";
        // multidim array
        private const string MultiDim_2DProp = @"
namespace Test
{
    public sealed class MultiDim_2DProp
    {
        public int[,] Arr_2d { get; set; }
        private int[,] PrivArr_2d { get; set; }
    }
}";
        private const string MultiDim_3DProp = @"
namespace Test
{
    public sealed class MultiDim_3DProp
    {
        public int[,,] Arr_3d { get; set; }
        private int[,] PrivArr_2d { get; set; } 
    }
}";
        // 2d class 
        private const string MultiDim_2D_PublicClassPublicMethod1 = @"
namespace Test
{
    public sealed class MultiDim_2D_PublicClassPublicMethod1
    {
        public int[,] D2_ReturnOnly() { return new int[4, 2]; }
    }
}";
        private const string MultiDim_2D_PublicClassPublicMethod2 = @"
namespace Test
{
    public sealed class MultiDim_2D_PublicClassPublicMethod2
    {
        public int[,] D2_ReturnAndInput1(int[,] arr) { return arr; }
    }
}";
        private const string MultiDim_2D_PublicClassPublicMethod3 = @"
namespace Test
{
    public sealed class MultiDim_2D_PublicClassPublicMethod3
    {
        public int[,] D2_ReturnAndInput2of2(bool a, int[,] arr) { return arr; }
    }
}";
        private const string MultiDim_2D_PublicClassPublicMethod4 = @"
namespace Test
{
    public sealed class MultiDim_2D_PublicClassPublicMethod4
    {
        public bool D2_NotReturnAndInput2of2(bool a, int[,] arr) { return a; }
    }
}";
        private const string MultiDim_2D_PublicClassPublicMethod5 = @"
namespace Test
{
    public sealed class MultiDim_2D_PublicClassPublicMethod5
    {
        public bool D2_NotReturnAndInput2of3(bool a, int[,] arr, bool b) { return a; }
    }
}";
        private const string MultiDim_2D_PublicClassPublicMethod6 = @"
namespace Test
{
    public sealed class MultiDim_2D_PublicClassPublicMethod6
    {
        public int[,] D2_ReturnAndInput2of3(bool a, int[,] arr, bool b) { return arr; }
    }
}";
        // 3d class  
        private const string MultiDim_3D_PublicClassPublicMethod1 = @"
namespace Test
{
    public sealed class MultiDim_3D_PublicClassPublicMethod1
    {
        public int[,,] D3_ReturnOnly() { return new int[2, 1, 3] { { { 1, 1, 1 } }, { { 2, 2, 2 } } }; }
    }
}";
        private const string MultiDim_3D_PublicClassPublicMethod2 = @"
namespace Test
{
    public sealed class MultiDim_3D_PublicClassPublicMethod2
    {
        public int[,,] D3_ReturnAndInput1(int[,,] arr) { return arr; }
    }
}";
        private const string MultiDim_3D_PublicClassPublicMethod3 = @"
namespace Test
{
    public sealed class MultiDim_3D_PublicClassPublicMethod3
    {
        public int[,,] D3_ReturnAndInput2of2(bool a, int[,,] arr) { return arr; }
    }
}";
        private const string MultiDim_3D_PublicClassPublicMethod4 = @"
namespace Test
{
    public sealed class MultiDim_3D_PublicClassPublicMethod4
    {
        public int[,,] D3_ReturnAndInput2of3(bool a, int[,,] arr, bool b) { return arr; }
    }
}";
        private const string MultiDim_3D_PublicClassPublicMethod5 = @"
namespace Test
{
    public sealed class MultiDim_3D_PublicClassPublicMethod5
    {
        public bool D3_NotReturnAndInput2of2(bool a, int[,,] arr) { return a; }
    }
}";
        private const string MultiDim_3D_PublicClassPublicMethod6 = @"
namespace Test
{
    public sealed class MultiDim_3D_PublicClassPublicMethod6
    {
        public bool D3_NotReturnAndInput2of3(bool a, int[,,] arr, bool b) { return a; }
    }
}";
        // 2d iface 
        private const string MultiDim_2D_Interface1 = @"
namespace Test
{
    public interface MultiDim_2D_Interface1
    {
        public int[,] D2_ReturnOnly();
    }
}";
        private const string MultiDim_2D_Interface2 = @"
namespace Test
{
    public interface MultiDim_2D_Interface2
    {
        public int[,] D2_ReturnAndInput1(int[,] arr);
    }
}";
        private const string MultiDim_2D_Interface3 = @"
namespace Test
{
    public interface MultiDim_2D_Interface3
    {
        public int[,] D2_ReturnAndInput2of2(bool a, int[,] arr);
    }
}";
        private const string MultiDim_2D_Interface4 = @"
namespace Test
{
    public interface MultiDim_2D_Interface4
    {
        public bool D2_NotReturnAndInput2of2(bool a, int[,] arr);
    }
}";
        private const string MultiDim_2D_Interface5 = @"
namespace Test
{
    public interface MultiDim_2D_Interface5
    {
        public bool D2_NotReturnAndInput2of3(bool a, int[,] arr, bool b);
    }
}";
        private const string MultiDim_2D_Interface6 = @"
namespace Test
{
    public interface MultiDim_2D_Interface6
    {
        public int[,] D2_ReturnAndInput2of3(bool a, int[,] arr, bool b);
    }
}";
        // 3d iface 
        private const string MultiDim_3D_Interface1 = @"
namespace Test
{
    public interface MultiDim_3D_Interface1
    {
        public int[,,] D3_ReturnOnly(); 
    }
}";
        private const string MultiDim_3D_Interface2 = @"
namespace Test
{
    public interface MultiDim_3D_Interface2
    {
        public int[,,] D3_ReturnAndInput1(int[,,] arr); 
    }
}";
        private const string MultiDim_3D_Interface3 = @"
namespace Test
{
    public interface MultiDim_3D_Interface3
    {
        public int[,,] D3_ReturnAndInput2of2(bool a, int[,,] arr);
    }
}";
        private const string MultiDim_3D_Interface4 = @"
namespace Test
{
    public interface MultiDim_3D_Interface4
    {
        public int[,,] D3_ReturnAndInput2of3(bool a, int[,,] arr, bool b);
    }
}";
        private const string MultiDim_3D_Interface5 = @"
namespace Test
{
    public interface MultiDim_3D_Interface5
    {
        public bool D3_NotReturnAndInput2of2(bool a, int[,,] arr);
    }
}";
        private const string MultiDim_3D_Interface6 = @"
namespace Test
{
public interface MultiDim_3D_Interface6
    {
        public bool D3_NotReturnAndInput2of3(bool a, int[,,] arr, bool b); 
    }

}";
        // subnamespace 2d iface
        private const string SubNamespaceInterface_D2Method1 = @"
namespace Test
{
    namespace SubNamespace
    {
        public interface SubNamespaceInterface_D2Methods
        {
            public int[,] D2_ReturnOnly();
        }
    }
}";
        private const string SubNamespaceInterface_D2Method2 = @"
namespace Test
{
    namespace SubNamespace
    { 
        public interface SubNamespaceInterface_D2Methods
        { 
            public int[,] D2_ReturnAndInput1(int[,] arr); 
        }
    }
}";
        private const string SubNamespaceInterface_D2Method3 = @"
namespace Test
{
    namespace SubNamespace
    { 
        public interface SubNamespaceInterface_D2Methods
        { 
            public int[,] D2_ReturnAndInput2of2(bool a, int[,] arr);
        }
    }
}";
        private const string SubNamespaceInterface_D2Method4 = @"
namespace Test
{
    namespace SubNamespace
    { 
        public interface SubNamespaceInterface_D2Methods
        { 
            public int[,] D2_ReturnAndInput2of3(bool a, int[,] arr, bool b);
        }
    }
}";
        private const string SubNamespaceInterface_D2Method5 = @"
namespace Test
{
    namespace SubNamespace
    { 
        public interface SubNamespaceInterface_D2Methods
        { 
            public bool D2_NotReturnAndInput2of2(bool a, int[,] arr);
        }
    }
}";
        private const string SubNamespaceInterface_D2Method6 = @"
namespace Test
{
    namespace SubNamespace 
    { 
        public interface SubNamespaceInterface_D2Methods 
        { 
            public bool D2_NotReturnAndInput2of3(bool a, int[,] arr, bool b); 
        } 
    }
}";
        // subnamespace 3d iface 
        private const string SubNamespaceInterface_D3Method1 = @"
namespace Test
{
    namespace SubNamespace 
    { 
        public interface SubNamespaceInterface_D3Method1
        { 
            public int[,,] D3_ReturnOnly(); 
        } 
    }
}";
        private const string SubNamespaceInterface_D3Method2 = @"
namespace Test
{
    namespace SubNamespace 
    { 
        public interface SubNamespaceInterface_D3Method2
        { 
            public int[,,] D3_ReturnAndInput1(int[,,] arr); 
        } 
    }
}";
        private const string SubNamespaceInterface_D3Method3 = @"
namespace Test
{
    namespace SubNamespace 
    { 
        public interface SubNamespaceInterface_D3Method3
        { 
            public int[,,] D3_ReturnAndInput2of2(bool a, int[,,] arr);
        } 
    }
}";
        private const string SubNamespaceInterface_D3Method4 = @"
namespace Test
{
    namespace SubNamespace 
    { 
        public interface SubNamespaceInterface_D3Method4
        { 
            public int[,,] D3_ReturnAndInput2of3(bool a, int[,,] arr, bool b);
        } 
    }
}";
        private const string SubNamespaceInterface_D3Method5 = @"
namespace Test
{
    namespace SubNamespace 
    { 
        public interface SubNamespaceInterface_D3Method5
        { 
            public bool D3_NotReturnAndInput2of2(bool a, int[,,] arr);
        } 
    }
}";
        private const string SubNamespaceInterface_D3Method6 = @"
namespace Test
{
    namespace SubNamespace 
    { 
        public interface SubNamespaceInterface_D3Method6
        { 
            public bool D3_NotReturnAndInput2of3(bool a, int[,,] arr, bool b); 
        } 
    }
}";
        // system array
        private const string ArrayInstanceProperty1 = @"
namespace Test
{
    // this might be valid...
    public sealed class ArrayInstanceProperty1
    {
        public int[] Arr
        {
            get { return (int[])Array.CreateInstance(typeof(int), new int[] { 4 }, new int[] { 1 }); }
        }
    } 
}";
        private const string ArrayInstanceProperty2 = @"
namespace Test
{
public sealed class ArrayInstanceProperty2
    {
        public System.Array Arr
        {
            get { return Array.CreateInstance(typeof(int), new int[] { 4 }, new int[] { 1 }); }
        }
    } 
}";
        private const string ArrayInstanceProperty3 = @"
namespace Test
{
    // this might be valid...
    public sealed class ArrayInstanceProperty3
    {
        public int[] Arr
        {
            get { return (int[])Array.CreateInstance(typeof(int), new int[] { 4 }); }
        }
    } 

}";
        private const string ArrayInstanceProperty4 = @"
namespace Test
{
public sealed class ArrayInstanceProperty4
    {
        public System.Array Arr
        {
            get { return Array.CreateInstance(typeof(int), new int[] { 4 }); }
        }
    }
}";
        private const string ArrayInstanceInterface1 = @"
namespace Test
{
 public interface ArrayInstanceInterface1
    {
        System.Array Id(System.Array arr);
    }
}";
        private const string ArrayInstanceInterface2 = @"
namespace Test
{
    public interface ArrayInstanceInterface2
    {
        void Method2(System.Array arr);
    }
}";
        private const string ArrayInstanceInterface3 = @"
namespace Test
{
    public interface ArrayInstanceInterface3
    {
        System.Array Method3();
    }
}";
        private const string SystemArrayProperty5 = @"
namespace Test
{
    public sealed class SystemArrayProperty
    {
       public System.Array Arr { get; set; }
    }
}";
        private const string SystemArrayJustReturn = @"
namespace Test
{
    public sealed class JustReturn 
    {
       public System.Array SystemArrayMethod() { return Array.CreateInstance(typeof(int), new int[] { 4 }); }
    }
}";
        private const string SystemArrayUnaryAndReturn = @"
namespace Test
{
    public sealed class UnaryAndReturn
    {
       public System.Array SystemArrayMethod(System.Array arr) { return arr; }
    }
}";
        private const string SystemArraySecondArgClass = @"
namespace Test
{
   public sealed class SecondArgClass
    {
       public bool SystemArrayMethod(bool a, System.Array arr) { return a; }
    }
}";
        private const string SystemArraySecondArg2Class = @"
namespace Test
{
public sealed class SecondArg2Class
    {
       public bool SystemArrayMethod(bool a, System.Array arr, bool b) { return a; }
    }
}";
        private const string SystemArraySecondArgAndReturnTypeClass = @"
namespace Test
{
    public sealed class SecondArgAndReturnType
    {
       public System.Array SystemArrayMethod(bool a, System.Array arr) { return arr; }
    }
}";
        private const string SystemArraySecondArgAndReturnTypeClass2 = @"
namespace Test
{
    public sealed class SecondArgAndReturnTypeClass2
    {
       public System.Array SystemArrayMethod(bool a, System.Array arr, bool b) { return arr; }
    }
}";
        private const string SystemArrayNilArgsButReturnTypeInterface = @"
namespace Test
{
public interface NilArgsButReturnTypeInterface
    {
       public System.Array SystemArrayMethod();
    }
}";
        private const string SystemArrayUnaryAndReturnTypeInterface = @"
namespace Test
{
public interface UnaryAndReturnTypeInterface
    {
       public System.Array SystemArrayMethod(System.Array arr);
    }
}";
        private const string SystemArraySecondArgAndReturnTypeInterface = @"
namespace Test
{
public interface SecondArgAndReturnTypeInterface
    {
       public System.Array SystemArrayMethod(bool a, System.Array arr);
    }
}";
        private const string SystemArraySecondArgAndReturnTypeInterface2 = @"
namespace Test
{
 public interface SecondArgAndReturnTypeInterface2
    {
       public System.Array SystemArrayMetho(bool a, System.Array arr, bool b);
    }
}";
        private const string SystemArraySecondArgInterface = @"
namespace Test
{
 public interface SecondArgInterface
    {
       public bool SystemArrayMethod(bool a, System.Array arr);
    }
}";
        private const string SystemArraySecondArgInterface2 = @"
namespace Test
{
public interface SecondArgInterface2
    {
       public bool SystemArrayMethod(bool a, System.Array arr, bool b);
    }
}";
        private const string SystemArraySubNamespace_ReturnOnly = @"
namespace Test
{
    namespace SubNamespace
{
public interface SubNamespace_ReturnOnly
        {
           public System.Array SystemArrayMethod();
        } 
}
}";
        private const string SystemArraySubNamespace_ReturnAndInput1 = @"
namespace Test
{
    namespace SubNamespace
{
public interface SubNamespace_ReturnAndInput1
        {
           public System.Array SystemArrayMethod(System.Array arr);
        }
}
}";
        private const string SystemArraySubNamespace_ReturnAndInput2of2 = @"
namespace Test
{
    namespace SubNamespace
{
public interface SubNamespace_ReturnAndInput2of2
        {
           public System.Array SystemArrayMethod(bool a, System.Array arr);
        }
}
}";
        private const string SystemArraySubNamespace_ReturnAndInput2of3 = @"
namespace Test
{
    namespace SubNamespace
{
public interface SubNamespace_ReturnAndInput2of3
        {
           public System.Array SystemArrayMethod(bool a, System.Array arr, bool b);
        } 
}
}";
        private const string SystemArraySubNamespace_NotReturnAndInput2of2 = @"
namespace Test
{
    namespace SubNamespace
{
 public interface SubNamespace_NotReturnAndInput2of2
        {
           public bool SystemArrayMethod(bool a, System.Array arr);
        } 
}
}";
        private const string SystemArraySubNamespace_NotReturnAndInput2of3 = @"
namespace Test
{
    namespace SubNamespace
{
public interface SubNamespace_NotReturnAndInput2of3
        {
           public bool SystemArrayMethod(bool a, System.Array arr, bool b);
        } 
}
}";
        // struct 
        private const string StructWithByteField = @"
namespace Test
{
public struct StructWithByteField_Valid
    {
        public byte b;
    }
}";
        private const string StructWithConstructor = @"
 namespace Test
{
   public struct StructWithConstructor_Invalid
    {
        int X;
        StructWithConstructor_Invalid(int x)
        {
            X = x;
        }
    }
} ";
        private const string StructWithClassField = @"
namespace Test 
{
        public sealed class SillyClass
        {
            public double Identity(double d)
            {
                return d;
            }

            public SillyClass() { }
        }

        public struct StructWithClass_Invalid
        {
            public SillyClass classField;
        }
}";
        private const string StructWithDelegateField = @"
namespace Test {
public struct StructWithDelegate_Invalid
    {
        public delegate int ADelegate(int x);
    }
}";
        private const string StructWithPrimitiveTypesMissingPublicKeyword = @"
namespace Test
{
    public struct StructWithAllValidFields
    {
        bool boolean;
        char character;
        decimal dec;
        double dbl;
        float flt;
        int i;
        uint nat;
        long lng;
        ulong ulng;
        short sh;
        ushort us;
        string str;
    }
}";
        // constructor of same arity 
        private const string ConstructorsOfSameArity = @"
namespace TestNamespace
{
public sealed class SameArityConstructors
{
    private int num;
    private string word;

    public SameArityConstructors(int i)
    {
        num = i;
        word = ""dog"";
    }
      
    public SameArityConstructors(string s)
    {
        num = 38;
        word = s;
    } 
}
}";
        // async interfaces  
        private const string ImplementsIAsyncOperationWithProgress = @"
using Windows.Foundation;
using System;
namespace TestNamespace
{
   public sealed class OpWithProgress : IAsyncOperationWithProgress<int, bool>
    {
        AsyncOperationProgressHandler<int, bool> IAsyncOperationWithProgress<int, bool>.Progress { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        AsyncOperationWithProgressCompletedHandler<int, bool> IAsyncOperationWithProgress<int, bool>.Completed { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        Exception IAsyncInfo.ErrorCode => throw new NotImplementedException();

        uint IAsyncInfo.Id => throw new NotImplementedException();

        AsyncStatus IAsyncInfo.Status => throw new NotImplementedException();

        void IAsyncInfo.Cancel()
        {
            throw new NotImplementedException();
        }

        void IAsyncInfo.Close()
        {
            throw new NotImplementedException();
        }

        int IAsyncOperationWithProgress<int, bool>.GetResults()
        {
            throw new NotImplementedException();
        }
    } 
}";
        private const string ImplementsIAsyncActionWithProgress = @"
using Windows.Foundation;
using System;
namespace TestNamespace
{
public class ActionWithProgress : IAsyncActionWithProgress<int>
    {
        AsyncActionProgressHandler<int> IAsyncActionWithProgress<int>.Progress { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        AsyncActionWithProgressCompletedHandler<int> IAsyncActionWithProgress<int>.Completed { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        Exception IAsyncInfo.ErrorCode => throw new NotImplementedException();

        uint IAsyncInfo.Id => throw new NotImplementedException();

        AsyncStatus IAsyncInfo.Status => throw new NotImplementedException();

        void IAsyncInfo.Cancel()
        {
            throw new NotImplementedException();
        }

        void IAsyncInfo.Close()
        {
            throw new NotImplementedException();
        }

        void IAsyncActionWithProgress<int>.GetResults()
        {
            throw new NotImplementedException();
        }
    }
}";
        private const string ImplementsIAsyncOperation = @"
using Windows.Foundation;
using System;
namespace TestNamespace
{
    public sealed class Op : IAsyncOperation<int>
    {
        AsyncOperationCompletedHandler<int> IAsyncOperation<int>.Completed { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        Exception IAsyncInfo.ErrorCode => throw new NotImplementedException();

        uint IAsyncInfo.Id => throw new NotImplementedException();

        AsyncStatus IAsyncInfo.Status => throw new NotImplementedException();

        void IAsyncInfo.Cancel()
        {
            throw new NotImplementedException();
        }

        void IAsyncInfo.Close()
        {
            throw new NotImplementedException();
        }

        int IAsyncOperation<int>.GetResults()
        {
            throw new NotImplementedException();
        }
    } 
}";
        private const string ImplementsIAsyncAction = @"
using Windows.Foundation;
using System;
namespace TestNamespace
{
   public sealed class AsyAction : IAsyncAction
    {
        public AsyncActionCompletedHandler Completed { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public Exception ErrorCode => throw new NotImplementedException();

        public uint Id => throw new NotImplementedException();

        public AsyncStatus Status => throw new NotImplementedException();

        AsyncActionProgressHandler<int> IAsyncActionWithProgress<int>.Progress { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        AsyncActionWithProgressCompletedHandler<int> IAsyncActionWithProgress<int>.Completed { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        Exception IAsyncInfo.ErrorCode => throw new NotImplementedException();

        uint IAsyncInfo.Id => throw new NotImplementedException();

        AsyncStatus IAsyncInfo.Status => throw new NotImplementedException();

        public void Cancel()
        {
            throw new NotImplementedException();
        }

        public void Close()
        {
            throw new NotImplementedException();
        }

        public void GetResults()
        {
            throw new NotImplementedException();
        }
    } 
}";
        // readonlyarray / writeonlyarray attribute
        private const string TestArrayParamAttrUnary_1 = @"
public sealed class OnlyParam
{
        public void BothAttributes_Separate([System.Runtime.InteropServices.WindowsRuntime.WriteOnlyArray][System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] arr) { }
}";
        private const string TestArrayParamAttrUnary_2 = @"
public sealed class OnlyParam
{
        public void BothAttributes_Together([System.Runtime.InteropServices.WindowsRuntime.WriteOnlyArray, System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] arr) { }
}";
        private const string TestArrayParamAttrUnary_3 = @"
public sealed class OnlyParam
{
        public void MarkedOutAndReadOnly([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] out int[] arr) { arr = new int[] { }; }
}";
        private const string TestArrayParamAttrUnary_4 = @"
public sealed class OnlyParam
{
        public void ArrayMarkedIn([In] int[] arr) { }
}";
        private const string TestArrayParamAttrUnary_5 = @"
public sealed class OnlyParam
{
        public void ArrayMarkedOut([Out] int[] arr) { }
}";
        private const string TestArrayParamAttrUnary_6 = @"
public sealed class OnlyParam
{
        public void NonArrayMarkedReadOnly([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int arr) { }
}";
        private const string TestArrayParamAttrUnary_7 = @"
public sealed class OnlyParam
{
        public void NonArrayMarkedWriteOnly([System.Runtime.InteropServices.WindowsRuntime.WriteOnlyArray] int arr) { }
}";
        private const string TestArrayParamAttrUnary_8 = @"
public sealed class OnlyParam
{
        public void ParamMarkedIn([In] int arr) { }
}";
        private const string TestArrayParamAttrUnary_9 = @"
public sealed class OnlyParam
{
        public void ParamMarkedOut([Out] int arr) { }
}";
        private const string TestArrayParamAttrUnary_10 = @"
public sealed class OnlyParam
{
        public void ArrayNotMarked(int[] arr) { }
}";
        private const string TestArrayParamAttrBinary_1 = @"
public sealed class TwoParam
{
        public void BothAttributes_Separate(int i, [System.Runtime.InteropServices.WindowsRuntime.WriteOnlyArray][System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] arr) { }
}";
        private const string TestArrayParamAttrBinary_2 = @"
public sealed class TwoParam
{
        public void BothAttributes_Together(int i, [System.Runtime.InteropServices.WindowsRuntime.WriteOnlyArray, System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] arr) { }
}";
        private const string TestArrayParamAttrBinary_3 = @"
public sealed class TwoParam
{
        public void MarkedOutAndReadOnly(int i, [System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] out int[] arr) { arr = new int[] { }; }
}";
        private const string TestArrayParamAttrBinary_4 = @"
public sealed class TwoParam
{
        public void ArrayMarkedIn(int i, [In] int[] arr) { }
}";
        private const string TestArrayParamAttrBinary_5 = @"
public sealed class TwoParam
{
        public void ArrayMarkedOut(int i, [Out] int[] arr) { }
}";
        private const string TestArrayParamAttrBinary_6 = @"
public sealed class TwoParam
{
        public void NonArrayMarkedReadOnly(int i, [System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int arr) { }
}";
        private const string TestArrayParamAttrBinary_7 = @"
public sealed class TwoParam
{
        public void NonArrayMarkedWriteOnly(int i, [System.Runtime.InteropServices.WindowsRuntime.WriteOnlyArray] int arr) { }
}";
        private const string TestArrayParamAttrBinary_8 = @"
public sealed class TwoParam
{
        public void ParamMarkedIn(int i, [In] int arr) { }
}";
        private const string TestArrayParamAttrBinary_9 = @"
public sealed class TwoParam
{
        public void ParamMarkedOut(int i, [Out] int arr) { }
}";
        private const string TestArrayParamAttrBinary_10 = @"
public sealed class TwoParam
{
        public void ArrayNotMarked(int i, int[] arr) { }
}";
        private const string TestArrayParamAttrBinary_11 = @"
public sealed class TwoArray
{
        public void OneValidOneInvalid_1(
[System.Runtime.InteropServices.WindowsRuntime.WriteOnlyArray] int[] xs, 
[System.Runtime.InteropServices.WindowsRuntime.WriteOnlyArray]
[System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] ys) { }
}";
        private const string TestArrayParamAttrBinary_12 = @"
public sealed class TwoArray
{
        public void OneValidOneInvalid_2(
[System.Runtime.InteropServices.WindowsRuntime.WriteOnlyArray]
[System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] xs, 
[System.Runtime.InteropServices.WindowsRuntime.WriteOnlyArray] int[] ys) { }
}";
        private const string TestArrayParamAttrBinary_13 = @"
public sealed class TwoArray
{
        public void MarkedOutAndReadOnly([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] xs, [System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] out int[] arr) { arr = new int[] { }; }
}";
        private const string TestArrayParamAttrBinary_14 = @"
public sealed class TwoParam
{
        public void ArrayMarkedIn(int i, [In] int[] arr) { }
}";
        private const string TestArrayParamAttrBinary_15 = @"
public sealed class TwoArray
{
        public void ArrayMarkedIn2([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] xs, [In] int[] arr) { }
}";
        private const string TestArrayParamAttrBinary_16 = @"
public sealed class TwoArray
{
        public void ArrayMarkedOut([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] xs, [Out] int[] arr) { }
}";
        private const string TestArrayParamAttrBinary_17 = @"
public sealed class TwoArray
{
        public void ArrayNotMarked(int i, int[] arr) { }
}";
        private const string TestArrayParamAttrBinary_18 = @"
public sealed class TwoArray
{
        public void NonArrayMarkedReadOnly([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] xs, [System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int i) { }
}";
        private const string TestArrayParamAttrBinary_19 = @"
public sealed class TwoArray
{
        public void NonArrayMarkedWriteOnly([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] xs, [System.Runtime.InteropServices.WindowsRuntime.WriteOnlyArray] int i) { }
}";
        private const string TestArrayParamAttrBinary_20 = @"
public sealed class TwoArray
{
        public void NonArrayMarkedWriteOnly2([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int i, [System.Runtime.InteropServices.WindowsRuntime.WriteOnlyArray] int[] arr) { }
}";
        private const string TestArrayParamAttrBinary_21 = @"
public sealed class TwoArray
{
        public void ParamMarkedIn([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] xs, [In] int arr) { }
}";
        private const string TestArrayParamAttrBinary_22 = @"
public sealed class TwoArray
{
        public void ParamMarkedOut([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] xs, [Out] int arr) { }
}";
        private const string TestArrayParamAttrBinary_23 = @"
public sealed class TwoArray
{
        public void ParamMarkedOut2([Out] int arr, [System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] xs) { }
}";
        private const string TestArrayParamAttrBinary_24 = @"
public sealed class TwoArray
{
        public void ArrayNotMarked([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] xs, int[] arr) { }
}";
        // ref param 
        private const string RefParam_InterfaceMethod = @"
public interface IHaveAMethodWithRefParam
    {
        void foo(ref int i);
    }
";
        private const string RefParam_ClassMethod = @"
public sealed class ClassWithMethodUsingRefParam
    {
        public void MethodWithRefParam(ref int i) { i++; }
    }
";
        // operator overload 
        private const string OperatorOverload_Class = @"
    public sealed class ClassThatOverloadsOperator
    {
        public static ClassThatOverloadsOperator operator +(ClassThatOverloadsOperator thing)
        {
            return thing;
        }
    }";
        // param name conflict 
        private const string DunderRetValParam = @"
public sealed class ParameterNamedDunderRetVal
    {
        public int Identity(int __retval)
        {
            return __retval;
        }
    }
";
        // struct fields 
        private const string StructWithIndexer = @"
namespace Test
{
public struct StructWithIndexer_Invalid
    {
        int[] arr;
        int this[int i] => arr[i];
    }
}";
        private const string StructWithMethods = @"
namespace Test
{
public struct StructWithMethods_Invalid
    {
        int foo(int x)
        {
            return x;
        }
    }
}";
        private const string StructWithConst = @"
namespace Test
{
    public struct StructWithConst_Invalid 
    {
        const int five = 5;
        private int six;
    }
}";
        private const string StructWithProperty = @"
namespace Test
{
    public enum BasicEnum
    { 
        First = 0,
        Second = 1
    }

    public struct Posn_Invalid 
    {
        BasicEnum enumField; 

        public int x { get; }
        public int y { get; }
    }
}";
        private const string StructWithPrivateField = @"
namespace Test
{
public struct StructWithPrivateField_Invalid
    {
        const int ci = 5;
        private int x;
    }
}";
        private const string StructWithObjectField = @"
namespace Test
{
public struct StructWithObjectField_Invalid
    {
        public object obj;
    }
}";
        private const string StructWithDynamicField = @"
namespace Test
{
public struct StructWithDynamicField_Invalid 
    {
        public dynamic dyn;
    }
}";
        // DefaultOverload attribute tests
        private const string TwoOverloads_NoAttribute = @"
namespace Test
{
    public sealed class TwoOverloads_NoAttribute
    {
        public string OverloadExample(string s) { return s; }

        public int OverloadExample(int n) { return n; }
    }
}";
        private const string TwoOverloads_TwoAttribute_OneInList = @"
namespace Test
{
    public sealed class TwoOverloads_TwoAttribute_OneInList
    {

        [Windows.Foundation.Metadata.Deprecated(""hu"", Windows.Foundation.Metadata.DeprecationType.Deprecate, 1), 
         Windows.Foundation.Metadata.DefaultOverload()]
        public string OverloadExample(string s) { return s; } 

        [Windows.Foundation.Metadata.DefaultOverload()]
        public int OverloadExample(int n) { return n; }
    }
}";
        private const string TwoOverloads_NoAttribute_OneIrrevAttr = @"
namespace Test
{
    public sealed class TwoOverloads_NoAttribute_OneIrrevAttr
    {
        [Windows.Foundation.Metadata.Deprecated(""hu"", Windows.Foundation.Metadata.DeprecationType.Deprecate, 1)]
        public string OverloadExample(string s) { return s; }

        public int OverloadExample(int n) { return n; }
    }
}";
        private const string TwoOverloads_TwoAttribute_BothInList = @"
namespace Test
{
    public sealed class TwoOverloads_TwoAttribute_BothInList
    {

        [Windows.Foundation.Metadata.Deprecated(""hu"", Windows.Foundation.Metadata.DeprecationType.Deprecate, 1), 
         Windows.Foundation.Metadata.DefaultOverload()]
        public string OverloadExample(string s) { return s; }

        [Windows.Foundation.Metadata.Deprecated(""hu"", Windows.Foundation.Metadata.DeprecationType.Deprecate, 1), 
         Windows.Foundation.Metadata.DefaultOverload()]
        public int OverloadExample(int n) { return n; }
    }
}";
        private const string TwoOverloads_TwoAttribute_TwoLists = @"
namespace Test
{
    public sealed class TwoOverloads_TwoAttribute_TwoLists
    {

        [Windows.Foundation.Metadata.Deprecated(""hu"", Windows.Foundation.Metadata.DeprecationType.Deprecate, 1)]
        [Windows.Foundation.Metadata.DefaultOverload()]
        public string OverloadExample(string s) { return s; } 

        [Windows.Foundation.Metadata.DefaultOverload()]
        public int OverloadExample(int n) { return n; }
    }
}";
        private const string TwoOverloads_TwoAttribute_OneInSeparateList_OneNot = @"
namespace Test
{
    public sealed class TwoOverloads_TwoAttribute_OneInSeparateList_OneNot
    {

        [Windows.Foundation.Metadata.Deprecated(""hu"", Windows.Foundation.Metadata.DeprecationType.Deprecate, 1)]
        [Windows.Foundation.Metadata.DefaultOverload()]
        public string OverloadExample(string s) { return s; }

        [Windows.Foundation.Metadata.Deprecated(""hu"", Windows.Foundation.Metadata.DeprecationType.Deprecate, 1), 
         Windows.Foundation.Metadata.DefaultOverload()]
        public int OverloadExample(int n) { return n; }
    }
}";
        private const string TwoOverloads_TwoAttribute_BothInSeparateList = @"
namespace Test
{
    public sealed class TwoOverloads_TwoAttribute_BothInSeparateList
    {

        [Windows.Foundation.Metadata.Deprecated(""hu"", Windows.Foundation.Metadata.DeprecationType.Deprecate, 1)]
        [Windows.Foundation.Metadata.DefaultOverload()]
        public string OverloadExample(string s) { return s; }

        [Windows.Foundation.Metadata.Deprecated(""hu"", Windows.Foundation.Metadata.DeprecationType.Deprecate, 1)]
        [Windows.Foundation.Metadata.DefaultOverload()]
        public int OverloadExample(int n) { return n; }
    }
}";
        private const string TwoOverloads_TwoAttribute = @"
namespace Test
{
    public sealed class TwoOverloads_TwoAttribute
    {

        [Windows.Foundation.Metadata.DefaultOverload()]
        public string OverloadExample(string s) { return s; }

        [Windows.Foundation.Metadata.DefaultOverload()]
        public int OverloadExample(int n) { return n; }
    }
}";
        private const string ThreeOverloads_TwoAttributes = @"
namespace Test
{
    public sealed class ThreeOverloads_TwoAttributes
    {
        public string OverloadExample(string s) { return s; }

        [Windows.Foundation.Metadata.DefaultOverload()]
        public int OverloadExample(int n) { return n; }

        [Windows.Foundation.Metadata.DefaultOverload()]
        public bool OverloadExample(bool b) { return b; }
    }
}";
        // jagged 2d/3d prop
        private const string Jagged2D_Property2 = @"
namespace Test
{
    public sealed class Jagged2D_Property2
    {
        public int[][] Arr { get; set; }
    }
}";
        private const string Jagged3D_Property1 = @"
namespace Test
{
    public sealed class Jagged3D_Property1
    {
        public int[][][] Arr3 { get; set; }
    }
}";
        // jagged 2d class method 
        private const string Jagged2D_ClassMethod1 = @"
namespace Test
{
    public sealed class Jagged2D_ClassMethod1
    {
        public int[][] J2_ReturnOnly() 
        {
            int[][] arr = new int[2][];
            arr[0] = new int[1] { 1 };
            arr[1] = new int[1] { 2 };
            return arr;
        }
        
    }
}";
        private const string Jagged2D_ClassMethod2 = @"
namespace Test
{
    public sealed class Jagged2D_ClassMethod2
    {
        public int[][] J2_ReturnAndInput1(int[][] arr) { return arr; }
    }
}";
        private const string Jagged2D_ClassMethod3 = @"
namespace Test
{
    public sealed class Jagged2D_ClassMethod3
    {
        public int[][] J2_ReturnAndInput2of2(bool a, int[][] arr) { return arr; }
    }
}";
        private const string Jagged2D_ClassMethod4 = @"
namespace Test
{
    public sealed class Jagged2D_ClassMethod4
    {
        public bool J2_NotReturnAndInput2of2(bool a, int[][] arr) { return a; }
    }
}";
        private const string Jagged2D_ClassMethod5 = @"
namespace Test
{
    public sealed class Jagged2D_ClassMethod5
    {
        public bool J2_NotReturnAndInput2of3(bool a, int[][] arr, bool b) { return a; }
    }
}";
        private const string Jagged2D_ClassMethod6 = @"
namespace Test
{
    public sealed class Jagged2D_ClassMethod6
    {
        public int[][] J2_ReturnAndInput2of3(bool a, int[][] arr, bool b) { return arr; }
    }
}";
        // jagged 3d class method
        private const string Jagged3D_ClassMethod1 = @"
namespace Test
{
    public sealed class Jagged3D_ClassMethod1
    {
        public int[][][] J3_ReturnOnly() 
        {
            int[][] arr2 = new int[2][];
            arr2[0] = new int[1] { 1 };
            arr2[1] = new int[1] { 2 };

            int[][][] arr = new int[1][][];
            arr[0] = arr2;
            return arr; 
        }
        
    }
}";
        private const string Jagged3D_ClassMethod2 = @"
namespace Test
{
    public sealed class Jagged3D_ClassMethod1
    {
        public int[][][] J3_ReturnAndInput1(int[][][] arr) { return arr; }
    }
}";
        private const string Jagged3D_ClassMethod3 = @"
namespace Test
{
    public sealed class Jagged3D_ClassMethod3
    {
        public int[][][] J3_ReturnAndInput2of2(bool a, int[][][] arr) { return arr; }
    }
}";
        private const string Jagged3D_ClassMethod4 = @"
namespace Test
{
    public sealed class Jagged3D_ClassMethod4
    {
        public int[][][] J3_ReturnAndInput2of3(bool a, int[][][] arr, bool b) { return arr; }
    }
}";
        private const string Jagged3D_ClassMethod5 = @"
namespace Test
{
    public sealed class Jagged3D_ClassMethod5
    {
        public bool J3_NotReturnAndInput2of2(bool a, int[][][] arr) { return a; }
    }
}";
        private const string Jagged3D_ClassMethod6 = @"
namespace Test
{
    public sealed class Jagged3D_ClassMethod6
    {
        public bool J3_NotReturnAndInput2of3(bool a, int[][][] arr, bool b) { return a; }
    }
}";
        // jagged 2d interface method
        private const string Jagged2D_InterfaceMethod1 = @"
namespace Test
{
    public interface Jagged2D_InterfaceMethod1
    {
        public int[][] J2_ReturnOnly();
    }
}";
        private const string Jagged2D_InterfaceMethod2 = @"
namespace Test
{
    public interface Jagged2D_InterfaceMethod2
    {
        public int[][] J2_ReturnAndInput1(int[,] arr);
    }
}";
        private const string Jagged2D_InterfaceMethod3 = @"
namespace Test
{
    public interface Jagged2D_InterfaceMethod3
    {
        public int[][] J2_ReturnAndInput2of2(bool a, int[][] arr);
    }
}";
        private const string Jagged2D_InterfaceMethod4 = @"
namespace Test
{
    public interface Jagged2D_InterfaceMethod4
    {
        public bool J2_NotReturnAndInput2of2(bool a, int[][] arr);
    }
}";
        private const string Jagged2D_InterfaceMethod5 = @"
namespace Test
{
    public interface Jagged2D_InterfaceMethod5
    {
        public bool J2_NotReturnAndInput2of3(bool a, int[][] arr, bool b);
    }
}";
        private const string Jagged2D_InterfaceMethod6 = @"
namespace Test
{
    public interface Jagged2D_InterfaceMethod6
    {
        public int[][] J2_ReturnAndInput2of3(bool a, int[][] arr, bool b);
    }
}";
        // jagged 2d interface method
        private const string Jagged3D_InterfaceMethod1 = @"
namespace Test
{
    public interface Jagged3D_InterfaceMethod1
    {
        public int[][][] J3_ReturnOnly();
    }
}";
        private const string Jagged3D_InterfaceMethod2 = @"
namespace Test
{
    public interface Jagged3D_InterfaceMethod2
    {
        public int[][][] J3_ReturnAndInput1(int[][][] arr);
    }
}";
        private const string Jagged3D_InterfaceMethod3 = @"
namespace Test
{
    public interface Jagged3D_InterfaceMethod3
    {
        public int[][][] J3_ReturnAndInput2of2(bool a, int[][][] arr);
    }
}";
        private const string Jagged3D_InterfaceMethod4 = @"
namespace Test
{
    public interface Jagged3D_InterfaceMethod4
    {
        public int[][][] J3_ReturnAndInput2of3(bool a, int[][][] arr, bool b);
    }
}";
        private const string Jagged3D_InterfaceMethod5 = @"
namespace Test
{
    public interface Jagged3D_InterfaceMethod5
    {
        public bool J3_NotReturnAndInput2of2(bool a, int[][][] arr);
    }
}";
        private const string Jagged3D_InterfaceMethod6 = @"
namespace Test
{
    public interface Jagged3D_InterfaceMethod6
    {
        public bool J3_NotReturnAndInput2of3(bool a, int[][][] arr, bool b);
    }
}";
        // subnamespace jagged 2d iface
        private const string SubNamespace_Jagged2DInterface1 = @"
namespace Test
{
    namespace SubNamespace
    {
        public interface SubNamespace_Jagged2DInterface1
        {
            public int[][] J2_ReturnOnly();
        }
    }
}";
        private const string SubNamespace_Jagged2DInterface2 = @"
namespace Test
{
    namespace SubNamespace
    {
        public interface SubNamespace_Jagged2DInterface2
        {
            public int[][] J2_ReturnAndInput1(int[,] arr);
        }
    }
}";
        private const string SubNamespace_Jagged2DInterface3 = @"
namespace Test
{
    namespace SubNamespace
    {
        public interface SubNamespace_Jagged2DInterface3
        {
            public int[][] J2_ReturnAndInput2of2(bool a, int[][] arr);
        }
    }
}";
        private const string SubNamespace_Jagged2DInterface4 = @"
namespace Test
{
    namespace SubNamespace
    {
        public interface SubNamespace_Jagged2DInterface4
        {
            public bool J2_NotReturnAndInput2of2(bool a, int[][] arr);
        }
    }
}";
        private const string SubNamespace_Jagged2DInterface5 = @"
namespace Test
{
    namespace SubNamespace
    {
        public interface SubNamespace_Jagged2DInterface5
        {
            public bool J2_NotReturnAndInput2of3(bool a, int[][] arr, bool b);
        }
    }
}";
        private const string SubNamespace_Jagged2DInterface6 = @"
namespace Test
{
    namespace SubNamespace
    {
        public interface SubNamespace_Jagged2DInterface6
        {
            public int[][] J2_ReturnAndInput2of3(bool a, int[][] arr, bool b);
        }
    }
}";
        // subnamespace jagged 3d iface
        private const string SubNamespace_Jagged3DInterface1 = @"
namespace Test
{
    namespace SubNamespace
    {
        public interface SubNamespace_Jagged3DInterface1
        {
            public int[][][] J3_ReturnOnly();
        }
    }
}";
        private const string SubNamespace_Jagged3DInterface2 = @"
namespace Test
{
    namespace SubNamespace
    {
        public interface SubNamespace_Jagged3DInterface2
        {
            public int[][][] J3_ReturnAndInput1(int[][][] arr);
        }
    }
}";
        private const string SubNamespace_Jagged3DInterface3 = @"
namespace Test
{
    namespace SubNamespace
    {
        public interface SubNamespace_Jagged3DInterface3
        {
            public int[][][] J3_ReturnAndInput2of2(bool a, int[][][] arr);
        }
    }
}";
        private const string SubNamespace_Jagged3DInterface4 = @"
namespace Test
{
    namespace SubNamespace
    {
        public interface SubNamespace_Jagged3DInterface4
        {
            public int[][][] J3_ReturnAndInput2of3(bool a, int[][][] arr, bool b);
        }
    }
}";
        private const string SubNamespace_Jagged3DInterface5 = @"
namespace Test
{
    namespace SubNamespace

    {
        public interface SubNamespace_Jagged3DInterface5
        {
            public bool J3_NotReturnAndInput2of2(bool a, int[][][] arr);
        }
    }
}";
        private const string SubNamespace_Jagged3DInterface6 = @"
namespace Test
{
    namespace SubNamespace
    {
        public interface SubNamespace_Jagged3DInterface6
        {
            public bool J3_NotReturnAndInput2of3(bool a, int[][][] arr, bool b);
        }
    }
}";
    }
}
