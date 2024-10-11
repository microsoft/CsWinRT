#pragma once
#include <unknwn.h>
#include <inspectable.h>
#include <winrt/Windows.Foundation.h>
#include <winrt/Windows.Foundation.Collections.h>
#include <winrt/Microsoft.UI.Xaml.h>
#include <winrt/Microsoft.UI.Xaml.Controls.h>
#include <winrt/Microsoft.UI.Xaml.Controls.Primitives.h>
#include <winrt/Microsoft.UI.Xaml.Data.h>
#include <winrt/Microsoft.UI.Xaml.Interop.h>
#include <winrt/Microsoft.UI.Xaml.Markup.h>
#include <winrt/Microsoft.UI.Xaml.Navigation.h>
#include <winrt/Windows.Web.Http.h>

// TODO: Replace with latest Cpp/WinRT
namespace winrt
{
	template <typename T, typename Allocator = std::allocator<T>>
	Windows::Foundation::Collections::IVectorView<T> single_threaded_vector_view(std::vector<T, Allocator>&& values = {})
	{
		return make<impl::input_vector_view<T, std::vector<T, Allocator>>>(std::move(values));
	}

	template <typename K, typename V, typename Allocator = std::allocator<K>>
	Windows::Foundation::Collections::IMapView<K, V> single_threaded_map_view(std::map<K, V, Allocator>&& values = {})
	{
		return make<impl::input_map_view<K, V, std::map<K, V, Allocator>>>(std::move(values));
	}
}