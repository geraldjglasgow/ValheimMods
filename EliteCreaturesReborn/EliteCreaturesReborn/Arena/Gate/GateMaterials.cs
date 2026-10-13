using System.Collections.Generic;
using BundlePrefabs;
using EliteCreaturesReborn.Arena.Hall;
using EliteCreaturesReborn.Raids;
using EliteCreaturesReborn.Util;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Gate
{
    /// <summary>
    /// Helgrind's look in the hall's stone, clients only (a dedicated server keeps the bundle's materials, never drawn).
    /// The stone portal's own body is <c>Custom/StaticRock</c>; the gate wears what the hall's pieces wear instead
    /// (<see cref="HallMaterials"/>), so it matches the hall it leads to: the game's building shader <c>Custom/Piece</c>
    /// from the stone wall's body (<c>stone_wall_4x2</c>, child <c>New</c>; never its snow overlay), white, gloss 0.15, the
    /// gate's albedo and normal map, and its Bloodgold shining through the gate's metal mask at the Nord Buckler's 0.76.
    /// Glow parts (under a <c>glow*</c> child) keep the bundle's own emissive material, emission on. Read from the scene's
    /// prefab list as the gate is made, before the scene has woken.
    /// </summary>
    internal static class GateMaterials
    {
        private const string StonePiece = "stone_wall_4x2";
        private const string StoneBody = "New";
        private const float StoneGloss = 0.15f;
        private const float MetalGloss = 0.76f;

        public static void Dress(GameObject model, List<GameObject> prefabs)
        {
            if (HallBuilder.NoScreen)
            {
                return;
            }
            Material? stone = GameMaterials.Borrow(ChestPrefab.Find(prefabs, StonePiece), StoneBody);
            if (stone == null || stone.shader.name != "Custom/Piece")
            {
                Log.Warn($"Helgrind: {StonePiece}'s body material (Custom/Piece) was not found as expected: "
                    + $"{(stone == null ? "none" : stone.shader.name)}.");
            }
            var dressed = new Dictionary<Material, Material>();
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                Swap(renderer, HallMaterials.IsGlow(renderer.transform, model.transform), stone, dressed);
            }
        }

        private static void Swap(Renderer renderer, bool glow, Material? stone, Dictionary<Material, Material> dressed)
        {
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] != null)
                {
                    materials[i] = For(materials[i], stone, glow, dressed);
                }
            }
            renderer.sharedMaterials = materials;
        }

        // Without the game's stone (logged above) a part keeps the bundle's own.
        private static Material For(Material placeholder, Material? stone, bool glow, Dictionary<Material, Material> dressed)
        {
            if (dressed.TryGetValue(placeholder, out Material made))
            {
                return made;
            }
            if (glow)
            {
                made = new Material(placeholder);
                made.EnableKeyword("_EMISSION");
            }
            else
            {
                made = stone == null ? placeholder : GameMaterials.Metal(
                    GameMaterials.Plain(GameMaterials.Dress(stone, placeholder), StoneGloss), placeholder, MetalGloss);
            }
            dressed.Add(placeholder, made);
            return made;
        }
    }
}
