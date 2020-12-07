#pragma once
#include "CustomBindableIteratorTest.g.h"
#include "CustomDisposableTest.g.h"
#include "CustomBindableVectorTest.g.h"

namespace winrt::TestComponentCSharp::implementation
{
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
}

namespace winrt::TestComponentCSharp::factory_implementation
{
	struct CustomBindableIteratorTest : CustomBindableIteratorTestT<CustomBindableIteratorTest, implementation::CustomBindableIteratorTest>
	{

	};

	struct CustomDisposableTest : CustomDisposableTestT<CustomDisposableTest, implementation::CustomDisposableTest>
	{

	};

	struct CustomBindableVectorTest : CustomBindableVectorTestT<CustomBindableVectorTest, implementation::CustomBindableVectorTest>
	{

	};
}