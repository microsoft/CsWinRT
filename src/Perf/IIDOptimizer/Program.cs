using Mono.Cecil;
using System;
using System.IO;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace GuidPatch
{
    class Program
    {
        static readonly Option _targetAssembly =
            new Option
            (
                name: "--targetAssembly",
                description: "The assembly to perform GUID optimizations on.",
                argumentType: typeof(string), 
                arity: ArgumentArity.ExactlyOne 
            );

        static readonly Option _runtimePath =
            new Option
            (
                name: "--runtimePath",
                description: "The path to the folder used to resolve WinRT.Runtime.dll assembly.",
                argumentType: typeof(string), 
                arity: ArgumentArity.ExactlyOne
            );

        static readonly Option _references =
            new Option
            (
                name: "--references",
                description: "Reference assemblies used when compiling the target assembly.",
                argumentType: typeof(FileInfo[]), 
                arity: ArgumentArity.ZeroOrMore
            );


        static async Task Main(string[] args)
        {
            var rootCommand = new RootCommand { };
            // friendlier option names 
            _targetAssembly.AddAlias("--target");
            _runtimePath.AddAlias("--runtime");
            _references.AddAlias("--refs");

            rootCommand.AddOption(_targetAssembly);
            rootCommand.AddOption(_runtimePath);
            rootCommand.AddOption(_references);

            rootCommand.Handler = CommandHandler.Create<string, string, IEnumerable<FileInfo>>(GuidPatch);
            await rootCommand.InvokeAsync(args);            
        }

        private static int GuidPatch(string targetAssembly, string runtimePath, IEnumerable<FileInfo> references)
        {
            // pass targetAssembly here and update ReferenceAssemblyResolver to use it when resolving ==> can patch WinRT.Runtime itself 
            var resolver = new ReferenceAssemblyResolver(references);
            try 
            {
                AssemblyDefinition winRTRuntimeAssembly = resolver.Resolve(new AssemblyNameReference("WinRT.Runtime", default));
                    
                Directory.CreateDirectory("obj\\IIDOptimizer"); 
                
                var guidPatcher = new GuidPatcher(
                    targetAssembly, 
                    resolver,
                    winRTRuntimeAssembly);

                int numPatches = guidPatcher.ProcessAssembly(); 

                guidPatcher.SaveAssembly(guidPatcher.OptimizerDir); 

                Console.WriteLine($"{numPatches} IID calculations/fetches patched"); 
                return 0; 
            }
            catch (AssemblyResolutionException e)
            { 
                Console.WriteLine("Failed to resolve an assembly, shutting down."); 
                Console.WriteLine($"\tAssembly : {e.AssemblyReference.Name}"); 
                return -1; 
            }
        }
    }
}
