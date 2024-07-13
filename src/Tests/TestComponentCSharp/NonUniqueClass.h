#pragma once
#include "TestPublicExclusiveTo.NonUniqueClass.g.h"

namespace winrt::TestComponentCSharp::TestPublicExclusiveTo::implementation
{
    struct NonUniqueClass : NonUniqueClassT<NonUniqueClass>
    {
    public:
        NonUniqueClass();

        static int32_t StaticProperty();
        hstring Path();
        int32_t Type();
    };
}
namespace winrt::TestComponentCSharp::TestPublicExclusiveTo::factory_implementation
{
    struct NonUniqueClass : NonUniqueClassT<NonUniqueClass, implementation::NonUniqueClass>
    {
    };
}
