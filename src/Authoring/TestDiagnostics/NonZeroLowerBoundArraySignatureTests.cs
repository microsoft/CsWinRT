using System;

namespace TestDiagnostics
{

    /* 
     * Invalid tests include public properties in public classes, public interface methods, 
     */

    public sealed class ArrayInstanceProperty1
    {
        public int[] Arr
        {
            get { return (int[])Array.CreateInstance(typeof(int), new int[] { 4 }, new int[] { 1 }); }
        }
    } 

    public sealed class ArrayInstanceProperty2
    {
        public System.Array Arr
        {
            get { return Array.CreateInstance(typeof(int), new int[] { 4 }, new int[] { 1 }); }
        }
    } 

    public sealed class ArrayInstanceProperty3
    {
        public int[] Arr
        {
            get { return (int[])Array.CreateInstance(typeof(int), new int[] { 4 }); }
        }
    } 

    public sealed class ArrayInstanceProperty4
    {
        public System.Array Arr
        {
            get { return Array.CreateInstance(typeof(int), new int[] { 4 }); }
        }
    } 
 
    public interface ArrayInstanceInterface1
    {
        System.Array Id(System.Array arr);
    }
    public interface ArrayInstanceInterface2
    {
        void Method2(System.Array arr);
    }
    
    public interface ArrayInstanceInterface3
    {
        System.Array Method3();
    }

    public sealed class SystemArrayProperty
    {
       public System.Array Arr { get; set; }
    }
    public sealed class SystemArrayProperty_Valid
    {
       private System.Array PrivArr { get; set; } 
    }
   
    public sealed class JustReturn 
    {
       public System.Array SystemArrayMethod() { return Array.CreateInstance(typeof(int), new int[] { 4 }); }
    }

    public sealed class UnaryAndReturn
    {
       public System.Array SystemArrayMethod(System.Array arr) { return arr; }
    }
    
    public sealed class SecondInput
    {
       public bool SystemArrayMethod(bool a, System.Array arr) { return a; }
    }

    public sealed class SecondInput2
    {
       public bool SystemArrayMethod(bool a, System.Array arr, bool b) { return a; }
    }

    public sealed class SecondInputAndReturnType
    {
       public System.Array SystemArrayMethod(bool a, System.Array arr) { return arr; }
    }

    public sealed class SecondInputAndReturnType2
    {
       public System.Array SystemArrayMethod(bool a, System.Array arr, bool b) { return arr; }
    }

    public interface SystemArrayInReturnTypeInterface
    {
       public System.Array SystemArrayMethod();
    }
    public interface UnaryAndReturnTypeInterface
    {
       public System.Array SystemArrayMethod(System.Array arr);
    }
    public interface SecondArgAndReturnTypeInterface
    {
       public System.Array SystemArrayMethod(bool a, System.Array arr);
    }
    public interface SecondArgInterface
    {
       public bool SystemArrayMethod(bool a, System.Array arr);
    }
    // pick up here
    public interface NZLBMemberOfInterface_Invalid
    {
       public bool NZLB_NotReturnAndInput2of3(bool a, System.Array arr, bool b);
    }
    public interface NZLBMemberOfInterface_Invalid2
    {
       public System.Array NZLB_ReturnAndInput2of3(bool a, System.Array arr, bool b);
    }

    namespace SubNamespace
    {
        public interface SubNamespacInterface_NZLBMethods_Invalid
        {
           public System.Array D2_ReturnOnly();
           public System.Array D2_ReturnAndInput1(System.Array arr);
           public System.Array D2_ReturnAndInput2of2(bool a, System.Array arr);
           public System.Array D2_ReturnAndInput2of3(bool a, System.Array arr, bool b);
           public bool D2_NotReturnAndInput2of2(bool a, System.Array arr);
           public bool D2_NotReturnAndInput2of3(bool a, System.Array arr, bool b);
       } 
    }
    */

    /*
     * Valid tests
     */

    /* 
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

    internal interface InterfaceWithNonZeroLowerBound_Valid
    {
        System.Array Id(System.Array arr);
        void Method2(System.Array arr);
        System.Array Method3();
    }

    internal class NZLBArraySignature_2D_PrivateClass_Valid
    {
        public System.Array Arr_2d { get; set; }
        public System.Array Arr_3d { get; set; }
        private System.Array PrivArr_2d { get; set; }
        private System.Array PrivArr_3d { get; set; }
    }
    public sealed class NZLBArraySignature_3D_Valid
    {
        private System.Array PrivArr_2d { get; set; }

        private Array PrivArr_3d { get; set; }
    }
    internal sealed class NZLBInternalPublic_Valid
    {
        public System.Array NZLB_ReturnOnly() { return Array.CreateInstance(typeof(int), new int[] { 4 }); }
        public System.Array NZLB_ReturnAndInput1(System.Array arr) { return arr; }
        public System.Array NZLB_ReturnAndInput2of2(bool a, System.Array arr) { return arr; }
        public bool NZLB_NotReturnAndInput2of2(bool a, System.Array arr) { return a; }
        public bool NZLB_NotReturnAndInput2of3(bool a, System.Array arr, bool b) { return a; }
        public System.Array NZLB_ReturnAndInput2of3(bool a, System.Array arr, bool b) { return arr; }
    }

    // tests return type and paramteter cases for 3-dimensional arrays 
    // we expect normal compilation since the methods are private 
    public sealed class NZLBPublicPrivate_Valid
    {
        private System.Array NZLB_ReturnOnly() { return Array.CreateInstance(typeof(int), new int[] { 4 }); }
        private System.Array NZLB_ReturnAndInput1(System.Array arr) { return arr; }
        private System.Array NZLB_ReturnAndInput2of2(bool a, System.Array arr) { return arr; }
        private System.Array NZLB_ReturnAndInput2of3(bool a, System.Array arr, bool b) { return arr; }
        private bool NZLB_NotReturnAndInput2of2(bool a, System.Array arr) { return a; }
        private bool NZLB_NotReturnAndInput2of3(bool a, System.Array arr, bool b) { return a; }
    }
    */
} 
