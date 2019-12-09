﻿#include "pch.h"
#include "Class.h"
#include "Class.g.cpp"

namespace winrt::TestComp::implementation
{
    namespace statics
    {
        static int _int {};
        static winrt::event<Windows::Foundation::EventHandler<int32_t>> _intChanged {};
        static winrt::hstring _string;
        static winrt::event<Windows::Foundation::TypedEventHandler<TestComp::Class, hstring>> _stringChanged {};
        static int _readWrite{};
    }

    Class::Class(int32_t intProperty) :
        Class(intProperty, L"")
    {
    }
    Class::Class(int32_t intProperty, hstring const& stringProperty) :
        _int(intProperty),
        _string(stringProperty)
    {
        _nonBlittableStruct.refs.ref32 = _int;
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
    winrt::event_token Class::StaticStringPropertyChanged(Windows::Foundation::TypedEventHandler<TestComp::Class, hstring> const& handler)
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
    void Class::StaticSetString(TestComp::ProvideString const& provideString)
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
    winrt::event_token Class::Event0(TestComp::EventHandler0 const& handler)
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
    winrt::event_token Class::Event1(TestComp::EventHandler1 const& handler)
    {
        return _event1.add(handler);
    }
    void Class::Event1(winrt::event_token const& token) noexcept
    {
        _event1.remove(token);
    }
    void Class::InvokeEvent1(TestComp::Class const& sender)
    {
        _event1(sender);
    }
    winrt::event_token Class::Event2(TestComp::EventHandler2 const& handler)
    {
        return _event2.add(handler);
    }
    void Class::Event2(winrt::event_token const& token) noexcept
    {
        _event2.remove(token);
    }
    void Class::InvokeEvent2(TestComp::Class const& sender, int32_t arg0)
    {
        _event2(sender, arg0);
    }
    winrt::event_token Class::Event3(TestComp::EventHandler3 const& handler)
    {
        return _event3.add(handler);
    }
    void Class::Event3(winrt::event_token const& token) noexcept
    {
        _event3.remove(token);
    }
    void Class::InvokeEvent3(TestComp::Class const& sender, int32_t arg0, hstring const& arg1)
    {
        _event3(sender, arg0, arg1);
    }
    winrt::event_token Class::CollectionEvent(TestComp::EventHandlerCollection const& handler)
    {
        return _collectionEvent.add(handler);
    }
    void Class::CollectionEvent(winrt::event_token const& token) noexcept
    {
        _collectionEvent.remove(token);
    }
    void Class::InvokeCollectionEvent(TestComp::Class const& sender, Windows::Foundation::Collections::IVector<int32_t> const& arg0, Windows::Foundation::Collections::IMap<int32_t, hstring> const& arg1)
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
    void Class::InvokeNestedEvent(TestComp::Class const& sender, Windows::Foundation::Collections::IVector<int32_t> const& arg0)
    {
        _nestedEvent(sender, arg0);
    }
    winrt::event_token Class::NestedTypedEvent(Windows::Foundation::TypedEventHandler<TestComp::Class, Windows::Foundation::Collections::IVector<hstring>> const& handler)
    {
        return _nestedTypedEvent.add(handler);
    }
    void Class::NestedTypedEvent(winrt::event_token const& token) noexcept
    {
        _nestedTypedEvent.remove(token);
    }
    void Class::InvokeNestedTypedEvent(TestComp::Class const& sender, Windows::Foundation::Collections::IVector<hstring> const& arg0)
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
    void Class::CallForInt(TestComp::ProvideInt const& provideInt)
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
    void Class::CallForBool(TestComp::ProvideBool const& provideBool)
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
    winrt::event_token Class::StringPropertyChanged(Windows::Foundation::TypedEventHandler<TestComp::Class, hstring> const& handler)
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
    void Class::CallForString(TestComp::ProvideString const& provideString)
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
    void Class::CallForObject(TestComp::ProvideObject const& provideObject)
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

    TestComp::ComposedNonBlittableStruct Class::ComposedNonBlittableStructProperty()
    {
        return _nonBlittableStruct;
    }

    void Class::ComposedNonBlittableStructProperty(TestComp::ComposedNonBlittableStruct const& value)
    {
        _nonBlittableStruct = value;
    }

    TestComp::ComposedNonBlittableStruct Class::GetComposedNonBlittableStruct()
    {
        return _nonBlittableStruct;
    }

    void Class::OutComposedNonBlittableStruct(TestComp::ComposedNonBlittableStruct& value)
    {
        value = _nonBlittableStruct;
    }

    void Class::SetComposedNonBlittableStruct(TestComp::ComposedNonBlittableStruct const& value)
    {
        _nonBlittableStruct = value;
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

    // IVector<String>
    //Windows::Foundation::Collections::IIterator<hstring> Class::First()
    //{
    //    throw hresult_not_implemented();
    //}
    //hstring Class::GetAt(uint32_t index)
    //{
    //    throw hresult_not_implemented();
    //}
    //uint32_t Class::Size()
    //{
    //    throw hresult_not_implemented();
    //}
    //Windows::Foundation::Collections::IVectorView<hstring> Class::GetView()
    //{
    //    throw hresult_not_implemented();
    //}
    //bool Class::IndexOf(hstring const& value, uint32_t& index)
    //{
    //    throw hresult_not_implemented();
    //}
    //void Class::SetAt(uint32_t index, hstring const& value)
    //{
    //    throw hresult_not_implemented();
    //}
    //void Class::InsertAt(uint32_t index, hstring const& value)
    //{
    //    throw hresult_not_implemented();
    //}
    //void Class::RemoveAt(uint32_t index)
    //{
    //    throw hresult_not_implemented();
    //}
    //void Class::Append(hstring const& value)
    //{
    //    throw hresult_not_implemented();
    //}
    //void Class::RemoveAtEnd()
    //{
    //    throw hresult_not_implemented();
    //}
    //void Class::Clear()
    //{
    //    throw hresult_not_implemented();
    //}
    //uint32_t Class::GetMany(uint32_t startIndex, array_view<hstring> items)
    //{
    //    throw hresult_not_implemented();
    //}
    //void Class::ReplaceAll(array_view<hstring const> items)
    //{
    //    throw hresult_not_implemented();
    //}

    // IMap<Int32, String>
    //Windows::Foundation::Collections::IIterator<Windows::Foundation::Collections::IKeyValuePair<int32_t, hstring>> Class::First()
    //{
    //    throw hresult_not_implemented();
    //}
    //hstring Class::Lookup(int32_t const& key)
    //{
    //    throw hresult_not_implemented();
    //}
    //uint32_t Class::Size()
    //{
    //    throw hresult_not_implemented();
    //}
    //bool Class::HasKey(int32_t const& key)
    //{
    //    throw hresult_not_implemented();
    //}
    //Windows::Foundation::Collections::IMapView<int32_t, hstring> Class::GetView()
    //{
    //    throw hresult_not_implemented();
    //}
    //bool Class::Insert(int32_t const& key, hstring const& value)
    //{
    //    throw hresult_not_implemented();
    //}
    //void Class::Remove(int32_t const& key)
    //{
    //    throw hresult_not_implemented();
    //}
    //void Class::Clear()
    //{
    //    throw hresult_not_implemented();
    //}
}
