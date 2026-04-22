using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using AuthoringTest;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.Graphics.Effects;
using WindowsRuntime;
using WindowsRuntime.InteropServices;
using WindowsRuntime.Xaml;

#pragma warning disable CA1416

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
    public delegate void DoubleDelegate(double value);
    internal delegate void PrivateDelegate(uint value);

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

        public BasicStruct GetBasicStruct() =>
            new BasicStruct() { X = 4, Y = 8, Value = "CsWinRT" };

        public int GetSumOfInts(BasicStruct basicStruct)
        {
            return basicStruct.X + basicStruct.Y;
        }

        public ComplexStruct GetComplexStruct() =>
            new ComplexStruct() { X = 12, Val = true, BasicStruct = GetBasicStruct() };

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

        public BasicStruct[] ReturnArray(ReadOnlySpan<BasicStruct> basicStructs)
        {
            BasicStruct[] copy = new BasicStruct[basicStructs.Length];
            for (int idx = 0; idx < copy.Length; idx++)
            {
                copy[idx] = basicStructs[idx];
            }
            return copy;
        }

        public int GetSum(ReadOnlySpan<int> arr)
        {
            int sum = 0;
            foreach (int value in arr)
            {
                sum += value;
            }
            return sum;
        }

        public void PopulateArray(Span<int> arr)
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
        public BasicEnum basicEnum;
    }

    public struct ComplexStruct
    {
        public int? X;
        public bool? Val;
        public BasicStruct BasicStruct;
    }

    internal struct PrivateStruct
    {
        public int X, Y;
        public string Value;
    }

    public sealed class CustomWWW : IWwwFormUrlDecoderEntry
    {
        public string Name => "CustomWWW";

        public string Value => "CsWinRT";
    }

    [GeneratedCustomPropertyProvider]
    public sealed partial class CustomProperty
    {
        public int Number { get; } = 4;
        public string Value => "CsWinRT";
        public CustomWWW Url => null;
        public CustomPropertyStructType CustomPropertyStructType => new CustomPropertyStructType();
    }

    [GeneratedCustomPropertyProvider]
    public partial struct CustomPropertyStructType
    {
        // Public WinRT struct types must have at least one field
        public int Dummy;

        public int Number => 4;
        public string Value => "CsWinRTFromStructType";
    }

    [GeneratedCustomPropertyProvider]
    internal sealed partial record CustomPropertyRecordType
    {
        public int Number { get; } = 4;
        public string Value => "CsWinRTFromRecordType";
    }

    [GeneratedCustomPropertyProvider]
    internal partial record struct CustomPropertyRecordStructType
    {
        public int Number => 4;
        public string Value => "CsWinRTFromRecordStructType";
    }

    public static class CustomPropertyRecordTypeFactory
    {
        public static object CreateStruct() => new CustomPropertyStructType();

        public static object CreateRecord() => new CustomPropertyRecordType();

        public static object CreateRecordStruct() => default(CustomPropertyRecordStructType);
    }
    
    public sealed partial class CustomPropertyProviderWithExplicitImplementation : ICustomPropertyProvider
    {
        public Type Type => typeof(CustomPropertyProviderWithExplicitImplementation);

        public ICustomProperty GetCustomProperty(string name)
        {
            if (name == "TestCustomProperty")
            {
                return new CustomPropertyWithExplicitImplementation();
            }

            return null;
        }

        public ICustomProperty GetIndexedProperty(string name, Type type)
        {
            return null;
        }

        public string GetStringRepresentation()
        {
            return string.Empty;
        }
    }

    public sealed partial class CustomPropertyWithExplicitImplementation : ICustomProperty
    {
        internal CustomPropertyWithExplicitImplementation()
        {
        }

        public bool CanRead => true;

        public bool CanWrite => false;

        public string Name => "TestCustomProperty";

        public Type Type => typeof(CustomPropertyWithExplicitImplementation);

        /// <inheritdoc />
        public object GetIndexedValue(object target, object index)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public object GetValue(object target)
        {
            return "TestPropertyValue";
        }

        /// <inheritdoc />
        public void SetIndexedValue(object target, object value, object index)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public void SetValue(object target, object value)
        {
            throw new NotSupportedException();
        }
    }

    [Version(3u)]
    public interface IDouble
    {
        double GetDouble();
        double GetDouble(bool ignoreFactor);

        string GetNumStr(int num);

        [Windows.Foundation.Metadata.DefaultOverload()]
        string GetNumStr(double num);
        double Number { get; set; }

        event DoubleDelegate DoubleDelegateEvent;
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
        public event DoubleDelegate DoubleDelegateEvent;

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
        public double Number { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }


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

        public static int DefaultNumber { get; set; }

        internal static int DefaultNumber2 { get; set; }

        public static event BasicDelegate StaticDelegateEvent;

        public static void FireStaticDelegate(uint value)
        {
            StaticDelegateEvent?.Invoke(value);
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

        public static IList<bool> GetBools()
        {
            return new List<bool>()
            {
                true,
                false,
                true
            };
        }

        public static IList<BasicStruct> GetBasicStructs()
        {
            return new List<BasicStruct>()
            {
                new BasicStruct() { X = 1, Y = 2, Value = "Basic" },
                new BasicStruct() { X = 2, Y = 4, Value = "Struct" },
            };
        }

        public static IList<ComplexStruct> GetComplexStructs()
        {
            return new List<ComplexStruct>()
            {
                new ComplexStruct() {
                    X = 12,
                    Val = true,
                    BasicStruct = new BasicStruct() { X = 1, Y = 2, Value = "Basic" } },
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

        public IAsyncOperationWithProgress<double, double> GetDoubleAsyncOperation()
        {
            return AsyncInfo.Run<double, double>(async (cancellationToken, progress) =>
            {
                await Task.Delay(100);
                return 4.0;
            });
        }

        public IAsyncOperation<BasicStruct> GetStructAsyncOperation()
        {
            return WindowsRuntime.InteropServices.AsyncInfo.FromResult(new BasicStruct() { X = 2, Y = 4, Value = "Test" });
        }

        public IAsyncOperation<bool> GetBoolAsyncOperation()
        {
            return Task.FromResult(false).AsAsyncOperation();
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

        // Type-erased objects
        public IList<object> GetTypeErasedProjectedObjects()
        {
            return new List<object>() {
                42,
                BasicEnum.First,
                new BasicStruct() { X = 1, Y = 2, Value = "Basic" },
                new BasicDelegate((uint value) => {}),
                typeof(TestClass),
                new DisposableClass().GetType()
            };
        }

        public IList<object> GetTypeErasedNonProjectedObjects()
        {
            return new List<object>() {
                PrivateEnum.PrivateFirst,
                new PrivateStruct() { X = 1, Y = 2, Value = "Private" },
                new PrivateDelegate((uint value) => { })
            };
        }

        public IList<object> GetTypeErasedProjectedArrays()
        {
            return new List<object>() {
                new int[] {42, 24, 12 },
                new AsyncStatus[] { AsyncStatus.Canceled, AsyncStatus.Completed},
                new BasicEnum[] {BasicEnum.First, BasicEnum.Fourth },
                new BasicStruct[] {new BasicStruct() { X = 1, Y = 2, Value = "Basic" } },
                new BasicDelegate[] { new BasicDelegate((uint value) => {}) },
                new [] { new DisposableClass().GetType() , new NonProjectedDisposableClass().GetType() },
                new Type[] { typeof(TestClass), typeof(DisposableClass) },
            };
        }
    }

    [WindowsRuntimeClassName("AuthoringTest.DisposableClassImpl")]
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

    internal sealed partial class NonProjectedDisposableClass : IDisposable
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
            _dictionary = new Dictionary<string, BasicStruct>(StringComparer.Ordinal);
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

    [WindowsRuntimeClassName("AuthoringTest.CustomReadOnlyDictionaryImpl")]
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

    [WindowsRuntimeClassName("AuthoringTest.StaticClassImpl")]
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

        public static int Number { get; set; }

        internal static int Number2 { get; set; }

        public static event DoubleDelegate DelegateEvent;

        public static void FireDelegate(double value)
        {
            DelegateEvent?.Invoke(value);
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

    public sealed class CustomXamlServiceProvider : IServiceProvider
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

    public sealed class CustomNotifyDataErrorInfo2 : INotifyDataErrorInfo
    {
        bool INotifyDataErrorInfo.HasErrors => throw new NotImplementedException();

        event EventHandler<DataErrorsChangedEventArgs> INotifyDataErrorInfo.ErrorsChanged
        {
            add
            {
                throw new NotImplementedException();
            }

            remove
            {
                throw new NotImplementedException();
            }
        }

        IEnumerable INotifyDataErrorInfo.GetErrors(string propertyName)
        {
            throw new NotImplementedException();
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

    public sealed class CustomXamlMetadataProvider : IXamlMetadataProvider
    {
        // Tests DefaultOverload attribute specified in projected interface.
        public IXamlType GetXamlType(Type type)
        {
            if (type == typeof(Nullable<double>) ||
                type == typeof(TimeSpan?) ||
                type == typeof(BasicEnum?) ||
                type == typeof(FlagsEnum?))
            {
                return new XamlType(type);
            }

            return null;
        }

        public IXamlType GetXamlType(string fullName)
        {
            return null;
        }

        public XmlnsDefinition[] GetXmlnsDefinitions()
        {
            return null;
        }
    }

    internal sealed partial class XamlType : IXamlType
    {
        private readonly Type _type;

        public XamlType(Type type)
        {
            _type = type;
        }

        public IXamlType BaseType => new XamlType(_type.BaseType);

        public IXamlType BoxedType => throw new NotImplementedException();

        public IXamlMember ContentProperty => throw new NotImplementedException();

        public string FullName => _type.FullName;

        public bool IsArray => _type.IsArray;

        public bool IsBindable => throw new NotImplementedException();

        public bool IsCollection => throw new NotImplementedException();

        public bool IsConstructible => throw new NotImplementedException();

        public bool IsDictionary => throw new NotImplementedException();

        public bool IsMarkupExtension => throw new NotImplementedException();

        public IXamlType ItemType => throw new NotImplementedException();

        public IXamlType KeyType => throw new NotImplementedException();

        public Type UnderlyingType => throw new NotImplementedException();

        public object ActivateInstance()
        {
            throw new NotImplementedException();
        }

        public void AddToMap(object instance, object key, object value)
        {
            throw new NotImplementedException();
        }

        public void AddToVector(object instance, object value)
        {
            throw new NotImplementedException();
        }

        public object CreateFromString(string value)
        {
            throw new NotImplementedException();
        }

        public IXamlMember GetMember(string name)
        {
            throw new NotImplementedException();
        }

        public void RunInitializer()
        {
            throw new NotImplementedException();
        }
    }
    
    public sealed class SingleInterfaceClass : IDouble
    {
        private double _number;
        private DoubleDelegate _doubleDelegate;

        [Required(ErrorMessage = "Number is required")]
        public double Number { get => _number; set => _number = value; }

        public event DoubleDelegate DoubleDelegateEvent
        {
            add
            {
                _doubleDelegate += value;
            }

            remove
            {
                _doubleDelegate -= value;
            }
        }

        public double GetDouble()
        {
            return 4;
        }

        public double GetDouble(bool ignoreFactor)
        {
            return 4;
        }

        public string GetNumStr(int num)
        {
            return num.ToString();
        }

        public string GetNumStr(double num)
        {
            return num.ToString();
        }
    }

    public interface IDouble2
    {
        double GetDouble();
        string GetNumStr(int num);
        double Number { get; set; }
        event DoubleDelegate DoubleDelegateEvent;
    }

    public sealed class ExplicltlyImplementedClass : IDouble, IDouble2
    {
        private double _number;
        private DoubleDelegate _doubleDelegate;
        private DoubleDelegate _doubleDelegate2;

        double IDouble2.Number { get => _number * 2; set => _number = value * 2; }
        double IDouble.Number { get => _number; set => _number = value; }

        event DoubleDelegate IDouble.DoubleDelegateEvent
        {
            add
            {
                _doubleDelegate += value;
            }

            remove
            {
                _doubleDelegate -= value;
            }
        }

        event DoubleDelegate IDouble2.DoubleDelegateEvent
        {
            add
            {
                _doubleDelegate2 += value;
            }

            remove
            {
                _doubleDelegate2 -= value;
            }
        }

        public void TriggerEvent(double value)
        {
            _doubleDelegate?.Invoke(value);
            _doubleDelegate2?.Invoke(value * 2);
        }

        double IDouble.GetDouble()
        {
            return 4;
        }

        double IDouble.GetDouble(bool ignoreFactor)
        {
            return 4;
        }

        double IDouble2.GetDouble()
        {
            return 8;
        }

        string IDouble.GetNumStr(int num)
        {
            return num.ToString();
        }

        public string GetNumStr(int num)
        {
            return (num * 2).ToString();
        }
        string IDouble.GetNumStr(double num)
        {
            return num.ToString();
        }
    }

    public sealed class ObservableVector : IObservableVector<IDouble>
    {
        public IDouble this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public int Count => throw new NotImplementedException();

        public bool IsReadOnly => throw new NotImplementedException();

        public event VectorChangedEventHandler<IDouble> VectorChanged;

        public void Add(IDouble item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(IDouble item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(IDouble[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<IDouble> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public int IndexOf(IDouble item)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, IDouble item)
        {
            throw new NotImplementedException();
        }

        public bool Remove(IDouble item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }

    public interface IInterfaceInheritance : IDouble, IWwwFormUrlDecoderEntry
    {
        void SetNumber(double number);
    }

    public sealed class InterfaceInheritance : IInterfaceInheritance
    {
        private double _number;
        public double Number { get => _number; set => _number = value; }

        public string Name => "IInterfaceInheritance";

        public string Value => "InterfaceInheritance";

        public event DoubleDelegate DoubleDelegateEvent;

        public double GetDouble()
        {
            return 2;
        }

        public double GetDouble(bool ignoreFactor)
        {
            return 2.5;
        }

        public string GetNumStr(int num)
        {
            return num.ToString();
        }

        public string GetNumStr(double num)
        {
            return num.ToString();
        }

        public void SetNumber(double number)
        {
            Number = number;
        }
    }

    public sealed class MultipleInterfaceMappingClass : IList<DisposableClass>, IList
    {
        private List<DisposableClass> _list = new List<DisposableClass>();

        DisposableClass IList<DisposableClass>.this[int index] { get => _list[index]; set => _list[index] = value; }
        object IList.this[int index] { get => _list[index]; set => ((IList)_list)[index] = value; }

        int ICollection<DisposableClass>.Count => _list.Count;

        int ICollection.Count => _list.Count;

        bool ICollection<DisposableClass>.IsReadOnly => true;

        bool IList.IsReadOnly => true;

        bool IList.IsFixedSize => false;

        bool ICollection.IsSynchronized => true;

        object ICollection.SyncRoot => ((ICollection)_list).SyncRoot;

        void ICollection<DisposableClass>.Add(DisposableClass item)
        {
            _list.Add(item);
        }

        int IList.Add(object value)
        {
            return ((IList)_list).Add(value);
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
            return ((IList)_list).Contains(value);
        }

        void ICollection<DisposableClass>.CopyTo(DisposableClass[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        void ICollection.CopyTo(Array array, int index)
        {
            ((ICollection)_list).CopyTo(array, index);
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
            return ((IList)_list).IndexOf(value);
        }

        void IList<DisposableClass>.Insert(int index, DisposableClass item)
        {
            _list.Insert(index, item);
        }

        void IList.Insert(int index, object value)
        {
            ((IList)_list).Insert(index, value);
        }

        bool ICollection<DisposableClass>.Remove(DisposableClass item)
        {
            return _list.Remove(item);
        }

        void IList.Remove(object value)
        {
            ((IList)_list).Remove(value);
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
    
    public sealed class CustomDictionary2 : IDictionary<string, int>
    {
        private readonly Dictionary<string, int> _dictionary = new Dictionary<string, int>();

        int IDictionary<string, int>.this[string key] { get => _dictionary[key]; set => _dictionary[key] = value; }

        ICollection<string> IDictionary<string, int>.Keys => _dictionary.Keys;

        ICollection<int> IDictionary<string, int>.Values => _dictionary.Values;

        int ICollection<KeyValuePair<string, int>>.Count => _dictionary.Count;

        bool ICollection<KeyValuePair<string, int>>.IsReadOnly => false;

        void IDictionary<string, int>.Add(string key, int value)
        {
            _dictionary.Add(key, value);
        }

        void ICollection<KeyValuePair<string, int>>.Add(KeyValuePair<string, int> item)
        {
            ((ICollection<KeyValuePair<string, int>>)_dictionary).Add(item);
        }

        void ICollection<KeyValuePair<string, int>>.Clear()
        {
            _dictionary.Clear();
        }

        bool ICollection<KeyValuePair<string, int>>.Contains(KeyValuePair<string, int> item)
        {
            return _dictionary.Contains(item);
        }

        bool IDictionary<string, int>.ContainsKey(string key)
        {
            return _dictionary.ContainsKey(key);
        }

        void ICollection<KeyValuePair<string, int>>.CopyTo(KeyValuePair<string, int>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<string, int>>)_dictionary).CopyTo(array, arrayIndex);
        }

        IEnumerator<KeyValuePair<string, int>> IEnumerable<KeyValuePair<string, int>>.GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

        bool IDictionary<string, int>.Remove(string key)
        {
            return _dictionary.Remove(key);
        }

        bool ICollection<KeyValuePair<string, int>>.Remove(KeyValuePair<string, int> item)
        {
            return ((ICollection<KeyValuePair<string, int>>)_dictionary).Remove(item);
        }

        bool IDictionary<string, int>.TryGetValue(string key, out int value)
        {
            return _dictionary.TryGetValue(key, out value);
        }
    }

    public sealed class TestCollection : CollectionBase
    {
    }
    
    public partial interface IPartialInterface
    {
        public string GetNumberAsString();
    }

    partial interface IPartialInterface
    {
        public int Number2 { get; }
    }

    public sealed partial class PartialClass
    {
        private int _number;

        public int GetNumber()
        {
            return _number;
        }
    }

    public partial class PartialClass
    {
        public void SetNumber(int number)
        {
            _number = number;
        }

        private void Get()
        {
        }

        internal void Set()
        {
        }
    }

    partial class PartialClass
    {
        public int Number
        {
            get { return _number; }
        }

        public PartialClass()
        {
        }

        public PartialClass(int number)
        {
            _number = number;
        }
    }

    partial class PartialClass : IPartialInterface
    {
        public int Number2 => _number * 2;

        public string GetNumberAsString()
        {
            return _number.ToString();
        }

        public PartialStruct GetPartialStruct()
        {
            PartialStruct partialStruct;
            partialStruct.X = _number;
            partialStruct.Y = _number + 1;
            partialStruct.Z = _number + 2;
            return partialStruct;
        }
    }

    internal partial class PartialClass2
    {
        public void InternalFunction()
        {
        }
    }

    partial class PartialClass2
    {
        public void InternalFunction2()
        {
        }
    }

    public partial struct PartialStruct
    {
        public int X;
    }

    partial struct PartialStruct
    {
        public int Y;
    }

    public partial struct PartialStruct
    {
        public double Z;
    }

    // Nested type to validate (https://github.com/microsoft/CsWinRT/issues/1477)
    // Doesn't need to be consumed, we just want to verify the generator does work.
    internal partial class Nested1
    {
        internal partial record struct Nested2
        {
            internal partial struct Nested3
            {
                internal partial interface INested4
                {
                    internal partial record Nested5
                    {
                        internal partial class InnerMostType : IGraphicsEffectSource, IPublicInterface, IDisposable
                        {
                            public string HelloWorld()
                            {
                                return "Hello from mixed WinRT/COM";
                            }

                            public void Dispose()
                            {
                            }
                        }
                    }
                }
            }
        }
    }

    /*
    public sealed class TestMixedWinRTCOMWrapper : IGraphicsEffectSource, IPublicInterface, IInternalInterface1, SomeInternalType.IInternalInterface2
    {
        public string HelloWorld()
        {
            return "Hello from mixed WinRT/COM";
        }

        unsafe int IInternalInterface1.GetNumber(int* value)
        {
            *value = 42;

            return 0;
        }

        unsafe int SomeInternalType.IInternalInterface2.GetNumber(int* value)
        {
            *value = 123;

            return 0;
        }
    }
    */

    public interface IPublicInterface
    {
        string HelloWorld();
    }

    [System.Runtime.InteropServices.Guid("26D8EE57-8B1B-46F4-A4F9-8C6DEEEAF53A")]
    public interface ICustomInterfaceGuid
    {
        string HelloWorld();
    }

    public sealed class CustomInterfaceGuidClass : ICustomInterfaceGuid
    {
        public string HelloWorld() => "Hello World!";
    }

    public sealed class NonActivatableType
    {
        private readonly string _text;

        // This should not be referenced by the generated activation factory
        internal NonActivatableType(string text)
        {
            _text = text;
        }

        public string GetText()
        {
            return _text;
        }
    }

    [WindowsRuntimeClassName("AuthoringTest.NonActivatableFactoryImpl")]
    public static class NonActivatableFactory
    {
        public static NonActivatableType Create()
        {
            return new("Test123");
        }
    }

    public sealed class TypeOnlyActivatableViaItsOwnFactory
    {
        private readonly string _text;

        private TypeOnlyActivatableViaItsOwnFactory(string text)
        {
            _text = text;
        }

        public static TypeOnlyActivatableViaItsOwnFactory Create()
        {
            return new("Hello!");
        }

        public string GetText()
        {
            return _text;
        }
    }
}

namespace AnotherNamespace
{
    internal partial class PartialClass3
    {
        public void InternalFunction()
        {
        }
    }

    partial class PartialClass3
    {
        public void InternalFunction2()
        {
        }
    }

    internal class InternalClass
    {
        public void InternalFunction()
        {
        }
    }

    // Out parameters on interface methods
    public interface IOutParams
    {
        void GetData(out string result);
        void GetStruct(out BasicStruct result);
        bool TryParse(string input, out int value);
    }

    // Nullable/IReference parameters
    public sealed class NullableParamClass
    {
        public int? NullableIntProp { get; set; }
        public double? NullableDoubleProp { get; set; }
        public bool? NullableBoolProp { get; set; }

        public int GetValueOrDefault(int? value, int defaultValue)
        {
            return value ?? defaultValue;
        }

        public int? TryGetValue(string key)
        {
            return null;
        }
    }

    // Mapped type parameters (DateTimeOffset, TimeSpan, Uri, Exception)
    public sealed class MappedTypeParamClass
    {
        public DateTimeOffset GetTimestamp()
        {
            return DateTimeOffset.UtcNow;
        }

        public void SetTimestamp(DateTimeOffset timestamp) { }

        public TimeSpan GetDuration()
        {
            return TimeSpan.FromSeconds(1);
        }

        public void SetDuration(TimeSpan duration) { }

        public Uri GetUri()
        {
            return new Uri("https://example.com");
        }

        public void SetUri(Uri uri) { }

        public string FormatTimestamp(DateTimeOffset timestamp, TimeSpan offset)
        {
            return (timestamp + offset).ToString();
        }
    }

    // Mixed Span and regular array parameters
    public sealed class MixedArrayClass
    {
        public void CopyToSpan(ReadOnlySpan<int> source, Span<int> destination)
        {
            source.CopyTo(destination);
        }

        public int[] TransformArray(ReadOnlySpan<int> input)
        {
            return input.ToArray();
        }

        public void FillWithIndex(Span<int> buffer)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = i;
            }
        }

        public void ProcessAndOutput(ReadOnlySpan<BasicStruct> input, out BasicStruct[] output)
        {
            output = input.ToArray();
        }
    }

    // Classes implementing IDisposable + custom interface
    public interface ICustomResource
    {
        string Name { get; }
        void Reset();
    }

    public sealed class DisposableResource : IDisposable, ICustomResource
    {
        public string Name => "Resource";
        public void Reset() { }
        public void Dispose() { }
    }

    // Multiple constructors (3+)
    public sealed class MultiConstructorClass
    {
        private readonly string _name;
        private readonly int _value;
        private readonly BasicStruct _data;

        public MultiConstructorClass()
        {
            _name = "";
            _value = 0;
            _data = default;
        }

        public MultiConstructorClass(string name)
        {
            _name = name;
            _value = 0;
            _data = default;
        }

        public MultiConstructorClass(string name, int value)
        {
            _name = name;
            _value = value;
            _data = default;
        }

        public MultiConstructorClass(string name, int value, BasicStruct data)
        {
            _name = name;
            _value = value;
            _data = data;
        }

        public string Name => _name;
        public int Value => _value;
        public BasicStruct Data => _data;
    }

    // Static properties returning complex types
    public sealed class StaticComplexProps
    {
        public static string DefaultName => "Default";
        public static BasicStruct DefaultStruct => new() { X = 1, Y = 2 };
        public static int MaxCount { get; set; } = 100;
    }

    // Multiple overloaded methods with DefaultOverload
    public sealed class OverloadedMethodClass
    {
        [Windows.Foundation.Metadata.DefaultOverload()]
        public string Format(int value)
        {
            return value.ToString();
        }

        public string Format(double value)
        {
            return value.ToString("F2");
        }

        public string Format(string value)
        {
            return value;
        }

        [Windows.Foundation.Metadata.DefaultOverload()]
        public static int Parse(string text)
        {
            return int.Parse(text);
        }

        public static int Parse(string text, int radix)
        {
            return Convert.ToInt32(text, radix);
        }
    }

    // Nested structs (2 levels)
    public struct InnerStruct
    {
        public int A;
        public int B;
    }

    public struct OuterStruct
    {
        public InnerStruct Inner;
        public int C;
    }

    // Delegates with 3+ parameters and struct params
    public delegate void MultiParamDelegate(int a, string b, double c);
    public delegate bool StructParamDelegate(BasicStruct data, int index);
    public delegate BasicStruct StructReturnDelegate(string name);

    // Flags enum edge cases
    [Flags]
    public enum DetailedFlags : uint
    {
        None = 0,
        Read = 1,
        Write = 2,
        Execute = 4,
        ReadWrite = Read | Write,
        All = Read | Write | Execute
    }

    // Enum without Flags (signed)
    public enum Priority
    {
        Low = -1,
        Normal = 0,
        High = 1,
        Critical = 2
    }

    // IAsyncAction (no return value)
    public sealed class AsyncMethodClass
    {
        public Windows.Foundation.IAsyncAction DoWorkAsync()
        {
            return Task.CompletedTask.AsAsyncAction();
        }

        public Windows.Foundation.IAsyncOperation<int> ComputeAsync()
        {
            return Task.FromResult(42).AsAsyncOperation();
        }
    }

    // Version attribute on methods/properties
    public interface IVersionedInterface
    {
        [Windows.Foundation.Metadata.Version(1u)]
        void OriginalMethod();

        [Windows.Foundation.Metadata.Version(2u)]
        void NewerMethod();

        [Windows.Foundation.Metadata.Version(1u)]
        string Name { get; }

        [Windows.Foundation.Metadata.Version(2u)]
        int Count { get; }
    }

    // Deprecated members
    public sealed class DeprecatedMembersClass
    {
        [Windows.Foundation.Metadata.Deprecated("Use NewMethod instead", Windows.Foundation.Metadata.DeprecationType.Deprecate, 1u)]
        public void OldMethod() { }

        public void NewMethod() { }

        [Windows.Foundation.Metadata.Deprecated("Use NewProp instead", Windows.Foundation.Metadata.DeprecationType.Deprecate, 1u)]
        public string OldProp => "";

        public string NewProp => "";
    }

    // Class implementing INotifyPropertyChanged + custom interface
    public sealed class NotifyWithCustomInterface : System.ComponentModel.INotifyPropertyChanged, ICustomResource
    {
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        public string Name { get; set; } = "";

        public void Reset()
        {
            Name = "";
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Name)));
        }
    }

    // Class with both factory and static members
    public sealed class FactoryAndStaticClass
    {
        private readonly string _id;

        public FactoryAndStaticClass(string id)
        {
            _id = id;
        }

        public FactoryAndStaticClass(string id, int version)
        {
            _id = $"{id}_v{version}";
        }

        public string Id => _id;

        public static string DefaultId => "default";
        public static FactoryAndStaticClass CreateDefault()
        {
            return new FactoryAndStaticClass(DefaultId);
        }
    }

    // Interface with properties, methods, and events combined
    public interface IFullFeaturedInterface
    {
        string Name { get; set; }
        int Count { get; }

        void DoWork();
        string GetData(int index);

        event EventHandler<string> DataChanged;
    }

    public sealed class FullFeaturedClass : IFullFeaturedInterface
    {
        public string Name { get; set; } = "";
        public int Count => 0;

        public void DoWork() { }
        public string GetData(int index) => "";

        public event EventHandler<string> DataChanged;

        public void RaiseDataChanged()
        {
            DataChanged?.Invoke(this, "changed");
        }
    }

    // Contract versioning
    [Windows.Foundation.Metadata.ApiContract]
    public enum AnotherNamespaceContract { }

    [Windows.Foundation.Metadata.ContractVersion(typeof(AnotherNamespaceContract), 1u)]
    public sealed class ContractVersionedClass
    {
        public string Name { get; set; } = "";
    }

    [Windows.Foundation.Metadata.ContractVersion(typeof(AnotherNamespaceContract), 2u)]
    public sealed class ContractVersionedClassV2
    {
        public string Name { get; set; } = "";
        public int Count { get; set; }
    }

    // Class evolving across contract versions with versioned members and interfaces
    public interface IContractVersionedMembersV1
    {
        string TrackName { get; }
    }

    [Windows.Foundation.Metadata.ContractVersion(typeof(AnotherNamespaceContract), 2u)]
    public interface IContractVersionedMembersV2
    {
        int Volume { get; }
    }

    [Windows.Foundation.Metadata.ContractVersion(typeof(AnotherNamespaceContract), 1u)]
    public sealed class ContractVersionedMembersClass : IContractVersionedMembersV1, IContractVersionedMembersV2
    {
        public string TrackName { get; set; } = "";

        [Windows.Foundation.Metadata.ContractVersion(typeof(AnotherNamespaceContract), 2u)]
        public int Volume { get; set; }

        [Windows.Foundation.Metadata.ContractVersion(typeof(AnotherNamespaceContract), 2u)]
        public string GetNowPlaying() => $"{TrackName} (Vol={Volume})";

        [Windows.Foundation.Metadata.ContractVersion(typeof(AnotherNamespaceContract), 2u)]
        public event System.EventHandler<string> TrackChanged;

        public void RaiseTrackChanged()
        {
            TrackChanged?.Invoke(this, TrackName);
        }
    }

    // Class evolving across Version attribute versions with versioned members and interfaces
    public interface IVersionedMembersV1
    {
        string Message { get; }
    }

    [Windows.Foundation.Metadata.Version(2u)]
    public interface IVersionedMembersV2
    {
        double Urgency { get; }
    }

    [Windows.Foundation.Metadata.Version(1u)]
    public sealed class VersionedMembersClass : IVersionedMembersV1, IVersionedMembersV2
    {
        public string Message { get; set; } = "";

        [Windows.Foundation.Metadata.Version(2u)]
        public double Urgency { get; set; }

        [Windows.Foundation.Metadata.Version(2u)]
        public string Format() => $"{Message}: {Urgency}";

        [Windows.Foundation.Metadata.Version(2u)]
        public event System.EventHandler<double> UrgencyChanged;

        public void RaiseUrgencyChanged()
        {
            UrgencyChanged?.Invoke(this, Urgency);
        }
    }
}
