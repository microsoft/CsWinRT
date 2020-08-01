#pragma once
#include "ManagedClass.g.h"

namespace winrt::TestComponentCSharp::implementation
{
    struct ManagedClass : ManagedClassT<ManagedClass>
    {
        ManagedClass()
        {
            // This class implemented in C# for hosting tests
            winrt::throw_hresult(E_NOTIMPL);
        }

        hstring ToString();
    };
}
namespace winrt::TestComponentCSharp::factory_implementation
{
    struct ManagedClass : ManagedClassT<ManagedClass, implementation::ManagedClass>
    {
    };
}
