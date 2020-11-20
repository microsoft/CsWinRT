using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Generator 
{
    public class WinRTRules
    { 
        /* MakeRule is a helper function that does most of the boilerplate information needed for Diagnostics
        param id : string, a short identifier for the diagnostic
        param title : string, a few words generally describing the diagnostic
        param messageFormat : string, describes the diagnostic -- formatted with {0}, ... -- 
            such that data can be passed in for the code the diagnostic is reported for  */
        static DiagnosticDescriptor MakeRule(string id, string title, string messageFormat)
        {
            return new DiagnosticDescriptor(
                id: id,
                title: title,
                messageFormat: messageFormat,
                category: "Usage",
                /* Warnings dont fail command line build; winmd generation is prevented regardless of severity.
                * Make this error when making final touches on this deliverable. */
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                helpLinkUri: "https://docs.microsoft.com/en-us/previous-versions/hh977010(v=vs.110)");
        }

        static DiagnosticDescriptor AsyncRule = MakeRule(
            "WME1084",
            "Async Interfaces Rule",
            "Runtime component class {0} cannot implement async interface {1}; use AsyncInfo class methods instead of async interfaces");
        
        static DiagnosticDescriptor ClassConstructorRule = MakeRule(
            "WME1099",
            "Class Constructor Rule",
            "Runtime component class {0} cannot have multiple constructors of the same arity {1}");

        static DiagnosticDescriptor ParameterNamedValueRule = MakeRule(
            "WME1092",
            "Parameter Named Value Rule",
            ("The method {0} has a parameter named {1} which is the same as the default return value name. "
            + "Consider using another name for the parameter or use the System.Runtime.InteropServices.WindowsRuntime.ReturnValueNameAttribute "
            + "to explicitly specify the name of the return value."));

        static DiagnosticDescriptor StructHasPropertyRule = MakeRule(
            "WME1060(a)",
            "Property in a Struct",
            "The structure {0} has a property {1}. In Windows Runtime, structures can only contain public fields of basic types, enums and structures.");

        static DiagnosticDescriptor StructHasPrivateFieldRule = MakeRule(
            "WME1060(b)",
            "Private field in struct",
            "Structure {0} has private field. All fields must be public for Windows Runtime structures.");

        static DiagnosticDescriptor StructHasInvalidFieldRule = MakeRule(
            "WME1060",
            "Invalid field in struct",
            ("Structure {0} has field {1} of type {2}. {2} is not a valid Windows Runtime field type. Each field " 
            + "in a Windows Runtime structure can only be UInt8, Int16, UInt16, Int32, UInt32, Int64, UInt64, Single, Double, Boolean, String, Enum, or itself a structure."));


        /* The full metadata name of Async interfaces that should not be implemented by Windows Runtime components */
        static string[] ProhibitedAsyncInterfaces = {
                "Windows.Foundation.IAsyncAction", 
                "Windows.Foundation.IAsyncActionWithProgress`1",
                "Windows.Foundation.IAsyncOperation`1",
                "Windows.Foundation.IAsyncOperationWithProgress`2"
        };

        /* SameAsyncInterface uses the proper ISymbol equality check on the OriginalDefinition of the given symbols */
        private bool SameAsyncInterface(INamedTypeSymbol interfaceA, INamedTypeSymbol interfaceB)
        {   
            /* Using OriginalDefinition b/c the generic field of the metadata type has the template name, e.g. `TProgress`
             * and the actual interface will have a concrete type, e.g. `int` */
            return SymbolEqualityComparer.Default.Equals(interfaceA.OriginalDefinition, interfaceB.OriginalDefinition);
        }

        /* ImplementsAsyncInterface 
            returns true if the class represented by the symbol implements any of the interfaces defined in ProhibitedAsyncInterfaces */
        public bool ImplementsAsyncInterface(ref GeneratorExecutionContext context, INamedTypeSymbol classSymbol, ClassDeclarationSyntax classDeclaration)
        { 
            foreach (string prohibitedInterface in ProhibitedAsyncInterfaces)
            {
                INamedTypeSymbol asyncInterface = context.Compilation.GetTypeByMetadataName(prohibitedInterface);
                foreach (INamedTypeSymbol interfaceImplemented in classSymbol.AllInterfaces)
                {
                    if (SameAsyncInterface(interfaceImplemented, asyncInterface))
                    { 
                        context.ReportDiagnostic(Diagnostic.Create(AsyncRule, classDeclaration.GetLocation(), classDeclaration.Identifier, interfaceImplemented));
                        return true; 
                        /* By exiting early, we only report diagnostic for first prohibited interface we see. 
                        If a class implemented 2 (or more) such interfaces, then we would only report diagnostic error for the first one. 
                        could thread `found` variable from CatchWinRTDiagnostics here as well, if we want more diagnostics reported */
                    }
                }
            }
            return false;
        }

        /* HasMultipleConstructorsOfSameArity 
            keeps track of the arity of all constructors, and reports the diagnostic (and exits) as soon as a two constructors of the same arity are seen. */
        public bool HasMultipleConstructorsOfSameArity(ref GeneratorExecutionContext context, ClassDeclarationSyntax classDeclaration)
        {
            IEnumerable<ConstructorDeclarationSyntax> constructors = classDeclaration.ChildNodes().OfType<ConstructorDeclarationSyntax>();
            
            /* more performant data structure? or use a Set, in order to not have to call Contains()? */
            IList<int> aritiesSeenSoFar = new List<int>();

            foreach (ConstructorDeclarationSyntax constructor in constructors)
            {
                int arity = constructor.ParameterList.Parameters.Count;

                if (aritiesSeenSoFar.Contains(arity))
                {
                    context.ReportDiagnostic(Diagnostic.Create(ClassConstructorRule, constructor.GetLocation(), classDeclaration.Identifier, arity));
                    return true;
                }
                else
                {
                    aritiesSeenSoFar.Add(arity);
                }
            }
            return false;
        }

        /* HasParameterNamedValue 
            the generated code for components uses the name "__retval" for the output variable, 
            we report diagnostic if a user uses this same identifier as a parameter to a method */
        public bool HasReturnValueNameConflict(ref GeneratorExecutionContext context, ClassDeclarationSyntax classDeclaration)
        {
            IEnumerable<MethodDeclarationSyntax> methods = classDeclaration.ChildNodes().OfType<MethodDeclarationSyntax>();

            foreach (MethodDeclarationSyntax method in methods)
            {
                foreach (ParameterSyntax parameter in method.ParameterList.Parameters)
                {
                    if (parameter.Identifier.Value.Equals("__retval"))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(ParameterNamedValueRule, parameter.GetLocation(), method.Identifier, parameter.Identifier));
                        return true;
                    }
                }
            }
            return false;
        }

        /* StructHasPropertyField
            structs cannot have properties in the Windows Runtime */
        public bool StructHasPropertyField(ref GeneratorExecutionContext context, StructDeclarationSyntax structDeclaration)
        { 
            var propertyDeclarations = structDeclaration.DescendantNodes().OfType<PropertyDeclarationSyntax>();
            if (propertyDeclarations.Any())
            {
                context.ReportDiagnostic(Diagnostic.Create(StructHasPropertyRule, structDeclaration.GetLocation(), structDeclaration.Identifier, propertyDeclarations.First().Identifier));
                return true;
            }
            return false;
        }

        /* StructHasInvalidFields
            structs can only contain fields of primitive types, enums, and other structs */
        public bool StructHasInvalidFields(ref GeneratorExecutionContext context, FieldDeclarationSyntax field, StructDeclarationSyntax structDeclaration, List<string> classNames)
        {
            bool found = false; 

            if (field.GetFirstToken().ToString().Equals("private")) // hmm
            {
                context.ReportDiagnostic(Diagnostic.Create(StructHasPrivateFieldRule, field.GetLocation(), structDeclaration.Identifier));
                found |= true;
            } 
            foreach (var variable in field.DescendantNodes().OfType<VariableDeclarationSyntax>())
            {
                var type = variable.Type;
                var typeStr = type.ToString();

                List<string> invalidTypes = new List<string> { "object", "byte", "dynamic" };
                invalidTypes.AddRange(classNames);
                
                if (invalidTypes.Contains(typeStr))
                { 
                    context.ReportDiagnostic(Diagnostic.Create(StructHasInvalidFieldRule,
                            variable.GetLocation(),
                            structDeclaration.Identifier,
                            field.ToString(),
                            typeStr));
                }
            }
            return found;
        }
    }
}
