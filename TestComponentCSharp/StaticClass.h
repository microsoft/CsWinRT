#pragma once
#include "StaticClass.g.h"

namespace winrt::TestComponentCSharp::implementation
{
    struct StaticClass
    {
        StaticClass() = default;

        static TestComponentCSharp::Class MakeClass();
        static int32_t NumClasses();
    };
}
namespace winrt::TestComponentCSharp::factory_implementation
{
    struct StaticClass : StaticClassT<StaticClass, implementation::StaticClass>
    {
    };
}
