#include "pch.h"
#include "CustomExperimentClass.h"
#include "CustomExperimentClass.g.cpp"

namespace winrt::TestComponentCSharp::implementation
{
    int32_t CustomExperimentClass::Value()
    {
        return 4;
    }
    void CustomExperimentClass::Value(int32_t value)
    {
    }
    void CustomExperimentClass::f()
    {
    }
}
