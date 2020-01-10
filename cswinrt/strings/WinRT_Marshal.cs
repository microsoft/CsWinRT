using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Linq.Expressions;

#pragma warning disable 0169 // The field 'xxx' is never used
#pragma warning disable 0649 // Field 'xxx' is never assigned to, and will always have its default value

namespace WinRT
{
    public static class MarshalExtensions
    {
        public static void Dispose(this GCHandle handle)
        {
            if (handle.IsAllocated)
            {
                handle.Free();
            }
        }
    }

    struct MarshalString
    {
        // TODO: optimization - box Cache struct for nested marshalings, 
        // to avoid heap allocations for top-level hstrings
        public class Cache
        {
            public bool Dispose()
            {
                _gchandle.Dispose();
                return false;
            }

            public unsafe struct HStringHeader // sizeof(HSTRING_HEADER)
            {
                public fixed byte Reserved[24];
            };
            public HStringHeader _header;
            public GCHandle _gchandle;
            public IntPtr _handle;
        }

        public static unsafe Cache CreateCache(string value)
        {
            Cache cache = new Cache();
            try
            {
                cache._gchandle = GCHandle.Alloc(value, GCHandleType.Pinned);
                fixed (void* chars = value, header = &cache._header, handle = &cache._handle)
                {
                    Marshal.ThrowExceptionForHR(Platform.WindowsCreateStringReference(
                        (char*)chars, value.Length, (IntPtr*)header, (IntPtr*)handle));
                };
                return cache;
            }
            catch (Exception) when (cache.Dispose())
            {
                // Will never execute 
                return default;
            }
        }

        public static IntPtr GetAbi(Cache cache) => cache._handle;
        
        public static IntPtr GetAbi(object box) => ((Cache)box)._handle;

        public static void DisposeCache(Cache cache) => cache.Dispose();
        // no need to delete hstring reference
        
        public static void DisposeCache(object box)
        {
            if (box != null)
                DisposeCache(((Cache)box));
        }

        public static void DisposeAbi(IntPtr hstring)
        {
            if (hstring != IntPtr.Zero)
                Platform.WindowsDeleteString(hstring);
        }
        public static void DisposeAbi(object abi)
        {
            if (abi != null)
                DisposeAbi(((IntPtr)abi));
        }

        public static string FromAbi(IntPtr value)
        {
            unsafe
            {
                uint length;
                var buffer = Platform.WindowsGetStringRawBuffer(value, &length);
                return new string(buffer, 0, (int)length);
            }
        }

        public struct CacheArray
        {
            public bool Dispose()
            {
                _gchandle.Dispose();
                foreach (var abi_string in _abi_strings)
                {
                    abi_string.Dispose();
                }
                return false;
            }

            public GCHandle _gchandle;
            public Cache[] _abi_strings;
        }

        public static CacheArray CreateCacheArray(string[] array)
        {
            CacheArray cache = new CacheArray();
            try
            {
                var length = array.Length;
                var abi_array = new IntPtr[length];
                cache._abi_strings = new MarshalString.Cache[length];
                for (int i = 0; i < length; i++)
                {
                    cache._abi_strings[i] = MarshalString.CreateCache(array[i]);
                    abi_array[i] = MarshalString.GetAbi(cache._abi_strings[i]);
                };
                cache._gchandle = GCHandle.Alloc(abi_array, GCHandleType.Pinned);
                return cache;
            }
            catch (Exception) when (cache.Dispose())
            {
                // Will never execute 
                return default;
            }
        }
        
        public static (IntPtr data, int length) GetAbiArray(object box)
        {
            var cache = (Cache)box;
            return (cache._gchandle.AddrOfPinnedObject(), ((Array)cache._gchandle.Target).Length);
        }

        public static unsafe string[] FromAbiArray(object box)
        {
            var abi = ((IntPtr data, int length))box;
            string[] array = new string[abi.length];
            var data = (IntPtr*)abi.data.ToPointer();
            for (int i = 0; i < abi.length; i++)
            {
                array[i] = MarshalString.FromAbi(data[i]);
            }
            return array;
        }

        public static void DisposeCacheArray(object box) => ((CacheArray)box).Dispose();
        
        public static void DisposeAbiArray(object box)
        {
            var abi = ((IntPtr data, int length))box;
            Marshal.FreeCoTaskMem(abi.data);
        }
    }

    struct MarshalBlittable
    {
        public struct CacheArray
        {
            public CacheArray(Array array) => _gchandle = GCHandle.Alloc(array, GCHandleType.Pinned);
            public void Dispose() => _gchandle.Dispose();

            public GCHandle _gchandle;
        };

        public static CacheArray CreateCacheArray(Array array) => new CacheArray(array);
        
        public static (IntPtr data, int length) GetAbiArray(object box)
        {
            var cache = (CacheArray)box;
            return (cache._gchandle.AddrOfPinnedObject(), ((Array)cache._gchandle.Target).Length);
        }
        
        public static unsafe T[] FromAbiArray<T>(object box)
        {
            var abi = ((IntPtr data, int length))box;
            Array array = Array.CreateInstance(typeof(T), abi.length);
            var array_handle = GCHandle.Alloc(array, GCHandleType.Pinned);
            var array_data = array_handle.AddrOfPinnedObject();
            var byte_length = abi.length * Marshal.SizeOf<T>();
            Buffer.MemoryCopy(abi.data.ToPointer(), array_data.ToPointer(), byte_length, byte_length);
            return (T[])array;
        }

        public static void DisposeCacheArray(object box) => ((CacheArray)box).Dispose();

        public static void DisposeAbiArray(object box)
        {
            var abi = ((IntPtr data, int length))box;
            Marshal.FreeCoTaskMem(abi.data);
        }
    }

    struct MarshalNonBlittable<T>
    {
        public struct CacheArray
        {
            public bool Dispose()
            {
                _gchandle.Dispose();
                foreach (var abi_element in _abi_elements)
                {
                    Marshaler<T>.DisposeCache(abi_element);
                }
                return false;
            }

            public GCHandle _gchandle;
            public object[] _abi_elements;
        }

        public static CacheArray CreateCacheArray(T[] array, Type abiType)
        {
            CacheArray cache = new CacheArray();
            try
            {
                var length = array.Length;
                var abi_array = Array.CreateInstance(abiType, length);
                cache._abi_elements = new object[length];
                for (int i = 0; i < length; i++)
                {
                    cache._abi_elements[i] = Marshaler<T>.CreateCache(array[i]);
                    abi_array.SetValue(Marshaler<T>.GetAbi(cache._abi_elements[i]), i);
                }
                cache._gchandle = GCHandle.Alloc(abi_array, GCHandleType.Pinned);
                return cache;
            }
            catch (Exception) when (cache.Dispose())
            {
                // Will never execute 
                return default;
            }
        }

        public static (IntPtr data, int length) GetAbiArray(object box)
        {
            var cache = (CacheArray)box;
            return (cache._gchandle.AddrOfPinnedObject(), ((Array)cache._gchandle.Target).Length);
        }

        public static unsafe T[] FromAbiArray(object box, Type abiType)
        {
            var abi = ((IntPtr data, int length))box;
            var array = new T[abi.length];
            var data = (byte*)abi.data.ToPointer();
            var abi_element_size = Marshal.SizeOf(abiType);
            for (int i = 0; i < abi.length; i++)
            {
                var abi_element = Marshal.PtrToStructure((IntPtr)data, abiType);
                array[i] = Marshaler<T>.FromAbi(abi_element);
                data += abi_element_size;
            }
            return array;
        }

        public static void DisposeCacheArray(object box) => ((CacheArray)box).Dispose();
        
        public static void DisposeAbiArray(object box)
        {
            var abi = ((IntPtr data, int length))box;
            Marshal.FreeCoTaskMem(abi.data);
        }
    }

    struct MarshalInterface<TInterface, TAbi> where TAbi : class, TInterface
    {
        private static Func<TAbi, IntPtr> _ToAbi;
        private static Func<IntPtr, TAbi> _FromAbi;

        public static TInterface FromAbi(IntPtr ptr)
        {
            // TODO: Check if the value is a CCW and return the underlying object.
            if (_FromAbi == null)
            {
                _FromAbi = BindFromAbi();
            }
            return _FromAbi(ptr);
        }

        public static IntPtr ToAbi(TInterface value)
        {
            // If the value passed in is the native implementation of the interface
            // use the ToAbi delegate since it will be faster than reflection.
            if (value is TAbi native)
            {
                if (_ToAbi == null)
                {
                    _ToAbi = BindToAbi();
                }
                return _ToAbi(native);
            }

            Type type = value.GetType();
            MethodInfo asInterfaceMethod = type.GetMethod("AsInterface`1");
            // If the type has an AsInterface<A> method, then it is an interface.
            if (asInterfaceMethod != null)
            {
                IObjectReference objReference = (IObjectReference)asInterfaceMethod.MakeGenericMethod(typeof(TInterface)).Invoke(value, null);
                return objReference.ThisPtr;
            }

            // The type is a class. We need to determine if it's an RCW or a managed class.
            Type interfaceTagType = type.GetNestedType("InterfaceTag`1", BindingFlags.NonPublic | BindingFlags.DeclaredOnly)?.MakeGenericType(typeof(TInterface));
            // If the type declares a nested InterfaceTag<I> type, then it is a generated RCW.
            if (interfaceTagType != null)
            {
                Type interfaceType = typeof(TInterface);
                TAbi iface = null;

                for (Type currentRcwType = type; currentRcwType != typeof(object); currentRcwType = interfaceType.BaseType)
                {
                    interfaceTagType = type.GetNestedType("InterfaceTag`1", BindingFlags.NonPublic | BindingFlags.DeclaredOnly)?.MakeGenericType(typeof(TInterface));
                    if (interfaceTagType == null)
                    {
                        throw new InvalidOperationException($"Found a non-RCW type '{currentRcwType}' that is a base class of a generated RCW type '{type}'.");
                    }

                    Type interfaceTag = interfaceTagType.MakeGenericType(interfaceType);
                    MethodInfo asInternalMethod = type.GetMethod("AsInternal", BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.Instance, null, new[] { interfaceTag }, null);
                    if (asInternalMethod != null)
                    {
                        iface = (TAbi)asInternalMethod.Invoke(value, new[] { Activator.CreateInstance(interfaceTag) });
                        break;
                    }
                }

                if (iface == null)
                {
                    throw new InvalidCastException($"Unable to get native interface of type '{interfaceType}' for object of type '{type}'.");
                }

                if (_ToAbi == null)
                {
                    _ToAbi = BindToAbi();
                }
                return _ToAbi(iface);
            }

            // TODO: Create a CCW for user-defined implementations of interfaces.
            throw new NotImplementedException("Generating a CCW for a user-defined class is not currently implemented");
        }

        private static Func<IntPtr, TAbi> BindFromAbi()
        {
            var fromAbiMethod = typeof(TAbi).GetMethod("FromAbi");
            var objReferenceConstructor = typeof(TAbi).GetConstructor(new[] { fromAbiMethod.ReturnType });
            var parms = new[] { Expression.Parameter(typeof(IntPtr), "arg") };
            return Expression.Lambda<Func<IntPtr, TAbi>>(
                    Expression.New(objReferenceConstructor,
                        Expression.Call(fromAbiMethod, parms[0])), parms).Compile();
        }

        private static Func<TAbi, IntPtr> BindToAbi()
        {
            var parms = new[] { Expression.Parameter(typeof(TAbi), "arg") };
            return Expression.Lambda<Func<TAbi, IntPtr>>(
                Expression.MakeMemberAccess(
                    Expression.Convert(parms[0], typeof(TAbi)),
                    typeof(TAbi).GetProperty("ThisPtr")), parms).Compile();
        }
    }

    public class Marshaler<T>
    {
        static Marshaler()
        {
            Type type = typeof(T);

            // structs cannot contain arrays, and arrays may only ever appear as parameters
            if (type.IsArray)
            {
                throw new InvalidOperationException("Arrays may not be marshaled generically.");
            }

            if (type.IsValueType)
            {
                // If type is blittable just pass through
                AbiType = Type.GetType($"{type.Namespace}.ABI.{type.FullName}");
                if (AbiType == null)
                {
                    AbiType = type;
                    FromAbi = (object value) => (T)value;
                    CreateCache = (T value) => value;
                    GetAbi = (object box) => box;
                    DisposeCache = (object box) => { };
                    DisposeAbi = (object box) => { };
                    CreateCacheArray = (T[] array) => MarshalBlittable.CreateCacheArray(array);
                    GetAbiArray = (object box) => MarshalBlittable.GetAbiArray(box);
                    FromAbiArray = (object box) => MarshalBlittable.FromAbiArray<T>(box);
                    DisposeCacheArray = (object box) => MarshalBlittable.DisposeCacheArray(box);
                    DisposeAbiArray = (object box) => MarshalBlittable.DisposeAbiArray(box);
                }
                else // bind to ABI counterpart's marshalers
                {
                    var CacheType = Type.GetType($"{type.Namespace}.ABI.{type.Name}+Cache");
                    CreateCache = BindCreateCache(AbiType);
                    GetAbi = BindGetAbi(AbiType, CacheType);
                    FromAbi = BindFromAbi(AbiType);
                    DisposeCache = BindDisposeCache(AbiType, CacheType);
                    DisposeAbi = (object box) => { };
                    CreateCacheArray = (T[] array) => MarshalNonBlittable<T>.CreateCacheArray(array, AbiType);
                    GetAbiArray = (object box) => MarshalNonBlittable<T>.GetAbiArray(box);
                    FromAbiArray = (object box) => MarshalNonBlittable<T>.FromAbiArray(box, AbiType);
                    DisposeCacheArray = (object box) => MarshalNonBlittable<T>.DisposeCacheArray(box);
                    DisposeAbiArray = (object box) => MarshalNonBlittable<T>.DisposeAbiArray(box);
                }
            }
            else if (type == typeof(String))
            {
                AbiType = typeof(IntPtr);
                FromAbi = (object value) => (T)(object)MarshalString.FromAbi((IntPtr)value);
                CreateCache = (T value) => MarshalString.CreateCache((string)(object)value);
                GetAbi = (object box) => MarshalString.GetAbi(box);
                DisposeCache = (object box) => MarshalString.DisposeCache(box);
                DisposeAbi = (object box) => MarshalString.DisposeAbi(box);
                CreateCacheArray = (T[] array) => MarshalString.CreateCacheArray((string[])(object)array);
                GetAbiArray = (object box) => MarshalString.GetAbiArray(box);
                FromAbiArray = (object box) => (T[])(object)MarshalString.FromAbiArray(box);
                DisposeCacheArray = (object box) => MarshalString.DisposeCacheArray(box);
                DisposeAbiArray = (object box) => MarshalString.DisposeAbiArray(box);
            }
            else 
            {
                // TODO: MarshalInterface
            }
            RefAbiType = AbiType.MakeByRefType();
        }

        private static Func<T, object> BindCreateCache(Type AbiType)
        {
            var parms = new[] { Expression.Parameter(typeof(T), "arg") };
            return Expression.Lambda<Func<T, object>>(
                Expression.Convert(Expression.Call(AbiType.GetMethod("CreateCache"), parms),
                    typeof(object)), parms).Compile();
        }

        private static Func<object, object> BindGetAbi(Type AbiType, Type CacheType)
        {
            var parms = new[] { Expression.Parameter(typeof(object), "arg") };
            return Expression.Lambda<Func<object, object>>(
                Expression.Convert(Expression.Call(AbiType.GetMethod("GetAbi"),
                    new[] { Expression.Convert(parms[0], CacheType) }),
                        typeof(object)), parms).Compile();
        }

        private static Func<object, T> BindFromAbi(Type AbiType)
        {
            var parms = new[] { Expression.Parameter(typeof(object), "arg") };
            return Expression.Lambda<Func<object, T>>(
                Expression.Call(AbiType.GetMethod("FromAbi"),
                    new[] { Expression.Convert(parms[0], AbiType) }), parms).Compile();
        }

        private static Action<object> BindDisposeCache(Type AbiType, Type CacheType)
        {
            var parms = new[] { Expression.Parameter(typeof(object), "arg") };
            return Expression.Lambda<Action<object>>(
                Expression.Call(AbiType.GetMethod("DisposeCache"),
                    new[] { Expression.Convert(parms[0], CacheType) }), parms).Compile();
        }

        public static readonly Type AbiType;
        public static readonly Type RefAbiType;
        public static readonly Func<object, T> FromAbi;
        public static readonly Func<T, object> CreateCache;
        public static readonly Func<object, object> GetAbi;
        public static readonly Action<object> DisposeCache;
        public static readonly Action<object> DisposeAbi;

        public static readonly Func<object, T[]> FromAbiArray;
        public static readonly Func<T[], object> CreateCacheArray;
        public static readonly Func<object, (IntPtr, int)> GetAbiArray;
        public static readonly Action<object> DisposeCacheArray;
        public static readonly Action<object> DisposeAbiArray;
    }
}
