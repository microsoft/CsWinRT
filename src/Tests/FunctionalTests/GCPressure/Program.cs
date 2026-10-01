using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Runtime;
using System.Threading;
using TestComponentCSharp;

// Validates that 'CsWinRTEnableBaseGCPressure' flows to the runtime configuration
if (!AppContext.TryGetSwitch("CSWINRT_ENABLE_BASE_GC_PRESSURE", out bool isBaseGCPressureEnabled) || isBaseGCPressureEnabled)
{
    return 101;
}

// The GC runtime events are only reliably observable via 'EventListener' when running on CoreCLR. We can't use
// 'RuntimeFeature.IsDynamicCodeCompiled' to check for that, as it's 'false' for AOT-compatible projects on CoreCLR
// too. Instead, check whether any methods were JIT compiled (which is always 0 on NativeAOT).
bool isCoreClr = JitInfo.GetCompiledMethodCount() > 0;

using MemoryPressureListener listener = isCoreClr ? new MemoryPressureListener() : null;

// Use a marker amount to make sure the listener is receiving the memory pressure events before running the test
if (listener is not null && !listener.WaitForMarker(MemoryPressureListener.StartMarker))
{
    return 102;
}

listener?.Reset();

// Static event raised with a new runtime class instance as args every time (like 'CompositionTarget.Rendering').
// This also creates the object reference for the activation factory of 'Class', used for all static members.
const int eventCount = 1000;
int eventsReceived = 0;
EventHandler<object> handler = (sender, e) =>
{
    // On NativeAOT, the args might not be resolved to their projected type (and use an 'IInspectable' wrapper instead), as the
    // runtime class name lookup relies on the projected type being preserved. Both paths create an RCW from a new native object.
    if (sender is null && (e is ObjectEventArgs args ? args.Value == eventsReceived : e is not null && !isCoreClr))
    {
        eventsReceived++;
    }
};

Class.StaticObjectEvent += handler;
Class.RaiseStaticObjectEvent(eventCount);
Class.StaticObjectEvent -= handler;

if (eventsReceived != eventCount)
{
    return 103;
}

// Other static members
Class.StaticIntProperty = 42;
if (Class.StaticIntProperty != 42)
{
    return 104;
}

// Instance RCWs, both activated and returned from native code
for (int i = 0; i < 100; i++)
{
    var instance = new Class();
    instance.IntProperty = i;
    if (instance.IntProperty != i)
    {
        return 105;
    }

    var fromString = Class.CreateFromString("Hello");
    if (fromString.StringProperty != "Hello")
    {
        return 106;
    }
}

// Make sure the object references are finalized (and disposed) with the switch off as well
GC.Collect();
GC.WaitForPendingFinalizers();
GC.Collect();

if (listener is not null)
{
    // Events are delivered in order, so once the end marker is observed, all events from the test have been received
    if (!listener.WaitForMarker(MemoryPressureListener.EndMarker))
    {
        return 107;
    }

    // No base memory pressure should have been reported (the amount used by 'IObjectReference' is 1000 bytes)
    if (listener.GetCount(1000) != 0)
    {
        return 108;
    }
}

return 100;

/// <summary>
/// An <see cref="EventListener"/> tracking the amounts of GC memory pressure being added.
/// </summary>
sealed class MemoryPressureListener : EventListener
{
    /// <summary>The marker amount used to detect the listener is receiving events.</summary>
    public const ulong StartMarker = 123_457;

    /// <summary>The marker amount used to detect all events from the test have been received.</summary>
    public const ulong EndMarker = 234_567;

    /// <summary>The number of times each memory pressure amount was added.</summary>
    private readonly Dictionary<ulong, int> _counts = new();

    /// <summary>
    /// Adds and removes the specified marker amount of GC memory pressure, and waits for the event to be received.
    /// </summary>
    /// <param name="marker">The marker amount to use.</param>
    /// <returns>Whether the marker event was received.</returns>
    public bool WaitForMarker(ulong marker)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < TimeSpan.FromSeconds(30))
        {
            GC.AddMemoryPressure((long)marker);
            GC.RemoveMemoryPressure((long)marker);

            Thread.Sleep(100);

            if (GetCount(marker) > 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the number of times the specified amount of GC memory pressure was added.
    /// </summary>
    /// <param name="amount">The amount of GC memory pressure.</param>
    /// <returns>The number of times <paramref name="amount"/> was added.</returns>
    public int GetCount(ulong amount)
    {
        lock (_counts)
        {
            return _counts.TryGetValue(amount, out int count) ? count : 0;
        }
    }

    /// <summary>
    /// Resets all tracked counts.
    /// </summary>
    public void Reset()
    {
        lock (_counts)
        {
            _counts.Clear();
        }
    }

    /// <inheritdoc/>
    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        // Keyword 0x1 is GC, and 'IncreaseMemoryPressure' is a verbose event
        if (eventSource.Name == "Microsoft-Windows-DotNETRuntime")
        {
            EnableEvents(eventSource, EventLevel.Verbose, (EventKeywords)0x1);
        }
    }

    /// <inheritdoc/>
    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        // Event 200 is 'IncreaseMemoryPressure'
        if (eventData.EventId != 200 || eventData.PayloadNames is null || eventData.Payload is null)
        {
            return;
        }

        int index = eventData.PayloadNames.IndexOf("BytesAllocated");

        if (index < 0)
        {
            return;
        }

        ulong amount = Convert.ToUInt64(eventData.Payload[index]);

        lock (_counts)
        {
            _counts[amount] = _counts.TryGetValue(amount, out int count) ? count + 1 : 1;
        }
    }
}
