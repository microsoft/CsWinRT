// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using AsmResolver.DotNet;
using ProjectionWriterTest.Helpers;
using WindowsRuntime.ProjectionWriter;

namespace ProjectionWriterTest;

/// <summary>
/// Tests event-source names for generic handlers and delegates shared between projections.
/// </summary>
[TestClass]
public class Test_EventSources
{
    /// <summary>
    /// An interface's Methods class uses the delegate's fully qualified ABI event-source type, including when
    /// the delegate is declared in another namespace or supplied by a separate projection.
    /// </summary>
    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public void CrossNamespaceDelegate_UsesQualifiedEventSource(bool idic, bool separateProjection)
    {
        string directory = Directory.CreateTempSubdirectory("CrossNamespaceEventSource_").FullName;

        try
        {
            string metadata = ExclusiveToMetadata.Create(directory, fastAbi: false);
            ModuleDefinition module = ModuleDefinition.FromFile(metadata);
            module.TopLevelTypes.Single(type => type.FullName == "Contoso.ChangedHandler").Namespace = "Contoso.Events";
            metadata = Path.Combine(directory, "CrossNamespace.winmd");
            module.Write(metadata);
            string[] references = [];

            string Generate(string name, string[] includes)
            {
                string output = Path.Combine(directory, name);
                ProjectionWriter.Run(new ProjectionWriterOptions
                {
                    InputPaths = [metadata],
                    OutputFolder = output,
                    OutputAssemblyName = name,
                    IncludeTypes = includes,
                    PublicExclusiveTo = !idic,
                    IdicExclusiveTo = idic
                });
                return output;
            }

            // A separately projected delegate supplies its own public event-source implementation
            if (separateProjection)
            {
                string delegateOutput = Generate("DelegateProjection", ["Contoso.Events.ChangedHandler"]);
                references = [ProjectionWriterRunner.CompileSources(
                    Directory.GetFiles(delegateOutput, "*.cs").Select(File.ReadAllText),
                    Path.Combine(directory, "DelegateProjection.dll"))];
            }

            string[] includes = ["Contoso.Widget", "Contoso.IWidget", "Contoso.IWidget2", "Contoso.IWidget3"];
            string output = Generate("EventProjection", separateProjection ? includes : [.. includes, "Contoso.Events.ChangedHandler"]);
            string source = File.ReadAllText(Path.Combine(output, "Contoso.cs"));

            StringAssert.Contains(source, "ConditionalWeakTable<object, global::ABI.Contoso.Events.ChangedHandlerEventSource>");
            StringAssert.Contains(source, "new global::ABI.Contoso.Events.ChangedHandlerEventSource(thisReference,");
            _ = ProjectionWriterRunner.CompileSources(
                Directory.GetFiles(output, "*.cs").Select(File.ReadAllText),
                Path.Combine(directory, "EventProjection.dll"),
                additionalReferences: references);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// Specialized event sources use the runtime event-source base name, not the delegate name
    /// followed by an 'EventSource' suffix after its generic arguments.
    /// </summary>
    [TestMethod]
    [DataRow(false, "VectorChangedEventHandlerEventSource'1<<#Windows>Windows-UI-Xaml-DependencyObject>")]
    [DataRow(false, "VectorChangedEventHandlerEventSource'1<object>")]
    [DataRow(true, "VectorChangedEventHandlerEventSource'1<<Microsoft-UI-Xaml>Microsoft-UI-Xaml-DependencyObject>")]
    [DataRow(true, "VectorChangedEventHandlerEventSource'1<object>")]
    [DataRow(false, "MapChangedEventHandlerEventSource'2<string|string>")]
    [DataRow(false, "MapChangedEventHandlerEventSource'2<string|object>")]
    [DataRow(true, "MapChangedEventHandlerEventSource'2<string|string>")]
    [DataRow(true, "MapChangedEventHandlerEventSource'2<string|object>")]
    [DataRow(false, "EventHandlerEventSource'1<<#Windows>Windows-Foundation-Diagnostics-TracingStatusChangedEventArgs>")]
    [DataRow(false, "EventHandlerEventSource'2<<#Windows>Windows-Foundation-Diagnostics-ILoggingChannel|object>")]
    [DataRow(true, "EventHandlerEventSource'1<<#Windows>Windows-Foundation-Diagnostics-TracingStatusChangedEventArgs>")]
    [DataRow(true, "EventHandlerEventSource'2<<#Windows>Windows-Foundation-Diagnostics-ILoggingChannel|object>")]
    public void GenericEvent_UsesInteropEventSourceConstructor(bool useWinUI, string eventSourceName)
    {
        string attribute = $"[return: UnsafeAccessorType(\"ABI.WindowsRuntime.InteropServices.<#CsWinRT>{eventSourceName}, WinRT.Interop\")]";

        Assert.IsTrue(ProjectionWriterRunner.GetSources(referenceProjection: false, useWinUI).Contains(attribute),
            $"The projection should construct '{eventSourceName}' in WinRT.Interop.");
        Assert.IsFalse(ProjectionWriterRunner.GetSources(referenceProjection: true, useWinUI).Contains(attribute),
            "Reference projections should not reference implementation-only event sources.");
    }

    /// <summary>
    /// Each input fixture projects only its selected XAML namespaces in both output modes.
    /// </summary>
    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public void XamlCollections_UseSelectedNamespaces(bool referenceProjection, bool useWinUI)
    {
        string sources = ProjectionWriterRunner.GetSources(referenceProjection, useWinUI);
        string expectedNamespace = useWinUI ? "Microsoft.UI.Xaml" : "Windows.UI.Xaml";
        string unexpectedNamespace = useWinUI ? "Windows.UI.Xaml" : "Microsoft.UI.Xaml";

        Assert.IsTrue(sources.Contains($"namespace {expectedNamespace}"),
            $"The fixture should project '{expectedNamespace}'.");
        Assert.IsTrue(sources.Contains($"namespace {expectedNamespace}.Controls"),
            $"The fixture should project '{expectedNamespace}.Controls'.");
        Assert.IsFalse(sources.Contains($"namespace {unexpectedNamespace}"),
            $"The fixture should not project '{unexpectedNamespace}'.");
    }
}
