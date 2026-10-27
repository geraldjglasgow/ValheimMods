using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What each biome tier's parts look like: a copy of the kit's plank wood material (the game's Custom/Piece, so rain,
    /// snow, value noise and the build highlight still work) wearing the tier's own textures for that kind of part, painted
    /// from scratch in the asset workshop (<c>ValheimAssets/Assets/Ships/Keelwright/tier_&lt;biome&gt;</c>, the planking's
    /// in <c>tier_planking</c>): the albedo <c>kw_&lt;tier&gt;_&lt;part&gt;.png</c>, its normal map (<c>_n</c>, or for the
    /// planking its relief, <c>_h</c>, which <see cref="KwRelief"/> makes one from), where it has metal a metal mask (<c>_m</c>:
    /// red 1 on metal) and where it glows a glow map (<c>_e</c>: eitr, flametal, the molten core). The planking and keel
    /// textures repeat along u and stop at their bands in v (<see cref="KwUv"/>, <see cref="KwKeelUv"/>); the deck's repeat
    /// both ways. A tier whose albedo is not embedded keeps the plank wood's texture, logged once. Made with the kit's pieces,
    /// shared by every part, preview, ghost and launched ship, never changed; each dressed in its textures the first time
    /// it is asked for, so a tier no ship wears loads none (the planking's are 2048 by 512 px, <see cref="KwTextures"/>),
    /// and never on a dedicated server, which draws nothing.
    /// </summary>
    public static class KwBiomeLooks
    {
        private static readonly Dictionary<string, Material> made = new Dictionary<string, Material>();
        private static readonly HashSet<string> dressed = new HashSet<string>();

        // Gloss of the plain surface, smoothness of its metal (where its mask marks metal), strength of its glow (where it
        // has a glow map): plain wood is (0.18, 0.63, 1). Resin, tar, sap, chitin and marble shine a little; silver,
        // black metal and flametal at the game's own (Silver Shield 0.65, Flametal Shield 0.77).
        private static readonly Dictionary<string, (float Gloss, float Metal, float Glow)> Finish = new Dictionary<string, (float Gloss, float Metal, float Glow)>
        {
            ["kw_meadows_hull"] = (0.22f, 0.63f, 1f),
            ["kw_blackforest_deck"] = (0.34f, 0.63f, 1f),
            ["kw_mountains_hull"] = (0.18f, 0.65f, 1f),
            ["kw_mountains_keel"] = (0.18f, 0.65f, 1f),
            ["kw_plains_hull"] = (0.42f, 0.6f, 1f),
            ["kw_plains_keel"] = (0.18f, 0.6f, 1f),
            ["kw_mistlands_hull"] = (0.3f, 0.63f, 1f),
            ["kw_mistlands_deck"] = (0.3f, 0.63f, 1f),
            ["kw_mistlands_keel"] = (0.2f, 0.63f, 1.6f),
            ["kw_ashlands_hull"] = (0.18f, 0.74f, 0.9f),
            ["kw_ashlands_keel"] = (0.2f, 0.74f, 2f),
        };

        public static void Make(Material? planks)
        {
            made.Clear();
            dressed.Clear();
            if (planks == null)
                return;
            foreach (KwBiomeTier tier in KwBiomeTiers.All)
            {
                foreach (KwPart part in new[] { KwPart.Hull, KwPart.Keel, KwPart.Deck })
                {
                    string name = Name(part, tier);
                    made[name] = new Material(planks) { name = name, color = Color.white, mainTextureScale = Vector2.one, mainTextureOffset = Vector2.zero };
                }
            }
            Log.Info($"Broadside: {made.Count} biome looks, each dressed when first worn.");
        }

        /// <summary>A tier's look for a kind of part; Meadows' when that tier has none; null before the kit is made.</summary>
        public static Material? For(KwPart part, KwBiomeTier tier) => Look(Name(part, tier), part) ?? Look(Name(part, KwBiomeTiers.Meadows), part);

        // A made look, dressed in its textures the first time it is asked for (on a screen only).
        private static Material? Look(string name, KwPart part)
        {
            if (!made.TryGetValue(name, out Material look))
                return null;
            if (dressed.Add(name) && KwShipBody.Drawn)
                Dress(look, Wrap(part));
            return look;
        }

        /// <summary>
        /// The look a blueprint's generated part wears, from its saved tier; null for a part the tiers do not dress (a railing,
        /// whose tier is its cost only, <see cref="KwRailLooks"/>, a ladder, a mast: the plank wood).
        /// </summary>
        public static Material? Of(string tool, List<KwBpValue> data)
        {
            KwPart? part = KwBiomeTiers.PartOf(tool);
            return part == null ? null : For(part.Value, KwBiomeTiers.Of(data));
        }

        /// <summary>
        /// The look a kit prefab wears as it is made: Meadows' keel for the keels, the ribs, the post caps and the keel and
        /// rib tools; Meadows' planking and deck for those generated parts (each part dresses itself by its tier when it
        /// appears, <see cref="KwBiomeDress"/>); null for every other piece.
        /// </summary>
        public static Material? Prefab(KwDef def)
        {
            KwPart? part = PrefabPart(def.Name);
            return part == null ? null : For(part.Value, KwBiomeTiers.Meadows);
        }

        private static KwPart? PrefabPart(string name)
        {
            if (name == KwToolPieces.PartPrefix + "planking")
                return KwPart.Hull;
            if (name == KwToolPieces.PartPrefix + "deck")
                return KwPart.Deck;
            foreach (string keel in new[] { KwCaps.Prefix, KwKeel.MadeName, KwRibs.Made, "KW_tool_keel", "KW_tool_rib" })
            {
                if (name.StartsWith(keel, System.StringComparison.Ordinal))
                    return KwPart.Keel;
            }
            return null;
        }

        private static string Name(KwPart part, KwBiomeTier tier) => $"kw_{tier.Id}_{part.ToString().ToLowerInvariant()}";

        // The planking and keel repeat along their length and keep to their bands across; a deck repeats both ways.
        private static (TextureWrapMode U, TextureWrapMode V) Wrap(KwPart part) =>
            part == KwPart.Deck ? (TextureWrapMode.Repeat, TextureWrapMode.Repeat) : (TextureWrapMode.Repeat, TextureWrapMode.Clamp);

        private static void Dress(Material look, (TextureWrapMode U, TextureWrapMode V) wrap)
        {
            string name = look.name;
            Texture2D? albedo = KwTextures.Load(name + ".png", false, wrap.U, wrap.V);
            if (albedo == null)
            {
                Log.Warn($"Broadside: {name}.png is not embedded; that look wears the plank wood.");
                return;
            }
            (float gloss, float metal, float glow) = Finish.TryGetValue(name, out var finish) ? finish : (0.18f, 0.63f, 1f);
            look.mainTexture = albedo;
            Set(look, "_BumpMap", KwTextures.Load(name + "_n.png", true, wrap.U, wrap.V) ?? KwRelief.Normals(name + "_h.png", wrap.U, wrap.V));
            Metal(look, KwTextures.Load(name + "_m.png", true, wrap.U, wrap.V), metal);
            Glow(look, KwTextures.Load(name + "_e.png", false, wrap.U, wrap.V), glow);
            Number(look, "_Glossiness", gloss);
        }

        // Metal through a mask: red 1 on metal texels, the game's way for pieces (_MetallicTex); without one, no metal.
        private static void Metal(Material look, Texture2D? mask, float smoothness)
        {
            Set(look, "_MetallicTex", mask);
            Number(look, "_Metallic", mask != null ? 1f : 0f);
            Number(look, "_MetallicAlphaGloss", mask != null ? smoothness : 0f);
        }

        // A glow map in the colour of the glow, at the tier's strength; without one, the emission stays black.
        private static void Glow(Material look, Texture2D? map, float strength)
        {
            Set(look, "_EmissionMap", map);
            if (look.HasProperty("_EmissionColor"))
                look.SetColor("_EmissionColor", map != null ? Color.white * strength : Color.black);
        }

        private static void Set(Material look, string property, Texture2D? texture)
        {
            if (look.HasProperty(property))
                look.SetTexture(property, texture);
        }

        private static void Number(Material look, string property, float value)
        {
            if (look.HasProperty(property))
                look.SetFloat(property, value);
        }
    }
}
