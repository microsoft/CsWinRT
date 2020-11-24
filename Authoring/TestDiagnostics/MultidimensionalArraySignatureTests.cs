using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestDiagnostics
{
    /* ** multidimensional array  ** */

    // public property in class
    public sealed class MultidimensionalArraySignature_2D_Invalid
    {
        public int[,] Arr { get; set; }
    }

    public sealed class MultidimensionalArraySignature_3D_Invalid
    {
        public int[,,] Arr { get; set; }
    }

    // positive test - private property in class
    public sealed class MultidimensionalArraySignature_2D_PrivateProperty_Valid
    {
        private int[,] Arr { get; set; }
    }

    // we don't care about private or internal classes or methods 

    public sealed class MultidimensionalArraySignature_3D_PrivateProperty_Valid
    {
        private int[,,] Arr { get; set; }
    }

    internal class MultidimensionalArraySignature_2D_PrivateClass_Valid
    {
        public int[,] Arr { get; set; }
    }

    internal sealed class MultidimensionalArraySignature_3D_PrivateClass_Valid
    {
        public int[,,] Arr { get; set; }
    }

    // positive test - private property in class
    internal sealed class MultidimensionalArraySignature_2D_PrivateProperty_PrivateClass_Valid
    {
        private int[,] Arr { get; set; }
    }

    // we don't care about private or internal classes or methods 

    internal sealed class MultidimensionalArraySignature_3D_PrivateProperty_PrivateClass_Valid
    {
        private int[,,] Arr { get; set; }
    }

    // public methods
    // return type
    // parameters, 1 and 2 and 3
    public sealed class D2PublicPublic
    {
        public int[,] D2_ReturnOnly() { return new int[4, 2]; }
        public int[,] D2_ReturnAndInput1(int[,] arr) { return arr; }
        public int[,] D2_ReturnAndInput2of2(bool a, int[,] arr) { return arr; }
        public bool D2_NotReturnAndInput2of2(bool a, int[,] arr) { return a; }
        public bool D2_NotReturnAndInput2of3(bool a, int[,] arr, bool b) { return a; }
        public int[,] D2_ReturnAndInput2of3(bool a, int[,] arr, bool b) { return arr; }

    }

    /*
    private sealed class D2PrivatePublic
    {
        public int[,] D2_ReturnOnly() { return new int[4, 2]; }
        public int[,] D2_ReturnAndInput1(int[,] arr) { return arr; }
        public int[,] D2_ReturnAndInput2of2(bool a, int[,] arr) { return arr; }
        public bool D2_NotReturnAndInput2of2(bool a, int[,] arr) { return a; }
        public bool D2_NotReturnAndInput2of3(bool a, int[,] arr, bool b) { return a; }
        public int[,] D2_ReturnAndInput2of3(bool a, int[,] arr, bool b) { return arr; }

    }
    */

    public sealed class D3PublicPublic
    {
        public int[,,] D3_ReturnOnly() { return new int[2, 1, 3] { { { 1, 1, 1 } }, { { 2, 2, 2 } } }; }
        public int[,,] D3_ReturnAndInput1(int[,,] arr) { return arr; }
        public int[,,] D3_ReturnAndInput2of2(bool a, int[,,] arr) { return arr; }
        public int[,,] D3_ReturnAndInput2of3(bool a, int[,,] arr, bool b) { return arr; }
        public bool D3_NotReturnAndInput2of2(bool a, int[,,] arr) { return a; }
        public bool D3_NotReturnAndInput2of3(bool a, int[,,] arr, bool b) { return a; }

    }
    public void Foo(int[,] arr);
    public void Foo2(int[,,] arr);
    // return type and parameters

    // positive test - private method 

    // field in struct -- think this will get covered already, but we'll see

    // declared (public) in interface

    // positive test - private in interface


}
