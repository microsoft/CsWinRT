namespace DiagnosticTests
{
    public partial class UnitTesting
    {
        private const string SameNameNamespacesDisjoint = @"
namespace Test
{
    public sealed class Coords
    {
        public Coords() {}
    }
}

namespace A
{
    public sealed class Dummy 
    { 
        public Dummy() {}
    }
}

namespace A
{
    public sealed class Blank { public Blank() {} }
}";


        private const string NamespaceDifferByDot = @"
namespace Test.A
{
    private Test.B.Blank _blank;
    public sealed class Dummy 
    { 
        public Dummy() {}
    }
}

namespace Test.B
{
    public sealed class Blank { public Blank() {} }
}";

        private const string NamespaceDifferByDot2 = @"
namespace Test.A
{
    private Test.Blank _blank;
    public sealed class Dummy
    {
        public Dummy() {}
    }
}

namespace Test
{
    public sealed class Blank { public Blank() {} }
}";
        private const string NamespacesDifferByCase = @"
namespace Test
{
    public sealed class Blank { public Blank() { } }

    namespace Sample
    { 
        public sealed class AnotherBlank { public AnotherBlank() { } }
    }

    namespace samplE 
    { 
        public sealed class AnotherBlank { public AnotherBlank() { } }
    }
}";

        private const string DisjointNamespaces = @"
// ""Test.winmd"" - types in namespace A won't be accessible
namespace Test 
{
    public sealed class Blank { public Blank() { } }
}

namespace A
{
    public sealed class Class4 { public Class4() { } }
}";



        private const string DisjointNamespaces2 = @"
// ""Test.winmd""  uses the other namespace
namespace Test 
{
    public sealed class Blank 
    { 
        public Blank() { } 
        public void Foo(A.B.F arg) { return; }
    }
}

namespace A
{
    public sealed class Class4 { public Class4() { } }
    namespace B
    {
        public sealed class F { public F() {} }
    }
}";

        private const string NoPublicTypes = @"
namespace Test
{
    internal sealed class RuntimeComponent
    {
        public RuntimeComponent() {}
    }
}";
        // Generic Dictionary 
        private const string InterfaceWithGenericDictReturnType = @"
using System.Collections.Generic;
namespace Test
{
    public interface MyInterface
    {
        Dictionary<int,bool> MakeDictionary(int length);
    }
}";
        private const string InterfaceWithGenericDictInput = @"

using System.Collections.Generic;
namespace Test
{
    public interface MyInterface
    {
        int ReturnInt(System.Collections.Generic.Dictionary<int,bool> ls);
    }
}";
        private const string ClassWithGenericDictReturnType = @"
using System.Collections.Generic;
namespace Test
{
    public sealed class MyClass
    {
        public System.Collections.Generic.Dictionary<int,int> ReturnsDict(int length) { return new Dictionary<int,int>(); };
    }
}";
        private const string ClassWithGenericDictInput = @"
using System.Collections.Generic;
namespace Test
{
    public sealed class MyClass
    {
        public int ReturnsInt(Dictionary<int,int> ls) { return 0; }
    }
}";
        private const string IfaceWithGenDictProp = @"
using System.Collections.Generic;
namespace Test
{
    public interface MyInterface
    {
        public Dictionary<int, int> Dict { get; set; }
    }
}";
        private const string ClassWithGenDictProp = @"
using System.Collections.Generic;
namespace Test
{
    public sealed class MyClass
    {
        public Dictionary<int,int> Dict { get; set; }
    }
}";
        // Generic ReadOnlyDictionary
        private const string InterfaceWithGenericRODictReturnType = @"
using System.Collections.ObjectModel;
namespace Test
{
    public interface MyInterface
    {
        System.Collections.ObjectModel.ReadOnlyDictionary<int,int> MakeIntList(int length);
    }
}";
        private const string InterfaceWithGenericRODictInput = @"
using System.Collections.ObjectModel;
namespace Test
{
    public interface MyInterface
    {
        int InputIsRODict(ReadOnlyDictionary<int,int> rodict);
    }
}";
        private const string ClassWithGenericRODictReturnType = @"
using System.Collections.ObjectModel;
namespace Test
{
    public sealed class MyClass
    {
        public ReadOnlyDictionary<int,int> ReturnsRODict(int length) { return new ReadOnlyDictionary<int,int>(); }
    }
}";
        private const string ClassWithGenericRODictInput = @"
using System.Collections.ObjectModel;
namespace Test
{
    public sealed class MyClass
    {
        public int ReturnsInt(ReadOnlyDictionary<int,int> ls) { return 0; }
    }
}";
        private const string IfaceWithGenRODictProp = @"
using System.Collections.ObjectModel;
namespace Test
{
    public interface MyInterface
    {
        public ReadOnlyDictionary<int,int> RODict { get; set; }
    }
}";
        private const string ClassWithGenRODictProp = @"
using System.Collections.ObjectModel;
namespace Test
{
    public sealed class MyClass
    {
        public ReadOnlyDictionary<int,int> RODict { get; set; }
    }
}";
        // NonGeneric KeyValuePair
        private const string InterfaceWithGenericKVPairReturnType = @"
using System.Collections.Generic;
namespace Test
{
    public interface MyInterface
    {
        KeyValuePair KVPair(int length);
    }
}";
        private const string InterfaceWithGenericKVPairInput = @"
using System.Collections.Generic;
namespace Test
{
    public interface MyInterface
    {
        int ReturnsInt(System.Collections.Generic.KeyValuePair kvp);
    }
}";
        private const string ClassWithGenericKVPairReturnType = @"
using System.Collections.Generic;
namespace Test
{
    public sealed class MyClass
    {
        public KeyValuePair ReturnsKVPair(int length);
    }
}";
        private const string ClassWithGenericKVPairInput = @"
using System.Collections.Generic;
namespace Test
{
    public sealed class MyClass
    {
        public int ReturnsInt(KeyValuePair ls) { return 0; }
    }
}";
        private const string IfaceWithGenKVPairProp = @"
using System.Collections.Generic;
namespace Test
{
    public interface MyInterface
    {
        public KeyValuePair KVpair { get; set; }
    }
}";
        private const string ClassWithGenKVPairProp = @"
using System.Collections.Generic;
namespace Test
{
    public sealed class MyClass
    {
        public KeyValuePair KVpair { get; set; }
    }
}";
        // Generic Enumerable 
        private const string InterfaceWithGenericEnumerableReturnType = @"
using System.Linq;
namespace Test
{
    public interface MyInterface
    {
        Enumerable MakeIntList(int length);
    }
}";
        private const string InterfaceWithGenericEnumerableInput = @"
using System.Linq;
namespace Test
{
    public interface MyInterface
    {
        int ReturnsInt(Enumerable ls);
    }
}";
        private const string ClassWithGenericEnumerableReturnType = @"
using System.Linq;
namespace Test
{
    public sealed class MyClass
    {
        public Enumerable ReturnsEnumerable(int length) { return new Enumerable(); }
    }
}";
        private const string ClassWithGenericEnumerableInput = @"
using System.Linq;
namespace Test
{
    public sealed class MyClass
    {
        public int ReturnsInt(Enumerable ls) { return 0; }
    }
}";
        private const string IfaceWithGenEnumerableProp = @"
using System.Linq;
namespace Test
{
    public interface MyInterface
    {
        public System.Linq.Enumerable Enumer { get; set; }
    }
}";
        private const string ClassWithGenEnumerableProp = @"
using System.Linq;
namespace Test
{
    public sealed class MyClass
    {
        public Enumerable Enumer { get; set; }
    }
}";
        // Generic List 
        private const string InterfaceWithGenericListReturnType = @"
using System.Collections.Generic;
namespace Test
{
    public interface MyInterface
    {
        List<int> MakeIntList(int length);
    }
}";
        private const string InterfaceWithGenericListInput = @"
using System.Collections.Generic;
namespace Test
{
    public interface MyInterface
    {
        int SizeOfIntList(List<int> ls);
    }
}";
        private const string ClassWithGenericListReturnType = @"
using System.Collections.Generic;
namespace Test
{
    public sealed class MyClass
    {
        public List<int> ReturnsIntList(int length);
    }
}";
        private const string ClassWithGenericListInput = @"
using System.Collections.Generic;
namespace Test
{
    public sealed class MyClass
    {
        public int ReturnsIntList(List<int> ls) { return 0; }
    }
}";
        private const string IfaceWithGenListProp = @"
using System.Collections.Generic;
namespace Test
{
    public interface MyInterface
    {
        public List<int> IntList { get; set; }
    }
}";
        private const string ClassWithGenListProp = @"
using System.Collections.Generic;
namespace Test
{
    public sealed class MyClass
    {
        public System.Collections.Generic.List<int> IntList { get; set; }
    }
}";
        
        private const string InterfaceWithOverloadNoAttribute = @"
namespace Test
{
    public interface MyInterface
    {
        int Foo(int n);
        int Foo(string s);
    }
}";
        private const string InterfaceWithOverloadAttributeTwice = @"
using Windows.Foundation.Metadata;
namespace Test
{
    public interface MyInterface
    {
        [DefaultOverload]
        int Foo(int n);
        [DefaultOverload]
        int Foo(string s);
    }
}";
 
        private const string StructWithInterfaceField = @"
namespace Test 
{
        public interface Foo 
        {
            int Id(int i);
        }

        public struct StructWithIface_Invalid
        {
            public Foo ifaceField;
        }
}";

        private const string UnsealedClass = @"
namespace Test 
{ 
    public class UnsealedClass 
    { 
        public UnsealedClass() {} 
    } 
}";
        private const string UnsealedClass2 = @"
namespace Test 
{ 
    public class UnsealedClass 
    { 
        private UnsealedClass() {} 
    } 
}"; 

        private const string GenericClass = @"
namespace Test 
{ 
    public sealed class GenericClass<T> 
    { 
        public UnsealedClass<T>() {} 
    } 
}";
        private const string GenericInterface = @"
namespace Test 
{ 
    public interface GenIface<T> 
    { 
        int Foo(T input); 
    }
}";
 
        private const string ClassInheritsException = @"
namespace Test 
{ 
    public sealed class ClassWithExceptions : System.Exception 
    { 
        public ClassWithExceptions() {} 
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
        private const string MultiDim_3DProp_Whitespace = @"
namespace Test
{
    public sealed class MultiDim_3DProp
    {
        public int[ , , ] Arr_3d { get; set; }
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
public sealed class ArrayInstanceProperty2
    {
        public System.Array Arr
        {
            get { return Array.CreateInstance(typeof(int), new int[] { 4 }, new int[] { 1 }); }
        }
    } 
}";
        private const string ArrayInstanceProperty2 = @"
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
        // constructor of same arity 
        private const string ConstructorsOfSameArity = @"
namespace Test
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
        private const string ClassImplementsAsyncAndException = @"
using Windows.Foundation;
namespace Test
{
    public sealed class OpWithProgress : System.Exception, IAsyncOperationWithProgress<int, bool>
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

        
        private const string ClassImplementsIAsyncOperationWithProgress = @"
using Windows.Foundation;
using System;
namespace Test
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
        private const string ClassImplementsIAsyncActionWithProgress = @"
using Windows.Foundation;
using System;
namespace Test
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
        private const string ClassImplementsIAsyncOperation = @"
using Windows.Foundation;
using System;
namespace Test
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
        private const string ClassImplementsIAsyncAction = @"
using Windows.Foundation;
using System;
namespace Test
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
        private const string InterfaceImplementsIAsyncOperationWithProgress = @"
using Windows.Foundation; using System;
namespace Test 
{ 
    public interface OpWithProgress : IAsyncOperationWithProgress<int, bool> {} 
}";
        private const string InterfaceImplementsIAsyncActionWithProgress = @"
using Windows.Foundation; 
using System;
namespace Test 
{ 
    public class ActionWithProgress : IAsyncActionWithProgress<int> {} 
}";
        private const string InterfaceImplementsIAsyncOperation = @"
using Windows.Foundation; 
using System;
namespace Test 
{ 
    public interface IAsyncOperation : IAsyncOperation<int> {} 
}";
        private const string InterfaceImplementsIAsyncAction = @"
using Windows.Foundation;
using System;
namespace Test 
{ 
    public interface AsyAction : IAsyncAction {} 
}";
        private const string InterfaceImplementsIAsyncOperationWithProgress2 = @"
using Windows.Foundation;
using System;
namespace Test
{
    public interface OpWithProgress : IAsyncOperationWithProgress<int, bool>
    {
        AsyncOperationProgressHandler<int, bool> IAsyncOperationWithProgress<int, bool>.Progress { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        AsyncOperationWithProgressCompletedHandler<int, bool> IAsyncOperationWithProgress<int, bool>.Completed { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        Exception IAsyncInfo.ErrorCode => throw new NotImplementedException();

        uint IAsyncInfo.Id => throw new NotImplementedException();

        AsyncStatus IAsyncInfo.Status => throw new NotImplementedException();

        void IAsyncInfo.Cancel()
        void IAsyncInfo.Close();
        int IAsyncOperationWithProgress<int, bool>.GetResults();
    }
}";
        private const string InterfaceImplementsIAsyncActionWithProgress2 = @"
using Windows.Foundation;
using System;
namespace Test
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
        private const string InterfaceImplementsIAsyncOperation2 = @"
using Windows.Foundation;
using System;
namespace Test
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
        private const string InterfaceImplementsIAsyncAction2 = @"
using Windows.Foundation;
using System;
namespace Test
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
namespace Test
{
    public sealed class OnlyParam
    {
        public void BothAttributes_Separate([System.Runtime.InteropServices.WindowsRuntime.WriteOnlyArray]
                                            [System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] arr) 
        { 
            return;
        }
    }
}";
        private const string TestArrayParamAttrUnary_2 = @"
namespace Test
{
    public sealed class OnlyParam
    {
        public void BothAttributes_Together([System.Runtime.InteropServices.WindowsRuntime.WriteOnlyArray, System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] arr) { }
    }
}";
        private const string TestArrayParamAttrUnary_3 = @"
namespace Test
{
    public sealed class OnlyParam
    {
        public void MarkedOutAndReadOnly([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] out int[] arr) { arr = new int[] { }; }
    }
}";
        private const string TestArrayParamAttrUnary_4 = @"
namespace Test
{
    public sealed class OnlyParam
    {
        public void ArrayMarkedIn([In] int[] arr) { }
    }
}";
        private const string TestArrayParamAttrUnary_5 = @"
namespace Test
{
    public sealed class OnlyParam
    {
        public void ArrayMarkedOut([Out] int[] arr) { }
    }
}";
        private const string TestArrayParamAttrUnary_6 = @"
namespace Test
{
    public sealed class OnlyParam
    {
        public void NonArrayMarkedReadOnly([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int arr) { }
    }
}";
        private const string TestArrayParamAttrUnary_7 = @"
namespace Test
{
    public sealed class OnlyParam
    {
        public void NonArrayMarkedWriteOnly([System.Runtime.InteropServices.WindowsRuntime.WriteOnlyArray] int arr) { }
    }
}";
        private const string TestArrayParamAttrUnary_8 = @"
namespace Test
{
    public sealed class OnlyParam
    {
        public void ParamMarkedIn([In] int arr) { }
    }
}";
        private const string TestArrayParamAttrUnary_9 = @"
namespace Test
{
    public sealed class OnlyParam
    {
        public void ParamMarkedOut([Out] int arr) { }
    }
}";
        private const string TestArrayParamAttrUnary_10 = @"
namespace Test
{
    public sealed class OnlyParam
    {
        public void ArrayNotMarked(int[] arr) { }
    }
}";
        private const string TestArrayParamAttrUnary_11 = @"
namespace Test
{
    public sealed class OnlyParam
    {
        public void ParamMarkedIn([System.Runtime.InteropServices.In] int arr) { }
    }
}";
        private const string TestArrayParamAttrUnary_12 = @"
namespace Test
{
    public sealed class OnlyParam
    {
        public void ParamMarkedOut([System.Runtime.InteropServices.Out] int arr) { }
    }
}";
        private const string TestArrayParamAttrUnary_13 = @"
namespace Test
{
    public sealed class OnlyParam
    {
        public void ArrayMarkedIn([System.Runtime.InteropServices.In] int[] arr) { }
    }
}";
        private const string TestArrayParamAttrUnary_14 = @"
namespace Test
{
    public sealed class OnlyParam
    {
        public void ArrayMarkedOut([System.Runtime.InteropServices.Out] int[] arr) { }
    }
}";
        private const string TestArrayParamAttrBinary_1 = @"
namespace Test
{
    public sealed class TwoParam
    {
        public void BothAttributes_Separate(int i, [System.Runtime.InteropServices.WindowsRuntime.WriteOnlyArray][System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] arr) { }
    }
}";
        private const string TestArrayParamAttrBinary_2 = @"
namespace Test
{
    public sealed class TwoParam
    {
        public void BothAttributes_Together(int i, [System.Runtime.InteropServices.WindowsRuntime.WriteOnlyArray, System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] arr) { }
    }
}";
        private const string TestArrayParamAttrBinary_3 = @"
namespace Test
{
    public sealed class TwoParam
    {
        public void MarkedOutAndReadOnly(int i, [System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] out int[] arr) { arr = new int[] { }; }
    }
}";
        private const string TestArrayParamAttrBinary_4 = @"
namespace Test
{
    public sealed class TwoParam
    {
        public void ArrayMarkedIn(int i, [In] int[] arr) { }
    }
}";
        private const string TestArrayParamAttrBinary_5 = @"
namespace Test
{
    public sealed class TwoParam
    {
        public void ArrayMarkedOut(int i, [Out] int[] arr) { }
    }
}";
        private const string TestArrayParamAttrBinary_6 = @"
namespace Test
{
    public sealed class TwoParam
    {
        public void NonArrayMarkedReadOnly(int i, [System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int arr) { }
    }
}";
        private const string TestArrayParamAttrBinary_7 = @"
namespace Test
{
    public sealed class TwoParam
    {
        public void NonArrayMarkedWriteOnly(int i, [System.Runtime.InteropServices.WindowsRuntime.WriteOnlyArray] int arr) { }
    }
}";
        private const string TestArrayParamAttrBinary_8 = @"
namespace Test
{
    public sealed class TwoParam
    {
        public void ParamMarkedIn(int i, [In] int arr) { }
    }
}";
        private const string TestArrayParamAttrBinary_9 = @"
namespace Test
{
    public sealed class TwoParam
    {
        public void ParamMarkedOut(int i, [Out] int arr) { }
    }
}";
        private const string TestArrayParamAttrBinary_10 = @"
namespace Test
{
    public sealed class TwoParam
    {
        public void ArrayNotMarked(int i, int[] arr) { }
    }
}";
        private const string TestArrayParamAttrBinary_11 = @"
namespace Test
{
    public sealed class TwoArray
    {
        public void OneValidOneInvalid_1(
            [System.Runtime.InteropServices.WindowsRuntime.WriteOnlyArray] int[] xs, 
            [System.Runtime.InteropServices.WindowsRuntime.WriteOnlyArray]
            [System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] ys) 
        {
            return;
        }
    }
}";
        private const string TestArrayParamAttrBinary_12 = @"
namespace Test
{
    public sealed class TwoArray
    {
        public void OneValidOneInvalid_2(
            [System.Runtime.InteropServices.WindowsRuntime.WriteOnlyArray]
            [System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] xs, 
            [System.Runtime.InteropServices.WindowsRuntime.WriteOnlyArray] int[] ys) 
        { 
            return; 
        }
    }
}";
        private const string TestArrayParamAttrBinary_13 = @"
namespace Test
{
    public sealed class TwoArray
    {
        public void MarkedOutAndReadOnly([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] xs, [System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] out int[] arr) 
        { 
            arr = new int[] { }; 
        }
    }
}";
        private const string TestArrayParamAttrBinary_14 = @"
namespace Test{
    public sealed class TwoParam
    {
        public void ArrayMarkedIn(int i, [In] int[] arr) { }
    }
}";
        private const string TestArrayParamAttrBinary_15 = @"
namespace Test 
{
    public sealed class TwoArray
    {
        public void ArrayMarkedIn2([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] xs, [In] int[] arr) { }
    }
}";
        private const string TestArrayParamAttrBinary_16 = @"
namespace Test
{
    public sealed class TwoArray
    {
        public void ArrayMarkedOut([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] xs, [Out] int[] arr) { }
    }
}";
        private const string TestArrayParamAttrBinary_17 = @"
namespace Test
{
    public sealed class TwoArray
    {
        public void ArrayNotMarked(int i, int[] arr) { }
    }
}";
        private const string TestArrayParamAttrBinary_18 = @"
namespace Test
{
    public sealed class TwoArray
    {
        public void NonArrayMarkedReadOnly([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] xs, [System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int i) { }
    }
}";
        private const string TestArrayParamAttrBinary_19 = @"
namespace Test
{
    public sealed class TwoArray
    {
        public void NonArrayMarkedWriteOnly([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] xs, [System.Runtime.InteropServices.WindowsRuntime.WriteOnlyArray] int i) { }
    }
}";
        private const string TestArrayParamAttrBinary_20 = @"
namespace Test
{ 
    public sealed class TwoArray
    {
        public void NonArrayMarkedWriteOnly2([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int i, [System.Runtime.InteropServices.WindowsRuntime.WriteOnlyArray] int[] arr) { }
    }
}";
        private const string TestArrayParamAttrBinary_21 = @"
namespace Test
{
    public sealed class TwoArray
    {
        public void ParamMarkedIn([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] xs, [In] int arr) { }
    }
}";
        private const string TestArrayParamAttrBinary_22 = @"
namespace Test
{
    public sealed class TwoArray
    {
        public void ParamMarkedOut([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] xs, [Out] int arr) { }
    }
}";
        private const string TestArrayParamAttrBinary_23 = @"
namespace Test
{
    public sealed class TwoArray
    {
        public void ParamMarkedOut2([Out] int arr, [System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] xs) { }
    }
}";
        private const string TestArrayParamAttrBinary_24 = @"
namespace Test
{
    public sealed class TwoArray
    {
        public void ArrayNotMarked([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] xs, int[] arr) { }
    }
}";
        // ref param 
        private const string RefParam_InterfaceMethod = @"
namespace Test
{
    public interface IHaveAMethodWithRefParam
    {
        void foo(ref int i);
    }
}";
        private const string RefParam_ClassMethod = @"
namespace Test
{
    public sealed class ClassWithMethodUsingRefParam
    {
        public void MethodWithRefParam(ref int i) { i++; }
    }
}";
        // operator overload 
        private const string OperatorOverload_Class = @"
namespace Test
{
    public sealed class ClassThatOverloadsOperator
    {
        public static ClassThatOverloadsOperator operator +(ClassThatOverloadsOperator thing)
        {
            return thing;
        }
    }
}";

        // param name conflict 
        private const string DunderRetValParam = @"
namespace Test
{
    public sealed class ParameterNamedDunderRetVal
    {
        public int Identity(int __retval)
        {
            return __retval;
        }
    }
}";
        // struct fields 
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
        private const string StructWithClassField2 = @"
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
}

namespace Prod
{
    public struct StructWithClass_Invalid
    {
        public Test.SillyClass classField;
    }
}";
        private const string StructWithDelegateField = @"
namespace Test 
{
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
        private const string EmptyStruct = @"
namespace Test 
{ 
    public struct Mt {} 
}";
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
        private const string TwoOverloads_NoAttribute_NamesHaveNumber = @"
namespace Test
{
    public sealed class TwoOverloads_NoAttribute_WithNum
    {
        public string OverloadExample1(string s) { return s; }

        public int OverloadExample1(int n) { return n; }
    }
}";
        // DefaultOverload attribute tests
        private const string TwoOverloads_TwoAttribute_OneInList_Unqualified = @"
using Windows.Foundation.Metadata;
namespace Test
{
    public sealed class TwoOverloads_TwoAttribute_OneInList
    {

        [Windows.Foundation.Metadata.Deprecated(""deprecated"", Windows.Foundation.Metadata.DeprecationType.Deprecate, 1), 
         DefaultOverload]
        public string OverloadExample(string s) { return s; } 

        [DefaultOverload]
        public int OverloadExample(int n) { return n; }
    }
}";
        private const string TwoOverloads_TwoAttribute_BothInList_Unqualified = @"
using Windows.Foundation.Metadata;
namespace Test
{
    public sealed class TwoOverloads_TwoAttribute_BothInList
    {

        [Windows.Foundation.Metadata.Deprecated(""deprecated"", Windows.Foundation.Metadata.DeprecationType.Deprecate, 1), 
         DefaultOverload()]
        public string OverloadExample(string s) { return s; }

        [Windows.Foundation.Metadata.Deprecated(""deprecated"", Windows.Foundation.Metadata.DeprecationType.Deprecate, 1), 
         DefaultOverload()]
        public int OverloadExample(int n) { return n; }
    }
}";
        private const string TwoOverloads_TwoAttribute_TwoLists_Unqualified = @"
using Windows.Foundation.Metadata;
namespace Test
{
    public sealed class TwoOverloads_TwoAttribute_TwoLists
    {

        [Windows.Foundation.Metadata.Deprecated(""deprecated"", Windows.Foundation.Metadata.DeprecationType.Deprecate, 1)]
        [DefaultOverload()]
        public string OverloadExample(string s) { return s; } 

        [DefaultOverload()]
        public int OverloadExample(int n) { return n; }
    }
}";
        private const string TwoOverloads_TwoAttribute_OneInSeparateList_OneNot_Unqualified = @"
using Windows.Foundation.Metadata;
namespace Test
{
    public sealed class TwoOverloads_TwoAttribute_OneInSeparateList_OneNot
    {
        [Windows.Foundation.Metadata.Deprecated(""deprecated"", Windows.Foundation.Metadata.DeprecationType.Deprecate, 1)]
        [DefaultOverload]
        public string OverloadExample(string s) { return s; }

        [Windows.Foundation.Metadata.Deprecated(""deprecated"", Windows.Foundation.Metadata.DeprecationType.Deprecate, 1), 
         DefaultOverload]
        public int OverloadExample(int n) { return n; }
    }
}";
        private const string TwoOverloads_TwoAttribute_BothInSeparateList_Unqualified = @"
using Windows.Foundation.Metadata;
namespace Test
{
    public sealed class TwoOverloads_TwoAttribute_BothInSeparateList
    {
        [Windows.Foundation.Metadata.Deprecated(""deprecated"", Windows.Foundation.Metadata.DeprecationType.Deprecate, 1)]
        [DefaultOverload()]
        public string OverloadExample(string s) { return s; }

        [Windows.Foundation.Metadata.Deprecated(""deprecated"", Windows.Foundation.Metadata.DeprecationType.Deprecate, 1)]
        [DefaultOverload]
        public int OverloadExample(int n) { return n; }
    }
}";
        private const string TwoOverloads_TwoAttribute_Unqualified = @"
using Windows.Foundation.Metadata;
namespace Test
{
    public sealed class TwoOverloads_TwoAttribute
    {
        [DefaultOverload]
        public string OverloadExample(string s) { return s; }

        [DefaultOverload]
        public int OverloadExample(int n) { return n; }
    }
}";
        private const string ThreeOverloads_TwoAttributes_Unqualified= @"
using Windows.Foundation.Metadata;
namespace Test
{
    public sealed class ThreeOverloads_TwoAttributes
    {
        public string OverloadExample(string s) { return s; }

        [DefaultOverload]
        public int OverloadExample(int n) { return n; }

        [DefaultOverload]
        public bool OverloadExample(bool b) { return b; }
    }
}";
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
        [Windows.Foundation.Metadata.Deprecated(""deprecated"", Windows.Foundation.Metadata.DeprecationType.Deprecate, 1), 
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
        [Windows.Foundation.Metadata.Deprecated(""deprecated"", Windows.Foundation.Metadata.DeprecationType.Deprecate, 1)]
        public string OverloadExample(string s) { return s; }

        public int OverloadExample(int n) { return n; }
    }
}";
        private const string TwoOverloads_TwoAttribute_BothInList = @"
namespace Test
{
    public sealed class TwoOverloads_TwoAttribute_BothInList
    {
        [Windows.Foundation.Metadata.Deprecated(""deprecated"", Windows.Foundation.Metadata.DeprecationType.Deprecate, 1), 
         Windows.Foundation.Metadata.DefaultOverload()]
        public string OverloadExample(string s) { return s; }

        [Windows.Foundation.Metadata.Deprecated(""deprecated"", Windows.Foundation.Metadata.DeprecationType.Deprecate, 1), 
         Windows.Foundation.Metadata.DefaultOverload()]
        public int OverloadExample(int n) { return n; }
    }
}";
        private const string TwoOverloads_TwoAttribute_TwoLists = @"
namespace Test
{
    public sealed class TwoOverloads_TwoAttribute_TwoLists
    {
        [Windows.Foundation.Metadata.Deprecated(""deprecated"", Windows.Foundation.Metadata.DeprecationType.Deprecate, 1)]
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
        [Windows.Foundation.Metadata.Deprecated(""deprecated"", Windows.Foundation.Metadata.DeprecationType.Deprecate, 1)]
        [Windows.Foundation.Metadata.DefaultOverload()]
        public string OverloadExample(string s) { return s; }

        [Windows.Foundation.Metadata.Deprecated(""deprecated"", Windows.Foundation.Metadata.DeprecationType.Deprecate, 1), 
         Windows.Foundation.Metadata.DefaultOverload()]
        public int OverloadExample(int n) { return n; }
    }
}";
        private const string TwoOverloads_TwoAttribute_BothInSeparateList = @"
namespace Test
{
    public sealed class TwoOverloads_TwoAttribute_BothInSeparateList
    {
        [Windows.Foundation.Metadata.Deprecated(""deprecated"", Windows.Foundation.Metadata.DeprecationType.Deprecate, 1)]
        [Windows.Foundation.Metadata.DefaultOverload()]
        public string OverloadExample(string s) { return s; }

        [Windows.Foundation.Metadata.Deprecated(""deprecated"", Windows.Foundation.Metadata.DeprecationType.Deprecate, 1)]
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
