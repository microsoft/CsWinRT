#include "pch.h"
#include "NonUniqueClass.h"
#include "TestPublicExclusiveTo.NonUniqueClass.g.cpp"

using namespace winrt;

namespace winrt::TestComponentCSharp::TestPublicExclusiveTo::implementation
{
    int32_t NonUniqueClass::StaticProperty()
    {
        throw hresult_not_implemented();
    }
    hstring NonUniqueClass::Path()
    {
        throw hresult_not_implemented();
    }
    int32_t NonUniqueClass::Type()
    {
        throw hresult_not_implemented();
    }
}
