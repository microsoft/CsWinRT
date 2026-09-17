// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using ProjectionWriterTest.Helpers;

namespace ProjectionWriterTest;

/// <summary>
/// Tests the names used to construct generic event sources in 'WinRT.Interop'.
/// </summary>
[TestClass]
public class Test_EventSources
{
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
