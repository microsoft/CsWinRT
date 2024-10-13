﻿using System;
using TestComponentCSharp;
using WinRT.Interop;
using WinRT;
using System.Collections.Generic;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Runtime.InteropServices;
using System.Collections.Specialized;
using Windows.Foundation;
using Windows.Web.Http;

var managedProperties = new ManagedProperties(42);
var instance = new Class();

// Ensure we can use the IProperties interface from the native side.
instance.CopyProperties(managedProperties);
if (managedProperties.ReadWriteProperty != instance.ReadWriteProperty)
{
    return 101;
}

// Check for the default interfaces provided by WinRT.Runtime
Guid IID_IMarshal = new("00000003-0000-0000-c000-000000000046");
IObjectReference ccw = MarshalInterface<IProperties1>.CreateMarshaler(managedProperties);
ccw.TryAs<IUnknownVftbl>(IID_IMarshal, out var marshalCCW);
if (marshalCCW == null)
{
    return 102;
}

// Check for managed implemented interface to ensure not trimmed.
Guid IID_IUriHandler = new("FF4B4334-2104-537D-812E-67E3856AC7A2");
ccw.TryAs<IUnknownVftbl>(IID_IUriHandler, out var uriHandlerCCW);
if (uriHandlerCCW == null)
{
    return 103;
}

if (!CheckRuntimeClassName(ccw, "TestComponentCSharp.IProperties1"))
{
    return 119;
}

// Ensure that interfaces on the vtable / object don't get trimmed even if unused.
Guid IID_IWarning1 = new("4DB3FA26-4BB1-50EA-8362-98F49651E516");
Guid IID_IWarningClassOverrides = new("E5635CE4-D483-55AA-86D5-080DC07F0A09");
Guid IID_IArtist = new("B7233F79-63CF-5AFA-A026-E4F1924F17A1");

var managedWarningClass = new ManagedWarningClass();
ccw = MarshalInterface<IUriHandler>.CreateMarshaler(managedWarningClass);
ccw.TryAs<IUnknownVftbl>(IID_IWarning1, out var warningCCW);
if (warningCCW == null)
{
    return 104;
}

ccw.TryAs<IUnknownVftbl>(IID_IWarningClassOverrides, out var warningOverrideCCW);
if (warningOverrideCCW == null)
{
    return 105;
}

ccw.TryAs<IUnknownVftbl>(IID_IArtist, out var artistCCW);
if (artistCCW == null)
{
    return 106;
}

// Testing for overrided name using attribute specified by author on type.
if (!CheckRuntimeClassName(ccw, "ManagedWarningClass"))
{
    return 120;
}

var managedWarningClass2 = new ManagedWarningClass2();
ccw = MarshalInspectable<object>.CreateMarshaler(managedWarningClass2);
ccw.TryAs<IUnknownVftbl>(IID_IWarning1, out var warningCCW2);
if (warningCCW2 == null)
{
    return 107;
}

ccw.TryAs<IUnknownVftbl>(IID_IWarningClassOverrides, out var warningOverrideCCW2);
if (warningOverrideCCW2 == null)
{
    return 108;
}

Guid IID_IProperties1 = new("4BB22177-718B-57C4-8977-CDF2621C781A");
Guid IID_IProperties2 = new("6090AE4B-83A1-5474-A8D9-AF9B8C8DBD09");
var managedInterfaceInheritance = new ManagedInterfaceInheritance();
ccw = MarshalInspectable<object>.CreateMarshaler(managedInterfaceInheritance);
ccw.TryAs<IUnknownVftbl>(IID_IProperties1, out var propertiesCCW);
if (propertiesCCW == null)
{
    return 109;
}

ccw.TryAs<IUnknownVftbl>(IID_IProperties2, out var properties2CCW);
if (properties2CCW == null)
{
    return 110;
}

if (!CheckRuntimeClassName(ccw, "TestComponentCSharp.IProperties2"))
{
    return 121;
}

Guid IID_IlistInt = new("B939AF5B-B45D-5489-9149-61442C1905FE");
Guid IID_IEnumerable = new("036D2C08-DF29-41AF-8AA2-D774BE62BA6F");
var intList = new ManagedIntList();
ccw = MarshalInspectable<object>.CreateMarshaler(intList);
ccw.TryAs<IUnknownVftbl>(IID_IlistInt, out var listIntCCW);
if (listIntCCW == null)
{
    return 111;
}

ccw.TryAs<IUnknownVftbl>(IID_IEnumerable, out var enumerableCCW);
if (enumerableCCW == null)
{
    return 112;
}

if (!CheckRuntimeClassName(ccw, "Windows.Foundation.Collections.IVector`1<Int32>"))
{
    return 122;
}

Guid IID_IEnumerableDerived = new ("A70EC662-9975-51BB-9A28-82A876E01177");
Guid IID_IEnumerableComposed = new ("BDCEC2FC-5BBE-5A69-989D-222563A811A6");
Guid IID_IEnumerableIRequiredTwo = new ("10879613-0953-58AC-A6C0-817E28DD5A25");
var derivedList = new ManagedDerivedList();
ccw = MarshalInspectable<object>.CreateMarshaler(derivedList);
ccw.TryAs<IUnknownVftbl>(IID_IEnumerableDerived, out var enumerableDerived);
if (enumerableDerived == null)
{
    return 113;
}

ccw.TryAs<IUnknownVftbl>(IID_IEnumerableComposed, out var enumerableComposed);
if (enumerableComposed == null)
{
    return 114;
}

ccw.TryAs<IUnknownVftbl>(IID_IEnumerableIRequiredTwo, out var enumerableRequiredTwo);
if (enumerableRequiredTwo == null)
{
    return 115;
}

if (!CheckRuntimeClassName(ccw, "Windows.Foundation.Collections.IVector`1<TestComponent.Derived>"))
{
    return 123;
}

var nestedClass = TestClass2.GetInstance();
ccw = MarshalInspectable<object>.CreateMarshaler(nestedClass);
ccw.TryAs<IUnknownVftbl>(IID_IProperties2, out properties2CCW);
if (properties2CCW == null)
{
    return 116;
}

var genericNestedClass = TestClass2.GetGenericInstance();
ccw = MarshalInspectable<object>.CreateMarshaler(genericNestedClass);
ccw.TryAs<IUnknownVftbl>(IID_IProperties2, out properties2CCW);
if (properties2CCW == null)
{
    return 117;
}

var managedWarningClassList = new List<ManagedWarningClass>();
instance.BindableIterableProperty = managedWarningClassList;

var notifyCollectionChangedActionList = new List<NotifyCollectionChangedAction>();
instance.BindableIterableProperty = notifyCollectionChangedActionList;

var nullableDoubleList = new List<double?>();
instance.BindableIterableProperty = nullableDoubleList;

var nullableDoubleList2 = new List<System.Nullable<double>>();
instance.BindableIterableProperty = nullableDoubleList2;

var nullableHandleList = new List<GCHandle?>();
instance.BindableIterableProperty = nullableHandleList;

var customCommand = new CustomCommand() as ICommand;
ccw = MarshalInspectable<object>.CreateMarshaler(customCommand);
ccw.TryAs<IUnknownVftbl>(ABI.System.Windows.Input.ICommandMethods.IID, out var commandCCW);
if (commandCCW == null)
{
    return 118;
}

if (!CheckRuntimeClassName(ccw, "Microsoft.UI.Xaml.Input.ICommand"))
{
    return 124;
}

TestClass.TestNestedClass();

// These scenarios aren't supported today on AOT, but testing to ensure they
// compile without issues.  They should still work fine outside of AOT.
try
{
    TestClass.TestGenericList<bool>();
}
catch(Exception)
{
    if (RuntimeFeature.IsDynamicCodeCompiled)
    {
        throw;
    }
}


// Test ICustomProperty
Language language = new Language();
language.Value = 42;
language[1] = "Bindable";

// Used for non-indexer types to avoid passing null.
// Note this isn't checked in non-indexer scenarios.
var ignoredType = typeof(Language);
if (!instance.ValidateBindableProperty(language, "Name", ignoredType, false, true, false, false, typeof(string), null, null, out var retrievedValue) ||
    !instance.ValidateBindableProperty(language, "Value", ignoredType, false, true, true, false, typeof(int), null, 22, out var retrievedValue2) ||
    !instance.ValidateBindableProperty(language, "Item", typeof(int), false, true, true, true, typeof(string), 1, "Language", out var retrievedValue3))
{
    return 125;
}

// Validate previous values
if ((string)retrievedValue != "Language" || (int)retrievedValue2 != 42 || (string)retrievedValue3 != "Bindable")
{
    return 126;
}

// Validate if values got set during ValidateBindableProperty via ICustomProperty
if (language.Value != 22 || language[1] != "Language")
{
    return 127;
}

Language2 language2 = new Language2();
// Test private property not found
if (instance.ValidateBindableProperty(language2, "Number", ignoredType, true, true, true, false, typeof(int), null, null, out _))
{
    return 128;
}

// Test private accessors not found
if (!instance.ValidateBindableProperty(language2, "SetOnly", ignoredType, false, false, true, false, typeof(string), null, "One", out _) ||
    !instance.ValidateBindableProperty(language2, "PrivateSet", ignoredType, false, true, false, false, typeof(string), null, "Two", out var retrievedValue4) ||
    !instance.ValidateBindableProperty(language2, "StaticDouble", ignoredType, false, true, true, false, typeof(double), null, 5.0, out var retrievedValue11))
{
    return 129;
}

// Set during SetOnly call.
if ((string)retrievedValue4 != "One" ||
    (double)retrievedValue11 != 4.0 || Language2.StaticDouble != 5.0)
{
    return 130;
}

Language4 language4 = new Language4();
// Test internal property not found
if (instance.ValidateBindableProperty(language4, "Name", ignoredType, true, true, false, false, typeof(string), null, null, out _))
{
    return 131;
}

// Validate generic scenarios
Language5<int> language5 = new Language5<int>();
language5.Value = 5;
if (!instance.ValidateBindableProperty(language5, "Value", ignoredType, false, true, true, false, typeof(int), null, 2, out var retrievedValue5))
{
    return 132;
}

if ((int)retrievedValue5 != 5 || language5.Value != 2)
{
    return 133;
}

Language5<object> language6 = new Language5<object>();
language6.Value = language2;
language6.Number = 4;
if (!instance.ValidateBindableProperty(language6, "Value", ignoredType, false, true, true, false, typeof(object), null, language, out var retrievedValue6) ||
    !instance.ValidateBindableProperty(language6, "Number", ignoredType, false, true, true, false, typeof(int), null, 2, out var retrievedValue7))
{
    return 133;
}

if (retrievedValue6 != language2 || language6.Value != language ||
    (int)retrievedValue7 != 4 || language6.Number != 2)
{
    return 134;
}

// Validate dervied scenarios
LanguageDervied languageDervied = new LanguageDervied();
languageDervied.Value = 22;
LanguageDervied2 languageDervied2 = new LanguageDervied2();
languageDervied2.Value = 11;
languageDervied2.Derived = 22;

if (!instance.ValidateBindableProperty(languageDervied, "Derived", ignoredType, false, true, false, false, typeof(int), null, null, out var retrievedValue8) ||
    // Not projected as custom property
    instance.ValidateBindableProperty(languageDervied, "Value", ignoredType, true, true, true, false, typeof(int), null, 33, out var _) ||
    !instance.ValidateBindableProperty(languageDervied2, "Derived", ignoredType, false, true, true, false, typeof(int), null, 2, out var retrievedValue9) ||
    !instance.ValidateBindableProperty(languageDervied2, "Name", ignoredType, false, true, false, false, typeof(string), null, null, out var retrievedValue10))
{
    return 135;
}

if ((int)retrievedValue8 != 4 ||
    (int)retrievedValue9 != 22 || languageDervied2.Derived != 2 ||
    (string)retrievedValue10 != "Language")
{
    return 136;
}

return 100;


[DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CallingConvention = CallingConvention.StdCall)]
static extern unsafe char* WindowsGetStringRawBuffer(IntPtr hstring, uint* length);

[DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CallingConvention = CallingConvention.StdCall)]
static extern int WindowsDeleteString(IntPtr hstring);

unsafe bool CheckRuntimeClassName(IObjectReference objRef, string expected)
{
    objRef.TryAs<IInspectable.Vftbl>(IID.IID_IInspectable, out var inspectable);
    if (inspectable == null)
    {
        return false;
    }

    IntPtr __retval = default;
    try
    {
        var hr = inspectable.Vftbl.GetRuntimeClassName(inspectable.ThisPtr, &__retval);
        if (hr != 0)
        {
            return false;
        }

        uint length;
        char* buffer = WindowsGetStringRawBuffer(__retval, &length);
        return expected == new string(buffer, 0, (int)length);
    }
    finally
    {
        WindowsDeleteString(__retval);
    }
}

sealed partial class ManagedProperties : IProperties1, IUriHandler
{
    private readonly int _value;

    public ManagedProperties(int value)
    {
        _value = value;
    }

    public int ReadWriteProperty => _value;

    public void AddUriHandler(ProvideUri provideUri)
    {
        _ = provideUri();
    }

    void IUriHandler.AddUriHandler(ProvideUri provideUri) => AddUriHandler(provideUri);
}

[WinRTRuntimeClassName("ManagedWarningClass")]
sealed partial class ManagedWarningClass : WarningClass, IUriHandler, IArtist
{
    public int Test => 4;

    public void AddUriHandler(ProvideUri provideUri)
    {
        _ = provideUri();
    }

    public void Draw()
    {
    }

    public void Draw(int _)
    {
    }

    public int DrawTo()
    {
        return 0;
    }

    void IUriHandler.AddUriHandler(ProvideUri provideUri) => AddUriHandler(provideUri);
}

// Used to test interfaces on base class where
// the child class has no WinRT interfaces.
sealed partial class ManagedWarningClass2 : WarningClass 
{
}

sealed partial class ManagedInterfaceInheritance : IProperties2
{
    private int _value;

    public int ReadWriteProperty { get => _value; set => _value = value; }
}

sealed partial class ManagedIntList : IList<int>
{
    public int this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public int Count => throw new NotImplementedException();

    public bool IsReadOnly => throw new NotImplementedException();

    public void Add(int item)
    {
        throw new NotImplementedException();
    }

    public void Clear()
    {
        throw new NotImplementedException();
    }

    public bool Contains(int item)
    {
        throw new NotImplementedException();
    }

    public void CopyTo(int[] array, int arrayIndex)
    {
        throw new NotImplementedException();
    }

    public IEnumerator<int> GetEnumerator()
    {
        throw new NotImplementedException();
    }

    public int IndexOf(int item)
    {
        throw new NotImplementedException();
    }

    public void Insert(int index, int item)
    {
        throw new NotImplementedException();
    }

    public bool Remove(int item)
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

sealed partial class ManagedDerivedList : IList<TestComponent.Derived>
{
    public TestComponent.Derived this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public int Count => throw new NotImplementedException();

    public bool IsReadOnly => throw new NotImplementedException();

    public void Add(TestComponent.Derived item)
    {
        throw new NotImplementedException();
    }

    public void Clear()
    {
        throw new NotImplementedException();
    }

    public bool Contains(TestComponent.Derived item)
    {
        throw new NotImplementedException();
    }

    public void CopyTo(TestComponent.Derived[] array, int arrayIndex)
    {
        throw new NotImplementedException();
    }

    public IEnumerator<TestComponent.Derived> GetEnumerator()
    {
        throw new NotImplementedException();
    }

    public int IndexOf(TestComponent.Derived item)
    {
        throw new NotImplementedException();
    }

    public void Insert(int index, TestComponent.Derived item)
    {
        throw new NotImplementedException();
    }

    public bool Remove(TestComponent.Derived item)
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

sealed class TestClass
{
    // Testing various nested and generic classes on vtable lookup table.
    public static void TestNestedClass()
    {
        var instance = new Class();
        var nestedClassList = new List<NestedClass>();
        instance.BindableIterableProperty = nestedClassList;

        var nestedClassList2 = new List<NestedClass.NestedClass2>();
        instance.BindableIterableProperty = nestedClassList2;

        var nestedClassList3 = new List<NestedGenericClass<int>>();
        instance.BindableIterableProperty = nestedClassList3;

        var nestedClassList4 = new List<NestedGenericClass<int>.NestedClass2>();
        instance.BindableIterableProperty = nestedClassList4;

        var nestedClassList5 = new List<NestedGenericClass<NestedGenericClass<int>.NestedClass2>.NestedClass2>();
        instance.BindableIterableProperty = nestedClassList5;

        var nestedClassList6 = new List<NestedGenericClass<int>.NestedClass3<double>>();
        instance.BindableIterableProperty = nestedClassList6;
    }

    public static void TestGenericList<T>()
    {
        var instance = new Class();
        var nestedClassList = new List<T>();
        instance.BindableIterableProperty = nestedClassList;
    }

#pragma warning disable CsWinRT1028 // Class is not marked partial
    sealed class NestedClass : IProperties2
    {
        private int _value;
        public int ReadWriteProperty { get => _value; set => _value = value; }

        internal sealed class NestedClass2 : IProperties2
        {
            private int _value;
            public int ReadWriteProperty { get => _value; set => _value = value; }
        }
    }

    sealed class NestedGenericClass<T> : IProperties2
    {
        private int _value;
        public int ReadWriteProperty { get => _value; set => _value = value; }

        internal sealed class NestedClass2 : IProperties2
        {
            private int _value;
            public int ReadWriteProperty { get => _value; set => _value = value; }
        }

        internal sealed class NestedClass3<S> : IProperties2
        {
            private int _value;
            public int ReadWriteProperty { get => _value; set => _value = value; }
        }
    }
#pragma warning restore CsWinRT1028 // Class is not marked partial
}

partial class TestClass2
{
    private partial class NestedTestClass : IProperties2
    {
        private int _value;
        public int ReadWriteProperty { get => _value; set => _value = value; }
    }

    // Implements non WinRT generic interface to test WinRTExposedType attribute
    // generated during these scenarios.
    private partial class GenericNestedTestClass<T> : IProperties2, IComparer<T>
    {
        private int _value;
        public int ReadWriteProperty { get => _value; set => _value = value; }

#nullable enable
        public int Compare(T? x, T? y)
        {
            return 1;
        }
#nullable restore
    }

    internal static IProperties2 GetInstance()
    {
        return new NestedTestClass();
    }

    internal static IProperties2 GetGenericInstance()
    {
        return new GenericNestedTestClass<int>();
    }
}

class TestClass3
{
    // Making sure it compiles if the parent class isn't partial, but the actual class is.
#pragma warning disable CsWinRT1028 // Class is not marked partial
    partial class NestedTestClass2 : IProperties2
#pragma warning restore CsWinRT1028 // Class is not marked partial
    {
        private int _value;
        public int ReadWriteProperty { get => _value; set => _value = value; }
    }
}
sealed partial class CustomCommand : ICommand
{
    public event EventHandler CanExecuteChanged;

    public bool CanExecute(object parameter)
    {
        throw new NotImplementedException();
    }

    public void Execute(object parameter)
    {
        throw new NotImplementedException();
    }
}

[GeneratedBindableCustomProperty([nameof(Name), nameof(Value)], [typeof(int)])]
partial class Language
{
    private readonly string[] _values = new string[4];

    public string Name { get; init; } = "Language";
    public int Value { get; set; }
    public string this[int i]
    {
        get => _values[i];
        set => _values[i] = value;
    }
}

[global::WinRT.GeneratedBindableCustomProperty([nameof(Name), nameof(Derived)], [typeof(int)])]
partial class LanguageDervied : Language
{
    public int Derived { get; } = 4;
}

[WinRT.GeneratedBindableCustomProperty]
partial class LanguageDervied2 : Language
{
    public int Derived { get; set; }
}

// Testing code compiles when not marked partial
[GeneratedBindableCustomProperty]
#pragma warning disable CsWinRT1028 // Class is not marked partial
class LanguageDervied3 : Language
#pragma warning restore CsWinRT1028 // Class is not marked partial
{
    public int Derived { get; set; }
}

class ParentClass
{
    // Testing code compiles when not marked partial
    [GeneratedBindableCustomProperty]
#pragma warning disable CsWinRT1028 // Class is not marked partial
    partial class LanguageDervied3 : Language
#pragma warning restore CsWinRT1028 // Class is not marked partial
    {
        public int Derived { get; set; }
    }
}


[GeneratedBindableCustomPropertyAttribute]
sealed partial class Language2
{
    public string Name { get; } = "Language2";
    public string[] Value { get; set; }
    private int Number { get; set; }
    public string SetOnly
    {
        set 
        { 
            PrivateSet = value; 
        }
    }
    public string PrivateSet { get; private set; } = "PrivateSet";
    public static double StaticDouble { get; set; } = 4.0;
}

[GeneratedBindableCustomProperty]
sealed partial class Language4
{
    internal string Name { get; }
    private int Number { get; set; }
}

[GeneratedBindableCustomProperty]
sealed partial class Language5<T>
{
    private readonly Dictionary<T, T> _values = new();

    public T Value { get; set; }
    public int Number { get; set; }
    public T this[T i]
    {
        get => _values[i];
        set => _values[i] = value;
    }
}

namespace Test
{
    namespace Test2
    {
        sealed partial class Nested
        {
            [GeneratedBindableCustomProperty([nameof(Value)], [])]
            sealed partial class Language3 : IProperties2
            {
                private readonly string[] _values = new string[4];

                public string Name { get; }
                public int Value { get; set; }
                public string this[int i]
                {
                    get => _values[i];
                    set => _values[i] = value;
                }
                private int Number { get; set; }
                public int ReadWriteProperty { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            }
        }
    }
}