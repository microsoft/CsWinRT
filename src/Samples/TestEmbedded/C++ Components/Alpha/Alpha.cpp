#include "pch.h"
#include "Alpha.h"
#include "Class.g.cpp"

namespace winrt::Alpha::implementation
{
    template <typename T, typename Allocator = std::allocator<T>>
    Windows::Foundation::Collections::IVectorView<T> single_threaded_vector_view(std::vector<T, Allocator>&& values = {})
    {
        return make<impl::input_vector_view<T, std::vector<T, Allocator>>>(std::move(values));
    }

    winrt::Windows::Foundation::Collections::IVector<hstring> Class::GetStringList()
    {
        return winrt::single_threaded_vector<hstring>({ L"alpha", L"beta" });
    }

    winrt::Windows::Foundation::Collections::IVector<int> Class::GetIntList()
    {
        return winrt::single_threaded_vector<int>({ 4, 3, 2, 1 });
    }

    winrt::Windows::Foundation::Collections::IVectorView<winrt::Alpha::Class> Class::GetObjectList()
    {
        return single_threaded_vector_view<winrt::Alpha::Class>({ *this, *this });
    }
}
