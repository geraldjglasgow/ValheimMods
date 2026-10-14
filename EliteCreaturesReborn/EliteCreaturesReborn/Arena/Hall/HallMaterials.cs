using System.Collections.Generic;
using BundlePrefabs;
using EliteCreaturesReborn.Util;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Hall
{
    /// <summary>
    /// The hall's look in the game's own shaders (features/arena.md section 1, "Materials"), clients only. The stone
    /// pieces wear the game's building shader <c>Custom/Piece</c> borrowed from the stone wall's body renderer
    /// (<c>stone_wall_4x2</c>, child <c>New/stone</c>, material <c>stone_mat_4x2</c>; its first child is the snow
    /// overlay <c>floor_1x1_snow</c>, never borrowed), tinted white, wearing each piece's own albedo and normal map. Hel
    /// and her spear wear the creature shader <c>Custom/Creature</c> from the Draugr's body (<c>Draugr_mat</c>), with
    /// the shine of her metal mask. Glow parts (anything under a <c>glow*</c> child) keep the bundle's own emissive
    /// material, emission on. Each placeholder is dressed once per process and shared by every build. Read in game
    /// through DevBridge on 2026-10-12.
    /// </summary>
    internal static class HallMaterials
    {
        private const string StonePiece = "stone_wall_4x2";
        private const string StoneBody = "New";
        private const string CreaturePrefab = "Draugr";
        private const string GlowPrefix = "glow";

        // The game's stone_mat_4x2 has gloss 0.15; Draugr_mat 0.1. Hel's crown and spear head shine like the game's
        // silver gear (the Silver Shield's metal gloss, 0.65).
        private const float StoneGloss = 0.15f;
        private const float CreatureGloss = 0.15f;
        private const float MetalGloss = 0.65f;

        private static readonly Dictionary<Material, Material> Dressed = new Dictionary<Material, Material>();
        private static Material? _stone;
        private static Material? _creature;

        /// <summary>A hall piece in the game's stone; its glow parts keep their own.</summary>
        public static void DressStone(GameObject piece) => Dress(piece, Stone(), StoneGloss, 0f);

        /// <summary>A piece in the game's stone whose metal (its bundle's metal mask: iron, Bloodgold) shines at
        /// <paramref name="metalGloss"/>, as Helgrind's does (Hel's Anvil: 0.72); its glow parts keep their own.</summary>
        public static void DressStoneMetal(GameObject piece, float metalGloss) => Dress(piece, Stone(), StoneGloss, metalGloss);

        /// <summary>Hel or her spear in the game's creature shader; her glowing eye keeps its own.</summary>
        public static void DressCreature(GameObject model) => Dress(model, Creature(), CreatureGloss, MetalGloss);

        // metalGloss 0: matte, the metal mask unused.
        private static void Dress(GameObject root, Material? game, float gloss, float metalGloss)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                bool glow = IsGlow(renderer.transform, root.transform);
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] != null)
                    {
                        materials[i] = glow ? Glowing(materials[i]) : Worn(materials[i], game, gloss, metalGloss);
                    }
                }
                renderer.sharedMaterials = materials;
            }
        }

        /// <summary>True for a part at or under a child named glow...: the workshop's glow contract.</summary>
        public static bool IsGlow(Transform part, Transform root)
        {
            for (Transform? at = part; at != null && at != root; at = at.parent)
            {
                if (at.name.StartsWith(GlowPrefix, System.StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        private static Material Glowing(Material placeholder)
        {
            if (!Dressed.TryGetValue(placeholder, out Material material))
            {
                material = new Material(placeholder);
                material.EnableKeyword("_EMISSION");
                Dressed.Add(placeholder, material);
            }
            return material;
        }

        // Without the game's material (logged by Stone or Creature) a part keeps the bundle's own.
        private static Material Worn(Material placeholder, Material? game, float gloss, float metalGloss)
        {
            if (game == null)
            {
                return placeholder;
            }
            if (!Dressed.TryGetValue(placeholder, out Material material))
            {
                material = GameMaterials.Plain(GameMaterials.Dress(game, placeholder), gloss);
                if (metalGloss > 0f)
                {
                    material = GameMaterials.Metal(material, placeholder, metalGloss);
                }
                Dressed.Add(placeholder, material);
            }
            return material;
        }

        private static Material? Stone()
        {
            if (_stone == null)
            {
                _stone = GameMaterials.Borrow(ZNetScene.instance?.GetPrefab(StonePiece), StoneBody);
                Warn(_stone, StonePiece, "Custom/Piece");
            }
            return _stone;
        }

        private static Material? Creature()
        {
            if (_creature == null)
            {
                GameObject? draugr = ZNetScene.instance?.GetPrefab(CreaturePrefab);
                Material? body = draugr == null ? null : CreatureBody.MainRenderer(draugr)?.sharedMaterial;
                _creature = body == null ? null : new Material(body);
                Warn(_creature, CreaturePrefab, "Custom/Creature");
            }
            return _creature;
        }

        private static void Warn(Material? borrowed, string prefab, string shader)
        {
            if (borrowed == null || borrowed.shader.name != shader)
            {
                Log.Warn($"Hel's hall: {prefab}'s body material ({shader}) was not found as expected: "
                    + $"{(borrowed == null ? "none" : borrowed.shader.name)}.");
            }
        }
    }
}
