using System.Runtime.InteropServices;

namespace TestDiagnostics
{
    namespace OtherNamespace_Valid
    {
        public sealed class Class1
        {
            int x;
            public Class1(int a) { x = a; }
        }
    }

    // WME1068
    public sealed class TestDiagnostics
    {
        bool b;
        public TestDiagnostics(bool x) { b = x; }
    }
}

// WME1044
namespace OtherNamespace
{
    public sealed class Class1
    {
        int x;

        public Class1(int a)
        {
            x = a;
        }
    }
}

// WME1067 
namespace Testdiagnostics
{ 
    namespace InnerNamespace
    {
        public sealed class Class1
        {
            int x;
            public Class1(int a) { x = a; }
        }
    }
}
