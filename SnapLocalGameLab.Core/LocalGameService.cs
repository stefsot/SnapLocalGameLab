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
                var playerDeck = options.PlayerCards == null ? DecodeDeck(api, options.PlayerDeck) : CreateDeck(api, options.PlayerCards);
                var aiDeck = options.AiCards == null ? DecodeDeck(api, options.AiDeck) : CreateDeck(api, options.AiCards);
                var locations = CreateLocations(api, managerType, options.Locations);
                var initialize = api.Method(managerType, "Initialize", 4);
                var parameters = initialize.GetParameters();
                if (!parameters[0].Name.Contains("GameMode") || !parameters[1].Name.EndsWith("LeagueDefId")
                    || !parameters[2].Name.Contains("LimitedEventDef") || parameters[3].Name != "System.String")
                    throw new NotSupportedException("Unexpected GameManager.Initialize signature.");
                var load = api.Method(sceneType, "ForceLoadPrimaryScene", 3);
                LocalGameInitialization.Prepare(api, options.Seed.HasValue || options.RandomKey != null);
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
                return "Local " + options.League + " loading"
                    + (string.IsNullOrWhiteSpace(options.AiPreset) ? "." : " with AI preset " + options.AiPreset + ".");
            }
        }

        public static string ReturnToMainScreen()
        {
            using (var api = new GameApi())
            {
                var sceneType = api.Type("CubeUnity.App.Scene", "CubeSceneManager");
                var sceneManager = api.Manager(sceneType);
                LocalGameInitialization.Cancel();
                LocalGameControls.RestoreTiming(api);
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
            var eventIds = api.Runtime.NestedClass(api.Type("CubeDef", "LimitedEventDef"), "Id");
            var eventIdType = api.Runtime.Method(eventIds, "Get", true, "System.String").ReturnType;
            var find = api.Runtime.Method(list, "Find", true, "CubeDef.LeagueDefId", eventIdType.Name);
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

        public static DeckEntry[] ReadDeck(string text)
        {
            using (var api = new GameApi())
            {
                var deck = DecodeDeck(api, text);
                if (deck == IntPtr.Zero) return Array.Empty<DeckEntry>();
                var cards = api.Call(api.Type("Cube.Common", "Deck"), deck, "get_Cards");
                var cardType = api.Type("Cube.Common", "Card");
                return api.References(cards).Select(card => api.Runtime.StringValue(
                        api.Value<IntPtr>(cardType, card, "get_CardDefId")))
                    .GroupBy(id => id).Select(g => new DeckEntry { CardId = g.Key, Count = g.Count() }).ToArray();
            }
        }

        private static IntPtr CreateDeck(GameApi api, DeckEntry[] cards)
        {
            LocalGameOptions.ValidateDeck(cards);
            var idFactory = api.Runtime.NestedClass(api.Type("CubeDef", "CardDef"), "Id");
            var getId = api.Runtime.Method(idFactory, "Get", true, "System.String");
            var arrayType = api.Type("System", "Array");
            var createArray = api.Runtime.Method(arrayType, "CreateInstance", true, "System.Type", "System.Int32");
            var array = api.Runtime.Invoke(createArray, IntPtr.Zero,
                A.Ref(api.Runtime.TypeObject(api.Runtime.ClassFromType(getId.ReturnType))), A.Value(cards.Sum(c => c.Count)));
            var set = api.Runtime.Method(arrayType, "SetValue", false, "System.Object", "System.Int32");
            var index = 0;
            foreach (var card in cards)
            {
                var canonical = GameCatalog.CardId(api, card.CardId);
                var boxed = api.Runtime.Invoke(getId, IntPtr.Zero, A.Ref(canonical));
                for (var copy = 0; copy < card.Count; copy++)
                    api.Runtime.Invoke(set, array, A.Ref(boxed), A.Value(index++));
            }
            var type = api.Type("Cube.Common", "Deck");
            var create = type.GetMethods().Single(m => m.Name == "Create" && m.ParamCount == 2
                && m.GetParameters()[0].Name.Contains("CardDef") && !m.GetParameters()[0].Name.Contains("ValueTuple"));
            var rarity = api.EnumValue(api.Type("CubeDef", "RarityDefId"), "Common");
            return api.Runtime.Invoke(create, IntPtr.Zero, A.Ref(array), A.Value(rarity));
        }

        private static IntPtr CreateLocations(GameApi api, IL2CppClassWrapper manager, string[] names)
        {
            if (names == null) return IntPtr.Zero;
            if (names.Length != 3 || names.Any(string.IsNullOrWhiteSpace))
                throw new ArgumentException("Choose all three starting locations, or turn off location overrides.");
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
