using System.Collections.Generic;
using BundlePrefabs;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The hammock (CLAUDE.md, "Hammock"; the user, 2026-11-03: "we need the ship hammock in broadside"): a hide sling on
    /// spreader bars, hung by its two ends, each placed by a click (<see cref="KwHammockTool"/>) and each its own mount: a
    /// free-standing post on a floor, a deck or the ground, an iron hook in anything else (the user, the same day: "If you
    /// try putting it on the ceiling or a different freestanding piece, then it should actually just be a hook."), its
    /// shape in its ZDO (<see cref="KwHammockSpec"/>), made in code (<see cref="KwHammockModel"/>). A copy of the game's
    /// bed (<c>bed</c>): its Bed (claim it, set your spawn point, sleep through the night with the bed's own checks and
    /// pose), its comfort, health, wear and sounds, its workbench (its player base area too, switched off as in the bed);
    /// its owner in its ZDO, the game's way. Built at a workbench on land and aboard a launched ship, which it then rides
    /// (<see cref="KwShipRiderRules"/>), its sling hanging plumb as the ship heels (<see cref="KwHammock"/>); a hook end
    /// comes down with what holds it (<see cref="KwHammockHold"/>); aboard, no fire is needed to sleep, and a spawn point set on it moves with
    /// the ship (<see cref="KwBedAboard"/>, <see cref="KwBedSpawn"/>, for any bed riding a ship). Made once when the
    /// network scene first wakes, after the ammunition piles, on every peer (the dedicated server too), registered every
    /// time it wakes; then the hammer is refreshed (<see cref="KwHammockHammer"/>). Its prefab name starts with
    /// <c>BS_</c>, never <c>KW_</c> (a ship part's). Without the game's bed there is no hammock (logged once).
    /// </summary>
    public static class KwHammocks
    {
        public const string Prefix = "BS_hammock", Prefab = Prefix, Label = "Hammock";
        private const string Source = "bed";
        private static readonly List<GameObject> built = new List<GameObject>();
        private static bool made;

        /// <summary>The recipe, given back when it is removed: Meadows and Black Forest things only (CLAUDE.md, Rules).</summary>
        public static readonly (string Item, int Amount)[] Cost = { ("Wood", 6), ("DeerHide", 2), ("LeatherScraps", 2) };

        /// <summary>The hammock piece, once made.</summary>
        public static IReadOnlyList<GameObject> All => built;

        /// <summary>Once, from the plugin, after the ammunition piles (<see cref="KwPiles.Install"/>).</summary>
        public static void Install(Harmony harmony) =>
            NetPrefabs.OnSceneAwake(harmony, scene => Guard.Run("Broadside hammock", () => Build(scene)));

        private static void Build(ZNetScene scene)
        {
            if (!made)
            {
                made = true;
                Create(scene);
            }
            foreach (GameObject prefab in built)
                NetPrefabs.Register(scene, prefab);
            KwHammer.Refresh();
        }

        private static void Create(ZNetScene scene)
        {
            GameObject? bed = scene.GetPrefab(Source);
            if (bed == null || bed.GetComponent<Bed>() == null || bed.GetComponent<Piece>() == null || bed.GetComponent<WearNTear>() == null)
            {
                Log.Warn($"Broadside: the game's {Source} was not found; no hammock.");
                return;
            }
            GameObject copy = PrefabBench.Copy(bed, Prefab);
            Configure(copy.GetComponent<Piece>());
            KwHammockModel.Wear(copy);
            copy.AddComponent<KwHammock>();
            built.Add(copy);
            Log.Info($"Broadside: the hammock built ({KwIcons.RenderAll(built, KwHammockShape.Stamp)} icon rendered).");
        }

        // In the Misc tab with Broadside's pieces and its build menu entry (Transportation's flag) and the game's Furniture
        // filter, at the bed's workbench; its comfort the bed's.
        private static void Configure(Piece piece)
        {
            piece.m_name = Label;
            piece.m_description = KwHammockWords.Description;
            piece.m_category = Piece.PieceCategory.Misc;
            piece.m_usage = Piece.UsageTagFlags.Transport | Piece.UsageTagFlags.Furniture;
            piece.m_enabled = true;
            // its recipe is set with the hammer (KwHammockHammer), once the item database is up
            piece.m_resources = new Piece.Requirement[0];
        }
    }
}
