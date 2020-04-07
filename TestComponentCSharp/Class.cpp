#include "pch.h"
#include "Class.h"
#include "Class.g.cpp"

using namespace std::chrono;

namespace winrt::TestComponentCSharp::implementation
{
    namespace statics
    {
        static int _int {};
        static winrt::event<Windows::Foundation::EventHandler<int32_t>> _intChanged {};
        static winrt::hstring _string;
        static winrt::event<Windows::Foundation::TypedEventHandler<TestComponentCSharp::Class, hstring>> _stringChanged {};
        static int _readWrite{};
    }

    template <typename K, typename V>
    struct key_value_pair : implements<key_value_pair<K, V>, Windows::Foundation::Collections::IKeyValuePair<K, V>>
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
    int32_t Class::StaticIntProperty()
    {
        return statics::_int;
    }
    void Class::StaticIntProperty(int32_t value)
    {
        statics::_int = value;
        statics::_intChanged(/*todo*/ nullptr, statics::_int);
    }
    winrt::event_token Class::StaticIntPropertyChanged(Windows::Foundation::EventHandler<int32_t> const& handler)
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
    winrt::event_token Class::StaticStringPropertyChanged(Windows::Foundation::TypedEventHandler<TestComponentCSharp::Class, hstring> const& handler)
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
    Windows::Foundation::TimeSpan Class::FromSeconds(int32_t seconds)
    {
        return std::chrono::seconds{seconds};
    }
    Windows::Foundation::DateTime Class::Now()
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
    void Class::InvokeCollectionEvent(TestComponentCSharp::Class const& sender, Windows::Foundation::Collections::IVector<int32_t> const& arg0, Windows::Foundation::Collections::IMap<int32_t, hstring> const& arg1)
    {
        _collectionEvent(sender, arg0, arg1);
    }
    winrt::event_token Class::NestedEvent(Windows::Foundation::EventHandler<Windows::Foundation::Collections::IVector<int32_t>> const& handler)
    {
        return _nestedEvent.add(handler);
    }
    void Class::NestedEvent(winrt::event_token const& token) noexcept
    {
        _nestedEvent.remove(token);
    }
    void Class::InvokeNestedEvent(TestComponentCSharp::Class const& sender, Windows::Foundation::Collections::IVector<int32_t> const& arg0)
    {
        _nestedEvent(sender, arg0);
    }
    winrt::event_token Class::NestedTypedEvent(Windows::Foundation::TypedEventHandler<TestComponentCSharp::Class, Windows::Foundation::Collections::IVector<hstring>> const& handler)
    {
        return _nestedTypedEvent.add(handler);
    }
    void Class::NestedTypedEvent(winrt::event_token const& token) noexcept
    {
        _nestedTypedEvent.remove(token);
    }
    void Class::InvokeNestedTypedEvent(TestComponentCSharp::Class const& sender, Windows::Foundation::Collections::IVector<hstring> const& arg0)
    {
        _nestedTypedEvent(sender, arg0);
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
    winrt::event_token Class::IntPropertyChanged(Windows::Foundation::EventHandler<int32_t> const& handler)
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
    winrt::event_token Class::BoolPropertyChanged(Windows::Foundation::EventHandler<bool> const& handler)
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
    winrt::event_token Class::StringPropertyChanged(Windows::Foundation::TypedEventHandler<TestComponentCSharp::Class, hstring> const& handler)
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
    Windows::Foundation::Collections::IVector<hstring> Class::StringsProperty()
    {
        return _strings;
    }
    void Class::StringsProperty(Windows::Foundation::Collections::IVector<hstring> const& value)
    {
        _strings = value;
    }
    Windows::Foundation::IInspectable Class::ObjectProperty()
    {
        return _object;
    }
    void Class::ObjectProperty(Windows::Foundation::IInspectable const& value)
    {
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
    winrt::event_token Class::ObjectPropertyChanged(Windows::Foundation::EventHandler<Windows::Foundation::IInspectable> const& handler)
    {
        return _objectChanged.add(handler);
    }
    void Class::ObjectPropertyChanged(winrt::event_token const& token) noexcept
    {
        _objectChanged.remove(token);
    }
    Windows::Foundation::Uri Class::UriProperty()
    {
        return _uri;
    }
    void Class::UriProperty(Windows::Foundation::Uri const& value)
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
    winrt::event_token Class::UriPropertyChanged(Windows::Foundation::EventHandler<Windows::Foundation::Uri> const& handler)
    {
        return _uriChanged.add(handler);
    }
    void Class::UriPropertyChanged(winrt::event_token const& token) noexcept
    {
        _uriChanged.remove(token);
    }

    Windows::Foundation::IAsyncOperation<int32_t> Class::GetIntAsync()
    {
        co_return _int;
    }
    Windows::Foundation::IAsyncOperationWithProgress<hstring, int32_t> Class::GetStringAsync()
    {
        co_return _string;
    }

    Windows::Foundation::Collections::IKeyValuePair<hstring, hstring> Class::StringPairProperty()
    {
        return _stringPair;
    }
    void Class::StringPairProperty(Windows::Foundation::Collections::IKeyValuePair<hstring, hstring> const& value)
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
    winrt::event_token Class::StringPairPropertyChanged(Windows::Foundation::EventHandler<Windows::Foundation::Collections::IKeyValuePair<hstring, hstring>> const& handler)
    {
        return _stringPairChanged.add(handler);
    }
    void Class::StringPairPropertyChanged(winrt::event_token const& token) noexcept
    {
        _stringPairChanged.remove(token);
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

    Windows::Foundation::Collections::IVectorView<int32_t> Class::GetIntVector()
    {
        return winrt::single_threaded_vector(std::vector{ 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }).GetView();
    }

    Windows::Foundation::Collections::IVectorView<bool> Class::GetBoolVector()
    {
        return winrt::single_threaded_vector(std::vector{ true, false, true, false }).GetView();
    }

    Windows::Foundation::Collections::IVectorView<hstring> Class::GetStringVector()
    {
        return winrt::single_threaded_vector(std::vector<hstring>{ L"String0", L"String1", L"String2", L"String3", L"String4" }).GetView();
    }

    Windows::Foundation::Collections::IVectorView<TestComponentCSharp::ComposedBlittableStruct> Class::GetBlittableStructVector()
    {
        return winrt::single_threaded_vector(std::vector{ ComposedBlittableStruct{0}, ComposedBlittableStruct{1},
            ComposedBlittableStruct{2}, ComposedBlittableStruct{3}, ComposedBlittableStruct{4} }).GetView();
    }

    Windows::Foundation::Collections::IVectorView<TestComponentCSharp::ComposedNonBlittableStruct> Class::GetNonBlittableStructVector()
    {
        return winrt::single_threaded_vector(std::vector
        {
            ComposedNonBlittableStruct{ { 0 }, { L"String0" }, { true, false, true, false }, { 0 } },
            ComposedNonBlittableStruct{ { 1 }, { L"String1" }, { false, true, false, true }, { 1 } },
            ComposedNonBlittableStruct{ { 2 }, { L"String2" }, { true, false, true, false }, { 2 } },
        }).GetView();
    }

    Windows::Foundation::Collections::IVectorView<Windows::Foundation::IInspectable> Class::GetObjectVector()
    {
        return winrt::single_threaded_vector(std::vector<IInspectable>{ winrt::box_value(0), winrt::box_value(1), winrt::box_value(2) }).GetView();
    }

    Windows::Foundation::Collections::IVectorView<TestComponentCSharp::IProperties1> Class::GetInterfaceVector()
    {
        return winrt::single_threaded_vector(std::vector<IProperties1>{ *this, *this, *this }).GetView();
    }

    Windows::Foundation::Collections::IVectorView<TestComponentCSharp::Class> Class::GetClassVector()
    {
        return winrt::single_threaded_vector(std::vector<TestComponentCSharp::Class>{ *this, *this, *this }).GetView();
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

    Windows::Foundation::IAsyncAction Class::DoitAsync()
    {
        _asyncResult = E_PENDING;
        co_await winrt::resume_background();

        auto cancel = co_await winrt::get_cancellation_token();
        cancel.callback([this]() { ::SetEvent(_syncHandle.get()); });

        co_await winrt::resume_on_signal(_syncHandle.get());
        if (cancel()) co_return;
        winrt::check_hresult(_asyncResult);
    }

    Windows::Foundation::IAsyncActionWithProgress<int32_t> Class::DoitAsyncWithProgress()
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

    Windows::Foundation::IAsyncOperation<int32_t> Class::AddAsync(int32_t lhs, int32_t rhs)
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

    Windows::Foundation::IAsyncOperationWithProgress<int32_t, int32_t> Class::AddAsyncWithProgress(int32_t lhs, int32_t rhs)
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

    Windows::Foundation::Point Class::PointProperty()
    {
        return _point;
    }

    void Class::PointProperty(Windows::Foundation::Point const& value)
    {
        _point = value;
    }

    Windows::Foundation::IReference<Windows::Foundation::Point> Class::GetPointReference()
    {
        return _point;
    }

    Windows::Foundation::TimeSpan Class::TimeSpanProperty()
    {
        return _timeSpan;
    }

    void Class::TimeSpanProperty(Windows::Foundation::TimeSpan const& value)
    {
        _timeSpan = value;
    }

    Windows::Foundation::IReference<Windows::Foundation::TimeSpan> Class::GetTimeSpanReference()
    {
        return _timeSpan;
    }

    Windows::Foundation::DateTime Class::DateTimeProperty()
    {
        return _dateTime;
    }

    void Class::DateTimeProperty(Windows::Foundation::DateTime const& value)
    {
        _dateTime = value;
    }

    Windows::Foundation::IReference<Windows::Foundation::DateTime> Class::GetDateTimeProperty()
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
    void Class::ReadWriteProperty(int32_t value)
    {
        _int = value;
    }

    Windows::Foundation::Collections::IIterable<int32_t> Class::GetIntIterable()
    {
        return _intColl;
    }
    void Class::SetIntIterable(Windows::Foundation::Collections::IIterable<int32_t> const& value)
    {
        _intColl = value;
    }

    Windows::UI::Xaml::Interop::IBindableVector Class::BindableVectorProperty()
    {
        return _bindableVector;
    }
    void Class::BindableVectorProperty(Windows::UI::Xaml::Interop::IBindableVector const& value)
    {
        _bindableVector = value;
    }
    void Class::RaiseBindableVectorChanged()
    {
        _bindableVectorChanged(*this, _bindableVector);
    }
    void Class::CallForBindableVector(TestComponentCSharp::ProvideBindableVector const& provideBindableVector)
    {
        _bindableVector = provideBindableVector();
    }
    winrt::event_token Class::BindableVectorPropertyChanged(Windows::Foundation::EventHandler<Windows::UI::Xaml::Interop::IBindableVector> const& handler)
    {
        return _bindableVectorChanged.add(handler);
    }
    void Class::BindableVectorPropertyChanged(winrt::event_token const& token) noexcept
    {
        _bindableVectorChanged.remove(token);
    }
    Windows::UI::Xaml::Interop::IBindableIterable Class::BindableIterableProperty()
    {
        throw hresult_not_implemented();
    }
    void Class::BindableIterableProperty(Windows::UI::Xaml::Interop::IBindableIterable const& value)
    {
        throw hresult_not_implemented();
    }
    Windows::UI::Xaml::Interop::IBindableIterator Class::BindableIteratorProperty()
    {
        throw hresult_not_implemented();
    }
    void Class::BindableIteratorProperty(Windows::UI::Xaml::Interop::IBindableIterator const& value)
    {
        throw hresult_not_implemented();
    }
    Windows::UI::Xaml::Interop::IBindableVectorView Class::BindableVectorViewProperty()
    {
        throw hresult_not_implemented();
    }
    void Class::BindableVectorViewProperty(Windows::UI::Xaml::Interop::IBindableVectorView const& value)
    {
        throw hresult_not_implemented();
    }
    Windows::UI::Xaml::Interop::IBindableObservableVector Class::BindableObservableVectorProperty()
    {
        throw hresult_not_implemented();
    }
    void Class::BindableObservableVectorProperty(Windows::UI::Xaml::Interop::IBindableObservableVector const& value)
    {
        throw hresult_not_implemented();
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

    int32_t Class::UnboxInt32(IInspectable const& obj)
    {
        return winrt::unbox_value<int32_t>(obj);
    }

    bool Class::UnboxBoolean(IInspectable const& obj)
    {
        return winrt::unbox_value<bool>(obj);
    }

    hstring Class::UnboxString(IInspectable const& obj)
    {
        return winrt::unbox_value<hstring>(obj);
    }

    com_array<int32_t> Class::UnboxInt32Array(IInspectable const& obj)
    {
        return obj.as<Windows::Foundation::IReferenceArray<int32_t>>().Value();
    }

    com_array<bool> Class::UnboxBooleanArray(IInspectable const& obj)
    {
        return obj.as<Windows::Foundation::IReferenceArray<bool>>().Value();
    }
    
    com_array<hstring> Class::UnboxStringArray(IInspectable const& obj)
    {
        return obj.as<Windows::Foundation::IReferenceArray<hstring>>().Value();
    }

    Windows::UI::Xaml::Interop::TypeName Class::Int32Type()
    {
        return winrt::xaml_typename<int32_t>();
    }

    Windows::UI::Xaml::Interop::TypeName Class::ReferenceInt32Type()
    {
        return winrt::xaml_typename<Windows::Foundation::IReference<int32_t>>();
    }

    Windows::UI::Xaml::Interop::TypeName Class::ThisClassType()
    {
        return winrt::xaml_typename<winrt::TestComponentCSharp::Class>();
    }

    bool Class::VerifyTypeIsInt32Type(Windows::UI::Xaml::Interop::TypeName const& type_name)
    {
        return winrt::xaml_typename<int32_t>() == type_name;
    }

    bool Class::VerifyTypeIsReferenceInt32Type(Windows::UI::Xaml::Interop::TypeName const& type_name)
    {
        return winrt::xaml_typename<Windows::Foundation::IReference<int32_t>>() == type_name;
    }

    bool Class::VerifyTypeIsThisClassType(Windows::UI::Xaml::Interop::TypeName const& type_name)
    {
        return winrt::xaml_typename<winrt::TestComponentCSharp::Class>() == type_name;
    }

    hstring Class::GetTypeNameForType(Windows::UI::Xaml::Interop::TypeName const& type)
    {
        return type.Name;
    }

}
