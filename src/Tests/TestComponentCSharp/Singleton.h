#pragma once
#include "Singleton.g.h"

namespace winrt::TestComponentCSharp::implementation
{
    struct Singleton
    {
        Singleton() = default;

        static TestComponentCSharp::ISingleton Instance();
        static void Instance(TestComponentCSharp::ISingleton const& value);
    };
}
namespace winrt::TestComponentCSharp::factory_implementation
{
    struct Singleton : SingletonT<Singleton, implementation::Singleton>
    {
    };
}
