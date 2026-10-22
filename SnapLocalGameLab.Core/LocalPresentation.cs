using System;
using System.Linq;
using A = IL2CppApi.Runtime.Il2CppArgument;

namespace SnapLocalGameLab.Core
{
    internal sealed class LocalPresentation
    {
        private readonly string _playerName;
        private readonly string _opponentName;
        private readonly IntPtr _playerAvatar;
        private readonly IntPtr _opponentAvatar;

        public LocalPresentation(GameApi api, PresentationOptions options)
        {
            _playerName = options.PlayerName;
            _opponentName = options.OpponentName;
            _playerAvatar = CreateAvatar(api, options.PlayerAvatar);
            _opponentAvatar = CreateAvatar(api, options.OpponentAvatar);
        }

        public void Apply(GameApi api, IntPtr game, string localAccount)
        {
            if (game == IntPtr.Zero) return;
            var playerType = api.Type("CubeGame", "Player");
            var infoType = api.Type("Cube.Common", "PlayerInfo");
            foreach (var player in api.References(api.Call(api.Type("CubeGame", "Game"), game, "get_Players")))
            {
                var info = api.Call(playerType, player, "get_PlayerInfo");
                if (info == IntPtr.Zero) continue;
                var account = api.Runtime.StringValue(api.Call(infoType, info, "get_AccountId"));
                var mine = account == localAccount;
                var name = mine ? _playerName : _opponentName;
                var avatar = mine ? _playerAvatar : _opponentAvatar;
                if (string.IsNullOrWhiteSpace(name) && avatar == IntPtr.Zero) continue;
                var copy = api.Call(api.Type("System", "Object"), info, "MemberwiseClone");
                if (!string.IsNullOrWhiteSpace(name)) api.Call(infoType, copy, "set_Name", A.Ref(api.Runtime.String(name.Trim())));
                if (avatar != IntPtr.Zero) api.Call(infoType, copy, "set_Avatar", A.Ref(avatar));
                api.Call(playerType, player, "set_PlayerInfo", A.Ref(copy));
            }
        }

        public void ApplyToBoard(GameApi api, IntPtr game, string localAccount)
        {
            var playerType = api.Type("CubeGame", "Player");
            var controllerType = api.Type("CubeUnity.App.Game", "PlayerController");
            var players = api.References(api.Call(api.Type("CubeGame", "Game"), game, "get_Players"));
            var controllers = api.FindObjects(controllerType).Where(controller =>
                players.Contains(api.Call(controllerType, controller, "get_PlayerModel"))).ToArray();
            if (controllers.Length == 0) throw new InvalidOperationException("Wait for the player portraits to finish loading.");
            var infoType = api.Type("Cube.Common", "PlayerInfo");
            foreach (var controller in controllers)
            {
                var player = api.Call(controllerType, controller, "get_PlayerModel");
                var info = api.Call(playerType, player, "get_PlayerInfo");
                var mine = api.Runtime.StringValue(api.Call(infoType, info, "get_AccountId")) == localAccount;
                var name = mine ? _playerName : _opponentName;
                var avatar = mine ? _playerAvatar : _opponentAvatar;
                if (!string.IsNullOrWhiteSpace(name))
                {
                    var label = api.Runtime.ReadReference(controller, api.Runtime.Field(controllerType, "_NameText"));
                    if (label != IntPtr.Zero)
                        api.Call(api.Type("TMPro", "TMP_Text"), label, "set_text", A.Ref(api.Runtime.String(name.Trim())));
                }
                if (avatar != IntPtr.Zero)
                {
                    var avatarType = api.Type("CubeUnity.App.Game", "Avatar");
                    var widget = api.Runtime.ReadReference(controller, api.Runtime.Field(controllerType, "_Avatar"));
                    if (widget == IntPtr.Zero) continue;
                    var view = api.Runtime.ReadReference(widget, api.Runtime.Field(avatarType, "_AvatarView"));
                    if (view != IntPtr.Zero) api.Call(api.Type("CubeUnity.App.View", "AvatarView"), view, "Load", A.Ref(avatar));
                }
            }
        }

        private static IntPtr CreateAvatar(GameApi api, AvatarChoice choice)
        {
            if (choice == null) return IntPtr.Zero;
            var type = api.Type("Cube.Common", "Avatar");
            if (choice.CustomId.HasValue)
                return api.Call(type, IntPtr.Zero, "Create", A.Value(choice.CustomId.Value), A.Value(0));
            var create = api.Method(type, "Create", 4, true);
            return api.Runtime.Invoke(create, IntPtr.Zero, A.Value(GameCatalog.CardId(api, choice.CardId)),
                api.Default(create.GetParameters()[1]), A.Value(0), A.Value(0));
        }
    }
}
