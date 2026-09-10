#pragma once

#include "../MinHook/include/MinHook.h"

namespace SnapLocalGameLab
{
namespace Hooking
{
public ref class HookHelper
{
public:
	static HookHelper()
	{
		MH_Initialize();
	}

	static bool CreateHook(void* target, void* detour, void** pOriginal)
	{
		return !(MH_CreateHook(target, detour, pOriginal) || MH_EnableHook(target) || MH_ApplyQueued());
	}

	static bool DeleteHook(void* target)
	{
		const auto status = MH_RemoveHook(target);
		return status == MH_OK || status == MH_ERROR_NOT_CREATED;
	}
};
}
}

