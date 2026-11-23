using System;
using System.Collections.Generic;
using System.Linq;
using IL2CppApi.Wrappers;
using A = IL2CppApi.Runtime.Il2CppArgument;

namespace SnapLocalGameLab.Core
{
    internal sealed class ChampionArenaSetup
    {
        private readonly string _playerChampion;
        private readonly string _opponentChampion;
        private readonly IL2CppMethodInfoWrapper _getDeck;
        private readonly int _player;
        private readonly int _opponent;

        public ChampionArenaSetup(GameApi api, IntPtr config, LocalGameOptions options)
        {
            var supported = ReadChampions(api, config);
            _playerChampion = CanonicalChampion(api, supported, options.PlayerChampion);
            _opponentChampion = CanonicalChampion(api, supported, options.OpponentChampion);
            var managerType = api.Type("CubeUnity.App.State", "GameManager");
            var playerType = api.Runtime.NestedClass(managerType, "LocalPlayer");
            _player = api.EnumValue(playerType, "Player");
            _opponent = api.EnumValue(playerType, "Ai");
            var getDeck = managerType.GetMethods().SingleOrDefault(m =>
                m.Name.StartsWith("<CreateLocalGame>g__GetDeckForLocalGame|", StringComparison.Ordinal)
                && m.ParamCount == 2 && m.ReturnType.Name == "Cube.Common.Deck"
                && m.GetParameters()[0].Name == playerType.Type.Name
                && m.GetParameters()[1].Name == "CubeDef.LeagueDefId");
            if (getDeck == null)
                throw new NotSupportedException("The client's local deck selection method was not found.");
            _getDeck = api.Runtime.Method(managerType, getDeck.Name, false,
                getDeck.GetParameters().Select(p => p.Name).ToArray());
        }

        public static HashSet<string> ReadChampions(GameApi api, IntPtr config)
        {
            var rules = api.Call(api.Type("CubeDef", "GameConfigDef"), config, "get_GameRulesDef");
            var special = api.Call(api.Type("CubeDef", "GameRulesDef"), rules, "get_SpecialDeck");
            if (special == IntPtr.Zero)
                throw new NotSupportedException("The client's Champion Arena configuration has no special deck rules.");
            var list = api.Runtime.ReadReference(special, api.Runtime.Field(api.Type("CubeDef", "SpecialDeckDef"), "Champions"));
            return new HashSet<string>(api.References(list).Select(id => api.Runtime.StringValue(api.Runtime.Value<IntPtr>(id)))
                .Where(id => !string.IsNullOrWhiteSpace(id)), StringComparer.OrdinalIgnoreCase);
        }

        private static string CanonicalChampion(GameApi api, HashSet<string> supported, string id)
        {
            var canonical = api.Runtime.StringValue(GameCatalog.CardId(api, id));
            if (!supported.Contains(canonical))
                throw new ArgumentException("The client's Champion Arena rules do not support " + canonical + ".");
            return canonical;
        }

        public void PrepareDecks(GameApi api, IntPtr manager, int league)
        {
            var playerSource = api.Runtime.Invoke(_getDeck, manager, A.Value(_player), A.Value(league));
            var opponentSource = api.Runtime.Invoke(_getDeck, manager, A.Value(_opponent), A.Value(league));
            var playerDeck = PrepareDeck(api, playerSource, _playerChampion);
            var opponentDeck = PrepareDeck(api, opponentSource, _opponentChampion);
            var managerType = api.Type("CubeUnity.App.State", "GameManager");
            api.Call(managerType, manager, "set_DebugOverrideSelectedDeck", A.Ref(playerDeck));
            api.Call(managerType, manager, "set_DebugOverrideSelectedDeckAi", A.Ref(opponentDeck));
            LocalGameInitialization.Report("Champions: Me / " + _playerChampion + "; Opponent / " + _opponentChampion + ".");
        }

        private static IntPtr PrepareDeck(GameApi api, IntPtr source, string championId)
        {
            if (source == IntPtr.Zero) throw new InvalidOperationException("Select a starting deck for both players.");
            var deckType = api.Type("Cube.Common", "Deck");
            var cardType = api.Type("Cube.Common", "Card");
            var cardListType = api.ReturnType(deckType, "get_Cards");
            var idListType = api.ReturnType(deckType, "get_CardIds");
            var cards = api.New(cardListType);
            var ids = api.New(idListType);
            var champion = IntPtr.Zero;
            var isChampion = api.Runtime.Method(api.Type("CubeDef", "CardDefExtensions"), "IsChampion", true, "CubeDef.CardDef");
            foreach (var card in api.References(api.Call(deckType, source, "get_Cards")))
            {
                var id = api.Runtime.StringValue(api.Value<IntPtr>(cardType, card, "get_CardDefId"));
                if (api.Runtime.InvokeValue<bool>(isChampion, IntPtr.Zero, A.Ref(GameCatalog.FindCard(api, id))))
                {
                    if (string.Equals(id, championId, StringComparison.OrdinalIgnoreCase) && champion == IntPtr.Zero)
                        champion = card;
                    continue;
                }
                Add(card);
            }
            if (champion == IntPtr.Zero)
            {
                var championDeck = LocalGameService.CreateDeck(api, new[] { new DeckEntry { CardId = championId, Count = 1 } });
                champion = api.References(api.Call(deckType, championDeck, "get_Cards")).Single();
            }
            Add(champion);
            var copy = api.Call(api.Type("System", "Object"), source, "MemberwiseClone");
            api.Call(deckType, copy, "set_Cards", A.Ref(cards));
            api.Call(deckType, copy, "set_CardIds", A.Ref(ids));
            return copy;

            void Add(IntPtr card)
            {
                api.Call(cardListType, cards, "Add", A.Ref(card));
                api.Call(idListType, ids, "Add", A.Ref(api.Call(cardType, card, "get_Id")));
            }
        }
    }
}
