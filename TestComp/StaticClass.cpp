#include "pch.h"
#include "Class.h"
#include "StaticClass.h"
#include "StaticClass.g.cpp"

namespace winrt::TestComp::implementation
{
    namespace statics
    {
        int _count{};
    }

    TestComp::Class StaticClass::MakeClass()
    {
        ++statics::_count;
        return winrt::make<TestComp::implementation::Class>();
    }
    int32_t StaticClass::NumClasses()
    {
        return statics::_count;
    }
}
