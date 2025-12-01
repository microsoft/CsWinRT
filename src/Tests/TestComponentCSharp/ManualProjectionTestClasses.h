#pragma once
#include "CustomBindableIteratorTest.g.h"
#include "CustomDisposableTest.g.h"
#include "CustomBindableVectorTest.g.h"
#include "CustomBindableObservableVectorTest.g.h"
#include "CustomIteratorTest.g.h"
#include "SetTypeProperties.g.h"

namespace winrt::TestComponentCSharp::implementation
{
    struct SetTypeProperties : SetTypePropertiesT<SetTypeProperties>
    {
        SetTypeProperties();
        winrt::hstring SetProperty();
    };

	struct CustomBindableIteratorTest : CustomBindableIteratorTestT<CustomBindableIteratorTest>
	{
		CustomBindableIteratorTest();
		bool MoveNext();
		Windows::Foundation::IInspectable Current();
		bool HasCurrent();
	};

	struct CustomDisposableTest : CustomDisposableTestT<CustomDisposableTest>
	{
		CustomDisposableTest();
		void Close();
	};

	struct CustomBindableVectorTest : CustomBindableVectorTestT<CustomBindableVectorTest>
	{
		CustomBindableVectorTest();
		void Clear();
		void Append(Windows::Foundation::IInspectable value);
		Windows::Foundation::IInspectable GetAt(int32_t index);
		winrt::Microsoft::UI::Xaml::Interop::IBindableVectorView GetView();
		bool IndexOf(Windows::Foundation::IInspectable, uint32_t& index);
		void InsertAt(int32_t index, Windows::Foundation::IInspectable value);
		void RemoveAt(int32_t index);
		void RemoveAtEnd();
		void SetAt(int32_t index, Windows::Foundation::IInspectable value);
		int32_t Size();
		winrt::Microsoft::UI::Xaml::Interop::IBindableIterator First();
	};

	struct CustomBindableObservableVectorTest : CustomBindableObservableVectorTestT<CustomBindableObservableVectorTest>
	{
		CustomBindableObservableVectorTest() = default;

		winrt::Microsoft::UI::Xaml::Interop::IBindableIterator First();
		winrt::Windows::Foundation::IInspectable GetAt(uint32_t index);
		uint32_t Size();
		winrt::Microsoft::UI::Xaml::Interop::IBindableVectorView GetView();
		bool IndexOf(winrt::Windows::Foundation::IInspectable const& value, uint32_t& index);
		void SetAt(uint32_t index, winrt::Windows::Foundation::IInspectable const& value);
		void InsertAt(uint32_t index, winrt::Windows::Foundation::IInspectable const& value);
		void RemoveAt(uint32_t index);
		void Append(winrt::Windows::Foundation::IInspectable const& value);
		void RemoveAtEnd();
		void Clear();
		winrt::event_token VectorChanged(winrt::Microsoft::UI::Xaml::Interop::BindableVectorChangedEventHandler const& handler);
		void VectorChanged(winrt::event_token const& token) noexcept;
	};

	struct CustomIteratorTest : CustomIteratorTestT<CustomIteratorTest>
	{
		CustomIteratorTest() = default;
		CustomIteratorTest(winrt::Windows::Foundation::Collections::IIterator<int> iterator);

		int32_t Current();
		bool HasCurrent();
		bool MoveNext();
		uint32_t GetMany(array_view<int32_t> items);

		winrt::Windows::Foundation::Collections::IIterator<int> _iterator;
	};
}

namespace winrt::TestComponentCSharp::factory_implementation
{
    struct SetTypeProperties : SetTypePropertiesT<SetTypeProperties, implementation::SetTypeProperties>
    {

    };

	struct CustomBindableIteratorTest : CustomBindableIteratorTestT<CustomBindableIteratorTest, implementation::CustomBindableIteratorTest>
	{

	};

	struct CustomDisposableTest : CustomDisposableTestT<CustomDisposableTest, implementation::CustomDisposableTest>
	{

	};

	struct CustomBindableVectorTest : CustomBindableVectorTestT<CustomBindableVectorTest, implementation::CustomBindableVectorTest>
	{

	};

	struct CustomBindableObservableVectorTest : CustomBindableObservableVectorTestT<CustomBindableObservableVectorTest, implementation::CustomBindableObservableVectorTest>
	{
	};

	struct CustomIteratorTest : CustomIteratorTestT<CustomIteratorTest, implementation::CustomIteratorTest>
	{
	};
}