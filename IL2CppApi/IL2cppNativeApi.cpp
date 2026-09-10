#include "stdafx.h"
#include "IL2cppNativeApi.h"
#include <vcclr.h>

//using namespace System;
using namespace IL2CppApi::Native;


Il2CppDomain* IL2cppNativeApi::il2cpp_domain_get()
{
	GET_ONCE_API_PROC_ADDRESS(il2cpp_domain_get)
	CALL_PROC_API(il2cpp_domain_get, ())
}

Il2CppAssembly** IL2cppNativeApi::il2cpp_domain_get_assemblies(const Il2CppDomain* domain, size_t* size)
{
	GET_ONCE_API_PROC_ADDRESS(il2cpp_domain_get_assemblies)
	CALL_PROC_API(il2cpp_domain_get_assemblies, (domain, size))
}

Il2CppImage* IL2cppNativeApi::il2cpp_assembly_get_image(const Il2CppAssembly* assembly)
{
	GET_ONCE_API_PROC_ADDRESS(il2cpp_assembly_get_image)
	CALL_PROC_API(il2cpp_assembly_get_image, (assembly))
}

size_t IL2cppNativeApi::il2cpp_image_get_class_count(const Il2CppImage* image)
{
	GET_ONCE_API_PROC_ADDRESS(il2cpp_image_get_class_count)
	CALL_PROC_API(il2cpp_image_get_class_count, (image))
}

Il2CppClass* IL2cppNativeApi::il2cpp_image_get_class(const Il2CppImage* image, size_t index)
{
	GET_ONCE_API_PROC_ADDRESS(il2cpp_image_get_class)
	CALL_PROC_API(il2cpp_image_get_class, (image, index))
}

const char* IL2cppNativeApi::il2cpp_class_get_name(const Il2CppClass* image)
{
	GET_ONCE_API_PROC_ADDRESS(il2cpp_class_get_name)
	CALL_PROC_API(il2cpp_class_get_name, (image))
}

const char* IL2cppNativeApi::il2cpp_class_get_namespace(const Il2CppClass* image)
{
	GET_ONCE_API_PROC_ADDRESS(il2cpp_class_get_namespace)
	CALL_PROC_API(il2cpp_class_get_namespace, (image))
}

MethodInfo* IL2cppNativeApi::il2cpp_class_get_methods(Il2CppClass* klass, void** iter)
{
	GET_ONCE_API_PROC_ADDRESS(il2cpp_class_get_methods)
	CALL_PROC_API(il2cpp_class_get_methods, (klass, iter))
}

FieldInfo* IL2cppNativeApi::il2cpp_class_get_fields(Il2CppClass* klass, void** iter)
{
	GET_ONCE_API_PROC_ADDRESS(il2cpp_class_get_fields)
	CALL_PROC_API(il2cpp_class_get_fields, (klass, iter))
}

PropertyInfo* IL2cppNativeApi::il2cpp_class_get_properties(Il2CppClass* klass, void** iter)
{
	GET_ONCE_API_PROC_ADDRESS(il2cpp_class_get_properties)
	CALL_PROC_API(il2cpp_class_get_properties, (klass, iter))
}

bool IL2cppNativeApi::il2cpp_class_is_valuetype(Il2CppClass* klass)
{
	GET_ONCE_API_PROC_ADDRESS(il2cpp_class_is_valuetype)
	CALL_PROC_API(il2cpp_class_is_valuetype, (klass))
}

Il2CppType* IL2cppNativeApi::il2cpp_field_get_type(FieldInfo* field)
{
	GET_ONCE_API_PROC_ADDRESS(il2cpp_field_get_type)
	CALL_PROC_API(il2cpp_field_get_type, (field))
}

int IL2cppNativeApi::il2cpp_field_get_flags(FieldInfo* field)
{
	GET_ONCE_API_PROC_ADDRESS(il2cpp_field_get_flags)
	CALL_PROC_API(il2cpp_field_get_flags, (field))
}

void IL2cppNativeApi::il2cpp_field_static_get_value(FieldInfo* field, void* value)
{
	GET_ONCE_API_PROC_ADDRESS(il2cpp_field_static_get_value)
	CALL_PROC_API(il2cpp_field_static_get_value, (field, value))
}

size_t IL2cppNativeApi::il2cpp_field_get_offset(FieldInfo* field)
{
	GET_ONCE_API_PROC_ADDRESS(il2cpp_field_get_offset)
	CALL_PROC_API(il2cpp_field_get_offset, (field))
}

Il2CppType* IL2cppNativeApi::il2cpp_class_get_type(Il2CppClass* klass)
{
	GET_ONCE_API_PROC_ADDRESS(il2cpp_class_get_type)
	CALL_PROC_API(il2cpp_class_get_type, (klass))
}

int32_t IL2cppNativeApi::il2cpp_class_value_size(Il2CppClass* klass, uint32_t* align)
{
	GET_ONCE_API_PROC_ADDRESS(il2cpp_class_value_size)
	CALL_PROC_API(il2cpp_class_value_size, (klass, align))
}

int IL2cppNativeApi::il2cpp_class_get_flags(Il2CppClass* klass)
{
	GET_ONCE_API_PROC_ADDRESS(il2cpp_class_get_flags)
	CALL_PROC_API(il2cpp_class_get_flags, (klass))
}

uint32_t IL2cppNativeApi::il2cpp_class_get_type_token(Il2CppClass* klass)
{
	GET_ONCE_API_PROC_ADDRESS(il2cpp_class_get_type_token)
	CALL_PROC_API(il2cpp_class_get_type_token, (klass))
}

Il2CppImage* IL2cppNativeApi::il2cpp_class_get_image(Il2CppClass* klass)
{
	GET_ONCE_API_PROC_ADDRESS(il2cpp_class_get_image)
	CALL_PROC_API(il2cpp_class_get_image, (klass))
}

int IL2cppNativeApi::il2cpp_class_get_rank(Il2CppClass* klass)
{
	GET_ONCE_API_PROC_ADDRESS(il2cpp_class_get_rank)
	CALL_PROC_API(il2cpp_class_get_rank, (klass))
}

Il2CppType* IL2cppNativeApi::il2cpp_class_enum_basetype(Il2CppClass* klass)
{
	GET_ONCE_API_PROC_ADDRESS(il2cpp_class_enum_basetype)
	CALL_PROC_API(il2cpp_class_enum_basetype, (klass))
}

Il2CppClass* IL2cppNativeApi::il2cpp_type_get_class_or_element_class(Il2CppType* type)
{
	GET_ONCE_API_PROC_ADDRESS(il2cpp_type_get_class_or_element_class)
	CALL_PROC_API(il2cpp_type_get_class_or_element_class, (type))
}


Il2CppClass* IL2cppNativeApi::il2cpp_class_get_declaring_type(Il2CppClass* klass)
{
	GET_ONCE_API_PROC_ADDRESS(il2cpp_class_get_declaring_type)
	CALL_PROC_API(il2cpp_class_get_declaring_type, (klass))
}

Il2CppClass* IL2cppNativeApi::il2cpp_class_get_parent(Il2CppClass* klass)
{
	GET_ONCE_API_PROC_ADDRESS(il2cpp_class_get_parent)
	CALL_PROC_API(il2cpp_class_get_parent, (klass))
}

uint32_t IL2cppNativeApi::il2cpp_method_get_flags(const MethodInfo* method, uint32_t* iflags)
{
	GET_ONCE_API_PROC_ADDRESS(il2cpp_method_get_flags)
	CALL_PROC_API(il2cpp_method_get_flags, (method, iflags))
}

bool IL2cppNativeApi::il2cpp_method_has_attribute(const MethodInfo* method, Il2CppClass* attr_class)
{
	GET_ONCE_API_PROC_ADDRESS(il2cpp_method_has_attribute)
	CALL_PROC_API(il2cpp_method_has_attribute, (method, attr_class))
}

bool IL2cppNativeApi::il2cpp_class_has_attribute(Il2CppClass* klass, Il2CppClass* attr_class)
{
	GET_ONCE_API_PROC_ADDRESS(il2cpp_class_has_attribute)
	CALL_PROC_API(il2cpp_class_has_attribute, (klass, attr_class))
}

Il2CppType* IL2cppNativeApi::il2cpp_method_get_return_type(const MethodInfo* method)
{
	GET_ONCE_API_PROC_ADDRESS(il2cpp_method_get_return_type)
	CALL_PROC_API(il2cpp_method_get_return_type, (method))
}

Il2CppClass* IL2cppNativeApi::il2cpp_method_get_declaring_type(const MethodInfo* method)
{
	GET_ONCE_API_PROC_ADDRESS(il2cpp_method_get_declaring_type)
	CALL_PROC_API(il2cpp_method_get_declaring_type, (method))
}

uint32_t IL2cppNativeApi::il2cpp_method_get_param_count(const MethodInfo* method)
{
	GET_ONCE_API_PROC_ADDRESS(il2cpp_method_get_param_count)
	CALL_PROC_API(il2cpp_method_get_param_count, (method))
}

Il2CppType* IL2cppNativeApi::il2cpp_method_get_param(const MethodInfo* method, uint32_t index)
{
	GET_ONCE_API_PROC_ADDRESS(il2cpp_method_get_param)
	CALL_PROC_API(il2cpp_method_get_param, (method, index))
}

uint32_t IL2cppNativeApi::il2cpp_method_get_token(const MethodInfo* method)
{
	GET_ONCE_API_PROC_ADDRESS(il2cpp_method_get_token)
	CALL_PROC_API(il2cpp_method_get_token, (method))
}

char* IL2cppNativeApi::il2cpp_method_get_param_name(const MethodInfo* method, uint32_t index)
{
	GET_ONCE_API_PROC_ADDRESS(il2cpp_method_get_param_name)
	CALL_PROC_API(il2cpp_method_get_param_name, (method, index))
}

Il2CppClass* IL2cppNativeApi::il2cpp_class_from_il2cpp_type(const Il2CppType* type)
{
	GET_ONCE_API_PROC_ADDRESS(il2cpp_class_from_il2cpp_type)
	CALL_PROC_API(il2cpp_class_from_il2cpp_type, (type))
}

bool IL2cppNativeApi::il2cpp_method_is_generic(const MethodInfo* method)
{
	GET_ONCE_API_PROC_ADDRESS(il2cpp_method_is_generic)
	CALL_PROC_API(il2cpp_method_is_generic, (method))
}

bool IL2cppNativeApi::il2cpp_class_is_generic(const Il2CppClass* klass)
{
	GET_ONCE_API_PROC_ADDRESS(il2cpp_class_is_generic)
	CALL_PROC_API(il2cpp_class_is_generic, (klass))
}

Il2CppClass* IL2cppNativeApi::il2cpp_class_get_element_class(const Il2CppClass* klass)
{
	GET_ONCE_API_PROC_ADDRESS(il2cpp_class_get_element_class)
	CALL_PROC_API(il2cpp_class_get_element_class, (klass))
}

int IL2cppNativeApi::il2cpp_array_element_size(const Il2CppClass* array_class)
{
	GET_ONCE_API_PROC_ADDRESS(il2cpp_array_element_size)
	CALL_PROC_API(il2cpp_array_element_size, (array_class))
}

bool IL2cppNativeApi::il2cpp_class_is_inflated(const Il2CppClass* klass)
{
	GET_ONCE_API_PROC_ADDRESS(il2cpp_class_is_inflated)
	CALL_PROC_API(il2cpp_class_is_inflated, (klass))
}

Il2CppClass* IL2cppNativeApi::il2cpp_class_get_nested_types(const Il2CppClass* klass, void** iter)
{
	GET_ONCE_API_PROC_ADDRESS(il2cpp_class_get_nested_types)
	CALL_PROC_API(il2cpp_class_get_nested_types, (klass, iter))
}

int IL2cppNativeApi::il2cpp_type_get_type(const Il2CppType* type)
{
	GET_ONCE_API_PROC_ADDRESS(il2cpp_type_get_type)
	CALL_PROC_API(il2cpp_type_get_type, (type))
}

Il2CppClass* IL2cppNativeApi::il2cpp_class_get_interfaces(const Il2CppClass* klass, void** iter)
{
	GET_ONCE_API_PROC_ADDRESS(il2cpp_class_get_interfaces)
	CALL_PROC_API(il2cpp_class_get_interfaces, (klass, iter))
}

char* IL2cppNativeApi::il2cpp_type_get_name(const Il2CppType* type)
{
	GET_ONCE_API_PROC_ADDRESS(il2cpp_type_get_name)
	CALL_PROC_API(il2cpp_type_get_name, (type))
}

char* IL2cppNativeApi::il2cpp_type_get_assembly_qualified_name(const Il2CppType* type)
{
	GET_ONCE_API_PROC_ADDRESS(il2cpp_type_get_assembly_qualified_name)
	CALL_PROC_API(il2cpp_type_get_assembly_qualified_name, (type))
}

bool IL2cppNativeApi::il2cpp_type_equals(const Il2CppType* type, const Il2CppType* otherType)
{
	GET_ONCE_API_PROC_ADDRESS(il2cpp_type_equals)
	CALL_PROC_API(il2cpp_type_equals, (type, otherType))
}

uint32_t IL2cppNativeApi::il2cpp_property_get_flags(PropertyInfo* prop)
{
	GET_ONCE_API_PROC_ADDRESS(il2cpp_property_get_flags)
	CALL_PROC_API(il2cpp_property_get_flags, (prop))
}

char* IL2cppNativeApi::il2cpp_property_get_name(PropertyInfo* prop)
{
	GET_ONCE_API_PROC_ADDRESS(il2cpp_property_get_name)
	CALL_PROC_API(il2cpp_property_get_name, (prop))
}

MethodInfo* IL2cppNativeApi::il2cpp_property_get_get_method(PropertyInfo* prop)
{
	GET_ONCE_API_PROC_ADDRESS(il2cpp_property_get_get_method)
	CALL_PROC_API(il2cpp_property_get_get_method, (prop))
}

MethodInfo* IL2cppNativeApi::il2cpp_property_get_set_method(PropertyInfo* prop)
{
	GET_ONCE_API_PROC_ADDRESS(il2cpp_property_get_set_method)
	CALL_PROC_API(il2cpp_property_get_set_method, (prop))
}

bool IL2cppNativeApi::il2cpp_field_has_attribute(FieldInfo* field, Il2CppClass* attr_class)
{
	GET_ONCE_API_PROC_ADDRESS(il2cpp_field_has_attribute)
	CALL_PROC_API(il2cpp_field_has_attribute, (field, attr_class))
}

Il2CppClass* IL2cppNativeApi::il2cpp_field_get_parent(FieldInfo* field)
{
	GET_ONCE_API_PROC_ADDRESS(il2cpp_field_get_parent)
	CALL_PROC_API(il2cpp_field_get_parent, (field))
}

// https://github.com/maugatto1996/SOSAMAZONIA/blob/d17d1926f0434db2c1efd009fa8aca8e8e22a42c/sddsfg/Il2CppOutputProject/IL2CPP/libil2cpp/vm/GenericClass.cpp#L223
Il2CppClass* IL2cppNativeApi::GenericClass_GetTypeDefinition(Il2CppGenericClass* gclass)
{
	if (api_GenericClass_GetTypeDefinition == nullptr)
		api_GenericClass_GetTypeDefinition = reinterpret_cast<decltype(api_GenericClass_GetTypeDefinition)>(reinterpret_cast<char*>(_module_handle) + 0x1767F0);

	return api_GenericClass_GetTypeDefinition(gclass);
}

IntPtr IL2cppNativeApi::GetModuleBase()
{
	return IntPtr(_module_handle);
}

static IL2cppNativeApi::IL2cppNativeApi()
{
	_module_handle = GetModuleHandleA("GameAssembly.dll");
}
