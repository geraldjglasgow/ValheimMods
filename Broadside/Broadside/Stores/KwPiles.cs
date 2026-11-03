using System.Collections.Generic;
using BundlePrefabs;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The ammunition piles: a cannonball pile that holds only the cannons' balls (every item of the cannonball ammunition
    /// type, <see cref="KwCannonballs.AmmoType"/>) and a black powder pile that holds only <see cref="KwBlackPowder"/>, each
    /// 4 x 2 slots; copies of the game's wood chest (its Container, whose items the game keeps in the ZDO so every player
    /// sees the same, its placement, health, wear and sounds) wearing their workshop models (<see cref="KwPileModel"/>), as
    /// full as they are (<see cref="KwPileFill"/>). Anything else is refused going in (<see cref="KwPileFilter"/>). Built on
    /// land like any chest and on a launched ship's deck, which it then rides (<see cref="KwShipRiderRules"/>); the captain's
    /// broadside reloads from the piles riding his ship first (<see cref="KwPileStore"/>, <see cref="KwPileAsk"/>). Made once
    /// when the network scene first wakes, after the storage crates, on every peer (the dedicated server too), registered
    /// every time it wakes; then the hammer is refreshed (<see cref="KwPileHammer"/>). Without its bundle a pile keeps the
    /// wood chest's look (logged once).
    /// </summary>
    public static class KwPiles
    {
        public const string Prefix = "BS_pile_";
        public const int Width = 4, Height = 2;
        private const string Source = "piece_chest_wood";
        private static readonly List<GameObject> built = new List<GameObject>();
        private static string? powder;

        /// <summary>The piles, balls first: the hammer's order. A pile's <see cref="KwPile.Kind"/> is its index here.</summary>
        public static readonly KwPileDef[] Defs =
        {
            new KwPileDef("cannonballs", "Cannonball pile", "cannonballs", IsBall, ("Wood", 8), ("Stone", 4)),
            new KwPileDef("powder", "Black powder pile", "black powder", IsPowder, ("Wood", 10), ("LeatherScraps", 2)),
        };

        /// <summary>The pile pieces, once made.</summary>
        public static IReadOnlyList<GameObject> All => built;

        /// <summary>Once, from the plugin, after the storage crates (<see cref="KwCrates.Install"/>).</summary>
        public static void Install(Harmony harmony) =>
            NetPrefabs.OnSceneAwake(harmony, scene => Guard.Run("Broadside ammunition piles", () => Build(scene)));

        /// <summary>The def of a pile kind (an index into <see cref="Defs"/>); the first for one out of range.</summary>
        public static KwPileDef Of(int kind) => Defs[kind >= 0 && kind < Defs.Length ? kind : 0];

        private static bool IsBall(ItemDrop.ItemData item) => item.m_shared.m_ammoType == KwCannonballs.AmmoType;

        // By the name inventories count it by (the ammunition store's), read from the item database once it has the powder.
        private static bool IsPowder(ItemDrop.ItemData item)
        {
            powder ??= KwArmWords.SharedName(KwBlackPowder.Name);
            return powder != null && item.m_shared.m_name == powder;
        }

        private static void Build(ZNetScene scene)
        {
            if (built.Count == 0)
                Create(scene);
            foreach (GameObject prefab in built)
                NetPrefabs.Register(scene, prefab);
            KwHammer.Refresh();
        }

        private static void Create(ZNetScene scene)
        {
            GameObject? chest = scene.GetPrefab(Source);
            if (chest == null || chest.GetComponent<Container>() == null || chest.GetComponent<Piece>() == null)
            {
                Log.Warn($"Broadside: the game's {Source} was not found; no ammunition piles.");
                return;
            }
            for (int kind = 0; kind < Defs.Length; kind++)
                built.Add(Make(chest, Defs[kind], kind));
            Log.Info($"Broadside: {built.Count} ammunition piles built.");
        }

        private static GameObject Make(GameObject chest, KwPileDef def, int kind)
        {
            GameObject copy = PrefabBench.Copy(chest, def.Prefab);
            Container box = copy.GetComponent<Container>();
            (box.m_name, box.m_width, box.m_height) = (def.Label, def.Width, def.Height);
            Piece piece = copy.GetComponent<Piece>();
            (piece.m_name, piece.m_description) = (def.Label, def.Description);
            // in the Misc tab with Broadside's pieces; the chest's own build menu filter kept, the Transportation one added
            piece.m_category = Piece.PieceCategory.Misc;
            piece.m_usage |= Piece.UsageTagFlags.Transport;
            copy.AddComponent<KwPile>().Kind = kind;
            if (KwPileModel.Wear(copy, chest, def.Bundle))
            {
                // no lid: the chest's open and closed states went with its children
                (box.m_open, box.m_closed) = (null, null);
                copy.AddComponent<KwPileFill>();
            }
            return copy;
        }
    }
}
