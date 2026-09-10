#pragma once
#include "IL2CppImageWrapper.h"


using namespace System::Runtime::CompilerServices;
using namespace IL2CppApi::Wrappers;

namespace IL2CppApi
{
	namespace Helpers
	{
		[ExtensionAttribute]
		public ref class Extensions abstract sealed
		{
		public:
			[ExtensionAttribute]
			static IL2CppImageWrapper^ GetClassImage(IL2CppClassWrapper^ klass)
			{
				return IL2CppImageWrapper::GetClassImage(klass);
			}

			[ExtensionAttribute]
			static IL2CppClassWrapper^ GetClass(IL2CppTypeWrapper^ type)
			{
				return IL2CppClassWrapper::GetClassFromType(type);
			}

			[ExtensionAttribute]
			static bool HasAttribute(IL2CppMethodInfoWrapper^ method, IL2CppClassWrapper^ attr_class)
			{
				return IL2CppClassWrapper::MethodHasAttribute(method, attr_class);
			}

			[ExtensionAttribute]
			static bool HasAttribute(IL2CppFieldInfoWrapper^ field, IL2CppClassWrapper^ attr_class)
			{
				return IL2CppClassWrapper::FieldHasAttribute(field, attr_class);
			}

			[ExtensionAttribute]
			static IL2CppClassWrapper^ GetDeclaringType(IL2CppMethodInfoWrapper^ method)
			{
				return IL2CppClassWrapper::GetMethodDeclaringType(method);
			}

			[ExtensionAttribute]
			static IL2CppClassWrapper^ GetParent(IL2CppFieldInfoWrapper^ field)
			{
				return IL2CppClassWrapper::GetFieldParent(field);
			}
		};
	}
}

