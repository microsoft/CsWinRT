#pragma once
#include "WarningClass.g.h"

namespace winrt::TestComponentCSharp::implementation
{
    struct WarningClass : WarningClassT<WarningClass>
    {
        WarningClass() = default;

        WarningClass(TestComponentCSharp::WarningEnum const& arg);
        int32_t WarningPropertySetter();
        void WarningMethod();
        int32_t WarningProperty();
        void WarningProperty(int32_t value);
        void WarningPropertySetter(int32_t value);
        winrt::event_token WarningEvent(Windows::Foundation::EventHandler<int32_t> const& handler);
        void WarningEvent(winrt::event_token const& token) noexcept;
        void WarningOverridableMethod();
        int32_t WarningOverridableProperty();
        void WarningOverridableProperty(int32_t value);
        //winrt::event_token WarningOverridableEvent(Windows::Foundation::EventHandler<int32_t> const& handler);
        //void WarningOverridableEvent(winrt::event_token const& token) noexcept;
        int32_t WarningInterfacePropertySetter();
        void WarningInterfaceMethod();
        int32_t WarningInterfaceProperty();
        void WarningInterfaceProperty(int32_t value);
        void WarningInterfacePropertySetter(int32_t value);
        winrt::event_token WarningInterfaceEvent(Windows::Foundation::EventHandler<int32_t> const& handler);
        void WarningInterfaceEvent(winrt::event_token const& token) noexcept;
    };
}
namespace winrt::TestComponentCSharp::factory_implementation
{
    struct WarningClass : WarningClassT<WarningClass, implementation::WarningClass>
    {
    };
}
