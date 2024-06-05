using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using WinRT.SourceGenerator;

namespace Generator
{
    public partial class WinRTComponentScanner
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
        private SemanticModel GetModel(SyntaxTree t) { return _context.Compilation.GetSemanticModel(t); }

        private INamedTypeSymbol GetTypeByMetadataName(string fullyQualifiedMetadataName)
        {
            return _context.Compilation.GetTypeByMetadataName(fullyQualifiedMetadataName);
        }

        private bool SymEq(ISymbol sym1, ISymbol sym2) { return SymbolEqualityComparer.Default.Equals(sym1, sym2); }

        /// <summary>
        /// Returns true if the class represented by the symbol 
        /// implements any of the interfaces defined in ProhibitedAsyncInterfaces (e.g., IAsyncAction, ...)</summary>
        /// <param name="typeSymbol">The class/interface type symbol</param><param name="typeDeclaration">The containing class/interface declaration</param>
        /// <returns>True iff the given class implements any of the IAsync interfaces that are not valid in Windows Runtime</returns>
        private void ImplementsInvalidInterface(INamedTypeSymbol typeSymbol, TypeDeclarationSyntax typeDeclaration)
        {
            bool AsyncActionCase(INamedTypeSymbol sym)
            {
                // are we using Windows.Foundation.IAsyncAction ?
                bool isWindowsFoundation = sym.ContainingNamespace.IsGlobalNamespace ||
                    string.CompareOrdinal(sym.ContainingNamespace.Name, "Windows.Foundation") == 0;
                bool isAsyncAction = string.CompareOrdinal(sym.MetadataName, "IAsyncAction") == 0;
                return isWindowsFoundation && isAsyncAction;
            }

            if (typeSymbol.BaseType != null && typeSymbol.BaseType.ContainingNamespace != null)
            {
                if (AsyncActionCase(typeSymbol.BaseType))
                {
                    Report(WinRTRules.NonWinRTInterface, typeDeclaration.GetLocation(), typeDeclaration.Identifier, "IAsyncAction");
                }
            }

            foreach (var implementedInterface in typeSymbol.AllInterfaces)
            {
                if (AsyncActionCase(implementedInterface))
                {
                    Report(WinRTRules.NonWinRTInterface, typeDeclaration.GetLocation(), typeDeclaration.Identifier, "IAsyncAction");
                }
            }

            foreach (string prohibitedInterface in nonWindowsRuntimeInterfaces)
            {
                if (ImplementsInterface(typeSymbol, prohibitedInterface))
                {
                    Report(WinRTRules.NonWinRTInterface, typeDeclaration.GetLocation(), typeDeclaration.Identifier, prohibitedInterface);
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

            // for interface type symbols
            foreach (var implementedInterface in typeSymbol.AllInterfaces)
            {
                if (implementedInterface.MetadataName == typeToCheck)
                {
                    return true;
                }
            }

            // class type symbols might have a BaseType, like System.Exception
            if (typeSymbol.BaseType != null)
            {
                foreach (var x in typeSymbol.BaseType.AllInterfaces)
                {
                    if (x.MetadataName == typeToCheck)
                    {
                        return true;
                    }
                }
                var typeToCheckSymbol = GetTypeByMetadataName(typeToCheck);
                if (SymEq(typeSymbol.BaseType, typeToCheckSymbol))
                {
                    return true;
                }
                // Type<T_1,T_2,...,T_n> -> Type`n
                if (typeSymbol.BaseType.MetadataName != null)
                {
                    return typeSymbol.BaseType.MetadataName == typeToCheck;
                }
            }

            return false;
        }

        private bool IsPublic(MemberDeclarationSyntax member) { return member.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)); }

        #region Attributes        

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
                    if (string.CompareOrdinal(attr.Name.ToString(), attrName) == 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private bool MatchesAnyAttribute(string attrName, ImmutableArray<AttributeData> attributes)
        {
            string attrClassName = attrName + "Attribute";
            return attributes.Any((attr) => attr.AttributeClass.Name == attrClassName);
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
                    Report(WinRTRules.RefParameterFound, method.GetLocation(), method.Identifier, param.Identifier);
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

        /// <summary>Looks at all possible attributes on a given parameter declaration </summary>
        /// <returns>returns true iff any are (string) equal to the given attribute name</returns>
        private bool ParamHasAttribute(ParameterSyntax param, string attrName) { return MatchesAnyAttribute(attrName, param.AttributeLists); }

        /// <summary>Check for qualified and unqualified [In] and [Out] attribute on the parameter</summary>
        /// <param name="param"></param>
        /// <returns>True if any attribute is the In or Out attribute</returns>
        private bool ParamHasInOrOutAttribute(ParameterSyntax param) { return InAndOutAttributeNames.Where(str => ParamHasAttribute(param, str)).Any(); }
        private bool ParamHasReadOnlyAttribute(ParameterSyntax param) { return ReadOnlyArrayAttributeNames.Where(str => ParamHasAttribute(param, str)).Any(); }
        private bool ParamHasWriteOnlyAttribute(ParameterSyntax param) { return WriteOnlyArrayAttributeNames.Where(str => ParamHasAttribute(param, str)).Any(); }

        private ISymbol GetInterfaceMemberFromClassMember(ISymbol classMember)
        {
            var parent = classMember.ContainingType;
            foreach (var @interface in parent.AllInterfaces)
            {
                foreach (var interfaceMember in @interface.GetMembers())
                {
                    if (SymbolEqualityComparer.Default.Equals(parent.FindImplementationForInterfaceMember(interfaceMember), classMember))
                    {
                        return interfaceMember;
                    }
                }
            }

            return null;
        }

        /// <summary>Check for qualified and unqualified [DefaultOverload] attribute on the parameter<</summary>
        /// <param name="method"></param>
        /// <returns>True if any attribute is the DefaultOverload attribute</returns>
        private bool HasDefaultOverloadAttribute(MethodDeclarationSyntax method)
        {
            if (OverloadAttributeNames.Where(str => MatchesAnyAttribute(str, method.AttributeLists)).Any())
            {
                return true;
            }

            var interfaceMember = GetInterfaceMemberFromClassMember(GetModel(method.SyntaxTree).GetDeclaredSymbol(method));
            if (interfaceMember == null)
            {
                return false;
            }
            return OverloadAttributeNames.Where(str => MatchesAnyAttribute(str, interfaceMember.GetAttributes())).Any();
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
            string methodNameWithArity = method.Identifier.Text + methodArity;

            // look at all the attributes on this method and see if any of them is the DefaultOverload attribute 
            bool hasDefaultOverloadAttribute = HasDefaultOverloadAttribute(method);
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
                    Report(WinRTRules.MultipleDefaultOverloadAttribute, method.GetLocation(), methodArity, method.Identifier, classId);
                }
                else if (!hasDefaultOverloadAttribute && !methodHasAttrAlready)
                {
                    // we could see this method later with the attribute, 
                    // so hold onto the diagnostic for it until we know it doesn't have the attribute
                    overloadsWithoutAttributeMap[methodNameWithArity] = Diagnostic.Create(
                        WinRTRules.NeedDefaultOverloadAttribute,
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

        #endregion 

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
                case "EventFieldDeclarationSyntax": return "event";
                case "ConstructorDeclarationSyntax": return "constructor";
                case "DelegateDeclarationSyntax": return "delegate";
                case "IndexerDeclarationSyntax": return "indexer";
                case "MethodDeclarationSyntax": return "method";
                case "OperatorDeclarationSyntax": return "operator";
                case "PropertyDeclarationSyntax": return "property";
                default: return "unknown syntax type: " + syntaxType;
            }
        }

        private SpecialType[] ValidStructFieldTypes = new SpecialType[]
        {
            SpecialType.System_Boolean,
            SpecialType.System_String,
            SpecialType.System_Single,
            SpecialType.System_Double,
            SpecialType.System_UInt16,
            SpecialType.System_UInt32,
            SpecialType.System_UInt64,
            SpecialType.System_Int16,
            SpecialType.System_Int32,
            SpecialType.System_Int64,
        };

        private static readonly HashSet<string> nonWindowsRuntimeInterfaces = new HashSet<string>()
        {
            "System.Exception",
            "IAsyncActionWithProgress`1",
            "IAsyncOperation`1",
            "IAsyncOperationWithProgress`2",
        };

        private static readonly HashSet<string> NotValidTypes = new HashSet<string>()
        {
            "System.Array",
            "System.Collections.Generic.Dictionary`2",
            "System.Collections.Generic.List`1",
            "System.Collections.Generic.KeyValuePair"
        };

        private static readonly HashSet<string> WIPNotValidTypes = new HashSet<string>()
        {
            "System.Linq.Enumerable",
            "Enumerable",
            "System.Collections.ObjectModel.ReadOnlyDictionary`2",
            "ReadOnlyDictionary`2"
        };

        private static readonly HashSet<string> InAndOutAttributeNames = new HashSet<string>()
            { "In", "Out", "System.Runtime.InteropServices.In", "System.Runtime.InteropServices.Out" };
        private static readonly HashSet<string> OverloadAttributeNames = new HashSet<string>()
            { "DefaultOverload", "Windows.Foundation.Metadata.DefaultOverload"  };
        private static readonly HashSet<string> ReadOnlyArrayAttributeNames = new HashSet<string>()
            { "System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray", "ReadOnlyArray" };
        private static readonly HashSet<string> WriteOnlyArrayAttributeNames = new HashSet<string>()
            { "System.Runtime.InteropServices.WindowsRuntime.WriteOnlyArray", "WriteOnlyArray" };

        private static readonly string GeneratedReturnValueName = "__retval";
    }
}
