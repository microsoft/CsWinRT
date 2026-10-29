// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using ProjectionWriterTest.Helpers;

namespace ProjectionWriterTest;

[TestClass]
public class Test_SupportedOSPlatformAttributes
{
    [TestMethod]
    [DataRow("Windows.ApplicationModel.FullTrustAppContract", 1, "10.0.15063.0")]
    [DataRow("Windows.ApplicationModel.FullTrustAppContract", 2, "10.0.22000.0")]
    [DataRow("Windows.ApplicationModel.Calls.Background.CallsBackgroundContract", 1, "10.0.10586.0")]
    [DataRow("Windows.Foundation.UniversalApiContract", 17, "10.0.26100.0")]
    [DataRow("Windows.UI.Xaml.Hosting.HostingContract", 5, "10.0.22000.0")]
    public void ContractVersion_ProjectsTheIntroducingPlatform(string contract, int version, string platform)
    {
        string directory = Directory.CreateTempSubdirectory("ProjectionPlatformTest_").FullName;

        try
        {
            string metadataPath = CreateFromStringMetadata.Create(directory);
            ModuleDefinition module = ModuleDefinition.FromBytes(File.ReadAllBytes(metadataPath));
            AssemblyReference foundation = module.AssemblyReferences.Single(
                reference => reference.Name == "Windows.Foundation.FoundationContract");
            TypeReference attributeType = new(module, foundation, "Windows.Foundation.Metadata", "ContractVersionAttribute");
            MemberReference constructor = new(attributeType, ".ctor", MethodSignature.CreateInstance(
                module.CorLibTypeFactory.Void, [module.CorLibTypeFactory.String, module.CorLibTypeFactory.UInt32]));
            TypeDefinition type = module.TopLevelTypes.Single(type => type.Name == "BlittableStruct");
            type.CustomAttributes.Add(new CustomAttribute(constructor, new CustomAttributeSignature(
                new CustomAttributeArgument(module.CorLibTypeFactory.String, contract),
                new CustomAttributeArgument(module.CorLibTypeFactory.UInt32, (uint)version << 16))));
            module.Write(metadataPath);

            string reference = string.Join("\n", ProjectionWriterRunner.GenerateSources(
                true, $"sdk,{metadataPath}", "TestComponentCSharp.BlittableStruct"));
            string implementation = string.Join("\n", ProjectionWriterRunner.GenerateSources(
                false, $"sdk,{metadataPath}", "TestComponentCSharp.BlittableStruct"));

            StringAssert.Contains(reference, $"SupportedOSPlatform(\"Windows{platform}\")");
            Assert.IsFalse(implementation.Contains("SupportedOSPlatform", StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
