using System;
using System.Collections.Generic;
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
            IObjectReference GetRefForObject()
            {
                if (ComWrappersSupport.TryUnwrapObject(value, out var objRef))
                {
                    return objRef.As<IUnknownVftbl>();
                }
                else
                {
                    throw new ArgumentException("Source object type is not a projected type.", nameof(value));
                }
            }

            if (typeof(TInterface) == typeof(object))
            {
                using var objRef = GetRefForObject();
                return (TInterface)ComWrappersSupport.CreateRcwForComObject(objRef.ThisPtr);
            }

            if (value is TInterface convertableInMetadata)
            {
                return convertableInMetadata;
            }

            using (var objRef = GetRefForObject())
            {
                return (TInterface)typeof(TInterface).GetHelperType().GetConstructor(new [] { typeof(IObjectReference) }).Invoke(new object[] { objRef });
            }
        }
    }

}
