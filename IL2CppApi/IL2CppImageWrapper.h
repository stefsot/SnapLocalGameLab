#pragma once
#include "il2cpp.h"
#include "IL2CppWrapper.h"
#include "IL2CppClassWrapper.h"

using namespace IL2CppApi;
using namespace System;

namespace IL2CppApi
{
	namespace Wrappers
	{
		public ref class IL2CppImageWrapper : IL2CppWrapper<Il2CppImage>
		{
		public:
			property String^ Name
			{
				String^ get()
				{
					return gcnew String(_native_pointer->name);
				}
			}

			property UInt32 Token
			{
				UInt32 get()
				{
					return UInt32(_native_pointer->token);
				}
			}

			array<IL2CppClassWrapper^>^ GetClasses()
			{
				const auto class_count = IL2cppNativeApi::il2cpp_image_get_class_count(_native_pointer);
				const auto array_classes = gcnew array<IL2CppClassWrapper^>(static_cast<int>(class_count));

				for (auto i = 0; i < class_count; i++)
				{
					const auto klass = IL2cppNativeApi::il2cpp_image_get_class(_native_pointer, i);
					array_classes[i] = gcnew IL2CppClassWrapper(klass);
				}

				return array_classes;
			}

			static IL2CppImageWrapper^ GetClassImage(IL2CppClassWrapper^ klass)
			{
				return gcnew IL2CppImageWrapper(IL2cppNativeApi::il2cpp_class_get_image(klass->_native_pointer));
			}

		internal:
			IL2CppImageWrapper(Il2CppImage* native_ptr) : IL2CppWrapper(native_ptr) {}
		};
	}
}


