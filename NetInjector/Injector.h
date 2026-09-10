#pragma once
#include "pch.h"

__declspec(dllexport)
LRESULT MessageHookProc(int nCode, WPARAM wparam, LPARAM lparam);

namespace ManagedInjector
{
	ref class InjectorData;

	public ref class Injector : System::Object
	{
	public:

		static void Launch(System::IntPtr windowHandle, InjectorData^ transportData);

		static void LogMessage(System::String^ message, bool append);
	};
}