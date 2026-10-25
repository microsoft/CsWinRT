// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using WinMDGeneratorTest.Helpers;

namespace WinMDGeneratorTest;

/// <summary>
/// End-to-end tests for the shared type mappings consumed by the WinMD generator.
/// </summary>
[TestClass]
public sealed class Test_TypeMappings
{
    [TestMethod]
    [DataRow(false, "Microsoft.UI.Xaml")]
    [DataRow(true, "Windows.UI.Xaml")]
    public void MappedSignatures_UseSelectedXamlMode(bool useWindowsUIXamlProjections, string xaml)
    {
        var (methods, _) = WinMDGeneratorRunner.GetGeneratedApiSurface("""
            namespace TestInput;
            public interface IContract
            {
                System.DateTimeOffset GetDate();
                System.TimeSpan GetDuration();
                System.Type GetTypeName();
                System.Collections.Generic.IList<int> GetValues();
                void SetValues(System.Collections.Generic.IList<int> values);
                System.ComponentModel.INotifyPropertyChanged GetChanges();
            }
            """, useWindowsUIXamlProjections);

        Assert.AreEqual(
            "Windows.Foundation.FoundationContract::Windows.Foundation.DateTime()",
            methods["TestInput.IContract.GetDate"]);
        Assert.AreEqual(
            "Windows.Foundation.FoundationContract::Windows.Foundation.TimeSpan()",
            methods["TestInput.IContract.GetDuration"]);
        Assert.AreEqual(
            "Windows.Foundation.UniversalApiContract::Windows.UI.Xaml.Interop.TypeName()",
            methods["TestInput.IContract.GetTypeName"]);
        Assert.AreEqual(
            "Windows.Foundation.FoundationContract::Windows.Foundation.Collections.IVector`1<Int32>()",
            methods["TestInput.IContract.GetValues"]);
        Assert.AreEqual(
            "Void(Windows.Foundation.FoundationContract::Windows.Foundation.Collections.IVector`1<Int32>)",
            methods["TestInput.IContract.SetValues"]);
        Assert.AreEqual($"{xaml}::{xaml}.Data.INotifyPropertyChanged()", methods["TestInput.IContract.GetChanges"]);
    }

    [TestMethod]
    public void MappedAndOmittedInterfaceImplementations_ArePreserved()
    {
        var (methods, interfaces) = WinMDGeneratorRunner.GetGeneratedApiSurface("""
            namespace TestInput;
            public sealed class Service : System.IDisposable, System.IEquatable<Service>
            {
                public void Dispose() { }
                public bool Equals(Service other) => false;
            }
            """);

        CollectionAssert.AreEquivalent(
            new[] { "Windows.Foundation.FoundationContract::Windows.Foundation.IClosable" },
            interfaces["TestInput.Service"]);
        Assert.IsFalse(methods.ContainsKey("TestInput.Service.Equals"), "The omitted interface implementation must not be exported.");
    }
}