using System;
using System.Linq;
using IL2CppApi.Runtime;
using IL2CppApi.Wrappers;
using A = IL2CppApi.Runtime.Il2CppArgument;

namespace SnapLocalGameLab.Core
{
    internal static class LocalGameService
    {
        public static string Start(LocalGameOptions options)
        {
            options.ValidateSnapping();
            using (var api = new GameApi())
            {
                var managerType = api.Type("CubeUnity.App.State", "GameManager");
                var manager = api.Manager(managerType);
                var sceneType = api.Type("CubeUnity.App.Scene", "CubeSceneManager");
                var sceneManager = api.Manager(sceneType);
                RequireHomeScreen(api, sceneType, sceneManager);
                var league = api.EnumValue(api.Type("CubeDef", "LeagueDefId"), options.League);
                ValidateLeague(api, league);
                var playerDeck = DecodeDeck(api, options.PlayerDeck);
                var aiDeck = DecodeDeck(api, options.AiDeck);
                var locations = CreateLocations(api, managerType, options.Locations);
                var initialize = api.Method(managerType, "Initialize", 4);
                var parameters = initialize.GetParameters();
                if (!parameters[0].Name.Contains("GameMode") || !parameters[1].Name.EndsWith("LeagueDefId")
                    || !parameters[2].Name.Contains("LimitedEventDef") || parameters[3].Name != "System.String")
                    throw new NotSupportedException("Unexpected GameManager.Initialize signature.");
                var load = api.Method(sceneType, "ForceLoadPrimaryScene", 3);
                LocalGameInitialization.Prepare(api, options.Seed.HasValue);
                api.Runtime.Invoke(initialize, manager, A.Value(0), A.Value(league),
                    api.Default(parameters[2]), A.Ref(api.Runtime.String("Play")));
                api.Call(managerType, manager, "set_DebugOverrideSelectedDeck", A.Ref(playerDeck));
                api.Call(managerType, manager, "set_DebugOverrideSelectedDeckAi", A.Ref(aiDeck));
                api.Call(managerType, manager, "set_DebugStartingLocations", A.Ref(locations));
                api.Call(managerType, manager, "set_DebugNoPostGameRewards", A.Value(true));
                api.Call(managerType, manager, "set_DebugIgnoreNewCardEncountered", A.Value(options.SkipNewCardPrompts));
                LocalGameInitialization.Arm(manager, options);
                try
                {
                    api.Runtime.Invoke(load, sceneManager, A.Ref(api.Runtime.String("Game")), A.Value(1), A.Ref(IntPtr.Zero));
                }
                catch { LocalGameInitialization.Cancel(); throw; }
                return "Local " + options.League + " loading.";
            }
        }

        public static string ReturnToMainScreen()
        {
            using (var api = new GameApi())
            {
                var sceneType = api.Type("CubeUnity.App.Scene", "CubeSceneManager");
                var sceneManager = api.Manager(sceneType);
                LocalGameInitialization.Cancel();
                api.Call(sceneType, sceneManager, "ForceLoadPrimaryScene",
                    A.Ref(api.Runtime.String("Play")), A.Value(1), A.Ref(IntPtr.Zero));
                return "Returning to main screen.";
            }
        }

        private static void RequireHomeScreen(GameApi api, IL2CppClassWrapper type, IntPtr manager)
        {
            if (api.Runtime.ReadField<bool>(manager, api.Runtime.Field(type, "_loadingNextPrimaryScene")))
                throw new InvalidOperationException("Wait for the current scene transition to finish.");
            var handle = api.Runtime.ReadField<int>(manager, api.Runtime.Field(type, "_primaryScene"));
            var scene = api.Type("UnityEngine.SceneManagement", "Scene");
            var name = api.Runtime.StringValue(api.Call(scene, IntPtr.Zero, "GetNameInternal", A.Value(handle)));
            if (name != "Play") throw new InvalidOperationException("Return to the Play/home screen before starting a local game. Current scene: " + name);
        }

        private static void ValidateLeague(GameApi api, int league)
        {
            var list = api.Type("CubeDef", "GameConfigDefList");
            var find = api.Method(list, "Find", 2, true);
            var config = api.Runtime.Invoke(find, IntPtr.Zero, A.Value(league), api.Default(find.GetParameters()[1]));
            var type = api.Runtime.ClassFromType(find.ReturnType);
            if (config == IntPtr.Zero || !api.Value<bool>(type, config, "get_IsAiGame"))
                throw new InvalidOperationException("The selected league is not configured as a local AI game.");
        }

        private static IntPtr DecodeDeck(GameApi api, string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return IntPtr.Zero;

            var type = api.Type("Cube.Common", "DeckSharingConverterUtil");
            var deck = api.Call(type, IntPtr.Zero, "ToDeck", A.Ref(api.Runtime.String(text)));
            if (deck == IntPtr.Zero) throw new ArgumentException("The game could not decode the deck.");
            return deck;
        }

        private static IntPtr CreateLocations(GameApi api, IL2CppClassWrapper manager, string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return IntPtr.Zero;
            var names = text.Split(',').Select(s => s.Trim()).ToArray();
            if (names.Length != 3 || names.Any(string.IsNullOrWhiteSpace))
                throw new ArgumentException("Enter exactly three internal location IDs separated by commas, or leave blank.");
            var setter = api.Method(manager, "set_DebugStartingLocations", 1);
            var listType = api.Runtime.ClassFromType(setter.GetParameters()[0]);
            var list = api.New(listType);
            var defs = api.Type("CubeDef", "LocationDefList");
            foreach (var name in names)
            {
                var id = A.Value(api.Runtime.String(name));
                var def = api.Call(defs, IntPtr.Zero, "Find", id, A.Value(true));
                if (def == IntPtr.Zero) throw new ArgumentException("Unknown location: " + name);
                var defType = api.Type("CubeDef", "LocationDef");
                var canonical = api.Value<IntPtr>(defType, def, "get_LocationDefId");
                api.Call(listType, list, "Add", A.Value(canonical));
            }
            return list;
        }
    }
}
