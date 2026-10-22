using System;
using A = IL2CppApi.Runtime.Il2CppArgument;

namespace SnapLocalGameLab.Core
{
    internal static class AugmentConfiguration
    {
        public static IntPtr Rule(GameApi api, IntPtr config)
        {
            var rules = api.Call(api.Type("CubeDef", "GameConfigDef"), config, "get_GameRulesDef");
            return rules == IntPtr.Zero ? IntPtr.Zero
                : api.Call(api.Type("CubeDef", "GameRulesDef"), rules, "get_CardAugment");
        }

        public static int Limit(GameApi api, IntPtr rule) => rule == IntPtr.Zero ? 0 : api.Runtime.ReadField<int>(rule,
            api.Runtime.Field(api.Type("CubeDef", "CardAugmentRuleDef"), "MaxAugmentsPerCard"));

        public static void Enable(GameApi api, IntPtr config)
        {
            if (Limit(api, Rule(api, config)) > 0) return;
            var configType = api.Type("CubeDef", "GameConfigDef");
            var rulesType = api.Type("CubeDef", "GameRulesDef");
            var source = api.Call(configType, config, "get_GameRulesDef");
            var rules = source == IntPtr.Zero ? api.New(rulesType)
                : api.Call(api.Type("System", "Object"), source, "MemberwiseClone");
            var rule = api.Call(api.Type("CubeDef", "CardAugmentRuleDef"), IntPtr.Zero, "GetDefaultCardAugmentRuleDef");
            api.Call(rulesType, rules, "set_CardAugment", A.Ref(rule));
            api.Call(configType, config, "set_GameRulesDef", A.Ref(rules));
        }

        public static void EnableForGame(GameApi api, IntPtr game)
        {
            var gameType = api.Type("CubeGame", "Game");
            var source = api.Call(gameType, game, "get_GameConfigDef");
            if (Limit(api, Rule(api, source)) > 0) return;
            var config = api.Call(api.Type("System", "Object"), source, "MemberwiseClone");
            Enable(api, config);
            api.Runtime.WriteReference(game, api.Runtime.Field(gameType, "_gameConfig"), config);
        }
    }
}
