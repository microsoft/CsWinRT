#include "pch.h"
#include "Singleton.h"
#include "Singleton.g.cpp"

using namespace winrt;
using namespace Windows::Foundation;

namespace winrt::TestComponentCSharp::implementation
{
    TestComponentCSharp::ISingleton Singleton::Instance()
    {
        struct singleton : winrt::implements<singleton, ISingleton>
        {
            int _int{};
            winrt::event<EventHandler<int32_t>> _intChanged {};

            int32_t IntProperty()
            {
                return _int;
            }
            void IntProperty(int32_t value)
            {
                _int = value;
                _intChanged(nullptr, _int);
            }
            winrt::event_token IntPropertyChanged(EventHandler<int32_t> const& handler)
            {
                return _intChanged.add(handler);
            }
            void IntPropertyChanged(winrt::event_token const& token) noexcept
            {
                _intChanged.remove(token);
            }
        };
        static TestComponentCSharp::ISingleton _singleton = winrt::make<singleton>();
        return _singleton;
    }

    void Singleton::Instance(TestComponentCSharp::ISingleton const& value)
    {
        throw hresult_not_implemented();
    }
}
