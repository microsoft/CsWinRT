#include "pch.h"
#include "Class.h"
#include "StaticClass.h"
#include "StaticClass.g.cpp"

namespace winrt::TestComponentCSharp::implementation
{
    namespace statics
    {
        int _count{};
    }

    TestComponentCSharp::Class StaticClass::MakeClass()
    {
        ++statics::_count;
        return winrt::make<TestComponentCSharp::implementation::Class>();
    }
    int32_t StaticClass::NumClasses()
    {
        return statics::_count;
    }
}
