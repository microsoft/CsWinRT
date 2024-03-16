using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

#pragma warning disable CA1416

namespace AuthoringWuxTest
{
    public sealed class DisposableClass : IDisposable
    {
        public bool IsDisposed { get; set; }

        public DisposableClass()
        {
            IsDisposed = false;
        }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }
    public sealed class CustomNotifyPropertyChanged : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public sealed class CustomNotifyCollectionChanged : INotifyCollectionChanged
    {
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public void RaiseCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            CollectionChanged?.Invoke(this, args);
        }
    }

    public sealed class CustomEnumerable : IEnumerable
    {
        private IEnumerable _enumerable;

        public CustomEnumerable(IEnumerable enumerable)
        {
            _enumerable = enumerable;
        }

        public IEnumerator GetEnumerator()
        {
            return _enumerable.GetEnumerator();
        }
    }

    public sealed class MultipleInterfaceMappingClass : IList<DisposableClass>, IList
    {
        private List<DisposableClass> _list = new List<DisposableClass>();

        DisposableClass IList<DisposableClass>.this[int index] { get => _list[index]; set => _list[index] = value; }
        object IList.this[int index] { get => _list[index]; set => ((IList)_list) [index] = value; }

        int ICollection<DisposableClass>.Count => _list.Count;

        int ICollection.Count => _list.Count;

        bool ICollection<DisposableClass>.IsReadOnly => true;

        bool IList.IsReadOnly => true;

        bool IList.IsFixedSize => false;

        bool ICollection.IsSynchronized => true;

        object ICollection.SyncRoot => ((ICollection) _list).SyncRoot;

        void ICollection<DisposableClass>.Add(DisposableClass item)
        {
            _list.Add(item);
        }

        int IList.Add(object value)
        {
            return ((IList) _list).Add(value);
        }

        void ICollection<DisposableClass>.Clear()
        {
            _list.Clear();
        }

        void IList.Clear()
        {
            _list.Clear();
        }

        bool ICollection<DisposableClass>.Contains(DisposableClass item)
        {
            return _list.Contains(item);
        }

        bool IList.Contains(object value)
        {
            return ((IList) _list).Contains(value);
        }

        void ICollection<DisposableClass>.CopyTo(DisposableClass[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        void ICollection.CopyTo(Array array, int index)
        {
             ((ICollection) _list).CopyTo(array, index);
        }

        IEnumerator<DisposableClass> IEnumerable<DisposableClass>.GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        int IList<DisposableClass>.IndexOf(DisposableClass item)
        {
            return _list.IndexOf(item);
        }

        int IList.IndexOf(object value)
        {
            return ((IList) _list).IndexOf(value);
        }

        void IList<DisposableClass>.Insert(int index, DisposableClass item)
        {
            _list.Insert(index, item);
        }

        void IList.Insert(int index, object value)
        {
            ((IList) _list).Insert(index, value);
        }

        bool ICollection<DisposableClass>.Remove(DisposableClass item)
        {
            return _list.Remove(item);
        }

        void IList.Remove(object value)
        {
            ((IList) _list).Remove(value);
        }

        void IList<DisposableClass>.RemoveAt(int index)
        {
            _list.RemoveAt(index);
        }

        void IList.RemoveAt(int index)
        {
            _list.RemoveAt(index);
        }
    }
}
