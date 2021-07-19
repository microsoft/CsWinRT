#include "pch.h"
#include "ABCDEFGHIJKLMNOPQRSTUVQXYZabcdefghijklmnopqrstuvqxyzABCDEFGHIJKLMNOPQRSTUVQXYZabcdefghijklmnopqrstuvqxyzABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz.h"
#include "ABCDEFGHIJKLMNOPQRSTUVQXYZabcdefghijklmnopqrstuvqxyzABCDEFGHIJKLMNOPQRSTUVQXYZabcdefghijklmnopqrstuvqxyzABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz.g.cpp"

namespace winrt::TestComponentCSharp::implementation
{
    winrt::event_token ABCDEFGHIJKLMNOPQRSTUVQXYZabcdefghijklmnopqrstuvqxyzABCDEFGHIJKLMNOPQRSTUVQXYZabcdefghijklmnopqrstuvqxyzABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz::EventForAVeryLongClassName(winrt::Windows::Foundation::TypedEventHandler<winrt::TestComponentCSharp::ABCDEFGHIJKLMNOPQRSTUVQXYZabcdefghijklmnopqrstuvqxyzABCDEFGHIJKLMNOPQRSTUVQXYZabcdefghijklmnopqrstuvqxyzABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz, winrt::TestComponentCSharp::ABCDEFGHIJKLMNOPQRSTUVQXYZabcdefghijklmnopqrstuvqxyzABCDEFGHIJKLMNOPQRSTUVQXYZabcdefghijklmnopqrstuvqxyzABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz> const& handler)
    {
        return _theEvent.add(handler);
    }
    void ABCDEFGHIJKLMNOPQRSTUVQXYZabcdefghijklmnopqrstuvqxyzABCDEFGHIJKLMNOPQRSTUVQXYZabcdefghijklmnopqrstuvqxyzABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz::EventForAVeryLongClassName(winrt::event_token const& token) noexcept
    {
        _theEvent.remove(token);
    }
    void ABCDEFGHIJKLMNOPQRSTUVQXYZabcdefghijklmnopqrstuvqxyzABCDEFGHIJKLMNOPQRSTUVQXYZabcdefghijklmnopqrstuvqxyzABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz::InvokeEvent()
    {
        _theEvent(*this, *this);
    }
}
