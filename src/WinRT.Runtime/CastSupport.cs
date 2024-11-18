using System;
using System.ComponentModel;

namespace WinRT
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class CastSupport
    {
        /// <summary>
        /// Register a runtime class in the cache.
        /// </summary>
        /// <param name="runtimeClassName">The runtime class name to register.</param>
        /// <param name="runtimeClass">The runtime class type to be registered.</param>
        /// <remarks>This method is only meant to be used in AotOptimizer, in order to allow casting from <see cref="object"/> to a WinRT class.</remarks>
        public static void RegisterTypeName(string runtimeClassName, Type runtimeClass)
            => TypeNameSupport.RegisterTypeName(runtimeClassName, runtimeClass);
    }
}
