// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WinRT.SourceGenerator
{
    [DiagnosticAnalyzer(LanguageNames.CSharp), Shared]
    public sealed class WinRTAotDiagnosticAnalyzer : DiagnosticAnalyzer
    {
        private static ImmutableArray<DiagnosticDescriptor> _supportedDiagnostics = 
            ImmutableArray.Create(
                WinRTRules.ClassNotAotCompatibleWarning,
                WinRTRules.ClassNotAotCompatibleInfo,
                WinRTRules.ClassNotAotCompatibleOldProjectionWarning,
                WinRTRules.ClassNotAotCompatibleOldProjectionInfo,
                WinRTRules.ClassEnableUnsafeWarning,
                WinRTRules.ClassEnableUnsafeInfo,
                WinRTRules.ClassWithBindableCustomPropertyNotPartial);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => _supportedDiagnostics;

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(static context =>
            {
                if (!context.Options.AnalyzerConfigOptionsProvider.IsCsWinRTAotOptimizerEnabled())
                {
                    return;
                }

                bool isComponentProject = context.Options.AnalyzerConfigOptionsProvider.IsCsWinRTComponent();
                var winrtTypeAttribute = context.Compilation.GetTypeByMetadataName("WinRT.WindowsRuntimeTypeAttribute");
                var winrtExposedTypeAttribute = context.Compilation.GetTypeByMetadataName("WinRT.WinRTExposedTypeAttribute");
                var generatedBindableCustomPropertyAttribute = context.Compilation.GetTypeByMetadataName("WinRT.GeneratedBindableCustomPropertyAttribute");
                if (winrtTypeAttribute is null || winrtExposedTypeAttribute is null || generatedBindableCustomPropertyAttribute is null)
                {
                    return;
                }

                bool isCsWinRTAotOptimizerAutoMode = GeneratorExecutionContextHelper.IsCsWinRTAotOptimizerInAutoMode(context.Options.AnalyzerConfigOptionsProvider, context.Compilation);
                var generatedWinRTExposedTypeAttribute = context.Compilation.GetTypeByMetadataName("WinRT.GeneratedWinRTExposedTypeAttribute");
                var generatedWinRTExposedExternalTypeAttribute = context.Compilation.GetTypeByMetadataName("WinRT.GeneratedWinRTExposedExternalTypeAttribute");
                if (!isCsWinRTAotOptimizerAutoMode && (generatedWinRTExposedTypeAttribute is null || generatedWinRTExposedExternalTypeAttribute is null))
                {
                    return;
                }

                var typeMapper = new TypeMapper(context.Options.AnalyzerConfigOptionsProvider.GetCsWinRTUseWindowsUIXamlProjections());
                var csWinRTAotWarningLevel = context.Options.AnalyzerConfigOptionsProvider.GetCsWinRTAotWarningLevel();
                var allowUnsafe = GeneratorHelper.AllowUnsafe(context.Compilation);
                var isCsWinRTCcwLookupTableGeneratorEnabled = context.Options.AnalyzerConfigOptionsProvider.IsCsWinRTCcwLookupTableGeneratorEnabled();

                context.RegisterSymbolAction(context =>
                {
                    // Filter to classes that can be passed as objects.
                    if (context.Symbol is INamedTypeSymbol namedType &&
                        namedType.TypeKind == TypeKind.Class &&
                        !namedType.IsAbstract &&
                        !namedType.IsStatic)
                    {
                        // Make sure classes with the GeneratedBindableCustomProperty attribute are marked partial.
                        if (GeneratorHelper.HasAttributeWithType(namedType, generatedBindableCustomPropertyAttribute) &&
                            !GeneratorHelper.IsPartial(namedType))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(WinRTRules.ClassWithBindableCustomPropertyNotPartial, namedType.Locations[0], namedType.Name));
                        }

                        // In opt-in mode, make sure it has the attribute asking to generate the vtable for it.
                        if (!isCsWinRTAotOptimizerAutoMode && !GeneratorHelper.HasAttributeWithType(namedType, generatedWinRTExposedTypeAttribute))
                        {
                            return;
                        }

                        // Make sure this is a class that we would generate the WinRTExposedType attribute on
                        // and that it isn't already partial.
                        if (!GeneratorHelper.IsWinRTType(namedType, winrtTypeAttribute, typeMapper, isComponentProject, context.Compilation.Assembly) &&
                            !GeneratorHelper.HasNonInstantiatedWinRTGeneric(namedType, typeMapper) &&
                            !GeneratorHelper.HasAttributeWithType(namedType, winrtExposedTypeAttribute))
                        {
                            var interfacesFromOldProjections = new HashSet<string>();
                            bool implementsWinRTInterfaces = false;
                            bool implementsCustomMappedInterfaces = false;
                            bool implementsGenericInterfaces = false;
                            foreach (var iface in namedType.AllInterfaces)
                            {
                                if (GeneratorHelper.IsWinRTType(iface, winrtTypeAttribute, typeMapper, isComponentProject, context.Compilation.Assembly))
                                {
                                    if (!iface.IsGenericType &&
                                        GeneratorHelper.IsOldProjectionAssembly(iface.ContainingAssembly))
                                    {
                                        interfacesFromOldProjections.Add(iface.Name);
                                    }

                                    bool isCustomMappedType = GeneratorHelper.IsCustomMappedType(iface, typeMapper);
                                    implementsCustomMappedInterfaces |= isCustomMappedType;
                                    implementsWinRTInterfaces |= !isCustomMappedType;
                                    implementsGenericInterfaces |= iface.IsGenericType;
                                }
                            }

                            if (!GeneratorHelper.IsPartial(namedType) && 
                                (implementsWinRTInterfaces || implementsCustomMappedInterfaces))
                            {
                                // Based on the warning level, emit as a warning or as an info.
                                var diagnosticDescriptor = (csWinRTAotWarningLevel >= 2 || (csWinRTAotWarningLevel == 1 && implementsWinRTInterfaces)) ?
                                    WinRTRules.ClassNotAotCompatibleWarning : WinRTRules.ClassNotAotCompatibleInfo;
                                context.ReportDiagnostic(Diagnostic.Create(diagnosticDescriptor, namedType.Locations[0], namedType.Name));
                            }

                            if (!allowUnsafe && implementsGenericInterfaces)
                            {
                                // Based on the warning level, emit as a warning or as an info.
                                var diagnosticDescriptor = (csWinRTAotWarningLevel >= 2 || (csWinRTAotWarningLevel == 1 && implementsWinRTInterfaces)) ?
                                    WinRTRules.ClassEnableUnsafeWarning : WinRTRules.ClassEnableUnsafeInfo;
                                context.ReportDiagnostic(Diagnostic.Create(diagnosticDescriptor, namedType.Locations[0], namedType.Name));
                            }

                            if (interfacesFromOldProjections.Count > 0)
                            {
                                var diagnosticDescriptor = csWinRTAotWarningLevel >= 1 ?
                                    WinRTRules.ClassNotAotCompatibleOldProjectionWarning : WinRTRules.ClassNotAotCompatibleOldProjectionInfo;
                                context.ReportDiagnostic(Diagnostic.Create(diagnosticDescriptor, namedType.Locations[0], namedType.Name, string.Join(", ", interfacesFromOldProjections)));
                            }
                        }
                    }
                }, SymbolKind.NamedType);

                if (!allowUnsafe && isCsWinRTCcwLookupTableGeneratorEnabled && isCsWinRTAotOptimizerAutoMode)
                {
                    context.RegisterSyntaxNodeAction(context =>
                    {
                        var isWinRTType = GeneratorHelper.IsWinRTType(context.SemanticModel.Compilation, isComponentProject);
                        var isWinRTClassOrInterface = GeneratorHelper.IsWinRTClassOrInterface(context.SemanticModel.Compilation, isWinRTType, typeMapper);

                        if (context.Node is InvocationExpressionSyntax invocation)
                        {
                            var invocationSymbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol;
                            if (invocationSymbol is IMethodSymbol methodSymbol)
                            {
                                var taskAdapter = GeneratorHelper.GetTaskAdapterIfAsyncMethod(methodSymbol);
                                if (!string.IsNullOrEmpty(taskAdapter))
                                {
                                    ReportEnableUnsafeDiagnostic(context.ReportDiagnostic, methodSymbol, invocation.GetLocation(), true);
                                    return;
                                }
                                // Filter checks for boxing and casts to ones calling CsWinRT projected functions and
                                // functions within same assembly.  Functions within same assembly can take a boxed value
                                // and end up calling a projection function (i.e. ones generated by XAML compiler)
                                // In theory, another library can also be called which can call a projected function
                                // but not handling those scenarios for now.
                                else if(isWinRTClassOrInterface(methodSymbol.ContainingSymbol, true) ||
                                 SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingAssembly, context.SemanticModel.Compilation.Assembly))
                                {
                                    // Get the concrete types directly from the argument rather than
                                    // using what the method accepts, which might just be an interface, so
                                    // that we can try to include any other WinRT interfaces implemented by
                                    // that type on the CCW when it is marshaled.
                                    for (int idx = 0, paramsIdx = 0; idx < invocation.ArgumentList.Arguments.Count; idx++)
                                    {
                                        if (methodSymbol.Parameters[paramsIdx].RefKind != RefKind.Out)
                                        {
                                            var argumentType = context.SemanticModel.GetTypeInfo(invocation.ArgumentList.Arguments[idx].Expression);
                                            if (IsTypeOnLookupTable(argumentType, methodSymbol.Parameters[paramsIdx].Type, out var implementsOnlyCustomMappedInterface))
                                            {
                                                ReportEnableUnsafeDiagnostic(context.ReportDiagnostic, argumentType.Type, invocation.GetLocation(), !implementsOnlyCustomMappedInterface);
                                                return;
                                            }
                                        }

                                        // The method parameter can be declared as params which means
                                        // an array of arguments can be passed for it and it is the
                                        // last argument.
                                        if (!methodSymbol.Parameters[paramsIdx].IsParams)
                                        {
                                            paramsIdx++;
                                        }
                                    }
                                }
                            }
                        }
                        else if (context.Node is AssignmentExpressionSyntax assignment)
                        {
                            var leftSymbol = context.SemanticModel.GetSymbolInfo(assignment.Left).Symbol;
                            if (leftSymbol is IPropertySymbol propertySymbol &&
                                (isWinRTClassOrInterface(propertySymbol.ContainingSymbol, true) ||
                                 SymbolEqualityComparer.Default.Equals(propertySymbol.ContainingAssembly, context.SemanticModel.Compilation.Assembly)))
                            {
                                var assignmentType = context.SemanticModel.GetTypeInfo(assignment.Right);
                                if (IsTypeOnLookupTable(assignmentType, propertySymbol.Type, out var implementsOnlyCustomMappedInterface))
                                {
                                    ReportEnableUnsafeDiagnostic(context.ReportDiagnostic, assignmentType.Type, assignment.GetLocation(), !implementsOnlyCustomMappedInterface);
                                    return;
                                }
                            }
                            else if (leftSymbol is IFieldSymbol fieldSymbol &&
                                // WinRT interfaces don't have fields, so we don't need to check for them.
                                (isWinRTClassOrInterface(fieldSymbol.ContainingSymbol, false) ||
                                 SymbolEqualityComparer.Default.Equals(fieldSymbol.ContainingAssembly, context.SemanticModel.Compilation.Assembly)))
                            {
                                var assignmentType = context.SemanticModel.GetTypeInfo(assignment.Right);
                                if (IsTypeOnLookupTable(assignmentType, fieldSymbol.Type, out var implementsOnlyCustomMappedInterface))
                                {
                                    ReportEnableUnsafeDiagnostic(context.ReportDiagnostic, assignmentType.Type, assignment.GetLocation(), !implementsOnlyCustomMappedInterface);
                                    return;
                                }
                            }
                        }
                        // Detect scenarios where the variable declaration is to a boxed or cast type during initialization.
                        else if (context.Node is VariableDeclarationSyntax variableDeclaration)
                        {
                            var leftSymbol = context.SemanticModel.GetSymbolInfo(variableDeclaration.Type).Symbol;
                            if (leftSymbol is INamedTypeSymbol namedType)
                            {
                                foreach (var variable in variableDeclaration.Variables)
                                {
                                    if (variable.Initializer != null)
                                    {
                                        var instantiatedType = context.SemanticModel.GetTypeInfo(variable.Initializer.Value);
                                        if (IsTypeOnLookupTable(instantiatedType, namedType, out var implementsOnlyCustomMappedInterface))
                                        {
                                            ReportEnableUnsafeDiagnostic(context.ReportDiagnostic, instantiatedType.Type, variableDeclaration.GetLocation(), !implementsOnlyCustomMappedInterface);
                                            return;
                                        }
                                    }
                                }
                            }
                        }
                        // Detect scenarios where the property declaration has an initializer and is to a boxed or cast type during initialization.
                        else if (context.Node is PropertyDeclarationSyntax propertyDeclaration)
                        {
                            if (propertyDeclaration.Initializer != null)
                            {
                                var leftSymbol = context.SemanticModel.GetSymbolInfo(propertyDeclaration.Type).Symbol;
                                if (leftSymbol is INamedTypeSymbol namedType)
                                {
                                    var instantiatedType = context.SemanticModel.GetTypeInfo(propertyDeclaration.Initializer.Value);
                                    if (IsTypeOnLookupTable(instantiatedType, namedType, out var implementsOnlyCustomMappedInterface))
                                    {
                                        ReportEnableUnsafeDiagnostic(context.ReportDiagnostic, instantiatedType.Type, propertyDeclaration.GetLocation(), !implementsOnlyCustomMappedInterface);
                                        return;
                                    }
                                }
                            }
                            else if (propertyDeclaration.ExpressionBody != null)
                            {
                                var leftSymbol = context.SemanticModel.GetSymbolInfo(propertyDeclaration.Type).Symbol;
                                if (leftSymbol is INamedTypeSymbol namedType)
                                {
                                    var instantiatedType = context.SemanticModel.GetTypeInfo(propertyDeclaration.ExpressionBody.Expression);
                                    if (IsTypeOnLookupTable(instantiatedType, namedType, out var implementsOnlyCustomMappedInterface))
                                    {
                                        ReportEnableUnsafeDiagnostic(context.ReportDiagnostic, instantiatedType.Type, propertyDeclaration.GetLocation(), !implementsOnlyCustomMappedInterface);
                                        return;
                                    }
                                }
                            }
                        }
                        // Detect scenarios where the method or property being returned from is doing a box or cast of the type
                        // in the return statement.
                        else if (context.Node is ReturnStatementSyntax returnDeclaration && returnDeclaration.Expression is not null)
                        {
                            var returnSymbol = context.SemanticModel.GetTypeInfo(returnDeclaration.Expression);
                            var parent = returnDeclaration.Ancestors().OfType<MemberDeclarationSyntax>().FirstOrDefault();
                            if (parent is MethodDeclarationSyntax methodDeclaration)
                            {
                                var methodReturnSymbol = context.SemanticModel.GetSymbolInfo(methodDeclaration.ReturnType).Symbol;
                                if (methodReturnSymbol is ITypeSymbol typeSymbol)
                                {
                                    if (IsTypeOnLookupTable(returnSymbol, typeSymbol, out var implementsOnlyCustomMappedInterface))
                                    {
                                        ReportEnableUnsafeDiagnostic(context.ReportDiagnostic, returnSymbol.Type, returnDeclaration.GetLocation(), !implementsOnlyCustomMappedInterface);
                                        return;
                                    }
                                }
                            }
                            else if (parent is BasePropertyDeclarationSyntax propertyDeclarationSyntax)
                            {
                                var propertyTypeSymbol = context.SemanticModel.GetSymbolInfo(propertyDeclarationSyntax.Type).Symbol;
                                if (propertyTypeSymbol is ITypeSymbol typeSymbol)
                                {
                                    if (IsTypeOnLookupTable(returnSymbol, typeSymbol, out var implementsOnlyCustomMappedInterface))
                                    {
                                        ReportEnableUnsafeDiagnostic(context.ReportDiagnostic, returnSymbol.Type, returnDeclaration.GetLocation(), !implementsOnlyCustomMappedInterface);
                                        return;
                                    }
                                }
                            }
                        }

                        bool IsTypeOnLookupTable(Microsoft.CodeAnalysis.TypeInfo instantiatedType, ITypeSymbol convertedToTypeSymbol, out bool implementsOnlyCustomMappedInterface)
                        {
                            if (instantiatedType.Type is IArrayTypeSymbol arrayType)
                            {
                                if (convertedToTypeSymbol is not IArrayTypeSymbol &&
                                    // Make sure we aren't just assigning it to a value type such as ReadOnlySpan
                                    !convertedToTypeSymbol.IsValueType &&
                                    GeneratorHelper.IsWinRTTypeOrImplementsWinRTType(arrayType.ElementType, winrtTypeAttribute, typeMapper, isComponentProject, context.Compilation.Assembly))
                                {
                                    // Arrays are projected as a list which is a custom mapped type, but if the element type is an actual WinRT type,
                                    // then we don't consider it as implemented only custom mapped interfaces.
                                    implementsOnlyCustomMappedInterface = GeneratorHelper.IsCustomMappedType(arrayType.ElementType, typeMapper);
                                    return true;
                                }
                            }
                            else if (instantiatedType.Type is not null || instantiatedType.ConvertedType is not null)
                            {
                                // Type might be null such as for lambdas, so check converted type in that case.
                                var instantiatedTypeSymbol = instantiatedType.Type ?? instantiatedType.ConvertedType;

                                // This handles the case where a generic delegate is passed to a parameter
                                // statically declared as an object and thereby we won't be able to detect
                                // its actual type and handle it at compile time within the generated projection.
                                // When it is not declared as an object parameter but rather the generic delegate
                                // type itself, the generated marshaler code in the function makes sure the vtable
                                // information is available.
                                if (instantiatedTypeSymbol.TypeKind == TypeKind.Delegate &&
                                    instantiatedTypeSymbol.MetadataName.Contains("`") &&
                                    GeneratorHelper.IsWinRTType(instantiatedTypeSymbol, winrtTypeAttribute, typeMapper, isComponentProject, context.Compilation.Assembly) &&
                                    convertedToTypeSymbol.SpecialType == SpecialType.System_Object)
                                {
                                    implementsOnlyCustomMappedInterface = GeneratorHelper.IsCustomMappedType(instantiatedTypeSymbol, typeMapper);
                                    return true;
                                }

                                // This handles the case where the source generator wasn't able to run
                                // and put the WinRTExposedType attribute on the class. This can be in the
                                // scenario where the caller defined their own generic class and
                                // pass it as a parameter.  With generic classes, the interface itself
                                // might be generic too and due to that we handle it here.
                                // This also handles the case where the type being passed is from a different
                                // library which happened to not run the AOT optimizer.  So as a best effort,
                                // we handle it here.
                                if (instantiatedTypeSymbol.TypeKind == TypeKind.Class)
                                {
                                    bool addClassOnLookupTable = false;
                                    if (instantiatedTypeSymbol.MetadataName.Contains("`"))
                                    {
                                        addClassOnLookupTable =
                                            !GeneratorHelper.HasWinRTExposedTypeAttribute(instantiatedTypeSymbol) &&
                                            // If the type is defined in the same assembly as what the source generator is running on,
                                            // we let the WinRTExposedType attribute generator handle it. The only scenario the generator
                                            // doesn't handle which we handle here is if it is a generic type implementing generic WinRT interfaces.
                                            (!SymbolEqualityComparer.Default.Equals(instantiatedTypeSymbol.ContainingAssembly, context.SemanticModel.Compilation.Assembly) ||
                                              GeneratorHelper.HasNonInstantiatedWinRTGeneric(instantiatedTypeSymbol.OriginalDefinition, typeMapper)) &&
                                            // Make sure the type we are passing is being boxed or cast to another interface.
                                            !SymbolEqualityComparer.Default.Equals(instantiatedTypeSymbol, convertedToTypeSymbol);
                                    }
                                    else if (!GeneratorHelper.IsWinRTType(instantiatedTypeSymbol, winrtTypeAttribute, typeMapper, isComponentProject, context.Compilation.Assembly))
                                    {
                                        addClassOnLookupTable =
                                            !GeneratorHelper.HasWinRTExposedTypeAttribute(instantiatedTypeSymbol) &&
                                            // If the type is defined in the same assembly as what the source generator is running on,
                                            // we let the WinRTExposedType attribute generator handle it.
                                            !SymbolEqualityComparer.Default.Equals(instantiatedTypeSymbol.ContainingAssembly, context.SemanticModel.Compilation.Assembly) &&
                                            // Make sure the type we are passing is being boxed or cast to another interface.
                                            !SymbolEqualityComparer.Default.Equals(instantiatedTypeSymbol, convertedToTypeSymbol);
                                    }

                                    if (addClassOnLookupTable)
                                    {
                                        // We loop through all the interfaces rather than stopping at the first WinRT interface
                                        // to determine whether it is also custom mapped WinRT interfaces or not.
                                        implementsOnlyCustomMappedInterface = true;
                                        bool implementsWinRTInterface = false;
                                        foreach (var iface in instantiatedTypeSymbol.AllInterfaces)
                                        {
                                            if (GeneratorHelper.IsWinRTType(iface, winrtTypeAttribute, typeMapper, isComponentProject, context.Compilation.Assembly))
                                            {
                                                implementsWinRTInterface = true;
                                                implementsOnlyCustomMappedInterface &= GeneratorHelper.IsCustomMappedType(iface, typeMapper);
                                            }
                                        }
                                        return implementsWinRTInterface;
                                    }
                                }
                            }

                            implementsOnlyCustomMappedInterface = false;
                            return false;
                        }
                    }, SyntaxKind.InvocationExpression, SyntaxKind.VariableDeclaration, SyntaxKind.PropertyDeclaration, SyntaxKind.ReturnStatement, SyntaxKind.SimpleAssignmentExpression);

                    void ReportEnableUnsafeDiagnostic(Action<Diagnostic> reportDiagnostic, ISymbol symbol, Location location, bool implementsWinRTInterfaces)
                    {
                        // Based on the warning level, emit as a warning or as an info.
                        var diagnosticDescriptor = (csWinRTAotWarningLevel >= 2 || (csWinRTAotWarningLevel == 1 && implementsWinRTInterfaces)) ?
                            WinRTRules.ClassEnableUnsafeWarning : WinRTRules.ClassEnableUnsafeInfo;
                        reportDiagnostic(Diagnostic.Create(diagnosticDescriptor, location, symbol));
                    }
                }

                if (!allowUnsafe && isCsWinRTCcwLookupTableGeneratorEnabled && !isCsWinRTAotOptimizerAutoMode)
                {
                    context.RegisterCompilationEndAction(context =>
                    {
                        // If any of generated code for the type in the attribute requires unsafe code, report the first one.
                        foreach (AttributeData attributeData in context.Compilation.Assembly.GetAttributes())
                        {
                            if (SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass, generatedWinRTExposedExternalTypeAttribute))
                            {
                                if (attributeData.ConstructorArguments is [{ Kind: TypedConstantKind.Type, Value: ITypeSymbol vtableType }])
                                {
                                    if (vtableType is IArrayTypeSymbol)
                                    {
                                        context.ReportDiagnostic(Diagnostic.Create(WinRTRules.ClassEnableUnsafeWarning, null, vtableType));
                                        return;
                                    }
                                    else if (vtableType.TypeKind == TypeKind.Delegate &&
                                             vtableType.MetadataName.Contains("`") &&
                                             GeneratorHelper.IsWinRTType(vtableType, winrtTypeAttribute, typeMapper, isComponentProject, context.Compilation.Assembly))
                                    {
                                        context.ReportDiagnostic(Diagnostic.Create(WinRTRules.ClassEnableUnsafeWarning, null, vtableType));
                                        return;
                                    }
                                    else if (vtableType.TypeKind == TypeKind.Class)
                                    {
                                        foreach (var iface in vtableType.AllInterfaces)
                                        {
                                            if (iface.IsGenericType &&
                                                (GeneratorHelper.IsWinRTType(iface, winrtTypeAttribute, typeMapper, isComponentProject, context.Compilation.Assembly) ||
                                                 GeneratorHelper.IsWinRTType(iface.OriginalDefinition, winrtTypeAttribute, typeMapper, isComponentProject, context.Compilation.Assembly)))
                                            {
                                                context.ReportDiagnostic(Diagnostic.Create(WinRTRules.ClassEnableUnsafeWarning, null, vtableType));
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    });
                }
            });
        }
    }

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(WinRTAotCodeFixer)), Shared]
    public sealed class WinRTAotCodeFixer : CodeFixProvider
    {
        private const string title = "Make type partial";

        private static ImmutableArray<string> _fixableDiagnosticIds = ImmutableArray.Create(WinRTRules.ClassNotAotCompatibleWarning.Id);

        public override ImmutableArray<string> FixableDiagnosticIds => _fixableDiagnosticIds;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root is null)
                return;

            var node = root.FindNode(context.Span);
            if (node is null)
                return;

            var declaration = node.FirstAncestorOrSelf<ClassDeclarationSyntax>();
            if (declaration is null)
                return;

            context.RegisterCodeFix(
                CodeAction.Create(
                    title,
                    ct => MakeTypePartial(context.Document, declaration, ct),
                    nameof(WinRTAotCodeFixer)),
                context.Diagnostics);
        }

        private static async Task<Document> MakeTypePartial(Document document, ClassDeclarationSyntax @class, CancellationToken token)
        {
            var oldRoot = await document.GetSyntaxRootAsync(token).ConfigureAwait(false);
            if (oldRoot is null)
                return document;

            var newRoot = oldRoot.ReplaceNodes(@class.AncestorsAndSelf().OfType<TypeDeclarationSyntax>(),
                (_, typeDeclaration) =>
                {
                    if (!typeDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword))
                    {
                        return typeDeclaration.AddModifiers(SyntaxFactory.Token(SyntaxKind.PartialKeyword));
                    }

                    return typeDeclaration;
                });

            return document.WithSyntaxRoot(newRoot);
        }

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;
    }

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(WinRTAotEnableUnsafeCodeFixer)), Shared]
    public sealed class WinRTAotEnableUnsafeCodeFixer : CodeFixProvider
    {
        private const string title = "Update project with EnableUnsafeBlocks set to true";

        private static ImmutableArray<string> _fixableDiagnosticIds = ImmutableArray.Create(WinRTRules.ClassEnableUnsafeWarning.Id);

        public override ImmutableArray<string> FixableDiagnosticIds => _fixableDiagnosticIds;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            return Task.Run(() => RegisterCodeFixes(context));
        }

        private static void RegisterCodeFixes(CodeFixContext context)
        {
            if (context.Document.Project.CompilationOptions is not CSharpCompilationOptions)
                return;

            context.RegisterCodeFix(
                CodeAction.Create(
                    title,
                    ct => EnableUnsafe(context.Document.Project.Solution, context.Document.Project, ct),
                    nameof(WinRTAotEnableUnsafeCodeFixer)),
                context.Diagnostics);
        }

        private static Task<Solution> EnableUnsafe(Solution solution, Project project, CancellationToken token)
        {
            return Task<Solution>.FromResult(
                solution.WithProjectCompilationOptions(project.Id, ((CSharpCompilationOptions)project.CompilationOptions).WithAllowUnsafe(true)));
        }

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;
    }
}
