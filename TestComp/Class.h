#pragma once

#include "Class.g.h"
#include "winrt/Windows.Foundation.Collections.h"

namespace winrt::TestComp::implementation
{
    struct Class : ClassT<Class>
    {
        Class() : Class(0, L"")
        {
            _strings = winrt::single_threaded_vector<hstring>({ L"foo", L"bar" });
        }

        winrt::event<EventHandler0> _event0;
        winrt::event<EventHandler1> _event1;
        winrt::event<EventHandler2> _event2;
        winrt::event<EventHandler3> _event3;
        winrt::event<EventHandlerCollection> _collectionEvent;
        winrt::event<Windows::Foundation::EventHandler<Windows::Foundation::Collections::IVector<int32_t>>> _nestedEvent;
        winrt::event<Windows::Foundation::TypedEventHandler<TestComp::Class, Windows::Foundation::Collections::IVector<hstring>>> _nestedTypedEvent;

        int32_t _int = 0;
        winrt::event<Windows::Foundation::EventHandler<int32_t>> _intChanged;
        bool _bool = false;
        winrt::event<Windows::Foundation::EventHandler<bool>> _boolChanged;
        winrt::hstring _string;
        winrt::hstring _string2;
        winrt::event<Windows::Foundation::TypedEventHandler<TestComp::Class, hstring>> _stringChanged;
        Windows::Foundation::Collections::IVector<hstring> _strings;
        Windows::Foundation::IInspectable _object;
        winrt::event<Windows::Foundation::EventHandler<Windows::Foundation::IInspectable>> _objectChanged;
        ComposedBlittableStruct _blittableStruct{};
        ComposedNonBlittableStruct _nonBlittableStruct{};
        std::vector<int32_t> _ints{ 1, 2, 3 };
        winrt::handle _syncHandle;
        int32_t _asyncResult;
        int32_t _asyncProgress;
        Windows::Foundation::Point _point{};
        Windows::Foundation::TimeSpan _timeSpan{};
        Windows::Foundation::DateTime _dateTime{};

        Class(int32_t intProperty);
        Class(int32_t intProperty, hstring const& stringProperty);
        static int32_t StaticIntProperty();
        static void StaticIntProperty(int32_t value);
        static winrt::event_token StaticIntPropertyChanged(Windows::Foundation::EventHandler<int32_t> const& handler);
        static void StaticIntPropertyChanged(winrt::event_token const& token) noexcept;
        static hstring StaticStringProperty();
        static void StaticStringProperty(hstring const& value);
        static winrt::event_token StaticStringPropertyChanged(Windows::Foundation::TypedEventHandler<TestComp::Class, hstring> const& handler);
        static void StaticStringPropertyChanged(winrt::event_token const& token) noexcept;
        static void StaticGetString();
        static void StaticSetString(TestComp::ProvideString const& provideString);
        static int32_t StaticReadWriteProperty();
        static void StaticReadWriteProperty(int32_t value);
        static Windows::Foundation::TimeSpan FromSeconds(int32_t seconds);
        static Windows::Foundation::DateTime Now();
        winrt::event_token Event0(TestComp::EventHandler0 const& handler);
        void Event0(winrt::event_token const& token) noexcept;
        void InvokeEvent0();
        winrt::event_token Event1(TestComp::EventHandler1 const& handler);
        void Event1(winrt::event_token const& token) noexcept;
        void InvokeEvent1(TestComp::Class const& sender);
        winrt::event_token Event2(TestComp::EventHandler2 const& handler);
        void Event2(winrt::event_token const& token) noexcept;
        void InvokeEvent2(TestComp::Class const& sender, int32_t arg0);
        winrt::event_token Event3(TestComp::EventHandler3 const& handler);
        void Event3(winrt::event_token const& token) noexcept;
        void InvokeEvent3(TestComp::Class const& sender, int32_t arg0, hstring const& arg1);
        winrt::event_token CollectionEvent(TestComp::EventHandlerCollection const& handler);
        void CollectionEvent(winrt::event_token const& token) noexcept;
        void InvokeCollectionEvent(TestComp::Class const& sender, Windows::Foundation::Collections::IVector<int32_t> const& arg0, Windows::Foundation::Collections::IMap<int32_t, hstring> const& arg1);
        winrt::event_token NestedEvent(Windows::Foundation::EventHandler<Windows::Foundation::Collections::IVector<int32_t>> const& handler);
        void NestedEvent(winrt::event_token const& token) noexcept;
        void InvokeNestedEvent(TestComp::Class const& sender, Windows::Foundation::Collections::IVector<int32_t> const& arg0);
        winrt::event_token NestedTypedEvent(Windows::Foundation::TypedEventHandler<TestComp::Class, Windows::Foundation::Collections::IVector<hstring>> const& handler);
        void NestedTypedEvent(winrt::event_token const& token) noexcept;
        void InvokeNestedTypedEvent(TestComp::Class const& sender, Windows::Foundation::Collections::IVector<hstring> const& arg0);
        int32_t IntProperty();
        void IntProperty(int32_t value);
        winrt::event_token IntPropertyChanged(Windows::Foundation::EventHandler<int32_t> const& handler);
        void IntPropertyChanged(winrt::event_token const& token) noexcept;
        void RaiseIntChanged();
        void CallForInt(TestComp::ProvideInt const& provideInt);
        bool BoolProperty();
        void BoolProperty(bool value);
        winrt::event_token BoolPropertyChanged(Windows::Foundation::EventHandler<bool> const& handler);
        void BoolPropertyChanged(winrt::event_token const& token) noexcept;
        void RaiseBoolChanged();
        void CallForBool(TestComp::ProvideBool const& provideBool);
        hstring StringProperty();
        void StringProperty(hstring const& value);
        winrt::event_token StringPropertyChanged(Windows::Foundation::TypedEventHandler<TestComp::Class, hstring> const& handler);
        void StringPropertyChanged(winrt::event_token const& token) noexcept;
        void RaiseStringChanged();
        void CallForString(TestComp::ProvideString const& provideString);
        hstring StringProperty2();
        void StringProperty2(hstring const& value);
        Windows::Foundation::Collections::IVector<hstring> StringsProperty();
        void StringsProperty(Windows::Foundation::Collections::IVector<hstring> const& value);
        Windows::Foundation::IInspectable ObjectProperty();
        void ObjectProperty(Windows::Foundation::IInspectable const& value);
        void RaiseObjectChanged();
        void CallForObject(TestComp::ProvideObject const& provideObject);
        winrt::event_token ObjectPropertyChanged(Windows::Foundation::EventHandler<Windows::Foundation::IInspectable> const& handler);
        void ObjectPropertyChanged(winrt::event_token const& token) noexcept;
        BlittableStruct BlittableStructProperty();
        void BlittableStructProperty(BlittableStruct const& value);
        BlittableStruct GetBlittableStruct();
        void OutBlittableStruct(BlittableStruct& value);
        void SetBlittableStruct(BlittableStruct const& value);
        ComposedBlittableStruct ComposedBlittableStructProperty();
        void ComposedBlittableStructProperty(ComposedBlittableStruct const& value);
        ComposedBlittableStruct GetComposedBlittableStruct();
        void OutComposedBlittableStruct(ComposedBlittableStruct& value);
        void SetComposedBlittableStruct(ComposedBlittableStruct const& value);
        NonBlittableStringStruct NonBlittableStringStructProperty();
        void NonBlittableStringStructProperty(NonBlittableStringStruct const& value);
        NonBlittableStringStruct GetNonBlittableStringStruct();
        void OutNonBlittableStringStruct(NonBlittableStringStruct& value);
        void SetNonBlittableStringStruct(NonBlittableStringStruct const& value);
        NonBlittableBoolStruct NonBlittableBoolStructProperty();
        void NonBlittableBoolStructProperty(NonBlittableBoolStruct const& value);
        NonBlittableBoolStruct GetNonBlittableBoolStruct();
        void OutNonBlittableBoolStruct(NonBlittableBoolStruct& value);
        void SetNonBlittableBoolStruct(NonBlittableBoolStruct const& value);
        NonBlittableRefStruct NonBlittableRefStructProperty();
        void NonBlittableRefStructProperty(NonBlittableRefStruct const& value);
        NonBlittableRefStruct GetNonBlittableRefStruct();
        void OutNonBlittableRefStruct(NonBlittableRefStruct& value);
        void SetNonBlittableRefStruct(NonBlittableRefStruct const& value);
        ComposedNonBlittableStruct ComposedNonBlittableStructProperty();
        void ComposedNonBlittableStructProperty(ComposedNonBlittableStruct const& value);
        ComposedNonBlittableStruct GetComposedNonBlittableStruct();
        void OutComposedNonBlittableStruct(ComposedNonBlittableStruct& value);
        void SetComposedNonBlittableStruct(ComposedNonBlittableStruct const& value);
        void SetInts(array_view<int32_t const> ints);
        com_array<int32_t> GetInts();
        void FillInts(array_view<int32_t> ints);

        Windows::Foundation::IAsyncOperation<int32_t> GetIntAsync();
        Windows::Foundation::IAsyncOperationWithProgress<hstring, int32_t> GetStringAsync();

        Windows::Foundation::Collections::IVectorView<int32_t> GetIntVector();
        Windows::Foundation::Collections::IVectorView<bool> GetBoolVector();
        Windows::Foundation::Collections::IVectorView<hstring> GetStringVector();
        Windows::Foundation::Collections::IVectorView<TestComp::ComposedBlittableStruct> GetBlittableStructVector();
        Windows::Foundation::Collections::IVectorView<TestComp::ComposedNonBlittableStruct> GetNonBlittableStructVector();
        Windows::Foundation::Collections::IVectorView<Windows::Foundation::IInspectable> GetObjectVector();
        Windows::Foundation::Collections::IVectorView<TestComp::IProperties1> GetInterfaceVector();
        Windows::Foundation::Collections::IVectorView<TestComp::Class> GetClassVector();

        void CompleteAsync();
        void CompleteAsync(int32_t hr);
        void AdvanceAsync(int32_t delta);
        Windows::Foundation::IAsyncAction DoitAsync();
        Windows::Foundation::IAsyncActionWithProgress<int32_t> DoitAsyncWithProgress();
        Windows::Foundation::IAsyncOperation<int32_t> AddAsync(int32_t lhs, int32_t rhs);
        Windows::Foundation::IAsyncOperationWithProgress<int32_t, int32_t> AddAsyncWithProgress(int32_t lhs, int32_t rhs);

        Windows::Foundation::Point PointProperty();
        void PointProperty(Windows::Foundation::Point const& value);
        Windows::Foundation::IReference<Windows::Foundation::Point> GetPointReference();
        Windows::Foundation::TimeSpan TimeSpanProperty();
        void TimeSpanProperty(Windows::Foundation::TimeSpan const& value);
        Windows::Foundation::IReference<Windows::Foundation::TimeSpan> GetTimeSpanReference();
        Windows::Foundation::DateTime DateTimeProperty();
        void DateTimeProperty(Windows::Foundation::DateTime const& value);
        Windows::Foundation::IReference<Windows::Foundation::DateTime> GetDateTimeProperty();

        // IStringable
        hstring ToString();

        // Property test interfaces
        //void Draw();
        //void Draw(hstring const& gunModel);
        //hstring DrawTo();
        //void Draw();
        //void Draw(int32_t figureSides);
        //int32_t DrawTo();
        int32_t ReadWriteProperty();
        //int32_t DistinctProperty();
        void ReadWriteProperty(int32_t value);
        //hstring DistinctProperty();
        //void DistinctProperty(hstring const& value);

        // IVector<String>
        //Windows::Foundation::Collections::IIterator<hstring> First();
        //hstring GetAt(uint32_t index);
        //uint32_t Size();
        //Windows::Foundation::Collections::IVectorView<hstring> GetView();
        //bool IndexOf(hstring const& value, uint32_t& index);
        //void SetAt(uint32_t index, hstring const& value);
        //void InsertAt(uint32_t index, hstring const& value);
        //void RemoveAt(uint32_t index);
        //void Append(hstring const& value);
        //void RemoveAtEnd();
        //void Clear();
        //uint32_t GetMany(uint32_t startIndex, array_view<hstring> items);
        //void ReplaceAll(array_view<hstring const> items);

        // IMap<Int32, String>
        //Windows::Foundation::Collections::IIterator<Windows::Foundation::Collections::IKeyValuePair<int32_t, hstring>> First();
        //hstring Lookup(int32_t const& key);
        //uint32_t Size();
        //bool HasKey(int32_t const& key);
        //Windows::Foundation::Collections::IMapView<int32_t, hstring> GetView();
        //bool Insert(int32_t const& key, hstring const& value);
        //void Remove(int32_t const& key);
        //void Clear();
    };
}

namespace winrt::TestComp::factory_implementation
{
    struct Class : ClassT<Class, implementation::Class>
    {
    };
}
