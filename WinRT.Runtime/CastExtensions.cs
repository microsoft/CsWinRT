using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using WinRT.Interop;

namespace WinRT
{
    public static class CastExtensions
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
            if (typeof(TInterface) == typeof(object))
            {
                if (TryGetRefForObject(value, allowComposed: false, out IObjectReference objRef))
                {
                    using (objRef)
                    {
                        return (TInterface)ComWrappersSupport.CreateRcwForComObject(objRef.ThisPtr);
                    }
                }
            }

            if (value is TInterface convertableInMetadata)
            {
                return convertableInMetadata;
            }

            using (var objRef = GetRefForObject(value))
            {
                return (TInterface)typeof(TInterface).GetHelperType().GetConstructor(new[] { typeof(IObjectReference) }).Invoke(new object[] { objRef });
            }
        }

        private static bool TryGetRefForObject(object value, bool allowComposed, out IObjectReference reference)
        {
            if (ComWrappersSupport.TryUnwrapObject(value, out var objRef))
            {
                reference = objRef.As<IUnknownVftbl>();
                return true;
            }
            else if (allowComposed && TryGetComposedRefForQI(value, out objRef))
            {
                reference = objRef.As<IUnknownVftbl>();
                return true;
            }
            reference = null;
            return false;
        }
        
        private static IObjectReference GetRefForObject(object value)
        {
            return TryGetRefForObject(value, allowComposed: true, out var objRef) ? objRef
                : throw new ArgumentException("Source object type is not a projected type and does not inherit from a projected type.", nameof(value));
        }

        private static bool TryGetComposedRefForQI(object value, out IObjectReference objRef)
        {
            var getReferenceMethod = value.GetType().GetMethod("GetDefaultReference", BindingFlags.NonPublic | BindingFlags.Instance).MakeGenericMethod(typeof(IUnknownVftbl));
            if (getReferenceMethod is null)
            {
                objRef = null;
                return false;
            }
            objRef = (IObjectReference)getReferenceMethod.Invoke(value, Array.Empty<object>());
            return true;
        }
    }
}
