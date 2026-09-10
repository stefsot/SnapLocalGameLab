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
		public ref class IL2CppFieldInfoWrapper : IL2CppWrapper<FieldInfo>
		{
		public:
			property String^ Name
			{
				String^ get()
				{
					return gcnew String(_native_pointer->name);
				}
			}

			property IL2CppTypeWrapper^ Type
			{
				IL2CppTypeWrapper^ get()
				{
					return gcnew IL2CppTypeWrapper(IL2cppNativeApi::il2cpp_field_get_type(_native_pointer));
				}
			}

			property Reflection::FieldAttributes Attributes
			{
				Reflection::FieldAttributes get()
				{
					return static_cast<Reflection::FieldAttributes>(IL2cppNativeApi::il2cpp_field_get_flags(_native_pointer));
				}
			}

			property IntPtr Offset
			{
				IntPtr get()
				{
					return IntPtr(reinterpret_cast<void*>(IL2cppNativeApi::il2cpp_field_get_offset(_native_pointer)));
				}
			}

			Object^ GetDefaultValue()
			{
				switch(Type->_native_pointer->type)
				{
				case IL2CPP_TYPE_BOOLEAN:
				case IL2CPP_TYPE_U1:
				case IL2CPP_TYPE_I1:
					{
						bool blob;
						IL2cppNativeApi::il2cpp_field_static_get_value(_native_pointer, &blob);
						return gcnew Boolean(blob);
					}

				case IL2CPP_TYPE_CHAR:
					{
						char blob;
						IL2cppNativeApi::il2cpp_field_static_get_value(_native_pointer, &blob);
						return gcnew Char(blob);			
					}

				case IL2CPP_TYPE_U2:
					{
						USHORT blob;
						IL2cppNativeApi::il2cpp_field_static_get_value(_native_pointer, &blob);
						return gcnew UInt16(blob);
					}

				case IL2CPP_TYPE_I2:
					{
						SHORT blob;
						IL2cppNativeApi::il2cpp_field_static_get_value(_native_pointer, &blob);
						return gcnew Int16(blob);
					}
					
				case IL2CPP_TYPE_U4:
				case IL2CPP_TYPE_VALUETYPE:
					{
						UINT blob;
						IL2cppNativeApi::il2cpp_field_static_get_value(_native_pointer, &blob);
						return gcnew UInt32(blob);
					}
					

				case IL2CPP_TYPE_I4:
					{
						INT blob;
						IL2cppNativeApi::il2cpp_field_static_get_value(_native_pointer, &blob);
						return gcnew Int32(blob);
					}

				case IL2CPP_TYPE_U8:
					{
						UINT64 blob;
						IL2cppNativeApi::il2cpp_field_static_get_value(_native_pointer, &blob);
						return gcnew UInt64(blob);
					}
					

				case IL2CPP_TYPE_I8:
					{
						INT64 blob;
						IL2cppNativeApi::il2cpp_field_static_get_value(_native_pointer, &blob);
						return gcnew Int64(blob);
					}
					
				case IL2CPP_TYPE_R4:
					{
						float blob;
						IL2cppNativeApi::il2cpp_field_static_get_value(_native_pointer, &blob);
						return gcnew Single(blob);
					}

				case IL2CPP_TYPE_R8:
					{
						double blob;
						IL2cppNativeApi::il2cpp_field_static_get_value(_native_pointer, &blob);
						return gcnew Double(blob);
					}

				case IL2CPP_TYPE_STRING:
					{
						Il2CppString* blob;			
						IL2cppNativeApi::il2cpp_field_static_get_value(_native_pointer, &blob);
						return gcnew String(blob->chars, 0, blob->length);
					}

				default:
					return nullptr;
				}
			}

		internal:
			IL2CppFieldInfoWrapper(FieldInfo* native_ptr) : IL2CppWrapper(native_ptr) {}
		};
	}
}