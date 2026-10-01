#pragma once
#include "ObjectEventArgs.g.h"

namespace winrt::TestComponentCSharp::implementation
{
    struct ObjectEventArgs : ObjectEventArgsT<ObjectEventArgs>
    {
        ObjectEventArgs(int32_t index);

        int32_t Value();

    private:
        int32_t _value;
    };
}

namespace winrt::TestComponentCSharp::factory_implementation
{
    struct ObjectEventArgs : ObjectEventArgsT<ObjectEventArgs, implementation::ObjectEventArgs>
    {
    };
}
