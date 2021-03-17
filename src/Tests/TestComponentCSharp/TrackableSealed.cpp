#include "pch.h"
#include "TrackableSealed.h"
#include "TrackableSealed.g.cpp"

namespace winrt::TestComponentCSharp::implementation
{
    winrt::event_token TrackableSealed::StringPropertyChanged(Windows::Foundation::TypedEventHandler<TestComponentCSharp::TrackableSealed, hstring> const& handler)
    {
        return _stringChanged.add(handler);
    }
    void TrackableSealed::StringPropertyChanged(winrt::event_token const& token) noexcept
    {
        _stringChanged.remove(token);
    }
}
