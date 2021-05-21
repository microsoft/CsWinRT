using Mono.Cecil;
using System;
using System.IO;

namespace GuidPatch
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine($"Expected to be given two arguments. Given {args.Length}");
            }
            else
            {
                /* The first argument given is the .dll to patch 
                   The second argument is the folder to look for winrt.runtime in */
                var resolver = new DefaultAssemblyResolver();

                try
                {
                    resolver.AddSearchDirectory(args[1]);
                    AssemblyDefinition winRTRuntimeAssembly = resolver.Resolve(new AssemblyNameReference("WinRT.Runtime", default));
                    var guidPatcher = new GuidPatcher(
                        args[0],
                        resolver,
                        winRTRuntimeAssembly);
                    int numPatches = guidPatcher.ProcessAssembly();
                    Directory.CreateDirectory("GuidPatcherOutput");
                    guidPatcher.SaveAssembly("GuidPatcherOutput");
                    Console.WriteLine($"{numPatches} IID calculations/fetches patched");
                }
                catch (AssemblyResolutionException)
                {
                    Console.WriteLine("Failed to resolve WinRT.Runtime, shutting down.");
                    return;
                }
            }
        }
    }
}
