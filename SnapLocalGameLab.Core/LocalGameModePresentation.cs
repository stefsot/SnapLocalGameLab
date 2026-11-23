using System;
using System.Linq;
using System.Runtime.InteropServices;
using IL2CppApi.Wrappers;
using A = IL2CppApi.Runtime.Il2CppArgument;

namespace SnapLocalGameLab.Core
{
    internal static class LocalGameModePresentation
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr ViewNameDelegate(IntPtr self, IntPtr method);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr LeagueLookupDelegate(IntPtr self, int league, IntPtr method);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void BoardUpdatedDelegate(IntPtr self, int previousLeague, int league, IntPtr method);

        private static ViewNameDelegate _viewName;
        private static LeagueLookupDelegate _eventProviderLookup;
        private static BoardUpdatedDelegate _boardUpdated;
        private static IL2CppMethodInfoWrapper _getCurrentGame;
        private static GameApi _roots;
        private static IntPtr _manager;
        private static IntPtr _localGame;
        private static IntPtr _name;
        private static IntPtr _eventProvider;
        private static IntPtr _boardId;
        private static IntPtr _boardProvider;
        private static IntPtr _previousBoardId;
        private static bool _boardWasChanged;
        private static int _league;
        private static bool _reportedFailure;

        public static void Prepare(GameApi api, LocalGameMode mode)
        {
            if (mode == LocalGameMode.Standard) return;
            if (_viewName == null)
            {
                _getCurrentGame = api.Runtime.Method(api.Type("CubeUnity.App.State", "GameManager"), "get_CurrentGame", false);
                var getter = api.Runtime.Method(api.Type("CubeUnity.App.Game", "BasicLimitedEventGameViewController"),
                    "get_LimitedEventGameModeViewDefName", false);
                _viewName = HookManager.CreateHook<ViewNameDelegate>(getter.MethodPointer, GetViewName, "Local mode view assets");
            }
            if (mode != LocalGameMode.ChampionArena) return;
            if (_eventProviderLookup == null)
            {
                var lookup = api.Runtime.Method(api.Type("CubeUnity.App.State", "CustomGameEventManager"),
                    "GetEventDataProviderForLeague", false, "CubeDef.LeagueDefId");
                _eventProviderLookup = HookManager.CreateHook<LeagueLookupDelegate>(lookup.MethodPointer,
                    GetEventProvider, "Local champion event presentation");
            }
            if (_boardUpdated == null)
            {
                var update = api.Runtime.Method(api.Type("", "GameBoardDataProvider"), "CurrentLeagueDefUpdated", false,
                    "CubeDef.LeagueDefId", "CubeDef.LeagueDefId");
                _boardUpdated = HookManager.CreateHook<BoardUpdatedDelegate>(update.MethodPointer,
                    BoardUpdated, "Local champion board");
            }
        }

        public static void Activate(IntPtr manager, LocalGameOptions options)
        {
            Reset();
            if (options.Mode == LocalGameMode.Standard) return;
            var roots = new GameApi();
            try
            {
                var game = roots.Runtime.Invoke(_getCurrentGame, manager);
                if (!roots.Runtime.IsInstance(game, roots.Type("CubeUnity.App.State", "LocalGame")))
                    throw new InvalidOperationException("Mode views can only be enabled for a local game.");
                _manager = roots.Runtime.Retain(manager);
                _localGame = game;
                _name = roots.Runtime.String(options.SelectedLeague);
                _league = roots.EnumValue(roots.Type("CubeDef", "LeagueDefId"), options.SelectedLeague);
                if (options.Mode == LocalGameMode.ChampionArena)
                    _eventProvider = ChampionArenaPresentation.Create(roots, options, _league, out _boardId);
                _roots = roots;
                if (_boardId != IntPtr.Zero) RefreshBoard(roots);
            }
            catch
            {
                if (_roots == roots) Reset();
                else roots.Dispose();
                throw;
            }
        }

        public static void Reset()
        {
            var roots = _roots;
            var boardId = _boardId;
            var boardProvider = _boardProvider;
            var previousBoardId = _previousBoardId;
            var boardWasChanged = _boardWasChanged;
            _roots = null;
            _manager = IntPtr.Zero;
            _localGame = IntPtr.Zero;
            _name = IntPtr.Zero;
            _eventProvider = IntPtr.Zero;
            _boardId = IntPtr.Zero;
            _boardProvider = IntPtr.Zero;
            _previousBoardId = IntPtr.Zero;
            _boardWasChanged = false;
            _league = 0;
            _reportedFailure = false;
            if (roots == null) return;
            try
            {
                if (boardWasChanged)
                {
                    var type = roots.Type("", "GameBoardDataProvider");
                    var property = roots.Call(type, boardProvider, "get_CurrentGameBoardDefId");
                    var propertyType = roots.ReturnType(type, "get_CurrentGameBoardDefId");
                    if (roots.Value<IntPtr>(propertyType, property, "get_Value") == boardId)
                        roots.Call(propertyType, property, "set_Value", A.Value(previousBoardId));
                }
            }
            catch (Exception exception) { LocalGameInitialization.Report("Could not restore the board: " + exception.Message); }
            finally { roots.Dispose(); }
        }

        private static void RefreshBoard(GameApi api)
        {
            var containerType = api.Type("CubeUnity.Infra.DataBinding", "DataProviderContainer");
            var container = api.Manager(containerType);
            var field = api.Runtime.Field(containerType, "_instanceDictionary");
            var dictionary = api.Runtime.ReadReference(container, field);
            var values = api.Call(api.Runtime.ClassFromType(field.Type), dictionary, "get_Values");
            var type = api.Type("", "GameBoardDataProvider");
            var provider = api.References(values).FirstOrDefault(value => api.Runtime.IsInstance(value, type));
            if (provider != IntPtr.Zero) ApplyBoard(api, provider);
        }

        private static void ApplyBoard(GameApi api, IntPtr provider)
        {
            var type = api.Type("", "GameBoardDataProvider");
            var property = api.Call(type, provider, "get_CurrentGameBoardDefId");
            var propertyType = api.ReturnType(type, "get_CurrentGameBoardDefId");
            var previous = api.Value<IntPtr>(propertyType, property, "get_Value");
            if (previous == _boardId) return;
            _boardProvider = _roots.Runtime.Retain(provider);
            _previousBoardId = _roots.Runtime.Retain(previous);
            _boardWasChanged = true;
            api.Call(propertyType, property, "set_Value", A.Value(_boardId));
        }

        private static bool IsActive()
        {
            if (_roots != null)
            {
                try
                {
                    using (var api = new GameApi())
                        return api.Runtime.Invoke(_getCurrentGame, _manager) == _localGame;
                }
                catch (Exception exception)
                {
                    if (!_reportedFailure)
                    {
                        _reportedFailure = true;
                        LocalGameInitialization.Report("Could not resolve local mode UI: " + exception.Message);
                    }
                }
            }
            return false;
        }

        private static IntPtr GetViewName(IntPtr self, IntPtr method) =>
            IsActive() ? _name : _viewName(self, method);

        private static IntPtr GetEventProvider(IntPtr self, int league, IntPtr method) =>
            _eventProvider != IntPtr.Zero && league == _league && IsActive()
                ? _eventProvider : _eventProviderLookup(self, league, method);

        private static void BoardUpdated(IntPtr self, int previousLeague, int league, IntPtr method)
        {
            _boardUpdated(self, previousLeague, league, method);
            if (_boardId == IntPtr.Zero || league != _league || !IsActive()) return;
            try
            {
                using (var api = new GameApi()) ApplyBoard(api, self);
            }
            catch (Exception exception) { LocalGameInitialization.Report("Could not select the champion board: " + exception.Message); }
        }
    }
}
