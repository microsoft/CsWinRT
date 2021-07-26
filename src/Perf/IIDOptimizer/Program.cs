using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.IO;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using System.Collections.Generic;

using System.Linq;

namespace GuidPatch
{
    class Program
    {
        static readonly Option _targetAssembly =
            new Option
            (
                alias: "--targetAssembly",
                description: "The assembly to perform GUID lookup optimizations on.",
                argumentType: typeof(string), 
                arity: ArgumentArity.ExactlyOne 
            );

        static readonly Option _outputDir =
            new Option
            (
                alias: "--outputDirectory",
                description: "The directory to save the patched .dll to.",
                argumentType: typeof(string),
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
            _outputDir.AddAlias("--outdir");
            _references.AddAlias("--refs");

            rootCommand.AddOption(_targetAssembly);
            rootCommand.AddOption(_outputDir);
            rootCommand.AddOption(_references);

            rootCommand.Handler = CommandHandler.Create<string, string, IEnumerable<FileInfo>>(GuidPatch);
            await rootCommand.InvokeAsync(args);            
        }

        private static int GuidPatch(string targetAssembly, string outputDirectory, IEnumerable<FileInfo> references)
        {
            var resolver = new ReferenceAssemblyResolver(references);
            try 
            {
                var readerParameters = new ReaderParameters(ReadingMode.Deferred)
                {
                    ReadWrite = true,
                    InMemory = true,
                    AssemblyResolver = resolver,
                    ThrowIfSymbolsAreNotMatching = false,
                    SymbolReaderProvider = new DefaultSymbolReaderProvider(false),
                    ApplyWindowsRuntimeProjections = false,
                    ReadSymbols = true
                };

                var targetAssemblyDefinition = AssemblyDefinition.ReadAssembly(targetAssembly, readerParameters);

                /// Set WinRT.Runtime.dll -- either we are patching it, or it is in the references 
                AssemblyDefinition? winRTRuntimeAssembly = null;

                if (targetAssemblyDefinition.Name.Name == "WinRT.Runtime")
                {
                    winRTRuntimeAssembly = targetAssemblyDefinition;
                }
                else
                {
                    foreach (var referenceAssembly in targetAssemblyDefinition.MainModule.AssemblyReferences)
                    {
                        if (referenceAssembly.Name == "WinRT.Runtime")
                        { 
                            winRTRuntimeAssembly = resolver.Resolve(referenceAssembly);
                        }
                    }

                }
                /// 

                /// Don't patch twice 
                if (targetAssemblyDefinition.MainModule.Types.Any(typeDef => typeDef.Name == "<GuidPatcherImplementationDetails>"))
                {
                    Console.WriteLine("Target assembly has already been patched. Exiting early as there is no work to do.");
                    return -2;
                }

                var guidPatcher = new GuidPatcher(winRTRuntimeAssembly!, targetAssemblyDefinition);

                int numPatches = guidPatcher.ProcessAssembly(); 

                guidPatcher.SaveAssembly(outputDirectory);

                Console.WriteLine($"Saved patched .dll to {outputDirectory}");
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
