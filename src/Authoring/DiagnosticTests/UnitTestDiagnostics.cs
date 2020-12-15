using NUnit.Framework;
using Microsoft.CodeAnalysis;
using WinRT.SourceGenerator;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.Linq;

namespace DiagnosticTests
{
    
    /*
         *  todo fix the `using` statement, based on Mano's PR comment 
         *  ********************
        
        private const string StructWithWinRTField = @"
using Some.Namespace
namespace Test
{
public struct StructWithWinRTStructField
    {
        public Matrix3x2 matrix;
    }
}";
         */

    [TestFixture]
    public partial class TestDiagnostics
    {

        /// <summary>
        /// CheckNoDiagnostic asserts that no diagnostics are raised on the 
        /// compilation produced from the cswinrt source generator based on the given source code
        /// 
        /// Add unit tests by creating a source code like this:
        /// private const string MyNewTest = @"namespace Test { ... }";
        /// 
        /// And have a DiagnosticDescriptor for the one to check for, they live in WinRT.SourceGenerator.DiagnosticRules
        /// 
        /// Then go to the DiagnosticValidData class here and add an entry for it
        /// </summary>
        /// <param name="source"></param>
        [Test, TestCaseSource(nameof(ValidCases))] 
        public void CheckNoDiagnostic(string source)
        { 
            Compilation compilation = CreateCompilation(source);
            RunGenerators(compilation, out var diagnosticsFound,  new Generator.SourceGenerator()); 
            Assert.That(diagnosticsFound.IsEmpty);
        }

        /// <summary>
        /// CodeHasDiagnostic takes some source code (string) and a Diagnostic descriptor, 
        /// it checks that a diagnostic with the same description was raised by the source generator 
        /// </summary> 
        [Test, TestCaseSource(nameof(WinRTComponentCases))]
        public void CodeHasDiagnostics(string testCode, params DiagnosticDescriptor[] rules)
        { 
            Compilation compilation = CreateCompilation(testCode);
            RunGenerators(compilation, out var diagnosticsFound,  new Generator.SourceGenerator());
            HashSet<DiagnosticDescriptor> diagDescsFound = new HashSet<DiagnosticDescriptor>();
            
            foreach (var d in diagnosticsFound)
            {
                diagDescsFound.Add(d.Descriptor);
            }

            foreach (var rule in rules)
            { 
                Assert.That(diagDescsFound.Contains(rule));
            }
        }

        private static IEnumerable<TestCaseData> WinRTComponentCases
        {
            get 
            {
                yield return new TestCaseData(ConstructorsOfSameArity, DiagnosticRules.ClassConstructorRule).SetName("Multiple constructors of same arity");
                yield return new TestCaseData(ImplementsIAsyncOperation, DiagnosticRules.AsyncRule).SetName("Implements IAsyncOperation");
                yield return new TestCaseData(ImplementsIAsyncOperationWithProgress, DiagnosticRules.AsyncRule).SetName("Implements IAsyncOperationWithProgress");
                yield return new TestCaseData(ImplementsIAsyncAction, DiagnosticRules.AsyncRule).SetName("Implements IAsyncAction");
                yield return new TestCaseData(ImplementsIAsyncActionWithProgress, DiagnosticRules.AsyncRule).SetName("Implements IAsyncActionWithProgress");
                yield return new TestCaseData(TestArrayParamAttrUnary_1, DiagnosticRules.ArrayParamMarkedBoth).SetName("TestArrayParamAttrUnary_1");
                yield return new TestCaseData(TestArrayParamAttrUnary_2, DiagnosticRules.ArrayParamMarkedBoth).SetName("TestArrayParamAttrUnary_2");
                yield return new TestCaseData(TestArrayParamAttrUnary_3, DiagnosticRules.ArrayOutputParamMarkedRead).SetName("TestArrayParamAttrUnary_3");
                yield return new TestCaseData(TestArrayParamAttrUnary_4, DiagnosticRules.ArrayMarkedInOrOut).SetName("TestArrayParamAttrUnary_4");
                yield return new TestCaseData(TestArrayParamAttrUnary_5, DiagnosticRules.ArrayMarkedInOrOut).SetName("TestArrayParamAttrUnary_5");
                yield return new TestCaseData(TestArrayParamAttrUnary_6, DiagnosticRules.NonArrayMarked).SetName("TestArrayParamAttrUnary_6");
                yield return new TestCaseData(TestArrayParamAttrUnary_7, DiagnosticRules.NonArrayMarked).SetName("TestArrayParamAttrUnary_7");
                yield return new TestCaseData(TestArrayParamAttrUnary_8, DiagnosticRules.NonArrayMarkedInOrOut).SetName("TestArrayParamAttrUnary_8");
                yield return new TestCaseData(TestArrayParamAttrUnary_9, DiagnosticRules.NonArrayMarkedInOrOut).SetName("TestArrayParamAttrUnary_9");
                yield return new TestCaseData(TestArrayParamAttrUnary_10, DiagnosticRules.ArrayParamNotMarked).SetName("TestArrayParamAttrUnary_10");
                yield return new TestCaseData(TestArrayParamAttrBinary_1, DiagnosticRules.ArrayParamMarkedBoth).SetName("TestArrayParamAttrBinary_1");
                yield return new TestCaseData(TestArrayParamAttrBinary_2, DiagnosticRules.ArrayParamMarkedBoth).SetName("TestArrayParamAttrBinary_2");
                yield return new TestCaseData(TestArrayParamAttrBinary_3, DiagnosticRules.ArrayOutputParamMarkedRead).SetName("TestArrayParamAttrBinary_3");
                yield return new TestCaseData(TestArrayParamAttrBinary_4, DiagnosticRules.ArrayMarkedInOrOut).SetName("TestArrayParamAttrBinary_4");
                yield return new TestCaseData(TestArrayParamAttrBinary_5, DiagnosticRules.ArrayMarkedInOrOut).SetName("TestArrayParamAttrBinary_5");
                yield return new TestCaseData(TestArrayParamAttrBinary_6, DiagnosticRules.NonArrayMarked).SetName("TestArrayParamAttrBinary_6");
                yield return new TestCaseData(TestArrayParamAttrBinary_7, DiagnosticRules.NonArrayMarked).SetName("TestArrayParamAttrBinary_7");
                yield return new TestCaseData(TestArrayParamAttrBinary_8, DiagnosticRules.NonArrayMarkedInOrOut).SetName("TestArrayParamAttrBinary_8");
                yield return new TestCaseData(TestArrayParamAttrBinary_9, DiagnosticRules.NonArrayMarkedInOrOut).SetName("TestArrayParamAttrBinary_9");
                yield return new TestCaseData(TestArrayParamAttrBinary_10, DiagnosticRules.ArrayParamNotMarked).SetName("TestArrayParamAttrBinary_10");
                yield return new TestCaseData(TestArrayParamAttrBinary_11, DiagnosticRules.ArrayParamMarkedBoth).SetName("TestArrayParamAttrBinary_11");
                yield return new TestCaseData(TestArrayParamAttrBinary_12, DiagnosticRules.ArrayParamMarkedBoth).SetName("TestArrayParamAttrBinary_12");
                yield return new TestCaseData(TestArrayParamAttrBinary_13, DiagnosticRules.ArrayOutputParamMarkedRead).SetName("TestArrayParamAttrBinary_13");
                yield return new TestCaseData(TestArrayParamAttrBinary_14, DiagnosticRules.ArrayMarkedInOrOut).SetName("TestArrayParamAttrBinary_14");
                yield return new TestCaseData(TestArrayParamAttrBinary_15, DiagnosticRules.ArrayMarkedInOrOut).SetName("TestArrayParamAttrBinary_15");
                yield return new TestCaseData(TestArrayParamAttrBinary_16, DiagnosticRules.ArrayMarkedInOrOut).SetName("TestArrayParamAttrBinary_16");
                yield return new TestCaseData(TestArrayParamAttrBinary_17, DiagnosticRules.ArrayParamNotMarked).SetName("TestArrayParamAttrBinary_17");
                yield return new TestCaseData(TestArrayParamAttrBinary_18, DiagnosticRules.NonArrayMarked).SetName("TestArrayParamAttrBinary_18");
                yield return new TestCaseData(TestArrayParamAttrBinary_19, DiagnosticRules.NonArrayMarked).SetName("TestArrayParamAttrBinary_19");
                yield return new TestCaseData(TestArrayParamAttrBinary_20, DiagnosticRules.NonArrayMarked).SetName("TestArrayParamAttrBinary_20");
                yield return new TestCaseData(TestArrayParamAttrBinary_21, DiagnosticRules.NonArrayMarkedInOrOut).SetName("TestArrayParamAttrBinary_21");
                yield return new TestCaseData(TestArrayParamAttrBinary_22, DiagnosticRules.NonArrayMarkedInOrOut).SetName("TestArrayParamAttrBinary_22");
                yield return new TestCaseData(TestArrayParamAttrBinary_23, DiagnosticRules.NonArrayMarkedInOrOut).SetName("TestArrayParamAttrBinary_23");
                yield return new TestCaseData(TestArrayParamAttrBinary_24, DiagnosticRules.ArrayParamNotMarked).SetName("TestArrayParamAttrBinary_24");
                yield return new TestCaseData(DunderRetValParam, DiagnosticRules.ParameterNamedValueRule).SetName("Test Parameter Name Conflict (__retval)");
                yield return new TestCaseData(OperatorOverload_Class, DiagnosticRules.OperatorOverloadedRule).SetName("Test Overload of Operator");
                yield return new TestCaseData(RefParam_ClassMethod, DiagnosticRules.RefParameterFound).SetName("Test For Method With Ref Param - Class");
                yield return new TestCaseData(RefParam_InterfaceMethod, DiagnosticRules.RefParameterFound).SetName("Test For Method With Ref Param - Interface");
                yield return new TestCaseData(StructWithClassField, DiagnosticRules.StructHasInvalidFieldRule).SetName("Struct with Class Field");
                yield return new TestCaseData(StructWithDelegateField, DiagnosticRules.StructHasInvalidFieldRule).SetName("Struct with Delegate Field");
                yield return new TestCaseData(StructWithIndexer, DiagnosticRules.StructHasInvalidFieldRule).SetName("Struct with Indexer Field");
                yield return new TestCaseData(StructWithMethods, DiagnosticRules.StructHasInvalidFieldRule).SetName("Struct with Method Field");
                yield return new TestCaseData(StructWithConst, DiagnosticRules.StructHasInvalidFieldRule).SetName("Struct with Const Field");
                yield return new TestCaseData(StructWithProperty, DiagnosticRules.StructHasInvalidFieldRule).SetName("Struct with Property Field");
                yield return new TestCaseData(StructWithPrivateField, DiagnosticRules.StructHasInvalidFieldRule).SetName("Struct with Private Field");
                yield return new TestCaseData(StructWithObjectField, DiagnosticRules.StructHasInvalidFieldRule).SetName("Struct with Object Field");
                yield return new TestCaseData(StructWithDynamicField, DiagnosticRules.StructHasInvalidFieldRule).SetName("Struct with Dynamic Field");
                yield return new TestCaseData(StructWithByteField, DiagnosticRules.StructHasInvalidFieldRule).SetName("Struct with Byte Field");
                yield return new TestCaseData(StructWithConstructor, DiagnosticRules.StructHasInvalidFieldRule).SetName("Struct with Constructor Field");
            }
        }

        #region ValidTests
 
        private static IEnumerable<TestCaseData> ValidCases
        {
            get
            {
                yield return new TestCaseData(Valid_ArrayParamAttrUnary_1).SetName("Valid - ArrayParamAttrUnary_1");
                yield return new TestCaseData(Valid_ArrayParamAttrUnary_2).SetName("Valid - ArrayParamAttrUnary_2");
                yield return new TestCaseData(Valid_ArrayParamAttrUnary_3).SetName("Valid - ArrayParamAttrUnary_3");
                yield return new TestCaseData(Valid_ArrayParamAttrUnary_4).SetName("Valid - ArrayParamAttrUnary_4");
                yield return new TestCaseData(Valid_ArrayParamAttrUnary_5).SetName("Valid - ArrayParamAttrUnary_5");
                yield return new TestCaseData(Valid_ArrayParamAttrBinary_1).SetName("Valid - ArrayParamAttrBinary_1");
                yield return new TestCaseData(Valid_ArrayParamAttrBinary_2).SetName("Valid - ArrayParamAttrBinary_2");
                yield return new TestCaseData(Valid_ArrayParamAttrBinary_3).SetName("Valid - ArrayParamAttrBinary_3");
                yield return new TestCaseData(Valid_ArrayParamAttrBinary_4).SetName("Valid - ArrayParamAttrBinary_4");
                yield return new TestCaseData(Valid_ArrayParamAttrBinary_5).SetName("Valid - ArrayParamAttrBinary_5");
                yield return new TestCaseData(Valid_ArrayParamAttrBinary_6).SetName("Valid - ArrayParamAttrBinary_6");
                yield return new TestCaseData(Valid_ArrayParamAttrBinary_7).SetName("Valid - ArrayParamAttrBinary_7");
                yield return new TestCaseData(Valid_ArrayParamAttrBinary_8).SetName("Valid - ArrayParamAttrBinary_8");
                yield return new TestCaseData(Valid_ArrayParamAttrBinary_9).SetName("Valid - ArrayParamAttrBinary_9");
                yield return new TestCaseData(StructWithAllValidFields).SetName("Valid - Struct with only fields of basic types");
            }
        }

        private const string Valid_ArrayParamAttrUnary_1 = @"
namespace TestNamespace 
{
  public sealed class OnlyParam
  { 
    public int GetSum([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] arr) { return 0; } 
  }
}";
        private const string Valid_ArrayParamAttrUnary_2 = @"
namespace TestNamespace 
{
  public sealed class OnlyParam
  { 
     public void MarkedWriteOnly_Valid([System.Runtime.InteropServices.WindowsRuntime.WriteOnlyArray] int[] arr) { }
  }
}";
        private const string Valid_ArrayParamAttrUnary_3 = @"
namespace TestNamespace 
{
  public sealed class OnlyParam
  { 
        public void MarkedOutAndWriteOnly_Valid([System.Runtime.InteropServices.WindowsRuntime.WriteOnlyArray] out int[] arr) { arr = new int[] { }; }
  }
}";
        private const string Valid_ArrayParamAttrUnary_4 = @"
namespace TestNamespace 
{
  public sealed class OnlyParam
  { 
        public void MarkedOutOnly_Valid(out int[] arr) { arr = new int[] { }; }
  }
}";
        private const string Valid_ArrayParamAttrUnary_5 = @"
namespace TestNamespace 
{
  public sealed class OnlyParam
  { 
        public void ArrayNotMarked_Valid(out int[] arr) { arr = new int[] { };  }
  }
}";
        private const string Valid_ArrayParamAttrBinary_1 = @"
namespace TestNamespace 
{
  public sealed class TwoParam
  { 
    public int GetSum(int i, [System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] arr) { return 0; } 
  }
}";
        private const string Valid_ArrayParamAttrBinary_2 = @"
namespace TestNamespace 
{
  public sealed class TwoParam
  { 
     public void MarkedWriteOnly_Valid(int i, [System.Runtime.InteropServices.WindowsRuntime.WriteOnlyArray] int[] arr) { }
  }
}";
        private const string Valid_ArrayParamAttrBinary_3 = @"
namespace TestNamespace 
{
  public sealed class TwoParam
  { 
        public void MarkedOutAndWriteOnly_Valid(int i, [System.Runtime.InteropServices.WindowsRuntime.WriteOnlyArray] out int[] arr) { arr = new int[] { }; }
  }
}";
        private const string Valid_ArrayParamAttrBinary_4 = @"
namespace TestNamespace 
{
  public sealed class TwoParam
  { 
        public void MarkedOutOnly_Valid(int i, out int[] arr) { arr = new int[] { }; }
  }
}";
        private const string Valid_ArrayParamAttrBinary_5 = @"
namespace TestNamespace 
{
  public sealed class TwoParam
  { 
        public void ArrayNotMarked_Valid(int i, out int[] arr) { arr = new int[] { };  }
  }
}";
        private const string Valid_ArrayParamAttrBinary_6 = @"
namespace TestNamespace 
{
  public sealed class TwoArray
  { 
        public void MarkedReadOnly_Valid([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] xs, [System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] arr) { }
  }
}";
        private const string Valid_ArrayParamAttrBinary_7 = @"
namespace TestNamespace 
{
  public sealed class TwoArray
  { 
        public void MarkedWriteOnly_Valid([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] xs, [System.Runtime.InteropServices.WindowsRuntime.WriteOnlyArray] int[] arr) { }
  }
}";
        private const string Valid_ArrayParamAttrBinary_8 = @"
namespace TestNamespace 
{
  public sealed class TwoArray
  { 
        public void MarkedOutAndWriteOnly_Valid([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] xs, [System.Runtime.InteropServices.WindowsRuntime.WriteOnlyArray] out int[] arr) { arr = new int[] { }; }
  }
}";
        private const string Valid_ArrayParamAttrBinary_9 = @"
namespace TestNamespace 
{
  public sealed class TwoArray
  { 
        public void MarkedOut_Valid([System.Runtime.InteropServices.WindowsRuntime.WriteOnlyArray] int[] xs, out int[] arr) { arr = new int[] { }; }
  }
}";
        private const string StructWithAllValidFields = @"
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

        #endregion

        #region DataTests

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
        private const string OperatorOverload_Class = @"
    public sealed class ClassThatOverloadsOperator
    {
        public static ClassThatOverloadsOperator operator +(ClassThatOverloadsOperator thing)
        {
            return thing;
        }
    }";
        private const string DunderRetValParam = @"
public sealed class ParameterNamedDunderRetVal
    {
        public int Identity(int __retval)
        {
            return __retval;
        }
    }
";
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

        #endregion
    }
}