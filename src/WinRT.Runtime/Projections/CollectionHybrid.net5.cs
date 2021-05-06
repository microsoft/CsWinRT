using System;
using System.Collections;
using System.Runtime.InteropServices;
using WinRT;

#pragma warning disable 0169 // warning CS0169: The field '...' is never used
#pragma warning disable 0649 // warning CS0169: Field '...' is never assigned to



namespace ABI.System.Collections.Generic
{


    [DynamicInterfaceCastableImplementation]
    interface IReadOnlyList<T> : global::System.Collections.Generic.IReadOnlyList<T>
    {
        private static global::System.Collections.Generic.IReadOnlyList<T> CreateHelper(IWinRTObject _this)
        {
            var genericType = typeof(T);
            if (genericType.IsGenericType && genericType.GetGenericTypeDefinition() == typeof(global::System.Collections.Generic.KeyValuePair<,>))
            {
                // was ROCollection does Dictionary, I changed to ROList here v
                var iROList = typeof(global::System.Collections.Generic.IReadOnlyList<>).MakeGenericType(genericType.GetGenericArguments());
                if (_this.IsInterfaceImplemented(iROList.TypeHandle, false))
                {
                    return (global::System.Collections.Generic.IReadOnlyList<T>) 
                        iROList.FindHelperType().GetMethod(
                            "_FromMapView", //  change?
                            global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.NonPublic | global::System.Reflection.BindingFlags.Static
                        ).Invoke(null, global::System.Reflection.BindingFlags.Default, null, new object[] { _this }, null);
                }
            }
            
            // needed ? 
            var iReadOnlyList = typeof(global::System.Collections.Generic.IReadOnlyList<T>);
            if (_this.IsInterfaceImplemented(iReadOnlyList.TypeHandle, false))
            {
                return IReadOnlyList<T>._FromVectorView(_this);
            }

            throw new InvalidOperationException("IReadOnlyCollection helper can not determine derived type.");
        }
    }

    /*
    [DynamicInterfaceCastableImplementation]
    interface IEnumerable<T> : global::System.Collections.Generic.IEnumerable<T>
    {
        private static global::System.Collections.Generic.IEnumerable<T> CreateHelper(IWinRTObject _this)
        {
            var genericType = typeof(T);
            if (genericType.IsGenericType && genericType.GetGenericTypeDefinition() == typeof(global::System.Collections.Generic.KeyValuePair<,>))
            {
                var iEnumerable = typeof(global::System.Collections.Generic.IEnumerable<>).MakeGenericType(genericType.GetGenericArguments());
                if (_this.IsInterfaceImplemented(iEnumerable.TypeHandle, false))
                {
                    return (global::System.Collections.Generic.ICollection<T>)
                        iEnumerable.FindHelperType().GetMethod(
                            "_FromMap????",
                            global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.NonPublic | global::System.Reflection.BindingFlags.Static
                        ).Invoke(null, global::System.Reflection.BindingFlags.Default, null, new object[] { _this }, null);
                }
            }
    
            // var iList = typeof(global::System.Collections.Generic.IList<T>);
            // if (_this.IsInterfaceImplemented(iList.TypeHandle, false))
            // {
            //     return IList<T>._FromVector(_this);
            // }
            throw new InvalidOperationException("ICollection helper can not determine derived type.");
        }

        private static global::System.Collections.Generic.IEnumerable<T> GetHelper(IWinRTObject _this)
        {
            return (global::System.Collections.Generic.IEnumerable<T>)_this.GetOrCreateTypeHelperData( 
                typeof(global::System.Collections.Generic.IEnumerable<T>).TypeHandle, 
                () => CreateHelper(_this));
        }

        global::System.Collections.Generic.IEnumerator<T> global::System.Collections.Generic.IEnumerable<T>.GetEnumerator()
            => GetHelper((IWinRTObject)this).GetEnumerator();
    }
    */

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
                    return (global::System.Collections.Generic.IReadOnlyCollection<T>) 
                        iReadOnlyDictionary.FindHelperType().GetMethod(
                            "_FromMapView",
                            global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.NonPublic | global::System.Reflection.BindingFlags.Static
                        ).Invoke(null, global::System.Reflection.BindingFlags.Default, null, new object[] { _this }, null);
                }
            }

            var iReadOnlyList = typeof(global::System.Collections.Generic.IReadOnlyList<T>);
            if (_this.IsInterfaceImplemented(iReadOnlyList.TypeHandle, false))
            {
                return IReadOnlyList<T>._FromVectorView(_this);
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
                    return (global::System.Collections.Generic.ICollection<T>)
                        iDictionary.FindHelperType().GetMethod(
                            "_FromMap",
                            global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.NonPublic | global::System.Reflection.BindingFlags.Static
                        ).Invoke(null, global::System.Reflection.BindingFlags.Default, null, new object[] { _this }, null);
                }
            }

            var iList = typeof(global::System.Collections.Generic.IList<T>);
            if (_this.IsInterfaceImplemented(iList.TypeHandle, false))
            {
                return IList<T>._FromVector(_this);
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
