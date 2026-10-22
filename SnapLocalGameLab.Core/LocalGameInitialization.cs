using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.InteropServices;
using IL2CppApi.Wrappers;

namespace SnapLocalGameLab.Core
{
    internal static class LocalGameInitialization
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr CreateDelegate(IntPtr self, IntPtr method);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void InitRandDelegate(IntPtr self, ulong nullableSeed, IntPtr randomKey, IntPtr method);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void InitConfigDelegate(IntPtr self, IntPtr method);
        private static CreateDelegate _create;
        private static InitRandDelegate _initRand;
        private static InitConfigDelegate _initConfig;
        private static IL2CppFieldInfoWrapper _configField;
        private static IL2CppMethodInfoWrapper _getGameState;
        private static IntPtr _manager;
        private static GameApi _pendingApi;
        private static IntPtr _pendingState;
        private static IntPtr _pendingConfig;
        private static int? _pendingSeed;
        private static IntPtr _pendingKey;
        private static LocalPresentation _pendingPresentation;
        private static readonly ConcurrentQueue<string> Messages = new ConcurrentQueue<string>();
        [ThreadStatic] private static GameApi _activeApi;
        [ThreadStatic] private static IntPtr _activeConfig;
        [ThreadStatic] private static int? _activeSeed;
        [ThreadStatic] private static IntPtr _activeKey;

        public static bool TryGetMessage(out string message) => Messages.TryDequeue(out message);

        public static void Prepare(GameApi api, bool useSeed)
        {
            var game = api.Type("CubeGame", "Game");
            var manager = api.Type("CubeUnity.App.State", "GameManager");
            _configField = api.Runtime.Field(game, "_gameConfig");
            _getGameState = api.Method(manager, "get_GameState", 0);
            if (useSeed && _initRand == null) PrepareSeed(api, game);
            if (_initConfig == null)
                _initConfig = HookManager.CreateHook<InitConfigDelegate>(api.Method(game, "InitConfigInternal", 0).MethodPointer,
                    InitConfig, "Local snap configuration");
            if (_create == null)
                _create = HookManager.CreateHook<CreateDelegate>(api.Method(manager, "CreateLocalGame", 0).MethodPointer,
                    Create, "Local creation scope");
        }

        private static void PrepareSeed(GameApi api, IL2CppClassWrapper game)
        {
            var init = game.GetMethods().SingleOrDefault(m => m.Name == "InitRand" && m.ParamCount == 2
                && m.GetParameters()[1].Name == "System.Byte[]");
            if (init == null) throw new NotSupportedException("The client's InitRand(integer seed, byte[] key) method was not found.");
            var parameters = init.GetParameters();
            var nullable = api.Runtime.ClassFromType(parameters[0]);
            if (!parameters[0].Name.Contains("System.Int32") || !nullable.IsValueType || nullable.ValueSize != 8
                || api.Runtime.Field(nullable, "value").Offset.ToInt64()
                   - api.Runtime.Field(nullable, "hasValue").Offset.ToInt64() != 4)
                throw new NotSupportedException("The random-seed argument has an unsupported native layout.");
            _initRand = HookManager.CreateHook<InitRandDelegate>(init.MethodPointer, InitRand, "Local random seed/key");
        }

        public static void Arm(IntPtr manager, LocalGameOptions options)
        {
            Cancel();
            var api = new GameApi();
            try
            {
                var managerType = api.Type("CubeUnity.App.State", "GameManager");
                var current = api.Call(managerType, manager, "get_CurrentGame");
                if (!api.Runtime.IsInstance(current, api.Type("CubeUnity.App.State", "LocalGame"))
                    || api.Value<bool>(managerType, manager, "get_IsReplay"))
                    throw new InvalidOperationException("Snap settings can only be applied to a new local game.");
                var state = api.Call(managerType, manager, "get_GameState");
                var source = api.Call(managerType, manager, "get_GameConfigDef");
                var config = SnapConfiguration.Create(api, source, options);
                var presentation = new LocalPresentation(api, options.Presentation);
                var key = options.RandomKey == null ? IntPtr.Zero : api.Bytes(options.RandomKey);
                api.Runtime.WriteReference(state, _configField, config);
                _manager = manager;
                _pendingState = state;
                _pendingConfig = config;
                _pendingSeed = options.Seed;
                _pendingKey = key;
                _pendingPresentation = presentation;
                _pendingApi = api;
            }
            catch { api.Dispose(); throw; }
        }

        public static void Cancel() => TakePendingApi()?.Dispose();

        private static GameApi TakePendingApi()
        {
            var api = _pendingApi;
            _pendingApi = null;
            _manager = IntPtr.Zero;
            _pendingState = IntPtr.Zero;
            _pendingConfig = IntPtr.Zero;
            _pendingSeed = null;
            _pendingKey = IntPtr.Zero;
            _pendingPresentation = null;
            return api;
        }

        private static IntPtr Create(IntPtr self, IntPtr method)
        {
            if (self != _manager || _pendingApi == null) return _create(self, method);
            if (_pendingApi.Runtime.Invoke(_getGameState, self) != _pendingState)
            {
                Cancel();
                return _create(self, method);
            }

            var api = _pendingApi;
            var previousApi = _activeApi;
            var previousConfig = _activeConfig;
            var previousSeed = _activeSeed;
            var previousKey = _activeKey;
            var presentation = _pendingPresentation;
            _activeApi = api;
            _activeConfig = _pendingConfig;
            _activeSeed = _pendingSeed;
            _activeKey = _pendingKey;
            TakePendingApi();
            try
            {
                var result = _create(self, method);
                try
                {
                    api.Runtime.Retain(result);
                    var managerType = api.Type("CubeUnity.App.State", "GameManager");
                    var localAccount = api.Runtime.StringValue(api.Call(managerType, self, "get_LocalPlayerAccountId"));
                    presentation?.Apply(api, result, localAccount);
                    var state = api.Runtime.Invoke(_getGameState, self);
                    if (state != result) presentation?.Apply(api, state, localAccount);
                }
                catch (Exception exception) { Messages.Enqueue("Could not apply local appearance: " + exception.Message); }
                return result;
            }
            finally
            {
                _activeApi = previousApi;
                _activeConfig = previousConfig;
                _activeSeed = previousSeed;
                _activeKey = previousKey;
                api.Dispose();
            }
        }

        private static void InitConfig(IntPtr self, IntPtr method)
        {
            if (_activeApi != null) _activeApi.Runtime.WriteReference(self, _configField, _activeConfig);
            _initConfig(self, method);
        }

        private static void InitRand(IntPtr self, ulong seed, IntPtr key, IntPtr method)
        {
            if (_activeApi != null && _activeSeed.HasValue)
            {
                seed = ((ulong)(uint)_activeSeed.Value << 32) | 1UL;
                key = IntPtr.Zero;
                Messages.Enqueue("Applied legacy integer seed: " + _activeSeed.Value);
            }
            else if (_activeApi != null && _activeKey != IntPtr.Zero)
            {
                seed = 0;
                key = _activeKey;
                Messages.Enqueue("Applied the 16-byte random key.");
            }
            _initRand(self, seed, key, method);
        }
    }
}
