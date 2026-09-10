using System;
using System.Linq;
using A = IL2CppApi.Runtime.Il2CppArgument;

namespace SnapLocalGameLab.Core
{
    internal static class LocalGameControls
    {
        public static string Send(string requestName, int amount = 0)
        {
            if (requestName != "DrawCardCheatRequest" && requestName != "AddEnergyCheatRequest")
                throw new ArgumentException("Unsupported local-game control.", nameof(requestName));

            using (var api = new GameApi())
            {
                var managerType = api.Type("CubeUnity.App.State", "GameManager");
                var manager = api.Manager(managerType);
                var current = api.Call(managerType, manager, "get_CurrentGame");
                if (!api.Runtime.IsInstance(current, api.Type("CubeUnity.App.State", "LocalGame")))
                    throw new InvalidOperationException("These controls only work in a local game, never online or in replays.");
                var serverGame = api.Call(api.Type("CubeUnity.App.State", "LocalGame"), current, "get_CubeGame");
                if (serverGame == IntPtr.Zero)
                    throw new InvalidOperationException("No local match is running. Click Start local game and wait for the board to load.");
                var componentType = api.Type("CubeUnity.App.Game", "LocalGame");
                var unity = api.Type("UnityEngine", "Object");
                var find = api.Runtime.Method(unity, "FindObjectsOfType", true, "System.Type");
                var objects = api.Runtime.ReferenceArray(api.Runtime.Invoke(find, IntPtr.Zero,
                    A.Ref(api.Runtime.TypeObject(componentType))));
                var gameField = api.Runtime.Field(componentType, "_game");
                var matches = objects.Where(obj => !api.Value<bool>(componentType, obj, "get_ShouldStopProcessing")
                    && api.Runtime.ReadReference(obj, gameField) == serverGame).ToArray();
                if (matches.Length != 1)
                    throw new InvalidOperationException($"Local board is not ready: {objects.Length} components, {matches.Length} matching the current match. Wait for loading to finish.");
                var playerId = api.Value<int>(managerType, manager, "get_LocalPlayerEntityId");
                if (playerId == 0) throw new InvalidOperationException("Wait until the local player has joined.");
                var requestType = api.Type("CubeGame", requestName);
                var request = api.New(requestType);
                var state = api.Call(managerType, manager, "get_GameState");
                var gameType = api.Type("CubeGame", "Game");
                api.Call(requestType, request, "set_GameId", A.Ref(api.Call(gameType, state, "get_Id")));
                api.Call(requestType, request, "set_AccountId", A.Ref(api.Call(managerType, manager, "get_LocalPlayerAccountId")));
                if (requestName == "DrawCardCheatRequest" || requestName == "AddEnergyCheatRequest")
                    api.Call(requestType, request, "set_PlayerEntityId", A.Value(playerId));
                if (requestName == "AddEnergyCheatRequest")
                    api.Call(requestType, request, "set_Amount", A.Value(amount));
                api.Call(componentType, matches[0], "SendRequest", A.Ref(request));
                return requestName + " queued for the local simulation.";
            }
        }
    }
}
