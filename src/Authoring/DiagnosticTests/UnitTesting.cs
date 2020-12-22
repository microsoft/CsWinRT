using NUnit.Framework;
using Microsoft.CodeAnalysis;
using WinRT.SourceGenerator;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.Linq;

namespace DiagnosticTests
{
    [TestFixture]
    public partial class UnitTesting
    {
        /* UnitTests require the "IsCsWinRTComponent" check in Generator.cs to be commented out, 
            until we can pass AnalyzerConfigOptions in our TestHelpers.cs file
           ---
           Add unit tests by creating a source code like this:
           private const string MyNewTest = @"namespace Test { ... }";
           
           And have a DiagnosticDescriptor for the one to check for, they live in WinRT.SourceGenerator.DiagnosticRules
           
           Then go to the ValidCases/InvalidCases property here and add an entry for it
        */

        /// <summary>
        /// CheckNoDiagnostic asserts that no diagnostics are raised on the 
        /// compilation produced from the cswinrt source generator based on the given source code /// </summary>
        /// <param name="source"></param>
        [Test, TestCaseSource(nameof(ValidCases))] 
        public void CheckNoDiagnostic(string source)
        { 
            Compilation compilation = CreateCompilation(source);
            RunGenerators(compilation, out var diagnosticsFound,  new Generator.SourceGenerator());
            var WinRTDiagnostics = diagnosticsFound.Where(diag => diag.Id.StartsWith("WME"));
            Assert.That(!WinRTDiagnostics.Any());
        }

        /// <summary>
        /// CodeHasDiagnostic takes some source code (string) and a Diagnostic descriptor, 
        /// it checks that a diagnostic with the same description was raised by the source generator 
        /// </summary> 
        [Test, TestCaseSource(nameof(InvalidCases))]
        public void CodeHasDiagnostic(string testCode, DiagnosticDescriptor rule)
        { 
            Compilation compilation = CreateCompilation(testCode);
            RunGenerators(compilation, out var diagnosticsFound,  new Generator.SourceGenerator());
            HashSet<DiagnosticDescriptor> diagDescsFound = MakeDiagnosticSet(diagnosticsFound);
            Assert.That(diagDescsFound.Contains(rule));
        }

        #region InvalidTests
        private static IEnumerable<TestCaseData> InvalidCases
        {
            get 
            {
                // namespace tests
                yield return new TestCaseData(NamespacesDifferByCase, WinRTRules.NamespacesDifferByCase).SetName("Namespace names only differ by case");
                yield return new TestCaseData(DisjointNamespaces, WinRTRules.DisjointNamespaceRule).SetName("Namespace isn't accessible without Test prefix, doesn't use type");
                yield return new TestCaseData(DisjointNamespaces2, WinRTRules.DisjointNamespaceRule).SetName("Namespace using type from inaccessible namespace");
                yield return new TestCaseData(NoPublicTypes, WinRTRules.NoPublicTypesRule).SetName("Component has no public types");
                // the below tests passes, you just have to change the assemblyName to Test.A instead of Test when making the WinRTScanner
                // yield return new TestCaseData(NamespaceDifferByDot, WinRTRules.DisjointNamespaceRule).SetName("Namespace Test.A and Test.B");
                // yield return new TestCaseData(NamespaceDifferByDot2, WinRTRules.DisjointNamespaceRule).SetName("Namespace Test.A and Test");

                // Unsealed classes, generic class/interfaces, invalid inheritance (System.Exception)
                yield return new TestCaseData(UnsealedClass, WinRTRules.UnsealedClassRule).SetName("Unsealed class public field");
                yield return new TestCaseData(UnsealedClass2, WinRTRules.UnsealedClassRule).SetName("Unsealed class private field");
                yield return new TestCaseData(GenericClass, WinRTRules.GenericTypeRule).SetName("Class marked generic");
                yield return new TestCaseData(GenericInterface, WinRTRules.GenericTypeRule).SetName("Interface marked generic");
                yield return new TestCaseData(ClassInheritsException, WinRTRules.NonWinRTInterface).SetName("Class inherits System.Exception");
                
                // Enumerable<T>
                yield return new TestCaseData(InterfaceWithGenericEnumerableReturnType, WinRTRules.UnsupportedTypeRule).SetName("Enumerable<> method return type, interface");
                yield return new TestCaseData(InterfaceWithGenericEnumerableInput, WinRTRules.UnsupportedTypeRule).SetName("Enumerable<> method parameter, interface");
                yield return new TestCaseData(ClassWithGenericEnumerableReturnType, WinRTRules.UnsupportedTypeRule).SetName("Enumerable<> method return type, class");
                yield return new TestCaseData(ClassWithGenericEnumerableInput, WinRTRules.UnsupportedTypeRule).SetName("Enumerable<> method parameter, class");
                yield return new TestCaseData(IfaceWithGenEnumerableProp, WinRTRules.UnsupportedTypeRule).SetName("Enumerable<> property, interface");
                yield return new TestCaseData(ClassWithGenEnumerableProp, WinRTRules.UnsupportedTypeRule).SetName("Enumerable<> property, class");
                 
                // KeyValuePair
                yield return new TestCaseData(ClassWithGenericKVPairReturnType, WinRTRules.UnsupportedTypeRule).SetName("KeyValuePair<> method return type, class");
                yield return new TestCaseData(ClassWithGenericKVPairInput, WinRTRules.UnsupportedTypeRule).SetName("KeyValuePair<> method parameter, class");
                yield return new TestCaseData(ClassWithGenKVPairProp, WinRTRules.UnsupportedTypeRule).SetName("KeyValuePair<> property, class ");
                yield return new TestCaseData(IfaceWithGenKVPairProp, WinRTRules.UnsupportedTypeRule).SetName("KeyValuePair<> property, interface ");
                yield return new TestCaseData(InterfaceWithGenericKVPairReturnType, WinRTRules.UnsupportedTypeRule).SetName("KeyValuePair<> method return type, interface ");
                yield return new TestCaseData(InterfaceWithGenericKVPairInput, WinRTRules.UnsupportedTypeRule).SetName("KeyValuePair<> method parameter, interface ");

                // readonlydict<T,S>
                yield return new TestCaseData(ClassWithGenericRODictReturnType, WinRTRules.UnsupportedTypeRule).SetName("ReadOnlyDictionary<> method return type, class");
                yield return new TestCaseData(InterfaceWithGenericRODictReturnType, WinRTRules.UnsupportedTypeRule).SetName("ReadOnlyDictionary<> method return type, interface");
                yield return new TestCaseData(InterfaceWithGenericRODictInput, WinRTRules.UnsupportedTypeRule).SetName("ReadOnlyDictionary<> method parameter, interface");
                yield return new TestCaseData(ClassWithGenericRODictInput, WinRTRules.UnsupportedTypeRule).SetName("ReadOnlyDictionary<> method parameter, class");
                yield return new TestCaseData(IfaceWithGenRODictProp, WinRTRules.UnsupportedTypeRule).SetName("ReadOnlyDictionary<> method property, interface");
                yield return new TestCaseData(ClassWithGenRODictProp, WinRTRules.UnsupportedTypeRule).SetName("ReadOnlyDictionary<>  method property, class");
                
                // dict<T,S>
                yield return new TestCaseData(IfaceWithGenDictProp, WinRTRules.UnsupportedTypeRule).SetName("Dictionary<> property, interface ");
                yield return new TestCaseData(ClassWithGenDictProp, WinRTRules.UnsupportedTypeRule).SetName("Dictionary<> property, class ");
                yield return new TestCaseData(ClassWithGenericDictInput, WinRTRules.UnsupportedTypeRule).SetName("Dictionary<> method parameter, class");
                yield return new TestCaseData(InterfaceWithGenericDictInput, WinRTRules.UnsupportedTypeRule).SetName("Dictionary<> method parameter, interface ");
                yield return new TestCaseData(ClassWithGenericDictReturnType, WinRTRules.UnsupportedTypeRule).SetName("Dictionary<> method return type, class ");
                yield return new TestCaseData(InterfaceWithGenericDictReturnType, WinRTRules.UnsupportedTypeRule).SetName("Dictionary<> method return type, interface ");
                
                // list<T> 
                yield return new TestCaseData(InterfaceWithGenericListReturnType, WinRTRules.UnsupportedTypeRule).SetName("List<> method return type, interface");
                yield return new TestCaseData(ClassWithGenericListReturnType, WinRTRules.UnsupportedTypeRule).SetName("List<> method return type, class");
                yield return new TestCaseData(InterfaceWithGenericListInput, WinRTRules.UnsupportedTypeRule).SetName("List<> method parameter, interface");
                yield return new TestCaseData(ClassWithGenericListInput, WinRTRules.UnsupportedTypeRule).SetName("List<> method parameter, class");
                yield return new TestCaseData(IfaceWithGenListProp, WinRTRules.UnsupportedTypeRule).SetName("List<> property, interface");
                yield return new TestCaseData(ClassWithGenListProp, WinRTRules.UnsupportedTypeRule).SetName("List<> property, class");
                
                // multi-dimensional array tests
                yield return new TestCaseData(MultiDim_2DProp, WinRTRules.MultiDimensionalArrayRule).SetName("Array [,] 2D Property");
                yield return new TestCaseData(MultiDim_3DProp, WinRTRules.MultiDimensionalArrayRule).SetName("Array [,,] 3D Property");
                yield return new TestCaseData(MultiDim_3DProp_Whitespace, WinRTRules.MultiDimensionalArrayRule).SetName("Array [,,] 3D Property With whitespace");
                yield return new TestCaseData(MultiDim_2D_PublicClassPublicMethod1, WinRTRules.MultiDimensionalArrayRule).SetName("Array [,] return type, class method signature");
                yield return new TestCaseData(MultiDim_2D_PublicClassPublicMethod2, WinRTRules.MultiDimensionalArrayRule).SetName("Array [,] return and first input, class method signature");
                yield return new TestCaseData(MultiDim_2D_PublicClassPublicMethod3, WinRTRules.MultiDimensionalArrayRule).SetName("Array [,] return and second input, class method signature");
                yield return new TestCaseData(MultiDim_2D_PublicClassPublicMethod4, WinRTRules.MultiDimensionalArrayRule).SetName("Array [,] second input, class method signature");
                yield return new TestCaseData(MultiDim_2D_PublicClassPublicMethod5, WinRTRules.MultiDimensionalArrayRule).SetName("Array [,] second of three inputs, class method signature");
                yield return new TestCaseData(MultiDim_2D_PublicClassPublicMethod6, WinRTRules.MultiDimensionalArrayRule).SetName("Array [,] return and second of three inputs, class method signature");
                yield return new TestCaseData(MultiDim_3D_PublicClassPublicMethod1, WinRTRules.MultiDimensionalArrayRule).SetName("Array [,,] return type, class method signature");
                yield return new TestCaseData(MultiDim_3D_PublicClassPublicMethod2, WinRTRules.MultiDimensionalArrayRule).SetName("Array [,,] return and first input, class method signature");
                yield return new TestCaseData(MultiDim_3D_PublicClassPublicMethod3, WinRTRules.MultiDimensionalArrayRule).SetName("Array [,,] return and second input, class method signature");
                yield return new TestCaseData(MultiDim_3D_PublicClassPublicMethod4, WinRTRules.MultiDimensionalArrayRule).SetName("Array [,,] second input, class method signature");
                yield return new TestCaseData(MultiDim_3D_PublicClassPublicMethod5, WinRTRules.MultiDimensionalArrayRule).SetName("Array [,,] second of three inputs, class method signature");
                yield return new TestCaseData(MultiDim_3D_PublicClassPublicMethod6, WinRTRules.MultiDimensionalArrayRule).SetName("Array [,,] return and second of three inputs, class method signature");
                yield return new TestCaseData(MultiDim_2D_Interface1, WinRTRules.MultiDimensionalArrayRule).SetName("Array [,] return type, interface method signature");
                yield return new TestCaseData(MultiDim_2D_Interface2, WinRTRules.MultiDimensionalArrayRule).SetName("Array [,] return and first input, interface method signature");
                yield return new TestCaseData(MultiDim_2D_Interface3, WinRTRules.MultiDimensionalArrayRule).SetName("Array [,] return and second input, interface method signature");
                yield return new TestCaseData(MultiDim_2D_Interface4, WinRTRules.MultiDimensionalArrayRule).SetName("Array [,] second input, interface method signature");
                yield return new TestCaseData(MultiDim_2D_Interface5, WinRTRules.MultiDimensionalArrayRule).SetName("Array [,] second of three inputs, interface method signature");
                yield return new TestCaseData(MultiDim_2D_Interface6, WinRTRules.MultiDimensionalArrayRule).SetName("Array [,] return and second of three inputs, interface method signature");
                yield return new TestCaseData(MultiDim_3D_Interface1, WinRTRules.MultiDimensionalArrayRule).SetName("Array [,,] return type, interface method signature");
                yield return new TestCaseData(MultiDim_3D_Interface2, WinRTRules.MultiDimensionalArrayRule).SetName("Array [,,] return and first input, interface method signature");
                yield return new TestCaseData(MultiDim_3D_Interface3, WinRTRules.MultiDimensionalArrayRule).SetName("Array [,,] return and second input, interface method signature");
                yield return new TestCaseData(MultiDim_3D_Interface4, WinRTRules.MultiDimensionalArrayRule).SetName("Array [,,] second input, interface method signature");
                yield return new TestCaseData(MultiDim_3D_Interface5, WinRTRules.MultiDimensionalArrayRule).SetName("Array [,,] second of three inputs, interface method signature");
                yield return new TestCaseData(MultiDim_3D_Interface6, WinRTRules.MultiDimensionalArrayRule).SetName("Array [,,] return and second of three inputs, interface method signature");
                yield return new TestCaseData(SubNamespaceInterface_D2Method1, WinRTRules.MultiDimensionalArrayRule) .SetName("Subnamespace: Array [,] return type, interface method signature");
                yield return new TestCaseData(SubNamespaceInterface_D2Method2, WinRTRules.MultiDimensionalArrayRule) .SetName("Subnamespace: Array [,] return and first input, interface method signature");
                yield return new TestCaseData(SubNamespaceInterface_D2Method3, WinRTRules.MultiDimensionalArrayRule) .SetName("Subnamespace: Array [,] return and second input, interface method signature");
                yield return new TestCaseData(SubNamespaceInterface_D2Method4, WinRTRules.MultiDimensionalArrayRule) .SetName("Subnamespace: Array [,] second input, interface method signature");
                yield return new TestCaseData(SubNamespaceInterface_D2Method5, WinRTRules.MultiDimensionalArrayRule) .SetName("Subnamespace: Array [,] second of three inputs, interface method signature");
                yield return new TestCaseData(SubNamespaceInterface_D2Method6, WinRTRules.MultiDimensionalArrayRule) .SetName("Subnamespace: Array [,] return and second of three inputs, interface method signature");
                yield return new TestCaseData(SubNamespaceInterface_D3Method1, WinRTRules.MultiDimensionalArrayRule) .SetName("Subnamespace: Array [,,] return type, interface method signature");
                yield return new TestCaseData(SubNamespaceInterface_D3Method2, WinRTRules.MultiDimensionalArrayRule) .SetName("Subnamespace: Array [,,] return and first input, interface method signature");
                yield return new TestCaseData(SubNamespaceInterface_D3Method3, WinRTRules.MultiDimensionalArrayRule) .SetName("Subnamespace: Array [,,] return and second input, interface method signature");
                yield return new TestCaseData(SubNamespaceInterface_D3Method4, WinRTRules.MultiDimensionalArrayRule) .SetName("Subnamespace: Array [,,] second input, interface method signature");
                yield return new TestCaseData(SubNamespaceInterface_D3Method5, WinRTRules.MultiDimensionalArrayRule) .SetName("Subnamespace: Array [,,] second of three inputs, interface method signature");
                yield return new TestCaseData(SubNamespaceInterface_D3Method6, WinRTRules.MultiDimensionalArrayRule) .SetName("Subnamespace: Array [,,] return and second of three inputs, interface method signature");

                #region JaggedArray
                // jagged array tests 
                yield return new TestCaseData(Jagged2D_Property2, WinRTRules.ArraySignature_JaggedArrayRule).SetName("Jagged 2D Array Property 2");
                yield return new TestCaseData(Jagged3D_Property1, WinRTRules.ArraySignature_JaggedArrayRule).SetName("Jagged 3D Array Property 1");
                yield return new TestCaseData(Jagged2D_ClassMethod1, WinRTRules.ArraySignature_JaggedArrayRule).SetName("Array [][] return type, class method signature");
                yield return new TestCaseData(Jagged2D_ClassMethod2, WinRTRules.ArraySignature_JaggedArrayRule).SetName("Array [][] return and first input, class method signature");
                yield return new TestCaseData(Jagged2D_ClassMethod3, WinRTRules.ArraySignature_JaggedArrayRule).SetName("Array [][] return and second input, class method signature");
                yield return new TestCaseData(Jagged2D_ClassMethod4, WinRTRules.ArraySignature_JaggedArrayRule).SetName("Array [][] second input, class method signature");
                yield return new TestCaseData(Jagged2D_ClassMethod5, WinRTRules.ArraySignature_JaggedArrayRule).SetName("Array [][] second of three inputs, class method signature");
                yield return new TestCaseData(Jagged2D_ClassMethod6, WinRTRules.ArraySignature_JaggedArrayRule).SetName("Array [][] return and second of three inputs, class method signature");
                yield return new TestCaseData(Jagged3D_ClassMethod1, WinRTRules.ArraySignature_JaggedArrayRule).SetName("Array [][][] return type, class method signature");
                yield return new TestCaseData(Jagged3D_ClassMethod2, WinRTRules.ArraySignature_JaggedArrayRule).SetName("Array [][][] return and first input, class method signature");
                yield return new TestCaseData(Jagged3D_ClassMethod3, WinRTRules.ArraySignature_JaggedArrayRule).SetName("Array [][][] return and second input, class method signature");
                yield return new TestCaseData(Jagged3D_ClassMethod4, WinRTRules.ArraySignature_JaggedArrayRule).SetName("Array [][][] second input, class method signature");
                yield return new TestCaseData(Jagged3D_ClassMethod5, WinRTRules.ArraySignature_JaggedArrayRule).SetName("Array [][][] second of three inputs, class method signature");
                yield return new TestCaseData(Jagged3D_ClassMethod6, WinRTRules.ArraySignature_JaggedArrayRule).SetName("Array [][][] return and second of three inputs, class method signature");
                yield return new TestCaseData(Jagged2D_InterfaceMethod1, WinRTRules.ArraySignature_JaggedArrayRule).SetName("Array [][] return type, interface method signature");
                yield return new TestCaseData(Jagged2D_InterfaceMethod2, WinRTRules.ArraySignature_JaggedArrayRule).SetName("Array [][] return and first input, interface method signature");
                yield return new TestCaseData(Jagged2D_InterfaceMethod3, WinRTRules.ArraySignature_JaggedArrayRule).SetName("Array [][] return and second input, interface method signature");
                yield return new TestCaseData(Jagged2D_InterfaceMethod4, WinRTRules.ArraySignature_JaggedArrayRule).SetName("Array [][] second input, interface method signature");
                yield return new TestCaseData(Jagged2D_InterfaceMethod5, WinRTRules.ArraySignature_JaggedArrayRule).SetName("Array [][] second of three inputs, interface method signature");
                yield return new TestCaseData(Jagged2D_InterfaceMethod6, WinRTRules.ArraySignature_JaggedArrayRule).SetName("Array [][] return and second of three inputs, interface method signature");
                yield return new TestCaseData(Jagged3D_InterfaceMethod1, WinRTRules.ArraySignature_JaggedArrayRule).SetName("Array [][][] return type, interface method signature");
                yield return new TestCaseData(Jagged3D_InterfaceMethod2, WinRTRules.ArraySignature_JaggedArrayRule).SetName("Array [][][] return and first input, interface method signature");
                yield return new TestCaseData(Jagged3D_InterfaceMethod3, WinRTRules.ArraySignature_JaggedArrayRule).SetName("Array [][][] return and second input, interface method signature");
                yield return new TestCaseData(Jagged3D_InterfaceMethod4, WinRTRules.ArraySignature_JaggedArrayRule).SetName("Array [][][] second input, interface method signature");
                yield return new TestCaseData(Jagged3D_InterfaceMethod5, WinRTRules.ArraySignature_JaggedArrayRule).SetName("Array [][][] second of three inputs, interface method signature");
                yield return new TestCaseData(Jagged3D_InterfaceMethod6, WinRTRules.ArraySignature_JaggedArrayRule).SetName("Array [][][] return and second of three inputs, interface method signature");
                yield return new TestCaseData(SubNamespace_Jagged2DInterface1, WinRTRules.ArraySignature_JaggedArrayRule).SetName("Subnamespace: Array [][] return type, interface method signature");
                yield return new TestCaseData(SubNamespace_Jagged2DInterface2, WinRTRules.ArraySignature_JaggedArrayRule).SetName("Subnamespace: Array [][] return and first input, interface method signature");
                yield return new TestCaseData(SubNamespace_Jagged2DInterface3, WinRTRules.ArraySignature_JaggedArrayRule).SetName("Subnamespace: Array [][] return and second input, interface method signature");
                yield return new TestCaseData(SubNamespace_Jagged2DInterface4, WinRTRules.ArraySignature_JaggedArrayRule).SetName("Subnamespace: Array [][] second input, interface method signature");
                yield return new TestCaseData(SubNamespace_Jagged2DInterface5, WinRTRules.ArraySignature_JaggedArrayRule).SetName("Subnamespace: Array [][] second of three inputs, interface method signature");
                yield return new TestCaseData(SubNamespace_Jagged2DInterface6, WinRTRules.ArraySignature_JaggedArrayRule).SetName("Subnamespace: Array [][] return and second of three inputs, interface method signature");
                yield return new TestCaseData(SubNamespace_Jagged3DInterface1, WinRTRules.ArraySignature_JaggedArrayRule).SetName("Subnamespace: Array [][][] return type, interface method signature");
                yield return new TestCaseData(SubNamespace_Jagged3DInterface2, WinRTRules.ArraySignature_JaggedArrayRule).SetName("Subnamespace: Array [][][] return and first input, interface method signature");
                yield return new TestCaseData(SubNamespace_Jagged3DInterface3, WinRTRules.ArraySignature_JaggedArrayRule).SetName("Subnamespace: Array [][][] return and second input, interface method signature");
                yield return new TestCaseData(SubNamespace_Jagged3DInterface4, WinRTRules.ArraySignature_JaggedArrayRule).SetName("Subnamespace: Array [][][] second input, interface method signature");
                yield return new TestCaseData(SubNamespace_Jagged3DInterface5, WinRTRules.ArraySignature_JaggedArrayRule).SetName("Subnamespace: Array [][][] second of three inputs, interface method signature");
                yield return new TestCaseData(SubNamespace_Jagged3DInterface6, WinRTRules.ArraySignature_JaggedArrayRule).SetName("Subnamespace: Array [][][] return and second of three inputs, interface method signature");
                #endregion

                #region overload_attribute_tests
                yield return new TestCaseData(InterfaceWithOverloadNoAttribute, WinRTRules.NeedDefaultOverloadAttribute).SetName("Need DefaultOverload - Interface");
                yield return new TestCaseData(InterfaceWithOverloadAttributeTwice, WinRTRules.MultipleDefaultOverloadAttribute).SetName("Interface has too many default overload attribute");
                yield return new TestCaseData(TwoOverloads_NoAttribute_NamesHaveNumber, WinRTRules.NeedDefaultOverloadAttribute) .SetName("Need DefaultOverload Attribute - Name has number");
                yield return new TestCaseData(TwoOverloads_NoAttribute, WinRTRules.NeedDefaultOverloadAttribute) .SetName("Need DefaultOverload Attribute"); 
                yield return new TestCaseData(TwoOverloads_TwoAttribute_OneInList_Unqualified, WinRTRules.MultipleDefaultOverloadAttribute) .SetName("Multiple DefaultOverload attributes, after attribute - unqualified");
                yield return new TestCaseData(TwoOverloads_TwoAttribute_BothInList_Unqualified, WinRTRules.MultipleDefaultOverloadAttribute) .SetName("Multiple DefaultOverload attributes - Unqualified");
                yield return new TestCaseData(TwoOverloads_TwoAttribute_TwoLists_Unqualified, WinRTRules.MultipleDefaultOverloadAttribute) .SetName("Multiple DefaultOverload attributes, two lists - unqualified");
                yield return new TestCaseData(TwoOverloads_TwoAttribute_OneInSeparateList_OneNot_Unqualified, WinRTRules.MultipleDefaultOverloadAttribute) .SetName("Multiple DefaultOverload attributes, one in separate list, one not - Unqualified");
                yield return new TestCaseData(TwoOverloads_TwoAttribute_BothInSeparateList_Unqualified, WinRTRules.MultipleDefaultOverloadAttribute) .SetName("Multiple DefaultOverload attributes, both in separate list - unqualified");
                yield return new TestCaseData(TwoOverloads_TwoAttribute_Unqualified, WinRTRules.MultipleDefaultOverloadAttribute) .SetName("Multiple DefaultOverload attributes - unqualified");
                yield return new TestCaseData(ThreeOverloads_TwoAttributes_Unqualified, WinRTRules.MultipleDefaultOverloadAttribute) .SetName("Multiple DefaultOverload attributes, three overlodas - unqualified");

                yield return new TestCaseData(TwoOverloads_NoAttribute_NamesHaveNumber, WinRTRules.NeedDefaultOverloadAttribute) .SetName("Need DefaultOverload Attribute - Name has number");
                yield return new TestCaseData(TwoOverloads_NoAttribute, WinRTRules.NeedDefaultOverloadAttribute) .SetName("Need DefaultOverload Attribute");
                yield return new TestCaseData(TwoOverloads_NoAttribute_OneIrrevAttr, WinRTRules.NeedDefaultOverloadAttribute) .SetName("Need DefaultOverload, uses irrelevant attribute");
                yield return new TestCaseData(TwoOverloads_TwoAttribute_OneInList, WinRTRules.MultipleDefaultOverloadAttribute) .SetName("Multiple DefaultOverload attributes, after attribute");
                yield return new TestCaseData(TwoOverloads_TwoAttribute_BothInList, WinRTRules.MultipleDefaultOverloadAttribute) .SetName("Multiple DefaultOverload attributes, same list");
                yield return new TestCaseData(TwoOverloads_TwoAttribute_TwoLists, WinRTRules.MultipleDefaultOverloadAttribute) .SetName("Multiple DefaultOverload attributes, two lists");
                yield return new TestCaseData(TwoOverloads_TwoAttribute_OneInSeparateList_OneNot, WinRTRules.MultipleDefaultOverloadAttribute) .SetName("Multiple DefaultOverload attributes, one in separate list, one not");
                yield return new TestCaseData(TwoOverloads_TwoAttribute_BothInSeparateList, WinRTRules.MultipleDefaultOverloadAttribute) .SetName("Multiple DefaultOverload attributes, both in separate list");
                yield return new TestCaseData(TwoOverloads_TwoAttribute, WinRTRules.MultipleDefaultOverloadAttribute) .SetName("Multiple DefaultOverload attributes");
                yield return new TestCaseData(ThreeOverloads_TwoAttributes, WinRTRules.MultipleDefaultOverloadAttribute) .SetName("Multiple DefaultOverload attributes, three overlodas");

                #endregion

                // multiple class constructors of same arity
                yield return new TestCaseData(ConstructorsOfSameArity, WinRTRules.ClassConstructorRule).SetName("Misc. Multiple constructors of same arity");

                #region InvalidInterfaceInheritance
                // implementing async interface
                yield return new TestCaseData(ClassImplementsIAsyncActionWithProgress, WinRTRules.NonWinRTInterface).SetName("InvalidInterface. Class implements IAsyncActionWithProgress");
                yield return new TestCaseData(InterfaceImplementsIAsyncActionWithProgress, WinRTRules.NonWinRTInterface).SetName("InvalidInterface. Interface Implements IAsyncActionWithProgress");
                yield return new TestCaseData(InterfaceImplementsIAsyncActionWithProgress2, WinRTRules.NonWinRTInterface).SetName("InvalidInterface. Interface Implements IAsyncActionWithProgress in full");

                yield return new TestCaseData(ClassImplementsIAsyncAction, WinRTRules.NonWinRTInterface).SetName("InvalidInterface. Class implements IAsyncAction");
                yield return new TestCaseData(InterfaceImplementsIAsyncAction, WinRTRules.NonWinRTInterface).SetName("InvalidInterface. Interface Implements IAsyncAction");
                yield return new TestCaseData(InterfaceImplementsIAsyncAction2, WinRTRules.NonWinRTInterface).SetName("InvalidInterface. Interface Implements IAsyncAction in full");

                yield return new TestCaseData(ClassImplementsIAsyncOperation, WinRTRules.NonWinRTInterface).SetName("InvalidInterface. Class implements IAsyncOperation");
                yield return new TestCaseData(InterfaceImplementsIAsyncOperation, WinRTRules.NonWinRTInterface).SetName("InvalidInterface. Interface implements IAsyncOperation");
                yield return new TestCaseData(InterfaceImplementsIAsyncOperation2, WinRTRules.NonWinRTInterface).SetName("InvalidInterface. Interface Implements IAsyncOperation in full");
                
                yield return new TestCaseData(ClassImplementsIAsyncOperationWithProgress, WinRTRules.NonWinRTInterface).SetName("InvalidInterface. Class implements IAsyncOperationWithProgress");
                yield return new TestCaseData(InterfaceImplementsIAsyncOperationWithProgress, WinRTRules.NonWinRTInterface).SetName("InvalidInterface. Interface Implements IAsyncOperationWithProgress");
                yield return new TestCaseData(InterfaceImplementsIAsyncOperationWithProgress2, WinRTRules.NonWinRTInterface).SetName("InvalidInterface. Interface Implements IAsyncOperationWithProgress in full");

                #endregion

                #region InOutAttribute
                yield return new TestCaseData(TestArrayParamAttrUnary_4, WinRTRules.ArrayMarkedInOrOut).SetName("InOutAttribute. Array parameter marked [In]");
                yield return new TestCaseData(TestArrayParamAttrUnary_5, WinRTRules.ArrayMarkedInOrOut).SetName("InOutAttribute. Array parameter marked [Out]");
                yield return new TestCaseData(TestArrayParamAttrUnary_8, WinRTRules.NonArrayMarkedInOrOut).SetName("InOutAttribute. Non-array parameter marked [In] - unqualified");
                yield return new TestCaseData(TestArrayParamAttrUnary_9, WinRTRules.NonArrayMarkedInOrOut).SetName("InOutAttribute. Non-array parameter marked [Out]");
                yield return new TestCaseData(TestArrayParamAttrUnary_11, WinRTRules.NonArrayMarkedInOrOut).SetName("InOutAttribute. Non-array parameter marked [In]");
                yield return new TestCaseData(TestArrayParamAttrUnary_12, WinRTRules.NonArrayMarkedInOrOut).SetName("InOutAttribute. TestArrayParamAttrUnary_12");
                yield return new TestCaseData(TestArrayParamAttrUnary_13, WinRTRules.ArrayMarkedInOrOut).SetName("InOutAttribute. TestArrayParamAttrUnary_13");
                yield return new TestCaseData(TestArrayParamAttrUnary_14, WinRTRules.ArrayMarkedInOrOut).SetName("InOutAttribute. TestArrayParamAttrUnary_14");
                yield return new TestCaseData(TestArrayParamAttrBinary_4, WinRTRules.ArrayMarkedInOrOut).SetName("InOutAttribute. TestArrayParamAttrBinary_4");
                yield return new TestCaseData(TestArrayParamAttrBinary_5, WinRTRules.ArrayMarkedInOrOut).SetName("InOutAttribute. TestArrayParamAttrBinary_5");
                yield return new TestCaseData(TestArrayParamAttrBinary_8, WinRTRules.NonArrayMarkedInOrOut).SetName("InOutAttribute. TestArrayParamAttrBinary_8");
                yield return new TestCaseData(TestArrayParamAttrBinary_9, WinRTRules.NonArrayMarkedInOrOut).SetName("InOutAttribute. TestArrayParamAttrBinary_9");
                yield return new TestCaseData(TestArrayParamAttrBinary_14, WinRTRules.ArrayMarkedInOrOut).SetName("InOutAttribute. TestArrayParamAttrBinary_14");
                yield return new TestCaseData(TestArrayParamAttrBinary_15, WinRTRules.ArrayMarkedInOrOut).SetName("InOutAttribute. Array marked [In] - second parameter");
                yield return new TestCaseData(TestArrayParamAttrBinary_16, WinRTRules.ArrayMarkedInOrOut).SetName("InOutAttribute. Array marked [Out] - second parameter");
                yield return new TestCaseData(TestArrayParamAttrBinary_21, WinRTRules.NonArrayMarkedInOrOut).SetName("InOutAttribute. Non array marked [In], with marked array");
                yield return new TestCaseData(TestArrayParamAttrBinary_22, WinRTRules.NonArrayMarkedInOrOut).SetName("InOutAttribute. Non array marked [Out], with marked array, reverse");
                yield return new TestCaseData(TestArrayParamAttrBinary_23, WinRTRules.NonArrayMarkedInOrOut).SetName("InOutAttribute. Non array marked [Out], with marked array");
                #endregion

                #region ArrayAccessAttribute
                yield return new TestCaseData(TestArrayParamAttrUnary_1, WinRTRules.ArrayParamMarkedBoth).SetName("ArrayAttribute. Both marked, separate lists");
                yield return new TestCaseData(TestArrayParamAttrUnary_2, WinRTRules.ArrayParamMarkedBoth).SetName("ArrayAttribute. Both marked, same list");
                yield return new TestCaseData(TestArrayParamAttrUnary_3, WinRTRules.ArrayOutputParamMarkedRead).SetName("ArrayAttribute. ReadOnlyArray attribute on array out parameter");
                yield return new TestCaseData(TestArrayParamAttrUnary_6, WinRTRules.NonArrayMarked).SetName("ArrayAttribute. Non-array parameter marked ReadOnlyArray");
                yield return new TestCaseData(TestArrayParamAttrUnary_7, WinRTRules.NonArrayMarked).SetName("ArrayAttribute. Non-array parameter marked WriteOnlyArray");
                yield return new TestCaseData(TestArrayParamAttrUnary_10, WinRTRules.ArrayParamNotMarked).SetName("ArrayAttribute. Array parameter not marked ReadOnlyArray or WriteOnlyArray");
                yield return new TestCaseData(TestArrayParamAttrBinary_1, WinRTRules.ArrayParamMarkedBoth).SetName("ArrayAttribute. Both marked, second parameter, same list");
                yield return new TestCaseData(TestArrayParamAttrBinary_2, WinRTRules.ArrayParamMarkedBoth).SetName("ArrayAttribute. Both marked, second parameter, separate list");
                yield return new TestCaseData(TestArrayParamAttrBinary_3, WinRTRules.ArrayOutputParamMarkedRead).SetName("ArrayAttribute. Array `out` var marked ReadOnly");
                yield return new TestCaseData(TestArrayParamAttrBinary_6, WinRTRules.NonArrayMarked).SetName("ArrayAttribute. Non-array parameter marked ReadOnly, second argument");
                yield return new TestCaseData(TestArrayParamAttrBinary_7, WinRTRules.NonArrayMarked).SetName("ArrayAttribute. Non-array parameter marked WriteOnly, second argument");
                yield return new TestCaseData(TestArrayParamAttrBinary_10, WinRTRules.ArrayParamNotMarked).SetName("ArrayAttribute. Array not marked, second argument");
                yield return new TestCaseData(TestArrayParamAttrBinary_11, WinRTRules.ArrayParamMarkedBoth).SetName("ArrayAttribute. Marked both, second array");
                yield return new TestCaseData(TestArrayParamAttrBinary_12, WinRTRules.ArrayParamMarkedBoth).SetName("ArrayAttribute. Marked both, first array");
                yield return new TestCaseData(TestArrayParamAttrBinary_13, WinRTRules.ArrayOutputParamMarkedRead).SetName("ArrayAttribute. Marked out and read only, second argument");
                yield return new TestCaseData(TestArrayParamAttrBinary_17, WinRTRules.ArrayParamNotMarked).SetName("ArrayAttribute. Array not marked - second argument");
                yield return new TestCaseData(TestArrayParamAttrBinary_18, WinRTRules.NonArrayMarked).SetName("ArrayAttribute. Non array marked ReadOnlyArray");
                yield return new TestCaseData(TestArrayParamAttrBinary_19, WinRTRules.NonArrayMarked).SetName("ArrayAttribute. Non array marked WriteOnlyArray");
                yield return new TestCaseData(TestArrayParamAttrBinary_20, WinRTRules.NonArrayMarked).SetName("ArrayAttribute. Non array marked [In], with marked array, reverse");
                yield return new TestCaseData(TestArrayParamAttrBinary_24, WinRTRules.ArrayParamNotMarked).SetName("ArrayAttribute. Array missing attribute");
                #endregion

                // name clash with params (__retval)
                yield return new TestCaseData(DunderRetValParam, WinRTRules.ParameterNamedValueRule).SetName("Misc. Parameter Name Conflict (__retval)");
                // operator overloading
                yield return new TestCaseData(OperatorOverload_Class, WinRTRules.OperatorOverloadedRule).SetName("Misc. Overload of Operator");
                // ref param
                yield return new TestCaseData(RefParam_ClassMethod, WinRTRules.RefParameterFound).SetName("Misc. Class Method With Ref Param");
                yield return new TestCaseData(RefParam_InterfaceMethod, WinRTRules.RefParameterFound).SetName("Misc. Interface Method With Ref Param");

                #region struct_field_tests
                yield return new TestCaseData(EmptyStruct, WinRTRules.StructWithNoFieldsRule).SetName("Empty struct");
                yield return new TestCaseData(StructWithInterfaceField, WinRTRules.StructHasInvalidFieldRule).SetName("Struct with Interface field");
                yield return new TestCaseData(StructWithClassField, WinRTRules.StructHasInvalidFieldRule).SetName("Struct with Class Field");
                yield return new TestCaseData(StructWithClassField2, WinRTRules.StructHasInvalidFieldRule).SetName("Struct with Class Field2");
                yield return new TestCaseData(StructWithDelegateField, WinRTRules.StructHasInvalidFieldRule).SetName("Struct with Delegate Field");
                yield return new TestCaseData(StructWithIndexer, WinRTRules.StructHasInvalidFieldRule).SetName("Struct with Indexer Field");
                yield return new TestCaseData(StructWithMethods, WinRTRules.StructHasInvalidFieldRule).SetName("Struct with Method Field");
                yield return new TestCaseData(StructWithConst, WinRTRules.StructHasConstFieldRule).SetName("Struct with Const Field");
                yield return new TestCaseData(StructWithProperty, WinRTRules.StructHasInvalidFieldRule).SetName("Struct with Property Field");
                yield return new TestCaseData(StructWithPrivateField, WinRTRules.StructHasPrivateFieldRule).SetName("Struct with Private Field");
                yield return new TestCaseData(StructWithObjectField, WinRTRules.StructHasInvalidFieldRule).SetName("Struct with Object Field");
                yield return new TestCaseData(StructWithDynamicField, WinRTRules.StructHasInvalidFieldRule).SetName("Struct with Dynamic Field");
                yield return new TestCaseData(StructWithConstructor, WinRTRules.StructHasInvalidFieldRule).SetName("Struct with Constructor Field");
                yield return new TestCaseData(StructWithPrimitiveTypesMissingPublicKeyword, WinRTRules.StructHasPrivateFieldRule).SetName("Struct with missing public field");
                #endregion

                #region InvalidType
                // system.array tests
                yield return new TestCaseData(ArrayInstanceProperty1, WinRTRules.UnsupportedTypeRule).SetName("InvalidType. System Array property");
                yield return new TestCaseData(ArrayInstanceProperty2, WinRTRules.UnsupportedTypeRule).SetName("InvalidType. System Array property 2");
                yield return new TestCaseData(SystemArrayProperty5, WinRTRules.UnsupportedTypeRule).SetName("InvalidType. System Array property 3");
                yield return new TestCaseData(ArrayInstanceInterface1, WinRTRules.UnsupportedTypeRule).SetName("InvalidType. System Array interface method return type and input");
                yield return new TestCaseData(ArrayInstanceInterface2, WinRTRules.UnsupportedTypeRule).SetName("InvalidType. System Array interface method input");
                yield return new TestCaseData(ArrayInstanceInterface3, WinRTRules.UnsupportedTypeRule).SetName("InvalidType. System Array interface method return type");
                yield return new TestCaseData(SystemArrayJustReturn, WinRTRules.UnsupportedTypeRule).SetName("InvalidType. System Array class method return type ");
                yield return new TestCaseData(SystemArrayUnaryAndReturn, WinRTRules.UnsupportedTypeRule).SetName("InvalidType. System Array class method return and input");
                yield return new TestCaseData(SystemArraySecondArgClass, WinRTRules.UnsupportedTypeRule).SetName("InvalidType. System Array class method - Arg 2/2");
                yield return new TestCaseData(SystemArraySecondArg2Class, WinRTRules.UnsupportedTypeRule).SetName("InvalidType. System Array class method - Arg 2/3");
                yield return new TestCaseData(SystemArraySecondArgAndReturnTypeClass, WinRTRules.UnsupportedTypeRule).SetName("InvalidType. System Array class method second input and return type");
                yield return new TestCaseData(SystemArraySecondArgAndReturnTypeClass2, WinRTRules.UnsupportedTypeRule).SetName("InvalidType. System Array class method second input and return type 2");
                yield return new TestCaseData(SystemArraySecondArgAndReturnTypeInterface, WinRTRules.UnsupportedTypeRule).SetName("InvalidType. System Array second argument and  return type");
                yield return new TestCaseData(SystemArraySecondArgAndReturnTypeInterface2, WinRTRules.UnsupportedTypeRule).SetName("InvalidType. System Array Interface 2nd of 3 and return type");
                yield return new TestCaseData(SystemArraySecondArgInterface, WinRTRules.UnsupportedTypeRule).SetName("InvalidType. System Array Interface 2nd of 2 arguments");
                yield return new TestCaseData(SystemArraySecondArgInterface2, WinRTRules.UnsupportedTypeRule).SetName("InvalidType. System Array Interface 2nd of 3 arguments");
                yield return new TestCaseData(SystemArraySubNamespace_ReturnOnly, WinRTRules.UnsupportedTypeRule).SetName("InvalidType. System Array Subnamespace Interface return only");
                yield return new TestCaseData(SystemArraySubNamespace_ReturnAndInput1, WinRTRules.UnsupportedTypeRule).SetName("InvalidType. System Array Subnamespace Interface return and only input");
                yield return new TestCaseData(SystemArraySubNamespace_ReturnAndInput2of2, WinRTRules.UnsupportedTypeRule).SetName("InvalidType. System Array Subnamespace Interface return type and 2nd arg of 2");
                yield return new TestCaseData(SystemArraySubNamespace_ReturnAndInput2of3, WinRTRules.UnsupportedTypeRule).SetName("InvalidType. System Array Subnamespace Interface return type and 2nd arg of 3rd");
                yield return new TestCaseData(SystemArraySubNamespace_NotReturnAndInput2of2, WinRTRules.UnsupportedTypeRule).SetName("InvalidType. System Array Subnamespace 2nd arg of two");
                yield return new TestCaseData(SystemArraySubNamespace_NotReturnAndInput2of3, WinRTRules.UnsupportedTypeRule).SetName("InvalidType. System Array Subnamespace 2nd arg of third");
                #endregion
            }
        }

        #endregion

        #region ValidTests

        private static IEnumerable<TestCaseData> ValidCases
        {
            get
            {
                yield return new TestCaseData(Valid_NestedNamespace).SetName("Valid Nested namespaces are fine");
                yield return new TestCaseData(Valid_NestedNamespace2).SetName("Valid Twice nested namespaces are fine");
                yield return new TestCaseData(Valid_NestedNamespace3).SetName("Valid Test[dot]Component with an inner component");
                yield return new TestCaseData(Valid_NestedNamespace4).SetName("Valid Test with an inner component");
                yield return new TestCaseData(Valid_NamespacesDiffer).SetName("Valid Similar namespace but different name (not just case)");
                yield return new TestCaseData(Valid_NamespaceAndPrefixedNamespace).SetName("Valid Two top-level namespaces, one prefixed with the other");
                

                #region InvalidTypes_Signatures
                yield return new TestCaseData(Valid_ListUsage).SetName("Valid Internally uses List<>");
                yield return new TestCaseData(Valid_ListUsage2).SetName("Valid Internally uses List<> (qualified)"); 
                yield return new TestCaseData(Valid_ClassWithGenericDictReturnType_Private).SetName("Valid Dictionary<> Private Method - ReturnType");
                yield return new TestCaseData(Valid_ClassWithGenericDictInput_Private).SetName("Valid Dictionary<> Private Method - parameter");
                yield return new TestCaseData(Valid_ClassWithPrivateGenDictProp).SetName("Valid Dictionary<> Private Property Class");
                yield return new TestCaseData(Valid_IfaceWithPrivateGenDictProp).SetName("Valid Dictionary<> Private Property Interface");

                yield return new TestCaseData(Valid_ClassWithGenericRODictInput_Private).SetName("Valid ReadOnlyDictionary<> Private Method ReturnType");
                yield return new TestCaseData(Valid_ClassWithGenericRODictReturnType_Private).SetName("Valid ReadOnlyDictionary<> Private Method parameter");
                yield return new TestCaseData(Valid_ClassWithPrivateGenRODictProp).SetName("Valid ReadOnlyDictionary<> Private Property Class");
                yield return new TestCaseData(Valid_IfaceWithPrivateGenRODictProp).SetName("Valid ReadOnlyDictionary<> Private Property Interface");

                yield return new TestCaseData(Valid_InterfaceWithGenericKVPairReturnType).SetName("Valid KeyValuePair<> as return type of signature in interface");
                yield return new TestCaseData(Valid_InterfaceWithGenericKVPairInput).SetName("Valid KeyValuePair<> interface with Generic KeyValuePair parameter");
                yield return new TestCaseData(Valid_ClassWithGenericKVPairReturnType).SetName("Valid KeyValuePair<> class with Generic KeyValuePair return type");
                yield return new TestCaseData(Valid_ClassWithGenericKVPairInput).SetName("Valid KeyValuePair<> class with Generic KeyValuePair parameter");
                yield return new TestCaseData(Valid_IfaceWithGenKVPairProp).SetName("Valid KeyValuePair<> interface with Generic KeyValuePair property");
                yield return new TestCaseData(Valid_ClassWithGenKVPairProp).SetName("Valid KeyValuePair<> class with Generic KeyValuePair return property");

                yield return new TestCaseData(Valid_ClassWithGenericKVPairInput_Private).SetName("Valid KeyValuePair<> as parameter to private method");
                yield return new TestCaseData(Valid_ClassWithGenericKVPairReturnType_Private).SetName("Valid KeyValuePair<> as return type of private method");
                yield return new TestCaseData(Valid_ClassWithPrivateGenKVPairProp).SetName("Valid KeyValuePair<> as private prop to class");
                yield return new TestCaseData(Valid_IfaceWithPrivateGenKVPairProp).SetName("Valid KeyValuePair<> as private prop to interface");

                yield return new TestCaseData(Valid_ClassWithGenericEnumerableInput_Private).SetName("Valid Enumerable<> as parameter to private method");
                yield return new TestCaseData(Valid_ClassWithGenericEnumerableReturnType_Private).SetName("Valid Enumerable<> as return type of private method");
                yield return new TestCaseData(Valid_ClassWithPrivateGenEnumerableProp).SetName("Valid Enumerable<> as private prop to class");
                yield return new TestCaseData(Valid_IfaceWithPrivateGenEnumerableProp).SetName("Valid Enumerable<> as private prop to interface");

                yield return new TestCaseData(Valid_ClassWithGenericListInput_Private).SetName("Valid List<> as parameter to private method");
                yield return new TestCaseData(Valid_ClassWithGenericListReturnType_Private).SetName("Valid List<> as return type of private method");
                yield return new TestCaseData(Valid_ClassWithPrivateGenListProp).SetName("Valid List<> as private prop to class");
                yield return new TestCaseData(Valid_IfaceWithPrivateGenListProp).SetName("Valid List<> as private prop to interface");

                #endregion

                #region ArrayAccessAttribute 
                // ReadOnlyArray / WriteOnlyArray Attribute
                yield return new TestCaseData(Valid_ArrayParamAttrUnary_1).SetName("Valid ArrayAttribute, Array marked read only");
                yield return new TestCaseData(Valid_ArrayParamAttrUnary_2).SetName("Valid ArrayAttribute, Array marked write only");
                yield return new TestCaseData(Valid_ArrayParamAttrUnary_3).SetName("Valid ArrayAttribute, Array marked out and write only");
                yield return new TestCaseData(Valid_ArrayParamAttrUnary_4).SetName("Valid ArrayAttribute, Array marked out");
                yield return new TestCaseData(Valid_ArrayParamAttrBinary_1).SetName("Valid ArrayAttribute, Array marked read only, second parameter");
                yield return new TestCaseData(Valid_ArrayParamAttrBinary_2).SetName("Valid ArrayAttribute, Array marked write only, second parameter");
                yield return new TestCaseData(Valid_ArrayParamAttrBinary_3).SetName("Valid ArrayAttribute, Array marked write only and out, second parameter");
                yield return new TestCaseData(Valid_ArrayParamAttrBinary_4).SetName("Valid ArrayAttribute, Array marked out, second parameter");
                yield return new TestCaseData(Valid_ArrayParamAttrBinary_5).SetName("Valid. ArrayAttribute, Two arrays, both marked read");
                yield return new TestCaseData(Valid_ArrayParamAttrBinary_6).SetName("Valid. ArrayAttribute, Two arrays, one marked read, one marked write");
                yield return new TestCaseData(Valid_ArrayParamAttrBinary_7).SetName("Valid. ArrayAttribute, Two arrays, one marked read, one marked write and out");
                yield return new TestCaseData(Valid_ArrayParamAttrBinary_8).SetName("Valid. ArrayAttribute, Two arrays, one marked write, one marked out");

                #endregion

                #region StructField
                yield return new TestCaseData(Valid_StructWithByteField).SetName("Valid. struct with byte field");
                yield return new TestCaseData(Valid_StructWithPrimitiveTypes).SetName("Valid. Struct with only fields of basic types");
                yield return new TestCaseData(Valid_StructWithImportedStruct).SetName("Valid. Struct with struct field");
                yield return new TestCaseData(Valid_StructWithImportedStructQualified).SetName("Valid. Struct with qualified struct field");
                #endregion

                #region InvalidArrayTypes_Signatures

                // SystemArray  
                yield return new TestCaseData(Valid_SystemArrayProperty).SetName("Valid SystemArray private property");
                yield return new TestCaseData(Valid_SystemArray_Interface1).SetName("Valid System Array internal interface 1");
                yield return new TestCaseData(Valid_SystemArray_Interface2).SetName("Valid System Array internal interface 2");
                yield return new TestCaseData(Valid_SystemArray_Interface3).SetName("Valid System Array internal interface 3");
                yield return new TestCaseData(Valid_SystemArray_InternalClass1).SetName("Valid System Array internal class 1");
                yield return new TestCaseData(Valid_SystemArray_InternalClass2).SetName("Valid System Array internal class 2");
                yield return new TestCaseData(Valid_SystemArray_InternalClass3).SetName("Valid System Array internal class 3");
                yield return new TestCaseData(Valid_SystemArray_InternalClass4).SetName("Valid System Array internal class 4");
                yield return new TestCaseData(Valid_SystemArray_PublicClassPrivateProperty1).SetName("Valid System Array public class / private property 1");
                yield return new TestCaseData(Valid_SystemArray_PublicClassPrivateProperty2).SetName("Valid System Array public class / private property 2");
                yield return new TestCaseData(Valid_SystemArray_PublicClassPrivateProperty3).SetName("Valid System Array public class / private property 3");
                yield return new TestCaseData(Valid_SystemArray_PublicClassPrivateProperty4).SetName("Valid System Array public class / private property 4");
                yield return new TestCaseData(Valid_SystemArray_PublicClassPrivateProperty5).SetName("Valid System Array public class / private property 5");
                yield return new TestCaseData(Valid_SystemArray_PublicClassPrivateProperty6).SetName("Valid System Array public class / private property 6");
                yield return new TestCaseData(Valid_SystemArray_InternalClassPublicMethods1).SetName("Valid System Array internal class / public method 1");
                yield return new TestCaseData(Valid_SystemArray_InternalClassPublicMethods2).SetName("Valid System Array internal class / public method 2");
                yield return new TestCaseData(Valid_SystemArray_InternalClassPublicMethods3).SetName("Valid System Array internal class / public method 3");
                yield return new TestCaseData(Valid_SystemArray_InternalClassPublicMethods4).SetName("Valid System Array internal class / public method 4");
                yield return new TestCaseData(Valid_SystemArray_InternalClassPublicMethods5).SetName("Valid System Array internal class / public method 5");
                yield return new TestCaseData(Valid_SystemArray_InternalClassPublicMethods6).SetName("Valid System Array internal class / public method 6");
                yield return new TestCaseData(Valid_SystemArray_PrivateClassPublicProperty1).SetName("Valid System Array internal class / public property 1");
                yield return new TestCaseData(Valid_SystemArray_PrivateClassPublicProperty2).SetName("Valid System Array internal class / public property 2");
                yield return new TestCaseData(Valid_SystemArray_PrivateClassPublicProperty3).SetName("Valid System Array internal class / public property 3");
                yield return new TestCaseData(Valid_SystemArray_PrivateClassPublicProperty4).SetName("Valid System Array internal class / public property 4");
                yield return new TestCaseData(Valid_SystemArray_PrivateClassPublicProperty5).SetName("Valid System Array internal class / public property 5");
                yield return new TestCaseData(Valid_SystemArray_PrivateClassPublicProperty6).SetName("Valid System Array internal class / public property 6");
                yield return new TestCaseData(Valid_SystemArray_PrivateClassPublicProperty7).SetName("Valid System Array internal class / public property 7");
                yield return new TestCaseData(Valid_SystemArray_PrivateClassPublicProperty8).SetName("Valid System Array internal class / public property 8");
                yield return new TestCaseData(Valid_SystemArrayPublicClassPrivateMethod1).SetName("Valid System Array public class / private method 1");
                yield return new TestCaseData(Valid_SystemArrayPublicClassPrivateMethod2).SetName("Valid System Array public class / private method 2");
                yield return new TestCaseData(Valid_SystemArrayPublicClassPrivateMethod3).SetName("Valid System Array public class / private method 3");
                yield return new TestCaseData(Valid_SystemArrayPublicClassPrivateMethod4).SetName("Valid System Array public class / private method 4");
                yield return new TestCaseData(Valid_SystemArrayPublicClassPrivateMethod5).SetName("Valid System Array public class / private method 5");
                yield return new TestCaseData(Valid_SystemArrayPublicClassPrivateMethod6).SetName("Valid System Array public class / private method 6");
                // multi dim array tests 
                yield return new TestCaseData(Valid_MultiDimArray_PublicClassPrivateMethod1).SetName("Valid array [,] public class / private method 1");
                yield return new TestCaseData(Valid_MultiDimArray_PublicClassPrivateMethod2).SetName("Valid array [,] public class / private method 2");
                yield return new TestCaseData(Valid_MultiDimArray_PublicClassPrivateMethod3).SetName("Valid array [,] public class / private method 3");
                yield return new TestCaseData(Valid_MultiDimArray_PublicClassPrivateMethod4).SetName("Valid array [,] public class / private method 4");
                yield return new TestCaseData(Valid_MultiDimArray_PublicClassPrivateMethod5).SetName("Valid array [,,] public class / private method 5");
                yield return new TestCaseData(Valid_MultiDimArray_PublicClassPrivateMethod6).SetName("Valid array [,,] public class / private method 6");
                
                yield return new TestCaseData(Valid_3D_PrivateClass_PublicMethod1).SetName("Valid MultiDim 3D private class / public method 1");
                yield return new TestCaseData(Valid_3D_PrivateClass_PublicMethod2).SetName("Valid MultiDim 3D private class / public method 2");
                yield return new TestCaseData(Valid_3D_PrivateClass_PublicMethod3).SetName("Valid MultiDim 3D private class / public method 3");
                yield return new TestCaseData(Valid_3D_PrivateClass_PublicMethod4).SetName("Valid MultiDim 3D private class / public method 4");
                yield return new TestCaseData(Valid_3D_PrivateClass_PublicMethod5).SetName("Valid MultiDim 3D private class / public method 5");
                yield return new TestCaseData(Valid_3D_PrivateClass_PublicMethod6).SetName("Valid MultiDim 3D private class / public method 6");
                
                yield return new TestCaseData(Valid_2D_PrivateClass_PublicMethod1).SetName("Valid MultiDim 2D private class / public method 1");
                yield return new TestCaseData(Valid_2D_PrivateClass_PublicMethod2).SetName("Valid MultiDim 2D private class / public method 2");
                yield return new TestCaseData(Valid_2D_PrivateClass_PublicMethod3).SetName("Valid MultiDim 2D private class / public method 3");
                yield return new TestCaseData(Valid_2D_PrivateClass_PublicMethod4).SetName("Valid MultiDim 2D private class / public method 4");
                yield return new TestCaseData(Valid_2D_PrivateClass_PublicMethod5).SetName("Valid MultiDim 2D private class / public method 5");
                yield return new TestCaseData(Valid_2D_PrivateClass_PublicMethod6).SetName("Valid  MultiDim 2D private class / public method 6");
                
                yield return new TestCaseData(Valid_MultiDimArray_PrivateClassPublicProperty1).SetName("Valid MultiDim 2D private class / public property 1");
                yield return new TestCaseData(Valid_MultiDimArray_PrivateClassPublicProperty2).SetName("Valid MultiDim 2D private class / public property 2");
                yield return new TestCaseData(Valid_MultiDimArray_PrivateClassPublicProperty3).SetName("Valid MultiDim 2D private class / public property 3");
                yield return new TestCaseData(Valid_MultiDimArray_PrivateClassPublicProperty4).SetName("Valid MultiDim 2D private class / public property 4");
                yield return new TestCaseData(Valid_MultiDimArray_PublicClassPrivateProperty1).SetName("Valid MultiDim 2D public class / private property 1");
                yield return new TestCaseData(Valid_MultiDimArray_PublicClassPrivateProperty2).SetName("Valid MultiDim 2D public class / private property 2");
                // jagged array tests
                yield return new TestCaseData(Valid_JaggedMix_PrivateClassPublicProperty).SetName("Valid Jagged Array private class / private property");
                yield return new TestCaseData(Valid_Jagged2D_PrivateClassPublicMethods).SetName("Valid Jagged Array private class / public method");
                yield return new TestCaseData(Valid_Jagged3D_PrivateClassPublicMethods).SetName("Valid Jagged Array private class / public method");
                yield return new TestCaseData(Valid_Jagged3D_PublicClassPrivateMethods).SetName("Valid Jagged Array public class / private method");
                yield return new TestCaseData(Valid_Jagged2D_Property).SetName("Valid Jagged 2D Array public property");
                yield return new TestCaseData(Valid_Jagged3D_Property).SetName("Valid Jagged 3D Array public property");

                #endregion

                #region   DefaultOverloadAttribute

                // overload attributes
                yield return new TestCaseData(Valid_InterfaceWithOverloadAttribute).SetName("Valid interface with overloads and one marked as default");
                yield return new TestCaseData(Valid_TwoOverloads_DiffParamCount).SetName("Valid DefaultOverload attribute 1");
                yield return new TestCaseData(Valid_TwoOverloads_OneAttribute_OneInList).SetName("Valid DefaultOverload attribute 2");
                yield return new TestCaseData(Valid_TwoOverloads_OneAttribute_OneIrrelevatAttribute).SetName("Valid DefaultOverload attribute 3");
                yield return new TestCaseData(Valid_TwoOverloads_OneAttribute_TwoLists).SetName("Valid DefaultOverload attribute 4");
                yield return new TestCaseData(Valid_ThreeOverloads_OneAttribute).SetName("Valid DefaultOverload attribute 5");
                yield return new TestCaseData(Valid_ThreeOverloads_OneAttribute_2).SetName("Valid DefaultOverload attribute 6");
                yield return new TestCaseData(Valid_TwoOverloads_OneAttribute_3).SetName("Valid DefaultOverload attribute 7");

                #endregion
            }
        }

        #endregion

    }
}