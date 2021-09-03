#pragma once
#include "AnotherAssembly.SetPropertyClass.g.h"

namespace winrt::TestComponentCSharp::AnotherAssembly::implementation
{
    struct SetPropertyClass : SetPropertyClassT<SetPropertyClass>
    {
        SetPropertyClass() = default;

        void ReadWriteProperty(int32_t value);
        int32_t ReadWriteProperty();

        int32_t readWriteValue;
    };
}
namespace winrt::TestComponentCSharp::AnotherAssembly::factory_implementation
{
    struct SetPropertyClass : SetPropertyClassT<SetPropertyClass, implementation::SetPropertyClass>
    {
    };
}