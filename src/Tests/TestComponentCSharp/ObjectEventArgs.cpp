#include "pch.h"
#include "ObjectEventArgs.h"
#include "ObjectEventArgs.g.cpp"

namespace winrt::TestComponentCSharp::implementation
{
    ObjectEventArgs::ObjectEventArgs(int32_t index) : _value(index)
    {
    }

    int32_t ObjectEventArgs::Value()
    {
        return _value;
    }
}
