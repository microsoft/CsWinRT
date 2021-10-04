#pragma once

#include "Class.g.h"

namespace winrt::Alpha::implementation
{
    struct Class : ClassT<Class>
    {
        Class() = default;
    };
}

namespace winrt::Alpha::factory_implementation
{
    struct Class : ClassT<Class, implementation::Class>
    {
    };
}
