using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using WinRT.SourceGenerator;

namespace Generator 
{
    public partial class WinRTRules
    {

        public WinRTRules() { _flag = false; }

        public WinRTRules(GeneratorExecutionContext context) { _flag = false; }

        private bool _flag;

        private GeneratorExecutionContext _context;

        private void Flag() { _flag |= true; }

        public bool Found() { return _flag; }

        public bool IsPublic<T>(T p) where T : MemberDeclarationSyntax { return ModifiersContains(p.Modifiers, "public");  }

        /// <summary>
        /// Returns true if the class represented by the symbol 
        /// implements any of the interfaces defined in ProhibitedAsyncInterfaces (e.g., IAsyncAction, ...) /// </summary>
        /// <param name="context"></param><param name="typeSymbol"></param><param name="classDeclaration"></param>
        /// <returns>True iff the given class implements any of the IAsync interfaces that are not valid in Windows Runtime</returns>
        public void ImplementsInvalidInterface<T>(ref GeneratorExecutionContext context, INamedTypeSymbol typeSymbol, T typeDeclaration)
            where T : TypeDeclarationSyntax
        {
            foreach (string prohibitedInterface in nonWinRuntimeInterfaces)
            {
                if (ImplementsInterface(typeSymbol, prohibitedInterface))
                {
                    Report(ref context, DiagnosticRules.NonWinRTInterface, typeDeclaration.GetLocation(), typeDeclaration.Identifier, prohibitedInterface);
                }
            }
        }

        /// <summary>
        /// Raises a diagnostic when multiple constructors for a class are defined with the same arity.</summary>
        /// <param name="context"></param><param name="classDeclaration">look for constructors of this class</param>
        /// <returns>True if multiple constructors of the same arity exist for the given class</returns>
        public void HasMultipleConstructorsOfSameArity(ref GeneratorExecutionContext context, ClassDeclarationSyntax classDeclaration)
        {
            IEnumerable<ConstructorDeclarationSyntax> constructors = classDeclaration.ChildNodes().OfType<ConstructorDeclarationSyntax>().Where(IsPublic);

            HashSet<int> aritiesSeenSoFar = new HashSet<int>();

            foreach (ConstructorDeclarationSyntax constructor in constructors)
            {
                int arity = constructor.ParameterList.Parameters.Count;
                if (aritiesSeenSoFar.Contains(arity))
                {
                    Report(ref context, DiagnosticRules.ClassConstructorRule, constructor.GetLocation(), classDeclaration.Identifier, arity);
                }
                else
                {
                    aritiesSeenSoFar.Add(arity);
                }
            }
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


        /// <summary>
        /// The code generation process makes functions with output param `__retval`, 
        /// we will shadow a user variable named the same thing -- so raise a diagnostic instead</summary>
        /// <param name="context">compilation unit to raise diagnostic on</param><param name="method">the method whose parameteres we are inspecting</param>
        public void HasConflictingParameterName(ref GeneratorExecutionContext context, MethodDeclarationSyntax method)
        {
            var hasInvalidParams = method.ParameterList.Parameters.Where(param => SyntaxTokenIs(param.Identifier, GeneratedReturnValueName)).Any();
            if (hasInvalidParams) 
            { 
                Report(ref context, DiagnosticRules.ParameterNamedValueRule, method.GetLocation(), method.Identifier); 
            }
        }

        public void HasInvalidMethods<T>(ref GeneratorExecutionContext context, IEnumerable<MethodDeclarationSyntax> methodDeclarations, SyntaxToken typeId)
            where T : TypeDeclarationSyntax
        {
            Dictionary<string, bool> methodsHasAttributeMap = new Dictionary<string, bool>();

            /* we can't throw the diagnostic as soon as we see a second overload without an attribute, 
             *   as there could be a third overload with the default attribute
             * So store a diagnostic in case we see all methods and none of this overload have the attribute */
            Dictionary<string, Diagnostic> overloadsWithoutAttributeMap = new Dictionary<string, Diagnostic>();

            // var methodDeclarations = interfaceDeclaration.DescendantNodes().OfType<MethodDeclarationSyntax>();
            foreach (MethodDeclarationSyntax method in methodDeclarations)
            {
                CheckOverloadAttributes(ref context, method, ref methodsHasAttributeMap, ref overloadsWithoutAttributeMap, typeId);
                HasConflictingParameterName(ref context, method);
                // found |= CheckMethod(ref context, method, typeId);
                CheckSignature(ref context, method, method.GetLocation(), method.Identifier, typeId);
                CheckParamsForArrayAttributes(method, ref context);

            }
            /* Finishes up the work started by `CheckOverloadAttributes` */
            foreach (var thing in overloadsWithoutAttributeMap)
            {
                context.ReportDiagnostic(thing.Value);
                Flag();
            }
        }

        /// <summary>Looks at all the properties of the given class and checks them for improper array types (System.Array instances, multidimensional, jagged)</summary>
        /// <param name="context"></param><param name="classDeclaration"></param>
        /// <returns>True iff any of the invalid array types are used in any of the propertyy signatures in the given class</returns>
        public void CheckSignatureOfProperties(ref GeneratorExecutionContext context, IEnumerable<PropertyDeclarationSyntax> props, SyntaxToken typeId)
        {
            foreach (var prop in props)
            {
                CheckSignature(ref context, prop, prop.GetLocation(), prop.Identifier, typeId);
            }
        }
       /// <summary>
        /// returns true iff there is a field of the given type in the given struct 
        /// e.g., if T is PropertyDeclarationSyntax, then if the struct has a property, we report a diagnostic and return true</summary>
        /// <typeparam name="T">T can vary over MethodDeclartion, EventDeclaration, etc... </typeparam>
        /// <param name="context"></param><param name="structDeclaration"></param><returns></returns>
        public void StructHasFieldOfType<T>(ref GeneratorExecutionContext context, StructDeclarationSyntax structDeclaration)
        {
            if (structDeclaration.DescendantNodes().OfType<T>().Any())
            {
                Report(ref context, DiagnosticRules.StructHasInvalidFieldRule2, structDeclaration.GetLocation(), structDeclaration.Identifier,  SimplifySyntaxTypeString(typeof(T).Name));
            }
        }

        public void CheckStructField(ref GeneratorExecutionContext context, StructDeclarationSyntax structDeclaration, HashSet<INamedTypeSymbol> userCreatedTypes, INamedTypeSymbol sym)
        { 
            /*
            void StructHasFieldOfType<T>() where T : MemberDeclarationSyntax
            { 
                if (structDeclaration.DescendantNodes().OfType<T>().Any())
                {
                    Report(ref context, DiagnosticRules.StructHasInvalidFieldRule2, structDeclaration.GetLocation(), structDeclaration.Identifier,  SimplifySyntaxTypeString(typeof(T).Name));
                };
            }
            */

            StructHasFieldOfType<ConstructorDeclarationSyntax>(ref context, structDeclaration);
            StructHasFieldOfType<DelegateDeclarationSyntax>(ref context, structDeclaration);
            StructHasFieldOfType<EventFieldDeclarationSyntax>(ref context, structDeclaration);
            StructHasFieldOfType<IndexerDeclarationSyntax>(ref context, structDeclaration);
            StructHasFieldOfType<MethodDeclarationSyntax>(ref context, structDeclaration);
            StructHasFieldOfType<OperatorDeclarationSyntax>(ref context, structDeclaration);
            StructHasFieldOfType<PropertyDeclarationSyntax>(ref context, structDeclaration);

            var fields = structDeclaration.DescendantNodes().OfType<FieldDeclarationSyntax>();
            foreach (var field in fields) 
            {
                CheckFieldValidity(ref context, field, structDeclaration.Identifier, userCreatedTypes); 
            }
            if (!fields.Any())
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticRules.StructWithNoFieldsRule, structDeclaration.GetLocation(), sym));
                Flag();
            }
        }

        public void UnsealedClass(ref GeneratorExecutionContext context, ISymbol sym, ClassDeclarationSyntax classDeclaration)
        {
            if (!sym.IsSealed)
            {
                Report(ref context, DiagnosticRules.UnsealedClassRule, classDeclaration.GetLocation(), classDeclaration.Identifier);
            }
        }

        /// <summary>
        /// returns true if there is a field declared private, 
        /// or  declared with a type that is a class or one of object, byte or dynamic</summary> 
        /// <param name="context"></param><param name="field">The field to inspect</param><param name="structId">The name of the struct the field belongs to</param>
        /// <param name="typeNames">A list of qualified class and interface names, which are invalid types to use in a struct for WinRT Component</param>
        /// <returns>True if the struct has a field of an type that is not supported in Windows Runtime</returns>
        public void CheckFieldValidity(ref GeneratorExecutionContext context, 
            FieldDeclarationSyntax field, 
            SyntaxToken structId, 
            HashSet<INamedTypeSymbol> typeNames)
        {
            if (!IsPublic(field))
            { 
                Report(ref context, DiagnosticRules.StructHasPrivateFieldRule, field.GetLocation(), structId);
            }

            if (ModifiersContains(field.Modifiers, "const"))
            {
                Report(ref context, DiagnosticRules.StructHasConstFieldRule, field.GetLocation(), structId);
            }

            foreach (var variable in field.DescendantNodes().OfType<VariableDeclarationSyntax>())
            {
                var typeStr = variable.Type.ToString();

                if (SymbolSetHasString(typeNames, typeStr) || typeStr == "dynamic" || typeStr == "object")
                { 
                    Report(ref context, DiagnosticRules.StructHasInvalidFieldRule, variable.GetLocation(), structId, field.ToString(), typeStr);
                }
            }
        }
        
        public void TypeIsGeneric<T>(ref GeneratorExecutionContext context, T classDeclaration)
            where T : TypeDeclarationSyntax
        {
            var genericArgs = classDeclaration.ChildNodes().OfType<TypeParameterListSyntax>(); 
            if (genericArgs.Any()) 
            { 
                Report(ref context, DiagnosticRules.GenericTypeRule, classDeclaration.GetLocation(), classDeclaration.Identifier);
            }
        }
    }
 }
