// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ProjectionWriterTest.Helpers;
using WindowsRuntime;
using WindowsRuntime.ProjectionWriter;

namespace ProjectionWriterTest;

[TestClass]
public class Test_ArrayProperties
{
    private static readonly Lazy<string> RuntimeFixture = new(CreateRuntimeFixture);

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public void ArrayProperties_CompileWithOwnedSetterArrays(bool referenceProjection, bool exclusiveTo)
    {
        string directory = Directory.CreateTempSubdirectory("ProjectionArrayPropertyTest_").FullName;

        try
        {
            string output = GenerateSources(directory, referenceProjection, exclusiveTo);
            string source = File.ReadAllText(Path.Combine(output, "Contoso.cs"));
            _ = ProjectionWriterRunner.CompileSources(
                Directory.GetFiles(output, "*.cs").Select(File.ReadAllText),
                Path.Combine(directory, "Projection.dll"),
                referenceProjection);

            if (referenceProjection)
            {
                StringAssert.Contains(source, "int[] Values");
                StringAssert.Contains(source, "string[] Names");
                return;
            }

            ClassDeclarationSyntax impl = CSharpSyntaxTree.ParseText(source).GetRoot().DescendantNodes()
                .OfType<ClassDeclarationSyntax>().Single(type => type.Identifier.ValueText == "IWidget2Impl");
            StringAssert.Contains(source, "class IWidget2Marshaller");
            MethodDeclarationSyntax[] setters = impl.Members.OfType<MethodDeclarationSyntax>()
                .Where(method => method.Identifier.ValueText.StartsWith("Do_Abi_put_", StringComparison.Ordinal)
                    && !method.Identifier.ValueText.StartsWith("Do_Abi_put_Label_", StringComparison.Ordinal)).ToArray();
            Assert.HasCount(8, setters);

            foreach (MethodDeclarationSyntax setter in setters)
            {
                TryStatementSyntax body = setter.Body!.Statements.OfType<TryStatementSyntax>().Single();
                Assert.HasCount(1, body.Catches);
                Assert.IsNull(body.Finally);
                Assert.IsFalse(setter.ToString().Contains("ArrayPool", StringComparison.Ordinal), setter.ToString());
                Assert.IsFalse(setter.ToString().Contains("InlineArray16", StringComparison.Ordinal), setter.ToString());
                Assert.IsFalse(setter.ToString().Contains("CopyToManaged", StringComparison.Ordinal), setter.ToString());
                Assert.IsTrue(body.Block.Statements.OfType<LocalFunctionStatementSyntax>().Any(
                    function => function.ReturnType is ArrayTypeSyntax
                        && function.Identifier.ValueText.StartsWith("ConvertToManaged_", StringComparison.Ordinal)), setter.ToString());
                AssignmentExpressionSyntax assignment = body.Block.Statements.OfType<ExpressionStatementSyntax>()
                    .Select(statement => statement.Expression).OfType<AssignmentExpressionSyntax>().Single();
                string parameterName = setter.ParameterList.Parameters[^1].Identifier.ValueText;
                Assert.AreEqual($"__{parameterName}", assignment.Right.ToString());
            }

            MethodDeclarationSyntax namesMethod = impl.Members.OfType<MethodDeclarationSyntax>()
                .Single(method => method.Identifier.ValueText.StartsWith("Do_Abi_AcceptNames_", StringComparison.Ordinal));
            StringAssert.Contains(namesMethod.ToString(), "Span<string>");
            StringAssert.Contains(namesMethod.ToString(), "CopyToManaged_value");
            StringAssert.Contains(namesMethod.ToString(), "ArrayPool<string>.Shared.Return");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(15)]
    [DataRow(16)]
    [DataRow(17)]
    [DataRow(33)]
    public void NativeSetters_RetainExactlySizedOwnedArrays(int length)
    {
        RunRuntimeFixture(length.ToString());
    }

    [TestMethod]
    public void NativeSetters_TranslateConversionFailuresWithoutCallingSetter()
    {
        RunRuntimeFixture("conversion-failure");
    }

    [TestMethod]
    public void NativeSetters_PreserveRetainedArrayWhenSetterThrows()
    {
        RunRuntimeFixture("setter-failure");
    }

    [TestMethod]
    public void NativeAccessors_PreserveGettersScalarSettersAndSpanParameters()
    {
        RunRuntimeFixture("other-members");
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(15)]
    [DataRow(16)]
    [DataRow(17)]
    [DataRow(33)]
    [DataRow(64)]
    public void NativeArrayMethods_PreserveExactSpanLengths(int length)
    {
        RunRuntimeFixture($"methods:{length}");
    }

    [TestMethod]
    [DataRow(17)]
    [DataRow(33)]
    public void NativeArrayMethods_DoNotRetainMarshalledObjects(int length)
    {
        RunRuntimeFixture($"retention:{length}");
    }

    [TestMethod]
    [DataRow(17)]
    [DataRow(33)]
    public void RcwFillArrays_FailureDoesNotReleaseStalePoolContents(int length)
    {
        RunRuntimeFixture($"fill-failure:{length}");
    }

    [TestMethod]
    [DataRow(17)]
    [DataRow(33)]
    public void RcwPassArrays_FailureDoesNotReleaseStalePoolContents(int length)
    {
        RunRuntimeFixture($"pass-failure:{length}");
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(16)]
    [DataRow(17)]
    [DataRow(33)]
    public void RcwStringFillArrays_DisposeOwnedHStrings(int length)
    {
        RunRuntimeFixture($"string-fill:{length}");
    }

    [TestMethod]
    [DataRow(17)]
    [DataRow(33)]
    public void RcwMappedArrays_ReturnTemporaryStorage(int length)
    {
        RunRuntimeFixture($"mapped-rental:{length}");
    }

    [TestMethod]
    [DataRow(2)]
    [DataRow(17)]
    [DataRow(33)]
    public void RcwStringFillArrays_FailedConversionDisposesEachHStringOnce(int length)
    {
        RunRuntimeFixture($"string-conversion-failure:{length}");
    }

    [ClassCleanup]
    public static void Cleanup()
    {
        if (RuntimeFixture.IsValueCreated)
        {
            Directory.Delete(RuntimeFixture.Value, recursive: true);
        }
    }

    [TestMethod]
    [DataRow("nullable", 0)]
    [DataRow("nullable", 1)]
    [DataRow("nullable", 16)]
    [DataRow("nullable", 17)]
    [DataRow("nullable", 33)]
    [DataRow("pairs", 0)]
    [DataRow("pairs", 1)]
    [DataRow("pairs", 16)]
    [DataRow("pairs", 17)]
    [DataRow("pairs", 33)]
    [DataRow("types", 0)]
    [DataRow("types", 1)]
    [DataRow("types", 16)]
    [DataRow("types", 17)]
    [DataRow("types", 33)]
    public void RcwArrayMethods_RoundTripMappedReferenceAndTypeElements(string elementType, int length)
    {
        RunRuntimeFixture($"array-abi:{elementType}:{length}");
    }

    [TestMethod]
    [DataRow(2)]
    [DataRow(17)]
    [DataRow(33)]
    public void RcwTypeArrays_FailedConversionDisposesEachNameOnce(int length)
    {
        RunRuntimeFixture($"type-failure:{length}");
    }

    private static string GenerateSources(string directory, bool referenceProjection, bool exclusiveTo)
    {
        string metadata = ArrayPropertyMetadata.Create(directory, exclusiveTo);
        string output = Path.Combine(directory, referenceProjection ? "reference" : "generated");
        ProjectionWriter.Run(new ProjectionWriterOptions
        {
            InputPaths = [metadata],
            OutputFolder = output,
            Include = ["Contoso"],
            PublicExclusiveToTypes = ["Contoso.IWidget2"],
            IdicExclusiveToTypes = ["Contoso.IWidget2"],
            ReferenceProjection = referenceProjection
        });
        return output;
    }

    private static string CreateRuntimeFixture()
    {
        string directory = Directory.CreateTempSubdirectory("ProjectionArrayPropertyRuntimeTest_").FullName;

        try
        {
            string output = GenerateSources(directory, referenceProjection: false, exclusiveTo: true);
            string projection = ProjectionWriterRunner.CompileSources(
                Directory.GetFiles(output, "*.cs").Select(File.ReadAllText),
                Path.Combine(directory, "WinRT.Sdk.Projection.dll"));
            string referenceOutput = Path.Combine(directory, "sdk-reference");
            Directory.CreateDirectory(referenceOutput);
            string referenceMetadata = ArrayPropertyMetadata.Create(referenceOutput, exclusiveTo: true, includeArrayProperties: false);
            ProjectionWriter.Run(new ProjectionWriterOptions
            {
                InputPaths = [referenceMetadata],
                OutputFolder = referenceOutput,
                // Only the class hierarchy needs a reference projection; the ABI harness targets the implementation.
                Include = ["Contoso"],
                ReferenceProjection = true
            });
            string sdkReference = ProjectionWriterRunner.CompileSources(
                Directory.GetFiles(referenceOutput, "*.cs").Select(File.ReadAllText),
                Path.Combine(referenceOutput, "Microsoft.Windows.SDK.NET.dll"),
                referenceProjection: true);
            using Stream resource = typeof(Test_ArrayProperties).Assembly.GetManifestResourceStream(
                "ProjectionWriterTest.Resources.ArrayPropertySetterTests.cs")!;
            using StreamReader reader = new(resource);
            string app = ProjectionWriterRunner.CompileSources(
                [reader.ReadToEnd()],
                Path.Combine(directory, "ArrayPropertySetterTests.dll"),
                outputKind: OutputKind.ConsoleApplication,
                additionalReferences: [projection]);
            string[] references = [projection, .. ProjectionWriterRunner.GetRuntimeReferencePaths()];
            string responseFile = Path.Combine(directory, "interop.rsp");
            File.WriteAllLines(responseFile,
            [
                $"--reference-assembly-paths {sdkReference},{string.Join(",", references)}",
                $"--implementation-assembly-paths {app},{string.Join(",", references)}",
                $"--output-assembly-path {app}",
                $"--winrt-sdk-projection-assembly-path {projection}",
                $"--generated-assembly-directory {directory}",
                "--use-windows-ui-xaml-projections false",
                "--validate-winrt-runtime-assembly-version true",
                "--validate-winrt-runtime-dll-version-2-references true",
                "--enable-incremental-generation false",
                "--treat-warnings-as-errors true",
                "--max-degrees-of-parallelism 1"
            ]);
            (int exitCode, string log) = ProjectionWriterRunner.Run(
                ProjectionWriterRunner.GetRequiredFilePath("InteropGeneratorAssemblyPath"), $"@{responseFile}");
            Assert.AreEqual(0, exitCode, log);
            Assert.IsTrue(File.Exists(Path.Combine(directory, "WinRT.Interop.dll")));
            WriteRuntimeWithHStringCleanupCounter(Path.Combine(directory, "WinRT.Runtime.dll"));
            File.WriteAllText(Path.Combine(directory, "ArrayPropertySetterTests.runtimeconfig.json"), """
                {"runtimeOptions":{"tfm":"net10.0","framework":{"name":"Microsoft.NETCore.App","version":"10.0.0"}}}
                """);
            return directory;
        }
        catch
        {
            Directory.Delete(directory, recursive: true);
            throw;
        }
    }

    private static void RunRuntimeFixture(string scenario)
    {
        (int exitCode, string log) = ProjectionWriterRunner.Run(
            Path.Combine(RuntimeFixture.Value, "ArrayPropertySetterTests.dll"), scenario);
        Assert.AreEqual(0, exitCode, $"Native array property scenario '{scenario}' failed:\n{log}");
    }

    private static void WriteRuntimeWithHStringCleanupCounter(string path)
    {
        ModuleDefinition runtime = ModuleDefinition.FromFile(typeof(WindowsRuntimeObject).Assembly.Location);
        TypeDefinition marshaller = runtime.TopLevelTypes.Single(
            type => type.FullName == "WindowsRuntime.InteropServices.Marshalling.HStringMarshaller");
        FieldDefinition counter = new("TestFreeCount", FieldAttributes.Public | FieldAttributes.Static, runtime.CorLibTypeFactory.Int32);
        marshaller.Fields.Add(counter);
        CilMethodBody body = marshaller.Methods.Single(method => method.Name == "Free").CilMethodBody!;
        body.Instructions.ExpandMacros();
        // Count execution of the real cleanup routine in the fixture's private runtime copy.
        var cleanupStart = body.Instructions[0].CreateLabel();
        CilInstruction[] increment =
        [
            new(CilOpCodes.Ldarg_0),
            new(CilOpCodes.Brfalse, cleanupStart),
            new(CilOpCodes.Ldsfld, counter),
            new(CilOpCodes.Ldc_I4_1),
            new(CilOpCodes.Add),
            new(CilOpCodes.Stsfld, counter)
        ];
        foreach (CilInstruction instruction in increment.Reverse())
        {
            body.Instructions.Insert(0, instruction);
        }
        FieldDefinition failure = new("TestFailCreateCountdown", FieldAttributes.Public | FieldAttributes.Static, runtime.CorLibTypeFactory.Int32);
        marshaller.Fields.Add(failure);
        CilMethodBody create = marshaller.Methods.Single(method => method.Name == "ConvertToUnmanaged").CilMethodBody!;
        create.Instructions.ExpandMacros();
        var createStart = create.Instructions[0].CreateLabel();
        TypeReference exception = new(runtime, runtime.CorLibTypeFactory.CorLibScope, "System", "InvalidOperationException");
        MemberReference constructor = new(exception, ".ctor", MethodSignature.CreateInstance(runtime.CorLibTypeFactory.Void));
        // Deterministically fail one allocation without exhausting process memory.
        CilInstruction[] failAllocation =
        [
            new(CilOpCodes.Ldsfld, failure),
            new(CilOpCodes.Brfalse, createStart),
            new(CilOpCodes.Ldsfld, failure),
            new(CilOpCodes.Ldc_I4_1),
            new(CilOpCodes.Sub),
            new(CilOpCodes.Dup),
            new(CilOpCodes.Stsfld, failure),
            new(CilOpCodes.Brtrue, createStart),
            new(CilOpCodes.Newobj, constructor),
            new(CilOpCodes.Throw)
        ];
        foreach (CilInstruction instruction in failAllocation.Reverse())
        {
            create.Instructions.Insert(0, instruction);
        }
        runtime.Write(path);
    }
}
