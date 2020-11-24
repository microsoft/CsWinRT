using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestDiagnostics
{

    /* ** non-zero lowerbound array ** */

    // public property in class
    public sealed class NonZeroLowerBound_PublicProperty_Invalid
    {
        public int[] Arr
        {
            get { return (int[])Array.CreateInstance(typeof(int), new int[] { 4 }, new int[] { 1 }); }
        }

        public System.Array Arr2
        {
            get { return Array.CreateInstance(typeof(int), new int[] { 4 }, new int[] { 1 }); }
        }

        public int[] Arr3
        {
            get { return (int[])Array.CreateInstance(typeof(int), new int[] { 4 }); }
        }
        public System.Array Arr4
        {
            get { return Array.CreateInstance(typeof(int), new int[] { 4 }); }
        }
    } 

    public sealed class NonZeroLowerBound_PrivateProperty_Valid
    {
        private int[] PrivArr
        {
            get { return (int[])Array.CreateInstance(typeof(int), new int[] { 4 }, new int[] { 1 }); }
        }
        private System.Array PrivArr2
        {
            get { return Array.CreateInstance(typeof(int), new int[] { 4 }, new int[] { 1 }); }
        }
        private int[] PrivArr3
        {
            get { return (int[])Array.CreateInstance(typeof(int), new int[] { 4 }); }
        }
        private System.Array PrivArr4
        {
            get { return Array.CreateInstance(typeof(int), new int[] { 4 }); }
        }
    }

    internal sealed class NonZeroLowerBound_PrivateClass_Valid
    {
        public int[] Arr
        {
            get { return (int[])Array.CreateInstance(typeof(int), new int[] { 4 }, new int[] { 1 }); }
        }

        public System.Array Arr2
        {
            get { return Array.CreateInstance(typeof(int), new int[] { 4 }, new int[] { 1 }); }
        }

        public int[] Arr3
        {
            get { return (int[])Array.CreateInstance(typeof(int), new int[] { 4 }); }
        }
        public System.Array Arr4
        {
            get { return Array.CreateInstance(typeof(int), new int[] { 4 }); }
        }

        private int[] PrivArr
        {
            get { return (int[])Array.CreateInstance(typeof(int), new int[] { 4 }, new int[] { 1 }); }
        }

        private System.Array PrivArr2
        {
            get { return Array.CreateInstance(typeof(int), new int[] { 4 }, new int[] { 1 }); }
        }

        private int[] PrivArr3
        {
            get { return (int[])Array.CreateInstance(typeof(int), new int[] { 4 }); }
        }

        private System.Array PrivArr4
        {
            get { return Array.CreateInstance(typeof(int), new int[] { 4 }); }
        }
    }

    public interface InterfaceWithNonZeroLowerBound
    {
        System.Array Id(System.Array arr);
    }
}
