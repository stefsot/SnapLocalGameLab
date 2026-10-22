using System;
using System.Collections.Generic;
using System.Linq;
using A = IL2CppApi.Runtime.Il2CppArgument;

namespace SnapLocalGameLab.Core
{
    internal sealed class CatalogItem
    {
        public string Id;
        public string Name;
        public int Kind;
        public override string ToString() => string.IsNullOrWhiteSpace(Name) || Name == Id ? Id : Name + " [" + Id + "]";
    }

    internal sealed class AvatarChoice
    {
        public string CardId;
        public int? CustomId;
        public string Label;
        public override string ToString() => Label;
    }

    internal sealed class GameCatalog
    {
        public CatalogItem[] Cards;
        public CatalogItem[] Locations;
        public CatalogItem[] Augments;
        public AvatarChoice[] Avatars;

        public static GameCatalog Load()
        {
            using (var api = new GameApi())
            {
                var cards = ReadDefs(api, "Card");
                var locations = ReadDefs(api, "Location");
                var character = api.EnumValue(api.Type("CubeDef", "CardType"), "Character");
                var augment = api.EnumValue(api.Type("CubeDef", "CardType"), "Augment");
                var avatars = cards.Where(c => c.Kind == character).Select(c => new AvatarChoice
                {
                    CardId = c.Id, Label = "Card: " + c
                }).ToList();
                var customType = api.Type("CubeDef", "CustomAvatarDef");
                foreach (var def in api.References(api.Call(api.Type("CubeProfile", "CustomAvatarDefList"), IntPtr.Zero, "get_Defs")))
                {
                    var id = api.Value<int>(customType, def, "get_CustomAvatarDefId");
                    if (id == 0) continue;
                    var name = api.Runtime.StringValue(api.Call(customType, def, "get_Name"));
                    avatars.Add(new AvatarChoice { CustomId = id, Label = "Avatar: " + name + " [" + id + "]" });
                }
                return new GameCatalog
                {
                    Cards = cards.Where(c => c.Kind != augment).ToArray(), Locations = locations,
                    Augments = cards.Where(c => c.Kind == augment).ToArray(),
                    Avatars = avatars.OrderBy(a => a.Label, StringComparer.OrdinalIgnoreCase).ToArray()
                };
            }
        }

        private static CatalogItem[] ReadDefs(GameApi api, string kind)
        {
            var type = api.Type("CubeDef", kind + "Def");
            var result = new List<CatalogItem>();
            foreach (var def in api.References(api.Call(api.Type("CubeDef", kind + "DefList"), IntPtr.Zero, "get_Defs")))
            {
                var id = api.Runtime.StringValue(api.Value<IntPtr>(type, def, "get_" + kind + "DefId"));
                if (string.IsNullOrWhiteSpace(id)) continue;
                result.Add(new CatalogItem
                {
                    Id = id, Name = api.Runtime.StringValue(api.Call(type, def, "get_Name")),
                    Kind = kind == "Card" ? api.Value<int>(type, def, "get_Type") : 0
                });
            }
            return result.GroupBy(d => d.Id).Select(g => g.First())
                .OrderBy(d => d.ToString(), StringComparer.OrdinalIgnoreCase).ToArray();
        }

        public static IntPtr FindCard(GameApi api, string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Select a card.");
            var type = api.Type("CubeDef", "CardDefList");
            var find = type.GetMethods().Single(m => m.Name == "Find" && m.ParamCount == 1
                && m.GetParameters()[0].Name.Contains("CardDef"));
            var def = api.Runtime.Invoke(find, IntPtr.Zero, A.Value(api.Runtime.String(id)));
            if (def == IntPtr.Zero) throw new ArgumentException("Unknown card: " + id);
            return def;
        }

        public static IntPtr CardId(GameApi api, string id) =>
            api.Value<IntPtr>(api.Type("CubeDef", "CardDef"), FindCard(api, id), "get_CardDefId");
    }
}
