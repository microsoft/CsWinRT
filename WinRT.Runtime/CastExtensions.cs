using System;
using System.Collections.Generic;
using System.Text;
using WinRT.Interop;

namespace WinRT
{
    public static class CastExtensions
    {
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
