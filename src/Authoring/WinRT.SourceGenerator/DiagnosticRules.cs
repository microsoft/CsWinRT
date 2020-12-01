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
            "In class {2}: Multiple {0}-parameter overloads of '{1}' are decorated with Windows.Foundation.Metadata.DefaultOverloadAttribute.");

        static DiagnosticDescriptor MethodOverload_NeedDefaultAttribute = MakeRule(
            "WME1085",
            "Multiple overloads seen, one needs a default", // todo better msg
            //"The {0}-parameter overloads of {1}.{2} must have exactly one method specified as the default overload by decorating it with Windows.Foundation.Metadata.DefaultOverloadAttribute.");
            "In class {2}: The {0}-parameter overloads of {1} must have exactly one method specified as the default overload by decorating it with Windows.Foundation.Metadata.DefaultOverloadAttribute.");

        static DiagnosticDescriptor ArraySignature_JaggedArrayRule = MakeRule(
            "WME10??",
            "Array signature found with jagged array, which is not a valid WinRT type", // todo better msg
            "In type {0}: the method {1} has signature that contains a jagged array; use a different type like List");

        static DiagnosticDescriptor ArraySignature_MultiDimensionalArrayRule = MakeRule(
            "WME10??",
            "Array signature found with multi-dimensional array, which is not a valid WinRT type", // todo better msg
            "In type {0}: the method {1} has signature that contains a multi-dimensional array; use a different type like List");

        static DiagnosticDescriptor ArraySignature_SystemArrayRule = MakeRule(
            "WME10??",
            "Array signature found with System.Array instance, which is not a valid WinRT type", // todo better msg
            "In type {0}: the method {1} has signature that contains a System.Array instance; use a different type like List");
        // "Method {0} has a multi-dimensional array of type {1} in its signature. Arrays in Windows Runtime must be one dimensional"

        #endregion

        private bool PropertyIsPublic(PropertyDeclarationSyntax p)
        {
            foreach (var thing in p.Modifiers)
            {
                if (thing.ValueText.Equals("public")) 
                {
                    return true;
                }
            }
            return false;
        }

        private bool MethodIsPublic(MethodDeclarationSyntax m)
        {
            foreach (var thing in m.Modifiers)
            {
                if (thing.ValueText.Equals("public")) 
                {
                    return true;
                }
            }
            return false;
        }

        public bool ClassIsPublic(ClassDeclarationSyntax m)
        {
            foreach (var thing in m.Modifiers)
            {
                if (thing.ValueText.Equals("public")) 
                {
                    return true;
                }
            }
            return false;
        }

        public bool InterfaceIsPublic(InterfaceDeclarationSyntax m)
        {
            foreach (var thing in m.Modifiers)
            {
                if (thing.ValueText.Equals("public")) 
                {
                    return true;
                }
            }
            return false;
        }


        #region AsyncInterfaces
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

        #endregion

        #region Misc
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

        /// MethodHasAttribute looks at all possible attributes on a given method declaration 
        ///     returns true iff any are (string) equal to the given attribute name 
        private bool MethodHasAttribute(string attrName, MethodDeclarationSyntax method)
        { 
            foreach (var attrList in method.AttributeLists) 
            {
                foreach (var attr in attrList.Attributes)
                {
                    if (attr.Name.ToString().Equals(attrName)) 
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Keeps track of repeated declarations of a method (overloads) and raises diagnostics according to the rule that exactly one overload should be attributed the default
        /// </summary>
        /// <param name="method">The method to check attributes for</param>
        /// <param name="methodHasAttributeMap">Keeps track of the method (via name + arity) and whether it was declared with the DefaultOverload attribute</param>
        /// <param name="overloadsWithoutAttributeMap">Keeps track of the methods that are overloads but don't have the DefaultOverload attribute (yet)</param>
        /// <param name="classIdentifier">The class the method lives in -- used for creating the diagnostic</param>
        /// <param name="context">The SourceGenerator context the code lives in</param>
        /// <returns>True iff multiple overloads of a method are found, where more than one has been designated as the default overload</returns>
        private bool CheckOverloadAttributes(MethodDeclarationSyntax method, 
            ref Dictionary<string, bool> methodHasAttributeMap, 
            ref Dictionary<string, Diagnostic> overloadsWithoutAttributeMap, 
            SyntaxToken classIdentifier, 
            ref GeneratorExecutionContext context)
        {
            bool found = false;
            int methodArity = method.ParameterList.Parameters.Count;
            string methodNameWithArity = method.Identifier.Text + methodArity.ToString();

            // look at all the attributes on this method and see if any of them is the DefaultOverload attribute 
            bool hasDefaultOverloadAttribute = MethodHasAttribute("Windows.Foundation.Metadata.DefaultOverload", method);
            
            bool seenMethodBefore = methodHasAttributeMap.TryGetValue(methodNameWithArity, out bool methodHasAttrAlready);

            // Do we have an overload ? 
            if (seenMethodBefore) 
            { 
                if (hasDefaultOverloadAttribute && !methodHasAttrAlready)
                { 
                    // we've seen it, but it didnt have the attribute, so mark that it has it now
                    methodHasAttributeMap[methodNameWithArity] = true;
                    overloadsWithoutAttributeMap.Remove(methodNameWithArity);
                }
                else if (hasDefaultOverloadAttribute && methodHasAttrAlready)
                {
                    // raise the "can't have multiple default attributes" diagnostic  
                    context.ReportDiagnostic(Diagnostic.Create(MethodOverload_MultipleDefaultAttribute, method.GetLocation(), 
                        methodArity, method.Identifier, classIdentifier));
                    found |= true;
                }
                else if (!hasDefaultOverloadAttribute && !methodHasAttrAlready) 
                {
                    // we could see this method later with the attribute, so hold onto the diagnostic for it until we know it doesn't have the attribute
                    Diagnostic diagnostic = Diagnostic.Create(MethodOverload_NeedDefaultAttribute, method.GetLocation(), methodArity, method.Identifier, classIdentifier);
                    overloadsWithoutAttributeMap[methodNameWithArity] = diagnostic;
                }
            }
            else 
            { 
                // first time we're seeing the method, add a pair in the map for its name and whether it has the attribute 
                methodHasAttributeMap[methodNameWithArity] = hasDefaultOverloadAttribute;
            }

            return found;
        }

        /// <summary>
        /// Checks to see if the class declares any operators (overloading them)
        /// </summary>
        /// <param name="context"></param>
        /// <param name="classDeclaration"></param>
        /// <returns>True iff an operator is overloaded by the given class</returns>
        public bool OverloadsOperator(ref GeneratorExecutionContext context, ClassDeclarationSyntax classDeclaration)
        {
            var operatorDeclarations = classDeclaration.DescendantNodes().OfType<OperatorDeclarationSyntax>(); 
            if (operatorDeclarations.Any()) 
            { 
                var overloadedOperator = operatorDeclarations.First(); 
                context.ReportDiagnostic(Diagnostic.Create(OperatorOverloadedRule, overloadedOperator.GetLocation(), overloadedOperator.OperatorToken));
                return true;
            }
            return false;
        }

        #endregion

        #region ArraySignatureChecking

        /// <summary>
        /// Looks at all the methods declared on the given interface and checks them for improper array types (System.Array instances, multidimensional, jagged)
        /// </summary>
        /// <param name="context"></param>
        /// <param name="interfaceDeclaration"></param>
        /// <returns>True iff any of the invalid array types are used in any of the method signatures on the given interface</returns>
        public bool Interface_CheckArraySignature(ref GeneratorExecutionContext context, InterfaceDeclarationSyntax interfaceDeclaration)
        {
            bool found = false;
            var methodDeclarations = interfaceDeclaration.DescendantNodes().OfType<MethodDeclarationSyntax>();
            foreach (var method in methodDeclarations) // interfaces don't have private methods, right?
            {
                found |= Method_CheckForSystemArrayType<QualifiedNameSyntax>(ref context, method, interfaceDeclaration.Identifier, "System.Array");
                found |= Method_CheckForSystemArrayType<IdentifierNameSyntax>(ref context, method, interfaceDeclaration.Identifier, "Array");
                found |= CheckForInvalidArrayType(method.DescendantNodes().OfType<ArrayTypeSyntax>(), ref context, interfaceDeclaration.Identifier, method.Identifier, method.GetLocation());
            }
            return found;
        }

        /// <summary>
        /// Looks at all the properties of the given class and checks them for improper array types (System.Array instances, multidimensional, jagged)
        /// </summary>
        /// <param name="context"></param>
        /// <param name="classDeclaration"></param>
        /// <returns>True iff any of the invalid array types are used in any of the propertyy signatures in the given class</returns>
        public bool Class_CheckArraySignature(ref GeneratorExecutionContext context, ClassDeclarationSyntax classDeclaration)
        {
            bool found = false;
            var props = classDeclaration.DescendantNodes().OfType<PropertyDeclarationSyntax>();
            foreach (var prop in props.Where(PropertyIsPublic))
            {
                found |= Property_CheckForSystemArrayType<QualifiedNameSyntax>(ref context, prop, classDeclaration.Identifier, "System.Array");
                found |= Property_CheckForSystemArrayType<IdentifierNameSyntax>(ref context, prop, classDeclaration.Identifier, "Array");
                found |= CheckForInvalidArrayType(prop.DescendantNodes().OfType<ArrayTypeSyntax>(), ref context, classDeclaration.Identifier, prop.Identifier, prop.GetLocation());
            }
            return found;
        }

        /// <summary>
        /// Look at all the array types and if any are of the form [][]+ or [,+] then raise the corresponding diagnostic and return true
        /// </summary>
        /// <param name="arrTypes"></param>
        /// <param name="context"></param>
        /// <param name="typeIdentifier">The type the array lives in</param>
        /// <param name="fieldIdentifier">The code the array is a part of the signature for; e.g. property or method</param>
        /// <param name="loc"></param>
        /// <returns>True iff any of the array types given are multidimensional or jagged</returns>
        private bool CheckForInvalidArrayType(IEnumerable<ArrayTypeSyntax> arrTypes, ref GeneratorExecutionContext context, SyntaxToken typeIdentifier, SyntaxToken fieldIdentifier, Location loc)
        {
            if (arrTypes.Any())
            {
                foreach (var arrType in arrTypes)
                {
                    var rankSpecs = arrType.DescendantNodes().OfType<ArrayRankSpecifierSyntax>();
                    if (rankSpecs.Count() > 1)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(ArraySignature_JaggedArrayRule, loc, typeIdentifier, fieldIdentifier));
                        return true; // could do or-eq on a `found` boolean instead of exiting as soon as we see invalid...
                    }
                    else if (rankSpecs.Count() == 1 && rankSpecs.First().ToString().Contains(","))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(ArraySignature_MultiDimensionalArrayRule, loc, typeIdentifier, fieldIdentifier));
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Checks method signatures with nodes of the generic type given for the given typename
        /// </summary>
        /// <typeparam name="T">
        /// The method is generic because we need to use this on fields that are of type QualifiedNameSyntax and IdentifierSyntax; e.g. the System.Array type and Array type
        /// </typeparam>
        /// <param name="context">Compilation unit</param>
        /// <param name="method">The method declaration</param>
        /// <param name="classIdentifier">The type the method lives in</param>
        /// <param name="typeName">The string representation of the type we are looking for -- either "Array" or "System.Array"</param>
        /// <returns>True iff the return type of the given method matches the given type</returns>
        private bool Method_CheckForSystemArrayType<T>(ref GeneratorExecutionContext context, MethodDeclarationSyntax method, SyntaxToken classIdentifier, string typeName)
        { 
            var qualName = method.DescendantNodes().OfType<T>(); 
            if (qualName.Any() && qualName.First().ToString().Equals(typeName)) 
            { 
                context.ReportDiagnostic(Diagnostic.Create(ArraySignature_SystemArrayRule, method.GetLocation(), classIdentifier, method.Identifier));
                return true;
            }
            return false;
        }

        /// <summary>
        /// Checks property signatures with nodes of the generic type given for the given typename
        /// </summary>
        /// <typeparam name="T">
        /// The method is generic because we need to use this on fields that are of type QualifiedNameSyntax and IdentifierSyntax; e.g. the System.Array type and Array type
        /// </typeparam>
        /// <param name="context">Compilation unit</param>
        /// <param name="prop">The property declaration</param>
        /// <param name="classIdentifier">The type the property lives in</param>
        /// <param name="typeName">The string representation of the type we are looking for -- either "Array" or "System.Array"</param>
        /// <returns>True if the return type of the given property matches the given type</returns>
        private bool Property_CheckForSystemArrayType<T>(ref GeneratorExecutionContext context, PropertyDeclarationSyntax prop, SyntaxToken classIdentifier, string typeName)
        { 
            var qualName = prop.DescendantNodes().OfType<T>(); 
            if (qualName.Any() && qualName.First().ToString().Equals(typeName)) 
            { 
                context.ReportDiagnostic(Diagnostic.Create(ArraySignature_SystemArrayRule, prop.GetLocation(), classIdentifier, prop.Identifier));
                return true;
            }
            return false;
        }

        #endregion

        /// <summary>
        /// Loops over each method declared in the given class and checks for various diagnostics
        /// </summary>
        /// <param name="context"></param>
        /// <param name="classDeclaration"></param>
        /// <returns>True iff any of the diagnostics checked for are found.</returns>
        public bool HasErrorsInMethods(ref GeneratorExecutionContext context, ClassDeclarationSyntax classDeclaration)
        {
            bool found = false;
            IEnumerable<MethodDeclarationSyntax> methods = classDeclaration.ChildNodes().OfType<MethodDeclarationSyntax>();

            Dictionary<string, bool> methodsHasAttributeMap = new Dictionary<string, bool>();
            
            /* we can't throw the diagnostic as soon as we see a second overload without an attribute, 
             *   as there could be a third overload with the default attribute
             * we use this map to keep track of these cases, and after we have checked all methods, 
             *   we check the elements of the map to raise diagnostics for those that didn't get attributed */
            Dictionary<string, Diagnostic> overloadsWithoutAttributeMap = new Dictionary<string, Diagnostic>();

            foreach (MethodDeclarationSyntax method in methods.Where(MethodIsPublic))
            {
                /* Gather information on overloaded methods; make sure there is only one marked DefaultOverload  */
                found |= CheckOverloadAttributes(method, ref methodsHasAttributeMap, ref overloadsWithoutAttributeMap, classDeclaration.Identifier, ref context);

                /* make sure no parameter has the name "__retval" */
                found |= HasParameterNamedValue(ref context, method);

                /* see if method signature contains the types System.Array or Array */
                found |= Method_CheckForSystemArrayType<QualifiedNameSyntax>(ref context, method, classDeclaration.Identifier, "System.Array");
                found |= Method_CheckForSystemArrayType<IdentifierNameSyntax>(ref context, method, classDeclaration.Identifier, "Array");
                found |= CheckForInvalidArrayType(method.DescendantNodes().OfType<ArrayTypeSyntax>(), ref context, classDeclaration.Identifier, method.Identifier, method.GetLocation());
            }

            /* Finishes up the work started by `CheckOverloadAttributes` */
            foreach (var thing in overloadsWithoutAttributeMap)
            {
                context.ReportDiagnostic(thing.Value);
                found |= true;
            }
            return found;
        }

        #region StructChecks

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
         *   returns true iff there is a field of the given type in the given struct 
         *   e.g., if T is PropertyDeclarationSyntax, then if the struct has a property, we report a diagnostic and return true */
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

        #endregion
    }
}
