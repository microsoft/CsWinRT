﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;
using WinRT;

#pragma warning disable 0169 // warning CS0169: The field '...' is never used
#pragma warning disable 0649 // warning CS0169: Field '...' is never assigned to

namespace ABI.System.Collections.Generic
{
    [DynamicInterfaceCastableImplementation]
    interface IReadOnlyCollection<T> : global::System.Collections.Generic.IReadOnlyCollection<T>
    {
        private static global::System.Collections.Generic.IReadOnlyCollection<T> CreateHelper(IWinRTObject _this)
        {
            var genericType = typeof(T);
            if (genericType.IsGenericType && genericType.GetGenericTypeDefinition() == typeof(global::System.Collections.Generic.KeyValuePair<,>))
            {
                var iReadOnlyDictionary = typeof(global::System.Collections.Generic.IReadOnlyDictionary<,>).MakeGenericType(genericType.GetGenericArguments());
                if (_this.IsInterfaceImplemented(iReadOnlyDictionary.TypeHandle, false))
                {
                    var iReadOnlyDictionaryImpl = typeof(global::System.Collections.Generic.IReadOnlyDictionaryImpl<,>).MakeGenericType(genericType.GetGenericArguments());
                    return (global::System.Collections.Generic.IReadOnlyCollection<T>)
                        iReadOnlyDictionaryImpl.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new global::System.Type[] { typeof(IObjectReference) }, null)
                        .Invoke(new object[] { _this.NativeObject });
                }
            }
             
            var iReadOnlyList = typeof(global::System.Collections.Generic.IReadOnlyList<T>);
            if (_this.IsInterfaceImplemented(iReadOnlyList.TypeHandle, false))
            {
                return new global::System.Collections.Generic.IReadOnlyListImpl<T>(_this.NativeObject);
            }

            throw new InvalidOperationException("IReadOnlyCollection helper can not determine derived type.");
        }

        private static global::System.Collections.Generic.IReadOnlyCollection<T> GetHelper(IWinRTObject _this)
        {
            return (global::System.Collections.Generic.IReadOnlyCollection<T>) _this.GetOrCreateTypeHelperData(
                typeof(global::System.Collections.Generic.IReadOnlyCollection<T>).TypeHandle,
                () => CreateHelper(_this));
        }

        int global::System.Collections.Generic.IReadOnlyCollection<T>.Count
            => GetHelper((IWinRTObject)this).Count;

        IEnumerator global::System.Collections.IEnumerable.GetEnumerator()
            => GetHelper((IWinRTObject)this).GetEnumerator();

        global::System.Collections.Generic.IEnumerator<T> global::System.Collections.Generic.IEnumerable<T>.GetEnumerator()
            => GetHelper((IWinRTObject)this).GetEnumerator();
    }

    [DynamicInterfaceCastableImplementation]
    interface ICollection<T> : global::System.Collections.Generic.ICollection<T>
    {
        private static global::System.Collections.Generic.ICollection<T> CreateHelper(IWinRTObject _this)
        {
            var genericType = typeof(T);
            if (genericType.IsGenericType && genericType.GetGenericTypeDefinition() == typeof(global::System.Collections.Generic.KeyValuePair<,>))
            {
                var iDictionary = typeof(global::System.Collections.Generic.IDictionary<,>).MakeGenericType(genericType.GetGenericArguments());
                if (_this.IsInterfaceImplemented(iDictionary.TypeHandle, false))
                {
                    var iDictionaryImpl = typeof(global::System.Collections.Generic.IDictionaryImpl<,>).MakeGenericType(genericType.GetGenericArguments());
                    return (global::System.Collections.Generic.ICollection<T>)
                        iDictionaryImpl.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new global::System.Type[] { typeof(IObjectReference) }, null)
                        .Invoke(new object[] { _this.NativeObject });
                }
            }

            var iList = typeof(global::System.Collections.Generic.IList<T>);
            if (_this.IsInterfaceImplemented(iList.TypeHandle, false))
            {
                return new global::System.Collections.Generic.IListImpl<T>(_this.NativeObject);
            }

            throw new InvalidOperationException("ICollection helper can not determine derived type.");
        }

        private static global::System.Collections.Generic.ICollection<T> GetHelper(IWinRTObject _this)
        {
            return (global::System.Collections.Generic.ICollection<T>)_this.GetOrCreateTypeHelperData(
                typeof(global::System.Collections.Generic.ICollection<T>).TypeHandle,
                () => CreateHelper(_this));
        }

        int global::System.Collections.Generic.ICollection<T>.Count 
            => GetHelper((IWinRTObject)this).Count;

        bool global::System.Collections.Generic.ICollection<T>.IsReadOnly
            => GetHelper((IWinRTObject)this).IsReadOnly;

        void global::System.Collections.Generic.ICollection<T>.Add(T item)
            => GetHelper((IWinRTObject)this).Add(item);

        void global::System.Collections.Generic.ICollection<T>.Clear()
            => GetHelper((IWinRTObject)this).Clear();

        bool global::System.Collections.Generic.ICollection<T>.Contains(T item)
            => GetHelper((IWinRTObject)this).Contains(item);

        void global::System.Collections.Generic.ICollection<T>.CopyTo(T[] array, int arrayIndex)
            => GetHelper((IWinRTObject)this).CopyTo(array, arrayIndex);

        bool global::System.Collections.Generic.ICollection<T>.Remove(T item)
            => GetHelper((IWinRTObject)this).Remove(item);

        IEnumerator global::System.Collections.IEnumerable.GetEnumerator()
            => GetHelper((IWinRTObject)this).GetEnumerator();

        global::System.Collections.Generic.IEnumerator<T> global::System.Collections.Generic.IEnumerable<T>.GetEnumerator()
            => GetHelper((IWinRTObject)this).GetEnumerator();
    }
}
