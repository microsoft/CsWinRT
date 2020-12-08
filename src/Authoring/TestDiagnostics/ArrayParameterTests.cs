﻿using ABI.Windows.Foundation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace TestDiagnostics
{
    /* TODO: 
     * what happens if you put another random attribute on an array param? 
     * check in WRC3 project ...
    */
    
    public interface IHaveAMethodWithRefParam
    {
        void foo(ref int i);
    }

    public interface IHaveAMethodNamedArray
    {
        void Array(int i); // shouldn't get hit, but might
    }

    // method with `ref` param 
    public sealed class OnlyParam
    {
        // todo: move this method/test out into a different file
        public void MethodWithRefParam(ref int i) { i++; }

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
        public void ParamMarkedIn(int i, [In] int arr) { }
        public void ParamMarkedOut(int i, [Out] int arr) { }

        // array as param but not marked either way
        public void ArrayNotMarked(int i, int[] arr) { }
    }
}
