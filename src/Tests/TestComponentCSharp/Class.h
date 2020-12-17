﻿#pragma once

#include "Class.g.h"
#include "winrt/Windows.Foundation.Collections.h"

namespace winrt::TestComponentCSharp::implementation
{
    struct Class : ClassT<Class>
    {
        Class();

        Windows::UI::Xaml::Interop::TypeName _typeProperty;
        void TypeProperty(Windows::UI::Xaml::Interop::TypeName val);
        Windows::UI::Xaml::Interop::TypeName TypeProperty();
        winrt::hstring GetTypePropertyAbiName();
        winrt::hstring GetTypePropertyKind();

        winrt::event<EventHandler0> _event0;
        winrt::event<EventHandler1> _event1;
        winrt::event<EventHandler2> _event2;
        winrt::event<EventHandler3> _event3;
        winrt::event<EventHandlerCollection> _collectionEvent;
        winrt::event<Windows::Foundation::EventHandler<Windows::Foundation::Collections::IVector<int32_t>>> _nestedEvent;
        winrt::event<Windows::Foundation::TypedEventHandler<TestComponentCSharp::Class, Windows::Foundation::Collections::IVector<hstring>>> _nestedTypedEvent;
        winrt::event<TestComponentCSharp::EventWithReturn> _returnEvent;

        Windows::Foundation::Collections::IVector<Windows::Foundation::IInspectable> GetUriVectorAsIInspectableVector();

        int32_t _int = 0;
        winrt::event<Windows::Foundation::EventHandler<int32_t>> _intChanged;
        bool _bool = false;
        winrt::event<Windows::Foundation::EventHandler<bool>> _boolChanged;
        winrt::hstring _string;
        winrt::hstring _string2;
        winrt::event<Windows::Foundation::TypedEventHandler<TestComponentCSharp::Class, hstring>> _stringChanged;
        Windows::Foundation::Collections::IVector<hstring> _strings;
        Windows::Foundation::IInspectable _object;
        winrt::event<Windows::Foundation::EventHandler<Windows::Foundation::IInspectable>> _objectChanged;
        Windows::Foundation::Collections::IIterable<Windows::Foundation::IInspectable> _objectIterable;
        Windows::Foundation::Collections::IIterable<Windows::Foundation::Collections::IIterable<Windows::Foundation::Point>> _pointIterableIterable;
        Windows::Foundation::Collections::IIterable<Windows::Foundation::Collections::IIterable<Windows::Foundation::IInspectable>> _objectIterableIterable;
        winrt::event<Windows::Foundation::EventHandler<Windows::Foundation::Collections::IIterable<Windows::Foundation::IInspectable>>> _objectIterableChanged;
        Windows::Foundation::Uri _uri;
        winrt::event<Windows::Foundation::EventHandler<Windows::Foundation::Uri>> _uriChanged;
        Windows::Foundation::Collections::IKeyValuePair<hstring, hstring> _stringPair;
        winrt::event<Windows::Foundation::EventHandler<Windows::Foundation::Collections::IKeyValuePair<hstring, hstring>>> _stringPairChanged;
        ComposedBlittableStruct _blittableStruct{};
        ComposedNonBlittableStruct _nonBlittableStruct{};
        std::vector<int32_t> _ints{ 1, 2, 3 };
        Windows::Foundation::Collections::IIterable<int32_t> _intColl;
        Microsoft::UI::Xaml::Interop::IBindableIterable _bindableIterable;
        Microsoft::UI::Xaml::Interop::IBindableVector _bindableVector;
        Microsoft::UI::Xaml::Interop::IBindableObservableVector _bindableObservable;
        winrt::event<Windows::Foundation::EventHandler<Microsoft::UI::Xaml::Interop::IBindableIterable>> _bindableIterableChanged;
        winrt::event<Windows::Foundation::EventHandler<Microsoft::UI::Xaml::Interop::IBindableVector>> _bindableVectorChanged;
        winrt::handle _syncHandle;
        int32_t _asyncResult;
        int32_t _asyncProgress;
        Windows::Foundation::Point _point{};
        Windows::Foundation::Rect _rect{};
        Windows::Foundation::Size _size{};
        Windows::UI::Color _color{};
        Microsoft::UI::Xaml::CornerRadius _cornerRadius{};
        Microsoft::UI::Xaml::Duration _duration{};
        Microsoft::UI::Xaml::GridLength _gridLength{};
        Microsoft::UI::Xaml::Thickness _thickness{};
        Microsoft::UI::Xaml::Controls::Primitives::GeneratorPosition _generatorPosition{};
        Microsoft::UI::Xaml::Media::Matrix _matrix{};
        Microsoft::UI::Xaml::Media::Animation::KeyTime _keyTime{};
        Microsoft::UI::Xaml::Media::Animation::RepeatBehavior _repeatBehavior{};
        Microsoft::UI::Xaml::Media::Media3D::Matrix3D _matrix3D{};
        Windows::Foundation::Numerics::float3x2 _matrix3x2;
        Windows::Foundation::Numerics::float4x4 _matrix4x4;
        Windows::Foundation::Numerics::plane _plane;
        Windows::Foundation::Numerics::quaternion _quaternion;
        Windows::Foundation::Numerics::float2 _vector2;
        Windows::Foundation::Numerics::float3 _vector3;
        Windows::Foundation::Numerics::float4 _vector4;
        Windows::Foundation::TimeSpan _timeSpan{};
        Windows::Foundation::DateTime _dateTime{};
        winrt::hresult _hr;

        Class(int32_t intProperty);
        Class(int32_t intProperty, hstring const& stringProperty);
        static int32_t StaticIntProperty();
        static void StaticIntProperty(int32_t value);
        static winrt::event_token StaticIntPropertyChanged(Windows::Foundation::EventHandler<int32_t> const& handler);
        static void StaticIntPropertyChanged(winrt::event_token const& token) noexcept;
        static hstring StaticStringProperty();
        static void StaticStringProperty(hstring const& value);
        static winrt::event_token StaticStringPropertyChanged(Windows::Foundation::TypedEventHandler<TestComponentCSharp::Class, hstring> const& handler);
        static void StaticStringPropertyChanged(winrt::event_token const& token) noexcept;
        static void StaticGetString();
        static void StaticSetString(TestComponentCSharp::ProvideString const& provideString);
        static int32_t StaticReadWriteProperty();
        static void StaticReadWriteProperty(int32_t value);
        static Windows::Foundation::TimeSpan FromSeconds(int32_t seconds);
        static Windows::Foundation::DateTime Now();
        winrt::event_token Event0(TestComponentCSharp::EventHandler0 const& handler);
        void Event0(winrt::event_token const& token) noexcept;
        void InvokeEvent0();
        winrt::event_token Event1(TestComponentCSharp::EventHandler1 const& handler);
        void Event1(winrt::event_token const& token) noexcept;
        void InvokeEvent1(TestComponentCSharp::Class const& sender);
        winrt::event_token Event2(TestComponentCSharp::EventHandler2 const& handler);
        void Event2(winrt::event_token const& token) noexcept;
        void InvokeEvent2(TestComponentCSharp::Class const& sender, int32_t arg0);
        winrt::event_token Event3(TestComponentCSharp::EventHandler3 const& handler);
        void Event3(winrt::event_token const& token) noexcept;
        void InvokeEvent3(TestComponentCSharp::Class const& sender, int32_t arg0, hstring const& arg1);
        winrt::event_token CollectionEvent(TestComponentCSharp::EventHandlerCollection const& handler);
        void CollectionEvent(winrt::event_token const& token) noexcept;
        void InvokeCollectionEvent(TestComponentCSharp::Class const& sender, Windows::Foundation::Collections::IVector<int32_t> const& arg0, Windows::Foundation::Collections::IMap<int32_t, hstring> const& arg1);
        winrt::event_token NestedEvent(Windows::Foundation::EventHandler<Windows::Foundation::Collections::IVector<int32_t>> const& handler);
        void NestedEvent(winrt::event_token const& token) noexcept;
        void InvokeNestedEvent(TestComponentCSharp::Class const& sender, Windows::Foundation::Collections::IVector<int32_t> const& arg0);
        winrt::event_token NestedTypedEvent(Windows::Foundation::TypedEventHandler<TestComponentCSharp::Class, Windows::Foundation::Collections::IVector<hstring>> const& handler);
        void NestedTypedEvent(winrt::event_token const& token) noexcept;
        void InvokeNestedTypedEvent(TestComponentCSharp::Class const& sender, Windows::Foundation::Collections::IVector<hstring> const& arg0);
        winrt::event_token ReturnEvent(TestComponentCSharp::EventWithReturn const& handler);
        void ReturnEvent(winrt::event_token const& token) noexcept;
        int32_t InvokeReturnEvent(int32_t const& arg0);

        int32_t IntProperty();
        void IntProperty(int32_t value);
        winrt::event_token IntPropertyChanged(Windows::Foundation::EventHandler<int32_t> const& handler);
        void IntPropertyChanged(winrt::event_token const& token) noexcept;
        void RaiseIntChanged();
        void CallForInt(TestComponentCSharp::ProvideInt const& provideInt);
        bool BoolProperty();
        void BoolProperty(bool value);
        winrt::event_token BoolPropertyChanged(Windows::Foundation::EventHandler<bool> const& handler);
        void BoolPropertyChanged(winrt::event_token const& token) noexcept;
        void RaiseBoolChanged();
        void CallForBool(TestComponentCSharp::ProvideBool const& provideBool);
        hstring StringProperty();
        void StringProperty(hstring const& value);
        winrt::event_token StringPropertyChanged(Windows::Foundation::TypedEventHandler<TestComponentCSharp::Class, hstring> const& handler);
        void StringPropertyChanged(winrt::event_token const& token) noexcept;
        void RaiseStringChanged();
        void CallForString(TestComponentCSharp::ProvideString const& provideString);
        void AddUriHandler(TestComponentCSharp::IUriHandler uriHandler);
        hstring StringProperty2();
        void StringProperty2(hstring const& value);
        Windows::Foundation::Collections::IVector<hstring> StringsProperty();
        void StringsProperty(Windows::Foundation::Collections::IVector<hstring> const& value);
        Windows::Foundation::IInspectable ObjectProperty();
        void ObjectProperty(Windows::Foundation::IInspectable const& value);
        void RaiseObjectChanged();
        void CallForObject(TestComponentCSharp::ProvideObject const& provideObject);
        winrt::event_token ObjectPropertyChanged(Windows::Foundation::EventHandler<Windows::Foundation::IInspectable> const& handler);
        Windows::Foundation::Collections::IIterable<Windows::Foundation::IInspectable> ObjectIterableProperty();
        void ObjectIterableProperty(Windows::Foundation::Collections::IIterable<Windows::Foundation::IInspectable> const& value);
        void RaiseObjectIterableChanged();
        void CallForObjectIterable(TestComponentCSharp::ProvideObjectIterable const& provideObjectIterable);
        winrt::event_token ObjectIterablePropertyChanged(Windows::Foundation::EventHandler<Windows::Foundation::Collections::IIterable<Windows::Foundation::IInspectable>> const& handler);
        void ObjectIterablePropertyChanged(winrt::event_token const& token) noexcept;
        Windows::Foundation::Collections::IIterable<Windows::Foundation::Collections::IIterable<Windows::Foundation::Point>> IterableOfPointIterablesProperty();
        void IterableOfPointIterablesProperty(Windows::Foundation::Collections::IIterable<Windows::Foundation::Collections::IIterable<Windows::Foundation::Point>> const& value);
        Windows::Foundation::Collections::IIterable<Windows::Foundation::Collections::IIterable<Windows::Foundation::IInspectable>> IterableOfObjectIterablesProperty();
        void IterableOfObjectIterablesProperty(Windows::Foundation::Collections::IIterable<Windows::Foundation::Collections::IIterable<Windows::Foundation::IInspectable>> const& value);
        Windows::Foundation::Uri UriProperty();
        void UriProperty(Windows::Foundation::Uri const& value);
        void RaiseUriChanged();
        void CallForUri(TestComponentCSharp::ProvideUri const& provideUri);
        winrt::event_token UriPropertyChanged(Windows::Foundation::EventHandler<Windows::Foundation::Uri> const& handler);
        void UriPropertyChanged(winrt::event_token const& token) noexcept;
        void ObjectPropertyChanged(winrt::event_token const& token) noexcept;
        Windows::Foundation::Collections::IKeyValuePair<hstring, hstring> StringPairProperty();
        void StringPairProperty(Windows::Foundation::Collections::IKeyValuePair<hstring, hstring> const& value);
        void RaiseStringPairChanged();
        void CallForStringPair(TestComponentCSharp::ProvideStringPair const& provideStringPair);
        winrt::event_token StringPairPropertyChanged(Windows::Foundation::EventHandler<Windows::Foundation::Collections::IKeyValuePair<hstring, hstring>> const& handler);
        void StringPairPropertyChanged(winrt::event_token const& token) noexcept;
        TestComponentCSharp::ProvideUri GetUriDelegate() noexcept;
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
        Windows::Foundation::Collections::IVectorView<TestComponentCSharp::ComposedBlittableStruct> GetBlittableStructVector();
        Windows::Foundation::Collections::IVectorView<TestComponentCSharp::ComposedNonBlittableStruct> GetNonBlittableStructVector();
        Windows::Foundation::Collections::IVectorView<Windows::Foundation::IInspectable> GetObjectVector();
        Windows::Foundation::Collections::IVectorView<TestComponentCSharp::IProperties1> GetInterfaceVector();
        Windows::Foundation::Collections::IVectorView<TestComponentCSharp::Class> GetClassVector() noexcept;

        Windows::Foundation::Collections::IIterable<int32_t> GetIntIterable();
        void SetIntIterable(Windows::Foundation::Collections::IIterable<int32_t> const& value);

        Microsoft::UI::Xaml::Interop::IBindableIterable BindableIterableProperty();
        void BindableIterableProperty(Microsoft::UI::Xaml::Interop::IBindableIterable const& value);
        void RaiseBindableIterableChanged();
        void CallForBindableIterable(TestComponentCSharp::ProvideBindableIterable const& provideBindableIterable);
        winrt::event_token BindableIterablePropertyChanged(Windows::Foundation::EventHandler<Microsoft::UI::Xaml::Interop::IBindableIterable> const& handler);
        void BindableIterablePropertyChanged(winrt::event_token const& token) noexcept;
        Microsoft::UI::Xaml::Interop::IBindableVector BindableVectorProperty();
        void BindableVectorProperty(Microsoft::UI::Xaml::Interop::IBindableVector const& value);
        void RaiseBindableVectorChanged();
        void CallForBindableVector(TestComponentCSharp::ProvideBindableVector const& provideBindableVector);
        winrt::event_token BindableVectorPropertyChanged(Windows::Foundation::EventHandler<Microsoft::UI::Xaml::Interop::IBindableVector> const& handler);
        void BindableVectorPropertyChanged(winrt::event_token const& token) noexcept;
        Microsoft::UI::Xaml::Interop::IBindableObservableVector BindableObservableVectorProperty();
        void BindableObservableVectorProperty(Microsoft::UI::Xaml::Interop::IBindableObservableVector const& value);

        void CopyProperties(TestComponentCSharp::IProperties1 const& src);
        void CopyPropertiesViaWeakReference(TestComponentCSharp::IProperties1 const& src);

        void CompleteAsync();
        void CompleteAsync(int32_t hr);
        void AdvanceAsync(int32_t delta);
        Windows::Foundation::IAsyncAction DoitAsync();
        Windows::Foundation::IAsyncActionWithProgress<int32_t> DoitAsyncWithProgress();
        Windows::Foundation::IAsyncOperation<int32_t> AddAsync(int32_t lhs, int32_t rhs);
        Windows::Foundation::IAsyncOperationWithProgress<int32_t, int32_t> AddAsyncWithProgress(int32_t lhs, int32_t rhs);

        Windows::Foundation::Point PointProperty();
        void PointProperty(Windows::Foundation::Point const& value);
        Windows::Foundation::Rect RectProperty();
        void RectProperty(Windows::Foundation::Rect const& value);
        Windows::Foundation::Size SizeProperty();
        void SizeProperty(Windows::Foundation::Size const& value);
        Windows::UI::Color ColorProperty();
        void ColorProperty(Windows::UI::Color const& value);
        Microsoft::UI::Xaml::CornerRadius CornerRadiusProperty();
        void CornerRadiusProperty(Microsoft::UI::Xaml::CornerRadius const& value);
        Microsoft::UI::Xaml::Duration DurationProperty();
        void DurationProperty(Microsoft::UI::Xaml::Duration const& value);
        Microsoft::UI::Xaml::GridLength GridLengthProperty();
        void GridLengthProperty(Microsoft::UI::Xaml::GridLength const& value);
        Microsoft::UI::Xaml::Thickness ThicknessProperty();
        void ThicknessProperty(Microsoft::UI::Xaml::Thickness const& value);
        Microsoft::UI::Xaml::Controls::Primitives::GeneratorPosition GeneratorPositionProperty();
        void GeneratorPositionProperty(Microsoft::UI::Xaml::Controls::Primitives::GeneratorPosition const& value);
        Microsoft::UI::Xaml::Media::Matrix MatrixProperty();
        void MatrixProperty(Microsoft::UI::Xaml::Media::Matrix const& value);
        Microsoft::UI::Xaml::Media::Animation::KeyTime KeyTimeProperty();
        void KeyTimeProperty(Microsoft::UI::Xaml::Media::Animation::KeyTime const& value);
        Microsoft::UI::Xaml::Media::Animation::RepeatBehavior RepeatBehaviorProperty();
        void RepeatBehaviorProperty(Microsoft::UI::Xaml::Media::Animation::RepeatBehavior const& value);
        Microsoft::UI::Xaml::Media::Media3D::Matrix3D Matrix3DProperty();
        void Matrix3DProperty(Microsoft::UI::Xaml::Media::Media3D::Matrix3D const& value);
        Windows::Foundation::Numerics::float3x2 Matrix3x2Property();
        void Matrix3x2Property(Windows::Foundation::Numerics::float3x2 const& value);
        Windows::Foundation::Numerics::float4x4 Matrix4x4Property();
        void Matrix4x4Property(Windows::Foundation::Numerics::float4x4 const& value);
        Windows::Foundation::Numerics::plane PlaneProperty();
        void PlaneProperty(Windows::Foundation::Numerics::plane const& value);
        Windows::Foundation::Numerics::quaternion QuaternionProperty();
        void QuaternionProperty(Windows::Foundation::Numerics::quaternion const& value);
        Windows::Foundation::Numerics::float2 Vector2Property();
        void Vector2Property(Windows::Foundation::Numerics::float2 const& value);
        Windows::Foundation::Numerics::float3 Vector3Property();
        void Vector3Property(Windows::Foundation::Numerics::float3 const& value);
        Windows::Foundation::Numerics::float4 Vector4Property();
        void Vector4Property(Windows::Foundation::Numerics::float4 const& value);
        Windows::Foundation::IReference<Windows::Foundation::Point> GetPointReference();
        Windows::Foundation::TimeSpan TimeSpanProperty();
        void TimeSpanProperty(Windows::Foundation::TimeSpan const& value);
        Windows::Foundation::IReference<Windows::Foundation::TimeSpan> GetTimeSpanReference();
        Windows::Foundation::DateTime DateTimeProperty();
        void DateTimeProperty(Windows::Foundation::DateTime const& value);
        Windows::Foundation::IReference<Windows::Foundation::DateTime> GetDateTimeProperty();
        winrt::hresult HResultProperty();
        void HResultProperty(winrt::hresult const& value);

        static int32_t UnboxInt32(IInspectable const& obj);
        static bool UnboxBoolean(IInspectable const& obj);
        static hstring UnboxString(IInspectable const& obj);
        static com_array<int32_t> UnboxInt32Array(IInspectable const& obj);
        static com_array<bool> UnboxBooleanArray(IInspectable const& obj);
        static com_array<hstring> UnboxStringArray(IInspectable const& obj);

        static Windows::UI::Xaml::Interop::TypeName Int32Type();
        static Windows::UI::Xaml::Interop::TypeName ReferenceInt32Type();
        static Windows::UI::Xaml::Interop::TypeName ThisClassType();
        static bool VerifyTypeIsInt32Type(Windows::UI::Xaml::Interop::TypeName const& type_name);
        static bool VerifyTypeIsReferenceInt32Type(Windows::UI::Xaml::Interop::TypeName const& type_name);
        static bool VerifyTypeIsThisClassType(Windows::UI::Xaml::Interop::TypeName const& type_name);
        static hstring GetTypeNameForType(Windows::UI::Xaml::Interop::TypeName const& type);

        static Windows::Foundation::IInspectable EmptyString();

        hstring Catch(hstring const& params, hstring& locks);

        static IProperties1 NativeProperties1();
        static Windows::Foundation::IInspectable ServiceProvider();

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
        void ReadWriteProperty(int32_t value) noexcept;
        //hstring DistinctProperty();
        //void DistinctProperty(hstring const& value);

        winrt::event<Windows::Foundation::EventHandler<Microsoft::UI::Xaml::Data::DataErrorsChangedEventArgs>> _dataErrorsChanged;
        bool HasErrors();
        winrt::event_token ErrorsChanged(Windows::Foundation::EventHandler<Microsoft::UI::Xaml::Data::DataErrorsChangedEventArgs> const& handler);
        void ErrorsChanged(winrt::event_token const& token) noexcept;
        Windows::Foundation::Collections::IIterable<Windows::Foundation::IInspectable> GetErrors(hstring const& propertyName);
        void RaiseDataErrorChanged();

        static Windows::Foundation::IInspectable BadRuntimeClassName();
    };
}

namespace winrt::TestComponentCSharp::factory_implementation
{
    struct Class : ClassT<Class, implementation::Class, Windows::Foundation::IStringable>
    {
        hstring ToString()
        {
            return L"Class";
        }
    };
}
