#pragma once
#include "il2cpp.h"
#include "windows.h"


// https://github.com/EmmaPham/Meow/blob/b16e7729622b7d29a94c28c052c27fc654809187/BuildForiOS/Libraries/libil2cpp/include/il2cpp-api-functions.h
// https://github.com/Jumboperson/Il2CppDumper/blob/master/il2cpp_utils.cpp
// https://github.com/maugatto1996/SOSAMAZONIA/blob/d17d1926f0434db2c1efd009fa8aca8e8e22a42c/sddsfg/Il2CppOutputProject/IL2CPP/libil2cpp/vm/Type.cpp


#define MAKE_PROXY_API(NAME, R, P) typedef R(*_api_##NAME) P; \
		private: \
			static _api_##NAME api_##NAME = nullptr; \
		internal: \
			static R NAME P;

#define MAKE_WRAPPER_API(NAME, T, P) public: \
	NativePointerWrapper<T>^ wrapper_##NAME P;


#define GET_PROC_ADDRESS_API(NAME) api_##NAME = reinterpret_cast<_api_##NAME>(GetProcAddress(_module_handle, #NAME));

#define GET_ONCE_API_PROC_ADDRESS(NAME) \
	if (api_##NAME == nullptr) \
	{ \
		GET_PROC_ADDRESS_API(NAME) \
	}

#define CALL_PROC_API(NAME, P) return api_##NAME P;

#define IMPLEMENT_PROXY_API(NAME, R, P) \
	R IL2cppNativeApi::NAME P \
	{ \
		GET_ONCE_API_PROC_ADDRESS(NAME) \
		CALL_PROC_API(NAME, P) \
	}

#define PROXY_API_BODY(NAME, P) GET_ONCE_API_PROC_ADDRESS(NAME) \
	CALL_PROC_API(NAME, P)


using namespace System;

namespace IL2CppApi
{
	namespace Native
	{
		public ref class IL2cppNativeApi abstract sealed
		{
		private: 
			static HMODULE _module_handle;

			MAKE_PROXY_API(il2cpp_domain_get, Il2CppDomain*, ())
			MAKE_PROXY_API(il2cpp_domain_get_assemblies, Il2CppAssembly**, (const Il2CppDomain* domain, size_t* size))
			MAKE_PROXY_API(il2cpp_assembly_get_image, Il2CppImage*, (const Il2CppAssembly* assembly))
			MAKE_PROXY_API(il2cpp_image_get_class_count, size_t, (const Il2CppImage* image))
			MAKE_PROXY_API(il2cpp_image_get_class, Il2CppClass*, (const Il2CppImage* image, size_t index))
			MAKE_PROXY_API(il2cpp_class_get_name, const char*, (const Il2CppClass* klass))
			MAKE_PROXY_API(il2cpp_class_get_namespace, const char*, (const Il2CppClass* klass))
			MAKE_PROXY_API(il2cpp_class_get_methods, MethodInfo*, (Il2CppClass* klass, void** iter))
			MAKE_PROXY_API(il2cpp_class_get_fields, FieldInfo*, (Il2CppClass* klass, void** iter))
			MAKE_PROXY_API(il2cpp_class_get_properties, PropertyInfo*, (Il2CppClass* klass, void** iter))
			MAKE_PROXY_API(il2cpp_class_is_valuetype, bool, (Il2CppClass* klass))
			MAKE_PROXY_API(il2cpp_field_get_type, Il2CppType*, (FieldInfo* field))
			MAKE_PROXY_API(il2cpp_field_get_flags, int, (FieldInfo* field))
			MAKE_PROXY_API(il2cpp_field_static_get_value, void, (FieldInfo* field, void* value))
			MAKE_PROXY_API(il2cpp_field_get_offset, size_t, (FieldInfo* field))
			MAKE_PROXY_API(il2cpp_class_get_type, Il2CppType*, (Il2CppClass* klass))
			MAKE_PROXY_API(il2cpp_class_value_size, int32_t, (Il2CppClass* klass, uint32_t* align))
			MAKE_PROXY_API(il2cpp_class_get_flags, int, (Il2CppClass* klass))
			MAKE_PROXY_API(il2cpp_class_get_type_token, uint32_t, (Il2CppClass* klass))
			MAKE_PROXY_API(il2cpp_class_get_image, Il2CppImage*, (Il2CppClass* klass))
			MAKE_PROXY_API(il2cpp_class_get_rank, int, (Il2CppClass* klass))
			MAKE_PROXY_API(il2cpp_class_enum_basetype, Il2CppType*, (Il2CppClass* klass))
			MAKE_PROXY_API(il2cpp_type_get_class_or_element_class, Il2CppClass*, (Il2CppType* type))
			MAKE_PROXY_API(il2cpp_class_get_declaring_type, Il2CppClass*, (Il2CppClass* klass))
			MAKE_PROXY_API(il2cpp_class_get_parent, Il2CppClass*, (Il2CppClass* klass))
			MAKE_PROXY_API(il2cpp_method_get_flags, uint32_t, (const MethodInfo* method, uint32_t* iflags))
			MAKE_PROXY_API(il2cpp_method_has_attribute, bool, (const MethodInfo* method, Il2CppClass* attr_class))
			MAKE_PROXY_API(il2cpp_class_has_attribute, bool, (Il2CppClass* klass, Il2CppClass* attr_class))
			MAKE_PROXY_API(il2cpp_method_get_return_type, Il2CppType*, (const MethodInfo* method))
			MAKE_PROXY_API(il2cpp_method_get_declaring_type, Il2CppClass*, (const MethodInfo* method))
			MAKE_PROXY_API(il2cpp_method_get_param_count, uint32_t, (const MethodInfo* method))
			MAKE_PROXY_API(il2cpp_method_get_param, Il2CppType*, (const MethodInfo* method, uint32_t index))
			MAKE_PROXY_API(il2cpp_method_get_token, uint32_t, (const MethodInfo* method))
			MAKE_PROXY_API(il2cpp_method_get_param_name, char*, (const MethodInfo* method, uint32_t index))
			MAKE_PROXY_API(il2cpp_class_from_il2cpp_type, Il2CppClass*, (const Il2CppType* type))
			MAKE_PROXY_API(il2cpp_method_is_generic, bool, (const MethodInfo* method))
			MAKE_PROXY_API(il2cpp_class_is_generic, bool, (const Il2CppClass* klass))
			MAKE_PROXY_API(il2cpp_class_get_element_class, Il2CppClass*, (const Il2CppClass* klass))
			MAKE_PROXY_API(il2cpp_array_element_size, int, (const Il2CppClass* array_class))
			MAKE_PROXY_API(il2cpp_class_is_inflated, bool, (const Il2CppClass* klass))
			MAKE_PROXY_API(il2cpp_class_get_nested_types, Il2CppClass*, (const Il2CppClass* klass, void** iter))
			MAKE_PROXY_API(il2cpp_type_get_type, int, (const Il2CppType* type))
			MAKE_PROXY_API(il2cpp_class_get_interfaces, Il2CppClass*, (const Il2CppClass* klass, void** iter))
			MAKE_PROXY_API(il2cpp_type_get_name, char*, (const Il2CppType* type))
			MAKE_PROXY_API(il2cpp_type_get_assembly_qualified_name, char*, (const Il2CppType* type))
			MAKE_PROXY_API(il2cpp_type_equals, bool, (const Il2CppType* type, const Il2CppType* otherType))
			MAKE_PROXY_API(il2cpp_property_get_flags, uint32_t, (PropertyInfo* prop))
			MAKE_PROXY_API(il2cpp_property_get_name, char*, (PropertyInfo* prop))
			MAKE_PROXY_API(il2cpp_property_get_get_method, MethodInfo*, (PropertyInfo* prop))
			MAKE_PROXY_API(il2cpp_property_get_set_method, MethodInfo*, (PropertyInfo* prop))
			MAKE_PROXY_API(il2cpp_field_has_attribute, bool, (FieldInfo* field, Il2CppClass* attr_class))
			MAKE_PROXY_API(il2cpp_field_get_parent, Il2CppClass*, (FieldInfo* field))


			// https://github.com/maugatto1996/SOSAMAZONIA/blob/d17d1926f0434db2c1efd009fa8aca8e8e22a42c/sddsfg/Il2CppOutputProject/IL2CPP/libil2cpp/vm/GenericClass.cpp#L223
			MAKE_PROXY_API(GenericClass_GetTypeDefinition, Il2CppClass*, (Il2CppGenericClass* gclass))

		public:
			static IntPtr GetModuleBase();
			static IL2cppNativeApi();
		};
	}
}

