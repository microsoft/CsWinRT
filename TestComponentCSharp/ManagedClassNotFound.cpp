#include "pch.h"
#include "ManagedClassNotFound.h"
#include "ManagedClassNotFound.g.cpp"

namespace winrt::TestComponentCSharp::implementation
{
    hstring ManagedClassNotFound::ToString()
    {
        // This class is implemented in the Test Projection
        throw hresult_not_implemented();
    }
}
