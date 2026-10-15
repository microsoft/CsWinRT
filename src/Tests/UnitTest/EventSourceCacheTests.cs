// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestComponentCSharp;
using Windows.Foundation.Collections;

namespace UnitTest;

[TestClass]
public class EventSourceCacheTests
{
    [TestMethod]
    public void MultipleEventsRemainUnsubscribableAfterCollection()
    {
        bool intEventCalled = false;
        bool boolEventCalled = false;
        void OnIntPropertyChanged(object sender, int value) => intEventCalled = true;
        void OnBoolPropertyChanged(object sender, bool value) => boolEventCalled = true;

        var classInstance = new Class();
        classInstance.IntPropertyChanged += OnIntPropertyChanged;
        classInstance.BoolPropertyChanged += OnBoolPropertyChanged;
        classInstance.RaiseIntChanged();
        classInstance.RaiseBoolChanged();

        Assert.IsTrue(intEventCalled);
        Assert.IsTrue(boolEventCalled);

        intEventCalled = false;
        boolEventCalled = false;

        GC.Collect(2, GCCollectionMode.Forced, true);
        GC.WaitForPendingFinalizers();

        classInstance.IntPropertyChanged -= OnIntPropertyChanged;
        classInstance.BoolPropertyChanged -= OnBoolPropertyChanged;
        classInstance.RaiseIntChanged();
        classInstance.RaiseBoolChanged();

        Assert.IsFalse(intEventCalled);
        Assert.IsFalse(boolEventCalled);
    }

    // Unsubscribing through a new wrapper for the same native object requires the event states to be
    // recovered from the event source cache. Both events share a single cache entry for that object.
    [TestMethod]
    public void MultipleEventsRemainUnsubscribableThroughNewWrapper()
    {
        bool intEventCalled = false;
        bool boolEventCalled = false;
        EventHandler<int> onIntPropertyChanged = (sender, value) => intEventCalled = true;
        EventHandler<bool> onBoolPropertyChanged = (sender, value) => boolEventCalled = true;

        // Keep the native object alive without keeping its managed wrapper alive
        PropertySet holder = [];
        GCHandle originalWrapper = SubscribeAndStore(holder, onIntPropertyChanged, onBoolPropertyChanged);

        try
        {
            Assert.IsTrue(intEventCalled);
            Assert.IsTrue(boolEventCalled);

            intEventCalled = false;
            boolEventCalled = false;

            GC.Collect(2, GCCollectionMode.Forced, true);
            GC.WaitForPendingFinalizers();
            GC.Collect(2, GCCollectionMode.Forced, true);

            // A weak 'GCHandle' is used rather than 'WeakReference<T>', as the latter can create a new wrapper
            // for a native object that is still alive, if that object implements 'IWeakReferenceSource'.
            Assert.IsNull(originalWrapper.Target, "The original wrapper must be collected to exercise the cache.");
        }
        finally
        {
            originalWrapper.Free();
        }

        Class classInstance = (Class)holder["instance"];

        // The handlers must still be registered after the original wrapper is collected, otherwise
        // the checks below would pass even if the unsubscriptions had never reached the native object.
        classInstance.RaiseIntChanged();
        classInstance.RaiseBoolChanged();

        Assert.IsTrue(intEventCalled);
        Assert.IsTrue(boolEventCalled);

        intEventCalled = false;
        boolEventCalled = false;

        classInstance.IntPropertyChanged -= onIntPropertyChanged;
        classInstance.BoolPropertyChanged -= onBoolPropertyChanged;
        classInstance.RaiseIntChanged();
        classInstance.RaiseBoolChanged();

        Assert.IsFalse(intEventCalled);
        Assert.IsFalse(boolEventCalled);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static GCHandle SubscribeAndStore(
            PropertySet holder,
            EventHandler<int> onIntPropertyChanged,
            EventHandler<bool> onBoolPropertyChanged)
        {
            Class classInstance = new();
            classInstance.IntPropertyChanged += onIntPropertyChanged;
            classInstance.BoolPropertyChanged += onBoolPropertyChanged;
            classInstance.RaiseIntChanged();
            classInstance.RaiseBoolChanged();

            holder["instance"] = classInstance;

            return GCHandle.Alloc(classInstance, GCHandleType.Weak);
        }
    }
}
