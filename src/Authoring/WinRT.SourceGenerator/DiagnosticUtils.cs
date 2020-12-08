using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using WinRT.SourceGenerator;

namespace Generator 
{
    public class WinRTRules
    {
        private bool SyntaxTokenIs(SyntaxToken stx, string str) { return stx.Value.Equals(str); }

        #region ModifierHelpers

        private bool ModifiersContains(SyntaxTokenList modifiers, string str)
        {
            foreach (var modifier in modifiers)
            {
                if (SyntaxTokenIs(modifier, str)) { return true; }
            }
            return false;
        }

        private bool PropertyIsPublic(PropertyDeclarationSyntax p) { return ModifiersContains(p.Modifiers, "public"); }
        private bool MethodIsPublic(MethodDeclarationSyntax m) { return ModifiersContains(m.Modifiers, "public"); }
        public bool ClassIsPublic(ClassDeclarationSyntax c) { return ModifiersContains(c.Modifiers, "public"); }
        public bool InterfaceIsPublic(InterfaceDeclarationSyntax i) { return ModifiersContains(i.Modifiers, "public"); }

        #endregion

        #region AttributeHelpers

        /// <summary>
        /// Attributes can come in one list or many, e.g. [A()][B()] vs. [A(),B()]
        /// look at all possible attributes and see if any match the given string
        /// </summary>
        /// <param name="attrName">
        /// attribute names need to be fully qualified, 
        /// e.g. DefaultOverload is really Windows.Foundation.Metadata.DefaultOverload
        /// </param>
        /// <param name="ls">all the syntax nodes that correspond to an attribute list</param>
        /// <returns>true iff the given attribute is in the list</returns>
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

        /// <summary>
        /// Looks at all possible attributes on a given method declaration 
        /// </summary>
        /// <returns>
        /// returns true iff any are (string) equal to the given attribute name 
        /// </returns>
        private bool MethodHasAttribute(string attrName, MethodDeclarationSyntax method) { return MatchesAnyAttribute(attrName, method.AttributeLists); }

        /// <summary>
        /// Looks at all possible attributes on a given parameter declaration 
        /// </summary>
        /// <returns>
        /// returns true iff any are (string) equal to the given attribute name 
        /// </returns>
        private bool ParamHasAttribute(string attrName, ParameterSyntax param) { return MatchesAnyAttribute(attrName, param.AttributeLists); }

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
        /// <param name="classId">The class the method lives in -- used for creating the diagnostic</param>
        /// <param name="context">The SourceGenerator context the code lives in</param>
        /// <returns>True iff multiple overloads of a method are found, where more than one has been designated as the default overload</returns>
        private bool CheckOverloadAttributes(MethodDeclarationSyntax method,
            ref Dictionary<string, bool> methodHasAttributeMap,
            ref Dictionary<string, Diagnostic> overloadsWithoutAttributeMap,
            SyntaxToken classId,
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
                        methodArity, method.Identifier, classId));
                    found |= true;
                }
                else if (!hasDefaultOverloadAttribute && !methodHasAttrAlready)
                {
                    // we could see this method later with the attribute, so hold onto the diagnostic for it until we know it doesn't have the attribute
                    Diagnostic diagnostic = Diagnostic.Create(MethodOverload_NeedDefaultAttribute, method.GetLocation(), methodArity, method.Identifier, classId);
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
                        context.ReportDiagnostic(Diagnostic.Create(DiagnosticRules.AsyncRule, classDeclaration.GetLocation(), classDeclaration.Identifier, interfaceImplemented));
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

        /// <summary>
        /// keeps track of the arity of all constructors, and reports the diagnostic (and exits) as soon as a two constructors of the same arity are seen.
        /// </summary>
        /// <param name="context">Object to raise diagnostic on</param>
        /// <param name="classDeclaration">look for constructors of this class</param>
        /// <returns>True if multiple constructors of the same arity exist for the given class</returns>
        public bool HasMultipleConstructorsOfSameArity(ref GeneratorExecutionContext context, ClassDeclarationSyntax classDeclaration)
        {
            IEnumerable<ConstructorDeclarationSyntax> constructors = classDeclaration.ChildNodes().OfType<ConstructorDeclarationSyntax>();

            // A true entry means a constructor of that arity has been seen
            Dictionary<int, bool> aritiesSeenSoFar = new Dictionary<int, bool>();

            foreach (ConstructorDeclarationSyntax constructor in constructors)
            {
                int arity = constructor.ParameterList.Parameters.Count;
                if (aritiesSeenSoFar.ContainsKey(arity))
                {
                    context.ReportDiagnostic(Diagnostic.Create(DiagnosticRules.ClassConstructorRule, constructor.GetLocation(), classDeclaration.Identifier, arity));
                    return true;
                }
                else
                {
                    aritiesSeenSoFar[arity] = true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks each type in the given list of types and sees if any are equal to the given type name
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context"></param>
        /// <param name="maybeQualName">A list of the descendent nodes that are of the given type, possibly empty. 
        /// empty example: this property doesnt have any qualified types in its signature
        /// </param>
        /// <param name="typeName">check to see if this type appears in the signature</param>
        /// <param name="classId">name of the class this field is in</param>
        /// <param name="loc">syntax location</param>
        /// <param name="fieldKind">either property or method</param>
        /// <returns>true if the given type is the same as the one in the list</returns>
        private bool SignatureContainsTypeName<T>(ref GeneratorExecutionContext context, 
            IEnumerable<T> typesInSignature, 
            string typeName, 
            SyntaxToken classId, 
            Location loc, 
            SyntaxToken fieldKind)
        {
            foreach (T name in typesInSignature)
            {
                if (name.ToString().Equals(typeName))
                {
                    context.ReportDiagnostic(Diagnostic.Create(DiagnosticRules.ArraySignature_SystemArrayRule, loc, classId, fieldKind)); 
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks to see if the class declares any operators (overloading them)
        /// </summary>
        /// <param name="context">Compilation unit</param>
        /// <param name="classDeclaration">
        /// Class to check for operator declarations 
        /// operator declarations are just like method declarations except they use the `operator` keyword 
        /// </param>
        /// <returns>True iff an operator is overloaded by the given class</returns>
        public bool OverloadsOperator(ref GeneratorExecutionContext context, ClassDeclarationSyntax classDeclaration)
        {
            var operatorDeclarations = classDeclaration.DescendantNodes().OfType<OperatorDeclarationSyntax>();
            foreach (var op in operatorDeclarations)
            { 
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticRules.OperatorOverloadedRule, op.GetLocation(), op.OperatorToken));
                return true;
            } 
            return false;
        }

        #region ArraySignatureChecking
        
        /// <summary>
        /// Looks at all the properties of the given class and checks them for improper array types (System.Array instances, multidimensional, jagged)
        /// </summary>
        /// <param name="context"></param>
        /// <param name="classDeclaration"></param>
        /// <returns>True iff any of the invalid array types are used in any of the propertyy signatures in the given class</returns>
        public bool CheckPropertiesForArrayTypes(ref GeneratorExecutionContext context, ClassDeclarationSyntax classDeclaration)
        {
            bool found = false;
            var props = classDeclaration.DescendantNodes().OfType<PropertyDeclarationSyntax>().Where(PropertyIsPublic);
            foreach (var prop in props)
            {
                var loc = prop.GetLocation();
                var propId = prop.Identifier;
                var classId = classDeclaration.Identifier;

                var qualName = prop.DescendantNodes().OfType<QualifiedNameSyntax>(); 
                found |= SignatureContainsTypeName(ref context, qualName, "System.Array", classId, loc, propId);

                var idName = prop.DescendantNodes().OfType<IdentifierNameSyntax>(); 
                found |= SignatureContainsTypeName(ref context, idName, "Array", classId, loc, propId);
                
                found |= ArrayIsntOneDim(prop.DescendantNodes().OfType<ArrayTypeSyntax>(), ref context, classId, propId, loc);
            }
            return found;
        }

        /// <summary>
        /// Look at all the array types and if any are of the form [][]+ or [,+] then raise the corresponding diagnostic and return true
        /// </summary>
        /// <param name="arrTypes"></param>
        /// <param name="context"></param>
        /// <param name="typeIdentifier">The type the array lives in</param>
        /// <param name="fieldId">The code the array is a part of the signature for; e.g. property or method</param>
        /// <param name="loc"></param>
        /// <returns>True iff any of the array types given are multidimensional or jagged</returns>
        private bool ArrayIsntOneDim(IEnumerable<ArrayTypeSyntax> arrTypes, ref GeneratorExecutionContext context, SyntaxToken typeIdentifier, SyntaxToken fieldId, Location loc)
        {
            foreach (var arrType in arrTypes)
            {
                var brackets = arrType.DescendantNodes().OfType<ArrayRankSpecifierSyntax>();
                if (brackets.Count() > 1) // [][]+ ?
                {
                    context.ReportDiagnostic(Diagnostic.Create(DiagnosticRules.ArraySignature_JaggedArrayRule, loc, fieldId, typeIdentifier));
                    return true; // could do or-eq on a `found` boolean instead of exiting as soon as we see invalid...
                }
                // [,_] ? 
                else if (brackets.Count() == 1 && brackets.First().ToString().Contains(","))
                {
                    context.ReportDiagnostic(Diagnostic.Create(DiagnosticRules.ArraySignature_MultiDimensionalArrayRule, loc, fieldId, typeIdentifier));
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region Parameters

        private bool ParamHasInOrOutAttribute(ParameterSyntax param)
        {
            string baseString = ""; //"System.Runtime.InteropServices.";
            return ParamHasAttribute(baseString + "In", param) || ParamHasAttribute(baseString + "Out", param);
        }

        /// <summary>
        /// e.g. `int foo(out int i) { ... }`
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        private bool ParamMarkedOutput(ParameterSyntax param) { return ModifiersContains(param.Modifiers, "out"); }

        /// <summary>
        /// e.g. `int foo(ref int i) { ... }`
        /// </summary>
        /// <param name="param">the parameter to look for the ref keyword on</param>
        /// <returns></returns>
        private bool ParamMarkedRef(ParameterSyntax param) { return ModifiersContains(param.Modifiers, "ref"); }

        /// <summary>
        /// The code generation process makes functions with output param `__retval`, 
        /// we will shadow a user variable named the same thing -- so raise a diagnostic instead
        /// </summary>
        /// <param name="context">compilation unit to raise diagnostic on</param>
        /// <param name="method">the method whose parameteres we are inspecting</param>
        /// <returns></returns>
        public bool HasConflictingParameterName(ref GeneratorExecutionContext context, MethodDeclarationSyntax method)
        {
            foreach (ParameterSyntax parameter in method.ParameterList.Parameters)
            {
                if (SyntaxTokenIs(parameter.Identifier, "__retval"))
                {
                    context.ReportDiagnostic(Diagnostic.Create(DiagnosticRules.ParameterNamedValueRule, parameter.GetLocation(), method.Identifier, parameter.Identifier));
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        ///  Checks to see if an array parameter has been marked with both Write and Read attributes
        ///  Does extra work, by catching `ref` params, done here since this code can be used by class or interface related methods
        /// </summary>
        /// <param name="method"></param>
        /// <param name="classIdentifier"></param>
        /// <param name="context"></param>
        /// <returns>true if array attributes are invalid (see summary)</returns>
        private bool CheckParamsForArrayAttributes(MethodDeclarationSyntax method, ref GeneratorExecutionContext context)
        {
            bool found = false;
            foreach (ParameterSyntax param in method.ParameterList.Parameters)
            {
                var isArrayType = param.ChildNodes().OfType<ArrayTypeSyntax>().Any();
                bool hasReadOnlyArray = ParamHasAttribute("ReadOnlyArray", param);
                bool hasWriteOnlyArray = ParamHasAttribute("WriteOnlyArray", param);
                bool isOutputParam = ParamMarkedOutput(param);

                // Nothing can be marked `ref`
                if (ParamMarkedRef(param))
                {
                    context.ReportDiagnostic(Diagnostic.Create(RefParameterFound, method.GetLocation(), method.Identifier, param.Identifier));
                    found |= true;
                }
                

                if (ParamHasInOrOutAttribute(param))
                {
                    // recommend using ReadOnlyArray or WriteOnlyArray
                    if (isArrayType)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(ArrayMarkedInOrOut, method.GetLocation(), method.Identifier, param.Identifier));
                        found |= true;
                    }
                    // if not array type, stil can't use [In] or [Out]
                    else
                    {
                        context.ReportDiagnostic(Diagnostic.Create(NonArrayMarkedInOrOut, method.GetLocation(), method.Identifier, param.Identifier));
                        found |= true;
                    }
                }

                if (isArrayType)
                {
                    // can't be both ReadOnly and WriteOnly
                    if (hasReadOnlyArray && hasWriteOnlyArray)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(ArrayParamMarkedBoth, method.GetLocation(), method.Identifier, param.Identifier));
                        found |= true;
                    }
                    // can't be both output (writeonly) and marked read only
                    else if (hasReadOnlyArray && isOutputParam)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(ArrayOutputParamMarkedRead, method.GetLocation(), method.Identifier, param.Identifier));
                        found |= true;
                    }
                    // must have some indication of ReadOnly or WriteOnly
                    else if (!hasWriteOnlyArray && !hasReadOnlyArray && !isOutputParam) 
                    { 
                        context.ReportDiagnostic(Diagnostic.Create(ArrayParamNotMarked, method.GetLocation(), method.Identifier, param.Identifier
                            , hasWriteOnlyArray, hasReadOnlyArray, isOutputParam));
                        found |= true;
                    }
                }
                // Non-array types shouldn't have attributes meant for arrays
                else if (hasWriteOnlyArray || hasReadOnlyArray)
                {
                    context.ReportDiagnostic(Diagnostic.Create(NonArrayMarked, method.GetLocation(), method.Identifier, param.Identifier));
                    found |= true;
                }
            }

            return found;
        }

#endregion

        #region CheckingMethods

        /// <summary>
        /// Loops over each method declared in the given class and checks for various diagnostics
        /// </summary>
        /// <param name="context"></param>
        /// <param name="classDeclaration"></param>
        /// <returns>True iff any of the diagnostics checked for are found.</returns>
        public bool ClassHasInvalidMethods(ref GeneratorExecutionContext context, ClassDeclarationSyntax classDeclaration)
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
    
                found |= CheckParamsForArrayAttributes(method, ref context);

                /* Gather information on overloaded methods; make sure there is only one marked DefaultOverload  */
                found |= CheckOverloadAttributes(method, ref methodsHasAttributeMap, ref overloadsWithoutAttributeMap, classDeclaration.Identifier, ref context);

                /* make sure no parameter has the name "__retval" */
                found |= HasConflictingParameterName(ref context, method);

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

        /// <summary>
        /// Looks at all the methods declared on the given interface and checks them for improper array types (System.Array instances, multidimensional, jagged)
        /// </summary>
        /// <param name="context"></param>
        /// <param name="interfaceDeclaration"></param>
        /// <returns>True iff any of the invalid array types are used in any of the method signatures on the given interface</returns>
        public bool InterfaceHasInvalidMethods(ref GeneratorExecutionContext context, InterfaceDeclarationSyntax interfaceDeclaration)
        {
            bool found = false;
            var methodDeclarations = interfaceDeclaration.DescendantNodes().OfType<MethodDeclarationSyntax>();
            foreach (var method in methodDeclarations) 
            {
                var loc = method.GetLocation();
                var methodId = method.Identifier;
                var interfaceId = interfaceDeclaration.Identifier;

                var qualifiedNames = method.DescendantNodes().OfType<QualifiedNameSyntax>();
                found |= SignatureContainsTypeName(ref context, qualifiedNames, "System.Array", interfaceId, loc, methodId);

                var identifiers = method.DescendantNodes().OfType<IdentifierNameSyntax>();
                found |= SignatureContainsTypeName(ref context, identifiers, "Array", interfaceId, loc, methodId);

                found |= ArrayIsntOneDim(method.DescendantNodes().OfType<ArrayTypeSyntax>(), ref context, interfaceId, methodId, loc);

                found |= CheckParamsForArrayAttributes(method, ref context);
            }
            return found;
        }
        #endregion

        #region Structs

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
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticRules.StructHasInvalidFieldRule2, 
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
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticRules.StructHasPrivateFieldRule, field.GetLocation(), structDeclaration.Identifier));
                found |= true;
            } 

            foreach (var variable in field.DescendantNodes().OfType<VariableDeclarationSyntax>())
            {
                var typeStr = variable.Type.ToString();

                List<string> invalidTypes = new List<string> { "object", "byte", "dynamic" };
                invalidTypes.AddRange(classNames);
                
                if (invalidTypes.Contains(typeStr))
                { 
                    context.ReportDiagnostic(Diagnostic.Create(DiagnosticRules.StructHasInvalidFieldRule,
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
