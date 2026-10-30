// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using ProjectionWriterTest.Helpers;
using WindowsRuntime;

namespace ProjectionWriterTest;

[TestClass]
public class Test_CreateFromStringAttributes
{
    private const string AttributeName = "Windows.Foundation.Metadata.CreateFromStringAttribute";
    private static readonly Lazy<byte[]> ReferenceProjection = new(() => CompileProjection(referenceProjection: true));
    private static readonly Lazy<byte[]> ImplementationProjection = new(() => CompileProjection(referenceProjection: false));

    [TestMethod]
    [DataRow(true, "Class", "TestComponentCSharp.Class.CreateFromString")]
    [DataRow(true, "NonBlittableStringStruct", "TestComponentCSharp.Class.CreateStructFromString")]
    [DataRow(false, "Class", "TestComponentCSharp.Class.CreateFromString")]
    [DataRow(false, "NonBlittableStringStruct", "TestComponentCSharp.Class.CreateStructFromString")]
    public void CreateFromString_IsCarriedOverToReferenceMetadataOnly(bool referenceProjection, string typeName, string methodName)
    {
        TypeDefinition type = GetType(referenceProjection, typeName);
        CustomAttribute? attribute = type.CustomAttributes.SingleOrDefault(
            attribute => attribute.Constructor?.DeclaringType?.FullName == AttributeName);

        if (!referenceProjection)
        {
            Assert.IsNull(attribute, "Implementation projections must not retain compiler-only conversion metadata.");
            return;
        }

        Assert.IsNotNull(attribute, $"The reference projection lost '{AttributeName}' on '{typeName}'.");
        Assert.AreEqual(0, attribute.Constructor!.Signature!.ParameterTypes.Count);
        Assert.AreEqual(0, attribute.Signature!.FixedArguments.Count);
        Assert.AreEqual(1, attribute.Signature.NamedArguments.Count);

        CustomAttributeNamedArgument argument = attribute.Signature.NamedArguments[0];

        Assert.AreEqual(CustomAttributeArgumentMemberType.Field, argument.MemberType);
        Assert.AreEqual("MethodName", argument.MemberName?.Value);
        Assert.AreEqual("System.String", argument.ArgumentType.FullName);
        Assert.AreEqual(methodName, argument.Argument.Element?.ToString());
    }

    [TestMethod]
    [DataRow(true, "NonAgileClass")]
    [DataRow(true, "BlittableStruct")]
    [DataRow(false, "NonAgileClass")]
    [DataRow(false, "BlittableStruct")]
    public void UnannotatedType_DoesNotAcquireCreateFromString(bool referenceProjection, string typeName)
    {
        TypeDefinition type = GetType(referenceProjection, typeName);

        Assert.IsFalse(type.CustomAttributes.Any(
            attribute => attribute.Constructor?.DeclaringType?.FullName == AttributeName));
    }

    private static TypeDefinition GetType(bool referenceProjection, string typeName)
    {
        byte[] bytes = (referenceProjection ? ReferenceProjection : ImplementationProjection).Value;
        ModuleDefinition module = ModuleDefinition.FromBytes(bytes);

        return module.TopLevelTypes.Single(type => type.FullName == $"TestComponentCSharp.{typeName}");
    }

    private static byte[] CompileProjection(bool referenceProjection)
    {
        string directory = Directory.CreateTempSubdirectory("ProjectionCreateFromStringTest_").FullName;

        try
        {
            string metadataPath = CreateFromStringMetadata.Create(directory);
            string[] sources = ProjectionWriterRunner.GenerateSources(
                referenceProjection, $"sdk,{metadataPath}", $"TestComponentCSharp,{AttributeName}");

            CSharpParseOptions parseOptions = new(LanguageVersion.CSharp14);
            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName: "CreateFromStringProjection",
                syntaxTrees: sources.Select((source, index) => CSharpSyntaxTree.ParseText(source, parseOptions, path: $"Generated{index}.cs")),
                references:
                [
                    .. Net100.References.All,
                    MetadataReference.CreateFromFile(typeof(WindowsRuntimeObject).Assembly.Location)
                ],
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));

            using MemoryStream output = new();
            EmitResult result = compilation.Emit(output,
                options: new EmitOptions(metadataOnly: referenceProjection, includePrivateMembers: !referenceProjection));

            Assert.IsTrue(result.Success, $"Projection compilation failed:\n{string.Join("\n", result.Diagnostics)}");

            return output.ToArray();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
