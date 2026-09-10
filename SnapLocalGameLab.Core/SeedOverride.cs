using System;
using System.Runtime.InteropServices;

namespace SnapLocalGameLab.Core
{
    internal static class SeedOverride
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr CreateDelegate(IntPtr self, IntPtr method);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void RandomDelegate(IntPtr self, ulong nullableSeed, IntPtr method);
        private static CreateDelegate _create;
        private static RandomDelegate _random;
        private static IntPtr _manager;
        private static int? _pending;
        private static DateTime _expires;
        [ThreadStatic] private static int? _active;

        public static void Prepare(GameApi api)
        {
            if (_create != null) return;
            var game = api.Type("CubeGame", "Game");
            var random = api.Method(game, "InitRand", 1);
            var nullable = api.Runtime.ClassFromType(random.GetParameters()[0]);
            if (!random.GetParameters()[0].Name.Contains("System.Int32") || nullable.ValueSize != 8
                || api.Runtime.Field(nullable, "value").Offset.ToInt64()
                   - api.Runtime.Field(nullable, "hasValue").Offset.ToInt64() != 4)
                throw new NotSupportedException("The random-seed argument has an unsupported native layout.");
            if (_random == null)
                _random = HookManager.CreateHook<RandomDelegate>(random.MethodPointer, InitRandom, "Local seed");
            var manager = api.Type("CubeUnity.App.State", "GameManager");
            _create = HookManager.CreateHook<CreateDelegate>(api.Method(manager, "CreateLocalGame", 0).MethodPointer,
                Create, "Local creation scope");
        }

        public static void Arm(IntPtr manager, int? seed)
        {
            _manager = manager;
            _pending = seed;
            _expires = DateTime.UtcNow.AddSeconds(30);
        }

        public static void Cancel() { _manager = IntPtr.Zero; _pending = null; }

        private static IntPtr Create(IntPtr self, IntPtr method)
        {
            var previous = _active;
            if (self == _manager && DateTime.UtcNow <= _expires)
            {
                _active = _pending;
                Cancel();
            }
            try { return _create(self, method); }
            finally { _active = previous; }
        }

        private static void InitRandom(IntPtr self, ulong seed, IntPtr method)
        {
            // Windows x64 passes this eight-byte Nullable<int> value in one integer register.
            if (_active.HasValue) seed = ((ulong)(uint)_active.Value << 32) | 1UL;
            _random(self, seed, method);
        }
    }
}
