using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestDiagnostics
{
    internal class MultidimensionalArraySignature_2D_PrivateClass_Valid
    {
        public int[,] Arr_2d { get; set; }
        public int[,,] Arr_3d { get; set; }
        private int[,] PrivArr_2d { get; set; }
        private int[,,] PrivArr_3d { get; set; }
    }

    public sealed class MultidimensionalArraySignature_3D_Valid
    {
        private int[,] PrivArr_2d { get; set; }
        private int[,,] PrivArr_3d { get; set; }
    }

    internal sealed class D2InternalPublic_Valid
    {
        public int[,] D2_ReturnOnly() { return new int[4, 2]; }
        public int[,] D2_ReturnAndInput1(int[,] arr) { return arr; }
        public int[,] D2_ReturnAndInput2of2(bool a, int[,] arr) { return arr; }
        public bool D2_NotReturnAndInput2of2(bool a, int[,] arr) { return a; }
        public bool D2_NotReturnAndInput2of3(bool a, int[,] arr, bool b) { return a; }
        public int[,] D2_ReturnAndInput2of3(bool a, int[,] arr, bool b) { return arr; }

    }

    internal sealed class D3InternalPublic_Valid
    {
        public int[,,] D3_ReturnOnly() { return new int[2, 1, 3] { { { 1, 1, 1 } }, { { 2, 2, 2 } } }; }
        public int[,,] D3_ReturnAndInput1(int[,,] arr) { return arr; }
        public int[,,] D3_ReturnAndInput2of2(bool a, int[,,] arr) { return arr; }
        public int[,,] D3_ReturnAndInput2of3(bool a, int[,,] arr, bool b) { return arr; }
        public bool D3_NotReturnAndInput2of2(bool a, int[,,] arr) { return a; }
        public bool D3_NotReturnAndInput2of3(bool a, int[,,] arr, bool b) { return a; }

    }

    public sealed class MultiDimPublicPrivate_Valid
    {
        private int[,,] D3_ReturnOnly() { return new int[2, 1, 3] { { { 1, 1, 1 } }, { { 2, 2, 2 } } }; }
        private int[,,] D3_ReturnAndInput1(int[,,] arr) { return arr; }
        private int[,,] D3_ReturnAndInput2of2(bool a, int[,,] arr) { return arr; }
        private int[,,] D3_ReturnAndInput2of3(bool a, int[,,] arr, bool b) { return arr; }
        private bool D3_NotReturnAndInput2of2(bool a, int[,,] arr) { return a; }
        private bool D3_NotReturnAndInput2of3(bool a, int[,,] arr, bool b) { return a; }

    }

    public interface MultiDimArrayTests_ValidInterface_NoMultiDim
    {
        int[] foo();
        bool bar(int[] arr);
    }

    /* 
     * Invalid tests include public properties in public classes, public interface methods, 
     */
    public sealed class MultidimensionalArraySignature_2D_Invalid
    {
        public int[,] Arr_2d { get; set; }
        public int[,,] Arr_3d { get; set; }
        private int[,] PrivArr_2d { get; set; } /* below should pass through undetected (private property) */
    }

    public sealed class D2PublicPublic_Invalid
    {
        public int[,] D2_ReturnOnly() { return new int[4, 2]; }
        public int[,] D2_ReturnAndInput1(int[,] arr) { return arr; }
        public int[,] D2_ReturnAndInput2of2(bool a, int[,] arr) { return arr; }
        public bool D2_NotReturnAndInput2of2(bool a, int[,] arr) { return a; }
        public bool D2_NotReturnAndInput2of3(bool a, int[,] arr, bool b) { return a; }
        public int[,] D2_ReturnAndInput2of3(bool a, int[,] arr, bool b) { return arr; }

    }

    public sealed class D3PublicPublic_Invalid
    {
        public int[,,] D3_ReturnOnly() { return new int[2, 1, 3] { { { 1, 1, 1 } }, { { 2, 2, 2 } } }; }
        public int[,,] D3_ReturnAndInput1(int[,,] arr) { return arr; }
        public int[,,] D3_ReturnAndInput2of2(bool a, int[,,] arr) { return arr; }
        public int[,,] D3_ReturnAndInput2of3(bool a, int[,,] arr, bool b) { return arr; }
        public bool D3_NotReturnAndInput2of2(bool a, int[,,] arr) { return a; }
        public bool D3_NotReturnAndInput2of3(bool a, int[,,] arr, bool b) { return a; }
    }

    public struct D2FieldInStruct
    {
        public int[,,] D3Method(bool b) { return new int[2, 1, 3] { { { 1, 1, 1 } }, { { 2, 2, 2 } } }; }
    }

    public interface D2MemberOfInterface_Invalid
    {
        public int[,] D2_ReturnOnly();
        public int[,] D2_ReturnAndInput1(int[,] arr);
        public int[,] D2_ReturnAndInput2of2(bool a, int[,] arr);
        public bool D2_NotReturnAndInput2of2(bool a, int[,] arr);
        public bool D2_NotReturnAndInput2of3(bool a, int[,] arr, bool b);
        public int[,] D2_ReturnAndInput2of3(bool a, int[,] arr, bool b);
    }

    public interface D3MemberOfInterface_Invalid
    {
        public int[,,] D3_ReturnOnly(); 
        public int[,,] D3_ReturnAndInput1(int[,,] arr); 
        public int[,,] D3_ReturnAndInput2of2(bool a, int[,,] arr);
        public int[,,] D3_ReturnAndInput2of3(bool a, int[,,] arr, bool b);
        public bool D3_NotReturnAndInput2of2(bool a, int[,,] arr);
        public bool D3_NotReturnAndInput2of3(bool a, int[,,] arr, bool b); 
    }

    namespace SubNamespace
    { 
        public interface SubNamespacInterface_D2Methods_Invalid
        { 
            public int[,] D2_ReturnOnly(); 
            public int[,] D2_ReturnAndInput1(int[,] arr); 
            public int[,] D2_ReturnAndInput2of2(bool a, int[,] arr);
            public int[,] D2_ReturnAndInput2of3(bool a, int[,] arr, bool b);
            public bool D2_NotReturnAndInput2of2(bool a, int[,] arr);
            public bool D2_NotReturnAndInput2of3(bool a, int[,] arr, bool b); 
        }

        public interface SubNamespaceInterface_D3Methods_Invalid
        { 
            public int[,,] D3_ReturnOnly(); 
            public int[,,] D3_ReturnAndInput1(int[,,] arr); 
            public int[,,] D3_ReturnAndInput2of2(bool a, int[,,] arr);
            public int[,,] D3_ReturnAndInput2of3(bool a, int[,,] arr, bool b);
            public bool D3_NotReturnAndInput2of2(bool a, int[,,] arr);
            public bool D3_NotReturnAndInput2of3(bool a, int[,,] arr, bool b); 
        }
    }
} // End TestDiagnostics namespace
