using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using WinRT.SourceGenerator;

using Microsoft.CodeAnalysis.CSharp;

namespace Generator 
{
    public partial class WinRTScanner
    {
        public WinRTScanner() { _flag = false; }

        public WinRTScanner(GeneratorExecutionContext context, string assemblyName) 
        { 
            _flag = false;
            _context = context;
            _assemblyName = assemblyName;
        }

        private bool _flag;

        private GeneratorExecutionContext _context;
        private string _assemblyName;

        private void Flag() { _flag |= true; }
        public bool Found() { return _flag; }

        public void FindDiagnostics()
        { 
            TypeCollector holder = CollectDefinedTypes(_context);
            HashSet<INamedTypeSymbol> userCreatedTypes = holder.GetTypes();
            HashSet<INamedTypeSymbol> userCreatedStructs = holder.GetStructs();
            HashSet<INamespaceSymbol> userCreatedNamespaces = holder.GetNamespaces();

            HasInvalidNamespace(userCreatedNamespaces, _assemblyName);
            HasSomePublicTypes(userCreatedTypes, userCreatedStructs);
            
            foreach (SyntaxTree tree in _context.Compilation.SyntaxTrees)
            {
                var model = _context.Compilation.GetSemanticModel(tree);
                var nodes = tree.GetRoot().DescendantNodes();

                var classes = nodes.OfType<ClassDeclarationSyntax>().Where(IsPublic);
                foreach (ClassDeclarationSyntax @class in classes)
                {
                    var classSymbol = model.GetDeclaredSymbol(@class);
                    UnsealedClass(classSymbol, @class);

                    OverloadsOperator(@class);
                    HasMultipleConstructorsOfSameArity(@class);

                    var classId = @class.Identifier;
                    TypeIsGeneric(classSymbol, @class);
                    ImplementsInvalidInterface(classSymbol, @class);
                    
                    var props = @class.DescendantNodes().OfType<PropertyDeclarationSyntax>().Where(IsPublic);
                    CheckSignatureOfProperties(props, classId);
                    
                    var publicMethods = @class.ChildNodes().OfType<MethodDeclarationSyntax>().Where(IsPublic);
                    CheckMethods(publicMethods, classId);
                }

                var interfaces = nodes.OfType<InterfaceDeclarationSyntax>().Where(IsPublic);
                foreach (InterfaceDeclarationSyntax @interface in interfaces)
                {
                    var interfaceSym = model.GetDeclaredSymbol(@interface);
                    
                    TypeIsGeneric(interfaceSym, @interface);
                    ImplementsInvalidInterface(interfaceSym, @interface);
                    
                    var props = @interface.DescendantNodes().OfType<PropertyDeclarationSyntax>().Where(IsPublic);
                    CheckSignatureOfProperties(props, @interface.Identifier);
                    
                    var methods = @interface.DescendantNodes().OfType<MethodDeclarationSyntax>();
                    CheckMethods(methods, @interface.Identifier);
                }

                var structs = nodes.OfType<StructDeclarationSyntax>();
                foreach (StructDeclarationSyntax @struct in structs)
                {
                    CheckStructField(@struct, userCreatedTypes, model.GetDeclaredSymbol(@struct)); 
                }
            } 
        }
       
        public bool IsPublic(MemberDeclarationSyntax member) { return ModifiersContains(member.Modifiers, "public");  }

        /// <summary>
        /// Returns true if the class represented by the symbol 
        /// implements any of the interfaces defined in ProhibitedAsyncInterfaces (e.g., IAsyncAction, ...) /// </summary>
        /// <param name="context"></param><param name="typeSymbol"></param><param name="classDeclaration"></param>
        /// <returns>True iff the given class implements any of the IAsync interfaces that are not valid in Windows Runtime</returns>
        public void ImplementsInvalidInterface(INamedTypeSymbol typeSymbol, TypeDeclarationSyntax typeDeclaration)
        {
            foreach (string prohibitedInterface in nonWindowsRuntimeInterfaces)
            {
                if (ImplementsInterface(typeSymbol, prohibitedInterface))
                {
                    Report(WinRTRules.NonWinRTInterface, typeDeclaration.GetLocation(), typeDeclaration.Identifier, prohibitedInterface);
                }
            }
        }

        public void HasSomePublicTypes(HashSet<INamedTypeSymbol> publicTypes, HashSet<INamedTypeSymbol> publicStructs) 
        {
            if (!publicTypes.Any() && !publicStructs.Any()) { Report(WinRTRules.NoPublicTypesRule, null); }
        }

        /// <summary>
        /// Raises a diagnostic when multiple constructors for a class are defined with the same arity.</summary>
        /// <param name="context"></param><param name="classDeclaration">look for constructors of this class</param>
        /// <returns>True if multiple constructors of the same arity exist for the given class</returns>
        public void HasMultipleConstructorsOfSameArity(ClassDeclarationSyntax classDeclaration)
        {
            IEnumerable<ConstructorDeclarationSyntax> constructors = classDeclaration.ChildNodes().OfType<ConstructorDeclarationSyntax>().Where(IsPublic);

            HashSet<int> aritiesSeenSoFar = new HashSet<int>();

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

        /// <summary>Checks to see if the class declares any operators (overloading them)</summary>
        ///></param><param name="classDeclaration">
        /// Class to check for operator declarations 
        /// operator declarations are just like method declarations except they use the `operator` keyword</param>
        /// <returns>True iff an operator is overloaded by the given class</returns>
        public bool OverloadsOperator(ClassDeclarationSyntax classDeclaration)
        {
            var operatorDeclarations = classDeclaration.DescendantNodes().OfType<OperatorDeclarationSyntax>();
            foreach (var op in operatorDeclarations) { Report(WinRTRules.OperatorOverloadedRule, op.GetLocation(), op.OperatorToken); } 
            return operatorDeclarations.Count() != 0;
        }

        /// <summary>
        /// The code generation process makes functions with output param `__retval`, 
        /// we will shadow a user variable named the same thing -- so raise a diagnostic instead</summary>
        /// <param name="context">compilation unit to raise diagnostic on</param><param name="method">the method whose parameteres we are inspecting</param>
        public void HasConflictingParameterName(MethodDeclarationSyntax method)
        {
            var hasInvalidParams = method.ParameterList.Parameters.Where(param => SyntaxTokenIs(param.Identifier, GeneratedReturnValueName)).Any();
            if (hasInvalidParams) 
            { 
                Report(WinRTRules.ParameterNamedValueRule, method.GetLocation(), method.Identifier); 
            }
        }

        public void HasInvalidNamespace(HashSet<INamespaceSymbol> definedNamespaces, string assemblyName)
        {
            HashSet<string> simplifiedNames = new HashSet<string>();

            foreach (var namespaceSymbol in definedNamespaces)
            {
                string upperNamed = namespaceSymbol.ToString().ToUpper();
                if (simplifiedNames.Contains(upperNamed))
                {
                    Report(WinRTRules.NamespacesDifferByCase, namespaceSymbol.Locations.First(), namespaceSymbol.Name);
                }
                else
                {
                    simplifiedNames.Add(upperNamed);
                }

                if (IsInvalidNamespace(namespaceSymbol, assemblyName))
                {
                    Report(WinRTRules.DisjointNamespaceRule, namespaceSymbol.Locations.First(), assemblyName, namespaceSymbol.Name);
                }
            }
        }

        public void CheckMethods(IEnumerable<MethodDeclarationSyntax> methodDeclarations, SyntaxToken typeId)
        {
            Dictionary<string, bool> methodsHasAttributeMap = new Dictionary<string, bool>();

            /* we can't throw the diagnostic as soon as we see a second overload without an attribute, 
             *   as there could be a third overload with the default attribute
             * So store a diagnostic in case we see all methods and none of this overload have the attribute */
            Dictionary<string, Diagnostic> overloadsWithoutAttributeMap = new Dictionary<string, Diagnostic>();

            // var methodDeclarations = interfaceDeclaration.DescendantNodes().OfType<MethodDeclarationSyntax>();
            foreach (MethodDeclarationSyntax method in methodDeclarations)
            {
                CheckOverloadAttributes(method, methodsHasAttributeMap, overloadsWithoutAttributeMap, typeId);
                HasConflictingParameterName(method);
                CheckSignature(method, method.GetLocation(), method.Identifier, typeId);
                CheckParamsForArrayAttributes(method);
            }
            /* Finishes up the work started by `CheckOverloadAttributes` */
            foreach (var thing in overloadsWithoutAttributeMap)
            {
                ReportDiagnostic(thing.Value);
            }
        }

        /// <summary>Looks at all the properties of the given class and checks them for improper array types (System.Array instances, multidimensional, jagged)</summary>
        /// <param name="context"></param><param name="classDeclaration"></param>
        /// <returns>True iff any of the invalid array types are used in any of the propertyy signatures in the given class</returns>
        public void CheckSignatureOfProperties(IEnumerable<PropertyDeclarationSyntax> props, SyntaxToken typeId)
        {
            foreach (var prop in props)
            {
                CheckSignature(prop, prop.GetLocation(), prop.Identifier, typeId);
            }
        }

        public void CheckStructField(StructDeclarationSyntax structDeclaration, HashSet<INamedTypeSymbol> userCreatedTypes, INamedTypeSymbol sym)
        { 
            // Helper function, raises diagnostic if the struct has the given kind of field 
            void StructHasFieldOfType<T>() where T : MemberDeclarationSyntax
            { 
                if (structDeclaration.DescendantNodes().OfType<T>().Any())
                {
                    Report(WinRTRules.StructHasInvalidFieldRule2, structDeclaration.GetLocation(), structDeclaration.Identifier,  SimplifySyntaxTypeString(typeof(T).Name));
                };
            }

            StructHasFieldOfType<ConstructorDeclarationSyntax>();
            StructHasFieldOfType<DelegateDeclarationSyntax>();
            StructHasFieldOfType<EventFieldDeclarationSyntax>();
            StructHasFieldOfType<IndexerDeclarationSyntax>();
            StructHasFieldOfType<MethodDeclarationSyntax>();
            StructHasFieldOfType<OperatorDeclarationSyntax>();
            StructHasFieldOfType<PropertyDeclarationSyntax>();

            var fields = structDeclaration.DescendantNodes().OfType<FieldDeclarationSyntax>();
            foreach (var field in fields) 
            {
                CheckFieldValidity(field, structDeclaration.Identifier, userCreatedTypes); 
            }
            if (!fields.Any())
            {
                Report(WinRTRules.StructWithNoFieldsRule, structDeclaration.GetLocation(), sym);
            }
        }

        public void UnsealedClass(ISymbol sym, ClassDeclarationSyntax classDeclaration)
        {
            if (!sym.IsSealed)
            {
                Report(WinRTRules.UnsealedClassRule, classDeclaration.GetLocation(), classDeclaration.Identifier);
            }
        }

        /// <summary>
        /// returns true if there is a field declared private, 
        /// or  declared with a type that is a class or one of object, byte or dynamic</summary> 
        /// <param name="context"></param><param name="field">The field to inspect</param><param name="structId">The name of the struct the field belongs to</param>
        /// <param name="typeNames">A list of qualified class and interface names, which are invalid types to use in a struct for WinRT Component</param>
        /// <returns>True if the struct has a field of an type that is not supported in Windows Runtime</returns>
        public void CheckFieldValidity(
            FieldDeclarationSyntax field, 
            SyntaxToken structId, 
            HashSet<INamedTypeSymbol> typeNames)
        {
            if (!IsPublic(field))
            { 
                Report(WinRTRules.StructHasPrivateFieldRule, field.GetLocation(), structId);
            }

            if (ModifiersContains(field.Modifiers, "const"))
            {
                Report(WinRTRules.StructHasConstFieldRule, field.GetLocation(), structId);
            }

            foreach (var variable in field.DescendantNodes().OfType<VariableDeclarationSyntax>())
            {
                var typeStr = variable.Type.ToString();

                if (SymbolSetHasString(typeNames, typeStr) || typeStr == "dynamic" || typeStr == "object")
                { 
                    Report(WinRTRules.StructHasInvalidFieldRule, variable.GetLocation(), structId, field.ToString(), typeStr);
                }
            }
        }
        
        public void TypeIsGeneric(INamedTypeSymbol sym, TypeDeclarationSyntax classDeclaration)
        {
            if (sym.IsGenericType)
            { 
                Report(WinRTRules.GenericTypeRule, classDeclaration.GetLocation(), classDeclaration.Identifier);
            }
        }
    }
 }
