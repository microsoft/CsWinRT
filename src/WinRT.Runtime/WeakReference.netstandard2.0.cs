using System;
using System.Collections.Generic;
using System.Text;
using WinRT.Interop;

namespace WinRT
{
    /// <summary>
    /// Provides a weak reference to a C#/WinRT wrapper of a WinRT object.
    /// The lifetime of the weak reference is the lifetime of the native object instead of the C#/WinRT wrapper.
    /// </summary>
    /// <typeparam name="T">The type of object the weak reference points to.</typeparam>
    public sealed class WeakReference<T>
        where T : class
    {
        private System.WeakReference<T> _managedWeakReference;
        private IWeakReference _nativeWeakReference;
        public WeakReference(T target)
        {
            _managedWeakReference = new System.WeakReference<T>(target);
            if (target is object && ComWrappersSupport.TryUnwrapObject(target, out var objRef))
            {
                _nativeWeakReference = target.As<IWeakReferenceSource>().GetWeakReference();
            }
        }

        public void SetTarget(T target)
        {
            lock(_managedWeakReference)
            {
                _managedWeakReference.SetTarget(target);
                if (target is object && ComWrappersSupport.TryUnwrapObject(target, out _))
                {
                    _nativeWeakReference = target.As<IWeakReferenceSource>().GetWeakReference();
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

        private static object ResolveNativeWeakReference(IWeakReference reference)
        {
            if (reference is null)
            {
                return null;
            }
            using var resolved = reference.Resolve(typeof(IUnknownVftbl).GUID);
            return ComWrappersSupport.CreateRcwForComObject<object>(resolved.ThisPtr);
        }
    }
}
