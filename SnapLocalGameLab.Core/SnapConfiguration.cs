using System;
using A = IL2CppApi.Runtime.Il2CppArgument;

namespace SnapLocalGameLab.Core
{
    internal static class SnapConfiguration
    {
        public static IntPtr Find(GameApi api, int league)
        {
            var list = api.Type("CubeDef", "GameConfigDefList");
            var eventIds = api.Runtime.NestedClass(api.Type("CubeDef", "LimitedEventDef"), "Id");
            var eventIdType = api.Runtime.Method(eventIds, "Get", true, "System.String").ReturnType;
            var find = api.Runtime.Method(list, "Find", true, "CubeDef.LeagueDefId", eventIdType.Name);
            var config = api.Runtime.Invoke(find, IntPtr.Zero, A.Value(league), api.Default(find.GetParameters()[1]));
            if (config == IntPtr.Zero)
                throw new NotSupportedException("The client does not contain a game configuration for the selected league.");
            return config;
        }

        public static IntPtr Create(GameApi api, IntPtr source, LocalGameOptions options)
        {
            var configType = api.Type("CubeDef", "GameConfigDef");
            if (options.Mode == LocalGameMode.Standard && !api.Value<bool>(configType, source, "get_IsAiGame"))
                throw new InvalidOperationException("The selected league is not configured as a local AI game.");
            var config = api.Call(api.Type("System", "Object"), source, "MemberwiseClone");
            if (options.Mode == LocalGameMode.Standard)
            {
                var snapType = api.Type("CubeDef", "SnapConfigDef");
                var snap = api.New(snapType);
                var snaps = options.EnableSnapping ? options.SnapsPerPlayer : 0;
                api.Call(snapType, snap, "set_MaxNumberOfEachPlayerSnaps", A.Value(snaps));
                api.Call(snapType, snap, "set_MaxNumberOfAllPlayerSnaps", A.Value(snaps * 2));
                api.Call(snapType, snap, "set_StakesMultiplierForPlayerSnap", A.Value(2));
                api.Call(snapType, snap, "set_StakesMultiplierForLastTurnAutoSnap", A.Value(options.DoubleFinalTurn ? 2 : 1));
                api.Call(configType, config, "set_SnapConfigDef", A.Ref(snap));
                api.Call(configType, config, "set_CannotRaiseStakes", A.Value(!options.EnableSnapping));
                api.Call(configType, config, "set_StartingCubeValue", A.Value(options.StartingCubes));
                api.Call(configType, config, "set_MaxCubeValue", A.Value(options.MaximumCubes));
            }

            var ai = api.Value<int>(configType, config, "get_AiConfigDefId");
            if (!string.IsNullOrWhiteSpace(options.AiPreset))
                ai = api.EnumValue(api.Type("CubeDef", "AiConfigDefId"), options.AiPreset);
            else if (options.Mode != LocalGameMode.Standard)
                ai = api.Value<int>(configType, Find(api, api.EnumValue(api.Type("CubeDef", "LeagueDefId"), "VsBalanceAi")),
                    "get_AiConfigDefId");
            var findAi = api.Runtime.Method(api.Type("CubeGame", "AiConfigDefList"), "Find", true, "CubeDef.AiConfigDefId");
            if (ai <= 0 || api.Runtime.Invoke(findAi, IntPtr.Zero, A.Value(ai)) == IntPtr.Zero)
                throw new ArgumentException("The client does not contain a valid AI configuration for this match.");
            api.Call(configType, config, "set_AiConfigDefId", A.Value(ai));
            AugmentConfiguration.Enable(api, config);
            return config;
        }
    }
}
