#include "pch.h"
#include "Composable.h"
#include "Composable.g.cpp"

namespace winrt::TestComponentCSharp::implementation
{
    winrt::event_token Composable::StringPropertyChanged(Windows::Foundation::TypedEventHandler<TestComponentCSharp::Composable, hstring> const& handler)
    {
        return _stringChanged.add(handler);
    }
    void Composable::StringPropertyChanged(winrt::event_token const& token) noexcept
    {
        _stringChanged.remove(token);
    }
}
