#pragma once
#include "Composable.g.h"

namespace winrt::TestComponentCSharp::implementation
{
    struct Composable : ComposableT<Composable>
    {
        Composable() = default;

        winrt::event<Windows::Foundation::TypedEventHandler<TestComponentCSharp::Composable, hstring>> _stringChanged;
        winrt::event_token StringPropertyChanged(Windows::Foundation::TypedEventHandler<TestComponentCSharp::Composable, hstring> const& handler);
        void StringPropertyChanged(winrt::event_token const& token) noexcept;
    };
}
namespace winrt::TestComponentCSharp::factory_implementation
{
    struct Composable : ComposableT<Composable, implementation::Composable>
    {
    };
}
