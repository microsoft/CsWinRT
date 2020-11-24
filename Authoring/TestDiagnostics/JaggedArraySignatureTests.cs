using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestDiagnostics
{
    /* ** jagged array  **  */

    // public property in class
    public sealed class JaggedArraySignature_2D_PublicProperty_Invalid
    {
        public int[][] Arr { get; set; }
    }

    public sealed class JaggedArraySignature_3D_PublicProperty_Invalid
    {
        public int[][] Arr { get; set; }
    }

    // positive test - private property in class

    public sealed class JaggedArraySignature_2D_PrivateProperty_Valid
    {
        private int[][] Arr { get; set; }
    }

    public sealed class JaggedArraySignature_3D_PrivateProperty_Valid
    {
        private int[][] Arr { get; set; }
    }

    internal sealed class JaggedArraySignature_2D_PublicProperty_PrivateClass_Valid
    {
        public int[][] Arr { get; set; }
    }

    internal sealed class JaggedArraySignature_3D_PublicProperty_PrivateClass_Valid
    {
        public int[][] Arr { get; set; }
    }

    // positive test - private property in class

    internal sealed class JaggedArraySignature_2D_PrivateProperty_PrivateClass_Valid
    {
        private int[][] Arr { get; set; }
    }

    internal sealed class JaggedArraySignature_3D_PrivateProperty_PrivateClass_Valid
    {
        private int[][] Arr { get; set; }
    }

    // public methods

    // positive test - private method 

    // field in struct -- think this will get covered already, but we'll see

    // declared (public) in interface

    // positive test - private in interface

}
