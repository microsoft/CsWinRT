using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Linq;

namespace Generator
{
    public class AotOptimizer
    {
        private readonly GeneratorExecutionContext context;

        public AotOptimizer(GeneratorExecutionContext context)
        {
            this.context = context;
        }

        public void Generate()
        {
            AotSyntaxReceiver syntaxReceiver = (AotSyntaxReceiver)context.SyntaxReceiver;
            foreach (var declaration in syntaxReceiver.Declarations)
            {
                var model = context.Compilation.GetSemanticModel(declaration.SyntaxTree);
                var symbol = model.GetDeclaredSymbol(declaration);

                // If the class is a CsWinRT projected class already, then it is an RCW
                // and we don't want to generate a vtable for it.
                if (GeneratorHelper.IsWinRTType(symbol))
                {
                    continue;
                }

                // Filter to the WinRT interfaces.
                List<INamedTypeSymbol> interfacesToAddToVtable = new();
                foreach (var @interface in symbol.Interfaces)
                {
                    if (GeneratorHelper.IsWinRTType(@interface))
                    {
                        interfacesToAddToVtable.Add(@interface);
                    }
                }

                if (interfacesToAddToVtable.Any())
                {
                    var vtableEntries = string.Join(", ", interfacesToAddToVtable.Select((@interface) => $@"typeof({@interface.ToDisplayString()})"));
                    string source = $@"
namespace {symbol.ContainingNamespace.ToDisplayString()}
{{
    [global::WinRT.WinRTExposedClass(new global::System.Type[]{{{vtableEntries}}})]
    partial class {symbol.Name}
    {{
    }}
}}
";
                    context.AddSource($"{symbol.ContainingNamespace}.{symbol.Name}.WinRTVtable.g.cs", source);
                }
            }
        }
    }
}
