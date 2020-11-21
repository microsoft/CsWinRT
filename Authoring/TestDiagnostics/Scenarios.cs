using ABI.System.Numerics;
using ABI.Windows.ApplicationModel.Contacts;
using System;
using System.Runtime.ExceptionServices;
using Windows.Foundation;
using Windows.Foundation.Numerics;
using Windows.Web.Syndication;

namespace TestDiagnostics
{

    // valid method overload tests
    public sealed class TwoOverloads_DiffParamCount_OneAttribute_OneInList_Valid
    {

        // pretty sure diagnostic will get thrown for this right now BUT it shouldn't 

        // the fix could be to append the method name (as a string) with its arity (as a string) and add that as the key to the map
        // so the map isnt exactly the method name, but methodnameN where N is the arity of the methodname at that time
        // think this fix would mean no logic has to change on the "have we seen this methodname before and did it have the attribute"
        public string OverloadExample(string s) { return s; }

        public int OverloadExample(int n, int m) { return n; }
    }

    public sealed class TwoOverloads_OneAttribute_OneInList_Valid
    {

        [Windows.Foundation.Metadata.Deprecated("hu", Windows.Foundation.Metadata.DeprecationType.Deprecate, 1), 
         Windows.Foundation.Metadata.DefaultOverload()]
        public string OverloadExample(string s) { return s; }

        public int OverloadExample(int n) { return n; }
    }

    public sealed class TwoOverloads_OneAttribute_OneIrrelevatAttribute_Valid
    {

        [Windows.Foundation.Metadata.Deprecated("hu", Windows.Foundation.Metadata.DeprecationType.Deprecate, 1)]
        public string OverloadExample(string s) { return s; }

        [Windows.Foundation.Metadata.DefaultOverload()]
        public int OverloadExample(int n) { return n; }
    }

    public sealed class TwoOverloads_OneAttribute_TwoLists_Valid
    {

        [Windows.Foundation.Metadata.Deprecated("hu", Windows.Foundation.Metadata.DeprecationType.Deprecate, 1)]
        [Windows.Foundation.Metadata.DefaultOverload()]
        public string OverloadExample(string s) { return s; }

        public int OverloadExample(int n) { return n; }
    }

    public sealed class ThreeOverloads_OneAttribute_Valid
    {

        public string OverloadExample(string s) { return s; }

        [Windows.Foundation.Metadata.DefaultOverload()]
        public int OverloadExample(int n) { return n; }

        public bool OverloadExample(bool b) { return b; }
    }

    public sealed class ThreeOverloads_OneAttribute_2_Valid
    {
        public string OverloadExample(string s) { return s; }

        public int OverloadExample(int n) { return n; }

        [Windows.Foundation.Metadata.DefaultOverload()]
        public bool OverloadExample(bool b) { return b; }
    }

    public sealed class TwoOverloads_OneAttribute_3_Valid
    {
        public string OverloadExample(string s) { return s; }

        [Windows.Foundation.Metadata.DefaultOverload()]
        public int OverloadExample(int n) { return n; }
    }

    // invalid method overload tests 
    public sealed class TwoOverloads_NoAttribute_Invalid
    {
        public string OverloadExample(string s) { return s; }

        public int OverloadExample(int n) { return n; }
    }

    public sealed class TwoOverloads_TwoAttribute_OneInList_Invalid
    {

        [Windows.Foundation.Metadata.Deprecated("hu", Windows.Foundation.Metadata.DeprecationType.Deprecate, 1), 
         Windows.Foundation.Metadata.DefaultOverload()]
        public string OverloadExample(string s) { return s; } 

        [Windows.Foundation.Metadata.DefaultOverload()]
        public int OverloadExample(int n) { return n; }
    }

    public sealed class TwoOverloads_NoAttribute_OneIrrevAttr_Invalid
    {

        [Windows.Foundation.Metadata.Deprecated("hu", Windows.Foundation.Metadata.DeprecationType.Deprecate, 1)]
        public string OverloadExample(string s) { return s; }

        public int OverloadExample(int n) { return n; }
    }

    public sealed class TwoOverloads_TwoAttribute_BothInList_Invalid
    {

        [Windows.Foundation.Metadata.Deprecated("hu", Windows.Foundation.Metadata.DeprecationType.Deprecate, 1), 
         Windows.Foundation.Metadata.DefaultOverload()]
        public string OverloadExample(string s) { return s; }

        [Windows.Foundation.Metadata.Deprecated("hu", Windows.Foundation.Metadata.DeprecationType.Deprecate, 1), 
         Windows.Foundation.Metadata.DefaultOverload()]
        public int OverloadExample(int n) { return n; }
    }

    public sealed class TwoOverloads_TwoAttribute_TwoLists_Invalid
    {

        [Windows.Foundation.Metadata.Deprecated("hu", Windows.Foundation.Metadata.DeprecationType.Deprecate, 1)]
        [Windows.Foundation.Metadata.DefaultOverload()]
        public string OverloadExample(string s) { return s; } 

        [Windows.Foundation.Metadata.DefaultOverload()]
        public int OverloadExample(int n) { return n; }
    }
    
    public sealed class TwoOverloads_TwoAttribute_OneInSeparateList_OneNot_Invalid
    {

        [Windows.Foundation.Metadata.Deprecated("hu", Windows.Foundation.Metadata.DeprecationType.Deprecate, 1)]
        [Windows.Foundation.Metadata.DefaultOverload()]
        public string OverloadExample(string s) { return s; }

        [Windows.Foundation.Metadata.Deprecated("hu", Windows.Foundation.Metadata.DeprecationType.Deprecate, 1), 
         Windows.Foundation.Metadata.DefaultOverload()]
        public int OverloadExample(int n) { return n; }
    }

    public sealed class TwoOverloads_TwoAttribute_BothInSeparateList_Invalid
    {

        [Windows.Foundation.Metadata.Deprecated("hu", Windows.Foundation.Metadata.DeprecationType.Deprecate, 1)]
        [Windows.Foundation.Metadata.DefaultOverload()]
        public string OverloadExample(string s) { return s; }

        [Windows.Foundation.Metadata.Deprecated("hu", Windows.Foundation.Metadata.DeprecationType.Deprecate, 1)]
        [Windows.Foundation.Metadata.DefaultOverload()]
        public int OverloadExample(int n) { return n; }
    }

    public sealed class TwoOverloads_TwoAttribute_Invalid
    {

        [Windows.Foundation.Metadata.DefaultOverload()]
        public string OverloadExample(string s) { return s; }

        [Windows.Foundation.Metadata.DefaultOverload()]
        public int OverloadExample(int n) { return n; }
    }

    public sealed class ThreeOverloads_TwoAttributes_Invalid
    {
        public string OverloadExample(string s) { return s; }

        [Windows.Foundation.Metadata.DefaultOverload()]
        public int OverloadExample(int n) { return n; }

        [Windows.Foundation.Metadata.DefaultOverload()]
        public bool OverloadExample(bool b) { return b; }
    }


    // end overload method tests 

    public sealed class ClassThatOverloadsOperator
    {
        public static ClassThatOverloadsOperator operator +(ClassThatOverloadsOperator thing) // the rule needs update, it says "operator" instead of "+"
        {
            return thing;
        }
    }

    /*
    public sealed class SillyClass
    { 
        public double Identity(double d)
        {
            return d;
        }
    }

    public struct StructWithClass_Invalid
    {
        public SillyClass classField;
    }

    public struct StructWithDelegate_Invalid
    { 
        public delegate int ADelegate(int x);
    }

    public struct StructWithEvent_Invalid
    { 
        public event EventHandler<int> EventField;
    }

    public struct StructWithConstructor_Invalid
    {
        int X;
        StructWithConstructor_Invalid(int x)
        {
            X = x;
        }
    }
    
    public struct StructWithIndexer_Invalid
    {
        int[] arr;
        int this[int i] => arr[i];
    }
    
    public struct StructWithMethods_Invalid
    {
        int foo(int x)
        {
            return x;
        }
    }
    public struct StructWithConst_Invalid // really invalid ?
    {
        const int five = 5;
    }
    */
    
    /*
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
    */

    /*
    public struct StructWithPrivateField_Invalid
    {
        const int ci = 5;
        private int x;
    }
    */

    /*
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
    */

    /*
    public struct StructWithObjectField_Invalid // is this really invalid? 
    {
        object obj;

    }
    public struct StructWithDynamicField_Invalid // is this really invalid? 
    {
        dynamic dyn;
    }

    public struct StructWithByteField_Invalid // is this really invalid? 
    {
        byte b;
    }

    public struct StructWithWinRTStructField
    {
        public Matrix3x2 matrix;
    }
    */

    /*
    public sealed class ParameterNamedDunderRetVal
    {
        public int Identity(int __retval)
        {
            return __retval;
        }
    }
    */

    /*
    public sealed class SameArityConstructors
    {
        private int num;
        private string word;

        public SameArityConstructors(int i)
        {
            num = i;
            word = "dog";
        }
       
        public SameArityConstructors(string s)
        {
            num = 38;
            word = s;
        } 

        // Can test that multiple constructors are allowed (has been verified already, too)
        // as long as they have different arities by making one take none or 2 arguments 
    }
    */

    /* Would be nice to not have to comment out scenarios... perhaps a file for each case to test?  
    public sealed class AsyAction : IAsyncAction, IAsyncActionWithProgress<int>
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
    */
   
    /*
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
    */

    /*
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
    */
}
