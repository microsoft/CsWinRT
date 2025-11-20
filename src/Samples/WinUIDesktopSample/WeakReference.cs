using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Wrapper around System.WeakReference until XAML removes its dependency on it.
namespace WinRT
{
    public sealed class WeakReference<T>
        where T : class
    {
        private System.WeakReference<T> _managedWeakReference;

        public WeakReference(T target)
        {
            _managedWeakReference = new System.WeakReference<T>(target);
        }

        public void SetTarget(T target)
        {
            lock (_managedWeakReference)
            {
                _managedWeakReference.SetTarget(target);
            }
        }

        public bool TryGetTarget(out T target)
        {
            lock (_managedWeakReference)
            {
                return _managedWeakReference.TryGetTarget(out target);
            }
        }
    }
}