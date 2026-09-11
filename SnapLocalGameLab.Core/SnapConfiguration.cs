using System;
using A = IL2CppApi.Runtime.Il2CppArgument;

namespace SnapLocalGameLab.Core
{
    internal static class SnapConfiguration
    {
        public static IntPtr Create(GameApi api, IntPtr source, LocalGameOptions options)
        {
            var configType = api.Type("CubeDef", "GameConfigDef");
            var config = api.Call(api.Type("System", "Object"), source, "MemberwiseClone");
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
            return config;
        }
    }
}
