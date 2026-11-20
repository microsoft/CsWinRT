// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WindowsRuntime.InteropServices;

namespace UnitTest;

/// <summary>
/// Tests for the <see cref="IEnumeratorAdapter{T}"/> mapping table.
/// </summary>
[TestClass]
public class EnumeratorAdapterTests
{
    [TestMethod]
    public void GetInstance_SameEnumerator_ReturnsSameAdapter()
    {
        DualEnumerator enumerator = new();

        IEnumeratorAdapter<int> adapter = IEnumeratorAdapter<int>.GetInstance(enumerator);

        Assert.AreSame(adapter, IEnumeratorAdapter<int>.GetInstance(enumerator));
        Assert.AreSame(adapter, IEnumeratorAdapter<int>.GetInstance(adapter));
    }

    // An enumerator implementing 'IEnumerator<T>' for multiple 'T'-s needs a separate, stateful adapter for each of them
    [TestMethod]
    public void GetInstance_EnumeratorWithMultipleElementTypes_ReturnsDistinctAdapters()
    {
        DualEnumerator enumerator = new();

        IEnumeratorAdapter<int> intAdapter = IEnumeratorAdapter<int>.GetInstance(enumerator);
        IEnumeratorAdapter<object> objectAdapter = IEnumeratorAdapter<object>.GetInstance(enumerator);

        Assert.AreNotSame<object>(intAdapter, objectAdapter);
        Assert.AreSame(intAdapter, IEnumeratorAdapter<int>.GetInstance(enumerator));
        Assert.AreSame(objectAdapter, IEnumeratorAdapter<object>.GetInstance(enumerator));

        // Each adapter must advance the enumerator on its own first access
        Assert.IsTrue(intAdapter.HasCurrent);
        Assert.AreEqual(1, intAdapter.Current);
        Assert.IsTrue(objectAdapter.HasCurrent);
        Assert.AreEqual(2, objectAdapter.Current);
    }

    [WindowsRuntimeManagedOnlyType]
    private sealed class DualEnumerator : IEnumerator<int>, IEnumerator<object>
    {
        private int _current;

        public bool MoveNext()
        {
            _current++;

            return true;
        }

        int IEnumerator<int>.Current => _current;

        object IEnumerator<object>.Current => _current;

        object IEnumerator.Current => _current;

        void IEnumerator.Reset()
        {
            _current = 0;
        }

        void IDisposable.Dispose()
        {
        }
    }
}
