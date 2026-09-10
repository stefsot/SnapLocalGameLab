#include "stdafx.h"
#include "IL2cppNativeApi.h"
#include "IL2CppDumper.h"
#include "Extensions.h"

using namespace IL2CppApi;
using namespace Native;


array<IL2CppImageWrapper^>^ IL2CppDumper::GetLoadedAssemblies()
{
	size_t assemblies_count;

	const auto domain = IL2cppNativeApi::il2cpp_domain_get();
	const auto assemblies = IL2cppNativeApi::il2cpp_domain_get_assemblies(domain, &assemblies_count);

	const auto array_assemblies = gcnew array<IL2CppImageWrapper^>(static_cast<int>(assemblies_count));

	for(auto i = 0; i < assemblies_count; i++)
	{
		const auto image = IL2cppNativeApi::il2cpp_assembly_get_image(assemblies[i]);
		array_assemblies[i] = gcnew IL2CppImageWrapper(image);
	}

	return array_assemblies;
}