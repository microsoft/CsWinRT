using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestDiagnostics
{
    /*
     * Valid tests
     */
    internal sealed class JaggedArray_Properties_Valid
    {
        private int[][] Arr { get; set; }
        public int[][] ArrP { get; set; }
        public int[][][] Arr3 { get; set; }
        private int[][][] Arr3P { get; set; }
    }

    internal sealed class J2InternalPublic_Valid
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
    internal sealed class J3InternalPublic_Valid
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

    public interface JaggedArrayTests_ValidInterface
    {
        int[] foo();
        bool bar(int[] arr);
    }

    public sealed class J3PublicPrivate_Valid
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

    /*
     * Invalid tests
     */
    
    public sealed class JaggedArray_Properties_Invalid
    {
        private int[][] ArrP { get; set; } 
        public int[][] Arr { get; set; }
        public int[][][] Arr3 { get; set; }
        private int[][][] Arr3P { get; set; }
    }

    public sealed class J2PublicPublic_Invalid
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

    public sealed class J3PublicPublic_Invalid
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
    
    public interface J2MemberOfInterface_Invalid
    {
        public int[][] J2_ReturnOnly();
        public int[][] J2_ReturnAndInput1(int[,] arr);
        public int[][] J2_ReturnAndInput2of2(bool a, int[][] arr);
        public bool J2_NotReturnAndInput2of2(bool a, int[][] arr);
        public bool J2_NotReturnAndInput2of3(bool a, int[][] arr, bool b);
        public int[][] J2_ReturnAndInput2of3(bool a, int[][] arr, bool b);
    }

    public interface J3MemberOfInterface_Invalid
    {
        public int[][][] J3_ReturnOnly();
        public int[][][] J3_ReturnAndInput1(int[][][] arr);
        public int[][][] J3_ReturnAndInput2of2(bool a, int[][][] arr);
        public int[][][] J3_ReturnAndInput2of3(bool a, int[][][] arr, bool b);
        public bool J3_NotReturnAndInput2of2(bool a, int[][][] arr);
        public bool J3_NotReturnAndInput2of3(bool a, int[][][] arr, bool b);
    }

    namespace SubNamespace
    { 
        public interface SubNamespaceInterface_J2Methods_Invalid
        {
            public int[][] J2_ReturnOnly();
            public int[][] J2_ReturnAndInput1(int[,] arr);
            public int[][] J2_ReturnAndInput2of2(bool a, int[][] arr);
            public bool J2_NotReturnAndInput2of2(bool a, int[][] arr);
            public bool J2_NotReturnAndInput2of3(bool a, int[][] arr, bool b);
            public int[][] J2_ReturnAndInput2of3(bool a, int[][] arr, bool b);
        }
        
        public interface SubNamespaceInterface_J3Methods_Invalid
        {
            public int[][][] J3_ReturnOnly(); 
            public int[][][] J3_ReturnAndInput1(int[][][] arr); 
            public int[][][] J3_ReturnAndInput2of2(bool a, int[][][] arr);
            public int[][][] J3_ReturnAndInput2of3(bool a, int[][][] arr, bool b);
            public bool J3_NotReturnAndInput2of2(bool a, int[][][] arr);
            public bool J3_NotReturnAndInput2of3(bool a, int[][][] arr, bool b); 
        }
    } // end SubNamespace
} 

