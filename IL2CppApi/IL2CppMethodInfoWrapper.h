#pragma once
#include "IL2cppNativeApi.h"
#include "il2cpp.h"
#include "IL2CppWrapper.h"
#include "IL2CppTypeWrapper.h"

using namespace IL2CppApi;
using namespace Native;

namespace IL2CppApi
{
	namespace Wrappers
	{
		public ref class IL2CppMethodInfoWrapper : IL2CppWrapper<MethodInfo>
		{
		public:
			property String^ Name
			{
				String^ get()
				{
					return gcnew String(_native_pointer->name);
				}
			}

			property Reflection::MethodAttributes Attributes
			{
				Reflection::MethodAttributes get()
				{
					uint32_t iflags;
					return static_cast<Reflection::MethodAttributes>(IL2cppNativeApi::il2cpp_method_get_flags(_native_pointer, &iflags));
				}
			}

			property IntPtr MethodPointer
			{
				IntPtr get()
				{
					return IntPtr(_native_pointer->methodPointer);
				}
			}

			property IL2CppTypeWrapper^ ReturnType
			{
				IL2CppTypeWrapper^ get()
				{
					return gcnew IL2CppTypeWrapper(IL2cppNativeApi::il2cpp_method_get_return_type(_native_pointer));
				}
			}

			property UInt32 ParamCount
			{
				UInt32 get()
				{
					return UInt32(IL2cppNativeApi::il2cpp_method_get_param_count(_native_pointer));
				}
			}

			property UInt32 Token
			{
				UInt32 get()
				{
					return UInt32(IL2cppNativeApi::il2cpp_method_get_token(_native_pointer));
				}
			}

			property bool IsGeneric
			{
				bool get()
				{
					return bool(IL2cppNativeApi::il2cpp_method_is_generic(_native_pointer));
				}
			}

			array<IL2CppTypeWrapper^>^ GetParameters()
			{
				const auto count = ParamCount;
				const auto params = gcnew array<IL2CppTypeWrapper^>(count);

				for(auto i = 0u; i < count; i++)
				{
					params[i] = gcnew IL2CppTypeWrapper(IL2cppNativeApi::il2cpp_method_get_param(_native_pointer, i));
				}

				return params;
			}

			Collections::Generic::Dictionary<String^, IL2CppTypeWrapper^>^ GetParametersWithNames()
			{
				const auto count = ParamCount;
				const auto params = gcnew Collections::Generic::Dictionary<String^, IL2CppTypeWrapper^>(count);

				for (auto i = 0u; i < count; i++)
				{
					const auto param = gcnew IL2CppTypeWrapper(IL2cppNativeApi::il2cpp_method_get_param(_native_pointer, i));
					const auto param_name = gcnew String(IL2cppNativeApi::il2cpp_method_get_param_name(_native_pointer, i));
					params->Add(param_name, param);
				}

				return params;
			}

			String^ GetParamName(const Int32 index)
			{
				return gcnew String(IL2cppNativeApi::il2cpp_method_get_param_name(_native_pointer, index));
			}

		internal:
			IL2CppMethodInfoWrapper(MethodInfo* native_ptr) : IL2CppWrapper(native_ptr) {}
		};
	}
}