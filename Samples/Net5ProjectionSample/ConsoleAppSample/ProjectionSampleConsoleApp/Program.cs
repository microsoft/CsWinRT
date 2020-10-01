using System;

namespace ConsoleAppSample
{
    class Program
    {
        static void Main(string[] args)
        {
            var x = new SimpleMathComponent.SimpleMath();
            Console.WriteLine("Adding 5.5 + 6.5 ...");
            Console.WriteLine(x.add(5.5, 6.5).ToString());
            Console.ReadLine();
        }
    }
}
