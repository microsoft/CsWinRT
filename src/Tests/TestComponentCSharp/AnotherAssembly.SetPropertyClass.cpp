#include "pch.h"
#include "AnotherAssembly.SetPropertyClass.h"
#include "AnotherAssembly.SetPropertyClass.g.cpp"

namespace winrt::TestComponentCSharp::AnotherAssembly::implementation
{
    void SetPropertyClass::ReadWriteProperty(int32_t value)
    {
        readWriteValue = value;
    }

    int32_t SetPropertyClass::ReadWriteProperty()
    {
        return readWriteValue;
    }
}
