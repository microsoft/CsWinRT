#pragma once
#include "ManagedClassNotFound.g.h"

namespace winrt::TestComponentCSharp::implementation
{
    struct ManagedClassNotFound : ManagedClassNotFoundT<ManagedClassNotFound>
    {
        ManagedClassNotFound()
        {
            // This class implemented in C# for hosting tests
            winrt::throw_hresult(E_NOTIMPL);
        }

        hstring ToString();
    };
}
namespace winrt::TestComponentCSharp::factory_implementation
{
    struct ManagedClassNotFound : ManagedClassNotFoundT<ManagedClassNotFound, implementation::ManagedClassNotFound>
    {
    };
}
