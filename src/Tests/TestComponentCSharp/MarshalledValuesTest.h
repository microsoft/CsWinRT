#pragma once
#include "MarshalledValuesTest.g.h"

namespace winrt::TestComponentCSharp::implementation
{
    struct MarshalledValuesTest : MarshalledValuesTestT<MarshalledValuesTest>
    {
        MarshalledValuesTest() = default;
        MarshalledValuesTest(winrt::array_view<winrt::Windows::Foundation::IReference<int32_t> const> nullableValues, winrt::array_view<winrt::Windows::Foundation::Collections::IKeyValuePair<winrt::hstring, winrt::hstring> const> pairValues, winrt::array_view<winrt::Windows::UI::Xaml::Interop::TypeName const> typeValues);
        MarshalledValuesTest(winrt::TestComponentCSharp::IArrayMarshalling const& target);

        winrt::com_array<winrt::Windows::Foundation::IReference<int32_t>> NullableValues();
        void NullableValues(winrt::array_view<winrt::Windows::Foundation::IReference<int32_t> const> values);
        winrt::com_array<winrt::Windows::Foundation::Collections::IKeyValuePair<winrt::hstring, winrt::hstring>> PairValues();
        void PairValues(winrt::array_view<winrt::Windows::Foundation::Collections::IKeyValuePair<winrt::hstring, winrt::hstring> const> values);
        winrt::com_array<winrt::Windows::UI::Xaml::Interop::TypeName> TypeValues();
        void TypeValues(winrt::array_view<winrt::Windows::UI::Xaml::Interop::TypeName const> values);
        void FillNullableValues(winrt::array_view<winrt::Windows::Foundation::IReference<int32_t>> values);
        void FillPairValues(winrt::array_view<winrt::Windows::Foundation::Collections::IKeyValuePair<winrt::hstring, winrt::hstring>> values);
        void FillTypeValues(winrt::array_view<winrt::Windows::UI::Xaml::Interop::TypeName> values);

        winrt::hresult Result();
        void Result(winrt::hresult const& value);
        winrt::hresult SwapResult(winrt::hresult const& value);
        void ExchangeResult(winrt::hresult const& value, winrt::hresult& previous);
        void ExchangeDateTime(winrt::Windows::Foundation::DateTime const& value, winrt::Windows::Foundation::DateTime& previous);
        void ExchangeTimeSpan(winrt::Windows::Foundation::TimeSpan const& value, winrt::Windows::Foundation::TimeSpan& previous);
        void ExchangeTypeName(winrt::Windows::UI::Xaml::Interop::TypeName const& value, winrt::Windows::UI::Xaml::Interop::TypeName& previous);
        winrt::Windows::Foundation::DateTime OffsetDateTime(winrt::Windows::Foundation::DateTime const& value, winrt::Windows::Foundation::TimeSpan const& offset);

        static winrt::hresult CallResultProperty(winrt::TestComponentCSharp::IMarshalledValues const& target, winrt::hresult const& value);
        static winrt::hresult CallSwapResult(winrt::TestComponentCSharp::IMarshalledValues const& target, winrt::hresult const& value);
        static winrt::hresult CallExchangeResult(winrt::TestComponentCSharp::IMarshalledValues const& target, winrt::hresult const& value);
        static winrt::Windows::Foundation::DateTime CallExchangeDateTime(winrt::TestComponentCSharp::IMarshalledValues const& target, winrt::Windows::Foundation::DateTime const& value);
        static winrt::Windows::Foundation::TimeSpan CallExchangeTimeSpan(winrt::TestComponentCSharp::IMarshalledValues const& target, winrt::Windows::Foundation::TimeSpan const& value);
        static winrt::Windows::UI::Xaml::Interop::TypeName CallExchangeTypeName(winrt::TestComponentCSharp::IMarshalledValues const& target, winrt::Windows::UI::Xaml::Interop::TypeName const& value);
        static winrt::Windows::Foundation::DateTime CallOffsetDateTime(winrt::TestComponentCSharp::IMarshalledValues const& target, winrt::Windows::Foundation::DateTime const& value, winrt::Windows::Foundation::TimeSpan const& offset);
        static winrt::hresult InvokeHandleResult(winrt::TestComponentCSharp::HandleResult const& handler, winrt::hresult const& value);

    private:
        std::vector<winrt::Windows::Foundation::IReference<int32_t>> _nullableValues;
        std::vector<winrt::Windows::Foundation::Collections::IKeyValuePair<winrt::hstring, winrt::hstring>> _pairValues;
        std::vector<winrt::Windows::UI::Xaml::Interop::TypeName> _typeValues;
        winrt::TestComponentCSharp::IArrayMarshalling _arrayTarget{ nullptr };
        winrt::hresult _result{};
        winrt::Windows::Foundation::DateTime _dateTime{};
        winrt::Windows::Foundation::TimeSpan _timeSpan{};
        winrt::Windows::UI::Xaml::Interop::TypeName _typeName{};
    };
}

namespace winrt::TestComponentCSharp::factory_implementation
{
    struct MarshalledValuesTest : MarshalledValuesTestT<MarshalledValuesTest, implementation::MarshalledValuesTest>
    {
    };
}
