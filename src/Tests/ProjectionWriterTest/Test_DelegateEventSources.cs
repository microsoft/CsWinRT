// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using ProjectionWriterTest.Helpers;
using WindowsRuntime.ProjectionWriter;

namespace ProjectionWriterTest;

/// <summary>
/// Covers the event sources emitted for projected (non-generic) delegates, in particular what happens when
/// the native event source invokes the managed event handler after all managed handlers were removed.
/// </summary>
/// <remarks>
/// A native event source can keep dispatching to the CCW of the event handler after the last managed handler
/// has been removed (eg. if the unsubscription races an event that was already in flight). That invocation
/// must not throw, as the exception would escape the CCW and be reported (and fail fast) by the native caller.
/// </remarks>
[TestClass]
public unsafe class Test_DelegateEventSources
{
    private static Assembly s_projection = null!;

    [ClassInitialize]
    public static void Initialize(TestContext _)
    {
        string directory = Directory.CreateTempSubdirectory("ProjectionDelegateEventSourcesTest_").FullName;

        try
        {
            string outputFolder = Path.Combine(directory, "Generated");

            ProjectionWriter.Run(new ProjectionWriterOptions
            {
                InputPaths = [EventDelegateMetadata.Create(directory)],
                OutputFolder = outputFolder,
                OutputAssemblyName = "Projection",
                Include = ["Contoso"]
            });

            string[] sources = [.. Directory.GetFiles(outputFolder, "*.cs").Select(File.ReadAllText)];
            // Compiling every delegate shape in the fixture (including by-reference struct and array parameters) validates the generated lambdas
            string assemblyPath = ProjectionWriterRunner.CompileSources(sources, Path.Combine(directory, "Projection.dll"));

            // Load from memory, so that the temporary directory can be deleted
            s_projection = Assembly.Load(File.ReadAllBytes(assemblyPath));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// A 'void' delegate with output parameters can't just use a conditional access, as outputs must be assigned.
    /// </summary>
    [TestMethod]
    public void VoidDelegateWithOutputParameter_AssignsOutputAndReturns()
    {
        using EventStateHarness harness = new("VoidOutputHandler");

        object?[] arguments = ["stale"];

        Assert.IsNull(harness.EventInvoke.DynamicInvoke(arguments));
        Assert.IsNull(arguments[0]);
    }

    /// <summary>
    /// With no target delegate, invoking a 'void' delegate does nothing.
    /// </summary>
    [TestMethod]
    public void VoidDelegate_WithNoTargetDelegate_DoesNothing()
    {
        using EventStateHarness harness = new("VoidHandler");

        object?[] arguments = [null, 42];

        Assert.IsNull(harness.EventInvoke.DynamicInvoke(arguments));

        // Handlers receive the arguments while subscribed
        int calls = 0;
        Delegate handler = Delegate.CreateDelegate(harness.DelegateType, new Action<object?, int>((sender, value) => calls += value), "Invoke");

        harness.AddHandler(handler);
        _ = harness.EventInvoke.DynamicInvoke(arguments);

        Assert.AreEqual(42, calls);

        // Once the last handler is removed, the invocation is not forwarded, and does not fail
        harness.RemoveHandler(handler);
        _ = harness.EventInvoke.DynamicInvoke(arguments);

        Assert.AreEqual(42, calls);
    }

    /// <summary>
    /// With no target delegate, invoking a delegate returning a value returns the default value.
    /// </summary>
    [TestMethod]
    public void ResultDelegate_WithNoTargetDelegate_ReturnsDefault()
    {
        using EventStateHarness harness = new("ResultHandler");

        object?[] arguments = [7];

        Assert.AreEqual(0, harness.EventInvoke.DynamicInvoke(arguments));

        Delegate handler = Delegate.CreateDelegate(harness.DelegateType, new Func<int, int>(value => value * 2), "Invoke");

        harness.AddHandler(handler);

        Assert.AreEqual(14, harness.EventInvoke.DynamicInvoke(arguments));

        harness.RemoveHandler(handler);

        Assert.AreEqual(0, harness.EventInvoke.DynamicInvoke(arguments));
    }

    /// <summary>
    /// With no target delegate, output parameters are assigned their default value, and the default value is returned.
    /// </summary>
    [TestMethod]
    public void OutputDelegate_WithNoTargetDelegate_AssignsDefaultOutputs()
    {
        using EventStateHarness harness = new("OutputHandler");

        object?[] arguments = [7, "stale", new int[] { 1 }];

        Assert.AreEqual(false, harness.EventInvoke.DynamicInvoke(arguments));
        Assert.IsNull(arguments[1]);
        Assert.IsNull(arguments[2]);
    }

    /// <summary>
    /// Wraps the generated 'EventState' type of a delegate event source, which is internal to the runtime.
    /// </summary>
    private sealed class EventStateHarness : IDisposable
    {
        private readonly object _state;
        private readonly Type _stateBaseType;

        public EventStateHarness(string delegateName)
        {
            Type eventSourceType = s_projection.GetType($"ABI.Contoso.{delegateName}EventSource", throwOnError: true)!;
            Type stateType = eventSourceType.GetNestedType("EventState", BindingFlags.NonPublic)!;

            DelegateType = s_projection.GetType($"Contoso.{delegateName}", throwOnError: true)!;

            // The pointer is only used as a key for the event source cache, and never dereferenced
            _state = stateType.GetConstructors()[0].Invoke([Pointer.Box((void*)1, typeof(void*)), 0]);
            _stateBaseType = stateType.BaseType!;

            EventInvoke = (Delegate)_stateBaseType.GetProperty("EventInvoke", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(_state)!;
        }

        public Type DelegateType { get; }

        public Delegate EventInvoke { get; }

        public void AddHandler(Delegate handler)
        {
            _ = _stateBaseType.GetMethod("AddHandler", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(_state, [handler]);
        }

        public void RemoveHandler(Delegate handler)
        {
            _ = _stateBaseType.GetMethod("RemoveHandler", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(_state, [handler]);
        }

        public void Dispose()
        {
            ((IDisposable)_state).Dispose();
        }
    }
}
