using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DiagnosticTests
{
    public sealed partial class UnitTesting
    {
        /// <summary>
        /// CreateCompilation creates a CSharpCompilation 
        /// </summary>
        /// <param name="source">string of source code</param>
        /// <returns></returns>
        private static Compilation CreateCompilation(string source)
            => CSharpCompilation.Create(
                assemblyName: "compilation",
                syntaxTrees: new[] { CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview)) },
                references: new[] { MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location) },
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        /// <summary>
        /// CreateDriver makes a CSharpGeneratorDriver
        /// </summary>
        /// <param name="compilation"></param>
        /// <param name="generators"></param>
        /// <returns></returns>
        private static GeneratorDriver CreateDriver(Compilation compilation, params ISourceGenerator[] generators)
            => CSharpGeneratorDriver.Create(
                generators: ImmutableArray.Create(generators),
                additionalTexts: ImmutableArray<AdditionalText>.Empty,
                parseOptions: (CSharpParseOptions)compilation.SyntaxTrees.First().Options,
                optionsProvider: null); 
        // todo: pass the CsWinRTComponent config option here so we don't have to comment out the check in the source generator 

        /// <summary>
        /// RunGenerators makes a driver and applies the given generators to the compilation, storing diagnostics in an out param
        /// </summary>
        /// <param name="compilation"></param>
        /// <param name="diagnostics"></param>
        /// <param name="generators"></param>
        /// <returns></returns>
        private static Compilation RunGenerators(Compilation compilation, out ImmutableArray<Diagnostic> diagnostics, params ISourceGenerator[] generators)
        {
            CreateDriver(compilation, generators).RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation, out diagnostics);
            return updatedCompilation;
        }

        /// <summary>
        /// Create a HashSet of DiagnosticDescriptor from the Array of Diagnostic
        /// </summary>
        /// <param name="arr"></param>
        /// <returns></returns>
        private static HashSet<DiagnosticDescriptor> MakeDiagnosticSet(ImmutableArray<Diagnostic> arr)
        { 
            HashSet<DiagnosticDescriptor> setSoFar = new();
            foreach (var d in arr)
            {
                setSoFar.Add(d.Descriptor);
            }
            return setSoFar;
        }
    }
}
