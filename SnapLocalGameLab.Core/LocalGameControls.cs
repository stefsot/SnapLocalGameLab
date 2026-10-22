using System;
using System.Linq;
using System.Threading.Tasks;
using IL2CppApi.Wrappers;
using A = IL2CppApi.Runtime.Il2CppArgument;

namespace SnapLocalGameLab.Core
{
    internal enum PlayerTarget { Me, Opponent, Both }
    internal enum CleanupTarget { Board, Hands, Decks, Everything }
    internal enum SpawnDestination { Deck, Hand, Location }

    internal sealed class MatchCard
    {
        public int EntityId;
        public string GameId;
        public string Label;
        public override string ToString() => Label;
    }

    internal static class LocalGameControls
    {
        private static float? _originalScale;
        private static float? _originalHold;

        public static string Draw(PlayerTarget target, string cardId = null) => WithMatch(match => match.WithSimulationLock(() =>
        {
            var id = cardId == null ? IntPtr.Zero : GameCatalog.CardId(match.Api, cardId);
            var players = match.PlayerIds(target);
            if (cardId != null)
            {
                var cards = match.Cards(match.Server, CleanupTarget.Decks);
                var type = match.Api.Type("CubeGame", "Card");
                foreach (var player in players)
                    if (!cards.Any(card => match.Owner(card) == player && string.Equals(cardId,
                        match.Api.Runtime.StringValue(match.Api.Value<IntPtr>(type, card, "get_CardDefId")), StringComparison.OrdinalIgnoreCase)))
                        throw new InvalidOperationException(cardId + " is not in "
                            + (player == match.LocalPlayerId ? "your" : "the opponent's") + " remaining deck.");
            }
            foreach (var player in players)
                match.Send("DrawCardCheatRequest", (type, request) =>
                {
                    match.Api.Call(type, request, "set_PlayerEntityId", A.Value(player));
                    if (id != IntPtr.Zero) match.Api.Call(type, request, "set_CardDefId", A.Value(id));
                });
            return "Draw " + (cardId ?? "next card") + " queued for " + target + ".";
        }));

        public static string AddEnergy(PlayerTarget target, int amount) => WithMatch(match =>
        {
            foreach (var player in match.PlayerIds(target))
                match.Send("AddEnergyCheatRequest", (type, request) =>
                {
                    match.Api.Call(type, request, "set_PlayerEntityId", A.Value(player));
                    match.Api.Call(type, request, "set_Amount", A.Value(amount));
                });
            return amount + " energy queued for " + target + ".";
        });

        public static string Spawn(PlayerTarget target, string cardId, SpawnDestination destination, int locationIndex) => WithMatch(match =>
        {
            if (!Enum.IsDefined(typeof(SpawnDestination), destination)) throw new ArgumentException("Choose a spawn destination.");
            if (destination == SpawnDestination.Location && (locationIndex < 0 || locationIndex > 2))
                throw new ArgumentException("Choose one of the three locations.");
            var api = match.Api;
            var id = GameCatalog.CardId(api, cardId);
            var playerType = api.Type("CubeGame", "Player");
            var players = api.References(api.Call(api.Type("CubeGame", "Game"), match.State, "get_Players"));
            var destinations = match.PlayerIds(target).Select(playerId =>
            {
                var player = players.Single(p => match.EntityId(p) == playerId);
                var zone = destination == SpawnDestination.Location
                    ? api.Call(api.Type("CubeGame", "Game"), match.State, "GetLocationBySlotIndex", A.Value(locationIndex), A.Value(false))
                    : api.Call(playerType, player, destination == SpawnDestination.Deck ? "get_Deck" : "get_Hand");
                if (zone == IntPtr.Zero) throw new InvalidOperationException("The selected destination is not available in this match.");
                return new { Player = playerId, Zone = match.EntityId(zone) };
            }).ToArray();
            var rarity = api.EnumValue(api.Type("CubeDef", "RarityDefId"), "Common");
            foreach (var entry in destinations)
                match.Send("SpawnCardCheatRequest", (type, request) =>
                {
                    api.Call(type, request, "set_CardDefId", A.Value(id));
                    api.Call(type, request, "set_ZoneEntityId", A.Value(entry.Zone));
                    api.Call(type, request, "set_OwnerEntityId", A.Value(entry.Player));
                    api.Call(type, request, "set_RarityDefId", A.Value(rarity));
                });
            var place = destination == SpawnDestination.Location ? "location " + (locationIndex + 1) : destination.ToString().ToLowerInvariant();
            return "Spawn " + cardId + " in " + place + " queued for " + target + ".";
        });

        public static string Cleanup(CleanupTarget cleanup, PlayerTarget target) => WithMatch(match =>
        {
            var api = match.Api;
            var kinds = cleanup == CleanupTarget.Everything
                ? new[] { CleanupTarget.Hands, CleanupTarget.Decks, CleanupTarget.Board } : new[] { cleanup };
            var players = match.PlayerIds(cleanup == CleanupTarget.Everything ? PlayerTarget.Both : target);
            var cards = kinds.SelectMany(match.Cards).Distinct().Where(c => players.Contains(match.Owner(c))).ToArray();
            var locationId = IntPtr.Zero;
            if (cleanup == CleanupTarget.Everything)
            {
                var def = api.Call(api.Type("CubeDef", "LocationDefList"), IntPtr.Zero, "Find",
                    A.Value(api.Runtime.String("Ruins")), A.Value(true));
                if (def == IntPtr.Zero) throw new InvalidOperationException("The client does not contain the Ruins location.");
                locationId = api.Value<IntPtr>(api.Type("CubeDef", "LocationDef"), def, "get_LocationDefId");
            }
            foreach (var card in cards)
            {
                var id = match.EntityId(card);
                match.Send("DestroyCardCheatRequest", (type, request) => api.Call(type, request, "set_EntityId", A.Value(id)));
            }
            if (cleanup == CleanupTarget.Everything)
                for (var i = 0; i < 3; i++)
                {
                    var index = i;
                    match.Send("SpawnLocationCheatRequest", (type, request) =>
                    {
                        api.Call(type, request, "set_LocationDefId", A.Value(locationId));
                        api.Call(type, request, "set_LocationIndex", A.Value(index));
                    });
                }
            return cleanup + ": queued cleanup for " + cards.Length + " cards"
                + (cleanup == CleanupTarget.Everything ? " and all three locations → Ruins." : ".");
        });

        public static MatchCard[] ReadCards()
        {
            using (var match = new Match())
                return match.WithSimulationLock(() =>
                {
                    var api = match.Api;
                    var type = api.Type("CubeGame", "Card");
                    var defType = api.Type("CubeDef", "CardDef");
                    return new[] { CleanupTarget.Board, CleanupTarget.Hands, CleanupTarget.Decks }
                        .SelectMany(zone => match.Cards(match.Server, zone).Select(card =>
                        {
                            var id = api.Runtime.StringValue(api.Value<IntPtr>(type, card, "get_CardDefId"));
                            var def = GameCatalog.FindCard(api, id);
                            var name = new CatalogItem { Id = id, Name = api.Runtime.StringValue(api.Call(defType, def, "get_Name")) };
                            var entity = match.EntityId(card);
                            return new MatchCard
                            {
                                EntityId = entity, GameId = match.GameId,
                                Label = (match.Owner(card) == match.LocalPlayerId ? "Me" : "Opponent") + " / " + zone
                                    + " / " + name + " (#" + entity + ")"
                            };
                        })).GroupBy(c => c.EntityId).Select(g => g.First()).ToArray();
                });
        }

        public static async Task<string> ApplyAugment(MatchCard card, string augmentId)
        {
            var expected = await GameThread.Run(() =>
            {
                using (var match = new Match())
                    return match.WithSimulationLock(() =>
                    {
                        var api = match.Api;
                        var currentCard = match.FindCard(card);
                        var def = GameCatalog.FindCard(api, augmentId);
                        var cardType = api.Type("CubeDef", "CardDef");
                        if (api.Value<int>(cardType, def, "get_Type") != api.EnumValue(api.Type("CubeDef", "CardType"), "Augment"))
                            throw new ArgumentException("Select an augment from the client list.");
                        AugmentConfiguration.EnableForGame(api, match.Server);
                        if (match.State != match.Server) AugmentConfiguration.EnableForGame(api, match.State);
                        var config = api.Call(api.Type("CubeGame", "Game"), match.Server, "get_GameConfigDef");
                        var rule = AugmentConfiguration.Rule(api, config);
                        var limit = AugmentConfiguration.Limit(api, rule);
                        var applied = match.Augments(currentCard);
                        if (applied.Length >= limit)
                            throw new InvalidOperationException("This card already has " + applied.Length + " augments; the match limit is " + limit + ".");
                        var id = api.Value<IntPtr>(cardType, def, "get_CardDefId");
                        if (!api.Value<bool>(api.Type("CubeDef", "CardAugmentRuleDef"), rule, "IsAugmentAllowed", A.Value(id)))
                            throw new InvalidOperationException("This match's rules do not allow " + augmentId + ".");
                        var count = match.CountAugment(currentCard, augmentId);
                        match.Send("ApplyAugmentCheatRequest", (type, request) =>
                        {
                            api.Call(type, request, "set_CardEntityId", A.Value(card.EntityId));
                            api.Call(type, request, "set_AugmentDefId", A.Value(id));
                        });
                        return count + 1;
                    });
            });
            foreach (var delay in new[] { 150, 300, 600, 1000 })
            {
                await Task.Delay(delay);
                var confirmed = await GameThread.Run(() =>
                {
                    using (var match = new Match())
                        return match.WithSimulationLock(() => match.CountAugment(match.FindCard(card), augmentId) >= expected,
                            onBusy: () => false);
                });
                if (confirmed) return "Applied " + augmentId + " to " + card.Label + ".";
            }
            return "Augment request sent, but application has not been confirmed. Wait for animations to finish and refresh match cards before trying again.";
        }

        public static string PauseTimer() => WithMatch(match =>
        {
            Command(match.Api, "StopTimer");
            return "Stopped the current turn's timer using the client's StopTimer command.";
        });

        public static string SetPresentation(PresentationOptions options) => WithMatch(match =>
        {
            var presentation = new LocalPresentation(match.Api, options);
            presentation.ApplyToBoard(match.Api, match.State, match.LocalAccountId);
            return "Applied local names and avatars to the current board.";
        });

        public static string SetTiming(float scale, float hold) => WithMatch(match =>
        {
            if (float.IsNaN(scale) || scale < 0 || scale > 5 || float.IsNaN(hold) || hold < 0.05f || hold > 30)
                throw new ArgumentException("Speed must be 0–5x and tap-and-hold time 0.05–30 seconds.");
            var api = match.Api;
            var time = api.Type("UnityEngine", "Time");
            var input = api.Type("CubeUnity.App.Game", "GameInputManager");
            if (!_originalScale.HasValue) _originalScale = api.Value<float>(time, IntPtr.Zero, "get_timeScale");
            if (!_originalHold.HasValue) _originalHold = api.Value<float>(input, IntPtr.Zero, "get_TapAndHoldTime");
            api.Call(input, IntPtr.Zero, "set_TapAndHoldTime", A.Value(hold));
            Command(api, "SetTimeScale", scale);
            return $"Game speed: {scale:0.##}x; tap-and-hold time: {hold:0.##} seconds.";
        });

        public static string RestoreTiming()
        {
            using (var api = new GameApi()) RestoreTiming(api);
            return "Restored the original speed and tap-and-hold time.";
        }

        public static void RestoreTiming(GameApi api)
        {
            if (_originalScale.HasValue)
            {
                Command(api, "SetTimeScale", _originalScale.Value);
                _originalScale = null;
            }
            if (_originalHold.HasValue)
            {
                api.Call(api.Type("CubeUnity.App.Game", "GameInputManager"), IntPtr.Zero, "set_TapAndHoldTime", A.Value(_originalHold.Value));
                _originalHold = null;
            }
        }

        private static void Command(GameApi api, string name, float? amount = null)
        {
            var type = api.Type("CubeUnity.App.Game", name);
            var create = api.Runtime.Method(api.Type("UnityEngine", "ScriptableObject"), "CreateInstance", true, "System.Type");
            var command = api.Runtime.Invoke(create, IntPtr.Zero, A.Ref(api.Runtime.TypeObject(type)));
            try
            {
                if (amount.HasValue) api.Call(type, command, "set_Value", A.Value(amount.Value));
                api.Call(type, command, "OnExecute");
            }
            finally
            {
                api.Runtime.Invoke(api.Runtime.Method(api.Type("UnityEngine", "Object"), "Destroy", true, "UnityEngine.Object"),
                    IntPtr.Zero, A.Ref(command));
            }
        }

        private static string WithMatch(Func<Match, string> action)
        {
            using (var match = new Match()) return action(match);
        }

        private sealed class Match : IDisposable
        {
            public readonly GameApi Api = new GameApi();
            public readonly IntPtr State;
            public readonly IntPtr Server;
            public readonly int LocalPlayerId;
            public readonly string GameId;
            private readonly IntPtr _component;
            private readonly IntPtr _accountId;
            private readonly int _opponentId;

            public Match()
            {
                try
                {
                    var managerType = Api.Type("CubeUnity.App.State", "GameManager");
                    var manager = Api.Manager(managerType);
                    var current = Api.Call(managerType, manager, "get_CurrentGame");
                    var localType = Api.Type("CubeUnity.App.State", "LocalGame");
                    if (!Api.Runtime.IsInstance(current, localType) || Api.Value<bool>(managerType, manager, "get_IsReplay"))
                        throw new InvalidOperationException("These controls require a local game.");
                    Server = Api.Call(localType, current, "get_CubeGame");
                    var componentType = Api.Type("CubeUnity.App.Game", "LocalGame");
                    var gameField = Api.Runtime.Field(componentType, "_game");
                    var matches = Api.FindObjects(componentType).Where(obj => Server != IntPtr.Zero
                        && !Api.Value<bool>(componentType, obj, "get_ShouldStopProcessing")
                        && Api.Runtime.ReadReference(obj, gameField) == Server).ToArray();
                    if (matches.Length != 1) throw new InvalidOperationException("Wait for the local board to finish loading.");
                    _component = matches[0];
                    State = Api.Call(managerType, manager, "get_GameState");
                    LocalPlayerId = Api.Value<int>(managerType, manager, "get_LocalPlayerEntityId");
                    _opponentId = Api.Value<int>(managerType, manager, "get_OpponentPlayerEntityId");
                    if (State == IntPtr.Zero || LocalPlayerId == 0 || _opponentId == 0)
                        throw new InvalidOperationException("Wait for both local players to join.");
                    _accountId = Api.Call(managerType, manager, "get_LocalPlayerAccountId");
                    GameId = Api.Runtime.StringValue(Api.Call(Api.Type("CubeGame", "Game"), State, "get_Id"));
                }
                catch { Api.Dispose(); throw; }
            }

            public int[] PlayerIds(PlayerTarget target) => target == PlayerTarget.Both ? new[] { LocalPlayerId, _opponentId }
                : new[] { target == PlayerTarget.Me ? LocalPlayerId : _opponentId };

            public string LocalAccountId => Api.Runtime.StringValue(_accountId);

            public int EntityId(IntPtr entity) => Api.Value<int>(Api.Type("CubeGame", "Entity"), entity, "get_EntityId");
            public int Owner(IntPtr card) => EntityId(Api.Call(Api.Type("CubeGame", "Card"), card, "get_Owner"));

            public IntPtr FindCard(MatchCard card)
            {
                var found = card == null || card.GameId != GameId ? IntPtr.Zero
                    : new[] { CleanupTarget.Board, CleanupTarget.Hands, CleanupTarget.Decks }
                        .SelectMany(zone => Cards(Server, zone)).FirstOrDefault(c => EntityId(c) == card.EntityId);
                if (found == IntPtr.Zero) throw new InvalidOperationException("Refresh match cards and select a card still on the board, in hand, or in the deck.");
                return found;
            }

            public IntPtr[] Augments(IntPtr card)
            {
                var system = Api.Call(Api.Type("CubeGame", "Game"), Server, "get_AugmentSystem");
                return Api.References(Api.Call(Api.Type("CubeGame", "AugmentSystem"), system, "GetAugments", A.Ref(card)));
            }

            public int CountAugment(IntPtr card, string id) => Augments(card).Count(augment => string.Equals(id,
                Api.Runtime.StringValue(Api.Value<IntPtr>(Api.Type("Cube.Common", "Augment"), augment, "get_AugmentDefId")),
                StringComparison.OrdinalIgnoreCase));

            public T WithSimulationLock<T>(Func<T> action, Func<T> onBusy = null)
            {
                var sync = Api.Call(Api.Type("CubeUnity.App.Game", "LocalGame"), _component, "get_GameSimLock");
                var monitor = Api.Type("System.Threading", "Monitor");
                var enter = Api.Runtime.Method(monitor, "TryEnter", true, "System.Object", "System.Boolean&");
                var exit = Api.Runtime.Method(monitor, "Exit", true, "System.Object");
                var lockTaken = A.Value(false);
                try
                {
                    Api.Runtime.Invoke(enter, IntPtr.Zero, A.Ref(sync), lockTaken);
                    if (lockTaken.ValueBytes[0] == 0)
                    {
                        if (onBusy != null) return onBusy();
                        throw new InvalidOperationException("The local simulation is busy. Wait for the current action to finish and try again.");
                    }
                    return action();
                }
                finally
                {
                    if (lockTaken.ValueBytes[0] != 0) Api.Runtime.Invoke(exit, IntPtr.Zero, A.Ref(sync));
                }
            }

            public IntPtr[] Cards(CleanupTarget zone) => Cards(State, zone);

            public IntPtr[] Cards(IntPtr game, CleanupTarget zone)
            {
                var type = Api.Type("CubeGame", "Game");
                if (zone == CleanupTarget.Board)
                {
                    var method = Api.Runtime.Method(type, "GetAllCardsAtAllLocations", false, "CubeGame.Location");
                    return Api.References(Api.Runtime.Invoke(method, game, A.Ref(IntPtr.Zero)))
                        .Concat(Api.References(Api.Call(type, game, "GetAllCardsStagedAtAllLocations", A.Ref(IntPtr.Zero)))).Distinct().ToArray();
                }
                return Api.References(Api.Call(type, game,
                    zone == CleanupTarget.Hands ? "GetAllCardsInPlayersHands" : "GetAllCardsInPlayersDecks"));
            }

            public void Send(string requestName, Action<IL2CppClassWrapper, IntPtr> configure)
            {
                var type = Api.Type("CubeGame", requestName);
                var request = Api.New(type);
                Api.Call(type, request, "set_GameId", A.Ref(Api.Runtime.String(GameId)));
                Api.Call(type, request, "set_AccountId", A.Ref(_accountId));
                configure(type, request);
                Api.Call(Api.Type("CubeUnity.App.Game", "LocalGame"), _component, "SendRequest", A.Ref(request));
            }

            public void Dispose() => Api.Dispose();
        }
    }
}
