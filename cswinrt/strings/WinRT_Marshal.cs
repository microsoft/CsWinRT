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

    // TODO: optimization - MarshalString struct, boxed for nested marshalings
    // to avoid heap allocations for top-level hstrings
    /*struct*/
    class MarshalString
    {
        public unsafe struct HStringHeader // sizeof(HSTRING_HEADER)
        {
            public fixed byte Reserved[24];
        };
        public HStringHeader _header;
        public GCHandle _gchandle;
        public IntPtr _handle;

        public bool Dispose()
        {
            _gchandle.Dispose();
            return false;
        }

        public static unsafe MarshalString Create(string value)
        {
            var m = new MarshalString();
            try
            {
                m._gchandle = GCHandle.Alloc(value, GCHandleType.Pinned);
                fixed (void* chars = value, header = &m._header, handle = &m._handle)
                {
                    Marshal.ThrowExceptionForHR(Platform.WindowsCreateStringReference(
                        (char*)chars, value.Length, (IntPtr*)header, (IntPtr*)handle));
                };
                return m;
            }
            catch (Exception) when (m.Dispose())
            {
                // Will never execute 
                return default;
            }
        }

        public static IntPtr GetAbi(MarshalString m) => m._handle;

        public static IntPtr GetAbi(object box) => ((MarshalString)box)._handle;

        public static void DisposeMarshaler(MarshalString m) => m.Dispose();
        // no need to delete hstring reference

        public static void DisposeMarshaler(object box)
        {
            if (box != null)
                DisposeMarshaler(((MarshalString)box));
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

        public static unsafe IntPtr FromManaged(string value)
        {
            IntPtr handle;
            Marshal.ThrowExceptionForHR(
                Platform.WindowsCreateString(value, value.Length, &handle));
            return handle;
        }

        public struct MarshalerArray
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
            public MarshalString[] _abi_strings;
        }

        public static MarshalerArray CreateArray(string[] array)
        {
            var m = new MarshalerArray();
            try
            {
                var length = array.Length;
                var abi_array = new IntPtr[length];
                m._abi_strings = new MarshalString[length];
                for (int i = 0; i < length; i++)
                {
                    m._abi_strings[i] = MarshalString.Create(array[i]);
                    abi_array[i] = MarshalString.GetAbi(m._abi_strings[i]);
                };
                m._gchandle = GCHandle.Alloc(abi_array, GCHandleType.Pinned);
                return m;
            }
            catch (Exception) when (m.Dispose())
            {
                // Will never execute 
                return default;
            }
        }

        public static (int length, IntPtr data) GetAbiArray(object box)
        {
            var m = (MarshalerArray)box;
            return (((Array)m._gchandle.Target).Length, m._gchandle.AddrOfPinnedObject());
        }

        public static unsafe string[] FromAbiArray(object box)
        {
            var abi = ((int length, IntPtr data))box;
            string[] array = new string[abi.length];
            var data = (IntPtr*)abi.data.ToPointer();
            for (int i = 0; i < abi.length; i++)
            {
                array[i] = MarshalString.FromAbi(data[i]);
            }
            return array;
        }

        public static void DisposeMarshalerArray(object box) => ((MarshalerArray)box).Dispose();

        public static void DisposeAbiArray(object box)
        {
            var abi = ((int length, IntPtr data))box;
            Marshal.FreeCoTaskMem(abi.data);
        }
    }

    struct MarshalBlittable<T>
    {
        public struct MarshalerArray
        {
            public MarshalerArray(Array array) => _gchandle = GCHandle.Alloc(array, GCHandleType.Pinned);
            public void Dispose() => _gchandle.Dispose();

            public GCHandle _gchandle;
        };

        public static MarshalerArray CreateArray(Array array) => new MarshalerArray(array);

        public static (int length, IntPtr data) GetAbiArray(object box)
        {
            var m = (MarshalerArray)box;
            return (((Array)m._gchandle.Target).Length, m._gchandle.AddrOfPinnedObject());
        }

        public static unsafe T[] FromAbiArray(object box)
        {
            var abi = ((int length, IntPtr data))box;
            Array array = Array.CreateInstance(typeof(T), abi.length);
            var array_handle = GCHandle.Alloc(array, GCHandleType.Pinned);
            var array_data = array_handle.AddrOfPinnedObject();
            var byte_length = abi.length * Marshal.SizeOf<T>();
            Buffer.MemoryCopy(abi.data.ToPointer(), array_data.ToPointer(), byte_length, byte_length);
            return (T[])array;
        }

        public static void DisposeMarshalerArray(object box) => ((MarshalerArray)box).Dispose();

        public static void DisposeAbiArray(object box)
        {
            var abi = ((int length, IntPtr data))box;
            Marshal.FreeCoTaskMem(abi.data);
        }
    }

    struct MarshalNonBlittable<T>
    {
        private static Type AbiType;

        static MarshalNonBlittable()
        {
            AbiType = Type.GetType($"{typeof(T).Namespace}.ABI.{typeof(T).FullName}");
            Create = BindCreate();
            GetAbi = BindGetAbi();
            FromAbi = BindFromAbi();
            DisposeMarshaler = BindDisposeMarshaler();
        }

        public struct MarshalerArray
        {
            public bool Dispose()
            {
                _gchandle.Dispose();
                foreach (var abi_element in _abi_elements)
                {
                    Marshaler<T>.DisposeMarshaler(abi_element);
                }
                return false;
            }

            public GCHandle _gchandle;
            public object[] _abi_elements;
        }

        public static readonly Func<T, object> Create;
        private static Func<T, object> BindCreate()
        {
            var parms = new[] { Expression.Parameter(typeof(T), "arg") };
            return Expression.Lambda<Func<T, object>>(
                Expression.Convert(Expression.Call(AbiType.GetMethod("Create"), parms),
                    typeof(object)), parms).Compile();
        }

        public static readonly Func<object, object> GetAbi;
        private static Func<object, object> BindGetAbi()
        {
            var parms = new[] { Expression.Parameter(typeof(object), "arg") };
            return Expression.Lambda<Func<object, object>>(
                Expression.Convert(Expression.Call(AbiType.GetMethod("GetAbi"),
                    new[] { Expression.Convert(parms[0], AbiType) }),
                        typeof(object)), parms).Compile();
        }

        public static readonly Func<object, T> FromAbi;
        private static Func<object, T> BindFromAbi()
        {
            var parms = new[] { Expression.Parameter(typeof(object), "arg") };
            return Expression.Lambda<Func<object, T>>(
                Expression.Call(AbiType.GetMethod("FromAbi"),
                    new[] { Expression.Convert(parms[0], AbiType) }), parms).Compile();
        }

        public static readonly Action<object> DisposeMarshaler;
        private static Action<object> BindDisposeMarshaler()
        {
            var parms = new[] { Expression.Parameter(typeof(object), "arg") };
            return Expression.Lambda<Action<object>>(
                Expression.Call(AbiType.GetMethod("DisposeMarshaler"),
                    new[] { Expression.Convert(parms[0], AbiType) }), parms).Compile();
        }

        public static MarshalerArray CreateArray(T[] array)
        {
            MarshalerArray m = new MarshalerArray();
            try
            {
                var length = array.Length;
                var abi_array = Array.CreateInstance(AbiType, length);
                m._abi_elements = new object[length];
                for (int i = 0; i < length; i++)
                {
                    m._abi_elements[i] = Marshaler<T>.Create(array[i]);
                    abi_array.SetValue(Marshaler<T>.GetAbi(m._abi_elements[i]), i);
                }
                m._gchandle = GCHandle.Alloc(abi_array, GCHandleType.Pinned);
                return m;
            }
            catch (Exception) when (m.Dispose())
            {
                // Will never execute 
                return default;
            }
        }

        public static (int length, IntPtr data) GetAbiArray(object box)
        {
            var m = (MarshalerArray)box;
            return (((Array)m._gchandle.Target).Length, m._gchandle.AddrOfPinnedObject());
        }

        public static unsafe T[] FromAbiArray(object box)
        {
            var abi = ((int length, IntPtr data))box;
            var array = new T[abi.length];
            var data = (byte*)abi.data.ToPointer();
            var abi_element_size = Marshal.SizeOf(AbiType);
            for (int i = 0; i < abi.length; i++)
            {
                var abi_element = Marshal.PtrToStructure((IntPtr)data, AbiType);
                array[i] = Marshaler<T>.FromAbi(abi_element);
                data += abi_element_size;
            }
            return array;
        }

        public static void DisposeMarshalerArray(object box) => ((MarshalerArray)box).Dispose();

        public static void DisposeAbiArray(object box)
        {
            var abi = ((int length, IntPtr data))box;
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

        // todo: define Marshaler struct to stabilize temporary CCW for abi call
        public static IntPtr Create(TInterface value) => ToAbi(value);

        public static IntPtr GetAbi(object value) => (IntPtr)value;

        public static void DisposeAbi(IntPtr thisPtr) =>
            ObjectReference<IInspectable>.Attach(ref thisPtr);

        public static void DisposeMarshaler(object value) { }

        public static IntPtr FromManaged(TInterface value)
        {
            Type type = value.GetType();
            var objRef = GetObjectReference(value, type);
            if (objRef != null)
            {
                return objRef.GetRef();
            }
            throw new InvalidCastException($"Unable to get native interface of type '{typeof(TInterface)}' for object of type '{type}'.");
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
            var objRef = GetObjectReference(value, type);
            if (objRef != null)
            { 
                return objRef.ThisPtr;
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

        private static IObjectReference GetObjectReference(TInterface value, Type type)
        {
            MethodInfo asInterfaceMethod = type.GetMethod("AsInterface`1");
            // If the type has an AsInterface<A> method, then it is an interface.
            if (asInterfaceMethod != null)
            {
                var objRef = (IObjectReference)asInterfaceMethod.MakeGenericMethod(typeof(TInterface)).Invoke(value, null);
                return objRef;
            }
            return null;
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

    public class MarshalClass<T>
    {
        public static T FromAbi(IntPtr value)
        {
            // todo: BindFromAbi ...
            return default;
        }

        public static void DisposeAbi(IntPtr abi)
        {
            // todo: ObjectReference release
        }
        public static void DisposeAbi(object abi)
        {
            if (abi != null)
                DisposeAbi(((IntPtr)abi));
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
                    Create = (T value) => value;
                    GetAbi = (object box) => box;
                    FromAbi = (object value) => (T)value;
                    DisposeMarshaler = (object box) => { };
                    DisposeAbi = (object box) => { };
                    CreateArray = (T[] array) => MarshalBlittable<T>.CreateArray(array);
                    GetAbiArray = (object box) => MarshalBlittable<T>.GetAbiArray(box);
                    FromAbiArray = (object box) => MarshalBlittable<T>.FromAbiArray(box);
                    DisposeMarshalerArray = (object box) => MarshalBlittable<T>.DisposeMarshalerArray(box);
                    DisposeAbiArray = (object box) => MarshalBlittable<T>.DisposeAbiArray(box);
                }
                else // bind to marshaler
                {
                    Create = MarshalNonBlittable<T>.Create;
                    GetAbi = MarshalNonBlittable<T>.GetAbi;
                    FromAbi = MarshalNonBlittable<T>.FromAbi;
                    DisposeMarshaler = MarshalNonBlittable<T>.DisposeMarshaler;
                    DisposeAbi = (object box) => { };
                    CreateArray = (T[] array) => MarshalNonBlittable<T>.CreateArray(array);
                    GetAbiArray = (object box) => MarshalNonBlittable<T>.GetAbiArray(box);
                    FromAbiArray = (object box) => MarshalNonBlittable<T>.FromAbiArray(box);
                    DisposeMarshalerArray = (object box) => MarshalNonBlittable<T>.DisposeMarshalerArray(box);
                    DisposeAbiArray = (object box) => MarshalNonBlittable<T>.DisposeAbiArray(box);
                }
            }
            else if (type == typeof(String))
            {
                AbiType = typeof(IntPtr);
                Create = (T value) => MarshalString.Create((string)(object)value);
                GetAbi = (object box) => MarshalString.GetAbi(box);
                FromAbi = (object value) => (T)(object)MarshalString.FromAbi((IntPtr)value);
                DisposeMarshaler = (object box) => MarshalString.DisposeMarshaler(box);
                DisposeAbi = (object box) => MarshalString.DisposeAbi(box);
                CreateArray = (T[] array) => MarshalString.CreateArray((string[])(object)array);
                GetAbiArray = (object box) => MarshalString.GetAbiArray(box);
                FromAbiArray = (object box) => (T[])(object)MarshalString.FromAbiArray(box);
                DisposeMarshalerArray = (object box) => MarshalString.DisposeMarshalerArray(box);
                DisposeAbiArray = (object box) => MarshalString.DisposeAbiArray(box);
            }
            else if (type.IsDelegate())
            {
                AbiType = typeof(IntPtr);
                //Create = (T value) => MarshalString.Create((string)(object)value);
                //GetAbi = (object box) => MarshalString.GetAbi(box);
                //FromAbi = (object value) => (T)(object)MarshalString.FromAbi((IntPtr)value);
                //DisposeMarshaler = (object box) => MarshalString.DisposeMarshaler(box);
                //DisposeAbi = (object box) => MarshalString.DisposeAbi(box);
                //CreateArray = (T[] array) => MarshalString.CreateArray((string[])(object)array);
                //GetAbiArray = (object box) => MarshalString.GetAbiArray(box);
                //FromAbiArray = (object box) => (T[])(object)MarshalString.FromAbiArray(box);
                //DisposeMarshalerArray = (object box) => MarshalString.DisposeMarshalerArray(box);
                //DisposeAbiArray = (object box) => MarshalString.DisposeAbiArray(box);
            }
            else
            {
                // TODO: MarshalInterface
            }
            RefAbiType = AbiType.MakeByRefType();
        }

        public static readonly Type AbiType;
        public static readonly Type RefAbiType;
        public static readonly Func<T, object> Create;
        public static readonly Func<object, object> GetAbi;
        public static readonly Func<object, T> FromAbi;
        public static readonly Action<object> DisposeMarshaler;
        public static readonly Action<object> DisposeAbi;
        public static readonly Func<T[], object> CreateArray;
        public static readonly Func<object, (int, IntPtr)> GetAbiArray;
        public static readonly Func<object, T[]> FromAbiArray;
        public static readonly Action<object> DisposeMarshalerArray;
        public static readonly Action<object> DisposeAbiArray;
    }
}
