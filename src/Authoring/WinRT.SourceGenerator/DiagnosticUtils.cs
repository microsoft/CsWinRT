using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System;
using WinRT.SourceGenerator;

namespace Generator
{
    public partial class WinRTComponentScanner
    {
        public WinRTComponentScanner(GeneratorExecutionContext context, string assemblyName)
        {
            _assemblyName = assemblyName;
            _context = context;
            _flag = false;
        }

        private readonly string _assemblyName;
        private readonly GeneratorExecutionContext _context;
        private bool _flag;

        public bool Found() { return _flag; }

        /// <summary>
        /// Gather information on all classes, interfaces and structs
        /// Perform code analysis to find scenarios that are erroneous in Windows Runtime</summary>
        public void FindDiagnostics()
        {
            WinRTSyntaxReciever syntaxReciever = (WinRTSyntaxReciever)_context.SyntaxReceiver;

            if (!syntaxReciever.Declarations.Any())
            {
                Report(WinRTRules.NoPublicTypesRule, null);
                return;
            }

            CheckNamespaces();
            CheckDeclarations();
        }

        private void CheckNamespaces()
        {
            WinRTSyntaxReciever syntaxReciever = (WinRTSyntaxReciever)_context.SyntaxReceiver;

            // Used to check for conflicitng namespace names
            HashSet<string> namespaceNames = new();

            foreach (var @namespace in syntaxReciever.Namespaces)
            {
                var model = _context.Compilation.GetSemanticModel(@namespace.SyntaxTree);
                var namespaceSymbol = model.GetDeclaredSymbol(@namespace);

                string namespaceString = namespaceSymbol.ToString();

                bool newNamespaceDeclaration = true;
                // Because modules could have a namespace defined in different places (i.e. defines a partial class)
                // we can't rely on `Contains` so we manually check that namespace names cannot differ by case only
                foreach (var usedNamespaceName in namespaceNames)
                {
                    if (String.Equals(namespaceString, usedNamespaceName, StringComparison.OrdinalIgnoreCase) &&
                        !String.Equals(namespaceString, usedNamespaceName, StringComparison.Ordinal))
                    {
                        newNamespaceDeclaration = false;
                        Report(WinRTRules.NamespacesDifferByCase, namespaceSymbol.Locations.First(), namespaceString);
                    }
                }

                if (newNamespaceDeclaration)
                {
                    namespaceNames.Add(namespaceString);
                }

                if (IsInvalidNamespace(namespaceSymbol, _assemblyName))
                {
                    Report(WinRTRules.DisjointNamespaceRule, namespaceSymbol.Locations.First(), _assemblyName, namespaceString);
                }
            }
        }

        private void CheckDeclarations()
        {
            WinRTSyntaxReciever syntaxReciever = (WinRTSyntaxReciever)_context.SyntaxReceiver;

            foreach (var declaration in syntaxReciever.Declarations)
            {
                var model = _context.Compilation.GetSemanticModel(declaration.SyntaxTree);

                // Check symbol information for whether it is public to properly detect partial types
                // which can leave out modifier.
                if (model.GetDeclaredSymbol(declaration).DeclaredAccessibility != Accessibility.Public)
                {
                    continue;
                }

                if (declaration is ClassDeclarationSyntax @class)
                {
                    var classId = @class.Identifier;
                    var classSymbol = model.GetDeclaredSymbol(@class);
                    var publicMethods = @class.ChildNodes().OfType<MethodDeclarationSyntax>().Where(IsPublic);
                    var props = @class.DescendantNodes().OfType<PropertyDeclarationSyntax>().Where(IsPublic);

                    // filter out methods and properties that will be replaced with our custom type mappings
                    IgnoreCustomTypeMappings(classSymbol, ref publicMethods, ref props);

                    if (!classSymbol.IsSealed && !classSymbol.IsStatic)
                    {
                        Report(WinRTRules.UnsealedClassRule, @class.GetLocation(), classId);
                    }

                    OverloadsOperator(@class);
                    HasMultipleConstructorsOfSameArity(@class);

                    if (classSymbol.IsGenericType)
                    {
                        Report(WinRTRules.GenericTypeRule, @class.GetLocation(), classId);
                    }

                    // check for things in nonWindowsRuntimeInterfaces
                    ImplementsInvalidInterface(classSymbol, @class);

                    CheckProperties(props, classId);

                    // check types -- todo: check for !valid types
                    CheckMethods(publicMethods, classId);
                }
                else if (declaration is InterfaceDeclarationSyntax @interface)
                {
                    var interfaceSym = model.GetDeclaredSymbol(@interface);
                    var methods = @interface.DescendantNodes().OfType<MethodDeclarationSyntax>();
                    var props = @interface.DescendantNodes().OfType<PropertyDeclarationSyntax>().Where(IsPublic);

                    // filter out methods and properties that will be replaced with our custom type mappings
                    IgnoreCustomTypeMappings(interfaceSym, ref methods, ref props);

                    if (interfaceSym.IsGenericType)
                    {
                        Report(WinRTRules.GenericTypeRule, @interface.GetLocation(), @interface.Identifier);
                    }

                    ImplementsInvalidInterface(interfaceSym, @interface);

                    CheckProperties(props, @interface.Identifier);

                    CheckMethods(methods, @interface.Identifier);
                }
                else if (declaration is StructDeclarationSyntax @struct)
                {
                    CheckStructFields(@struct);
                }
            }
        }

        private void IgnoreCustomTypeMappings(INamedTypeSymbol typeSymbol,
            ref IEnumerable<MethodDeclarationSyntax> methods,
            ref IEnumerable<PropertyDeclarationSyntax> properties)
        {
            string QualifiedName(INamedTypeSymbol sym)
            {
                return sym.OriginalDefinition.ContainingNamespace + "." + sym.OriginalDefinition.MetadataName;
            }

            HashSet<ISymbol> classMethods = new();

            foreach (var @interface in typeSymbol.AllInterfaces.
                        Where(symbol => WinRTTypeWriter.MappedCSharpTypes.ContainsKey(QualifiedName(symbol)) ||
                                        WinRTTypeWriter.ImplementedInterfacesWithoutMapping.Contains(QualifiedName(symbol))))
            {
                foreach (var interfaceMember in @interface.GetMembers())
                {
                    classMethods.Add(typeSymbol.FindImplementationForInterfaceMember(interfaceMember));
                }
            }
            methods = methods.Where(m => !classMethods.Contains(GetModel(m.SyntaxTree).GetDeclaredSymbol(m)));
            properties = properties.Where(p => !classMethods.Contains(GetModel(p.SyntaxTree).GetDeclaredSymbol(p)));
        }

        /// <summary>Checks to see if the class declares any operators (overloading them)</summary>
        ///<param name="classDeclaration">Class to check</param>
        /// Class to check for operator declarations 
        /// operator declarations are just like method declarations except they use the `operator` keyword</param>
        /// <returns>True iff an operator is overloaded by the given class</returns>
        private void OverloadsOperator(ClassDeclarationSyntax classDeclaration)
        {
            var operatorDeclarations = classDeclaration.DescendantNodes().OfType<OperatorDeclarationSyntax>();
            foreach (var op in operatorDeclarations) 
            { 
                Report(WinRTRules.OperatorOverloadedRule, op.GetLocation(), op.OperatorToken); 
            }
        }

        /// <summary>
        /// Raises a diagnostic when multiple constructors for a class are defined with the same arity.</summary>
        /// <param name="classDeclaration">Look at constructors of this class</param>
        private void HasMultipleConstructorsOfSameArity(ClassDeclarationSyntax classDeclaration)
        {
            IEnumerable<ConstructorDeclarationSyntax> constructors = classDeclaration.ChildNodes().OfType<ConstructorDeclarationSyntax>().Where(IsPublic);

            HashSet<int> aritiesSeenSoFar = new();

            foreach (ConstructorDeclarationSyntax constructor in constructors)
            {
                int arity = constructor.ParameterList.Parameters.Count;
                if (aritiesSeenSoFar.Contains(arity))
                {
                    Report(WinRTRules.ClassConstructorRule, constructor.GetLocation(), classDeclaration.Identifier, arity);
                }
                else
                {
                    aritiesSeenSoFar.Add(arity);
                }
            }
        }

        /// <summary>
        /// The code generation process makes functions with output param `__retval`, 
        /// we will shadow a user variable named the same thing -- so raise a diagnostic instead</summary>
        /// <param name="method">the method whose parameteres we are inspecting</param>
        private void HasConflictingParameterName(MethodDeclarationSyntax method)
        {
            // check if the identifier is our special name GeneratedReturnValueName
            bool IsInvalidParameterName(ParameterSyntax stx) { return stx.Identifier.Value.Equals(GeneratedReturnValueName); }

            var hasInvalidParams = method.ParameterList.Parameters.Any(IsInvalidParameterName);
            if (hasInvalidParams)
            {
                Report(WinRTRules.ParameterNamedValueRule, method.GetLocation(), method.Identifier);
            }
        }

        /// <summary>
        /// Look for overloads/defaultoverload attribute, conflicting parameter names, 
        /// parameter attribute errors, invalid types</summary>
        /// <param name="methodDeclarations">Collection of methods</param><param name="typeId">Containing class or interface</param>
        private void CheckMethods(IEnumerable<MethodDeclarationSyntax> methodDeclarations, SyntaxToken typeId)
        {
            Dictionary<string, bool> methodsHasAttributeMap = new();
            Dictionary<string, Diagnostic> overloadsWithoutAttributeMap = new Dictionary<string, Diagnostic>();

            // var methodDeclarations = interfaceDeclaration.DescendantNodes().OfType<MethodDeclarationSyntax>();
            foreach (MethodDeclarationSyntax method in methodDeclarations)
            {
                /* Gather information on which methods have overloads, and if any method has the DefaultOverload attribute */
                CheckOverloadAttributes(method, methodsHasAttributeMap, overloadsWithoutAttributeMap, typeId);
                /* Has parameter named __retval */
                HasConflictingParameterName(method);
                /* Check signature for invalid types */
                ParameterHasAttributeErrors(method);

                var methodSym = GetModel(method.SyntaxTree).GetDeclaredSymbol(method);

                ReportIfInvalidType(methodSym.ReturnType, method.GetLocation(), method.Identifier, typeId);
                foreach (var arg in methodSym.Parameters)
                {
                    ReportIfInvalidType(arg.Type, method.GetLocation(), method.Identifier, typeId);
                }
            }
            /* Finishes up the work started by `CheckOverloadAttributes` */
            foreach (var overloadWithDiagnostic in overloadsWithoutAttributeMap)
            {
                ReportDiagnostic(overloadWithDiagnostic.Value);
            }
        }

        /// <summary>Looks at all the properties of the given class and checks them for improper array types (System.Array instances, multidimensional, jagged)</summary>
        ///<param name="props">collection of properties</param><param name="typeId">containing class/interface</param>
        /// <returns>True iff any of the invalid array types are used in any of the propertyy signatures in the given class</returns>
        private void CheckProperties(IEnumerable<PropertyDeclarationSyntax> props, SyntaxToken typeId)
        {
            foreach (var prop in props)
            {
                var propSym = GetModel(prop.SyntaxTree).GetDeclaredSymbol(prop);
                var loc = prop.GetLocation();

                if (propSym.GetMethod == null || !propSym.GetMethod.DeclaredAccessibility.Equals(Accessibility.Public))
                {
                    Report(WinRTRules.PrivateGetterRule, loc, prop.Identifier);
                }

                ReportIfInvalidType(propSym.Type, loc, prop.Identifier, typeId);
                foreach (var arg in propSym.Parameters)
                {
                    ReportIfInvalidType(arg.Type, loc, prop.Identifier, typeId);
                }
            }
        }

        /// <summary>All struct fields must be public, of basic types, and not const</summary>
        /// <param name="struct">struct declaration</param>
        private void CheckStructFields(StructDeclarationSyntax @struct)
        {
            // delegates not allowed 
            if (@struct.DescendantNodes().OfType<DelegateDeclarationSyntax>().Any())
            {
                Report(WinRTRules.StructHasInvalidFieldRule, @struct.GetLocation(), @struct.Identifier, SimplifySyntaxTypeString(typeof(DelegateDeclarationSyntax).Name));
            }
            // methods not allowed
            if (@struct.DescendantNodes().OfType<MethodDeclarationSyntax>().Any())
            {
                Report(WinRTRules.StructHasInvalidFieldRule, @struct.GetLocation(), @struct.Identifier, SimplifySyntaxTypeString(typeof(MethodDeclarationSyntax).Name));
            }

            var structSym = GetModel(@struct.SyntaxTree).GetDeclaredSymbol(@struct);

            // constructors not allowed 
            if (structSym.Constructors.Length > 1)
            {
                Report(WinRTRules.StructHasInvalidFieldRule, @struct.GetLocation(), @struct.Identifier, SimplifySyntaxTypeString(typeof(ConstructorDeclarationSyntax).Name));
            }

            var fields = @struct.DescendantNodes().OfType<FieldDeclarationSyntax>();
            foreach (var field in fields)
            {
                // all fields must be public
                if (!IsPublic(field))
                {
                    Report(WinRTRules.StructHasPrivateFieldRule, field.GetLocation(), @struct.Identifier);
                }

                // const fields not allowed
                if (field.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.ConstKeyword)))
                {
                    Report(WinRTRules.StructHasConstFieldRule, field.GetLocation(), @struct.Identifier);
                }
                // see what type the field is, it must be an allowed type or another struct
                foreach (var variable in field.Declaration.Variables)
                {
                    IFieldSymbol varFieldSym = (IFieldSymbol)GetModel(variable.SyntaxTree).GetDeclaredSymbol(variable);

                    if (ValidStructFieldTypes.Contains(varFieldSym.Type.SpecialType) || varFieldSym.Type.TypeKind == TypeKind.Struct)
                    {
                        break;
                    }
                    else
                    {
                        Report(WinRTRules.StructHasInvalidFieldRule, variable.GetLocation(), @struct.Identifier, varFieldSym.Name);
                    }
                }
            }
            // Structs must have some public fields
            if (!fields.Any())
            {
                Report(WinRTRules.StructWithNoFieldsRule, @struct.GetLocation(), @struct.Identifier);
            }
        }

        /// <summary>Make sure any namespace defined is the same as the winmd or a subnamespace of it
        /// If component is A.B, e.g. A.B.winmd , then D.Class1 is invalid, as well as A.C.Class2
        /// </summary>
        /// <param name="namespace">the authored namespace to check</param><param name="assemblyName">the name of the component/winmd</param>
        /// <returns>True iff namespace is disjoint from the assembly name</returns>
        private bool IsInvalidNamespace(INamespaceSymbol @namespace, string assemblyName)
        {
            if (@namespace.ToString() == assemblyName)
            {
                return false;
            }

            var topLevel = @namespace;
            while (!topLevel.ContainingNamespace.IsGlobalNamespace)
            {
                if (topLevel.ToString() == assemblyName)
                {
                    return false;
                }
                topLevel = topLevel.ContainingNamespace;
            }

            return topLevel.ToString() != assemblyName;
        }

        ///<summary>Array types can only be one dimensional and not System.Array, 
        ///and there are some types not usable in the Windows Runtime, like KeyValuePair</summary> 
        ///<param name="typeSymbol">The type to check</param><param name="loc">where the type is</param>
        ///<param name="memberId">The method or property with this type in its signature</param>
        /// <param name="typeId">the type this member (method/prop) lives in</param>
        private void ReportIfInvalidType(ITypeSymbol typeSymbol, Location loc, SyntaxToken memberId, SyntaxToken typeId)
        {
            // If it's of the form int[], it has to be one dimensional
            if (typeSymbol.TypeKind == TypeKind.Array)
            {
                IArrayTypeSymbol arrTypeSym = (IArrayTypeSymbol)typeSymbol;

                // [,,]?
                if (arrTypeSym.Rank > 1)
                {
                    Report(WinRTRules.MultiDimensionalArrayRule, loc, memberId, typeId);
                    return;
                }
                // [][]?
                if (arrTypeSym.ElementType.TypeKind == TypeKind.Array)
                {
                    Report(WinRTRules.JaggedArrayRule, loc, memberId, typeId);
                    return;
                }
            }

            // NotValidTypes is an array of types that don't exist in Windows Runtime, so can't be passed between functions in Windows Runtime
            foreach (var typeName in NotValidTypes)
            {
                var notValidTypeSym = GetTypeByMetadataName(typeName);
                if (SymEq(typeSymbol.OriginalDefinition, notValidTypeSym))
                {
                    Report(WinRTRules.UnsupportedTypeRule, loc, memberId, typeName, SuggestType(typeName));
                    return;
                }
            }

            // construct the qualified name for this type 
            string qualifiedName = "";
            if (typeSymbol.ContainingNamespace != null && !typeSymbol.ContainingNamespace.IsGlobalNamespace)
            {
                // ContainingNamespace for Enumerable is just System, but we need System.Linq which is the ContainingSymbol
                qualifiedName += typeSymbol.ContainingSymbol + ".";
            }
            // instead of TypeName<int>, TypeName`1
            qualifiedName += typeSymbol.MetadataName;

            // GetTypeByMetadataName fails on "System.Linq.Enumerable" & "System.Collections.ObjectModel.ReadOnlyDictionary`2"
            // Would be fixed by issue #678 on the dotnet/roslyn-sdk repo
            foreach (var notValidType in WIPNotValidTypes)
            {
                if (qualifiedName == notValidType)
                {
                    Report(WinRTRules.UnsupportedTypeRule, loc, memberId, notValidType, SuggestType(notValidType));
                    return;
                }
            }
        }
    }
}
