// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using WinRT;
using Xunit;

namespace UnitTest
{
#if NET
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
#endif
    public class EventSourceCacheTests
    {
        private static readonly Type CacheType = typeof(IObjectReference).Assembly.GetType(
            "ABI.WinRT.Interop.EventSourceCache", throwOnError: true);

        [Theory]
        [InlineData(0)]
        [InlineData(unchecked((int)0x80010108))]
        [InlineData(unchecked((int)0x800706BA))]
        public void GetStateTreatsDisconnectedTargetAsExpired(int error)
        {
            var target = new TestWeakReference { Error = CreateError(error) };
            var state = new System.WeakReference<object>(new object());
            var cache = CreateCache(target, 1, state);

            Assert.Null(GetState(cache, 1));
            Assert.Equal(1, target.ResolveCalls);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(unchecked((int)0x80010108))]
        [InlineData(unchecked((int)0x800706BA))]
        public void UpdateReplacesDisconnectedTargetAndClearsOldStates(int error)
        {
            var oldTarget = new TestWeakReference { Error = CreateError(error) };
            var oldState = new System.WeakReference<object>(new object());
            var cache = CreateCache(oldTarget, 1, oldState);
            var newTarget = new TestWeakReference { IsAlive = true };
            var newState = new System.WeakReference<object>(new object());

            Assert.Same(cache, Update(cache, newTarget, 2, newState));
            Assert.Null(GetState(cache, 1));
            Assert.Same(newState, GetState(cache, 2));
            Assert.Equal(1, oldTarget.ResolveCalls);
            Assert.Equal(2, newTarget.ResolveCalls);
        }

        [Theory]
        [InlineData(false, unchecked((int)0x80004005))]
        [InlineData(true, unchecked((int)0x80004005))]
        [InlineData(false, unchecked((int)0x80070005))]
        [InlineData(true, unchecked((int)0x80070005))]
        public void UnrelatedResolveFailuresAreNotSuppressed(bool update, int error)
        {
            var expected = CreateError(error);
            var target = new TestWeakReference { Error = expected };
            var state = new System.WeakReference<object>(new object());
            var cache = CreateCache(target, 1, state);

            var actual = Assert.Throws<TargetInvocationException>(() =>
            {
                if (update)
                {
                    Update(cache, new TestWeakReference { IsAlive = true }, 2, state);
                }
                else
                {
                    GetState(cache, 1);
                }
            });

            Assert.Same(expected, actual.InnerException);
        }

        [Fact]
        public void UpdatePreservesStatesForLiveTarget()
        {
            var target = new TestWeakReference { IsAlive = true };
            var firstState = new System.WeakReference<object>(new object());
            var secondState = new System.WeakReference<object>(new object());
            var cache = CreateCache(target, 1, firstState);

            Update(cache, target, 2, secondState);

            Assert.Same(firstState, GetState(cache, 1));
            Assert.Same(secondState, GetState(cache, 2));
        }

        // Exercise a single cache entry without depending on COM pointer reuse or global cache state.
        private static object CreateCache(WinRT.Interop.IWeakReference target, int index, System.WeakReference<object> state)
        {
            return Activator.CreateInstance(CacheType, BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null, args: new object[] { target, index, state }, culture: null);
        }

        private static object GetState(object cache, int index)
        {
            return CacheType.GetMethod("GetState", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(cache, new object[] { index });
        }

        private static object Update(object cache, WinRT.Interop.IWeakReference target, int index, System.WeakReference<object> state)
        {
            return CacheType.GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(cache, new object[] { target, index, state });
        }

        private static COMException CreateError(int error)
        {
            return error == 0 ? null : new COMException("Weak reference resolution failed.", error);
        }

        private sealed class TestWeakReference : WinRT.Interop.IWeakReference
        {
            public COMException Error { get; set; }
            public bool IsAlive { get; set; }
            public int ResolveCalls { get; private set; }

            public IObjectReference Resolve(Guid riid)
            {
                ResolveCalls++;
                if (Error != null)
                {
                    throw Error;
                }

                return IsAlive ? ComWrappersSupport.CreateCCWForObject(this) : null;
            }
        }
    }
}
