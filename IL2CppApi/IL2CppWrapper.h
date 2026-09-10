#pragma once

using namespace System;

namespace IL2CppApi
{
	namespace Wrappers
	{
		template<typename T>
		public ref class IL2CppWrapper
		{
		internal:
			T* _native_pointer;

		public:
			property IntPtr Ptr
			{
				IntPtr get()
				{
					return IntPtr(_native_pointer);
				};
			};

			property bool IsNull
			{
				bool get()
				{
					return _native_pointer == nullptr;
				};
			};

		internal:
			IL2CppWrapper(T* native_ptr) : _native_pointer(native_ptr) {}
		};
	}
}