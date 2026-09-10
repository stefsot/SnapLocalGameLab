#pragma once
#include "IL2cppNativeApi.h"
#include "il2cpp.h"
#include "IL2CppWrapper.h"

using namespace IL2CppApi;
using namespace Native;

namespace IL2CppApi
{
	namespace Wrappers
	{
		public ref class IL2CppPropertyInfoWrapper : IL2CppWrapper<PropertyInfo>
		{
		public:
			property String^ Name
			{
				String^ get()
				{
					return gcnew String(IL2cppNativeApi::il2cpp_property_get_name(_native_pointer));
				}
			}

			property Reflection::PropertyAttributes Attributes
			{
				Reflection::PropertyAttributes get()
				{
					return static_cast<Reflection::PropertyAttributes>(IL2cppNativeApi::il2cpp_property_get_flags(_native_pointer));
				}
			}

			property IL2CppMethodInfoWrapper^ GetMethod
			{
				IL2CppMethodInfoWrapper^ get()
				{
					return gcnew IL2CppMethodInfoWrapper(IL2cppNativeApi::il2cpp_property_get_get_method(_native_pointer));
				}
			}

			property IL2CppMethodInfoWrapper^ SetMethod
			{
				IL2CppMethodInfoWrapper^ get()
				{
					return gcnew IL2CppMethodInfoWrapper(IL2cppNativeApi::il2cpp_property_get_set_method(_native_pointer));
				}
			}

		internal:
			IL2CppPropertyInfoWrapper(PropertyInfo* native_ptr) : IL2CppWrapper(native_ptr) {}
		};
	}
}