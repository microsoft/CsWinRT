#pragma once
#include "CustomBindableIteratorTest.g.h"
#include "CustomDisposableTest.g.h"

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
}

namespace winrt::TestComponentCSharp::factory_implementation
{
	struct CustomBindableIteratorTest : CustomBindableIteratorTestT<CustomBindableIteratorTest, implementation::CustomBindableIteratorTest>
	{

	};

	struct CustomDisposableTest : CustomDisposableTestT<CustomDisposableTest, implementation::CustomDisposableTest>
	{

	};
}