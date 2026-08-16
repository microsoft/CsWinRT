// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTest;

[TestClass]
public class BindableAdapterTests
{
    private static readonly MethodInfo GetViewMethod = GetBindableIListAdapterType().GetMethod(
        "GetView",
        BindingFlags.Public | BindingFlags.Static)!;

    [TestMethod]
    public void GetViewReusesAdapterForSameList()
    {
        IList list = new ArrayList();

        object first = GetView(list);
        object second = GetView(list);

        Assert.AreSame(first, second);
    }

    [TestMethod]
    public void GetViewUsesDifferentAdaptersForDifferentLists()
    {
        object first = GetView(new ArrayList());
        object second = GetView(new ArrayList());

        Assert.AreNotSame(first, second);
    }

    [TestMethod]
    public void GetViewReflectsChangesToUnderlyingList()
    {
        IList list = new ArrayList { 1 };
        object view = GetView(list);

        list.Add(2);

        CollectionAssert.AreEqual(new object[] { 1, 2 }, ((IEnumerable)view).Cast<object>().ToArray());
    }

    [TestMethod]
    public void GetViewConcurrentlyReusesAdapter()
    {
        IList list = new ArrayList();
        object[] views = new object[32];

        Parallel.For(0, views.Length, i => views[i] = GetView(list));

        Assert.IsTrue(views.All(view => ReferenceEquals(views[0], view)));
    }

    [TestMethod]
    public void GetViewCacheDoesNotRootListOrAdapter()
    {
        (WeakReference list, WeakReference view) = CreateWeakReferences();

        CollectGarbageWhile(() => list.IsAlive || view.IsAlive);

        Assert.IsFalse(list.IsAlive);
        Assert.IsFalse(view.IsAlive);
    }

    [TestMethod]
    public void GetViewCacheRetainsAdapterWhileListIsAlive()
    {
        IList list = new ArrayList();
        WeakReference view = CreateViewWeakReference(list);

        CollectGarbageWhile(() => true);

        Assert.IsTrue(view.IsAlive);
        Assert.AreSame(view.Target, GetView(list));
        GC.KeepAlive(list);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (WeakReference List, WeakReference View) CreateWeakReferences()
    {
        IList list = new ArrayList();
        object view = GetView(list);

        return (new WeakReference(list), new WeakReference(view));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateViewWeakReference(IList list)
    {
        return new WeakReference(GetView(list));
    }

    private static void CollectGarbageWhile(Func<bool> condition)
    {
        for (int i = 0; i < 3 && condition(); i++)
        {
            GC.Collect(2, GCCollectionMode.Forced, blocking: true);
            GC.WaitForPendingFinalizers();
        }
    }

    [DynamicDependency(
        DynamicallyAccessedMemberTypes.PublicMethods,
        "WindowsRuntime.InteropServices.BindableIListAdapter",
        "WinRT.Runtime")]
    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
    private static Type GetBindableIListAdapterType()
    {
        return Type.GetType(
            "WindowsRuntime.InteropServices.BindableIListAdapter, WinRT.Runtime",
            throwOnError: true)!;
    }

    private static object GetView(IList list)
    {
        return GetViewMethod.Invoke(null, [list])!;
    }
}
