using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestComponentCSharp;

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
}
