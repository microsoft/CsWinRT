using NUnit.Framework;

namespace DiagnosticTests
{
    public sealed partial class UnitTesting
    {

        private const string Valid_UnrelatedNamespaceWithNoPublicTypes = @"
namespace DiagnosticTests
{
    namespace Foo
    {
        public sealed class Dog
        {
            public int Woof { get; set; }
        }
    }
}
namespace Utilities
{
    private class Sandwich
    {
        private int BreadCount { get; set; }
    }
}";

        private const string Valid_UnrelatedNamespaceWithNoPublicTypes2 = @"
namespace DiagnosticTests
{
    namespace Foo
    {
        public sealed class Dog
        {
            public int Woof { get; set; }
        }
    }
}
namespace Utilities
{
    internal partial class Sandwich
    {
        private int BreadCount { get; set; }
    }

    partial class Sandwich
    {
        private int BreadCount2 { get; set; }
    }
}";

        private const string Valid_SubNamespacesWithOverlappingNames = @"
namespace DiagnosticTests
{
    namespace Bar.Dog.Test
    {
        public sealed class Dog
        {
            public int Woof { get; set; }
        }
    }
    namespace Bar.Cat.Test
    {
        public sealed class Cat
        {
            public int Meow { get; set; }
        }
    }
}";

        private const string Valid_PrivateSetter = @"
namespace DiagnosticTests
{
    public sealed class PrivateSetter
    {
        public int MyInt { get; private set; }
    }
}";

        private const string Valid_RollYourOwnAsyncAction = @"
using System;
using Windows.Foundation;
namespace DiagnosticTests
{

    public interface IAsyncAction
    {
        void GetResults();
        AsyncActionCompletedHandler Completed { get; set; }
        void Cancel();
        void Close();
        Exception ErrorCode { get; }
        uint Id { get; }
        AsyncStatus Status { get; }
    }

    public sealed class ClassAsyncAction : IAsyncAction
    {
        public void GetResults() { throw new NotImplementedException(); }

        public AsyncActionCompletedHandler Completed { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public void Cancel() { throw new NotImplementedException(); }

        public void Close() { throw new NotImplementedException(); }

        public Exception ErrorCode => throw new NotImplementedException();

        public uint Id => throw new NotImplementedException();

        public AsyncStatus Status => throw new NotImplementedException();
    }
}";

        private const string Valid_CustomList = @"
using System;
using System.Collections.Generic;
namespace DiagnosticTests
{
    public sealed class DisposableClass : IDisposable
    {
        public bool IsDisposed { get; set; }

        public DisposableClass()
        {
            IsDisposed = false;
        }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    public sealed class CustomVector : IList<DisposableClass>
    {
        private IList<DisposableClass> _list;

        public CustomVector()
        {
            _list = new List<DisposableClass>();
        }

        public CustomVector(IList<DisposableClass> list)
        {
            _list = list;
        }

        public DisposableClass this[int index] { get => _list[index]; set => _list[index] = value; }

        public int Count => _list.Count();

        public bool IsReadOnly => false;

        public void Add(DisposableClass item)
        {
            _list.Add(item);
        }

        public void Clear()
        {
            _list.Clear();
        }

        public bool Contains(DisposableClass item)
        {
            return _list.Contains(item);
        }

        public void CopyTo(DisposableClass[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        public IEnumerator<DisposableClass> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public int IndexOf(DisposableClass item)
        {
            return _list.IndexOf(item);
        }

        public void Insert(int index, DisposableClass item)
        {
            _list.Insert(index, item);
        }

        public bool Remove(DisposableClass item)
        {
            return _list.Remove(item);
        }

        public void RemoveAt(int index)
        {
            _list.RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }
    }
}
";
        private const string Valid_CustomDictionary = @"
using System.Collections;
using System.Collections.Generic;
namespace DiagnosticTests
{
    public struct BasicStruct
    {
        public int X, Y;
        public string Value;
    }

    public sealed class CustomDictionary : IDictionary<string, BasicStruct>
    {
        private readonly Dictionary<string, BasicStruct> _dictionary;

        public CustomDictionary()
        {
            _dictionary = new Dictionary<string, BasicStruct>();
        }

        public BasicStruct this[string key] { 
            get => _dictionary[key];
            set => _dictionary[key] = value;
        }

        public ICollection<string> Keys => _dictionary.Keys;

        public ICollection<BasicStruct> Values => _dictionary.Values;

        public int Count => _dictionary.Count;

        public bool IsReadOnly => false;

        public void Add(string key, BasicStruct value)
        {
            _dictionary.Add(key, value);
        }

        public void Add(KeyValuePair<string, BasicStruct> item)
        {
            _dictionary.Add(item.Key, item.Value);
        }

        public void Clear()
        {
            _dictionary.Clear();
        }

        public bool Contains(KeyValuePair<string, BasicStruct> item)
        {
            return _dictionary.ContainsKey(item.Key);
        }

        public bool ContainsKey(string key)
        {
            return _dictionary.ContainsKey(key);
        }
        
        public void CopyTo(KeyValuePair<string, BasicStruct>[] array, int arrayIndex)
        {
        }

        public IEnumerator<KeyValuePair<string, BasicStruct>> GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

        public bool Remove(string key)
        {
            return _dictionary.Remove(key);
        }

        public bool Remove(KeyValuePair<string, BasicStruct> item)
        {
            return _dictionary.Remove(item.Key);
        }

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out BasicStruct value)
        {
            return _dictionary.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }
    }
}";
        private const string Valid_TwoNamespacesSameName = @"
namespace DiagnosticTests.Component
{
    public sealed class A
    {
        public static IReadOnlyList<Uri> GetUris()
        {
            return new List<Uri>() {
                new Uri(""http://github.com""),
                new Uri(""http://microsoft.com"")
                };
        }
    }
}

namespace DiagnosticTests.Component
{
    public sealed class B
    {
        public static IReadOnlyList<int> GetNums()
        {
            return new List<int>() { 311, 313 };
        }
    }
}";
        private const string Valid_ListUsage = @"
using System.Collections.Generic;
namespace DiagnosticTests 
{
    public sealed class A
    {
        public static IReadOnlyList<Uri> GetUris()
        {
            return new List<Uri>() {
                new Uri(""http://github.com""),
                new Uri(""http://microsoft.com"")
                };
        }
    }
}";

        private const string Valid_ListUsage2 = @"
namespace DiagnosticTests 
{
    public sealed class A
    {
        public static System.Collections.Generic.IReadOnlyList<Uri> GetUris()
        {
            return new System.Collections.Generic.List<Uri>() {
                new Uri(""http://github.com""),
                new Uri(""http://microsoft.com"")
                };
        }
    }
}";

        // Namespaces
        private const string Valid_NestedNamespace = @"
namespace DiagnosticTests 
{
    public sealed class Blank { public Blank() { } }
    namespace A
    {
        public sealed class Class4 { public Class4() { } }
    }
}";
        private const string Valid_NestedNamespace2 = @"
namespace DiagnosticTests 
{
    public sealed class Blank { public Blank() { } }
    namespace A
    {
        public sealed class Class4 { public Class4() { } }

        namespace B
        {
            public sealed class F() { public F() {} }
        }
    }
}";
        private const string Valid_NestedNamespace3 = @"
namespace DiagnosticTests.Component
{
    public sealed Class1 { public int X { get; set; }  }

    namespace InnerComponent
    {
        public sealed class Class2 { public int Y { get; } }
    }
}";
        private const string Valid_NestedNamespace4 = @"
namespace DiagnosticTests
{
    public sealed class Blank { public Blank() {} }
}

namespace DiagnosticTests.Component
{
    public sealed Class1 { public int X { get; set; }  }

    namespace InnerComponent
    {
        public sealed class Class2 { public int Y { get; } }
    }
}";
        private const string Valid_NestedNamespace5 = @"
namespace DiagnosticTests.Component
{
    namespace SubNamespace
    {
        public sealed class InnerClass { public InnerClass() {} }
    }
    public sealed class Blank { public Blank() {} }
}

namespace DiagnosticTests.Component
{
    public sealed Class1 { public int X { get; set; }  }

    namespace InnerComponent
    {
        public sealed class Class2 { public int Y { get; } }
    }
}";
        private const string Valid_NamespacesDiffer = @"
namespace DiagnosticTests 
{
    public sealed class Blank { public Blank() { } }

    namespace InnerA
    { 
        public sealed class AnotherBlank { public AnotherBlank() { } }
    }

    namespace InnerB
    { 
        public sealed class AnotherBlank { public AnotherBlank() { } }
    }
}";
        private const string Valid_NamespaceAndPrefixedNamespace = @"
namespace DiagnosticTests
{
    public sealed class Blank { public Blank() { } }
}

namespace DiagnosticTests.A
{
    public sealed class AClass { public AClass() { } }
}
";
        // Dict
        private const string Valid_ClassWithGenericDictReturnType_Private = @"
using System.Collections.Generic;
namespace DiagnosticTests
{
    public sealed class MyClass
    {
        private Dictionary<int,int> ReturnsDict(int length) { return new Dictionary<int,int>(); }
    }
}";
        private const string Valid_ClassWithGenericDictInput_Private = @"
using System.Collections.Generic;
namespace DiagnosticTests
{
    public sealed class MyClass
    {
        private int ReturnsInt(Dictionary<int,int> ls) { return 0; }
    }
}";
        private const string Valid_ClassWithPrivateGenDictProp = @"
using System.Collections.Generic;
namespace DiagnosticTests
{
    public sealed class MyClass
    {
        private System.Collections.Generic.Dictionary<int,int> IntList { get; set; }
    }
}";
        private const string Valid_IfaceWithPrivateGenDictProp = @"
using System.Collections.Generic;
namespace DiagnosticTests
{
    public interface MyInterface
    {
       private Dictionary<int,int> Dict { get; set; }
    }
}";
        // RODict
        private const string Valid_ClassWithGenericRODictReturnType_Private = @"
using System.Collections.ObjectModel;
namespace DiagnosticTests
{
    public sealed class MyClass
    {
        private ReadOnlyDictionary<int,int> ReturnsRODict(int length) { return new ReadOnlyDictionary<int,int>(); }
    }
}";
        private const string Valid_ClassWithGenericRODictInput_Private = @"
using System.Collections.ObjectModel;
namespace DiagnosticTests
{
    public sealed class MyClass
    {
        private int ReturnsInt(ReadOnlyDictionary<int,int> ls) { return 0; }
    }
}";
        private const string Valid_ClassWithPrivateGenRODictProp = @"
using System.Collections.ObjectModel;
namespace DiagnosticTests
{
    public sealed class MyClass
    {
        private ReadOnlyDictionary<int,int> RODict { get; set; }
    }
}";
        private const string Valid_IfaceWithPrivateGenRODictProp = @"
using System.Collections.ObjectModel;
namespace DiagnosticTests
{
    public interface MyInterface
    {
       private ReadOnlyDictionary<int,int> RODict { get; set; }
    }
}";
        // KeyValuePair 
        private const string Valid_InterfaceWithGenericKVPairReturnType = @"
using System.Collections.Generic;
namespace DiagnosticTests
{
    public interface MyInterface
    {
        KeyValuePair<int,int> KVPair(int length);
    }
}";
        private const string Valid_InterfaceWithGenericKVPairInput = @"
using System.Collections.Generic;
namespace DiagnosticTests
{
    public interface MyInterface
    {
        int ReturnsInt(System.Collections.Generic.KeyValuePair<int,int> kvp);
    }
}";
        private const string Valid_ClassWithGenericKVPairReturnType = @"
using System.Collections.Generic;
namespace DiagnosticTests
{
    public sealed class MyClass
    {
        public KeyValuePair<int,int> ReturnsKVPair(int length);
    }
}";
        private const string Valid_ClassWithGenericKVPairInput = @"
using System.Collections.Generic;
namespace DiagnosticTests
{
    public sealed class MyClass
    {
        public int ReturnsInt(KeyValuePair<int,int> ls) { return 0; }
    }
}";
        private const string Valid_IfaceWithGenKVPairProp = @"
using System.Collections.Generic;
namespace DiagnosticTests
{
    public interface MyInterface
    {
        public KeyValuePair<int,int> KVpair { get; set; }
    }
}";
        private const string Valid_ClassWithGenKVPairProp = @"
using System.Collections.Generic;
namespace DiagnosticTests
{
    public sealed class MyClass
    {
        public KeyValuePair<int,int> KVpair { get; set; }
    }
}";
        private const string Valid_ClassWithGenericKVPairReturnType_Private = @"
using System.Collections.Generic;
namespace DiagnosticTests
{
    public sealed class MyClass
    {
        private KeyValuePair<int,int> ReturnsKVPair(int length) { return new KeyValuePair<int,int>(); }
    }
}";
        private const string Valid_ClassWithGenericKVPairInput_Private = @"
using System.Collections.Generic;
namespace DiagnosticTests
{
    public sealed class MyClass
    {
        private int ReturnsInt(KeyValuePair<int,int> ls) { return 0; }
    }
}";
        private const string Valid_ClassWithPrivateGenKVPairProp = @"
using System.Collections.Generic;
namespace DiagnosticTests
{
    public sealed class MyClass
    {
        private KeyValuePair<int,int> KVPair { get; set; }
    }
}";
        private const string Valid_IfaceWithPrivateGenKVPairProp = @"
using System.Collections.Generic;
namespace DiagnosticTests
{
    public interface MyInterface
    {
       private KeyValuePair<int,int> KVPair { get; set; }
    }
}";
        // Enumerable
        private const string Valid_ClassWithGenericEnumerableReturnType_Private = @"
using System.Linq;
namespace DiagnosticTests
{
    public sealed class MyClass
    {
        private Enumerable ReturnsIntList(int length) { return new Enumerable(); }
    }
}";
        private const string Valid_ClassWithGenericEnumerableInput_Private = @"
using System.Linq;
namespace DiagnosticTests
{
    public sealed class MyClass
    {
        private int ReturnsIntList(Enumerable ls) { return 0; }
    }
}";
        private const string Valid_ClassWithPrivateGenEnumerableProp = @"
using System.Linq;
namespace DiagnosticTests
{
    public sealed class MyClass
    {
        private Enumerable IntList { get; set; }
    }
}";
        private const string Valid_IfaceWithPrivateGenEnumerableProp = @"
using System.Linq;
namespace DiagnosticTests
{
    public interface MyInterface
    {
       private Enumerable Enumer { get; set; }
    }
}";
        // List
        private const string Valid_ClassWithGenericListReturnType_Private = @"
using System.Collections.Generic;
namespace DiagnosticTests
{
    public sealed class MyClass
    {
        private List<int> ReturnsIntList(int length) { return List<int>(); }
    }
}";
        private const string Valid_ClassWithGenericListInput_Private = @"
using System.Collections.Generic;
namespace DiagnosticTests
{
    public sealed class MyClass
    {
        private int ReturnsIntList(List<int> ls) { return 0; }
    }
}";
        private const string Valid_ClassWithPrivateGenListProp = @"
using System.Collections.Generic;
namespace DiagnosticTests
{
    public sealed class MyClass
    {
        private List<int> IntList { get; set; }
    }
}";
        private const string Valid_IfaceWithPrivateGenListProp = @"
namespace DiagnosticTests
{
    public interface MyInterface
    {
       private List<int> IntList { get; set; }
    }
}";
        //// DefaultOverload attribute
        private const string Valid_InterfaceWithOverloadAttribute = @"
using Windows.Foundation.Metadata;
namespace DiagnosticTests
{
    public interface MyInterface
    {
        int Foo(int n);
        [DefaultOverload]
        int Foo(string s);
    }
}";
        private const string Valid_TwoOverloads_DiffParamCount = @"
namespace DiagnosticTests
{
    public sealed class Valid_TwoOverloads_DiffParamCount
    {
       public string OverloadExample(string s) { return s; }
       public int OverloadExample(int n, int m) { return n; }
    }
}";
        private const string Valid_TwoOverloads_OneAttribute_OneInList = @"
namespace DiagnosticTests
{    
    public sealed class Valid_TwoOverloads_OneAttribute_OneInList
    {
        [Windows.Foundation.Metadata.Deprecated(""deprecated"", Windows.Foundation.Metadata.DeprecationType.Deprecate, 1), 
         Windows.Foundation.Metadata.DefaultOverload()]
        public string OverloadExample(string s) { return s; }

        public int OverloadExample(int n) { return n; }
    }

}";
        private const string Valid_TwoOverloads_OneAttribute_OneIrrelevatAttribute = @"
namespace DiagnosticTests
{
    public sealed class Valid_TwoOverloads_OneAttribute_OneIrrelevatAttribute
    {
        [Windows.Foundation.Metadata.Deprecated(""deprecated"", Windows.Foundation.Metadata.DeprecationType.Deprecate, 1)]
        public string OverloadExample(string s) { return s; }

        [Windows.Foundation.Metadata.DefaultOverload()]
        public int OverloadExample(int n) { return n; }
    }
}";
        private const string Valid_TwoOverloads_OneAttribute_TwoLists = @"
namespace DiagnosticTests
{
    public sealed class Valid_TwoOverloads_OneAttribute_TwoLists
    {

        [Windows.Foundation.Metadata.Deprecated(""deprecated"", Windows.Foundation.Metadata.DeprecationType.Deprecate, 1)]
        [Windows.Foundation.Metadata.DefaultOverload()]
        public string OverloadExample(string s) { return s; }

        public int OverloadExample(int n) { return n; }
    }
}";
        private const string Valid_ThreeOverloads_OneAttribute = @"
namespace DiagnosticTests
{
    public sealed class Valid_ThreeOverloads_OneAttribute
    {
        public string OverloadExample(string s) { return s; }

        [Windows.Foundation.Metadata.DefaultOverload()]
        public int OverloadExample(int n) { return n; }

        public bool OverloadExample(bool b) { return b; }
    }
}";
        private const string Valid_ThreeOverloads_OneAttribute_2 = @"
namespace DiagnosticTests
{
    public sealed class Valid_ThreeOverloads_OneAttribute_2
    {
        public string OverloadExample(string s) { return s; }

        public int OverloadExample(int n) { return n; }

        [Windows.Foundation.Metadata.DefaultOverload()]
        public bool OverloadExample(bool b) { return b; }
    }
}";
        private const string Valid_TwoOverloads_OneAttribute_3 = @"
namespace DiagnosticTests
{
    public sealed class Valid_TwoOverloads_OneAttribute_3
    {
        public string OverloadExample(string s) { return s; }

        [Windows.Foundation.Metadata.DefaultOverload()]
        public int OverloadExample(int n) { return n; }
    }
}";
        //// Jagged array
        private const string Valid_JaggedMix_PrivateClassPublicProperty = @"
namespace DiagnosticTests
{
    public sealed class Blank
    {
        public Blank() {}
    }
    internal sealed class Valid_JaggedArray_PrivateClassPublicProperty
    {
        private int[][] Arr { get; set; }
        public int[][] ArrP { get; set; }
        public int[][][] Arr3 { get; set; }
        private int[][][] Arr3P { get; set; }
    }
}";
        private const string Valid_Jagged2D_PrivateClassPublicMethods = @"
namespace DiagnosticTests
{
    public sealed class Blank
    {
        public Blank() {}
    }
    internal sealed class Valid_JaggedArray_PrivateClassPublicMethods
    {
        public int[][] J2_ReturnOnly() 
        {
            int[][] arr = new int[2][];
            arr[0] = new int[1] { 1 };
            arr[1] = new int[1] { 2 };
            return arr;
        }
        public int[][] J2_ReturnAndInput1(int[][] arr) { return arr; }
        public int[][] J2_ReturnAndInput2of2(bool a, int[][] arr) { return arr; }
        public bool J2_NotReturnAndInput2of2(bool a, int[][] arr) { return a; }
        public bool J2_NotReturnAndInput2of3(bool a, int[][] arr, bool b) { return a; }
        public int[][] J2_ReturnAndInput2of3(bool a, int[][] arr, bool b) { return arr; }
    }
}";
        private const string Valid_Jagged3D_PrivateClassPublicMethods = @"
namespace DiagnosticTests
{
    public sealed class Blank
    {
        public Blank() {}
    }
    internal sealed class Valid_Jagged3D_PrivateClassPublicMethods 
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
        public int[][][] J3_ReturnAndInput1(int[][][] arr) { return arr; }
        public int[][][] J3_ReturnAndInput2of2(bool a, int[][][] arr) { return arr; }
        public int[][][] J3_ReturnAndInput2of3(bool a, int[][][] arr, bool b) { return arr; }
        public bool J3_NotReturnAndInput2of2(bool a, int[][][] arr) { return a; }
        public bool J3_NotReturnAndInput2of3(bool a, int[][][] arr, bool b) { return a; }
    }
}";
        private const string Valid_Jagged3D_PublicClassPrivateMethods = @"
namespace DiagnosticTests
{
    public sealed class Valid_Jagged3D_PublicClassPrivateMethods
    {
        private int[][][] D3_ReturnOnly() 
        {
            int[][] arr2 = new int[2][];
            arr2[0] = new int[1] { 1 };
            arr2[1] = new int[1] { 2 };

            int[][][] arr = new int[1][][];
            arr[0] = arr2;
            return arr; 
        }
        private int[][][] D3_ReturnAndInput1(int[][][] arr) { return arr; }
        private int[][][] D3_ReturnAndInput2of2(bool a, int[][][] arr) { return arr; }
        private int[][][] D3_ReturnAndInput2of3(bool a, int[][][] arr, bool b) { return arr; }
        private bool D3_NotReturnAndInput2of2(bool a, int[][][] arr) { return a; }
        private bool D3_NotReturnAndInput2of3(bool a, int[][][] arr, bool b) { return a; }
    }
}";
        private const string Valid_Jagged2D_Property = @"
namespace DiagnosticTests
{
    public sealed class Jagged2D_Property1
    {
        private int[][] ArrP { get; set; } 
    }
}";
        private const string Valid_Jagged3D_Property = @"
namespace DiagnosticTests
{
    public sealed class Jagged3D_Property2
    {
        private int[][][] Arr3P { get; set; }
    }
}";
        // prop
        private const string Valid_MultiDimArray_PrivateClassPublicProperty1 = @"
namespace DiagnosticTests
{
    public sealed class Blank
    {
        public Blank() {}
    }
    internal class Valid_MultiDimArray_PrivateClassPublicProperty1
    {
        public int[,] Arr_2d { get; set; }
    }
}";
        private const string Valid_MultiDimArray_PrivateClassPublicProperty2 = @"
namespace DiagnosticTests
{
    public sealed class Blank
    {
        public Blank() {}
    }
    internal class Valid_MultiDimArray_PrivateClassPublicProperty2
    {
        public int[,,] Arr_3d { get; set; }
    }
}";
        private const string Valid_MultiDimArray_PrivateClassPublicProperty3 = @"
namespace DiagnosticTests
{
    public sealed class Blank
    {
        public Blank() {}
    }
    internal class Valid_MultiDimArray_PrivateClassPublicProperty3
    {
        private int[,] PrivArr_2d { get; set; }
    }
}";
        private const string Valid_MultiDimArray_PrivateClassPublicProperty4 = @"
namespace DiagnosticTests
{
    public sealed class Blank
    {
        public Blank() {}
    }
    internal class Valid_MultiDimArray_PrivateClassPublicProperty4
    {
        private int[,,] PrivArr_3d { get; set; }
    }
}";
        private const string Valid_MultiDimArray_PublicClassPrivateProperty1 = @"
namespace DiagnosticTests
{
    public sealed class Valid_MultiDimArray_PublicClassPrivateProperty1
    {
        private int[,] PrivArr_2d { get; set; }
    }
}";
        private const string Valid_MultiDimArray_PublicClassPrivateProperty2 = @"
namespace DiagnosticTests
{
    public sealed class Valid_MultiDimArray_PublicClassPrivateProperty2
    {
        private int[,,] PrivArr_3d { get; set; }
    }
}";
        // 2d 
        private const string Valid_2D_PrivateClass_PublicMethod1 = @"
namespace DiagnosticTests
{
    public sealed class Blank
    {
        public Blank() {}
    }
    internal sealed class Valid_2D_PrivateClass_PublicMethod1
    {
        public int[,] D2_ReturnOnly() { return new int[4, 2]; }
    }
}";
        private const string Valid_2D_PrivateClass_PublicMethod2 = @"
namespace DiagnosticTests
{
    public sealed class Blank
    {
        public Blank() {}
    }
    internal sealed class Valid_2D_PrivateClass_PublicMethod2
    {
        public int[,] D2_ReturnAndInput1(int[,] arr) { return arr; }
    }
}";
        private const string Valid_2D_PrivateClass_PublicMethod3 = @"
namespace DiagnosticTests
{
    public sealed class Blank
    {
        public Blank() {}
    }
    internal sealed class Valid_2D_PrivateClass_PublicMethod3
    {
        public int[,] D2_ReturnAndInput2of2(bool a, int[,] arr) { return arr; }
    }
}";
        private const string Valid_2D_PrivateClass_PublicMethod4 = @"
namespace DiagnosticTests
{
    public sealed class Blank
    {
        public Blank() {}
    }
    internal sealed class Valid_2D_PrivateClass_PublicMethod4
    {
        public bool D2_NotReturnAndInput2of2(bool a, int[,] arr) { return a; }
    }
}";
        private const string Valid_2D_PrivateClass_PublicMethod5 = @"
namespace DiagnosticTests
{
    public sealed class Blank
    {
        public Blank() {}
    }
    internal sealed class Valid_2D_PrivateClass_PublicMethod5
    {
        public bool D2_NotReturnAndInput2of3(bool a, int[,] arr, bool b) { return a; }
    }
}";
        private const string Valid_2D_PrivateClass_PublicMethod6 = @"
namespace DiagnosticTests
{
    public sealed class Blank
    {
        public Blank() {}
    }
    internal sealed class Valid_2D_PrivateClass_PublicMethod6
    {
        public int[,] D2_ReturnAndInput2of3(bool a, int[,] arr, bool b) { return arr; }
    }
}";
        // 3d 
        private const string Valid_3D_PrivateClass_PublicMethod1 = @"
namespace DiagnosticTests
{
    public sealed class Blank
    {
        public Blank() {}
    }
    internal sealed class Valid_3D_PrivateClass_PublicMethod1
    {
        public int[,,] D3_ReturnOnly() { return new int[2, 1, 3] { { { 1, 1, 1 } }, { { 2, 2, 2 } } }; }
    }
}";
        private const string Valid_3D_PrivateClass_PublicMethod2 = @"
namespace DiagnosticTests
{
    public sealed class Blank
    {
        public Blank() {}
    }
    internal sealed class Valid_3D_PrivateClass_PublicMethod2
    {
        public int[,,] D3_ReturnAndInput1(int[,,] arr) { return arr; }
    }
}";
        private const string Valid_3D_PrivateClass_PublicMethod3 = @"
namespace DiagnosticTests
{
    public sealed class Blank
    {
        public Blank() {}
    }
    internal sealed class Valid_3D_PrivateClass_PublicMethod3
    {
        public int[,,] D3_ReturnAndInput2of2(bool a, int[,,] arr) { return arr; }
    }
}";
        private const string Valid_3D_PrivateClass_PublicMethod4 = @"
namespace DiagnosticTests
{
    public sealed class Blank
    {
        public Blank() {}
    }
    internal sealed class Valid_3D_PrivateClass_PublicMethod4
    {
        public int[,,] D3_ReturnAndInput2of3(bool a, int[,,] arr, bool b) { return arr; }
    }
}";
        private const string Valid_3D_PrivateClass_PublicMethod5 = @"
namespace DiagnosticTests
{
    public sealed class Blank
    {
        public Blank() {}
    }
    internal sealed class Valid_3D_PrivateClass_PublicMethod5
    {
        public bool D3_NotReturnAndInput2of2(bool a, int[,,] arr) { return a; }
    }
}";
        private const string Valid_3D_PrivateClass_PublicMethod6 = @"
namespace DiagnosticTests
{
    public sealed class Blank
    {
        public Blank() {}
    }
    internal sealed class Valid_3D_PrivateClass_PublicMethod6
    {
        public bool D3_NotReturnAndInput2of3(bool a, int[,,] arr, bool b) { return a; }
    }
}";
        // methods
        private const string Valid_MultiDimArray_PublicClassPrivateMethod1 = @"
namespace DiagnosticTests
{
    public sealed class Valid_MultiDimArray_PublicClassPrivateProperty1
    {
        private int[,,] D3_ReturnOnly() { return new int[2, 1, 3] { { { 1, 1, 1 } }, { { 2, 2, 2 } } }; }
    }
}";
        private const string Valid_MultiDimArray_PublicClassPrivateMethod2 = @"
namespace DiagnosticTests
{
    public sealed class Valid_MultiDimArray_PublicClassPrivateProperty2
    {
        private int[,,] D3_ReturnAndInput1(int[,,] arr) { return arr; }
    }
}";
        private const string Valid_MultiDimArray_PublicClassPrivateMethod3 = @"
namespace DiagnosticTests
{
    public sealed class Valid_MultiDimArray_PublicClassPrivateProperty3
    {
        private int[,,] D3_ReturnAndInput2of2(bool a, int[,,] arr) { return arr; }
    }
}";
        private const string Valid_MultiDimArray_PublicClassPrivateMethod4 = @"
namespace DiagnosticTests
{
    public sealed class Valid_MultiDimArray_PublicClassPrivateProperty4
    {
        private int[,,] D3_ReturnAndInput2of3(bool a, int[,,] arr, bool b) { return arr; }
    }
}";
        private const string Valid_MultiDimArray_PublicClassPrivateMethod5 = @"
namespace DiagnosticTests
{
    public sealed class Valid_MultiDimArray_PublicClassPrivateProperty5
    {
        private bool D3_NotReturnAndInput2of2(bool a, int[,,] arr) { return a; }
    }
}";
        private const string Valid_MultiDimArray_PublicClassPrivateMethod6 = @"
namespace DiagnosticTests
{
    public sealed class Valid_MultiDimArray_PublicClassPrivateProperty6
    {
        private bool D3_NotReturnAndInput2of3(bool a, int[,,] arr, bool b) { return a; }
    }
}";
        //// System.Array 
        private const string Valid_SystemArray_Interface1 = @"
namespace DiagnosticTests
{
    public sealed class Blank
    {
        public Blank() {}
    }
    internal interface Valid_SystemArray_Interface1
    {
        System.Array Id(System.Array arr);
    }
}";
        private const string Valid_SystemArray_Interface2 = @"
namespace DiagnosticTests
{
    public sealed class Blank
    {
        public Blank() {}
    }
    internal interface Valid_SystemArray_Interface2
    {
        void Method2(System.Array arr);
    }
}";
        private const string Valid_SystemArray_Interface3 = @"
namespace DiagnosticTests
{
    public sealed class Blank
    {
        public Blank() {}
    }
    internal interface Valid_SystemArray_Interface3
    {
        System.Array Method3();
    }
}";
        private const string Valid_SystemArray_InternalClass1 = @"
namespace DiagnosticTests
{
    public sealed class Blank
    {
        public Blank() {}
    }
    internal class Valid_SystemArray_InternalClass1
    {
        public System.Array Arr_2d { get; set; }
    }
}";
        private const string Valid_SystemArray_InternalClass2 = @"
namespace DiagnosticTests
{
    public sealed class Blank
    {
        public Blank() {}
    }
    internal class Valid_SystemArray_InternalClass2
    {

        public System.Array Arr_3d { get; set; }
    }
}";
        private const string Valid_SystemArray_InternalClass3 = @"
namespace DiagnosticTests
{
    public sealed class Blank
    {
        public Blank() {}
    }
    internal class Valid_SystemArray_InternalClass3
    {
        private System.Array PrivArr_2d { get; set; }
    }
}";
        private const string Valid_SystemArray_InternalClass4 = @"
namespace DiagnosticTests
{
    public sealed class Blank
    {
        public Blank() {}
    }
    internal class Valid_SystemArray_InternalClass4
    {
        private System.Array PrivArr_3d { get; set; }
    }
}";
        private const string Valid_SystemArray_PublicClassPrivateProperty1 = @"
namespace DiagnosticTests
{
    public sealed class Valid_SystemArray_PublicClassPrivateProperty1
    {
        private System.Array PrivArr_2d { get; set; }
    }
}";
        private const string Valid_SystemArray_PublicClassPrivateProperty2 = @"
using System;
namespace DiagnosticTests
{
    public sealed class Valid_SystemArray_PublicClassPrivateProperty2
    {
        private Array PrivArr_3d { get; set; }
    }
}";
        private const string Valid_SystemArray_PublicClassPrivateProperty3 = @"
namespace DiagnosticTests
{
    public sealed class Valid_SystemArrayPublicClassPrivateProperty3
    {
        private int[] PrivArr3 { get { return (int[])Array.CreateInstance(typeof(int), new int[] { 4 }); } }
    }
}";
        private const string Valid_SystemArray_PublicClassPrivateProperty4 = @"
namespace DiagnosticTests
{
    public sealed class Valid_SystemArrayPublicClassPrivateProperty4
    {
        private System.Array PrivArr4 { get { return Array.CreateInstance(typeof(int), new int[] { 4 }); } }
    }
}";
        private const string Valid_SystemArray_PublicClassPrivateProperty5 = @"
namespace DiagnosticTests
{
    public sealed class Valid_SystemArrayPublicClassPrivateProperty1
    {
        private int[] PrivArr { get { return (int[])Array.CreateInstance(typeof(int), new int[] { 4 }, new int[] { 1 }); } }
    }
}";
        private const string Valid_SystemArray_PublicClassPrivateProperty6 = @"
namespace DiagnosticTests
{
    public sealed class Valid_SystemArrayPublicClassPrivateProperty2
    {
        private System.Array PrivArr2 { get { return Array.CreateInstance(typeof(int), new int[] { 4 }, new int[] { 1 }); } }
    }
}";
        private const string Valid_SystemArray_InternalClassPublicMethods1 = @"
namespace DiagnosticTests
{
    public sealed class Blank
    {
        public Blank() {}
    }
    internal sealed class Valid_SystemArray_InternalClassPublicMethods1
    {
        public System.Array SysArr_ReturnOnly() { return Array.CreateInstance(typeof(int), new int[] { 4 }); }
    }
}";
        private const string Valid_SystemArray_InternalClassPublicMethods2 = @"
namespace DiagnosticTests
{
    public sealed class Blank
    {
        public Blank() {}
    }
    internal sealed class Valid_SystemArray_InternalClassPublicMethods2
    {
        public System.Array SysArr_ReturnAndInput1(System.Array arr) { return arr; }
    }
}";
        private const string Valid_SystemArray_InternalClassPublicMethods3 = @"
namespace DiagnosticTests
{
    public sealed class Blank
    {
        public Blank() {}
    }
    internal sealed class Valid_SystemArray_InternalClassPublicMethods3
    {
        public System.Array SysArr_ReturnAndInput2of2(bool a, System.Array arr) { return arr; }
    }
}";
        private const string Valid_SystemArray_InternalClassPublicMethods4 = @"
namespace DiagnosticTests
{
    public sealed class Blank
    {
        public Blank() {}
    }
    internal sealed class Valid_SystemArray_InternalClassPublicMethods4
    {
        public bool SysArr_NotReturnAndInput2of2(bool a, System.Array arr) { return a; }
    }
}";
        private const string Valid_SystemArray_InternalClassPublicMethods5 = @"
namespace DiagnosticTests
{
    public sealed class Blank
    {
        public Blank() {}
    }
    internal sealed class Valid_SystemArray_InternalClassPublicMethods5
    {
        public bool SysArr_NotReturnAndInput2of3(bool a, System.Array arr, bool b) { return a; }
    }
}";
        private const string Valid_SystemArray_InternalClassPublicMethods6 = @"
namespace DiagnosticTests
{
    public sealed class Blank
    {
        public Blank() {}
    }
    internal sealed class Valid_SystemArray_InternalClassPublicMethods6
    {
        public System.Array SysArr_ReturnAndInput2of3(bool a, System.Array arr, bool b) { return arr; }
    }
}";
        private const string Valid_SystemArray_PrivateClassPublicProperty1 = @"
namespace DiagnosticTests
{
    public sealed class Blank
    {
        public Blank() {}
    }
    internal sealed class Valid_SystemArray_PrivateClassPublicProperty1
    {
        public int[] Arr { get { return (int[])Array.CreateInstance(typeof(int), new int[] { 4 }, new int[] { 1 }); } }
    }
}";
        private const string Valid_SystemArray_PrivateClassPublicProperty2 = @"
namespace DiagnosticTests
{    public sealed class Blank
    {
        public Blank() {}
    }
    internal sealed class Valid_SystemArray_PrivateClassPublicProperty2
    {
        public System.Array Arr2 { get { return Array.CreateInstance(typeof(int), new int[] { 4 }, new int[] { 1 }); } }
    }
}";
        private const string Valid_SystemArray_PrivateClassPublicProperty3 = @"
namespace DiagnosticTests
{    public sealed class Blank
    {
        public Blank() {}
    }
    internal sealed class Valid_SystemArray_PrivateClassPublicProperty3
    {
        public int[] Arr3 { get { return (int[])Array.CreateInstance(typeof(int), new int[] { 4 }); } }
    }
}";
        private const string Valid_SystemArray_PrivateClassPublicProperty4 = @"
namespace DiagnosticTests
{
    public sealed class Blank
    {
        public Blank() {}
    }
    internal sealed class Valid_SystemArray_PrivateClassPublicProperty4
    {
        public System.Array Arr4 { get { return Array.CreateInstance(typeof(int), new int[] { 4 }); } }
    }
}";
        private const string Valid_SystemArray_PrivateClassPublicProperty5 = @"
namespace DiagnosticTests
{    
    public sealed class Blank
    {
        public Blank() {}
    }

    internal sealed class Valid_SystemArray_PrivateClassPublicProperty5
    {
        private int[] PrivArr { get { return (int[])Array.CreateInstance(typeof(int), new int[] { 4 }, new int[] { 1 }); } }
    }
}";
        private const string Valid_SystemArray_PrivateClassPublicProperty6 = @"
namespace DiagnosticTests
{
    public sealed class Blank
    {
        public Blank() {}
    }
    internal sealed class Valid_SystemArray_PrivateClassPublicProperty6
    {
        private System.Array PrivArr2 { get { return Array.CreateInstance(typeof(int), new int[] { 4 }, new int[] { 1 }); } }
    }
}";
        private const string Valid_SystemArray_PrivateClassPublicProperty7 = @"
namespace DiagnosticTests
{
    public sealed class Blank
    {
        public Blank() {}
    }
    internal sealed class Valid_SystemArray_PrivateClassPublicProperty7
    {
        private int[] PrivArr3 { get { return (int[])Array.CreateInstance(typeof(int), new int[] { 4 }); } }
    }
}";
        private const string Valid_SystemArray_PrivateClassPublicProperty8 = @"
namespace DiagnosticTests
{
    public sealed class Blank
    {
        public Blank() {}
    }
    internal sealed class Valid_SystemArray_PrivateClassPublicProperty8
    {
        private System.Array PrivArr4 { get { return Array.CreateInstance(typeof(int), new int[] { 4 }); } }
    }
}";
        private const string Valid_SystemArrayPublicClassPrivateMethod1 = @"
namespace DiagnosticTests
{
    public sealed class Valid_SystemArrayPublicClassPrivateMethod1
    {
        private System.Array SysArr_ReturnOnly() { return Array.CreateInstance(typeof(int), new int[] { 4 }); }
    }
}";
        private const string Valid_SystemArrayPublicClassPrivateMethod2 = @"
namespace DiagnosticTests
{
    public sealed class Valid_SystemArrayPublicClassPrivateMethod2
    {
        private System.Array SysArr_ReturnAndInput1(System.Array arr) { return arr; }
    }
}";
        private const string Valid_SystemArrayPublicClassPrivateMethod3 = @"
namespace DiagnosticTests
{
    public sealed class Valid_SystemArrayPublicClassPrivateMethod3
    {
        private System.Array SysArr_ReturnAndInput2of2(bool a, System.Array arr) { return arr; }
    }
}";
        private const string Valid_SystemArrayPublicClassPrivateMethod4 = @"
namespace DiagnosticTests
{
    public sealed class Valid_SystemArrayPublicClassPrivateMethod4
    {
        private System.Array SysArr_ReturnAndInput2of3(bool a, System.Array arr, bool b) { return arr; }
    }
}";
        private const string Valid_SystemArrayPublicClassPrivateMethod5 = @"
namespace DiagnosticTests
{
    public sealed class Valid_SystemArrayPublicClassPrivateMethod5
    {
        private bool SysArr_NotReturnAndInput2of2(bool a, System.Array arr) { return a; }
    }
}";
        private const string Valid_SystemArrayPublicClassPrivateMethod6 = @"
namespace DiagnosticTests
{
    public sealed class Valid_SystemArrayPublicClassPrivateMethod6
    {
        private bool SysArr_NotReturnAndInput2of3(bool a, System.Array arr, bool b) { return a; }
    }
}";
        private const string Valid_SystemArrayProperty = @"
namespace DiagnosticTests
{
    public sealed class SystemArrayProperty_Valid
    {
       private System.Array PrivArr { get; set; } 
    }
}";
        //// ReadOnlyArray / WriteOnlyArray
        private const string Valid_ArrayParamAttrUnary_1 = @"
namespace DiagnosticTests 
{
    public sealed class OnlyParam
    { 
        public int GetSum([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] arr) { return 0; } 
    }
}";
        private const string Valid_ArrayParamAttrUnary_2 = @"
namespace DiagnosticTests 
{
    public sealed class OnlyParam
    { 
        public void MarkedWriteOnly_Valid([System.Runtime.InteropServices.WindowsRuntime.WriteOnlyArray] int[] arr) { }
    }
}";
        private const string Valid_ArrayParamAttrUnary_3 = @"
namespace DiagnosticTests 
{
    public sealed class OnlyParam
    { 
        public void MarkedOutAndWriteOnly_Valid([System.Runtime.InteropServices.WindowsRuntime.WriteOnlyArray] out int[] arr) { arr = new int[] { }; }
    }
}";
        private const string Valid_ArrayParamAttrUnary_4 = @"
namespace DiagnosticTests 
{
    public sealed class OnlyParam
    { 
        public void MarkedOutOnly_Valid(out int[] arr) { arr = new int[] { }; }
    }
}";
        private const string Valid_ArrayParamAttrBinary_1 = @"
namespace DiagnosticTests 
{
    public sealed class TwoParam
    { 
        public int GetSum(int i, [System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] arr) { return 0; } 
    }
}";
        private const string Valid_ArrayParamAttrBinary_2 = @"
namespace DiagnosticTests 
{
    public sealed class TwoParam
    { 
        public void MarkedWriteOnly_Valid(int i, [System.Runtime.InteropServices.WindowsRuntime.WriteOnlyArray] int[] arr) { }
    }
}";
        private const string Valid_ArrayParamAttrBinary_3 = @"
namespace DiagnosticTests 
{
    public sealed class TwoParam
    { 
        public void MarkedOutAndWriteOnly_Valid(int i, [System.Runtime.InteropServices.WindowsRuntime.WriteOnlyArray] out int[] arr) { arr = new int[] { }; }
    }
}";
        private const string Valid_ArrayParamAttrBinary_4 = @"
namespace DiagnosticTests 
{
    public sealed class TwoParam
    { 
        public void MarkedOutOnly_Valid(int i, out int[] arr) { arr = new int[] { }; }
    }
}";
        private const string Valid_ArrayParamAttrBinary_5 = @"
namespace DiagnosticTests 
{
    public sealed class TwoArray
    { 
        public void MarkedReadOnly_Valid([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] xs, [System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] arr) { }
    }
}";
        private const string Valid_ArrayParamAttrBinary_6 = @"
namespace DiagnosticTests 
{
    public sealed class TwoArray
    { 
        public void MarkedWriteOnly_Valid([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] xs, [System.Runtime.InteropServices.WindowsRuntime.WriteOnlyArray] int[] arr) { }
    }
}";
        private const string Valid_ArrayParamAttrBinary_7 = @"
namespace DiagnosticTests 
{
    public sealed class TwoArray
    { 
        public void MarkedOutAndWriteOnly_Valid([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] xs, [System.Runtime.InteropServices.WindowsRuntime.WriteOnlyArray] out int[] arr) { arr = new int[] { }; }
    }
}";
        private const string Valid_ArrayParamAttrBinary_8 = @"
namespace DiagnosticTests 
{
    public sealed class TwoArray
    { 
        public void MarkedOut_Valid([System.Runtime.InteropServices.WindowsRuntime.WriteOnlyArray] int[] xs, out int[] arr) { arr = new int[] { }; }
    }
}";
        //// Struct field 
        private const string Valid_StructWithByteField = @"
namespace DiagnosticTests
{
    public struct StructWithByteField_Valid
    {
        public byte b;
    }
}";

        private const string Valid_StructWithEnumField = @"
namespace DiagnosticTests
{
    public enum AnEnum { A = 0, B = 1 }
    public struct StructWithEnumField_Valid
    {
        public AnEnum value;
    }
}";

        private const string Valid_StructWithImportedStruct = @"
using System.Numerics;
namespace DiagnosticTests
{
    public struct StructWithWinRTStructField
    {
        public Matrix3x2 matrix;
    }
}";
        private const string Valid_StructWithImportedStructQualified = @"
using System.Numerics;
namespace DiagnosticTests
{
    public struct StructWithWinRTStructField
    {
        public System.Numerics.Matrix3x2 matrix;
    }
}";
        private const string Valid_StructWithPrimitiveTypes = @"
namespace DiagnosticTests
{
    public struct StructWithAllValidFields
    {
        public bool boolean;
        public char character;
        public decimal dec;
        public double dbl;
        public float flt;
        public int i;
        public uint nat;
        public long lng;
        public ulong ulng;
        public short sh;
        public ushort us;
        public string str;
    }
}";
    }
}