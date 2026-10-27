using System.Collections.Generic;
using BundlePrefabs;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What the carved heads wear (<see cref="KwCapHeads"/>): the kit's plank wood material (the game's building shader,
    /// <c>Custom/Piece</c>, from the wood floor's own material, never its snow overlay: <see cref="KwPrefabs.Planks"/>) copied
    /// and dressed in the bundle's atlas and normal map (<see cref="GameMaterials.Dress"/>, its metal and glow cleared,
    /// <see cref="GameMaterials.Plain"/>), then tinted by the cap's keel tier as the doors are (<see cref="KwDoorLooks"/>):
    /// the atlas is a pale, grey wood by design, Meadows warms it to light wood, the later tiers darken it, Ashlands' nearly
    /// black. One copy a tier, made the first time it is asked for, shared by every head, ghost, preview and blueprint look
    /// (a launched ship takes its own ship copies, <see cref="KwShipMaterials"/>). Null on a dedicated server or without the
    /// bundle, where a head is drawn by the code in its keel's look.
    /// </summary>
    public static class KwHeadLooks
    {
        private const float Gloss = 0.15f;

        // Each tier's tint over the atlas: Meadows' light wood, Black Forest's warmer and darker, the Swamp's grey-brown, the
        // Mountains' weathered grey, the Plains' tarred brown, Yggdrasil's red-brown, Ashlands' blackwood.
        private static readonly Dictionary<string, Color> Tints = new Dictionary<string, Color>
        {
            ["meadows"] = new Color(1f, 0.88f, 0.7f),
            ["blackforest"] = new Color(0.78f, 0.6f, 0.44f),
            ["swamp"] = new Color(0.6f, 0.54f, 0.45f),
            ["mountains"] = new Color(0.62f, 0.6f, 0.56f),
            ["plains"] = new Color(0.58f, 0.44f, 0.32f),
            ["mistlands"] = new Color(0.5f, 0.38f, 0.35f),
            ["ashlands"] = new Color(0.22f, 0.19f, 0.18f),
        };

        private static readonly Dictionary<string, Material> made = new Dictionary<string, Material>();
        private static Material? plain;

        /// <summary>A tier's look for the carved heads; null where none can be made (no screen, no bundle, no plank wood).</summary>
        public static Material? For(KwBiomeTier tier)
        {
            if (made.TryGetValue(tier.Id, out Material look) && look != null)
                return look;
            Material? dressed = Plain();
            if (dressed == null)
                return null;
            look = new Material(dressed) { name = "kw_head_" + tier.Id };
            look.color = Tints.TryGetValue(tier.Id, out Color tint) ? tint : Color.white;
            return made[tier.Id] = look;
        }

        /// <summary>Whether a material is the heads' Meadows look, as a prefab wears it before its tier dresses it.</summary>
        public static bool IsMeadows(Material material) => made.TryGetValue(KwBiomeTiers.Meadows.Id, out Material look) && material == look;

        // The plank wood in the bundle's maps, made once (the bundle read first, by asking for a head).
        private static Material? Plain()
        {
            if (plain != null)
                return plain;
            if (!KwShipBody.Drawn || KwPrefabs.Planks == null || KwCapHeads.Of(KwCaps.Styles[0].Id) == null || KwCapHeads.Material == null)
                return null;
            return plain = GameMaterials.Plain(GameMaterials.Dress(KwPrefabs.Planks, KwCapHeads.Material), Gloss);
        }
    }
}
