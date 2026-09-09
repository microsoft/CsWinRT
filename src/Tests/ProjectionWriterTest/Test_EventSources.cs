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
    [DataRow("VectorChangedEventHandlerEventSource'1<<#Windows>Windows-UI-Xaml-DependencyObject>")]
    [DataRow("VectorChangedEventHandlerEventSource'1<object>")]
    [DataRow("MapChangedEventHandlerEventSource'2<string|string>")]
    [DataRow("MapChangedEventHandlerEventSource'2<string|object>")]
    [DataRow("EventHandlerEventSource'1<<#Windows>Windows-Foundation-Diagnostics-TracingStatusChangedEventArgs>")]
    [DataRow("EventHandlerEventSource'2<<#Windows>Windows-Foundation-Diagnostics-ILoggingChannel|object>")]
    public void GenericEvent_UsesInteropEventSourceConstructor(string eventSourceName)
    {
        string attribute = $"[return: UnsafeAccessorType(\"ABI.WindowsRuntime.InteropServices.<#CsWinRT>{eventSourceName}, WinRT.Interop\")]";

        Assert.IsTrue(ProjectionWriterRunner.GetSources(referenceProjection: false).Contains(attribute),
            $"The projection should construct '{eventSourceName}' in WinRT.Interop.");
        Assert.IsFalse(ProjectionWriterRunner.GetSources(referenceProjection: true).Contains(attribute),
            "Reference projections should not reference implementation-only event sources.");
    }
}
