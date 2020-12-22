using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using WinRT.SourceGenerator;

namespace Generator 
{
    // Helper Class, makes for clean collection of types and namespaces, needed for checking
    internal class TypeCollector 
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

    public partial class WinRTScanner
    {
        private void Flag() { _flag |= true; }
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

        ///<summary>Array types can only be one dimensional and not System.Array, 
        ///and there are some types not usable in the Windows Runtime, like KeyValuePair</summary> 
        ///<param name="typeSymbol">The type to check</param><param name="loc">where the type is</param>
        ///<param name="memberId">The method or property with this type in its signature</param>
        /// <param name="typeId">the type this member (method/prop) lives in</param>
        private void ReportIfInvalidType(ITypeSymbol typeSymbol, Location loc, SyntaxToken memberId, SyntaxToken typeId) 
        { 

            if (typeSymbol.TypeKind == TypeKind.Array) 
            { 
                IsInvalidArrayType((IArrayTypeSymbol)typeSymbol, loc, memberId, typeId);
                return;
            }

            foreach (var s in InvalidTypes)
            { 
                var invalidTypeSym = _context.Compilation.GetTypeByMetadataName(s);
                if (SymbolEqualityComparer.Default.Equals(typeSymbol.OriginalDefinition, invalidTypeSym))
                {
                    Report(WinRTRules.UnsupportedTypeRule, loc, memberId, s, SuggestType(s));
                    return;
                }
            }

            // helper function, we can't get a type symbol for some of the invalid types 
            bool SameType(ITypeSymbol sym, string s)
            {
                string baseStr = sym.ContainingNamespace.IsGlobalNamespace ? "" : sym.ContainingSymbol + ".";
                var x = baseStr + sym.MetadataName;
                return x == s;
            }

            // GetTypeByMetadataName fails on "System.Linq.Enumerable`1" & "System.Collections.ObjectModel.ReadOnlyDictionary`2"
            // Would be fixed by issue #678 on the dotnet/roslyn-sdk repo
            foreach (var invalidType in WIPInvalidTypes) 
            {
                if (SameType(typeSymbol, invalidType))
                { 
                    Report(WinRTRules.UnsupportedTypeRule, loc, memberId, invalidType, SuggestType(invalidType));
                    return;
                }
            }
        }
 
        /// <summary>
        /// See if this class/interfaces inherits the given type
        /// </summary>
        /// <param name="typeSymbol">type that might inherit</param>
        /// <param name="typeToCheck">Inherited interface or class</param>
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

        /// <summary>
        ///  Checks to see if an array parameter has been marked with both Write and Read attributes
        ///  Does extra work, by catching `ref` params, done here since this code can be used by class or interface related methods</summary>
        /// <param name="method">Method declared</param>
        /// <returns>true if array attributes are invalid (see summary)</returns>
        private void ParameterHasAttributeErrors(MethodDeclarationSyntax method)
        {
            // helper function, used to see if param has Ref or Out keyword
            bool HasModifier(ParameterSyntax param, SyntaxKind kind) { return param.Modifiers.Where(m => m.IsKind(kind)).Any(); }

            foreach (ParameterSyntax param in method.ParameterList.Parameters)
            {
                var isArrayType = param.Type.IsKind(SyntaxKind.ArrayType);
                bool hasReadOnlyArray = ParamHasReadOnlyAttribute(param);
                bool hasWriteOnlyArray = ParamHasWriteOnlyAttribute(param);
                
                // Nothing can be marked `ref`
                if (HasModifier(param, SyntaxKind.RefKeyword))
                { 
                    Report(WinRTRules.RefParameterFound, method.GetLocation(), param.Identifier); 
                }
                
                if (ParamHasInOrOutAttribute(param))
                {
                    // recommend using ReadOnlyArray or WriteOnlyArray instead of In/Out
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
                    bool isOutputParam = HasModifier(param, SyntaxKind.OutKeyword);
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

        /// <summary>Make sure any namespace defined is the same as the winmd or a subnamespace of it
        /// If component is A.B, e.g. A.B.winmd , then D.Class1 is invalid, as well as A.C.Class2
        /// </summary>
        /// <param name="namespace">the authored namesapce to check</param>
        /// <param name="assemblyName">the name of the component/winmd</param>
        /// <returns>True iff namespace is disjoint from the assembly name</returns>
        private bool IsInvalidNamespace(INamespaceSymbol @namespace, string assemblyName)
        {
            var contain = @namespace;
            while (!contain.ContainingNamespace.IsGlobalNamespace)
            {
                contain = contain.ContainingNamespace;
            }

            return !contain.Name.Equals(assemblyName) && !@namespace.Name.Equals(assemblyName);
        }

        /// <summary>Arrays cannot be jagged (int[][]+) or multidimensional (int[,+])</summary>
        /// <param name="arrTypeSym">array type</param><param name="loc">syntax info for diagnostic</param>
        /// <param name="memberId">method/prop name</param><param name="typeId">class/interface name</param>
        private void IsInvalidArrayType(IArrayTypeSymbol arrTypeSym, Location loc, SyntaxToken memberId, SyntaxToken typeId)
        { 
            if (arrTypeSym.Rank > 1) 
            { 
                Report(WinRTRules.MultiDimensionalArrayRule, loc, memberId, typeId); 
            } 
            if (arrTypeSym.ElementType.TypeKind == TypeKind.Array) 
            { 
                Report(WinRTRules.ArraySignature_JaggedArrayRule, loc, memberId, typeId); 
            }
        }
       
        private bool IsPublic(MemberDeclarationSyntax member) { return member.Modifiers.Where(m => m.IsKind(SyntaxKind.PublicKeyword)).Any(); }
        
        /// <summary>Attributes can come in one list or many, e.g. [A(),B()] vs. [A()][B()]
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
                    // no declared symbol for AttributeSyntax...
                    if (attr.Name.ToString().Equals(attrName))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>Looks at all possible attributes on a given parameter declaration </summary>
        /// <returns>returns true iff any are (string) equal to the given attribute name</returns>
        private bool ParamHasAttribute(ParameterSyntax param, string attrName) { return MatchesAnyAttribute(attrName, param.AttributeLists); }

        /// <summary>Check for qualified and unqualified [In] and [Out] attribute on the parameter</summary>
        /// <param name="param"></param>
        /// <returns>True if any attribute is the In or Out attribute</returns>
        private bool ParamHasInOrOutAttribute(ParameterSyntax param) { return InAndOutAttributeNames.Where(str => ParamHasAttribute(param, str)).Any(); }
        private bool ParamHasReadOnlyAttribute(ParameterSyntax param) { return ReadOnlyArrayAttributeNames.Where(str => ParamHasAttribute(param, str)).Any(); }
        private bool ParamHasWriteOnlyAttribute(ParameterSyntax param) { return WriteOnlyArrayAttributeNames.Where(str => ParamHasAttribute(param, str)).Any(); }

        /// <summary>Check for qualified and unqualified [DefaultOverload] attribute on the parameter<</summary>
        /// <param name="method"></param>
        /// <returns>True if any attribute is the DefaultOverload attribute</returns>
        private bool HasDefaultOverloadAttribute(MethodDeclarationSyntax method) { return OverloadAttributeNames.Where(str => MatchesAnyAttribute(str, method.AttributeLists)).Any(); }

        /// <summary>Gather the type symbols for all classes, interfaces and structs</summary>
        /// <param name="context">Context used for syntax trees</param><returns>A TypeCollector populated with the type symbols</returns>
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
        private static readonly string[] OverloadAttributeNames = { "DefaultOverload", "Windows.Foundation.Metadata.DefaultOverload"  };
        private static readonly string[] ReadOnlyArrayAttributeNames = { "System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray", "ReadOnlyArray" };
        private static readonly string[] WriteOnlyArrayAttributeNames = { "System.Runtime.InteropServices.WindowsRuntime.WriteOnlyArray", "WriteOnlyArray" };
    
        private static readonly string GeneratedReturnValueName = "__retval";
    }
}
