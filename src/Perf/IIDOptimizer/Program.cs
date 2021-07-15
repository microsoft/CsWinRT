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
                alias: "--targetAssembly",
                description: "The assembly to perform GUID lookup optimizations on.",
                argumentType: typeof(FileInfo), 
                arity: ArgumentArity.ExactlyOne 
            );
        
        static readonly Option _references =
            new Option
            (
                alias: "--references",
                description: "Reference assemblies used when compiling the target assembly.",
                argumentType: typeof(FileInfo[]), 
                arity: ArgumentArity.ZeroOrMore
            );

        static async Task Main(string[] args)
        {
            var rootCommand = new RootCommand { };
            // friendlier option names 
            _targetAssembly.AddAlias("--target");
            _references.AddAlias("--refs");

            rootCommand.AddOption(_targetAssembly);
            rootCommand.AddOption(_references);

            rootCommand.Handler = CommandHandler.Create<FileInfo, IEnumerable<FileInfo>>(GuidPatch);
            await rootCommand.InvokeAsync(args);            
        }

        private static int GuidPatch(FileInfo targetAssembly, IEnumerable<FileInfo> references)
        {
            var resolver = new ReferenceAssemblyResolver(targetAssembly, references);
            try 
            {
                AssemblyDefinition winRTRuntimeAssembly = resolver.Resolve(new AssemblyNameReference("WinRT.Runtime", default));
                   
                Directory.CreateDirectory("obj\\IIDOptimizer"); 
                
                var guidPatcher = new GuidPatcher(
                    targetAssembly.FullName, 
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
