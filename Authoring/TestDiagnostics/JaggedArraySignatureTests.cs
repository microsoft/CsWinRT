using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestDiagnostics
{
    /* ** jagged array  **  */

    // public property in class
    public sealed class JaggedArray_Properties_Invalid
    {
        private int[][] Arr { get; set; }
        public int[][] ArrP { get; set; }
        public int[][][] Arr3 { get; set; }
        private int[][][] Arr3P { get; set; }
    }

    internal sealed class JaggedArray_Properties_Valid
    {
        private int[][] Arr { get; set; }
        public int[][] ArrP { get; set; }
        public int[][][] Arr3 { get; set; }
        private int[][][] Arr3P { get; set; }
    }


    // tests return type and paramteter cases for 2-dimensional arrays 
    // we expect diagnostics to be raised since the methods are public
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

    // tests return type and paramteter cases for 3-dimensional arrays 
    // we expect diagnostics to be raised since the methods are public
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

    // tests return type and paramteter cases for 3-dimensional arrays 
    // we expect normal compilation since the methods are private 
    public sealed class J3PublicPrivate_Invalid
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
 
    public interface J2MemberOfInterface_Invalid
    {
        public int[][] J2_ReturnOnly();
        public int[][] J2_ReturnAndInput1(int[,] arr);
        public int[][] J2_ReturnAndInput2of2(bool a, int[][] arr);
        public bool J2_NotReturnAndInput2of2(bool a, int[][] arr);
        public bool J2_NotReturnAndInput2of3(bool a, int[][] arr, bool b);
        public int[][] J2_ReturnAndInput2of3(bool a, int[][] arr, bool b);
    }

    public interface J2MemberOfInterface_Valid
    {
        private int[][] J2_ReturnOnly() 
        {
            int[][] arr = new int[2][];
            arr[0] = new int[1] { 1 };
            arr[1] = new int[1] { 2 };
            return arr;
        }
        private int[][] J2_ReturnAndInput1(int[][] arr) { return arr; }
        private int[][] J2_ReturnAndInput2of2(bool a, int[][] arr) { return arr; }
        private bool J2_NotReturnAndInput2of2(bool a, int[][] arr) { return a; }
        private bool J2_NotReturnAndInput2of3(bool a, int[][] arr, bool b) { return a; }
        private int[][] J2_ReturnAndInput2of3(bool a, int[][] arr, bool b) { return arr; }

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
    
    public interface J3MemberOfInterface_Valid
    {
        private int[][][] J3_ReturnOnly() {
            int[][] arr2 = new int[2][];
            arr2[0] = new int[1] { 1 };
            arr2[1] = new int[1] { 2 };

            int[][][] arr = new int[1][][];
            arr[0] = arr2;
            return arr; 
        }
        private int[][][] J3_ReturnAndInput1(int[][][] arr) { return arr; }
        private int[][][] J3_ReturnAndInput2of2(bool a, int[][][] arr) { return arr; }
        private int[][][] J3_ReturnAndInput2of3(bool a, int[][][] arr, bool b) { return arr; }
        private bool J3_NotReturnAndInput2of2(bool a, int[][][] arr) { return a; }
        private bool J3_NotReturnAndInput2of3(bool a, int[][][] arr, bool b) { return a; }
    }
}

