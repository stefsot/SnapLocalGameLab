using System;
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
        private delegate void GameConstructorDelegate(IntPtr self, IntPtr oldGame, IntPtr id, int league,
            IntPtr limitedEventId, int scenario, IntPtr players, ulong nullableSeed, IntPtr startTime,
            IntPtr defRegistry, IntPtr liveConfig, IntPtr method);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void InitConfigDelegate(IntPtr self, IntPtr method);
        private static CreateDelegate _create;
        private static GameConstructorDelegate _gameConstructor;
        private static InitConfigDelegate _initConfig;
        private static IL2CppFieldInfoWrapper _configField;
        private static IL2CppMethodInfoWrapper _getGameState;
        private static IntPtr _manager;
        private static GameApi _pendingApi;
        private static IntPtr _pendingState;
        private static IntPtr _pendingConfig;
        private static int? _pendingSeed;
        [ThreadStatic] private static GameApi _activeApi;
        [ThreadStatic] private static IntPtr _activeConfig;
        [ThreadStatic] private static int? _activeSeed;

        public static void Prepare(GameApi api, bool useSeed)
        {
            var game = api.Type("CubeGame", "Game");
            var manager = api.Type("CubeUnity.App.State", "GameManager");
            _configField = api.Runtime.Field(game, "_gameConfig");
            _getGameState = api.Method(manager, "get_GameState", 0);
            if (useSeed && _gameConstructor == null) PrepareSeed(api, game);
            if (_initConfig == null)
                _initConfig = HookManager.CreateHook<InitConfigDelegate>(api.Method(game, "InitConfigInternal", 0).MethodPointer,
                    InitConfig, "Local snap configuration");
            if (_create == null)
                _create = HookManager.CreateHook<CreateDelegate>(api.Method(manager, "CreateLocalGame", 0).MethodPointer,
                    Create, "Local creation scope");
        }

        private static void PrepareSeed(GameApi api, IL2CppClassWrapper game)
        {
            var constructor = game.GetMethods().SingleOrDefault(m => m.Name == ".ctor" && m.ParamCount == 10
                && m.GetParameters()[4].Name == "CubeDef.ScenarioDefId");
            if (constructor == null) throw new NotSupportedException("The seeded game constructor was not found.");
            var parameters = constructor.GetParameters();
            ValidateConstructor(api, parameters);
            var nullable = api.Runtime.ClassFromType(parameters[6]);
            if (!parameters[6].Name.Contains("System.Int32")
                || api.Runtime.Field(nullable, "value").Offset.ToInt64()
                   - api.Runtime.Field(nullable, "hasValue").Offset.ToInt64() != 4)
                throw new NotSupportedException("The random-seed argument has an unsupported native layout.");
            _gameConstructor = HookManager.CreateHook<GameConstructorDelegate>(constructor.MethodPointer,
                ConstructGame, "Local seed");
        }

        private static void ValidateConstructor(GameApi api, IL2CppTypeWrapper[] parameters)
        {
            var sizes = new[] { 0, 0, 4, 8, 4, 0, 8, 16, 0, 0 };
            for (var i = 0; i < sizes.Length; i++)
            {
                var type = api.Runtime.ClassFromType(parameters[i]);
                if (type.IsValueType != (sizes[i] != 0) || (type.IsValueType && type.ValueSize != sizes[i]))
                    throw new NotSupportedException("Unsupported seeded game constructor argument: " + parameters[i].Name);
            }
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
                api.Runtime.WriteReference(state, _configField, config);
                _manager = manager;
                _pendingState = state;
                _pendingConfig = config;
                _pendingSeed = options.Seed;
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
            _activeApi = api;
            _activeConfig = _pendingConfig;
            _activeSeed = _pendingSeed;
            TakePendingApi();
            try { return _create(self, method); }
            finally
            {
                _activeApi = previousApi;
                _activeConfig = previousConfig;
                _activeSeed = previousSeed;
                api.Dispose();
            }
        }

        private static void InitConfig(IntPtr self, IntPtr method)
        {
            if (_activeApi != null) _activeApi.Runtime.WriteReference(self, _configField, _activeConfig);
            _initConfig(self, method);
        }

        private static void ConstructGame(IntPtr self, IntPtr oldGame, IntPtr id, int league,
            IntPtr limitedEventId, int scenario, IntPtr players, ulong seed, IntPtr startTime,
            IntPtr defRegistry, IntPtr liveConfig, IntPtr method)
        {
            if (_activeSeed.HasValue) seed = ((ulong)(uint)_activeSeed.Value << 32) | 1UL;
            _gameConstructor(self, oldGame, id, league, limitedEventId, scenario, players,
                seed, startTime, defRegistry, liveConfig, method);
        }
    }
}
