using System;
using System.Collections.Generic;
using System.Linq;
using A = IL2CppApi.Runtime.Il2CppArgument;

namespace SnapLocalGameLab.Core
{
    internal static class ChampionArenaPresentation
    {
        public static IntPtr Create(GameApi api, LocalGameOptions options, int league, out IntPtr boardId)
        {
            var selected = new HashSet<string>(new[] { options.PlayerChampion, options.OpponentChampion }, StringComparer.OrdinalIgnoreCase);
            var groupType = api.Type("CubeDef", "DeckGroupDef");
            var groups = new Dictionary<string, IntPtr>(StringComparer.OrdinalIgnoreCase);
            foreach (var group in api.References(api.Call(api.Type("CubeDef", "DeckGroupDefList"), IntPtr.Zero, "get_Defs")))
            {
                var cardId = api.Runtime.StringValue(api.Value<IntPtr>(groupType, group, "get_PreviewCardId"));
                if (cardId == null || !selected.Contains(cardId)) continue;
                var view = api.Runtime.StringValue(api.Call(groupType, group, "get_ViewDefName"));
                if (string.IsNullOrWhiteSpace(view) || view.IndexOf("_CI_", StringComparison.OrdinalIgnoreCase) >= 0
                    || view.IndexOf("_Test_", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                if (!groups.ContainsKey(cardId)) groups.Add(cardId, group);
            }
            var missing = selected.Where(id => !groups.ContainsKey(id)).ToArray();
            if (missing.Length != 0)
                throw new NotSupportedException("The client has no champion portrait mapping for " + string.Join(", ", missing) + ".");

            var boardType = api.Type("CubeDef", "GameBoardDef");
            boardId = IntPtr.Zero;
            foreach (var board in api.References(api.Call(api.Type("CubeDef", "GameBoardDefList"), IntPtr.Zero, "get_Defs")))
            {
                var id = api.Value<IntPtr>(boardType, board, "get_GameBoardDefId");
                if (!string.Equals(api.Runtime.StringValue(id), "ChampionArena", StringComparison.OrdinalIgnoreCase)) continue;
                boardId = api.Runtime.Retain(id);
                break;
            }
            if (boardId == IntPtr.Zero)
                throw new NotSupportedException("The client does not contain the Champion Arena board definition.");

            var groupId = api.Runtime.Method(groupType, "get_DefId", false);
            var arrayType = api.Type("System", "Array");
            var createArray = api.Runtime.Method(arrayType, "CreateInstance", true, "System.Type", "System.Int32");
            var groupIds = api.Runtime.Invoke(createArray, IntPtr.Zero,
                A.Ref(api.Runtime.TypeObject(api.Runtime.ClassFromType(groupId.ReturnType))), A.Value(groups.Count));
            var set = api.Runtime.Method(arrayType, "SetValue", false, "System.Object", "System.Int32");
            var index = 0;
            foreach (var group in groups.Values)
                api.Runtime.Invoke(set, groupIds, A.Ref(api.Runtime.Invoke(groupId, group)), A.Value(index++));

            var customType = api.Type("CubeLimitedTimeEvents.GameModes.CustomGame", "CustomGameEventDef");
            var custom = api.New(customType);
            api.Call(customType, custom, "set_LeagueDefId", A.Value(league));
            api.Call(customType, custom, "set_ViewDefName", A.Ref(api.Runtime.String("ChampionArena")));
            api.Call(customType, custom, "set_GameBoardDefId", A.Value(boardId));
            api.Call(customType, custom, "set_DeckGroupIds", A.Ref(groupIds));
            var eventType = api.Type("CubeDef", "LimitedEventDef");
            var eventDef = api.New(eventType);
            api.Call(eventType, eventDef, "set_CustomGameEventDef", A.Ref(custom));

            var providerType = api.Type("CubeUnity.App.State.ViewModels", "CustomGameEventDataProvider");
            var provider = api.New(providerType);
            Set("get_EventDef", A.Ref(eventDef));
            Set("get_ViewDefName", A.Ref(api.Runtime.String("ChampionArena")));
            Set("get_LeagueDefId", A.Value(league));
            api.Call(providerType, provider, "set_GameBoardDefId", A.Value(boardId));
            return provider;

            void Set(string getter, A value)
            {
                var property = api.Call(providerType, provider, getter);
                api.Call(api.ReturnType(providerType, getter), property, "set_Value", value);
            }
        }
    }
}
