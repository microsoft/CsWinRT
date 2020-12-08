using System.Runtime.InteropServices;

namespace TestDiagnostics
{
    /* TODO: 
     * what happens if you put another random attribute on an array param? 
     * check in WRC3 project ...
    */
    public class ReadOnlyArray : System.Attribute
    {
        public ReadOnlyArray() { }
    }

    public class WriteOnlyArray : System.Attribute
    {
        public WriteOnlyArray() { }
    }

    /*
    public interface IHaveAMethodNamedArray_Valid
    {
        void Array(int i);
        int[] Foo(out int[] arr);
    }

    public sealed class OutParam_Valid
    {
        public void Foo(out int[] arr) { arr = new int[] { }; }
    }
    
    // method with `ref` param 
    public sealed class OnlyParam
    {
        //  array param with both attributes 
        public void BothAttributes_Separate([WriteOnlyArray()][ReadOnlyArray()] int[] arr) { }

        public void BothAttributes_Together([WriteOnlyArray(), ReadOnlyArray()] int[] arr) { }

        // array marked `out` but marked with ReadOnlyArray Attribute
        public void MarkedOutAndReadOnly([ReadOnlyArray()] out int[] arr) { arr = new int[] { }; }
        public void MarkedReadOnly_Valid([ReadOnlyArray()] int[] arr) { }

        // Valid WriteOnlyArray Tests
        public void MarkedWriteOnly_Valid([WriteOnlyArray()] int[] arr) { }
        public void MarkedOutAndWriteOnly_Valid([WriteOnlyArray()] out int[] arr) { arr = new int[] { }; }
        public void MarkedOutOnly_Valid(out int[] arr) { arr = new int[] { }; }

        // param is array, and marked either InAttribute or OutAttribute
        // must have ReadOnlyArray or WriteOnlyArray
        public void ArrayMarkedIn([In] int[] arr) { }
        public void ArrayMarkedOut([Out] int[] arr) { }

        // method has param marked with  ReadOnlyArray / WriteOnlyArray 
        //  but param isnt array
        public void NonArrayMarkedReadOnly([ReadOnlyArray()] int arr) { }
        public void NonArrayMarkedWriteOnly([WriteOnlyArray()] int arr) { }

        // param marked InAttribute or OutAttribute , disallowed in total  
        public void ParamMarkedIn([In] int arr) { }
        public void ParamMarkedOut([Out] int arr) { }

        // array as param but not marked either way
        public void ArrayNotMarked(int[] arr) { }

        public void ArrayNotMarked_Valid(out int[] arr) { arr = new int[] { };  }
    }

    public sealed class TwoParam
    { 
         public void BothAttributes(int i, [WriteOnlyArray()][ReadOnlyArray()] int[] arr) { }
        // array marked `out` but marked with ReadOnlyArray Attribute
        public void MarkedOutAndReadOnly(int i, [ReadOnlyArray()] out int[] arr) { arr = new int[] { }; }
        public void MarkedReadOnly_Valid(int i, [ReadOnlyArray()] int[] arr) { }

        // Valid WriteOnlyArray Tests
        public void MarkedWriteOnly_Valid(int i, [WriteOnlyArray()] int[] arr) { }
        public void MarkedOutAndWriteOnly_Valid(int i, [WriteOnlyArray()] out int[] arr) { arr = new int[] { }; }
        public void MarkedOut_Valid(int i, out int[] arr) { arr = new int[] { }; }

        // param is array, and marked either InAttribute or OutAttribute
        // must have ReadOnlyArray or WriteOnlyArray
        public void ArrayMarkedIn(int i, [In] int[] arr) { }
        public void ArrayMarkedOut(int i, [Out] int[] arr) { }

        // method has param marked with  ReadOnlyArray / WriteOnlyArray 
        //  but param isnt array
        public void NonArrayMarkedReadOnly(int i, [ReadOnlyArray()] int arr) { }
        public void NonArrayMarkedWriteOnly(int i, [WriteOnlyArray()] int arr) { }

        // param marked InAttribute or OutAttribute , disallowed in total  
        public void ParamMarkedIn(int i, [In] int arr) { } // hey!
        public void ParamMarkedOut(int i, [Out] int arr) { }

        // array as param but not marked either way
        public void ArrayNotMarked(int i, int[] arr) { }
    }
    
    public sealed class TwoArrays
    { 
        public void OneValidOneInvalid_1([WriteOnlyArray] int[] xs, [WriteOnlyArray()][ReadOnlyArray()] int[] ys) { }
        public void OneValidOneInvalid_2([WriteOnlyArray][ReadOnlyArray()] int[] xs, [WriteOnlyArray()] int[] ys) { }

        // array marked `out` but marked with ReadOnlyArray Attribute
        public void MarkedOutAndReadOnly([ReadOnlyArray()] int[] xs, [ReadOnlyArray()] out int[] arr) { arr = new int[] { }; }
        public void MarkedReadOnly_Valid([ReadOnlyArray()] int[] xs, [ReadOnlyArray()] int[] arr) { }

        // Valid WriteOnlyArray Tests
        public void MarkedWriteOnly_Valid([ReadOnlyArray()] int[] xs, [WriteOnlyArray()] int[] arr) { }
        public void MarkedOutAndWriteOnly_Valid([ReadOnlyArray()] int[] xs, [WriteOnlyArray()] out int[] arr) { arr = new int[] { }; }
        public void MarkedOut_Valid([WriteOnlyArray()] int[] xs, out int[] arr) { arr = new int[] { }; }

        // param is array, and marked either InAttribute or OutAttribute
        // must have ReadOnlyArray or WriteOnlyArray
        public void ArrayMarkedIn(int i, [In] int[] arr) { }
        public void ArrayMarkedIn2([ReadOnlyArray()] int[] xs, [In] int[] arr) { }
        public void ArrayMarkedOut([ReadOnlyArray()] int[] xs, [Out] int[] arr) { }

        // method has param marked with  ReadOnlyArray / WriteOnlyArray but param isnt array
        public void NonArrayMarkedReadOnly([ReadOnlyArray()] int[] xs, [ReadOnlyArray()] int i) { }
        public void NonArrayMarkedWriteOnly([ReadOnlyArray()] int[] xs, [WriteOnlyArray()] int i) { }
        public void NonArrayMarkedWriteOnly2([ReadOnlyArray()] int i, [WriteOnlyArray()] int[] arr) { }

        // param marked InAttribute or OutAttribute , disallowed in total  
        public void ParamMarkedIn([ReadOnlyArray()] int[] xs, [In] int arr) { }
        public void ParamMarkedOut([ReadOnlyArray()] int[] xs, [Out] int arr) { }
        public void ParamMarkedOut2([Out] int arr, [ReadOnlyArray()] int[] xs) { }

        // array as param but not marked either way
        public void ArrayNotMarked([ReadOnlyArray()] int[] xs, int[] arr) { }
    }
    */
}
