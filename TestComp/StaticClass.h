#pragma once
#include "StaticClass.g.h"

namespace winrt::TestComp::implementation
{
    struct StaticClass
    {
        StaticClass() = default;

        static TestComp::Class MakeClass();
        static int32_t NumClasses();
    };
}
namespace winrt::TestComp::factory_implementation
{
    struct StaticClass : StaticClassT<StaticClass, implementation::StaticClass>
    {
    };
}
