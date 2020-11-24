using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestDiagnostics
{

    /* ** non-zero lowerbound array ** */

    // public property in class
    public sealed class NonZeroLowerBound_PublicProperty1_Invalid
    {
        public int[] Arr
        {
            get { return (int[])Array.CreateInstance(typeof(int), new int[] { 4 }, new int[] { 1 }); }
        }
    }

    public sealed class NonZeroLowerBound_PublicProperty2_Invalid
    {
        public System.Array Arr
        {
            get { return Array.CreateInstance(typeof(int), new int[] { 4 }, new int[] { 1 }); }
        }
    }

    public sealed class NonZeroLowerBound_PublicProperty1_Valid
    {
        public int[] Arr
        {
            get { return (int[])Array.CreateInstance(typeof(int), new int[] { 4 }); }
        }
    }

    public sealed class NonZeroLowerBound_PublicProperty2_Valid
    {
        public System.Array Arr
        {
            get { return Array.CreateInstance(typeof(int), new int[] { 4 }); }
        }
    }

    // positive test - private property in class
    public sealed class NonZeroLowerBound_PrivateProperty1_Valid
    {
        private int[] Arr
        {
            get { return (int[])Array.CreateInstance(typeof(int), new int[] { 4 }, new int[] { 1 }); }
        }
    }

    public sealed class NonZeroLowerBound_PrivateProperty2_Valid
    {
        private System.Array Arr
        {
            get { return Array.CreateInstance(typeof(int), new int[] { 4 }, new int[] { 1 }); }
        }
    }

    public sealed class NonZeroLowerBound_PrivateProperty3_Valid
    {
        private int[] Arr
        {
            get { return (int[])Array.CreateInstance(typeof(int), new int[] { 4 }); }
        }
    }

    public sealed class NonZeroLowerBound_PrivateProperty4_Valid
    {
        private System.Array Arr
        {
            get { return Array.CreateInstance(typeof(int), new int[] { 4 }); }
        }
    }

    internal sealed class NonZeroLowerBound_PublicProperty1_PrivateClass_Valid
    {
        public int[] Arr
        {
            get { return (int[])Array.CreateInstance(typeof(int), new int[] { 4 }, new int[] { 1 }); }
        }
    }

    internal sealed class NonZeroLowerBound_PublicProperty2_PrivateClass_Valid
    {
        public System.Array Arr
        {
            get { return Array.CreateInstance(typeof(int), new int[] { 4 }, new int[] { 1 }); }
        }
    }

    internal sealed class NonZeroLowerBound_PublicProperty1_PrivateClass2_Valid
    {
        public int[] Arr
        {
            get { return (int[])Array.CreateInstance(typeof(int), new int[] { 4 }); }
        }
    }

    internal sealed class NonZeroLowerBound_PublicProperty2_PrivateClass3_Valid
    {
        public System.Array Arr
        {
            get { return Array.CreateInstance(typeof(int), new int[] { 4 }); }
        }
    }

    // positive test - private property in class
    internal sealed class NonZeroLowerBound_PrivateProperty1_PrivateClass_Valid
    {
        private int[] Arr
        {
            get { return (int[])Array.CreateInstance(typeof(int), new int[] { 4 }, new int[] { 1 }); }
        }
    }

    internal sealed class NonZeroLowerBound_PrivateProperty2_PrivateClass_Valid
    {
        private System.Array Arr
        {
            get { return Array.CreateInstance(typeof(int), new int[] { 4 }, new int[] { 1 }); }
        }
    }

    internal sealed class NonZeroLowerBound_PrivateProperty3_PrivateClass_Valid
    {
        private int[] Arr
        {
            get { return (int[])Array.CreateInstance(typeof(int), new int[] { 4 }); }
        }
    }

    internal sealed class NonZeroLowerBound_PrivateProperty4_PrivateClass_Valid
    {
        private System.Array Arr
        {
            get { return Array.CreateInstance(typeof(int), new int[] { 4 }); }
        }
    }

    // public methods

    // positive test - private method 

    // field in struct -- think this will get covered already, but we'll see

    // declared (public) in interface

    // positive test - private in interface

}
