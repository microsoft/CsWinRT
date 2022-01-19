#pragma once

#include "Class.g.h"

namespace winrt::Gamma::implementation
{
    struct Class : ClassT<Class>
    {
        Class() = default;

    };
}

namespace winrt::Gamma::factory_implementation
{
    struct Class : ClassT<Class, implementation::Class>
    {
    };
}
