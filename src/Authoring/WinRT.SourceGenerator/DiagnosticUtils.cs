using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using WinRT.SourceGenerator;

namespace Generator 
{
    public class WinRTRules
    {
        private void Report(ref GeneratorExecutionContext context, DiagnosticDescriptor d, Location loc, params object[] args) 
        { 
            context.ReportDiagnostic(Diagnostic.Create(d, loc, args)); 
        }

        private bool SymbolSetHasString(HashSet<INamedTypeSymbol> typeNames, string typeStr)
        {
            return typeNames.Where(sym => sym.ToString().Contains(typeStr)).Any();
        }

        private bool SyntaxTokenIs(SyntaxToken stx, string str) { return stx.Value.Equals(str); }
        
        private bool ModifiersContains(SyntaxTokenList modifiers, string str) { return modifiers.Any(modifier => modifier.ValueText == str); }

        public bool IsPublic<T>(T p) where T : MemberDeclarationSyntax { return ModifiersContains(p.Modifiers, "public");  }

        private static bool ImplementsInterface(INamedTypeSymbol typeSymbol, string typeToCheck)
        {
            if (typeSymbol == null)
            {
                return false;
            }

            if (typeSymbol.BaseType != null && typeSymbol.BaseType.MetadataName == typeToCheck)
            {
                return true;
            }

            foreach (var implementedInterface in typeSymbol.AllInterfaces)
            {
                if (implementedInterface.MetadataName == typeToCheck)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Attributes can come in one list or many, e.g. [A()][B()] vs. [A(),B()]
        /// look at all possible attributes and see if any match the given string</summary>
        /// <param name="attrName">attribute names need to be fully qualified, e.g. DefaultOverload is really Windows.Foundation.Metadata.DefaultOverload</param>
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
        /// Looks at all possible attributes on a given parameter declaration 
        /// </summary>
        /// <returns>
        /// returns true iff any are (string) equal to the given attribute name 
        /// </returns>
        private bool ParamHasAttribute(string attrName, ParameterSyntax param) { return MatchesAnyAttribute(attrName, param.AttributeLists); }

        private static readonly string[] InAndOutAttributeNames = { "In", "Out", "System.Runtime.InteropServices.In", "System.Runtime.InteropServices.Out" };
        private static readonly string[] OverloadAttributeNames = { "Windows.Foundation.Metadata.DefaultOverload", "DefaultOverload" };

        private bool ParamHasInOrOutAttribute(ParameterSyntax param)
        {
            return InAndOutAttributeNames.Where(str => ParamHasAttribute(str, param)).Any();
        }

        private bool MethodHasDefaultOverloadAttribute(MethodDeclarationSyntax method)
        {
            return OverloadAttributeNames.Where(str => MatchesAnyAttribute(str, method.AttributeLists)).Any();
        }

        /// <summary>e.g. `int foo(out int i) { ... }` /// </summary>
        /// <param name="param"></param>True if the parameter has the `ref` modifier<returns></returns>
        private bool ParamMarkedOutput(ParameterSyntax param) { return ModifiersContains(param.Modifiers, "out"); }

        /// <summary>e.g. `int foo(ref int i) { ... }` </summary>
        /// <param name="param">the parameter to look for the ref keyword on</param>
        /// <returns>True if the parameter has the `ref` modifier</returns>
        private bool ParamMarkedRef(ParameterSyntax param) { return ModifiersContains(param.Modifiers, "ref"); }

        /* SimplifySyntaxTypeString 
         *  returns the more common term for the given kind of syntax; used when creating a diagnostic for an invalid field in a struct */
        private string SimplifySyntaxTypeString(string syntaxType)
        {
            switch (syntaxType)
            {
                case "EventFieldDeclarationSyntax":  return "event";
                case "ConstructorDeclarationSyntax": return "constructor";
                case "DelegateDeclarationSyntax":    return "delegate";
                case "IndexerDeclarationSyntax":     return "indexer";
                case "MethodDeclarationSyntax":      return "method";
                case "OperatorDeclarationSyntax":    return "operator";
                case "PropertyDeclarationSyntax":    return "property";
                default: return "unknown syntax type: " + syntaxType;
            }
        }

        /// <summary>
        /// Keeps track of repeated declarations of a method (overloads) and raises diagnostics according to the rule that exactly one overload should be attributed the default</summary>
        /// <param name="context"></param><param name="method">Look for overloads of this method, checking the attributes as well attributes for</param>
        /// <param name="methodHasAttributeMap">
        /// Keeps track of the method (via qualified name + arity) and whether it was declared with the DefaultOverload attribute
        /// this variable is ref because we are mutating this map with each method, so we only look at a method a second time if it has an overload but no attribute</param>
        /// <param name="overloadsWithoutAttributeMap">
        ///     Keeps track of the methods that are overloads but don't have the DefaultOverload attribute (yet)
        ///     Used after this function executes, hence the reference parameter</param>
        /// <param name="classId">The class the method lives in -- used for creating the diagnostic</param>
        /// <returns>True iff multiple overloads of a method are found, where more than one has been designated as the default overload</returns>
        private bool CheckOverloadAttributes(ref GeneratorExecutionContext context,
            MethodDeclarationSyntax method,
            ref Dictionary<string, bool> methodHasAttributeMap,
            ref Dictionary<string, Diagnostic> overloadsWithoutAttributeMap,
            SyntaxToken classId)
        {
            bool found = false;
            int methodArity = method.ParameterList.Parameters.Count;
            string methodNameWithArity = method.Identifier.Text + methodArity.ToString();

            // look at all the attributes on this method and see if any of them is the DefaultOverload attribute 
            bool hasDefaultOverloadAttribute = MethodHasDefaultOverloadAttribute(method);
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
                    Report(ref context, DiagnosticRules.MethodOverload_MultipleDefaultAttribute, method.GetLocation(), methodArity, method.Identifier, classId);
                    found |= true;
                }
                else if (!hasDefaultOverloadAttribute && !methodHasAttrAlready)
                {
                    // we could see this method later with the attribute, so hold onto the diagnostic for it until we know it doesn't have the attribute
                    overloadsWithoutAttributeMap[methodNameWithArity] = Diagnostic.Create(DiagnosticRules.MethodOverload_NeedDefaultAttribute, method.GetLocation(), methodArity, method.Identifier, classId);
                }
            }
            else
            {
                // first time we're seeing the method, add a pair in the map for its name and whether it has the attribute 
                methodHasAttributeMap[methodNameWithArity] = hasDefaultOverloadAttribute;
            }

            return found;
        } 

        private static readonly string[] ProhibitedAsyncInterfaces = {
                "IAsyncAction",
                "IAsyncActionWithProgress`1",
                "IAsyncOperation`1",
                "IAsyncOperationWithProgress`2"
        };

        /// <summary>
        /// Returns true if the class represented by the symbol 
        /// implements any of the interfaces defined in ProhibitedAsyncInterfaces (e.g., IAsyncAction, ...) /// </summary>
        /// <param name="context"></param><param name="typeSymbol"></param><param name="classDeclaration"></param>
        /// <returns>True iff the given class implements any of the IAsync interfaces that are not valid in Windows Runtime</returns>
        public bool ImplementsAsyncInterface<T>(ref GeneratorExecutionContext context, INamedTypeSymbol typeSymbol, T typeDeclaration)
            where T : TypeDeclarationSyntax
        {
            foreach (string prohibitedInterface in ProhibitedAsyncInterfaces)
            {
                if (ImplementsInterface(typeSymbol, prohibitedInterface))
                {
                    Report(ref context, DiagnosticRules.AsyncRule, typeDeclaration.GetLocation(), typeDeclaration.Identifier, prohibitedInterface);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Raises a diagnostic when multiple constructors for a class are defined with the same arity.</summary>
        /// <param name="context"></param><param name="classDeclaration">look for constructors of this class</param>
        /// <returns>True if multiple constructors of the same arity exist for the given class</returns>
        public bool HasMultipleConstructorsOfSameArity(ref GeneratorExecutionContext context, ClassDeclarationSyntax classDeclaration)
        {
            IEnumerable<ConstructorDeclarationSyntax> constructors = classDeclaration.ChildNodes().OfType<ConstructorDeclarationSyntax>().Where(IsPublic);

            HashSet<int> aritiesSeenSoFar = new HashSet<int>();

            foreach (ConstructorDeclarationSyntax constructor in constructors)
            {
                int arity = constructor.ParameterList.Parameters.Count;
                if (aritiesSeenSoFar.Contains(arity))
                {
                    Report(ref context, DiagnosticRules.ClassConstructorRule, constructor.GetLocation(), classDeclaration.Identifier, arity);
                    return true;
                }
                else
                {
                    aritiesSeenSoFar.Add(arity);
                }
            }
            return false;
        }

        /// <summary>Checks each type in the given list of types and sees if any are equal to the given type name</summary>
        /// <typeparam name="T"></typeparam><param name="context"></param>
        /// <param name="typesInSignature">A list of the descendent nodes that are of the given type, possibly empty. 
        /// empty example: this property doesnt have any qualified types in its signature</param>
        /// <param name="typeName">check to see if this type appears in the signature</param><param name="diag">diagnostic to report if we see the typeName</param>
        /// <returns>true if the given type is the same as the one in the list</returns>
        private bool SignatureContainsTypeName<T>(ref GeneratorExecutionContext context, IEnumerable<T> typesInSignature, string typeName, Diagnostic diag)
        {
            foreach (T name in typesInSignature)
            {
                if (name.ToString().Equals(typeName))
                {
                    context.ReportDiagnostic(diag);
                    return true;
                }
            }
            return false;
        }

        /// <summary>Checks to see if the class declares any operators (overloading them)</summary>
        /// <param name="context"></param><param name="classDeclaration">
        /// Class to check for operator declarations 
        /// operator declarations are just like method declarations except they use the `operator` keyword</param>
        /// <returns>True iff an operator is overloaded by the given class</returns>
        public bool OverloadsOperator(ref GeneratorExecutionContext context, ClassDeclarationSyntax classDeclaration)
        {
            var operatorDeclarations = classDeclaration.DescendantNodes().OfType<OperatorDeclarationSyntax>();
            foreach (var op in operatorDeclarations) { Report(ref context, DiagnosticRules.OperatorOverloadedRule, op.GetLocation(), op.OperatorToken); } 
            return operatorDeclarations.Count() != 0;
        }

        /// <summary>Looks at all the properties of the given class and checks them for improper array types (System.Array instances, multidimensional, jagged)</summary>
        /// <param name="context"></param><param name="classDeclaration"></param>
        /// <returns>True iff any of the invalid array types are used in any of the propertyy signatures in the given class</returns>
        public bool CheckPropertySignature(ref GeneratorExecutionContext context, IEnumerable<PropertyDeclarationSyntax> props, SyntaxToken typeId)
        {
            bool found = false;
            foreach (var prop in props)
            {
                var loc = prop.GetLocation();
                var propId = prop.Identifier;

                var qualifiedTypes = prop.DescendantNodes().OfType<QualifiedNameSyntax>(); 
                var d = Diagnostic.Create(DiagnosticRules.ArraySignature_SystemArrayRule, loc, typeId, propId);
                found |= SignatureContainsTypeName(ref context, qualifiedTypes, "System.Array", d);
                
                var types = prop.DescendantNodes().OfType<IdentifierNameSyntax>(); 
                var d2 = Diagnostic.Create(DiagnosticRules.ArraySignature_SystemArrayRule, loc, typeId, propId);
                found |= SignatureContainsTypeName(ref context, types, "Array", d2);
                
                found |= ArrayIsntOneDim(prop.DescendantNodes().OfType<ArrayTypeSyntax>(), ref context, typeId, propId, loc);
            }
            return found;
        }

        /// <summary>
        /// Look at all the array types and if any are of the form [][]+ or [,+] then raise the corresponding diagnostic and return true</summary>
        /// <param name="arrTypes"></param><param name="context"></param><param name="typeIdentifier">The type the array lives in</param>
        /// <param name="fieldId">The code the array is a part of the signature for; e.g. property or method</param><param name="loc"></param>
        /// <returns>True iff any of the array types given are multidimensional or jagged</returns>
        private bool ArrayIsntOneDim(IEnumerable<ArrayTypeSyntax> arrTypes, 
            ref GeneratorExecutionContext context, 
            SyntaxToken typeIdentifier, 
            SyntaxToken fieldId, 
            Location loc)
        {
            foreach (var arrType in arrTypes)
            {
                var brackets = arrType.DescendantNodes().OfType<ArrayRankSpecifierSyntax>();
                // [][]+ ?
                if (brackets.Count() > 1) 
                {
                    Report(ref context, DiagnosticRules.ArraySignature_JaggedArrayRule, loc, fieldId, typeIdentifier);
                    return true;
                }
                // [,+] ? 
                else if (brackets.Count() == 1 && brackets.First().ToString().Contains(","))
                {
                    Report(ref context, DiagnosticRules.ArraySignature_MultiDimensionalArrayRule, loc, fieldId, typeIdentifier);
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// The code generation process makes functions with output param `__retval`, 
        /// we will shadow a user variable named the same thing -- so raise a diagnostic instead</summary>
        /// <param name="context">compilation unit to raise diagnostic on</param><param name="method">the method whose parameteres we are inspecting</param>
        /// <returns>True if any parameter is named "__retval"</returns>
        public bool HasConflictingParameterName(ref GeneratorExecutionContext context, MethodDeclarationSyntax method)
        {
            var hasInvalidParams = method.ParameterList.Parameters.Where(param => SyntaxTokenIs(param.Identifier, "__retval")).Any();
            if (hasInvalidParams) 
            { 
                Report(ref context, DiagnosticRules.ParameterNamedValueRule, method.GetLocation(), method.Identifier); 
            }
            return hasInvalidParams;
        }

        /// <summary>
        ///  Checks to see if an array parameter has been marked with both Write and Read attributes
        ///  Does extra work, by catching `ref` params, done here since this code can be used by class or interface related methods</summary>
        /// <param name="method"></param><param name="classIdentifier"></param><param name="context"></param>
        /// <returns>true if array attributes are invalid (see summary)</returns>
        private bool CheckParamsForArrayAttributes(MethodDeclarationSyntax method, ref GeneratorExecutionContext context)
        {
            bool found = false;
            foreach (ParameterSyntax param in method.ParameterList.Parameters)
            {
                var isArrayType = param.ChildNodes().OfType<ArrayTypeSyntax>().Any();
                bool hasReadOnlyArray = ParamHasAttribute("System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray", param);
                bool hasWriteOnlyArray = ParamHasAttribute("System.Runtime.InteropServices.WindowsRuntime.WriteOnlyArray", param);
                bool isOutputParam = ParamMarkedOutput(param);

                // Nothing can be marked `ref`
                if (ParamMarkedRef(param))
                {
                    Report(ref context, DiagnosticRules.RefParameterFound, method.GetLocation(), param.Identifier);
                    found |= true;
                }
                
                if (ParamHasInOrOutAttribute(param))
                {
                    // recommend using ReadOnlyArray or WriteOnlyArray
                    if (isArrayType)
                    {
                        Report(ref context, DiagnosticRules.ArrayMarkedInOrOut, method.GetLocation(), method.Identifier, param.Identifier);
                        found |= true;
                    }
                    // if not array type, stil can't use [In] or [Out]
                    else
                    {
                        Report(ref context, DiagnosticRules.NonArrayMarkedInOrOut, method.GetLocation(), method.Identifier, param.Identifier);
                        found |= true;
                    }
                }

                if (isArrayType)
                {
                    // can't be both ReadOnly and WriteOnly
                    if (hasReadOnlyArray && hasWriteOnlyArray)
                    {
                        Report(ref context, DiagnosticRules.ArrayParamMarkedBoth, method.GetLocation(), method.Identifier, param.Identifier);
                        found |= true;
                    }
                    // can't be both output (writeonly) and marked read only
                    else if (hasReadOnlyArray && isOutputParam)
                    {
                        Report(ref context, DiagnosticRules.ArrayOutputParamMarkedRead, method.GetLocation(), method.Identifier, param.Identifier);
                        found |= true;
                    }
                    // must have some indication of ReadOnly or WriteOnly
                    else if (!hasWriteOnlyArray && !hasReadOnlyArray && !isOutputParam) 
                    {
                        Report(ref context, DiagnosticRules.ArrayParamNotMarked, method.GetLocation(), method.Identifier, param.Identifier);
                        found |= true;
                    }
                }
                // Non-array types shouldn't have attributes meant for arrays
                else if (hasWriteOnlyArray || hasReadOnlyArray)
                {
                    Report(ref context, DiagnosticRules.NonArrayMarked, method.GetLocation(), method.Identifier, param.Identifier);
                    found |= true;
                }
            }

            return found;
        }

        public bool HasInvalidMethods<T>(ref GeneratorExecutionContext context, IEnumerable<MethodDeclarationSyntax> methodDeclarations, SyntaxToken typeId)
            where T : TypeDeclarationSyntax
        {
            bool found = false;
            Dictionary<string, bool> methodsHasAttributeMap = new Dictionary<string, bool>();

            /* we can't throw the diagnostic as soon as we see a second overload without an attribute, 
             *   as there could be a third overload with the default attribute
             * So store a diagnostic in case we see all methods and none of this overload have the attribute */
            Dictionary<string, Diagnostic> overloadsWithoutAttributeMap = new Dictionary<string, Diagnostic>();

            // var methodDeclarations = interfaceDeclaration.DescendantNodes().OfType<MethodDeclarationSyntax>();
            foreach (MethodDeclarationSyntax method in methodDeclarations)
            {
                found |= CheckOverloadAttributes(ref context, method, ref methodsHasAttributeMap, ref overloadsWithoutAttributeMap, typeId);
                found |= HasConflictingParameterName(ref context, method);
                found |= CheckMethod(ref context, method, typeId);
            }
            /* Finishes up the work started by `CheckOverloadAttributes` */
            foreach (var thing in overloadsWithoutAttributeMap)
            {
                context.ReportDiagnostic(thing.Value);
                found |= true;
            }
            return found;
        }
        private bool CheckMethod(ref GeneratorExecutionContext context, MethodDeclarationSyntax method, SyntaxToken parentNodeId)
        {
            bool found = false;
            Location loc = method.GetLocation();
            SyntaxToken methodId = method.Identifier;
            IEnumerable<QualifiedNameSyntax> qualifiedTypes = method.DescendantNodes().OfType<QualifiedNameSyntax>();
            IEnumerable<IdentifierNameSyntax> types = method.DescendantNodes().OfType<IdentifierNameSyntax>();
            IEnumerable<ArrayTypeSyntax> arrays = method.DescendantNodes().OfType<ArrayTypeSyntax>();

            var d = Diagnostic.Create(DiagnosticRules.ArraySignature_SystemArrayRule, loc, parentNodeId, methodId);
            found |= SignatureContainsTypeName(ref context, qualifiedTypes, "System.Array", d);  
            found |= SignatureContainsTypeName(ref context, types, "Array", d);
            found |= ArrayIsntOneDim(arrays, ref context, parentNodeId, methodId, loc);
            found |= CheckParamsForArrayAttributes(method, ref context);
            return found;
        }

        /// <summary>
        /// returns true iff there is a field of the given type in the given struct 
        /// e.g., if T is PropertyDeclarationSyntax, then if the struct has a property, we report a diagnostic and return true</summary>
        /// <typeparam name="T">T can vary over MethodDeclartion, EventDeclaration, etc... </typeparam>
        /// <param name="context"></param>
        /// <param name="structDeclaration"></param>
        /// <returns></returns>
        public bool StructHasFieldOfType<T>(ref GeneratorExecutionContext context, StructDeclarationSyntax structDeclaration)
        {
            if (structDeclaration.DescendantNodes().OfType<T>().Any())
            {
                Report(ref context, DiagnosticRules.StructHasInvalidFieldRule2, structDeclaration.GetLocation(), structDeclaration.Identifier,  SimplifySyntaxTypeString(typeof(T).Name));
                return true;
            }
            return false;
        }

        /// <summary>
        /// returns true if there is a field declared private, 
        /// or (inclusive) declared with a type that is a class or one of object, byte or dynamic</summary> 
        /// <param name="context"></param><param name="field">The field to inspect</param><param name="structId">The name of the struct the field belongs to</param>
        /// <param name="typeNames">A list of qualified class and interface names, which are invalid types to use in a struct for WinRT Component</param>
        /// <returns>True if the struct has a field of an type that is not supported in Windows Runtime</returns>
        public bool CheckFieldValidity(ref GeneratorExecutionContext context, 
            FieldDeclarationSyntax field, 
            SyntaxToken structId, 
            HashSet<INamedTypeSymbol> typeNames)
        {
            bool found = false; 

            /* No private fields allowed in Windows Runtime components */
            if (!IsPublic(field))
            { 
                Report(ref context, DiagnosticRules.StructHasPrivateFieldRule, field.GetLocation(), structId);
                found |= true;
            }

            if (ModifiersContains(field.Modifiers, "const"))
            {
                Report(ref context, DiagnosticRules.StructHasConstFieldRule, field.GetLocation(), structId);
                found |= true;
            }

            foreach (var variable in field.DescendantNodes().OfType<VariableDeclarationSyntax>())
            {
                var typeStr = variable.Type.ToString();

                HashSet<string> invalidTypes = new HashSet<string> { "object", "dynamic" };
                if (SymbolSetHasString(typeNames, typeStr) || invalidTypes.Contains(typeStr))
                { 
                    Report(ref context, DiagnosticRules.StructHasInvalidFieldRule, variable.GetLocation(), structId, field.ToString(), typeStr);
                    found |= true;
                }
            }
            return found;
        }
    }
}
