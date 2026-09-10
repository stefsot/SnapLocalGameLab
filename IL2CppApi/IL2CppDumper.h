#pragma once
#include "IL2CppImageWrapper.h"

using namespace System;
using namespace IL2CppApi::Wrappers;

namespace IL2CppApi
{
	public ref class IL2CppDumper
	{
	public:
		static array<IL2CppImageWrapper^>^ GetLoadedAssemblies();
	};
}
