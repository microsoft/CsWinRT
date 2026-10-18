#include "pch.h"
#include "MarshalledValuesTest.h"
#include "MarshalledValuesTest.g.cpp"

namespace WF = winrt::Windows::Foundation;

namespace winrt::TestComponentCSharp::implementation
{
    MarshalledValuesTest::MarshalledValuesTest(array_view<WF::IReference<int32_t> const> nullableValues, array_view<WF::Collections::IKeyValuePair<hstring, hstring> const> pairValues, array_view<Windows::UI::Xaml::Interop::TypeName const> typeValues)
    {
        NullableValues(nullableValues);
        PairValues(pairValues);
        TypeValues(typeValues);
    }

    MarshalledValuesTest::MarshalledValuesTest(TestComponentCSharp::IArrayMarshalling const& target) :
        _arrayTarget(target)
    {
    }

    com_array<WF::IReference<int32_t>> MarshalledValuesTest::NullableValues()
    {
        if (_arrayTarget)
        {
            return _arrayTarget.NullableValues();
        }
        return { _nullableValues.begin(), _nullableValues.end() };
    }

    void MarshalledValuesTest::NullableValues(array_view<WF::IReference<int32_t> const> values)
    {
        if (_arrayTarget)
        {
            _arrayTarget.NullableValues(values);
            return;
        }
        _nullableValues.assign(values.begin(), values.end());
    }

    com_array<WF::Collections::IKeyValuePair<hstring, hstring>> MarshalledValuesTest::PairValues()
    {
        if (_arrayTarget)
        {
            return _arrayTarget.PairValues();
        }
        return { _pairValues.begin(), _pairValues.end() };
    }

    void MarshalledValuesTest::PairValues(array_view<WF::Collections::IKeyValuePair<hstring, hstring> const> values)
    {
        if (_arrayTarget)
        {
            _arrayTarget.PairValues(values);
            return;
        }
        _pairValues.assign(values.begin(), values.end());
    }

    com_array<Windows::UI::Xaml::Interop::TypeName> MarshalledValuesTest::TypeValues()
    {
        if (_arrayTarget)
        {
            return _arrayTarget.TypeValues();
        }
        return { _typeValues.begin(), _typeValues.end() };
    }

    void MarshalledValuesTest::TypeValues(array_view<Windows::UI::Xaml::Interop::TypeName const> values)
    {
        if (_arrayTarget)
        {
            _arrayTarget.TypeValues(values);
            return;
        }
        _typeValues.assign(values.begin(), values.end());
    }

    void MarshalledValuesTest::FillNullableValues(array_view<WF::IReference<int32_t>> values)
    {
        if (_arrayTarget)
        {
            _arrayTarget.FillNullableValues(values);
            return;
        }
        if (values.size() != _nullableValues.size())
        {
            throw hresult_invalid_argument();
        }
        std::copy(_nullableValues.begin(), _nullableValues.end(), values.begin());
    }

    void MarshalledValuesTest::FillPairValues(array_view<WF::Collections::IKeyValuePair<hstring, hstring>> values)
    {
        if (_arrayTarget)
        {
            _arrayTarget.FillPairValues(values);
            return;
        }
        if (values.size() != _pairValues.size())
        {
            throw hresult_invalid_argument();
        }
        std::copy(_pairValues.begin(), _pairValues.end(), values.begin());
    }

    void MarshalledValuesTest::FillTypeValues(array_view<Windows::UI::Xaml::Interop::TypeName> values)
    {
        if (_arrayTarget)
        {
            _arrayTarget.FillTypeValues(values);
            return;
        }
        if (values.size() != _typeValues.size())
        {
            throw hresult_invalid_argument();
        }
        std::copy(_typeValues.begin(), _typeValues.end(), values.begin());
    }

    winrt::hresult MarshalledValuesTest::Result()
    {
        return _result;
    }

    void MarshalledValuesTest::Result(winrt::hresult const& value)
    {
        _result = value;
    }

    winrt::hresult MarshalledValuesTest::SwapResult(winrt::hresult const& value)
    {
        return std::exchange(_result, value);
    }

    void MarshalledValuesTest::ExchangeResult(winrt::hresult const& value, winrt::hresult& previous)
    {
        previous = std::exchange(_result, value);
    }

    void MarshalledValuesTest::ExchangeDateTime(WF::DateTime const& value, WF::DateTime& previous)
    {
        previous = std::exchange(_dateTime, value);
    }

    void MarshalledValuesTest::ExchangeTimeSpan(WF::TimeSpan const& value, WF::TimeSpan& previous)
    {
        previous = std::exchange(_timeSpan, value);
    }

    void MarshalledValuesTest::ExchangeTypeName(winrt::Windows::UI::Xaml::Interop::TypeName const& value, winrt::Windows::UI::Xaml::Interop::TypeName& previous)
    {
        previous = std::exchange(_typeName, value);
    }

    WF::DateTime MarshalledValuesTest::OffsetDateTime(WF::DateTime const& value, WF::TimeSpan const& offset)
    {
        return value + offset;
    }

    winrt::hresult MarshalledValuesTest::CallResultProperty(TestComponentCSharp::IMarshalledValues const& target, winrt::hresult const& value)
    {
        target.Result(value);

        return target.Result();
    }

    winrt::hresult MarshalledValuesTest::CallSwapResult(TestComponentCSharp::IMarshalledValues const& target, winrt::hresult const& value)
    {
        return target.SwapResult(value);
    }

    winrt::hresult MarshalledValuesTest::CallExchangeResult(TestComponentCSharp::IMarshalledValues const& target, winrt::hresult const& value)
    {
        winrt::hresult previous;

        target.ExchangeResult(value, previous);

        return previous;
    }

    WF::DateTime MarshalledValuesTest::CallExchangeDateTime(TestComponentCSharp::IMarshalledValues const& target, WF::DateTime const& value)
    {
        WF::DateTime previous;

        target.ExchangeDateTime(value, previous);

        return previous;
    }

    WF::TimeSpan MarshalledValuesTest::CallExchangeTimeSpan(TestComponentCSharp::IMarshalledValues const& target, WF::TimeSpan const& value)
    {
        WF::TimeSpan previous;

        target.ExchangeTimeSpan(value, previous);

        return previous;
    }

    winrt::Windows::UI::Xaml::Interop::TypeName MarshalledValuesTest::CallExchangeTypeName(TestComponentCSharp::IMarshalledValues const& target, winrt::Windows::UI::Xaml::Interop::TypeName const& value)
    {
        winrt::Windows::UI::Xaml::Interop::TypeName previous;

        target.ExchangeTypeName(value, previous);

        return previous;
    }

    WF::DateTime MarshalledValuesTest::CallOffsetDateTime(TestComponentCSharp::IMarshalledValues const& target, WF::DateTime const& value, WF::TimeSpan const& offset)
    {
        return target.OffsetDateTime(value, offset);
    }

    winrt::hresult MarshalledValuesTest::InvokeHandleResult(TestComponentCSharp::HandleResult const& handler, winrt::hresult const& value)
    {
        return handler(value);
    }
}
