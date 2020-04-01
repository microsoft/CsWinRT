using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using WinRT;
using WinRT.Interop;
using System.Diagnostics;

#pragma warning disable 0169 // warning CS0169: The field '...' is never used
#pragma warning disable 0649 // warning CS0169: Field '...' is never assigned to

#nullable enable
namespace ABI.Windows.Foundation.Collections.Adapters
{
    using global::Windows.Foundation.Collections;

    internal static class ExceptionExtensions
    {
        public static void SetHResult(this Exception ex, int value)
        {
            ex.GetType().GetProperty("HResult").SetValue(ex, value);
        }

        internal static Exception GetExceptionForHR(this Exception? innerException, int hresult, string messageResource)
        {
            Exception? e;
            if (innerException != null)
            {
                string message = innerException.Message ?? messageResource;
                e = new Exception(message, innerException);
            }
            else
            {
                e = new Exception(messageResource);
            }

            e.SetHResult(hresult);
            return e;
        }
    }

    internal class ErrorStrings
    {
        internal static string Format(string format, params object[] args) => String.Format(format, args);

        internal static readonly string Arg_IndexOutOfRangeException = "Index was outside the bounds of the array.";
        internal static readonly string Arg_KeyNotFound = "The given key was not present in the dictionary.";
        internal static readonly string Arg_KeyNotFoundWithKey = "The given key '{0}' was not present in the dictionary.";
        internal static readonly string Argument_AddingDuplicate = "An item with the same key has already been added.";
        internal static readonly string Argument_AddingDuplicateWithKey = "An item with the same key has already been added. Key: {0}";
        internal static readonly string Argument_IndexOutOfArrayBounds = "The specified index is out of bounds of the specified array.";
        internal static readonly string Argument_InsufficientSpaceToCopyCollection = "The specified space is not sufficient to copy the elements from this Collection.";
        internal static readonly string ArgumentOutOfRange_Index = "Index was out of range. Must be non-negative and less than the size of the collection.";
        internal static readonly string ArgumentOutOfRange_IndexLargerThanMaxValue = "This collection cannot work with indices larger than Int32.MaxValue - 1 (0x7FFFFFFF - 1).";
        internal static readonly string InvalidOperation_CannotRemoveLastFromEmptyCollection = "Cannot remove the last element from an empty collection.";
        internal static readonly string InvalidOperation_CollectionBackingDictionaryTooLarge = "The collection backing this Dictionary contains too many elements.";
        internal static readonly string InvalidOperation_CollectionBackingListTooLarge = "The collection backing this List contains too many elements.";
        internal static readonly string InvalidOperation_EnumEnded = "Enumeration already finished.";
        internal static readonly string InvalidOperation_EnumFailedVersion = "Collection was modified; enumeration operation may not execute.";
        internal static readonly string InvalidOperation_EnumNotStarted = "Enumeration has not started. Call MoveNext.";
        internal static readonly string NotSupported_KeyCollectionSet = "Mutating a key collection derived from a dictionary is not allowed.";
        internal static readonly string NotSupported_ValueCollectionSet = "Mutating a value collection derived from a dictionary is not allowed.";
    }

    internal static class Adapter
    {
        internal static void EnsureIndexInt32(uint index, int limit = int.MaxValue)
        {
            // We use '<=' and not '<' because int.MaxValue == index would imply
            // that Size > int.MaxValue:
            if (((uint)int.MaxValue) <= index || index >= (uint)limit)
            {
                Exception e = new ArgumentOutOfRangeException(nameof(index), ErrorStrings.ArgumentOutOfRange_IndexLargerThanMaxValue);
                e.SetHResult(ExceptionHelpers.E_BOUNDS);
                throw e;
            }
        }
    }

    public class IterableToEnumerable<T> : IEnumerable<T>
    {
        private readonly global::ABI.System.Collections.Generic.IEnumerable<T> _iterable;

        public IterableToEnumerable(IObjectReference obj) :
            this(new global::ABI.System.Collections.Generic.IEnumerable<T>(obj))
        {
        }

        public IterableToEnumerable(global::ABI.System.Collections.Generic.IEnumerable<T> iterable)
        {
            _iterable = iterable;
        }

        public IEnumerator<T> GetEnumerator()
        {
            var first = _iterable.First();
            if (first is global::ABI.System.Collections.Generic.IEnumerator<T> iterable)
            {
                return iterable;
            }
            if (first is EnumeratorToIterator<T> adapter)
            {
                return adapter.Enumerator;
            }
            throw new InvalidOperationException("Unexpected type for enumerator");
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public sealed class EnumerableToIterable<T> : global::Windows.Foundation.Collections.IIterable<T> //, IBindableIterable
    {
        private readonly IEnumerable<T> m_enumerable;

        internal EnumerableToIterable(IEnumerable<T> enumerable) => m_enumerable = enumerable;

        public global::Windows.Foundation.Collections.IIterator<T> First() =>
            new EnumeratorToIterator<T>(m_enumerable.GetEnumerator());
    }

    public class IteratorToEnumerator<T> : IEnumerator<T>
    {
        private readonly global::ABI.System.Collections.Generic.IEnumerator<T> _iterator;

        public IteratorToEnumerator(IObjectReference obj) :
            this(new global::ABI.System.Collections.Generic.IEnumerator<T>(obj))
        {
        }

        public IteratorToEnumerator(global::ABI.System.Collections.Generic.IEnumerator<T> iterator)
        {
            _iterator = iterator;
        }

        private bool m_hadCurrent = true;
        private T m_current = default!;
        private bool m_isInitialized = false;

        public T Current
        {
            get
            {
                // The enumerator has not been advanced to the first element yet.
                if (!m_isInitialized)
                    throw new InvalidOperationException(ErrorStrings.InvalidOperation_EnumNotStarted);
                // The enumerator has reached the end of the collection
                if (!m_hadCurrent)
                    throw new InvalidOperationException(ErrorStrings.InvalidOperation_EnumEnded);
                return m_current;
            }
        }

        object? IEnumerator.Current
        {
            get
            {
                // The enumerator has not been advanced to the first element yet.
                if (!m_isInitialized)
                    throw new InvalidOperationException(ErrorStrings.InvalidOperation_EnumNotStarted);
                // The enumerator has reached the end of the collection
                if (!m_hadCurrent)
                    throw new InvalidOperationException(ErrorStrings.InvalidOperation_EnumEnded);
                return m_current;
            }
        }

        public bool MoveNext()
        {
            // If we've passed the end of the iteration, IEnumerable<T> should return false, while
            // IIterable will fail the interface call
            if (!m_hadCurrent)
            {
                return false;
            }

            // IIterators start at index 0, rather than -1.  If this is the first call, we need to just
            // check HasCurrent rather than actually moving to the next element
            try
            {
                if (!m_isInitialized)
                {
                    m_hadCurrent = _iterator.HasCurrent;
                    m_isInitialized = true;
                }
                else
                {
                    m_hadCurrent = _iterator._MoveNext();
                }

                // We want to save away the current value for two reasons:
                //  1. Accessing .Current is cheap on other iterators, so having it be a property which is a
                //     simple field access preserves the expected performance characteristics (as opposed to
                //     triggering a COM call every time the property is accessed)
                //
                //  2. This allows us to preserve the same semantics as generic collection iteration when iterating
                //     beyond the end of the collection - namely that Current continues to return the last value
                //     of the collection
                if (m_hadCurrent)
                {
                    m_current = _iterator._Current;
                }
            }
            catch (Exception e)
            {
                // Translate E_CHANGED_STATE into an InvalidOperationException for an updated enumeration
                if (Marshal.GetHRForException(e) == ExceptionHelpers.E_CHANGED_STATE)
                {
                    throw new InvalidOperationException(ErrorStrings.InvalidOperation_EnumFailedVersion);
                }
                else
                {
                    throw;
                }
            }

            return m_hadCurrent;
        }

        public void Reset()
        {
            throw new NotSupportedException();
        }

        public void Dispose()
        {
        }
    }

    public sealed class EnumeratorToIterator<T> : global::Windows.Foundation.Collections.IIterator<T> //, IBindableIterator
    {
        private readonly IEnumerator<T> m_enumerator;
        private bool m_firstItem = true;
        private bool m_hasCurrent;

        public IEnumerator<T> Enumerator { get => m_enumerator; }

        internal EnumeratorToIterator(IEnumerator<T> enumerator) => m_enumerator = enumerator;

        public T _Current
        {
            get
            {
                // IEnumerator starts at item -1, while IIterators start at item 0.  Therefore, if this is the
                // first access to the iterator we need to advance to the first item.
                if (m_firstItem)
                {
                    m_firstItem = false;
                    _MoveNext();
                }

                if (!m_hasCurrent)
                {
                    ExceptionHelpers.ThrowExceptionForHR(ExceptionHelpers.E_BOUNDS);
                }

                return m_enumerator.Current;
            }
        }

        //object? IBindableIterator.Current => ((IIterator<T>)this).Current;

        public bool HasCurrent
        {
            get
            {
                // IEnumerator starts at item -1, while IIterators start at item 0.  Therefore, if this is the
                // first access to the iterator we need to advance to the first item.
                if (m_firstItem)
                {
                    m_firstItem = false;
                    _MoveNext();
                }

                return m_hasCurrent;
            }
        }

        public bool _MoveNext()
        {
            try
            {
                m_hasCurrent = m_enumerator.MoveNext();
            }
            catch (InvalidOperationException)
            {
                ExceptionHelpers.ThrowExceptionForHR(ExceptionHelpers.E_CHANGED_STATE);
            }

            return m_hasCurrent;
        }

        public uint GetMany(ref T[] items)
        {
            if (items == null)
            {
                return 0;
            }

            int index = 0;
            while (index < items.Length && HasCurrent)
            {
                items[index] = _Current;
                _MoveNext();
                ++index;
            }

            if (typeof(T) == typeof(string))
            {
                string[] stringItems = (items as string[])!;

                // Fill the rest of the array with string.Empty to avoid marshaling failure
                for (int i = index; i < items.Length; ++i)
                    stringItems[i] = string.Empty;
            }

            return (uint)index;
        }
    }

    public class VectorToList<T> : IList<T>
    {
        private readonly global::ABI.System.Collections.Generic.IList<T> _vector;
        private readonly global::ABI.System.Collections.Generic.IEnumerable<T> _enumerable;

        public VectorToList(IObjectReference obj) :
            this(new global::ABI.System.Collections.Generic.IList<T>(obj))
        {
        }

        public VectorToList(global::ABI.System.Collections.Generic.IList<T> vector)
        {
            _vector = vector;
            _enumerable = new ABI.System.Collections.Generic.IEnumerable<T>(vector.ObjRef);
        }

        public int Count
        {
            get
            {
                uint size = _vector.Size;
                if (((uint)int.MaxValue) < size)
                {
                    throw new InvalidOperationException(ErrorStrings.InvalidOperation_CollectionBackingListTooLarge);
                }

                return (int)size;
            }
        }

        public bool IsReadOnly { get => false; }

        public void Add(T item) => _vector.Append(item);

        public void Clear() => _vector._Clear();

        public bool Contains(T item) => _vector.IndexOf(item, out _);

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));

            if (array.Length <= arrayIndex && Count > 0)
                throw new ArgumentException(ErrorStrings.Argument_IndexOutOfArrayBounds);

            if (array.Length - arrayIndex < Count)
                throw new ArgumentException(ErrorStrings.Argument_InsufficientSpaceToCopyCollection);


            int count = Count;
            for (int i = 0; i < count; i++)
            {
                array[i + arrayIndex] = VectorToList<T>.GetAt(_vector, (uint)i);
            }
        }

        public bool Remove(T item)
        {
            uint index;
            bool exists = _vector.IndexOf(item, out index);

            if (!exists)
                return false;

            if (((uint)int.MaxValue) < index)
            {
                throw new InvalidOperationException(ErrorStrings.InvalidOperation_CollectionBackingListTooLarge);
            }

            VectorToList<T>.RemoveAtHelper(_vector, index);
            return true;
        }

        public T this[int index] { get => Indexer_Get(index); set => Indexer_Set(index, value); }

        private T Indexer_Get(int index)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            return GetAt(_vector, (uint)index);
        }

        private void Indexer_Set(int index, T value)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            SetAt(_vector, (uint)index, value);
        }

        public int IndexOf(T item)
        {
            uint index;
            bool exists = _vector.IndexOf(item, out index);

            if (!exists)
                return -1;

            if (((uint)int.MaxValue) < index)
            {
                throw new InvalidOperationException(ErrorStrings.InvalidOperation_CollectionBackingListTooLarge);
            }

            return (int)index;
        }

        public void Insert(int index, T item)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            InsertAtHelper(_vector, (uint)index, item);
        }

        public void RemoveAt(int index)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            RemoveAtHelper(_vector, (uint)index);
        }

        internal static T GetAt(global::Windows.Foundation.Collections.IVector<T> _this, uint index)
        {
            try
            {
                return _this.GetAt(index);

                // We delegate bounds checking to the underlying collection and if it detected a fault,
                // we translate it to the right exception:
            }
            catch (Exception ex)
            {
                if (ExceptionHelpers.E_BOUNDS == ex.HResult)
                    throw new ArgumentOutOfRangeException(nameof(index));

                throw;
            }
        }

        private static void SetAt(global::Windows.Foundation.Collections.IVector<T> _this, uint index, T value)
        {
            try
            {
                _this.SetAt(index, value);

                // We deligate bounds checking to the underlying collection and if it detected a fault,
                // we translate it to the right exception:
            }
            catch (Exception ex)
            {
                if (ExceptionHelpers.E_BOUNDS == ex.HResult)
                    throw new ArgumentOutOfRangeException(nameof(index));

                throw;
            }
        }

        private static void InsertAtHelper(global::Windows.Foundation.Collections.IVector<T> _this, uint index, T item)
        {
            try
            {
                _this.InsertAt(index, item);

                // We delegate bounds checking to the underlying collection and if it detected a fault,
                // we translate it to the right exception:
            }
            catch (Exception ex)
            {
                if (ExceptionHelpers.E_BOUNDS == ex.HResult)
                    throw new ArgumentOutOfRangeException(nameof(index));

                throw;
            }
        }

        internal static void RemoveAtHelper(global::Windows.Foundation.Collections.IVector<T> _this, uint index)
        {
            try
            {
                _this.RemoveAt(index);

                // We delegate bounds checking to the underlying collection and if it detected a fault,
                // we translate it to the right exception:
            }
            catch (Exception ex)
            {
                if (ExceptionHelpers.E_BOUNDS == ex.HResult)
                    throw new ArgumentOutOfRangeException(nameof(index));

                throw;
            }
        }

        public IEnumerator<T> GetEnumerator() => _enumerable.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public sealed class ListToVector<T> : global::Windows.Foundation.Collections.IVector<T>
    {
        private IList<T> _list;

        public ListToVector(IList<T> list) => _list = list;

        public global::Windows.Foundation.Collections.IIterator<T> First() =>
            new EnumeratorToIterator<T>(_list.GetEnumerator());

        public T GetAt(uint index)
        {
            Adapter.EnsureIndexInt32(index, _list.Count);

            try
            {
                return _list[(int)index];
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw ex.GetExceptionForHR(ExceptionHelpers.E_BOUNDS, ErrorStrings.ArgumentOutOfRange_Index);
            }
        }

        public uint Size => (uint)_list.Count;

        public global::Windows.Foundation.Collections.IVectorView<T> GetView()
        {
            // Note: This list is not really read-only - you could QI for a modifiable
            // list.  We gain some perf by doing this.  We believe this is acceptable.
            if (!(_list is IReadOnlyList<T> roList))
            {
                roList = new ReadOnlyCollection<T>(_list);
            }
            return new ReadOnlyListToVectorView<T>(roList);
        }

        public bool IndexOf(T value, out uint index)
        {
            int ind = _list.IndexOf(value);

            if (-1 == ind)
            {
                index = 0;
                return false;
            }

            index = (uint)ind;
            return true;
        }

        public void SetAt(uint index, T value)
        {
            Adapter.EnsureIndexInt32(index, _list.Count);

            try
            {
                _list[(int)index] = value;
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw ex.GetExceptionForHR(ExceptionHelpers.E_BOUNDS, ErrorStrings.ArgumentOutOfRange_Index);
            }
        }

        public void InsertAt(uint index, T value)
        {
            // Inserting at an index one past the end of the list is equivalent to appending
            // so we need to ensure that we're within (0, count + 1).
            Adapter.EnsureIndexInt32(index, _list.Count + 1);

            try
            {
                _list.Insert((int)index, value);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                // Change error code to match what WinRT expects
                ex.SetHResult(ExceptionHelpers.E_BOUNDS);
                throw;
            }
        }

        public void RemoveAt(uint index)
        {
            Adapter.EnsureIndexInt32(index, _list.Count);

            try
            {
                _list.RemoveAt((int)index);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                // Change error code to match what WinRT expects
                ex.SetHResult(ExceptionHelpers.E_BOUNDS);
                throw;
            }
        }

        public void Append(T value)
        {
            _list.Add(value);
        }

        public void RemoveAtEnd()
        {
            if (_list.Count == 0)
            {
                Exception e = new InvalidOperationException(ErrorStrings.InvalidOperation_CannotRemoveLastFromEmptyCollection);
                e.SetHResult(ExceptionHelpers.E_BOUNDS);
                throw e;
            }

            uint size = (uint)_list.Count;
            RemoveAt(size - 1);
        }

        public void _Clear()
        {
            _list.Clear();
        }

        public uint GetMany(uint startIndex, ref T[] items)
        {
            return GetManyHelper(_list, startIndex, items);
        }

        public void ReplaceAll(T[] items)
        {
            _list.Clear();

            if (items != null)
            {
                foreach (T item in items)
                {
                    _list.Add(item);
                }
            }
        }

        private static uint GetManyHelper(IList<T> sourceList, uint startIndex, T[] items)
        {
            // Calling GetMany with a start index equal to the size of the list should always
            // return 0 elements, regardless of the input item size
            if (startIndex == sourceList.Count)
            {
                return 0;
            }

            Adapter.EnsureIndexInt32(startIndex, sourceList.Count);

            if (items == null)
            {
                return 0;
            }

            uint itemCount = Math.Min((uint)items.Length, (uint)sourceList.Count - startIndex);
            for (uint i = 0; i < itemCount; ++i)
            {
                items[i] = sourceList[(int)(i + startIndex)];
            }

            if (typeof(T) == typeof(string))
            {
                string[] stringItems = (items as string[])!;

                // Fill in rest of the array with string.Empty to avoid marshaling failure
                for (uint i = itemCount; i < items.Length; ++i)
                    stringItems[i] = string.Empty;
            }

            return itemCount;
        }
    }

    public class VectorViewToReadOnlyList<T> : IReadOnlyList<T>
    {
        private readonly global::ABI.System.Collections.Generic.IReadOnlyList<T> _vectorView;
        private readonly global::ABI.System.Collections.Generic.IEnumerable<T> _enumerable;

        public VectorViewToReadOnlyList(IObjectReference obj) :
            this(new global::ABI.System.Collections.Generic.IReadOnlyList<T>(obj))
        {
        }

        public VectorViewToReadOnlyList(global::ABI.System.Collections.Generic.IReadOnlyList<T> vectorView)
        {
            _vectorView = vectorView;
            _enumerable = new ABI.System.Collections.Generic.IEnumerable<T>(vectorView.ObjRef);
        }

        public int Count
        {
            get
            {
                uint size = _vectorView.Size;
                if (((uint)int.MaxValue) < size)
                {
                    throw new InvalidOperationException(ErrorStrings.InvalidOperation_CollectionBackingListTooLarge);
                }

                return (int)size;
            }
        }

        public T this[int index] { get => Indexer_Get(index); }

        private T Indexer_Get(int index)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));

            try
            {
                return _vectorView.GetAt((uint)index);

                // We delegate bounds checking to the underlying collection and if it detected a fault,
                // we translate it to the right exception:
            }
            catch (Exception ex)
            {
                if (ExceptionHelpers.E_BOUNDS == ex.HResult)
                    throw new ArgumentOutOfRangeException(nameof(index));

                throw;
            }
        }

        public IEnumerator<T> GetEnumerator() => _enumerable.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        //// T this[int index] { get }
        //internal T Indexer_Get_Variance(int index) where T : class
        //{
        //    bool fUseString;
        //    Delegate target = System.StubHelpers.StubHelpers.GetTargetForAmbiguousVariantCall(
        //        this,
        //        typeof(IReadOnlyList<T>).TypeHandle.Value,
        //        out fUseString);

        //    if (target != null)
        //    {
        //        return (Unsafe.As<Indexer_Get_Delegate<T>>(target))(index);
        //    }

        //    if (fUseString)
        //    {
        //        return Unsafe.As<T>(Indexer_Get<string>(index));
        //    }

        //    return Indexer_Get<T>(index);
        //}
    }

    public sealed class ReadOnlyListToVectorView<T> : global::Windows.Foundation.Collections.IVectorView<T>
    {
        private readonly IReadOnlyList<T> _list;

        internal ReadOnlyListToVectorView(IReadOnlyList<T> list) => _list = list;

        public global::Windows.Foundation.Collections.IIterator<T> First() =>
            new EnumeratorToIterator<T>(_list.GetEnumerator());

        public T GetAt(uint index)
        {
            Adapter.EnsureIndexInt32(index, _list.Count);

            try
            {
                return _list[(int)index];
            }
            catch (ArgumentOutOfRangeException ex)
            {
                ex.SetHResult(ExceptionHelpers.E_BOUNDS);
                throw;
            }
        }

        public uint Size => (uint)_list.Count;

        public bool IndexOf(T value, out uint index)
        {
            int ind = -1;
            int max = _list.Count;
            for (int i = 0; i < max; i++)
            {
                if (EqualityComparer<T>.Default.Equals(value, _list[i]))
                {
                    ind = i;
                    break;
                }
            }

            if (-1 == ind)
            {
                index = 0;
                return false;
            }

            index = (uint)ind;
            return true;
        }

        public uint GetMany(uint startIndex, ref T[] items)
        {
            // REX spec says "calling GetMany with startIndex equal to the length of the vector
            // (last valid index + 1) and any specified capacity will succeed and return zero actual
            // elements".
            if (startIndex == _list.Count)
                return 0;

            Adapter.EnsureIndexInt32(startIndex, _list.Count);

            if (items == null)
            {
                return 0;
            }

            uint itemCount = Math.Min((uint)items.Length, (uint)_list.Count - startIndex);

            for (uint i = 0; i < itemCount; ++i)
            {
                items[i] = _list[(int)(i + startIndex)];
            }

            if (typeof(T) == typeof(string))
            {
                string[] stringItems = (items as string[])!;

                // Fill in the rest of the array with string.Empty to avoid marshaling failure
                for (uint i = itemCount; i < items.Length; ++i)
                    stringItems[i] = string.Empty;
            }

            return itemCount;
        }
    }

    public class MapToDictionary<K, V> : IDictionary<K, V>
    {
        private readonly global::ABI.System.Collections.Generic.IDictionary<K, V> _map;
        private readonly global::ABI.System.Collections.Generic.IEnumerable<KeyValuePair<K, V>> _enumerable;

        public MapToDictionary(IObjectReference obj) :
            this(new global::ABI.System.Collections.Generic.IDictionary<K, V>(obj))
        {
        }

        public MapToDictionary(global::ABI.System.Collections.Generic.IDictionary<K, V> map)
        {
            _map = map;
            _enumerable = new ABI.System.Collections.Generic.IEnumerable<KeyValuePair<K, V>>(map.ObjRef);
        }

        public int Count
        {
            get
            {
                uint size = _map.Size;

                if (((uint)int.MaxValue) < size)
                {
                    throw new InvalidOperationException(ErrorStrings.InvalidOperation_CollectionBackingDictionaryTooLarge);
                }

                return (int)size;

                //if (this is global::Windows.Foundation.Collections.IMap<K, V> _this_map)
                //{
                //    uint size = _this_map.Size;

                //    if (((uint)int.MaxValue) < size)
                //    {
                //        throw new InvalidOperationException(ErrorStrings.InvalidOperation_CollectionBackingDictionaryTooLarge);
                //    }

                //    return (int)size;
                //}
                //else
                //{
                //    var _this_vector = (global::Windows.Foundation.Collections.IVector<KeyValuePair<K, V>>)this;
                //    uint size = _this_vector.Size;

                //    if (((uint)int.MaxValue) < size)
                //    {
                //        throw new InvalidOperationException(ErrorStrings.InvalidOperation_CollectionBackingListTooLarge);
                //    }

                //    return (int)size;
                //}
            }
        }

        public bool IsReadOnly { get => false; }

        public void Add(KeyValuePair<K, V> item)
        {
            _map.Insert(item.Key, item.Value);

            //if (this is IDictionary<K, V> _this_dictionary)
            //{
            //    _this_dictionary.Add(item.Key, item.Value);
            //}
            //else
            //{
            //    var _this_vector = (global::Windows.Foundation.Collections.IVector<KeyValuePair<K, V>>)this;
            //    _this_vector.Append(item);
            //}
        }

        public void Clear()
        {
            _map.Clear();

            //if (this is global::Windows.Foundation.Collections.IMap<K, V> _this_map)
            //{
            //    _this_map.Clear();
            //}
            //else
            //{
            //    var _this_vector = (global::Windows.Foundation.Collections.IVector<KeyValuePair<K, V>>)this;
            //    _this_vector.Clear();
            //}
        }

        public bool Contains(KeyValuePair<K, V> item)
        {
            bool hasKey = _map.HasKey(item.Key);
            if (!hasKey)
                return false;
            // todo: toctou
            V value = _map.Lookup(item.Key);
            return EqualityComparer<V>.Default.Equals(value, item.Value);

            //if (this is IDictionary<K, V> _this_dictionary)
            //{
            //    V value;
            //    bool hasKey = _this_dictionary.TryGetValue(item.Key, out value);

            //    if (!hasKey)
            //        return false;

            //    return EqualityComparer<V>.Default.Equals(value, item.Value);
            //}
            //else
            //{
            //    var _this_vector = (global::Windows.Foundation.Collections.IVector<KeyValuePair<K, V>>)this;

            //    return _this_vector.IndexOf(item, out _);
            //}
        }

        public void CopyTo(KeyValuePair<K, V>[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));

            if (array.Length <= arrayIndex && Count > 0)
                throw new ArgumentException(ErrorStrings.Argument_IndexOutOfArrayBounds);

            if (array.Length - arrayIndex < Count)
                throw new ArgumentException(ErrorStrings.Argument_InsufficientSpaceToCopyCollection);

            foreach (KeyValuePair<K, V> mapping in this)
            {
                array[arrayIndex++] = mapping;
            }
        }

        public bool Remove(KeyValuePair<K, V> item)
        {
            _map._Remove(item.Key);
            return true;

            //if (this is IDictionary<K, V> _this_dictionary)
            //{
            //    return _this_dictionary.Remove(item.Key);
            //}
            //else
            //{
            //    var _this_vector = (global::Windows.Foundation.Collections.IVector<KeyValuePair<K, V>>)this;
            //    uint index;
            //    bool exists = _this_vector.IndexOf(item, out index);

            //    if (!exists)
            //        return false;

            //    if (((uint)int.MaxValue) < index)
            //    {
            //        throw new InvalidOperationException(ErrorStrings.InvalidOperation_CollectionBackingListTooLarge);
            //    }

            //    VectorToList<KeyValuePair<K, V>>.RemoveAtHelper(_this_vector, index);
            //    return true;
            //}
        }

        public V this[K key] { get => Indexer_Get(key); set => Indexer_Set(key, value); }

        private V Indexer_Get(K key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            return Lookup(_map, key);
        }
        private void Indexer_Set(K key, V value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            Insert(_map, key, value);
        }

        public ICollection<K> Keys { get => new DictionaryKeyCollection(this); }

        public ICollection<V> Values { get => new DictionaryValueCollection(this); }

        public bool ContainsKey(K key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            return _map.HasKey(key);
        }

        public void Add(K key, V value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (ContainsKey(key))
                throw new ArgumentException(ErrorStrings.Argument_AddingDuplicate);

            Insert(_map, key, value);
        }

        public bool Remove(K key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (!_map.HasKey(key))
                return false;

            try
            {
                _map._Remove(key);
                return true;
            }
            catch (Exception ex)
            {
                if (ExceptionHelpers.E_BOUNDS == ex.HResult)
                    return false;

                throw;
            }
        }

        public bool TryGetValue(K key, out V value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (!_map.HasKey(key))
            {
                value = default!;
                return false;
            }

            try
            {
                value = Lookup(_map, key);
                return true;
            }
            catch (KeyNotFoundException)
            {
                value = default!;
                return false;
            }
        }

        private static V Lookup(global::Windows.Foundation.Collections.IMap<K, V> _this, K key)
        {
            Debug.Assert(null != key);

            try
            {
                return _this.Lookup(key);
            }
            catch (Exception ex)
            {
                if (ExceptionHelpers.E_BOUNDS == ex.HResult)
                    throw new KeyNotFoundException(ErrorStrings.Arg_KeyNotFound);
                throw;
            }
        }

        private static bool Insert(global::Windows.Foundation.Collections.IMap<K, V> _this, K key, V value)
        {
            Debug.Assert(null != key);

            bool replaced = _this.Insert(key, value);
            return replaced;
        }

        public IEnumerator<KeyValuePair<K, V>> GetEnumerator() => _enumerable.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private sealed class DictionaryKeyCollection : ICollection<K>
        {
            private readonly IDictionary<K, V> dictionary;

            public DictionaryKeyCollection(IDictionary<K, V> dictionary)
            {
                if (dictionary == null)
                    throw new ArgumentNullException(nameof(dictionary));

                this.dictionary = dictionary;
            }

            public void CopyTo(K[] array, int index)
            {
                if (array == null)
                    throw new ArgumentNullException(nameof(array));
                if (index < 0)
                    throw new ArgumentOutOfRangeException(nameof(index));
                if (array.Length <= index && this.Count > 0)
                    throw new ArgumentException(ErrorStrings.Arg_IndexOutOfRangeException);
                if (array.Length - index < dictionary.Count)
                    throw new ArgumentException(ErrorStrings.Argument_InsufficientSpaceToCopyCollection);

                int i = index;
                foreach (KeyValuePair<K, V> mapping in dictionary)
                {
                    array[i++] = mapping.Key;
                }
            }

            public int Count => dictionary.Count;

            bool ICollection<K>.IsReadOnly => true;

            void ICollection<K>.Add(K item)
            {
                throw new NotSupportedException(ErrorStrings.NotSupported_KeyCollectionSet);
            }

            void ICollection<K>.Clear()
            {
                throw new NotSupportedException(ErrorStrings.NotSupported_KeyCollectionSet);
            }

            public bool Contains(K item)
            {
                return dictionary.ContainsKey(item);
            }

            bool ICollection<K>.Remove(K item)
            {
                throw new NotSupportedException(ErrorStrings.NotSupported_KeyCollectionSet);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable<K>)this).GetEnumerator();
            }

            public IEnumerator<K> GetEnumerator()
            {
                return new DictionaryKeyEnumerator(dictionary);
            }

            private sealed class DictionaryKeyEnumerator : IEnumerator<K>
            {
                private readonly IDictionary<K, V> dictionary;
                private IEnumerator<KeyValuePair<K, V>> enumeration;

                public DictionaryKeyEnumerator(IDictionary<K, V> dictionary)
                {
                    if (dictionary == null)
                        throw new ArgumentNullException(nameof(dictionary));

                    this.dictionary = dictionary;
                    enumeration = dictionary.GetEnumerator();
                }

                void IDisposable.Dispose()
                {
                    enumeration.Dispose();
                }

                public bool MoveNext()
                {
                    return enumeration.MoveNext();
                }

                object? IEnumerator.Current => ((IEnumerator<K>)this).Current;

                public K Current => enumeration.Current.Key;

                public void Reset()
                {
                    enumeration = dictionary.GetEnumerator();
                }
            }
        }

        private sealed class DictionaryValueCollection : ICollection<V>
        {
            private readonly IDictionary<K, V> dictionary;

            public DictionaryValueCollection(IDictionary<K, V> dictionary)
            {
                if (dictionary == null)
                    throw new ArgumentNullException(nameof(dictionary));

                this.dictionary = dictionary;
            }

            public void CopyTo(V[] array, int index)
            {
                if (array == null)
                    throw new ArgumentNullException(nameof(array));
                if (index < 0)
                    throw new ArgumentOutOfRangeException(nameof(index));
                if (array.Length <= index && this.Count > 0)
                    throw new ArgumentException(ErrorStrings.Arg_IndexOutOfRangeException);
                if (array.Length - index < dictionary.Count)
                    throw new ArgumentException(ErrorStrings.Argument_InsufficientSpaceToCopyCollection);

                int i = index;
                foreach (KeyValuePair<K, V> mapping in dictionary)
                {
                    array[i++] = mapping.Value;
                }
            }

            public int Count => dictionary.Count;

            bool ICollection<V>.IsReadOnly => true;

            void ICollection<V>.Add(V item)
            {
                throw new NotSupportedException(ErrorStrings.NotSupported_ValueCollectionSet);
            }

            void ICollection<V>.Clear()
            {
                throw new NotSupportedException(ErrorStrings.NotSupported_ValueCollectionSet);
            }

            public bool Contains(V item)
            {
                EqualityComparer<V> comparer = EqualityComparer<V>.Default;
                foreach (V value in this)
                    if (comparer.Equals(item, value))
                        return true;
                return false;
            }

            bool ICollection<V>.Remove(V item)
            {
                throw new NotSupportedException(ErrorStrings.NotSupported_ValueCollectionSet);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable<V>)this).GetEnumerator();
            }

            public IEnumerator<V> GetEnumerator()
            {
                return new DictionaryValueEnumerator(dictionary);
            }

            private sealed class DictionaryValueEnumerator : IEnumerator<V>
            {
                private readonly IDictionary<K, V> dictionary;
                private IEnumerator<KeyValuePair<K, V>> enumeration;

                public DictionaryValueEnumerator(IDictionary<K, V> dictionary)
                {
                    if (dictionary == null)
                        throw new ArgumentNullException(nameof(dictionary));

                    this.dictionary = dictionary;
                    enumeration = dictionary.GetEnumerator();
                }

                void IDisposable.Dispose()
                {
                    enumeration.Dispose();
                }

                public bool MoveNext()
                {
                    return enumeration.MoveNext();
                }

                object? IEnumerator.Current => ((IEnumerator<V>)this).Current;

                public V Current => enumeration.Current.Value;

                public void Reset()
                {
                    enumeration = dictionary.GetEnumerator();
                }
            }
        }
    }

    public class DictionaryToMap</*KDict, VDict,*/ K, V> : global::Windows.Foundation.Collections.IMap<K, V>
    {
        private readonly IDictionary</*KDict, VDict*/ K, V> _dictionary;

        public DictionaryToMap(IDictionary</*KDict, VDict*/ K, V> dictionary) => _dictionary = dictionary;

        public global::Windows.Foundation.Collections.IIterator<global::Windows.Foundation.Collections.IKeyValuePair<K, V>> First() =>
            new EnumeratorToIterator<IKeyValuePair<K, V>>(
                new KeyValuePairEnumerator<K, V>(_dictionary.GetEnumerator()));

        public V Lookup(K key)
        {
            V value;
            bool keyFound = _dictionary.TryGetValue(key, out value);

            if (!keyFound)
            {
                Debug.Assert(key != null);
                Exception e = new KeyNotFoundException(ErrorStrings.Format(ErrorStrings.Arg_KeyNotFoundWithKey, key.ToString()));
                e.SetHResult(ExceptionHelpers.E_BOUNDS);
                throw e;
            }

            return value;
        }

        public uint Size { get => (uint)_dictionary.Count; }

        public bool HasKey(K key) => _dictionary.ContainsKey(key);

        public global::Windows.Foundation.Collections.IMapView<K, V> GetView()
        {
            // Note: This dictionary is not really read-only - you could QI for a modifiable
            // dictionary.  We gain some perf by doing this.  We believe this is acceptable.
            if (!(_dictionary is IReadOnlyDictionary<K, V> roDictionary))
            {
                roDictionary = new ReadOnlyDictionary<K, V>(_dictionary);
            }
            return new ReadOnlyDictionaryToMapView<K, V>(roDictionary);
        }

        public bool Insert(K key, V value)
        {
            bool replacing = _dictionary.ContainsKey(key);
            _dictionary[key] = value;
            return replacing;
        }

        public void _Remove(K key)
        {
            bool removed = _dictionary.Remove(key);

            if (!removed)
            {
                Debug.Assert(key != null);
                Exception e = new KeyNotFoundException(ErrorStrings.Format(ErrorStrings.Arg_KeyNotFoundWithKey, key.ToString()));
                e.SetHResult(ExceptionHelpers.E_BOUNDS);
                throw e;
            }
        }

        public void Clear() => _dictionary.Clear();
    }

    public class MapViewToReadOnlyDictionary<K, V> : IReadOnlyDictionary<K, V>
    {
        private readonly global::ABI.System.Collections.Generic.IReadOnlyDictionary<K, V> _mapView;
        private readonly global::ABI.System.Collections.Generic.IEnumerable<KeyValuePair<K, V>> _enumerable;

        public MapViewToReadOnlyDictionary(IObjectReference obj) :
            this(new global::ABI.System.Collections.Generic.IReadOnlyDictionary<K, V>(obj))
        {
        }

        public MapViewToReadOnlyDictionary(global::ABI.System.Collections.Generic.IReadOnlyDictionary<K, V> mapView)
        {
            _mapView = mapView;
            _enumerable = new ABI.System.Collections.Generic.IEnumerable<KeyValuePair<K, V>>(mapView.ObjRef);
        }

        public int Count
        {
            get
            {
                uint size = _mapView.Size;

                if (((uint)int.MaxValue) < size)
                {
                    throw new InvalidOperationException(ErrorStrings.InvalidOperation_CollectionBackingDictionaryTooLarge);
                }

                return (int)size;

                //if (this is global::Windows.Foundation.Collections.IMapView<K, V> _this_map)
                //{
                //    uint size = _this_map.Size;

                //    if (((uint)int.MaxValue) < size)
                //    {
                //        throw new InvalidOperationException(ErrorStrings.InvalidOperation_CollectionBackingDictionaryTooLarge);
                //    }

                //    return (int)size;
                //}
                //else
                //{
                //    var _this_vector = (global::Windows.Foundation.Collections.IVectorView<KeyValuePair<K, V>>)this;
                //    uint size = _this_vector.Size;

                //    if (((uint)int.MaxValue) < size)
                //    {
                //        throw new InvalidOperationException(ErrorStrings.InvalidOperation_CollectionBackingListTooLarge);
                //    }

                //    return (int)size;
                //}
            }
        }

        public V this[K key] { get => Indexer_Get(key); }

        private V Indexer_Get(K key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            return Lookup(_mapView, key);
        }

        public IEnumerable<K> Keys { get => new ReadOnlyDictionaryKeyCollection(this); }

        public IEnumerable<V> Values { get => new ReadOnlyDictionaryValueCollection(this); }

        public bool ContainsKey(K key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            return _mapView.HasKey(key);
        }

        public bool TryGetValue(K key, out V value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            // It may be faster to call HasKey then Lookup.  On failure, we would otherwise
            // throw an exception from Lookup.
            if (!_mapView.HasKey(key))
            {
                value = default!;
                return false;
            }

            try
            {
                value = _mapView.Lookup(key);
                return true;
            }
            catch (Exception ex)  // Still may hit this case due to a race condition
            {
                if (ExceptionHelpers.E_BOUNDS == ex.HResult)
                {
                    value = default!;
                    return false;
                }
                throw;
            }
        }

        private static V Lookup(global::Windows.Foundation.Collections.IMapView<K, V> _this, K key)
        {
            try
            {
                return _this.Lookup(key);
            }
            catch (Exception ex)
            {
                if (ExceptionHelpers.E_BOUNDS == ex.HResult)
                    throw new KeyNotFoundException(ErrorStrings.Format(ErrorStrings.Arg_KeyNotFoundWithKey, key.ToString()));
                throw;
            }
        }

        public IEnumerator<KeyValuePair<K, V>> GetEnumerator() => _enumerable.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private sealed class ReadOnlyDictionaryKeyCollection : IEnumerable<K>
        {
            private readonly IReadOnlyDictionary<K, V> dictionary;

            public ReadOnlyDictionaryKeyCollection(IReadOnlyDictionary<K, V> dictionary)
            {
                if (dictionary == null)
                    throw new ArgumentNullException(nameof(dictionary));

                this.dictionary = dictionary;
            }

            public IEnumerator<K> GetEnumerator()
            {
                return new ReadOnlyDictionaryKeyEnumerator(dictionary);
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class ReadOnlyDictionaryKeyEnumerator : IEnumerator<K>
            {
                private readonly IReadOnlyDictionary<K, V> dictionary;
                private IEnumerator<KeyValuePair<K, V>> enumeration;

                public ReadOnlyDictionaryKeyEnumerator(IReadOnlyDictionary<K, V> dictionary)
                {
                    if (dictionary == null)
                        throw new ArgumentNullException(nameof(dictionary));

                    this.dictionary = dictionary;
                    enumeration = dictionary.GetEnumerator();
                }

                void IDisposable.Dispose()
                {
                    enumeration.Dispose();
                }

                public bool MoveNext()
                {
                    return enumeration.MoveNext();
                }

                object? IEnumerator.Current => ((IEnumerator<K>)this).Current;

                public K Current => enumeration.Current.Key;

                public void Reset()
                {
                    enumeration = dictionary.GetEnumerator();
                }
            }
        }

        private sealed class ReadOnlyDictionaryValueCollection : IEnumerable<V>
        {
            private readonly IReadOnlyDictionary<K, V> dictionary;

            public ReadOnlyDictionaryValueCollection(IReadOnlyDictionary<K, V> dictionary)
            {
                if (dictionary == null)
                    throw new ArgumentNullException(nameof(dictionary));

                this.dictionary = dictionary;
            }

            public IEnumerator<V> GetEnumerator()
            {
                return new ReadOnlyDictionaryValueEnumerator(dictionary);
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class ReadOnlyDictionaryValueEnumerator : IEnumerator<V>
            {
                private readonly IReadOnlyDictionary<K, V> dictionary;
                private IEnumerator<KeyValuePair<K, V>> enumeration;

                public ReadOnlyDictionaryValueEnumerator(IReadOnlyDictionary<K, V> dictionary)
                {
                    if (dictionary == null)
                        throw new ArgumentNullException(nameof(dictionary));

                    this.dictionary = dictionary;
                    enumeration = dictionary.GetEnumerator();
                }

                void IDisposable.Dispose()
                {
                    enumeration.Dispose();
                }

                public bool MoveNext()
                {
                    return enumeration.MoveNext();
                }

                object? IEnumerator.Current => ((IEnumerator<V>)this).Current;

                public V Current => enumeration.Current.Value;

                public void Reset()
                {
                    enumeration = dictionary.GetEnumerator();
                }
            }
        }
    }

    public class ReadOnlyDictionaryToMapView<K, V> : global::Windows.Foundation.Collections.IMapView<K, V>
    {
        private readonly IReadOnlyDictionary<K, V> _dictionary;

        internal ReadOnlyDictionaryToMapView(IReadOnlyDictionary<K, V> dictionary) => _dictionary = dictionary;

        uint global::Windows.Foundation.Collections.IMapView<K, V>.Size { get => (uint)_dictionary.Count; }

        public global::Windows.Foundation.Collections.IIterator<global::Windows.Foundation.Collections.IKeyValuePair<K, V>> First() =>
            new EnumeratorToIterator<IKeyValuePair<K, V>>(
                new KeyValuePairEnumerator<K, V>(_dictionary.GetEnumerator()));

        public V Lookup(K key)
        {
            V value;
            bool keyFound = _dictionary.TryGetValue(key, out value);

            if (!keyFound)
            {
                Exception e = new KeyNotFoundException(ErrorStrings.Format(ErrorStrings.Arg_KeyNotFoundWithKey, key.ToString()));
                e.SetHResult(ExceptionHelpers.E_BOUNDS);
                throw e;
            }

            return value;
        }

        public uint Size() => (uint)_dictionary.Count;

        public bool HasKey(K key) => _dictionary.ContainsKey(key);

        public void Split(out global::Windows.Foundation.Collections.IMapView<K, V>? first, out global::Windows.Foundation.Collections.IMapView<K, V>? second)
        {
            if (_dictionary.Count < 2)
            {
                first = null;
                second = null;
                return;
            }

            if (!(_dictionary is ConstantSplittableMap splittableMap))
                splittableMap = new ConstantSplittableMap(_dictionary);

            splittableMap.Split(out first, out second);
        }

        private sealed class ConstantSplittableMap : global::Windows.Foundation.Collections.IMapView<K, V>
        {
            private class KeyValuePairComparator : IComparer<KeyValuePair<K, V>>
            {
                private static readonly IComparer<K> keyComparator = Comparer<K>.Default;

                public int Compare(KeyValuePair<K, V> x, KeyValuePair<K, V> y)
                {
                    return keyComparator.Compare(x.Key, y.Key);
                }
            }

            private static readonly KeyValuePairComparator keyValuePairComparator = new KeyValuePairComparator();

            private readonly KeyValuePair<K, V>[] items;
            private readonly int firstItemIndex;
            private readonly int lastItemIndex;

            internal ConstantSplittableMap(IReadOnlyDictionary<K, V> data)
            {
                if (data == null)
                    throw new ArgumentNullException(nameof(data));

                firstItemIndex = 0;
                lastItemIndex = data.Count - 1;
                items = CreateKeyValueArray(data.Count, data.GetEnumerator());
            }

            private ConstantSplittableMap(KeyValuePair<K, V>[] items, int firstItemIndex, int lastItemIndex)
            {
                this.items = items;
                this.firstItemIndex = firstItemIndex;
                this.lastItemIndex = lastItemIndex;
            }

            private KeyValuePair<K, V>[] CreateKeyValueArray(int count, IEnumerator<KeyValuePair<K, V>> data)
            {
                KeyValuePair<K, V>[] kvArray = new KeyValuePair<K, V>[count];

                int i = 0;
                while (data.MoveNext())
                    kvArray[i++] = data.Current;

                Array.Sort(kvArray, keyValuePairComparator);

                return kvArray;
            }

            public int Count => lastItemIndex - firstItemIndex + 1;

            public uint Size => (uint)(lastItemIndex - firstItemIndex + 1);

            public V Lookup(K key)
            {
                V value;
                bool found = TryGetValue(key, out value);

                if (!found)
                {
                    Exception e = new KeyNotFoundException(ErrorStrings.Format(ErrorStrings.Arg_KeyNotFoundWithKey, key.ToString()));
                    e.SetHResult(ExceptionHelpers.E_BOUNDS);
                    throw e;
                }

                return value;
            }

            public bool HasKey(K key) =>
                TryGetValue(key, out _);

            public global::Windows.Foundation.Collections.IIterator<global::Windows.Foundation.Collections.IKeyValuePair<K, V>> First() =>
                new EnumeratorToIterator<global::Windows.Foundation.Collections.IKeyValuePair<K, V>>(GetEnumerator());

            private IEnumerator<global::Windows.Foundation.Collections.IKeyValuePair<K, V>> GetEnumerator() =>
                new IKeyValuePairEnumerator(items, firstItemIndex, lastItemIndex);

            public void Split(out global::Windows.Foundation.Collections.IMapView<K, V>? firstPartition, out global::Windows.Foundation.Collections.IMapView<K, V>? secondPartition)
            {
                if (Count < 2)
                {
                    firstPartition = null;
                    secondPartition = null;
                    return;
                }

                int pivot = (int)(((long)firstItemIndex + (long)lastItemIndex) / (long)2);

                firstPartition = new ConstantSplittableMap(items, firstItemIndex, pivot);
                secondPartition = new ConstantSplittableMap(items, pivot + 1, lastItemIndex);
            }

            public bool TryGetValue(K key, out V value)
            {
                KeyValuePair<K, V> searchKey = new KeyValuePair<K, V>(key, default!);
                int index = Array.BinarySearch(items, firstItemIndex, Count, searchKey, keyValuePairComparator);

                if (index < 0)
                {
                    value = default!;
                    return false;
                }

                value = items[index].Value;
                return true;
            }

            internal struct IKeyValuePairEnumerator : IEnumerator<global::Windows.Foundation.Collections.IKeyValuePair<K, V>>
            {
                private readonly KeyValuePair<K, V>[] _array;
                private readonly int _start;
                private readonly int _end;
                private int _current;

                internal IKeyValuePairEnumerator(KeyValuePair<K, V>[] items, int first, int end)
                {
                    _array = items;
                    _start = first;
                    _end = end;
                    _current = _start - 1;
                }

                public bool MoveNext()
                {
                    if (_current < _end)
                    {
                        _current++;
                        return true;
                    }
                    return false;
                }

                public global::Windows.Foundation.Collections.IKeyValuePair<K, V> Current
                {
                    get
                    {
                        if (_current < _start) throw new InvalidOperationException(ErrorStrings.InvalidOperation_EnumNotStarted);
                        if (_current > _end) throw new InvalidOperationException(ErrorStrings.InvalidOperation_EnumEnded);
                        return new KeyValuePairToIKeyValuePair<K, V>(ref _array[_current]);
                    }
                }

                object? IEnumerator.Current => Current;

                void IEnumerator.Reset() =>
                    _current = _start - 1;

                public void Dispose()
                {
                }
            }
        }
    }
}
#nullable disable


namespace Windows.Foundation.Collections
{
    [Guid("FAA585EA-6214-4217-AFDA-7F46DE5869B3")]
    public interface IIterable<T>
    {
        IIterator<T> First();
    }
    [Guid("6A79E863-4300-459A-9966-CBB660963EE1")]
    public interface IIterator<T>
    {
        bool _MoveNext();
        uint GetMany(ref T[] items);
        T _Current { get; }
        bool HasCurrent { get; }
    }
    [Guid("E480CE40-A338-4ADA-ADCF-272272E48CB9")]
    public interface IMapView<K, V> : IIterable<IKeyValuePair<K, V>>
    {
        V Lookup(K key);
        bool HasKey(K key);
        void Split(out IMapView<K, V> first, out IMapView<K, V> second);
        uint Size { get; }
    }
    [Guid("3C2925FE-8519-45C1-AA79-197B6718C1C1")]
    public interface IMap<K, V> : IIterable<IKeyValuePair<K, V>>
    {
        V Lookup(K key);
        bool HasKey(K key);
        IMapView<K, V> GetView();
        bool Insert(K key, V value);
        void _Remove(K key);
        void Clear();
        uint Size { get; }
    }
    [Guid("BBE1FA4C-B0E3-4583-BAEF-1F1B2E483E56")]
    public interface IVectorView<T> : IIterable<T>
    {
        T GetAt(uint index);
        bool IndexOf(T value, out uint index);
        uint GetMany(uint startIndex, ref T[] items);
        uint Size { get; }
    }
    [Guid("913337E9-11A1-4345-A3A2-4E7F956E222D")]
    public interface IVector<T> : IIterable<T>
    {
        T GetAt(uint index);
        IVectorView<T> GetView();
        bool IndexOf(T value, out uint index);
        void SetAt(uint index, T value);
        void InsertAt(uint index, T value);
        void RemoveAt(uint index);
        void Append(T value);
        void RemoveAtEnd();
        void _Clear();
        uint GetMany(uint startIndex, ref T[] items);
        void ReplaceAll(T[] items);
        uint Size { get; }
    }
}

//namespace ABI.Windows.Foundation.Collections
namespace ABI.System.Collections.Generic
{
    using ABI.Windows.Foundation.Collections.Adapters;
    using global::System;

    [Guid("FAA585EA-6214-4217-AFDA-7F46DE5869B3")]
    public class IEnumerable<T> : global::System.Collections.Generic.IEnumerable<T>, global::Windows.Foundation.Collections.IIterable<T>
    {
        public static IObjectReference CreateMarshaler(global::System.Collections.Generic.IEnumerable<T> obj)
        {
            return MarshalInterface<global::Windows.Foundation.Collections.IIterable<T>>.CreateMarshaler(new EnumerableToIterable<T>(obj));
        }
        
        public static IntPtr GetAbi(IObjectReference objRef) =>
            objRef?.ThisPtr ?? IntPtr.Zero;
        
        public static global::System.Collections.Generic.IEnumerable<T> FromAbi(IntPtr thisPtr) =>
            new IEnumerable<T>(ObjRefFromAbi(thisPtr));

        public static IntPtr FromManaged(global::System.Collections.Generic.IEnumerable<T> value)
        {
            if (value is null)
            {
                return IntPtr.Zero;
            }
            return CreateMarshaler(value).GetRef();
        }

        public static void DisposeMarshaler(IObjectReference objRef) => objRef?.Dispose();

        public static void DisposeAbi(IntPtr abi) =>
            MarshalInterfaceHelper<global::Windows.Foundation.Collections.IIterable<T>>.DisposeAbi(abi);

        public static string GetGuidSignature() => GuidGenerator.GetSignature(typeof(IEnumerable<T>));

        [Guid("FAA585EA-6214-4217-AFDA-7F46DE5869B3")]
        public struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            public IIterable_Delegates.First_0 First_0;
            public static Guid PIID = GuidGenerator.CreateIID(typeof(IEnumerable<T>));

            internal unsafe Vftbl(IntPtr thisPtr)
            {
                var vftblPtr = Marshal.PtrToStructure<VftblPtr>(thisPtr);
                var vftbl = (IntPtr*)vftblPtr.Vftbl;
                IInspectableVftbl = Marshal.PtrToStructure<IInspectable.Vftbl>(vftblPtr.Vftbl);
                First_0 = Marshal.GetDelegateForFunctionPointer<IIterable_Delegates.First_0>(vftbl[6]);
            }

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;
            static unsafe Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,
                    First_0 = Do_Abi_First_0
                };
                var nativeVftbl = (IntPtr*)Marshal.AllocCoTaskMem(Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 1);
                Marshal.StructureToPtr(AbiToProjectionVftable.IInspectableVftbl, (IntPtr)nativeVftbl, false);
                nativeVftbl[6] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.First_0);

                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

            private static unsafe int Do_Abi_First_0(IntPtr thisPtr, out IntPtr __return_value__)
            {
                global::Windows.Foundation.Collections.IIterator<T> ____return_value__ = default;

                __return_value__ = default;

                try
                {
                    ____return_value__ = global::WinRT.ComWrappersSupport.FindObject<global::Windows.Foundation.Collections.IIterable<T>>(thisPtr).First();
                    __return_value__ = MarshalInterface<global::Windows.Foundation.Collections.IIterator<T>>.FromManaged(____return_value__);
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
        }

        public static ObjectReference<Vftbl> ObjRefFromAbi(IntPtr thisPtr)
        {
            if (thisPtr == IntPtr.Zero)
            {
                return null;
            }
            var vftblT = new Vftbl(thisPtr);
            return ObjectReference<Vftbl>.FromAbi(thisPtr, vftblT.IInspectableVftbl.IUnknownVftbl, vftblT);
        }
        public static Guid PIID = Vftbl.PIID;

        public static implicit operator IEnumerable<T>(IObjectReference obj) => (obj != null) ? new IEnumerable<T>(obj) : null;
        public static implicit operator IEnumerable<T>(ObjectReference<Vftbl> obj) => (obj != null) ? new IEnumerable<T>(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IObjectReference ObjRef { get => _obj; }

        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public IEnumerable(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        public IEnumerable(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
            _iterableToEnumerable = new IterableToEnumerable<T>(this);
        }
        IterableToEnumerable<T> _iterableToEnumerable;

        public unsafe global::Windows.Foundation.Collections.IIterator<T> First()
        {
            IntPtr __retval = default;
            try
            {
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.First_0(ThisPtr, out __retval));
                return MarshalInterface<global::Windows.Foundation.Collections.IIterator<T>>.FromAbi(__retval);
            }
            finally
            {
                MarshalInterface<global::Windows.Foundation.Collections.IIterator<T>>.DisposeAbi(__retval);
            }
        }

        public global::System.Collections.Generic.IEnumerator<T> GetEnumerator() => _iterableToEnumerable.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    public static class IIterable_Delegates
    {
        public unsafe delegate int First_0(IntPtr thisPtr, out IntPtr __return_value__);
    }

    [Guid("6A79E863-4300-459A-9966-CBB660963EE1")]
    public class IEnumerator<T> : global::System.Collections.Generic.IEnumerator<T>, global::Windows.Foundation.Collections.IIterator<T>
    {
        public static IObjectReference CreateMarshaler(global::System.Collections.Generic.IEnumerator<T> obj) =>
            MarshalInterface<global::Windows.Foundation.Collections.IIterator<T>>.CreateMarshaler(new EnumeratorToIterator<T>(obj));

        public static IntPtr GetAbi(IObjectReference objRef) =>
            objRef?.ThisPtr ?? IntPtr.Zero;

        public static global::System.Collections.Generic.IEnumerator<T> FromAbi(IntPtr thisPtr) =>
            new IEnumerator<T>(ObjRefFromAbi(thisPtr));

        public static IntPtr FromManaged(global::System.Collections.Generic.IEnumerator<T> value)
        {
            if (value is null)
            {
                return IntPtr.Zero;
            }
            return CreateMarshaler(value).GetRef();
        }

        public static void DisposeMarshaler(IObjectReference objRef) => objRef?.Dispose();

        public static void DisposeAbi(IntPtr abi) =>
            MarshalInterfaceHelper<global::Windows.Foundation.Collections.IIterator<T>>.DisposeAbi(abi);

        public static string GetGuidSignature() => GuidGenerator.GetSignature(typeof(IEnumerator<T>));

        [Guid("6A79E863-4300-459A-9966-CBB660963EE1")]
        public struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            public global::System.Delegate get_Current_0;
            internal _get_PropertyAsBoolean get_HasCurrent_1;
            public IIterator_Delegates.MoveNext_2 MoveNext_2;
            public IIterator_Delegates.GetMany_3 GetMany_3;
            public static Guid PIID = GuidGenerator.CreateIID(typeof(IEnumerator<T>));
            private static readonly Type get_Current_0_Type = Expression.GetDelegateType(new Type[] { typeof(void*), Marshaler<T>.AbiType.MakeByRefType(), typeof(int) });

            internal unsafe Vftbl(IntPtr thisPtr)
            {
                var vftblPtr = Marshal.PtrToStructure<VftblPtr>(thisPtr);
                var vftbl = (IntPtr*)vftblPtr.Vftbl;
                IInspectableVftbl = Marshal.PtrToStructure<IInspectable.Vftbl>(vftblPtr.Vftbl);
                get_Current_0 = Marshal.GetDelegateForFunctionPointer(vftbl[6], get_Current_0_Type);
                get_HasCurrent_1 = Marshal.GetDelegateForFunctionPointer<_get_PropertyAsBoolean>(vftbl[7]);
                MoveNext_2 = Marshal.GetDelegateForFunctionPointer<IIterator_Delegates.MoveNext_2>(vftbl[8]);
                GetMany_3 = Marshal.GetDelegateForFunctionPointer<IIterator_Delegates.GetMany_3>(vftbl[9]);
            }

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;
            static unsafe Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,
                    get_Current_0 = global::System.Delegate.CreateDelegate(get_Current_0_Type, typeof(Vftbl).GetMethod("Do_Abi_get_Current_0", BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(Marshaler<T>.AbiType)),
                    get_HasCurrent_1 = Do_Abi_get_HasCurrent_1,
                    MoveNext_2 = Do_Abi_MoveNext_2,
                    GetMany_3 = Do_Abi_GetMany_3
                };
                var nativeVftbl = (IntPtr*)Marshal.AllocCoTaskMem(Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 4);
                Marshal.StructureToPtr(AbiToProjectionVftable.IInspectableVftbl, (IntPtr)nativeVftbl, false);
                nativeVftbl[6] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.get_Current_0);
                nativeVftbl[7] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.get_HasCurrent_1);
                nativeVftbl[8] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.MoveNext_2);
                nativeVftbl[9] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.GetMany_3);

                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

            private static unsafe int Do_Abi_MoveNext_2(IntPtr thisPtr, out byte __return_value__)
            {
                bool ____return_value__ = default;

                __return_value__ = default;

                try
                {
                    ____return_value__ = global::WinRT.ComWrappersSupport.FindObject<global::Windows.Foundation.Collections.IIterator<T>>(thisPtr)._MoveNext();
                    __return_value__ = (byte)(____return_value__ ? 1 : 0);

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_GetMany_3(IntPtr thisPtr, int __itemsSize, IntPtr items, out uint __return_value__)
            {
                uint ____return_value__ = default;

                __return_value__ = default;
                T[] __items = Marshaler<T>.FromAbiArray((__itemsSize, items));

                try
                {
                    ____return_value__ = global::WinRT.ComWrappersSupport.FindObject<global::Windows.Foundation.Collections.IIterator<T>>(thisPtr).GetMany(ref __items);
                    Marshaler<T>.CopyManagedArray(__items, items);
                    __return_value__ = ____return_value__;

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_get_Current_0<TAbi>(void* thisPtr, out TAbi __return_value__)
            {
                T ____return_value__ = default;

                __return_value__ = default;

                try
                {
                    ____return_value__ = global::WinRT.ComWrappersSupport.FindObject<global::Windows.Foundation.Collections.IIterator<T>>(new IntPtr(thisPtr))._Current;
                    __return_value__ = (TAbi)Marshaler<T>.FromManaged(____return_value__);
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_get_HasCurrent_1(IntPtr thisPtr, out byte __return_value__)
            {
                bool ____return_value__ = default;

                __return_value__ = default;

                try
                {
                    ____return_value__ = global::WinRT.ComWrappersSupport.FindObject<global::Windows.Foundation.Collections.IIterator<T>>(thisPtr).HasCurrent;
                    __return_value__ = (byte)(____return_value__ ? 1 : 0);
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
        }
        public static ObjectReference<Vftbl> ObjRefFromAbi(IntPtr thisPtr)
        {
            if (thisPtr == IntPtr.Zero)
            {
                return null;
            }
            var vftblT = new Vftbl(thisPtr);
            return ObjectReference<Vftbl>.FromAbi(thisPtr, vftblT.IInspectableVftbl.IUnknownVftbl, vftblT);
        }
        public static Guid PIID = Vftbl.PIID;

        public static implicit operator IEnumerator<T>(IObjectReference obj) => (obj != null) ? new IEnumerator<T>(obj) : null;
        public static implicit operator IEnumerator<T>(ObjectReference<Vftbl> obj) => (obj != null) ? new IEnumerator<T>(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IObjectReference ObjRef { get => _obj; }

        public IntPtr ThisPtr => _obj.ThisPtr;

        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();

        public IEnumerator(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        public IEnumerator(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
            _iteratorToEnumerator = new IteratorToEnumerator<T>(this);
        }
        IteratorToEnumerator<T> _iteratorToEnumerator;

        public unsafe bool _MoveNext()
        {
            byte __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.MoveNext_2(ThisPtr, out __retval));
            return __retval != 0;
        }

        public unsafe uint GetMany(ref T[] items)
        {
            object __items = default;
            int __items_length = default;
            IntPtr __items_data = default;
            uint __retval = default;
            try
            {
                __items = Marshaler<T>.CreateMarshalerArray(items);
                (__items_length, __items_data) = Marshaler<T>.GetAbiArray(__items);
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetMany_3(ThisPtr, __items_length, __items_data, out __retval));
                items = Marshaler<T>.FromAbiArray((__items_length, __items_data));
                return __retval;
            }
            finally
            {
                Marshaler<T>.DisposeMarshalerArray(__items);
            }
        }

        public unsafe T _Current
        {
            get
            {
                var __params = new object[] { ThisPtr, null };
                try
                {
                    _obj.Vftbl.get_Current_0.DynamicInvokeAbi(__params);
                    return Marshaler<T>.FromAbi(__params[1]);
                }
                finally
                {
                    Marshaler<T>.DisposeAbi(__params[1]);
                }
            }
        }

        public unsafe bool HasCurrent
        {
            get
            {
                byte __retval = default;
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.get_HasCurrent_1(ThisPtr, out __retval));
                return __retval != 0;
            }
        }

        public bool MoveNext() => _iteratorToEnumerator.MoveNext();
        public void Reset() => _iteratorToEnumerator.Reset();
        public void Dispose() => _iteratorToEnumerator.Dispose();
        public T Current => _iteratorToEnumerator.Current;
        object IEnumerator.Current => Current;
    }
    public static class IIterator_Delegates
    {
        public unsafe delegate int MoveNext_2(IntPtr thisPtr, out byte __return_value__);
        public unsafe delegate int GetMany_3(IntPtr thisPtr, int __itemsSize, IntPtr items, out uint __return_value__);
    }

    [Guid("E480CE40-A338-4ADA-ADCF-272272E48CB9")]
    public class IReadOnlyDictionary<K, V> : global::System.Collections.Generic.IReadOnlyDictionary<K, V>, global::Windows.Foundation.Collections.IMapView<K, V>
    {
        public static IObjectReference CreateMarshaler(global::System.Collections.Generic.IReadOnlyDictionary<K, V> obj)
        {
            return MarshalInterface<global::Windows.Foundation.Collections.IMapView<K, V>>.CreateMarshaler(new ReadOnlyDictionaryToMapView<K, V>(obj));
        }

        public static IntPtr GetAbi(IObjectReference objRef) =>
            objRef?.ThisPtr ?? IntPtr.Zero;
        
        public static global::System.Collections.Generic.IReadOnlyDictionary<K, V> FromAbi(IntPtr thisPtr) =>
            new IReadOnlyDictionary<K, V>(ObjRefFromAbi(thisPtr));

        public static IntPtr FromManaged(global::System.Collections.Generic.IReadOnlyDictionary<K, V> value)
        {
            if (value is null)
            {
                return IntPtr.Zero;
            }
            return CreateMarshaler(value).GetRef();
        }

        public static void DisposeMarshaler(IObjectReference objRef) => objRef?.Dispose();

        public static void DisposeAbi(IntPtr abi) =>
            MarshalInterfaceHelper<global::Windows.Foundation.Collections.IMapView<K, V>>.DisposeAbi(abi);

        public static string GetGuidSignature() => GuidGenerator.GetSignature(typeof(IReadOnlyDictionary<K, V>));

        [Guid("E480CE40-A338-4ADA-ADCF-272272E48CB9")]
        public struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            public global::System.Delegate Lookup_0;
            internal _get_PropertyAsUInt32 get_Size_1;
            public global::System.Delegate HasKey_2;
            public IMapView_Delegates.Split_3 Split_3;
            public static Guid PIID = GuidGenerator.CreateIID(typeof(IReadOnlyDictionary<K, V>));
            private static readonly Type Lookup_0_Type = Expression.GetDelegateType(new Type[] { typeof(void*), Marshaler<K>.AbiType, Marshaler<V>.AbiType.MakeByRefType(), typeof(int) });
            private static readonly Type HasKey_2_Type = Expression.GetDelegateType(new Type[] { typeof(void*), Marshaler<K>.AbiType, typeof(byte).MakeByRefType(), typeof(int) });

            internal unsafe Vftbl(IntPtr thisPtr)
            {
                var vftblPtr = Marshal.PtrToStructure<VftblPtr>(thisPtr);
                var vftbl = (IntPtr*)vftblPtr.Vftbl;
                IInspectableVftbl = Marshal.PtrToStructure<IInspectable.Vftbl>(vftblPtr.Vftbl);
                Lookup_0 = Marshal.GetDelegateForFunctionPointer(vftbl[6], Lookup_0_Type);
                get_Size_1 = Marshal.GetDelegateForFunctionPointer<_get_PropertyAsUInt32>(vftbl[7]);
                HasKey_2 = Marshal.GetDelegateForFunctionPointer(vftbl[8], HasKey_2_Type);
                Split_3 = Marshal.GetDelegateForFunctionPointer<IMapView_Delegates.Split_3>(vftbl[9]);
            }

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;
            static unsafe Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,
                    Lookup_0 = global::System.Delegate.CreateDelegate(Lookup_0_Type, typeof(Vftbl).GetMethod("Do_Abi_Lookup_0", BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(Marshaler<K>.AbiType, Marshaler<V>.AbiType)),
                    get_Size_1 = Do_Abi_get_Size_1,
                    HasKey_2 = global::System.Delegate.CreateDelegate(HasKey_2_Type, typeof(Vftbl).GetMethod("Do_Abi_HasKey_2", BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(Marshaler<K>.AbiType)),
                    Split_3 = Do_Abi_Split_3
                };
                var nativeVftbl = (IntPtr*)Marshal.AllocCoTaskMem(Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 4);
                Marshal.StructureToPtr(AbiToProjectionVftable.IInspectableVftbl, (IntPtr)nativeVftbl, false);
                nativeVftbl[6] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.Lookup_0);
                nativeVftbl[7] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.get_Size_1);
                nativeVftbl[8] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.HasKey_2);
                nativeVftbl[9] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.Split_3);

                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

            private static unsafe int Do_Abi_Lookup_0<KAbi, VAbi>(void* thisPtr, KAbi key, out VAbi __return_value__)
            {
                V ____return_value__ = default;

                __return_value__ = default;

                try
                {
                    ____return_value__ = global::WinRT.ComWrappersSupport.FindObject<global::Windows.Foundation.Collections.IMapView<K, V>>(new IntPtr(thisPtr)).Lookup(Marshaler<K>.FromAbi(key));
                    __return_value__ = (VAbi)Marshaler<V>.FromManaged(____return_value__);
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_HasKey_2<KAbi>(void* thisPtr, KAbi key, out byte __return_value__)
            {
                bool ____return_value__ = default;

                __return_value__ = default;

                try
                {
                    ____return_value__ = global::WinRT.ComWrappersSupport.FindObject<global::Windows.Foundation.Collections.IMapView<K, V>>(new IntPtr(thisPtr)).HasKey(Marshaler<K>.FromAbi(key));
                    __return_value__ = (byte)(____return_value__ ? 1 : 0);
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_Split_3(IntPtr thisPtr, out IntPtr first, out IntPtr second)
            {

                first = default;
                second = default;
                global::Windows.Foundation.Collections.IMapView<K, V> __first = default;
                global::Windows.Foundation.Collections.IMapView<K, V> __second = default;

                try
                {
                    global::WinRT.ComWrappersSupport.FindObject<global::Windows.Foundation.Collections.IMapView<K, V>>(thisPtr).Split(out __first, out __second);
                    first = MarshalInterface<global::Windows.Foundation.Collections.IMapView<K, V>>.FromManaged(__first);
                    second = MarshalInterface<global::Windows.Foundation.Collections.IMapView<K, V>>.FromManaged(__second);
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_get_Size_1(IntPtr thisPtr, out uint __return_value__)
            {
                uint ____return_value__ = default;

                __return_value__ = default;

                try
                {
                    ____return_value__ = global::WinRT.ComWrappersSupport.FindObject<global::Windows.Foundation.Collections.IMapView<K, V>>(thisPtr).Size;
                    __return_value__ = ____return_value__;
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
        }
        public static ObjectReference<Vftbl> ObjRefFromAbi(IntPtr thisPtr)
        {
            if (thisPtr == IntPtr.Zero)
            {
                return null;
            }
            var vftblT = new Vftbl(thisPtr);
            return ObjectReference<Vftbl>.FromAbi(thisPtr, vftblT.IInspectableVftbl.IUnknownVftbl, vftblT);
        }
        public static Guid PIID = Vftbl.PIID;

        public static implicit operator IReadOnlyDictionary<K, V>(IObjectReference obj) => (obj != null) ? new IReadOnlyDictionary<K, V>(obj) : null;
        public static implicit operator IReadOnlyDictionary<K, V>(ObjectReference<Vftbl> obj) => (obj != null) ? new IReadOnlyDictionary<K, V>(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IObjectReference ObjRef { get => _obj; }

        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public IReadOnlyDictionary(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        public IReadOnlyDictionary(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
            _mapViewToReadOnlyDictionary = new MapViewToReadOnlyDictionary<K, V>(this);
        }
        MapViewToReadOnlyDictionary<K, V> _mapViewToReadOnlyDictionary;

        public unsafe V Lookup(K key)
        {
            object __key = default;
            var __params = new object[] { ThisPtr, null, null };
            try
            {
                __key = Marshaler<K>.CreateMarshaler(key);
                __params[1] = Marshaler<K>.GetAbi(__key);
                _obj.Vftbl.Lookup_0.DynamicInvokeAbi(__params);
                return Marshaler<V>.FromAbi(__params[2]);
            }
            finally
            {
                Marshaler<K>.DisposeMarshaler(__key);
                Marshaler<V>.DisposeAbi(__params[2]);
            }
        }

        public unsafe bool HasKey(K key)
        {
            object __key = default;
            var __params = new object[] { ThisPtr, null, null };
            try
            {
                __key = Marshaler<K>.CreateMarshaler(key);
                __params[1] = Marshaler<K>.GetAbi(__key);
                _obj.Vftbl.HasKey_2.DynamicInvokeAbi(__params);
                return (byte)__params[2] != 0;
            }
            finally
            {
                Marshaler<K>.DisposeMarshaler(__key);
            }
        }

        public unsafe void Split(out global::Windows.Foundation.Collections.IMapView<K, V> first, out global::Windows.Foundation.Collections.IMapView<K, V> second)
        {
            IntPtr __first = default;
            IntPtr __second = default;
            try
            {
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.Split_3(ThisPtr, out __first, out __second));
                first = MarshalInterface<global::Windows.Foundation.Collections.IMapView<K, V>>.FromAbi(__first);
                second = MarshalInterface<global::Windows.Foundation.Collections.IMapView<K, V>>.FromAbi(__second);
            }
            finally
            {
                MarshalInterface<global::Windows.Foundation.Collections.IMapView<K, V>>.DisposeAbi(__first);
                MarshalInterface<global::Windows.Foundation.Collections.IMapView<K, V>>.DisposeAbi(__second);
            }
        }

        public unsafe uint Size
        {
            get
            {
                uint __retval = default;
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.get_Size_1(ThisPtr, out __retval));
                return __retval;
            }
        }

        global::Windows.Foundation.Collections.IIterator<global::Windows.Foundation.Collections.IKeyValuePair<K, V>> global::Windows.Foundation.Collections.IIterable<global::Windows.Foundation.Collections.IKeyValuePair<K, V>>.First() 
            => As<IEnumerable<global::Windows.Foundation.Collections.IKeyValuePair<K, V>>>().First();

        public global::System.Collections.Generic.IEnumerable<K> Keys => _mapViewToReadOnlyDictionary.Keys;
        public global::System.Collections.Generic.IEnumerable<V> Values => _mapViewToReadOnlyDictionary.Values;
        public int Count => _mapViewToReadOnlyDictionary.Count;
        public V this[K key] => _mapViewToReadOnlyDictionary[key];
        public bool ContainsKey(K key) => _mapViewToReadOnlyDictionary.ContainsKey(key);
        public bool TryGetValue(K key, out V value) => _mapViewToReadOnlyDictionary.TryGetValue(key, out value);
        public global::System.Collections.Generic.IEnumerator<global::System.Collections.Generic.KeyValuePair<K, V>> GetEnumerator() => _mapViewToReadOnlyDictionary.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    public static class IMapView_Delegates
    {
        public unsafe delegate int Split_3(IntPtr thisPtr, out IntPtr first, out IntPtr second);
    }

    [Guid("3C2925FE-8519-45C1-AA79-197B6718C1C1")]
    public class IDictionary<K, V> : global::System.Collections.Generic.IDictionary<K, V>, global::Windows.Foundation.Collections.IMap<K, V>
    {
        public static IObjectReference CreateMarshaler(global::System.Collections.Generic.IDictionary<K, V> obj)
        {
            //var (TMapK, _) = ComWrappersSupport.FindTypeByName(typeof(K).Name.AsSpan());
            //var (TMapV, _) = ComWrappersSupport.FindTypeByName(typeof(V).Name.AsSpan());

            //var TDictionary = typeof(global::System.Collections.Generic.IDictionary<K, V>);
            //var TAdapter = typeof(DictionaryToMap<,,,>).MakeGenericType(typeof(K), typeof(V), TMapK, TMapV);
            //var TMarshal = typeof(MarshalInterface<>).MakeGenericType(typeof(global::Windows.Foundation.Collections.IMap<K, V>));
            
            //ParameterExpression[] parms = new[] { Expression.Parameter(TDictionary, "dictionary") };
            //var createMarshaler = 
            //    Expression.Lambda<Func<global::System.Collections.Generic.IDictionary<K, V>, IObjectReference>>(
            //        Expression.Call(TMarshal.GetMethod("CreateMarshaler"), 
            //            Expression.New(TAdapter.GetConstructor(new[] { TDictionary }), parms[0])), parms).Compile();
            
            //return createMarshaler(obj);
            
            return MarshalInterface<global::Windows.Foundation.Collections.IMap<K, V>>.CreateMarshaler(new DictionaryToMap<K, V>(obj));
        }

        public static IntPtr GetAbi(IObjectReference objRef) =>
            objRef?.ThisPtr ?? IntPtr.Zero;

        public static global::System.Collections.Generic.IDictionary<K, V> FromAbi(IntPtr thisPtr) =>
            new IDictionary<K, V>(ObjRefFromAbi(thisPtr));

        public static IntPtr FromManaged(global::System.Collections.Generic.IDictionary<K, V> value)
        {
            if (value is null)
            {
                return IntPtr.Zero;
            }
            return CreateMarshaler(value).GetRef();
        }

        public static void DisposeMarshaler(IObjectReference objRef) => objRef?.Dispose();

        public static void DisposeAbi(IntPtr abi) =>
            MarshalInterfaceHelper<global::Windows.Foundation.Collections.IMap<K, V>>.DisposeAbi(abi);

        public static string GetGuidSignature() => GuidGenerator.GetSignature(typeof(IDictionary<K, V>));

        [Guid("3C2925FE-8519-45C1-AA79-197B6718C1C1")]
        public struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            public global::System.Delegate Lookup_0;
            internal _get_PropertyAsUInt32 get_Size_1;
            public global::System.Delegate HasKey_2;
            public IMap_Delegates.GetView_3 GetView_3;
            public global::System.Delegate Insert_4;
            public global::System.Delegate Remove_5;
            public IMap_Delegates.Clear_6 Clear_6;
            public static Guid PIID = GuidGenerator.CreateIID(typeof(IDictionary<K, V>));
            private static readonly Type Lookup_0_Type = Expression.GetDelegateType(new Type[] { typeof(void*), Marshaler<K>.AbiType, Marshaler<V>.AbiType.MakeByRefType(), typeof(int) });
            private static readonly Type HasKey_2_Type = Expression.GetDelegateType(new Type[] { typeof(void*), Marshaler<K>.AbiType, typeof(byte).MakeByRefType(), typeof(int) });
            private static readonly Type Insert_4_Type = Expression.GetDelegateType(new Type[] { typeof(void*), Marshaler<K>.AbiType, Marshaler<V>.AbiType, typeof(byte).MakeByRefType(), typeof(int) });
            private static readonly Type Remove_5_Type = Expression.GetDelegateType(new Type[] { typeof(void*), Marshaler<K>.AbiType, typeof(int) });

            internal unsafe Vftbl(IntPtr thisPtr)
            {
                var vftblPtr = Marshal.PtrToStructure<VftblPtr>(thisPtr);
                var vftbl = (IntPtr*)vftblPtr.Vftbl;
                IInspectableVftbl = Marshal.PtrToStructure<IInspectable.Vftbl>(vftblPtr.Vftbl);
                Lookup_0 = Marshal.GetDelegateForFunctionPointer(vftbl[6], Lookup_0_Type);
                get_Size_1 = Marshal.GetDelegateForFunctionPointer<_get_PropertyAsUInt32>(vftbl[7]);
                HasKey_2 = Marshal.GetDelegateForFunctionPointer(vftbl[8], HasKey_2_Type);
                GetView_3 = Marshal.GetDelegateForFunctionPointer<IMap_Delegates.GetView_3>(vftbl[9]);
                Insert_4 = Marshal.GetDelegateForFunctionPointer(vftbl[10], Insert_4_Type);
                Remove_5 = Marshal.GetDelegateForFunctionPointer(vftbl[11], Remove_5_Type);
                Clear_6 = Marshal.GetDelegateForFunctionPointer<IMap_Delegates.Clear_6>(vftbl[12]);
            }

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;
            static unsafe Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,
                    Lookup_0 = global::System.Delegate.CreateDelegate(Lookup_0_Type, typeof(Vftbl).GetMethod("Do_Abi_Lookup_0", BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(Marshaler<K>.AbiType, Marshaler<V>.AbiType)),
                    get_Size_1 = Do_Abi_get_Size_1,
                    HasKey_2 = global::System.Delegate.CreateDelegate(HasKey_2_Type, typeof(Vftbl).GetMethod("Do_Abi_HasKey_2", BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(Marshaler<K>.AbiType)),
                    GetView_3 = Do_Abi_GetView_3,
                    Insert_4 = global::System.Delegate.CreateDelegate(Insert_4_Type, typeof(Vftbl).GetMethod("Do_Abi_Insert_4", BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(Marshaler<K>.AbiType, Marshaler<V>.AbiType)),
                    Remove_5 = global::System.Delegate.CreateDelegate(Remove_5_Type, typeof(Vftbl).GetMethod("Do_Abi_Remove_5", BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(Marshaler<K>.AbiType)),
                    Clear_6 = Do_Abi_Clear_6
                };
                var nativeVftbl = (IntPtr*)Marshal.AllocCoTaskMem(Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 7);
                Marshal.StructureToPtr(AbiToProjectionVftable.IInspectableVftbl, (IntPtr)nativeVftbl, false);
                nativeVftbl[6] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.Lookup_0);
                nativeVftbl[7] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.get_Size_1);
                nativeVftbl[8] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.HasKey_2);
                nativeVftbl[9] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.GetView_3);
                nativeVftbl[10] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.Insert_4);
                nativeVftbl[11] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.Remove_5);
                nativeVftbl[12] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.Clear_6);

                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

            private static unsafe int Do_Abi_Lookup_0<KAbi, VAbi>(void* thisPtr, KAbi key, out VAbi __return_value__)
            {
                V ____return_value__ = default;

                __return_value__ = default;

                try
                {
                    ____return_value__ = global::WinRT.ComWrappersSupport.FindObject<global::Windows.Foundation.Collections.IMap<K, V>>(new IntPtr(thisPtr)).Lookup(Marshaler<K>.FromAbi(key));
                    __return_value__ = (VAbi)Marshaler<V>.FromManaged(____return_value__);
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_HasKey_2<KAbi>(void* thisPtr, KAbi key, out byte __return_value__)
            {
                bool ____return_value__ = default;

                __return_value__ = default;

                try
                {
                    ____return_value__ = global::WinRT.ComWrappersSupport.FindObject<global::Windows.Foundation.Collections.IMap<K, V>>(new IntPtr(thisPtr)).HasKey(Marshaler<K>.FromAbi(key)); 
                    __return_value__ = (byte)(____return_value__ ? 1 : 0);
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_GetView_3(IntPtr thisPtr, out IntPtr __return_value__)
            {
                global::Windows.Foundation.Collections.IMapView<K, V> ____return_value__ = default;

                __return_value__ = default;

                try
                {
                    ____return_value__ = global::WinRT.ComWrappersSupport.FindObject<global::Windows.Foundation.Collections.IMap<K, V>>(thisPtr).GetView();
                    __return_value__ = MarshalInterface<global::Windows.Foundation.Collections.IMapView<K, V>>.FromManaged(____return_value__);

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_Insert_4<KAbi, VAbi>(void* thisPtr, KAbi key, VAbi value, out byte __return_value__)
            {
                bool ____return_value__ = default;

                __return_value__ = default;

                try
                {
                    ____return_value__ = global::WinRT.ComWrappersSupport.FindObject<global::Windows.Foundation.Collections.IMap<K, V>>(new IntPtr(thisPtr)).Insert(Marshaler<K>.FromAbi(key), Marshaler<V>.FromAbi(value)); 
                    __return_value__ = (byte)(____return_value__ ? 1 : 0);
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_Remove_5<KAbi>(void* thisPtr, KAbi key)
            {


                try
                {
                    global::WinRT.ComWrappersSupport.FindObject<global::Windows.Foundation.Collections.IMap<K, V>>(new IntPtr(thisPtr))._Remove(Marshaler<K>.FromAbi(key));
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_Clear_6(IntPtr thisPtr)
            {


                try
                {
                    global::WinRT.ComWrappersSupport.FindObject<global::Windows.Foundation.Collections.IMap<K, V>>(thisPtr).Clear();
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_get_Size_1(IntPtr thisPtr, out uint __return_value__)
            {
                uint ____return_value__ = default;

                __return_value__ = default;

                try
                {
                    ____return_value__ = global::WinRT.ComWrappersSupport.FindObject<global::Windows.Foundation.Collections.IMap<K, V>>(thisPtr).Size; __return_value__ = ____return_value__;

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
        }
        public static ObjectReference<Vftbl> ObjRefFromAbi(IntPtr thisPtr)
        {
            if (thisPtr == IntPtr.Zero)
            {
                return null;
            }
            var vftblT = new Vftbl(thisPtr);
            return ObjectReference<Vftbl>.FromAbi(thisPtr, vftblT.IInspectableVftbl.IUnknownVftbl, vftblT);
        }
        public static Guid PIID = Vftbl.PIID;

        public static implicit operator IDictionary<K, V>(IObjectReference obj) => (obj != null) ? new IDictionary<K, V>(obj) : null;
        public static implicit operator IDictionary<K, V>(ObjectReference<Vftbl> obj) => (obj != null) ? new IDictionary<K, V>(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IObjectReference ObjRef { get => _obj; }

        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public IDictionary(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        public IDictionary(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
            _mapToDictionary = new MapToDictionary<K, V>(this);
        }
        MapToDictionary<K, V> _mapToDictionary;

        public unsafe V Lookup(K key)
        {
            object __key = default;
            var __params = new object[] { ThisPtr, null, null };
            try
            {
                __key = Marshaler<K>.CreateMarshaler(key);
                __params[1] = Marshaler<K>.GetAbi(__key);
                _obj.Vftbl.Lookup_0.DynamicInvokeAbi(__params);
                return Marshaler<V>.FromAbi(__params[2]);
            }
            finally
            {
                Marshaler<K>.DisposeMarshaler(__key);
                Marshaler<V>.DisposeAbi(__params[2]);
            }
        }

        public unsafe bool HasKey(K key)
        {
            object __key = default;
            var __params = new object[] { ThisPtr, null, null };
            try
            {
                __key = Marshaler<K>.CreateMarshaler(key);
                __params[1] = Marshaler<K>.GetAbi(__key);
                _obj.Vftbl.HasKey_2.DynamicInvokeAbi(__params);
                return (byte)__params[2] != 0;
            }
            finally
            {
                Marshaler<K>.DisposeMarshaler(__key);
            }
        }

        public unsafe global::Windows.Foundation.Collections.IMapView<K, V> GetView()
        {
            IntPtr __retval = default;
            try
            {
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetView_3(ThisPtr, out __retval));
                return MarshalInterface<global::Windows.Foundation.Collections.IMapView<K, V>>.FromAbi(__retval);
            }
            finally
            {
                MarshalInterface<global::Windows.Foundation.Collections.IMapView<K, V>>.DisposeAbi(__retval);
            }
        }

        public unsafe bool Insert(K key, V value)
        {
            object __key = default;
            object __value = default;
            var __params = new object[] { ThisPtr, null, null, null };
            try
            {
                __key = Marshaler<K>.CreateMarshaler(key);
                __params[1] = Marshaler<K>.GetAbi(__key);
                __value = Marshaler<V>.CreateMarshaler(value);
                __params[2] = Marshaler<V>.GetAbi(__value);
                _obj.Vftbl.Insert_4.DynamicInvokeAbi(__params);
                return (byte)__params[3] != 0;
            }
            finally
            {
                Marshaler<K>.DisposeMarshaler(__key);
                Marshaler<V>.DisposeMarshaler(__value);
            }
        }

        public unsafe void _Remove(K key)
        {
            object __key = default;
            var __params = new object[] { ThisPtr, null };
            try
            {
                __key = Marshaler<K>.CreateMarshaler(key);
                __params[1] = Marshaler<K>.GetAbi(__key);
                _obj.Vftbl.Remove_5.DynamicInvokeAbi(__params);
            }
            finally
            {
                Marshaler<K>.DisposeMarshaler(__key);
            }
        }

        public unsafe void Clear()
        {
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.Clear_6(ThisPtr));
        }

        public unsafe uint Size
        {
            get
            {
                uint __retval = default;
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.get_Size_1(ThisPtr, out __retval));
                return __retval;
            }
        }

        global::Windows.Foundation.Collections.IIterator<global::Windows.Foundation.Collections.IKeyValuePair<K, V>> global::Windows.Foundation.Collections.IIterable<global::Windows.Foundation.Collections.IKeyValuePair<K, V>>.First() 
            => As<IEnumerable<global::Windows.Foundation.Collections.IKeyValuePair<K, V>>>().First();

        public ICollection<K> Keys => _mapToDictionary.Keys;
        public ICollection<V> Values => _mapToDictionary.Values;
        public int Count => _mapToDictionary.Count;
        public bool IsReadOnly => _mapToDictionary.IsReadOnly;
        public V this[K key] { get => _mapToDictionary[key]; set => _mapToDictionary[key] = value; }
        public void Add(K key, V value) => _mapToDictionary.Add(key, value);
        public bool ContainsKey(K key) => _mapToDictionary.ContainsKey(key);
        public bool Remove(K key) => _mapToDictionary.Remove(key);
        public bool TryGetValue(K key, out V value) => _mapToDictionary.TryGetValue(key, out value);
        public void Add(global::System.Collections.Generic.KeyValuePair<K, V> item) => _mapToDictionary.Add(item);
        public bool Contains(global::System.Collections.Generic.KeyValuePair<K, V> item) => _mapToDictionary.Contains(item);
        public void CopyTo(global::System.Collections.Generic.KeyValuePair<K, V>[] array, int arrayIndex) => _mapToDictionary.CopyTo(array, arrayIndex);
        public bool Remove(global::System.Collections.Generic.KeyValuePair<K, V> item) => _mapToDictionary.Remove(item);
        public global::System.Collections.Generic.IEnumerator<global::System.Collections.Generic.KeyValuePair<K, V>> GetEnumerator() => _mapToDictionary.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    public static class IMap_Delegates
    {
        public unsafe delegate int GetView_3(IntPtr thisPtr, out IntPtr __return_value__);
        public unsafe delegate int Clear_6(IntPtr thisPtr);
    }

    [Guid("BBE1FA4C-B0E3-4583-BAEF-1F1B2E483E56")]
    public class IReadOnlyList<T> : global::System.Collections.Generic.IReadOnlyList<T>, global::Windows.Foundation.Collections.IVectorView<T>
    {
        public static IObjectReference CreateMarshaler(global::System.Collections.Generic.IReadOnlyList<T> obj) =>
            MarshalInterface<global::Windows.Foundation.Collections.IVectorView<T>>.CreateMarshaler(new ReadOnlyListToVectorView<T>(obj));

        public static IntPtr GetAbi(IObjectReference objRef) =>
            objRef?.ThisPtr ?? IntPtr.Zero;

        public static global::System.Collections.Generic.IReadOnlyList<T> FromAbi(IntPtr thisPtr) =>
            new IReadOnlyList<T>(ObjRefFromAbi(thisPtr));

        public static IntPtr FromManaged(global::System.Collections.Generic.IReadOnlyList<T> value)
        {
            if (value is null)
            {
                return IntPtr.Zero;
            }
            return CreateMarshaler(value).GetRef();
        }

        public static void DisposeMarshaler(IObjectReference objRef) => objRef?.Dispose();

        public static void DisposeAbi(IntPtr abi) =>
            MarshalInterfaceHelper<global::Windows.Foundation.Collections.IVectorView<T>>.DisposeAbi(abi);

        public static string GetGuidSignature() => GuidGenerator.GetSignature(typeof(IReadOnlyList<T>));

        [Guid("BBE1FA4C-B0E3-4583-BAEF-1F1B2E483E56")]
        public struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            public global::System.Delegate GetAt_0;
            internal _get_PropertyAsUInt32 get_Size_1;
            public global::System.Delegate IndexOf_2;
            public IVectorView_Delegates.GetMany_3 GetMany_3;
            public static Guid PIID = GuidGenerator.CreateIID(typeof(IReadOnlyList<T>));
            private static readonly Type GetAt_0_Type = Expression.GetDelegateType(new Type[] { typeof(void*), typeof(uint), Marshaler<T>.AbiType.MakeByRefType(), typeof(int) });
            private static readonly Type IndexOf_2_Type = Expression.GetDelegateType(new Type[] { typeof(void*), Marshaler<T>.AbiType, typeof(uint).MakeByRefType(), typeof(byte).MakeByRefType(), typeof(int) });

            internal unsafe Vftbl(IntPtr thisPtr)
            {
                var vftblPtr = Marshal.PtrToStructure<VftblPtr>(thisPtr);
                var vftbl = (IntPtr*)vftblPtr.Vftbl;
                IInspectableVftbl = Marshal.PtrToStructure<IInspectable.Vftbl>(vftblPtr.Vftbl);
                GetAt_0 = Marshal.GetDelegateForFunctionPointer(vftbl[6], GetAt_0_Type);
                get_Size_1 = Marshal.GetDelegateForFunctionPointer<_get_PropertyAsUInt32>(vftbl[7]);
                IndexOf_2 = Marshal.GetDelegateForFunctionPointer(vftbl[8], IndexOf_2_Type);
                GetMany_3 = Marshal.GetDelegateForFunctionPointer<IVectorView_Delegates.GetMany_3>(vftbl[9]);
            }

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;
            static unsafe Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,
                    GetAt_0 = global::System.Delegate.CreateDelegate(GetAt_0_Type, typeof(Vftbl).GetMethod("Do_Abi_GetAt_0", BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(Marshaler<T>.AbiType)),
                    get_Size_1 = Do_Abi_get_Size_1,
                    IndexOf_2 = global::System.Delegate.CreateDelegate(IndexOf_2_Type, typeof(Vftbl).GetMethod("Do_Abi_IndexOf_2", BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(Marshaler<T>.AbiType)),
                    GetMany_3 = Do_Abi_GetMany_3
                };
                var nativeVftbl = (IntPtr*)Marshal.AllocCoTaskMem(Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 4);
                Marshal.StructureToPtr(AbiToProjectionVftable.IInspectableVftbl, (IntPtr)nativeVftbl, false);
                nativeVftbl[6] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.GetAt_0);
                nativeVftbl[7] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.get_Size_1);
                nativeVftbl[8] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.IndexOf_2);
                nativeVftbl[9] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.GetMany_3);

                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

            private static unsafe int Do_Abi_GetAt_0<TAbi>(void* thisPtr, uint index, out TAbi __return_value__)
            {
                T ____return_value__ = default;

                __return_value__ = default;

                try
                {
                    ____return_value__ = global::WinRT.ComWrappersSupport.FindObject<global::Windows.Foundation.Collections.IVectorView<T>>(new IntPtr(thisPtr)).GetAt(index);
                    __return_value__ = (TAbi)Marshaler<T>.FromManaged(____return_value__);

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_IndexOf_2<TAbi>(void* thisPtr, TAbi value, out uint index, out byte __return_value__)
            {
                bool ____return_value__ = default;

                index = default;
                __return_value__ = default;
                uint __index = default;

                try
                {
                    ____return_value__ = global::WinRT.ComWrappersSupport.FindObject<global::Windows.Foundation.Collections.IVectorView<T>>(new IntPtr(thisPtr)).IndexOf(Marshaler<T>.FromAbi(value), out __index);
                    index = __index;
                    __return_value__ = (byte)(____return_value__ ? 1 : 0);

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_GetMany_3(IntPtr thisPtr, uint startIndex, int __itemsSize, IntPtr items, out uint __return_value__)
            {
                uint ____return_value__ = default;

                __return_value__ = default;
                T[] __items = Marshaler<T>.FromAbiArray((__itemsSize, items));

                try
                {
                    ____return_value__ = global::WinRT.ComWrappersSupport.FindObject<global::Windows.Foundation.Collections.IVectorView<T>>(thisPtr).GetMany(startIndex, ref __items);
                    Marshaler<T>.CopyManagedArray(__items, items);
                    __return_value__ = ____return_value__;

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_get_Size_1(IntPtr thisPtr, out uint __return_value__)
            {
                uint ____return_value__ = default;

                __return_value__ = default;

                try
                {
                    ____return_value__ = global::WinRT.ComWrappersSupport.FindObject<global::Windows.Foundation.Collections.IVectorView<T>>(thisPtr).Size;
                    __return_value__ = ____return_value__;

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
        }
        public static ObjectReference<Vftbl> ObjRefFromAbi(IntPtr thisPtr)
        {
            if (thisPtr == IntPtr.Zero)
            {
                return null;
            }
            var vftblT = new Vftbl(thisPtr);
            return ObjectReference<Vftbl>.FromAbi(thisPtr, vftblT.IInspectableVftbl.IUnknownVftbl, vftblT);
        }
        public static Guid PIID = Vftbl.PIID;

        public static implicit operator IReadOnlyList<T>(IObjectReference obj) => (obj != null) ? new IReadOnlyList<T>(obj) : null;
        public static implicit operator IReadOnlyList<T>(ObjectReference<Vftbl> obj) => (obj != null) ? new IReadOnlyList<T>(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IObjectReference ObjRef { get => _obj; }

        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public IReadOnlyList(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        public IReadOnlyList(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
            _vectorViewToReadOnlyList = new VectorViewToReadOnlyList<T>(this);
        }
        VectorViewToReadOnlyList<T> _vectorViewToReadOnlyList;

        public unsafe T GetAt(uint index)
        {
            var __params = new object[] { ThisPtr, index, null };
            try
            {
                _obj.Vftbl.GetAt_0.DynamicInvokeAbi(__params);
                return Marshaler<T>.FromAbi(__params[2]);
            }
            finally
            {
                Marshaler<T>.DisposeAbi(__params[2]);
            }
        }

        public unsafe bool IndexOf(T value, out uint index)
        {
            object __value = default;
            var __params = new object[] { ThisPtr, null, null, null };
            try
            {
                __value = Marshaler<T>.CreateMarshaler(value);
                __params[1] = Marshaler<T>.GetAbi(__value);
                _obj.Vftbl.IndexOf_2.DynamicInvokeAbi(__params);
                index = (uint)__params[2];
                return (byte)__params[3] != 0;
            }
            finally
            {
                Marshaler<T>.DisposeMarshaler(__value);
            }
        }

        public unsafe uint GetMany(uint startIndex, ref T[] items)
        {
            object __items = default;
            int __items_length = default;
            IntPtr __items_data = default;
            uint __retval = default;
            try
            {
                __items = Marshaler<T>.CreateMarshalerArray(items);
                (__items_length, __items_data) = Marshaler<T>.GetAbiArray(__items);
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetMany_3(ThisPtr, startIndex, __items_length, __items_data, out __retval));
                items = Marshaler<T>.FromAbiArray((__items_length, __items_data));
                return __retval;
            }
            finally
            {
                Marshaler<T>.DisposeMarshalerArray(__items);
            }
        }

        public unsafe uint Size
        {
            get
            {
                uint __retval = default;
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.get_Size_1(ThisPtr, out __retval));
                return __retval;
            }
        }

        global::Windows.Foundation.Collections.IIterator<T> global::Windows.Foundation.Collections.IIterable<T>.First()
            => As<IEnumerable<T>>().First();

        public int Count => _vectorViewToReadOnlyList.Count;

        public T this[int index] => _vectorViewToReadOnlyList[index];

        public global::System.Collections.Generic.IEnumerator<T> GetEnumerator() => _vectorViewToReadOnlyList.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    public static class IVectorView_Delegates
    {
        public unsafe delegate int GetMany_3(IntPtr thisPtr, uint startIndex, int __itemsSize, IntPtr items, out uint __return_value__);
    }

    [Guid("913337E9-11A1-4345-A3A2-4E7F956E222D")]
    public class IList<T> : global::System.Collections.Generic.IList<T>, global::Windows.Foundation.Collections.IVector<T>
    {
        public static IObjectReference CreateMarshaler(global::System.Collections.Generic.IList<T> obj) =>
            MarshalInterface<global::Windows.Foundation.Collections.IVector<T>>.CreateMarshaler(new ListToVector<T>(obj));

        public static IntPtr GetAbi(IObjectReference objRef) =>
            objRef?.ThisPtr ?? IntPtr.Zero;

        public static global::System.Collections.Generic.IList<T> FromAbi(IntPtr thisPtr) =>
            new IList<T>(ObjRefFromAbi(thisPtr));

        public static IntPtr FromManaged(global::System.Collections.Generic.IList<T> value)
        {
            if (value is null)
            {
                return IntPtr.Zero;
            }
            return CreateMarshaler(value).GetRef();
        }

        public static void DisposeMarshaler(IObjectReference objRef) => objRef?.Dispose();

        public static void DisposeAbi(IntPtr abi) =>
            MarshalInterfaceHelper<global::Windows.Foundation.Collections.IVector<T>>.DisposeAbi(abi);

        public static string GetGuidSignature() => GuidGenerator.GetSignature(typeof(IList<T>));

        [Guid("913337E9-11A1-4345-A3A2-4E7F956E222D")]
        public struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            public global::System.Delegate GetAt_0;
            internal _get_PropertyAsUInt32 get_Size_1;
            public IVector_Delegates.GetView_2 GetView_2;
            public global::System.Delegate IndexOf_3;
            public global::System.Delegate SetAt_4;
            public global::System.Delegate InsertAt_5;
            public IVector_Delegates.RemoveAt_6 RemoveAt_6;
            public global::System.Delegate Append_7;
            public IVector_Delegates.RemoveAtEnd_8 RemoveAtEnd_8;
            public IVector_Delegates.Clear_9 Clear_9;
            public IVector_Delegates.GetMany_10 GetMany_10;
            public IVector_Delegates.ReplaceAll_11 ReplaceAll_11;
            public static Guid PIID = GuidGenerator.CreateIID(typeof(IList<T>));
            private static readonly Type GetAt_0_Type = Expression.GetDelegateType(new Type[] { typeof(void*), typeof(uint), Marshaler<T>.AbiType.MakeByRefType(), typeof(int) });
            private static readonly Type IndexOf_3_Type = Expression.GetDelegateType(new Type[] { typeof(void*), Marshaler<T>.AbiType, typeof(uint).MakeByRefType(), typeof(byte).MakeByRefType(), typeof(int) });
            private static readonly Type SetAt_4_Type = Expression.GetDelegateType(new Type[] { typeof(void*), typeof(uint), Marshaler<T>.AbiType, typeof(int) });
            private static readonly Type InsertAt_5_Type = Expression.GetDelegateType(new Type[] { typeof(void*), typeof(uint), Marshaler<T>.AbiType, typeof(int) });
            private static readonly Type Append_7_Type = Expression.GetDelegateType(new Type[] { typeof(void*), Marshaler<T>.AbiType, typeof(int) });

            internal unsafe Vftbl(IntPtr thisPtr)
            {
                var vftblPtr = Marshal.PtrToStructure<VftblPtr>(thisPtr);
                var vftbl = (IntPtr*)vftblPtr.Vftbl;
                IInspectableVftbl = Marshal.PtrToStructure<IInspectable.Vftbl>(vftblPtr.Vftbl);
                GetAt_0 = Marshal.GetDelegateForFunctionPointer(vftbl[6], GetAt_0_Type);
                get_Size_1 = Marshal.GetDelegateForFunctionPointer<_get_PropertyAsUInt32>(vftbl[7]);
                GetView_2 = Marshal.GetDelegateForFunctionPointer<IVector_Delegates.GetView_2>(vftbl[8]);
                IndexOf_3 = Marshal.GetDelegateForFunctionPointer(vftbl[9], IndexOf_3_Type);
                SetAt_4 = Marshal.GetDelegateForFunctionPointer(vftbl[10], SetAt_4_Type);
                InsertAt_5 = Marshal.GetDelegateForFunctionPointer(vftbl[11], InsertAt_5_Type);
                RemoveAt_6 = Marshal.GetDelegateForFunctionPointer<IVector_Delegates.RemoveAt_6>(vftbl[12]);
                Append_7 = Marshal.GetDelegateForFunctionPointer(vftbl[13], Append_7_Type);
                RemoveAtEnd_8 = Marshal.GetDelegateForFunctionPointer<IVector_Delegates.RemoveAtEnd_8>(vftbl[14]);
                Clear_9 = Marshal.GetDelegateForFunctionPointer<IVector_Delegates.Clear_9>(vftbl[15]);
                GetMany_10 = Marshal.GetDelegateForFunctionPointer<IVector_Delegates.GetMany_10>(vftbl[16]);
                ReplaceAll_11 = Marshal.GetDelegateForFunctionPointer<IVector_Delegates.ReplaceAll_11>(vftbl[17]);
            }

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;
            static unsafe Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,
                    GetAt_0 = global::System.Delegate.CreateDelegate(GetAt_0_Type, typeof(Vftbl).GetMethod("Do_Abi_GetAt_0", BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(Marshaler<T>.AbiType)),
                    get_Size_1 = Do_Abi_get_Size_1,
                    GetView_2 = Do_Abi_GetView_2,
                    IndexOf_3 = global::System.Delegate.CreateDelegate(IndexOf_3_Type, typeof(Vftbl).GetMethod("Do_Abi_IndexOf_3", BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(Marshaler<T>.AbiType)),
                    SetAt_4 = global::System.Delegate.CreateDelegate(SetAt_4_Type, typeof(Vftbl).GetMethod("Do_Abi_SetAt_4", BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(Marshaler<T>.AbiType)),
                    InsertAt_5 = global::System.Delegate.CreateDelegate(InsertAt_5_Type, typeof(Vftbl).GetMethod("Do_Abi_InsertAt_5", BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(Marshaler<T>.AbiType)),
                    RemoveAt_6 = Do_Abi_RemoveAt_6,
                    Append_7 = global::System.Delegate.CreateDelegate(Append_7_Type, typeof(Vftbl).GetMethod("Do_Abi_Append_7", BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(Marshaler<T>.AbiType)),
                    RemoveAtEnd_8 = Do_Abi_RemoveAtEnd_8,
                    Clear_9 = Do_Abi_Clear_9,
                    GetMany_10 = Do_Abi_GetMany_10,
                    ReplaceAll_11 = Do_Abi_ReplaceAll_11
                };
                var nativeVftbl = (IntPtr*)Marshal.AllocCoTaskMem(Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 12);
                Marshal.StructureToPtr(AbiToProjectionVftable.IInspectableVftbl, (IntPtr)nativeVftbl, false);
                nativeVftbl[6] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.GetAt_0);
                nativeVftbl[7] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.get_Size_1);
                nativeVftbl[8] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.GetView_2);
                nativeVftbl[9] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.IndexOf_3);
                nativeVftbl[10] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.SetAt_4);
                nativeVftbl[11] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.InsertAt_5);
                nativeVftbl[12] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.RemoveAt_6);
                nativeVftbl[13] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.Append_7);
                nativeVftbl[14] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.RemoveAtEnd_8);
                nativeVftbl[15] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.Clear_9);
                nativeVftbl[16] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.GetMany_10);
                nativeVftbl[17] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.ReplaceAll_11);

                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

            private static unsafe int Do_Abi_GetAt_0<TAbi>(void* thisPtr, uint index, out TAbi __return_value__)
            {
                T ____return_value__ = default;

                __return_value__ = default;

                try
                {
                    //____return_value__ = ListToVector<T>.Find(new IntPtr(thisPtr)).GetAt(index);
                    ____return_value__ = global::WinRT.ComWrappersSupport.FindObject<global::Windows.Foundation.Collections.IVector<T>>(new IntPtr(thisPtr)).GetAt(index);
                    __return_value__ = (TAbi)Marshaler<T>.FromManaged(____return_value__);

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_GetView_2(IntPtr thisPtr, out IntPtr __return_value__)
            {
                global::Windows.Foundation.Collections.IVectorView<T> ____return_value__ = default;

                __return_value__ = default;

                try
                {
                    //____return_value__ = ListToVector<T>.Find(thisPtr).GetView();
                    ____return_value__ = global::WinRT.ComWrappersSupport.FindObject<global::Windows.Foundation.Collections.IVector<T>>(thisPtr).GetView();
                    __return_value__ = MarshalInterface<global::Windows.Foundation.Collections.IVectorView<T>>.FromManaged(____return_value__);

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_IndexOf_3<TAbi>(void* thisPtr, TAbi value, out uint index, out byte __return_value__)
            {
                bool ____return_value__ = default;

                index = default;
                __return_value__ = default;
                uint __index = default;

                try
                {
                    ____return_value__ = global::WinRT.ComWrappersSupport.FindObject<global::Windows.Foundation.Collections.IVector<T>>(new IntPtr(thisPtr)).IndexOf(Marshaler<T>.FromAbi(value), out __index); index = __index;
                    __return_value__ = (byte)(____return_value__ ? 1 : 0);

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_SetAt_4<TAbi>(void* thisPtr, uint index, TAbi value)
            {


                try
                {
                    global::WinRT.ComWrappersSupport.FindObject<global::Windows.Foundation.Collections.IVector<T>>(new IntPtr(thisPtr)).SetAt(index, Marshaler<T>.FromAbi(value));
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_InsertAt_5<TAbi>(void* thisPtr, uint index, TAbi value)
            {


                try
                {
                    global::WinRT.ComWrappersSupport.FindObject<global::Windows.Foundation.Collections.IVector<T>>(new IntPtr(thisPtr)).InsertAt(index, Marshaler<T>.FromAbi(value));
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_RemoveAt_6(IntPtr thisPtr, uint index)
            {


                try
                {
                    global::WinRT.ComWrappersSupport.FindObject<global::Windows.Foundation.Collections.IVector<T>>(thisPtr).RemoveAt(index);
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_Append_7<TAbi>(void* thisPtr, TAbi value)
            {


                try
                {
                    global::WinRT.ComWrappersSupport.FindObject<global::Windows.Foundation.Collections.IVector<T>>(new IntPtr(thisPtr)).Append(Marshaler<T>.FromAbi(value));
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_RemoveAtEnd_8(IntPtr thisPtr)
            {


                try
                {
                    global::WinRT.ComWrappersSupport.FindObject<global::Windows.Foundation.Collections.IVector<T>>(thisPtr).RemoveAtEnd();
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_Clear_9(IntPtr thisPtr)
            {


                try
                {
                    global::WinRT.ComWrappersSupport.FindObject<global::Windows.Foundation.Collections.IVector<T>>(thisPtr)._Clear();
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_GetMany_10(IntPtr thisPtr, uint startIndex, int __itemsSize, IntPtr items, out uint __return_value__)
            {
                uint ____return_value__ = default;

                __return_value__ = default;
                T[] __items = Marshaler<T>.FromAbiArray((__itemsSize, items));

                try
                {
                    ____return_value__ = global::WinRT.ComWrappersSupport.FindObject<global::Windows.Foundation.Collections.IVector<T>>(thisPtr).GetMany(startIndex, ref __items); Marshaler<T>.CopyManagedArray(__items, items);
                    __return_value__ = ____return_value__;

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_ReplaceAll_11(IntPtr thisPtr, int __itemsSize, IntPtr items)
            {


                try
                {
                    global::WinRT.ComWrappersSupport.FindObject<global::Windows.Foundation.Collections.IVector<T>>(thisPtr).ReplaceAll(Marshaler<T>.FromAbiArray((__itemsSize, items)));
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_get_Size_1(IntPtr thisPtr, out uint __return_value__)
            {
                uint ____return_value__ = default;

                __return_value__ = default;

                try
                {
                    ____return_value__ = global::WinRT.ComWrappersSupport.FindObject<global::Windows.Foundation.Collections.IVector<T>>(thisPtr).Size;
                    //____return_value__ = ListToVector<T>.Find(thisPtr).Size;
                    __return_value__ = ____return_value__;
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
        }
        public static ObjectReference<Vftbl> ObjRefFromAbi(IntPtr thisPtr)
        {
            if (thisPtr == IntPtr.Zero)
            {
                return null;
            }
            var vftblT = new Vftbl(thisPtr);
            return ObjectReference<Vftbl>.FromAbi(thisPtr, vftblT.IInspectableVftbl.IUnknownVftbl, vftblT);
        }
        public static Guid PIID = Vftbl.PIID;

        public static implicit operator IList<T>(IObjectReference obj) => (obj != null) ? new IList<T>(obj) : null;
        public static implicit operator IList<T>(ObjectReference<Vftbl> obj) => (obj != null) ? new IList<T>(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IObjectReference ObjRef { get => _obj; }

        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public IList(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        public IList(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
            _vectorToList = new VectorToList<T>(this);
        }
        VectorToList<T> _vectorToList;


        public unsafe T GetAt(uint index)
        {
            var __params = new object[] { ThisPtr, index, null };
            try
            {
                _obj.Vftbl.GetAt_0.DynamicInvokeAbi(__params);
                return Marshaler<T>.FromAbi(__params[2]);
            }
            finally
            {
                Marshaler<T>.DisposeAbi(__params[2]);
            }
        }

        public unsafe global::Windows.Foundation.Collections.IVectorView<T> GetView()
        {
            IntPtr __retval = default;
            try
            {
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetView_2(ThisPtr, out __retval));
                return MarshalInterface<global::Windows.Foundation.Collections.IVectorView<T>>.FromAbi(__retval);
            }
            finally
            {
                MarshalInterface<global::Windows.Foundation.Collections.IVectorView<T>>.DisposeAbi(__retval);
            }
        }

        public unsafe bool IndexOf(T value, out uint index)
        {
            object __value = default;
            var __params = new object[] { ThisPtr, null, null, null };
            try
            {
                __value = Marshaler<T>.CreateMarshaler(value);
                __params[1] = Marshaler<T>.GetAbi(__value);
                _obj.Vftbl.IndexOf_3.DynamicInvokeAbi(__params);
                index = (uint)__params[2];
                return (byte)__params[3] != 0;
            }
            finally
            {
                Marshaler<T>.DisposeMarshaler(__value);
            }
        }

        public unsafe void SetAt(uint index, T value)
        {
            object __value = default;
            var __params = new object[] { ThisPtr, index, null };
            try
            {
                __value = Marshaler<T>.CreateMarshaler(value);
                __params[2] = Marshaler<T>.GetAbi(__value);
                _obj.Vftbl.SetAt_4.DynamicInvokeAbi(__params);
            }
            finally
            {
                Marshaler<T>.DisposeMarshaler(__value);
            }
        }

        public unsafe void InsertAt(uint index, T value)
        {
            object __value = default;
            var __params = new object[] { ThisPtr, index, null };
            try
            {
                __value = Marshaler<T>.CreateMarshaler(value);
                __params[2] = Marshaler<T>.GetAbi(__value);
                _obj.Vftbl.InsertAt_5.DynamicInvokeAbi(__params);
            }
            finally
            {
                Marshaler<T>.DisposeMarshaler(__value);
            }
        }

        public unsafe void RemoveAt(uint index)
        {
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.RemoveAt_6(ThisPtr, index));
        }

        public unsafe void Append(T value)
        {
            object __value = default;
            var __params = new object[] { ThisPtr, null };
            try
            {
                __value = Marshaler<T>.CreateMarshaler(value);
                __params[1] = Marshaler<T>.GetAbi(__value);
                _obj.Vftbl.Append_7.DynamicInvokeAbi(__params);
            }
            finally
            {
                Marshaler<T>.DisposeMarshaler(__value);
            }
        }

        public unsafe void RemoveAtEnd()
        {
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.RemoveAtEnd_8(ThisPtr));
        }

        public unsafe void _Clear()
        {
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.Clear_9(ThisPtr));
        }

        public unsafe uint GetMany(uint startIndex, ref T[] items)
        {
            object __items = default;
            int __items_length = default;
            IntPtr __items_data = default;
            uint __retval = default;
            try
            {
                __items = Marshaler<T>.CreateMarshalerArray(items);
                (__items_length, __items_data) = Marshaler<T>.GetAbiArray(__items);
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetMany_10(ThisPtr, startIndex, __items_length, __items_data, out __retval));
                items = Marshaler<T>.FromAbiArray((__items_length, __items_data));
                return __retval;
            }
            finally
            {
                Marshaler<T>.DisposeMarshalerArray(__items);
            }
        }

        public unsafe void ReplaceAll(T[] items)
        {
            object __items = default;
            int __items_length = default;
            IntPtr __items_data = default;
            try
            {
                __items = Marshaler<T>.CreateMarshalerArray(items);
                (__items_length, __items_data) = Marshaler<T>.GetAbiArray(__items);
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.ReplaceAll_11(ThisPtr, __items_length, __items_data));
            }
            finally
            {
                Marshaler<T>.DisposeMarshalerArray(__items);
            }
        }

        public unsafe uint Size
        {
            get
            {
                uint __retval = default;
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.get_Size_1(ThisPtr, out __retval));
                return __retval;
            }
        }
        
        global::Windows.Foundation.Collections.IIterator<T> global::Windows.Foundation.Collections.IIterable<T>.First() => 
            As<IEnumerable<T>>().First();

        public int Count => _vectorToList.Count;
        public bool IsReadOnly => _vectorToList.IsReadOnly;
        public T this[int index] { get => _vectorToList[index]; set => _vectorToList[index] = value; }
        public int IndexOf(T item) => _vectorToList.IndexOf(item);
        public void Insert(int index, T item) => _vectorToList.Insert(index, item);
        public void RemoveAt(int index) => _vectorToList.RemoveAt(index);
        public void Add(T item) => _vectorToList.Add(item);
        public void Clear() => _vectorToList.Clear();
        public bool Contains(T item) => _vectorToList.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => _vectorToList.CopyTo(array, arrayIndex);
        public bool Remove(T item) => _vectorToList.Remove(item);
        public global::System.Collections.Generic.IEnumerator<T> GetEnumerator() => _vectorToList.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    public static class IVector_Delegates
    {
        public unsafe delegate int GetView_2(IntPtr thisPtr, out IntPtr __return_value__);
        public unsafe delegate int RemoveAt_6(IntPtr thisPtr, uint index);
        public unsafe delegate int RemoveAtEnd_8(IntPtr thisPtr);
        public unsafe delegate int Clear_9(IntPtr thisPtr);
        public unsafe delegate int GetMany_10(IntPtr thisPtr, uint startIndex, int __itemsSize, IntPtr items, out uint __return_value__);
        public unsafe delegate int ReplaceAll_11(IntPtr thisPtr, int __itemsSize, IntPtr items);
    }
}
