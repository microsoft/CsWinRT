using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Foundation;
using Windows.Foundation.Metadata;

namespace AuthoringTest
{
    public enum BasicEnum
    {
        First = -1,
        Second = 0,
        Third = 1,
        Fourth
    }

    [Flags]
    public enum FlagsEnum : uint
    {
        First = 0,
        Second = 1,
        Third = 2,
        Fourth = 4
    }

    internal enum PrivateEnum
    {
        PrivateFirst,
        PrivateSecond
    }

    public delegate void BasicDelegate(uint value);
    public delegate bool ComplexDelegate(double value, int value2);

    public sealed class BasicClass
    {
        private BasicEnum basicEnum = BasicEnum.First;
        private FlagsEnum flagsEnum = FlagsEnum.Second | FlagsEnum.Third;

        public event ComplexDelegate ComplexDelegateEvent;
        private event ComplexDelegate ComplexDelegateEvent2;

        private DateTimeOffset dateTime = DateTime.Today;

        public Point GetPoint()
        {
            Point p = new Point
            {
                X = 2,
                Y = 3
            };
            return p;
        }

        public void SetDate(DateTimeOffset dateTime)
        {
            this.dateTime = dateTime;
        }

        public DateTimeOffset GetDate()
        {
            return dateTime;
        }

        public TimeSpan GetTimespan()
        {
            return new TimeSpan(100);
        }

        public CustomWWW GetCustomWWW()
        {
            return new CustomWWW();
        }

        public BasicStruct GetBasicStruct()
        {
            BasicStruct basicStruct;
            basicStruct.X = 4;
            basicStruct.Y = 8;
            basicStruct.Value = "CsWinRT";
            return basicStruct;
        }

        public int GetSumOfInts(BasicStruct basicStruct)
        {
            return basicStruct.X + basicStruct.Y;
        }

        public ComplexStruct GetComplexStruct()
        {
            ComplexStruct complexStruct;
            complexStruct.X = 12;
            complexStruct.Val = true;
            complexStruct.BasicStruct = GetBasicStruct();
            return complexStruct;
        }

        public int? GetX(ComplexStruct basicStruct)
        {
            return basicStruct.X;
        }

        public void SetBasicEnum(BasicEnum basicEnum)
        {
            this.basicEnum = basicEnum;
        }

        public BasicEnum GetBasicEnum()
        {
            return basicEnum;
        }

        public void SetFlagsEnum(FlagsEnum flagsEnum)
        {
            this.flagsEnum = flagsEnum;
        }

        public FlagsEnum GetFlagsEnum()
        {
            return flagsEnum;
        }

        public BasicClass ReturnParameter(BasicClass basicClass)
        {
            return basicClass;
        }

        public BasicStruct[] ReturnArray([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] BasicStruct[] basicStructs)
        {
            BasicStruct[] copy = new BasicStruct[basicStructs.Length];
            for (int idx = 0; idx < copy.Length; idx++)
            {
                copy[idx] = basicStructs[idx];
            }
            return copy;
        }

        public int GetSum([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] int[] arr)
        {
            return arr.Sum();
        }

        public void PopulateArray([System.Runtime.InteropServices.WindowsRuntime.WriteOnlyArray] int[] arr)
        {
            for (int idx = 0; idx < arr.Length; idx++)
            {
                arr[idx] = idx + 1;
            }
        }

        public void GetArrayOfLength(int length, out int[] arr)
        {
            arr = new int[length];
            for (int idx = 0; idx < arr.Length; idx++)
            {
                arr[idx] = idx + 1;
            }
        }

        private void PrivateFunction()
        {
        }
    }

    public struct BasicStruct
    {
        public int X, Y;
        public string Value;
    }

    public struct ComplexStruct
    {
        public int? X;
        public bool? Val;
        public BasicStruct BasicStruct;
    }

    public sealed class CustomWWW : IWwwFormUrlDecoderEntry
    {
        public string Name => "CustomWWW";

        public string Value => "CsWinRT";
    }

    [Version(3u)]
    public interface IDouble
    {
        double GetDouble();
        double GetDouble(bool ignoreFactor);

        string GetNumStr(int num);

        [Windows.Foundation.Metadata.DefaultOverload()]
        string GetNumStr(double num);
    }

    public interface IAnotherInterface
    {
        event ComplexDelegate ComplexDelegateEvent;

        bool FireComplexDelegate(double value, int value2);

        [Version(5u)]
        int GetThree();
    }

    public sealed class TestClass : IDouble, IAnotherInterface
    {
        public event BasicDelegate BasicDelegateEvent, BasicDelegateEvent2;
        public event ComplexDelegate ComplexDelegateEvent;

        public int Factor { get; set; }
        private int Factor2 { get; set; }
        public uint DelegateValue { get; set; }
        public IDisposable DisposableObject { get; set; }
        public DisposableClass DisposableClassObject { get; set; }
        public IList<object> ObjectList { get; set; }
        public IAsyncOperation<Int32> IntAsyncOperation { get; set; }
        public Type Type { get; set; }
        [Windows.Foundation.Metadata.Deprecated("test", DeprecationType.Deprecate, 3)]
        public int Deprecated { get; }

        public TestClass()
        {
            Factor = 1;
            Factor2 = 1;
            BasicDelegateEvent += TestClass_BasicDelegateEvent;
        }

        private void TestClass_BasicDelegateEvent(uint value)
        {
            DelegateValue = value;
        }

        // Factory

        public TestClass(int factor)
        {
            Factor = factor;
        }

        // Statics
        public static int GetDefaultFactor()
        {
            return 1;
        }

        public static int GetDefaultNumber()
        {
            return 2;
        }

        // Default interface

        public void FireBasicDelegate(uint value)
        {
            BasicDelegateEvent.Invoke(value);
        }

        public void FireBasicDelegate2(uint value)
        {
            BasicDelegateEvent2.Invoke(value);
        }

        public int GetFactor()
        {
            return Factor;
        }

        public void SetProjectedDisposableObject()
        {
            DisposableObject = new DisposableClass();
            DisposableClassObject = new DisposableClass();
        }

        public void SetNonProjectedDisposableObject()
        {
            DisposableObject = new NonProjectedDisposableClass();
        }

        public IList<IDisposable> GetDisposableObjects()
        {
            return new List<IDisposable>() {
                new DisposableClass(),
                new NonProjectedDisposableClass(),
                new DisposableClass()
            };
        }

        public static IReadOnlyList<Uri> GetUris()
        {
            return new List<Uri>() {
                new Uri("http://github.com"),
                new Uri("http://microsoft.com")
            };
        }

        public IAsyncOperation<Int32> GetIntAsyncOperation()
        {
            int val = IntAsyncOperation.GetResults();

            var task = Task<int>.Run(() =>
            {
                Thread.Sleep(100);
                return val;
            });
            return task.AsAsyncOperation();
        }

        public int SetIntAsyncOperation(IAsyncOperation<Int32> op)
        {
            return op.GetResults();
        }

        public int GetObjectListSum()
        {
            int sum = 0;
            foreach (var obj in ObjectList)
            {
                sum += (int)(obj as int?);
            }
            return sum;
        }

        public int GetSum(CustomDictionary dictionary, string element)
        {
            if (dictionary.Count != 0 && dictionary.ContainsKey(element))
            {
                return dictionary[element].X + dictionary[element].Y;
            }

            return -1;
        }

        public void SetTypeToTestClass()
        {
            Type = typeof(TestClass);
        }

        // Method overloading

        public int GetNumber()
        {
            return 2 * Factor;
        }

        public int GetNumber(bool ignoreFactor)
        {
            return ignoreFactor ? 2 : GetNumber();
        }

        public int GetNumberWithDelta(bool ignoreFactor, int delta)
        {
            return delta + (ignoreFactor ? 2 : GetNumber());
        }

        // Implementing interface

        public double GetDouble()
        {
            return 2.0 * Factor;
        }

        public double GetDouble(bool ignoreFactor)
        {
            return ignoreFactor ? 2.0 : GetNumber();
        }

        [Windows.Foundation.Metadata.DefaultOverload()]
        public int Get(int num)
        {
            return num;
        }

        public string Get(string str)
        {
            return str;
        }

        public string GetNumStr(int num)
        {
            return num.ToString();
        }

        public string GetNumStr(double num)
        {
            return num.ToString();
        }

        // Implementing another interface

        public bool FireComplexDelegate(double value, int value2)
        {
            return ComplexDelegateEvent.Invoke(value, value2);
        }

        public int GetThree()
        {
            return 3;
        }

    }

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

    internal sealed class NonProjectedDisposableClass : IDisposable
    {
        public bool IsDisposed { get; set; }

        public NonProjectedDisposableClass()
        {
            IsDisposed = false;
        }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    internal class InternalClass
    {
        public static void Get()
        {
        }
    }

    public sealed class CustomDictionary : IDictionary<string, BasicStruct>
    {
        private readonly Dictionary<string, BasicStruct> _dictionary;

        public CustomDictionary()
        {
            _dictionary = new Dictionary<string, BasicStruct>();
        }

        public BasicStruct this[string key]
        {
            get => _dictionary[key];
            set => _dictionary[key] = value;
        }

        public ICollection<string> Keys => _dictionary.Keys;

        public ICollection<BasicStruct> Values => _dictionary.Values;

        public int Count => _dictionary.Count;

        public bool IsReadOnly => false;

        public void Add(string key, BasicStruct value)
        {
            _dictionary.Add(key, value);
        }

        public void Add(KeyValuePair<string, BasicStruct> item)
        {
            _dictionary.Add(item.Key, item.Value);
        }

        public void Clear()
        {
            _dictionary.Clear();
        }

        public bool Contains(KeyValuePair<string, BasicStruct> item)
        {
            return _dictionary.ContainsKey(item.Key);
        }

        public bool ContainsKey(string key)
        {
            return _dictionary.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, BasicStruct>[] array, int arrayIndex)
        {
        }

        public IEnumerator<KeyValuePair<string, BasicStruct>> GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

        public bool Remove(string key)
        {
            return _dictionary.Remove(key);
        }

        public bool Remove(KeyValuePair<string, BasicStruct> item)
        {
            return _dictionary.Remove(item.Key);
        }

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out BasicStruct value)
        {
            return _dictionary.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }
    }

    public sealed class CustomReadOnlyDictionary : IReadOnlyDictionary<string, BasicStruct>
    {
        private readonly CustomDictionary _dictionary;

        public CustomReadOnlyDictionary(CustomDictionary dictionary)
        {
            _dictionary = dictionary;
        }

        public BasicStruct this[string key] => _dictionary[key];

        public IEnumerable<string> Keys => _dictionary.Keys;

        public IEnumerable<BasicStruct> Values => _dictionary.Values;

        public int Count => _dictionary.Count;

        public bool ContainsKey(string key)
        {
            return _dictionary.ContainsKey(key);
        }

        public IEnumerator<KeyValuePair<string, BasicStruct>> GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out BasicStruct value)
        {
            return _dictionary.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }
    }

    // Evaluate whether it should be supported - .NET native doesn't.
    // TODO: conflict with IDisposable
    /*
    public sealed class CustomEnumerator : IEnumerator<DisposableClass>
    {
        private readonly DisposableClass[] _disposableObjects;
        private readonly IEnumerator _enumerator;

        public CustomEnumerator()
        {
        }

        public CustomEnumerator(DisposableClass[] disposableObjects)
        {
            _disposableObjects = disposableObjects;
            _enumerator = _disposableObjects.GetEnumerator();
        }

        public DisposableClass Current => (DisposableClass)_enumerator.Current;

        object IEnumerator.Current => _enumerator.Current;

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            return _enumerator.MoveNext();
        }

        public void Reset()
        {
            _enumerator.Reset();
        }
    }
    */

    public sealed class CustomVector : IList<DisposableClass>
    {
        private IList<DisposableClass> _list;

        public CustomVector()
        {
            _list = new List<DisposableClass>();
        }

        public CustomVector(IList<DisposableClass> list)
        {
            _list = list;
        }

        public DisposableClass this[int index] { get => _list[index]; set => _list[index] = value; }

        public int Count => _list.Count();

        public bool IsReadOnly => false;

        public void Add(DisposableClass item)
        {
            _list.Add(item);
        }

        public void Clear()
        {
            _list.Clear();
        }

        public bool Contains(DisposableClass item)
        {
            return _list.Contains(item);
        }

        public void CopyTo(DisposableClass[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        public IEnumerator<DisposableClass> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public int IndexOf(DisposableClass item)
        {
            return _list.IndexOf(item);
        }

        public void Insert(int index, DisposableClass item)
        {
            _list.Insert(index, item);
        }

        public bool Remove(DisposableClass item)
        {
            return _list.Remove(item);
        }

        public void RemoveAt(int index)
        {
            _list.RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }
    }

    public sealed class CustomVectorView : IReadOnlyList<DisposableClass>
    {
        private CustomVector _customVector;

        public CustomVectorView()
        {
            _customVector = new CustomVector();
        }

        public CustomVectorView(CustomVector customVector)
        {
            _customVector = customVector;
        }

        public DisposableClass this[int index] => _customVector[index];

        public int Count => _customVector.Count;

        public IEnumerator<DisposableClass> GetEnumerator()
        {
            return _customVector.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _customVector.GetEnumerator();
        }
    }

    public sealed class CustomVector2 : IList
    {
        private IList _list;

        public CustomVector2()
        {
            _list = new ArrayList();
        }

        public CustomVector2(IList list)
        {
            _list = list;
        }

        public object this[int index] { get => _list[index]; set => _list[index] = value; }

        public bool IsFixedSize => _list.IsFixedSize;

        public bool IsReadOnly => _list.IsReadOnly;

        public int Count => _list.Count;

        public bool IsSynchronized => _list.IsSynchronized;

        public object SyncRoot => _list.SyncRoot;

        public int Add(object value)
        {
            return _list.Add(value);
        }

        public void Clear()
        {
            _list.Clear();
        }

        public bool Contains(object value)
        {
            return _list.Contains(value);
        }

        public void CopyTo(Array array, int index)
        {
            _list.CopyTo(array, index);
        }

        public IEnumerator GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public int IndexOf(object value)
        {
            return _list.IndexOf(value);
        }

        public void Insert(int index, object value)
        {
            _list.Insert(index, value);
        }

        public void Remove(object value)
        {
            _list.Remove(value);
        }

        public void RemoveAt(int index)
        {
            _list.RemoveAt(index);
        }
    }

    public static class StaticClass
    {
        public static int GetNumber()
        {
            return 4;
        }

        public static int GetNumber(int number)
        {
            return number;
        }
    }

    public static class ButtonUtils
    {
        public static Button GetButton()
        {
            Button button = new Button
            {
                Content = "Button"
            };
            return button;
        }

        public static CustomButton GetCustomButton()
        {
            return new CustomButton();
        }

        public static CustomButton GetCustomButton(string text)
        {
            return new CustomButton(text);
        }
    }

    public sealed class CustomButton : Button
    {
        public string Text { get; private set; }
        public bool OverrideEntered { get; set; }

        public CustomButton()
            : this("CustomButton")
        {
        }

        public CustomButton(string text)
        {
            Text = text;
            Content = text;
            OverrideEntered = true;
        }

        protected override void OnPointerEntered(global::Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (!OverrideEntered)
            {
                base.OnPointerEntered(e);
                return;
            }

            Text = Content?.ToString();
            Content = "Entered";
        }

        protected override void OnPointerExited(global::Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (!OverrideEntered)
            {
                base.OnPointerExited(e);
                return;
            }

            Content = Text;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var size = new Size(160, 30);
            base.MeasureOverride(size);
            return size;
        }

        public string GetText()
        {
            return Text;
        }
    }

    // Also tests scenario of class with no default interface members.
    public sealed class CustomStackPanel : StackPanel
    {
        protected override Size MeasureOverride(Size availableSize)
        {
            foreach (var child in Children)
            {
                child.Measure(new Size(160, 50));
            }

            return new Size(330, 500);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            int x = 0, y = 0;
            foreach (var child in Children)
            {
                child.Arrange(new Rect(x, y, child.DesiredSize.Width, child.DesiredSize.Height));
                x = (x + 165) % 330;
                if (x == 0)
                {
                    y += 50;
                }
            }

            return new Size(330, 500);
        }
    }

    public sealed class CustomXamlServiceProvider : IXamlServiceProvider
    {
        public object GetService(Type type)
        {
            return type.Name;
        }
    }

    public sealed class CustomNotifyPropertyChanged : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
    }

    public sealed class CustomCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;
        private bool _canExecute = false;

        public void SetCanExecute(bool canExecute)
        {
            _canExecute = canExecute;
            CanExecuteChanged?.Invoke(this, new EventArgs());
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute;
        }

        public void Execute(object parameter)
        {
        }
    }

    public sealed class CustomNotifyCollectionChanged : INotifyCollectionChanged
    {
        public event NotifyCollectionChangedEventHandler CollectionChanged;
    }

    public sealed class CustomNotifyDataErrorInfo : INotifyDataErrorInfo
    {
        public bool HasErrors => false;

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        public IEnumerable GetErrors(string propertyName)
        {
            return null;
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
}