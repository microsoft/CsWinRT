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
            // 
            "In class {2}: The {0}-parameter overloads of {1} must have exactly one method specified as the default overload by decorating it with Windows.Foundation.Metadata.DefaultOverloadAttribute.");

        static DiagnosticDescriptor ArraySignature_JaggedArrayRule = MakeRule(
            "WME10??",
            "Array signature found with jagged array, which is not a valid WinRT type", // todo better msg
            //
            "In type {0}: the method {1} has signature that contains a jagged array; use a different type like List");

        static DiagnosticDescriptor ArraySignature_MultiDimensionalArrayRule = MakeRule(
            "WME10??",
            "Array signature found with multi-dimensional array, which is not a valid WinRT type", // todo better msg
            //
            "In type {0}: the method {1} has signature that contains a multi-dimensional array; use a different type like List");

        static DiagnosticDescriptor ArraySignature_SystemArrayRule = MakeRule(
            "WME10??",
            "Array signature found with System.Array instance, which is not a valid WinRT type", // todo better msg
            //
            "In type {0}: the method {1} has signature that contains a System.Array instance; use a different type like List");
        // "Method {0} has a multi-dimensional array of type {1} in its signature. Arrays in Windows Runtime must be one dimensional"

        static DiagnosticDescriptor ArrayMarkedInOrOut = MakeRule(
            "WME1103",
            "Array parameter marked InAttribute or OutAttribute",
            //
            "Method '{0}' has parameter '{1}' which is an array, and which has either a "
            + "System.Runtime.InteropServices.InAttribute or a System.Runtime.InteropServices.OutAttribute. "
            + "In the Windows Runtime, array parameters must have either ReadOnlyArray or WriteOnlyArray. "
            + "Please remove these attributes or replace them with the appropriate Windows "
            + "Runtime attribute if necessary.");

        static DiagnosticDescriptor NonArrayMarkedInOrOut = MakeRule(
            "WME1105",
            "Parameter (not array type) marked InAttribute or OutAttribute",
            "Method '{0}' has parameter '{1}' with a System.Runtime.InteropServices.InAttribute "
            //
            + "or System.Runtime.InteropServices.OutAttribute.Windows Runtime does not support "
            + "marking parameters with System.Runtime.InteropServices.InAttribute or "
            + "System.Runtime.InteropServices.OutAttribute. Please consider removing "
            + "System.Runtime.InteropServices.InAttribute and replace "
            + "System.Runtime.InteropServices.OutAttribute with 'out' modifier instead.");

        static DiagnosticDescriptor ArrayParamMarkedBoth = MakeRule(
            "WME1101",
            "Array paramter marked both ReadOnlyArray and WriteOnlyArray",
            //
            "Method '{0}' has parameter '{1}' which is an array, and which has both ReadOnlyArray and WriteOnlyArray. "
            + "In the Windows Runtime, the contents array parameters must be either readable "
            + "or writable.Please remove one of the attributes from '{1}'.");

        static DiagnosticDescriptor ArrayOutputParamMarkedRead = MakeRule(
            "WME1102",
            "Array parameter marked `out` and ReadOnlyArray",
            //
            "Method '{0}' has an output parameter '{1}' which is an array, but which has ReadOnlyArray attribute. In the Windows Runtime, "
            + "the contents of output arrays are writable.Please remove the attribute from '{1}'.");

        static DiagnosticDescriptor ArrayParamNotMarked = MakeRule(
            "WME1106",
            "Array parameter not marked ReadOnlyArray or WriteOnlyArray way",
            //
            "Method '{0}' has parameter '{1}' which is an array. In the Windows Runtime, the "
            + "contents of array parameters must be either readable or writable.Please apply either ReadOnlyArray or WriteOnlyArray to '{1}'.");

        static DiagnosticDescriptor NonArrayMarked = MakeRule(
            "WME1104",
            "Non-array parameter marked with ReadOnlyArray or WriteOnlyArray",
            // 
            "Method '{0}' has parameter '{1}' which is not an array, and which has either a "
            + "ReadOnlyArray attribute or a WriteOnlyArray attribute . Windows Runtime does "
            + "not support marking non-array parameters with ReadOnlyArray or WriteOnlyArray.");

        #endregion

        private bool SyntaxTokenIs(SyntaxToken stx, string str)
        {
            return stx.Value.Equals(str);
        }


        #region ModifierHelpers

        private bool ModifierIs(SyntaxTokenList modifiers, string str)
        { 
            foreach (var modifier in modifiers)
            {
                if (SyntaxTokenIs(modifier, str)) //thing.ValueText.Equals(str)) 
                { 
                    return true; 
                }
            }
            return false;
        }

        private bool PropertyIsPublic(PropertyDeclarationSyntax p) { return ModifierIs(p.Modifiers, "public"); }
        private bool MethodIsPublic(MethodDeclarationSyntax m) { return ModifierIs(m.Modifiers, "public"); }
        public bool ClassIsPublic(ClassDeclarationSyntax c) { return ModifierIs(c.Modifiers, "public"); }
        public bool InterfaceIsPublic(InterfaceDeclarationSyntax i) { return ModifierIs(i.Modifiers, "public"); }

        #endregion

        #region AttributeHelpers

        private bool MatchesAnyAttribute(string attrName, SyntaxList<AttributeListSyntax> ls) 
        {
            foreach (var attrList in ls) 
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

        
        /// MethodHasAttribute looks at all possible attributes on a given method declaration 
        ///     returns true iff any are (string) equal to the given attribute name 
        private bool MethodHasAttribute(string attrName, MethodDeclarationSyntax method) { return MatchesAnyAttribute(attrName, method.AttributeLists); }

        private bool ParamHasAttribute(string attrName, ParameterSyntax param) { return MatchesAnyAttribute(attrName, param.AttributeLists); }

        #endregion

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

        #region NeedsDocumentation
        /* HasMultipleConstructorsOfSameArity 
         *  keeps track of the arity of all constructors, and reports the diagnostic (and exits) as soon as a two constructors of the same arity are seen. */
        public bool HasMultipleConstructorsOfSameArity(ref GeneratorExecutionContext context, ClassDeclarationSyntax classDeclaration)
        {
            IEnumerable<ConstructorDeclarationSyntax> constructors = classDeclaration.ChildNodes().OfType<ConstructorDeclarationSyntax>();
            
            Dictionary<int,bool> aritiesSeenSoFar = new Dictionary<int, bool>();

            foreach (ConstructorDeclarationSyntax constructor in constructors)
            {
                int arity = constructor.ParameterList.Parameters.Count;
                if (aritiesSeenSoFar.ContainsKey(arity))
                {
                    context.ReportDiagnostic(Diagnostic.Create(ClassConstructorRule, constructor.GetLocation(), classDeclaration.Identifier, arity));
                    return true;
                }
                else
                {
                    aritiesSeenSoFar[arity] = true;
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
                if (SyntaxTokenIs(parameter.Identifier, "__retval"))
                { 
                    context.ReportDiagnostic(Diagnostic.Create(ParameterNamedValueRule, parameter.GetLocation(), method.Identifier, parameter.Identifier)); 
                    return true; 
                } 
            }
            return false;
        }

        #endregion

        #region good
        /// <summary>
        /// Keeps track of repeated declarations of a method (overloads) and raises diagnostics according to the rule that exactly one overload should be attributed the default
        /// </summary>
        /// <param name="method">
        /// The method to check attributes for</param>
        /// <param name="methodHasAttributeMap">
        /// Keeps track of the method (via name + arity) and whether it was declared with the DefaultOverload attribute</param>
        /// <param name="overloadsWithoutAttributeMap">
        ///     Keeps track of the methods that are overloads but don't have the DefaultOverload attribute (yet)
        ///     Used after this function executes, hence the reference parameter 
        ///     </param>
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
        #endregion

        #region Misc

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
                // found |= SignatureHasArrayType<QualifiedNameSyntax>(ref context, method, interfaceDeclaration.Identifier, "System.Array");

                var qualName = method.DescendantNodes().OfType<QualifiedNameSyntax>();
                found |= SignatureContainsTypeName(ref context, qualName, "System.Array", interfaceDeclaration.Identifier, method.GetLocation(), method.Identifier);

                // found |= SignatureHasArrayType<IdentifierNameSyntax>(ref context, method, interfaceDeclaration.Identifier, "Array");

                var idName = method.DescendantNodes().OfType<IdentifierNameSyntax>();
                found |= SignatureContainsTypeName(ref context, idName, "Array", interfaceDeclaration.Identifier, method.GetLocation(), method.Identifier);

                found |= ArrayIsntOneDim(method.DescendantNodes().OfType<ArrayTypeSyntax>(), ref context, interfaceDeclaration.Identifier, method.Identifier, method.GetLocation());
            }
            return found;
        }

        /*
        private bool Foo<T>(ref GeneratorExecutionContext context, IEnumerable<T> props, SyntaxToken classId, SyntaxToken propId)
            where T : Microsoft.CodeAnalysis.CSharp.Syntax.BasePropertyDeclarationSyntax 
        {
            bool found = false;
            foreach (var prop in props)
            {
                found |= Property_CheckForSystemArrayType<QualifiedNameSyntax>(ref context, prop, classId, "System.Array");
                var qualName = prop.DescendantNodes().OfType<QualifiedNameSyntax>(); 
                return Helper<T>(ref context, qualName, typeName, classId, prop.GetLocation(), prop.Identifier);

                found |= Property_CheckForSystemArrayType<IdentifierNameSyntax>(ref context, prop, classId, "Array");

                found |= ArrayIsntOneDim(prop.DescendantNodes().OfType<ArrayTypeSyntax>(), ref context, classId, propId, prop.GetLocation());
            }
            return found;
        }
        */

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
                var qualName = prop.DescendantNodes().OfType<QualifiedNameSyntax>(); 
                found |= SignatureContainsTypeName(ref context, qualName, "System.Array", classDeclaration.Identifier, prop.GetLocation(), prop.Identifier);
                //    |= Property_CheckForSystemArrayType<QualifiedNameSyntax>(ref context, prop, classDeclaration.Identifier, "System.Array");

                var idName = prop.DescendantNodes().OfType<IdentifierNameSyntax>(); 
                found |= SignatureContainsTypeName(ref context, idName, "Array", classDeclaration.Identifier, prop.GetLocation(), prop.Identifier);
                //    |= Property_CheckForSystemArrayType<IdentifierNameSyntax>(ref context, prop, classDeclaration.Identifier, "Array");
                
                //
                found |= ArrayIsntOneDim(prop.DescendantNodes().OfType<ArrayTypeSyntax>(), ref context, classDeclaration.Identifier, prop.Identifier, prop.GetLocation());
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
        private bool ArrayIsntOneDim(IEnumerable<ArrayTypeSyntax> arrTypes, ref GeneratorExecutionContext context, SyntaxToken typeIdentifier, SyntaxToken fieldIdentifier, Location loc)
        {
            if (arrTypes.Any())
            {
                foreach (var arrType in arrTypes)
                {
                    var brackets = arrType.DescendantNodes().OfType<ArrayRankSpecifierSyntax>();

                    if (brackets.Count() > 1) // [][]+ ?
                    {
                        context.ReportDiagnostic(Diagnostic.Create(ArraySignature_JaggedArrayRule, loc, typeIdentifier, fieldIdentifier));
                        return true; // could do or-eq on a `found` boolean instead of exiting as soon as we see invalid...
                    }
                    // [,_] ? 
                    else if (brackets.Count() == 1 && brackets.First().ToString().Contains(",")) 
                    {
                        context.ReportDiagnostic(Diagnostic.Create(ArraySignature_MultiDimensionalArrayRule, loc, typeIdentifier, fieldIdentifier));
                        return true;
                    }
                }
            }
            return false;
        }

        private bool SignatureContainsTypeName<T>(ref GeneratorExecutionContext context, IEnumerable<T> qualName, string typeName, SyntaxToken classIdentifier, Location loc, SyntaxToken signatureKind)
        {
            if (qualName.Any() && qualName.First().ToString().Equals(typeName))
            {
                context.ReportDiagnostic(Diagnostic.Create(ArraySignature_SystemArrayRule, loc, classIdentifier, signatureKind));
                return true;
            }
            return false;
        }

        #endregion

        #region cutHuh
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
            return SignatureContainsTypeName<T>(ref context, qualName, typeName, classIdentifier, prop.GetLocation(), prop.Identifier);
            /*
             * if (qualName.Any() && qualName.First().ToString().Equals(typeName)) 
            { 
                context.ReportDiagnostic(Diagnostic.Create(ArraySignature_SystemArrayRule, prop.GetLocation(), classIdentifier, prop.Identifier));
                return true;
            }
            return false;
            */
        }
        /// <summary>
        /// Checks method signatures to see if a given type is used 
        /// </summary>
        /// <typeparam name="T">
        /// Typenames can come in two forms, qualified name or identifier
        /// </typeparam>
        /// <param name="context">Compilation unit</param>
        /// <param name="method">Method whose return type and paramters are seen</param>
        /// <param name="classIdentifier">The type the method lives in</param>
        /// <param name="typeName">The string representation of the type we are looking for -- either "Array" or "System.Array"</param>
        /// <returns>True iff the return type of the given method matches the given type</returns>
        private bool SignatureHasArrayType<T>(ref GeneratorExecutionContext context, MethodDeclarationSyntax method, SyntaxToken classIdentifier, string typeName)
        { 
            var qualName = method.DescendantNodes().OfType<T>();
            return SignatureContainsTypeName<T>(ref context, qualName, typeName, classIdentifier, method.GetLocation(), method.Identifier);
            /*if (qualName.Any() && qualName.First().ToString().Equals(typeName)) 
            { 
                context.ReportDiagnostic(Diagnostic.Create(ArraySignature_SystemArrayRule, method.GetLocation(), classIdentifier, method.Identifier));
                return true;
            }
            return false;
            */
        }
        #endregion

        private bool ParamHasInOrOutAttribute(ParameterSyntax param)
        {
            string baseString = "System.Runtime.InteropServices.";
            return ParamHasAttribute(baseString + "In", param) || ParamHasAttribute(baseString + "Out", param);
        }

        private bool ParamHasArrayAttribute(string arrayAttribute, ParameterSyntax param)
        {
            return ParamHasAttribute("To.Be.Defined." + arrayAttribute, param);
        }

        /// <summary>
        /// TODO!! Checks to see if param is an output param
        /// e.g. `int foo(out int i) { ... }`
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        private bool ParamMarkedOutput(ParameterSyntax param)
        {
            return false;
        }

        /// <summary>
        ///  Checks to see if an array parameter has been marked with both Write and Read attributes
        ///   & if  array param is marked `out` but also marked with Read attribute         [/]
        ///         array param is marked In or Out (must have Read xor Write)              [/]
        ///         array param marked both Read and Write                                  [/]
        ///         array param not marked (and no `out` tag)                               [/]
        ///         param marked {Write,Read}OnlyArray but is not an array type param       [/]
        ///         param is marked {In,Out}Attribute                                       [/]
        ///         param is marked ref                                                     [ ]
        /// </summary>
        /// <param name="method"></param>
        /// <param name="classIdentifier"></param>
        /// <param name="context"></param>
        /// <returns>true if array attributes are invalid (see summary)</returns>
        private bool CheckParamsForArrayAttributes(MethodDeclarationSyntax method, SyntaxToken classIdentifier, ref GeneratorExecutionContext context)
        {
            bool found = false;
            foreach (ParameterSyntax param in method.ParameterList.Parameters)
            {
                var isArrayType = param.ChildNodes().OfType<ArrayTypeSyntax>().Any();
                bool hasReadOnlyArray = ParamHasArrayAttribute("ReadOnlyArray", param);
                bool hasWriteOnlyArray = ParamHasArrayAttribute("WriteOnlyArray", param);
                bool isOutputParam = ParamMarkedOutput(param); // TODO: implement

                if (ParamHasInOrOutAttribute(param))
                {
                    if (isArrayType)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(ArrayMarkedInOrOut, method.GetLocation(), method.Identifier, param.Identifier));
                        found |= true;
                    }
                    else 
                    {
                        context.ReportDiagnostic(Diagnostic.Create(NonArrayMarkedInOrOut, method.GetLocation(), method.Identifier, param.Identifier));
                        found |= true;
                    }
                }

                if (isArrayType)
                {
                    if (hasReadOnlyArray && hasWriteOnlyArray)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(ArrayParamMarkedBoth, method.GetLocation(), method.Identifier, param.Identifier));
                        found |= true;
                    }
                    else if (hasReadOnlyArray)
                    {
                        if (isOutputParam)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(ArrayOutputParamMarkedRead, method.GetLocation(), method.Identifier, param.Identifier));
                            found |= true;
                        }
                    }
                    else if (hasWriteOnlyArray) { }
                    else
                    {
                        if (!isOutputParam)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(ArrayParamNotMarked, method.GetLocation(), method.Identifier, param.Identifier));
                            found |= true;
                        }
                    }
                }
                else if (hasWriteOnlyArray || hasReadOnlyArray)
                {
                    context.ReportDiagnostic(Diagnostic.Create(NonArrayMarked, method.GetLocation(), method.Identifier, param.Identifier));
                    found |= true;
                }
            }
              
            return found;
        }

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

                // TODO: add diagnostic for ref keyword
                var paramList = method.ChildNodes().OfType<ParameterListSyntax>();
    
                found |= CheckParamsForArrayAttributes(method, classDeclaration.Identifier, ref context);

                /* Gather information on overloaded methods; make sure there is only one marked DefaultOverload  */
                found |= CheckOverloadAttributes(method, ref methodsHasAttributeMap, ref overloadsWithoutAttributeMap, classDeclaration.Identifier, ref context);

                /* make sure no parameter has the name "__retval" */
                found |= HasParameterNamedValue(ref context, method);

                /* see if method signature contains the types System.Array or Array */
                // found |= SignatureHasArrayType<QualifiedNameSyntax>(ref context, method, classDeclaration.Identifier, "System.Array");

                var qualName = method.DescendantNodes().OfType<QualifiedNameSyntax>();
                found |=  SignatureContainsTypeName(ref context, qualName, "System.Array", classDeclaration.Identifier, method.GetLocation(), method.Identifier);

                // found |= SignatureHasArrayType<IdentifierNameSyntax>(ref context, method, classDeclaration.Identifier, "Array");
                var idName = method.DescendantNodes().OfType<IdentifierNameSyntax>();
                found |=  SignatureContainsTypeName(ref context, idName, "Array", classDeclaration.Identifier, method.GetLocation(), method.Identifier);

                //
                found |= ArrayIsntOneDim(method.DescendantNodes().OfType<ArrayTypeSyntax>(), ref context, classDeclaration.Identifier, method.Identifier, method.GetLocation());
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
        public bool StructHasFieldOfInvalidType(ref GeneratorExecutionContext context, 
            FieldDeclarationSyntax field, 
            StructDeclarationSyntax structDeclaration, 
            List<string> classNames)
        {
            bool found = false; 

            /* No private fields allowed in Windows Runtime components */
            if (SyntaxTokenIs(field.GetFirstToken(), "private"))
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
