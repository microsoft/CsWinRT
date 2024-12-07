#pragma once
#include "CastMetadata.Class.g.h"
#include "CastMetadata.ClassFactory.g.h"

namespace winrt::TestComponentCSharp::CastMetadata::implementation
{
	struct Class : ClassT<Class>
	{
		Class() = default;
	};

	struct ClassFactory
	{
		static winrt::Windows::Foundation::IInspectable Create();
	};
}

namespace winrt::TestComponentCSharp::CastMetadata::factory_implementation
{
	struct Class : ClassT<Class, implementation::Class>
	{
	};

	struct ClassFactory : ClassFactoryT<ClassFactory, implementation::ClassFactory>
	{
	};
}