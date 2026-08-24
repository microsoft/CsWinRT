#include "pch.h"
#include "MarshalledValuesTest.h"
#include "MarshalledValuesTest.g.cpp"

namespace WF = winrt::Windows::Foundation;

namespace winrt::TestComponentCSharp::implementation
{
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
