using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestDiagnostics
{
    /* ** multidimensional array  ** */

    /*
     * Properties in Class 
     */
    public sealed class MultidimensionalArraySignature_2D_Invalid
    {
        public int[,] Arr { get; set; }
        private int[,] PrivArr { get; set; }

    }

    public sealed class MultidimensionalArraySignature_3D_Invalid
    {
        public int[,,] Arr { get; set; }
        private int[,,] PrivArr { get; set; }
    }

    // we don't care about private or internal methods or classes

    internal class MultidimensionalArraySignature_2D_PrivateClass_Valid
    {
        public int[,] Arr { get; set; }
        private int[,] PrivArr { get; set; }
    }

    internal sealed class MultidimensionalArraySignature_3D_PrivateClass_Valid
    {
        public int[,,] Arr { get; set; }
        private int[,,] PrivArr { get; set; }
    }


    // tests return type and paramteter cases for 2-dimensional arrays 
    // we expect diagnostics to be raised since the methods are public
    public sealed class D2PublicPublic
    {
        public int[,] D2_ReturnOnly() { return new int[4, 2]; }
        public int[,] D2_ReturnAndInput1(int[,] arr) { return arr; }
        public int[,] D2_ReturnAndInput2of2(bool a, int[,] arr) { return arr; }
        public bool D2_NotReturnAndInput2of2(bool a, int[,] arr) { return a; }
        public bool D2_NotReturnAndInput2of3(bool a, int[,] arr, bool b) { return a; }
        public int[,] D2_ReturnAndInput2of3(bool a, int[,] arr, bool b) { return arr; }

    }

    internal sealed class D2InternalPublic
    {
        public int[,] D2_ReturnOnly() { return new int[4, 2]; }
        public int[,] D2_ReturnAndInput1(int[,] arr) { return arr; }
        public int[,] D2_ReturnAndInput2of2(bool a, int[,] arr) { return arr; }
        public bool D2_NotReturnAndInput2of2(bool a, int[,] arr) { return a; }
        public bool D2_NotReturnAndInput2of3(bool a, int[,] arr, bool b) { return a; }
        public int[,] D2_ReturnAndInput2of3(bool a, int[,] arr, bool b) { return arr; }

    }

    // tests return type and paramteter cases for 3-dimensional arrays 
    // we expect diagnostics to be raised since the methods are public
    public sealed class D3PublicPublic
    {
        public int[,,] D3_ReturnOnly() { return new int[2, 1, 3] { { { 1, 1, 1 } }, { { 2, 2, 2 } } }; }
        public int[,,] D3_ReturnAndInput1(int[,,] arr) { return arr; }
        public int[,,] D3_ReturnAndInput2of2(bool a, int[,,] arr) { return arr; }
        public int[,,] D3_ReturnAndInput2of3(bool a, int[,,] arr, bool b) { return arr; }
        public bool D3_NotReturnAndInput2of2(bool a, int[,,] arr) { return a; }
        public bool D3_NotReturnAndInput2of3(bool a, int[,,] arr, bool b) { return a; }

    }

    internal sealed class D3InternalPublic
    {
        public int[,,] D3_ReturnOnly() { return new int[2, 1, 3] { { { 1, 1, 1 } }, { { 2, 2, 2 } } }; }
        public int[,,] D3_ReturnAndInput1(int[,,] arr) { return arr; }
        public int[,,] D3_ReturnAndInput2of2(bool a, int[,,] arr) { return arr; }
        public int[,,] D3_ReturnAndInput2of3(bool a, int[,,] arr, bool b) { return arr; }
        public bool D3_NotReturnAndInput2of2(bool a, int[,,] arr) { return a; }
        public bool D3_NotReturnAndInput2of3(bool a, int[,,] arr, bool b) { return a; }

    }

    // tests return type and paramteter cases for 3-dimensional arrays 
    // we expect normal compilation since the methods are private 
    public sealed class D3PublicPrivate
    {
        private int[,,] D3_ReturnOnly() { return new int[2, 1, 3] { { { 1, 1, 1 } }, { { 2, 2, 2 } } }; }
        private int[,,] D3_ReturnAndInput1(int[,,] arr) { return arr; }
        private int[,,] D3_ReturnAndInput2of2(bool a, int[,,] arr) { return arr; }
        private int[,,] D3_ReturnAndInput2of3(bool a, int[,,] arr, bool b) { return arr; }
        private bool D3_NotReturnAndInput2of2(bool a, int[,,] arr) { return a; }
        private bool D3_NotReturnAndInput2of3(bool a, int[,,] arr, bool b) { return a; }

    }

    // field in struct -- think this will get covered already, but we'll see
    // yeah you can't even have methods in fields in Windows Runtime 
    public struct D2FieldInStruct
    {
        public int[,,] D3Method(bool b) { return new int[2, 1, 3] { { { 1, 1, 1 } }, { { 2, 2, 2 } } }; }
    }

    /*
     * negative test - public methods as members of an interface 
     */

    public interface D2MemberOfInterface_N
    {
        public int[,] D2_ReturnOnly();
        public int[,] D2_ReturnAndInput1(int[,] arr);
        public int[,] D2_ReturnAndInput2of2(bool a, int[,] arr);
        public bool D2_NotReturnAndInput2of2(bool a, int[,] arr);
        public bool D2_NotReturnAndInput2of3(bool a, int[,] arr, bool b);
        public int[,] D2_ReturnAndInput2of3(bool a, int[,] arr, bool b);
    }

    public interface D2MemberOfInterface_P
    {
        private int[,] D2_ReturnOnly() { return new int[4, 2]; }
        private int[,] D2_ReturnAndInput1(int[,] arr) { return arr; }
        private int[,] D2_ReturnAndInput2of2(bool a, int[,] arr) { return arr; }
        private bool D2_NotReturnAndInput2of2(bool a, int[,] arr) { return a; }
        private bool D2_NotReturnAndInput2of3(bool a, int[,] arr, bool b) { return a; }
        private int[,] D2_ReturnAndInput2of3(bool a, int[,] arr, bool b) { return arr; }

    }

    public interface D3MemberOfInterface_N
    {
        public int[,,] D3_ReturnOnly(); 
        public int[,,] D3_ReturnAndInput1(int[,,] arr); 
        public int[,,] D3_ReturnAndInput2of2(bool a, int[,,] arr);
        public int[,,] D3_ReturnAndInput2of3(bool a, int[,,] arr, bool b);
        public bool D3_NotReturnAndInput2of2(bool a, int[,,] arr);
        public bool D3_NotReturnAndInput2of3(bool a, int[,,] arr, bool b); 
    }
    
    public interface D3MemberOfInterface_P
    {
        private int[,,] D3_ReturnOnly() { return new int[2, 1, 3] { { { 1, 1, 1 } }, { { 2, 2, 2 } } }; }
        private int[,,] D3_ReturnAndInput1(int[,,] arr) { return arr; }
        private int[,,] D3_ReturnAndInput2of2(bool a, int[,,] arr) { return arr; }
        private int[,,] D3_ReturnAndInput2of3(bool a, int[,,] arr, bool b) { return arr; }
        private bool D3_NotReturnAndInput2of2(bool a, int[,,] arr) { return a; }
        private bool D3_NotReturnAndInput2of3(bool a, int[,,] arr, bool b) { return a; }
    }
} // End TestDiagnostics namespace


// maybe we need all the interface tests at the top level ? 
public interface TopLevel_PrivateD3MemberOfInterface
    { 
        public int[,,] D3_ReturnOnly(); 
        public int[,,] D3_ReturnAndInput1(int[,,] arr); 
        public int[,,] D3_ReturnAndInput2of2(bool a, int[,,] arr);
        public int[,,] D3_ReturnAndInput2of3(bool a, int[,,] arr, bool b);
        public bool D3_NotReturnAndInput2of2(bool a, int[,,] arr);
        public bool D3_NotReturnAndInput2of3(bool a, int[,,] arr, bool b); 
    }

public interface TopLevel_D2MemberOfInterface_P
{
    private int[,] D2_ReturnOnly() { return new int[4, 2]; }
    private int[,] D2_ReturnAndInput1(int[,] arr) { return arr; }
    private int[,] D2_ReturnAndInput2of2(bool a, int[,] arr) { return arr; }
    private bool D2_NotReturnAndInput2of2(bool a, int[,] arr) { return a; }
    private bool D2_NotReturnAndInput2of3(bool a, int[,] arr, bool b) { return a; }
    private int[,] D2_ReturnAndInput2of3(bool a, int[,] arr, bool b) { return arr; }

}
