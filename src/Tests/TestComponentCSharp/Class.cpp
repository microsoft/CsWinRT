﻿#include "pch.h"
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
        return _intChanged.add(handler);
    }
    void Class::IntPropertyChanged(winrt::event_token const& token) noexcept
    {
        _intChanged.remove(token);
    }
    void Class::RaiseIntChanged()
    {
        _intChanged(*this, _int);
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

    hstring Class::Catch(hstring const& /*params*/, hstring& /*lock*/)
    {
        // Compile-only test for keyword escaping
        throw hresult_not_implemented();
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

