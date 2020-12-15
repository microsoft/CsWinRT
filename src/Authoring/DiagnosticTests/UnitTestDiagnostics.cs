using NUnit.Framework;
using Microsoft.CodeAnalysis;
using WinRT.SourceGenerator;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.Linq;

namespace DiagnosticTests
{
    [TestFixture]
    public partial class TestDiagnostics
    {

        /// <summary>
        /// CheckNoDiagnostic asserts that no diagnostics are raised on the 
        /// compilation produced from the cswinrt source generator based on the given source code
        /// 
        /// Add unit tests by creating a source code like this:
        /// private const string MyNewTest = @"namespace Test { ... }";
        /// 
        /// And have a DiagnosticDescriptor for the one to check for, they live in WinRT.SourceGenerator.DiagnosticRules
        /// 
        /// Then go to the DiagnosticValidData class here and add an entry for it
        /// </summary>
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
                // multi-dimensional array tests
                yield return new TestCaseData(MultiDim_2DProp, DiagnosticRules.ArraySignature_MultiDimensionalArrayRule).SetName("MultiDim 2D Array Property");
                yield return new TestCaseData(MultiDim_3DProp, DiagnosticRules.ArraySignature_MultiDimensionalArrayRule).SetName("MultiDim 3D Array Property");
                
                yield return new TestCaseData(MultiDim_2D_PublicClassPublicMethod1, DiagnosticRules.ArraySignature_MultiDimensionalArrayRule).SetName("MultiDim 2D Class Method 1");
                yield return new TestCaseData(MultiDim_2D_PublicClassPublicMethod2, DiagnosticRules.ArraySignature_MultiDimensionalArrayRule).SetName("MultiDim 2D Class Method 2");
                yield return new TestCaseData(MultiDim_2D_PublicClassPublicMethod3, DiagnosticRules.ArraySignature_MultiDimensionalArrayRule).SetName("MultiDim 2D Class Method 3");
                yield return new TestCaseData(MultiDim_2D_PublicClassPublicMethod4, DiagnosticRules.ArraySignature_MultiDimensionalArrayRule).SetName("MultiDim 2D Class Method 4");
                yield return new TestCaseData(MultiDim_2D_PublicClassPublicMethod5, DiagnosticRules.ArraySignature_MultiDimensionalArrayRule).SetName("MultiDim 2D Class Method 5");
                yield return new TestCaseData(MultiDim_2D_PublicClassPublicMethod6, DiagnosticRules.ArraySignature_MultiDimensionalArrayRule).SetName("MultiDim 2D Class Method 6");

                yield return new TestCaseData(MultiDim_3D_PublicClassPublicMethod1, DiagnosticRules.ArraySignature_MultiDimensionalArrayRule).SetName("MultiDim 3D Class Method 1");
                yield return new TestCaseData(MultiDim_3D_PublicClassPublicMethod2, DiagnosticRules.ArraySignature_MultiDimensionalArrayRule).SetName("MultiDim 3D Class Method 2");
                yield return new TestCaseData(MultiDim_3D_PublicClassPublicMethod3, DiagnosticRules.ArraySignature_MultiDimensionalArrayRule).SetName("MultiDim 3D Class Method 3");
                yield return new TestCaseData(MultiDim_3D_PublicClassPublicMethod4, DiagnosticRules.ArraySignature_MultiDimensionalArrayRule).SetName("MultiDim 3D Class Method 4");
                yield return new TestCaseData(MultiDim_3D_PublicClassPublicMethod5, DiagnosticRules.ArraySignature_MultiDimensionalArrayRule).SetName("MultiDim 3D Class Method 5");
                yield return new TestCaseData(MultiDim_3D_PublicClassPublicMethod6, DiagnosticRules.ArraySignature_MultiDimensionalArrayRule).SetName("MultiDim 3D Class Method 6");
                
                yield return new TestCaseData(MultiDim_2D_Interface1, DiagnosticRules.ArraySignature_MultiDimensionalArrayRule).SetName("MultiDim 2D Interface Method 1");
                yield return new TestCaseData(MultiDim_2D_Interface2, DiagnosticRules.ArraySignature_MultiDimensionalArrayRule).SetName("MultiDim 2D Interface Method 2");
                yield return new TestCaseData(MultiDim_2D_Interface3, DiagnosticRules.ArraySignature_MultiDimensionalArrayRule).SetName("MultiDim 2D Interface Method 3");
                yield return new TestCaseData(MultiDim_2D_Interface4, DiagnosticRules.ArraySignature_MultiDimensionalArrayRule).SetName("MultiDim 2D Interface Method 4");
                yield return new TestCaseData(MultiDim_2D_Interface5, DiagnosticRules.ArraySignature_MultiDimensionalArrayRule).SetName("MultiDim 2D Interface Method 5");
                yield return new TestCaseData(MultiDim_2D_Interface6, DiagnosticRules.ArraySignature_MultiDimensionalArrayRule).SetName("MultiDim 2D Interface Method 6");
              
                yield return new TestCaseData(MultiDim_3D_Interface1, DiagnosticRules.ArraySignature_MultiDimensionalArrayRule).SetName("MultiDim 3D Interface Method 1");
                yield return new TestCaseData(MultiDim_3D_Interface2, DiagnosticRules.ArraySignature_MultiDimensionalArrayRule).SetName("MultiDim 3D Interface Method 2");
                yield return new TestCaseData(MultiDim_3D_Interface3, DiagnosticRules.ArraySignature_MultiDimensionalArrayRule).SetName("MultiDim 3D Interface Method 3");
                yield return new TestCaseData(MultiDim_3D_Interface4, DiagnosticRules.ArraySignature_MultiDimensionalArrayRule).SetName("MultiDim 3D Interface Method 4");
                yield return new TestCaseData(MultiDim_3D_Interface5, DiagnosticRules.ArraySignature_MultiDimensionalArrayRule).SetName("MultiDim 3D Interface Method 5");
                yield return new TestCaseData(MultiDim_3D_Interface6, DiagnosticRules.ArraySignature_MultiDimensionalArrayRule).SetName("MultiDim 3D Interface Method 6");
                yield return new TestCaseData(SubNamespaceInterface_D2Method1, DiagnosticRules.ArraySignature_MultiDimensionalArrayRule)
                    .SetName("MultiDim 2D Subnamespace Interface Method 1");
                yield return new TestCaseData(SubNamespaceInterface_D2Method2, DiagnosticRules.ArraySignature_MultiDimensionalArrayRule)
                    .SetName("MultiDim 2D Subnamespace Interface Method 2");
                yield return new TestCaseData(SubNamespaceInterface_D2Method3, DiagnosticRules.ArraySignature_MultiDimensionalArrayRule)
                    .SetName("MultiDim 2D Subnamespace Interface Method 3");
                yield return new TestCaseData(SubNamespaceInterface_D2Method4, DiagnosticRules.ArraySignature_MultiDimensionalArrayRule)
                    .SetName("MultiDim 2D Subnamespace Interface Method 4");
                yield return new TestCaseData(SubNamespaceInterface_D2Method5, DiagnosticRules.ArraySignature_MultiDimensionalArrayRule)
                    .SetName("MultiDim 2D Subnamespace Interface Method 5");
                yield return new TestCaseData(SubNamespaceInterface_D2Method6, DiagnosticRules.ArraySignature_MultiDimensionalArrayRule)
                    .SetName("MultiDim 2D Subnamespace Interface Method 6");
                yield return new TestCaseData(SubNamespaceInterface_D3Method1, DiagnosticRules.ArraySignature_MultiDimensionalArrayRule)
                    .SetName("MultiDim 3D Subnamespace Interface Method 1");
                yield return new TestCaseData(SubNamespaceInterface_D3Method2, DiagnosticRules.ArraySignature_MultiDimensionalArrayRule)
                    .SetName("MultiDim 3D Subnamespace Interface Method 2");
                yield return new TestCaseData(SubNamespaceInterface_D3Method3, DiagnosticRules.ArraySignature_MultiDimensionalArrayRule)
                    .SetName("MultiDim 3D Subnamespace Interface Method 3");
                yield return new TestCaseData(SubNamespaceInterface_D3Method4, DiagnosticRules.ArraySignature_MultiDimensionalArrayRule)
                    .SetName("MultiDim 3D Subnamespace Interface Method 4");
                yield return new TestCaseData(SubNamespaceInterface_D3Method5, DiagnosticRules.ArraySignature_MultiDimensionalArrayRule)
                    .SetName("MultiDim 3D Subnamespace Interface Method 5");
                yield return new TestCaseData(SubNamespaceInterface_D3Method6, DiagnosticRules.ArraySignature_MultiDimensionalArrayRule)
                    .SetName("MultiDim 3D Subnamespace Interface Method 6");


                // jagged array tests 
                yield return new TestCaseData(Jagged2D_Property2, DiagnosticRules.ArraySignature_JaggedArrayRule).SetName("Jagged 2D Array Property 2");
                yield return new TestCaseData(Jagged3D_Property1, DiagnosticRules.ArraySignature_JaggedArrayRule).SetName("Jagged 3D Array Property 1");
                yield return new TestCaseData(Jagged2D_ClassMethod1, DiagnosticRules.ArraySignature_JaggedArrayRule).SetName("Jagged 2D Array Class Method 1");
                yield return new TestCaseData(Jagged2D_ClassMethod2, DiagnosticRules.ArraySignature_JaggedArrayRule).SetName("Jagged 2D Array Class Method 2");
                yield return new TestCaseData(Jagged2D_ClassMethod3, DiagnosticRules.ArraySignature_JaggedArrayRule).SetName("Jagged 2D Array Class Method 3");
                yield return new TestCaseData(Jagged2D_ClassMethod4, DiagnosticRules.ArraySignature_JaggedArrayRule).SetName("Jagged 2D Array Class Method 4");
                yield return new TestCaseData(Jagged2D_ClassMethod5, DiagnosticRules.ArraySignature_JaggedArrayRule).SetName("Jagged 2D Array Class Method 5");
                yield return new TestCaseData(Jagged2D_ClassMethod6, DiagnosticRules.ArraySignature_JaggedArrayRule).SetName("Jagged 2D Array Class Method 6");
                yield return new TestCaseData(Jagged3D_ClassMethod1, DiagnosticRules.ArraySignature_JaggedArrayRule).SetName("Jagged 3D Array Class Method 1");
                yield return new TestCaseData(Jagged3D_ClassMethod2, DiagnosticRules.ArraySignature_JaggedArrayRule).SetName("Jagged 3D Array Class Method 2");
                yield return new TestCaseData(Jagged3D_ClassMethod3, DiagnosticRules.ArraySignature_JaggedArrayRule).SetName("Jagged 3D Array Class Method 3");
                yield return new TestCaseData(Jagged3D_ClassMethod4, DiagnosticRules.ArraySignature_JaggedArrayRule).SetName("Jagged 3D Array Class Method 4");
                yield return new TestCaseData(Jagged3D_ClassMethod5, DiagnosticRules.ArraySignature_JaggedArrayRule).SetName("Jagged 3D Array Class Method 5");
                yield return new TestCaseData(Jagged3D_ClassMethod6, DiagnosticRules.ArraySignature_JaggedArrayRule).SetName("Jagged 3D Array Class Method 6");
                yield return new TestCaseData(Jagged2D_InterfaceMethod1, DiagnosticRules.ArraySignature_JaggedArrayRule).SetName("Jagged 2D Array Interface Method 1");
                yield return new TestCaseData(Jagged2D_InterfaceMethod2, DiagnosticRules.ArraySignature_JaggedArrayRule).SetName("Jagged 2D Array Interface Method 2");
                yield return new TestCaseData(Jagged2D_InterfaceMethod3, DiagnosticRules.ArraySignature_JaggedArrayRule).SetName("Jagged 2D Array Interface Method 3");
                yield return new TestCaseData(Jagged2D_InterfaceMethod4, DiagnosticRules.ArraySignature_JaggedArrayRule).SetName("Jagged 2D Array Interface Method 4");
                yield return new TestCaseData(Jagged2D_InterfaceMethod5, DiagnosticRules.ArraySignature_JaggedArrayRule).SetName("Jagged 2D Array Interface Method 5");
                yield return new TestCaseData(Jagged2D_InterfaceMethod6, DiagnosticRules.ArraySignature_JaggedArrayRule).SetName("Jagged 2D Array Interface Method 6");
                yield return new TestCaseData(Jagged3D_InterfaceMethod1, DiagnosticRules.ArraySignature_JaggedArrayRule).SetName("Jagged 3D Array Interface Method 1");
                yield return new TestCaseData(Jagged3D_InterfaceMethod2, DiagnosticRules.ArraySignature_JaggedArrayRule).SetName("Jagged 3D Array Interface Method 2");
                yield return new TestCaseData(Jagged3D_InterfaceMethod3, DiagnosticRules.ArraySignature_JaggedArrayRule).SetName("Jagged 3D Array Interface Method 3");
                yield return new TestCaseData(Jagged3D_InterfaceMethod4, DiagnosticRules.ArraySignature_JaggedArrayRule).SetName("Jagged 3D Array Interface Method 4");
                yield return new TestCaseData(Jagged3D_InterfaceMethod5, DiagnosticRules.ArraySignature_JaggedArrayRule).SetName("Jagged 3D Array Interface Method 5");
                yield return new TestCaseData(Jagged3D_InterfaceMethod6, DiagnosticRules.ArraySignature_JaggedArrayRule).SetName("Jagged 3D Array Interface Method 6");
                yield return new TestCaseData(SubNamespace_Jagged2DInterface1, DiagnosticRules.ArraySignature_JaggedArrayRule).SetName("Jagged 2D Array SubNamespace Interface Method 1");
                yield return new TestCaseData(SubNamespace_Jagged2DInterface2, DiagnosticRules.ArraySignature_JaggedArrayRule).SetName("Jagged 2D Array SubNamespace Interface Method 2");
                yield return new TestCaseData(SubNamespace_Jagged2DInterface3, DiagnosticRules.ArraySignature_JaggedArrayRule).SetName("Jagged 2D Array SubNamespace Interface Method 3");
                yield return new TestCaseData(SubNamespace_Jagged2DInterface4, DiagnosticRules.ArraySignature_JaggedArrayRule).SetName("Jagged 2D Array SubNamespace Interface Method 4");
                yield return new TestCaseData(SubNamespace_Jagged2DInterface5, DiagnosticRules.ArraySignature_JaggedArrayRule).SetName("Jagged 2D Array SubNamespace Interface Method 5");
                yield return new TestCaseData(SubNamespace_Jagged2DInterface6, DiagnosticRules.ArraySignature_JaggedArrayRule).SetName("Jagged 2D Array SubNamespace Interface Method 6");
                yield return new TestCaseData(SubNamespace_Jagged3DInterface1, DiagnosticRules.ArraySignature_JaggedArrayRule).SetName("Jagged 3D Array SubNamespace Interface Method 1");
                yield return new TestCaseData(SubNamespace_Jagged3DInterface2, DiagnosticRules.ArraySignature_JaggedArrayRule).SetName("Jagged 3D Array SubNamespace Interface Method 2");
                yield return new TestCaseData(SubNamespace_Jagged3DInterface3, DiagnosticRules.ArraySignature_JaggedArrayRule).SetName("Jagged 3D Array SubNamespace Interface Method 3");
                yield return new TestCaseData(SubNamespace_Jagged3DInterface4, DiagnosticRules.ArraySignature_JaggedArrayRule).SetName("Jagged 3D Array SubNamespace Interface Method 4");
                yield return new TestCaseData(SubNamespace_Jagged3DInterface5, DiagnosticRules.ArraySignature_JaggedArrayRule).SetName("Jagged 3D Array SubNamespace Interface Method 5");
                yield return new TestCaseData(SubNamespace_Jagged3DInterface6, DiagnosticRules.ArraySignature_JaggedArrayRule).SetName("Jagged 3D Array SubNamespace Interface Method 6");

                // overload attribute tests
                yield return new TestCaseData(TwoOverloads_NoAttribute, DiagnosticRules.MethodOverload_NeedDefaultAttribute)
                    .SetName("DefaultOverload - Need Attribute 1");
                yield return new TestCaseData(TwoOverloads_NoAttribute_OneIrrevAttr, DiagnosticRules.MethodOverload_NeedDefaultAttribute)
                    .SetName("DefaultOverload - Need Attribute 2");
                yield return new TestCaseData(TwoOverloads_TwoAttribute_OneInList, DiagnosticRules.MethodOverload_MultipleDefaultAttribute)
                    .SetName("DefaultOverload - Multiple Attribute 1");
                yield return new TestCaseData(TwoOverloads_TwoAttribute_BothInList, DiagnosticRules.MethodOverload_MultipleDefaultAttribute)
                    .SetName("DefaultOverload - Multiple Attribute 2");
                yield return new TestCaseData(TwoOverloads_TwoAttribute_TwoLists, DiagnosticRules.MethodOverload_MultipleDefaultAttribute)
                    .SetName("DefaultOverload - Multiple Attribute 3");
                yield return new TestCaseData(TwoOverloads_TwoAttribute_OneInSeparateList_OneNot, DiagnosticRules.MethodOverload_MultipleDefaultAttribute)
                    .SetName("DefaultOverload - Multiple Attribute 4");
                yield return new TestCaseData(TwoOverloads_TwoAttribute_BothInSeparateList, DiagnosticRules.MethodOverload_MultipleDefaultAttribute)
                    .SetName("DefaultOverload - Multiple Attribute 5");
                yield return new TestCaseData(TwoOverloads_TwoAttribute, DiagnosticRules.MethodOverload_MultipleDefaultAttribute)
                    .SetName("DefaultOverload - Multiple Attribute 6");
                yield return new TestCaseData(ThreeOverloads_TwoAttributes, DiagnosticRules.MethodOverload_MultipleDefaultAttribute)
                    .SetName("DefaultOverload - Multiple Attribute 7");
               
                // .......................................................................................................................................
                // multiple class constructors of same arity
                yield return new TestCaseData(ConstructorsOfSameArity, DiagnosticRules.ClassConstructorRule).SetName("Multiple constructors of same arity");
                // implementing async interface
                yield return new TestCaseData(ImplementsIAsyncOperation, DiagnosticRules.AsyncRule).SetName("Implements IAsyncOperation");
                yield return new TestCaseData(ImplementsIAsyncOperationWithProgress, DiagnosticRules.AsyncRule).SetName("Implements IAsyncOperationWithProgress");
                yield return new TestCaseData(ImplementsIAsyncAction, DiagnosticRules.AsyncRule).SetName("Implements IAsyncAction");
                yield return new TestCaseData(ImplementsIAsyncActionWithProgress, DiagnosticRules.AsyncRule).SetName("Implements IAsyncActionWithProgress");
                // readonly/writeonlyArray attribute
                yield return new TestCaseData(TestArrayParamAttrUnary_1, DiagnosticRules.ArrayParamMarkedBoth).SetName("TestArrayParamAttrUnary_1");
                yield return new TestCaseData(TestArrayParamAttrUnary_2, DiagnosticRules.ArrayParamMarkedBoth).SetName("TestArrayParamAttrUnary_2");
                yield return new TestCaseData(TestArrayParamAttrUnary_3, DiagnosticRules.ArrayOutputParamMarkedRead).SetName("TestArrayParamAttrUnary_3");
                yield return new TestCaseData(TestArrayParamAttrUnary_4, DiagnosticRules.ArrayMarkedInOrOut).SetName("TestArrayParamAttrUnary_4");
                yield return new TestCaseData(TestArrayParamAttrUnary_5, DiagnosticRules.ArrayMarkedInOrOut).SetName("TestArrayParamAttrUnary_5");
                yield return new TestCaseData(TestArrayParamAttrUnary_6, DiagnosticRules.NonArrayMarked).SetName("TestArrayParamAttrUnary_6");
                yield return new TestCaseData(TestArrayParamAttrUnary_7, DiagnosticRules.NonArrayMarked).SetName("TestArrayParamAttrUnary_7");
                yield return new TestCaseData(TestArrayParamAttrUnary_8, DiagnosticRules.NonArrayMarkedInOrOut).SetName("TestArrayParamAttrUnary_8");
                yield return new TestCaseData(TestArrayParamAttrUnary_9, DiagnosticRules.NonArrayMarkedInOrOut).SetName("TestArrayParamAttrUnary_9");
                yield return new TestCaseData(TestArrayParamAttrUnary_10, DiagnosticRules.ArrayParamNotMarked).SetName("TestArrayParamAttrUnary_10");
                yield return new TestCaseData(TestArrayParamAttrBinary_1, DiagnosticRules.ArrayParamMarkedBoth).SetName("TestArrayParamAttrBinary_1");
                yield return new TestCaseData(TestArrayParamAttrBinary_2, DiagnosticRules.ArrayParamMarkedBoth).SetName("TestArrayParamAttrBinary_2");
                yield return new TestCaseData(TestArrayParamAttrBinary_3, DiagnosticRules.ArrayOutputParamMarkedRead).SetName("TestArrayParamAttrBinary_3");
                yield return new TestCaseData(TestArrayParamAttrBinary_4, DiagnosticRules.ArrayMarkedInOrOut).SetName("TestArrayParamAttrBinary_4");
                yield return new TestCaseData(TestArrayParamAttrBinary_5, DiagnosticRules.ArrayMarkedInOrOut).SetName("TestArrayParamAttrBinary_5");
                yield return new TestCaseData(TestArrayParamAttrBinary_6, DiagnosticRules.NonArrayMarked).SetName("TestArrayParamAttrBinary_6");
                yield return new TestCaseData(TestArrayParamAttrBinary_7, DiagnosticRules.NonArrayMarked).SetName("TestArrayParamAttrBinary_7");
                yield return new TestCaseData(TestArrayParamAttrBinary_8, DiagnosticRules.NonArrayMarkedInOrOut).SetName("TestArrayParamAttrBinary_8");
                yield return new TestCaseData(TestArrayParamAttrBinary_9, DiagnosticRules.NonArrayMarkedInOrOut).SetName("TestArrayParamAttrBinary_9");
                yield return new TestCaseData(TestArrayParamAttrBinary_10, DiagnosticRules.ArrayParamNotMarked).SetName("TestArrayParamAttrBinary_10");
                yield return new TestCaseData(TestArrayParamAttrBinary_11, DiagnosticRules.ArrayParamMarkedBoth).SetName("TestArrayParamAttrBinary_11");
                yield return new TestCaseData(TestArrayParamAttrBinary_12, DiagnosticRules.ArrayParamMarkedBoth).SetName("TestArrayParamAttrBinary_12");
                yield return new TestCaseData(TestArrayParamAttrBinary_13, DiagnosticRules.ArrayOutputParamMarkedRead).SetName("TestArrayParamAttrBinary_13");
                yield return new TestCaseData(TestArrayParamAttrBinary_14, DiagnosticRules.ArrayMarkedInOrOut).SetName("TestArrayParamAttrBinary_14");
                yield return new TestCaseData(TestArrayParamAttrBinary_15, DiagnosticRules.ArrayMarkedInOrOut).SetName("TestArrayParamAttrBinary_15");
                yield return new TestCaseData(TestArrayParamAttrBinary_16, DiagnosticRules.ArrayMarkedInOrOut).SetName("TestArrayParamAttrBinary_16");
                yield return new TestCaseData(TestArrayParamAttrBinary_17, DiagnosticRules.ArrayParamNotMarked).SetName("TestArrayParamAttrBinary_17");
                yield return new TestCaseData(TestArrayParamAttrBinary_18, DiagnosticRules.NonArrayMarked).SetName("TestArrayParamAttrBinary_18");
                yield return new TestCaseData(TestArrayParamAttrBinary_19, DiagnosticRules.NonArrayMarked).SetName("TestArrayParamAttrBinary_19");
                yield return new TestCaseData(TestArrayParamAttrBinary_20, DiagnosticRules.NonArrayMarked).SetName("TestArrayParamAttrBinary_20");
                yield return new TestCaseData(TestArrayParamAttrBinary_21, DiagnosticRules.NonArrayMarkedInOrOut).SetName("TestArrayParamAttrBinary_21");
                yield return new TestCaseData(TestArrayParamAttrBinary_22, DiagnosticRules.NonArrayMarkedInOrOut).SetName("TestArrayParamAttrBinary_22");
                yield return new TestCaseData(TestArrayParamAttrBinary_23, DiagnosticRules.NonArrayMarkedInOrOut).SetName("TestArrayParamAttrBinary_23");
                yield return new TestCaseData(TestArrayParamAttrBinary_24, DiagnosticRules.ArrayParamNotMarked).SetName("TestArrayParamAttrBinary_24");
                // name clash with params (__retval)
                yield return new TestCaseData(DunderRetValParam, DiagnosticRules.ParameterNamedValueRule).SetName("Test Parameter Name Conflict (__retval)");
                // operator overloading
                yield return new TestCaseData(OperatorOverload_Class, DiagnosticRules.OperatorOverloadedRule).SetName("Test Overload of Operator");
                // ref param
                yield return new TestCaseData(RefParam_ClassMethod, DiagnosticRules.RefParameterFound).SetName("Test For Method With Ref Param - Class");
                yield return new TestCaseData(RefParam_InterfaceMethod, DiagnosticRules.RefParameterFound).SetName("Test For Method With Ref Param - Interface");
                // startuc field tests
                yield return new TestCaseData(StructWithClassField, DiagnosticRules.StructHasInvalidFieldRule).SetName("Struct with Class Field");
                yield return new TestCaseData(StructWithDelegateField, DiagnosticRules.StructHasInvalidFieldRule2).SetName("Struct with Delegate Field");
                yield return new TestCaseData(StructWithIndexer, DiagnosticRules.StructHasInvalidFieldRule2).SetName("Struct with Indexer Field");
                yield return new TestCaseData(StructWithMethods, DiagnosticRules.StructHasInvalidFieldRule2).SetName("Struct with Method Field");
                yield return new TestCaseData(StructWithConst, DiagnosticRules.StructHasConstFieldRule).SetName("Struct with Const Field");
                yield return new TestCaseData(StructWithProperty, DiagnosticRules.StructHasInvalidFieldRule2).SetName("Struct with Property Field");
                yield return new TestCaseData(StructWithPrivateField, DiagnosticRules.StructHasPrivateFieldRule).SetName("Struct with Private Field");
                yield return new TestCaseData(StructWithObjectField, DiagnosticRules.StructHasInvalidFieldRule).SetName("Struct with Object Field");
                yield return new TestCaseData(StructWithDynamicField, DiagnosticRules.StructHasInvalidFieldRule).SetName("Struct with Dynamic Field");
                // bytes are valid in structs I think? yield return new TestCaseData(StructWithByteField, DiagnosticRules.StructHasInvalidFieldRule).SetName("Struct with Byte Field");
                yield return new TestCaseData(StructWithConstructor, DiagnosticRules.StructHasInvalidFieldRule2).SetName("Struct with Constructor Field");
                yield return new TestCaseData(StructWithPrimitiveTypesMissingPublicKeyword, DiagnosticRules.StructHasPrivateFieldRule).SetName("Struct with Constructor Field");
                // system.array tests
                yield return new TestCaseData(ArrayInstanceProperty1, DiagnosticRules.ArraySignature_SystemArrayRule).SetName("System.Array Property 1");
                yield return new TestCaseData(ArrayInstanceProperty2, DiagnosticRules.ArraySignature_SystemArrayRule).SetName("System.Array Property 2");
                yield return new TestCaseData(ArrayInstanceProperty3, DiagnosticRules.ArraySignature_SystemArrayRule).SetName("System.Array Property 3");
                yield return new TestCaseData(ArrayInstanceProperty4, DiagnosticRules.ArraySignature_SystemArrayRule).SetName("System.Array Property 4");
                yield return new TestCaseData(SystemArrayProperty5, DiagnosticRules.ArraySignature_SystemArrayRule).SetName("System.Array Property 5");
                yield return new TestCaseData(ArrayInstanceInterface1, DiagnosticRules.ArraySignature_SystemArrayRule).SetName("System.Array Interface 1");
                yield return new TestCaseData(ArrayInstanceInterface2, DiagnosticRules.ArraySignature_SystemArrayRule).SetName("System.Array Interface 2");
                yield return new TestCaseData(ArrayInstanceInterface3, DiagnosticRules.ArraySignature_SystemArrayRule).SetName("System.Array Interface 3");
                yield return new TestCaseData(SystemArrayJustReturn, DiagnosticRules.ArraySignature_SystemArrayRule).SetName("System.Array Method - Return only");
                yield return new TestCaseData(SystemArrayUnaryAndReturn, DiagnosticRules.ArraySignature_SystemArrayRule).SetName("System.Array Method - Unary and return");
                yield return new TestCaseData(SystemArraySecondArgClass, DiagnosticRules.ArraySignature_SystemArrayRule).SetName("System.Array Method - Arg 2/2");
                yield return new TestCaseData(SystemArraySecondArg2Class, DiagnosticRules.ArraySignature_SystemArrayRule).SetName("System.Array Method - Arg 2/3");
                yield return new TestCaseData(SystemArraySecondArgAndReturnTypeClass, DiagnosticRules.ArraySignature_SystemArrayRule).SetName("System.Array Class 1");
                yield return new TestCaseData(SystemArraySecondArgAndReturnTypeClass2, DiagnosticRules.ArraySignature_SystemArrayRule).SetName("System.Array Class 2");
                yield return new TestCaseData(SystemArrayNilArgsButReturnTypeInterface, DiagnosticRules.ArraySignature_SystemArrayRule).SetName("System.Array Interface 4");
                yield return new TestCaseData(SystemArrayUnaryAndReturnTypeInterface, DiagnosticRules.ArraySignature_SystemArrayRule).SetName("System.Array Interface 5");
                yield return new TestCaseData(SystemArraySecondArgAndReturnTypeInterface, DiagnosticRules.ArraySignature_SystemArrayRule).SetName("System.Array Interface 6");
                yield return new TestCaseData(SystemArraySecondArgAndReturnTypeInterface2, DiagnosticRules.ArraySignature_SystemArrayRule).SetName("System.Array Interface 7");
                yield return new TestCaseData(SystemArraySecondArgInterface, DiagnosticRules.ArraySignature_SystemArrayRule).SetName("System.Array Interface 8");
                yield return new TestCaseData(SystemArraySecondArgInterface2, DiagnosticRules.ArraySignature_SystemArrayRule).SetName("System.Array Interface 9");
                yield return new TestCaseData(SystemArraySubNamespace_ReturnOnly, DiagnosticRules.ArraySignature_SystemArrayRule).SetName("System.Array Subnamespace Interface 1/6");
                yield return new TestCaseData(SystemArraySubNamespace_ReturnAndInput1, DiagnosticRules.ArraySignature_SystemArrayRule).SetName("System.Array Subnamespace Interface 2/6");
                yield return new TestCaseData(SystemArraySubNamespace_ReturnAndInput2of2, DiagnosticRules.ArraySignature_SystemArrayRule).SetName("System.Array Subnamespace Interface 3/6");
                yield return new TestCaseData(SystemArraySubNamespace_ReturnAndInput2of3, DiagnosticRules.ArraySignature_SystemArrayRule).SetName("System.Array Subnamespace Interface 4/6");
                yield return new TestCaseData(SystemArraySubNamespace_NotReturnAndInput2of2, DiagnosticRules.ArraySignature_SystemArrayRule).SetName("System.Array Subnamespace Interface 5/6");
                yield return new TestCaseData(SystemArraySubNamespace_NotReturnAndInput2of3, DiagnosticRules.ArraySignature_SystemArrayRule).SetName("System.Array Subnamespace Interface 6/6");
            }
        }

        #endregion

        #region ValidTests

        private static IEnumerable<TestCaseData> ValidCases
        {
            get
            {
                // ReadOnlyArray / WriteOnlyArray Attribute
                yield return new TestCaseData(Valid_ArrayParamAttrUnary_1).SetName("Valid - Unary - Array marked read only");
                yield return new TestCaseData(Valid_ArrayParamAttrUnary_2).SetName("Valid - Unary - Array marked write only");
                yield return new TestCaseData(Valid_ArrayParamAttrUnary_3).SetName("Valid - Unary - Array marked out and write only");
                yield return new TestCaseData(Valid_ArrayParamAttrUnary_4).SetName("Valid - ArrayParamAttrUnary_4");
                yield return new TestCaseData(Valid_ArrayParamAttrUnary_5).SetName("Valid - ArrayParamAttrUnary_5");
                yield return new TestCaseData(Valid_ArrayParamAttrBinary_1).SetName("Valid - ArrayParamAttrBinary_1");
                yield return new TestCaseData(Valid_ArrayParamAttrBinary_2).SetName("Valid - ArrayParamAttrBinary_2");
                yield return new TestCaseData(Valid_ArrayParamAttrBinary_3).SetName("Valid - ArrayParamAttrBinary_3");
                yield return new TestCaseData(Valid_ArrayParamAttrBinary_4).SetName("Valid - ArrayParamAttrBinary_4");
                yield return new TestCaseData(Valid_ArrayParamAttrBinary_5).SetName("Valid - ArrayParamAttrBinary_5");
                yield return new TestCaseData(Valid_ArrayParamAttrBinary_6).SetName("Valid - ArrayParamAttrBinary_6");
                yield return new TestCaseData(Valid_ArrayParamAttrBinary_7).SetName("Valid - ArrayParamAttrBinary_7");
                yield return new TestCaseData(Valid_ArrayParamAttrBinary_8).SetName("Valid - ArrayParamAttrBinary_8");
                yield return new TestCaseData(Valid_ArrayParamAttrBinary_9).SetName("Valid - ArrayParamAttrBinary_9");
                // Struct field 
                yield return new TestCaseData(Valid_StructWithPrimitiveTypes).SetName("Valid - Struct with only fields of basic types");
                yield return new TestCaseData(Valid_StructWithWinRTField).SetName("(TODO - fix the namespace) Valid - Struct with struct field");
                // SystemArray  
                yield return new TestCaseData(Valid_SystemArrayProperty).SetName("Valid - System.Array private property");
                yield return new TestCaseData(Valid_SystemArray_Interface1).SetName("Valid - System.Array internal interface 1");
                yield return new TestCaseData(Valid_SystemArray_Interface2).SetName("Valid - System.Array internal interface 2");
                yield return new TestCaseData(Valid_SystemArray_Interface3).SetName("Valid - System.Array internal interface 3");
                yield return new TestCaseData(Valid_SystemArray_InternalClass1).SetName("Valid - System.Array internal class 1");
                yield return new TestCaseData(Valid_SystemArray_InternalClass2).SetName("Valid - System.Array internal class 2");
                yield return new TestCaseData(Valid_SystemArray_InternalClass3).SetName("Valid - System.Array internal class 3");
                yield return new TestCaseData(Valid_SystemArray_InternalClass4).SetName("Valid - System.Array internal class 4");
                yield return new TestCaseData(Valid_SystemArray_PublicClassPrivateProperty1).SetName("Valid - System.Array public class / private property 1");
                yield return new TestCaseData(Valid_SystemArray_PublicClassPrivateProperty2).SetName("Valid - System.Array public class / private property 2");
                yield return new TestCaseData(Valid_SystemArray_PublicClassPrivateProperty3).SetName("Valid - System.Array public class / private property 3");
                yield return new TestCaseData(Valid_SystemArray_PublicClassPrivateProperty4).SetName("Valid - System.Array public class / private property 4");
                yield return new TestCaseData(Valid_SystemArray_PublicClassPrivateProperty5).SetName("Valid - System.Array public class / private property 5");
                yield return new TestCaseData(Valid_SystemArray_PublicClassPrivateProperty6).SetName("Valid - System.Array public class / private property 6");
                yield return new TestCaseData(Valid_SystemArray_InternalClassPublicMethods1).SetName("Valid - System.Array internal class / public method 1");
                yield return new TestCaseData(Valid_SystemArray_InternalClassPublicMethods2).SetName("Valid - System.Array internal class / public method 2");
                yield return new TestCaseData(Valid_SystemArray_InternalClassPublicMethods3).SetName("Valid - System.Array internal class / public method 3");
                yield return new TestCaseData(Valid_SystemArray_InternalClassPublicMethods4).SetName("Valid - System.Array internal class / public method 4");
                yield return new TestCaseData(Valid_SystemArray_InternalClassPublicMethods5).SetName("Valid - System.Array internal class / public method 5");
                yield return new TestCaseData(Valid_SystemArray_InternalClassPublicMethods6).SetName("Valid - System.Array internal class / public method 6");
                yield return new TestCaseData(Valid_SystemArray_PrivateClassPublicProperty1).SetName("Valid - System.Array internal class / public property 1");
                yield return new TestCaseData(Valid_SystemArray_PrivateClassPublicProperty2).SetName("Valid - System.Array internal class / public property 2");
                yield return new TestCaseData(Valid_SystemArray_PrivateClassPublicProperty3).SetName("Valid - System.Array internal class / public property 3");
                yield return new TestCaseData(Valid_SystemArray_PrivateClassPublicProperty4).SetName("Valid - System.Array internal class / public property 4");
                yield return new TestCaseData(Valid_SystemArray_PrivateClassPublicProperty5).SetName("Valid - System.Array internal class / public property 5");
                yield return new TestCaseData(Valid_SystemArray_PrivateClassPublicProperty6).SetName("Valid - System.Array internal class / public property 6");
                yield return new TestCaseData(Valid_SystemArray_PrivateClassPublicProperty7).SetName("Valid - System.Array internal class / public property 7");
                yield return new TestCaseData(Valid_SystemArray_PrivateClassPublicProperty8).SetName("Valid - System.Array internal class / public property 8");
                yield return new TestCaseData(Valid_SystemArrayPublicClassPrivateMethod1).SetName("Valid - System.Array public class / private method 1");
                yield return new TestCaseData(Valid_SystemArrayPublicClassPrivateMethod2).SetName("Valid - System.Array public class / private method 2");
                yield return new TestCaseData(Valid_SystemArrayPublicClassPrivateMethod3).SetName("Valid - System.Array public class / private method 3");
                yield return new TestCaseData(Valid_SystemArrayPublicClassPrivateMethod4).SetName("Valid - System.Array public class / private method 4");
                yield return new TestCaseData(Valid_SystemArrayPublicClassPrivateMethod5).SetName("Valid - System.Array public class / private method 5");
                yield return new TestCaseData(Valid_SystemArrayPublicClassPrivateMethod6).SetName("Valid - System.Array public class / private method 6");
                // multi dim array tests 
                yield return new TestCaseData(Valid_MultiDimArray_PublicClassPrivateMethod1).SetName("Valid - MultiDim public class / private method 1");
                yield return new TestCaseData(Valid_MultiDimArray_PublicClassPrivateMethod2).SetName("Valid - MultiDim public class / private method 2");
                yield return new TestCaseData(Valid_MultiDimArray_PublicClassPrivateMethod3).SetName("Valid - MultiDim public class / private method 3");
                yield return new TestCaseData(Valid_MultiDimArray_PublicClassPrivateMethod4).SetName("Valid - MultiDim public class / private method 4");
                yield return new TestCaseData(Valid_MultiDimArray_PublicClassPrivateMethod5).SetName("Valid - MultiDim public class / private method 5");
                yield return new TestCaseData(Valid_MultiDimArray_PublicClassPrivateMethod6).SetName("Valid - MultiDim public class / private method 6");
                
                yield return new TestCaseData(Valid_3D_PrivateClass_PublicMethod1).SetName("Valid - MultiDim 3D private class / public method 1");
                yield return new TestCaseData(Valid_3D_PrivateClass_PublicMethod2).SetName("Valid - MultiDim 3D private class / public method 2");
                yield return new TestCaseData(Valid_3D_PrivateClass_PublicMethod3).SetName("Valid - MultiDim 3D private class / public method 3");
                yield return new TestCaseData(Valid_3D_PrivateClass_PublicMethod4).SetName("Valid - MultiDim 3D private class / public method 4");
                yield return new TestCaseData(Valid_3D_PrivateClass_PublicMethod5).SetName("Valid - MultiDim 3D private class / public method 5");
                yield return new TestCaseData(Valid_3D_PrivateClass_PublicMethod6).SetName("Valid - MultiDim 3D private class / public method 6");
                
                yield return new TestCaseData(Valid_2D_PrivateClass_PublicMethod1).SetName("Valid - MultiDim 2D private class / public method 1");
                yield return new TestCaseData(Valid_2D_PrivateClass_PublicMethod2).SetName("Valid - MultiDim 2D private class / public method 2");
                yield return new TestCaseData(Valid_2D_PrivateClass_PublicMethod3).SetName("Valid - MultiDim 2D private class / public method 3");
                yield return new TestCaseData(Valid_2D_PrivateClass_PublicMethod4).SetName("Valid - MultiDim 2D private class / public method 4");
                yield return new TestCaseData(Valid_2D_PrivateClass_PublicMethod5).SetName("Valid - MultiDim 2D private class / public method 5");
                yield return new TestCaseData(Valid_2D_PrivateClass_PublicMethod6).SetName("Valid - MultiDim 2D private class / public method 6");
                
                yield return new TestCaseData(Valid_MultiDimArray_PrivateClassPublicProperty1).SetName("Valid - MultiDim 2D private class / public property 1");
                yield return new TestCaseData(Valid_MultiDimArray_PrivateClassPublicProperty2).SetName("Valid - MultiDim 2D private class / public property 2");
                yield return new TestCaseData(Valid_MultiDimArray_PrivateClassPublicProperty3).SetName("Valid - MultiDim 2D private class / public property 3");
                yield return new TestCaseData(Valid_MultiDimArray_PrivateClassPublicProperty4).SetName("Valid - MultiDim 2D private class / public property 4");
                yield return new TestCaseData(Valid_MultiDimArray_PublicClassPrivateProperty1).SetName("Valid - MultiDim 2D public class / private property 1");
                yield return new TestCaseData(Valid_MultiDimArray_PublicClassPrivateProperty2).SetName("Valid - MultiDim 2D public class / private property 2");
                // jagged array tests
                yield return new TestCaseData(Valid_JaggedMix_PrivateClassPublicProperty).SetName("Valid - Jagged Array private class / private property");
                yield return new TestCaseData(Valid_Jagged2D_PrivateClassPublicMethods).SetName("Valid - Jagged Array private class / public method");
                yield return new TestCaseData(Valid_Jagged3D_PrivateClassPublicMethods).SetName("Valid - Jagged Array private class / public method");
                yield return new TestCaseData(Valid_Jagged3D_PublicClassPrivateMethods).SetName("Valid - Jagged Array public class / private method");
                yield return new TestCaseData(Valid_Jagged2D_Property).SetName("Valid - Jagged 2D Array public property");
                yield return new TestCaseData(Valid_Jagged3D_Property).SetName("Valid - Jagged 3D Array public property");
                // overload attributes
                yield return new TestCaseData(Valid_TwoOverloads_DiffParamCount).SetName("Valid - DefaultOverload attribute 1");
                yield return new TestCaseData(Valid_TwoOverloads_OneAttribute_OneInList).SetName("Valid - DefaultOverload attribute 2");
                yield return new TestCaseData(Valid_TwoOverloads_OneAttribute_OneIrrelevatAttribute).SetName("Valid - DefaultOverload attribute 3");
                yield return new TestCaseData(Valid_TwoOverloads_OneAttribute_TwoLists).SetName("Valid - DefaultOverload attribute 4");
                yield return new TestCaseData(Valid_ThreeOverloads_OneAttribute).SetName("Valid - DefaultOverload attribute 5");
                yield return new TestCaseData(Valid_ThreeOverloads_OneAttribute_2).SetName("Valid - DefaultOverload attribute 6");
                yield return new TestCaseData(Valid_TwoOverloads_OneAttribute_3).SetName("Valid - DefaultOverload attribute 7");
            }
        }

        #endregion

    }
}