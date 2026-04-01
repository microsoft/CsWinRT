// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
#if !NET
using System.Reflection;
#endif
using System.Runtime.CompilerServices;
using WinRT.Interop;

namespace WinRT
{
#if EMBED
    internal 
#else
    public
#endif 
    static class CastExtensions
    {
        /// <summary>
        /// Cast a WinRT object to an interface type it implements in its implementation
        /// even if it doesn't implement it in metadata.
        /// </summary>
        /// <typeparam name="TInterface">The interface type to cast to.</typeparam>
        /// <param name="value">The object.</param>
        /// <returns>
        /// If <typeparamref name="TInterface"/> is <see cref="object"/>, returns the "default" wrapper for this WinRT object that implements all of the types that this object implements in metadata.
        /// If <paramref name="value"/> implements <typeparamref name="TInterface"/> in metadata, casts <paramref name="value"/> to <typeparamref name="TInterface"/>.
        /// Otherwise, creates a new wrapper of the underlying WinRT object that implements <typeparamref name="TInterface"/>.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown if the runtime type of <paramref name="value"/> is not a projected type (if the object is a managed object).</exception>
        public static TInterface As<TInterface>(this object value)
        {
            IObjectReference objRef;

            if (typeof(TInterface) == typeof(object))
            {
                if (TryGetRefForObject(value, allowComposed: false, out objRef))
                {
                    using (objRef)
                    {
                        return ComWrappersSupport.CreateRcwForComObject<TInterface>(objRef.ThisPtr);
                    }
                }
            }

            if (value is TInterface convertableInMetadata)
            {
                return convertableInMetadata;
            }

            if (TryGetRefForObject(value, allowComposed: true, out objRef))
            {
                using (objRef)
                {
                    return objRef.AsInterface<TInterface>();
                }
            }

            // Shared helper to get an exception with a helpful message, that we can efficiently throw
            [MethodImpl(MethodImplOptions.NoInlining)]
            static Exception GetArgumentExceptionForFailedCast(object value)
            {
                if (value is null)
                {
                    return new ArgumentNullException(
                        paramName: nameof(value),
                        message: $"The source object being cast to type '{typeof(TInterface)}' is 'null'.");
                }

                return new ArgumentException(
                    paramName: nameof(value),
                    message: $"The source object type ('{value.GetType()}') being cast to type '{typeof(TInterface)}' is not a projected type, nor does it inherit from a projected type.");
            }

            throw GetArgumentExceptionForFailedCast(value);
        }

        /// <summary>
        /// Create an agile reference for a given WinRT object.  The agile reference can be passed to another apartment
        /// within the process from which the original object can be retrieved even if it wasn't agile.
        /// </summary>
        /// <typeparam name="T">Type of WinRT object.</typeparam>
        /// <param name="value">The object.</param>
        /// <returns>
        /// If <paramref name="value"/> is a WinRT object, returns a AgileReference for it.
        /// Otherwise, returns null.
        /// </returns>
        /// <exception cref="InvalidOperationException">Thrown if the runtime type of <paramref name="value"/> is not a projected type.</exception>
        public static AgileReference<T> AsAgile<T>(this T value) where T : class
        {
            if(value == null)
            {
                return new AgileReference<T>(null);
            }

            var objrefValue = MarshalInspectable<T>.CreateMarshaler2(value);
            try
            {
                return new AgileReference<T>(objrefValue);
            }
            finally
            {
                objrefValue.Dispose();
            }
        }

        private static bool TryGetRefForObject(object value, bool allowComposed, out IObjectReference reference)
        {
            if (ComWrappersSupport.TryUnwrapObject(value, out var objRef))
            {
                reference = objRef.As<IUnknownVftbl>(IID.IID_IUnknown);
                return true;
            }
            else if (allowComposed && TryGetComposedRefForQI(value, out objRef))
            {
                reference = objRef.As<IUnknownVftbl>(IID.IID_IUnknown);
                return true;
            }
            reference = null;
            return false;
        }

        private static bool TryGetComposedRefForQI(object value, out IObjectReference objRef)
        {
#if !NET
            var getReferenceMethod = value.GetType().GetMethod("GetDefaultReference", BindingFlags.NonPublic | BindingFlags.Instance).MakeGenericMethod(typeof(IUnknownVftbl));
            if (getReferenceMethod is null)
            {
                objRef = null;
                return false;
            }
            objRef = (IObjectReference)getReferenceMethod.Invoke(value, Array.Empty<object>());
            return true;
#else
            if(value is IWinRTObject winrtObj)
            {
                objRef = winrtObj.NativeObject;
                return true;
            }
            objRef = null;
            return false;
#endif
        }
    }
}
