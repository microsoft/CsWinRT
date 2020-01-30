﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Linq.Expressions;
using System.Collections.Concurrent;

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

    // TODO: minimize heap allocations for marshalers by eliminating explicit try/finally
    // and adopting ref structs with non-IDisposable Dispose and 'using var ...' pattern,
    // as well as passing marshalers to FromAbi by ref so they can be disposed.
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

        public static unsafe MarshalString CreateMarshaler(string value)
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

        public static void DisposeMarshaler(MarshalString m) => m?.Dispose();

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

        public static unsafe string FromAbi(IntPtr value)
        {
            uint length;
            var buffer = Platform.WindowsGetStringRawBuffer(value, &length);
            return new string(buffer, 0, (int)length);
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
                foreach (var abi_string in _abi_strings)
                {
                    abi_string.Dispose();
                }
                Marshal.FreeCoTaskMem(_array);
                return false;
            }

            public IntPtr _array;
            public MarshalString[] _abi_strings;
        }

        public static unsafe MarshalerArray CreateMarshalerArray(string[] array)
        {
            var m = new MarshalerArray();
            try
            {
                var length = array.Length;
                m._array = Marshal.AllocCoTaskMem(length * Marshal.SizeOf<IntPtr>());
                var elements = (IntPtr*)m._array;
                m._abi_strings = new MarshalString[length];
                for (int i = 0; i < length; i++)
                {
                    m._abi_strings[i] = MarshalString.CreateMarshaler(array[i]);
                    elements[i] = MarshalString.GetAbi(m._abi_strings[i]);
                };
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
            return (m._abi_strings.Length, m._array);
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

        public static void DisposeMarshalerArray(object box)
        {
            if (box != null)
                ((MarshalerArray)box).Dispose();
        }

        public static unsafe void DisposeAbiArray(object box)
        {
            if (box != null)
            {
                var abi = ((int length, IntPtr data))box;
                var elements = (IntPtr*)abi.data;
                for (int i = 0; i < abi.length; i++)
                {
                    DisposeAbi(elements[i]);
                }
                Marshal.FreeCoTaskMem(abi.data);
            }
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

        public static MarshalerArray CreateMarshalerArray(Array array) => new MarshalerArray(array);

        public static (int length, IntPtr data) GetAbiArray(object box)
        {
            var m = (MarshalerArray)box;
            return (((Array)m._gchandle.Target).Length, m._gchandle.AddrOfPinnedObject());
        }

        public static unsafe T[] FromAbiArray(object box)
        {
            var abi = ((int length, IntPtr data))box;
            // TODO: consider System.Memory nuget package dependency here:
            //var abiSpan = new ReadOnlySpan<T>(abi.data.ToPointer(), abi.length);
            //return abiSpan.ToArray();
            var array = (T[])Array.CreateInstance(typeof(T), abi.length);
            var array_handle = GCHandle.Alloc(array, GCHandleType.Pinned);
            var array_data = array_handle.AddrOfPinnedObject();
            var byte_length = abi.length * Marshal.SizeOf<T>();
            Buffer.MemoryCopy(abi.data.ToPointer(), array_data.ToPointer(), byte_length, byte_length);
            array_handle.Free();
            return array;
        }

        public static void DisposeMarshalerArray(object box)
        {
            if (box != null)
                ((MarshalerArray)box).Dispose();
        }

        public static void DisposeAbiArray(object box)
        {
            if (box != null)
            {
                var abi = ((int length, IntPtr data))box;
                Marshal.FreeCoTaskMem(abi.data);
            }
        }
    }

    class MarshalGeneric<T>
    {
        protected static readonly Type HelperType = typeof(T).GetHelperType();
        protected static readonly Type AbiType = typeof(T).GetHelperType().GetMethod("GetAbi").ReturnType;
        protected static readonly Type MarshalerType = typeof(T).GetHelperType().GetMethod("CreateMarshaler").ReturnType;

        static MarshalGeneric()
        {
            CreateMarshaler = BindCreateMarshaler();
            GetAbi = BindGetAbi();
            FromAbi = BindFromAbi();
            DisposeMarshaler = BindDisposeMarshaler();
        }

        public static readonly Func<T, object> CreateMarshaler;
        private static Func<T, object> BindCreateMarshaler()
        {
            var parms = new[] { Expression.Parameter(typeof(T), "arg") };
            return Expression.Lambda<Func<T, object>>(
                Expression.Convert(Expression.Call(HelperType.GetMethod("CreateMarshaler"), parms),
                    typeof(object)), parms).Compile();
        }

        public static readonly Func<object, object> GetAbi;
        private static Func<object, object> BindGetAbi()
        {
            var parms = new[] { Expression.Parameter(typeof(object), "arg") };
            return Expression.Lambda<Func<object, object>>(
                Expression.Convert(Expression.Call(HelperType.GetMethod("GetAbi"),
                    new[] { Expression.Convert(parms[0], MarshalerType) }),
                        typeof(object)), parms).Compile();
        }

        public static readonly Func<object, T> FromAbi;
        private static Func<object, T> BindFromAbi()
        {
            var parms = new[] { Expression.Parameter(typeof(object), "arg") };
            return Expression.Lambda<Func<object, T>>(
                Expression.Call(HelperType.GetMethod("FromAbi"),
                    new[] { Expression.Convert(parms[0], AbiType) }), parms).Compile();
        }

        public static readonly Action<object> DisposeMarshaler;
        private static Action<object> BindDisposeMarshaler()
        {
            var parms = new[] { Expression.Parameter(typeof(object), "arg") };
            return Expression.Lambda<Action<object>>(
                Expression.Call(HelperType.GetMethod("DisposeMarshaler"),
                    new[] { Expression.Convert(parms[0], MarshalerType) }), parms).Compile();
        }
    }

    class MarshalNonBlittable<T> : MarshalGeneric<T>
    {
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

        public static MarshalerArray CreateMarshalerArray(T[] array)
        {
            MarshalerArray m = new MarshalerArray();
            try
            {
                var length = array.Length;
                // TODO: consider Marshal.AllocCoTaskMem based on sizeof(HelperType)
                // and a Marshaler<T>.CopyTo to allow blitting into array directly
                // without boxing
                var abi_array = Array.CreateInstance(HelperType, length);
                m._abi_elements = new object[length];
                for (int i = 0; i < length; i++)
                {
                    m._abi_elements[i] = Marshaler<T>.CreateMarshaler(array[i]);
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
            var abi_element_size = Marshal.SizeOf(HelperType);
            for (int i = 0; i < abi.length; i++)
            {
                var abi_element = Marshal.PtrToStructure((IntPtr)data, HelperType);
                array[i] = Marshaler<T>.FromAbi(abi_element);
                data += abi_element_size;
            }
            return array;
        }

        public static void DisposeMarshalerArray(object box) => ((MarshalerArray)box).Dispose();

        public static unsafe void DisposeAbiArray(object box)
        {
            var abi = ((int length, IntPtr data))box;
            var data = (byte*)abi.data.ToPointer();
            var abi_element_size = Marshal.SizeOf(HelperType);
            for (int i = 0; i < abi.length; i++)
            {
                var abi_element = Marshal.PtrToStructure((IntPtr)data, HelperType);
                Marshaler<T>.DisposeAbi(abi_element);
                data += abi_element_size;
            }
            Marshal.FreeCoTaskMem(abi.data);
        }
    }

    struct MarshalInterface<T>
    {
        private static readonly Type HelperType = typeof(T).GetHelperType();
        private static Func<T, IObjectReference> _ToAbi;
        private static Func<IntPtr, T> _FromAbi;
        private static Func<IObjectReference, IObjectReference> _As;

        public static T FromAbi(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
            {
                return (T)(object)null;
            }
            // TODO: Check if the value is a CCW and return the underlying object.
            if (_FromAbi == null)
            {
                _FromAbi = BindFromAbi();
            }
            return _FromAbi(ptr);
        }

        public static IntPtr GetAbi(IObjectReference value) => MarshalInspectable.GetAbi(value);

        public static void DisposeAbi(IntPtr thisPtr) => MarshalInspectable.DisposeAbi(thisPtr);

        public static void DisposeMarshaler(IObjectReference value) => MarshalInspectable.DisposeMarshaler(value);

        public static IntPtr FromManaged(T value)
        {
            if (value is null)
            {
                return IntPtr.Zero;
            }
            return CreateMarshaler(value).GetRef();
        }

        public static IObjectReference CreateMarshaler(T value)
        {
            if (value is null)
            {
                return null;
            }

            // If the value passed in is the native implementation of the interface
            // use the ToAbi delegate since it will be faster than reflection.
            if (value.GetType() == HelperType)
            {
                if (_ToAbi == null)
                {
                    _ToAbi = BindToAbi();
                }
                return _ToAbi(value);
            }

            if (_As is null)
            {
                _As = BindAs();
            }

            var inspectable = MarshalInspectable.CreateMarshaler(value, true);

            return _As(inspectable);
        }

        private static Func<IntPtr, T> BindFromAbi()
        {
            var fromAbiMethod = HelperType.GetMethod("FromAbi");
            var objReferenceConstructor = HelperType.GetConstructor(new[] { fromAbiMethod.ReturnType });
            var parms = new[] { Expression.Parameter(typeof(IntPtr), "arg") };
            return Expression.Lambda<Func<IntPtr, T>>(
                    Expression.New(objReferenceConstructor,
                        Expression.Call(fromAbiMethod, parms[0])), parms).Compile();
        }

        private static Func<T, IObjectReference> BindToAbi()
        {
            var parms = new[] { Expression.Parameter(typeof(T), "arg") };
            return Expression.Lambda<Func<T, IObjectReference>>(
                Expression.MakeMemberAccess(
                    Expression.Convert(parms[0], HelperType),
                    HelperType.GetField("_obj", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)), parms).Compile();
        }

        private static Func<IObjectReference, IObjectReference> BindAs()
        {
            var helperType = typeof(T).GetHelperType();
            var parms = new[] { Expression.Parameter(typeof(IObjectReference), "arg") };
            return Expression.Lambda<Func<IObjectReference, IObjectReference>>(
                Expression.Call(
                    parms[0],
                    typeof(IObjectReference).GetMethod("As", Type.EmptyTypes).MakeGenericMethod(helperType.GetNestedType("Vftbl"))
                    ), parms).Compile();
        }
    }

    static class MarshalInspectable
    {
        private readonly static ConcurrentDictionary<string, Func<IInspectable, object>> TypedObjectFactoryCache = new ConcurrentDictionary<string, Func<IInspectable, object>>();

        public static IObjectReference CreateMarshaler(object o, bool unwrapObject = true)
        {
            if (o is null)
            {
                return null;
            }

            if (unwrapObject && TryUnwrapObject(o, out var objRef))
            {
                return objRef.As<IInspectable.Vftbl>();
            }
            // TODO: Create a CCW for user-defined implementations of interfaces.
            throw new NotImplementedException("Generating a CCW for a user-defined class is not currently implemented");
        }

        public static IntPtr GetAbi(IObjectReference objRef)
        {
            return objRef?.ThisPtr ?? IntPtr.Zero;
        }

        public static object FromAbi(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
            {
                return null;
            }

            var inspectable = new IInspectable(ObjectReference<WinRT.Interop.IUnknownVftbl>.Attach(ref ptr));
            string runtimeClassName = inspectable.GetRuntimeClassName();
            return TypedObjectFactoryCache.GetOrAdd(runtimeClassName, className => CreateTypedRcwFactory(className))(inspectable);
        }

        public static void DisposeMarshaler(IObjectReference objRef)
        {
            // Since IObjectReference doesn't have an explicit dispose,
            // just use GC.KeepAlive here to ensure that the IObjectReference instance
            // gets finalized after the call to native.
            GC.KeepAlive(objRef);
        }

        public static void DisposeAbi(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero) return;
            // TODO: this should be a direct v-table call when function pointers are a thing
            ObjectReference<IInspectable.Vftbl>.Attach(ref ptr);
        }

        public static IntPtr FromManaged(object o, bool unwrapObject = true)
        {
            var objRef = CreateMarshaler(o, unwrapObject);
            return objRef?.GetRef() ?? IntPtr.Zero;
        }

        private static bool TryUnwrapObject(object o, out IObjectReference objRef)
        {
            // The unwrapping here needs to be in exact type match in case the user
            // has implemented a WinRT interface or inherited from a WinRT class
            // in a .NET (non-projected) type.

            // TODO: Define and output attributes defining that a type is a projected interface
            // or class type to avoid accidental collisions.
            // Also, it might be a good idea to add a property to get the IObjectReference
            // that is marked [EditorBrowsable(EditorBrowsableState.Never)] to hide it from most IDEs
            // to help avoid using private implementation details.
            Type type = o.GetType();
            // Projected interface types have fields name _obj that hold their object reference.
            objRef = (IObjectReference)type.GetField("_obj", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)?.GetValue(o);
            if (objRef != null)
            {
                return true;
            }

            // If we get here, we're either a class or a non-WinRT type. If we're a class, we'll have a _default field holding a reference to our default interface.
            object defaultInterface = type.GetField("_default", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)?.GetValue(o);
            if (defaultInterface != null)
            {
                return TryUnwrapObject(defaultInterface, out objRef);
            }

            objRef = null;
            return false;
        }

        private static Func<IInspectable, object> CreateTypedRcwFactory(string runtimeClassName)
        {
            var (implementationType, _) = FindTypeByName(runtimeClassName);

            Type classType;
            Type interfaceType;
            Type vftblType;
            if (implementationType.IsInterface)
            {
                classType = null;
                interfaceType = FindTypeByName("ABI." + runtimeClassName).type ??
                    throw new TypeLoadException($"Unable to find an ABI implementation for the type '{runtimeClassName}'");
                vftblType = interfaceType.GetNestedType("Vftbl") ?? throw new TypeLoadException($"Unable to find a vtable type for the type '{runtimeClassName}'");
                if (vftblType.IsGenericTypeDefinition)
                {
                    vftblType = vftblType.MakeGenericType(interfaceType.GetGenericArguments());
                }
            }
            else
            {
                classType = implementationType;
                interfaceType = classType.GetField("_default", BindingFlags.Instance | BindingFlags.NonPublic)?.FieldType;
                if (interfaceType is null)
                {
                    throw new TypeLoadException($"Unable to create a runtime wrapper for a WinRT object of type '{runtimeClassName}'. This type is not a projected type.");
                }
                vftblType = interfaceType.GetNestedType("Vftbl") ?? throw new TypeLoadException($"Unable to find a vtable type for the type '{runtimeClassName}'");
            }

            ParameterExpression[] parms = new[] { Expression.Parameter(typeof(IInspectable), "inspectable") };
            var createInterfaceInstanceExpression = Expression.New(interfaceType.GetConstructor(new[] { typeof(ObjectReference<>).MakeGenericType(vftblType) }),
                    Expression.Call(parms[0],
                        typeof(IInspectable).GetMethod(nameof(IInspectable.As)).MakeGenericMethod(vftblType)));

            if (classType is null)
            {
                return Expression.Lambda<Func<IInspectable, object>>(createInterfaceInstanceExpression, parms).Compile();
            }

            return Expression.Lambda<Func<IInspectable, object>>(
                Expression.New(classType.GetConstructor(BindingFlags.NonPublic | BindingFlags.CreateInstance | BindingFlags.Instance, null, new[] { interfaceType }, null),
                    createInterfaceInstanceExpression),
                parms).Compile();
        }

        private static (Type type, int remaining) FindTypeByName(string runtimeClassName)
        {
            var (genericTypeName, genericTypes, remaining) = ParseGenericTypeName(runtimeClassName);
            return (FindTypeByNameCore(genericTypeName, genericTypes), remaining);
        }

        private static Type FindTypeByNameCore(string runtimeClassName, Type[] genericTypes)
        {
            // TODO: This implementation is a strawman implementation.
            // It's missing support for types not loaded in the default ALC.
            if (genericTypes is null)
            {
                Type primitiveType = ResolvePrimitiveType(runtimeClassName);
                if (primitiveType is object)
                {
                    return primitiveType;
                }
            }
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(runtimeClassName);
                if (type is object)
                {
                    if (genericTypes != null)
                    {
                        type = type.MakeGenericType(genericTypes);
                    }
                    return type;
                }
            }
            throw new TypeLoadException($"Unable to find a type named '{runtimeClassName}'");
        }

        private static Type ResolvePrimitiveType(string primitiveTypeName)
        {
            return primitiveTypeName switch
            {
                "UInt8" => typeof(byte),
                "Int8" => typeof(sbyte),
                "UInt16" => typeof(ushort),
                "Int16" => typeof(short),
                "UInt32" => typeof(uint),
                "Int32" => typeof(int),
                "UInt64" => typeof(ulong),
                "Int64" => typeof(long),
                "Boolean" => typeof(bool),
                "String" => typeof(string),
                "Char" => typeof(char),
                "Single" => typeof(float),
                "Double" => typeof(double),
                "Guid" => typeof(Guid),
                "Object" => typeof(object),
                _ => null
            };
        }

        // TODO: Use types from System.Memory to eliminate allocations of intermediate strings.
        private static (string genericTypeName, Type[] genericTypes, int remaining) ParseGenericTypeName(string partialTypeName)
        {
            int possibleEndOfSimpleTypeName = partialTypeName.IndexOfAny(new[] { ',', '>' });
            int endOfSimpleTypeName = partialTypeName.Length;
            if (possibleEndOfSimpleTypeName != -1)
            {
                endOfSimpleTypeName = possibleEndOfSimpleTypeName;
            }
            string typeName = partialTypeName.Substring(0, endOfSimpleTypeName);

            if (!typeName.Contains('`'))
            {
                return (typeName.ToString(), null, endOfSimpleTypeName);
            }

            int genericTypeListStart = partialTypeName.IndexOf('<');
            string genericTypeName = partialTypeName.Substring(0, genericTypeListStart);
            string remaining = partialTypeName.Substring(genericTypeListStart + 1);
            int remainingIndex = genericTypeListStart + 1;
            List<Type> genericTypes = new List<Type>();
            while (true)
            {
                var (genericType, endOfGenericArgument) = FindTypeByName(remaining);
                remainingIndex += endOfGenericArgument;
                genericTypes.Add(genericType);
                remaining = remaining.Substring(endOfGenericArgument);
                if (remaining[0] == ',')
                {
                    // Skip the comma and the space in the type name.
                    remainingIndex += 2;
                    remaining = remaining.Substring(2);
                    continue;
                }
                else if (remaining[0] == '>')
                {
                    break;
                }
                else
                {
                    throw new InvalidOperationException("The provided type name is invalid.");
                }
            }
            return (genericTypeName, genericTypes.ToArray(), partialTypeName.Length - remaining.Length);
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

            if (type == typeof(bool))
            {
                AbiType = typeof(byte);
                CreateMarshaler = (T value) => value;
                GetAbi = (object box) => (bool)box ? 1 : 0;
                FromAbi = (object box) => (T)(object)((byte)box != 0);
                DisposeMarshaler = (object box) => { };
                DisposeAbi = (object box) => { };
                CreateMarshalerArray = (T[] array) => MarshalNonBlittable<T>.CreateMarshalerArray(array);
                GetAbiArray = (object box) => MarshalNonBlittable<T>.GetAbiArray(box);
                FromAbiArray = (object box) => MarshalNonBlittable<T>.FromAbiArray(box);
                DisposeMarshalerArray = (object box) => MarshalNonBlittable<T>.DisposeMarshalerArray(box);
                DisposeAbiArray = (object box) => MarshalNonBlittable<T>.DisposeAbiArray(box);
            }
            else if (type == typeof(String))
            {
                AbiType = typeof(IntPtr);
                CreateMarshaler = (T value) => MarshalString.CreateMarshaler((string)(object)value);
                GetAbi = (object box) => MarshalString.GetAbi(box);
                FromAbi = (object value) => (T)(object)MarshalString.FromAbi((IntPtr)value);
                DisposeMarshaler = (object box) => MarshalString.DisposeMarshaler(box);
                DisposeAbi = (object box) => MarshalString.DisposeAbi(box);
                CreateMarshalerArray = (T[] array) => MarshalString.CreateMarshalerArray((string[])(object)array);
                GetAbiArray = (object box) => MarshalString.GetAbiArray(box);
                FromAbiArray = (object box) => (T[])(object)MarshalString.FromAbiArray(box);
                DisposeMarshalerArray = (object box) => MarshalString.DisposeMarshalerArray(box);
                DisposeAbiArray = (object box) => MarshalString.DisposeAbiArray(box);
            }
            else if (type.IsValueType)
            {
                // If type is blittable just pass through
                AbiType = type.FindHelperType();
                if (AbiType != null)
                {
                    // Could still be blittable and the 'ABI.*' type exists for other reasons (e.g. it's a mapped type)
                    if (AbiType.GetMethod("FromAbi") == null)
                    {
                        AbiType = null;
                    }
                }

                if (AbiType == null)
                {
                    AbiType = type;
                    CreateMarshaler = (T value) => value;
                    GetAbi = (object box) => box;
                    FromAbi = (object value) => (T)value;
                    DisposeMarshaler = (object box) => { };
                    DisposeAbi = (object box) => { };
                    CreateMarshalerArray = (T[] array) => MarshalBlittable<T>.CreateMarshalerArray(array);
                    GetAbiArray = (object box) => MarshalBlittable<T>.GetAbiArray(box);
                    FromAbiArray = (object box) => MarshalBlittable<T>.FromAbiArray(box);
                    DisposeMarshalerArray = (object box) => MarshalBlittable<T>.DisposeMarshalerArray(box);
                    DisposeAbiArray = (object box) => MarshalBlittable<T>.DisposeAbiArray(box);
                }
                else // bind to marshaler
                {
                    CreateMarshaler = MarshalNonBlittable<T>.CreateMarshaler;
                    GetAbi = MarshalNonBlittable<T>.GetAbi;
                    FromAbi = MarshalNonBlittable<T>.FromAbi;
                    DisposeMarshaler = MarshalNonBlittable<T>.DisposeMarshaler;
                    DisposeAbi = (object box) => { };
                    CreateMarshalerArray = (T[] array) => MarshalNonBlittable<T>.CreateMarshalerArray(array);
                    GetAbiArray = (object box) => MarshalNonBlittable<T>.GetAbiArray(box);
                    FromAbiArray = (object box) => MarshalNonBlittable<T>.FromAbiArray(box);
                    DisposeMarshalerArray = (object box) => MarshalNonBlittable<T>.DisposeMarshalerArray(box);
                    DisposeAbiArray = (object box) => MarshalNonBlittable<T>.DisposeAbiArray(box);
                }
            }
            else if (type.IsDelegate())
            {
                AbiType = typeof(IntPtr);
                CreateMarshaler = MarshalGeneric<T>.CreateMarshaler;
                GetAbi = MarshalGeneric<T>.GetAbi;
                FromAbi = MarshalGeneric<T>.FromAbi;
                DisposeMarshaler = MarshalGeneric<T>.DisposeMarshaler;
                DisposeAbi = (object box) => { };
            }
            else if (type.IsInterface)
            {
                AbiType = typeof(IntPtr);
                CreateMarshaler = (T value) => MarshalInterface<T>.CreateMarshaler(value);
                GetAbi = (object objRef) => MarshalInterface<T>.GetAbi((IObjectReference)objRef);
                FromAbi = (object value) => (T)(object)MarshalInterface<T>.FromAbi((IntPtr)value);
                DisposeMarshaler = (object objRef) => MarshalInterface<T>.DisposeMarshaler((IObjectReference)objRef);
                DisposeAbi = (object box) => MarshalInterface<T>.DisposeAbi((IntPtr)box);
            }
            else if (typeof(T) == typeof(object))
            {
                AbiType = typeof(IntPtr);
                CreateMarshaler = (T value) => MarshalInspectable.CreateMarshaler(value);
                GetAbi = (object objRef) => MarshalInspectable.GetAbi((IObjectReference)objRef);
                FromAbi = (object box) => (T)MarshalInspectable.FromAbi((IntPtr)box);
                DisposeMarshaler = (object objRef) => MarshalInspectable.DisposeMarshaler((IObjectReference)objRef);
                DisposeAbi = (object box) => MarshalInspectable.DisposeAbi((IntPtr)box);
            }
            else // class type
            {
                AbiType = typeof(IntPtr);
                var interfaceAbiType = type.GetField("_default", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)?.FieldType;
                if (interfaceAbiType is null)
                {
                    throw new ArgumentException($"Unable to marshal non-WinRT class: '{type.FullName}'");
                }
                CreateMarshaler = (T value) => value;
                GetAbi = BindClassGetAbi();
                FromAbi = BindClassFromAbi(interfaceAbiType);
                DisposeMarshaler = (object box) => { };
                DisposeAbi = (object box) => { };
            }
            RefAbiType = AbiType.MakeByRefType();
        }

        private static Func<object, T> BindClassFromAbi(Type AbiType)
        {
            var fromAbiMethod = AbiType.GetMethod("FromAbi");
            var objReferenceConstructor = AbiType.GetConstructor(new[] { fromAbiMethod.ReturnType });
            var parms = new[] { Expression.Parameter(typeof(object), "arg") };
            return Expression.Lambda<Func<object, T>>(
                Expression.New(
                    typeof(T).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.CreateInstance, null, new[] { AbiType }, null),
                    Expression.New(objReferenceConstructor,
                        Expression.Call(fromAbiMethod, Expression.Convert(parms[0], typeof(IntPtr))))), parms).Compile();
        }

        private static Func<object, object> BindClassGetAbi()
        {
            var thisPtrField = (Func<T, IntPtr>)typeof(T).GetProperty("ThisPtr").GetMethod.CreateDelegate(typeof(Func<T, IntPtr>));
            return (object value) => (object)thisPtrField((T)value);
        }

        public static readonly Type AbiType;
        public static readonly Type RefAbiType;
        public static readonly Func<T, object> CreateMarshaler;
        public static readonly Func<object, object> GetAbi;
        public static readonly Func<object, T> FromAbi;
        public static readonly Action<object> DisposeMarshaler;
        public static readonly Action<object> DisposeAbi;
        public static readonly Func<T[], object> CreateMarshalerArray;
        public static readonly Func<object, (int, IntPtr)> GetAbiArray;
        public static readonly Func<object, T[]> FromAbiArray;
        public static readonly Action<object> DisposeMarshalerArray;
        public static readonly Action<object> DisposeAbiArray;
    }
}
