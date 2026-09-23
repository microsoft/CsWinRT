// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.ProjectionWriter.Helpers;

namespace ProjectionWriterTest;

/// <summary>
/// Covers how the assembly marker in an interop type name is chosen, which has to agree with the marshallers
/// the interop generator emits: it resolves the projection each type actually lands in, so a contract shipped
/// by a component is named after itself even when it sits under the <c>Windows</c> namespace root.
/// </summary>
[TestClass]
public class Test_InteropAssemblyMarker
{
    /// <summary>
    /// Sharing the <c>Windows</c> namespace root is not enough to belong to the Windows SDK, which ships its
    /// union as <c>Windows</c> and each contract as <c>Windows.&lt;Area&gt;.&lt;Name&gt;Contract</c>. Naming a
    /// third party contract's marshallers <c>&lt;#Windows&gt;</c> asks for ones that were never generated.
    /// </summary>
    [TestMethod]
    [DataRow("Windows", "Windows.UI", "Color", "<#Windows>")]
    [DataRow("Windows.Foundation.FoundationContract", "Windows.Foundation", "Uri", "<#Windows>")]
    [DataRow("Windows.Foundation.UniversalApiContract", "Windows.Storage", "StorageFile", "<#Windows>")]
    [DataRow("Windows.UI.Contoso", "Windows.UI.Contoso", "WidgetItemView", "<Windows-UI-Contoso>")]
    [DataRow("Contoso.Widgets", "Windows.UI.Contoso", "WidgetItemView", "<Contoso-Widgets>")]
    public void WindowsNamespace_IsMarkedByItsContract_NotByItsNamespace(
        string assemblyName,
        string typeNamespace,
        string typeName,
        string expected)
    {
        Assert.AreEqual(expected, GetMarker(assemblyName, typeNamespace, typeName));
    }

    /// <summary>
    /// A <c>.winmd</c> authored in C# references the Windows SDK through its managed projection assembly
    /// rather than a contract, so those names count as the SDK too. <c>WindowsRuntime.Internal.winmd</c> is
    /// built that way, and its interop interfaces return SDK types.
    /// </summary>
    [TestMethod]
    [DataRow("Microsoft.Windows.SDK.NET", "Windows.Security.Credentials.UI", "UserConsentVerificationResult", "<#Windows>")]
    [DataRow("Microsoft.Windows.SDK.NET", "Windows.Foundation", "Uri", "<#Windows>")]
    [DataRow("Microsoft.Windows.UI.Xaml", "Windows.UI.Xaml", "DependencyObject", "<#Windows>")]
    public void ManagedWindowsSdkProjectionAssemblies_AreTheSdk(
        string assemblyName,
        string typeNamespace,
        string typeName,
        string expected)
    {
        Assert.AreEqual(expected, GetMarker(assemblyName, typeNamespace, typeName));
    }

    /// <summary>
    /// Types outside the <c>Windows</c> namespace root keep naming their own contract, and the CsWinRT runtime
    /// types keep their own marker.
    /// </summary>
    [TestMethod]
    [DataRow("AuthoringTest", "AuthoringTest", "Widget", "<AuthoringTest>")]
    [DataRow("WinRT.Runtime", "WindowsRuntime.InteropServices", "Anything", "<#CsWinRT>")]
    public void OtherNamespaces_AreUnchanged(
        string assemblyName,
        string typeNamespace,
        string typeName,
        string expected)
    {
        Assert.AreEqual(expected, GetMarker(assemblyName, typeNamespace, typeName));
    }

    /// <summary>
    /// Builds a type reference scoped to the given Windows Runtime contract and asks for its assembly marker.
    /// </summary>
    private static string GetMarker(string assemblyName, string typeNamespace, string typeName)
    {
        ModuleDefinition module = new("Test.winmd") { RuntimeVersion = "WindowsRuntime 1.4" };

        AssemblyReference contract = new(assemblyName, new Version(255, 255, 255, 255))
        {
            Attributes = AssemblyAttributes.ContentWindowsRuntime
        };

        TypeReference type = new(module, contract, typeNamespace, typeName);

        return InteropTypeNameWriter.GetInteropAssemblyMarker(typeNamespace, typeName, mapped: null, type);
    }
}
