#pragma once
#include "Class1.g.h"
#include <iostream>

namespace winrt::CSWinMDComponent::implementation
{
  struct Class1 : Class1T<Class1>
  {
    Class1() = default;

    void f(int32_t x, E1 y) {
      std::cout << "Called Class1::f with x = " << x << ", y = " << static_cast<int>(y) << "\n";
    }
  };
}
namespace winrt::CSWinMDComponent::factory_implementation
{
  struct Class1 : Class1T<Class1, implementation::Class1>
  {
  };
}
