using System;
using TestEmbeddedLibrary;

namespace UseTestLib
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            TestEmbeddedLibrary.TestLib testLib = new();
            Console.WriteLine("Expect 10, Got " + testLib.Test1());

            Console.WriteLine("Expect 5, Got " + testLib.Test2());

            (bool, bool) pair = testLib.Test3();
            Console.WriteLine("Expect false, Got " + pair.Item1);
            Console.WriteLine("Expect true, Got " + pair.Item2);

            Console.WriteLine("Expect 5, Got " + testLib.Test4());

            testLib.Test5();
        }
    }
}
