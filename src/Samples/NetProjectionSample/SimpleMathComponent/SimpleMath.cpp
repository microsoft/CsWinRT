// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "pch.h"
#include "SimpleMath.h"
#include "SimpleMath.g.cpp"

namespace winrt::SimpleMathComponent::implementation
{
	double SimpleMath::add(double firstNumber, double secondNumber)
	{
		return firstNumber + secondNumber;
	}
	double SimpleMath::subtract(double firstNumber, double secondNumber)
	{
		return firstNumber - secondNumber;
	}
	double SimpleMath::multiply(double firstNumber, double secondNumber)
	{
		return firstNumber * secondNumber;
	}
	double SimpleMath::divide(double firstNumber, double secondNumber)
	{
		if (0 == secondNumber)
			return -1;
		return firstNumber / secondNumber;
	}
}
