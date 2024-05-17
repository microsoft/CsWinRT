// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using WinRT.Interop;

namespace WinRT
{
    /// <summary>
    /// Provides a weak reference to a C#/WinRT wrapper of a WinRT object.
    /// The lifetime of the weak reference is the lifetime of the native object instead of the C#/WinRT wrapper.
    /// </summary>
    /// <typeparam name="T">The type of object the weak reference points to.</typeparam>
#if EMBED
    internal
#else
    public
#endif
    sealed class WeakReference<T>
        where T : class
    {
        private System.WeakReference<T> _managedWeakReference;
        private global::WinRT.Interop.IWeakReference _nativeWeakReference;
        public WeakReference(T target)
        {
            _managedWeakReference = new System.WeakReference<T>(target);
            if (target is object && ComWrappersSupport.TryUnwrapObject(target, out var _))
            {
                _nativeWeakReference = target.As<global::WinRT.Interop.IWeakReferenceSource>().GetWeakReference();
            }
        }

        public void SetTarget(T target)
        {
            lock(_managedWeakReference)
            {
                _managedWeakReference.SetTarget(target);
                if (target is object && ComWrappersSupport.TryUnwrapObject(target, out _))
                {
                    _nativeWeakReference = target.As<global::WinRT.Interop.IWeakReferenceSource>().GetWeakReference();
                }
            }
        }

        public bool TryGetTarget(out T target)
        {
            lock (_managedWeakReference)
            {
                if (_managedWeakReference.TryGetTarget(out target))
                {
                    return true;
                }
                else
                {
                    target = ResolveNativeWeakReference(_nativeWeakReference)?.As<T>();
                    _managedWeakReference.SetTarget(target);
                    return target is object;
                }
            }
        }

        private static object ResolveNativeWeakReference(global::WinRT.Interop.IWeakReference reference)
        {
            if (reference is null)
            {
                return null;
            }
            using var resolved = reference.Resolve(IUnknownVftbl.IID);
            return ComWrappersSupport.CreateRcwForComObject<object>(resolved.ThisPtr);
        }
    }
}
