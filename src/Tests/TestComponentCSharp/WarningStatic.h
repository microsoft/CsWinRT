#pragma once
#include "WarningStatic.g.h"

namespace winrt::TestComponentCSharp::implementation
{
    struct WarningStatic
    {
        WarningStatic() = default;

        static void Method();
        static int32_t Property();
        static void Property(int32_t value);
        static winrt::event_token Event(Windows::Foundation::EventHandler<int32_t> const& handler);
        static void Event(winrt::event_token const& token) noexcept;
        static void WarningMethod();
        static int32_t WarningProperty();
        static void WarningProperty(int32_t value);
        static winrt::event_token WarningEvent(Windows::Foundation::EventHandler<int32_t> const& handler);
        static void WarningEvent(winrt::event_token const& token) noexcept;
        static int32_t ReadWriteProperty();
        static void ReadWriteProperty(int32_t value);
    };
}
namespace winrt::TestComponentCSharp::factory_implementation
{
    struct WarningStatic : WarningStaticT<WarningStatic, implementation::WarningStatic>
    {
    };
}
