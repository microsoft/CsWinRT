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

        public bool Found() { return _flag; }

        /// <summary>
        /// Gather information on all classes, interfaces and structs
        /// Perform code analysis to find scenarios that are erroneous in Windows Runtime</summary>
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
                    var classId = @class.Identifier;
                    var classSymbol = model.GetDeclaredSymbol(@class);
                    var loc = @class.GetLocation();

                    if (!classSymbol.IsSealed)
                    {
                        Report(WinRTRules.UnsealedClassRule, loc, classId);
                    }

                    OverloadsOperator(@class);
                    HasMultipleConstructorsOfSameArity(@class);

                    if (classSymbol.IsGenericType)
                    { 
                        Report(WinRTRules.GenericTypeRule, @class.GetLocation(), classId);
                    }

                    // check for things in nonWindowsRuntimeInterfaces
                    ImplementsInvalidInterface(classSymbol, @class);
                    
                    var props = @class.DescendantNodes().OfType<PropertyDeclarationSyntax>().Where(IsPublic);
                    CheckPropertySignature(props, classId);
                    
                    var publicMethods = @class.ChildNodes().OfType<MethodDeclarationSyntax>().Where(IsPublic);
                    CheckMethods(publicMethods, classId);
                }

                var interfaces = nodes.OfType<InterfaceDeclarationSyntax>().Where(IsPublic);
                foreach (InterfaceDeclarationSyntax @interface in interfaces)
                {
                    var interfaceSym = model.GetDeclaredSymbol(@interface);
                    
                    if (interfaceSym.IsGenericType)
                    {
                        Report(WinRTRules.GenericTypeRule, @interface.GetLocation(), @interface.Identifier);
                    }

                    ImplementsInvalidInterface(interfaceSym, @interface);
                    
                    var props = @interface.DescendantNodes().OfType<PropertyDeclarationSyntax>().Where(IsPublic);
                    CheckPropertySignature(props, @interface.Identifier);
                    
                    var methods = @interface.DescendantNodes().OfType<MethodDeclarationSyntax>();
                    CheckMethods(methods, @interface.Identifier);
                }

                var structs = nodes.OfType<StructDeclarationSyntax>();
                foreach (StructDeclarationSyntax @struct in structs)
                {
                    CheckStructFields(@struct, userCreatedTypes); 
                }
            } 
        }

        /// <summary>
        /// Returns true if the class represented by the symbol 
        /// implements any of the interfaces defined in ProhibitedAsyncInterfaces (e.g., IAsyncAction, ...)</summary>
        /// <param name="typeSymbol">The class/interface type symbol</param><param name="typeDeclaration">The containing class/interface declaration</param>
        /// <returns>True iff the given class implements any of the IAsync interfaces that are not valid in Windows Runtime</returns>
        private void ImplementsInvalidInterface(INamedTypeSymbol typeSymbol, TypeDeclarationSyntax typeDeclaration)
        {
            foreach (string prohibitedInterface in nonWindowsRuntimeInterfaces)
            {
                if (ImplementsInterface(typeSymbol, prohibitedInterface))
                {
                    Report(WinRTRules.NonWinRTInterface, typeDeclaration.GetLocation(), typeDeclaration.Identifier, prohibitedInterface);
                }
            }
        }

        /// <summary>Checks to see if the class declares any operators (overloading them)</summary>
        ///<param name="classDeclaration">Class to check</param>
        /// Class to check for operator declarations 
        /// operator declarations are just like method declarations except they use the `operator` keyword</param>
        /// <returns>True iff an operator is overloaded by the given class</returns>
        private bool OverloadsOperator(ClassDeclarationSyntax classDeclaration)
        {
            var operatorDeclarations = classDeclaration.DescendantNodes().OfType<OperatorDeclarationSyntax>();
            foreach (var op in operatorDeclarations) { Report(WinRTRules.OperatorOverloadedRule, op.GetLocation(), op.OperatorToken); } 
            return operatorDeclarations.Count() != 0;
        }

        /// <summary>Raise a diagnostic if there are no public types in the namespace</summary>
        /// <param name="publicTypes">collection of class and interface symbols </param>
        /// <param name="publicStructs">collection of struct symbols</param>
        private void HasSomePublicTypes(HashSet<INamedTypeSymbol> publicTypes, HashSet<INamedTypeSymbol> publicStructs) 
        {
            // types are interfaces, classes and structs
            if (!publicTypes.Any() && !publicStructs.Any()) { Report(WinRTRules.NoPublicTypesRule, null); }
        }

        /// <summary>
        /// Raises a diagnostic when multiple constructors for a class are defined with the same arity.</summary>
        /// <param name="classDeclaration">Look at constructors of this class</param>
        private void HasMultipleConstructorsOfSameArity(ClassDeclarationSyntax classDeclaration)
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

        /// <summary>
        /// The code generation process makes functions with output param `__retval`, 
        /// we will shadow a user variable named the same thing -- so raise a diagnostic instead</summary>
        /// <param name="method">the method whose parameteres we are inspecting</param>
        private void HasConflictingParameterName(MethodDeclarationSyntax method)
        {
            // check if the identifier is our special name GeneratedReturnValueName
            bool IsInvalidParameterName(ParameterSyntax stx) { return stx.Identifier.Value.Equals(GeneratedReturnValueName); }

            var hasInvalidParams = method.ParameterList.Parameters.Where(IsInvalidParameterName).Any();
            if (hasInvalidParams) 
            { 
                Report(WinRTRules.ParameterNamedValueRule, method.GetLocation(), method.Identifier); 
            }
        }

        /// <summary>
        /// Namespaces can't only differ by cases, also check if the name is invalid for the winmd being made</summary>
        /// <param name="definedNamespaces"></param><param name="assemblyName">"Test" for unit tests, otherwise stored in AnalyzerconfigPropertyProvider</param>
        private void HasInvalidNamespace(HashSet<INamespaceSymbol> definedNamespaces, string assemblyName)
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

        /// <summary>
        /// Look for overloads/defaultoverload attribute, conflicting parameter names, 
        /// parameter attribute errors, invalid types</summary>
        /// <param name="methodDeclarations">Collection of methods</param><param name="typeId">Containing class or interface</param>
        private void CheckMethods(IEnumerable<MethodDeclarationSyntax> methodDeclarations, SyntaxToken typeId)
        {
            Dictionary<string, bool> methodsHasAttributeMap = new Dictionary<string, bool>();
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

                var methodSym = _context.Compilation.GetSemanticModel(method.SyntaxTree).GetDeclaredSymbol(method);

                ReportIfInvalidType(methodSym.ReturnType, method.GetLocation(), method.Identifier, typeId);
                foreach (var arg in methodSym.Parameters)
                {
                    ReportIfInvalidType(arg.Type, method.GetLocation(), method.Identifier, typeId);
                } 
            }
            /* Finishes up the work started by `CheckOverloadAttributes` */
            foreach (var thing in overloadsWithoutAttributeMap)
            {
                ReportDiagnostic(thing.Value);
            }
        }

        /// <summary>Looks at all the properties of the given class and checks them for improper array types (System.Array instances, multidimensional, jagged)</summary>
        ///<param name="props">collection of properties</param><param name="typeId">containing class/interface</param>
        /// <returns>True iff any of the invalid array types are used in any of the propertyy signatures in the given class</returns>
        private void CheckPropertySignature(IEnumerable<PropertyDeclarationSyntax> props, SyntaxToken typeId)
        {
            foreach (var prop in props)
            {
                var propSym = _context.Compilation.GetSemanticModel(prop.SyntaxTree).GetDeclaredSymbol(prop);
                var loc = prop.GetLocation();

                ReportIfInvalidType(propSym.Type, loc, prop.Identifier, typeId);
                foreach (var arg in propSym.Parameters)
                {
                    ReportIfInvalidType(arg.Type, loc, prop.Identifier, typeId);
                }
            }
        }

        /// <summary>All struct fields must be public, of basic types, and not const</summary>
        /// <param name="struct">struct declaration</param><param name="userCreatedTypes">Classes/interfaces the author defined</param>
        private void CheckStructFields(StructDeclarationSyntax @struct, HashSet<INamedTypeSymbol> userCreatedTypes)
        { 
            // Helper function, raises diagnostic if the struct has the given kind of field 
            void StructHasFieldOfType<T>() where T : MemberDeclarationSyntax
            { 
                if (@struct.DescendantNodes().OfType<T>().Any())
                {
                    Report(WinRTRules.StructHasInvalidFieldRule, @struct.GetLocation(), @struct.Identifier,  SimplifySyntaxTypeString(typeof(T).Name));
                };
            }

            StructHasFieldOfType<ConstructorDeclarationSyntax>();
            StructHasFieldOfType<DelegateDeclarationSyntax>();
            StructHasFieldOfType<EventFieldDeclarationSyntax>();
            StructHasFieldOfType<IndexerDeclarationSyntax>();
            StructHasFieldOfType<MethodDeclarationSyntax>();
            StructHasFieldOfType<OperatorDeclarationSyntax>();
            StructHasFieldOfType<PropertyDeclarationSyntax>();

            var fields = @struct.DescendantNodes().OfType<FieldDeclarationSyntax>();
            foreach (var field in fields) 
            {
                CheckFieldValidity(field, @struct.Identifier, userCreatedTypes);
            }
            // Public types must have some member
            if (!fields.Any())
            {
                Report(WinRTRules.StructWithNoFieldsRule, @struct.GetLocation(), @struct.Identifier);
            }
        }

        /// <summary>Struct fields must be public, cannot be const, or `object`, `dynamic`, or any defined class/interface type</summary> 
        /// <param name="field">The field to inspect</param><param name="structId">The name of the struct the field belongs to</param>
        /// <param name="typeNames">A list of qualified class and interface names, which are invalid types to use in a struct for WinRT Component</param>
        private void CheckFieldValidity(
            FieldDeclarationSyntax field, 
            SyntaxToken structId, 
            HashSet<INamedTypeSymbol> typeNames)
        {

            if (!IsPublic(field))
            { 
                Report(WinRTRules.StructHasPrivateFieldRule, field.GetLocation(), structId);
            }

            if (field.Modifiers.Where(m => m.IsKind(SyntaxKind.ConstKeyword)).Any())
            {
                Report(WinRTRules.StructHasConstFieldRule, field.GetLocation(), structId);
            }

            // helper function to see if a string is in the list of typenames
            bool SymbolSetHasString(string typeStr) { return typeNames.Where(sym => sym.ToString().Contains(typeStr)).Any(); }

            foreach (var variable in field.DescendantNodes().OfType<VariableDeclarationSyntax>())
            {
                // No type info for dynamic, cant get type symbol for variable declaration
                var typeStr = variable.Type.ToString();
                // can't know if it's predefined type syntax to see if it's ObjectKeyword
                if (SymbolSetHasString(typeStr) || typeStr == "dynamic" || typeStr == "object")
                { 
                    Report(WinRTRules.StructHasInvalidFieldRule, variable.GetLocation(), structId, typeStr);
                }
            }
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
    }
}
