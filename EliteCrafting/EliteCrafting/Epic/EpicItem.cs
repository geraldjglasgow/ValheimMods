using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EliteCrafting.Epic
{
    /// <summary>
    /// One Epic Loot magic item as its API hands it over (<c>GetMagicItemJson</c>): a JSON object whose fields the runes
    /// read and change, everything else (sockets, set, legendary id, augment marks of kept effects) carried through
    /// untouched. Rarity is Epic Loot's index: 0 Magic, 1 Rare, 2 Epic and up. A working copy; nothing is written to the
    /// item until <see cref="EpicApi.Apply"/>.
    /// </summary>
    internal sealed class EpicItem
    {
        private static readonly string[] RarityNames = { "Magic", "Rare", "Epic", "Legendary", "Mythic", "Ancient" };

        private const int UnknownRarity = 99;

        private readonly JObject root;

        private EpicItem(JObject root)
        {
            this.root = root;
        }

        /// <summary>The item read from Epic Loot's JSON; null for an empty or unreadable text.</summary>
        public static EpicItem? Parse(string? json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }
            try
            {
                return JToken.Parse(json!) is JObject root ? new EpicItem(root) : null;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>Epic Loot's rarity index (written as a number, the way its JSON stores the enum).</summary>
        public int Rarity
        {
            get => ReadRarity(root["Rarity"]);
            set => root["Rarity"] = value;
        }

        /// <summary>The rarity's name as Epic Loot's effect definitions key their values (<c>ValuesPerRarity</c>).</summary>
        public string RarityName => Rarity >= 0 && Rarity < RarityNames.Length ? RarityNames[Rarity] : "";

        public int EffectCount => Effects.Count;

        /// <summary>Epic Loot's name for the item; null shows the plain item name.</summary>
        public void SetDisplayName(string? name) => root["DisplayName"] = name;

        public string Json => root.ToString(Formatting.None);

        public EpicItem Copy() => new EpicItem((JObject)root.DeepClone());

        public void AddEffect(string type, float value)
        {
            Effects.Add(new JObject { ["Version"] = 1, ["EffectValue"] = value, ["EffectType"] = type });
        }

        /// <summary>The type and value of the newest effect; an empty type when the item has none.</summary>
        public (string Type, float Value) LastEffect()
        {
            if (Effects.Count == 0)
            {
                return ("", 0f);
            }
            JToken last = Effects[Effects.Count - 1];
            return ((string?)last["EffectType"] ?? "", (float?)last["EffectValue"] ?? 1f);
        }

        /// <summary>
        /// Every effect replaced by another item's (the Serpent's reroll). The augment and temper marks pointed at the old
        /// effects, so they go; sockets and their shards stay.
        /// </summary>
        public void TakeEffects(EpicItem other)
        {
            root["Effects"] = other.Effects.DeepClone();
            root["AugmentedEffectIndex"] = -1;
            root["AugmentedEffectIndices"] = new JArray();
            root["TemperedEffectIndices"] = new JArray();
        }

        private JArray Effects
        {
            get
            {
                if (root["Effects"] is JArray effects)
                {
                    return effects;
                }
                JArray fresh = new JArray();
                root["Effects"] = fresh;
                return fresh;
            }
        }

        // A number, or the enum's name when some mod made Newtonsoft write enums as text. Unknown reads as above every
        // rarity the runes know, so a rune refuses it rather than treating it as a plain item.
        private static int ReadRarity(JToken? token)
        {
            if (token == null)
            {
                return 0;
            }
            if (token.Type == JTokenType.Integer)
            {
                return (int)token;
            }
            int index = System.Array.IndexOf(RarityNames, (string?)token);
            return index < 0 ? UnknownRarity : index;
        }
    }
}
