using System;
using System.IO;
using TestComponentCSharp;
using Windows.Storage;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Xunit;

namespace UnitTest
{
    public class TestCSharpNet5
    {
        public Class TestObject { get; private set; }

        public TestCSharpNet5()
        {
            TestObject = new Class();
        }

      
    }
}
