#pragma once

#include "Class.g.h"

namespace winrt::Beta::implementation
{
    struct Class : ClassT<Class>
    {
        Class() = default;
    };
}

namespace winrt::Beta::factory_implementation
{
    struct Class : ClassT<Class, implementation::Class>
    {
    };
}
