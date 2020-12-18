﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using WinRT.SourceGenerator;

namespace Generator 
{
    public partial class WinRTRules
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
        
        /// <summary>
        /// Look at all the array types and if any are of the form [][]+ or [,+] then raise the corresponding diagnostic and return true</summary>
        /// <param name="arrTypes"></param><param name="context"></param><param name="typeIdentifier">The type the array lives in</param>
        /// <param name="fieldId">The code the array is a part of the signature for; e.g. property or method</param><param name="loc"></param>
        /// <returns>True iff any of the array types given are multidimensional or jagged</returns>
        private bool ArrayIsntOneDim(ref GeneratorExecutionContext context,
            IEnumerable<ArrayTypeSyntax> arrTypes, 
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
         private static bool ImplementsInterface(INamedTypeSymbol typeSymbol, string typeToCheck)
        {
            if (typeSymbol == null)
            {
                return false;
            }

            if (typeSymbol.BaseType != null)
            {
                return typeSymbol.BaseType.MetadataName == typeToCheck || typeSymbol.BaseType.ToString() == typeToCheck;
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

        #region ParameterHelpers

        /// <summary>Looks at all possible attributes on a given parameter declaration </summary>
        /// <returns>returns true iff any are (string) equal to the given attribute name</returns>
        private bool ParamHasAttribute(string attrName, ParameterSyntax param) { return MatchesAnyAttribute(attrName, param.AttributeLists); }

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

        #endregion

        /// <summary>
        /// Make a suggestion for types to use instead of this type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private string SuggestAlternativeType(string type)
        {
            switch (type)
            {
                case "Dictionary": return "IDictionary<TKey,TValue>, IReadOnlyDictionary<TKey,TValue>, IEnumerable<KeyValuePair<TKey,TValue>>";
                case "ReadOnlyDictionary": return "IReadOnlyDictionary<TKey,TValue>, IEnumerable<KeyValuePair<TKey,TValue>>, IDictionary<TKey,TValue>";
                case "List": return "IList<T>, IReadOnlyList<T>, IEnumerable<T>";
                case "Enumerable": return "IEnumerable<T>";
                case "KeyValuePair": return "IKeyValuePair<TKey,TValue>";
                default: return "No suggestions for type";
            }
        }
     
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
        private bool SignatureHasInvalidGenericType(ref GeneratorExecutionContext context, IEnumerable<GenericNameSyntax> genericTypes, Location loc, SyntaxToken memberId)
        {
            bool found = false;
            foreach (var generic in genericTypes)
            {
                if (InvalidGenericTypes.Contains(generic.Identifier.ToString()))
                {
                    Report(ref context, DiagnosticRules.UnsupportedTypeRule, loc, memberId, generic.Identifier, SuggestAlternativeType(generic.Identifier.ToString()));
                    found |= true;
                }
            }
            return found;
        }
        private bool CheckSignature<T>(ref GeneratorExecutionContext context, T member, Location loc, SyntaxToken memberId, SyntaxToken parentTypeId)
            where T : MemberDeclarationSyntax
        {
            bool found = false;
            var arrayDiagnostic = Diagnostic.Create(DiagnosticRules.ArraySignature_SystemArrayRule, loc, parentTypeId, memberId);
            // var model = context.Compilation.GetSemanticModel();

            IEnumerable<GenericNameSyntax> genericTypes = member.DescendantNodes().OfType<GenericNameSyntax>();
            found |= SignatureHasInvalidGenericType(ref context, genericTypes, loc, memberId);

            IEnumerable<QualifiedNameSyntax> qualifiedTypes = member.DescendantNodes().OfType<QualifiedNameSyntax>();
            found |= SignatureContainsTypeName(ref context, qualifiedTypes, "System.Array", arrayDiagnostic);

            IEnumerable<IdentifierNameSyntax> types = member.DescendantNodes().OfType<IdentifierNameSyntax>();
            found |= SignatureContainsTypeName(ref context, types, "Array", arrayDiagnostic);

            IEnumerable<ArrayTypeSyntax> arrays = member.DescendantNodes().OfType<ArrayTypeSyntax>();
            found |= ArrayIsntOneDim(ref context, arrays, parentTypeId, memberId, loc);

            return found;
        }

        private static readonly string[] nonWinRuntimeInterfaces = {
                "System.Exception",
                "IAsyncAction",
                "IAsyncActionWithProgress`1",
                "IAsyncOperation`1",
                "IAsyncOperationWithProgress`2",
        };

        private readonly static string[] InvalidGenericTypes = { 
            // "Array"  
            "Dictionary",
            "Enumerable",
            "KeyValuePair",
            "List",
            "ReadOnlyDictionary",
        };

        private static readonly string[] InAndOutAttributeNames = { "In", "Out", "System.Runtime.InteropServices.In", "System.Runtime.InteropServices.Out" };
        private static readonly string[] OverloadAttributeNames = { "Windows.Foundation.Metadata.DefaultOverload", "DefaultOverload" };
    }
}
