using NUnit.Framework;
using Microsoft.CodeAnalysis;
using WinRT.SourceGenerator;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.Linq;
using System;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace DiagnosticTests
{
    class ConfigOptions : AnalyzerConfigOptions
    {
        public Dictionary<string, string> Values { get; set; } = new();
        public override bool TryGetValue(string key, [NotNullWhen(true)] out string value)
        {
            return Values.TryGetValue(key, out value);
        }
    }

    class ConfigProvider : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions { get; } = new ConfigOptions();

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
        {
            return GlobalOptions;
        }

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
        {
            return GlobalOptions;
        }
    }

    [TestFixture]
    public sealed partial class UnitTesting
    {

        private static AnalyzerConfigOptionsProvider Options
        {
            get
            {
                var o = new ConfigProvider();
                var config = o.GlobalOptions as ConfigOptions;
                config.Values["build_property.AssemblyName"] = "DiagnosticTests";
                config.Values["build_property.AssemblyVersion"] = "1.0.0.0";
                config.Values["build_property.CsWinRTComponent"] = "true";
                return o;
            }
        }

        /// <summary>
        /// CheckNoDiagnostic asserts that no diagnostics are raised on the compilation produced 
        /// from the cswinrt source generator based on the given source code</summary>
        /// <param name="source"></param>
        [Test, TestCaseSource(nameof(ValidCases))]
        public void CheckNoDiagnostic(string source)
        {
            Assert.DoesNotThrow(() =>
            {
                Compilation compilation = CreateCompilation(source);
                RunGenerators(compilation, out var diagnosticsFound, out var result, Options, new Generator.SourceGenerator());

                var WinRTDiagnostics = diagnosticsFound.Where(diag =>
                    diag.Id.StartsWith("CsWinRT", StringComparison.Ordinal)
                    );

                if (WinRTDiagnostics.Any())
                {
                    var foundDiagnostics = string.Join("\n", WinRTDiagnostics.Select(x => x.GetMessage()));
                    Exception inner = null;
                    if (!result.Results.IsEmpty)
                    {
                        inner = result.Results[0].Exception;
                    }

                    throw new AssertionException("Expected no diagnostics. But found:\n" + foundDiagnostics, inner);
                }
            });
        }

        /// <summary>
        /// CodeHasDiagnostic takes some source code (string) and a Diagnostic descriptor, 
        /// it checks that a diagnostic with the same description was raised by the source generator 
        /// </summary> 
        [Test, TestCaseSource(nameof(InvalidCases))]
        public void CodeHasDiagnostic(string testCode, DiagnosticDescriptor rule)
        {
            Compilation compilation = CreateCompilation(testCode);
            RunGenerators(compilation, out var diagnosticsFound, out var result, Options, new Generator.SourceGenerator());
            HashSet<DiagnosticDescriptor> diagDescsFound = MakeDiagnosticSet(diagnosticsFound);
            if (!diagDescsFound.Contains(rule))
            {
                if (diagDescsFound.Count != 0)
                {
                    var foundDiagnostics = string.Join("\n", diagDescsFound.Select(x => x.Description));
                    Exception inner = null;
                    if (!result.Results.IsEmpty)
                    {
                        inner = result.Results[0].Exception;
                    }
                    throw new SuccessException("Didn't find the expected diagnostic, found:\n" + foundDiagnostics, inner);
                }
                else
                {
                    throw new SuccessException("No diagnostics found.");
                }
            }
        }

        #region InvalidTests
        private static IEnumerable<TestCaseData> InvalidCases
        {
            get
            {
                // private getter
                yield return new TestCaseData(PrivateGetter, WinRTRules.PrivateGetterRule).SetName("Property. PrivateGetter");
                yield return new TestCaseData(PropertyNoGetter, WinRTRules.PrivateGetterRule).SetName("Property. No Get, public Setter");
                // namespace tests
                yield return new TestCaseData(SameNameNamespacesDisjoint, WinRTRules.DisjointNamespaceRule).SetName("Namespace. isn't accessible without Test prefix, doesn't use type");
                yield return new TestCaseData(UnrelatedNamespaceWithPublicPartialTypes, WinRTRules.DisjointNamespaceRule).SetName("Namespace. Component has public types in different namespaces");
                yield return new TestCaseData(NamespacesDifferByCase, WinRTRules.NamespacesDifferByCase).SetName("Namespace. names only differ by case");
                yield return new TestCaseData(DisjointNamespaces, WinRTRules.DisjointNamespaceRule).SetName("Namespace. isn't accessible without Test prefix, doesn't use type");
                yield return new TestCaseData(DisjointNamespaces2, WinRTRules.DisjointNamespaceRule).SetName("Namespace. using type from inaccessible namespace");
                yield return new TestCaseData(NoPublicTypes, WinRTRules.NoPublicTypesRule).SetName("Component has no public types");
                // the below tests are positive tests when the winmd is named "Test.winmd", and negative when it is "Test.A.winmd" 
                // they are examples of what it means for namespace to be "prefixed with the winmd file name"
                yield return new TestCaseData(NamespaceDifferByDot, WinRTRules.DisjointNamespaceRule).SetName("Namespace Test.A and Test.B");
                yield return new TestCaseData(NamespaceDifferByDot2, WinRTRules.DisjointNamespaceRule).SetName("Namespace Test.A and Test");

                // Unsealed classes, generic class/interfaces, invalid inheritance (System.Exception)
                yield return new TestCaseData(UnsealedClass, WinRTRules.UnsealedClassRule).SetName("Unsealed class public field");
                yield return new TestCaseData(UnsealedClass2, WinRTRules.UnsealedClassRule).SetName("Unsealed class private field");
                yield return new TestCaseData(GenericClass, WinRTRules.GenericTypeRule).SetName("Class marked generic");
                yield return new TestCaseData(GenericInterface, WinRTRules.GenericTypeRule).SetName("Interface marked generic");

                // Enumerable
                yield return new TestCaseData(InterfaceWithGenericEnumerableReturnType, WinRTRules.UnsupportedTypeRule).SetName("NotValidType. Enumerable method return type, interface");
                yield return new TestCaseData(InterfaceWithGenericEnumerableInput, WinRTRules.UnsupportedTypeRule).SetName("NotValidType. Enumerable method parameter, interface");
                yield return new TestCaseData(ClassWithGenericEnumerableReturnType, WinRTRules.UnsupportedTypeRule).SetName("NotValidType. Enumerable method return type, class");
                yield return new TestCaseData(ClassWithGenericEnumerableInput, WinRTRules.UnsupportedTypeRule).SetName("NotValidType. Enumerable method parameter, class");
                yield return new TestCaseData(IfaceWithGenEnumerableProp, WinRTRules.UnsupportedTypeRule).SetName("NotValidType. Enumerable property, interface");
                yield return new TestCaseData(ClassWithGenEnumerableProp, WinRTRules.UnsupportedTypeRule).SetName("NotValidType. Enumerable property, class");

                // KeyValuePair
                yield return new TestCaseData(ClassWithGenericKVPairReturnType, WinRTRules.UnsupportedTypeRule).SetName("NotValidType. KeyValuePair<> method return type, class");
                yield return new TestCaseData(ClassWithGenericKVPairInput, WinRTRules.UnsupportedTypeRule).SetName("NotValidType. KeyValuePair<> method parameter, class");
                yield return new TestCaseData(ClassWithGenKVPairProp, WinRTRules.UnsupportedTypeRule).SetName("NotValidType. KeyValuePair<> property, class ");
                yield return new TestCaseData(IfaceWithGenKVPairProp, WinRTRules.UnsupportedTypeRule).SetName("NotValidType. KeyValuePair<> property, interface ");
                yield return new TestCaseData(InterfaceWithGenericKVPairReturnType, WinRTRules.UnsupportedTypeRule).SetName("NotValidType. KeyValuePair<> method return type, interface ");
                yield return new TestCaseData(InterfaceWithGenericKVPairInput, WinRTRules.UnsupportedTypeRule).SetName("NotValidType. KeyValuePair<> method parameter, interface ");

                // readonlydict<T,S>
                yield return new TestCaseData(ClassWithGenericRODictReturnType, WinRTRules.UnsupportedTypeRule).SetName("NotValidType. ReadOnlyDictionary<> method return type, class");
                yield return new TestCaseData(InterfaceWithGenericRODictReturnType, WinRTRules.UnsupportedTypeRule).SetName("NotValidType. ReadOnlyDictionary<> method return type, interface");
                yield return new TestCaseData(InterfaceWithGenericRODictInput, WinRTRules.UnsupportedTypeRule).SetName("NotValidType. ReadOnlyDictionary<> method parameter, interface");
                yield return new TestCaseData(ClassWithGenericRODictInput, WinRTRules.UnsupportedTypeRule).SetName("NotValidType. ReadOnlyDictionary<> method parameter, class");
                yield return new TestCaseData(IfaceWithGenRODictProp, WinRTRules.UnsupportedTypeRule).SetName("NotValidType. ReadOnlyDictionary<> method property, interface");
                yield return new TestCaseData(ClassWithGenRODictProp, WinRTRules.UnsupportedTypeRule).SetName("NotValidType. ReadOnlyDictionary<>  method property, class");

                // dict<T,S>
                yield return new TestCaseData(IfaceWithGenDictProp, WinRTRules.UnsupportedTypeRule).SetName("NotValidType. Dictionary<> property, interface ");
                yield return new TestCaseData(ClassWithGenDictProp, WinRTRules.UnsupportedTypeRule).SetName("NotValidType. Dictionary<> property, class ");
                yield return new TestCaseData(ClassWithGenericDictInput, WinRTRules.UnsupportedTypeRule).SetName("NotValidType. Dictionary<> method parameter, class");
                yield return new TestCaseData(InterfaceWithGenericDictInput, WinRTRules.UnsupportedTypeRule).SetName("NotValidType. Dictionary<> method parameter, interface ");
                yield return new TestCaseData(ClassWithGenericDictReturnType, WinRTRules.UnsupportedTypeRule).SetName("NotValidType. Dictionary<> method return type, class ");
                yield return new TestCaseData(InterfaceWithGenericDictReturnType, WinRTRules.UnsupportedTypeRule).SetName("NotValidType. Dictionary<> method return type, interface ");

                // list<T> 
                yield return new TestCaseData(InterfaceWithGenericListReturnType, WinRTRules.UnsupportedTypeRule).SetName("NotValidType. List<> method return type, interface");
                yield return new TestCaseData(ClassWithGenericListReturnType, WinRTRules.UnsupportedTypeRule).SetName("NotValidType. List<> method return type, class");
                yield return new TestCaseData(InterfaceWithGenericListInput, WinRTRules.UnsupportedTypeRule).SetName("NotValidType. List<> method parameter, interface");
                yield return new TestCaseData(ClassWithGenericListInput, WinRTRules.UnsupportedTypeRule).SetName("NotValidType. List<> method parameter, class");
                yield return new TestCaseData(IfaceWithGenListProp, WinRTRules.UnsupportedTypeRule).SetName("NotValidType. List<> property, interface");
                yield return new TestCaseData(ClassWithGenListProp, WinRTRules.UnsupportedTypeRule).SetName("NotValidType. List<> property, class");

                // multi-dimensional array tests
                yield return new TestCaseData(MultiDim_2DProp, WinRTRules.MultiDimensionalArrayRule).SetName("Array [,]. 2D Property");
                yield return new TestCaseData(MultiDim_3DProp, WinRTRules.MultiDimensionalArrayRule).SetName("Array [,,]. 3D Property");
                yield return new TestCaseData(MultiDim_3DProp_Whitespace, WinRTRules.MultiDimensionalArrayRule).SetName("Array [,,]. 3D Property With whitespace");
                yield return new TestCaseData(MultiDim_2D_PublicClassPublicMethod1, WinRTRules.MultiDimensionalArrayRule).SetName("Array [,]. return type, class method signature");
                yield return new TestCaseData(MultiDim_2D_PublicClassPublicMethod2, WinRTRules.MultiDimensionalArrayRule).SetName("Array [,]. return and first input, class method signature");
                yield return new TestCaseData(MultiDim_2D_PublicClassPublicMethod3, WinRTRules.MultiDimensionalArrayRule).SetName("Array [,]. return and second input, class method signature");
                yield return new TestCaseData(MultiDim_2D_PublicClassPublicMethod4, WinRTRules.MultiDimensionalArrayRule).SetName("Array [,]. second input, class method signature");
                yield return new TestCaseData(MultiDim_2D_PublicClassPublicMethod5, WinRTRules.MultiDimensionalArrayRule).SetName("Array [,]. second of three inputs, class method signature");
                yield return new TestCaseData(MultiDim_2D_PublicClassPublicMethod6, WinRTRules.MultiDimensionalArrayRule).SetName("Array [,]. return and second of three inputs, class method signature");
                yield return new TestCaseData(MultiDim_3D_PublicClassPublicMethod1, WinRTRules.MultiDimensionalArrayRule).SetName("Array [,,]. return type, class method signature");
                yield return new TestCaseData(MultiDim_3D_PublicClassPublicMethod2, WinRTRules.MultiDimensionalArrayRule).SetName("Array [,,]. return and first input, class method signature");
                yield return new TestCaseData(MultiDim_3D_PublicClassPublicMethod3, WinRTRules.MultiDimensionalArrayRule).SetName("Array [,,]. return and second input, class method signature");
                yield return new TestCaseData(MultiDim_3D_PublicClassPublicMethod4, WinRTRules.MultiDimensionalArrayRule).SetName("Array [,,]. second input, class method signature");
                yield return new TestCaseData(MultiDim_3D_PublicClassPublicMethod5, WinRTRules.MultiDimensionalArrayRule).SetName("Array [,,]. second of three inputs, class method signature");
                yield return new TestCaseData(MultiDim_3D_PublicClassPublicMethod6, WinRTRules.MultiDimensionalArrayRule).SetName("Array [,,]. return and second of three inputs, class method signature");
                yield return new TestCaseData(MultiDim_2D_Interface1, WinRTRules.MultiDimensionalArrayRule).SetName("Array [,]. return type, interface method signature");
                yield return new TestCaseData(MultiDim_2D_Interface2, WinRTRules.MultiDimensionalArrayRule).SetName("Array [,]. return and first input, interface method signature");
                yield return new TestCaseData(MultiDim_2D_Interface3, WinRTRules.MultiDimensionalArrayRule).SetName("Array [,]. return and second input, interface method signature");
                yield return new TestCaseData(MultiDim_2D_Interface4, WinRTRules.MultiDimensionalArrayRule).SetName("Array [,]. second input, interface method signature");
                yield return new TestCaseData(MultiDim_2D_Interface5, WinRTRules.MultiDimensionalArrayRule).SetName("Array [,]. second of three inputs, interface method signature");
                yield return new TestCaseData(MultiDim_2D_Interface6, WinRTRules.MultiDimensionalArrayRule).SetName("Array [,]. return and second of three inputs, interface method signature");
                yield return new TestCaseData(MultiDim_3D_Interface1, WinRTRules.MultiDimensionalArrayRule).SetName("Array [,,]. return type, interface method signature");
                yield return new TestCaseData(MultiDim_3D_Interface2, WinRTRules.MultiDimensionalArrayRule).SetName("Array [,,]. return and first input, interface method signature");
                yield return new TestCaseData(MultiDim_3D_Interface3, WinRTRules.MultiDimensionalArrayRule).SetName("Array [,,]. return and second input, interface method signature");
                yield return new TestCaseData(MultiDim_3D_Interface4, WinRTRules.MultiDimensionalArrayRule).SetName("Array [,,]. second input, interface method signature");
                yield return new TestCaseData(MultiDim_3D_Interface5, WinRTRules.MultiDimensionalArrayRule).SetName("Array [,,]. second of three inputs, interface method signature");
                yield return new TestCaseData(MultiDim_3D_Interface6, WinRTRules.MultiDimensionalArrayRule).SetName("Array [,,]. return and second of three inputs, interface method signature");
                yield return new TestCaseData(SubNamespaceInterface_D2Method1, WinRTRules.MultiDimensionalArrayRule).SetName("Subnamespace. Array [,] return type, interface method signature");
                yield return new TestCaseData(SubNamespaceInterface_D2Method2, WinRTRules.MultiDimensionalArrayRule).SetName("Subnamespace. Array [,] return and first input, interface method signature");
                yield return new TestCaseData(SubNamespaceInterface_D2Method3, WinRTRules.MultiDimensionalArrayRule).SetName("Subnamespace. Array [,] return and second input, interface method signature");
                yield return new TestCaseData(SubNamespaceInterface_D2Method4, WinRTRules.MultiDimensionalArrayRule).SetName("Subnamespace. Array [,] second input, interface method signature");
                yield return new TestCaseData(SubNamespaceInterface_D2Method5, WinRTRules.MultiDimensionalArrayRule).SetName("Subnamespace. Array [,] second of three inputs, interface method signature");
                yield return new TestCaseData(SubNamespaceInterface_D2Method6, WinRTRules.MultiDimensionalArrayRule).SetName("Subnamespace. Array [,] return and second of three inputs, interface method signature");
                yield return new TestCaseData(SubNamespaceInterface_D3Method1, WinRTRules.MultiDimensionalArrayRule).SetName("Subnamespace. Array [,,] return type, interface method signature");
                yield return new TestCaseData(SubNamespaceInterface_D3Method2, WinRTRules.MultiDimensionalArrayRule).SetName("Subnamespace. Array [,,] return and first input, interface method signature");
                yield return new TestCaseData(SubNamespaceInterface_D3Method3, WinRTRules.MultiDimensionalArrayRule).SetName("Subnamespace. Array [,,] return and second input, interface method signature");
                yield return new TestCaseData(SubNamespaceInterface_D3Method4, WinRTRules.MultiDimensionalArrayRule).SetName("Subnamespace. Array [,,] second input, interface method signature");
                yield return new TestCaseData(SubNamespaceInterface_D3Method5, WinRTRules.MultiDimensionalArrayRule).SetName("Subnamespace. Array [,,] second of three inputs, interface method signature");
                yield return new TestCaseData(SubNamespaceInterface_D3Method6, WinRTRules.MultiDimensionalArrayRule).SetName("Subnamespace. Array [,,] return and second of three inputs, interface method signature");

                #region JaggedArray
                // jagged array tests 
                yield return new TestCaseData(Jagged2D_Property2, WinRTRules.JaggedArrayRule).SetName("Array [][]. Property 2");
                yield return new TestCaseData(Jagged3D_Property1, WinRTRules.JaggedArrayRule).SetName("Array [][][]. Property 1");
                yield return new TestCaseData(Jagged2D_ClassMethod1, WinRTRules.JaggedArrayRule).SetName("Array [][]. return type, class method signature");
                yield return new TestCaseData(Jagged2D_ClassMethod2, WinRTRules.JaggedArrayRule).SetName("Array [][]. return and first input, class method signature");
                yield return new TestCaseData(Jagged2D_ClassMethod3, WinRTRules.JaggedArrayRule).SetName("Array [][]. return and second input, class method signature");
                yield return new TestCaseData(Jagged2D_ClassMethod4, WinRTRules.JaggedArrayRule).SetName("Array [][]. second input, class method signature");
                yield return new TestCaseData(Jagged2D_ClassMethod5, WinRTRules.JaggedArrayRule).SetName("Array [][]. second of three inputs, class method signature");
                yield return new TestCaseData(Jagged2D_ClassMethod6, WinRTRules.JaggedArrayRule).SetName("Array [][]. return and second of three inputs, class method signature");
                yield return new TestCaseData(Jagged3D_ClassMethod1, WinRTRules.JaggedArrayRule).SetName("Array [][][]. return type, class method signature");
                yield return new TestCaseData(Jagged3D_ClassMethod2, WinRTRules.JaggedArrayRule).SetName("Array [][][]. return and first input, class method signature");
                yield return new TestCaseData(Jagged3D_ClassMethod3, WinRTRules.JaggedArrayRule).SetName("Array [][][]. return and second input, class method signature");
                yield return new TestCaseData(Jagged3D_ClassMethod4, WinRTRules.JaggedArrayRule).SetName("Array [][][]. second input, class method signature");
                yield return new TestCaseData(Jagged3D_ClassMethod5, WinRTRules.JaggedArrayRule).SetName("Array [][][]. second of three inputs, class method signature");
                yield return new TestCaseData(Jagged3D_ClassMethod6, WinRTRules.JaggedArrayRule).SetName("Array [][][]. return and second of three inputs, class method signature");
                yield return new TestCaseData(Jagged2D_InterfaceMethod1, WinRTRules.JaggedArrayRule).SetName("Array [][]. return type, interface method signature");
                yield return new TestCaseData(Jagged2D_InterfaceMethod2, WinRTRules.JaggedArrayRule).SetName("Array [][]. return and first input, interface method signature");
                yield return new TestCaseData(Jagged2D_InterfaceMethod3, WinRTRules.JaggedArrayRule).SetName("Array [][]. return and second input, interface method signature");
                yield return new TestCaseData(Jagged2D_InterfaceMethod4, WinRTRules.JaggedArrayRule).SetName("Array [][]. second input, interface method signature");
                yield return new TestCaseData(Jagged2D_InterfaceMethod5, WinRTRules.JaggedArrayRule).SetName("Array [][]. second of three inputs, interface method signature");
                yield return new TestCaseData(Jagged2D_InterfaceMethod6, WinRTRules.JaggedArrayRule).SetName("Array [][]. return and second of three inputs, interface method signature");
                yield return new TestCaseData(Jagged3D_InterfaceMethod1, WinRTRules.JaggedArrayRule).SetName("Array [][][]. return type, interface method signature");
                yield return new TestCaseData(Jagged3D_InterfaceMethod2, WinRTRules.JaggedArrayRule).SetName("Array [][][]. return and first input, interface method signature");
                yield return new TestCaseData(Jagged3D_InterfaceMethod3, WinRTRules.JaggedArrayRule).SetName("Array [][][]. return and second input, interface method signature");
                yield return new TestCaseData(Jagged3D_InterfaceMethod4, WinRTRules.JaggedArrayRule).SetName("Array [][][]. second input, interface method signature");
                yield return new TestCaseData(Jagged3D_InterfaceMethod5, WinRTRules.JaggedArrayRule).SetName("Array [][][]. second of three inputs, interface method signature");
                yield return new TestCaseData(Jagged3D_InterfaceMethod6, WinRTRules.JaggedArrayRule).SetName("Array [][][]. return and second of three inputs, interface method signature");
                yield return new TestCaseData(SubNamespace_Jagged2DInterface1, WinRTRules.JaggedArrayRule).SetName("Subnamespace. Array [][]. return type, interface method signature");
                yield return new TestCaseData(SubNamespace_Jagged2DInterface2, WinRTRules.JaggedArrayRule).SetName("Subnamespace. Array [][]. return and first input, interface method signature");
                yield return new TestCaseData(SubNamespace_Jagged2DInterface3, WinRTRules.JaggedArrayRule).SetName("Subnamespace. Array [][]. return and second input, interface method signature");
                yield return new TestCaseData(SubNamespace_Jagged2DInterface4, WinRTRules.JaggedArrayRule).SetName("Subnamespace. Array [][]. second input, interface method signature");
                yield return new TestCaseData(SubNamespace_Jagged2DInterface5, WinRTRules.JaggedArrayRule).SetName("Subnamespace. Array [][]. second of three inputs, interface method signature");
                yield return new TestCaseData(SubNamespace_Jagged2DInterface6, WinRTRules.JaggedArrayRule).SetName("Subnamespace. Array [][]. return and second of three inputs, interface method signature");
                yield return new TestCaseData(SubNamespace_Jagged3DInterface1, WinRTRules.JaggedArrayRule).SetName("Subnamespace. Array [][][]. return type, interface method signature");
                yield return new TestCaseData(SubNamespace_Jagged3DInterface2, WinRTRules.JaggedArrayRule).SetName("Subnamespace. Array [][][]. return and first input, interface method signature");
                yield return new TestCaseData(SubNamespace_Jagged3DInterface3, WinRTRules.JaggedArrayRule).SetName("Subnamespace. Array [][][]. return and second input, interface method signature");
                yield return new TestCaseData(SubNamespace_Jagged3DInterface4, WinRTRules.JaggedArrayRule).SetName("Subnamespace. Array [][][]. second input, interface method signature");
                yield return new TestCaseData(SubNamespace_Jagged3DInterface5, WinRTRules.JaggedArrayRule).SetName("Subnamespace. Array [][][]. second of three inputs, interface method signature");
                yield return new TestCaseData(SubNamespace_Jagged3DInterface6, WinRTRules.JaggedArrayRule).SetName("Subnamespace. Array [][][]. return and second of three inputs, interface method signature");
                #endregion

                #region overload_attribute_tests
                yield return new TestCaseData(InterfaceWithOverloadNoAttribute, WinRTRules.NeedDefaultOverloadAttribute).SetName("DefaultOverload. Need attribute - Interface");
                yield return new TestCaseData(InterfaceWithOverloadAttributeTwice, WinRTRules.MultipleDefaultOverloadAttribute).SetName("DefaultOverload. Interface has too many attributes");
                yield return new TestCaseData(TwoOverloads_NoAttribute_NamesHaveNumber, WinRTRules.NeedDefaultOverloadAttribute).SetName("DefaultOverload. Need attribute  - Name has number");
                yield return new TestCaseData(TwoOverloads_NoAttribute, WinRTRules.NeedDefaultOverloadAttribute).SetName("DefaultOverload. Need Attribute, unqualified");
                yield return new TestCaseData(TwoOverloads_TwoAttribute_OneInList_Unqualified, WinRTRules.MultipleDefaultOverloadAttribute).SetName("DefaultOverload. Multiple attributes, after attribute - unqualified");
                yield return new TestCaseData(TwoOverloads_TwoAttribute_BothInList_Unqualified, WinRTRules.MultipleDefaultOverloadAttribute).SetName("DefaultOverload. Multiple attributes - Unqualified");
                yield return new TestCaseData(TwoOverloads_TwoAttribute_TwoLists_Unqualified, WinRTRules.MultipleDefaultOverloadAttribute).SetName("DefaultOverload. Multiple attributes, two lists - unqualified");
                yield return new TestCaseData(TwoOverloads_TwoAttribute_OneInSeparateList_OneNot_Unqualified, WinRTRules.MultipleDefaultOverloadAttribute).SetName("DefaultOverload. Multiple attributes, one in separate list, one not - Unqualified");
                yield return new TestCaseData(TwoOverloads_TwoAttribute_BothInSeparateList_Unqualified, WinRTRules.MultipleDefaultOverloadAttribute).SetName("DefaultOverload. Multiple attributes, both in separate list - unqualified");
                yield return new TestCaseData(TwoOverloads_TwoAttribute_Unqualified, WinRTRules.MultipleDefaultOverloadAttribute).SetName("DefaultOverload. Multiple attributes - unqualified");
                yield return new TestCaseData(ThreeOverloads_TwoAttributes_Unqualified, WinRTRules.MultipleDefaultOverloadAttribute).SetName("DefaultOverload. Multiple attributes, three overlodas - unqualified");

                yield return new TestCaseData(TwoOverloads_NoAttribute_NamesHaveNumber, WinRTRules.NeedDefaultOverloadAttribute).SetName("DefaultOverload. Need Attribute - Name has number");
                yield return new TestCaseData(TwoOverloads_NoAttribute_OneIrrevAttr, WinRTRules.NeedDefaultOverloadAttribute).SetName("DefaultOverload. Need attribute uses irrelevant attribute");
                yield return new TestCaseData(TwoOverloads_TwoAttribute_OneInList, WinRTRules.MultipleDefaultOverloadAttribute).SetName("DefaultOverload. Multiple attributes, after attribute");
                yield return new TestCaseData(TwoOverloads_TwoAttribute_BothInList, WinRTRules.MultipleDefaultOverloadAttribute).SetName("DefaultOverload. Multiple attributes, same list");
                yield return new TestCaseData(TwoOverloads_TwoAttribute_TwoLists, WinRTRules.MultipleDefaultOverloadAttribute).SetName("DefaultOverload. Multiple attributes, two lists");
                yield return new TestCaseData(TwoOverloads_TwoAttribute_OneInSeparateList_OneNot, WinRTRules.MultipleDefaultOverloadAttribute).SetName("DefaultOverload. Multiple attributes, one in separate list, one not");
                yield return new TestCaseData(TwoOverloads_TwoAttribute_BothInSeparateList, WinRTRules.MultipleDefaultOverloadAttribute).SetName("DefaultOverload. Multiple attributes, both in separate list");
                yield return new TestCaseData(TwoOverloads_TwoAttribute, WinRTRules.MultipleDefaultOverloadAttribute).SetName("DefaultOverload. Multiple attributes");
                yield return new TestCaseData(ThreeOverloads_TwoAttributes, WinRTRules.MultipleDefaultOverloadAttribute).SetName("DefaultOverload. Multiple attributes, three overlodas");

                #endregion

                // multiple class constructors of same arity
                yield return new TestCaseData(ConstructorsOfSameArity, WinRTRules.ClassConstructorRule).SetName("Misc. Multiple constructors of same arity");

                #region InvalidInterfaceInheritance
                yield return new TestCaseData(ClassInheritsException, WinRTRules.NonWinRTInterface).SetName("Inheritance. Class base type System.Exception");
                // implementing async interface
                yield return new TestCaseData(ClassImplementsAsyncAndException, WinRTRules.NonWinRTInterface).SetName("Inheritance. Class implements Exception and IAsyncActionWithProgress");
                yield return new TestCaseData(ClassImplementsIAsyncActionWithProgress, WinRTRules.NonWinRTInterface).SetName("Inheritance. Class implements IAsyncActionWithProgress");
                yield return new TestCaseData(ClassImplementsIAsyncActionWithProgress_Qualified, WinRTRules.NonWinRTInterface).SetName("Inheritance. Qualified, class implements IAsyncActionWithProgress");
                yield return new TestCaseData(InterfaceImplementsIAsyncActionWithProgress, WinRTRules.NonWinRTInterface).SetName("Inheritance. Interface Implements IAsyncActionWithProgress");
                yield return new TestCaseData(InterfaceImplementsIAsyncActionWithProgress2, WinRTRules.NonWinRTInterface).SetName("Inheritance. Interface Implements IAsyncActionWithProgress in full");

                yield return new TestCaseData(ClassImplementsIAsyncAction, WinRTRules.NonWinRTInterface).SetName("Inheritance. Class implements IAsyncAction");
                yield return new TestCaseData(InterfaceImplementsIAsyncAction, WinRTRules.NonWinRTInterface).SetName("Inheritance. Interface Implements IAsyncAction");
                yield return new TestCaseData(InterfaceImplementsIAsyncAction2, WinRTRules.NonWinRTInterface).SetName("Inheritance. Interface Implements IAsyncAction in full");

                yield return new TestCaseData(ClassImplementsIAsyncOperation, WinRTRules.NonWinRTInterface).SetName("Inheritance. Class implements IAsyncOperation");
                yield return new TestCaseData(InterfaceImplementsIAsyncOperation, WinRTRules.NonWinRTInterface).SetName("Inheritance. Interface implements IAsyncOperation");
                yield return new TestCaseData(InterfaceImplementsIAsyncOperation2, WinRTRules.NonWinRTInterface).SetName("Inheritance. Interface Implements IAsyncOperation in full");

                yield return new TestCaseData(ClassImplementsIAsyncOperationWithProgress, WinRTRules.NonWinRTInterface).SetName("Inheritance. Class implements IAsyncOperationWithProgress");
                yield return new TestCaseData(InterfaceImplementsIAsyncOperationWithProgress, WinRTRules.NonWinRTInterface).SetName("Inheritance. Interface Implements IAsyncOperationWithProgress");
                yield return new TestCaseData(InterfaceImplementsIAsyncOperationWithProgress2, WinRTRules.NonWinRTInterface).SetName("Inheritance. Interface Implements IAsyncOperationWithProgress in full");

                #endregion

                #region InOutAttribute
                yield return new TestCaseData(ArrayParamAttrUnary_4, WinRTRules.ArrayMarkedInOrOut).SetName("InOutAttribute. Array parameter marked [In]");
                yield return new TestCaseData(ArrayParamAttrUnary_5, WinRTRules.ArrayMarkedInOrOut).SetName("InOutAttribute. Array parameter marked [Out]");
                yield return new TestCaseData(ArrayParamAttrUnary_8, WinRTRules.NonArrayMarkedInOrOut).SetName("InOutAttribute. Non-array parameter marked [In] - unqualified");
                yield return new TestCaseData(ArrayParamAttrUnary_9, WinRTRules.NonArrayMarkedInOrOut).SetName("InOutAttribute. Non-array parameter marked [Out]");
                yield return new TestCaseData(ArrayParamAttrUnary_11, WinRTRules.NonArrayMarkedInOrOut).SetName("InOutAttribute. Non-array parameter marked [In]");
                yield return new TestCaseData(ArrayParamAttrUnary_12, WinRTRules.NonArrayMarkedInOrOut).SetName("InOutAttribute. TestArrayParamAttrUnary_12");
                yield return new TestCaseData(ArrayParamAttrUnary_13, WinRTRules.ArrayMarkedInOrOut).SetName("InOutAttribute. TestArrayParamAttrUnary_13");
                yield return new TestCaseData(ArrayParamAttrUnary_14, WinRTRules.ArrayMarkedInOrOut).SetName("InOutAttribute. TestArrayParamAttrUnary_14");
                yield return new TestCaseData(ArrayParamAttrBinary_4, WinRTRules.ArrayMarkedInOrOut).SetName("InOutAttribute. TestArrayParamAttrBinary_4");
                yield return new TestCaseData(ArrayParamAttrBinary_5, WinRTRules.ArrayMarkedInOrOut).SetName("InOutAttribute. TestArrayParamAttrBinary_5");
                yield return new TestCaseData(ArrayParamAttrBinary_8, WinRTRules.NonArrayMarkedInOrOut).SetName("InOutAttribute. TestArrayParamAttrBinary_8");
                yield return new TestCaseData(ArrayParamAttrBinary_9, WinRTRules.NonArrayMarkedInOrOut).SetName("InOutAttribute. TestArrayParamAttrBinary_9");
                yield return new TestCaseData(ArrayParamAttrBinary_14, WinRTRules.ArrayMarkedInOrOut).SetName("InOutAttribute. TestArrayParamAttrBinary_14");
                yield return new TestCaseData(ArrayParamAttrBinary_15, WinRTRules.ArrayMarkedInOrOut).SetName("InOutAttribute. Array marked [In] - second parameter");
                yield return new TestCaseData(ArrayParamAttrBinary_16, WinRTRules.ArrayMarkedInOrOut).SetName("InOutAttribute. Array marked [Out] - second parameter");
                yield return new TestCaseData(ArrayParamAttrBinary_21, WinRTRules.NonArrayMarkedInOrOut).SetName("InOutAttribute. Non array marked [In], with marked array");
                yield return new TestCaseData(ArrayParamAttrBinary_22, WinRTRules.NonArrayMarkedInOrOut).SetName("InOutAttribute. Non array marked [Out], with marked array, reverse");
                yield return new TestCaseData(ArrayParamAttrBinary_23, WinRTRules.NonArrayMarkedInOrOut).SetName("InOutAttribute. Non array marked [Out], with marked array");
                #endregion

                #region ArrayAccessAttribute
                yield return new TestCaseData(ArrayParamAttrUnary_1, WinRTRules.ArrayParamMarkedBoth).SetName("ArrayAttribute. Both marked, separate lists");
                yield return new TestCaseData(ArrayParamAttrUnary_2, WinRTRules.ArrayParamMarkedBoth).SetName("ArrayAttribute. Both marked, same list");
                yield return new TestCaseData(ArrayParamAttrUnary_3, WinRTRules.ArrayOutputParamMarkedRead).SetName("ArrayAttribute. ReadOnlyArray attribute on array out parameter");
                yield return new TestCaseData(ArrayParamAttrUnary_6, WinRTRules.NonArrayMarked).SetName("ArrayAttribute. Non-array parameter marked ReadOnlyArray");
                yield return new TestCaseData(ArrayParamAttrUnary_7, WinRTRules.NonArrayMarked).SetName("ArrayAttribute. Non-array parameter marked WriteOnlyArray");
                yield return new TestCaseData(ArrayParamAttrUnary_10, WinRTRules.ArrayParamNotMarked).SetName("ArrayAttribute. Array parameter not marked ReadOnlyArray or WriteOnlyArray");
                yield return new TestCaseData(ArrayParamAttrBinary_1, WinRTRules.ArrayParamMarkedBoth).SetName("ArrayAttribute. Both marked, second parameter, same list");
                yield return new TestCaseData(ArrayParamAttrBinary_2, WinRTRules.ArrayParamMarkedBoth).SetName("ArrayAttribute. Both marked, second parameter, separate list");
                yield return new TestCaseData(ArrayParamAttrBinary_3, WinRTRules.ArrayOutputParamMarkedRead).SetName("ArrayAttribute. Array `out` var marked ReadOnly");
                yield return new TestCaseData(ArrayParamAttrBinary_6, WinRTRules.NonArrayMarked).SetName("ArrayAttribute. Non-array parameter marked ReadOnly, second argument");
                yield return new TestCaseData(ArrayParamAttrBinary_7, WinRTRules.NonArrayMarked).SetName("ArrayAttribute. Non-array parameter marked WriteOnly, second argument");
                yield return new TestCaseData(ArrayParamAttrBinary_10, WinRTRules.ArrayParamNotMarked).SetName("ArrayAttribute. Array not marked, second argument");
                yield return new TestCaseData(ArrayParamAttrBinary_11, WinRTRules.ArrayParamMarkedBoth).SetName("ArrayAttribute. Marked both, second array");
                yield return new TestCaseData(ArrayParamAttrBinary_12, WinRTRules.ArrayParamMarkedBoth).SetName("ArrayAttribute. Marked both, first array");
                yield return new TestCaseData(ArrayParamAttrBinary_13, WinRTRules.ArrayOutputParamMarkedRead).SetName("ArrayAttribute. Marked out and read only, second argument");
                yield return new TestCaseData(ArrayParamAttrBinary_17, WinRTRules.ArrayParamNotMarked).SetName("ArrayAttribute. Array not marked - second argument");
                yield return new TestCaseData(ArrayParamAttrBinary_18, WinRTRules.NonArrayMarked).SetName("ArrayAttribute. Non array marked ReadOnlyArray");
                yield return new TestCaseData(ArrayParamAttrBinary_19, WinRTRules.NonArrayMarked).SetName("ArrayAttribute. Non array marked WriteOnlyArray");
                yield return new TestCaseData(ArrayParamAttrBinary_20, WinRTRules.NonArrayMarked).SetName("ArrayAttribute. Non array marked [In], with marked array, reverse");
                yield return new TestCaseData(ArrayParamAttrBinary_24, WinRTRules.ArrayParamNotMarked).SetName("ArrayAttribute. Array missing attribute");
                #endregion

                // name clash with params (__retval)
                yield return new TestCaseData(DunderRetValParam, WinRTRules.ParameterNamedValueRule).SetName("Misc. Parameter Name Conflict (__retval)");
                // operator overloading
                yield return new TestCaseData(OperatorOverload_Class, WinRTRules.OperatorOverloadedRule).SetName("Misc. Overload of Operator");
                // ref param
                yield return new TestCaseData(RefParam_ClassMethod, WinRTRules.RefParameterFound).SetName("Misc. Class Method With Ref Param");
                yield return new TestCaseData(RefParam_InterfaceMethod, WinRTRules.RefParameterFound).SetName("Misc. Interface Method With Ref Param");

                #region struct_field_tests
                yield return new TestCaseData(EmptyStruct, WinRTRules.StructWithNoFieldsRule).SetName("Struct. Empty struct");
                yield return new TestCaseData(StructWithInterfaceField, WinRTRules.StructHasInvalidFieldRule).SetName("Struct. with Interface field");
                yield return new TestCaseData(StructWithClassField, WinRTRules.StructHasInvalidFieldRule).SetName("Struct. with Class Field");
                yield return new TestCaseData(StructWithClassField2, WinRTRules.StructHasInvalidFieldRule).SetName("Struct. with Class Field2");
                yield return new TestCaseData(StructWithDelegateField, WinRTRules.StructHasInvalidFieldRule).SetName("Struct. with Delegate Field");
                yield return new TestCaseData(StructWithIndexer, WinRTRules.StructHasInvalidFieldRule).SetName("Struct. with Indexer Field");
                yield return new TestCaseData(StructWithMethods, WinRTRules.StructHasInvalidFieldRule).SetName("Struct. with Method Field");
                yield return new TestCaseData(StructWithConst, WinRTRules.StructHasConstFieldRule).SetName("Struct. with Const Field");
                yield return new TestCaseData(StructWithProperty, WinRTRules.StructHasInvalidFieldRule).SetName("Struct. with Property Field");
                yield return new TestCaseData(StructWithPrivateField, WinRTRules.StructHasPrivateFieldRule).SetName("Struct. with Private Field");
                yield return new TestCaseData(StructWithObjectField, WinRTRules.StructHasInvalidFieldRule).SetName("Struct. with Object Field");
                yield return new TestCaseData(StructWithDynamicField, WinRTRules.StructHasInvalidFieldRule).SetName("Struct. with Dynamic Field");
                yield return new TestCaseData(StructWithConstructor, WinRTRules.StructHasInvalidFieldRule).SetName("Struct. with Constructor Field");
                yield return new TestCaseData(StructWithPrimitiveTypesMissingPublicKeyword, WinRTRules.StructHasPrivateFieldRule).SetName("Struct. with missing public field");
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
                yield return new TestCaseData(Valid_UnrelatedNamespaceWithNoPublicTypes).SetName("Valid. Namespace. Helper namespace with no public types");
                yield return new TestCaseData(Valid_UnrelatedNamespaceWithNoPublicTypes2).SetName("Valid. Namespace. Helper namespace with partial types but aren't public");
                yield return new TestCaseData(Valid_SubNamespacesWithOverlappingNames).SetName("Valid. Namespace. Overlapping namesepaces names is OK if in different namespaces");
                yield return new TestCaseData(Valid_PrivateSetter).SetName("Valid. Property. Private Setter");
                yield return new TestCaseData(Valid_RollYourOwnAsyncAction).SetName("Valid. AsyncInterfaces. Implementing your own IAsyncAction");
                yield return new TestCaseData(Valid_CustomDictionary).SetName("Valid. CustomProjection. IDictionary<string,BasicStruct>");
                yield return new TestCaseData(Valid_CustomList).SetName("Valid. CustomProjection. IList<DisposableClass>");
                yield return new TestCaseData(Valid_TwoNamespacesSameName).SetName("Valid. Namespaces with same name");
                yield return new TestCaseData(Valid_NestedNamespace).SetName("Valid. Nested namespaces are fine");
                yield return new TestCaseData(Valid_NestedNamespace2).SetName("Valid. Twice nested namespaces are fine");
                //yield return new TestCaseData(Valid_NestedNamespace3).SetName("Valid. Namespace. Test[dot]Component with an inner namespace InnerComponent");
                //yield return new TestCaseData(Valid_NestedNamespace4).SetName("Valid. Namespace. Test and Test[dot]Component namespaces, latter with an inner namespace");
                //yield return new TestCaseData(Valid_NestedNamespace5).SetName("Valid. Namespace. ABCType in ABwinmd");
                yield return new TestCaseData(Valid_NamespacesDiffer).SetName("Valid. Similar namespace but different name (not just case)");
                yield return new TestCaseData(Valid_NamespaceAndPrefixedNamespace).SetName("Valid. Two top-level namespaces, one prefixed with the other");

                #region InvalidTypes_Signatures
                yield return new TestCaseData(Valid_ListUsage).SetName("Valid. Internally uses List<>");
                yield return new TestCaseData(Valid_ListUsage2).SetName("Valid. Internally uses List<> (qualified)");
                yield return new TestCaseData(Valid_ClassWithGenericDictReturnType_Private).SetName("Valid. Dictionary<> Private Method - ReturnType");
                yield return new TestCaseData(Valid_ClassWithGenericDictInput_Private).SetName("Valid. Dictionary<> Private Method - parameter");
                yield return new TestCaseData(Valid_ClassWithPrivateGenDictProp).SetName("Valid. Dictionary<> Private Property Class");
                yield return new TestCaseData(Valid_IfaceWithPrivateGenDictProp).SetName("Valid. Dictionary<> Private Property Interface");

                yield return new TestCaseData(Valid_ClassWithGenericRODictInput_Private).SetName("Valid. ReadOnlyDictionary<> Private Method ReturnType");
                yield return new TestCaseData(Valid_ClassWithGenericRODictReturnType_Private).SetName("Valid. ReadOnlyDictionary<> Private Method parameter");
                yield return new TestCaseData(Valid_ClassWithPrivateGenRODictProp).SetName("Valid. ReadOnlyDictionary<> Private Property Class");
                yield return new TestCaseData(Valid_IfaceWithPrivateGenRODictProp).SetName("Valid. ReadOnlyDictionary<> Private Property Interface");

                yield return new TestCaseData(Valid_InterfaceWithGenericKVPairReturnType).SetName("Valid. KeyValuePair<> as return type of signature in interface");
                yield return new TestCaseData(Valid_InterfaceWithGenericKVPairInput).SetName("Valid. KeyValuePair<> interface with Generic KeyValuePair parameter");
                yield return new TestCaseData(Valid_ClassWithGenericKVPairReturnType).SetName("Valid. KeyValuePair<> class with Generic KeyValuePair return type");
                yield return new TestCaseData(Valid_ClassWithGenericKVPairInput).SetName("Valid. KeyValuePair<> class with Generic KeyValuePair parameter");
                yield return new TestCaseData(Valid_IfaceWithGenKVPairProp).SetName("Valid. KeyValuePair<> interface with Generic KeyValuePair property");
                yield return new TestCaseData(Valid_ClassWithGenKVPairProp).SetName("Valid. KeyValuePair<> class with Generic KeyValuePair return property");

                yield return new TestCaseData(Valid_ClassWithGenericKVPairInput_Private).SetName("Valid. KeyValuePair<> as parameter to private method");
                yield return new TestCaseData(Valid_ClassWithGenericKVPairReturnType_Private).SetName("Valid. KeyValuePair<> as return type of private method");
                yield return new TestCaseData(Valid_ClassWithPrivateGenKVPairProp).SetName("Valid. KeyValuePair<> as private prop to class");
                yield return new TestCaseData(Valid_IfaceWithPrivateGenKVPairProp).SetName("Valid. KeyValuePair<> as private prop to interface");

                yield return new TestCaseData(Valid_ClassWithGenericEnumerableInput_Private).SetName("Valid. Enumerable as parameter to private method");
                yield return new TestCaseData(Valid_ClassWithGenericEnumerableReturnType_Private).SetName("Valid. Enumerable as return type of private method");
                yield return new TestCaseData(Valid_ClassWithPrivateGenEnumerableProp).SetName("Valid. Enumerable as private prop to class");
                yield return new TestCaseData(Valid_IfaceWithPrivateGenEnumerableProp).SetName("Valid. Enumerable as private prop to interface");

                yield return new TestCaseData(Valid_ClassWithGenericListInput_Private).SetName("Valid. List<> as parameter to private method");
                yield return new TestCaseData(Valid_ClassWithGenericListReturnType_Private).SetName("Valid. List<> as return type of private method");
                yield return new TestCaseData(Valid_ClassWithPrivateGenListProp).SetName("Valid. List<> as private prop to class");
                yield return new TestCaseData(Valid_IfaceWithPrivateGenListProp).SetName("Valid. List<> as private prop to interface");

                #endregion

                #region ArrayAccessAttribute 
                // ReadOnlyArray / WriteOnlyArray Attribute
                yield return new TestCaseData(Valid_ArrayParamAttrUnary_1).SetName("Valid. ArrayAttribute, Array marked read only");
                yield return new TestCaseData(Valid_ArrayParamAttrUnary_2).SetName("Valid. ArrayAttribute, Array marked write only");
                yield return new TestCaseData(Valid_ArrayParamAttrUnary_3).SetName("Valid. ArrayAttribute, Array marked out and write only");
                yield return new TestCaseData(Valid_ArrayParamAttrUnary_4).SetName("Valid. ArrayAttribute, Array marked out");
                yield return new TestCaseData(Valid_ArrayParamAttrBinary_1).SetName("Valid. ArrayAttribute, Array marked read only, second parameter");
                yield return new TestCaseData(Valid_ArrayParamAttrBinary_2).SetName("Valid. ArrayAttribute, Array marked write only, second parameter");
                yield return new TestCaseData(Valid_ArrayParamAttrBinary_3).SetName("Valid. ArrayAttribute, Array marked write only and out, second parameter");
                yield return new TestCaseData(Valid_ArrayParamAttrBinary_4).SetName("Valid. ArrayAttribute, Array marked out, second parameter");
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
                yield return new TestCaseData(Valid_StructWithEnumField).SetName("Valid. Struct with enum field");
                #endregion

                #region InvalidArrayTypes_Signatures

                // SystemArray  
                yield return new TestCaseData(Valid_SystemArrayProperty).SetName("Valid. SystemArray private property");
                yield return new TestCaseData(Valid_SystemArray_Interface1).SetName("Valid. System Array internal interface 1");
                yield return new TestCaseData(Valid_SystemArray_Interface2).SetName("Valid. System Array internal interface 2");
                yield return new TestCaseData(Valid_SystemArray_Interface3).SetName("Valid. System Array internal interface 3");
                yield return new TestCaseData(Valid_SystemArray_InternalClass1).SetName("Valid. System Array internal class 1");
                yield return new TestCaseData(Valid_SystemArray_InternalClass2).SetName("Valid. System Array internal class 2");
                yield return new TestCaseData(Valid_SystemArray_InternalClass3).SetName("Valid. System Array internal class 3");
                yield return new TestCaseData(Valid_SystemArray_InternalClass4).SetName("Valid. System Array internal class 4");
                yield return new TestCaseData(Valid_SystemArray_PublicClassPrivateProperty1).SetName("Valid. System Array public class / private property 1");
                yield return new TestCaseData(Valid_SystemArray_PublicClassPrivateProperty2).SetName("Valid. System Array public class / private property 2");
                yield return new TestCaseData(Valid_SystemArray_PublicClassPrivateProperty3).SetName("Valid. System Array public class / private property 3");
                yield return new TestCaseData(Valid_SystemArray_PublicClassPrivateProperty4).SetName("Valid. System Array public class / private property 4");
                yield return new TestCaseData(Valid_SystemArray_PublicClassPrivateProperty5).SetName("Valid. System Array public class / private property 5");
                yield return new TestCaseData(Valid_SystemArray_PublicClassPrivateProperty6).SetName("Valid. System Array public class / private property 6");
                yield return new TestCaseData(Valid_SystemArray_InternalClassPublicMethods1).SetName("Valid. System Array internal class / public method 1");
                yield return new TestCaseData(Valid_SystemArray_InternalClassPublicMethods2).SetName("Valid. System Array internal class / public method 2");
                yield return new TestCaseData(Valid_SystemArray_InternalClassPublicMethods3).SetName("Valid. System Array internal class / public method 3");
                yield return new TestCaseData(Valid_SystemArray_InternalClassPublicMethods4).SetName("Valid. System Array internal class / public method 4");
                yield return new TestCaseData(Valid_SystemArray_InternalClassPublicMethods5).SetName("Valid. System Array internal class / public method 5");
                yield return new TestCaseData(Valid_SystemArray_InternalClassPublicMethods6).SetName("Valid. System Array internal class / public method 6");
                yield return new TestCaseData(Valid_SystemArray_PrivateClassPublicProperty1).SetName("Valid. System Array internal class / public property 1");
                yield return new TestCaseData(Valid_SystemArray_PrivateClassPublicProperty2).SetName("Valid. System Array internal class / public property 2");
                yield return new TestCaseData(Valid_SystemArray_PrivateClassPublicProperty3).SetName("Valid. System Array internal class / public property 3");
                yield return new TestCaseData(Valid_SystemArray_PrivateClassPublicProperty4).SetName("Valid. System Array internal class / public property 4");
                yield return new TestCaseData(Valid_SystemArray_PrivateClassPublicProperty5).SetName("Valid. System Array internal class / public property 5");
                yield return new TestCaseData(Valid_SystemArray_PrivateClassPublicProperty6).SetName("Valid. System Array internal class / public property 6");
                yield return new TestCaseData(Valid_SystemArray_PrivateClassPublicProperty7).SetName("Valid. System Array internal class / public property 7");
                yield return new TestCaseData(Valid_SystemArray_PrivateClassPublicProperty8).SetName("Valid. System Array internal class / public property 8");
                yield return new TestCaseData(Valid_SystemArrayPublicClassPrivateMethod1).SetName("Valid. System Array public class / private method 1");
                yield return new TestCaseData(Valid_SystemArrayPublicClassPrivateMethod2).SetName("Valid. System Array public class / private method 2");
                yield return new TestCaseData(Valid_SystemArrayPublicClassPrivateMethod3).SetName("Valid. System Array public class / private method 3");
                yield return new TestCaseData(Valid_SystemArrayPublicClassPrivateMethod4).SetName("Valid. System Array public class / private method 4");
                yield return new TestCaseData(Valid_SystemArrayPublicClassPrivateMethod5).SetName("Valid. System Array public class / private method 5");
                yield return new TestCaseData(Valid_SystemArrayPublicClassPrivateMethod6).SetName("Valid. System Array public class / private method 6");
                // multi dim array tests 
                yield return new TestCaseData(Valid_MultiDimArray_PublicClassPrivateMethod1).SetName("Valid. array [,] public class / private method 1");
                yield return new TestCaseData(Valid_MultiDimArray_PublicClassPrivateMethod2).SetName("Valid. array [,] public class / private method 2");
                yield return new TestCaseData(Valid_MultiDimArray_PublicClassPrivateMethod3).SetName("Valid. array [,] public class / private method 3");
                yield return new TestCaseData(Valid_MultiDimArray_PublicClassPrivateMethod4).SetName("Valid. array [,] public class / private method 4");
                yield return new TestCaseData(Valid_MultiDimArray_PublicClassPrivateMethod5).SetName("Valid. array [,,] public class / private method 5");
                yield return new TestCaseData(Valid_MultiDimArray_PublicClassPrivateMethod6).SetName("Valid. array [,,] public class / private method 6");

                yield return new TestCaseData(Valid_3D_PrivateClass_PublicMethod1).SetName("Valid. MultiDim 3D private class / public method 1");
                yield return new TestCaseData(Valid_3D_PrivateClass_PublicMethod2).SetName("Valid. MultiDim 3D private class / public method 2");
                yield return new TestCaseData(Valid_3D_PrivateClass_PublicMethod3).SetName("Valid. MultiDim 3D private class / public method 3");
                yield return new TestCaseData(Valid_3D_PrivateClass_PublicMethod4).SetName("Valid. MultiDim 3D private class / public method 4");
                yield return new TestCaseData(Valid_3D_PrivateClass_PublicMethod5).SetName("Valid. MultiDim 3D private class / public method 5");
                yield return new TestCaseData(Valid_3D_PrivateClass_PublicMethod6).SetName("Valid. MultiDim 3D private class / public method 6");

                yield return new TestCaseData(Valid_2D_PrivateClass_PublicMethod1).SetName("Valid. MultiDim 2D private class / public method 1");
                yield return new TestCaseData(Valid_2D_PrivateClass_PublicMethod2).SetName("Valid. MultiDim 2D private class / public method 2");
                yield return new TestCaseData(Valid_2D_PrivateClass_PublicMethod3).SetName("Valid. MultiDim 2D private class / public method 3");
                yield return new TestCaseData(Valid_2D_PrivateClass_PublicMethod4).SetName("Valid. MultiDim 2D private class / public method 4");
                yield return new TestCaseData(Valid_2D_PrivateClass_PublicMethod5).SetName("Valid. MultiDim 2D private class / public method 5");
                yield return new TestCaseData(Valid_2D_PrivateClass_PublicMethod6).SetName("Valid.  MultiDim 2D private class / public method 6");

                yield return new TestCaseData(Valid_MultiDimArray_PrivateClassPublicProperty1).SetName("Valid. MultiDim 2D private class / public property 1");
                yield return new TestCaseData(Valid_MultiDimArray_PrivateClassPublicProperty2).SetName("Valid. MultiDim 2D private class / public property 2");
                yield return new TestCaseData(Valid_MultiDimArray_PrivateClassPublicProperty3).SetName("Valid. MultiDim 2D private class / public property 3");
                yield return new TestCaseData(Valid_MultiDimArray_PrivateClassPublicProperty4).SetName("Valid. MultiDim 2D private class / public property 4");
                yield return new TestCaseData(Valid_MultiDimArray_PublicClassPrivateProperty1).SetName("Valid. MultiDim 2D public class / private property 1");
                yield return new TestCaseData(Valid_MultiDimArray_PublicClassPrivateProperty2).SetName("Valid. MultiDim 2D public class / private property 2");
                // jagged array tests
                yield return new TestCaseData(Valid_JaggedMix_PrivateClassPublicProperty).SetName("Valid. Jagged Array private class / private property");
                yield return new TestCaseData(Valid_Jagged2D_PrivateClassPublicMethods).SetName("Valid. Jagged Array private class / public method");
                yield return new TestCaseData(Valid_Jagged3D_PrivateClassPublicMethods).SetName("Valid. Jagged Array private class / public method");
                yield return new TestCaseData(Valid_Jagged3D_PublicClassPrivateMethods).SetName("Valid. Jagged Array public class / private method");
                yield return new TestCaseData(Valid_Jagged2D_Property).SetName("Valid. Jagged 2D Array public property");
                yield return new TestCaseData(Valid_Jagged3D_Property).SetName("Valid. Jagged 3D Array public property");

                #endregion

                #region   DefaultOverloadAttribute

                // overload attributes
                yield return new TestCaseData(Valid_InterfaceWithOverloadAttribute).SetName("Valid. interface with overloads and one marked as default");
                yield return new TestCaseData(Valid_TwoOverloads_DiffParamCount).SetName("Valid. DefaultOverload attribute 1");
                yield return new TestCaseData(Valid_TwoOverloads_OneAttribute_OneInList).SetName("Valid. DefaultOverload attribute 2");
                yield return new TestCaseData(Valid_TwoOverloads_OneAttribute_OneIrrelevatAttribute).SetName("Valid. DefaultOverload attribute 3");
                yield return new TestCaseData(Valid_TwoOverloads_OneAttribute_TwoLists).SetName("Valid. DefaultOverload attribute 4");
                yield return new TestCaseData(Valid_ThreeOverloads_OneAttribute).SetName("Valid. DefaultOverload attribute 5");
                yield return new TestCaseData(Valid_ThreeOverloads_OneAttribute_2).SetName("Valid. DefaultOverload attribute 6");
                yield return new TestCaseData(Valid_TwoOverloads_OneAttribute_3).SetName("Valid. DefaultOverload attribute 7");

                #endregion
            }
        }

        #endregion

    }
}