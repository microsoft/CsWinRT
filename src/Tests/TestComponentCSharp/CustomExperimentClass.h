#pragma once
#include "CustomExperimentClass.g.h"

namespace winrt::TestComponentCSharp::implementation
{
    struct CustomExperimentClass : CustomExperimentClassT<CustomExperimentClass>
    {
        CustomExperimentClass() = default;

        int32_t Value();
        void Value(int32_t value);
        void f();
    };
}
namespace winrt::TestComponentCSharp::factory_implementation
{
    struct CustomExperimentClass : CustomExperimentClassT<CustomExperimentClass, implementation::CustomExperimentClass>
    {
    };
}