using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Generator 
{
    public class WinRTRules
    {
        #region RuleDescriptors 

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

        static DiagnosticDescriptor StructHasPrivateFieldRule = MakeRule(
            "WME1060(b)",
            "Private field in struct",
            "Structure {0} has private field. All fields must be public for Windows Runtime structures.");

        static DiagnosticDescriptor StructHasInvalidFieldRule = MakeRule(
            "WME1060",
            "Invalid field in struct",
            ("Structure {0} has field '{1}' of type {2}; {2} is not a valid Windows Runtime field type. Each field " 
            + "in a Windows Runtime structure can only be UInt8, Int16, UInt16, Int32, UInt32, Int64, UInt64, Single, Double, Boolean, String, Enum, or itself a structure."));
        
        static DiagnosticDescriptor StructHasInvalidFieldRule2 = MakeRule(
            "WME1060",
            "Invalid field in struct",
            ("Structure {0} has a field of type {1}; {1} is not a valid Windows Runtime field type. Each field " 
            + "in a Windows Runtime structure can only be UInt8, Int16, UInt16, Int32, UInt32, Int64, UInt64, Single, Double, Boolean, String, Enum, or itself a structure."));

        static DiagnosticDescriptor OperatorOverloadedRule = MakeRule(
            "WME1087",
            "Operator overload exposed",
            "{0} is an operator overload. Managed types cannot expose operator overloads in the Windows Runtime");

        static DiagnosticDescriptor MethodOverload_MultipleDefaultAttribute = MakeRule(
            "WME1059",
            "Only one overload should be designated default", // todo better msg
            //"Multiple {0}-parameter overloads of '{1}.{2}' are decorated with Windows.Foundation.Metadata.DefaultOverloadAttribute.");
            "Multiple {0}-parameter overloads of '{1}' are decorated with Windows.Foundation.Metadata.DefaultOverloadAttribute.");

        static DiagnosticDescriptor MethodOverload_NeedDefaultAttribute = MakeRule(
            "WME1085",
            "Multiple overloads seen, one needs a default", // todo better msg
            //"The {0}-parameter overloads of {1}.{2} must have exactly one method specified as the default overload by decorating it with Windows.Foundation.Metadata.DefaultOverloadAttribute.");
            "The {0}-parameter overloads of {1} must have exactly one method specified as the default overload by decorating it with Windows.Foundation.Metadata.DefaultOverloadAttribute.");

        #endregion

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
         *  returns true if the class represented by the symbol implements any of the interfaces defined in ProhibitedAsyncInterfaces */
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
         *  keeps track of the arity of all constructors, and reports the diagnostic (and exits) as soon as a two constructors of the same arity are seen. */
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
         *  the generated code for components uses the name "__retval" for the output variable, 
         *  we report diagnostic if a user uses this same identifier as a parameter to a method */
        public bool HasParameterNamedValue(ref GeneratorExecutionContext context, MethodDeclarationSyntax method)
        { 
            foreach (ParameterSyntax parameter in method.ParameterList.Parameters) 
            { 
                if (parameter.Identifier.Value.Equals("__retval")) 
                { 
                    context.ReportDiagnostic(Diagnostic.Create(ParameterNamedValueRule, parameter.GetLocation(), method.Identifier, parameter.Identifier)); 
                    return true; 
                } 
            }
            return false;
        }

        public bool HasErrorsInMethods(ref GeneratorExecutionContext context, ClassDeclarationSyntax classDeclaration)
        {
            bool found = false;
            IEnumerable<MethodDeclarationSyntax> methods = classDeclaration.ChildNodes().OfType<MethodDeclarationSyntax>();

            // the boolean indicates that the method name (the key) has been marked as the DefaultOverload (via an attribute)
            Dictionary<string, bool> myMap = new Dictionary<string, bool>(); 

            foreach (MethodDeclarationSyntax method in methods)
            {
                string methodName = method.Identifier.Text;

                bool seenMethodBefore = myMap.TryGetValue(methodName, out bool methodHasAttrAlready);

                bool hasDefaultOverloadAttribute = false;

                // look at all the attributes on this method and see if any of them is the DefaultOverload attribute 
                foreach (var attrList in method.AttributeLists)
                {
                    foreach (var attr in attrList.Attributes)
                    {
                        if (attr.Name.ToString().Equals("Windows.Foundation.Metadata.DefaultOverload")) // maybe name is just DefaultOverload?
                        {
                            hasDefaultOverloadAttribute = true;
                            break;
                        }
                    }
                }

                // what do we do if it is in there but not with the attribute ?
                //   what do we do if above AND we see the attribute now
                //   what do we do if above AND we DONT see the attribute now

                // ... 

                // later if we have seen it before... 
                // and if it doesnt have it now and it didnt have it before, raise the "add attribute error"
                //     if it doesnt have it now and it did before, thats fine, 
                //       note: dont change its mapping!

                // if it has it now and it didn't before, thats fine (make sure to mark it as having it)
                // if it has it now and it did before, raise "multiple attributes error"
   
                // Do we have an overload ? 
                if (seenMethodBefore) 
                {
                    if (hasDefaultOverloadAttribute && !methodHasAttrAlready)
                    {
                        // we've seen it, but it didnt have the attribute, so mark that it has it now
                        myMap[methodName] = true;
                    }
                    else if (hasDefaultOverloadAttribute && methodHasAttrAlready)
                    {
                        // raise the "dont have multiple default overloads attributed" diagnostic  
                        context.ReportDiagnostic(Diagnostic.Create(MethodOverload_MultipleDefaultAttribute, method.GetLocation(), method.Arity, method.Identifier));
                        found |= true;
                    }
                    else if (!hasDefaultOverloadAttribute && !methodHasAttrAlready) 
                    {
                        // raise the "multiple overloads, one needs the attribute!"
                        context.ReportDiagnostic(Diagnostic.Create(MethodOverload_NeedDefaultAttribute, method.GetLocation(), method.Arity, method.Identifier));
                        found |= true;
                    }
                }
                else 
                { 
                    // if the method hasn't been seen before, this will make a new pair and mark it 
                    myMap[methodName] = hasDefaultOverloadAttribute;  // this is where we store it with value false 
                }


                /* make sure no parameter has the name "__retval" */
                found |= HasParameterNamedValue(ref context, method);
            }
            return false;
        }

        /* SimplifySyntaxTypeString 
         *  returns the more common term for the given kind of syntax; used when creating a diagnostic for an invalid field in a struct */ 
        private string SimplifySyntaxTypeString(string syntaxType)
        {
            switch (syntaxType)
            {
                case "EventFieldDeclarationSyntax":
                    return "event";
                case "ConstructorDeclarationSyntax":
                    return "constructor";
                case "DelegateDeclarationSyntax":
                    return "delegate";
                case "IndexerDeclarationSyntax":
                    return "indexer";
                case "MethodDeclarationSyntax":
                    return "method";
                case "OperatorDeclarationSyntax":
                    return "operator";
                case "PropertyDeclarationSyntax":
                    return "property";
                default:
                    return "unknown syntax type: " + syntaxType;
            }
        }

        /*  StructHasFieldOfType
         *   returns true iff there is a declaration of a field with the given type argument 
         *   e.g., if a struct has a property, and T is PropertyDeclarationSyntax, we report a diagnostic and return true */
        public bool StructHasFieldOfType<T>(ref GeneratorExecutionContext context, StructDeclarationSyntax structDeclaration)
        {
            if (structDeclaration.DescendantNodes().OfType<T>().Any())
            {
                context.ReportDiagnostic(Diagnostic.Create(StructHasInvalidFieldRule2, 
                    structDeclaration.GetLocation(), 
                    structDeclaration.Identifier, 
                    SimplifySyntaxTypeString(typeof(T).Name)));
                return true;
            }
            return false;
        }


        /* StructHasInvalidFields
         *   returns true if there is a field declared private, 
         *   or (inclusive) declared with a type that is a class or one of object, byte or dynamic  */
        public bool StructHasFieldOfInvalidType(ref GeneratorExecutionContext context, FieldDeclarationSyntax field, StructDeclarationSyntax structDeclaration, List<string> classNames)
        {
            bool found = false; 

            /* No private fields allowed in Windows Runtime components */
            if (field.GetFirstToken().ToString().Equals("private")) // hmm
            {
                context.ReportDiagnostic(Diagnostic.Create(StructHasPrivateFieldRule, field.GetLocation(), structDeclaration.Identifier));
                found |= true;
            } 

            foreach (var variable in field.DescendantNodes().OfType<VariableDeclarationSyntax>())
            {
                var typeStr = variable.Type.ToString();

                List<string> invalidTypes = new List<string> { "object", "byte", "dynamic" };
                invalidTypes.AddRange(classNames);
                
                if (invalidTypes.Contains(typeStr))
                { 
                    context.ReportDiagnostic(Diagnostic.Create(StructHasInvalidFieldRule,
                            variable.GetLocation(),
                            structDeclaration.Identifier,
                            field.ToString(),
                            typeStr));
                    found |= true;
                }
            }
            return found;
        }

        public bool OverloadsOperator(ref GeneratorExecutionContext context, ClassDeclarationSyntax classDeclaration)
        {
            var operatorDeclarations = classDeclaration.DescendantNodes().OfType<OperatorDeclarationSyntax>(); 
            if (operatorDeclarations.Any()) 
            { 
                var overloadedOperator = operatorDeclarations.First(); 
                context.ReportDiagnostic(Diagnostic.Create(OperatorOverloadedRule, overloadedOperator.GetLocation(), overloadedOperator.OperatorKeyword.Text));
                return true;
            }
            return false;
        }
    }
}
