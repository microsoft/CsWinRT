#include "pch.h"
#include "Class.h"
#include "Class.g.cpp"

using namespace std::chrono;

using namespace winrt;
using namespace Windows::Foundation;
namespace WF = Windows::Foundation;
using namespace Collections;
using namespace Microsoft::UI::Xaml::Data;
using namespace Microsoft::UI::Xaml::Interop;
using Windows::UI::Xaml::Interop::TypeName;

namespace winrt::TestComponentCSharp::implementation
{
    namespace statics
    {
        static int _int {};
        static winrt::event<EventHandler<int32_t>> _intChanged {};
        static winrt::hstring _string;
        static winrt::event<TypedEventHandler<TestComponentCSharp::Class, hstring>> _stringChanged {};
        static int _readWrite{};
    }

    template <typename K, typename V>
    struct key_value_pair : implements<key_value_pair<K, V>, IKeyValuePair<K, V>>
    {
        key_value_pair(K key, V value) :
            m_key(std::move(key)),
            m_value(std::move(value))
        {
        }

        K Key() const
        {
            return m_key;
        }

        V Value() const
        {
            return m_value;
        }

    private:

        K m_key;
        V m_value;
    };
	
    struct bindable_vector : winrt::implements<bindable_vector, IBindableVector, IBindableIterable>
    {
        IBindableVector _wrapped;

        bindable_vector(IBindableVector wrapped)
        {
            _wrapped = wrapped;
        }

        IBindableIterator First() const
        {
            return _wrapped.First();
        }

        WF::IInspectable GetAt(uint32_t index) const
        {
            return _wrapped.GetAt(index);
        }
        uint32_t Size() const
        {
            return _wrapped.Size();
        }
        IBindableVectorView GetView() const
        {
            return _wrapped.GetView();
        }
        bool IndexOf(WF::IInspectable const& value, uint32_t& index) const
        {
            return _wrapped.IndexOf(value, index);
        }
        void SetAt(uint32_t index, WF::IInspectable const& value) const
        {
            _wrapped.SetAt(index, value);
        }
        void InsertAt(uint32_t index, WF::IInspectable const& value) const
        {
            _wrapped.InsertAt(index, value);
        }
        void RemoveAt(uint32_t index) const
        {
            _wrapped.RemoveAt(index);
        }
        void Append(WF::IInspectable const& value) const
        {
            _wrapped.Append(value);
        }
        void RemoveAtEnd() const
        {
            _wrapped.RemoveAtEnd();
        }
        void Clear() const
        {
            _wrapped.Clear();
        }
    };

    struct bindable_observable_vector : winrt::implements<bindable_observable_vector, IBindableObservableVector, IBindableIterable, IBindableVector>
    {
        IBindableObservableVector _wrapped;

        bindable_observable_vector(IBindableObservableVector wrapped)
        {
            _wrapped = wrapped;
        }

        IBindableIterator First()
        {
            return _wrapped.First();
        }

        WF::IInspectable GetAt(uint32_t index)
        {
            return _wrapped.GetAt(index);
        }

        uint32_t Size()
        {
            return _wrapped.Size();
        }

        IBindableVectorView GetView()
        {
            return _wrapped.GetView();
        }

        bool IndexOf(WF::IInspectable const& value, uint32_t& index)
        {
            return _wrapped.IndexOf(value, index);
        }

        void SetAt(uint32_t index, WF::IInspectable const& value)
        {
            return _wrapped.SetAt(index, value);
        }

        void InsertAt(uint32_t index, WF::IInspectable const& value)
        {
            return _wrapped.InsertAt(index, value);
        }

        void RemoveAt(uint32_t index)
        {
            return _wrapped.RemoveAt(index);
        }

        void Append(WF::IInspectable const& value)
        {
            _wrapped.Append(value);
        }

        void RemoveAtEnd()
        {
            _wrapped.RemoveAtEnd();
        }

        void Clear()
        {
            _wrapped.Clear();
        }

        winrt::event_token VectorChanged(BindableVectorChangedEventHandler const& handler)
        {
            return _wrapped.VectorChanged(handler);
        }

        void VectorChanged(winrt::event_token const& token) noexcept
        {
            _wrapped.VectorChanged(token);
        }
    };

    struct data_errors_changed_event_args : implements<data_errors_changed_event_args, IDataErrorsChangedEventArgs>
    {
        data_errors_changed_event_args(winrt::hstring name) :
            m_name(name)
        {
        }

        hstring PropertyName() const
        {
            return m_name;
        }

        void PropertyName(param::hstring const& name)
        {
            m_name = name;
        }

    private:
        winrt::hstring m_name;
    };

    Class::Class() :
        Class(0, L"")
    {
    }
    Class::Class(int32_t intProperty) :
        Class(intProperty, L"")
    {
    }
    Class::Class(int32_t intProperty, hstring const& stringProperty) :
        _int(intProperty),
        _string(stringProperty),
        _stringPair(make<key_value_pair<hstring, hstring>>(L"", L"")),
        _uri(nullptr)
    {
        _nonBlittableStruct.refs.ref32 = 42;
        _syncHandle.attach(::CreateEventW(nullptr, false, false, nullptr));
        if (!_syncHandle)
        {
            winrt::throw_last_error();
        }
        _strings = winrt::single_threaded_vector<hstring>({ L"foo", L"bar" });
    }

    void Class::TypeProperty(Windows::UI::Xaml::Interop::TypeName val)
    {
        _typeProperty = val;
    }

    Windows::UI::Xaml::Interop::TypeName Class::TypeProperty()
    {
        return _typeProperty;
    }

    winrt::hstring Class::GetTypePropertyAbiName()
    {
        return _typeProperty.Name;
    }

    winrt::hstring Class::GetTypePropertyKind()
    {
        switch (_typeProperty.Kind)
        {
        case Windows::UI::Xaml::Interop::TypeKind::Custom:
            return winrt::hstring(L"Custom");
        case Windows::UI::Xaml::Interop::TypeKind::Metadata:
            return winrt::hstring(L"Metadata");
        default:
            return winrt::hstring(L"Primitive");
        }
    }

    Windows::UI::Xaml::Interop::TypeName TypeProperty()
    {
        return Windows::UI::Xaml::Interop::TypeName();
    }

    int32_t Class::StaticIntProperty()
    {
        return statics::_int;
    }
    void Class::StaticIntProperty(int32_t value)
    {
        statics::_int = value;
        statics::_intChanged(/*todo*/ nullptr, statics::_int);
    }
    winrt::event_token Class::StaticIntPropertyChanged(EventHandler<int32_t> const& handler)
    {
        return statics::_intChanged.add(handler);
    }
    void Class::StaticIntPropertyChanged(winrt::event_token const& token) noexcept
    {
        statics::_intChanged.remove(token);
    }
    hstring Class::StaticStringProperty()
    {
        return statics::_string;
    }
    void Class::StaticStringProperty(hstring const& value)
    {
        statics::_string = value;
    }
    winrt::event_token Class::StaticStringPropertyChanged(TypedEventHandler<TestComponentCSharp::Class, hstring> const& handler)
    {
        return statics::_stringChanged.add(handler);
    }
    void Class::StaticStringPropertyChanged(winrt::event_token const& token) noexcept
    {
        statics::_stringChanged.remove(token);
    }
    void Class::StaticGetString()
    {
        statics::_stringChanged(/*todo*/ nullptr, statics::_string);
    }
    void Class::StaticSetString(TestComponentCSharp::ProvideString const& provideString)
    {
        statics::_string = provideString();
    }
    int32_t Class::StaticReadWriteProperty()
    {
        return statics::_readWrite;
    }
    void Class::StaticReadWriteProperty(int32_t value)
    {
        statics::_readWrite = value;
    }
    TimeSpan Class::FromSeconds(int32_t seconds)
    {
        return std::chrono::seconds{seconds};
    }
    DateTime Class::Now()
    {
        return winrt::clock::now();
    }
    winrt::event_token Class::Event0(TestComponentCSharp::EventHandler0 const& handler)
    {
        return _event0.add(handler);
    }
    void Class::Event0(winrt::event_token const& token) noexcept
    {
        _event0.remove(token);
    }
    void Class::InvokeEvent0()
    {
        _event0();
    }
    winrt::event_token Class::Event1(TestComponentCSharp::EventHandler1 const& handler)
    {
        return _event1.add(handler);
    }
    void Class::Event1(winrt::event_token const& token) noexcept
    {
        _event1.remove(token);
    }
    void Class::InvokeEvent1(TestComponentCSharp::Class const& sender)
    {
        _event1(sender);
    }
    winrt::event_token Class::Event2(TestComponentCSharp::EventHandler2 const& handler)
    {
        return _event2.add(handler);
    }
    void Class::Event2(winrt::event_token const& token) noexcept
    {
        _event2.remove(token);
    }
    void Class::InvokeEvent2(TestComponentCSharp::Class const& sender, int32_t arg0)
    {
        _event2(sender, arg0);
    }
    winrt::event_token Class::Event3(TestComponentCSharp::EventHandler3 const& handler)
    {
        return _event3.add(handler);
    }
    void Class::Event3(winrt::event_token const& token) noexcept
    {
        _event3.remove(token);
    }
    void Class::InvokeEvent3(TestComponentCSharp::Class const& sender, int32_t arg0, hstring const& arg1)
    {
        _event3(sender, arg0, arg1);
    }
    winrt::event_token Class::CollectionEvent(TestComponentCSharp::EventHandlerCollection const& handler)
    {
        return _collectionEvent.add(handler);
    }
    void Class::CollectionEvent(winrt::event_token const& token) noexcept
    {
        _collectionEvent.remove(token);
    }
    void Class::InvokeCollectionEvent(TestComponentCSharp::Class const& sender, IVector<int32_t> const& arg0, IMap<int32_t, hstring> const& arg1)
    {
        _collectionEvent(sender, arg0, arg1);
    }
    winrt::event_token Class::GuidEvent(TestComponentCSharp::EventWithGuid const& handler)
    {
        return _guidEvent.add(handler);
    }
    void Class::GuidEvent(winrt::event_token const& token) noexcept
    {
        _guidEvent.remove(token);
    }
    void Class::InvokeGuidEvent(winrt::guid const& correlationGuid)
    {
        _guidEvent(correlationGuid);
    }
    winrt::event_token Class::NestedEvent(EventHandler<IVector<int32_t>> const& handler)
    {
        return _nestedEvent.add(handler);
    }
    void Class::NestedEvent(winrt::event_token const& token) noexcept
    {
        _nestedEvent.remove(token);
    }
    void Class::InvokeNestedEvent(TestComponentCSharp::Class const& sender, IVector<int32_t> const& arg0)
    {
        _nestedEvent(sender, arg0);
    }
    winrt::event_token Class::NestedTypedEvent(TypedEventHandler<TestComponentCSharp::Class, IVector<hstring>> const& handler)
    {
        return _nestedTypedEvent.add(handler);
    }
    void Class::NestedTypedEvent(winrt::event_token const& token) noexcept
    {
        _nestedTypedEvent.remove(token);
    }
    void Class::InvokeNestedTypedEvent(TestComponentCSharp::Class const& sender, IVector<hstring> const& arg0)
    {
        _nestedTypedEvent(sender, arg0);
    }
    winrt::event_token Class::ReturnEvent(TestComponentCSharp::EventWithReturn const& handler)
    {
        return _returnEvent.add(handler);
    }
    void Class::ReturnEvent(winrt::event_token const& token) noexcept
    {
        _returnEvent.remove(token);
    }
    int32_t Class::InvokeReturnEvent(int32_t const& arg0)
    {
        _returnEvent(arg0);
        return arg0;
    }

    winrt::event_token Class::PropertyChangedEventHandler(winrt::Microsoft::UI::Xaml::Data::PropertyChangedEventHandler const& handler)
    {
        return _propertyChangedEventHandler.add(handler);
    }
    void Class::PropertyChangedEventHandler(winrt::event_token const& token) noexcept
    {
        _propertyChangedEventHandler.remove(token);
    }

    winrt::guid Class::TestReturnGuid(winrt::guid const& arg)
    {
        return arg;
    }
    int32_t Class::IntProperty()
    {
        return _int;
    }
    void Class::IntProperty(int32_t value)
    {
        _int = value;
        _intChanged(*this, _int);
    }
    winrt::event_token Class::IntPropertyChanged(EventHandler<int32_t> const& handler)
    {
        auto eventToken = _intChanged.add(handler);
        _lastIntChangedEventToken = eventToken;
        return eventToken;
    }
    void Class::IntPropertyChanged(winrt::event_token const& token) noexcept
    {
        _intChanged.remove(token);
    }
    void Class::RaiseIntChanged()
    {
        _intChanged(*this, _int);
    }
    void Class::RemoveLastIntPropertyChangedHandler()
    {
        _intChanged.remove(_lastIntChangedEventToken);
    }
    void Class::CallForInt(TestComponentCSharp::ProvideInt const& provideInt)
    {
        _int = provideInt();
    }
    bool Class::BoolProperty()
    {
        return _bool;
    }
    void Class::BoolProperty(bool value)
    {
        _bool = value;
        _boolChanged(*this, _bool);
    }
    winrt::event_token Class::BoolPropertyChanged(EventHandler<bool> const& handler)
    {
        return _boolChanged.add(handler);
    }
    void Class::BoolPropertyChanged(winrt::event_token const& token) noexcept
    {
        _boolChanged.remove(token);
    }
    void Class::RaiseBoolChanged()
    {
        _boolChanged(*this, _bool);
    }
    void Class::CallForBool(TestComponentCSharp::ProvideBool const& provideBool)
    {
        _bool = provideBool();
        _boolChanged(*this, _bool);
    }
    void Class::InvokeBoolChanged(winrt::Windows::Foundation::EventHandler<bool> const& boolChanged)
    {
        boolChanged(*this, _bool);
    }

    TestComponentCSharp::EnumValue Class::EnumProperty()
    {
        return _enumValue;
    }
    void Class::EnumProperty(TestComponentCSharp::EnumValue const& value)
    {
        _enumValue = value;
    }
    winrt::event_token Class::EnumPropertyChanged(Windows::Foundation::EventHandler<TestComponentCSharp::EnumValue> const& handler)
    {
        return _enumChanged.add(handler);
    }
    void Class::EnumPropertyChanged(winrt::event_token const& token) noexcept
    {
        _enumChanged.remove(token);
    }
    void Class::RaiseEnumChanged()
    {
        _enumChanged(*this, _enumValue);
    }
    void Class::CallForEnum(TestComponentCSharp::ProvideEnum const& provide)
    {
        _enumValue = provide();
        _enumChanged(*this, _enumValue);
    }
    TestComponentCSharp::EnumStruct Class::EnumStructProperty()
    {
        return _enumStruct;
    }
    void Class::EnumStructProperty(TestComponentCSharp::EnumStruct const& value)
    {
        _enumStruct = value;
    }
    winrt::event_token Class::EnumStructPropertyChanged(Windows::Foundation::EventHandler<TestComponentCSharp::EnumStruct> const& handler)
    {
        return _enumStructChanged.add(handler);
    }
    void Class::EnumStructPropertyChanged(winrt::event_token const& token) noexcept
    {
        _enumStructChanged.remove(token);
    }
    void Class::RaiseEnumStructChanged()
    {
        _enumStructChanged(*this, _enumStruct);
    }
    void Class::CallForEnumStruct(TestComponentCSharp::ProvideEnumStruct const& provide)
    {
        _enumStruct = provide();
        _enumStructChanged(*this, _enumStruct);
    }
    com_array<TestComponentCSharp::EnumValue> Class::EnumsProperty()
    {
        return { _enums.begin(), _enums.end() };
    }
    void Class::EnumsProperty(array_view<TestComponentCSharp::EnumValue const> value)
    {
        _enums.assign(value.begin(), value.end());
    }
    void Class::CallForEnums(TestComponentCSharp::ProvideEnums const& provide)
    {
        EnumsProperty(provide());
    }
    com_array<TestComponentCSharp::EnumStruct> Class::EnumStructsProperty()
    {
        return { _enumStructs.begin(), _enumStructs.end() };
    }
    void Class::EnumStructsProperty(array_view<TestComponentCSharp::EnumStruct const> value)
    {
        _enumStructs.assign(value.begin(), value.end());
    }
    void Class::CallForEnumStructs(TestComponentCSharp::ProvideEnumStructs const& provide)
    {
        EnumStructsProperty(provide());
    }

    TestComponentCSharp::FlagValue Class::FlagProperty()
    {
        return _flagValue;
    }
    void Class::FlagProperty(TestComponentCSharp::FlagValue const& value)
    {
        _flagValue = value;
    }
    winrt::event_token Class::FlagPropertyChanged(Windows::Foundation::EventHandler<TestComponentCSharp::FlagValue> const& handler)
    {
        return _flagChanged.add(handler);
    }
    void Class::FlagPropertyChanged(winrt::event_token const& token) noexcept
    {
        _flagChanged.remove(token);
    }
    void Class::RaiseFlagChanged()
    {
        _flagChanged(*this, _flagValue);
    }
    void Class::CallForFlag(TestComponentCSharp::ProvideFlag const& provide)
    {
        _flagValue = provide();
        _flagChanged(*this, _flagValue);
    }
    TestComponentCSharp::FlagStruct Class::FlagStructProperty()
    {
        return _flagStruct;
    }
    void Class::FlagStructProperty(TestComponentCSharp::FlagStruct const& value)
    {
        _flagStruct = value;
    }
    winrt::event_token Class::FlagStructPropertyChanged(Windows::Foundation::EventHandler<TestComponentCSharp::FlagStruct> const& handler)
    {
        return _flagStructChanged.add(handler);
    }
    void Class::FlagStructPropertyChanged(winrt::event_token const& token) noexcept
    {
        _flagStructChanged.remove(token);
    }
    void Class::RaiseFlagStructChanged()
    {
        _flagStructChanged(*this, _flagStruct);
    }
    void Class::CallForFlagStruct(TestComponentCSharp::ProvideFlagStruct const& provide)
    {
        _flagStruct = provide();
        _flagStructChanged(*this, _flagStruct);
    }
    com_array<TestComponentCSharp::FlagValue> Class::FlagsProperty()
    {
        return { _flags.begin(), _flags.end() };
    }
    void Class::FlagsProperty(array_view<TestComponentCSharp::FlagValue const> value)
    {
        _flags.assign(value.begin(), value.end());
    }
    void Class::CallForFlags(TestComponentCSharp::ProvideFlags const& provide)
    {
        FlagsProperty(provide());
    }
    com_array<TestComponentCSharp::FlagStruct> Class::FlagStructsProperty()
    {
        return { _flagStructs.begin(), _flagStructs.end() };
    }
    void Class::FlagStructsProperty(array_view<TestComponentCSharp::FlagStruct const> value)
    {
        _flagStructs.assign(value.begin(), value.end());
    }
    void Class::CallForFlagStructs(TestComponentCSharp::ProvideFlagStructs const& provide)
    {
        FlagStructsProperty(provide());
    }

    hstring Class::StringProperty()
    {
        return _string;
    }
    void Class::StringProperty(hstring const& value)
    {
        _string = value;
    }
    winrt::event_token Class::StringPropertyChanged(TypedEventHandler<TestComponentCSharp::Class, hstring> const& handler)
    {
        return _stringChanged.add(handler);
    }
    void Class::StringPropertyChanged(winrt::event_token const& token) noexcept
    {
        _stringChanged.remove(token);
    }
    void Class::RaiseStringChanged()
    {
        _stringChanged(*this, _string);
    }
    void Class::CallForString(TestComponentCSharp::ProvideString const& provideString)
    {
        _string = provideString();
    }
    hstring Class::StringProperty2()
    {
        return _string2;
    }
    void Class::StringProperty2(hstring const& value)
    {
        _string2 = value;
    }
    IVector<hstring> Class::StringsProperty()
    {
        return _strings;
    }
    void Class::StringsProperty(IVector<hstring> const& value)
    {
        _strings = value;
    }
    WF::IInspectable Class::ObjectProperty()
    {
        return _object;
    }
    void Class::ObjectProperty(WF::IInspectable const& value)
    {
        if (auto uri = value.try_as<Windows::Foundation::Uri>())
        {
            _uri = uri;
        }
        _object = value;
    }
    void Class::RaiseObjectChanged()
    {
        _objectChanged(*this, _object);
    }
    void Class::CallForObject(TestComponentCSharp::ProvideObject const& provideObject)
    {
        _object = provideObject();
    }
    winrt::event_token Class::ObjectPropertyChanged(EventHandler<WF::IInspectable> const& handler)
    {
        return _objectChanged.add(handler);
    }
    void Class::ObjectPropertyChanged(winrt::event_token const& token) noexcept
    {
        _objectChanged.remove(token);
    }
    IIterable<WF::IInspectable> Class::ObjectIterableProperty()
    {
        return _objectIterable;
    }
    void Class::ObjectIterableProperty(IIterable<WF::IInspectable> const& value)
    {
        for (auto element : value)
        {
        }
        _objectIterable = value;
    }
    void Class::RaiseObjectIterableChanged()
    {
        _objectIterableChanged(*this, _objectIterable);
    }
    void Class::CallForObjectIterable(TestComponentCSharp::ProvideObjectIterable const& provideObjectIterable)
    {
        _objectIterable = provideObjectIterable();
    }
    winrt::event_token Class::ObjectIterablePropertyChanged(EventHandler<IIterable<WF::IInspectable>> const& handler)
    {
        return _objectIterableChanged.add(handler);
    }
    void Class::ObjectIterablePropertyChanged(winrt::event_token const& token) noexcept
    {
        _objectIterableChanged.remove(token);
    }
    IIterable<IIterable<WF::Point>> Class::IterableOfPointIterablesProperty()
    {
        return _pointIterableIterable;
    }
    void Class::IterableOfPointIterablesProperty(IIterable<IIterable<WF::Point>> const& value)
    {
        for (auto points : value)
        {
            for (auto point : points)
            {
            }
        }
        _pointIterableIterable = value;
    }
    IIterable<IIterable<WF::IInspectable>> Class::IterableOfObjectIterablesProperty()
    {
        return _objectIterableIterable;
    }
    void Class::IterableOfObjectIterablesProperty(IIterable<IIterable<WF::IInspectable>> const& value)
    {
        for (auto objects : value)
        {
            for (auto object : objects)
            {
            }
        }
        _objectIterableIterable = value;
    }
    Uri Class::UriProperty()
    {
        return _uri;
    }
    void Class::UriProperty(Uri const& value)
    {
        _uri = value;
    }
    void Class::RaiseUriChanged()
    {
        _uriChanged(*this, _uri);
    }
    void Class::CallForUri(TestComponentCSharp::ProvideUri const& provideUri)
    {
        _uri = provideUri();
    }
    winrt::event_token Class::UriPropertyChanged(EventHandler<Uri> const& handler)
    {
        return _uriChanged.add(handler);
    }
    void Class::UriPropertyChanged(winrt::event_token const& token) noexcept
    {
        _uriChanged.remove(token);
    }

    Windows::Foundation::Collections::IVector<Windows::Foundation::IInspectable> Class::GetUriVectorAsIInspectableVector()
    {
        return single_threaded_vector<Windows::Foundation::IInspectable>({ Uri(L"https://microsoft.com"), Uri(L"https://github.com") });
    };

    IAsyncOperation<int32_t> Class::GetIntAsync()
    {
        co_return _int;
    }
    IAsyncOperationWithProgress<hstring, int32_t> Class::GetStringAsync()
    {
        co_return _string;
    }

    IKeyValuePair<hstring, hstring> Class::StringPairProperty()
    {
        return _stringPair;
    }
    void Class::StringPairProperty(IKeyValuePair<hstring, hstring> const& value)
    {
        _stringPair = value;
    }
    void Class::RaiseStringPairChanged()
    {
        _stringPairChanged(*this, _stringPair);
    }
    void Class::CallForStringPair(TestComponentCSharp::ProvideStringPair const& provideStringPair)
    {
        _stringPair = provideStringPair();
    }
    winrt::event_token Class::StringPairPropertyChanged(EventHandler<IKeyValuePair<hstring, hstring>> const& handler)
    {
        return _stringPairChanged.add(handler);
    }
    void Class::StringPairPropertyChanged(winrt::event_token const& token) noexcept
    {
        _stringPairChanged.remove(token);
    }

    TestComponentCSharp::ProvideUri Class::GetUriDelegate() noexcept
    {
        TestComponentCSharp::ProvideUri handler = [] { return Windows::Foundation::Uri(L"http://microsoft.com"); };
        return handler;
    }

    void Class::AddUriHandler(TestComponentCSharp::IUriHandler uriHandler)
    {
        TestComponentCSharp::ProvideUri handler = [] { return Windows::Foundation::Uri(L"http://github.com"); };
        uriHandler.AddUriHandler(handler);
    }

    BlittableStruct Class::BlittableStructProperty()
    {
        return _blittableStruct.blittable;
    }

    void Class::BlittableStructProperty(BlittableStruct const& value)
    {
        _blittableStruct.blittable = value;
    }

    BlittableStruct Class::GetBlittableStruct()
    {
        return _blittableStruct.blittable;
    }

    void Class::OutBlittableStruct(BlittableStruct& value)
    {
        value = _blittableStruct.blittable;
    }

    void Class::SetBlittableStruct(BlittableStruct const& value)
    {
        _blittableStruct.blittable = value;
    }

    ComposedBlittableStruct Class::ComposedBlittableStructProperty()
    {
        return _blittableStruct;
    }

    void Class::ComposedBlittableStructProperty(ComposedBlittableStruct const& value)
    {
        _blittableStruct = value;
    }

    ComposedBlittableStruct Class::GetComposedBlittableStruct()
    {
        return _blittableStruct;
    }

    void Class::OutComposedBlittableStruct(ComposedBlittableStruct& value)
    {
        value = _blittableStruct;
    }

    void Class::SetComposedBlittableStruct(ComposedBlittableStruct const& value)
    {
        _blittableStruct = value;
    }

    NonBlittableStringStruct Class::NonBlittableStringStructProperty()
    {
        return _nonBlittableStruct.strings;
    }

    void Class::NonBlittableStringStructProperty(NonBlittableStringStruct const& value)
    {
        _nonBlittableStruct.strings = value;
    }

    NonBlittableStringStruct Class::GetNonBlittableStringStruct()
    {
        return _nonBlittableStruct.strings;
    }

    void Class::OutNonBlittableStringStruct(NonBlittableStringStruct& value)
    {
        value = _nonBlittableStruct.strings;
    }

    void Class::SetNonBlittableStringStruct(NonBlittableStringStruct const& value)
    {
        _nonBlittableStruct.strings = value;
    }

    NonBlittableBoolStruct Class::NonBlittableBoolStructProperty()
    {
        return _nonBlittableStruct.bools;
    }

    void Class::NonBlittableBoolStructProperty(NonBlittableBoolStruct const& value)
    {
        _nonBlittableStruct.bools = value;
    }

    NonBlittableBoolStruct Class::GetNonBlittableBoolStruct()
    {
        return _nonBlittableStruct.bools;
    }

    void Class::OutNonBlittableBoolStruct(NonBlittableBoolStruct& value)
    {
        value = _nonBlittableStruct.bools;
    }

    void Class::SetNonBlittableBoolStruct(NonBlittableBoolStruct const& value)
    {
        _nonBlittableStruct.bools = value;
    }

    NonBlittableRefStruct Class::NonBlittableRefStructProperty()
    {
        return _nonBlittableStruct.refs;
    }

    void Class::NonBlittableRefStructProperty(NonBlittableRefStruct const& value)
    {
        _nonBlittableStruct.refs = value;
    }

    NonBlittableRefStruct Class::GetNonBlittableRefStruct()
    {
        return _nonBlittableStruct.refs;
    }

    void Class::OutNonBlittableRefStruct(NonBlittableRefStruct& value)
    {
        value = _nonBlittableStruct.refs;
    }

    void Class::SetNonBlittableRefStruct(NonBlittableRefStruct const& value)
    {
        _nonBlittableStruct.refs = value;
    }

    TestComponentCSharp::ComposedNonBlittableStruct Class::ComposedNonBlittableStructProperty()
    {
        return _nonBlittableStruct;
    }

    void Class::ComposedNonBlittableStructProperty(TestComponentCSharp::ComposedNonBlittableStruct const& value)
    {
        _nonBlittableStruct = value;
    }

    TestComponentCSharp::ComposedNonBlittableStruct Class::GetComposedNonBlittableStruct()
    {
        return _nonBlittableStruct;
    }

    void Class::OutComposedNonBlittableStruct(TestComponentCSharp::ComposedNonBlittableStruct& value)
    {
        value = _nonBlittableStruct;
    }

    void Class::SetComposedNonBlittableStruct(TestComponentCSharp::ComposedNonBlittableStruct const& value)
    {
        _nonBlittableStruct = value;
    }

    void Class::SetInts(array_view<int32_t const> ints)
    {
        _ints.assign( ints.begin(), ints.end() );
    }

    com_array<int32_t> Class::GetInts()
    {
        return { _ints.begin(), _ints.end() };
    }

    void Class::FillInts(array_view<int32_t> ints)
    {
        std::copy(_ints.begin(), _ints.end(), ints.begin());
    }

    com_array<winrt::hresult> Class::GetAndSetHResults(array_view<winrt::hresult const> hresults)
    {
        return com_array<winrt::hresult>(hresults.begin(), hresults.end());
    }

    com_array<winrt::Windows::Foundation::Uri> Class::GetAndSetUris(array_view<winrt::Windows::Foundation::Uri const> uris)
    {
        return com_array<winrt::Windows::Foundation::Uri>(uris.begin(), uris.end());
    }

    com_array<winrt::Windows::Foundation::DateTime> Class::GetAndSetDateTimes(array_view<winrt::Windows::Foundation::DateTime const> datetime)
    {
        return com_array<winrt::Windows::Foundation::DateTime>(datetime.begin(), datetime.end());
    }

    IVectorView<int32_t> Class::GetIntVector()
    {
        return winrt::single_threaded_vector_view(std::vector{ 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });
    }

    IVectorView<bool> Class::GetBoolVector()
    {
        return winrt::single_threaded_vector_view(std::vector{ true, false, true, false });
    }

    IVectorView<hstring> Class::GetStringVector()
    {
        return winrt::single_threaded_vector_view(std::vector<hstring>{ L"String0", L"String1", L"String2", L"String3", L"String4" });
    }

    IVectorView<TestComponentCSharp::ComposedBlittableStruct> Class::GetBlittableStructVector()
    {
        return winrt::single_threaded_vector_view(std::vector{ ComposedBlittableStruct{0}, ComposedBlittableStruct{1},
            ComposedBlittableStruct{2}, ComposedBlittableStruct{3}, ComposedBlittableStruct{4} });
    }

    IVectorView<TestComponentCSharp::ComposedNonBlittableStruct> Class::GetNonBlittableStructVector()
    {
        return winrt::single_threaded_vector_view(std::vector
        {
            ComposedNonBlittableStruct{ { 0 }, { L"String0" }, { true, false, true, false }, { 0 } },
            ComposedNonBlittableStruct{ { 1 }, { L"String1" }, { false, true, false, true }, { 1 } },
            ComposedNonBlittableStruct{ { 2 }, { L"String2" }, { true, false, true, false }, { 2 } },
        });
    }

    IVectorView<WF::IInspectable> Class::GetObjectVector()
    {
        return winrt::single_threaded_vector_view(std::vector<WF::IInspectable>{ winrt::box_value(0), winrt::box_value(1), winrt::box_value(2) });
    }

    IVectorView<TestComponentCSharp::IProperties1> Class::GetInterfaceVector()
    {
        return winrt::single_threaded_vector_view(std::vector<IProperties1>{ *this, *this, *this });
    }

    IVectorView<TestComponentCSharp::Class> Class::GetClassVector() noexcept
    {
        return winrt::single_threaded_vector_view(std::vector<TestComponentCSharp::Class>{ *this, *this, *this });
    }
    
    IMap<int32_t, int32_t> Class::GetIntToIntDictionary()
    {
        return single_threaded_map<int32_t, int32_t>(std::map<int32_t, int32_t>{ {1, 4}, { 2, 8 }, { 3, 12 } });
    }
    
    IMap<hstring, TestComponentCSharp::ComposedBlittableStruct> Class::GetStringToBlittableDictionary()
    {
        return single_threaded_map<hstring, TestComponentCSharp::ComposedBlittableStruct>(std::map<hstring, TestComponentCSharp::ComposedBlittableStruct>
        { 
            { L"alpha", ComposedBlittableStruct{ 5 } }, 
            { L"beta", ComposedBlittableStruct{ 4 } }, 
            { L"charlie", ComposedBlittableStruct{ 7 } } 
        });
    }
    
    IMap<hstring, TestComponentCSharp::ComposedNonBlittableStruct> Class::GetStringToNonBlittableDictionary()
    {
        return single_threaded_map<hstring, TestComponentCSharp::ComposedNonBlittableStruct>(std::map<hstring, TestComponentCSharp::ComposedNonBlittableStruct>
        {
            { L"String0", ComposedNonBlittableStruct{ { 0 }, { L"String0" }, { true, false, true, false }, { 0 } } },
            { L"String1", ComposedNonBlittableStruct{ { 1 }, { L"String1" }, { false, true, false, true }, { 1 } } },
            { L"String2", ComposedNonBlittableStruct{ { 2 }, { L"String2" }, { true, false, true, false }, { 2 } } }
        });
    }

    IMap<int32_t, Windows::Foundation::Collections::IVector<TestComponentCSharp::EnumValue>> Class::GetIntToListDictionary()
    {
        auto vector = winrt::single_threaded_vector(std::vector { TestComponentCSharp::EnumValue::One });
        return single_threaded_map<int32_t, Windows::Foundation::Collections::IVector<TestComponentCSharp::EnumValue>>(std::map<int32_t, Windows::Foundation::Collections::IVector<TestComponentCSharp::EnumValue>>{ {1, vector}});
    }
    
    struct ComposedBlittableStructComparer
    {
        bool operator() (const TestComponentCSharp::ComposedBlittableStruct& l, const TestComponentCSharp::ComposedBlittableStruct& r) const
        {
            return (l.blittable.i32 < r.blittable.i32);
        }
    };

    IMap<TestComponentCSharp::ComposedBlittableStruct, WF::IInspectable> Class::GetBlittableToObjectDictionary()
    {
        return single_threaded_map<TestComponentCSharp::ComposedBlittableStruct, WF::IInspectable>(std::map<TestComponentCSharp::ComposedBlittableStruct, WF::IInspectable, ComposedBlittableStructComparer>
        {
            { ComposedBlittableStruct{ 1 }, winrt::box_value(0) },
            { ComposedBlittableStruct{ 4 }, winrt::box_value(L"box") },
            { ComposedBlittableStruct{ 8 }, *this }
        });
    }

    IVector<int32_t> Class::GetIntVector2()
    {
        return winrt::single_threaded_vector(std::vector{ 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });
    }

    IVector<TestComponentCSharp::ComposedBlittableStruct> Class::GetBlittableStructVector2()
    {
        return winrt::single_threaded_vector(std::vector{ ComposedBlittableStruct{0}, ComposedBlittableStruct{1},
            ComposedBlittableStruct{2}, ComposedBlittableStruct{3}, ComposedBlittableStruct{4} });
    }

    IVector<TestComponentCSharp::ComposedNonBlittableStruct> Class::GetNonBlittableStructVector2()
    {
        return winrt::single_threaded_vector(std::vector
            {
                ComposedNonBlittableStruct{ { 0 }, { L"String0" }, { true, false, true, false }, { 0 } },
                ComposedNonBlittableStruct{ { 1 }, { L"String1" }, { false, true, false, true }, { 1 } },
                ComposedNonBlittableStruct{ { 2 }, { L"String2" }, { true, false, true, false }, { 2 } },
            });
    }

    // Test IIDOptimizer
    IVectorView<Microsoft::UI::Xaml::Data::DataErrorsChangedEventArgs> Class::GetEventArgsVector()
    {
        auto mock = make<data_errors_changed_event_args>(L"name");
        DataErrorsChangedEventArgs args(detach_abi(mock), take_ownership_from_abi_t());
        return winrt::single_threaded_vector_view(std::vector<Microsoft::UI::Xaml::Data::DataErrorsChangedEventArgs>{ args });
    }

    // Test IIDOptimizer
    IVectorView<TestComponentCSharp::ProvideUri> Class::GetNonGenericDelegateVector()
    {
        TestComponentCSharp::ProvideUri handler = [] { return Windows::Foundation::Uri(L"http://microsoft.com"); };
        return winrt::single_threaded_vector_view(std::vector<ProvideUri>{ handler });
    }

    void Class::CompleteAsync()
    {
        CompleteAsync(S_OK);
    }

    void Class::CompleteAsync(int32_t hr)
    {
        assert(_asyncResult == E_PENDING);
        _asyncResult = hr;
        winrt::check_bool(::SetEvent(_syncHandle.get()));
    }

    void Class::AdvanceAsync(int32_t delta)
    {
        assert(_asyncResult == E_PENDING);
        _asyncProgress += delta;
        winrt::check_bool(::SetEvent(_syncHandle.get()));
    }

    IAsyncAction Class::DoitAsync()
    {
        _asyncResult = E_PENDING;
        co_await winrt::resume_background();

        auto cancel = co_await winrt::get_cancellation_token();
        cancel.callback([this]() { ::SetEvent(_syncHandle.get()); });

        co_await winrt::resume_on_signal(_syncHandle.get());
        if (cancel()) co_return;
        winrt::check_hresult(_asyncResult);
    }

    IAsyncActionWithProgress<int32_t> Class::DoitAsyncWithProgress()
    {
        _asyncResult = E_PENDING;
        _asyncProgress = 0;
        co_await winrt::resume_background();

        auto cancel = co_await winrt::get_cancellation_token();
        cancel.callback([this]() { ::SetEvent(_syncHandle.get()); });

        auto progress = co_await winrt::get_progress_token();
        while (true)
        {
            co_await winrt::resume_on_signal(_syncHandle.get());
            if (cancel()) co_return;
            if (_asyncResult != E_PENDING) break;
            progress(_asyncProgress);
        }

        winrt::check_hresult(_asyncResult);
    }

    IAsyncOperation<int32_t> Class::AddAsync(int32_t lhs, int32_t rhs)
    {
        _asyncResult = E_PENDING;
        co_await winrt::resume_background();

        auto cancel = co_await winrt::get_cancellation_token();
        cancel.callback([this]() { ::SetEvent(_syncHandle.get()); });

        co_await winrt::resume_on_signal(_syncHandle.get());
        if (cancel()) co_return lhs + rhs; // TODO: Why do I need to provide a value
        winrt::check_hresult(_asyncResult);
        co_return lhs + rhs;
    }

    IAsyncOperationWithProgress<int32_t, int32_t> Class::AddAsyncWithProgress(int32_t lhs, int32_t rhs)
    {
        _asyncResult = E_PENDING;
        _asyncProgress = 0;
        co_await winrt::resume_background();

        auto cancel = co_await winrt::get_cancellation_token();
        cancel.callback([this]() { ::SetEvent(_syncHandle.get()); });

        auto progress = co_await winrt::get_progress_token();
        while (true)
        {
            co_await winrt::resume_on_signal(_syncHandle.get());
            if (cancel()) co_return lhs + rhs; // TODO: Why do I need to provide a value
            if (_asyncResult != E_PENDING) break;
            progress(_asyncProgress);
        }

        winrt::check_hresult(_asyncResult);
        co_return lhs + rhs;
    }

    Point Class::PointProperty()
    {
        return _point;
    }

    void Class::PointProperty(Point const& value)
    {
        _point = value;
    }

    Rect Class::RectProperty()
    {
        return _rect;
    }

    void Class::RectProperty(Rect const& value)
    {
        _rect = value;
    }

    Size Class::SizeProperty()
    {
        return _size;
    }

    void Class::SizeProperty(Size const& value)
    {
        _size = value;
    }

    Windows::UI::Color Class::ColorProperty()
    {
        return _color;
    }
    
    void Class::ColorProperty(Windows::UI::Color const& value)
    {
        _color = value;
    }

    Microsoft::UI::Xaml::CornerRadius Class::CornerRadiusProperty()
    {
        return _cornerRadius;
    }
    void Class::CornerRadiusProperty(Microsoft::UI::Xaml::CornerRadius const& value)
    {
        _cornerRadius = value;
    }
    Microsoft::UI::Xaml::Duration Class::DurationProperty()
    {
        return _duration;
    }
    void Class::DurationProperty(Microsoft::UI::Xaml::Duration const& value)
    {
        _duration = value;
    }
    Microsoft::UI::Xaml::GridLength Class::GridLengthProperty()
    {
        return _gridLength;
    }
    void Class::GridLengthProperty(Microsoft::UI::Xaml::GridLength const& value)
    {
        _gridLength = value;
    }
    Microsoft::UI::Xaml::Thickness Class::ThicknessProperty()
    {
        return _thickness;
    }
    void Class::ThicknessProperty(Microsoft::UI::Xaml::Thickness const& value)
    {
        _thickness = value;
    }
    Microsoft::UI::Xaml::Controls::Primitives::GeneratorPosition Class::GeneratorPositionProperty()
    {
        return _generatorPosition;
    }
    void Class::GeneratorPositionProperty(Microsoft::UI::Xaml::Controls::Primitives::GeneratorPosition const& value)
    {
        _generatorPosition = value;
    }
    Microsoft::UI::Xaml::Media::Matrix Class::MatrixProperty()
    {
        return _matrix;
    }
    void Class::MatrixProperty(Microsoft::UI::Xaml::Media::Matrix const& value)
    {
        _matrix = value;
    }
    Microsoft::UI::Xaml::Media::Animation::KeyTime Class::KeyTimeProperty()
    {
        return _keyTime;
    }
    void Class::KeyTimeProperty(Microsoft::UI::Xaml::Media::Animation::KeyTime const& value)
    {
        _keyTime = value;
    }
    Microsoft::UI::Xaml::Media::Animation::RepeatBehavior Class::RepeatBehaviorProperty()
    {
        return _repeatBehavior;
    }
    void Class::RepeatBehaviorProperty(Microsoft::UI::Xaml::Media::Animation::RepeatBehavior const& value)
    {
        _repeatBehavior = value;
    }
    Microsoft::UI::Xaml::Media::Media3D::Matrix3D Class::Matrix3DProperty()
    {
        return _matrix3D;
    }
    void Class::Matrix3DProperty(Microsoft::UI::Xaml::Media::Media3D::Matrix3D const& value)
    {
        _matrix3D = value;
    }
    Numerics::float3x2 Class::Matrix3x2Property()
    {
        return _matrix3x2;
    }
    void Class::Matrix3x2Property(Numerics::float3x2 const& value)
    {
        _matrix3x2 = value;
    }
    Numerics::float4x4 Class::Matrix4x4Property()
    {
        return _matrix4x4;
    }
    void Class::Matrix4x4Property(Numerics::float4x4 const& value)
    {
        _matrix4x4 = value;
    }
    Numerics::plane Class::PlaneProperty()
    {
        return _plane;
    }
    void Class::PlaneProperty(Numerics::plane const& value)
    {
        _plane = value;
    }
    Numerics::quaternion Class::QuaternionProperty()
    {
        return _quaternion;
    }
    void Class::QuaternionProperty(Numerics::quaternion const& value)
    {
        _quaternion = value;
    }
    Numerics::float2 Class::Vector2Property()
    {
        return _vector2;
    }
    void Class::Vector2Property(Numerics::float2 const& value)
    {
        _vector2 = value;
    }
    Numerics::float3 Class::Vector3Property()
    {
        return _vector3;
    }
    void Class::Vector3Property(Numerics::float3 const& value)
    {
        _vector3 = value;
    }
    Windows::Foundation::IReference<Windows::Foundation::Numerics::float3> Class::Vector3NullableProperty()
    {
        return _vector3;
    }
    void Class::Vector3NullableProperty(Windows::Foundation::IReference<Windows::Foundation::Numerics::float3> const& value)
    {
        _vector3 = value.Value();
    }
    Numerics::float4 Class::Vector4Property()
    {
        return _vector4;
    }
    void Class::Vector4Property(Numerics::float4 const& value)
    {
        _vector4 = value;
    }
    IReference<Point> Class::GetPointReference()
    {
        return _point;
    }

    TimeSpan Class::TimeSpanProperty()
    {
        return _timeSpan;
    }

    void Class::TimeSpanProperty(TimeSpan const& value)
    {
        _timeSpan = value;
    }

    IReference<TimeSpan> Class::GetTimeSpanReference()
    {
        return _timeSpan;
    }

    DateTime Class::DateTimeProperty()
    {
        return _dateTime;
    }

    void Class::DateTimeProperty(DateTime const& value)
    {
        _dateTime = value;
    }

    IReference<DateTime> Class::GetDateTimeProperty()
    {
        return _dateTime;
    }

    winrt::hresult Class::HResultProperty()
    {
        return _hr;
    }

    void Class::HResultProperty(winrt::hresult const& hr)
    {
        _hr = hr;
    }

    // IStringable
    hstring Class::ToString()
    {
        return _string;
    }

    int32_t Class::ReadWriteProperty() 
    {
        return _int;
    }
    void Class::ReadWriteProperty(int32_t value) noexcept
    {
        _int = value;
    }

    IIterable<int32_t> Class::GetIntIterable()
    {
        return _intColl;
    }
    void Class::SetIntIterable(IIterable<int32_t> const& value)
    {
        _intColl = value;
    }

    void Class::SetCharIterable(IIterable<char16_t> const& value)
    {
        _charColl = value;
    }

    IIterable<EnumValue> Class::GetEnumIterable()
    {
        return winrt::single_threaded_vector(std::vector{ EnumValue::One, EnumValue::Two });
    }

    IIterable<TestComponentCSharp::CustomDisposableTest> Class::GetClassIterable()
    {
        TestComponentCSharp::CustomDisposableTest first;
        TestComponentCSharp::CustomDisposableTest second;
        return winrt::single_threaded_vector(std::vector{ first, second });
    }

    IIterator<int32_t> Class::GetIteratorForCollection(IIterable<int32_t> iterable)
    {
        return iterable.First();
    }

    IBindableIterable Class::BindableIterableProperty()
    {
        return _bindableIterable;
    }
    void Class::BindableIterableProperty(IBindableIterable const& value)
    {
        _bindableIterable = value;
        auto iterator = _bindableIterable.First();
        int32_t expected = 0;
        while (iterator.HasCurrent())
        {
            if (winrt::unbox_value<int32_t>(iterator.Current()) != expected++)
            {
                throw new winrt::hresult_invalid_argument();
            }
            iterator.MoveNext();
        }
    }
    void Class::RaiseBindableIterableChanged()
    {
        _bindableIterableChanged(*this, _bindableIterable);
    }
    void Class::CallForBindableIterable(TestComponentCSharp::ProvideBindableIterable const& provideBindableIterable)
    {
        _bindableIterable = provideBindableIterable();
    }
    winrt::event_token Class::BindableIterablePropertyChanged(EventHandler<IBindableIterable> const& handler)
    {
        return _bindableIterableChanged.add(handler);
    }
    void Class::BindableIterablePropertyChanged(winrt::event_token const& token) noexcept
    {
        _bindableIterableChanged.remove(token);
    }
    IBindableVector Class::BindableVectorProperty()
    {
        return _bindableVector;
    }

    void Class::BindableVectorProperty(IBindableVector const& value)
    {
        _bindableVector = winrt::make<bindable_vector>(value);
        auto view = _bindableVector.GetView();
        int32_t expected = 0;
        for (uint32_t i = 0; i < view.Size(); i++)
        {
            if (winrt::unbox_value<int32_t>(view.GetAt(i)) != expected++)
            {
                throw new winrt::hresult_invalid_argument();
            }
        }
    }
    void Class::RaiseBindableVectorChanged()
    {
        _bindableVectorChanged(*this, _bindableVector);
    }
    void Class::CallForBindableVector(TestComponentCSharp::ProvideBindableVector const& provideBindableVector)
    {
        _bindableVector = provideBindableVector();
    }
    winrt::event_token Class::BindableVectorPropertyChanged(EventHandler<IBindableVector> const& handler)
    {
        return _bindableVectorChanged.add(handler);
    }
    void Class::BindableVectorPropertyChanged(winrt::event_token const& token) noexcept
    {
        _bindableVectorChanged.remove(token);
    }
    IBindableObservableVector Class::BindableObservableVectorProperty()
    {
        return _bindableObservable;
    }
    void Class::BindableObservableVectorProperty(IBindableObservableVector const& value)
    {
        _bindableObservable = value;
        _bindableObservable.VectorChanged(
            [](IBindableObservableVector vector, WF::IInspectable e) {
                int32_t sum = 0;
                auto view = vector.GetView();
                for (uint32_t i = 0; i < view.Size(); i++)
                {
                    sum += winrt::unbox_value<int32_t>(view.GetAt(i));
                }
                e.as<IProperties2>().ReadWriteProperty(sum);
        });
    }

    IBindableObservableVector Class::GetBindableObservableVector(IBindableObservableVector vector)
    {
        return winrt::make<bindable_observable_vector>(vector);
    }

    bool Class::ValidateBindableProperty(
        WF::IInspectable const& bindableObject,
        hstring property,
        Windows::UI::Xaml::Interop::TypeName const& indexerType,
        bool validateOnlyExists,
        bool canRead,
        bool canWrite,
        bool isIndexer,
        Windows::UI::Xaml::Interop::TypeName const& type,
        WF::IInspectable const& indexerValue,
        WF::IInspectable const& setValue,
        WF::IInspectable& retrievedValue)
    {
        auto customPropertyProvider = bindableObject.as<ICustomPropertyProvider>();
        auto customProperty = !isIndexer ? customPropertyProvider.GetCustomProperty(property) : customPropertyProvider.GetIndexedProperty(property, indexerType);
        if (customProperty == nullptr)
		{
            return false;
		}

        if (validateOnlyExists)
        {
            return true;
        }

        if (customProperty.Name() != property ||
            customProperty.CanRead() != canRead ||
            customProperty.CanWrite() != canWrite ||
            customProperty.Type() != type)
        {
            return false;
        }

        if (!isIndexer)
        {
            if (customProperty.CanRead())
            {
                retrievedValue = customProperty.GetValue(bindableObject);
            }

            if (customProperty.CanWrite())
            {
                customProperty.SetValue(bindableObject, setValue);
            }
        }
        else
        {
            if (customProperty.CanRead())
            {
                retrievedValue = customProperty.GetIndexedValue(bindableObject, indexerValue);
            }

            if (customProperty.CanWrite())
            {
                customProperty.SetIndexedValue(bindableObject, setValue, indexerValue);
            }
        }

        return true;
    }

    void Class::CopyProperties(winrt::TestComponentCSharp::IProperties1 const& src)
    {
        ReadWriteProperty(src.ReadWriteProperty());
    }

    void Class::CopyPropertiesViaWeakReference(winrt::TestComponentCSharp::IProperties1 const& src)
    {
        auto weak_ref = winrt::make_weak(src);
        ReadWriteProperty(weak_ref.get().ReadWriteProperty());
    }

    int32_t Class::UnboxInt32(WF::IInspectable const& obj)
    {
        return winrt::unbox_value<int32_t>(obj);
    }

    bool Class::UnboxBoolean(WF::IInspectable const& obj)
    {
        return winrt::unbox_value<bool>(obj);
    }

    hstring Class::UnboxString(WF::IInspectable const& obj)
    {
        return winrt::unbox_value<hstring>(obj);
    }

    EnumValue Class::UnboxEnum(WF::IInspectable const& obj)
    {
        return winrt::unbox_value<EnumValue>(obj);
    }

    winrt::TestComponentCSharp::ProvideInt Class::UnboxDelegate(WF::IInspectable const& obj)
    {
        return winrt::unbox_value<TestComponentCSharp::ProvideInt>(obj);
    }

    Windows::UI::Xaml::Interop::TypeName Class::UnboxType(WF::IInspectable const& obj)
    {
        return winrt::unbox_value<Windows::UI::Xaml::Interop::TypeName>(obj);
    }

    com_array<int32_t> Class::UnboxInt32Array(WF::IInspectable const& obj)
    {
        return obj.as<IReferenceArray<int32_t>>().Value();
    }

    com_array<bool> Class::UnboxBooleanArray(WF::IInspectable const& obj)
    {
        return obj.as<IReferenceArray<bool>>().Value();
    }
    
    com_array<hstring> Class::UnboxStringArray(WF::IInspectable const& obj)
    {
        return obj.as<IReferenceArray<hstring>>().Value();
    }

    int32_t Class::GetPropertyType(IInspectable const& obj)
    {
        if (auto ipv = obj.try_as<IPropertyValue>())
        {
            return static_cast<int32_t>(ipv.Type());
        }
        return -1;
    }

    hstring Class::GetName(IInspectable const& obj)
    {
        return get_class_name(obj);
    }

    TypeName Class::Int32Type()
    {
        return winrt::xaml_typename<int32_t>();
    }

    TypeName Class::ReferenceInt32Type()
    {
        return winrt::xaml_typename<IReference<int32_t>>();
    }

    TypeName Class::ThisClassType()
    {
        return winrt::xaml_typename<winrt::TestComponentCSharp::Class>();
    }

    WF::IInspectable Class::BoxedType()
    {
        return winrt::box_value(winrt::xaml_typename<winrt::TestComponentCSharp::Class>());
    }

    IVector<Windows::UI::Xaml::Interop::TypeName> Class::ListOfTypes()
    {
        return single_threaded_vector<Windows::UI::Xaml::Interop::TypeName>({ winrt::xaml_typename<winrt::TestComponentCSharp::Class>(), winrt::xaml_typename<IReference<int32_t>>() });
    }

    bool Class::VerifyTypeIsInt32Type(TypeName const& type_name)
    {
        return winrt::xaml_typename<int32_t>() == type_name;
    }

    bool Class::VerifyTypeIsReferenceInt32Type(TypeName const& type_name)
    {
        return winrt::xaml_typename<IReference<int32_t>>() == type_name;
    }

    bool Class::VerifyTypeIsThisClassType(TypeName const& type_name)
    {
        return winrt::xaml_typename<winrt::TestComponentCSharp::Class>() == type_name;
    }

    hstring Class::GetTypeNameForType(TypeName const& type)
    {
        return type.Name;
    }

    WF::IInspectable Class::EmptyString()
    {
        return winrt::box_value(hstring{});
    }

    WF::IInspectable Class::BoxedDelegate()
    {
        TestComponentCSharp::ProvideUri handler = [] { return Windows::Foundation::Uri(L"http://microsoft.com"); };
        return winrt::box_value(handler);
    }

    WF::IInspectable Class::BoxedEnum()
    {
        EnumValue val = EnumValue::Two;
        return winrt::box_value(val);
    }

    WF::IInspectable Class::BoxedEventHandler()
    {
        Windows::Foundation::EventHandler<int> handler = [](auto&&...) { };
        return winrt::box_value(handler);
    }

    hstring Class::Catch(hstring const& /*params*/, hstring& /*lock*/)
    {
        // Compile-only test for keyword escaping
        throw hresult_not_implemented();
    }

    hstring Class::ThrowExceptionWithMessage(hstring message, bool throwNonMappedError)
    {
        if (throwNonMappedError)
        {
            throw hresult_wrong_thread(message.c_str());
        }
        else
        {
            throw hresult_invalid_argument(message.c_str());
        }
    }

    extern "C" BOOL __stdcall RoOriginateLanguageException(HRESULT error, void* message, void* languageException);

    hstring Class::OriginateAndThrowExceptionWithMessage(hstring message)
    {
        struct language_exception : winrt::implements<language_exception, ::IUnknown>
        {
        };

        RoOriginateLanguageException(winrt::impl::error_invalid_argument, get_abi(message), get_abi(winrt::make<language_exception>()));
        throw hresult_invalid_argument(hresult_error::from_abi);
    }

    void Class::UnboxAndCallProgressHandler(WF::IInspectable const& httpProgressHandler)
	{
        Windows::Web::Http::HttpProgress progress;
        progress.BytesReceived = 3;
        progress.TotalBytesToReceive = 4;

        winrt::unbox_value<Windows::Foundation::AsyncActionProgressHandler<Windows::Web::Http::HttpProgress>>(httpProgressHandler)(nullptr, progress);
    }

    double Class::Calculate(winrt::Windows::Foundation::Collections::IVector<winrt::Windows::Foundation::IReference<double>> const& values)
    {
        double result = 0;
        for (auto val : values)
        {
            if (val)
            {
                result += val.Value();
            }
        }
        return result;
    }

    winrt::Windows::Foundation::Collections::IVector<winrt::Windows::Foundation::IReference<int32_t>> Class::GetNullableIntList()
    {
        return single_threaded_vector<winrt::Windows::Foundation::IReference<int32_t>>({ 1, nullptr, 2 });
    }

    TestComponentCSharp::IProperties1 Class::NativeProperties1()
    {
        struct native_properties1 : winrt::implements<native_properties1, TestComponentCSharp::IProperties1>
        {
            int32_t ReadWriteProperty()
            {
                return 42;
            }
        };

        return winrt::make<native_properties1>();
    }

    // TODO: when the public WinUI nuget supports IXamlServiceProvider, just use the projection
    struct __declspec(uuid("68B3A2DF-8173-539F-B524-C8A2348F5AFB")) IServiceProviderInterop : ::IInspectable
    {
        virtual HRESULT __stdcall GetService(int32_t* type, int32_t* service) noexcept = 0;
    };

    WF::IInspectable Class::ServiceProvider()
    {
        struct service_provider : winrt::implements<service_provider, WF::IInspectable, IServiceProviderInterop>
        {
            HRESULT __stdcall GetService(int32_t* type, int32_t* service) noexcept override
            {
                *service = 42;
                return 0;
            }
        };

        return winrt::make<service_provider>();
    }

    struct __declspec(uuid("15651B9F-6C6B-4CC0-944C-C7D7B0F36F81")) IComInterop : ::IUnknown
    {
        virtual HRESULT __stdcall ReturnWindowHandle(HWND window, guid riid, int64_t* value) noexcept = 0;
    };

    struct __declspec(uuid("5AD8CBA7-4C01-4DAC-9074-827894292D63")) IDragDropManagerInterop : ::IInspectable
    {
        virtual HRESULT __stdcall GetForWindow(HWND window, guid const& riid, void** value) noexcept = 0;
    };

    WF::IInspectable Class::ComInterop()
    {
        struct com_interop : winrt::implements<com_interop, WF::IInspectable, IComInterop, IDragDropManagerInterop>
        {
            HRESULT __stdcall ReturnWindowHandle(HWND window, guid riid, int64_t* value) noexcept override
            {
                *value = riid == winrt::guid_of<IComInterop>() ? (int64_t)window : 0;
                return 0;
            }

            HRESULT __stdcall GetForWindow(HWND window, guid const& riid, void** value) noexcept override
            {
                static const guid ICoreDragDropManager("7D56D344-8464-4FAF-AA49-37EA6E2D7BD1");
                if (riid == ICoreDragDropManager)
                {
                    auto dummy = winrt::make<com_interop>();
                    *value = winrt::detach_abi(dummy);
                    return 0;
                }
                *value = 0;
                return 0x80070057; // E_INVALIDARG
            }
        };

        return winrt::make<com_interop>();
    }

    WF::Collections::IPropertySet Class::PropertySet()
    {
        WF::Collections::PropertySet propertySet;
        propertySet.Insert(L"alpha", winrt::box_value(L"first"));
        propertySet.Insert(L"beta", winrt::box_value(L"second"));
        propertySet.Insert(L"charlie", winrt::box_value(L"third"));
        return propertySet;
    }

    // INotifyDataErrorInfo
    bool Class::HasErrors()
    {
        return true;
    }
    winrt::event_token Class::ErrorsChanged(EventHandler<DataErrorsChangedEventArgs> const& handler)
    {
        return _dataErrorsChanged.add(handler);
    }
    void Class::ErrorsChanged(winrt::event_token const& token) noexcept
    {
        _dataErrorsChanged.remove(token);
    }
    IIterable<WF::IInspectable> Class::GetErrors(hstring const& propertyName)
    {
        return _objectIterable;
    }
    void Class::RaiseDataErrorChanged()
    {
        auto mock = make<data_errors_changed_event_args>(L"name");
        DataErrorsChangedEventArgs args(detach_abi(mock), take_ownership_from_abi_t());
        _dataErrorsChanged(*this, args);
    }

    // ICommand
    winrt::event_token Class::CanExecuteChanged(winrt::Windows::Foundation::EventHandler<winrt::Windows::Foundation::IInspectable> const& handler)
    {
        return _canExecuteChanged.add(handler);
    }
    void Class::CanExecuteChanged(winrt::event_token const& token) noexcept
    {
        return _canExecuteChanged.remove(token);
    }
    bool Class::CanExecute(winrt::Windows::Foundation::IInspectable const& parameter)
    {
        return true;
    }
    void Class::Execute(winrt::Windows::Foundation::IInspectable const& parameter)
    {
    }
    void Class::RaiseCanExecuteChanged()
    {
        _canExecuteChanged(*this, *this);
    }


    WF::IInspectable Class::BadRuntimeClassName()
    {
        struct bad_runtime_classname : winrt::implements<bad_runtime_classname, WF::IInspectable>
        {
            hstring GetRuntimeClassName()
            {
                return L"BadRuntimeClassName<T>";
            }
        };

        return winrt::make<bad_runtime_classname>();
    }
}

