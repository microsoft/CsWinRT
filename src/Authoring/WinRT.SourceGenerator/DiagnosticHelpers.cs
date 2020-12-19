using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using WinRT.SourceGenerator;

namespace Generator 
{
    public partial class WinRTScanner
    {
        private class TypeCollector
        {
            private HashSet<INamedTypeSymbol> types;
            private HashSet<INamedTypeSymbol> structs;
            private HashSet<INamespaceSymbol> namespaces;

            public TypeCollector()
            {
                types = new HashSet<INamedTypeSymbol>();
                structs = new HashSet<INamedTypeSymbol>();
                namespaces = new HashSet<INamespaceSymbol>();
            }
            public void AddType(INamedTypeSymbol newType) { types.Add(newType); }
            public void AddStruct(INamedTypeSymbol newType) { structs.Add(newType); }
            public void AddNamespace(INamespaceSymbol newType) { namespaces.Add(newType); }

            public HashSet<INamedTypeSymbol> GetTypes() { return types; }
            public HashSet<INamedTypeSymbol> GetStructs() { return structs; }
            public HashSet<INamespaceSymbol> GetNamespaces() { return namespaces; }
        }

        private TypeCollector CollectDefinedTypes(GeneratorExecutionContext context)
        {
            TypeCollector collectedTypes = new TypeCollector();

            foreach (SyntaxTree tree in context.Compilation.SyntaxTrees)
            {
                var model = context.Compilation.GetSemanticModel(tree);

                var classes = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Where(IsPublic);
                foreach (var @class in classes) 
                {
                    collectedTypes.AddType(model.GetDeclaredSymbol(@class));
                }

                var interfaces = tree.GetRoot().DescendantNodes().OfType<InterfaceDeclarationSyntax>().Where(IsPublic);
                foreach (var @interface in interfaces)
                {
                    collectedTypes.AddType(model.GetDeclaredSymbol(@interface));
                }

                var structs = tree.GetRoot().DescendantNodes().OfType<StructDeclarationSyntax>().Where(IsPublic);
                foreach (var @struct in structs)
                {
                    collectedTypes.AddStruct(model.GetDeclaredSymbol(@struct));
                }

                var namespaces = tree.GetRoot().DescendantNodes().OfType<NamespaceDeclarationSyntax>();
                foreach (var @namespace in namespaces)
                {
                    collectedTypes.AddNamespace(model.GetDeclaredSymbol(@namespace));
                }
            }
            return collectedTypes;
        }

        private bool ImplementsInterface(INamedTypeSymbol typeSymbol, string typeToCheck)
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

        private bool ModifiersContains(SyntaxTokenList modifiers, string str) { return modifiers.Any(modifier => modifier.ValueText == str); }

        /// <summary>Raise the flag so we don't make a winmd, and add a diagnostic to the sourcegenerator</summary>
        /// <param name="d"></param><param name="loc"></param><param name="args"></param>
        private void Report(DiagnosticDescriptor d, Location loc, params object[] args)
        {
            Flag();
            _context.ReportDiagnostic(Diagnostic.Create(d, loc, args));
        }

        private void ReportDiagnostic(Diagnostic d)
        {
            Flag();
            _context.ReportDiagnostic(d);
        }

        /// <summary>Check if any of the given symbols represents the given type</summary>
        /// <param name="typeNames"></param><param name="typeStr"></param>
        private bool SymbolSetHasString(HashSet<INamedTypeSymbol> typeNames, string typeStr) { return typeNames.Where(sym => sym.ToString().Contains(typeStr)).Any(); }

        /// <summary>Check to see if the piece of syntax is the same as the string</summary>
        /// <param name="stx"></param><param name="str"></param>
        private bool SyntaxTokenIs(SyntaxToken stx, string str) { return stx.Value.Equals(str); }

        /// <summary>Attributes can come in one list or many, e.g. [A()][B()] vs. [A(),B()]
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

        /// <summary>Check for qualified and unqualified [In] and [Out] attribute on the parameter</summary>
        /// <param name="param"></param>
        /// <returns>True if any attribute is the In or Out attribute</returns>
        private bool ParamHasInOrOutAttribute(ParameterSyntax param) { return InAndOutAttributeNames.Where(str => ParamHasAttribute(str, param)).Any(); }

        /// <summary>Check for qualified and unqualified [DefaultOverload] attribute on the parameter<</summary>
        /// <param name="method"></param>
        /// <returns>True if any attribute is the DefaultOverload attribute</returns>
        private bool MethodHasDefaultOverloadAttribute(MethodDeclarationSyntax method) { return OverloadAttributeNames.Where(str => MatchesAnyAttribute(str, method.AttributeLists)).Any(); }

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
        /// <param name="method">Method declared</param>
        /// <returns>true if array attributes are invalid (see summary)</returns>
        private void CheckParamsForArrayAttributes(MethodDeclarationSyntax method)
        {
            foreach (ParameterSyntax param in method.ParameterList.Parameters)
            {
                var isArrayType = param.ChildNodes().OfType<ArrayTypeSyntax>().Any();
                bool hasReadOnlyArray = ParamHasAttribute("System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray", param);
                bool hasWriteOnlyArray = ParamHasAttribute("System.Runtime.InteropServices.WindowsRuntime.WriteOnlyArray", param);
                bool isOutputParam = ParamMarkedOutput(param);

                // Nothing can be marked `ref`
                if (ParamMarkedRef(param)) { Report(WinRTRules.RefParameterFound, method.GetLocation(), param.Identifier); }
                
                if (ParamHasInOrOutAttribute(param))
                {
                    // recommend using ReadOnlyArray or WriteOnlyArray
                    if (isArrayType) 
                    { 
                        Report(WinRTRules.ArrayMarkedInOrOut, method.GetLocation(), method.Identifier, param.Identifier); 
                    }
                    // if not array type, stil can't use [In] or [Out]
                    else 
                    { 
                        Report(WinRTRules.NonArrayMarkedInOrOut, method.GetLocation(), method.Identifier, param.Identifier); 
                    }
                }

                if (isArrayType)
                {
                    // can't be both ReadOnly and WriteOnly
                    if (hasReadOnlyArray && hasWriteOnlyArray)
                    {
                        Report(WinRTRules.ArrayParamMarkedBoth, method.GetLocation(), method.Identifier, param.Identifier);
                    }
                    // can't be both output (writeonly) and marked read only
                    else if (hasReadOnlyArray && isOutputParam)
                    {
                        Report(WinRTRules.ArrayOutputParamMarkedRead, method.GetLocation(), method.Identifier, param.Identifier);
                    }
                    // must have some indication of ReadOnly or WriteOnly
                    else if (!hasWriteOnlyArray && !hasReadOnlyArray && !isOutputParam) 
                    {
                        Report(WinRTRules.ArrayParamNotMarked, method.GetLocation(), method.Identifier, param.Identifier);
                    }
                }
                // Non-array types shouldn't have attributes meant for arrays
                else if (hasWriteOnlyArray || hasReadOnlyArray)
                {
                    Report(WinRTRules.NonArrayMarked, method.GetLocation(), method.Identifier, param.Identifier);
                }
            }
        }

        #endregion

        private bool IsInvalidNamespace(INamespaceSymbol @namespace, string assemblyName)
        {
            var contain = @namespace;
            while (!contain.ContainingNamespace.IsGlobalNamespace)
            {
                contain = contain.ContainingNamespace;
            }

            return (!contain.Name.Equals(assemblyName) && !@namespace.Name.Equals(assemblyName));
        }

        /// <summary>
        /// Keeps track of repeated declarations of a method (overloads) and raises diagnostics according to the rule that exactly one overload should be attributed the default</summary>
        /// <param name="method">Look for overloads of this method, checking the attributes as well attributes for</param>
        /// <param name="methodHasAttributeMap">
        /// The strings are unique names for each method -- made by its name and arity
        /// Some methods may get the attribute, some may not, we keep track of this in the map.
        /// <param name="overloadsWithoutAttributeMap">
        /// Once we have seen an overload and the method still has no attribute, we make a diagnostic for it
        /// If we see the method again but this time with the attribute, we remove it (and its diagnostic) from the map
        /// <param name="classId">The class the method lives in -- used for creating the diagnostic</param>
        /// <returns>True iff multiple overloads of a method are found, where more than one has been designated as the default overload</returns>
        private void CheckOverloadAttributes(MethodDeclarationSyntax method,
            Dictionary<string, bool> methodHasAttributeMap,
            Dictionary<string, Diagnostic> overloadsWithoutAttributeMap,
            SyntaxToken classId)
        {
            int methodArity = method.ParameterList.Parameters.Count;
            string methodNameWithArity = method.Identifier.Text + methodArity.ToString(); //

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
                    // We finally got an attribute, so dont raise a diagnostic for this method
                    overloadsWithoutAttributeMap.Remove(methodNameWithArity);
                }
                else if (hasDefaultOverloadAttribute && methodHasAttrAlready)
                {
                    // Special case in that multiple instances of the DefaultAttribute being used on the method 
                    Report(WinRTRules.MethodOverload_MultipleDefaultAttribute, method.GetLocation(), methodArity, method.Identifier, classId);
                }
                else if (!hasDefaultOverloadAttribute && !methodHasAttrAlready)
                {
                    // we could see this method later with the attribute, 
                    // so hold onto the diagnostic for it until we know it doesn't have the attribute
                    overloadsWithoutAttributeMap[methodNameWithArity] = Diagnostic.Create(
                        WinRTRules.MethodOverload_NeedDefaultAttribute, 
                        method.GetLocation(), 
                        methodArity, 
                        method.Identifier,
                        classId);
                }
            }
            else
            {
                // first time we're seeing the method, add a pair in the map for its name and whether it has the attribute 
                methodHasAttributeMap[methodNameWithArity] = hasDefaultOverloadAttribute;
            }
        }


        #region StringHelpers

        /// <summary>Make a suggestion for types to use instead of the given type</summary>
        /// <param name="type">A type that is not valid in Windows Runtime</param>
        /// <returns>string of types that the given type implements and are valid Windows Runtime types</returns>
        private string SuggestType(string type)
        {
            switch (type)
            {
                case "System.Collections.Generic.Dictionary`2": 
                    return "IDictionary<TKey,TValue>, IReadOnlyDictionary<TKey,TValue>, IEnumerable<KeyValuePair<TKey,TValue>>";
                case "System.Collections.ObjectModel.ReadOnlyDictionary`2":
                    return "IReadOnlyDictionary<TKey,TValue>, IEnumerable<KeyValuePair<TKey,TValue>>, IDictionary<TKey,TValue>";
                case "System.Collections.Generic.List`1": 
                    return "IList<T>, IReadOnlyList<T>, IEnumerable<T>";
                case "System.Linq.Enumerable`1": 
                    return "IEnumerable<T>";
                case "System.Collections.Generic.KeyValuePair": 
                    return "KeyValuePair<TKey,TValue>";
                case "System.Array": 
                    return "T[]";
                default: return "No suggestions for type";
            }
        }
     
        /// <param name="syntaxType"></param>
        /// <returns>the common term for the given syntax type</returns>
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

        private static readonly string[] nonWindowsRuntimeInterfaces = 
        {
            "System.Exception",
            "IAsyncAction",
            "IAsyncActionWithProgress`1",
            "IAsyncOperation`1",
            "IAsyncOperationWithProgress`2",
        };

        private readonly static string[] InvalidTypes = 
        { 
            "System.Array",
            "System.Collections.Generic.Dictionary`2",
            "System.Collections.Generic.List`1", 
            "System.Collections.Generic.KeyValuePair"
        };

        private readonly static string[] WIPInvalidTypes =
        {
            "System.Linq.Enumerable`1",
            "Enumerable`1",
            "System.Collections.ObjectModel.ReadOnlyDictionary`2",
            "ReadOnlyDictionary`2"
        };

        private static readonly string[] InAndOutAttributeNames = { "In", "Out", "System.Runtime.InteropServices.In", "System.Runtime.InteropServices.Out" };
        private static readonly string[] OverloadAttributeNames = { "Windows.Foundation.Metadata.DefaultOverload", "DefaultOverload" };
    
        private static readonly string GeneratedReturnValueName = "__retval";

        #endregion
    }
}
